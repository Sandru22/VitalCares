using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Timers;
using Microsoft.Maui.ApplicationModel;
using SQLite;

namespace VitalCares.Services
{
    public class LocalMeasurement
    {
        public string tip_parametru { get; set; }
        public double valoare { get; set; }
        public string unitate_masurata { get; set; }
        public string moment_inregistrare { get; set; }
    }

    public class BleDataEventArgs : EventArgs
    {
        public double Puls { get; set; }
        public double SpO2 { get; set; }
        public double Temp { get; set; }
        public double Hum { get; set; }
        public float EcgValue { get; set; }
    }

    public class OfflinePacketEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string JsonData { get; set; }
    }

    public class BleManagerService
    {
        private static BleManagerService _instance;
        public static BleManagerService Instance => _instance ??= new BleManagerService();

        public IBluetoothLE Ble { get; }
        public IAdapter Adapter { get; }
        public IDevice Esp32Device { get; set; }
        public ICharacteristic Characteristic { get; set; }

        private System.Timers.Timer _syncTimer;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiSyncUrl = "https://api.newsflowapi.uk/save_vitals.php";

        private readonly ConcurrentBag<LocalMeasurement> _localBuffer = new ConcurrentBag<LocalMeasurement>();

        private SQLiteAsyncConnection _database;
        private bool _isSyncing = false;

        public event EventHandler<BleDataEventArgs> OnDataReceived;

        private BleManagerService()
        {
            Ble = CrossBluetoothLE.Current;
            Adapter = CrossBluetoothLE.Current.Adapter;

            InitDatabase();
            SetupSyncTimer();
        }

        private async void InitDatabase()
        {
            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitalcares_offline.db3");
                _database = new SQLiteAsyncConnection(dbPath);
                await _database.CreateTableAsync<OfflinePacketEntity>();
                System.Diagnostics.Debug.WriteLine($"[SQLite] Baza de date inițializată la: {dbPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SQLite] Eroare inițializare DB: {ex.Message}");
            }
        }

        public void StartListening()
        {
            if (Characteristic != null)
            {
                Characteristic.ValueUpdated -= OnValueUpdated;
                Characteristic.ValueUpdated += OnValueUpdated;
            }
        }

        private void SetupSyncTimer()
        {
            _syncTimer = new System.Timers.Timer(30000); // 30 secunde
            _syncTimer.Elapsed += OnSyncTimerElapsed;
            _syncTimer.AutoReset = true;
            _syncTimer.Start();
        }

        private void OnValueUpdated(object sender, CharacteristicUpdatedEventArgs args)
        {
            try
            {
                var bytes = args.Characteristic.Value;
                if (bytes == null || bytes.Length == 0) return;

                string jsonString = Encoding.UTF8.GetString(bytes);

                using JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;

                double p = root.GetProperty("puls").GetDouble();
                double s = root.GetProperty("spo2").GetDouble();
                double t = root.GetProperty("temp").GetDouble();
                double h = root.GetProperty("hum").GetDouble();
                float ecgValue = (float)root.GetProperty("ecg").GetDouble();

                string acum = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // REPARAT: Adăugăm toate datele brute în RAM, inclusiv semnalul ECG brut
                _localBuffer.Add(new LocalMeasurement { tip_parametru = "Puls", valoare = p, unitate_masurata = "bpm", moment_inregistrare = acum });
                _localBuffer.Add(new LocalMeasurement { tip_parametru = "SpO2", valoare = s, unitate_masurata = "%", moment_inregistrare = acum });
                _localBuffer.Add(new LocalMeasurement { tip_parametru = "Temperatura", valoare = t, unitate_masurata = "°C", moment_inregistrare = acum });
                _localBuffer.Add(new LocalMeasurement { tip_parametru = "Umiditate", valoare = h, unitate_masurata = "%", moment_inregistrare = acum });
                _localBuffer.Add(new LocalMeasurement { tip_parametru = "ECG", valoare = ecgValue, unitate_masurata = "raw", moment_inregistrare = acum });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnDataReceived?.Invoke(this, new BleDataEventArgs
                    {
                        Puls = p,
                        SpO2 = s,
                        Temp = t,
                        Hum = h,
                        EcgValue = ecgValue
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Eroare procesare date brute: " + ex.Message);
            }
        }

        private async void OnSyncTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isSyncing || _database == null) return;
            _isSyncing = true;

            try
            {
                var dateBruteInterval = new List<LocalMeasurement>();
                while (_localBuffer.TryTake(out var item))
                {
                    dateBruteInterval.Add(item);
                }

                if (dateBruteInterval.Any())
                {
                    var masuratoriProcesate = new List<LocalMeasurement>();
                    string momentPachet = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // REPARAT: Separăm datele care necesită medie de cele ECG care trebuie salvate brute
                    var dateStandard = dateBruteInterval.Where(x => x.tip_parametru != "ECG");
                    var dateEcg = dateBruteInterval.Where(x => x.tip_parametru == "ECG");

                    // 1. Calculăm media doar pentru parametrii lenți (Puls, Temp, Hum, SpO2)
                    var categorii = dateStandard.GroupBy(x => x.tip_parametru);
                    foreach (var grup in categorii)
                    {
                        double medieValoare = grup.Average(x => x.valoare);
                        medieValoare = grup.Key == "Puls" ? Math.Round(medieValoare) : Math.Round(medieValoare, 1);

                        masuratoriProcesate.Add(new LocalMeasurement
                        {
                            tip_parametru = grup.Key,
                            valoare = medieValoare,
                            unitate_masurata = grup.First().unitate_masurata,
                            moment_inregistrare = momentPachet
                        });
                    }

                    // 2. Pentru ECG adăugăm TOATE eșantioanele salvate, fără să le facem media
                    foreach (var punctEcg in dateEcg)
                    {
                        masuratoriProcesate.Add(new LocalMeasurement
                        {
                            tip_parametru = "ECG",
                            valoare = punctEcg.valoare,
                            unitate_masurata = "raw",
                            moment_inregistrare = punctEcg.moment_inregistrare // Păstrează timpul exact al eșantionului
                        });
                    }

                    // Conversie text JSON și salvare fizică în baza de date SQLite locală
                    string jsonPachet = JsonSerializer.Serialize(masuratoriProcesate);
                    await _database.InsertAsync(new OfflinePacketEntity { JsonData = jsonPachet });
                    System.Diagnostics.Debug.WriteLine($"[SQLite] Pachet salvat local ({masuratoriProcesate.Count} înregistrări, incluzând ECG).");
                }

                // 3. EXPEDIEREA: Dacă avem conexiune la internet, trimitem pachetele spre cloud
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    var pacheteSalvate = await _database.Table<OfflinePacketEntity>().ToListAsync();

                    if (pacheteSalvate.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"[Sync] Se trimit {pacheteSalvate.Count} pachete spre server...");
                        int currentPatientId = Preferences.Default.Get("CurrentPatientID", 1);

                        foreach (var entity in pacheteSalvate)
                        {
                            var masuratori = JsonSerializer.Deserialize<List<LocalMeasurement>>(entity.JsonData);
                            var payload = new { id_pacient = currentPatientId, masuratori = masuratori };

                            string jsonPayload = JsonSerializer.Serialize(payload);
                            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                            try
                            {
                                var response = await _httpClient.PostAsync(ApiSyncUrl, content);
                                if (response.IsSuccessStatusCode)
                                {
                                    await _database.DeleteAsync(entity);
                                    System.Diagnostics.Debug.WriteLine($"[Sync] Pachetul {entity.Id} sincronizat cu succes în Cloud.");
                                }
                                else
                                {
                                    break; // Serverul respinge pachetul (eroare cod/structură), ne oprim ca să nu pierdem datele
                                }
                            }
                            catch (Exception)
                            {
                                break; // Eroare de conexiune la internet, reluăm la tura următoare
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Eroare critică la salvare/sincronizare: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}