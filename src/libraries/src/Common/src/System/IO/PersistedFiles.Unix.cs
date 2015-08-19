// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class PersistedFiles
    {
        private static string s_tempProductDirectory;
        private static string s_userProductDirectory;

        /// <summary>
        /// Get the location of where to store temporary files for a particular aspect of the framework,
        /// such as "maps".
        /// </summary>
        /// <param name="featureName">The directory name for the feature</param>
        /// <returns>A path within the temp directory for storing temporary files related to the feature.</returns>
        internal static string GetTempFeatureDirectory(string featureName)
        {
            string path = s_tempProductDirectory;
            if (path == null)
            {
                s_tempProductDirectory = path = Path.Combine(Path.GetTempPath(), TopLevelHiddenDirectory, SecondLevelDirectory);
            }

            return Path.Combine(path, featureName);
        }

        /// <summary>
        /// Get the location of where to persist information for a particular aspect of the framework,
        /// such as "cryptography".
        /// </summary>
        /// <param name="featureName">The directory name for the feature</param>
        /// <returns>A path within the user's home directory for persisting data for the feature</returns>
        internal static string GetUserFeatureDirectory(string featureName)
        {
            if (s_userProductDirectory == null)
            {
                EnsureUserDirectories();
            }

            return Path.Combine(s_userProductDirectory, featureName);
        }

        /// <summary>
        /// Get the location of where to persist information for a particular aspect of a feature of
        /// the framework, such as "x509stores" within "cryptography".
        /// </summary>
        /// <param name="featureName">The directory name for the feature</param>
        /// <param name="subFeatureName">The directory name for the sub-feature</param>
        /// <returns>A path within the user's home directory for persisting data for the sub-feature</returns>
        internal static string GetUserFeatureDirectory(string featureName, string subFeatureName)
        {
            if (s_userProductDirectory == null)
            {
                EnsureUserDirectories();
            }

            return Path.Combine(s_userProductDirectory, featureName, subFeatureName);
        }

        /// <summary>
        /// Get the location of where to persist information for a particular aspect of the framework,
        /// with a lot of hierarchy, such as ["cryptography", "x509stores", "my"]
        /// </summary>
        /// <param name="featurePathParts">A non-empty set of directories to use for the storage hierarchy</param>
        /// <returns>A path within the user's home directory for persisting data for the feature</returns>
        internal static string GetUserFeatureDirectory(params string[] featurePathParts)
        {
            Debug.Assert(featurePathParts != null);
            Debug.Assert(featurePathParts.Length > 0);

            if (s_userProductDirectory == null)
            {
                EnsureUserDirectories();
            }

            return Path.Combine(s_userProductDirectory, Path.Combine(featurePathParts));
        }

        private static void EnsureUserDirectories()
        {
            string userHomeDirectory = GetHomeDirectory();

            if (string.IsNullOrEmpty(userHomeDirectory))
            {
                throw new InvalidOperationException(SR.PersistedFiles_NoHomeDirectory);
            }

            s_userProductDirectory = Path.Combine(
                userHomeDirectory,
                TopLevelHiddenDirectory,
                SecondLevelDirectory);
        }

        /// <summary>Gets the current user's home directory.</summary>
        /// <returns>The path to the home directory, or null if it could not be determined.</returns>
        internal static string GetHomeDirectory()
        {
            // First try to get the user's home directory from the HOME environment variable.
            // This should work in most cases.
            string userHomeDirectory = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(userHomeDirectory))
                return userHomeDirectory;

            // In initialization conditions, however, the "HOME" enviroment variable may 
            // not yet be set. For such cases, consult with the password entry.
            unsafe
            {
                // First try with a buffer that should suffice for 99% of cases.
                // Note that, theoretically, userHomeDirectory may be null in the success case 
                // if we simply couldn't find a home directory for the current user.
                // In that case, we pass back the null value and let the caller decide
                // what to do.
                const int BufLen = 1024;
                byte* stackBuf = stackalloc byte[BufLen];
                if (TryGetHomeDirectoryFromPasswd(stackBuf, BufLen, out userHomeDirectory))
                    return userHomeDirectory;

                // Fallback to heap allocations if necessary, growing the buffer until
                // we succeed.  TryGetHomeDirectory will throw if there's an unexpected error.
                int lastBufLen = BufLen;
                while (true)
                {
                    lastBufLen *= 2;
                    byte[] heapBuf = new byte[lastBufLen];
                    fixed (byte* buf = heapBuf)
                    {
                        if (TryGetHomeDirectoryFromPasswd(buf, heapBuf.Length, out userHomeDirectory))
                            return userHomeDirectory;
                    }
                }
            }
        }

        /// <summary>Wrapper for getpwuid_r.</summary>
        /// <param name="buf">The scratch buffer to pass into getpwuid_r.</param>
        /// <param name="bufLen">The length of <paramref name="buf"/>.</param>
        /// <param name="path">The resulting path; null if the user didn't have an entry.</param>
        /// <returns>true if the call was successful (path may still be null); false is a larger buffer is needed.</returns>
        private static unsafe bool TryGetHomeDirectoryFromPasswd(byte* buf, int bufLen, out string path)
        {
            while (true)
            {
                // Call getpwuid_r to get the passwd struct
                Interop.Sys.Passwd passwd;
                IntPtr result;
                int rv = Interop.Sys.GetPwUid(Interop.Sys.GetEUid(), out passwd, buf, bufLen, out result);

                // If the call succeeds, give back the home directory path retrieved
                if (rv == 0)
                {
                    if (result == IntPtr.Zero)
                    {
                        // Current user's entry could not be found
                        path = null; // we'll still return true, as false indicates the buffer was too small
                    }
                    else
                    {
                        Debug.Assert(result == (IntPtr)(&passwd));
                        Debug.Assert(passwd.HomeDirectory != null);
                        path = Marshal.PtrToStringAnsi((IntPtr)passwd.HomeDirectory);
                    }
                    return true;
                }

                // If the call failed because it was interrupted, try again.
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.EINTR)
                    continue;

                // If the call failed because the buffer was too small, return false to 
                // indicate the caller should try again with a larger buffer.
                if (errorInfo.Error == Interop.Error.ERANGE)
                {
                    path = null;
                    return false;
                }

                // Otherwise, fail.
                throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
            }
        }

    }
}
