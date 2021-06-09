// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace System.Text.Tests
{
    public class EncodingTests
    {
#pragma warning disable SYSLIB0001 // UTF7Encoding is obsolete
        private static UTF7Encoding _utf7Encoding = new UTF7Encoding();
#pragma warning restore SYSLIB0001

        public static IEnumerable<object[]> DisallowedEncodings()
        {
            yield return new object[] { "utf-7", 65000 };
        }

        [Theory]
        [MemberData(nameof(DisallowedEncodings))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        public void GetEncoding_BuiltIn_ByCodePage_WithDisallowedEncoding_Throws(string encodingName, int codePage)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        {
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(codePage));
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(codePage, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Moq uses Reflection.Emit
        [MemberData(nameof(DisallowedEncodings))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        public void GetEncoding_FromProvider_ByCodePage_WithDisallowedEncoding_Throws(string encodingName, int codePage)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        {
            Mock<Encoding> mockEncoding = new Mock<Encoding>();
            mockEncoding.Setup(o => o.CodePage).Returns(codePage);

            Mock<EncodingProvider> mockProvider = new Mock<EncodingProvider>();
            mockProvider.Setup(o => o.GetEncoding(codePage)).Returns(mockEncoding.Object);
            mockProvider.Setup(o => o.GetEncoding(codePage, It.IsAny<EncoderFallback>(), It.IsAny<DecoderFallback>())).Returns(mockEncoding.Object);

            ThreadStaticEncodingProvider.WithEncodingProvider(mockProvider.Object, () =>
            {
                Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(codePage));
                Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(codePage, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback));
            });
        }

        [Theory]
        [MemberData(nameof(DisallowedEncodings))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        public void GetEncoding_BuiltIn_ByEncodingName_WithDisallowedEncoding_Throws(string encodingName, int codePage)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        {
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(encodingName));
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(encodingName, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Moq uses Reflection.Emit
        [MemberData(nameof(DisallowedEncodings))]
        public void GetEncoding_FromProvider_ByEncodingName_WithDisallowedEncoding_Throws(string encodingName, int codePage)
        {
            Mock<Encoding> mockEncoding = new Mock<Encoding>();
            mockEncoding.Setup(o => o.CodePage).Returns(codePage);

            Mock<EncodingProvider> mockProvider = new Mock<EncodingProvider>();
            mockProvider.Setup(o => o.GetEncoding(encodingName)).Returns(mockEncoding.Object);
            mockProvider.Setup(o => o.GetEncoding(encodingName, It.IsAny<EncoderFallback>(), It.IsAny<DecoderFallback>())).Returns(mockEncoding.Object);

            ThreadStaticEncodingProvider.WithEncodingProvider(mockProvider.Object, () =>
            {
                Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(encodingName));
                Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(encodingName, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback));
            });
        }

        [Theory]
        [MemberData(nameof(DisallowedEncodings))]
        public void GetEncodings_BuiltIn_DoesNotContainDisallowedEncodings(string encodingName, int codePage)
        {
            foreach (EncodingInfo encodingInfo in Encoding.GetEncodings())
            {
                Assert.NotEqual(encodingName, encodingInfo.Name, StringComparer.OrdinalIgnoreCase);
                Assert.NotEqual(codePage, encodingInfo.CodePage);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Moq uses Reflection.Emit
        [MemberData(nameof(DisallowedEncodings))]
        public void GetEncodings_FromProvider_DoesNotContainDisallowedEncodings(string encodingName, int codePage)
        {
            Mock<EncodingProvider> mockProvider = new Mock<EncodingProvider>(MockBehavior.Strict);
            mockProvider.Setup(o => o.GetEncodings()).Returns(
                new[] { new EncodingInfo(mockProvider.Object, codePage, encodingName, "UTF-7") });

            ThreadStaticEncodingProvider.WithEncodingProvider(mockProvider.Object, () =>
            {
                foreach (EncodingInfo encodingInfo in Encoding.GetEncodings())
                {
                    Assert.NotEqual(encodingName, encodingInfo.Name, StringComparer.OrdinalIgnoreCase);
                    Assert.NotEqual(codePage, encodingInfo.CodePage);
                }
            });
        }

        private sealed class ThreadStaticEncodingProvider : EncodingProvider
        {
            private static readonly object _globalRegistrationLockObj = new object();
            private static bool _globalRegistrationCompleted;

            [ThreadStatic]
            private static EncodingProvider _staticInstance;

            private ThreadStaticEncodingProvider() { }

            public static void WithEncodingProvider(EncodingProvider instance, Action action)
            {
                EnsureProviderRegistered();

                EncodingProvider oldInstance = _staticInstance;
                try
                {
                    _staticInstance = instance;
                    action();
                }
                finally
                {
                    _staticInstance = oldInstance;
                }
            }

            private static void EnsureProviderRegistered()
            {
                if (!_globalRegistrationCompleted)
                {
                    lock (_globalRegistrationLockObj)
                    {
                        if (!_globalRegistrationCompleted)
                        {
                            Encoding.RegisterProvider(new ThreadStaticEncodingProvider());
                            _globalRegistrationCompleted = true;
                        }
                    }
                }
            }

            public override Encoding GetEncoding(int codepage)
                => _staticInstance?.GetEncoding(codepage);

            public override Encoding GetEncoding(int codepage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
                => _staticInstance?.GetEncoding(codepage, encoderFallback, decoderFallback);

            public override Encoding GetEncoding(string name)
                => _staticInstance?.GetEncoding(name);

            public override Encoding GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
                => _staticInstance?.GetEncoding(name, encoderFallback, decoderFallback);

            public override IEnumerable<EncodingInfo> GetEncodings()
                => _staticInstance?.GetEncodings() ?? Enumerable.Empty<EncodingInfo>();
        }
    }
}
