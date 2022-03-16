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
        private object? _newValue;

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

        public bool HasNewValue { get; private set; }

        public object? Value => HasNewValue ? _newValue : _initialValue ??= _initialValueProvider?.Invoke();

        public void SetValue(object? newValue)
        {
            Debug.Assert(!IsReadOnly);
            Debug.Assert(!HasNewValue);
            _newValue = newValue;
            HasNewValue = true;
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
