// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class TextInfo
    {
        internal unsafe void JsChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);

            int exception;
            object ex_result;
            if (HasEmptyCultureName)
            {
                Interop.JsGlobalization.ChangeCaseInvariant(src, srcLen, dstBuffer, dstBufferCapacity, toUpper, out exception, out ex_result);
            }
            else
            {
                Interop.JsGlobalization.ChangeCase(_cultureName, src, srcLen, dstBuffer, dstBufferCapacity, toUpper, out exception, out ex_result);
            }
            if (exception != 0)
                throw new Exception((string)ex_result);
        }
    }
}
