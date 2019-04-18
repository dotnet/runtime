// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Policies
{
    internal abstract class JsonValueConverter<TValue>
    {
        public abstract bool TryRead(Type valueType, ref Utf8JsonReader reader, out TValue value);
        public abstract void Write(TValue value, Utf8JsonWriter writer);
        public abstract void Write(Span<byte> escapedPropertyName, TValue value, Utf8JsonWriter writer);
    }
}
