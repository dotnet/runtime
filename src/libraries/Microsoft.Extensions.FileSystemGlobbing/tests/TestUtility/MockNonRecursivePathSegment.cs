// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternContexts
{
    internal class MockNonRecursivePathSegment : IPathSegment
    {
        private readonly StringComparison _comparisonType;

        public MockNonRecursivePathSegment(StringComparison comparisonType)
        {
            _comparisonType = comparisonType;
        }

        public MockNonRecursivePathSegment(string value)
        {
            Value = value;
        }

        public bool CanProduceStem { get { return false; } }

        public string Value { get; }

        public bool Match(string value)
        {
            return string.Compare(Value, value, _comparisonType) == 0;
        }
    }
}
