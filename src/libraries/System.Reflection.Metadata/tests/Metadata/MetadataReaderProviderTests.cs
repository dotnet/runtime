// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class MetadataReaderProviderTests
    {
        private static BlobBuilder BuildMetadataImage()
        {
            var mdBuilder = new MetadataBuilder();
            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder, "v9.9.9.9");

            var builder = new BlobBuilder();
            rootBuilder.Serialize(builder, 0, 0);

            return builder;
        }

        [Fact]
        public unsafe void FromMetadataImage_BadArgs()
        {
            Assert.Throws<ArgumentNullException>(() => MetadataReaderProvider.FromMetadataImage(null, 10));

            Assert.Throws<ArgumentOutOfRangeException>(() => 
            {
                fixed (byte* p = new byte[] { 0 }) MetadataReaderProvider.FromMetadataImage(p, -1);
            });

            Assert.Throws<ArgumentNullException>(() => MetadataReaderProvider.FromMetadataImage(default(ImmutableArray<byte>)));
        }

        [Fact]
        public void FromMetadataStream1()
        {
            Assert.Throws<ArgumentNullException>(() => MetadataReaderProvider.FromMetadataStream(null, MetadataStreamOptions.Default));

            var invalid = new MemoryStream(new byte[] { 1, 2, 3, 4 });

            // the stream should not be disposed if the arguments are bad
            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataReaderProvider.FromMetadataStream(invalid, (MetadataStreamOptions)int.MaxValue));
            Assert.True(invalid.CanRead);

            // prefetching metadata doesn't create a reader yet, so no exception is thrown:
            var badReader = MetadataReaderProvider.FromMetadataStream(invalid, MetadataStreamOptions.PrefetchMetadata | MetadataStreamOptions.LeaveOpen);
            Assert.True(invalid.CanRead);
            invalid.Position = 0;
            Assert.Throws<BadImageFormatException>(() => badReader.GetMetadataReader());

            // valid metadata:
            var valid = new MemoryStream(PortablePdbs.DocumentsPdb);
            var PortablePdbReader = MetadataReaderProvider.FromMetadataStream(valid, MetadataStreamOptions.Default);
            Assert.True(valid.CanRead);
            PortablePdbReader.Dispose();
            Assert.False(valid.CanRead);
        }

        [Fact]
        public void FromMetadataStream2()
        {
            AssertExtensions.Throws<ArgumentException>("stream", () => MetadataReaderProvider.FromMetadataStream(new CustomAccessMemoryStream(canRead: false, canSeek: false, canWrite: false)));
            AssertExtensions.Throws<ArgumentException>("stream", () => MetadataReaderProvider.FromMetadataStream(new CustomAccessMemoryStream(canRead: true, canSeek: false, canWrite: false)));
            MetadataReaderProvider.FromMetadataStream(new CustomAccessMemoryStream(canRead: true, canSeek: true, canWrite: false));
        }

        [Fact]
        public void GetMetadataReader_EmptyStream()
        {
            var provider = MetadataReaderProvider.FromMetadataStream(new MemoryStream(), MetadataStreamOptions.PrefetchMetadata);
            Assert.Throws<BadImageFormatException>(() => provider.GetMetadataReader());
        }

        [Fact]
        public void GetMetadataReader_FromMetadataImage()
        {
            TestGetMetadataReader(MetadataReaderProvider.FromMetadataImage(BuildMetadataImage().ToImmutableArray()));
        }

        [Fact]
        public void GetMetadataReader_FromMetadataStream()
        {
            TestGetMetadataReader(MetadataReaderProvider.FromMetadataStream(new MemoryStream(BuildMetadataImage().ToArray())));
        }

        private unsafe void TestGetMetadataReader(MetadataReaderProvider provider)
        {
            var decoder = new TestMetadataStringDecoder(Encoding.UTF8, (a, b) => "str");

            var reader1 = provider.GetMetadataReader(MetadataReaderOptions.None, decoder);
            Assert.Equal("str", reader1.MetadataVersion);
            Assert.Same(reader1.UTF8Decoder, decoder);
            Assert.Equal(reader1.Options, MetadataReaderOptions.None);

            var reader2 = provider.GetMetadataReader(MetadataReaderOptions.None, decoder);
            Assert.Same(reader1, reader2);

            var reader3 = provider.GetMetadataReader(MetadataReaderOptions.None);
            Assert.NotSame(reader2, reader3);
            Assert.Equal("v9.9.9.9", reader3.MetadataVersion);
            Assert.Same(reader3.UTF8Decoder, MetadataStringDecoder.DefaultUTF8);
            Assert.Equal(reader3.Options, MetadataReaderOptions.None);

            var reader4 = provider.GetMetadataReader(MetadataReaderOptions.None);
            Assert.Same(reader3, reader4);

            var reader5 = provider.GetMetadataReader(MetadataReaderOptions.ApplyWindowsRuntimeProjections);
            Assert.NotSame(reader4, reader5);
            Assert.Equal("v9.9.9.9", reader5.MetadataVersion);
            Assert.Same(reader5.UTF8Decoder, MetadataStringDecoder.DefaultUTF8);
            Assert.Equal(reader5.Options, MetadataReaderOptions.ApplyWindowsRuntimeProjections);

            provider.Dispose();
            Assert.Throws<ObjectDisposedException>(() => provider.GetMetadataReader(MetadataReaderOptions.ApplyWindowsRuntimeProjections));
            Assert.Throws<ObjectDisposedException>(() => provider.GetMetadataReader());
        }

        [Fact]
        [ActiveIssue(7996)]
        public void FromMetadataStream_NonZeroStart()
        {
            var stream = new MemoryStream();
            stream.WriteByte(0xff);
            stream.Write(PortablePdbs.DocumentsPdb, 0, PortablePdbs.DocumentsPdb.Length);
            stream.WriteByte(0xff);
            stream.WriteByte(0xff);

            stream.Position = 1;
            var PortablePdbReader1 = MetadataReaderProvider.FromMetadataStream(stream, MetadataStreamOptions.LeaveOpen, PortablePdbs.DocumentsPdb.Length);

            Assert.Equal(PortablePdbs.DocumentsPdb.Length, PortablePdbReader1.GetMetadataReader().Block.Length);
            var reader1 = PortablePdbReader1.GetMetadataReader();
            Assert.Equal(13, reader1.Documents.Count);

            stream.Position = 1;
            var PortablePdbReader2 = MetadataReaderProvider.FromMetadataStream(stream, MetadataStreamOptions.LeaveOpen | MetadataStreamOptions.PrefetchMetadata, PortablePdbs.DocumentsPdb.Length);

            Assert.Equal(PortablePdbs.DocumentsPdb.Length, PortablePdbReader2.GetMetadataReader().Block.Length);
            var reader2 = PortablePdbReader2.GetMetadataReader();
            Assert.Equal(13, reader2.Documents.Count);
        }
    }
}
