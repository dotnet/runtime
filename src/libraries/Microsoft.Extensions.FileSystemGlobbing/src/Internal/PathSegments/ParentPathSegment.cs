// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments
{
    public class ParentPathSegment : IPathSegment
    {
        private const string LiteralParent = "..";

        public bool CanProduceStem { get { return false; } }

        public bool Match(string value)
        {
            return string.Equals(LiteralParent, value, StringComparison.Ordinal);
        }
    }
}
