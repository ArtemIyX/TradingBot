using Newtonsoft.Json;
using RestSharp;
using System;

var client = new RestClient("https://fapi.binance.com");
var request = new RestRequest("/fapi/v1/ticker/price", Method.Get);
request.AddParameter("symbol", "BTCUSDT");

while (true)
{
    var response = client.Execute(request);
    var data = JsonConvert.DeserializeObject<dynamic>(response.Content);

    Console.WriteLine($"BTC/USDT futures price: {data.price}");
}