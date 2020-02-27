// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    public static class MemoryMappedFileAcl
    {
        /// <summary>
        /// Creates a memory-mapped file from an existing file with the specified access mode, name, inheritability, capacity and security permissions.
        /// </summary>
        /// <param name="fileStream">The file stream of the existing file.</param>
        /// <param name="mapName">A name to assign to the memory-mapped file, or <see langword="null" /> for a <see cref="MemoryMappedFile" /> that you do not intend to share across processes.</param>
        /// <param name="capacity">The maximum size, in bytes, to allocate to the memory-mapped file. Specify 0 to set the capacity to the size of the <paramref name="fileStream" />.</param>
        /// <param name="access">One of the enumeration values that specifies the type of access allowed to the memory-mapped file.
        /// This parameter can't be set to <see cref="MemoryMappedFileAccess.Write" />.</param>
        /// <param name="memoryMappedFileSecurity">The permissions that can be granted for file access and operations on memory-mapped files.
        /// This parameter cannot be <see langword= "null" />.</param>
        /// <param name="inheritability">One of the enumeration values that specifies whether a handle to the memory-mapped file can be inherited by a child process.
        /// The default is <see cref="HandleInheritability.None" />.</param>
        /// <param name="leaveOpen">If set to <see langword="true" />, the <paramref name="fileStream" /> will be left open after the current memory mapped file instance is disposed; if set to <see langword="false" />, the <paramref name="fileStream" /> will be closed when the current memory mapped file instance is disposed.</param>
        /// <returns>A memory-mapped file that has the specified characteristics.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileStream" /> or <paramref name="memoryMappedFileSecurity" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="mapName" /> is <see langword="null" /> or empty.
        /// -or-
        /// The file is empty: <paramref name="capacity" /> is 0 and the length of <paramref name="fileStream" /> is 0.
        /// -or-
        /// <paramref name="access"/> is <see cref="MemoryMappedFileAccess.Write" />.
        /// -or-
        /// <paramref name="access"/> is <see cref="MemoryMappedFileAccess.Read" /> and the <paramref name="capacity" /> is larger than the length of <paramref name="fileStream" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.
        /// -or-
        /// <paramref name="access" /> or <paramref name="inheritability" /> is not a valid enumeration value.
        /// -or-
        /// The capacity may not be smaller than the file size.</exception>
        /// <remarks>
        /// If `capacity` is larger than the size of the file on disk, the file on disk is increased to match the specified capacity even if no data is written to the memory-mapped file. To prevent this from occurring, specify 0 (zero) for the default capacity, which will internally set `capacity` to the size of the file on disk.
        /// </remarks>
        public static unsafe MemoryMappedFile CreateFromFile(
            FileStream fileStream,
            string? mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability,
            bool leaveOpen)
        {
            long updatedCapacity = MemoryMappedFileInternal.VerifyParametersCreateFromFile(fileStream, mapName, capacity, access, inheritability);

            if (memoryMappedFileSecurity == null)
            {
                throw new ArgumentNullException(nameof(memoryMappedFileSecurity));
            }

            fixed (byte* pSecurityDescriptor = memoryMappedFileSecurity.GetSecurityDescriptorBinaryForm())
            {
                SafeMemoryMappedFileHandle handle = MemoryMappedFileInternal.CreateCore(fileStream, mapName, access, MemoryMappedFileOptions.None, updatedCapacity, MemoryMappedFileInternal.GetSecAttrs(pSecurityDescriptor, inheritability));

                IDisposable disposable = leaveOpen ?
                    new MemoryMappedFileInternal.FileStreamRooter(fileStream) :
                    (IDisposable)fileStream;

                return new MemoryMappedFile(handle, disposable);
            }
        }

        /// <summary>
        /// Creates a memory-mapped file that has the specified capacity, access type, memory allocation, security permissions, and inheritability in system memory.
        /// </summary>
        /// <param name="mapName">A name to assign to the memory-mapped file, or <see langword="null" /> for a <see cref="MemoryMappedFile" /> that you do not intend to share across processes.</param>
        /// <param name="capacity">The maximum size, in bytes, to allocate to the memory-mapped file.</param>
        /// <param name="access">One of the enumeration values that specifies the type of access allowed to the memory-mapped file. The default is <see cref="MemoryMappedFileAccess.ReadWrite" />.</param>
        /// <param name="options">A bitwise combination of enumeration values that specifies memory allocation options for the memory-mapped file.</param>
        /// <param name="memoryMappedFileSecurity">The permissions that can be granted for file access and operations on memory-mapped files.
        /// This parameter cannot be <see langword="null" />.</param>
        /// <param name="inheritability">One of the enumeration values that specifies whether a handle to the memory-mapped file can be inherited by a child process.</param>
        /// <returns>A memory-mapped file that has the specified characteristics.</returns>
        /// <exception cref="ArgumentException"><paramref name="mapName" /> is <see langword="null" /> or empty.
        /// -or-
        /// <paramref name="access" /> is set to <see cref="MemoryMappedFileAccess.Write" />, which is not permitted when creating new memory mapped files. Use <see cref="MemoryMappedFileAccess.ReadWrite" /> instead.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is not a positive number.
        /// -or-
        /// The capacity cannot be greater than the size of the system's logical address space.
        /// -or-
        /// <paramref name="access" /> or <paramref name="options" /> or <paramref name="inheritability" /> is not a valid enumeration value.</exception>
        /// <remarks>Use this method to create a memory-mapped file that is not persisted (that is, not associated with a file on disk), which you can use to share data between processes.</remarks>
        public static unsafe MemoryMappedFile CreateNew(
            string mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability)
        {
            MemoryMappedFileInternal.VerifyParametersCreateNew(mapName, capacity, access, options, inheritability);

            if (memoryMappedFileSecurity == null)
            {
                throw new ArgumentNullException(nameof(memoryMappedFileSecurity));
            }

            fixed (byte* pSecurityDescriptor = memoryMappedFileSecurity.GetSecurityDescriptorBinaryForm())
            {
                SafeMemoryMappedFileHandle handle = MemoryMappedFileInternal.CreateCore(null, mapName, access, options, capacity, MemoryMappedFileInternal.GetSecAttrs(pSecurityDescriptor, inheritability));
                return new MemoryMappedFile(handle);
            }
        }

        /// <summary>
        /// Creates or opens a memory-mapped file that has the specified name, capacity, access type, memory allocation, security permissions, and inheritability in system memory.
        /// </summary>
        /// <param name="mapName">The name of the memory-mapped file.</param>
        /// <param name="capacity">The maximum size, in bytes, to allocate to the memory-mapped file.</param>
        /// <param name="access">One of the enumeration values that specifies the type of access allowed to the memory-mapped file. The default is <see cref="MemoryMappedFileAccess.ReadWrite" />.</param>
        /// <param name="options">A bitwise combination of enumeration values that specifies memory allocation options for the memory-mapped file.</param>
        /// <param name="memoryMappedFileSecurity">The permissions that can be granted for file access and operations on memory-mapped files. This parameter cannot be <see langword="null" />.</param>
        /// <param name="inheritability">One of the enumeration values that specifies whether a handle to the memory-mapped file can be inherited by a child process.</param>
        /// <returns>A memory-mapped file that has the specified characteristics.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="mapName" /> or <paramref name="memoryMappedFileSecurity" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="mapName" /> is an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is not a positive number.
        /// -or-
        /// The capacity cannot be greater than the size of the system's logical address space.
        /// -or-
        /// <paramref name="access"/> or <paramref name="options" /> or <paramref name="inheritability" /> is not a valid enumeration value.</exception>
        /// <remarks>Use this method to create or open a memory-mapped file that is not persisted (that is, not associated with a file on disk), which you can use to share data between processes.</remarks>
        public static unsafe MemoryMappedFile CreateOrOpen(
            string mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            MemoryMappedFileSecurity memoryMappedFileSecurity,
            HandleInheritability inheritability)
        {
            MemoryMappedFileInternal.VerifyParametersCreateOrOpen(mapName, capacity, access, options, inheritability);

            if (memoryMappedFileSecurity == null)
            {
                throw new ArgumentNullException(nameof(memoryMappedFileSecurity));
            }

            SafeMemoryMappedFileHandle handle;
            // special case for write access; create will never succeed
            if (access == MemoryMappedFileAccess.Write)
            {
                handle = MemoryMappedFileInternal.OpenCore(mapName, inheritability, access, true);
            }
            else
            {
                fixed (byte* pSecurityDescriptor = memoryMappedFileSecurity.GetSecurityDescriptorBinaryForm())
                {
                    handle = MemoryMappedFileInternal.CreateOrOpenCore(mapName, inheritability, access, options, capacity, MemoryMappedFileInternal.GetSecAttrs(pSecurityDescriptor, inheritability));
                }
            }

            return new MemoryMappedFile(handle);
        }

        /// <summary>
        /// Gets the access control to the memory-mapped file resource.
        /// </summary>
        /// <param name="memoryMappedFile">A memory-mapped file instance.</param>
        /// <returns>The permissions that can be granted for file access and operations on memory-mapped files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="memoryMappedFile" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The memory-mapped file is closed.</exception>
        /// <exception cref="InvalidOperationException">An underlying call to set security information failed.</exception>
        /// <exception cref="NotSupportedException">An underlying call to set security information failed.</exception>
        /// <exception cref="PlatformNotSupportedException">The current platform is unsupported.</exception>
        /// <exception cref="UnauthorizedAccessException">An underlying call to set security information failed.
        /// -or-
        /// The memory-mapped file was opened as <see cref="MemoryMappedFileAccess.Write" /> only.</exception>
        public static MemoryMappedFileSecurity GetAccessControl(this MemoryMappedFile memoryMappedFile)
        {
            if (memoryMappedFile == null)
                throw new ArgumentNullException(nameof(memoryMappedFile));

            if (memoryMappedFile.SafeMemoryMappedFileHandle.IsClosed)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_MemoryMappedFileClosed);

            return new MemoryMappedFileSecurity(memoryMappedFile.SafeMemoryMappedFileHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        /// <summary>
        /// Sets the access control to the memory-mapped file resource.
        /// </summary>
        /// <param name="memoryMappedFile">A memory-mapped file instance.</param>
        /// <param name="memoryMappedFileSecurity">The permissions that can be granted for file access and operations on memory-mapped files.</param>
        /// <exception cref="ArgumentNullException"><paramref name="memoryMappedFile" /> or <paramref name="memoryMappedFileSecurity" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The memory-mapped file is closed.</exception>
        /// <exception cref="InvalidOperationException">An underlying call to set security information failed.</exception>
        /// <exception cref="NotSupportedException">An underlying call to set security information failed.</exception>
        /// <exception cref="UnauthorizedAccessException">An underlying call to set security information failed.</exception>
        public static void SetAccessControl(this MemoryMappedFile memoryMappedFile, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            if (memoryMappedFile == null)
                throw new ArgumentNullException(nameof(memoryMappedFile));

            if (memoryMappedFileSecurity == null)
                throw new ArgumentNullException(nameof(memoryMappedFileSecurity));

            if (memoryMappedFile.SafeMemoryMappedFileHandle.IsClosed)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_MemoryMappedFileClosed);

            memoryMappedFileSecurity.PersistHandle(memoryMappedFile.SafeMemoryMappedFileHandle);
        }
    }
}
