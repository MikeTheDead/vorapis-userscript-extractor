using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Downloading vorapis zip :P");
        string finalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Vorapis.user.js");
        
        string writeLocation = string.Empty;
        var request = WebRequest.Create("https://vorapis.pages.dev/product/v3/game_service/get_latest_client");
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Console.WriteLine(response.StatusDescription);
        string writeFolder = Path.Combine(Directory.GetCurrentDirectory(), "extracted");

        using (var dataStream = response.GetResponseStream())
        using (var reader = new StreamReader(dataStream))
        {
            string responseFromServer = reader.ReadToEnd();
            var sections = responseFromServer.Split('/');
            writeLocation = Path.GetFullPath("source.zip");
            Console.WriteLine("Deleting existing files :P");
            if (Directory.Exists(writeFolder))
                Directory.Delete(writeFolder, true);
            if (File.Exists(writeLocation))
                File.Delete(writeLocation);
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            
            using (HttpClient client = new HttpClient())
            {
                var zip = await client.GetAsync(responseFromServer);
                using (var fs = new FileStream(writeLocation, FileMode.CreateNew))
                {
                    await zip.Content.CopyToAsync(fs);
                }
                Console.WriteLine("Extracting zip :P");
                ZipFile.ExtractToDirectory(writeLocation, writeFolder);
            }
        }

        Console.WriteLine("Yoinking userscript :P");
        string userscriptPath = Path.Combine(writeFolder, "Vorapis.user.js");
        
        File.Move(userscriptPath, finalPath);
        userscriptPath = finalPath;

        Console.WriteLine("clean up:P");
        if (File.Exists(writeLocation))
            File.Delete(writeLocation);
        if (Directory.Exists(writeFolder))
            Directory.Delete(writeFolder, true);

        
        string browserPath = GetPathToDefaultBrowser();
        if (!string.IsNullOrEmpty(browserPath))
        {
            Console.WriteLine($"Opening {browserPath} :P");
            Process.Start(new ProcessStartInfo
            {
                FileName = browserPath,
                Arguments = $"\"{userscriptPath}\"",
                UseShellExecute = true
            });
        }
        else
        {
            Console.WriteLine("Could not find the default browser.");
        }
    }

    static string GetPathToDefaultBrowser()
    {
        const string userChoiceKeyPath = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
        using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey(userChoiceKeyPath, false))
        {
            if (userChoiceKey == null) return "";

            object progIdValue = userChoiceKey.GetValue("ProgId");
            if (progIdValue == null) return "";

            string progId = progIdValue.ToString();
            using (RegistryKey commandKey = Registry.ClassesRoot.OpenSubKey($"{progId}\\shell\\open\\command", false))
            {
                if (commandKey == null) return "";

                string rawValue = (string)commandKey.GetValue("");
                Regex regex = new Regex("\"([^\"]*)\"");
                Match match = regex.Match(rawValue);

                return match.Success ? match.Groups[1].Value : "";
            }
        }
    }
}
