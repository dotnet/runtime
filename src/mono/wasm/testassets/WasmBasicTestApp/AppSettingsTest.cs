using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class AppSettingsTest
{
    [JSExport]
    public static void Run()
    {
        PrintFileExistence("/appsettings.json");
        PrintFileExistence("/appsettings.Development.json");
        PrintFileExistence("/appsettings.Production.json");
    }

    // Synchronize with AppSettingsTests
    private static void PrintFileExistence(string path) => TestOutput.WriteLine($"'{path}' exists '{File.Exists(path)}'");
}
