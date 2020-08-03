// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
