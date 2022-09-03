// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    internal static class DiagnosticsConfiguration
    {
        internal static int PerformanceCountersFileMappingSize
        {
            get
            {
                // This is supposed to read the filemappingsize value from the System.Diagnostics section in the app config file, but it was
                // discovered it was broken so the code was removed.
                // Removing the code was necessary to support TraceSource-based config entries which also need to read the System.Diagnostics
                // section. The code as also removed to support using performance counters in general (without even using the filemappingsize entry)
                // since just the presence of a System.Diagnostics section itself caused the removed code to throw so many performance counter
                // scenarios would throw when having any TraceSource-related entries in the System.Diagnostics section.
                // See https://github.com/dotnet/runtime/issues/73706.
                return SharedPerformanceCounter.DefaultCountersFileMappingSize;
            }
        }
    }
}
