// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;
using System.Runtime.InteropServices.JavaScript;

public partial class SatelliteAssembliesTest
{
    [JSExport]
    public static async Task Run()
    {
        var rm = new ResourceManager("WasmBasicTestApp.words", typeof(Program).Assembly);
        TestOutput.WriteLine("default: " + rm.GetString("hello", CultureInfo.CurrentCulture));
        TestOutput.WriteLine("es-ES without satellite: " + rm.GetString("hello", new CultureInfo("es-ES")));

        await LoadSatelliteAssemblies(new[] { "es-ES" });

        rm = new ResourceManager("WasmBasicTestApp.words", typeof(Program).Assembly);
        TestOutput.WriteLine("default: " + rm.GetString("hello", CultureInfo.CurrentCulture));
        TestOutput.WriteLine("es-ES with satellite: " + rm.GetString("hello", new CultureInfo("es-ES")));
    }

    [JSImport("INTERNAL.loadSatelliteAssemblies")]
    public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);
}
