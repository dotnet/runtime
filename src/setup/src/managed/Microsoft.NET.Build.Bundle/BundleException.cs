// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.NET.Build.Bundle
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

