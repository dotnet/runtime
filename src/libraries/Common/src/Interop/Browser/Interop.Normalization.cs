// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int IsNormalized(NormalizationForm normalizationForm, in string source, out int exceptionalResult, out object result);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int NormalizeString(NormalizationForm normalizationForm, in string source, char* dstBuffer, int dstBufferCapacity, out int exceptionalResult, out object result);
    }
}
