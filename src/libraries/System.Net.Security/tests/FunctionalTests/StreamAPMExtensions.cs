// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// Extensions that add the legacy APM Pattern (Begin/End) for generic Streams
    /// </summary>
    public static class StreamAPMExtensions
    {
        public static IAsyncResult BeginRead(this Stream s, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return s.ReadAsync(buffer, offset, count).ToApm<int>(callback, state);
        }

        public static IAsyncResult BeginWrite(this Stream s, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return s.WriteAsync(buffer, offset, count).ToApm(callback, state);
        }

        public static int EndRead(this Stream s, IAsyncResult asyncResult)
        {
            var t = (Task<int>)asyncResult;
            return t.GetAwaiter().GetResult();
        }

        public static void EndWrite(this Stream s, IAsyncResult asyncResult)
        {
            Task t = (Task)asyncResult;
            t.GetAwaiter().GetResult();
        }
    }
}
