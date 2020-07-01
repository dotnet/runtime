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
        private const System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes MembersAccessedOnRead = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties;
        private const System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes MembersAccessedOnWrite = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties;
        public static object? Deserialize(System.ReadOnlySpan<byte> utf8Json, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(string json, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(ref System.Text.Json.Utf8JsonReader reader, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Threading.Tasks.ValueTask<object?> DeserializeAsync(System.IO.Stream utf8Json, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<TValue> DeserializeAsync<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        public static TValue Deserialize<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] TValue>(System.ReadOnlySpan<byte> utf8Json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        public static TValue Deserialize<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] TValue>(string json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        public static TValue Deserialize<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnRead)] TValue>(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static string Serialize(object? value, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static void Serialize(System.Text.Json.Utf8JsonWriter writer, object? value, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, object? value, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SerializeAsync<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static byte[] SerializeToUtf8Bytes(object? value, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static byte[] SerializeToUtf8Bytes<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static void Serialize<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] TValue>(System.Text.Json.Utf8JsonWriter writer, TValue value, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static string Serialize<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(MembersAccessedOnWrite)] TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
    }
}
