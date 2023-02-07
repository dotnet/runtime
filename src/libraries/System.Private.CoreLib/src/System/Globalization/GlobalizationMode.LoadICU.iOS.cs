// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static int LoadICU()
        {
            // NativeAOT doesn't set ICU_DAT_FILE_PATH so we fallback to icudt.dat in the app bundle root in that case
            string datPath = AppContext.GetData("ICU_DAT_FILE_PATH")?.ToString() ?? Interop.Globalization.GetICUDataPathFallback();

            return Interop.Globalization.LoadICUData(datPath);
        }
    }
}
