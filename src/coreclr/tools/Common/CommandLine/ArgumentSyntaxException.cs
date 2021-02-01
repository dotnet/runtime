// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.CommandLine
{
    public sealed class ArgumentSyntaxException : Exception
    {
        public ArgumentSyntaxException()
        {
        }

        public ArgumentSyntaxException(string message)
            : base(message)
        {
        }

        public ArgumentSyntaxException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
