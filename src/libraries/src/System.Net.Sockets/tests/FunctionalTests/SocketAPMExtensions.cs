// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.Sockets.Tests
{
    internal static partial class SocketAsyncExtensions
    {
        public static void AcceptAPM(this Socket socket, Action<Socket> handler)
        {
            var callback = new AsyncCallback(asyncResult => handler(((Socket)asyncResult.AsyncState).EndAccept(asyncResult)));
            socket.BeginAccept(callback, socket);
        }

        public static void ConnectAPM(this Socket socket, EndPoint remoteEndpoint, Action handler)
        {
            var callback = new AsyncCallback(asyncResult =>
            {
                ((Socket)asyncResult.AsyncState).EndConnect(asyncResult);
                handler();
            });
            socket.BeginConnect(remoteEndpoint, callback, socket);
        }

        public static void ReceiveAPM(this Socket socket, byte[] buffer, int offset, int count, SocketFlags flags, Action<int> handler)
        {
            var callback = new AsyncCallback(asyncResult => handler(((Socket)asyncResult.AsyncState).EndReceive(asyncResult)));
            socket.BeginReceive(buffer, offset, count, flags, callback, socket);
        }

        public static void ReceiveAPM(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags flags, Action<int> handler)
        {
            var callback = new AsyncCallback(asyncResult => handler(((Socket)asyncResult.AsyncState).EndReceive(asyncResult)));
            socket.BeginReceive(buffers, flags, callback, socket);
        }

        public static void ReceiveFromAPM(this Socket socket, byte[] buffer, int offset, int count, SocketFlags flags, EndPoint remoteEndpoint, Action<int, EndPoint> handler)
        {
            var callback = new AsyncCallback(asyncResult =>
            {
                int received = ((Socket)asyncResult.AsyncState).EndReceiveFrom(asyncResult, ref remoteEndpoint);
                handler(received, remoteEndpoint);
            });
            socket.BeginReceiveFrom(buffer, offset, count, flags, ref remoteEndpoint, callback, socket);
        }

        public static void SendAPM(this Socket socket, byte[] buffer, int offset, int count, SocketFlags flags, Action<int> handler)
        {
            var callback = new AsyncCallback(asyncResult => handler(((Socket)asyncResult.AsyncState).EndSend(asyncResult)));
            socket.BeginSend(buffer, offset, count, flags, callback, socket);
        }

        public static void SendAPM(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags flags, Action<int> handler)
        {
            var callback = new AsyncCallback(asyncResult => handler(((Socket)asyncResult.AsyncState).EndSend(asyncResult)));
            socket.BeginSend(buffers, flags, callback, socket);
        }

        public static void SendFileAPM(this Socket socket, string filename, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, Action handler)
        {
            var callback = new AsyncCallback(asyncResult =>
            {
                ((Socket)asyncResult.AsyncState).EndSendFile(asyncResult);
                handler();
            });
            socket.BeginSendFile(filename, preBuffer, postBuffer, flags, callback, socket);
        }

        public static void SendToAPM(this Socket socket, byte[] buffer, int offset, int count, SocketFlags flags, EndPoint remoteEndpoint, Action<int> handler)
        {
            var callback = new AsyncCallback(asyncResult => handler(((Socket)asyncResult.AsyncState).EndSendTo(asyncResult)));
            socket.BeginSendTo(buffer, offset, count, flags, remoteEndpoint, callback, socket);
        }
    }
}
