// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Text
{
    public readonly ref partial struct Utf8Span
    {
        public SplitResult Split(char separator, Utf8StringSplitOptions options = Utf8StringSplitOptions.None)
        {
            if (!Rune.TryCreate(separator, out Rune rune))
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(separator),
                    message: SR.ArgumentOutOfRange_Utf16SurrogatesDisallowed);
            }

            Utf8String.CheckSplitOptions(options);

            return new SplitResult(this, rune, options);
        }

        public SplitResult Split(Rune separator, Utf8StringSplitOptions options = Utf8StringSplitOptions.None)
        {
            Utf8String.CheckSplitOptions(options);

            return new SplitResult(this, separator, options);
        }

        public SplitResult Split(Utf8Span separator, Utf8StringSplitOptions options = Utf8StringSplitOptions.None)
        {
            if (separator.IsEmpty)
            {
                throw new ArgumentException(
                    paramName: nameof(separator),
                    message: SR.Argument_CannotBeEmptySpan);
            }

            Utf8String.CheckSplitOptions(options);

            return new SplitResult(this, separator, options);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOn(char separator)
        {
            return TryFind(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOn(char separator, StringComparison comparisonType)
        {
            return TryFind(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOn(Rune separator)
        {
            return TryFind(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOn(Rune separator, StringComparison comparisonType)
        {
            return TryFind(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOn(Utf8Span separator)
        {
            return TryFind(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOn(Utf8Span separator, StringComparison comparisonType)
        {
            return TryFind(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOnLast(char separator)
        {
            return TryFindLast(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOnLast(char separator, StringComparison comparisonType)
        {
            return TryFindLast(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOnLast(Rune separator)
        {
            return TryFindLast(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOnLast(Rune separator, StringComparison comparisonType)
        {
            return TryFindLast(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public SplitOnResult SplitOnLast(Utf8Span separator)
        {
            return TryFindLast(separator, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Locates the last occurrence of <paramref name="separator"/> within this <see cref="Utf8Span"/> instance, creating <see cref="Utf8Span"/>
        /// instances which represent the data on either side of the separator. If <paramref name="separator"/> is not found
        /// within this <see cref="Utf8Span"/> instance, returns the tuple "(this, Empty)".
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public SplitOnResult SplitOnLast(Utf8Span separator, StringComparison comparisonType)
        {
            return TryFindLast(separator, comparisonType, out Range range) ? new SplitOnResult(this, range) : new SplitOnResult(this);
        }

        /// <summary>
        /// Trims whitespace from the beginning and the end of this <see cref="Utf8Span"/>,
        /// returning a new <see cref="Utf8Span"/> containing the resulting slice.
        /// </summary>
        public Utf8Span Trim() => TrimHelper(TrimType.Both);

        /// <summary>
        /// Trims whitespace from only the end of this <see cref="Utf8Span"/>,
        /// returning a new <see cref="Utf8Span"/> containing the resulting slice.
        /// </summary>
        public Utf8Span TrimEnd() => TrimHelper(TrimType.Tail);

        internal Utf8Span TrimHelper(TrimType trimType)
        {
            ReadOnlySpan<byte> retSpan = Bytes;

            if ((trimType & TrimType.Head) != 0)
            {
                int indexOfFirstNonWhiteSpaceChar = Utf8Utility.GetIndexOfFirstNonWhiteSpaceChar(retSpan);
                Debug.Assert((uint)indexOfFirstNonWhiteSpaceChar <= (uint)retSpan.Length);

                // TODO_UTF8STRING: Can use an unsafe slicing routine below if we need a perf boost.

                retSpan = retSpan.Slice(indexOfFirstNonWhiteSpaceChar);
            }

            if ((trimType & TrimType.Tail) != 0)
            {
                int indexOfTrailingWhiteSpaceSequence = Utf8Utility.GetIndexOfTrailingWhiteSpaceSequence(retSpan);
                Debug.Assert((uint)indexOfTrailingWhiteSpaceSequence <= (uint)retSpan.Length);

                // TODO_UTF8STRING: Can use an unsafe slicing routine below if we need a perf boost.

                retSpan = retSpan.Slice(0, indexOfTrailingWhiteSpaceSequence);
            }

            return UnsafeCreateWithoutValidation(retSpan);
        }

        /// <summary>
        /// Trims whitespace from only the beginning of this <see cref="Utf8Span"/>,
        /// returning a new <see cref="Utf8Span"/> containing the resulting slice.
        /// </summary>
        public Utf8Span TrimStart() => TrimHelper(TrimType.Head);

        [StructLayout(LayoutKind.Auto)]
        public readonly ref struct SplitResult
        {
            private readonly State _state;

            internal SplitResult(Utf8Span source, Rune searchRune, Utf8StringSplitOptions splitOptions)
            {
                _state = new State
                {
                    RemainingSearchSpace = source,
                    SearchRune = searchRune.Value,
                    SearchTerm = default,
                    SplitOptions = splitOptions
                };
            }

            internal SplitResult(Utf8Span source, Utf8Span searchTerm, Utf8StringSplitOptions splitOptions)
            {
                _state = new State
                {
                    RemainingSearchSpace = source,
                    SearchRune = -1,
                    SearchTerm = searchTerm,
                    SplitOptions = splitOptions
                };
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span item1, out Utf8Span item2)
            {
                _state.DeconstructHelper(in _state.RemainingSearchSpace, out item1, out item2);
                TrimIfNeeded(ref item2);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span item1, out Utf8Span item2, out Utf8Span item3)
            {
                _state.DeconstructHelper(in _state.RemainingSearchSpace, out item1, out Utf8Span remainder);
                _state.DeconstructHelper(in remainder, out item2, out item3);
                TrimIfNeeded(ref item3);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span item1, out Utf8Span item2, out Utf8Span item3, out Utf8Span item4)
            {
                _state.DeconstructHelper(in _state.RemainingSearchSpace, out item1, out Utf8Span remainder);
                _state.DeconstructHelper(in remainder, out item2, out remainder);
                _state.DeconstructHelper(in remainder, out item3, out item4);
                TrimIfNeeded(ref item4);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span item1, out Utf8Span item2, out Utf8Span item3, out Utf8Span item4, out Utf8Span item5)
            {
                _state.DeconstructHelper(in _state.RemainingSearchSpace, out item1, out Utf8Span remainder);
                _state.DeconstructHelper(in remainder, out item2, out remainder);
                _state.DeconstructHelper(in remainder, out item3, out remainder);
                _state.DeconstructHelper(in remainder, out item4, out item5);
                TrimIfNeeded(ref item5);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span item1, out Utf8Span item2, out Utf8Span item3, out Utf8Span item4, out Utf8Span item5, out Utf8Span item6)
            {
                _state.DeconstructHelper(in _state.RemainingSearchSpace, out item1, out Utf8Span remainder);
                _state.DeconstructHelper(in remainder, out item2, out remainder);
                _state.DeconstructHelper(in remainder, out item3, out remainder);
                _state.DeconstructHelper(in remainder, out item4, out remainder);
                _state.DeconstructHelper(in remainder, out item5, out item6);
                TrimIfNeeded(ref item6);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span item1, out Utf8Span item2, out Utf8Span item3, out Utf8Span item4, out Utf8Span item5, out Utf8Span item6, out Utf8Span item7)
            {
                _state.DeconstructHelper(in _state.RemainingSearchSpace, out item1, out Utf8Span remainder);
                _state.DeconstructHelper(in remainder, out item2, out remainder);
                _state.DeconstructHelper(in remainder, out item3, out remainder);
                _state.DeconstructHelper(in remainder, out item4, out remainder);
                _state.DeconstructHelper(in remainder, out item5, out remainder);
                _state.DeconstructHelper(in remainder, out item6, out item7);
                TrimIfNeeded(ref item7);
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span item1, out Utf8Span item2, out Utf8Span item3, out Utf8Span item4, out Utf8Span item5, out Utf8Span item6, out Utf8Span item7, out Utf8Span item8)
            {
                _state.DeconstructHelper(in _state.RemainingSearchSpace, out item1, out Utf8Span remainder);
                _state.DeconstructHelper(in remainder, out item2, out remainder);
                _state.DeconstructHelper(in remainder, out item3, out remainder);
                _state.DeconstructHelper(in remainder, out item4, out remainder);
                _state.DeconstructHelper(in remainder, out item5, out remainder);
                _state.DeconstructHelper(in remainder, out item6, out remainder);
                _state.DeconstructHelper(in remainder, out item7, out item8);
                TrimIfNeeded(ref item8);
            }

            public Enumerator GetEnumerator() => new Enumerator(this);

            private void TrimIfNeeded(ref Utf8Span span)
            {
                if ((_state.SplitOptions & Utf8StringSplitOptions.TrimEntries) != 0)
                {
                    span = span.Trim();
                }
            }

            [StructLayout(LayoutKind.Auto)]
            public ref struct Enumerator
            {
                private const Utf8StringSplitOptions HALT_ENUMERATION = (Utf8StringSplitOptions)int.MinValue;

                private Utf8Span _current;
                private State _state;

                internal Enumerator(SplitResult result)
                {
                    _current = default;
                    _state = result._state; // copy by value
                }

                public Utf8Span Current => _current;

                public bool MoveNext()
                {
                    // Happy path: if the search term was found, then the two 'out' fields below are overwritten with
                    // the contents of the (before, after) tuple, and we can return right away.

                    if (_state.DeconstructHelper(in _state.RemainingSearchSpace, out _current, out _state.RemainingSearchSpace))
                    {
                        return true;
                    }

                    // At this point, the search term was not found within the search space. '_current' contains the last
                    // bit of data after the final occurrence of the search term. We'll also set a flag saying that we've
                    // completed enumeration.

                    if (_current.IsEmpty && (_state.SplitOptions & Utf8StringSplitOptions.RemoveEmptyEntries) != 0)
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
            }

            [StructLayout(LayoutKind.Auto)]
            private ref struct State // fully mutable
            {
                internal Utf8Span RemainingSearchSpace;
                internal int SearchRune; // -1 if not specified, takes less space than "Rune?"
                internal Utf8Span SearchTerm;
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

                        if (SearchRune >= 0)
                        {
                            wasMatchFound = searchSpan.TryFind(Rune.UnsafeCreate((uint)SearchRune), out matchRange);
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
            }
        }

        [StructLayout(LayoutKind.Auto)]
        public readonly ref struct SplitOnResult
        {
            // Used when there is no match.
            internal SplitOnResult(Utf8Span originalSearchSpace)
            {
                Before = originalSearchSpace;
                After = Empty;
            }

            // Used when a match is found.
            internal SplitOnResult(Utf8Span originalSearchSpace, Range searchTermMatchRange)
            {
                (int startIndex, int length) = searchTermMatchRange.GetOffsetAndLength(originalSearchSpace.Length);

                // TODO_UTF8STRING: The below indexer performs correctness checks. We can skip these checks (and even the
                // bounds checks more generally) since we know the inputs are all valid and the containing struct is not
                // subject to tearing.

                Before = originalSearchSpace[..startIndex];
                After = originalSearchSpace[(startIndex + length)..];
            }

            public Utf8Span After { get; }
            public Utf8Span Before { get; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Deconstruct(out Utf8Span before, out Utf8Span after)
            {
                before = Before;
                after = After;
            }
        }
    }
}
