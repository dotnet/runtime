// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public sealed class UnreachableException : SystemException
    {
        public UnreachableException()
            : base(SR.Arg_UnreachableException)
        {
        }

        public UnreachableException(string? message)
            : base(message)
        {
        }

        public UnreachableException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
