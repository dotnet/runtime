// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// mscorlib defines the HRESULT constants it uses in the internal class System.__HResults.
    /// Since we cannot use that internal class in this assembly, we define the constants we need
    /// in this class.
    /// </summary>
    internal static class HResults
    {
        internal const Int32 E_XAMLPARSEFAILED = unchecked((int)0x802B000A);
        internal const Int32 E_LAYOUTCYCLE = unchecked((int)0x802B0014);
        internal const Int32 E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);
        internal const Int32 E_ELEMENTNOTAVAILABLE = unchecked((int)0x802B001F);
    }  // internal static sealed class HResults
}  // namespace System.Runtime.InteropServices.WindowsRuntime

// HResults.cs
