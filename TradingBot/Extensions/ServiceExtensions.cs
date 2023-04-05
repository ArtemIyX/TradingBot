﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TradingBot.Data;

namespace TradingBot.Extensions;

public static class ServiceExtensions
{
    public static CryptoCurrency ToCrtypoCurrency(this string? input)
    {
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

        // Run the shell command to set the system time
        Process.Start("sudo", $"date -s \"{serverTime.ToString("yyyy-MM-dd HH:mm:ss")}\"").WaitForExit();
        logger.LogInformation("New time is: " + serverTime);

    }
}