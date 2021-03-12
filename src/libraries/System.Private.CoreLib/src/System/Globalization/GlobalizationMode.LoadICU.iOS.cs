// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static int LoadICU()
        {
            object? datPath = AppContext.GetData("ICU_DAT_FILE_PATH");
            return (datPath != null) ? Interop.Globalization.LoadICUData(datPath!.ToString()!) : Interop.Globalization.LoadICU();
        }
    }
}
