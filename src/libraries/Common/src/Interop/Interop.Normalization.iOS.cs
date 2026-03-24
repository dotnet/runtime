// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [RequiresUnsafe]
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IsNormalizedNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int IsNormalizedNative(NormalizationForm normalizationForm, char* src, int srcLen);

        [RequiresUnsafe]
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_NormalizeStringNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int NormalizeStringNative(NormalizationForm normalizationForm, char* src, int srcLen, char* buffer, int bufferLength);
    }
}
