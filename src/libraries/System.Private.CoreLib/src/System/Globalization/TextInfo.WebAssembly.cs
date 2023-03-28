// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Globalization
{
    internal static unsafe class TextInfoInterop
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void ChangeCaseInvariantJS(out string exceptionMessage, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void ChangeCaseJS(out string exceptionMessage, in string culture, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);
    }

    public partial class TextInfo
    {
        internal unsafe void JsChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);

            string exceptionMessage;
            if (IsInvariant)
            {
                TextInfoInterop.ChangeCaseInvariantJS(out exceptionMessage, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
            }
            else
            {
                TextInfoInterop.ChangeCaseJS(out exceptionMessage, _cultureName, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
            }
            if (!string.IsNullOrEmpty(exceptionMessage))
                throw new Exception(exceptionMessage);
        }

    }
}
