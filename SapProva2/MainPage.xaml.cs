using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Devices.Gpio;
using Newtonsoft.Json;


namespace SapProva2
{
    public sealed partial class MainPage : Page
    {
        Device device = new Device();
        Uri uri = new Uri(
                    "https://iotmmsp1941683943trial.hanatrial.ondemand.com/com.sap.iotservices.mms/v1/api/http/data/1764772f-f48c-43b8-8186-ff41348d7796");

        public MainPage()
        {
            this.InitializeComponent();
           
            device.InitGPIO();
            device.InitSPI();
            if (device.Pin != null)
            {
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(5000);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, object e)
        {  
            Send();
            Poll();
        }

        

        private async void Send()
        {
            var temp = device.Result();

            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + "bdb31fd4655dcf1ef85823d59ba92fc");

            //Find unix timestamp (seconds since 01/01/1970)
            var ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000; //Convert windows ticks to seconds

            var postData =
                "{\"mode\":\"async\",\"messageType\":\"0c6c466ffd27fb36d0ce\",\"messages\":[{\"sensor\": \"1\",\"value\":" +temp+ ",\"timestamp\":" +ticks+ "}]}";
            var content = new HttpStringContent(postData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            var postResult = await httpClient.PostAsync(uri, content);

            try
            {
                if (postResult.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Message Sent: {0}", content);
                }
                else
                {
                    Debug.WriteLine("Failed sending message: {0}", postResult.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception when sending message:" + e.Message);
            }

        }

        private async void Poll()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + "bdb31fd4655dcf1ef85823d59ba92fc");
            var response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = responseBody.Equals("[]", StringComparison.Ordinal);
            if (result != true)
            {
                var jsonObject = JsonConvert.DeserializeObject<List<RootObject>>(responseBody); 
                
                foreach (var post in jsonObject)
                {
                    Debug.WriteLine(post.Messages[0].Operand);
                    var ledValue = post.Messages[0].Operand;
                    if (ledValue == 0)
                    {
                        device.PinValue = GpioPinValue.Low;
                        device.Pin.Write(device.PinValue);
                    }
                    else
                    {
                        device.PinValue = GpioPinValue.High;
                        device.Pin.Write(device.PinValue);
                    }
                }
            }
        }

        public class RootObject
        {
            public string MessageType { get; set; }
            public string Sender { get; set; }
            public List<RootObject2> Messages { get; set; }
        }

        public class RootObject2
        {
            public string Opcode { get; set; }
            public int Operand { get; set; }
        }
    }

    
}
