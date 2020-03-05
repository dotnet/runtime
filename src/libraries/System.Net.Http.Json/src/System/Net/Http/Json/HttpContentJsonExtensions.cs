using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static class HttpContentJsonExtensions
    {
        //public static Task<object> ReadFromJsonAsync(
        //    this HttpContent content,
        //    Type type,
        //    JsonSerializerOptions? options = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    //Stream jsonStream = content.;
        //    object retValue = JsonSerializer.DeserializeAsync(content.ReadAsStreamAsync(), type, options, cancellationToken);
        //}

        //public static Task<T> ReadFromJsonAsync<T>(
        //    this HttpContent content,
        //    JsonSerializerOptions? options = null,
        //    CancellationToken cancellationToken = default)
        //{

        //}

        //private static async Task<T> ReadCore<T>(HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken)
        //{
        //    Stream utf8Stream = content.
        //}
    }
}
