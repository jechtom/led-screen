using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController
{
    public class WeatherLoader
    {
        private HttpClient _httpClient;

        public string City = "Melnik,CZE";
        public string APIKey = "7c48b344fc6772d5612a63ad702debed";

        public WeatherLoader()
        {
            _httpClient = new HttpClient();
        }

        public async Task<(decimal Temp, string Icon)> RefreshAsync()
        {
            string url = $"http://api.openweathermap.org/data/2.5/weather?q={City}&mode=json&units=metric&APPID={APIKey}";
            string json = await _httpClient.GetStringAsync(url);
            var weather = JObject.Parse(json);
            Console.WriteLine($"Weather data:\n{json}");
            return (weather["main"]["temp"].Value<decimal>(), weather["weather"].First["icon"].Value<string>() ?? string.Empty);
        }

        /* Example:
        {
            "coord":{
                "lon":14.4741,
                "lat":50.3505
            },
            "weather":[
                {
                    "id":804,
                    "main":"Clouds",
                    "description":"overcast clouds",
                    "icon":"04n"
                }
            ],
            "base":"stations",
            "main":{
                "temp":5.07,
                "feels_like":5.07,
                "temp_min":4.04,
                "temp_max":6.53,
                "pressure":1013,
                "humidity":77
            },
            "visibility":10000,
            "wind":{
                "speed":1.26,
                "deg":324,
                "gust":1.45
            },
            "clouds":{
                "all":100
            },
            "dt":1637535954,
            "sys":{
                "type":2,
                "id":2019943,
                "country":"CZ",
                "sunrise":1637562449,
                "sunset":1637593762
            },
            "timezone":3600,
            "id":3070862,
            "name":"Mělník",
            "cod":200
        }
        */
    }
}
