// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternContexts
{
    internal class MockRecursivePathSegment : IPathSegment
    {
        public bool CanProduceStem {  get { return false; } }

        public bool Match(string value)
        {
            return false;
        }
    }
}
