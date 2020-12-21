// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.WebSockets.Client.Tests
{
    public static class WebSocketData
    {
        public static ArraySegment<byte> GetBufferFromText(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            return new ArraySegment<byte>(buffer);
        }

        public static string GetTextFromBuffer(ArraySegment<byte> buffer)
        {
            return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
