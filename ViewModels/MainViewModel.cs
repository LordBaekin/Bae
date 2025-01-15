using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using Bae.Models;
using Bae.Services;

namespace Bae.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<PacketInfo> _capturedPackets = new();
        private ILiveDevice? _selectedDevice;
        private ObservableCollection<ILiveDevice> _networkInterfaces = new();

        public ObservableCollection<PacketInfo> CapturedPackets
        {
            get => _capturedPackets;
            set
            {
                _capturedPackets = value;
                OnPropertyChanged(nameof(CapturedPackets));
            }
        }

        public ObservableCollection<ILiveDevice> NetworkInterfaces
        {
            get => _networkInterfaces;
            set
            {
                _networkInterfaces = value;
                OnPropertyChanged(nameof(NetworkInterfaces));
            }
        }

        public ILiveDevice? SelectedInterface
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedInterface));
            }
        }

        public ICommand StartCaptureCommand { get; }
        public ICommand StopCaptureCommand { get; }

        public MainViewModel()
        {
            StartCaptureCommand = new RelayCommand(StartCapture, CanStartCapture);
            StopCaptureCommand = new RelayCommand(StopCapture, CanStopCapture);

            LoadNetworkInterfaces();
            AutoStartCapture();
        }

        private void LoadNetworkInterfaces()
        {
            try
            {
                NetworkInterfaces.Clear();
                var devices = LibPcapLiveDeviceList.Instance;
                foreach (var device in devices)
                {
                    NetworkInterfaces.Add(device);
                }

                var activeDevice = GetActiveNetworkInterface();
                if (activeDevice != null)
                {
                    SelectedInterface = activeDevice;
                }
                else if (NetworkInterfaces.Any())
                {
                    SelectedInterface = NetworkInterfaces[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading network interfaces: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ILiveDevice? GetActiveNetworkInterface()
        {
            try
            {
                var activeNetworkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                          (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                                           ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

                if (activeNetworkInterface != null)
                {
                    return NetworkInterfaces.FirstOrDefault(d =>
                        d.Name == activeNetworkInterface.Name ||
                        d.Description.Contains(activeNetworkInterface.Description));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding active network interface: {ex.Message}");
            }
            return null;
        }


        private void AutoStartCapture()
        {
            if (SelectedInterface != null && CanStartCapture())
            {
                StartCapture();
            }
            else
            {
                MessageBox.Show("Unable to start capture automatically. Please select an interface and start manually.", "Auto-Start Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }





        private void StartCapture()
        {
            if (SelectedInterface == null) return;

            try
            {
                SelectedInterface.OnPacketArrival += Device_OnPacketArrival;
                SelectedInterface.Open(DeviceModes.Promiscuous);
                SelectedInterface.Filter = "ip"; // Capture only IP packets
                SelectedInterface.StartCapture();

                Console.WriteLine("Capture started.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting capture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopCapture()
        {
            if (SelectedInterface == null) return;

            SelectedInterface.StopCapture();
            SelectedInterface.Close();
            Console.WriteLine("Capture stopped.");
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var packetInfo = PacketProcessor.ProcessPacket(e);
            Application.Current.Dispatcher.Invoke(() =>
            {
                CapturedPackets.Add(packetInfo);
            });
        }

        private bool CanStartCapture() => SelectedInterface != null && !SelectedInterface.Started;
        private bool CanStopCapture() => SelectedInterface != null && SelectedInterface.Started;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}