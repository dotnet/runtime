// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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