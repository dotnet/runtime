// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments
{
    public class WildcardPathSegment : IPathSegment
    {
        // It doesn't matter which StringComparison type is used in this MatchAll segment because 
        // all comparing are skipped since there is no content in the segment.
        public static readonly WildcardPathSegment MatchAll = new WildcardPathSegment(
            string.Empty, new List<string>(), string.Empty, StringComparison.OrdinalIgnoreCase);

        private readonly StringComparison _comparisonType;

        public WildcardPathSegment(string beginsWith, List<string> contains, string endsWith, StringComparison comparisonType)
        {
            BeginsWith = beginsWith;
            Contains = contains;
            EndsWith = endsWith;
            _comparisonType = comparisonType;
        }

        public bool CanProduceStem { get { return true; } }

        public string BeginsWith { get; }

        public List<string> Contains { get; }

        public string EndsWith { get; }

        public bool Match(string value)
        {
            var wildcard = this;

            if (value.Length < wildcard.BeginsWith.Length + wildcard.EndsWith.Length)
            {
                return false;
            }

            if (!value.StartsWith(wildcard.BeginsWith, _comparisonType))
            {
                return false;
            }

            if (!value.EndsWith(wildcard.EndsWith, _comparisonType))
            {
                return false;
            }

            var beginRemaining = wildcard.BeginsWith.Length;
            var endRemaining = value.Length - wildcard.EndsWith.Length;
            for (var containsIndex = 0; containsIndex != wildcard.Contains.Count; ++containsIndex)
            {
                var containsValue = wildcard.Contains[containsIndex];
                var indexOf = value.IndexOf(
                    value: containsValue,
                    startIndex: beginRemaining,
                    count: endRemaining - beginRemaining,
                    comparisonType: _comparisonType);
                if (indexOf == -1)
                {
                    return false;
                }

                beginRemaining = indexOf + containsValue.Length;
            }

            return true;
        }
    }
}