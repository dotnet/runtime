// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Avoid loading System.Private.Uri.dll until necessary.
    /// </summary>
    internal sealed class UriConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type type)
        {
            return (type.ToString() == "System.Uri" && IsSystemUriType(type));
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            return new UriConverter();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsSystemUriType(Type type)
        {
            // Verify the Type is what we expect.
            return (type == typeof(Uri));
        }
    }
}
