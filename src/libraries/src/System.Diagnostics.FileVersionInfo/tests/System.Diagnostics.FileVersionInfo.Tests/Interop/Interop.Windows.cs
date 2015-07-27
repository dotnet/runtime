// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode, EntryPoint = "VerLanguageNameW")]
        public static extern int VerLanguageName(uint langID, StringBuilder lpBuffer, uint nSize);
    }
}
