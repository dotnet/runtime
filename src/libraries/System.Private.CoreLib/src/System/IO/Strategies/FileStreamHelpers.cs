// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal static partial class FileStreamHelpers
    {
        /// <summary>Caches whether Serialization Guard has been disabled for file writes</summary>
        private static int s_cachedSerializationSwitch;

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            FileStreamStrategy strategy =
                EnableBufferingIfNeeded(ChooseStrategyCore(handle, access, isAsync), bufferSize);

            return WrapIfDerivedType(fileStream, strategy);
        }

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize, UnixFileMode? unixCreateMode)
        {
            FileStreamStrategy strategy =
                EnableBufferingIfNeeded(ChooseStrategyCore(path, mode, access, share, options, preallocationSize, unixCreateMode), bufferSize);

            return WrapIfDerivedType(fileStream, strategy);
        }

        private static FileStreamStrategy EnableBufferingIfNeeded(FileStreamStrategy strategy, int bufferSize)
            => bufferSize > 1 ? new BufferedFileStreamStrategy(strategy, bufferSize) : strategy;

        private static FileStreamStrategy WrapIfDerivedType(FileStream fileStream, FileStreamStrategy strategy)
            => fileStream.GetType() == typeof(FileStream)
                ? strategy
                : new DerivedFileStreamStrategy(fileStream, strategy);

        internal static bool IsIoRelatedException(Exception e) =>
            // These all derive from IOException
            //     DirectoryNotFoundException
            //     DriveNotFoundException
            //     EndOfStreamException
            //     FileLoadException
            //     FileNotFoundException
            //     PathTooLongException
            //     PipeException
            e is IOException ||
            // Note that SecurityException is only thrown on runtimes that support CAS
            // e is SecurityException ||
            e is UnauthorizedAccessException ||
            e is NotSupportedException ||
            (e is ArgumentException && !(e is ArgumentNullException));

        internal static void ValidateArguments(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            // don't include inheritable in our bounds check for share
            FileShare tempshare = share & ~FileShare.Inheritable;
            string? badArg = null;

            if (mode < FileMode.CreateNew || mode > FileMode.Append)
            {
                badArg = nameof(mode);
            }
            else if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                badArg = nameof(access);
            }
            else if (tempshare < FileShare.None || tempshare > (FileShare.ReadWrite | FileShare.Delete))
            {
                badArg = nameof(share);
            }

            if (badArg != null)
            {
                throw new ArgumentOutOfRangeException(badArg, SR.ArgumentOutOfRange_Enum);
            }

            // NOTE: any change to FileOptions enum needs to be matched here in the error validation
            if (options != FileOptions.None && (options & ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Encrypted | (FileOptions)0x20000000 /* NoBuffering */)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_Enum);
            }
            else if (bufferSize < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum(nameof(bufferSize));
            }
            else if (preallocationSize < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum(nameof(preallocationSize));
            }

            // Write access validation
            if ((access & FileAccess.Write) == 0)
            {
                if (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append)
                {
                    // No write access, mode and access disagree but flag access since mode comes first
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidFileModeAndAccessCombo, mode, access), nameof(access));
                }
            }

            if ((access & FileAccess.Read) != 0 && mode == FileMode.Append)
            {
                throw new ArgumentException(SR.Argument_InvalidAppendMode, nameof(access));
            }

            if (preallocationSize > 0)
            {
                ValidateArgumentsForPreallocation(mode, access);
            }

            SerializationGuard(access);
        }

        internal static void ValidateArgumentsForPreallocation(FileMode mode, FileAccess access)
        {
            // The user will be writing into the preallocated space.
            if ((access & FileAccess.Write) == 0)
            {
                throw new ArgumentException(SR.Argument_InvalidPreallocateAccess, nameof(access));
            }

            // Only allow preallocation for newly created/overwritten files.
            // When we fail to preallocate, we'll remove the file.
            if (mode != FileMode.Create &&
                mode != FileMode.CreateNew)
            {
                throw new ArgumentException(SR.Argument_InvalidPreallocateMode, nameof(mode));
            }
        }

        internal static void SerializationGuard(FileAccess access)
        {
            if ((access & FileAccess.Write) == FileAccess.Write)
            {
                SerializationInfo.ThrowIfDeserializationInProgress("AllowFileWrites", ref s_cachedSerializationSwitch);
            }
        }
    }
}
