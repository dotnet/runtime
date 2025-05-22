// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class JsonDocument
    {
        private struct PropertyNameSet
        {
            // Data structure to track property names in an object while deserializing
            // into a JsonDocument and validate that there are no duplicates. Note that
            // when there are only a few properties and no escaped property names, the
            // cache is empty and duplicates are checked against the database.

            private const int CacheThreshold = 4;

            // This is lazily initialized when there's an escaped property name
            // or the number of properties exceeds a threshold.
            private List<ReadOnlyMemory<byte>>? _cache;

            // Call this *before* adding the property. The metadata database should not
            // contain the property and the next append, if this method succeeds,
            // should be the property name.
            internal void AddPropertyName(
                ReadOnlyMemory<byte> utf8Json,
                ref readonly Utf8JsonReader reader,
                ref readonly MetadataDb database,
                int currentNumberOfProperties)
            {
                Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
                Debug.Assert(!reader.HasValueSequence);

                // Name without quotes
                ReadOnlyMemory<byte> propertyName;

                if (reader.ValueIsEscaped)
                {
                    propertyName = JsonReaderHelper.GetUnescaped(reader.ValueSpan);
                }
                else
                {
                    // The backing buffer is a ReadOnlyMemory<byte> so it must be int indexable
                    int startPosition = checked((int)reader.TokenStartIndex);

                    // Remove the quotes
                    propertyName = utf8Json.Slice(startPosition + 1, reader.ValueSpan.Length);
                }

                if (_cache != null)
                {
                    foreach (ReadOnlyMemory<byte> previousPropertyName in _cache)
                    {
                        if (previousPropertyName.Span.SequenceEqual(propertyName.Span))
                        {
                            ThrowHelper.ThrowJsonReaderException(in reader, ExceptionResource.DuplicatePropertiesNotAllowed);
                        }
                    }

                    _cache.Add(propertyName);
                }
                else
                {
                    // TODO determine a threshold on number of properties (and update test)
                    bool shouldCreateSet = reader.ValueIsEscaped || currentNumberOfProperties >= CacheThreshold;

                    if (shouldCreateSet)
                    {
                        // The internal default List<T> capacity is 4, but if we have more, we can pre-allocate
                        _cache = new List<ReadOnlyMemory<byte>>(Math.Max(4, currentNumberOfProperties + 1));
                    }

                    foreach (int previousPropertyNameIdx in database.EnumeratePreviousProperties())
                    {
                        DbRow row = database.Get(previousPropertyNameIdx);

                        // Note that the previous property name is not escaped.
                        // If it was, then we would have previously created
                        // the cache for it and could not be in this branch.
                        Debug.Assert(row.TokenType is JsonTokenType.PropertyName);
                        Debug.Assert(!row.HasComplexChildren);

                        ReadOnlyMemory<byte> previousPropertyName =
                            utf8Json.Slice(row.Location, row.SizeOrLength);

                        if (previousPropertyName.Span.SequenceEqual(propertyName.Span))
                        {
                            ThrowHelper.ThrowJsonReaderException(in reader, ExceptionResource.DuplicatePropertiesNotAllowed);
                        }

                        if (shouldCreateSet)
                        {
                            Debug.Assert(_cache is not null);
                            _cache.Add(previousPropertyName);
                        }
                    }

                    if (shouldCreateSet)
                    {
                        Debug.Assert(_cache is not null);
                        _cache!.Add(propertyName);
                    }
                }
            }
        }
    }
}
