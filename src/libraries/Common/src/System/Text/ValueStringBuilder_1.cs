// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable

namespace System.Text
{
    internal ref partial struct ValueStringBuilder<TChar>
        where TChar : unmanaged
    {
        private TChar[]? _arrayToReturnToPool;
        private Span<TChar> _chars;
        private int _pos;

        public ValueStringBuilder(Span<TChar> initialBuffer)
        {
            Debug.Assert((typeof(TChar) == typeof(Utf8Char)) || (typeof(TChar) == typeof(Utf16Char)));

            _arrayToReturnToPool = null;
            _chars = initialBuffer;
            _pos = 0;
        }

        public ValueStringBuilder(int initialCapacity)
        {
            Debug.Assert((typeof(TChar) == typeof(Utf8Char)) || (typeof(TChar) == typeof(Utf16Char)));

            _arrayToReturnToPool = ArrayPool<TChar>.Shared.Rent(initialCapacity);
            _chars = _arrayToReturnToPool;
            _pos = 0;
        }

        public int Length
        {
            readonly get => _pos;
            set
            {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= _chars.Length);
                _pos = value;
            }
        }

        public readonly int Capacity => _chars.Length;

        public void EnsureCapacity(int capacity)
        {
            // This is not expected to be called this with negative capacity
            Debug.Assert(capacity >= 0);

            // If the caller has a bug and calls this with negative capacity, make sure to call Grow to throw an exception.
            if ((uint)capacity > (uint)_chars.Length)
                Grow(capacity - _pos);
        }

        /// <summary>
        /// Get a pinnable reference to the builder.
        /// Does not ensure there is a null TChar after <see cref="Length"/>
        /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
        /// the explicit method call, and write eg "fixed (TChar* c = builder)"
        /// </summary>
        public readonly ref TChar GetPinnableReference()
        {
            return ref MemoryMarshal.GetReference(_chars);
        }

        /// <summary>
        /// Get a pinnable reference to the builder.
        /// </summary>
        /// <param name="terminate">Ensures that the builder has a null TChar after <see cref="Length"/></param>
        public ref TChar GetPinnableReference(bool terminate)
        {
            if (terminate)
            {
                EnsureCapacity(Length + 1);
                _chars[Length] = default;
            }
            return ref MemoryMarshal.GetReference(_chars);
        }

        public readonly ref TChar this[int index]
        {
            get
            {
                Debug.Assert(index < _pos);
                return ref _chars[index];
            }
        }

        public override string ToString()
        {
            string result;
            Span<TChar> slice = _chars.Slice(0, _pos);

            if (typeof(TChar) == typeof(Utf8Char))
            {
                result = Encoding.UTF8.GetString(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(slice));
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(Utf16Char));
                result = Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(slice).ToString();
            }

            Dispose();
            return result;
        }

        /// <summary>Returns the underlying storage of the builder.</summary>
        public readonly Span<TChar> RawChars => _chars;

        /// <summary>
        /// Returns a span around the contents of the builder.
        /// </summary>
        /// <param name="terminate">Ensures that the builder has a null TChar after <see cref="Length"/></param>
        public ReadOnlySpan<TChar> AsSpan(bool terminate)
        {
            if (terminate)
            {
                EnsureCapacity(Length + 1);
                _chars[Length] = default;
            }
            return _chars.Slice(0, _pos);
        }

        public readonly ReadOnlySpan<TChar> AsSpan() => _chars.Slice(0, _pos);
        public readonly ReadOnlySpan<TChar> AsSpan(int start) => _chars.Slice(start, _pos - start);
        public readonly ReadOnlySpan<TChar> AsSpan(int start, int length) => _chars.Slice(start, length);

        public bool TryCopyTo(Span<TChar> destination, out int charsWritten)
        {
            if (_chars.Slice(0, _pos).TryCopyTo(destination))
            {
                charsWritten = _pos;
                Dispose();
                return true;
            }
            else
            {
                charsWritten = 0;
                Dispose();
                return false;
            }
        }

        public void Insert(int index, TChar value, int count)
        {
            if (_pos > _chars.Length - count)
            {
                Grow(count);
            }

            int remaining = _pos - index;
            _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
            _chars.Slice(index, count).Fill(value);
            _pos += count;
        }

        public void Insert(int index, ReadOnlySpan<TChar> text)
        {
            if (text.IsEmpty)
            {
                return;
            }

            int count = text.Length;

            if (_pos > (_chars.Length - count))
            {
                Grow(count);
            }

            int remaining = _pos - index;
            _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
            text.CopyTo(_chars.Slice(index));
            _pos += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(TChar c)
        {
            int pos = _pos;
            Span<TChar> chars = _chars;
            if ((uint)pos < (uint)chars.Length)
            {
                chars[pos] = c;
                _pos = pos + 1;
            }
            else
            {
                GrowAndAppend(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(ReadOnlySpan<TChar> text)
        {
            if (text.IsEmpty)
            {
                return;
            }

            int pos = _pos;
            if (text.Length == 1 && (uint)pos < (uint)_chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
            {
                _chars[pos] = text[0];
                _pos = pos + 1;
            }
            else
            {
                AppendSlow(text);
            }
        }

        private void AppendSlow(ReadOnlySpan<TChar> text)
        {
            int pos = _pos;
            if (pos > _chars.Length - text.Length)
            {
                Grow(text.Length);
            }

            text.CopyTo(_chars.Slice(pos));
            _pos += text.Length;
        }

        public void Append(TChar c, int count)
        {
            if (_pos > _chars.Length - count)
            {
                Grow(count);
            }

            Span<TChar> dst = _chars.Slice(_pos, count);
            for (int i = 0; i < dst.Length; i++)
            {
                dst[i] = c;
            }
            _pos += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<TChar> AppendSpan(int length)
        {
            int origPos = _pos;
            if (origPos > _chars.Length - length)
            {
                Grow(length);
            }

            _pos = origPos + length;
            return _chars.Slice(origPos, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAppend(TChar c)
        {
            Grow(1);
            Append(c);
        }

        /// <summary>
        /// Resize the internal buffer either by doubling current buffer size or
        /// by adding <paramref name="additionalCapacityBeyondPos"/> to
        /// <see cref="_pos"/> whichever is greater.
        /// </summary>
        /// <param name="additionalCapacityBeyondPos">
        /// Number of chars requested beyond current position.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int additionalCapacityBeyondPos)
        {
            Debug.Assert(additionalCapacityBeyondPos > 0);
            Debug.Assert(_pos > _chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

            const uint ArrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

            // Increase to at least the required size (_pos + additionalCapacityBeyondPos), but try
            // to double the size if possible, bounding the doubling to not go beyond the max array length.
            int newCapacity = (int)Math.Max(
                (uint)(_pos + additionalCapacityBeyondPos),
                Math.Min((uint)_chars.Length * 2, ArrayMaxLength));

            // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative.
            // This could also go negative if the actual required length wraps around.
            TChar[] poolArray = ArrayPool<TChar>.Shared.Rent(newCapacity);

            _chars.Slice(0, _pos).CopyTo(poolArray);

            TChar[]? toReturn = _arrayToReturnToPool;
            _chars = _arrayToReturnToPool = poolArray;
            if (toReturn != null)
            {
                ArrayPool<TChar>.Shared.Return(toReturn);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            TChar[]? toReturn = _arrayToReturnToPool;
            this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
            if (toReturn != null)
            {
                ArrayPool<TChar>.Shared.Return(toReturn);
            }
        }
    }
}
