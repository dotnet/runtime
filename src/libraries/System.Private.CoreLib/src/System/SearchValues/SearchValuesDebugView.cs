// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    internal sealed class SearchValuesDebugView<T> where T : IEquatable<T>?
    {
        private readonly SearchValues<T> _values;

        public SearchValuesDebugView(SearchValues<T> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            _values = values;
        }

        public T[] Values => _values.GetValues();
    }
}
