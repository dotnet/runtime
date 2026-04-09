// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace System.Speech.Internal.SapiInterop
{
    internal class SpStreamWrapper : IStream, IDisposable
    {
        #region Constructors

        internal SpStreamWrapper(Stream stream)
        {
            _stream = stream;
            _endOfStreamPosition = stream.Length;
        }

        public void Dispose()
        {
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region public Methods

        #region ISpStreamFormat interface implementation

        public void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            if (_endOfStreamPosition >= 0 && _stream.Position + cb > _endOfStreamPosition)
            {
                cb = (int)(_endOfStreamPosition - _stream.Position);
            }

            int read = 0;
            try
            {
                read = _stream.Read(pv, 0, cb);
            }
            catch (EndOfStreamException)
            {
                read = 0;
            }

            if (pcbRead != IntPtr.Zero)
            {
                Marshal.WriteIntPtr(pcbRead, new IntPtr(read));
            }
        }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            throw new NotSupportedException();
        }

        public void Seek(long offset, int seekOrigin, IntPtr plibNewPosition)
        {
            _stream.Seek(offset, (SeekOrigin)seekOrigin);

            if (plibNewPosition != IntPtr.Zero)
            {
                Marshal.WriteIntPtr(plibNewPosition, new IntPtr(_stream.Position));
            }
        }
        public void SetSize(long libNewSize)
        {
            throw new NotSupportedException();
        }
        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            throw new NotSupportedException();
        }
        public void Commit(int grfCommitFlags)
        {
            _stream.Flush();
        }
        public void Revert()
        {
            throw new NotSupportedException();
        }
        public void LockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotSupportedException();
        }
        public void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotSupportedException();
        }
        public void Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG
            {
                cbSize = _stream.Length
            };
        }

        public void Clone(out IStream ppstm)
        {
            throw new NotSupportedException();
        }

        #endregion

        #endregion

        #region Private Fields

        private Stream _stream;
        protected long _endOfStreamPosition = -1;

        #endregion
    }
}
