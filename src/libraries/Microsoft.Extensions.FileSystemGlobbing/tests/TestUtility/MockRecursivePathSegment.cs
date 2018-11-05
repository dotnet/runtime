// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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