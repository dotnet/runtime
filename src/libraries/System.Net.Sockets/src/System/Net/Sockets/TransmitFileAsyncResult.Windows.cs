// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Net.Sockets
{
    internal sealed class TransmitFileAsyncResult : BaseOverlappedAsyncResult
    {
        private FileStream _fileStream;
        private bool _doDisconnect;

        internal TransmitFileAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback) :
            base(socket, asyncState, asyncCallback)
        {
        }

        internal void SetUnmanagedStructures(FileStream fileStream, byte[] preBuffer, byte[] postBuffer, bool doDisconnect)
        {
            _fileStream = fileStream;
            _doDisconnect = doDisconnect;

            int buffsNumber = 0;

            if (preBuffer != null && preBuffer.Length > 0)
                ++buffsNumber;

            if (postBuffer != null && postBuffer.Length > 0)
                ++buffsNumber;

            object[] objectsToPin = null;
            if (buffsNumber != 0)
            {
                objectsToPin = new object[buffsNumber];

                if (preBuffer != null && preBuffer.Length > 0)
                {
                    objectsToPin[--buffsNumber] = preBuffer;
                }

                if (postBuffer != null && postBuffer.Length > 0)
                {
                    objectsToPin[--buffsNumber] = postBuffer;
                }
            }

            base.SetUnmanagedStructures(objectsToPin);
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

        internal bool DoDisconnect => _doDisconnect;
    } 
}
