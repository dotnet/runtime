// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Build.Bundle
{
    /// <summary>
    /// This exception is thrown when a bundle/extraction
    /// operation fails due known user errors.
    /// </summary>
    public class BundleException : Exception
    {
        public BundleException(string message) :
                base(message)
        {
        }
    }
}

