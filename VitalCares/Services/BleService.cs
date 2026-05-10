// Services/BleService.cs
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;
using System.Text.Json;
using VitalCares.Classes;

namespace VitalCares.Services
{
    public class BleService
    {
        private IBluetoothLE _ble;
        private IAdapter _adapter;
        private IDevice _device;
        private IService _service;
        private ICharacteristic _characteristic;

        public event EventHandler<VitalSign> DataReceived;

        private const string SERVICE_UUID = "4fafc201-1fb5-459e-8fcc-c5c9c331914b";
        private const string CHARACTERISTIC_UUID = "beb5483e-36e1-4688-b7f5-ea07361b26a8";

        public BleService()
        {
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
        }

        public async Task<bool> ConnectToDevice()
        {
            try
            {
                // Scanează dispozitivele
                _adapter.ScanMode = ScanMode.LowLatency;
                _adapter.ScanTimeout = 10000;

                await _adapter.StartScanningForDevicesAsync();

                // Caută dispozitivul "VitalCares Monitor"
                var targetDevice = _adapter.DiscoveredDevices.FirstOrDefault(d => d.Name == "VitalCares Monitor");

                if (targetDevice == null)
                {
                    System.Diagnostics.Debug.WriteLine("Dispozitivul nu a fost găsit");
                    return false;
                }

                _device = targetDevice;

                // Conectare
                await _adapter.ConnectToDeviceAsync(_device);
                System.Diagnostics.Debug.WriteLine("Conectat la dispozitiv");

                // Obține service-ul
                _service = await _device.GetServiceAsync(Guid.Parse(SERVICE_UUID));
                if (_service == null)
                {
                    System.Diagnostics.Debug.WriteLine("Service-ul nu a fost găsit");
                    return false;
                }

                // Obține characteristic-ul
                _characteristic = await _service.GetCharacteristicAsync(Guid.Parse(CHARACTERISTIC_UUID));
                if (_characteristic == null)
                {
                    System.Diagnostics.Debug.WriteLine("Characteristic-ul nu a fost găsit");
                    return false;
                }

                // Activează notificările
                _characteristic.ValueUpdated += OnCharacteristicValueUpdated;
                await _characteristic.StartUpdatesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Eroare conectare: {ex.Message}");
                return false;
            }
        }

        private void OnCharacteristicValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var bytes = e.Characteristic.Value;
            var jsonString = Encoding.UTF8.GetString(bytes);

            try
            {
                // Parsează JSON-ul primit
                var jsonDoc = JsonDocument.Parse(jsonString);
                var vitalSign = new VitalSign
                {
                    Timestamp = DateTime.Now,
                    Puls = jsonDoc.RootElement.GetProperty("puls").GetDouble(),
                    Temperatura = jsonDoc.RootElement.GetProperty("temperatura").GetDouble(),
                    Umiditate = jsonDoc.RootElement.GetProperty("umiditate").GetDouble(),
                    SpO2 = jsonDoc.RootElement.GetProperty("spo2").GetDouble()
                };

                // Declanșează evenimentul
                DataReceived?.Invoke(this, vitalSign);

                System.Diagnostics.Debug.WriteLine($"Date primite: Puls={vitalSign.Puls}, Temp={vitalSign.Temperatura}°C");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Eroare parsare date: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            _characteristic?.StopUpdatesAsync();
            _adapter?.DisconnectDeviceAsync(_device);
        }
    }
}