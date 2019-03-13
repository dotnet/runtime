// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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