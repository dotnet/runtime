// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Text
{
    internal abstract class Utf8StringComparer : IComparer<Utf8String?>, IEqualityComparer<Utf8String?>
    {
        // Nobody except for nested classes can create instances of this type.
        private Utf8StringComparer() { }

        public static Utf8StringComparer CurrentCulture => new CultureAwareComparer(CultureInfo.CurrentCulture.CompareInfo, CompareOptions.None);
        public static Utf8StringComparer CurrentCultureIgnoreCase => new CultureAwareComparer(CultureInfo.CurrentCulture.CompareInfo, CompareOptions.IgnoreCase);
        public static Utf8StringComparer InvariantCulture => CultureAwareComparer.Invariant;
        public static Utf8StringComparer InvariantCultureIgnoreCase => CultureAwareComparer.InvariantIgnoreCase;
        public static Utf8StringComparer Ordinal => OrdinalComparer.Instance;
        public static Utf8StringComparer OrdinalIgnoreCase => OrdinalIgnoreCaseComparer.Instance;

        public static Utf8StringComparer Create(CultureInfo culture, bool ignoreCase) => Create(culture, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);

        public static Utf8StringComparer Create(CultureInfo culture, CompareOptions options)
        {
            if (culture is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            return new CultureAwareComparer(culture.CompareInfo, options);
        }

        public static Utf8StringComparer FromComparison(StringComparison comparisonType)
        {
            return comparisonType switch
            {
                StringComparison.CurrentCulture => CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase => CurrentCultureIgnoreCase,
                StringComparison.InvariantCulture => InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase => InvariantCultureIgnoreCase,
                StringComparison.Ordinal => Ordinal,
                StringComparison.OrdinalIgnoreCase => OrdinalIgnoreCase,
                _ => throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType)),
            };
        }

        public abstract int Compare(Utf8String? x, Utf8String? y);
        public abstract int Compare(Utf8Span x, Utf8Span y);
        public abstract bool Equals(Utf8String? x, Utf8String? y);
        public abstract bool Equals(Utf8Span x, Utf8Span y);
#pragma warning disable CS8614 // Remove warning disable when nullable attributes are respected
        public abstract int GetHashCode(Utf8String obj);
#pragma warning restore CS8614
        public abstract int GetHashCode(Utf8Span obj);

        private sealed class CultureAwareComparer : Utf8StringComparer
        {
            internal static readonly CultureAwareComparer Invariant = new CultureAwareComparer(CompareInfo.Invariant, CompareOptions.None);
            internal static readonly CultureAwareComparer InvariantIgnoreCase = new CultureAwareComparer(CompareInfo.Invariant, CompareOptions.IgnoreCase);

            private readonly CompareInfo _compareInfo;
            private readonly CompareOptions _options;

            internal CultureAwareComparer(CompareInfo compareInfo, CompareOptions options)
            {
                Debug.Assert(compareInfo != null);

                _compareInfo = compareInfo;
                _options = options;
            }

            public override int Compare(Utf8String? x, Utf8String? y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return _compareInfo.Compare(x?.ToString(), y?.ToString(), _options);
            }

            public override int Compare(Utf8Span x, Utf8Span y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return _compareInfo.Compare(x.ToString(), y.ToString(), _options);
            }

            public override bool Equals(Utf8String? x, Utf8String? y) => Compare(x, y) == 0;
            public override bool Equals(Utf8Span x, Utf8Span y) => Compare(x, y) == 0;

            public override int GetHashCode(Utf8String? obj)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return (obj is null) ? 0 : _compareInfo.GetHashCode(obj.ToString(), _options);
            }

            public override int GetHashCode(Utf8Span obj)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return _compareInfo.GetHashCode(obj.ToString(), _options);
            }
        }

        private sealed class OrdinalComparer : Utf8StringComparer
        {
            public static readonly OrdinalComparer Instance = new OrdinalComparer();

            // All accesses must be through the static factory.
            private OrdinalComparer() { }

            public override int Compare(Utf8String? x, Utf8String? y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return string.CompareOrdinal(x?.ToString(), y?.ToString());
            }

            public override int Compare(Utf8Span x, Utf8Span y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return string.CompareOrdinal(x.ToString(), y.ToString());
            }

            public override bool Equals(Utf8String? x, Utf8String? y) => Utf8String.Equals(x, y);
            public override bool Equals(Utf8Span x, Utf8Span y) => Utf8Span.Equals(x, y);
            public override int GetHashCode(Utf8String obj) => obj.GetHashCode();
            public override int GetHashCode(Utf8Span obj) => obj.GetHashCode();
        }

        private sealed class OrdinalIgnoreCaseComparer : Utf8StringComparer
        {
            public static readonly OrdinalIgnoreCaseComparer Instance = new OrdinalIgnoreCaseComparer();

            // All accesses must be through the static factory.
            private OrdinalIgnoreCaseComparer() { }

            public override int Compare(Utf8String? x, Utf8String? y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return StringComparer.OrdinalIgnoreCase.Compare(x?.ToString(), y?.ToString());
            }

            public override int Compare(Utf8Span x, Utf8Span y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return StringComparer.OrdinalIgnoreCase.Compare(x.ToString(), y.ToString());
            }

            public override bool Equals(Utf8String? x, Utf8String? y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return StringComparer.OrdinalIgnoreCase.Equals(x?.ToString(), y?.ToString());
            }

            public override bool Equals(Utf8Span x, Utf8Span y)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return StringComparer.OrdinalIgnoreCase.Equals(x.ToString(), y.ToString());
            }

            public override int GetHashCode(Utf8String obj)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ToString());
            }

            public override int GetHashCode(Utf8Span obj)
            {
                // TODO_UTF8STRING: Avoid the allocations below.

                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ToString());
            }
        }
    }
}
