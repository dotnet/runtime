// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class TextInfo
    {
        internal unsafe void JsChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);

            ReadOnlySpan<char> cultureName = _cultureName.AsSpan();
            fixed (char* pCultureName = &MemoryMarshal.GetReference(cultureName))
            {
                nint exceptionPtr = HasEmptyCultureName ?
                    Interop.JsGlobalization.ChangeCaseInvariant(src, srcLen, dstBuffer, dstBufferCapacity, toUpper) :
                    Interop.JsGlobalization.ChangeCase(pCultureName, cultureName.Length, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
                Helper.MarshalAndThrowIfException(exceptionPtr);
            }
        }
    }
}
