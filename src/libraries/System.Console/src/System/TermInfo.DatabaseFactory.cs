// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System;

internal static partial class TermInfo
{
    internal sealed class DatabaseFactory
    {
        /// <summary>
        /// The default locations in which to search for terminfo databases.
        /// This is the ordering of well-known locations used by ncurses.
        /// </summary>
        internal static readonly string[] s_terminfoLocations = {
            "/etc/terminfo",
            "/lib/terminfo",
            "/usr/share/terminfo",
            "/usr/share/misc/terminfo",
            "/usr/local/share/terminfo"
        };

        /// <summary>Read the database for the current terminal as specified by the "TERM" environment variable.</summary>
        /// <returns>The database, or null if it could not be found.</returns>
        internal static Database? ReadActiveDatabase()
        {
            string? term = Environment.GetEnvironmentVariable("TERM");
            return !string.IsNullOrEmpty(term) ? ReadDatabase(term) : null;
        }

        /// <summary>Read the database for the specified terminal.</summary>
        /// <param name="term">The identifier for the terminal.</param>
        /// <returns>The database, or null if it could not be found.</returns>
        internal static Database? ReadDatabase(string term)
        {
            // This follows the same search order as prescribed by ncurses.
            Database? db;

            // First try a location specified in the TERMINFO environment variable.
            string? terminfo = Environment.GetEnvironmentVariable("TERMINFO");
            if (!string.IsNullOrWhiteSpace(terminfo) && (db = ReadDatabase(term, terminfo)) != null)
            {
                return db;
            }

            // Then try in the user's home directory.
            string? home = PersistedFiles.GetHomeDirectory();
            if (!string.IsNullOrWhiteSpace(home) && (db = ReadDatabase(term, home + "/.terminfo")) != null)
            {
                return db;
            }

            // Then try a set of well-known locations.
            foreach (string terminfoLocation in s_terminfoLocations)
            {
                if ((db = ReadDatabase(term, terminfoLocation)) != null)
                {
                    return db;
                }
            }

            // Couldn't find one
            return null;
        }

        /// <summary>Attempt to open as readonly the specified file path.</summary>
        /// <param name="filePath">The path to the file to open.</param>
        /// <param name="fd">If successful, the opened file descriptor; otherwise, -1.</param>
        /// <returns>true if the file was successfully opened; otherwise, false.</returns>
        private static bool TryOpen(string filePath, [NotNullWhen(true)] out SafeFileHandle? fd)
        {
            fd = Interop.Sys.Open(filePath, Interop.Sys.OpenFlags.O_RDONLY | Interop.Sys.OpenFlags.O_CLOEXEC, 0);
            if (fd.IsInvalid)
            {
                // Don't throw in this case, as we'll be polling multiple locations looking for the file.
                fd.Dispose();
                fd = null;
                return false;
            }

            return true;
        }

        /// <summary>Read the database for the specified terminal from the specified directory.</summary>
        /// <param name="term">The identifier for the terminal.</param>
        /// <param name="directoryPath">The path to the directory containing terminfo database files.</param>
        /// <returns>The database, or null if it could not be found.</returns>
        internal static Database? ReadDatabase(string? term, string? directoryPath)
        {
            if (string.IsNullOrEmpty(term) || string.IsNullOrEmpty(directoryPath))
            {
                return null;
            }

            Span<char> stackBuffer = stackalloc char[256];
            SafeFileHandle? fd;
            if (!TryOpen(string.Create(null, stackBuffer, $"{directoryPath}/{term[0]}/{term}"), out fd) &&       // /directory/termFirstLetter/term      (Linux)
                !TryOpen(string.Create(null, stackBuffer, $"{directoryPath}/{(int)term[0]:X}/{term}"), out fd))  // /directory/termFirstLetterAsHex/term (Mac)
            {
                return null;
            }

            using (fd)
            {
                // Read in all of the terminfo data
                long termInfoLength = RandomAccess.GetLength(fd);
                const int MaxTermInfoLength = 4096; // according to the term and tic man pages, 4096 is the terminfo file size max
                const int HeaderLength = 12;
                if (termInfoLength <= HeaderLength || termInfoLength > MaxTermInfoLength)
                {
                    throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                }

                byte[] data = new byte[(int)termInfoLength];
                long fileOffset = 0;
                do
                {
                    int bytesRead = RandomAccess.Read(fd, new Span<byte>(data, (int)fileOffset, (int)(termInfoLength - fileOffset)), fileOffset);
                    if (bytesRead == 0)
                    {
                        throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                    }

                    fileOffset += bytesRead;
                } while (fileOffset < termInfoLength);

                // Create the database from the data
                return new Database(term, data);
            }
        }
    }
}
