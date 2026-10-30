// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Static class containing a variety of helper methods.
    internal static partial class TarHelpers
    {
        private const int MaxSymlinkDepth = 40;

        internal const short RecordSize = 512;
        internal const int MaxBufferLength = 4096;

        internal struct ExtractState
        {
            internal SortedDictionary<string, UnixFileMode>? PendingModes;
            internal Stack<(string, DateTimeOffset)> DirectoryModificationTimes;
            internal TarHardLinkMode HardLinkMode;
            // Allocated lazily on the first deferred symlink; most archives have none.
            internal Dictionary<FileId, (string SymlinkPath, string LinkTarget)>? PendingLinks;

            public ExtractState()
            {
                PendingModes = CreatePendingModesDictionary();
                DirectoryModificationTimes = new();
            }

            internal Dictionary<FileId, (string SymlinkPath, string LinkTarget)> GetOrCreatePendingLinks()
                => PendingLinks ??= new();

            internal readonly void Apply(string destinationDirectoryPath)
            {
                CreatePendingLinks(destinationDirectoryPath, PendingLinks);
                SetPendingModes(PendingModes);
                SetPendingModificationTimes(DirectoryModificationTimes);
            }
        }

        internal const UnixFileMode ValidUnixFileModes =
            UnixFileMode.UserRead |
            UnixFileMode.UserWrite |
            UnixFileMode.UserExecute |
            UnixFileMode.GroupRead |
            UnixFileMode.GroupWrite |
            UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead |
            UnixFileMode.OtherWrite |
            UnixFileMode.OtherExecute |
            UnixFileMode.StickyBit |
            UnixFileMode.SetGroup |
            UnixFileMode.SetUser;

        // Default mode for TarEntry created for a file-type.
        private const UnixFileMode DefaultFileMode =
            UnixFileMode.UserRead | UnixFileMode.UserWrite |
            UnixFileMode.GroupRead |
            UnixFileMode.OtherRead;

        // Default mode for TarEntry created for a directory-type.
        private const UnixFileMode DefaultDirectoryMode =
            DefaultFileMode |
            UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

        internal static int GetDefaultMode(TarEntryType type)
            => type is TarEntryType.Directory or TarEntryType.DirectoryList ? (int)DefaultDirectoryMode : (int)DefaultFileMode;

        // Helps advance the stream a total number of bytes larger than int.MaxValue.
        internal static async ValueTask AdvanceStreamCoreAsync<TAdapter>(Stream archiveStream, long bytesToDiscard, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (archiveStream.CanSeek)
            {
                archiveStream.Position += bytesToDiscard;
            }
            else if (bytesToDiscard > 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: (int)Math.Min(MaxBufferLength, bytesToDiscard));
                try
                {
                    while (bytesToDiscard > 0)
                    {
                        int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToDiscard);
                        await TAdapter.ReadExactlyAsync(archiveStream, buffer.AsMemory(0, currentLengthToRead), cancellationToken).ConfigureAwait(false);
                        bytesToDiscard -= currentLengthToRead;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        // Helps copy a specific number of bytes from one stream into another.
        internal static async ValueTask CopyBytesCoreAsync<TAdapter>(Stream origin, Stream destination, long bytesToCopy, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: (int)Math.Min(MaxBufferLength, bytesToCopy));
            try
            {
                while (bytesToCopy > 0)
                {
                    int currentLengthToRead = (int)Math.Min(MaxBufferLength, bytesToCopy);
                    Memory<byte> memory = buffer.AsMemory(0, currentLengthToRead);
                    await TAdapter.ReadExactlyAsync(origin, memory, cancellationToken).ConfigureAwait(false);
                    await TAdapter.WriteAsync(destination, memory, cancellationToken).ConfigureAwait(false);
                    bytesToCopy -= currentLengthToRead;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Returns the number of bytes until the next multiple of the record size.
        internal static int CalculatePadding(long size)
        {
            long ceilingMultipleOfRecordSize = ((RecordSize - 1) | (size - 1)) + 1;
            int padding = (int)(ceilingMultipleOfRecordSize - size);
            return padding;
        }

        // Returns true if all the bytes in the specified array are nulls, false otherwise.
        internal static bool IsAllNullBytes(ReadOnlySpan<byte> buffer) =>
            !buffer.ContainsAnyExcept((byte)0);

        // Converts the specified number of seconds that have passed since the Unix Epoch to a DateTimeOffset.
        internal static DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(long secondsSinceUnixEpoch) =>
            new DateTimeOffset((secondsSinceUnixEpoch * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);

        // Converts the specified number of seconds that have passed since the Unix Epoch to a DateTimeOffset.
        private static DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(decimal secondsSinceUnixEpoch) =>
            new DateTimeOffset((long)(secondsSinceUnixEpoch * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);

        // Converts the specified DateTimeOffset to the number of seconds that have passed since the Unix Epoch.
        private static decimal GetSecondsSinceEpochFromDateTimeOffset(DateTimeOffset dateTimeOffset) =>
            ((decimal)(dateTimeOffset.UtcDateTime - DateTime.UnixEpoch).Ticks) / TimeSpan.TicksPerSecond;

        // If the specified fieldName is found in the provided dictionary and it is a valid decimal number, returns true and sets the value in 'dateTimeOffset'.
        internal static bool TryGetDateTimeOffsetFromTimestampString(Dictionary<string, string>? dict, string fieldName, out DateTimeOffset dateTimeOffset)
        {
            return TryGetDateTimeOffsetFromTimestampString(
                dict is not null && dict.TryGetValue(fieldName, out string? value) ? value : null,
                out dateTimeOffset);
        }

        internal static bool TryGetDateTimeOffsetFromTimestampString(string? value, out DateTimeOffset dateTimeOffset)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal secondsSinceEpoch))
            {
                dateTimeOffset = GetDateTimeOffsetFromSecondsSinceEpoch(secondsSinceEpoch);
                return true;
            }

            dateTimeOffset = default;
            return false;
        }

        // Converts the specified DateTimeOffset to the string representation of seconds since the Unix Epoch.
        internal static string GetTimestampStringFromDateTimeOffset(DateTimeOffset timestamp)
        {
            decimal secondsSinceEpoch = GetSecondsSinceEpochFromDateTimeOffset(timestamp);

            // Use 'G' to ensure the decimals get preserved (avoid losing precision).
            return secondsSinceEpoch.ToString("G", CultureInfo.InvariantCulture);
        }

        // If the specified fieldName has a non-empty value, parses it as a base-10 integer and returns true. Parsing exceptions propagate.
        internal static bool TryGetStringAsBaseTenInteger(IReadOnlyDictionary<string, string> dict, string fieldName, out int baseTenInteger)
        {
            return TryGetStringAsBaseTenInteger(
                dict.TryGetValue(fieldName, out string? value) ? value : null,
                out baseTenInteger);
        }

        /// <summary>Parses a uid or gid extended attribute value. See <see cref="ParseUidGid"/> for how out of range values are handled.</summary>
        internal static bool TryGetStringAsUidGid(string? value, out int id)
        {
            if (!string.IsNullOrEmpty(value))
            {
                id = ToUidGid(long.Parse(value, CultureInfo.InvariantCulture));
                return true;
            }

            id = 0;
            return false;
        }

        internal static bool TryGetStringAsBaseTenInteger(string? value, out int baseTenInteger)
        {
            if (!string.IsNullOrEmpty(value))
            {
                baseTenInteger = int.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }

            baseTenInteger = 0;
            return false;
        }

        // If the specified fieldName has a non-empty value, parses it as a base-10 long and returns true. Parsing exceptions propagate.
        internal static bool TryGetStringAsBaseTenLong(IReadOnlyDictionary<string, string> dict, string fieldName, out long baseTenLong)
        {
            return TryGetStringAsBaseTenLong(
                dict.TryGetValue(fieldName, out string? value) ? value : null,
                out baseTenLong);
        }

        internal static bool TryGetStringAsBaseTenLong(string? value, out long baseTenLong)
        {
            if (!string.IsNullOrEmpty(value))
            {
                baseTenLong = long.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }

            baseTenLong = 0;
            return false;
        }

        // Chooses the compatible regular file entry type for the specified format.
        internal static TarEntryType GetRegularFileEntryTypeForFormat(TarEntryFormat format) => format is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;

        // When writing an entry that came from an archive of a different format, if its entry type happens to
        // be an incompatible regular file entry type, convert it to the compatible one.
        // No change for all other entry types.
        internal static TarEntryType GetCorrectTypeFlagForFormat(TarEntryFormat format, TarEntryType entryType) =>
            (format, entryType) switch
            {
                (TarEntryFormat.V7, TarEntryType.RegularFile) => TarEntryType.V7RegularFile,
                (not TarEntryFormat.V7, TarEntryType.V7RegularFile) => TarEntryType.RegularFile,
                _ => entryType,
            };

        /// <summary>Parses a uid or gid numeric field.</summary>
        /// <remarks>
        /// Unix uid_t and gid_t are 32-bit unsigned, and archives may contain values larger than <see cref="int.MaxValue"/>
        /// (for example, GNU base-256 encoded fields). Such values are reinterpreted as <see cref="int"/> without an
        /// overflow check, which matches how <see cref="TarWriter"/> stores uid and gid values read from the file system.
        /// Values that don't fit in 32 bits can't be a valid id and are rejected.
        /// </remarks>
        internal static int ParseUidGid(ReadOnlySpan<byte> buffer) => ToUidGid(ParseNumeric<long>(buffer));

        private static int ToUidGid(long value)
        {
            if (value < int.MinValue || value > uint.MaxValue)
            {
                ThrowInvalidNumber();
            }

            return unchecked((int)value);
        }

        /// <summary>Parses a numeric field.</summary>
        internal static T ParseNumeric<T>(ReadOnlySpan<byte> buffer) where T : struct, INumber<T>, IBinaryInteger<T>
        {
            // The tar standard specifies that numeric fields are stored using an octal representation.
            // This limits the range of values that can be stored in the fields.
            // To increase the supported range, a GNU extension defines that when the leading byte is
            // '0xff'/'0x80' the remaining bytes are a negative/positive big formatted endian value.
            // Like the 'tar' tool we are permissive when encountering this representation in non GNU formats.
            byte leadingByte = buffer[0];
            if (leadingByte == 0xff)
            {
                return T.ReadBigEndian(buffer, isUnsigned: false);
            }
            else if (leadingByte == 0x80)
            {
                return T.ReadBigEndian(buffer.Slice(1), isUnsigned: true);
            }
            else
            {
                return ParseOctal<T>(buffer);
            }
        }

        /// <summary>Parses a byte span that represents an ASCII string containing a number in octal base.</summary>
        internal static T ParseOctal<T>(ReadOnlySpan<byte> buffer) where T : struct, INumber<T>
        {
            buffer = TrimNullTerminated(buffer);

            // We ignore spaces because some archives seem to have them (even though they shouldn't).
            buffer = buffer.Trim((byte)' ');

            if (buffer.Length == 0)
            {
                return T.Zero;
            }

            T octalFactor = T.CreateTruncating(8u);
            T value = T.Zero;
            foreach (byte b in buffer)
            {
                uint digit = (uint)(b - '0');
                if (digit >= 8)
                {
                    ThrowInvalidNumber();
                }

                value = checked((value * octalFactor) + T.CreateTruncating(digit));
            }

            return value;
        }

        [DoesNotReturn]
        private static void ThrowInvalidNumber() =>
            throw new InvalidDataException(SR.TarInvalidNumber);

        // Returns the null-terminated UTF8 string contained in the specified buffer.
        internal static string ParseUtf8String(ReadOnlySpan<byte> buffer)
        {
            buffer = TrimNullTerminated(buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        internal static ReadOnlySpan<byte> TrimNullTerminated(ReadOnlySpan<byte> buffer)
        {
            int i = buffer.IndexOf((byte)0);
            if (i != -1)
            {
                buffer = buffer[0..i];
            }
            return buffer;
        }

        // After the file contents, there may be zero or more null characters,
        // which exist to ensure the data is aligned to the record size. Skip them and
        // set the stream position to the first byte of the next entry.
        internal static async ValueTask<int> SkipBlockAlignmentPaddingCoreAsync<TAdapter>(Stream archiveStream, long size, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            int bytesToSkip = CalculatePadding(size);
            await AdvanceStreamCoreAsync<TAdapter>(archiveStream, bytesToSkip, cancellationToken).ConfigureAwait(false);
            return bytesToSkip;
        }

        // Throws if the specified entry type is not supported for the specified format.
        internal static void ThrowIfEntryTypeNotSupported(TarEntryType entryType, TarEntryFormat archiveFormat, [CallerArgumentExpression(nameof(entryType))] string? paramName = null)
        {
            switch (archiveFormat)
            {
                case TarEntryFormat.V7:
                    if (entryType is
                        TarEntryType.Directory or
                        TarEntryType.HardLink or
                        TarEntryType.V7RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        return;
                    }
                    break;

                case TarEntryFormat.Ustar:
                    if (entryType is
                        TarEntryType.BlockDevice or
                        TarEntryType.CharacterDevice or
                        TarEntryType.Directory or
                        TarEntryType.Fifo or
                        TarEntryType.HardLink or
                        TarEntryType.RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        return;
                    }
                    break;

                case TarEntryFormat.Pax:
                    if (entryType is
                        TarEntryType.BlockDevice or
                        TarEntryType.CharacterDevice or
                        TarEntryType.Directory or
                        TarEntryType.Fifo or
                        TarEntryType.HardLink or
                        TarEntryType.RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        // GlobalExtendedAttributes is handled via PaxGlobalExtendedAttributesEntry

                        // Not supported for writing - internally autogenerated:
                        // - ExtendedAttributes
                        return;
                    }
                    break;

                case TarEntryFormat.Gnu:
                    if (entryType is
                        TarEntryType.BlockDevice or
                        TarEntryType.CharacterDevice or
                        TarEntryType.Directory or
                        TarEntryType.Fifo or
                        TarEntryType.HardLink or
                        TarEntryType.RegularFile or
                        TarEntryType.SymbolicLink)
                    {
                        // Not supported for writing:
                        // - ContiguousFile
                        // - DirectoryList
                        // - MultiVolume
                        // - RenamedOrSymlinked
                        // - SparseFile
                        // - TapeVolume

                        // Also not supported for writing - internally autogenerated:
                        // - LongLink
                        // - LongPath
                        return;
                    }
                    break;

                case TarEntryFormat.Unknown:
                default:
                    throw new InvalidDataException(SR.Format(SR.TarInvalidFormat, archiveFormat));
            }

            throw new ArgumentException(SR.Format(SR.TarEntryTypeNotSupportedInFormat, entryType, archiveFormat), paramName);
        }

        public static void SetPendingModificationTimes(Stack<(string, DateTimeOffset)> directoryModificationTimes)
        {
            // note: these are ordered child to parent.
            while (directoryModificationTimes.TryPop(out (string Path, DateTimeOffset Modified) item))
            {
                AttemptDirectorySetLastWriteTime(item.Path, item.Modified);
            }
        }

        public static void UpdatePendingModificationTimes(Stack<(string, DateTimeOffset)> directoryModificationTimes, string fullPath, DateTimeOffset modified)
        {
            // We can't set the modification time when we create the directory because extracting entries into it
            // will cause that time to change. Instead, we track the times to set them later.

            // We take into account that regular tar files are ordered:
            // when we see a new directory which is not a child of the previous directory
            // we can set the parent directory timestamps, and stop tracking them.
            // This avoids having to track all directory entries until we've finished extracting the entire archive.
            while (directoryModificationTimes.TryPeek(out (string Path, DateTimeOffset Modified) previous) &&
                   !IsChildPath(previous.Path, fullPath))
            {
                directoryModificationTimes.TryPop(out previous);
                AttemptDirectorySetLastWriteTime(previous.Path, previous.Modified);
            }

            directoryModificationTimes.Push((fullPath, modified));
        }

        private static bool IsChildPath(string parentFullPath, string childFullPath)
        {
            // Both paths may end with an additional separator.

            // Verify that either the parent path ends with a separator
            // or the child path has a separator where the parent path ends.
            if (IsDirectorySeparatorChar(parentFullPath[^1]))
            {
                // The child needs to be at least a char longer than the parent for the name.
                if (childFullPath.Length <= parentFullPath.Length)
                {
                    return false;
                }
            }
            else
            {
                // The child needs to be at least 2 chars longer than the parent:
                // one for the separator, and one for the name.
                if ((childFullPath.Length < parentFullPath.Length + 2) ||
                    !IsDirectorySeparatorChar(childFullPath[parentFullPath.Length]))
                {
                    return false;
                }
            }

            return childFullPath.StartsWith(parentFullPath, PathInternal.StringComparison);

            // We don't need to check for AltDirectorySeparatorChar, full paths are normalized to DirectorySeparatorChar.
            static bool IsDirectorySeparatorChar(char c)
                => c == Path.DirectorySeparatorChar;
        }

        private static void AttemptDirectorySetLastWriteTime(string fullPath, DateTimeOffset lastWriteTime)
        {
            try
            {
                Directory.SetLastWriteTime(fullPath, lastWriteTime.UtcDateTime);
            }
            catch
            {
                // Some OSes like Android might not support setting the last write time, the extraction should not fail because of that
            }
        }

        // We delayed creating some symbolic links because they might target locations outside the destination.
        // Any symlink that is in the destination directory at the time this method is called is either one that points in the destination
        // or a pre-existing symlink that we are allowed to follow outside.
        internal static void CreatePendingLinks(string destinationDirectoryFullPath, Dictionary<FileId, (string SymlinkPath, string LinkTarget)>? pendingLinks)
        {
            Debug.Assert(destinationDirectoryFullPath[^1] == Path.DirectorySeparatorChar);

            if (pendingLinks is not { Count: > 0 })
            {
                // No pending links, nothing to do.
                return;
            }

            // Resolve the destination directory against the filesystem root, following any pre-existing symlinks in its path.
            string resolvedDestinationDirectoryFullPath;
            int destinationSymlinkDepth;
            {
                string root = Path.GetPathRoot(destinationDirectoryFullPath)!;
                var rootResolver = new PendingLinkResolver(root, root, destinationSymlinkDepth: 0, pendingLinks: null);
                resolvedDestinationDirectoryFullPath = rootResolver.ResolveDestinationDirectory(destinationDirectoryFullPath.AsSpan(root.Length));
                destinationSymlinkDepth = rootResolver.SymlinkDepth;

                // Ensure DirectorySeparatorChar.
                if (!resolvedDestinationDirectoryFullPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    resolvedDestinationDirectoryFullPath += Path.DirectorySeparatorChar;
                }
            }

            // Now create all pending links (that have ".." segments)
            var resolver = new PendingLinkResolver(destinationDirectoryFullPath, resolvedDestinationDirectoryFullPath, destinationSymlinkDepth, pendingLinks);

            // CreateLink removes the link from pendingLinks once it is created, and it may create (and remove) several
            // more when a link resolves through other pending links. Because that mutates the dictionary, we can't
            // enumerate it directly, so we take a snapshot of the keys and skip the ones that were already created.
            // The order of creation doesn't matter.
            FileId[] fileIds = new FileId[pendingLinks.Count];
            pendingLinks.Keys.CopyTo(fileIds, 0);

            foreach (FileId fileId in fileIds)
            {
                if (pendingLinks.TryGetValue(fileId, out (string SymlinkPath, string LinkTarget) pendingLink))
                {
                    resolver.CreateLink(fileId, pendingLink.SymlinkPath, pendingLink.LinkTarget);
                }
            }

            Debug.Assert(pendingLinks.Count == 0);
        }

        // checks if a path is equal to or nested under a full path. The full path must end with a directory separator.
        // examples:
        //    FullPathIsEqualOrNested("C:\foo\", "C:\foo\bar") => true
        //    FullPathIsEqualOrNested("C:\foo\", "C:\foo") => true
        //    FullPathIsEqualOrNested("C:\foo\", "C:\foobar") => false
        internal static bool FullPathIsEqualOrNested(string fullPath, string nestedFullPath)
        {
            Debug.Assert(fullPath[^1] == Path.DirectorySeparatorChar);

            return nestedFullPath.StartsWith(fullPath, PathInternal.StringComparison)
                || string.Equals(nestedFullPath, fullPath.TrimEnd(Path.DirectorySeparatorChar), PathInternal.StringComparison);
        }

        // Resolves the targets of deferred (pending) symbolic links produced during extraction, ensuring none of them
        // resolve outside the destination directory. Pre-existing symlinks that already point outside the destination
        // widen the allowed region: a pending link may point under the location such a symlink resolved to.
        private sealed class PendingLinkResolver
        {
            // Invariant for the lifetime of the resolver.
            private readonly string _destination;
            private readonly string _resolvedDestination;
            private readonly int _destinationSymlinkDepth;
            private readonly Dictionary<FileId, (string SymlinkPath, string LinkTarget)>? _pendingLinks;

            // Reset at the start of each top-level link resolution; shared (via ref semantics) across the recursive walk.
            private int _symlinkDepth;

            internal PendingLinkResolver(string destination, string resolvedDestination, int destinationSymlinkDepth, Dictionary<FileId, (string SymlinkPath, string LinkTarget)>? pendingLinks)
            {
                Debug.Assert(destination[^1] == Path.DirectorySeparatorChar);

                _destination = destination;
                _resolvedDestination = resolvedDestination;
                _destinationSymlinkDepth = destinationSymlinkDepth;
                _pendingLinks = pendingLinks;
                _symlinkDepth = destinationSymlinkDepth;
            }

            internal int SymlinkDepth => _symlinkDepth;

            // Resolves the destination directory itself (used by RealPath). Nothing is ever outside the filesystem root.
            internal string ResolveDestinationDirectory(ReadOnlySpan<char> relativePath)
            {
                string? rootPath = _destination;
                Resolve(ref rootPath, _destination, relativePath, out string resolvedPath, visitedLinks: null);
                return resolvedPath;
            }

            // Resolves and creates a single top-level pending link.
            internal void CreateLink(FileId fileId, string symlinkPath, string linkTarget)
            {
                // Each top-level link starts a fresh resolution walk.
                _symlinkDepth = _destinationSymlinkDepth;
                AddPendingLink(_resolvedDestination, fileId, symlinkPath, linkTarget, visitedLinks: null, out _);
            }

            private void AddPendingLink(string rootPath, FileId fileId, string symlinkPath, string linkTarget, HashSet<FileId>? visitedLinks, out string resolvedFullPath)
            {
                Debug.Assert(rootPath is not null);

                string? rootPathLocal = rootPath;
                ReadOnlySpan<char> relativeLinkPath = symlinkPath.AsSpan(_destination.Length);
                string resolvedPath = _resolvedDestination;
                ReadOnlySpan<char> directoryPath = Path.GetDirectoryName(relativeLinkPath);
                if (!directoryPath.IsEmpty)
                {
                    if (!Resolve(ref rootPathLocal, resolvedPath, directoryPath, out resolvedPath, visitedLinks))
                    {
                        throw new IOException(SR.Format(SR.TarExtractingResultsLinkOutside, linkTarget, _destination));
                    }
                    Debug.Assert(rootPathLocal is not null);
                }

                ReadOnlySpan<char> resolveTarget = linkTarget;
                if (Path.IsPathFullyQualified(linkTarget))
                {
                    rootPathLocal = _resolvedDestination;
                    resolvedPath = Path.GetPathRoot(linkTarget)!;
                    resolveTarget = linkTarget.AsSpan(resolvedPath.Length);
                }

                if (!Resolve(ref rootPathLocal, resolvedPath, resolveTarget, out resolvedFullPath, visitedLinks))
                {
                    throw new IOException(SR.Format(SR.TarExtractingResultsLinkOutside, linkTarget, _destination));
                }
                Debug.Assert(rootPathLocal is not null);

                if (_pendingLinks!.Remove(fileId))
                {
                    File.Delete(symlinkPath);
                    File.CreateSymbolicLink(symlinkPath, linkTarget);
                }
            }

            // Returns whether the path formed by combining baseFullPath and relativePath resides in either the root path or the destination path.
            // The containmentRootPath path tracks a location that may not be in the destination path but which is reachable (via preexisting links)
            // and considered a root that a delayed link target may point under.
            private bool Resolve(ref string? containmentRootPath, string baseFullPath, ReadOnlySpan<char> relativePath, out string resolvedFullPath, HashSet<FileId>? visitedLinks)
            {
                resolvedFullPath = baseFullPath;
                bool checkExists = true;

                // walk the relative path and resolve each component, following symlinks and checking for loops.
                foreach (Range range in relativePath.SplitAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                {
                    ReadOnlySpan<char> component = relativePath[range];
                    if (component.IsEmpty || component is ".")
                    {
                        continue;
                    }
                    if (component is "..")
                    {
                        // Traverse to the parent. Strip any trailing separators first, otherwise
                        // GetDirectoryName only strips them and doesn't move up a level.
                        ReadOnlySpan<char> resolvedFullPathSpan = resolvedFullPath.AsSpan();
                        while (!resolvedFullPathSpan.IsEmpty && PathInternal.IsDirectorySeparator(resolvedFullPathSpan[^1]))
                        {
                            resolvedFullPathSpan = resolvedFullPathSpan[..^1];
                        }

                        // GetDirectoryName returns null at the root, map back to root (.. on / yields /).
                        resolvedFullPathSpan = Path.GetDirectoryName(resolvedFullPathSpan);
                        resolvedFullPath = resolvedFullPathSpan.IsEmpty ? resolvedFullPath : resolvedFullPathSpan.ToString();

                        if (containmentRootPath is not null && !IsInRootOrDestination(containmentRootPath, resolvedFullPath))
                        {
                            return false;
                        }
                        continue;
                    }

                    string componentPath = Path.Join(resolvedFullPath, component);

                    // TryGetPathInfo throws on error and returns false when the path does not exist.
                    if (!checkExists || !TryGetPathInfo(componentPath, out FileId fileId, out bool isSymlink))
                    {
                        // Path doesn't exist; remaining components can't exist either.
                        // short-circuit to simply appending the remaining components to the path for the final check.
                        resolvedFullPath = componentPath;
                        checkExists = false;
                        continue;
                    }

                    string? linkTarget = null;
                    (string SymlinkPath, string LinkTarget) pendingLink = default;
                    if (isSymlink)
                    {
                        linkTarget = new FileInfo(componentPath).LinkTarget;
                    }
                    else if (_pendingLinks?.TryGetValue(fileId, out pendingLink) == true)
                    {
                        linkTarget = pendingLink.LinkTarget;
                    }

                    if (linkTarget is null)
                    {
                        resolvedFullPath = componentPath;
                        continue;
                    }

                    if (++_symlinkDepth > MaxSymlinkDepth)
                    {
                        throw new IOException(SR.Format(SR.IO_TooManySymbolicLinkLevels, componentPath));
                    }

                    // To allow creating symbolic links that cause loops we need to detect the loops.
                    // visitedLinks tracks the links we've followed.
                    visitedLinks ??= new HashSet<FileId>();
                    if (!visitedLinks.Add(fileId))
                    {
                        // Already visited.
                        visitedLinks.Clear(); // Clear the set so callers detect circularity via Remove returning false below.
                        resolvedFullPath = componentPath;
                        break;
                    }

                    if (isSymlink)
                    {
                        if (!FollowExistingSymlink(ref containmentRootPath, resolvedFullPath, linkTarget, visitedLinks, out resolvedFullPath))
                        {
                            Debug.Fail("Should be able to follow existing symlinks");
                            return false;
                        }
                    }
                    else
                    {
                        // Pending link. A fully qualified target must point inside the destination.
                        if (Path.IsPathFullyQualified(linkTarget))
                        {
                            containmentRootPath = _resolvedDestination;
                        }
                        // If no rootPath was set yet, limit the pending link to the destination dir.
                        AddPendingLink(containmentRootPath ?? _resolvedDestination, fileId, pendingLink.SymlinkPath!, pendingLink.LinkTarget!, visitedLinks, out resolvedFullPath);
                    }

                    if (!visitedLinks.Remove(fileId))
                    {
                        // Circular reference detected while resolving the link above.
                        break;
                    }
                }

                return containmentRootPath is null || IsInRootOrDestination(containmentRootPath, resolvedFullPath);
            }

            // Follows a pre-existing symlink that is already on disk. Existing symlinks are not limited to a root path;
            // after resolving, the root path is widened to the resolved location so pending links may also point under it.
            private bool FollowExistingSymlink(ref string? rootPath, string linkDirectory, string linkTarget, HashSet<FileId>? visitedLinks, out string resolvedFullPath)
            {
                string resolveFrom;
                ReadOnlySpan<char> resolveTarget;
                if (Path.IsPathFullyQualified(linkTarget))
                {
                    resolveFrom = Path.GetPathRoot(linkTarget)!;
                    resolveTarget = linkTarget.AsSpan(resolveFrom.Length);
                }
                else
                {
                    resolveFrom = linkDirectory;
                    resolveTarget = linkTarget;
                }

                // set root path as null to disable the containment checks inside Resolve
                string? symlinkRootPath = null;
                if (!Resolve(ref symlinkRootPath, resolveFrom, resolveTarget, out resolvedFullPath, visitedLinks))
                {
                    return false;
                }

                rootPath = resolvedFullPath;
                if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    rootPath += Path.DirectorySeparatorChar;
                }
                return true;
            }

            private bool IsInRootOrDestination(string rootPath, string resolvedPath)
                => FullPathIsEqualOrNested(rootPath, resolvedPath)
                || (!ReferenceEquals(rootPath, _resolvedDestination) && FullPathIsEqualOrNested(_resolvedDestination, resolvedPath));
        }
    }
}
