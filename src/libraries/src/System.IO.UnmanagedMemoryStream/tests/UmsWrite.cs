// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.Tests
{
    public class UmsWriteTests
    {
        // TODO: add tests for different offsets and lengths
        [Fact]
        public static void Write()
        {
            var length = 1000;
            using (var manager = new UmsManager(FileAccess.Write, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.WriteUmsInvariants(stream);
                Assert.Equal(stream.Length, length);

                var bytes = ArrayHelpers.CreateByteArray(length);
                stream.Write(bytes.Copy(), 0, length);

                var memory = manager.ToArray();

                Assert.True(ArrayHelpers.Comparer<byte>().Equals(bytes, memory));

                stream.Write(new byte[0], 0, 0);
            }
        }

        [Fact]
        public static void WriteByte()
        {
            var length = 1000;
            using (var manager = new UmsManager(FileAccess.Write, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.WriteUmsInvariants(stream);

                var bytes = ArrayHelpers.CreateByteArray(length);

                for (int index = 0; index < bytes.Length; index++)
                {
                    stream.WriteByte(bytes[index]);
                }

                var memory = manager.ToArray();

                Assert.True(ArrayHelpers.Comparer<byte>().Equals(bytes, memory));
            }
        }

        [Fact]
        public static void CannotWriteToReadStream()
        {
            using (var manager = new UmsManager(FileAccess.Read, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.ReadUmsInvariants(stream);

                var bytes = new byte[3];
                Assert.Throws<NotSupportedException>(() => stream.Write(bytes, 0, bytes.Length));
                Assert.Throws<NotSupportedException>(() => stream.WriteByte(1));
            }
        }

        [Fact]
        public static void CannotWriteWithOverflow()
        {
            using (var manager = new UmsManager(FileAccess.Write, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.WriteUmsInvariants(stream);
                stream.Position = long.MaxValue;

                var bytes = new byte[3];
                Assert.Throws<IOException>(() => stream.Write(bytes, 0, bytes.Length));
                Assert.Throws<IOException>(() => stream.WriteByte(1));
            }
        }
    }
}
