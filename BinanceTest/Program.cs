using System;
using System.Net.Http;
using System.Threading.Tasks;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;

namespace BinanceAPIExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string key = "MNEUPWOPOEYAXCQSTN";
            string secret = "WRGRMKYIBQHYMLDEIWLWZFWKFABWSDGCOXTF";
            BybitClient bybitClient = new BybitClient(new BybitClientOptions
            {
                ApiCredentials = new ApiCredentials(key, secret)
            });
            decimal balance = 0.0m;
            var balanceRes = await bybitClient.UsdPerpetualApi.Account.GetBalancesAsync();
            var usdt = balanceRes.Data.FirstOrDefault(x => x.Key == "USDT");
            balance = usdt.Value.AvailableBalance;
            Console.WriteLine("Available balance: " + balance);
            decimal percent = 0.1m;
            decimal leverage = 10.0m;
            decimal price = 0.8842m;
            decimal qty = Math.Round((((balance * percent) * leverage) / price), 2);
            Console.WriteLine("Placing order async on XNOUSDT, qty: " + qty);
            try
            {
                var res = await bybitClient.UsdPerpetualApi.Trading.PlaceOrderAsync(
                    symbol: "XNOUSDT",
                    side: OrderSide.Buy,
                    type: OrderType.Market,
                    quantity: qty,
                    timeInForce: TimeInForce.GoodTillCanceled,
                    reduceOnly: false,
                    closeOnTrigger: false,
                    positionMode: PositionMode.OneWay,
                    takeProfitPrice: 1.0m, stopLossPrice: 0.85m);
                if (!res.Success)
                {
                    Console.WriteLine(res.Error.Message);
                }
                else
                {
                    Console.WriteLine();
                    await Task.Delay(2500);
                    Console.WriteLine("Canceling order...");
                    var res2 = await bybitClient.UsdPerpetualApi.Trading.PlaceOrderAsync(
                        symbol: "XNOUSDT",
                        side: OrderSide.Sell,
                        type: OrderType.Market,
                        quantity: 1.0m,
                        timeInForce: TimeInForce.GoodTillCanceled,
                        reduceOnly: true,
                        closeOnTrigger: false,
                        positionMode: PositionMode.OneWay);
                    if (!res2.Success)
                    {
                        Console.WriteLine(res2.Error.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
