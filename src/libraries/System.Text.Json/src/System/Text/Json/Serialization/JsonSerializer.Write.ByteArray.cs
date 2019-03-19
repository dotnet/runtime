// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        // Name \ feature is pending closure on API review
        public static byte[] ToBytes<TValue>(TValue value, JsonSerializerOptions options = null)
        {
            return WriteCoreBytes(value, typeof(TValue), options);
        }

        // Name \ feature is pending closure on API review
        public static byte[] ToBytes(object value, Type type, JsonSerializerOptions options = null)
        {
            VerifyValueAndType(value, type);
            return WriteCoreBytes(value, type, options);
        }
    }
}
