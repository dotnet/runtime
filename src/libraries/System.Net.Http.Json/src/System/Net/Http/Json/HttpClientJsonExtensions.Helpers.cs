// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        #region Post/Put helpers
        private static JsonContent CreateJsonContent(Type type, object? value, JsonSerializerOptions? options)
        {
            return new JsonContent(type, value, options);
        }

        private static JsonContent CreateJsonContent<T>(T value, JsonSerializerOptions? options)
        {
            return JsonContent.Create<T>(value, options);
        }
        #endregion
    }
}
