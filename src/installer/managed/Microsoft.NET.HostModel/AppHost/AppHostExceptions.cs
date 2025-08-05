// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// An instance of this exception is thrown when an AppHost binary update
    /// fails due to known user errors.
    /// </summary>
    public class AppHostUpdateException : Exception
    {
        internal AppHostUpdateException(string message = null)
            : base(message)
        {
        }
    }

    /// <summary>
    /// The MachO application host executable cannot be customized because
    /// it was not in the expected format
    /// </summary>
    public sealed class AppHostMachOFormatException : AppHostUpdateException
    {
        public readonly MachOFormatError Error;

        internal AppHostMachOFormatException(MachOFormatError error)
            : base($"Failed to process MachO file: {error}")
        {
            Error = error;
        }
    }

    /// <summary>
    /// Unable to use the input file as application host executable because it's not a
    /// Windows executable for the CUI (Console) subsystem.
    /// </summary>
    public sealed class AppHostNotCUIException : AppHostUpdateException
    {
        internal AppHostNotCUIException(ushort subsystem)
            : base($"Selected apphost is not a CUI Windows application. Subsystem: {subsystem}")
        {
        }
    }

    /// <summary>
    ///  Unable to use the input file as an application host executable
    ///  because it's not a Windows PE file
    /// </summary>
    public sealed class AppHostNotPEFileException : AppHostUpdateException
    {
        public readonly string Reason;

        internal AppHostNotPEFileException(string reason)
            : base($"Selected apphost is not a valid PE file. {reason}")
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Unable to sign the apphost binary.
    /// </summary>
    public sealed class AppHostSigningException : AppHostUpdateException
    {
        public readonly int ExitCode;

        internal AppHostSigningException(int exitCode, string signingErrorMessage)
            : base($"{signingErrorMessage}; Exit code: {exitCode}")
        {
            ExitCode = exitCode;
        }
    }

    /// <summary>
    /// Given app file name is longer than 1024 bytes
    /// </summary>
    public sealed class AppNameTooLongException : AppHostUpdateException
    {
        public string LongName { get; }

        internal AppNameTooLongException(string name, int maxSize)
            : base($"The name of the app is too long (must be less than {maxSize} bytes when encoded in UTF-8). Name: {name}")
        {
            LongName = name;
        }
    }

    /// <summary>
    /// App-relative .NET path is an absolute path
    /// </summary>
    public sealed class AppRelativePathRootedException : AppHostUpdateException
    {
        public string Path { get; }

        internal AppRelativePathRootedException(string path)
            : base($"The app-relative .NET path should not be an absolute path. Path: {path}")
        {
            Path = path;
        }
    }

    /// <summary>
    /// App-relative .NET path is too long to be embedded in the apphost
    /// </summary>
    public sealed class AppRelativePathTooLongException : AppHostUpdateException
    {
        public string Path { get; }

        internal AppRelativePathTooLongException(string path, int maxSize)
            : base($"The app-relative .NET path is too long (must be less than {maxSize} bytes when encoded in UTF-8). Path: {path}")
        {
            Path = path;
        }
    }
}
