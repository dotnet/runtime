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
    /// The application host executable cannot be customized because adding resources requires
    /// that the build be performed on Windows (excluding Nano Server).
    /// </summary>
    public sealed class AppHostCustomizationUnsupportedOSException : AppHostUpdateException
    {
        internal AppHostCustomizationUnsupportedOSException()
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
        internal AppHostNotCUIException()
        {
        }
    }

    /// <summary>
    ///  Unable to use the input file as an application host executable
    ///  because it's not a Windows PE file
    /// </summary>
    public sealed class AppHostNotPEFileException : AppHostUpdateException
    {
        internal AppHostNotPEFileException()
        {
        }
    }

    /// <summary>
    /// Unable to sign the apphost binary.
    /// </summary>
    public sealed class AppHostSigningException : AppHostUpdateException
    {
        public readonly int ExitCode;

        internal AppHostSigningException(int exitCode, string signingErrorMessage)
            : base(signingErrorMessage)
        {
        }
    }

    /// <summary>
    /// Given app file name is longer than 1024 bytes
    /// </summary>
    public sealed class AppNameTooLongException : AppHostUpdateException
    {
        public string LongName { get; }

        internal AppNameTooLongException(string name)
        {
            LongName = name;
        }
    }
}
