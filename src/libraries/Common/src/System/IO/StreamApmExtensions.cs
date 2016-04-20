// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides Stream.Begin/EndRead/Write wrappers for Stream.Read/WriteAsync.</summary>
    internal static class StreamApmExtensions
    {
        public static IAsyncResult BeginRead(this Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(stream.ReadAsync(buffer, offset, count), callback, state);

        public static int EndRead(this Stream stream, IAsyncResult asyncResult) => 
            TaskToApm.End<int>(asyncResult);

        public static IAsyncResult BeginWrite(this Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(stream.WriteAsync(buffer, offset, count), callback, state);

        public static void EndWrite(this Stream stream, IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);
    }
}
