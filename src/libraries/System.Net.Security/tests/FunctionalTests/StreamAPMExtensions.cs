// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

/// <summary>
/// Extensions that add the legacy APM Pattern (Begin/End) for generic Streams
/// </summary>


namespace System.IO
{
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
