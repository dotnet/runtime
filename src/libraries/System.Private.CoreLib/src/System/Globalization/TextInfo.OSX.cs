// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private enum ErrorCodes
        {
            ERROR_SUCCESS = 0,
            ERROR_INVALID_CODE_POINT = 1
        }

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

            if (result != (int)ErrorCodes.ERROR_SUCCESS)
                throw new Exception(result == (int)ErrorCodes.ERROR_INVALID_CODE_POINT
                                   ? "Invalid code point while case changing" : "Exception occurred while case changing");
        }
    }
}
