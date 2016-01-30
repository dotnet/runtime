// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
