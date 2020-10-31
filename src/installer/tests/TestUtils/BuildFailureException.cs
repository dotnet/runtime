// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public partial class BuildFailureException : Exception
    {
        public BuildFailureException()
        {
        }

        public BuildFailureException(string message) : base(message)
        {
        }

        public BuildFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
