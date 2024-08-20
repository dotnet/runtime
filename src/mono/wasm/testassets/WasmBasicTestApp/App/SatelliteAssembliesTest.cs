// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

public partial class SatelliteAssembliesTest
{
    [JSExport]
    public static async Task Run()
    {
        ResourceLibrary.ResourceAccessor.Read(TestOutput.WriteLine);

        await LoadSatelliteAssemblies(new[] { "es-ES" });

        ResourceLibrary.ResourceAccessor.Read(TestOutput.WriteLine);
    }

    [JSImport("INTERNAL.loadSatelliteAssemblies")]
    public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);
}
