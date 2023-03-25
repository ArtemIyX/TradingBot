using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BinanceAPIExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var baseUrl = "https://api.binance.com";
            var symbol = "BTCUSDT";
            var interval = "1m";
            var limit = 10;

            var endpoint = $"/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
            var url = baseUrl + endpoint;


            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<object[][]>(json);

                    
                    foreach (var candle in data)
                    {
                        Console.WriteLine($"[Open time: {candle[0]} {candle[0].GetType()}" +
                            $"\nOpen: {candle[1]} {candle[1].GetType()}, " +
                            $"\nHigh: {candle[2]} {candle[2].GetType()}, " +
                            $"\nLow: {candle[3]} {candle[3].GetType()}, " +
                            $"\nClose: {candle[4]} {candle[4].GetType()}, " +
                            $"\nVolume: {candle[5]} {candle[5].GetType()}, " +
                            $"\nClose time: {candle[6]} {candle[6].GetType()}]");
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
        }
    }
}
