// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Normaliz
    {
        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe BOOL IsNormalizedString(NormalizationForm normForm, char* source, int length);

        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int NormalizeString(
                                        NormalizationForm normForm,
                                        char* source,
                                        int sourceLength,
                                        char* destination,
                                        int destinationLength);
    }
}
