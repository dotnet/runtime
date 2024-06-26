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

            if (HasEmptyCultureName)
            {
                ReadOnlySpan<char> source = new ReadOnlySpan<char>(src, srcLen);
                Span<char> destination = new Span<char>(dstBuffer, dstBufferCapacity);
                if (toUpper)
                {
                    InvariantModeCasing.ToUpper(source, destination);
                }
                else
                {
                    InvariantModeCasing.ToLower(source, destination);
                }
                return;
            }

            ReadOnlySpan<char> cultureName = _cultureName.AsSpan();
            fixed (char* pCultureName = &MemoryMarshal.GetReference(cultureName))
            {
                nint exceptionPtr = Interop.JsGlobalization.ChangeCase(pCultureName, cultureName.Length, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
                Helper.MarshalAndThrowIfException(exceptionPtr);
            }
        }
    }
}
