using Bybit.Net.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Tls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TradingBot.Data;

namespace TradingBot.Extensions;

public static class ServiceExtensions
{
    public static CryptoCurrency ToCrtypoCurrency(this string? input)
    {
        if(string.IsNullOrEmpty(input))
        {
            return CryptoCurrency.None;
        }
        if (input.EndsWith(".P"))
        {
            input = input.Substring(0, input.Length - 2); // remove the last two characters (".P")
        }
        if (Enum.TryParse(input, out CryptoCurrency currency))
        {
            return currency;
        }
        return CryptoCurrency.None;
    }

    public static async Task SyncTime(ILogger logger)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            logger.LogInformation("Platform is not linux, can not sync time");
            return;
        }
        logger.LogInformation("Sync time with bybit server");

        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("https://api.bybit.com");

        HttpResponseMessage response = await client.GetAsync("/v2/public/time");
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        JObject json = JObject.Parse(responseBody);
        string serverTimeUnixStr = json["time_now"].Value<string>();
        double serverTimeUnix = double.Parse(serverTimeUnixStr);

        DateTime serverTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(serverTimeUnix)
                .ToLocalTime();
        DateTime timeNow = DateTime.Now;

        TimeSpan difference = (serverTime > timeNow) ? serverTime - timeNow : serverTime - timeNow;
        if (Math.Abs(difference.TotalSeconds) > 2)
        {
            // Run the shell command to set the system time
            Process.Start("sudo", $"date -s \"{serverTime.ToString("yyyy-MM-dd HH:mm:ss")}\"").WaitForExit();
            logger.LogInformation("New time is: " + serverTime);
        }
        else
        {
            logger.LogInformation($"Dont need to sync time. \nServer time: {serverTime}\nCurrent time: {timeNow}");
        }
          

    }

    private static int DigitsAfterDecimal(string input) => input.Contains(".") ? input.Length - 1 - input.IndexOf('.') : 0;

    public static int PriceRoundingAccuracy(InstrumentInfoResult instrumentInfo)
    {
        string numberString = instrumentInfo.List.First().PriceFilter.TickSize;
        return DigitsAfterDecimal(numberString);
    }

    public static int QtyRoundingAccuaracy(InstrumentInfoResult instrumentInfo)
    {
        string numberString = instrumentInfo.List.First().LotSizeFilter.QtyStep;
        return DigitsAfterDecimal(numberString);
    }

    public static async Task<InstrumentInfoResult?> GetInstrumentInfo(CryptoCurrency currency)
    {
        using (var client = new HttpClient())
        {
            string url = $"https://api.bybit.com/derivatives/v3/public/instruments-info?symbol={currency.ToString()}";
            HttpResponseMessage response = await client.GetAsync(url);
            //response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            JObject jObject = JObject.Parse(json);
            return jObject.GetValue("result").ToObject<InstrumentInfoResult>();
        }
    }

    public static KlineInterval ParseKlineInterval(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));
        }

        input = input.ToLower();

        if (input.EndsWith("m"))
        {
            if (int.TryParse(input.Substring(0, input.Length - 1), out int minutes))
            {
                switch (minutes)
                {
                    case 1:
                        return KlineInterval.OneMinute;
                    case 3:
                        return KlineInterval.ThreeMinutes;
                    case 5:
                        return KlineInterval.FiveMinutes;
                    case 15:
                        return KlineInterval.FifteenMinutes;
                    case 30:
                        return KlineInterval.ThirtyMinutes;
                    default:
                        throw new ArgumentException($"Invalid interval: {input}", nameof(input));
                }
            }
            else
            {
                throw new ArgumentException($"Invalid interval: {input}", nameof(input));
            }
        }
        else if (input.EndsWith("h"))
        {
            if (int.TryParse(input.Substring(0, input.Length - 1), out int hours))
            {
                switch (hours)
                {
                    case 1:
                        return KlineInterval.OneHour;
                    case 2:
                        return KlineInterval.TwoHours;
                    case 4:
                        return KlineInterval.FourHours;
                    case 6:
                        return KlineInterval.SixHours;
                    case 12:
                        return KlineInterval.TwelveHours;
                    default:
                        throw new ArgumentException($"Invalid interval: {input}", nameof(input));
                }
            }
            else
            {
                throw new ArgumentException($"Invalid interval: {input}", nameof(input));
            }
        }
        else
        {
            throw new ArgumentException($"Invalid interval: {input}", nameof(input));
        }
    }
}