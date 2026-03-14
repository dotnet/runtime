// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Normaliz
    {
        [LibraryImport("Normaliz.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial BOOL IsNormalizedString(NormalizationForm normForm, char* source, int length);

        [LibraryImport("Normaliz.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int NormalizeString(
                                        NormalizationForm normForm,
                                        char* source,
                                        int sourceLength,
                                        char* destination,
                                        int destinationLength);
    }
}
