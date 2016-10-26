// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    internal class TransmitFileAsyncResult : BaseOverlappedAsyncResult
    {
        private FileStream _fileStream;
        private bool _doDisconnect;
        private Interop.Mswsock.TransmitFileBuffers _transmitFileBuffers;

        internal TransmitFileAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback) :
            base(socket, asyncState, asyncCallback)
        {
        }

        internal void SetUnmanagedStructures(FileStream fileStream, byte[] preBuffer, byte[] postBuffer, bool doDisconnect)
        {
            _fileStream = fileStream;
            _doDisconnect = doDisconnect;

            // Construct TransmitFileBuffers, if necessary
            _transmitFileBuffers = null;
            int buffsNumber = 0;

            if (preBuffer != null && preBuffer.Length > 0)
                ++buffsNumber;

            if (postBuffer != null && postBuffer.Length > 0)
                ++buffsNumber;

            object[] objectsToPin = null;
            if (buffsNumber != 0)
            {
                ++buffsNumber;
                objectsToPin = new object[buffsNumber];

                _transmitFileBuffers = new Interop.Mswsock.TransmitFileBuffers();

                objectsToPin[--buffsNumber] = _transmitFileBuffers;

                if (preBuffer != null && preBuffer.Length > 0)
                {
                    _transmitFileBuffers.HeadLength = preBuffer.Length;
                    objectsToPin[--buffsNumber] = preBuffer;
                }

                if (postBuffer != null && postBuffer.Length > 0)
                {
                    _transmitFileBuffers.TailLength = postBuffer.Length;
                    objectsToPin[--buffsNumber] = postBuffer;
                }

                base.SetUnmanagedStructures(objectsToPin);

                if (preBuffer != null && preBuffer.Length > 0)
                {
                    _transmitFileBuffers.Head = Marshal.UnsafeAddrOfPinnedArrayElement(preBuffer, 0);
                }

                if (postBuffer != null && postBuffer.Length > 0)
                {
                    _transmitFileBuffers.Tail = Marshal.UnsafeAddrOfPinnedArrayElement(postBuffer, 0);
                }
            }
            else
            {
                base.SetUnmanagedStructures(null);
            }
        }

        protected override void ForceReleaseUnmanagedStructures()
        {
            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }

            base.ForceReleaseUnmanagedStructures();
        }

        internal Interop.Mswsock.TransmitFileBuffers TransmitFileBuffers => _transmitFileBuffers;

        internal bool DoDisconnect => _doDisconnect;
    } 
}
