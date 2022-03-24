// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System
{
    public partial class String
    {
        private const int StackallocIntBufferSizeLimit = 128;

        private static void FillStringChecked(string dest, int destPos, string src)
        {
            Debug.Assert(dest != null);
            Debug.Assert(src != null);
            if (src.Length > dest.Length - destPos)
            {
                throw new IndexOutOfRangeException();
            }

            Buffer.Memmove(
                destination: ref Unsafe.Add(ref dest._firstChar, destPos),
                source: ref src._firstChar,
                elementCount: (uint)src.Length);
        }

        public static string Concat(object? arg0) =>
            arg0?.ToString() ?? Empty;

        public static string Concat(object? arg0, object? arg1) =>
            Concat(arg0?.ToString(), arg1?.ToString());

        public static string Concat(object? arg0, object? arg1, object? arg2) =>
            Concat(arg0?.ToString(), arg1?.ToString(), arg2?.ToString());

        public static string Concat(params object?[] args!!)
        {
            if (args.Length <= 1)
            {
                return args.Length == 0 ?
                    string.Empty :
                    args[0]?.ToString() ?? string.Empty;
            }

            // We need to get an intermediary string array
            // to fill with each of the args' ToString(),
            // and then just concat that in one operation.

            // This way we avoid any intermediary string representations,
            // or buffer resizing if we use StringBuilder (although the
            // latter case is partially alleviated due to StringBuilder's
            // linked-list style implementation)

            var strings = new string[args.Length];

            int totalLength = 0;

            for (int i = 0; i < args.Length; i++)
            {
                object? value = args[i];

                string toString = value?.ToString() ?? string.Empty; // We need to handle both the cases when value or value.ToString() is null
                strings[i] = toString;

                totalLength += toString.Length;

                if (totalLength < 0) // Check for a positive overflow
                {
                    throw new OutOfMemoryException();
                }
            }

            // If all of the ToStrings are null/empty, just return string.Empty
            if (totalLength == 0)
            {
                return string.Empty;
            }

            string result = FastAllocateString(totalLength);
            int position = 0; // How many characters we've copied so far

            for (int i = 0; i < strings.Length; i++)
            {
                string s = strings[i];

                Debug.Assert(s != null);
                Debug.Assert(position <= totalLength - s.Length, "We didn't allocate enough space for the result string!");

                FillStringChecked(result, position, s);
                position += s.Length;
            }

            return result;
        }

        public static string Concat<T>(IEnumerable<T> values!!)
        {
            if (typeof(T) == typeof(char))
            {
                // Special-case T==char, as we can handle that case much more efficiently,
                // and string.Concat(IEnumerable<char>) can be used as an efficient
                // enumerable-based equivalent of new string(char[]).
                using (IEnumerator<char> en = Unsafe.As<IEnumerable<char>>(values).GetEnumerator())
                {
                    if (!en.MoveNext())
                    {
                        // There weren't any chars.  Return the empty string.
                        return Empty;
                    }

                    char c = en.Current; // save the first char

                    if (!en.MoveNext())
                    {
                        // There was only one char.  Return a string from it directly.
                        return CreateFromChar(c);
                    }

                    // Create the StringBuilder, add the chars we've already enumerated,
                    // add the rest, and then get the resulting string.
                    var result = new ValueStringBuilder(stackalloc char[256]);
                    result.Append(c); // first value
                    do
                    {
                        c = en.Current;
                        result.Append(c);
                    }
                    while (en.MoveNext());
                    return result.ToString();
                }
            }
            else
            {
                using (IEnumerator<T> en = values.GetEnumerator())
                {
                    if (!en.MoveNext())
                        return string.Empty;

                    // We called MoveNext once, so this will be the first item
                    T currentValue = en.Current;

                    // Call ToString before calling MoveNext again, since
                    // we want to stay consistent with the below loop
                    // Everything should be called in the order
                    // MoveNext-Current-ToString, unless further optimizations
                    // can be made, to avoid breaking changes
                    string? firstString = currentValue?.ToString();

                    // If there's only 1 item, simply call ToString on that
                    if (!en.MoveNext())
                    {
                        // We have to handle the case of either currentValue
                        // or its ToString being null
                        return firstString ?? string.Empty;
                    }

                    var result = new ValueStringBuilder(stackalloc char[256]);

                    result.Append(firstString);

                    do
                    {
                        currentValue = en.Current;

                        if (currentValue != null)
                        {
                            result.Append(currentValue.ToString());
                        }
                    }
                    while (en.MoveNext());

                    return result.ToString();
                }
            }
        }

        public static string Concat(IEnumerable<string?> values!!)
        {
            using (IEnumerator<string?> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return string.Empty;

                string? firstValue = en.Current;

                if (!en.MoveNext())
                {
                    return firstValue ?? string.Empty;
                }

                var result = new ValueStringBuilder(stackalloc char[256]);

                result.Append(firstValue);

                do
                {
                    result.Append(en.Current);
                }
                while (en.MoveNext());

                return result.ToString();
            }
        }

        public static string Concat(string? str0, string? str1)
        {
            if (IsNullOrEmpty(str0))
            {
                if (IsNullOrEmpty(str1))
                {
                    return string.Empty;
                }
                return str1;
            }

            if (IsNullOrEmpty(str1))
            {
                return str0;
            }

            int str0Length = str0.Length;

            string result = FastAllocateString(str0Length + str1.Length);

            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0Length, str1);

            return result;
        }

        public static string Concat(string? str0, string? str1, string? str2)
        {
            if (IsNullOrEmpty(str0))
            {
                return Concat(str1, str2);
            }

            if (IsNullOrEmpty(str1))
            {
                return Concat(str0, str2);
            }

            if (IsNullOrEmpty(str2))
            {
                return Concat(str0, str1);
            }

            int totalLength = str0.Length + str1.Length + str2.Length;

            string result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);

            return result;
        }

        public static string Concat(string? str0, string? str1, string? str2, string? str3)
        {
            if (IsNullOrEmpty(str0))
            {
                return Concat(str1, str2, str3);
            }

            if (IsNullOrEmpty(str1))
            {
                return Concat(str0, str2, str3);
            }

            if (IsNullOrEmpty(str2))
            {
                return Concat(str0, str1, str3);
            }

            if (IsNullOrEmpty(str3))
            {
                return Concat(str0, str1, str2);
            }

            int totalLength = str0.Length + str1.Length + str2.Length + str3.Length;

            string result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);
            FillStringChecked(result, str0.Length + str1.Length + str2.Length, str3);

            return result;
        }

        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1)
        {
            int length = checked(str0.Length + str1.Length);
            if (length == 0)
            {
                return Empty;
            }

            string result = FastAllocateString(length);
            Span<char> resultSpan = new Span<char>(ref result._firstChar, result.Length);

            str0.CopyTo(resultSpan);
            str1.CopyTo(resultSpan.Slice(str0.Length));

            return result;
        }

        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2)
        {
            int length = checked(str0.Length + str1.Length + str2.Length);
            if (length == 0)
            {
                return Empty;
            }

            string result = FastAllocateString(length);
            Span<char> resultSpan = new Span<char>(ref result._firstChar, result.Length);

            str0.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(str0.Length);

            str1.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(str1.Length);

            str2.CopyTo(resultSpan);

            return result;
        }

        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2, ReadOnlySpan<char> str3)
        {
            int length = checked(str0.Length + str1.Length + str2.Length + str3.Length);
            if (length == 0)
            {
                return Empty;
            }

            string result = FastAllocateString(length);
            Span<char> resultSpan = new Span<char>(ref result._firstChar, result.Length);

            str0.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(str0.Length);

            str1.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(str1.Length);

            str2.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(str2.Length);

            str3.CopyTo(resultSpan);

            return result;
        }

        public static string Concat(params string?[] values!!)
        {
            if (values.Length <= 1)
            {
                return values.Length == 0 ?
                    string.Empty :
                    values[0] ?? string.Empty;
            }

            // It's possible that the input values array could be changed concurrently on another
            // thread, such that we can't trust that each read of values[i] will be equivalent.
            // Worst case, we can make a defensive copy of the array and use that, but we first
            // optimistically try the allocation and copies assuming that the array isn't changing,
            // which represents the 99.999% case, in particular since string.Concat is used for
            // string concatenation by the languages, with the input array being a params array.

            // Sum the lengths of all input strings
            long totalLengthLong = 0;
            for (int i = 0; i < values.Length; i++)
            {
                string? value = values[i];
                if (value != null)
                {
                    totalLengthLong += value.Length;
                }
            }

            // If it's too long, fail, or if it's empty, return an empty string.
            if (totalLengthLong > int.MaxValue)
            {
                throw new OutOfMemoryException();
            }
            int totalLength = (int)totalLengthLong;
            if (totalLength == 0)
            {
                return string.Empty;
            }

            // Allocate a new string and copy each input string into it
            string result = FastAllocateString(totalLength);
            int copiedLength = 0;
            for (int i = 0; i < values.Length; i++)
            {
                string? value = values[i];
                if (!string.IsNullOrEmpty(value))
                {
                    int valueLen = value.Length;
                    if (valueLen > totalLength - copiedLength)
                    {
                        copiedLength = -1;
                        break;
                    }

                    FillStringChecked(result, copiedLength, value);
                    copiedLength += valueLen;
                }
            }

            // If we copied exactly the right amount, return the new string.  Otherwise,
            // something changed concurrently to mutate the input array: fall back to
            // doing the concatenation again, but this time with a defensive copy. This
            // fall back should be extremely rare.
            return copiedLength == totalLength ? result : Concat((string?[])values.Clone());
        }

        public static string Format(string format, object? arg0)
        {
            return FormatHelper(null, format, new ParamsArray(arg0));
        }

        public static string Format(string format, object? arg0, object? arg1)
        {
            return FormatHelper(null, format, new ParamsArray(arg0, arg1));
        }

        public static string Format(string format, object? arg0, object? arg1, object? arg2)
        {
            return FormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static string Format(string format, params object?[] args)
        {
            if (args is null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in FormatHelper.
                ArgumentNullException.Throw(format is null ? nameof(format) : nameof(args));
            }

            return FormatHelper(null, format, new ParamsArray(args));
        }

        public static string Format(IFormatProvider? provider, string format, object? arg0)
        {
            return FormatHelper(provider, format, new ParamsArray(arg0));
        }

        public static string Format(IFormatProvider? provider, string format, object? arg0, object? arg1)
        {
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1));
        }

        public static string Format(IFormatProvider? provider, string format, object? arg0, object? arg1, object? arg2)
        {
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static string Format(IFormatProvider? provider, string format, params object?[] args)
        {
            if (args is null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in FormatHelper.
                ArgumentNullException.Throw(format is null ? nameof(format) : nameof(args));
            }

            return FormatHelper(provider, format, new ParamsArray(args));
        }

        private static string FormatHelper(IFormatProvider? provider, string format!!, ParamsArray args)
        {
            var sb = new ValueStringBuilder(stackalloc char[256]);
            sb.EnsureCapacity(format.Length + args.Length * 8);
            sb.AppendFormatHelper(provider, format, args);
            return sb.ToString();
        }

        public string Insert(int startIndex, string value!!)
        {
            if ((uint)startIndex > Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int oldLength = Length;
            int insertLength = value.Length;

            if (oldLength == 0)
                return value;
            if (insertLength == 0)
                return this;

            // In case this computation overflows, newLength will be negative and FastAllocateString throws OutOfMemoryException
            int newLength = oldLength + insertLength;
            string result = FastAllocateString(newLength);

            Buffer.Memmove(ref result._firstChar, ref _firstChar, (nuint)startIndex);
            Buffer.Memmove(ref Unsafe.Add(ref result._firstChar, startIndex), ref value._firstChar, (nuint)insertLength);
            Buffer.Memmove(ref Unsafe.Add(ref result._firstChar, startIndex + insertLength), ref Unsafe.Add(ref _firstChar, startIndex), (nuint)(oldLength - startIndex));

            return result;
        }

        public static string Join(char separator, params string?[] value)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return JoinCore(MemoryMarshal.CreateReadOnlySpan(ref separator, 1), new ReadOnlySpan<string?>(value));
        }

        public static string Join(string? separator, params string?[] value)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return JoinCore(separator.AsSpan(), new ReadOnlySpan<string?>(value));
        }

        public static string Join(char separator, string?[] value, int startIndex, int count) =>
            JoinCore(MemoryMarshal.CreateReadOnlySpan(ref separator, 1), value, startIndex, count);

        public static string Join(string? separator, string?[] value, int startIndex, int count) =>
            JoinCore(separator.AsSpan(), value, startIndex, count);

        private static string JoinCore(ReadOnlySpan<char> separator, string?[] value!!, int startIndex, int count)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NegativeCount);
            }
            if (startIndex > value.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_IndexCountBuffer);
            }

            return JoinCore(separator, new ReadOnlySpan<string?>(value, startIndex, count));
        }

        public static string Join(string? separator, IEnumerable<string?> values)
        {
            if (values is List<string?> valuesList)
            {
                return JoinCore(separator.AsSpan(), CollectionsMarshal.AsSpan(valuesList));
            }

            if (values is string?[] valuesArray)
            {
                return JoinCore(separator.AsSpan(), new ReadOnlySpan<string?>(valuesArray));
            }

            if (values == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            using (IEnumerator<string?> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                {
                    return Empty;
                }

                string? firstValue = en.Current;

                if (!en.MoveNext())
                {
                    // Only one value available
                    return firstValue ?? Empty;
                }

                // Null separator and values are handled by the StringBuilder
                var result = new ValueStringBuilder(stackalloc char[256]);

                result.Append(firstValue);

                do
                {
                    result.Append(separator);
                    result.Append(en.Current);
                }
                while (en.MoveNext());

                return result.ToString();
            }
        }

        public static string Join(char separator, params object?[] values) =>
            JoinCore(MemoryMarshal.CreateReadOnlySpan(ref separator, 1), values);

        public static string Join(string? separator, params object?[] values) =>
            JoinCore(separator.AsSpan(), values);

        private static string JoinCore(ReadOnlySpan<char> separator, object?[] values)
        {
            if (values == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            if (values.Length == 0)
            {
                return Empty;
            }

            string? firstString = values[0]?.ToString();

            if (values.Length == 1)
            {
                return firstString ?? Empty;
            }

            var result = new ValueStringBuilder(stackalloc char[256]);

            result.Append(firstString);

            for (int i = 1; i < values.Length; i++)
            {
                result.Append(separator);
                object? value = values[i];
                if (value != null)
                {
                    result.Append(value.ToString());
                }
            }

            return result.ToString();
        }

        public static string Join<T>(char separator, IEnumerable<T> values) =>
            JoinCore(MemoryMarshal.CreateReadOnlySpan(ref separator, 1), values);

        public static string Join<T>(string? separator, IEnumerable<T> values) =>
            JoinCore(separator.AsSpan(), values);

        private static string JoinCore<T>(ReadOnlySpan<char> separator, IEnumerable<T> values)
        {
            if (typeof(T) == typeof(string))
            {
                if (values is List<string?> valuesList)
                {
                    return JoinCore(separator, CollectionsMarshal.AsSpan(valuesList));
                }

                if (values is string?[] valuesArray)
                {
                    return JoinCore(separator, new ReadOnlySpan<string?>(valuesArray));
                }
            }

            if (values == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                {
                    return Empty;
                }

                // We called MoveNext once, so this will be the first item
                T currentValue = en.Current;

                // Call ToString before calling MoveNext again, since
                // we want to stay consistent with the below loop
                // Everything should be called in the order
                // MoveNext-Current-ToString, unless further optimizations
                // can be made, to avoid breaking changes
                string? firstString = currentValue?.ToString();

                // If there's only 1 item, simply call ToString on that
                if (!en.MoveNext())
                {
                    // We have to handle the case of either currentValue
                    // or its ToString being null
                    return firstString ?? Empty;
                }

                var result = new ValueStringBuilder(stackalloc char[256]);

                result.Append(firstString);

                do
                {
                    currentValue = en.Current;

                    result.Append(separator);
                    if (currentValue != null)
                    {
                        result.Append(currentValue.ToString());
                    }
                }
                while (en.MoveNext());

                return result.ToString();
            }
        }

        private static string JoinCore(ReadOnlySpan<char> separator, ReadOnlySpan<string?> values)
        {
            if (values.Length <= 1)
            {
                return values.IsEmpty ?
                    Empty :
                    values[0] ?? Empty;
            }

            long totalSeparatorsLength = (long)(values.Length - 1) * separator.Length;
            if (totalSeparatorsLength > int.MaxValue)
            {
                ThrowHelper.ThrowOutOfMemoryException();
            }
            int totalLength = (int)totalSeparatorsLength;

            // Calculate the length of the resultant string so we know how much space to allocate.
            foreach (string? value in values)
            {
                if (value != null)
                {
                    totalLength += value.Length;
                    if (totalLength < 0) // Check for overflow
                    {
                        ThrowHelper.ThrowOutOfMemoryException();
                    }
                }
            }

            // Copy each of the strings into the result buffer, interleaving with the separator.
            string result = FastAllocateString(totalLength);
            int copiedLength = 0;

            for (int i = 0; i < values.Length; i++)
            {
                // It's possible that another thread may have mutated the input array
                // such that our second read of an index will not be the same string
                // we got during the first read.

                // We range check again to avoid buffer overflows if this happens.

                if (values[i] is string value)
                {
                    int valueLen = value.Length;
                    if (valueLen > totalLength - copiedLength)
                    {
                        copiedLength = -1;
                        break;
                    }

                    // Fill in the value.
                    FillStringChecked(result, copiedLength, value);
                    copiedLength += valueLen;
                }

                if (i < values.Length - 1)
                {
                    // Fill in the separator.
                    // Special-case length 1 to avoid additional overheads of CopyTo.
                    // This is common due to the char separator overload.

                    ref char dest = ref Unsafe.Add(ref result._firstChar, copiedLength);

                    if (separator.Length == 1)
                    {
                        dest = separator[0];
                    }
                    else
                    {
                        separator.CopyTo(new Span<char>(ref dest, separator.Length));
                    }

                    copiedLength += separator.Length;
                }
            }

            // If we copied exactly the right amount, return the new string.  Otherwise,
            // something changed concurrently to mutate the input array: fall back to
            // doing the concatenation again, but this time with a defensive copy. This
            // fall back should be extremely rare.
            return copiedLength == totalLength ?
                result :
                JoinCore(separator, values.ToArray().AsSpan());
        }

        public string PadLeft(int totalWidth) => PadLeft(totalWidth, ' ');

        public string PadLeft(int totalWidth, char paddingChar)
        {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(totalWidth), SR.ArgumentOutOfRange_NeedNonNegNum);
            int oldLength = Length;
            int count = totalWidth - oldLength;
            if (count <= 0)
                return this;

            string result = FastAllocateString(totalWidth);

            new Span<char>(ref result._firstChar, count).Fill(paddingChar);
            Buffer.Memmove(ref Unsafe.Add(ref result._firstChar, count), ref _firstChar, (nuint)oldLength);

            return result;
        }

        public string PadRight(int totalWidth) => PadRight(totalWidth, ' ');

        public string PadRight(int totalWidth, char paddingChar)
        {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(totalWidth), SR.ArgumentOutOfRange_NeedNonNegNum);
            int oldLength = Length;
            int count = totalWidth - oldLength;
            if (count <= 0)
                return this;

            string result = FastAllocateString(totalWidth);

            Buffer.Memmove(ref result._firstChar, ref _firstChar, (nuint)oldLength);
            new Span<char>(ref Unsafe.Add(ref result._firstChar, oldLength), count).Fill(paddingChar);

            return result;
        }

        public string Remove(int startIndex, int count)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NegativeCount);
            int oldLength = this.Length;
            if (count > oldLength - startIndex)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_IndexCount);

            if (count == 0)
                return this;
            int newLength = oldLength - count;
            if (newLength == 0)
                return Empty;

            string result = FastAllocateString(newLength);

            Buffer.Memmove(ref result._firstChar, ref _firstChar, (nuint)startIndex);
            Buffer.Memmove(ref Unsafe.Add(ref result._firstChar, startIndex), ref Unsafe.Add(ref _firstChar, startIndex + count), (nuint)(newLength - startIndex));

            return result;
        }

        // a remove that just takes a startindex.
        public string Remove(int startIndex)
        {
            if ((uint)startIndex > Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex < 0 ? SR.ArgumentOutOfRange_StartIndex : SR.ArgumentOutOfRange_StartIndexLargerThanLength);

            return Substring(0, startIndex);
        }

        public string Replace(string oldValue, string? newValue, bool ignoreCase, CultureInfo? culture)
        {
            return ReplaceCore(oldValue, newValue, culture?.CompareInfo, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public string Replace(string oldValue, string? newValue, StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return ReplaceCore(oldValue, newValue, CultureInfo.CurrentCulture.CompareInfo, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return ReplaceCore(oldValue, newValue, CompareInfo.Invariant, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                    return Replace(oldValue, newValue);

                case StringComparison.OrdinalIgnoreCase:
                    return ReplaceCore(oldValue, newValue, CompareInfo.Invariant, CompareOptions.OrdinalIgnoreCase);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        private string ReplaceCore(string oldValue, string? newValue, CompareInfo? ci, CompareOptions options)
        {
            ArgumentException.ThrowIfNullOrEmpty(oldValue);

            // If they asked to replace oldValue with a null, replace all occurrences
            // with the empty string. AsSpan() will normalize appropriately.
            //
            // If inner ReplaceCore method returns null, it means no substitutions were
            // performed, so as an optimization we'll return the original string.

            return ReplaceCore(this, oldValue.AsSpan(), newValue.AsSpan(), ci ?? CultureInfo.CurrentCulture.CompareInfo, options)
                ?? this;
        }

        private static string? ReplaceCore(ReadOnlySpan<char> searchSpace, ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue, CompareInfo compareInfo, CompareOptions options)
        {
            Debug.Assert(!oldValue.IsEmpty);
            Debug.Assert(compareInfo != null);

            var result = new ValueStringBuilder(stackalloc char[256]);
            result.EnsureCapacity(searchSpace.Length);

            bool hasDoneAnyReplacements = false;

            while (true)
            {
                int index = compareInfo.IndexOf(searchSpace, oldValue, options, out int matchLength);

                // There's the possibility that 'oldValue' has zero collation weight (empty string equivalent).
                // If this is the case, we behave as if there are no more substitutions to be made.

                if (index < 0 || matchLength == 0)
                {
                    break;
                }

                // append the unmodified portion of search space
                result.Append(searchSpace.Slice(0, index));

                // append the replacement
                result.Append(newValue);

                searchSpace = searchSpace.Slice(index + matchLength);
                hasDoneAnyReplacements = true;
            }

            // Didn't find 'oldValue' in the remaining search space, or the match
            // consisted only of zero collation weight characters. As an optimization,
            // if we have not yet performed any replacements, we'll save the
            // allocation.

            if (!hasDoneAnyReplacements)
            {
                result.Dispose();
                return null;
            }

            // Append what remains of the search space, then allocate the new string.

            result.Append(searchSpace);
            return result.ToString();
        }

        // Replaces all instances of oldChar with newChar.
        //
        public string Replace(char oldChar, char newChar)
        {
            if (oldChar == newChar)
                return this;

            int firstIndex = IndexOf(oldChar);

            if (firstIndex < 0)
                return this;

            int remainingLength = Length - firstIndex;
            string result = FastAllocateString(Length);

            int copyLength = firstIndex;

            // Copy the characters already proven not to match.
            if (copyLength > 0)
            {
                Buffer.Memmove(ref result._firstChar, ref _firstChar, (uint)copyLength);
            }

            // Copy the remaining characters, doing the replacement as we go.
            ref ushort pSrc = ref Unsafe.Add(ref Unsafe.As<char, ushort>(ref _firstChar), copyLength);
            ref ushort pDst = ref Unsafe.Add(ref Unsafe.As<char, ushort>(ref result._firstChar), copyLength);

            if (Vector.IsHardwareAccelerated && remainingLength >= Vector<ushort>.Count)
            {
                Vector<ushort> oldChars = new Vector<ushort>(oldChar);
                Vector<ushort> newChars = new Vector<ushort>(newChar);

                do
                {
                    Vector<ushort> original = Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<ushort, byte>(ref pSrc));
                    Vector<ushort> equals = Vector.Equals(original, oldChars);
                    Vector<ushort> results = Vector.ConditionalSelect(equals, newChars, original);
                    Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref pDst), results);

                    pSrc = ref Unsafe.Add(ref pSrc, Vector<ushort>.Count);
                    pDst = ref Unsafe.Add(ref pDst, Vector<ushort>.Count);
                    remainingLength -= Vector<ushort>.Count;
                }
                while (remainingLength >= Vector<ushort>.Count);
            }

            for (; remainingLength > 0; remainingLength--)
            {
                ushort currentChar = pSrc;
                pDst = currentChar == oldChar ? newChar : currentChar;

                pSrc = ref Unsafe.Add(ref pSrc, 1);
                pDst = ref Unsafe.Add(ref pDst, 1);
            }

            return result;
        }

        public string Replace(string oldValue, string? newValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(oldValue);

            // If newValue is null, treat it as an empty string.  Callers use this to remove the oldValue.
            newValue ??= Empty;

            // Track the locations of oldValue to be replaced.
            var replacementIndices = new ValueListBuilder<int>(stackalloc int[StackallocIntBufferSizeLimit]);

            if (oldValue.Length == 1)
            {
                // Special-case oldValues that are a single character.  Even though there's an overload that takes
                // a single character, its newValue is also a single character, so this overload ends up being used
                // often to remove characters by having an empty newValue.

                if (newValue.Length == 1)
                {
                    // With both the oldValue and newValue being a single character, it's cheaper to just use the other overload.
                    return Replace(oldValue[0], newValue[0]);
                }

                // Find all occurrences of the oldValue character.
                char c = oldValue[0];
                int i = 0;
                while (true)
                {
                    int pos = SpanHelpers.IndexOf(ref Unsafe.Add(ref _firstChar, i), c, Length - i);
                    if (pos < 0)
                    {
                        break;
                    }
                    replacementIndices.Append(i + pos);
                    i += pos + 1;
                }
            }
            else
            {
                // Find all occurrences of the oldValue string.
                int i = 0;
                while (true)
                {
                    int pos = SpanHelpers.IndexOf(ref Unsafe.Add(ref _firstChar, i), Length - i, ref oldValue._firstChar, oldValue.Length);
                    if (pos < 0)
                    {
                        break;
                    }
                    replacementIndices.Append(i + pos);
                    i += pos + oldValue.Length;
                }
            }

            // If the oldValue wasn't found, just return the original string.
            if (replacementIndices.Length == 0)
            {
                return this;
            }

            // Perform the replacement. String allocation and copying is in separate method to make this method faster
            // for the case where nothing needs replacing.
            string dst = ReplaceHelper(oldValue.Length, newValue, replacementIndices.AsSpan());

            replacementIndices.Dispose();

            return dst;
        }

        private string ReplaceHelper(int oldValueLength, string newValue, ReadOnlySpan<int> indices)
        {
            Debug.Assert(indices.Length > 0);

            long dstLength = this.Length + ((long)(newValue.Length - oldValueLength)) * indices.Length;
            if (dstLength > int.MaxValue)
                throw new OutOfMemoryException();
            string dst = FastAllocateString((int)dstLength);

            Span<char> dstSpan = new Span<char>(ref dst._firstChar, dst.Length);

            int thisIdx = 0;
            int dstIdx = 0;

            for (int r = 0; r < indices.Length; r++)
            {
                int replacementIdx = indices[r];

                // Copy over the non-matching portion of the original that precedes this occurrence of oldValue.
                int count = replacementIdx - thisIdx;
                if (count != 0)
                {
                    this.AsSpan(thisIdx, count).CopyTo(dstSpan.Slice(dstIdx));
                    dstIdx += count;
                }
                thisIdx = replacementIdx + oldValueLength;

                // Copy over newValue to replace the oldValue.
                newValue.CopyTo(dstSpan.Slice(dstIdx));
                dstIdx += newValue.Length;
            }

            // Copy over the final non-matching portion at the end of the string.
            Debug.Assert(this.Length - thisIdx == dstSpan.Length - dstIdx);
            this.AsSpan(thisIdx).CopyTo(dstSpan.Slice(dstIdx));

            return dst;
        }

        /// <summary>
        /// Replaces all newline sequences in the current string with <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <returns>
        /// A string whose contents match the current string, but with all newline sequences replaced
        /// with <see cref="Environment.NewLine"/>.
        /// </returns>
        /// <remarks>
        /// This method searches for all newline sequences within the string and canonicalizes them to match
        /// the newline sequence for the current environment. For example, when running on Windows, all
        /// occurrences of non-Windows newline sequences will be replaced with the sequence CRLF. When
        /// running on Unix, all occurrences of non-Unix newline sequences will be replaced with
        /// a single LF character.
        ///
        /// It is not recommended that protocol parsers utilize this API. Protocol specifications often
        /// mandate specific newline sequences. For example, HTTP/1.1 (RFC 8615) mandates that the request
        /// line, status line, and headers lines end with CRLF. Since this API operates over a wide range
        /// of newline sequences, a protocol parser utilizing this API could exhibit behaviors unintended
        /// by the protocol's authors.
        ///
        /// This overload is equivalent to calling <see cref="ReplaceLineEndings(string)"/>, passing
        /// <see cref="Environment.NewLine"/> as the <em>replacementText</em> parameter.
        ///
        /// This method is guaranteed O(n) complexity, where <em>n</em> is the length of the input string.
        /// </remarks>
        public string ReplaceLineEndings() => ReplaceLineEndings(Environment.NewLineConst);

        /// <summary>
        /// Replaces all newline sequences in the current string with <paramref name="replacementText"/>.
        /// </summary>
        /// <returns>
        /// A string whose contents match the current string, but with all newline sequences replaced
        /// with <paramref name="replacementText"/>.
        /// </returns>
        /// <remarks>
        /// This method searches for all newline sequences within the string and canonicalizes them to the
        /// newline sequence provided by <paramref name="replacementText"/>. If <paramref name="replacementText"/>
        /// is <see cref="string.Empty"/>, all newline sequences within the string will be removed.
        ///
        /// It is not recommended that protocol parsers utilize this API. Protocol specifications often
        /// mandate specific newline sequences. For example, HTTP/1.1 (RFC 8615) mandates that the request
        /// line, status line, and headers lines end with CRLF. Since this API operates over a wide range
        /// of newline sequences, a protocol parser utilizing this API could exhibit behaviors unintended
        /// by the protocol's authors.
        ///
        /// The list of recognized newline sequences is CR (U+000D), LF (U+000A), CRLF (U+000D U+000A),
        /// NEL (U+0085), LS (U+2028), FF (U+000C), and PS (U+2029). This list is given by the Unicode
        /// Standard, Sec. 5.8, Recommendation R4 and Table 5-2.
        ///
        /// This method is guaranteed O(n * r) complexity, where <em>n</em> is the length of the input string,
        /// and where <em>r</em> is the length of <paramref name="replacementText"/>.
        /// </remarks>
        public string ReplaceLineEndings(string replacementText!!)
        {
            // Early-exit: do we need to do anything at all?
            // If not, return this string as-is.

            int idxOfFirstNewlineChar = IndexOfNewlineChar(this, out int stride);
            if (idxOfFirstNewlineChar < 0)
            {
                return this;
            }

            // While writing to the builder, we don't bother memcpying the first
            // or the last segment into the builder. We'll use the builder only
            // for the intermediate segments, then we'll sandwich everything together
            // with one final string.Concat call.

            ReadOnlySpan<char> firstSegment = this.AsSpan(0, idxOfFirstNewlineChar);
            ReadOnlySpan<char> remaining = this.AsSpan(idxOfFirstNewlineChar + stride);

            ValueStringBuilder builder = new ValueStringBuilder(stackalloc char[256]);
            while (true)
            {
                int idx = IndexOfNewlineChar(remaining, out stride);
                if (idx < 0) { break; } // no more newline chars
                builder.Append(replacementText);
                builder.Append(remaining.Slice(0, idx));
                remaining = remaining.Slice(idx + stride);
            }

            string retVal = Concat(firstSegment, builder.AsSpan(), replacementText, remaining);
            builder.Dispose();
            return retVal;
        }

        // Scans the input text, returning the index of the first newline char.
        // Newline chars are given by the Unicode Standard, Sec. 5.8.
        internal static int IndexOfNewlineChar(ReadOnlySpan<char> text, out int stride)
        {
            // !! IMPORTANT !!
            //
            // We expect this method may be called with untrusted input, which means we need to
            // bound the worst-case runtime of this method. We rely on MemoryExtensions.IndexOfAny
            // having worst-case runtime O(i), where i is the index of the first needle match within
            // the haystack; or O(n) if no needle is found. This ensures that in the common case
            // of this method being called within a loop, the worst-case runtime is O(n) rather than
            // O(n^2), where n is the length of the input text.
            //
            // The Unicode Standard, Sec. 5.8, Recommendation R4 and Table 5-2 state that the CR, LF,
            // CRLF, NEL, LS, FF, and PS sequences are considered newline functions. That section
            // also specifically excludes VT from the list of newline functions, so we do not include
            // it in the needle list.

            const string needles = "\r\n\f\u0085\u2028\u2029";

            stride = default;
            int idx = text.IndexOfAny(needles);
            if ((uint)idx < (uint)text.Length)
            {
                stride = 1; // needle found

                // Did we match CR? If so, and if it's followed by LF, then we need
                // to consume both chars as a single newline function match.

                if (text[idx] == '\r')
                {
                    int nextCharIdx = idx + 1;
                    if ((uint)nextCharIdx < (uint)text.Length && text[nextCharIdx] == '\n')
                    {
                        stride = 2;
                    }
                }
            }

            return idx;
        }

        public string[] Split(char separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return SplitInternal(new ReadOnlySpan<char>(ref separator, 1), int.MaxValue, options);
        }

        public string[] Split(char separator, int count, StringSplitOptions options = StringSplitOptions.None)
        {
            return SplitInternal(new ReadOnlySpan<char>(ref separator, 1), count, options);
        }

        // Creates an array of strings by splitting this string at each
        // occurrence of a separator.  The separator is searched for, and if found,
        // the substring preceding the occurrence is stored as the first element in
        // the array of strings.  We then continue in this manner by searching
        // the substring that follows the occurrence.  On the other hand, if the separator
        // is not found, the array of strings will contain this instance as its only element.
        // If the separator is null
        // whitespace (i.e., Character.IsWhitespace) is used as the separator.
        //
        public string[] Split(params char[]? separator)
        {
            return SplitInternal(separator, int.MaxValue, StringSplitOptions.None);
        }

        // Creates an array of strings by splitting this string at each
        // occurrence of a separator.  The separator is searched for, and if found,
        // the substring preceding the occurrence is stored as the first element in
        // the array of strings.  We then continue in this manner by searching
        // the substring that follows the occurrence.  On the other hand, if the separator
        // is not found, the array of strings will contain this instance as its only element.
        // If the separator is the empty string (i.e., string.Empty), then
        // whitespace (i.e., Character.IsWhitespace) is used as the separator.
        // If there are more than count different strings, the last n-(count-1)
        // elements are concatenated and added as the last string.
        //
        public string[] Split(char[]? separator, int count)
        {
            return SplitInternal(separator, count, StringSplitOptions.None);
        }

        public string[] Split(char[]? separator, StringSplitOptions options)
        {
            return SplitInternal(separator, int.MaxValue, options);
        }

        public string[] Split(char[]? separator, int count, StringSplitOptions options)
        {
            return SplitInternal(separator, count, options);
        }

        private string[] SplitInternal(ReadOnlySpan<char> separators, int count, StringSplitOptions options)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                    SR.ArgumentOutOfRange_NegativeCount);

            CheckStringSplitOptions(options);

        ShortCircuit:
            if (count <= 1 || Length == 0)
            {
                // Per the method's documentation, we'll short-circuit the search for separators.
                // But we still need to post-process the results based on the caller-provided flags.

                string candidate = this;
                if (((options & StringSplitOptions.TrimEntries) != 0) && (count > 0))
                {
                    candidate = candidate.Trim();
                }
                if (((options & StringSplitOptions.RemoveEmptyEntries) != 0) && (candidate.Length == 0))
                {
                    count = 0;
                }
                return (count == 0) ? Array.Empty<string>() : new string[] { candidate };
            }

            if (separators.IsEmpty)
            {
                // Caller is already splitting on whitespace; no need for separate trim step
                options &= ~StringSplitOptions.TrimEntries;
            }

            var sepListBuilder = new ValueListBuilder<int>(stackalloc int[StackallocIntBufferSizeLimit]);

            MakeSeparatorList(separators, ref sepListBuilder);
            ReadOnlySpan<int> sepList = sepListBuilder.AsSpan();

            // Handle the special case of no replaces.
            if (sepList.Length == 0)
            {
                count = 1;
                goto ShortCircuit;
            }

            string[] result = (options != StringSplitOptions.None)
                ? SplitWithPostProcessing(sepList, default, 1, count, options)
                : SplitWithoutPostProcessing(sepList, default, 1, count);

            sepListBuilder.Dispose();

            return result;
        }

        public string[] Split(string? separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return SplitInternal(separator ?? string.Empty, null, int.MaxValue, options);
        }

        public string[] Split(string? separator, int count, StringSplitOptions options = StringSplitOptions.None)
        {
            return SplitInternal(separator ?? string.Empty, null, count, options);
        }

        public string[] Split(string[]? separator, StringSplitOptions options)
        {
            return SplitInternal(null, separator, int.MaxValue, options);
        }

        public string[] Split(string[]? separator, int count, StringSplitOptions options)
        {
            return SplitInternal(null, separator, count, options);
        }

        private string[] SplitInternal(string? separator, string?[]? separators, int count, StringSplitOptions options)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    SR.ArgumentOutOfRange_NegativeCount);
            }

            CheckStringSplitOptions(options);

            bool singleSeparator = separator != null;

            if (!singleSeparator && (separators == null || separators.Length == 0))
            {
                // split on whitespace
                return SplitInternal(default(ReadOnlySpan<char>), count, options);
            }

        ShortCircuit:
            if (count <= 1 || Length == 0)
            {
                // Per the method's documentation, we'll short-circuit the search for separators.
                // But we still need to post-process the results based on the caller-provided flags.

                string candidate = this;
                if (((options & StringSplitOptions.TrimEntries) != 0) && (count > 0))
                {
                    candidate = candidate.Trim();
                }
                if (((options & StringSplitOptions.RemoveEmptyEntries) != 0) && (candidate.Length == 0))
                {
                    count = 0;
                }
                return (count == 0) ? Array.Empty<string>() : new string[] { candidate };
            }

            if (singleSeparator)
            {
                if (separator!.Length == 0)
                {
                    count = 1;
                    goto ShortCircuit;
                }
                else
                {
                    return SplitInternal(separator, count, options);
                }
            }

            var sepListBuilder = new ValueListBuilder<int>(stackalloc int[StackallocIntBufferSizeLimit]);
            var lengthListBuilder = new ValueListBuilder<int>(stackalloc int[StackallocIntBufferSizeLimit]);

            MakeSeparatorList(separators!, ref sepListBuilder, ref lengthListBuilder);
            ReadOnlySpan<int> sepList = sepListBuilder.AsSpan();
            ReadOnlySpan<int> lengthList = lengthListBuilder.AsSpan();

            // Handle the special case of no replaces.
            if (sepList.Length == 0)
            {
                return new string[] { this };
            }

            string[] result = (options != StringSplitOptions.None)
                ? SplitWithPostProcessing(sepList, lengthList, 0, count, options)
                : SplitWithoutPostProcessing(sepList, lengthList, 0, count);

            sepListBuilder.Dispose();
            lengthListBuilder.Dispose();

            return result;
        }

        private string[] SplitInternal(string separator, int count, StringSplitOptions options)
        {
            var sepListBuilder = new ValueListBuilder<int>(stackalloc int[StackallocIntBufferSizeLimit]);

            MakeSeparatorList(separator, ref sepListBuilder);
            ReadOnlySpan<int> sepList = sepListBuilder.AsSpan();
            if (sepList.Length == 0)
            {
                // there are no separators so sepListBuilder did not rent an array from pool and there is no need to dispose it
                string candidate = this;
                if ((options & StringSplitOptions.TrimEntries) != 0)
                {
                    candidate = candidate.Trim();
                }
                return ((candidate.Length == 0) && ((options & StringSplitOptions.RemoveEmptyEntries) != 0))
                    ? Array.Empty<string>()
                    : new string[] { candidate };
            }

            string[] result = (options != StringSplitOptions.None)
                ? SplitWithPostProcessing(sepList, default, separator.Length, count, options)
                : SplitWithoutPostProcessing(sepList, default, separator.Length, count);

            sepListBuilder.Dispose();

            return result;
        }

        // This function will not trim entries or special-case empty entries
        private string[] SplitWithoutPostProcessing(ReadOnlySpan<int> sepList, ReadOnlySpan<int> lengthList, int defaultLength, int count)
        {
            Debug.Assert(count >= 2);

            int currIndex = 0;
            int arrIndex = 0;

            count--;
            int numActualReplaces = (sepList.Length < count) ? sepList.Length : count;

            // Allocate space for the new array.
            // +1 for the string from the end of the last replace to the end of the string.
            string[] splitStrings = new string[numActualReplaces + 1];

            for (int i = 0; i < numActualReplaces && currIndex < Length; i++)
            {
                splitStrings[arrIndex++] = Substring(currIndex, sepList[i] - currIndex);
                currIndex = sepList[i] + (lengthList.IsEmpty ? defaultLength : lengthList[i]);
            }

            // Handle the last string at the end of the array if there is one.
            if (currIndex < Length && numActualReplaces >= 0)
            {
                splitStrings[arrIndex] = Substring(currIndex);
            }
            else if (arrIndex == numActualReplaces)
            {
                // We had a separator character at the end of a string.  Rather than just allowing
                // a null character, we'll replace the last element in the array with an empty string.
                splitStrings[arrIndex] = string.Empty;
            }

            return splitStrings;
        }


        // This function may trim entries or omit empty entries
        private string[] SplitWithPostProcessing(ReadOnlySpan<int> sepList, ReadOnlySpan<int> lengthList, int defaultLength, int count, StringSplitOptions options)
        {
            Debug.Assert(count >= 2);

            int numReplaces = sepList.Length;

            // Allocate array to hold items. This array may not be
            // filled completely in this function, we will create a
            // new array and copy string references to that new array.
            int maxItems = (numReplaces < count) ? (numReplaces + 1) : count;
            string[] splitStrings = new string[maxItems];

            int currIndex = 0;
            int arrIndex = 0;

            ReadOnlySpan<char> thisEntry;

            for (int i = 0; i < numReplaces; i++)
            {
                thisEntry = this.AsSpan(currIndex, sepList[i] - currIndex);
                if ((options & StringSplitOptions.TrimEntries) != 0)
                {
                    thisEntry = thisEntry.Trim();
                }
                if (!thisEntry.IsEmpty || ((options & StringSplitOptions.RemoveEmptyEntries) == 0))
                {
                    splitStrings[arrIndex++] = thisEntry.ToString();
                }
                currIndex = sepList[i] + (lengthList.IsEmpty ? defaultLength : lengthList[i]);
                if (arrIndex == count - 1)
                {
                    // The next iteration of the loop will provide the final entry into the
                    // results array. If needed, skip over all empty entries before that
                    // point.
                    if ((options & StringSplitOptions.RemoveEmptyEntries) != 0)
                    {
                        while (++i < numReplaces)
                        {
                            thisEntry = this.AsSpan(currIndex, sepList[i] - currIndex);
                            if ((options & StringSplitOptions.TrimEntries) != 0)
                            {
                                thisEntry = thisEntry.Trim();
                            }
                            if (!thisEntry.IsEmpty)
                            {
                                break; // there's useful data here
                            }
                            currIndex = sepList[i] + (lengthList.IsEmpty ? defaultLength : lengthList[i]);
                        }
                    }
                    break;
                }
            }

            // we must have at least one slot left to fill in the last string.
            Debug.Assert(arrIndex < maxItems);

            // Handle the last substring at the end of the array
            // (could be empty if separator appeared at the end of the input string)
            thisEntry = this.AsSpan(currIndex);
            if ((options & StringSplitOptions.TrimEntries) != 0)
            {
                thisEntry = thisEntry.Trim();
            }
            if (!thisEntry.IsEmpty || ((options & StringSplitOptions.RemoveEmptyEntries) == 0))
            {
                splitStrings[arrIndex++] = thisEntry.ToString();
            }

            Array.Resize(ref splitStrings, arrIndex);
            return splitStrings;
        }

        /// <summary>
        /// Uses ValueListBuilder to create list that holds indexes of separators in string.
        /// </summary>
        /// <param name="separators"><see cref="ReadOnlySpan{T}"/> of separator chars</param>
        /// <param name="sepListBuilder"><see cref="ValueListBuilder{T}"/> to store indexes</param>
        private void MakeSeparatorList(ReadOnlySpan<char> separators, ref ValueListBuilder<int> sepListBuilder)
        {
            // Special-case no separators to mean any whitespace is a separator.
            if (separators.Length == 0)
            {
                for (int i = 0; i < Length; i++)
                {
                    if (char.IsWhiteSpace(this[i]))
                    {
                        sepListBuilder.Append(i);
                    }
                }
            }

            // Special-case the common cases of 1, 2, and 3 separators, with manual comparisons against each separator.
            else if (separators.Length <= 3)
            {
                char sep0, sep1, sep2;
                sep0 = separators[0];
                sep1 = separators.Length > 1 ? separators[1] : sep0;
                sep2 = separators.Length > 2 ? separators[2] : sep1;
                if (Vector128.IsHardwareAccelerated && Length >= Vector128<ushort>.Count * 2)
                {
                    MakeSeparatorListVectorized(ref sepListBuilder, sep0, sep1, sep2);
                    return;
                }

                for (int i = 0; i < Length; i++)
                {
                    char c = this[i];
                    if (c == sep0 || c == sep1 || c == sep2)
                    {
                        sepListBuilder.Append(i);
                    }
                }
            }

            // Handle > 3 separators with a probabilistic map, ala IndexOfAny.
            // This optimizes for chars being unlikely to match a separator.
            else
            {
                unsafe
                {
                    ProbabilisticMap map = default;
                    uint* charMap = (uint*)&map;
                    ProbabilisticMap.Initialize(charMap, separators);

                    for (int i = 0; i < Length; i++)
                    {
                        char c = this[i];
                        if (ProbabilisticMap.IsCharBitSet(charMap, (byte)c) &&
                            ProbabilisticMap.IsCharBitSet(charMap, (byte)(c >> 8)) &&
                            separators.Contains(c))
                        {
                            sepListBuilder.Append(i);
                        }
                    }
                }
            }
        }

        private void MakeSeparatorListVectorized(ref ValueListBuilder<int> sepListBuilder, char c, char c2, char c3)
        {
            // Redundant test so we won't prejit remainder of this method
            // on platforms where it is not supported
            if (!Vector128.IsHardwareAccelerated)
            {
                throw new PlatformNotSupportedException();
            }

            Debug.Assert(Length >= Vector128<ushort>.Count);

            nuint offset = 0;
            nuint lengthToExamine = (nuint)(uint)Length;

            ref ushort source = ref Unsafe.As<char, ushort>(ref _firstChar);

            Vector128<ushort> v1 = Vector128.Create((ushort)c);
            Vector128<ushort> v2 = Vector128.Create((ushort)c2);
            Vector128<ushort> v3 = Vector128.Create((ushort)c3);

            do
            {
                Vector128<ushort> vector = Vector128.LoadUnsafe(ref source, offset);
                Vector128<ushort> v1Eq = Vector128.Equals(vector, v1);
                Vector128<ushort> v2Eq = Vector128.Equals(vector, v2);
                Vector128<ushort> v3Eq = Vector128.Equals(vector, v3);
                Vector128<byte> cmp = (v1Eq | v2Eq | v3Eq).AsByte();

                if (cmp != Vector128<byte>.Zero)
                {
                    // Skip every other bit
                    uint mask = cmp.ExtractMostSignificantBits() & 0x5555;
                    do
                    {
                        uint bitPos = (uint)BitOperations.TrailingZeroCount(mask) / sizeof(char);
                        sepListBuilder.Append((int)(offset + bitPos));
                        mask = BitOperations.ResetLowestSetBit(mask);
                    } while (mask != 0);
                }

                offset += (nuint)Vector128<ushort>.Count;
            } while (offset <= lengthToExamine - (nuint)Vector128<ushort>.Count);

            while (offset < lengthToExamine)
            {
                char curr = (char)Unsafe.Add(ref source, offset);
                if (curr == c || curr == c2 || curr == c3)
                {
                    sepListBuilder.Append((int)offset);
                }
                offset++;
            }
        }

        /// <summary>
        /// Uses ValueListBuilder to create list that holds indexes of separators in string.
        /// </summary>
        /// <param name="separator">separator string</param>
        /// <param name="sepListBuilder"><see cref="ValueListBuilder{T}"/> to store indexes</param>
        private void MakeSeparatorList(string separator, ref ValueListBuilder<int> sepListBuilder)
        {
            Debug.Assert(!IsNullOrEmpty(separator), "!string.IsNullOrEmpty(separator)");

            int currentSepLength = separator.Length;

            for (int i = 0; i < Length; i++)
            {
                if (this[i] == separator[0] && currentSepLength <= Length - i)
                {
                    if (currentSepLength == 1
                        || this.AsSpan(i, currentSepLength).SequenceEqual(separator))
                    {
                        sepListBuilder.Append(i);
                        i += currentSepLength - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Uses ValueListBuilder to create list that holds indexes of separators in string and list that holds length of separator strings.
        /// </summary>
        /// <param name="separators">separator strngs</param>
        /// <param name="sepListBuilder"><see cref="ValueListBuilder{T}"/> for separator indexes</param>
        /// <param name="lengthListBuilder"><see cref="ValueListBuilder{T}"/> for separator length values</param>
        private void MakeSeparatorList(string?[] separators, ref ValueListBuilder<int> sepListBuilder, ref ValueListBuilder<int> lengthListBuilder)
        {
            Debug.Assert(separators != null && separators.Length > 0, "separators != null && separators.Length > 0");

            for (int i = 0; i < Length; i++)
            {
                for (int j = 0; j < separators.Length; j++)
                {
                    string? separator = separators[j];
                    if (IsNullOrEmpty(separator))
                    {
                        continue;
                    }
                    int currentSepLength = separator.Length;
                    if (this[i] == separator[0] && currentSepLength <= Length - i)
                    {
                        if (currentSepLength == 1
                            || this.AsSpan(i, currentSepLength).SequenceEqual(separator))
                        {
                            sepListBuilder.Append(i);
                            lengthListBuilder.Append(currentSepLength);
                            i += currentSepLength - 1;
                            break;
                        }
                    }
                }
            }
        }

        private static void CheckStringSplitOptions(StringSplitOptions options)
        {
            const StringSplitOptions AllValidFlags = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

            if ((options & ~AllValidFlags) != 0)
            {
                // at least one invalid flag was set
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
            }
        }

        // Returns a substring of this string.
        //
        public string Substring(int startIndex) => Substring(startIndex, Length - startIndex);

        public string Substring(int startIndex, int length)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);
            }

            if (startIndex > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndexLargerThanLength);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NegativeLength);
            }

            if (startIndex > Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_IndexLength);
            }

            if (length == 0)
            {
                return string.Empty;
            }

            if (startIndex == 0 && length == this.Length)
            {
                return this;
            }

            return InternalSubString(startIndex, length);
        }

        private string InternalSubString(int startIndex, int length)
        {
            Debug.Assert(startIndex >= 0 && startIndex <= this.Length, "StartIndex is out of range!");
            Debug.Assert(length >= 0 && startIndex <= this.Length - length, "length is out of range!");

            string result = FastAllocateString(length);

            Buffer.Memmove(
                elementCount: (uint)result.Length, // derefing Length now allows JIT to prove 'result' not null below
                destination: ref result._firstChar,
                source: ref Unsafe.Add(ref _firstChar, (nint)(uint)startIndex /* force zero-extension */));

            return result;
        }

        // Creates a copy of this string in lower case.  The culture is set by culture.
        public string ToLower() => ToLower(null);

        // Creates a copy of this string in lower case.  The culture is set by culture.
        public string ToLower(CultureInfo? culture)
        {
            CultureInfo cult = culture ?? CultureInfo.CurrentCulture;
            return cult.TextInfo.ToLower(this);
        }

        // Creates a copy of this string in lower case based on invariant culture.
        public string ToLowerInvariant()
        {
            return TextInfo.Invariant.ToLower(this);
        }

        public string ToUpper() => ToUpper(null);

        // Creates a copy of this string in upper case.  The culture is set by culture.
        public string ToUpper(CultureInfo? culture)
        {
            CultureInfo cult = culture ?? CultureInfo.CurrentCulture;
            return cult.TextInfo.ToUpper(this);
        }

        // Creates a copy of this string in upper case based on invariant culture.
        public string ToUpperInvariant()
        {
            return TextInfo.Invariant.ToUpper(this);
        }

        // Trims the whitespace from both ends of the string.  Whitespace is defined by
        // char.IsWhiteSpace.
        //
        public string Trim() => TrimWhiteSpaceHelper(TrimType.Both);

        // Removes a set of characters from the beginning and end of this string.
        public unsafe string Trim(char trimChar) => TrimHelper(&trimChar, 1, TrimType.Both);

        // Removes a set of characters from the beginning and end of this string.
        public unsafe string Trim(params char[]? trimChars)
        {
            if (trimChars == null || trimChars.Length == 0)
            {
                return TrimWhiteSpaceHelper(TrimType.Both);
            }
            fixed (char* pTrimChars = &trimChars[0])
            {
                return TrimHelper(pTrimChars, trimChars.Length, TrimType.Both);
            }
        }

        // Removes a set of characters from the beginning of this string.
        public string TrimStart() => TrimWhiteSpaceHelper(TrimType.Head);

        // Removes a set of characters from the beginning of this string.
        public unsafe string TrimStart(char trimChar) => TrimHelper(&trimChar, 1, TrimType.Head);

        // Removes a set of characters from the beginning of this string.
        public unsafe string TrimStart(params char[]? trimChars)
        {
            if (trimChars == null || trimChars.Length == 0)
            {
                return TrimWhiteSpaceHelper(TrimType.Head);
            }
            fixed (char* pTrimChars = &trimChars[0])
            {
                return TrimHelper(pTrimChars, trimChars.Length, TrimType.Head);
            }
        }

        // Removes a set of characters from the end of this string.
        public string TrimEnd() => TrimWhiteSpaceHelper(TrimType.Tail);

        // Removes a set of characters from the end of this string.
        public unsafe string TrimEnd(char trimChar) => TrimHelper(&trimChar, 1, TrimType.Tail);

        // Removes a set of characters from the end of this string.
        public unsafe string TrimEnd(params char[]? trimChars)
        {
            if (trimChars == null || trimChars.Length == 0)
            {
                return TrimWhiteSpaceHelper(TrimType.Tail);
            }
            fixed (char* pTrimChars = &trimChars[0])
            {
                return TrimHelper(pTrimChars, trimChars.Length, TrimType.Tail);
            }
        }

        private string TrimWhiteSpaceHelper(TrimType trimType)
        {
            // end will point to the first non-trimmed character on the right.
            // start will point to the first non-trimmed character on the left.
            int end = Length - 1;
            int start = 0;

            // Trim specified characters.
            if ((trimType & TrimType.Head) != 0)
            {
                for (start = 0; start < Length; start++)
                {
                    if (!char.IsWhiteSpace(this[start]))
                    {
                        break;
                    }
                }
            }

            if ((trimType & TrimType.Tail) != 0)
            {
                for (end = Length - 1; end >= start; end--)
                {
                    if (!char.IsWhiteSpace(this[end]))
                    {
                        break;
                    }
                }
            }

            return CreateTrimmedString(start, end);
        }

        private unsafe string TrimHelper(char* trimChars, int trimCharsLength, TrimType trimType)
        {
            Debug.Assert(trimChars != null);
            Debug.Assert(trimCharsLength > 0);

            // end will point to the first non-trimmed character on the right.
            // start will point to the first non-trimmed character on the left.
            int end = Length - 1;
            int start = 0;

            // Trim specified characters.
            if ((trimType & TrimType.Head) != 0)
            {
                for (start = 0; start < Length; start++)
                {
                    int i = 0;
                    char ch = this[start];
                    for (i = 0; i < trimCharsLength; i++)
                    {
                        if (trimChars[i] == ch)
                        {
                            break;
                        }
                    }
                    if (i == trimCharsLength)
                    {
                        // The character is not in trimChars, so stop trimming.
                        break;
                    }
                }
            }

            if ((trimType & TrimType.Tail) != 0)
            {
                for (end = Length - 1; end >= start; end--)
                {
                    int i = 0;
                    char ch = this[end];
                    for (i = 0; i < trimCharsLength; i++)
                    {
                        if (trimChars[i] == ch)
                        {
                            break;
                        }
                    }
                    if (i == trimCharsLength)
                    {
                        // The character is not in trimChars, so stop trimming.
                        break;
                    }
                }
            }

            return CreateTrimmedString(start, end);
        }

        private string CreateTrimmedString(int start, int end)
        {
            int len = end - start + 1;
            return
                len == Length ? this :
                len == 0 ? string.Empty :
                InternalSubString(start, len);
        }
    }
}
