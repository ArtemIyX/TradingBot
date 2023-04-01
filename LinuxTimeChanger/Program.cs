// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

try
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
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
        Console.WriteLine($"New time: {serverTime}");
    }
    else
    {
        Console.WriteLine("The current operating system is not Linux.");
    }
}
catch(Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}