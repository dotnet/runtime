// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler
{
    public class InternalCompilerErrorException : Exception
    {
        public InternalCompilerErrorException(string message)
            : this(message, innerException: null)
        {
        }

        public InternalCompilerErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
