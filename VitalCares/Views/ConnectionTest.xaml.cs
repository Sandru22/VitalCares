using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.LocalNotification;
using System.Text;
using System.Text.Json;

namespace VitalCares.Views;

public partial class ConnectionTest : ContentPage
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private IDevice _esp32Device;
    private ICharacteristic _characteristic;

    // UUID-urile din ESP32
    private readonly Guid ServiceGuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    private readonly Guid CharacteristicGuid = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");

    // Numele ESP32-ului tău (verifică în Serial Monitor)
    private const string ESP32_NAME = "VitalCares Monitor";

    public ConnectionTest()
    {
        InitializeComponent();
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Verificăm dacă avem un ID salvat
        string savedId = Preferences.Default.Get("LastDeviceID", string.Empty);

        if (!string.IsNullOrEmpty(savedId) && _ble.State == BluetoothState.On)
        {
            lblStatus.Text = "Status: Reconectare automată...";
            await AttemptAutoConnect(Guid.Parse(savedId));
        }
    }

    private async Task AttemptAutoConnect(Guid deviceId)
    {
        try
        {
            // Încercăm să luăm dispozitivul direct din sistem (fără scanare)
            _esp32Device = await _adapter.ConnectToKnownDeviceAsync(deviceId);

            if (_esp32Device != null)
            {
                var service = await _esp32Device.GetServiceAsync(ServiceGuid);
                _characteristic = await service.GetCharacteristicAsync(CharacteristicGuid);

                var connectParams = new Plugin.BLE.Abstractions.ConnectParameters(
                    autoConnect: true,  // <-- Aceasta îi spune Android-ului să refacă conexiunea imediat ce device-ul e în zonă
                    forceBleTransport: true
                );

                if (_characteristic != null)
                {
                    await _esp32Device.RequestMtuAsync(256);
                    _characteristic.ValueUpdated += OnValueUpdated;
                    await _characteristic.StartUpdatesAsync();

                    lblStatus.Text = "Status: Reconectat automat ✓";
                    BtnConnect.IsVisible = false;
                    BtnDisconnect.IsVisible = true;
                }
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Status: Auto-connect eșuat";
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
            // Verifică permisiuni
            if (!await CheckPermissionsAsync())
            {
                await DisplayAlert("Eroare", "Permisiuni Bluetooth refuzate", "OK");
                return;
            }

            if (_ble.State != BluetoothState.On)
            {
                await DisplayAlert("Eroare", "Activează Bluetooth-ul", "OK");
                return;
            }

            lblStatus.Text = "Status: Scanare...";
            BtnConnect.IsEnabled = false;
            _esp32Device = null;

            // Configurează handler-ul ÎNAINTE de scanare
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.ScanTimeout = 10000; // 10 secunde

            // Începe scanarea
            await _adapter.StartScanningForDevicesAsync();

            // Așteaptă finalizarea scanării
            await Task.Delay(10000);
            await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceDiscovered -= OnDeviceDiscovered;

            if (_esp32Device == null)
            {
                lblStatus.Text = "Status: ESP32 nu a fost găsit";
                BtnConnect.IsEnabled = true;
                return;
            }

            // Conectare
            lblStatus.Text = "Status: Conectare...";

            var connectParams = new Plugin.BLE.Abstractions.ConnectParameters(
                autoConnect: true,
                forceBleTransport: true
            );

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await _adapter.ConnectToDeviceAsync(_esp32Device, connectParams, cts.Token);

            Preferences.Default.Set("LastDeviceID", _esp32Device.Id.ToString());

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var mtuSuccess = await _esp32Device.RequestMtuAsync(256);
                System.Diagnostics.Debug.WriteLine($"MTU setat la 256: {mtuSuccess}");
                // Un mic delay pentru a lăsa Android-ul să proceseze schimbarea de MTU
                await Task.Delay(1000);
            }

            if (_esp32Device.State != Plugin.BLE.Abstractions.DeviceState.Connected)
            {
                await DisplayAlert("Eroare", "Conectarea a eșuat", "OK");
                BtnConnect.IsEnabled = true;
                return;
            }

            // Obține service și characteristic
            lblStatus.Text = "Status: Căutare servicii...";

            var service = await _esp32Device.GetServiceAsync(ServiceGuid);
            if (service == null)
            {
                await DisplayAlert("Eroare", $"Service negăsit: {ServiceGuid}", "OK");
                await ListServicesAsync(); // Pentru debug
                return;
            }

            _characteristic = await service.GetCharacteristicAsync(CharacteristicGuid);
            if (_characteristic == null)
            {
                await DisplayAlert("Eroare", "Characteristic negăsit", "OK");
                return;
            }

            // Activează notificări
            if (_characteristic.CanUpdate)
            {
                _characteristic.ValueUpdated += OnValueUpdated;
                await _characteristic.StartUpdatesAsync();
                System.Diagnostics.Debug.WriteLine($"CanUpdate: {_characteristic.CanUpdate}");
            } else
            {
                await DisplayAlert("Eroare", "Characteristic nu suportă notificări", "OK");
                return;
            }

                lblStatus.Text = "Status: Conectat ✓";
            BtnConnect.IsVisible = false;
            BtnDisconnect.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", $"Excepție: {ex.Message}", "OK");
            lblStatus.Text = "Status: Eroare";
            BtnConnect.IsEnabled = true;
        }
    }

    // Handler pentru dispozitive descoperite
    private void OnDeviceDiscovered(object sender, DeviceEventArgs e)
    {
        var device = e.Device;
        var name = device.Name ?? "Necunoscut";

        System.Diagnostics.Debug.WriteLine($"Găsit: {name} | ID: {device.Id}");

        // Actualizează UI pentru debug
        MainThread.BeginInvokeOnMainThread(() =>
        {
            lblStatus.Text = $"Găsit: {name}";
        });

        // Verifică dacă e ESP32-ul nostru
        if (!string.IsNullOrEmpty(device.Name) &&
            device.Name.Contains(ESP32_NAME, StringComparison.OrdinalIgnoreCase))
        {
            _esp32Device = device;
            System.Diagnostics.Debug.WriteLine($"ESP32 găsit: {device.Name}");
        }
    }

    private void OnValueUpdated(object sender, CharacteristicUpdatedEventArgs args)
    {
        try
        {
            var bytes = args.Characteristic.Value;
            string jsonString = Encoding.UTF8.GetString(bytes);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(jsonString);
                    JsonElement root = doc.RootElement;

                    double p = root.GetProperty("puls").GetDouble();
                    double s = root.GetProperty("spo2").GetDouble();
                    double t = root.GetProperty("temp").GetDouble();

                    // Forțăm afișarea ca să fim siguri că firul de UI primește comanda
                    lblPuls.Text = p.ToString();
                    lblSpO2.Text = s.ToString();
                    lblTemp.Text = t.ToString();
                    lblHum.Text = root.GetProperty("hum").ToString();
                    lblEcg.Text = root.GetProperty("ecg").ToString();

                    lblStatus.Text = "Date actualizate: " + DateTime.Now.ToString("HH:mm:ss");

                    CheckMedicalAlerts(p,s,t);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Eroare JSON: " + ex.Message;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Eroare critică BLE: " + ex.Message);
        }
    }

    // Debug: listează toate serviciile
    private async Task ListServicesAsync()
    {
        if (_esp32Device == null) return;

        var services = await _esp32Device.GetServicesAsync();
        var sb = new StringBuilder("Servicii găsite:\n");

        foreach (var svc in services)
        {
            sb.AppendLine($"- {svc.Id}");
        }

        await DisplayAlert("Debug", sb.ToString(), "OK");
    }

    private async Task<bool> CheckPermissionsAsync()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
           
            // Android 12+ necesită permisiuni noi
            var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (bluetoothStatus != PermissionStatus.Granted)
            {
                bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
            }

            return locationStatus == PermissionStatus.Granted;
        }

        return true;
    }

    private async void OnDisconnectClicked(object sender, EventArgs e)
    {
        if (_esp32Device != null)
        {
            await _adapter.DisconnectDeviceAsync(_esp32Device);
            BtnConnect.IsVisible = true;
            BtnDisconnect.IsVisible = false;
            lblStatus.Text = "Status: Deconectat";
        }
    }

    private void CheckMedicalAlerts(double puls, double spo2, double temp)
    {
        string alertMessage = "";

        if (puls > MedicalThresholds.MaxPuls)
            alertMessage += $"Puls ridicat: {puls} BPM! ";

        if (spo2 < MedicalThresholds.MinSpO2)
            alertMessage += $"Saturație oxigen scăzută: {spo2}%! ";

        if (temp > MedicalThresholds.MaxTemp)
            alertMessage += $"Febră detectată: {temp}°C! ";

        if (!string.IsNullOrEmpty(alertMessage))
        {
            SendNotification("Alertă VitalCares", alertMessage);
        }
    }

    private void SendNotification(string title, string message)
    {
        // Folosind Plugin.LocalNotification
        var request = new NotificationRequest
        {
            NotificationId = 1000,
            Title = title,
            Description = message,
            BadgeNumber = 1,
            Schedule = { NotifyTime = DateTime.Now }
        };
        LocalNotificationCenter.Current.Show(request);

        // Opțional: Schimbă culoarea unui label în UI pentru feedback vizual imediat
        MainThread.BeginInvokeOnMainThread(() => {
            lblStatus.Text = "ATENȚIE: Valori anormale!";
            lblStatus.TextColor = Colors.Red;
        });
    }

    public static class MedicalThresholds
    {
        public const double MaxPuls = 93.0;
        public const double MinPuls = 68.0;
        public const double MinSpO2 = 98.0;
        public const double MaxTemp = 39.0;
    }
}
