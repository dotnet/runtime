// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class TextInfo
    {
        internal unsafe void ChangeCaseNative(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);
            int result;

            if (HasEmptyCultureName)
                result = Interop.Globalization.ChangeCaseInvariantNative(src, srcLen, dstBuffer, dstBufferCapacity, toUpper);
            else
                result = Interop.Globalization.ChangeCaseNative(_cultureName, _cultureName.Length, src, srcLen, dstBuffer, dstBufferCapacity, toUpper);

            if (result != (int)Interop.Globalization.ResultCode.Success)
                throw new Exception(result == (int)Interop.Globalization.ResultCode.InvalidCodePoint ? "Invalid code point while case changing" :
                                    result == (int)Interop.Globalization.ResultCode.InsufficientBuffer ? "Insufficiently sized destination buffer" : "Exception occurred while case changing");
        }
    }
}
