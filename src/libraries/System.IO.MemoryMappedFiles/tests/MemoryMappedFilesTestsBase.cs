// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>Base class from which all of the memory mapped files test classes derive.</summary>
    public abstract partial class MemoryMappedFilesTestBase : FileCleanupTestBase
    {
        /// <summary>Gets whether named maps are supported by the current platform.</summary>
        protected static bool MapNamesSupported { get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows); } }

        /// <summary>Creates a map name guaranteed to be unique.</summary>
        protected static string CreateUniqueMapName() { return Guid.NewGuid().ToString("N"); }
        
        /// <summary>Creates a map name guaranteed to be unique and contain only whitespace characters.</summary>
        protected static string CreateUniqueWhitespaceMapName()
        {
            var data = Guid.NewGuid().ToByteArray();
            var sb = StringBuilderCache.Acquire(data.Length * 4);
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                sb.Append(s_fourWhitespaceCharacters[b & 0x3]);
                sb.Append(s_fourWhitespaceCharacters[(b & 0xC) >> 2]);
                sb.Append(s_fourWhitespaceCharacters[(b & 0x30) >> 4]);
                sb.Append(s_fourWhitespaceCharacters[(b & 0xC0) >> 6]);
            }
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>An array of four whitespace characters.</summary>
        private static readonly char[] s_fourWhitespaceCharacters = { ' ', '\t', '\r', '\n' };

        /// <summary>Creates a set of valid map names to use to test various map creation scenarios.</summary>
        public static IEnumerable<object[]> CreateValidMapNames() // often used with [MemberData]
        {
            // Normal name
            yield return new object[] { CreateUniqueMapName() };

            // Name that's entirely whitespace
            yield return new object[] { CreateUniqueWhitespaceMapName() };

            // Names with prefixes recognized by Windows
            yield return new object[] { "local/" + CreateUniqueMapName() };
            yield return new object[] { "global/" + CreateUniqueMapName() };

            // Very long name
            yield return new object[] { CreateUniqueMapName() + new string('a', 1000) };
        }

        /// <summary>
        /// Creates and yields a variety of different maps, suitable for a wide range of testing of
        /// views created from maps.
        /// </summary>
        protected IEnumerable<MemoryMappedFile> CreateSampleMaps(
            int capacity = 4096, MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWrite,
            [CallerMemberName]string fileName = null, [CallerLineNumber] int lineNumber = 0)
        {
            yield return MemoryMappedFile.CreateNew(null, capacity, access);
            yield return MemoryMappedFile.CreateFromFile(Path.Combine(TestDirectory, Guid.NewGuid().ToString("N")), FileMode.CreateNew, null, capacity, access);
            if (MapNamesSupported)
            {
                yield return MemoryMappedFile.CreateNew(CreateUniqueMapName(), capacity, access);
                yield return MemoryMappedFile.CreateFromFile(GetTestFilePath(null, fileName, lineNumber), FileMode.CreateNew, CreateUniqueMapName(), capacity, access);
            }
        }

        /// <summary>Performs basic verification on a map.</summary>
        /// <param name="mmf">The map.</param>
        /// <param name="expectedCapacity">The capacity that was specified to create the map.</param>
        /// <param name="expectedAccess">The access specified to create the map.</param>
        /// <param name="expectedInheritability">The inheritability specified to create the map.</param>
        protected static void ValidateMemoryMappedFile(MemoryMappedFile mmf, 
            long expectedCapacity, 
            MemoryMappedFileAccess expectedAccess = MemoryMappedFileAccess.ReadWrite,
            HandleInheritability expectedInheritability = HandleInheritability.None)
        {
            // Validate that we got a MemoryMappedFile object and that its handle is valid
            Assert.NotNull(mmf);
            Assert.NotNull(mmf.SafeMemoryMappedFileHandle);
            Assert.Same(mmf.SafeMemoryMappedFileHandle, mmf.SafeMemoryMappedFileHandle);
            Assert.False(mmf.SafeMemoryMappedFileHandle.IsClosed);
            Assert.False(mmf.SafeMemoryMappedFileHandle.IsInvalid);
            AssertInheritability(mmf.SafeMemoryMappedFileHandle, expectedInheritability);

            // Create and validate one or more views from the map
            if (IsReadable(expectedAccess) && IsWritable(expectedAccess))
            {
                CreateAndValidateViews(mmf, expectedCapacity, MemoryMappedFileAccess.Read);
                CreateAndValidateViews(mmf, expectedCapacity, MemoryMappedFileAccess.Write);
                CreateAndValidateViews(mmf, expectedCapacity, MemoryMappedFileAccess.ReadWrite);
            }
            else if (IsWritable(expectedAccess))
            {
                CreateAndValidateViews(mmf, expectedCapacity, MemoryMappedFileAccess.Write);
            }
            else if (IsReadable(expectedAccess))
            {
                CreateAndValidateViews(mmf, expectedCapacity, MemoryMappedFileAccess.Read);
            }
            else
            {
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(0, expectedCapacity, MemoryMappedFileAccess.Read));
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(0, expectedCapacity, MemoryMappedFileAccess.Write));
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(0, expectedCapacity, MemoryMappedFileAccess.ReadWrite));
            }
        }

        /// <summary>Creates and validates a view accessor and a view stream from the map.</summary>
        /// <param name="mmf">The map.</param>
        /// <param name="capacity">The capacity to use when creating the view.</param>
        /// <param name="access">The access to use when creating the view.</param>
        private static void CreateAndValidateViews(MemoryMappedFile mmf, long capacity, MemoryMappedFileAccess access)
        {
            using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, capacity, access))
            {
                ValidateMemoryMappedViewAccessor(accessor, capacity, access);
            }
            using (MemoryMappedViewStream stream = mmf.CreateViewStream(0, capacity, access))
            {
                ValidateMemoryMappedViewStream(stream, capacity, access);
            }
        }

        /// <summary>Performs validation on a view accessor.</summary>
        /// <param name="accessor">The accessor to validate.</param>
        /// <param name="capacity">The capacity specified when creating the accessor.</param>
        /// <param name="access">The access specified when creating the accessor.</param>
        protected static void ValidateMemoryMappedViewAccessor(MemoryMappedViewAccessor accessor, long capacity, MemoryMappedFileAccess access)
        {
            // Validate the accessor and its handle
            Assert.NotNull(accessor);
            Assert.NotNull(accessor.SafeMemoryMappedViewHandle);
            Assert.Same(accessor.SafeMemoryMappedViewHandle, accessor.SafeMemoryMappedViewHandle);

            // Ensure its properties match the criteria specified when it was created
            Assert.InRange(capacity, 0, accessor.Capacity); // the capacity may be rounded up to page size, so all we guarantee is that the accessor's capacity >= capacity
            Assert.Equal(0, accessor.PointerOffset);

            // If it's supposed to be readable, try to read from it.
            // Otherwise, verify we can't.
            if (IsReadable(access))
            {
                Assert.True(accessor.CanRead);
                Assert.Equal(0, accessor.ReadByte(0));
                Assert.Equal(0, accessor.ReadByte(capacity - 1));
            }
            else
            {
                Assert.False(accessor.CanRead);
                Assert.Throws<NotSupportedException>(() => accessor.ReadByte(0));
            }

            // If it's supposed to be writable, try to write to it
            if (IsWritable(access) || access == MemoryMappedFileAccess.CopyOnWrite)
            {
                Assert.True(accessor.CanWrite);

                // Write some data
                accessor.Write(0, (byte)42);
                accessor.Write(capacity - 1, (byte)42);

                // If possible, ensure we can read it back
                if (IsReadable(access))
                {
                    Assert.Equal(42, accessor.ReadByte(0));
                    Assert.Equal(42, accessor.ReadByte(capacity - 1));
                }

                // Write 0 back where we wrote data
                accessor.Write(0, (byte)0);
                accessor.Write(capacity - 1, (byte)0);
            }
            else
            {
                Assert.False(accessor.CanWrite);
                Assert.Throws<NotSupportedException>(() => accessor.Write(0, (byte)0));
            }
        }

        /// <summary>Performs validation on a view stream.</summary>
        /// <param name="stream">The stream to verify.</param>
        /// <param name="capacity">The capacity specified when the stream was created.</param>
        /// <param name="access">The access specified when the stream was created.</param>
        protected static void ValidateMemoryMappedViewStream(MemoryMappedViewStream stream, long capacity, MemoryMappedFileAccess access)
        {
            // Validate the stream and its handle
            Assert.NotNull(stream);
            Assert.NotNull(stream.SafeMemoryMappedViewHandle);
            Assert.Same(stream.SafeMemoryMappedViewHandle, stream.SafeMemoryMappedViewHandle);

            // Validate its properties report the values they should
            Assert.InRange(capacity, 0, stream.Length); // the capacity may be rounded up to page size, so all we guarantee is that the stream's length >= capacity

            // If it's supposed to be readable, read from it.
            if (IsReadable(access))
            {
                Assert.True(stream.CanRead);

                // Seek to the beginning
                stream.Position = 0;
                Assert.Equal(0, stream.Position);

                // Read a byte
                Assert.Equal(0, stream.ReadByte());
                Assert.Equal(1, stream.Position);

                // Seek to just before the end
                Assert.Equal(capacity - 1, stream.Seek(capacity - 1, SeekOrigin.Begin));

                // Read another byte
                Assert.Equal(0, stream.ReadByte());
                Assert.Equal(capacity, stream.Position);
            }
            else
            {
                Assert.False(stream.CanRead);
            }

            // If it's supposed to be writable, try to write to it.
            if (IsWritable(access) || access == MemoryMappedFileAccess.CopyOnWrite)
            {
                Assert.True(stream.CanWrite);

                // Seek to the beginning, write a byte, seek to the almost end, write a byte
                stream.Position = 0;
                stream.WriteByte(42);
                stream.Position = stream.Length - 1;
                stream.WriteByte(42);

                // Verify the written bytes if possible
                if (IsReadable(access))
                {
                    stream.Position = 0;
                    Assert.Equal(42, stream.ReadByte());
                    stream.Position = stream.Length - 1;
                    Assert.Equal(42, stream.ReadByte());
                }

                // Reset the written bytes
                stream.Position = 0;
                stream.WriteByte(0);
                stream.Position = stream.Length - 1;
                stream.WriteByte(0);
            }
            else
            {
                Assert.False(stream.CanWrite);
            }
        }

        /// <summary>Gets whether the specified access implies writability.</summary>
        protected static bool IsWritable(MemoryMappedFileAccess access)
        {
            switch (access)
            {
                case MemoryMappedFileAccess.Write:
                case MemoryMappedFileAccess.ReadWrite:
                case MemoryMappedFileAccess.ReadWriteExecute:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Gets whether the specified access implies readability.</summary>
        protected static bool IsReadable(MemoryMappedFileAccess access)
        {
            switch (access)
            {
                case MemoryMappedFileAccess.CopyOnWrite:
                case MemoryMappedFileAccess.Read:
                case MemoryMappedFileAccess.ReadExecute:
                case MemoryMappedFileAccess.ReadWrite:
                case MemoryMappedFileAccess.ReadWriteExecute:
                    return true;
                default:
                    return false;
            }
        }
    }
}
