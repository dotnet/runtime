// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public static class GlobalizationExtensions
    {
        public static StringComparer GetStringComparer(this CompareInfo compareInfo, CompareOptions options)
        {
            if (compareInfo == null)
            {
                throw new ArgumentNullException(nameof(compareInfo));
            }

            if (options == CompareOptions.Ordinal)
            {
                return StringComparer.Ordinal;
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return StringComparer.OrdinalIgnoreCase;
            }

            if ((options & CultureAwareComparer.ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            return new CultureAwareComparer(compareInfo, options);
        }
    }

    [Serializable]
    internal sealed class CultureAwareComparer : StringComparer
    {
        internal const CompareOptions ValidCompareMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);

        private readonly CompareInfo _compareInfo;
        private readonly CompareOptions _options;

        internal CultureAwareComparer(CompareInfo compareInfo, CompareOptions options)
        {
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);
            _compareInfo = compareInfo;
            _options = options;
        }

        public override int Compare(string x, string y)
        {
            if (Object.ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return _compareInfo.Compare(x, y, _options);
        }

        public override bool Equals(string x, string y)
        {
            if (Object.ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            return (_compareInfo.Compare(x, y, _options) == 0);
        }

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            Contract.EndContractBlock();

            // StringSort used in compare operation and not with the hashing
            return _compareInfo.GetHashCode(obj, _options & (~CompareOptions.StringSort));
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj)
        {
            CultureAwareComparer comparer = obj as CultureAwareComparer;
            return
                comparer != null &&
                _options == comparer._options &&
                _compareInfo.Equals(comparer._compareInfo);
        }

        public override int GetHashCode()
        {
            return _compareInfo.GetHashCode() ^ ((int)_options & 0x7FFFFFFF);
        }
    }
}

