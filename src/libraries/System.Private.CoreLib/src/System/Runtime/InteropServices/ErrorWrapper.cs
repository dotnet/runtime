// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices
{
    // Wrapper that is converted to a variant with VT_ERROR.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ErrorWrapper
    {
        public ErrorWrapper(int errorCode)
        {
            ErrorCode = errorCode;
        }

        public ErrorWrapper(object errorCode)
        {
            if (errorCode is not int i)
                throw new ArgumentException(SR.Arg_MustBeInt32, nameof(errorCode));
            ErrorCode = i;
        }

        public ErrorWrapper(Exception e)
        {
            ErrorCode = Marshal.GetHRForException(e);
        }

        public int ErrorCode { get; }
    }
}
