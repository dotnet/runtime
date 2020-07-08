// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    /// <summary>
    /// Given a fixed length, compares two <see cref="Range"/> instances for equality.
    /// </summary>
    public sealed class RangeEqualityComparer : IEqualityComparer<Range>
    {
        private int _length;

        public RangeEqualityComparer(int length)
        {
            Assert.True(length >= 0);

            _length = length;
        }

        public bool Equals(Range x, Range y)
        {
            (int offsetX, int lengthX) = x.GetOffsetAndLength(_length);
            (int offsetY, int lengthY) = y.GetOffsetAndLength(_length);

            return offsetX == offsetY && lengthX == lengthY;
        }

        public int GetHashCode(Range obj)
        {
            (int offset, int length) = obj.GetOffsetAndLength(_length);
#if NETCOREAPP
            return HashCode.Combine(offset, length);
#else
            return Tuple.Create(offset, length).GetHashCode();
#endif
        }
    }
}
