// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Wrapper that is converted to a variant with VT_ERROR.
    public sealed class ErrorWrapper
    {
        public ErrorWrapper(int errorCode)
        {
            ErrorCode = errorCode;
        }

        public ErrorWrapper(object errorCode)
        {
            if (!(errorCode is int))
                throw new ArgumentException(SR.Arg_MustBeInt32, nameof(errorCode));
            ErrorCode = (int)errorCode;
        }

        public ErrorWrapper(Exception e)
        {
            ErrorCode = Marshal.GetHRForException(e);
        }

        public int ErrorCode { get; }
    }
}
