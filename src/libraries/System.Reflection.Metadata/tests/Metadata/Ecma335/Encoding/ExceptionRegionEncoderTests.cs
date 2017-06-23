// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class ExceptionRegionEncoderTests
    {
        [Fact]
        public void IsSmallRegionCount()
        {
            Assert.True(ExceptionRegionEncoder.IsSmallRegionCount(0));
            Assert.True(ExceptionRegionEncoder.IsSmallRegionCount(20));
            Assert.False(ExceptionRegionEncoder.IsSmallRegionCount(-1));
            Assert.False(ExceptionRegionEncoder.IsSmallRegionCount(21));
            Assert.False(ExceptionRegionEncoder.IsSmallRegionCount(int.MinValue));
            Assert.False(ExceptionRegionEncoder.IsSmallRegionCount(int.MaxValue));
        }

        [Fact]
        public void IsSmallExceptionRegion()
        {
            Assert.True(ExceptionRegionEncoder.IsSmallExceptionRegion(0, 0));
            Assert.True(ExceptionRegionEncoder.IsSmallExceptionRegion(ushort.MaxValue, byte.MaxValue));

            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(ushort.MaxValue + 1, byte.MaxValue));
            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(ushort.MaxValue, byte.MaxValue + 1));

            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(-1, 0));
            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(0, -1));
            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(int.MinValue, int.MinValue));
            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(int.MaxValue, int.MaxValue));
            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(int.MaxValue, int.MinValue));
            Assert.False(ExceptionRegionEncoder.IsSmallExceptionRegion(int.MinValue, int.MaxValue));
        }

        [Fact]
        public void SerializeTableHeader()
        {
            var builder = new BlobBuilder();

            builder.WriteByte(0xff);
            ExceptionRegionEncoder.SerializeTableHeader(builder, ExceptionRegionEncoder.MaxSmallExceptionRegions, hasSmallRegions: true);
            AssertEx.Equal(new byte[] 
            {
                0xff, 0x00, 0x00, 0x00, // padding
                0x01, // flags
                0xf4, // size
                0x00, 0x00
            }, builder.ToArray());
            builder.Clear();

            builder.WriteByte(0xff);
            ExceptionRegionEncoder.SerializeTableHeader(builder, ExceptionRegionEncoder.MaxExceptionRegions, hasSmallRegions: false);
            AssertEx.Equal(new byte[]
            {
                0xff, 0x00, 0x00, 0x00, // padding
                0x41, // flags
                0xf4, 0xff, 0xff, // size
            }, builder.ToArray());
        }

        [Fact]
        public void Add_Small()
        {
            var builder = new BlobBuilder();
            var encoder = new ExceptionRegionEncoder(builder, hasSmallFormat: true);

            encoder.Add(ExceptionRegionKind.Catch, 1, 2, 4, 5, catchType: MetadataTokens.TypeDefinitionHandle(1));

            AssertEx.Equal(new byte[]
            {
                0x00, 0x00,            // kind
                0x01, 0x00,            // try offset
                0x02,                  // try length
                0x04, 0x00,            // handler offset
                0x05,                  // handler length
                0x01, 0x00, 0x00, 0x02 // catch type
            }, builder.ToArray());
            builder.Clear();

            encoder.Add(ExceptionRegionKind.Filter, 0xffff, 0xff, 0xffff, 0xff, filterOffset: int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x00,            // kind
                0xff, 0xff,            // try offset
                0xff,                  // try length
                0xff, 0xff,            // handler offset
                0xff,                  // handler length
                0xff, 0xff, 0xff, 0x7f // filter offset
            }, builder.ToArray());
            builder.Clear();

            encoder.Add(ExceptionRegionKind.Fault, 0xffff, 0xff, 0xffff, 0xff);

            AssertEx.Equal(new byte[]
            {
                0x04, 0x00,            // kind
                0xff, 0xff,            // try offset
                0xff,                  // try length
                0xff, 0xff,            // handler offset
                0xff,                  // handler length
                0x00, 0x00, 0x00, 0x00
            }, builder.ToArray());
            builder.Clear();

            encoder.Add(ExceptionRegionKind.Finally, 0, 0, 0, 0);

            AssertEx.Equal(new byte[]
            {
                0x02, 0x00,            // kind
                0x00, 0x00,            // try offset
                0x00,                  // try length
                0x00, 0x00,            // handler offset
                0x00,                  // handler length
                0x00, 0x00, 0x00, 0x00
            }, builder.ToArray());
            builder.Clear();
        }

        [Fact]
        public void Add_Large()
        {
            var builder = new BlobBuilder();
            var encoder = new ExceptionRegionEncoder(builder, hasSmallFormat: false);

            encoder.Add(ExceptionRegionKind.Catch, 1, 2, 4, 5, catchType: MetadataTokens.TypeDefinitionHandle(1));

            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // kind
                0x01, 0x00, 0x00, 0x00, // try offset
                0x02, 0x00, 0x00, 0x00, // try length
                0x04, 0x00, 0x00, 0x00, // handler offset
                0x05, 0x00, 0x00, 0x00, // handler length
                0x01, 0x00, 0x00, 0x02  // catch type
            }, builder.ToArray());
            builder.Clear();

            encoder.Add(ExceptionRegionKind.Filter, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, filterOffset: int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x00, 0x00, 0x00, // kind
                0xff, 0xff, 0xff, 0x7f, // try offset
                0xff, 0xff, 0xff, 0x7f, // try length
                0xff, 0xff, 0xff, 0x7f, // handler offset
                0xff, 0xff, 0xff, 0x7f, // handler length
                0xff, 0xff, 0xff, 0x7f // filter offset
            }, builder.ToArray());
            builder.Clear();

            encoder.Add(ExceptionRegionKind.Fault, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                0x04, 0x00, 0x00, 0x00,  // kind
                0xff, 0xff, 0xff, 0x7f,  // try offset
                0xff, 0xff, 0xff, 0x7f,  // try length
                0xff, 0xff, 0xff, 0x7f,  // handler offset
                0xff, 0xff, 0xff, 0x7f,  // handler length
                0x00, 0x00, 0x00, 0x00
            }, builder.ToArray());
            builder.Clear();

            encoder.Add(ExceptionRegionKind.Finally, 0, 0, 0, 0);

            AssertEx.Equal(new byte[]
            {
                0x02, 0x00, 0x00, 0x00, // kind
                0x00, 0x00, 0x00, 0x00, // try offset
                0x00, 0x00, 0x00, 0x00, // try length
                0x00, 0x00, 0x00, 0x00, // handler offset
                0x00, 0x00, 0x00, 0x00, // handler length
                0x00, 0x00, 0x00, 0x00
            }, builder.ToArray());
            builder.Clear();
        }

        [Fact]
        public void Add_Errors()
        {
            Assert.Throws<InvalidOperationException>(() => default(ExceptionRegionEncoder).Add(ExceptionRegionKind.Fault, 0, 0, 0, 0));
        
            var builder = new BlobBuilder();
            var smallEncoder = new ExceptionRegionEncoder(builder, hasSmallFormat: true);
            var fatEncoder = new ExceptionRegionEncoder(builder, hasSmallFormat: false);

            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, -1, 2, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, 1, -1, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, 1, 2, -1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, 1, 2, 4, -1));

            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, 0x10000, 2, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, 1, 0x100, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, 1, 2, 0x10000, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => smallEncoder.Add(ExceptionRegionKind.Finally, 1, 2, 4, 0x100));

            Assert.Throws<ArgumentOutOfRangeException>(() => fatEncoder.Add(ExceptionRegionKind.Finally, -1, 2, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => fatEncoder.Add(ExceptionRegionKind.Finally, 1, -1, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => fatEncoder.Add(ExceptionRegionKind.Finally, 1, 2, -1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => fatEncoder.Add(ExceptionRegionKind.Finally, 1, 2, 4, -1));

            Assert.Throws<ArgumentOutOfRangeException>(() => fatEncoder.Add((ExceptionRegionKind)5, 1, 2, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => fatEncoder.Add(ExceptionRegionKind.Filter, 1, 2, 4, 5, filterOffset: -1));
            AssertExtensions.Throws<ArgumentException>("catchType", () => fatEncoder.Add(ExceptionRegionKind.Catch, 1, 2, 4, 5, catchType: default(EntityHandle)));
            AssertExtensions.Throws<ArgumentException>("catchType", () => fatEncoder.Add(ExceptionRegionKind.Catch, 1, 2, 4, 5, catchType: MetadataTokens.ImportScopeHandle(1)));
        }
    }
}
