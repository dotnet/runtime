// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public sealed partial class JsonDocument : IDisposable
    {
        private ReadOnlyMemory<byte> _utf8Json;
        private MetadataDb _parsedData;
        private byte[] _extraRentedBytes;
        private (int, string) _lastIndexAndString = (-1, null);

        public JsonElement RootElement => new JsonElement(this, 0);

        private JsonDocument(ReadOnlyMemory<byte> utf8Json, MetadataDb parsedData, byte[] extraRentedBytes)
        {
            Debug.Assert(!utf8Json.IsEmpty);

            _utf8Json = utf8Json;
            _parsedData = parsedData;
            _extraRentedBytes = extraRentedBytes;
        }

        public void Dispose()
        {
            if (_utf8Json.IsEmpty)
            {
                return;
            }

            int length = _utf8Json.Length;
            _utf8Json = ReadOnlyMemory<byte>.Empty;
            _parsedData.Dispose();

            // When "extra rented bytes exist" they contain the document,
            // and thus need to be cleared before being returned.
            if (_extraRentedBytes != null)
            {
                _extraRentedBytes.AsSpan(0, length).Clear();
                ArrayPool<byte>.Shared.Return(_extraRentedBytes);
                _extraRentedBytes = null;
            }
        }

        internal JsonTokenType GetJsonTokenType(int index)
        {
            CheckNotDisposed();

            return _parsedData.GetJsonTokenType(index);
        }

        internal int GetArrayLength(int index)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.StartArray, row.TokenType);

            return row.SizeOrLength;
        }

        internal JsonElement GetArrayIndexElement(int currentIndex, int arrayIndex)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(currentIndex);

            CheckExpectedType(JsonTokenType.StartArray, row.TokenType);

            int arrayLength = row.SizeOrLength;

            if ((uint)arrayIndex >= (uint)arrayLength)
            {
                throw new IndexOutOfRangeException();
            }

            if (!row.HasComplexChildren)
            {
                // Since we wouldn't be here without having completed the document parse, and we
                // already vetted the index against the length, this new index will always be
                // within the table.
                return new JsonElement(this, currentIndex + ((arrayIndex + 1) * DbRow.Size));
            }

            int elementCount = 0;
            int objectOffset = currentIndex + DbRow.Size;

            for (; objectOffset < _parsedData.Length; objectOffset += DbRow.Size)
            {
                if (arrayIndex == elementCount)
                {
                    return new JsonElement(this, objectOffset);
                }

                row = _parsedData.Get(objectOffset);

                if (!row.IsSimpleValue)
                {
                    objectOffset += DbRow.Size * row.NumberOfRows;
                }

                elementCount++;
            }

            Debug.Fail(
                $"Ran out of database searching for array index {arrayIndex} from {currentIndex} when length was {arrayLength}");
            throw new IndexOutOfRangeException();
        }

        internal int GetEndIndex(int index, bool includeEndElement)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            if (row.IsSimpleValue)
            {
                return index + DbRow.Size;
            }

            int endIndex = index + DbRow.Size * row.NumberOfRows;

            if (includeEndElement)
            {
                endIndex += DbRow.Size;
            }

            return endIndex;
        }

        private ReadOnlyMemory<byte> GetRawValue(int index, bool includeQuotes)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            if (row.IsSimpleValue)
            {
                if (includeQuotes && row.TokenType == JsonTokenType.String)
                {
                    // Start one character earlier than the value (the open quote)
                    // End one character after the value (the close quote)
                    return _utf8Json.Slice(row.Location - 1, row.SizeOrLength + 2);
                }

                return _utf8Json.Slice(row.Location, row.SizeOrLength);
            }

            int endElementIdx = GetEndIndex(index, includeEndElement: false);
            int start = row.Location;
            row = _parsedData.Get(endElementIdx);
            return _utf8Json.Slice(start, row.Location - start + row.SizeOrLength);
        }

        private ReadOnlyMemory<byte> GetPropertyRawValue(int valueIndex)
        {
            CheckNotDisposed();

            // The property name is stored one row before the value
            DbRow row = _parsedData.Get(valueIndex - DbRow.Size);
            Debug.Assert(row.TokenType == JsonTokenType.PropertyName);

            // Subtract one for the open quote.
            int start = row.Location - 1;
            int end;

            row = _parsedData.Get(valueIndex);

            if (row.IsSimpleValue)
            {
                end = row.Location + row.SizeOrLength;

                // If the value was a string, pick up the terminating quote.
                if (row.TokenType == JsonTokenType.String)
                {
                    end++;
                }

                return _utf8Json.Slice(start, end - start);
            }

            int endElementIdx = GetEndIndex(valueIndex, includeEndElement: false);
            row = _parsedData.Get(endElementIdx);
            end = row.Location + row.SizeOrLength;
            return _utf8Json.Slice(start, end - start);
        }

        internal string GetString(int index, JsonTokenType expectedType)
        {
            CheckNotDisposed();

            (int lastIdx, string lastString) = _lastIndexAndString;

            if (lastIdx == index)
            {
                return lastString;
            }

            DbRow row = _parsedData.Get(index);

            JsonTokenType tokenType = row.TokenType;

            if (tokenType == JsonTokenType.Null)
            {
                return null;
            }

            CheckExpectedType(expectedType, tokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            if (row.HasComplexChildren)
            {
                int backslash = segment.IndexOf(JsonConstants.BackSlash);
                lastString = JsonReaderHelper.GetUnescapedString(segment, backslash);
            }
            else
            {
                lastString = JsonReaderHelper.TranscodeHelper(segment);
            }

            _lastIndexAndString = (index, lastString);
            return lastString;
        }

        internal string GetNameOfPropertyValue(int index)
        {
            // The property name is one row before the property value
            return GetString(index - DbRow.Size, JsonTokenType.PropertyName);
        }

        internal bool TryGetValue(int index, out int value)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.Number, row.TokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            if (Utf8Parser.TryParse(segment, out int tmp, out int consumed) &&
                consumed == segment.Length)
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        internal bool TryGetValue(int index, out uint value)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.Number, row.TokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            if (Utf8Parser.TryParse(segment, out uint tmp, out int consumed) &&
                consumed == segment.Length)
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        internal bool TryGetValue(int index, out long value)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.Number, row.TokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            if (Utf8Parser.TryParse(segment, out long tmp, out int consumed) &&
                consumed == segment.Length)
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        internal bool TryGetValue(int index, out ulong value)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.Number, row.TokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            if (Utf8Parser.TryParse(segment, out ulong tmp, out int consumed) &&
                consumed == segment.Length)
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        internal bool TryGetValue(int index, out double value)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.Number, row.TokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            char standardFormat = row.HasComplexChildren ? JsonConstants.ScientificNotationFormat : default;

            if (Utf8Parser.TryParse(segment, out double tmp, out int bytesConsumed, standardFormat) &&
                segment.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        internal bool TryGetValue(int index, out float value)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.Number, row.TokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            char standardFormat = row.HasComplexChildren ? JsonConstants.ScientificNotationFormat : default;

            if (Utf8Parser.TryParse(segment, out float tmp, out int bytesConsumed, standardFormat) &&
                segment.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        internal bool TryGetValue(int index, out decimal value)
        {
            CheckNotDisposed();

            DbRow row = _parsedData.Get(index);

            CheckExpectedType(JsonTokenType.Number, row.TokenType);

            ReadOnlySpan<byte> data = _utf8Json.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location, row.SizeOrLength);

            char standardFormat = row.HasComplexChildren ? JsonConstants.ScientificNotationFormat : default;

            if (Utf8Parser.TryParse(segment, out decimal tmp, out int bytesConsumed, standardFormat) &&
                segment.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        internal string GetRawValueAsString(int index)
        {
            ReadOnlyMemory<byte> segment = GetRawValue(index, includeQuotes: true);
            return JsonReaderHelper.TranscodeHelper(segment.Span);
        }

        internal string GetPropertyRawValueAsString(int valueIndex)
        {
            ReadOnlyMemory<byte> segment = GetPropertyRawValue(valueIndex);
            return JsonReaderHelper.TranscodeHelper(segment.Span);
        }

        private static void Parse(
            ReadOnlySpan<byte> utf8JsonSpan,
            Utf8JsonReader reader,
            ref MetadataDb database,
            ref StackRowStack stack)
        {
            bool inArray = false;
            int arrayItemsCount = 0;
            int numberOfRowsForMembers = 0;
            int numberOfRowsForValues = 0;

            ref byte jsonStart = ref MemoryMarshal.GetReference(utf8JsonSpan);

            while (reader.Read())
            {
                JsonTokenType tokenType = reader.TokenType;

                int tokenStart = Unsafe.ByteOffset(
                    ref jsonStart,
                    ref MemoryMarshal.GetReference(reader.ValueSpan)).ToInt32();

                if (tokenType == JsonTokenType.StartObject)
                {
                    if (inArray)
                    {
                        arrayItemsCount++;
                    }

                    numberOfRowsForValues++;
                    database.Append(tokenType, tokenStart, DbRow.UnknownSize);
                    var row = new StackRow(numberOfRowsForMembers + 1);
                    stack.Push(row);
                    numberOfRowsForMembers = 0;
                }
                else if (tokenType == JsonTokenType.EndObject)
                {
                    int rowIndex = database.FindIndexOfFirstUnsetSizeOrLength(JsonTokenType.StartObject);

                    numberOfRowsForValues++;
                    numberOfRowsForMembers++;
                    database.SetLength(rowIndex, numberOfRowsForMembers);

                    int newRowIndex = database.Length;
                    database.Append(tokenType, tokenStart, reader.ValueSpan.Length);
                    database.SetNumberOfRows(rowIndex, numberOfRowsForMembers);
                    database.SetNumberOfRows(newRowIndex, numberOfRowsForMembers);

                    StackRow row = stack.Pop();
                    numberOfRowsForMembers += row.SizeOrLength;
                }
                else if (tokenType == JsonTokenType.StartArray)
                {
                    if (inArray)
                    {
                        arrayItemsCount++;
                    }

                    numberOfRowsForMembers++;
                    database.Append(tokenType, tokenStart, DbRow.UnknownSize);
                    var row = new StackRow(arrayItemsCount, numberOfRowsForValues + 1);
                    stack.Push(row);
                    arrayItemsCount = 0;
                    numberOfRowsForValues = 0;
                }
                else if (tokenType == JsonTokenType.EndArray)
                {
                    int rowIndex = database.FindIndexOfFirstUnsetSizeOrLength(JsonTokenType.StartArray);

                    numberOfRowsForValues++;
                    numberOfRowsForMembers++;
                    database.SetLength(rowIndex, arrayItemsCount);
                    database.SetNumberOfRows(rowIndex, numberOfRowsForValues);

                    // If the array item count is (e.g.) 12 and the number of rows is (e.g.) 13
                    // then the extra row is just this EndArray item, so the array was made up
                    // of simple values.
                    //
                    // If the off-by-one relationship does not hold, then one of the values was
                    // more than one row, making it a complex object.
                    //
                    // This check is similar to tracking the start array and painting it when
                    // StartObject or StartArray is encountered, but avoids the mixed state
                    // where "UnknownSize" implies "has complex children".
                    if (arrayItemsCount + 1 != numberOfRowsForValues)
                    {
                        database.SetHasComplexChildren(rowIndex);
                    }

                    int newRowIndex = database.Length;
                    database.Append(tokenType, tokenStart, reader.ValueSpan.Length);
                    database.SetNumberOfRows(newRowIndex, numberOfRowsForValues);

                    StackRow row = stack.Pop();
                    arrayItemsCount = row.SizeOrLength;
                    numberOfRowsForValues += row.NumberOfRows;
                }
                else if (tokenType == JsonTokenType.PropertyName)
                {
                    numberOfRowsForValues++;
                    numberOfRowsForMembers++;
                    database.Append(tokenType, tokenStart, reader.ValueSpan.Length);

                    if (reader._stringHasEscaping)
                    {
                        database.SetHasComplexChildren(database.Length - DbRow.Size);
                    }

                    Debug.Assert(!inArray);
                }
                else
                {
                    Debug.Assert(tokenType >= JsonTokenType.String && tokenType <= JsonTokenType.Null);
                    numberOfRowsForValues++;
                    numberOfRowsForMembers++;
                    database.Append(tokenType, tokenStart, reader.ValueSpan.Length);

                    if (inArray)
                    {
                        arrayItemsCount++;
                    }

                    if (tokenType == JsonTokenType.Number)
                    {
                        switch (reader._numberFormat)
                        {
                            case JsonConstants.ScientificNotationFormat:
                                database.SetHasComplexChildren(database.Length - DbRow.Size);
                                break;
                            default:
                                Debug.Assert(
                                    reader._numberFormat == default,
                                    $"Unhandled numeric format {reader._numberFormat}");
                                break;
                        }
                    }
                    else if (tokenType == JsonTokenType.String)
                    {
                        if (reader._stringHasEscaping)
                        {
                            database.SetHasComplexChildren(database.Length - DbRow.Size);
                        }
                    }
                }

                inArray = reader.IsInArray;
            }

            Debug.Assert(reader.BytesConsumed == utf8JsonSpan.Length);
            database.TrimExcess();
        }

        private void CheckNotDisposed()
        {
            if (_utf8Json.IsEmpty)
            {
                throw new ObjectDisposedException(nameof(JsonDocument));
            }
        }

        private void CheckExpectedType(JsonTokenType expected, JsonTokenType actual)
        {
            if (expected != actual)
            {
                throw ThrowHelper.GetJsonElementWrongTypeException(expected, actual);
            }
        }

        private static void CheckSupportedOptions(JsonReaderOptions readerOptions)
        {
            if (readerOptions.CommentHandling == JsonCommentHandling.Allow)
            {
                throw new ArgumentException(
                    SR.JsonDocumentDoesNotSupportComments,
                    nameof(readerOptions));
            }
        }
    }
}
