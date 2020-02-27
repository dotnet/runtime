// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.MemoryMappedFiles
{
    internal static partial class MemoryMappedFileInternal
    {
        // Ensures the passed fileStream is rooted while the MemoryMappedFile lives,
        // and helps avoid disposing the fileStream when the MemoryMappedFile is disposed.
        internal class FileStreamRooter : IDisposable
        {
            private readonly FileStream? _fileStreamToRoot;

            public FileStreamRooter(FileStream? fileStreamToRoot) => _fileStreamToRoot = fileStreamToRoot;

            public void Dispose()
            {
            }
        }

        internal const int DefaultSize = 0;

        // This converts a MemoryMappedFileRights to its corresponding native FILE_MAP_XXX value to be used when creating new views.
        internal static int GetFileMapAccess(MemoryMappedFileRights rights)
        {
            return (int)rights;
        }

        internal static long VerifyParametersCreateFromFile(
            FileStream fileStream,
            string? mapName,
            long capacity,
            MemoryMappedFileAccess access,
            HandleInheritability inheritability)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream), SR.ArgumentNull_FileStream);
            }

            if (mapName != null && mapName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_MapNameEmptyString);
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_PositiveOrDefaultCapacityRequired);
            }

            if (capacity == 0 && fileStream.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyFile);
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

            if (access == MemoryMappedFileAccess.Read && capacity > fileStream.Length)
            {
                throw new ArgumentException(SR.Argument_ReadAccessWithLargeCapacity);
            }

            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability));
            }

            // flush any bytes written to the FileStream buffer so that we can see them in our MemoryMappedFile
            fileStream.Flush();

            long updatedCapacity = (capacity == DefaultSize) ? fileStream.Length : capacity;

            // one can always create a small view if they do not want to map an entire file
            if (fileStream.Length > updatedCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_CapacityGEFileSizeRequired);
            }

            return updatedCapacity;
        }

        internal static void VerifyParametersCreateNew(
            string? mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            HandleInheritability inheritability)
        {
            if (mapName != null && mapName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_MapNameEmptyString);
            }

            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_NeedPositiveNumber);
            }

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
        }

        internal static void VerifyParametersCreateOrOpen(
            string? mapName,
            long capacity,
            MemoryMappedFileAccess access,
            MemoryMappedFileOptions options,
            HandleInheritability inheritability)
        {
            if (mapName == null)
            {
                throw new ArgumentNullException(nameof(mapName), SR.ArgumentNull_MapName);
            }

            if (mapName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_MapNameEmptyString);
            }

            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_NeedPositiveNumber);
            }

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
        }
    }
}
