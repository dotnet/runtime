// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal static partial class FileStreamHelpers
    {
        // It's enabled by default. We are going to change that once we fix #16354, #25905 and #24847.
        internal static bool UseLegacyStrategy { get; } = GetLegacyFileStreamSetting();

        private static bool GetLegacyFileStreamSetting()
        {
            if (AppContext.TryGetSwitch("System.IO.UseNet5CompatFileStream", out bool fileConfig))
            {
                return fileConfig;
            }

            string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_IO_USENET5COMPATFILESTREAM");
            return envVar is null
                ? true // legacy is currently enabled by default;
                : bool.IsTrueStringIgnoreCase(envVar) || envVar.Equals("1");
        }

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
            => WrapIfDerivedType(fileStream, ChooseStrategyCore(handle, access, bufferSize, isAsync));

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => WrapIfDerivedType(fileStream, ChooseStrategyCore(path, mode, access, share, bufferSize, options));

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
    }
}
