// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    internal struct Range
    {
        private int _min;
        private int _max;
        private bool _isNotNull; // zero bit pattern represents null

        public Range(int min, int max)
        {
            if (min > max)
            {
                throw ExceptionBuilder.RangeArgument(min, max);
            }
            _min = min;
            _max = max;
            _isNotNull = true;
        }

        public int Count => IsNull ? 0 : _max - _min + 1;

        public bool IsNull => !_isNotNull;

        public int Max
        {
            get
            {
                CheckNull();
                return _max;
            }
        }

        public int Min
        {
            get
            {
                CheckNull();
                return _min;
            }
        }

        internal void CheckNull()
        {
            if (IsNull)
            {
                throw ExceptionBuilder.NullRange();
            }
        }
    }
}
