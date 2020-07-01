// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        public static object? Deserialize(System.ReadOnlySpan<byte> utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(string json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(ref System.Text.Json.Utf8JsonReader reader, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Threading.Tasks.ValueTask<object?> DeserializeAsync(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<TValue> DeserializeAsync<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        public static TValue Deserialize<TValue>(System.ReadOnlySpan<byte> utf8Json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        public static TValue Deserialize<TValue>(string json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        public static TValue Deserialize<TValue>(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static string Serialize(object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static void Serialize(System.Text.Json.Utf8JsonWriter writer, object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static byte[] SerializeToUtf8Bytes(object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static void Serialize<TValue>(System.Text.Json.Utf8JsonWriter writer, TValue value, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static string Serialize<TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
    }
}
