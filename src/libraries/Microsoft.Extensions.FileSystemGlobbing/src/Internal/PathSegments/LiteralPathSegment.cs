// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Util;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments
{
    public class LiteralPathSegment : IPathSegment
    {
        private readonly StringComparison _comparisonType;

        public bool CanProduceStem { get { return false; } }

        public LiteralPathSegment(string value, StringComparison comparisonType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Value = value;

            _comparisonType = comparisonType;
        }

        public string Value { get; }

        public bool Match(string value)
        {
            return string.Equals(Value, value, _comparisonType);
        }

        public override bool Equals(object obj)
        {
            var other = obj as LiteralPathSegment;

            return other != null &&
                _comparisonType == other._comparisonType &&
                string.Equals(other.Value, Value, _comparisonType);
        }

        public override int GetHashCode()
        {
            return StringComparisonHelper.GetStringComparer(_comparisonType).GetHashCode(Value);
        }
    }
}