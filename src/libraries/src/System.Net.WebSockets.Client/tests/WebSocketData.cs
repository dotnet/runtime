// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
