// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    internal sealed class IndexOfAnyValuesDebugView<T> where T : IEquatable<T>?
    {
        private readonly IndexOfAnyValues<T> _values;

        public IndexOfAnyValuesDebugView(IndexOfAnyValues<T> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            _values = values;
        }

        public T[] Values => _values.GetValues();
    }
}
