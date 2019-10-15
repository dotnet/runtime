// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    internal sealed class DelegateEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
    {
        private readonly Func<T, T, bool> _equals;
        private readonly Func<T, int> _getHashCode;
        private readonly Func<object, object, bool> _objectEquals;
        private readonly Func<object, int> _objectGetHashCode;

        public DelegateEqualityComparer(
            Func<T, T, bool> equals = null,
            Func<T, int> getHashCode = null,
            Func<object, object, bool> objectEquals = null,
            Func<object, int> objectGetHashCode = null)
        {
            _equals = equals ?? ((x, y) => { throw new NotImplementedException(); });
            _getHashCode = getHashCode ?? (obj => { throw new NotImplementedException(); });
            _objectEquals = objectEquals ?? ((x, y) => { throw new NotImplementedException(); });
            _objectGetHashCode = objectGetHashCode ?? (obj => { throw new NotImplementedException(); });
        }

        public bool Equals(T x, T y) => _equals(x, y);

        public int GetHashCode(T obj) => _getHashCode(obj);

        bool IEqualityComparer.Equals(object x, object y) => _objectEquals(x, y);

        int IEqualityComparer.GetHashCode(object obj) => _objectGetHashCode(obj);
    }
}
