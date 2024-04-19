// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile : IDisposable
    {
        private readonly SafeMemoryMappedFileHandle _handle;
        private readonly bool _leaveOpen;
        private readonly SafeFileHandle? _fileHandle;
        internal const int DefaultSize = 0;

        // Private constructors to be used by the factory methods.
        private MemoryMappedFile(SafeMemoryMappedFileHandle handle)
        {
            Debug.Assert(handle != null);
            Debug.Assert(!handle.IsClosed);
            Debug.Assert(!handle.IsInvalid);

            _handle = handle;
            _leaveOpen = true; // No SafeFileHandle to dispose of in this case.
        }

        private MemoryMappedFile(SafeMemoryMappedFileHandle handle, SafeFileHandle fileHandle, bool leaveOpen)
        {
            Debug.Assert(handle != null);
            Debug.Assert(!handle.IsClosed);
            Debug.Assert(!handle.IsInvalid);
            Debug.Assert(fileHandle != null);

            _handle = handle;
            _fileHandle = fileHandle;
            _leaveOpen = leaveOpen;
        }

        // Factory Method Group #1: Opens an existing named memory mapped file. The native OpenFileMapping call
        // will check the desiredAccessRights against the ACL on the memory mapped file.  Note that a memory
        // mapped file created without an ACL will use a default ACL taken from the primary or impersonation token
        // of the creator.  On my machine, I always get ReadWrite access to it so I never have to use anything but
        // the first override of this method.  Note: having ReadWrite access to the object does not mean that we
        // have ReadWrite access to the pages mapping the file.  The OS will check against the access on the pages
        // when a view is created.
        [SupportedOSPlatform("windows")]
        public static MemoryMappedFile OpenExisting(string mapName)
        {
            return OpenExisting(mapName, MemoryMappedFileRights.ReadWrite, HandleInheritability.None);
        }

        [SupportedOSPlatform("windows")]
        public static MemoryMappedFile OpenExisting(string mapName, MemoryMappedFileRights desiredAccessRights)
        {
            return OpenExisting(mapName, desiredAccessRights, HandleInheritability.None);
        }

        [SupportedOSPlatform("windows")]
        public static MemoryMappedFile OpenExisting(string mapName, MemoryMappedFileRights desiredAccessRights,
                                                                    HandleInheritability inheritability)
        {
            ArgumentException.ThrowIfNullOrEmpty(mapName);

            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability));
            }

            if (((int)desiredAccessRights & ~((int)(MemoryMappedFileRights.FullControl | MemoryMappedFileRights.AccessSystemSecurity))) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredAccessRights));
            }

            SafeMemoryMappedFileHandle handle = OpenCore(mapName, inheritability, desiredAccessRights, false);
            return new MemoryMappedFile(handle);
        }

        // Factory Method Group #2: Creates a new memory mapped file where the content is taken from an existing
        // file on disk.  This file must be opened by a FileStream before given to us.  Specifying DefaultSize to
        // the capacity will make the capacity of the memory mapped file match the size of the file.  Specifying
        // a value larger than the size of the file will enlarge the new file to this size.  Note that in such a
        // case, the capacity (and there for the size of the file) will be rounded up to a multiple of the system
        // page size.  One can use FileStream.SetLength to bring the length back to a desirable size. By default,
        // the MemoryMappedFile will close the FileStream object when it is disposed.  This behavior can be
        // changed by the leaveOpen boolean argument.
        public static MemoryMappedFile CreateFromFile(string path)
        {
            return CreateFromFile(path, FileMode.Open, null, DefaultSize, MemoryMappedFileAccess.ReadWrite);
        }
        public static MemoryMappedFile CreateFromFile(string path, FileMode mode)
        {
            return CreateFromFile(path, mode, null, DefaultSize, MemoryMappedFileAccess.ReadWrite);
        }

        public static MemoryMappedFile CreateFromFile(string path, FileMode mode, string? mapName)
        {
            return CreateFromFile(path, mode, mapName, DefaultSize, MemoryMappedFileAccess.ReadWrite);
        }

        public static MemoryMappedFile CreateFromFile(string path, FileMode mode, string? mapName, long capacity)
        {
            return CreateFromFile(path, mode, mapName, capacity, MemoryMappedFileAccess.ReadWrite);
        }

        public static MemoryMappedFile CreateFromFile(string path, FileMode mode, string? mapName, long capacity,
                                                                        MemoryMappedFileAccess access)
        {
            ArgumentNullException.ThrowIfNull(path);
            ValidateCreateFile(mapName, capacity, access);

            if (mode == FileMode.Append)
            {
                throw new ArgumentException(SR.Argument_NewMMFAppendModeNotAllowed, nameof(mode));
            }
            if (mode == FileMode.Truncate)
            {
                throw new ArgumentException(SR.Argument_NewMMFTruncateModeNotAllowed, nameof(mode));
            }

            bool existed = mode switch
            {
                FileMode.Open => true, // FileStream ctor will throw if the file doesn't exist
                FileMode.CreateNew => false,
                _ => File.Exists(path)
            };
            SafeFileHandle fileHandle = File.OpenHandle(path, mode, GetFileAccess(access), FileShare.Read, FileOptions.None);
            long fileSize = 0;
            if (mode is not (FileMode.CreateNew or FileMode.Create)) // the file is brand new and it's empty
            {
                try
                {
                    fileSize = RandomAccess.GetLength(fileHandle);
                }
                catch
                {
                    fileHandle.Dispose();
                    throw;
                }
            }

            if (capacity == 0 && fileSize == 0)
            {
                CleanupFile(fileHandle, existed, path);
                throw new ArgumentException(SR.Argument_EmptyFile);
            }

            if (capacity == DefaultSize)
            {
                capacity = fileSize;
            }

            SafeMemoryMappedFileHandle? handle;
            try
            {
                handle = CreateCore(fileHandle, mapName, HandleInheritability.None,
                    access, MemoryMappedFileOptions.None, capacity, fileSize);
            }
            catch
            {
                CleanupFile(fileHandle, existed, path);
                throw;
            }

            Debug.Assert(handle != null);
            Debug.Assert(!handle.IsInvalid);
            return new MemoryMappedFile(handle, fileHandle, false);
        }

        /// <summary>
        /// Creates a memory-mapped file from an existing file using a <see cref="SafeFileHandle"/>,
        /// and the specified access mode, name, inheritability, and capacity.
        /// </summary>
        /// <param name="fileHandle">The <see cref="SafeFileHandle"/> to the existing file. Caller is
        /// responsible for disposing <paramref name="fileHandle"/> when <paramref name="leaveOpen"/> is <see langword="true" /> (otherwise,
        /// automatically disposed by the <see cref="MemoryMappedFile"/>). </param>
        /// <param name="mapName">A name to assign to the memory-mapped file, or <see langword="null" /> for a
        /// <see cref="MemoryMappedFile"/> that you do not intend to share across processes.</param>
        /// <param name="capacity">The maximum size, in bytes, to allocate to the memory-mapped file.
        /// Specify 0 to set the capacity to the size of the file.</param>
        /// <param name="access">One of the enumeration values that specifies the type of access allowed
        /// to the memory-mapped file.
        /// <para>This parameter can't be set to <see cref="MemoryMappedFileAccess.Write"/></para></param>
        /// <param name="inheritability">One of the enumeration values that specifies whether a handle
        /// to the memory-mapped file can be inherited by a child process. The default is <see cref="HandleInheritability.None"/>.</param>
        /// <param name="leaveOpen">A value that indicates whether to close the source file handle when
        /// the <see cref="MemoryMappedFile"/> is disposed.</param>
        /// <returns>A memory-mapped file that has the specified characteristics.</returns>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="mapName"/> is <see langword="null" /> or an empty string.</para>
        /// <para>-or-</para>
        /// <para><paramref name="capacity"/> and the length of the file are zero.</para>
        /// <para>-or-</para>
        /// <para><paramref name="access"/> is set to <see cref="MemoryMappedFileAccess.Write"/>, which is not allowed.</para>
        /// <para>-or-</para>
        /// <para><paramref name="access"/> is set to <see cref="MemoryMappedFileAccess.Read"/> and <paramref name="capacity"/> is larger than the length of the file.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="fileHandle"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="capacity"/> is less than zero.</para>
        /// <para>-or-</para>
        /// <para><paramref name="capacity"/> is less than the file size.</para>
        /// <para>-or-</para>
        /// <para><paramref name="access"/> is not a valid <see cref="MemoryMappedFileAccess"/> enumeration value.</para>
        /// <para>-or-</para>
        /// <para><paramref name="inheritability"/> is not a valid <see cref="HandleInheritability"/> enumeration value.</para>
        /// </exception>
        public static MemoryMappedFile CreateFromFile(SafeFileHandle fileHandle, string? mapName, long capacity,
                                                        MemoryMappedFileAccess access,
                                                        HandleInheritability inheritability, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(fileHandle);
            ValidateCreateFile(mapName, capacity, access);

            long fileSize = RandomAccess.GetLength(fileHandle);
            if (capacity == 0 && fileSize == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyFile);
            }

            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability));
            }

            if (capacity == DefaultSize)
            {
                capacity = fileSize;
            }

            SafeMemoryMappedFileHandle handle = CreateCore(fileHandle, mapName, inheritability,
                access, MemoryMappedFileOptions.None, capacity, fileSize);

            return new MemoryMappedFile(handle, fileHandle, leaveOpen);
        }

        public static MemoryMappedFile CreateFromFile(FileStream fileStream, string? mapName, long capacity,
                                                        MemoryMappedFileAccess access,
                                                        HandleInheritability inheritability, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(fileStream);
            ValidateCreateFile(mapName, capacity, access);

            long fileSize = fileStream.Length;
            if (capacity == 0 && fileSize == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyFile);
            }

            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability));
            }

            // flush any bytes written to the FileStream buffer so that we can see them in our MemoryMappedFile
            fileStream.Flush();

            if (capacity == DefaultSize)
            {
                capacity = fileSize;
            }

            SafeFileHandle fileHandle = fileStream.SafeFileHandle; // access the property only once (it might perform a sys-call)
            SafeMemoryMappedFileHandle handle = CreateCore(fileHandle, mapName, inheritability,
                access, MemoryMappedFileOptions.None, capacity, fileSize);

            return new MemoryMappedFile(handle, fileHandle, leaveOpen);
        }

        // Factory Method Group #3: Creates a new empty memory mapped file.  Such memory mapped files are ideal
        // for IPC, when mapName != null.
        public static MemoryMappedFile CreateNew(string? mapName, long capacity)
        {
            return CreateNew(mapName, capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None,
                   HandleInheritability.None);
        }

        public static MemoryMappedFile CreateNew(string? mapName, long capacity, MemoryMappedFileAccess access)
        {
            return CreateNew(mapName, capacity, access, MemoryMappedFileOptions.None,
                   HandleInheritability.None);
        }

        public static MemoryMappedFile CreateNew(string? mapName, long capacity, MemoryMappedFileAccess access,
                                                    MemoryMappedFileOptions options,
                                                    HandleInheritability inheritability)
        {
            if (mapName != null && mapName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_MapNameEmptyString);
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

            if (IntPtr.Size == 4 && capacity > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }

            if (access < MemoryMappedFileAccess.ReadWrite ||
                access > MemoryMappedFileAccess.ReadWriteExecute)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }

            if (access == MemoryMappedFileAccess.Write)
            {
                throw new ArgumentException(SR.Argument_NewMMFWriteAccessNotAllowed, nameof(access));
            }

            if (((int)options & ~((int)(MemoryMappedFileOptions.DelayAllocatePages))) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability));
            }

            SafeMemoryMappedFileHandle handle = CreateCore(null, mapName, inheritability, access, options, capacity, -1);
            return new MemoryMappedFile(handle);
        }

        // Factory Method Group #4: Creates a new empty memory mapped file or opens an existing
        // memory mapped file if one exists with the same name.  The capacity, options, and
        // memoryMappedFileSecurity arguments will be ignored in the case of the later.
        // This is ideal for P2P style IPC.
        [SupportedOSPlatform("windows")]
        public static MemoryMappedFile CreateOrOpen(string mapName, long capacity)
        {
            return CreateOrOpen(mapName, capacity, MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.None, HandleInheritability.None);
        }

        [SupportedOSPlatform("windows")]
        public static MemoryMappedFile CreateOrOpen(string mapName, long capacity,
                                                    MemoryMappedFileAccess access)
        {
            return CreateOrOpen(mapName, capacity, access, MemoryMappedFileOptions.None, HandleInheritability.None);
        }

        [SupportedOSPlatform("windows")]
        public static MemoryMappedFile CreateOrOpen(string mapName, long capacity,
                                                    MemoryMappedFileAccess access, MemoryMappedFileOptions options,
                                                    HandleInheritability inheritability)
        {
            ArgumentException.ThrowIfNullOrEmpty(mapName);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

            if (IntPtr.Size == 4 && capacity > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }

            if (access < MemoryMappedFileAccess.ReadWrite ||
                access > MemoryMappedFileAccess.ReadWriteExecute)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }

            if (((int)options & ~((int)(MemoryMappedFileOptions.DelayAllocatePages))) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability));
            }

            SafeMemoryMappedFileHandle handle;
            // special case for write access; create will never succeed
            if (access == MemoryMappedFileAccess.Write)
            {
                handle = OpenCore(mapName, inheritability, access, true);
            }
            else
            {
                handle = CreateOrOpenCore(mapName, inheritability, access, options, capacity);
            }
            return new MemoryMappedFile(handle);
        }

        // Creates a new view in the form of a stream.
        public MemoryMappedViewStream CreateViewStream()
        {
            return CreateViewStream(0, DefaultSize, MemoryMappedFileAccess.ReadWrite);
        }

        public MemoryMappedViewStream CreateViewStream(long offset, long size)
        {
            return CreateViewStream(offset, size, MemoryMappedFileAccess.ReadWrite);
        }

        public MemoryMappedViewStream CreateViewStream(long offset, long size, MemoryMappedFileAccess access)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(offset);

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), SR.ArgumentOutOfRange_PositiveOrDefaultSizeRequired);
            }

            if (access < MemoryMappedFileAccess.ReadWrite || access > MemoryMappedFileAccess.ReadWriteExecute)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }

            if (IntPtr.Size == 4 && size > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(size), SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }

            MemoryMappedView view = MemoryMappedView.CreateView(_handle, access, offset, size);
            return new MemoryMappedViewStream(view);
        }

        // Creates a new view in the form of an accessor.  Accessors are for random access.
        public MemoryMappedViewAccessor CreateViewAccessor()
        {
            return CreateViewAccessor(0, DefaultSize, MemoryMappedFileAccess.ReadWrite);
        }

        public MemoryMappedViewAccessor CreateViewAccessor(long offset, long size)
        {
            return CreateViewAccessor(offset, size, MemoryMappedFileAccess.ReadWrite);
        }

        public MemoryMappedViewAccessor CreateViewAccessor(long offset, long size, MemoryMappedFileAccess access)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(offset);

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), SR.ArgumentOutOfRange_PositiveOrDefaultSizeRequired);
            }

            if (access < MemoryMappedFileAccess.ReadWrite || access > MemoryMappedFileAccess.ReadWriteExecute)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }

            if (IntPtr.Size == 4 && size > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(size), SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }

            MemoryMappedView view = MemoryMappedView.CreateView(_handle, access, offset, size);
            return new MemoryMappedViewAccessor(view);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!_handle.IsClosed)
                {
                    _handle.Dispose();
                }
            }
            finally
            {
                if (!_leaveOpen)
                {
                    _fileHandle?.Dispose();
                }
            }
        }

        public SafeMemoryMappedFileHandle SafeMemoryMappedFileHandle
        {
            get { return _handle; }
        }

        // This converts a MemoryMappedFileAccess to a FileAccess. MemoryMappedViewStream and
        // MemoryMappedViewAccessor subclass UnmanagedMemoryStream and UnmanagedMemoryAccessor, which both use
        // FileAccess to determine whether they are writable and/or readable.
        internal static FileAccess GetFileAccess(MemoryMappedFileAccess access)
        {
            switch (access)
            {
                case MemoryMappedFileAccess.Read:
                case MemoryMappedFileAccess.ReadExecute:
                    return FileAccess.Read;

                case MemoryMappedFileAccess.ReadWrite:
                case MemoryMappedFileAccess.CopyOnWrite:
                case MemoryMappedFileAccess.ReadWriteExecute:
                    return FileAccess.ReadWrite;

                default:
                    Debug.Assert(access == MemoryMappedFileAccess.Write);
                    return FileAccess.Write;
            }
        }

        // clean up: close file handle and delete files we created
        private static void CleanupFile(SafeFileHandle fileHandle, bool existed, string path)
        {
            fileHandle.Dispose();
            if (!existed)
            {
                File.Delete(path);
            }
        }

        private static void ValidateCreateFile(string? mapName, long capacity, MemoryMappedFileAccess access)
        {
            if (mapName != null && mapName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_MapNameEmptyString);
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_PositiveOrDefaultCapacityRequired);
            }

            if (access < MemoryMappedFileAccess.ReadWrite ||
                access > MemoryMappedFileAccess.ReadWriteExecute)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }

            if (access == MemoryMappedFileAccess.Write)
            {
                throw new ArgumentException(SR.Argument_NewMMFWriteAccessNotAllowed, nameof(access));
            }
        }
    }
}
