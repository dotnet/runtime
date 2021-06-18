// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

#if MS_IO_REDIST
using Microsoft.IO;
#endif

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        private static readonly bool s_isCaseSensitive = GetIsCaseSensitive();

        /// <summary>Returns a comparison that can be used to compare file and directory names for equality.</summary>
        internal static StringComparison StringComparison
        {
            get
            {
                return s_isCaseSensitive ?
                    StringComparison.Ordinal :
                    StringComparison.OrdinalIgnoreCase;
            }
        }

        /// <summary>Gets whether the system is case-sensitive.</summary>
        internal static bool IsCaseSensitive { get { return s_isCaseSensitive; } }

        /// <summary>
        /// Determines whether the file system is case sensitive.
        /// </summary>
        /// <remarks>
        /// Ideally we'd use something like pathconf with _PC_CASE_SENSITIVE, but that is non-portable,
        /// not supported on Windows or Linux, etc. For now, this function creates a tmp file with capital letters
        /// and then tests for its existence with lower-case letters.  This could return invalid results in corner
        /// cases where, for example, different file systems are mounted with differing sensitivities.
        /// </remarks>
        private static bool GetIsCaseSensitive()
        {
            try
            {
                string pathWithUpperCase = Path.Combine(Path.GetTempPath(), "CASESENSITIVETEST" + Guid.NewGuid().ToString("N"));
                using (new FileStream(pathWithUpperCase, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 0x1000, FileOptions.DeleteOnClose))
                {
                    string lowerCased = pathWithUpperCase.ToLowerInvariant();
                    return !File.Exists(lowerCased);
                }
            }
            catch
            {
                // In case something goes wrong (e.g. temp pointing to a privilieged directory), we don't
                // want to fail just because of a casing test, so we assume case-insensitive-but-preserving.
                return false;
            }
        }
    }
}
