// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Configuration
{
    internal sealed class BindingPoint
    {
        private readonly Func<object?>? _initialValueProvider;
        private object? _initialValue;
        private object? _setValue;
        private bool _valueSet;

        public BindingPoint(object? initialValue = null, bool isReadOnly = false)
        {
            _initialValue = initialValue;
            IsReadOnly = isReadOnly;
        }

        public BindingPoint(Func<object?> initialValueProvider, bool isReadOnly)
        {
            _initialValueProvider = initialValueProvider;
            IsReadOnly = isReadOnly;
        }

        public bool IsReadOnly { get; }

        public bool HasNewValue
        {
            get
            {
                if (IsReadOnly)
                {
                    return false;
                }

                if (_valueSet)
                {
                    return true;
                }

                // When binding mutable value types, even if we didn't explicitly set a new value
                // We still end up editing a copy of that value and therefore should treat it as
                // a new value that needs to be written back to the parent object.
                return _initialValue?.GetType() is { } initialValueType
                    && initialValueType.IsValueType
                    // Skipping primitive value types isn't strictly necessary but avoids us needing
                    // to update the parent object in a very common case (certainly more common than
                    // mutable structs). We'll still do a "wasted" update for non-primitive immutable structs.
                    && !initialValueType.IsPrimitive;
            }
        }

        public object? Value => _valueSet ? _setValue : _initialValue ??= _initialValueProvider?.Invoke();

        public void SetValue(object? newValue)
        {
            Debug.Assert(!IsReadOnly);
            Debug.Assert(!_valueSet);
            _setValue = newValue;
            _valueSet = true;
        }

        public void TrySetValue(object? newValue)
        {
            if (!IsReadOnly)
            {
                SetValue(newValue);
            }
        }
    }
}
