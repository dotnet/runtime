// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Convert the provided value into a <see cref="System.Byte"/> array.
        /// </summary>
        /// <returns>A UTF-8 representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the convertion behavior.</param>
        public static byte[] ToBytes<TValue>(TValue value, JsonSerializerOptions options = null)
        {
            return WriteCoreBytes(value, typeof(TValue), options);
        }

        /// <summary>
        /// Convert the provided value into a <see cref="System.Byte"/> array.
        /// </summary>
        /// <returns>A UTF-8 representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="type">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the convertion behavior.</param>
        public static byte[] ToBytes(object value, Type type, JsonSerializerOptions options = null)
        {
            VerifyValueAndType(value, type);
            return WriteCoreBytes(value, type, options);
        }
    }
}
