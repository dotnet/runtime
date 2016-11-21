// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias System_Runtime_Extensions;

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Runtime.WindowsRuntime.Internal;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;

using MemoryStream = System_Runtime_Extensions::System.IO.MemoryStream;

namespace System.IO
{
    /// <summary>
    /// An <code>wrapper</code> for a managed stream that implements all WinRT stream operations.
    /// This class must not implement any WinRT stream interfaces directly.
    /// We never create instances of this class directly; instead we use classes defined in
    /// the region Interface adapters to implement WinRT ifaces and create instances of those types.
    /// See comment in that region for technical details.
    /// </summary>
    internal abstract class NetFxToWinRtStreamAdapter : IDisposable
    {
        #region Construction

        #region Interface adapters

        // Instances of private types defined in this section will be returned from NetFxToWinRtStreamAdapter.Create(..).
        // Depending on the capabilities of the .NET stream for which we need to construct the adapter, we need to return
        // an object that can be QIed (COM speak for "cast") to a well-defined set of ifaces.
        // E.g, if the specified stream CanRead, but not CanSeek and not CanWrite, then we *must* return an object that
        // can be QIed to IInputStream, but *not* IRandomAccessStream and *not* IOutputStream.
        // There are two ways to do that:
        //   - We could explicitly implement ICustomQueryInterface and respond to QI requests by analyzing the stream capabilities
        //   - We can use the runtime's ability to do that for us, based on the ifaces the concrete class implements (or does not).
        // The latter is much more elegant, and likely also faster.


        private class InputStream : NetFxToWinRtStreamAdapter, IInputStream, IDisposable
        {
            internal InputStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }


        private class OutputStream : NetFxToWinRtStreamAdapter, IOutputStream, IDisposable
        {
            internal OutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }


        private class RandomAccessStream : NetFxToWinRtStreamAdapter, IRandomAccessStream, IInputStream, IOutputStream, IDisposable
        {
            internal RandomAccessStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }


        private class InputOutputStream : NetFxToWinRtStreamAdapter, IInputStream, IOutputStream, IDisposable
        {
            internal InputOutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }

        #endregion Interface adapters

        // We may want to define different behaviour for different types of streams.
        // For instance, ReadAsync treats MemoryStream special for performance reasons.
        // The enum 'StreamReadOperationOptimization' describes the read optimization to employ for a
        // given NetFxToWinRtStreamAdapter instance. In future, we might define other enums to follow a
        // similar pattern, e.g. 'StreamWriteOperationOptimization' or 'StreamFlushOperationOptimization'.
        private enum StreamReadOperationOptimization
        {
            AbstractStream = 0, MemoryStream
        }


        internal static NetFxToWinRtStreamAdapter Create(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            StreamReadOperationOptimization readOptimization = StreamReadOperationOptimization.AbstractStream;
            if (stream.CanRead)
                readOptimization = DetermineStreamReadOptimization(stream);

            NetFxToWinRtStreamAdapter adapter;

            if (stream.CanSeek)
                adapter = new RandomAccessStream(stream, readOptimization);

            else if (stream.CanRead && stream.CanWrite)
                adapter = new InputOutputStream(stream, readOptimization);

            else if (stream.CanRead)
                adapter = new InputStream(stream, readOptimization);

            else if (stream.CanWrite)
                adapter = new OutputStream(stream, readOptimization);

            else
                throw new ArgumentException(SR.Argument_NotSufficientCapabilitiesToConvertToWinRtStream);

            return adapter;
        }


        private static StreamReadOperationOptimization DetermineStreamReadOptimization(Stream stream)
        {
            Debug.Assert(stream != null);

            if (CanApplyReadMemoryStreamOptimization(stream))
                return StreamReadOperationOptimization.MemoryStream;

            return StreamReadOperationOptimization.AbstractStream;
        }


        private static bool CanApplyReadMemoryStreamOptimization(Stream stream)
        {
            MemoryStream memStream = stream as MemoryStream;
            if (memStream == null)
                return false;

            ArraySegment<byte> arrSeg;
            return memStream.TryGetBuffer(out arrSeg);
        }


        private NetFxToWinRtStreamAdapter(Stream stream, StreamReadOperationOptimization readOptimization)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead || stream.CanWrite || stream.CanSeek);
            Contract.EndContractBlock();

            Debug.Assert(!stream.CanRead || (stream.CanRead && this is IInputStream));
            Debug.Assert(!stream.CanWrite || (stream.CanWrite && this is IOutputStream));
            Debug.Assert(!stream.CanSeek || (stream.CanSeek && this is IRandomAccessStream));

            _readOptimization = readOptimization;
            _managedStream = stream;
        }

        #endregion Construction


        #region Instance variables

        private Stream _managedStream = null;
        private bool _leaveUnderlyingStreamOpen = true;
        private readonly StreamReadOperationOptimization _readOptimization;

        #endregion Instance variables


        #region Tools and Helpers

        /// <summary>
        /// We keep tables for mappings between managed and WinRT streams to make sure to always return the same adapter for a given underlying stream.
        /// However, in order to avoid global locks on those tables, several instances of this type may be created and then can race to be entered
        /// into the appropriate map table. All except for the winning instances will be thrown away. However, we must ensure that when the losers are
        /// finalized, they do not dispose the underlying stream. To ensure that, we must call this method on the winner to notify it that it is safe to
        /// dispose the underlying stream.
        /// </summary>
        internal void SetWonInitializationRace()
        {
            _leaveUnderlyingStreamOpen = false;
        }


        public Stream GetManagedStream()
        {
            return _managedStream;
        }


        private Stream EnsureNotDisposed()
        {
            Stream str = _managedStream;

            if (str == null)
            {
                ObjectDisposedException ex = new ObjectDisposedException(SR.ObjectDisposed_CannotPerformOperation);
                ex.SetErrorCode(HResults.RO_E_CLOSED);
                throw ex;
            }

            return str;
        }

        #endregion Tools and Helpers


        #region Common public interface

        /// <summary>Implements IDisposable.Dispose (IClosable.Close in WinRT)</summary>
        void IDisposable.Dispose()
        {
            Stream str = _managedStream;
            if (str == null)
                return;

            _managedStream = null;

            if (!_leaveUnderlyingStreamOpen)
                str.Dispose();
        }

        #endregion Common public interface


        #region IInputStream public interface

        public IAsyncOperationWithProgress<IBuffer, UInt32> ReadAsync(IBuffer buffer, UInt32 count, InputStreamOptions options)
        {
            if (buffer == null)
            {
                // Mapped to E_POINTER.
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count < 0 || Int32.MaxValue < count)
            {
                ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(count));
                ex.SetErrorCode(HResults.E_INVALIDARG);
                throw ex;
            }

            if (buffer.Capacity < count)
            {
                ArgumentException ex = new ArgumentException(SR.Argument_InsufficientBufferCapacity);
                ex.SetErrorCode(HResults.E_INVALIDARG);
                throw ex;
            }

            if (!(options == InputStreamOptions.None || options == InputStreamOptions.Partial || options == InputStreamOptions.ReadAhead))
            {
                ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(options),
                                                                                 SR.ArgumentOutOfRange_InvalidInputStreamOptionsEnumValue);
                ex.SetErrorCode(HResults.E_INVALIDARG);
                throw ex;
            }

            // Commented due to a reported CCRewrite bug. Should uncomment when fixed:
            //Contract.Ensures(Contract.Result<IAsyncOperationWithProgress<IBuffer, UInt32>>() != null);
            //Contract.EndContractBlock();

            Stream str = EnsureNotDisposed();

            IAsyncOperationWithProgress<IBuffer, UInt32> readAsyncOperation;
            switch (_readOptimization)
            {
                case StreamReadOperationOptimization.MemoryStream:
                    readAsyncOperation = StreamOperationsImplementation.ReadAsync_MemoryStream(str, buffer, count);
                    break;

                case StreamReadOperationOptimization.AbstractStream:
                    readAsyncOperation = StreamOperationsImplementation.ReadAsync_AbstractStream(str, buffer, count, options);
                    break;

                // Use this pattern to add more optimisation options if necessary:
                //case StreamReadOperationOptimization.XxxxStream:
                //    readAsyncOperation = StreamOperationsImplementation.ReadAsync_XxxxStream(str, buffer, count, options);
                //    break;

                default:
                    Debug.Assert(false, "We should never get here. Someone forgot to handle an input stream optimisation option.");
                    readAsyncOperation = null;
                    break;
            }

            return readAsyncOperation;
        }

        #endregion IInputStream public interface


        #region IOutputStream public interface

        public IAsyncOperationWithProgress<UInt32, UInt32> WriteAsync(IBuffer buffer)
        {
            if (buffer == null)
            {
                // Mapped to E_POINTER.
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Capacity < buffer.Length)
            {
                ArgumentException ex = new ArgumentException(SR.Argument_BufferLengthExceedsCapacity);
                ex.SetErrorCode(HResults.E_INVALIDARG);
                throw ex;
            }

            // Commented due to a reported CCRewrite bug. Should uncomment when fixed:
            //Contract.Ensures(Contract.Result<IAsyncOperationWithProgress<UInt32, UInt32>>() != null);
            //Contract.EndContractBlock();

            Stream str = EnsureNotDisposed();
            return StreamOperationsImplementation.WriteAsync_AbstractStream(str, buffer);
        }


        public IAsyncOperation<Boolean> FlushAsync()
        {
            Contract.Ensures(Contract.Result<IAsyncOperation<Boolean>>() != null);
            Contract.EndContractBlock();

            Stream str = EnsureNotDisposed();
            return StreamOperationsImplementation.FlushAsync_AbstractStream(str);
        }

        #endregion IOutputStream public interface


        #region IRandomAccessStream public interface


        #region IRandomAccessStream public interface: Not cloning related

        public void Seek(UInt64 position)
        {
            if (position > Int64.MaxValue)
            {
                ArgumentException ex = new ArgumentException(SR.IO_CannotSeekBeyondInt64MaxValue);
                ex.SetErrorCode(HResults.E_INVALIDARG);
                throw ex;
            }

            // Commented due to a reported CCRewrite bug. Should uncomment when fixed:
            //Contract.EndContractBlock();

            Stream str = EnsureNotDisposed();
            Int64 pos = unchecked((Int64)position);

            Debug.Assert(str != null);
            Debug.Assert(str.CanSeek, "The underlying str is expected to support Seek, but it does not.");
            Debug.Assert(0 <= pos, "Unexpected pos=" + pos + ".");

            str.Seek(pos, SeekOrigin.Begin);
        }


        public bool CanRead
        {
            get
            {
                Stream str = EnsureNotDisposed();
                return str.CanRead;
            }
        }


        public bool CanWrite
        {
            get
            {
                Stream str = EnsureNotDisposed();
                return str.CanWrite;
            }
        }


        public UInt64 Position
        {
            get
            {
                Contract.Ensures(Contract.Result<UInt64>() >= 0);

                Stream str = EnsureNotDisposed();
                return (UInt64)str.Position;
            }
        }


        public UInt64 Size
        {
            get
            {
                Contract.Ensures(Contract.Result<UInt64>() >= 0);

                Stream str = EnsureNotDisposed();
                return (UInt64)str.Length;
            }

            set
            {
                if (value > Int64.MaxValue)
                {
                    ArgumentException ex = new ArgumentException(SR.IO_CannotSetSizeBeyondInt64MaxValue);
                    ex.SetErrorCode(HResults.E_INVALIDARG);
                    throw ex;
                }

                // Commented due to a reported CCRewrite bug. Should uncomment when fixed:
                //Contract.EndContractBlock();

                Stream str = EnsureNotDisposed();

                if (!str.CanWrite)
                {
                    InvalidOperationException ex = new InvalidOperationException(SR.InvalidOperation_CannotSetStreamSizeCannotWrite);
                    ex.SetErrorCode(HResults.E_ILLEGAL_METHOD_CALL);
                    throw ex;
                }

                Int64 val = unchecked((Int64)value);

                Debug.Assert(str != null);
                Debug.Assert(str.CanSeek, "The underlying str is expected to support Seek, but it does not.");
                Debug.Assert(0 <= val, "Unexpected val=" + val + ".");

                str.SetLength(val);
            }
        }

        #endregion IRandomAccessStream public interface: Not cloning related


        #region IRandomAccessStream public interface: Cloning related

        // We do not want to support the cloning-related operation for now.
        // They appear to mainly target corner-case scenarios in Windows itself,
        // and are (mainly) a historical artefact of abandoned early designs
        // for IRandonAccessStream.
        // Cloning can be added in future, however, it would be quite complex
        // to support it correctly for generic streams.

        private static void ThrowCloningNotSuported(String methodName)
        {
            NotSupportedException nse = new NotSupportedException(SR.Format(SR.NotSupported_CloningNotSupported, methodName));
            nse.SetErrorCode(HResults.E_NOTIMPL);
            throw nse;
        }


        public IRandomAccessStream CloneStream()
        {
            ThrowCloningNotSuported("CloneStream");
            return null;
        }


        public IInputStream GetInputStreamAt(UInt64 position)
        {
            ThrowCloningNotSuported("GetInputStreamAt");
            return null;
        }


        public IOutputStream GetOutputStreamAt(UInt64 position)
        {
            ThrowCloningNotSuported("GetOutputStreamAt");
            return null;
        }
        #endregion IRandomAccessStream public interface: Cloning related

        #endregion IRandomAccessStream public interface

    }  // class NetFxToWinRtStreamAdapter
}  // namespace

// NetFxToWinRtStreamAdapter.cs
