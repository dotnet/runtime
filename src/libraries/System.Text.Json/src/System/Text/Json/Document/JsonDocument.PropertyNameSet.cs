// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public sealed partial class JsonDocument
    {
        // Ensure this stays on the stack by making it a ref struct.
        private ref struct PropertyNameSet : IDisposable
        {
            // Data structure to track property names in an object while deserializing
            // into a JsonDocument and validate that there are no duplicates. A small inline
            // array is used for the first few property names and then a pooled array is used
            // for the rest. Escaped property names are always stored in the pooled array.

            private ReadOnlyMemory<byte>[]? _heapCache;
            private int _heapCacheCount = 0;

#if NET
            private const int StackCacheThreshold = 16;

            [InlineArray(StackCacheThreshold)]
            private struct InlineRangeArray16
            {
                private Range _element0;
            }

            private InlineRangeArray16 _stackCache;
            private int _stackCacheCount = 0;
#endif

            public PropertyNameSet()
            {
            }

            internal void AddPropertyName(JsonProperty property, JsonDocument document)
            {
                DbRow dbRow = document._parsedData.Get(property.Value.MetadataDbIndex - DbRow.Size);
                Debug.Assert(dbRow.TokenType is JsonTokenType.PropertyName);

                // Name without quotes
                ReadOnlyMemory<byte> utf8Json = document._utf8Json;
                ReadOnlyMemory<byte> propertyName = utf8Json.Slice(dbRow.Location, dbRow.SizeOrLength);

                if (dbRow.HasComplexChildren)
                {
                    propertyName = JsonReaderHelper.GetUnescaped(propertyName.Span);
                }

#if NET
                for (int i = 0; i < _stackCacheCount; i++)
                {
                    ReadOnlySpan<byte> previousPropertyName = utf8Json[_stackCache[i]].Span;

                    if (previousPropertyName.SequenceEqual(propertyName.Span))
                    {
                        ThrowHelper.ThrowJsonException_DuplicatePropertyNotAllowed(propertyName.Span);
                    }
                }
#endif

                // Positive heap cache count implies it is not null.
                Debug.Assert(_heapCacheCount is 0 || _heapCache is not null);

                for (int i = 0; i < _heapCacheCount; i++)
                {
                    ReadOnlySpan<byte> previousPropertyName = _heapCache![i].Span;

                    if (previousPropertyName.SequenceEqual(propertyName.Span))
                    {
                        ThrowHelper.ThrowJsonException_DuplicatePropertyNotAllowed(propertyName.Span);
                    }
                }

#if NET
                // Property name is not a duplicate, so add it to the cache.
                // Use the stack cache if there's space and property name is not escaped
                if (!dbRow.HasComplexChildren && _stackCacheCount < StackCacheThreshold)
                {
                    _stackCache[_stackCacheCount] = dbRow.Location..(dbRow.Location + dbRow.SizeOrLength);
                    _stackCacheCount++;
                    return;
                }
#endif

                EnsureHeapCacheCapacity();

                // Add the property name to the heap cache.
                _heapCache![_heapCacheCount] = propertyName;
                _heapCacheCount++;
            }

            private void EnsureHeapCacheCapacity()
            {
                if (_heapCache is not null && _heapCacheCount < _heapCache.Length)
                {
                    return;
                }

                int newSize = _heapCache is null ? 4 : checked(_heapCache.Length * 2);
                ReadOnlyMemory<byte>[] newArray = ArrayPool<ReadOnlyMemory<byte>>.Shared.Rent(newSize);

                // Copy and clear the old array if it exists.
                if (_heapCache is not null)
                {
                    Span<ReadOnlyMemory<byte>> oldArraySpan = _heapCache.AsSpan();
                    oldArraySpan.CopyTo(newArray);
                    oldArraySpan.Clear();
                    ArrayPool<ReadOnlyMemory<byte>>.Shared.Return(_heapCache!);
                }

                _heapCache = newArray;
            }

            internal void Reset()
            {
                // Clear the ReadOnlyMemory<byte> contents so they don't root any data.
                if (_heapCache is not null)
                {
                    _heapCache.AsSpan(0, _heapCacheCount).Clear();
                    _heapCacheCount = 0;
                }

#if NET
                _stackCacheCount = 0;
#endif
            }

            public void Dispose()
            {
                Reset();

                // Dispose of the heap cache if it was allocated.
                if (_heapCache is not null)
                {
                    ArrayPool<ReadOnlyMemory<byte>>.Shared.Return(_heapCache);
                }
            }
        }
    }
}
