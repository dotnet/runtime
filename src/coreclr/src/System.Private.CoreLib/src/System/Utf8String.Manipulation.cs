// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System
{
    public sealed partial class Utf8String
    {
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CheckSplitOptions(Utf8StringSplitOptions options)
        {
            if ((uint)options > (uint)(Utf8StringSplitOptions.RemoveEmptyEntries | Utf8StringSplitOptions.TrimEntries))
            {
                CheckSplitOptions_Throw(options);
            }
        }

        [StackTraceHidden]
        private static void CheckSplitOptions_Throw(Utf8StringSplitOptions options)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(options),
                message: SR.Format(SR.Arg_EnumIllegalVal, (int)options));
        }

        /// <summary>
        /// Substrings this <see cref="Utf8String"/> without bounds checking.
        /// </summary>
        private Utf8String InternalSubstring(int startIndex, int length)
        {
            Debug.Assert(startIndex >= 0, "StartIndex cannot be negative.");
            Debug.Assert(startIndex <= this.Length, "StartIndex cannot point beyond the end of the string (except to the null terminator).");
            Debug.Assert(length >= 0, "Length cannot be negative.");
            Debug.Assert(startIndex + length <= this.Length, "StartIndex and Length cannot point beyond the end of the string.");

            Debug.Assert(length != 0 && length != this.Length, "Caller should handle Length boundary conditions.");

            // Since Utf8String instances must contain well-formed UTF-8 data, we cannot allow a substring such that
            // either boundary of the new substring splits a multi-byte UTF-8 subsequence. Fortunately this is a very
            // easy check: since we assume the original buffer consisted entirely of well-formed UTF-8 data, all we
            // need to do is check that neither the substring we're about to create nor the substring that would
            // follow immediately thereafter begins with a UTF-8 continuation byte. Should this occur, it means that
            // the UTF-8 lead byte is in a prior substring, which would indicate a multi-byte sequence has been split.
            // It's ok for us to dereference the element immediately after the end of the Utf8String instance since
            // we know it's a null terminator.

            if (Utf8Utility.IsUtf8ContinuationByte(DangerousGetMutableReference(startIndex))
                || Utf8Utility.IsUtf8ContinuationByte(DangerousGetMutableReference(startIndex + length)))
            {
                ThrowImproperStringSplit();
            }

            Utf8String newString = FastAllocateSkipZeroInit(length);
            Buffer.Memmove(ref newString.DangerousGetMutableReference(), ref this.DangerousGetMutableReference(startIndex), (uint)length);
            return newString;
        }

        private Utf8String InternalSubstringWithoutCorrectnessChecks(int startIndex, int length)
        {
            Debug.Assert(startIndex >= 0, "StartIndex cannot be negative.");
            Debug.Assert(startIndex <= this.Length, "StartIndex cannot point beyond the end of the string (except to the null terminator).");
            Debug.Assert(length >= 0, "Length cannot be negative.");
            Debug.Assert(startIndex + length <= this.Length, "StartIndex and Length cannot point beyond the end of the string.");

            // In debug mode, perform the checks anyway. It's ok if we read just past the end of the
            // Utf8String instance, since we'll just be reading the null terminator (which is safe).

            Debug.Assert(!Utf8Utility.IsUtf8ContinuationByte(DangerousGetMutableReference(startIndex)), "Somebody is trying to split this Utf8String improperly.");
            Debug.Assert(!Utf8Utility.IsUtf8ContinuationByte(DangerousGetMutableReference(startIndex + length)), "Somebody is trying to split this Utf8String improperly.");

            if (length == 0)
            {
                return Empty;
            }
            else if (length == this.Length)
            {
                return this;
            }
            else
            {
                Utf8String newString = FastAllocateSkipZeroInit(length);
                Buffer.Memmove(ref newString.DangerousGetMutableReference(), ref this.DangerousGetMutableReference(startIndex), (uint)length);
                return newString;
            }
        }

        [StackTraceHidden]
        internal static void ThrowImproperStringSplit()
        {
            throw new InvalidOperationException(
                message: SR.Utf8String_CannotSplitMultibyteSubsequence);
        }

        internal Utf8String Substring(int startIndex, int length)
        {
            ValidateStartIndexAndLength(startIndex, length);

            // Optimizations: since instances are immutable, we can return 'this' or the known
            // Empty instance if the caller passed us a startIndex at the string boundary.

            if (length == 0)
            {
                return Empty;
            }

            if (length == this.Length)
            {
                return this;
            }

            return InternalSubstring(startIndex, length);
        }

        public SplitResult Split(char separator, Utf8StringSplitOptions options = Utf8StringSplitOptions.None)
        {
            if (!Rune.TryCreate(separator, out Rune rune))
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(separator),
                    message: SR.ArgumentOutOfRange_Utf16SurrogatesDisallowed);
            }

            CheckSplitOptions(options);

            return new SplitResult(this, rune, options);
        }

        public SplitResult Split(Rune separator, Utf8StringSplitOptions options = Utf8StringSplitOptions.None)
        {
            CheckSplitOptions(options);

            return new SplitResult(this, separator, options);
        }

        public SplitResult Split(Utf8String separator, Utf8StringSplitOptions options = Utf8StringSplitOptions.None)
        {
            if (IsNullOrEmpty(separator))
            {
                throw new ArgumentException(
                    paramName: nameof(separator),
                    message: SR.Argument_CannotBeNullOrEmpty);
            }

            CheckSplitOptions(options);

            return new SplitResult(this, separator, options);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOn(char separator)
        {
            return TryFind(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOn(char separator, StringComparison comparisonType)
        {
            return TryFind(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOn(Rune separator)
        {
            return TryFind(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOn(Rune separator, StringComparison comparisonType)
        {
            return TryFind(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOn(Utf8String separator)
        {
            return TryFind(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOn(Utf8String separator, StringComparison comparisonType)
        {
            return TryFind(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOnLast(char separator)
        {
            return TryFindLast(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOnLast(char separator, StringComparison comparisonType)
        {
            return TryFindLast(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOnLast(Rune separator)
        {
            return TryFindLast(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOnLast(Rune separator, StringComparison comparisonType)
        {
            return TryFindLast(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOnLast(Utf8String separator)
        {
            return TryFindLast(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8String"/> instance, creating <see cref="Utf8String"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8String"/> instance, returns the tuple "(this, null)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOnLast(Utf8String separator, StringComparison comparisonType)
        {
            return TryFindLast(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Trims whitespace from the beginning and the end of this <see cref="Utf8String"/>,
        /// returning a new <see cref="Utf8String"/> containing the resulting slice.
        /// </summary>
        public Utf8String Trim() => TrimHelper(TrimType.Both);

        /// <summary>
        /// Trims whitespace from only the end of this <see cref="Utf8String"/>,
        /// returning a new <see cref="Utf8String"/> containing the resulting slice.
        /// </summary>
        public Utf8String TrimEnd() => TrimHelper(TrimType.Tail);

        private Utf8String TrimHelper(TrimType trimType)
        {
            Utf8Span trimmedSpan = this.AsSpan().TrimHelper(trimType);

            // Try to avoid allocating a new Utf8String instance if possible.
            // Otherwise, allocate a new substring wrapped around the resulting slice.

            return (trimmedSpan.Length == this.Length) ? this : trimmedSpan.ToUtf8String();
        }

        /// <summary>
        /// Trims whitespace from only the beginning of this <see cref="Utf8String"/>,
        /// returning a new <see cref="Utf8String"/> containing the resulting slice.
        /// </summary>
        public Utf8String TrimStart() => TrimHelper(TrimType.Head);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        private void ValidateStartIndexAndLength(int startIndex, int length)
        {
#if BIT64
            // See comment in Span<T>.Slice for how this works.
            if ((ulong)(uint)startIndex + (ulong)(uint)length > (ulong)(uint)this.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();
#else
            if ((uint)startIndex > (uint)this.Length || (uint)length > (uint)(this.Length - startIndex))
                ThrowHelper.ThrowArgumentOutOfRangeException();
#endif
        }

        [StructLayout(LayoutKind.Auto)]
        public readonly struct SplitResult : IEnumerable<Utf8String?>
        {
            private readonly State _state;

            internal SplitResult(Utf8String source, Rune searchRune, Utf8StringSplitOptions splitOptions)
            {
                _state = new State
                {
                    FullSearchSpace = source,
                    OffsetAtWhichToContinueSearch = 0,
                    SearchRune = searchRune.Value,
                    SearchTerm = default,
                    SplitOptions = splitOptions
                };
            }

            internal SplitResult(Utf8String source, Utf8String searchTerm, Utf8StringSplitOptions splitOptions)
            {
                _state = new State
                {
                    FullSearchSpace = source,
                    OffsetAtWhichToContinueSearch = 0,
                    SearchRune = -1,
                    SearchTerm = searchTerm,
                    SplitOptions = splitOptions
                };
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String? item1, out Utf8String? item2)
            {
                _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span nextItem, out Utf8Span remainder);
                item1 = TrimIfNeeded(nextItem);

                item2 = TrimIfNeeded(remainder);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String? item1, out Utf8String? item2, out Utf8String? item3)
            {
                _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span nextItem, out Utf8Span remainder);
                item1 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item2 = TrimIfNeeded(nextItem);

                item3 = TrimIfNeeded(remainder);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String? item1, out Utf8String? item2, out Utf8String? item3, out Utf8String? item4)
            {
                _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span nextItem, out Utf8Span remainder);
                item1 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item2 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item3 = TrimIfNeeded(nextItem);

                item4 = TrimIfNeeded(remainder);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String? item1, out Utf8String? item2, out Utf8String? item3, out Utf8String? item4, out Utf8String? item5)
            {
                _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span nextItem, out Utf8Span remainder);
                item1 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item2 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item3 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item4 = TrimIfNeeded(nextItem);

                item5 = TrimIfNeeded(remainder);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String? item1, out Utf8String? item2, out Utf8String? item3, out Utf8String? item4, out Utf8String? item5, out Utf8String? item6)
            {
                _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span nextItem, out Utf8Span remainder);
                item1 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item2 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item3 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item4 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item5 = TrimIfNeeded(nextItem);

                item6 = TrimIfNeeded(remainder);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String? item1, out Utf8String? item2, out Utf8String? item3, out Utf8String? item4, out Utf8String? item5, out Utf8String? item6, out Utf8String? item7)
            {
                _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span nextItem, out Utf8Span remainder);
                item1 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item2 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item3 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item4 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item5 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item6 = TrimIfNeeded(nextItem);

                item7 = TrimIfNeeded(remainder);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String? item1, out Utf8String? item2, out Utf8String? item3, out Utf8String? item4, out Utf8String? item5, out Utf8String? item6, out Utf8String? item7, out Utf8String? item8)
            {
                _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span nextItem, out Utf8Span remainder);
                item1 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item2 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item3 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item4 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item5 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item6 = TrimIfNeeded(nextItem);

                _state.DeconstructHelper(in remainder, out nextItem, out remainder);
                item7 = TrimIfNeeded(nextItem);

                item8 = TrimIfNeeded(remainder);
            }

            public Enumerator GetEnumerator() => new Enumerator(this);

            private unsafe Utf8String? TrimIfNeeded(Utf8Span span)
            {
                if ((_state.SplitOptions & Utf8StringSplitOptions.TrimEntries) != 0)
                {
                    span = span.Trim();
                }

                if (span.Length < _state.FullSearchSpace.Length)
                {
                    if (!span.IsEmpty)
                    {
                        return span.ToUtf8String();
                    }
                    else
                    {
                        // normalize empty spans to null if needed, otherwise normalize to Utf8String.Empty

                        if ((_state.SplitOptions & Utf8StringSplitOptions.RemoveEmptyEntries) != 0
                            || Unsafe.AreSame(ref span.DangerousGetMutableReference(), ref Unsafe.AsRef<byte>(null)))
                        {
                            return null;
                        }

                        return Empty;
                    }
                }
                else
                {
                    // Don't bother making a copy of the entire Utf8String instance;
                    // just return the original value.

                    return _state.FullSearchSpace;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<Utf8String?> IEnumerable<Utf8String?>.GetEnumerator() => GetEnumerator();

            [StructLayout(LayoutKind.Auto)]
            public struct Enumerator : IEnumerator<Utf8String?>
            {
                private const Utf8StringSplitOptions HALT_ENUMERATION = (Utf8StringSplitOptions)int.MinValue;

                private Utf8String? _current;
                private State _state;

                internal Enumerator(SplitResult result)
                {
                    _current = null;
                    _state = result._state; // copy by value
                }

                public Utf8String? Current => _current;

                public bool MoveNext()
                {
                    bool wasMatchFound = _state.DeconstructHelper(_state.GetRemainingSearchSpace(), out Utf8Span firstItem, out Utf8Span remainder);

                    _current = (firstItem.IsEmpty) ? Empty : (firstItem.Length == _state.FullSearchSpace.Length) ? _state.FullSearchSpace : firstItem.ToUtf8String();
                    _state.OffsetAtWhichToContinueSearch = _state.FullSearchSpace.Length - remainder.Length;

                    if (wasMatchFound)
                    {
                        return true;
                    }

                    // At this point, the search term was not found within the search space. '_current' contains the last
                    // bit of data after the final occurrence of the search term. We'll also set a flag saying that we've
                    // completed enumeration.

                    if (firstItem.IsEmpty && (_state.SplitOptions & Utf8StringSplitOptions.RemoveEmptyEntries) != 0)
                    {
                        return false;
                    }

                    if ((_state.SplitOptions & HALT_ENUMERATION) != 0)
                    {
                        return false;
                    }

                    _state.SplitOptions |= HALT_ENUMERATION; // prevents yielding <empty> forever at end of split

                    return true;
                }

                void IDisposable.Dispose()
                {
                    // no-op
                }

                object? IEnumerator.Current => Current;

                void IEnumerator.Reset()
                {
                    throw new NotSupportedException();
                }
            }

            [StructLayout(LayoutKind.Auto)]
            private struct State // fully mutable
            {
                internal Utf8String FullSearchSpace;
                internal int OffsetAtWhichToContinueSearch;
                internal int SearchRune; // -1 if not specified, takes less space than "Rune?"
                internal Utf8String? SearchTerm;
                internal Utf8StringSplitOptions SplitOptions;

                // Returns 'true' if a match was found, 'false' otherwise.
                internal readonly bool DeconstructHelper(in Utf8Span source, out Utf8Span firstItem, out Utf8Span remainder)
                {
                    // n.b. Our callers might pass the same reference for 'source' and 'remainder'.
                    // We need to take care not to read 'source' after writing 'remainder'.

                    bool wasMatchFound;
                    ref readonly Utf8Span searchSpan = ref source;

                    while (true)
                    {
                        if (searchSpan.IsEmpty)
                        {
                            firstItem = searchSpan;
                            remainder = default;
                            wasMatchFound = false;
                            break;
                        }

                        Range matchRange;

                        int searchRune = SearchRune; // local copy so as to avoid struct tearing
                        if (searchRune >= 0)
                        {
                            wasMatchFound = searchSpan.TryFind(Rune.UnsafeCreate((uint)searchRune), out matchRange);
                        }
                        else
                        {
                            wasMatchFound = searchSpan.TryFind(SearchTerm, out matchRange);
                        }

                        if (!wasMatchFound)
                        {
                            // If no match was found, we move 'source' to 'firstItem', trim if necessary, and return right away.

                            firstItem = searchSpan;

                            if ((SplitOptions & Utf8StringSplitOptions.TrimEntries) != 0)
                            {
                                firstItem = firstItem.Trim();
                            }

                            remainder = default;
                        }
                        else
                        {
                            // Otherwise, if a match was found, split the result across 'firstItem' and 'remainder',
                            // applying trimming if necessary.

                            firstItem = searchSpan[..matchRange.Start]; // TODO_UTF8STRING: Could use unsafe slicing as optimization
                            remainder = searchSpan[matchRange.End..]; // TODO_UTF8STRING: Could use unsafe slicing as optimization

                            if ((SplitOptions & Utf8StringSplitOptions.TrimEntries) != 0)
                            {
                                firstItem = firstItem.Trim();
                            }

                            // If we're asked to remove empty entries, loop until there's a real value in 'firstItem'.

                            if ((SplitOptions & Utf8StringSplitOptions.RemoveEmptyEntries) != 0 && firstItem.IsEmpty)
                            {
                                searchSpan = ref remainder;
                                continue;
                            }
                        }

                        break; // loop only if explicit 'continue' statement was hit
                    }

                    return wasMatchFound;
                }

                internal Utf8Span GetRemainingSearchSpace()
                {
                    // TODO_UTF8STRING: The slice below can be optimized by performing a specialized bounds check
                    // and multi-byte subsequence check, since we don't need to check the end of the span.
                    // If we do optimize this we need to remember to make local copies of the fields we're reading
                    // to guard against torn structs.

                    return FullSearchSpace.AsSpanSkipNullCheck()[OffsetAtWhichToContinueSearch..];
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        public readonly struct SplitOnResult
        {
            // Used when there is no match.
            internal SplitOnResult(Utf8String originalSearchSpace)
            {
                Before = originalSearchSpace;
                After = null;
            }

            // Used when a match is found.
            internal SplitOnResult(Utf8String originalSearchSpace, Range searchTermMatchRange)
            {
                (int startIndex, int length) = searchTermMatchRange.GetOffsetAndLength(originalSearchSpace.Length);

                // TODO_UTF8STRING: The below indexer performs correctness checks. We can skip these checks (and even the
                // bounds checks more generally) since we know the inputs are all valid and the containing struct is not
                // subject to tearing.

                Before = originalSearchSpace[..startIndex];
                After = originalSearchSpace[(startIndex + length)..];
            }

            public Utf8String? After { get; }
            public Utf8String Before { get; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8String before, out Utf8String? after)
            {
                before = Before;
                after = After;
            }
        }
    }
}
