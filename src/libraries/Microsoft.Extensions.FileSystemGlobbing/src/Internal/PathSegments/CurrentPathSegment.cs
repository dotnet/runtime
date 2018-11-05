// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments
{
    public class CurrentPathSegment : IPathSegment
    {
        public bool CanProduceStem { get { return false; } }

        public bool Match(string value)
        {
            return false;
        }
    }
}