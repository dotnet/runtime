// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

public partial class SatelliteAssembliesTest
{
    [JSExport]
    public static async Task Run(bool loadSatelliteAssemblies)
    {
        if (loadSatelliteAssemblies)
        {
            ResourceLibrary.ResourceAccessor.Read(TestOutput.WriteLine, false);
            await LoadSatelliteAssemblies(new[] { "es-ES" });
        }

        ResourceLibrary.ResourceAccessor.Read(TestOutput.WriteLine, true);
    }

    [JSImport("INTERNAL.loadSatelliteAssemblies")]
    public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);
}
