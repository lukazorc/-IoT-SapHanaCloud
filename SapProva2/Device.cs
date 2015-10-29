using System;
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;

namespace SapProva2
{
    public class Device
    {
        /*RaspBerry Pi2  Parameters*/
        private const string SpiControllerName = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private const int SpiChipSelectLine = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */

        /*Uncomment if you are using mcp3208/3008 which is 12 bits output */
        readonly byte[] _readBuffer = new byte[3]; /*this is defined to hold the output data*/
        readonly byte[] _writeBuffer = new byte[3] { 0x06, 0x00, 0x00 };//00000110 00; // It is SPI port serial input pin, and is used to load channel configuration data into the device

        /*Uncomment if you are using mcp3002*/
        /* byte[] readBuffer = new byte[3]; /*this is defined to hold the output data*/
        // byte[] writeBuffer = new byte[3] { 0x68, 0x00, 0x00 };//01101000 00; /* It is SPI port serial input pin, and is used to load channel configuration data into the device*/

        private SpiDevice _spiDisplay;
        private const int LedPin = 5;
        public GpioPin Pin { get; set; }
        public GpioPinValue PinValue { get; set; }

        public void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                Pin = null;
                Debug.WriteLine("There is no GPIO controller on this device.");
                return;
            }

            Pin = gpio.OpenPin(LedPin);
            PinValue = GpioPinValue.High;
            Pin.Write(PinValue);
            Pin.SetDriveMode(GpioPinDriveMode.Output);

            Debug.WriteLine("GPIO pin initialized correctly.");
        }

        public async void InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SpiChipSelectLine);
                settings.ClockFrequency = 500000; // 10000000;
                settings.Mode = SpiMode.Mode0; //Mode3;

                var spiAqs = SpiDevice.GetDeviceSelector(SpiControllerName);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                _spiDisplay = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);
            }

            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        public double Result()
        {
            _spiDisplay.TransferFullDuplex(_writeBuffer, _readBuffer);
            var temp = ConvertToDouble(_readBuffer);
            return temp;
        }

        private static double ConvertToDouble(byte[] data)
        {
            /*Uncomment if you are using mcp3208/3008 which is 12 bits output */
            var result = data[1] & 0x0F;
            result <<= 8;
            result += data[2];
            var millivolts = Convert.ToDouble(result) * (3.4 / 4095); // (VOLTS / 4095) Volte prebrat z multimetrom.
            var tempC = (millivolts - 0.5) * 100;
            return tempC;
        }
    }
}
