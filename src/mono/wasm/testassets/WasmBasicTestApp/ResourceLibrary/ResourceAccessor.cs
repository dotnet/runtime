// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;

namespace ResourceLibrary;

public static class ResourceAccessor
{
    public static void Read(Action<string> testOuput, bool hasSatellites)
    {
        var rm = new ResourceManager("ResourceLibrary.words", typeof(ResourceAccessor).Assembly);
        testOuput($"default: {rm.GetString("hello", CultureInfo.CurrentCulture)}");
        testOuput($"es-ES {(hasSatellites ? "with" : "without")} satellite: {rm.GetString("hello", new CultureInfo("es-ES"))}");
    }
}
