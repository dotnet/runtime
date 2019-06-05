// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static object ReadCore(
            Type returnType,
            JsonSerializerOptions options,
            ref Utf8JsonReader reader)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            ReadStack state = default;
            state.Current.Initialize(returnType, options);

            ReadCore(options, ref reader, ref state);

            return state.Current.ReturnValue;
        }
    }
}
