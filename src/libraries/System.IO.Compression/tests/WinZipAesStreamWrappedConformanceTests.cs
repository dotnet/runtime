//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.

//using System.IO.Tests;
//using System.Reflection;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.DotNet.XUnitExtensions;

//namespace System.IO.Compression.Tests
//{
//    /// <summary>
//    /// Wrapped connected stream conformance tests for WinZipAesStream (AES-128).
//    /// Tests encryption → decryption data flow through connected streams.
//    /// 
//    /// Note: WinZipAesStream has significant limitations for streaming:
//    /// 1. It requires knowing the total encrypted size upfront for decryption
//    /// 2. It has a 10-byte HMAC trailer written only on dispose
//    /// 
//    /// These tests use a fixed-size approach where we know the test data size.
//    /// </summary>
//    public class WinZipAes128StreamWrappedConformanceTests : WinZipAesStreamWrappedConformanceTestsBase
//    {
//        protected override int KeySizeBits => 128;
//    }

//    /// <summary>
//    /// Wrapped connected stream conformance tests for WinZipAesStream (AES-256).
//    /// </summary>
//    public class WinZipAes256StreamWrappedConformanceTests : WinZipAesStreamWrappedConformanceTestsBase
//    {
//        protected override int KeySizeBits => 256;
//    }

//    /// <summary>
//    /// Base class for WinZipAesStream wrapped connected stream conformance tests.
//    /// 
//    /// WinZipAesStream is fundamentally incompatible with the connected stream
//    /// conformance test model because:
//    /// 
//    /// 1. Decryption requires knowing total stream size BEFORE creating the stream
//    ///    (to distinguish encrypted data from the 10-byte HMAC trailer)
//    /// 2. Connected stream tests write variable amounts of data dynamically
//    /// 3. The HMAC trailer is only written when the encryption stream is disposed
//    /// 
//    /// This is an architectural limitation of the WinZip AES format, not a bug.
//    /// The format was designed for file-based archives where sizes are known upfront,
//    /// not for streaming scenarios.
//    ///
//    /// TODO: Should I just delete these tests altogether since they can't run?
//    /// 
//    /// </summary>
//    public abstract class WinZipAesStreamWrappedConformanceTestsBase : WrappingConnectedStreamConformanceTests
//    {
//        private const string TestPassword = "test-password";

//        private static readonly Type s_winZipAesStreamType;
//        private static readonly MethodInfo s_createKeyMethod;
//        private static readonly MethodInfo s_createMethod;
//        private static readonly MethodInfo s_getSaltSizeMethod;

//        protected abstract int KeySizeBits { get; }
//        protected int SaltSize => KeySizeBits / 16;
//        protected int KeySizeBytes => KeySizeBits / 8;
//        // Header = salt + 2-byte verifier, Trailer = 10-byte HMAC
//        protected int HeaderSize => SaltSize + 2;
//        protected const int HmacSize = 10;

//        static WinZipAesStreamWrappedConformanceTestsBase()
//        {
//            var assembly = typeof(ZipArchive).Assembly;
//            s_winZipAesStreamType = assembly.GetType("System.IO.Compression.WinZipAesStream", throwOnError: true)!;

//            s_createKeyMethod = s_winZipAesStreamType.GetMethod("CreateKey",
//                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
//                null,
//                new[] { typeof(ReadOnlyMemory<char>), typeof(byte[]), typeof(int) },
//                null)!;

//            s_createMethod = s_winZipAesStreamType.GetMethod("Create",
//                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
//                null,
//                new[] { typeof(Stream), typeof(byte[]), typeof(int), typeof(long), typeof(bool), typeof(bool) },
//                null)!;

//            s_getSaltSizeMethod = s_winZipAesStreamType.GetMethod("GetSaltSize",
//                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
//                null,
//                new[] { typeof(int) },
//                null)!;
//        }

//        // WinZipAesStream doesn't support seeking
//        protected override bool CanSeek => false;

//        // The encryption header is written lazily, so flush is required
//        protected override bool FlushRequiredToWriteData => true;
//        protected override bool FlushGuaranteesAllDataWritten => true;

//        // AES-CTR mode blocks on zero byte reads when data is buffered
//        protected override bool BlocksOnZeroByteReads => true;

//        // No concurrent exception type
//        protected override Type UnsupportedConcurrentExceptionType => null!;

//        private const string SkipReason = "WinZipAesStream requires knowing total encrypted stream size " +
//            "upfront for decryption (to locate HMAC trailer). This is incompatible with connected " +
//            "stream tests where data size is not known ahead of time.";

//        /// <summary>
//        /// WinZipAesStream requires knowing total stream size for decryption.
//        /// This is incompatible with generic connected stream tests that don't know
//        /// the data size upfront.
//        /// </summary>
//        protected override Task<StreamPair> CreateConnectedStreamsAsync()
//        {
//            throw new SkipTestException(SkipReason);
//        }

//        protected override Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false)
//        {
//            throw new SkipTestException(SkipReason);
//        }
//    }
//}
