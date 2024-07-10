// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    /// <summary>
    /// A struct variant of <see cref="Queue{T}"/> that only allocates for Counts > 1.
    /// </summary>
    internal struct ValueQueue<T>
    {
        private byte _state; // 0 = empty, 1 = single, 2 = multiple
        private T? _single;
        private Queue<T>? _multiple;

        public readonly int Count => _state is < 2 and byte state ? state : _multiple!.Count;

        public void Enqueue(T value)
        {
            switch (_state)
            {
                case 0:
                    _single = value;
                    _state = 1;
                    break;

                case 1:
                    (_multiple ??= new()).Enqueue(_single!);
                    _single = default;
                    _state = 2;
                    goto default;

                default:
                    Debug.Assert(_multiple != null);
                    _multiple.Enqueue(value);
                    break;
            }
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T? value)
        {
            switch (_state)
            {
                case 0:
                    value = default;
                    return false;

                case 1:
                    value = _single;
                    _single = default;
                    _state = 0;
                    return true;

                default:
                    Debug.Assert(_multiple != null);
                    return _multiple.TryDequeue(out value);
            }
        }
    }
}
