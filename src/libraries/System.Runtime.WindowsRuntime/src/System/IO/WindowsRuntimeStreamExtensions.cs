// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias System_Runtime_Extensions;

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using BufferedStream = System_Runtime_Extensions::System.IO.BufferedStream;

namespace System.IO
{
    /// <summary>
    /// Contains extension methods for conversion between WinRT streams and managed streams.
    /// This class is the public facade for the stream adapters library.
    /// </summary>
    public static class WindowsRuntimeStreamExtensions
    {
        #region Constants and static Fields

        private const int DefaultBufferSize = 16384;  // = 0x4000 = 16 KBytes.

        private static ConditionalWeakTable<object, Stream> s_winRtToNetFxAdapterMap
                 = new ConditionalWeakTable<object, Stream>();

        private static ConditionalWeakTable<Stream, NetFxToWinRtStreamAdapter> s_netFxToWinRtAdapterMap
                 = new ConditionalWeakTable<Stream, NetFxToWinRtStreamAdapter>();

        #endregion Constants and static Fields


        #region Helpers

#if DEBUG
        private static void AssertMapContains<TKey, TValue>(ConditionalWeakTable<TKey, TValue> map, TKey key, TValue value,
                                                            bool valueMayBeWrappedInBufferedStream)
                                                                                                where TKey : class
                                                                                                where TValue : class
        {
            TValue valueInMap;

            Debug.Assert(key != null);

            bool hasValueForKey = map.TryGetValue(key, out valueInMap);

            Debug.Assert(hasValueForKey);

            if (valueMayBeWrappedInBufferedStream)
            {
                BufferedStream bufferedValueInMap = valueInMap as BufferedStream;
                Debug.Assert(Object.ReferenceEquals(value, valueInMap)
                                || (bufferedValueInMap != null && Object.ReferenceEquals(value, bufferedValueInMap.UnderlyingStream)));
            }
            else
            {
                Debug.Assert(Object.ReferenceEquals(value, valueInMap));
            }
        }
#endif  // DEBUG

        private static void EnsureAdapterBufferSize(Stream adapter, int requiredBufferSize, string methodName)
        {
            Debug.Assert(adapter != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(methodName));

            int currentBufferSize = 0;
            BufferedStream bufferedAdapter = adapter as BufferedStream;
            if (bufferedAdapter != null)
                currentBufferSize = bufferedAdapter.BufferSize;

            if (requiredBufferSize != currentBufferSize)
            {
                if (requiredBufferSize == 0)
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_CannotChangeBufferSizeOfWinRtStreamAdapterToZero, methodName));

                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_CannotChangeBufferSizeOfWinRtStreamAdapter, methodName));
            }
        }

        #endregion Helpers


        #region WinRt-to-NetFx conversion

        [CLSCompliant(false)]
        public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream)
        {
            return AsStreamInternal(windowsRuntimeStream, DefaultBufferSize, "AsStreamForRead", forceBufferSize: false);
        }


        [CLSCompliant(false)]
        public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream, int bufferSize)
        {
            return AsStreamInternal(windowsRuntimeStream, bufferSize, "AsStreamForRead", forceBufferSize: true);
        }


        [CLSCompliant(false)]
        public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream)
        {
            return AsStreamInternal(windowsRuntimeStream, DefaultBufferSize, "AsStreamForWrite", forceBufferSize: false);
        }


        [CLSCompliant(false)]
        public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream, int bufferSize)
        {
            return AsStreamInternal(windowsRuntimeStream, bufferSize, "AsStreamForWrite", forceBufferSize: true);
        }


        [CLSCompliant(false)]
        public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream)
        {
            return AsStreamInternal(windowsRuntimeStream, DefaultBufferSize, "AsStream", forceBufferSize: false);
        }


        [CLSCompliant(false)]
        public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream, int bufferSize)
        {
            return AsStreamInternal(windowsRuntimeStream, bufferSize, "AsStream", forceBufferSize: true);
        }


        private static Stream AsStreamInternal(object windowsRuntimeStream, int bufferSize, string invokedMethodName, bool forceBufferSize)
        {
            if (windowsRuntimeStream == null)
                throw new ArgumentNullException(nameof(windowsRuntimeStream));

            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_WinRtAdapterBufferSizeMayNotBeNegative);

            Debug.Assert(!string.IsNullOrWhiteSpace(invokedMethodName));

            // If the WinRT stream is actually a wrapped managed stream, we will unwrap it and return the original.
            // In that case we do not need to put the wrapper into the map.

            // We currently do capability-based adapter selection for WinRt->NetFx, but not vice versa (time constraints).
            // Once we added the reverse direction, we will be able replce this entire section with just a few lines.
            NetFxToWinRtStreamAdapter sAdptr = windowsRuntimeStream as NetFxToWinRtStreamAdapter;
            if (sAdptr != null)
            {
                Stream wrappedNetFxStream = sAdptr.GetManagedStream();
                if (wrappedNetFxStream == null)
                    throw new ObjectDisposedException(nameof(windowsRuntimeStream), SR.ObjectDisposed_CannotPerformOperation);

#if DEBUG  // In Chk builds, verify that the original managed stream is correctly entered into the NetFx->WinRT map:
                AssertMapContains(s_netFxToWinRtAdapterMap, wrappedNetFxStream, sAdptr,
                                  valueMayBeWrappedInBufferedStream: false);
#endif  // DEBUG

                return wrappedNetFxStream;
            }

            // We have a real WinRT stream.

            Stream adapter;
            bool adapterExists = s_winRtToNetFxAdapterMap.TryGetValue(windowsRuntimeStream, out adapter);

            // There is already an adapter:
            if (adapterExists)
            {
                Debug.Assert((adapter is BufferedStream && ((BufferedStream)adapter).UnderlyingStream is WinRtToNetFxStreamAdapter)
                                || (adapter is WinRtToNetFxStreamAdapter));

                if (forceBufferSize)
                    EnsureAdapterBufferSize(adapter, bufferSize, invokedMethodName);

                return adapter;
            }

            // We do not have an adapter for this WinRT stream yet and we need to create one.
            // Do that in a thread-safe manner in a separate method such that we only have to pay for the compiler allocating
            // the required closure if this code path is hit:

            return AsStreamInternalFactoryHelper(windowsRuntimeStream, bufferSize, invokedMethodName, forceBufferSize);
        }


        // Separate method so we only pay for closure allocation if this code is executed:
        private static Stream WinRtToNetFxAdapterMap_GetValue(object winRtStream)
        {
            return s_winRtToNetFxAdapterMap.GetValue(winRtStream, (wrtStr) => WinRtToNetFxStreamAdapter.Create(wrtStr));
        }


        // Separate method so we only pay for closure allocation if this code is executed:
        private static Stream WinRtToNetFxAdapterMap_GetValue(object winRtStream, int bufferSize)
        {
            return s_winRtToNetFxAdapterMap.GetValue(winRtStream, (wrtStr) => new BufferedStream(WinRtToNetFxStreamAdapter.Create(wrtStr), bufferSize));
        }


        private static Stream AsStreamInternalFactoryHelper(object windowsRuntimeStream, int bufferSize, string invokedMethodName, bool forceBufferSize)
        {
            Debug.Assert(windowsRuntimeStream != null);
            Debug.Assert(bufferSize >= 0);
            Debug.Assert(!string.IsNullOrWhiteSpace(invokedMethodName));

            // Get the adapter for this windowsRuntimeStream again (it may have been created concurrently).
            // If none exists yet, create a new one:
            Stream adapter = (bufferSize == 0)
                                ? WinRtToNetFxAdapterMap_GetValue(windowsRuntimeStream)
                                : WinRtToNetFxAdapterMap_GetValue(windowsRuntimeStream, bufferSize);

            Debug.Assert(adapter != null);
            Debug.Assert((adapter is BufferedStream && ((BufferedStream)adapter).UnderlyingStream is WinRtToNetFxStreamAdapter)
                                || (adapter is WinRtToNetFxStreamAdapter));

            if (forceBufferSize)
                EnsureAdapterBufferSize(adapter, bufferSize, invokedMethodName);

            WinRtToNetFxStreamAdapter actualAdapter = adapter as WinRtToNetFxStreamAdapter;
            if (actualAdapter == null)
                actualAdapter = ((BufferedStream)adapter).UnderlyingStream as WinRtToNetFxStreamAdapter;

            actualAdapter.SetWonInitializationRace();

            return adapter;
        }

        #endregion WinRt-to-NetFx conversion


        #region NetFx-to-WinRt conversion

        [CLSCompliant(false)]
        public static IInputStream AsInputStream(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new NotSupportedException(SR.NotSupported_CannotConvertNotReadableToInputStream);

            object adapter = AsWindowsRuntimeStreamInternal(stream);

            IInputStream winRtStream = adapter as IInputStream;
            Debug.Assert(winRtStream != null);

            return winRtStream;
        }


        [CLSCompliant(false)]
        public static IOutputStream AsOutputStream(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanWrite)
                throw new NotSupportedException(SR.NotSupported_CannotConvertNotWritableToOutputStream);

            object adapter = AsWindowsRuntimeStreamInternal(stream);

            IOutputStream winRtStream = adapter as IOutputStream;
            Debug.Assert(winRtStream != null);

            return winRtStream;
        }


        [CLSCompliant(false)]
        public static IRandomAccessStream AsRandomAccessStream(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanSeek)
                throw new NotSupportedException(SR.NotSupported_CannotConvertNotSeekableToRandomAccessStream);

            object adapter = AsWindowsRuntimeStreamInternal(stream);

            IRandomAccessStream winRtStream = adapter as IRandomAccessStream;
            Debug.Assert(winRtStream != null);

            return winRtStream;
        }


        private static object AsWindowsRuntimeStreamInternal(Stream stream)
        {
            // Check to see if the managed stream is actually a wrapper of a WinRT stream:
            // (This can be either an adapter directly, or an adapter wrapped in a BufferedStream.)
            WinRtToNetFxStreamAdapter sAdptr = stream as WinRtToNetFxStreamAdapter;
            if (sAdptr == null)
            {
                BufferedStream buffAdptr = stream as BufferedStream;
                if (buffAdptr != null)
                    sAdptr = buffAdptr.UnderlyingStream as WinRtToNetFxStreamAdapter;
            }

            // If the managed stream us actually a WinRT stream, we will unwrap it and return the original.
            // In that case we do not need to put the wrapper into the map.
            if (sAdptr != null)
            {
                object wrappedWinRtStream = sAdptr.GetWindowsRuntimeStream<Object>();
                if (wrappedWinRtStream == null)
                    throw new ObjectDisposedException(nameof(stream), SR.ObjectDisposed_CannotPerformOperation);

#if DEBUG  // In Chk builds, verify that the original WinRT stream is correctly entered into the WinRT->NetFx map:
                AssertMapContains(s_winRtToNetFxAdapterMap, wrappedWinRtStream, sAdptr, valueMayBeWrappedInBufferedStream: true);
#endif  // DEBUG
                return wrappedWinRtStream;
            }

            // We have a real managed Stream.

            // See if the managed stream already has an adapter:
            NetFxToWinRtStreamAdapter adapter;
            bool adapterExists = s_netFxToWinRtAdapterMap.TryGetValue(stream, out adapter);

            // There is already an adapter:
            if (adapterExists)
                return adapter;

            // We do not have an adapter for this managed stream yet and we need to create one.
            // Do that in a thread-safe manner in a separate method such that we only have to pay for the compiler allocating
            // the required closure if this code path is hit:
            return AsWindowsRuntimeStreamInternalFactoryHelper(stream);
        }


        private static NetFxToWinRtStreamAdapter AsWindowsRuntimeStreamInternalFactoryHelper(Stream stream)
        {
            Debug.Assert(stream != null);

            // Get the adapter for managed stream again (it may have been created concurrently).
            // If none exists yet, create a new one:
            NetFxToWinRtStreamAdapter adapter = s_netFxToWinRtAdapterMap.GetValue(stream, (str) => NetFxToWinRtStreamAdapter.Create(str));

            Debug.Assert(adapter != null);
            adapter.SetWonInitializationRace();

            return adapter;
        }
        #endregion NetFx-to-WinRt conversion

    }  // class WindowsRuntimeStreamExtensions
}  // namespace

// WindowsRuntimeStreamExtensions.cs
