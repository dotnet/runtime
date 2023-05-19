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

            string exceptionMessage;
            if (HasEmptyCultureName)
            {
                Interop.JsGlobalization.ChangeCaseInvariant(out exceptionMessage, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
            }
            else
            {
                Interop.JsGlobalization.ChangeCase(out exceptionMessage, _cultureName, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
            }
            if (!string.IsNullOrEmpty(exceptionMessage))
                throw new Exception(exceptionMessage);
        }
    }
}
