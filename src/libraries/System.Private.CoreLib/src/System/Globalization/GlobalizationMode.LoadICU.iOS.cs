// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static int LoadICU()
        {
            return Interop.Globalization.LoadICUData((string?)AppContext.GetData("ICU_DAT_FILE_PATH")); // we handle a null path in the native code
        }
    }
}
