// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Localization, CharSet = CharSet.Unicode, EntryPoint = "VerLanguageNameW")]
        internal static extern int VerLanguageName(uint langID, StringBuilder lpBuffer, uint nSize);
    }
}
