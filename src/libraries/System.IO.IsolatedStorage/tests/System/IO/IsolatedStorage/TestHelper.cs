// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO.IsolatedStorage
{
    public static class TestHelper
    {
        private static PropertyInfo s_rootDirectoryProperty;
        private static List<string> s_roots;

        static TestHelper()
        {
            s_rootDirectoryProperty = typeof(IsolatedStorageFile).GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance);

            s_roots = new List<string>();

            string hash;
            object identity;
            Helper.GetDefaultIdentityAndHash(out identity, out hash, '.');

            string userRoot = Helper.GetDataDirectory(IsolatedStorageScope.User);
            string randomUserRoot = Helper.GetRandomDirectory(userRoot, IsolatedStorageScope.User);
            s_roots.Add(Path.Combine(randomUserRoot, hash));

            // Application scope doesn't go under a random dir
            s_roots.Add(Path.Combine(userRoot, hash));

            // https://github.com/dotnet/corefx/issues/12628
            // https://github.com/dotnet/corefx/issues/19839
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && !PlatformDetection.IsInAppContainer)
            {
                s_roots.Add(Helper.GetDataDirectory(IsolatedStorageScope.Machine));
            }

            // We don't expose Roaming yet
            // Helper.GetDataDirectory(IsolatedStorageScope.Roaming);
        }

        /// <summary>
        /// Where the user's files go
        /// </summary>
        public static string GetUserRootDirectory(this IsolatedStorageFile isf)
        {
            // CoreFX and NetFX use the same internal property
            return (string)s_rootDirectoryProperty.GetValue(isf);
        }

        /// <summary>
        /// The actual root of the store (housekeeping files are kept here in NetFX)
        /// </summary>
        public static string GetIdentityRootDirectory(this IsolatedStorageFile isf)
        {
            return Path.GetDirectoryName(isf.GetUserRootDirectory().TrimEnd(Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Simple wrapper to create the given file (and close the handle)
        /// </summary>
        public static void CreateTestFile(this IsolatedStorageFile isf, string fileName, string content = null)
        {
            using (var stream = isf.CreateFile(fileName))
            {
                if (content != null)
                    stream.WriteAllText(content);
            }
        }

        public static void WriteAllText(this IsolatedStorageFile isf, string fileName, string content)
        {
            using (var stream = isf.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                stream.WriteAllText(content);
            }
        }

        public static void WriteAllText(this IsolatedStorageFileStream stream, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            stream.Write(buffer, offset: 0, count: buffer.Length);
        }

        public static string ReadAllText(this IsolatedStorageFile isf, string fileName)
        {
            using (var stream = isf.OpenFile(fileName, FileMode.Open, FileAccess.Read))
            {
                return stream.ReadAllText();
            }
        }

        public static string ReadAllText(this IsolatedStorageFileStream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public static void WipeStores()
        {
            foreach (string root in s_roots)
            {
                try
                {
                    if (Directory.Exists(root))
                    {
                        Directory.Delete(root, recursive: true);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to wipe stores: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Returns true if the given time is within a minute of the current time
        /// </summary>
        public static bool IsTimeCloseToNow(DateTimeOffset time)
        {
            DateTimeOffset currentTime = DateTimeOffset.Now;
            TimeSpan difference = currentTime - time;
            return difference.TotalMinutes < 1.0;
        }
    }
}
