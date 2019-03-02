// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        public static string ToString<TValue>(TValue value, JsonSerializerOptions options = null)
        {
            return ToStringInternal(value, typeof(TValue), options);
        }

        public static string ToString(object value, Type type, JsonSerializerOptions options = null)
        {
            VerifyValueAndType(value, type);

            return ToStringInternal(value, type, options);
        }

        private static string ToStringInternal(object value, Type type, JsonSerializerOptions options)
        {
            return WriteCoreString(value, type, options);
        }
    }
}
