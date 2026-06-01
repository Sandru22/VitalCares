using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.LocalNotification;
using System;
using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Timers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using VitalCares.Services;
using VitalCares.Classes;

namespace VitalCares.Views;


// =========================================================================
// DEFINIRE CLASĂ CUSTOM PENTRU PERMISIUNILE DE BLUETOOTH PE ANDROID 12+
// =========================================================================
public class AndroidBluetoothPermissions : Permissions.BasePlatformPermission
{
#if ANDROID
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new (string androidPermission, bool isRuntime)[]
        {
            (Android.Manifest.Permission.BluetoothScan, true),
            (Android.Manifest.Permission.BluetoothConnect, true)
        };
#endif
}

public partial class ConnectionTest : ContentPage
{
    private DateTime _lastChartUpdate = DateTime.MinValue;
    private readonly Guid ServiceGuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    private readonly Guid CharacteristicGuid = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
    private const string ESP32_NAME = "VitalCares Monitor";

    public ObservableCollection<EcgPoint> EcgData { get; set; } = new ObservableCollection<EcgPoint>();
    private int _currentIndex = 0;
    private const int MaxPoints = 100;

    private readonly HttpClient _httpClient = new HttpClient();
    private bool _isAlertWindowOpen = false;
    private const string ApiAlarmUrl = "https://api.newsflowapi.uk/save_alarm.php";

    private PatientThresholds _praguriCurente = new PatientThresholds
    {
        max_puls = 93.0,
        min_puls = 68.0,
        min_spo2 = 95.0,
        max_temp = 38.5
    };

    private int CurrentPatientId => Preferences.Default.Get("CurrentPatientID", 1);

    public ConnectionTest()
    {
        InitializeComponent();
        this.BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Învelim în verificări de siguranță anti-NullReference
        if (BleManagerService.Instance != null)
        {
            BleManagerService.Instance.OnDataReceived += OnBleDataReceivedInUi;
        }

        await IncarcaPraguriPacientAsync();

        string savedId = Preferences.Default.Get("LastDeviceID", string.Empty);
        if (!string.IsNullOrEmpty(savedId) && BleManagerService.Instance?.Ble?.State == BluetoothState.On)
        {
            if (lblStatus != null) lblStatus.Text = "Status: Reconectare automată...";
            await AttemptAutoConnect(Guid.Parse(savedId));
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BleManagerService.Instance != null)
        {
            BleManagerService.Instance.OnDataReceived -= OnBleDataReceivedInUi;
        }
    }

    private void OnBleDataReceivedInUi(object sender, BleDataEventArgs e)
    {
        if (e == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (lblPuls == null || lblTemp == null || lblHum == null) return;

            lblPuls.Text = e.Puls.ToString();
            lblTemp.Text = e.Temp.ToString();
            lblHum.Text = e.Hum.ToString();

            if ((DateTime.Now - _lastChartUpdate).TotalMilliseconds > 100)
            {
                UpdateEcgChart(e.EcgValue);
                _lastChartUpdate = DateTime.Now;
            }

            CheckMedicalAlerts(e.Puls, e.SpO2, e.Temp);
        });
    }

    private async Task IncarcaPraguriPacientAsync()
    {
        try
        {
            string url = $"https://api.newsflowapi.uk/get_praguri.php?id_pacient={CurrentPatientId}";
            var praguri = await _httpClient.GetFromJsonAsync<PatientThresholds>(url);
            if (praguri != null)
            {
                _praguriCurente = praguri;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Praguri] Eroare server: {ex.Message}");
        }
    }

    private async Task AttemptAutoConnect(Guid deviceId)
    {
        try
        {
            if (BleManagerService.Instance?.Adapter == null) return;

            BleManagerService.Instance.Esp32Device = await BleManagerService.Instance.Adapter.ConnectToKnownDeviceAsync(deviceId);
            if (BleManagerService.Instance.Esp32Device != null)
            {
                var service = await BleManagerService.Instance.Esp32Device.GetServiceAsync(ServiceGuid);
                if (service == null) return;

                BleManagerService.Instance.Characteristic = await service.GetCharacteristicAsync(CharacteristicGuid);

                if (BleManagerService.Instance.Characteristic != null)
                {
                    await BleManagerService.Instance.Esp32Device.RequestMtuAsync(256);
                    BleManagerService.Instance.StartListening();
                    await BleManagerService.Instance.Characteristic.StartUpdatesAsync();

                    if (lblStatus != null) lblStatus.Text = "Status: Reconectat automat ✓";
                    if (BtnConnect != null) BtnConnect.IsVisible = false;
                    if (BtnDisconnect != null) BtnDisconnect.IsVisible = true;
                }
            }
        }
        catch (Exception ex)
        {
            if (lblStatus != null) lblStatus.Text = "Status: Auto-connect eșuat";
            System.Diagnostics.Debug.WriteLine($"Eroare auto-connect: {ex.Message}");
        }
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }

            // Aici se apelează noua logică combinată de permisiuni
            if (!await CheckPermissionsAsync())
            {
                await DisplayAlert("Permisiuni Necesare", "Aplicația are nevoie de permisiunea 'Dispozitive în apropiere' (Bluetooth Scan/Connect) pentru a detecta monitorul medical.", "OK");
                if (BtnConnect != null) BtnConnect.IsEnabled = true;
                return;
            }

            if (BleManagerService.Instance?.Ble == null || BleManagerService.Instance.Ble.State != BluetoothState.On)
            {
                await DisplayAlert("Bluetooth Dezactivat", "Te rugăm să activezi modulul Bluetooth din setările telefonului.", "OK");
                if (BtnConnect != null) BtnConnect.IsEnabled = true;
                return;
            }

            if (lblStatus != null) lblStatus.Text = "Status: Scanare...";
            if (BtnConnect != null) BtnConnect.IsEnabled = false;
            BleManagerService.Instance.Esp32Device = null;

            BleManagerService.Instance.Adapter.DeviceDiscovered += OnDeviceDiscovered;
            BleManagerService.Instance.Adapter.ScanTimeout = 10000;

            await BleManagerService.Instance.Adapter.StartScanningForDevicesAsync();
            await Task.Delay(10000);
            await BleManagerService.Instance.Adapter.StopScanningForDevicesAsync();
            BleManagerService.Instance.Adapter.DeviceDiscovered -= OnDeviceDiscovered;

            if (BleManagerService.Instance.Esp32Device == null)
            {
                if (lblStatus != null) lblStatus.Text = "Status: Monitor negăsit";
                if (BtnConnect != null) BtnConnect.IsEnabled = true;
                return;
            }

            if (lblStatus != null) lblStatus.Text = "Status: Conectare...";
            var connectParams = new Plugin.BLE.Abstractions.ConnectParameters(autoConnect: true, forceBleTransport: true);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await BleManagerService.Instance.Adapter.ConnectToDeviceAsync(BleManagerService.Instance.Esp32Device, connectParams, cts.Token);

            Preferences.Default.Set("LastDeviceID", BleManagerService.Instance.Esp32Device.Id.ToString());

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await BleManagerService.Instance.Esp32Device.RequestMtuAsync(256);
                await Task.Delay(1000);
            }

            if (BleManagerService.Instance.Esp32Device.State != Plugin.BLE.Abstractions.DeviceState.Connected)
            {
                await DisplayAlert("Eroare", "Conectarea cu ESP32 a eșuat", "OK");
                if (BtnConnect != null) BtnConnect.IsEnabled = true;
                return;
            }

            var service = await BleManagerService.Instance.Esp32Device.GetServiceAsync(ServiceGuid);
            if (service == null) return;

            BleManagerService.Instance.Characteristic = await service.GetCharacteristicAsync(CharacteristicGuid);
            if (BleManagerService.Instance.Characteristic == null) return;

            if (BleManagerService.Instance.Characteristic.CanUpdate)
            {
                BleManagerService.Instance.StartListening();
                await BleManagerService.Instance.Characteristic.StartUpdatesAsync();
            }

            if (lblStatus != null) lblStatus.Text = "Status: Conectat ✓";
            if (BtnConnect != null) { BtnConnect.IsVisible = false; BtnConnect.IsEnabled = true; }
            if (BtnDisconnect != null) BtnDisconnect.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Excepție: {ex.Message}", "OK");
            if (lblStatus != null) lblStatus.Text = "Status: Eroare";
            if (BtnConnect != null) BtnConnect.IsEnabled = true;
        }
    }

    private void OnDeviceDiscovered(object sender, DeviceEventArgs e)
    {
        var device = e.Device;
        if (device != null && !string.IsNullOrEmpty(device.Name) && device.Name.Contains(ESP32_NAME, StringComparison.OrdinalIgnoreCase))
        {
            if (BleManagerService.Instance != null)
            {
                BleManagerService.Instance.Esp32Device = device;
            }
        }
    }

    private void UpdateEcgChart(float newValue)
    {
        if (EcgData == null) return;
        EcgData.Add(new EcgPoint { Index = _currentIndex++, Value = newValue });
        if (EcgData.Count > MaxPoints)
        {
            EcgData.RemoveAt(0);
        }
    }

    // =========================================================================
    // METODĂ ACTUALIZATĂ: SOLICITĂ CORECT ȘI LOCAȚIA ȘI DISPOZITIVELE DIN APROPIERE
    // =========================================================================
    private async Task<bool> CheckPermissionsAsync()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // 1. Verifică/Cere Locația (obligatorie pentru motoarele BLE)
            var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            if (locationStatus != PermissionStatus.Granted) return false;

            // 2. Verifică/Cere "Dispozitive în apropiere" (Rezolvă eroarea de scanare Android 12+)
            var bluetoothStatus = await Permissions.CheckStatusAsync<AndroidBluetoothPermissions>();
            if (bluetoothStatus != PermissionStatus.Granted)
            {
                bluetoothStatus = await Permissions.RequestAsync<AndroidBluetoothPermissions>();
            }
            return bluetoothStatus == PermissionStatus.Granted;
        }
        return true;
    }

    private async void OnDisconnectClicked(object sender, EventArgs e)
    {
        if (BleManagerService.Instance?.Esp32Device != null)
        {
            if (BleManagerService.Instance.Characteristic != null)
            {
                await BleManagerService.Instance.Characteristic.StopUpdatesAsync();
            }

            await BleManagerService.Instance.Adapter.DisconnectDeviceAsync(BleManagerService.Instance.Esp32Device);
            if (BtnConnect != null) BtnConnect.IsVisible = true;
            if (BtnDisconnect != null) BtnDisconnect.IsVisible = false;
            if (lblStatus != null) lblStatus.Text = "Status: Deconectat";
        }
    }

    private void CheckMedicalAlerts(double puls, double spo2, double temp)
    {
        if (_isAlertWindowOpen || _praguriCurente == null) return;

        string tipParametruCritic = "";
        double valoareCritica = 0;
        string motivAlarma = "";

        if (puls > _praguriCurente.max_puls || puls < _praguriCurente.min_puls)
        {
            tipParametruCritic = "Puls";
            valoareCritica = puls;
            motivAlarma = $"Puls anormal: {puls} BPM!";
        }
        else if (spo2 < _praguriCurente.min_spo2)
        {
            tipParametruCritic = "SpO2";
            valoareCritica = spo2;
            motivAlarma = $"SpO2 critic: {spo2}%!";
        }
        else if (temp > _praguriCurente.max_temp)
        {
            tipParametruCritic = "Temperatura";
            valoareCritica = temp;
            motivAlarma = $"Febră: {temp}°C!";
        }

        if (!string.IsNullOrEmpty(tipParametruCritic))
        {
            _isAlertWindowOpen = true;
            SendNotification("ALERTĂ VITALĂ INSTANTANEE", motivAlarma);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                string selectieStare = await DisplayActionSheet(
                    title: $"⚠ Valori Critice: {motivAlarma}",
                    cancel: "Sunt OK / Ignoră Alarma",
                    destruction: null,
                    buttons: new string[] { "Mă simt amețit", "Am făcut efort fizic", "Sunt agitat", "Alt motiv nespecificat" });

                if (selectieStare != "Sunt OK / Ignoră Alarma" && selectieStare != null)
                {
                    // MODIFICARE AICI: Dacă a ales "Alt motiv", îi deschidem o casetă de text
                    if (selectieStare == "Alt motiv nespecificat")
                    {
                        string textCustom = await DisplayPromptAsync(
                            title: "Motiv Custom",
                            message: "Descrie pe scurt cum te simți sau ce s-a întâmplat:",
                            accept: "Trimite",
                            cancel: "Anulează",
                            placeholder: "Ex: Am băut cafea / Am emoții...");

                        // Dacă a completat ceva și nu a dat "Anulează", înlocuim comentariul trimis la cloud
                        if (!string.IsNullOrWhiteSpace(textCustom))
                        {
                            selectieStare = textCustom;
                        }
                        else
                        {
                            // Dacă a dat anulează sau a lăsat gol, punem un text implicit ca să nu trimitem gol la server
                            selectieStare = "Alt motiv (Pacientul nu a specificat)";
                        }
                    }

                    // Trimitem datele la cloud (acum selectieStare conține textul scris de el)
                    await TrimiteAlarmaLaCloudAsync(tipParametruCritic, valoareCritica, selectieStare);
                }
                _isAlertWindowOpen = false;
            });
        }
    }

    private async Task TrimiteAlarmaLaCloudAsync(string parametru, double valoare, string comentariu)
    {
        try
        {
            var alarmPayload = new { id_pacient = CurrentPatientId, tip_parametru = parametru, valoare_critica = valoare, mesaj_pacient = comentariu };
            string json = JsonSerializer.Serialize(alarmPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(ApiAlarmUrl, content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Alarma] Eroare rețea: {ex.Message}");
        }
    }

    private void SendNotification(string title, string message)
    {
        try
        {
            var request = new NotificationRequest
            {
                NotificationId = 1000,
                Title = title,
                Description = message,
                BadgeNumber = 1,
                Schedule = { NotifyTime = DateTime.Now }
            };
            LocalNotificationCenter.Current.Show(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Notificare] Eroare: {ex.Message}");
        }
    }
}