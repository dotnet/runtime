// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    internal sealed partial class GPStream : Interop.Ole32.IStream
    {
        public unsafe Interop.HRESULT Clone(IntPtr* ppstm)
        {
            if (ppstm == null)
            {
                return Interop.HRESULT.STG_E_INVALIDPOINTER;
            }

            // The cloned object should have the same current "position"
            var clone = new GPStream(_dataStream)
            {
                _virtualPosition = _virtualPosition
            };

            *ppstm = DrawingCom.Instance.GetOrCreateComInterfaceForObject(clone, CreateComInterfaceFlags.None);

            return Interop.HRESULT.S_OK;
        }

        public unsafe Interop.HRESULT CopyTo(IntPtr pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            ulong remaining = cb;
            ulong totalWritten = 0;
            ulong totalRead = 0;

            fixed (byte* b = buffer)
            {
                while (remaining > 0)
                {
                    uint read = remaining < (ulong)buffer.Length ? (uint)remaining : (uint)buffer.Length;
                    Read(b, read, &read);
                    remaining -= read;
                    totalRead += read;

                    if (read == 0)
                    {
                        break;
                    }

                    uint written;
                    Interop.HRESULT hr = (Interop.HRESULT)WriteToStream(pstm, b, read, &written);
                    if (hr != Interop.HRESULT.S_OK)
                    {
                        return hr;
                    }
                    totalWritten += written;
                }
            }

            ArrayPool<byte>.Shared.Return(buffer);

            if (pcbRead != null)
            {
                *pcbRead = totalRead;
            }

            if (pcbWritten != null)
            {
                *pcbWritten = totalWritten;
            }

            return Interop.HRESULT.S_OK;
        }

        private static unsafe int WriteToStream(IntPtr pstm, byte* pv, uint cb, uint* pcbWritten)
        {
            return ((delegate* unmanaged<IntPtr, byte*, uint, uint*, int>)(*(*(void***)pstm + 4 /* IStream.Write slot */)))
                (pstm, pv, cb, pcbWritten);
        }
    }
}
