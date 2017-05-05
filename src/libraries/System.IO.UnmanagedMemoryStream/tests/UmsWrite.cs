// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class UmsWriteTests
    {
        [Fact]
        public static void Write()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.Write, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                UmsTests.WriteUmsInvariants(stream);
                Assert.Equal(stream.Length, length);

                var bytes = ArrayHelpers.CreateByteArray(length);
                stream.Write(bytes.Copy(), 0, length);
                var memory = manager.ToArray();
                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());

                stream.Write(new byte[0], 0, 0);

                stream.SetLength(1);
                Assert.Equal(1, stream.Length);
                stream.SetLength(4);
                Assert.Equal(4, stream.Length);
                stream.SetLength(0);
                Assert.Equal(0, stream.Length);

                stream.Position = 1;
                bytes = ArrayHelpers.CreateByteArray(length - 1);
                stream.Write(bytes, 0, length - 1);
                memory = manager.ToArray();
                for (int i = 0; i < bytes.Length; i++)
                {
                    Assert.Equal(bytes[i], memory[i + 1]);
                }
                
                Assert.True(stream.WriteAsync(bytes, 0, bytes.Length, new CancellationToken(true)).IsCanceled);

                stream.Position = 0;
                bytes = ArrayHelpers.CreateByteArray(length);
                for (int i = 0; i < 4; i++)
                {
                    Task t = stream.WriteAsync(bytes, i * (bytes.Length / 4), bytes.Length / 4);
                    Assert.True(t.Status == TaskStatus.RanToCompletion);
                }
                Assert.Equal(bytes, manager.ToArray(), ArrayHelpers.Comparer<byte>());
            }
        }

        [Fact]
        public static void WriteByte()
        {
            const int length = 1000;
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
                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());

                stream.SetLength(0);
                stream.Position = 1;
                bytes = ArrayHelpers.CreateByteArray(length);
                for (int index = 1; index < bytes.Length; index++)
                {
                    stream.WriteByte(bytes[index]);
                }
                stream.Position = 0;
                stream.WriteByte(bytes[0]);
                memory = manager.ToArray();
                Assert.Equal(bytes, memory, ArrayHelpers.Comparer<byte>());
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
