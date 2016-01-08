// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    public partial class ZipArchiveEntry
    {
        internal const ZipVersionMadeByPlatform CurrentZipPlatform = ZipVersionMadeByPlatform.Unix;

        /// <summary>
        /// To get the file name of a ZipArchiveEntry, we should be parsing the FullName based
        /// on the path specifications and requirements of the OS that ZipArchive was created on.
        /// This method takes in a FullName and the platform of the ZipArchiveEntry and returns
        /// the platform-correct file name.
        /// </summary>
        internal static string ParseFileName(string path, ZipVersionMadeByPlatform madeByPlatform)
        {
            // Validation checking is done based on current OS, not source OS.
            PathInternal.CheckInvalidPathChars(path);

            if (madeByPlatform == ZipVersionMadeByPlatform.Windows)
            {
                int length = path.Length;
                for (int i = length; --i >= 0;)
                {
                    char ch = path[i];
                    if (ch == '\\' || ch == '/' || ch == ':')
                        return path.Substring(i + 1);
                }
                return path;
            }
            else
            {
                return Path.GetFileName(path);
            }
        }
    }
}
