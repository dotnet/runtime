// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal static partial class FileStreamHelpers
    {
        internal static bool UseNet5CompatStrategy { get; } = AppContextConfigHelper.GetBooleanConfig("System.IO.UseNet5CompatFileStream", "DOTNET_SYSTEM_IO_USENET5COMPATFILESTREAM");

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, SafeFileHandle handle, FileAccess access, FileShare share, int bufferSize, bool isAsync)
            => WrapIfDerivedType(fileStream, ChooseStrategyCore(handle, access, share, bufferSize, isAsync));

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
            => WrapIfDerivedType(fileStream, ChooseStrategyCore(path, mode, access, share, bufferSize, options, preallocationSize));

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

        internal static bool ShouldPreallocate(long preallocationSize, FileAccess access, FileMode mode)
            => preallocationSize > 0
               && (access & FileAccess.Write) != 0
               && mode != FileMode.Open && mode != FileMode.Append;
    }
}
