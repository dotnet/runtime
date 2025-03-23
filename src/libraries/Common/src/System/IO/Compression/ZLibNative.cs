// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security;

namespace System.IO.Compression
{
    /// <summary>
    /// This class provides declaration for constants and PInvokes as well as some basic tools for exposing the
    /// native System.IO.Compression.Native.dll (effectively, ZLib) library to managed code.
    ///
    /// <para>See also: How to choose a compression level (in comments to <see cref="CompressionLevel" />.)</para>
    /// </summary>
    /// <seealso href="https://www.zlib.net/manual.html">ZLib manual</seealso>
    internal static partial class ZLibNative
    {
        // This is the NULL pointer for using with ZLib pointers;
        // we prefer it to IntPtr.Zero to mimic the definition of Z_NULL in zlib.h:
        internal static readonly IntPtr ZNullPtr = IntPtr.Zero;

        public enum FlushCode : int
        {
            NoFlush = 0,
            SyncFlush = 2,
            Finish = 4,
            Block = 5
        }

        public enum ErrorCode : int
        {
            Ok = 0,
            StreamEnd = 1,
            StreamError = -2,
            DataError = -3,
            MemError = -4,
            BufError = -5,
            VersionError = -6
        }

        /// <summary>
        /// <para><strong>From the ZLib manual:</strong><br />
        /// <see cref="CompressionStrategy" /> is used to tune the compression algorithm.<br />
        /// Use the value <see cref="DefaultStrategy" /> for normal data, <see cref="Filtered" /> for data produced by a filter (or predictor),
        /// <see cref="HuffmanOnly" /> to force Huffman encoding only (no string match), or <see cref="RunLengthEncoding" /> to limit match distances to one
        /// (run-length encoding). Filtered data consists mostly of small values with a somewhat random distribution. In this case, the
        /// compression algorithm is tuned to compress them better. The effect of <see cref="Filtered" /> is to force more Huffman coding and
        /// less string matching; it is somewhat intermediate between <see cref="DefaultStrategy" /> and <see cref="HuffmanOnly" />.
        /// <see cref="RunLengthEncoding" /> is designed to be almost as fast as <see cref="HuffmanOnly" />, but give better compression for PNG image data.
        /// The strategy parameter only affects the compression ratio but not the correctness of the compressed output even if it is not set
        /// appropriately. <see cref="Fixed" /> prevents the use of dynamic Huffman codes, allowing for a simpler decoder for special applications.</para>
        ///
        /// <para><strong>For .NET Framework use:</strong><br />
        /// We have investigated compression scenarios for a bunch of different frequently occurring compression data and found that in all
        /// cases we investigated so far, <see cref="DefaultStrategy" /> provided best results</para>
        ///
        /// <para>See also: How to choose a compression level (in comments to <see cref="CompressionLevel" />.)</para>
        /// </summary>
        public enum CompressionStrategy : int
        {
            DefaultStrategy = 0,
            Filtered = 1,
            HuffmanOnly = 2,
            RunLengthEncoding = 3,
            Fixed = 4
        }

        /// <summary>
        /// In version 2.2.1, zlib-ng provides only the <see cref="Deflated" /> <see cref="CompressionMethod" />.
        /// </summary>
        public enum CompressionMethod : int
        {
            Deflated = 8
        }

        /// <summary>
        /// <para><strong>From the ZLib manual:</strong><br />
        /// ZLib's <c>windowBits</c> parameter is the base two logarithm of the window size (the size of the history buffer).
        /// It should be in the range 8..15 for this version of the library. Larger values of this parameter result in better compression
        /// at the expense of memory usage. The default value is 15 if <c>deflateInit</c> is used instead.</para>
        ///
        /// <para><strong>Note</strong>: <c>windowBits</c> can also be -8..-15 for raw deflate. In this case, -windowBits determines the window size.
        /// <c>Deflate</c> will then generate raw deflate data with no ZLib header or trailer, and will not compute an adler32 check value.</para>
        ///
        /// <para>See also: How to choose a compression level (in comments to <see cref="CompressionLevel" />.)</para>
        /// </summary>
        public const int Deflate_DefaultWindowBits = -15; // Legal values are 8..15 and -8..-15. 15 is the window size,
                                                          // negative val causes deflate to produce raw deflate data (no zlib header).

        /// <summary>
        /// <para><strong>From the ZLib manual:</strong><br />
        /// ZLib's <c>windowBits</c> parameter is the base two logarithm of the window size (the size of the history buffer).
        /// It should be in the range 8..15 for this version of the library. Larger values of this parameter result in better compression
        /// at the expense of memory usage. The default value is 15 if <c>deflateInit</c> is used instead.</para>
        /// </summary>
        public const int ZLib_DefaultWindowBits = 15;

        /// <summary>
        /// <para>ZLib's <c>windowBits</c> parameter is the base two logarithm of the window size (the size of the history buffer).
        /// For GZip header encoding, <c>windowBits</c> should be equal to a value between 8..15 (to specify Window Size) added to
        /// 16. The range of values for GZip encoding is therefore 24..31.</para>
        /// <para><strong>Note</strong>:<br />
        /// The GZip header will have no file name, no extra data, no comment, no modification time (set to zero), no header crc, and
        /// the operating system will be set based on the OS that the ZLib library was compiled to. <c>ZStream.adler</c>
        /// is a crc32 instead of an adler32.</para>
        /// </summary>
        public const int GZip_DefaultWindowBits = 31;

        /// <summary>
        /// <para><strong>From the ZLib manual:</strong><br />
        /// The <c>memLevel</c> parameter specifies how much memory should be allocated for the internal compression state.
        /// <c>memLevel</c> = 1 uses minimum memory but is slow and reduces compression ratio; <c>memLevel</c> = 9 uses maximum
        /// memory for optimal speed. The default value is 8.</para>
        ///
        /// <para>See also: How to choose a compression level (in comments to <see cref="CompressionLevel" />.)</para>
        /// </summary>
        public const int Deflate_DefaultMemLevel = 8;     // Memory usage by deflate. Legal range: [1..9]. 8 is ZLib default.
                                                          // More is faster and better compression with more memory usage.
        public const int Deflate_NoCompressionMemLevel = 7;

        public const byte GZip_Header_ID1 = 31;
        public const byte GZip_Header_ID2 = 139;

        /**
         * Do not remove the nested typing of types inside of <see cref="ZLibNative" />.
         * This was done on purpose to:
         *
         * - Achieve the right encapsulation in a situation where <see cref="ZLibNative" /> may be compiled division-wide
         *   into different assemblies that wish to consume <c>System.IO.Compression.Native</c>. Since <c>internal</c>
         *   scope is effectively like <c>public</c> scope when compiling <see cref="ZLibNative" /> into a higher
         *   level assembly, we need a combination of inner types and <c>private</c>-scope members to achieve
         *   the right encapsulation.
         *
         * - Achieve late dynamic loading of <c>System.IO.Compression.Native.dll</c> at the right time.
         *   The native assembly will not be loaded unless it is actually used since the loading is performed by a static
         *   constructor of an inner type that is not directly referenced by user code.
         *
         *   In Dev12 we would like to create a proper feature for loading native assemblies from user-specified
         *   directories in order to PInvoke into them. This would preferably happen in the native interop/PInvoke
         *   layer; if not we can add a Framework level feature.
         */

        /// <summary>
        /// The <see cref="ZLibStreamHandle" /> could be a <see cref="System.Runtime.ConstrainedExecution.CriticalFinalizerObject" /> rather than a
        /// <see cref="SafeHandle" />. This would save an <see cref="IntPtr" /> field since
        /// <see cref="ZLibStreamHandle" /> does not actually use its <see cref="SafeHandle.handle" /> field.
        /// Instead it uses a private <see cref="_zStream" /> field which is the actual handle data
        /// structure requiring critical finalization.
        /// However, we would like to take advantage if the better debugability offered by the fact that a
        /// <em>releaseHandleFailed MDA</em> is raised if the <see cref="ReleaseHandle" /> method returns
        /// <c>false</c>, which can for instance happen if the underlying ZLib <see cref="Interop.ZLib.InflateEnd"/>
        /// or <see cref="Interop.ZLib.DeflateEnd"/> routines return an failure error code.
        /// </summary>
        public sealed class ZLibStreamHandle : SafeHandle
        {
            public enum State
            {
                NotInitialized,
                InitializedForDeflate,
                InitializedForInflate,
                Disposed
            }

            private ZStream _zStream;

            private volatile State _initializationState;

            public ZLibStreamHandle()
                : base(new IntPtr(-1), true)
            {
                _initializationState = State.NotInitialized;
                SetHandle(IntPtr.Zero);
            }

            public override bool IsInvalid
            {
                get { return handle == new IntPtr(-1); }
            }

            public State InitializationState
            {
                get { return _initializationState; }
            }

            protected override bool ReleaseHandle() =>
                InitializationState switch
                {
                    State.NotInitialized => true,
                    State.InitializedForDeflate => (DeflateEnd() == ErrorCode.Ok),
                    State.InitializedForInflate => (InflateEnd() == ErrorCode.Ok),
                    State.Disposed => true,
                    _ => false,  // This should never happen. Did we forget one of the State enum values in the switch?
                };

            public IntPtr NextIn
            {
                get { return _zStream.nextIn; }
                set { _zStream.nextIn = value; }
            }

            public uint AvailIn
            {
                get { return _zStream.availIn; }
                set { _zStream.availIn = value; }
            }

            public IntPtr NextOut
            {
                get { return _zStream.nextOut; }
                set { _zStream.nextOut = value; }
            }

            public uint AvailOut
            {
                get { return _zStream.availOut; }
                set { _zStream.availOut = value; }
            }

            private void EnsureNotDisposed()
            {
                ObjectDisposedException.ThrowIf(InitializationState == State.Disposed, this);
            }

            private void EnsureState(State requiredState)
            {
                if (InitializationState != requiredState)
                    throw new InvalidOperationException("InitializationState != " + requiredState.ToString());
            }

            public unsafe ErrorCode DeflateInit2_(CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
            {
                EnsureNotDisposed();
                EnsureState(State.NotInitialized);

                fixed (ZStream* stream = &_zStream)
                {
                    ErrorCode errC = Interop.ZLib.DeflateInit2_(stream, level, CompressionMethod.Deflated, windowBits, memLevel, strategy);
                    _initializationState = State.InitializedForDeflate;

                    return errC;
                }
            }

            public unsafe ErrorCode Deflate(FlushCode flush)
            {
                EnsureNotDisposed();
                EnsureState(State.InitializedForDeflate);

                fixed (ZStream* stream = &_zStream)
                {
                    return Interop.ZLib.Deflate(stream, flush);
                }
            }

            public unsafe ErrorCode DeflateEnd()
            {
                EnsureNotDisposed();
                EnsureState(State.InitializedForDeflate);

                fixed (ZStream* stream = &_zStream)
                {
                    ErrorCode errC = Interop.ZLib.DeflateEnd(stream);
                    _initializationState = State.Disposed;

                    return errC;
                }
            }

            public unsafe ErrorCode InflateInit2_(int windowBits)
            {
                EnsureNotDisposed();
                EnsureState(State.NotInitialized);

                fixed (ZStream* stream = &_zStream)
                {
                    ErrorCode errC = Interop.ZLib.InflateInit2_(stream, windowBits);
                    _initializationState = State.InitializedForInflate;

                    return errC;
                }
            }

            public unsafe ErrorCode Inflate(FlushCode flush)
            {
                EnsureNotDisposed();
                EnsureState(State.InitializedForInflate);

                fixed (ZStream* stream = &_zStream)
                {
                    return Interop.ZLib.Inflate(stream, flush);
                }
            }

            public unsafe ErrorCode InflateEnd()
            {
                EnsureNotDisposed();
                EnsureState(State.InitializedForInflate);

                fixed (ZStream* stream = &_zStream)
                {
                    ErrorCode errC = Interop.ZLib.InflateEnd(stream);
                    _initializationState = State.Disposed;

                    return errC;
                }
            }

            // This can work even after XxflateEnd().
            public string GetErrorMessage() => _zStream.msg != ZNullPtr ? Marshal.PtrToStringUTF8(_zStream.msg)! : string.Empty;
        }

        public static ErrorCode CreateZLibStreamForDeflate(out ZLibStreamHandle zLibStreamHandle, CompressionLevel level,
            int windowBits, int memLevel, CompressionStrategy strategy)
        {
            zLibStreamHandle = new ZLibStreamHandle();
            return zLibStreamHandle.DeflateInit2_(level, windowBits, memLevel, strategy);
        }

        public static ErrorCode CreateZLibStreamForInflate(out ZLibStreamHandle zLibStreamHandle, int windowBits)
        {
            zLibStreamHandle = new ZLibStreamHandle();
            return zLibStreamHandle.InflateInit2_(windowBits);
        }
    }
}
