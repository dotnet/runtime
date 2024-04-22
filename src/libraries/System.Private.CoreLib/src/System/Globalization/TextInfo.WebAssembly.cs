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

            nint exceptionPtr = HasEmptyCultureName ?
                Interop.JsGlobalization.ChangeCaseInvariant(src, srcLen, dstBuffer, dstBufferCapacity, toUpper) :
                Interop.JsGlobalization.ChangeCase(_cultureName, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
            if (exceptionPtr != IntPtr.Zero)
            {
                string message = Marshal.PtrToStringUni(exceptionPtr)!;
                Marshal.FreeHGlobal(exceptionPtr);
                throw new Exception(message);
            }
        }
    }
}
