// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    public partial class ZipArchiveEntry
    {
        internal const ZipVersionMadeByPlatform CurrentZipPlatform = ZipVersionMadeByPlatform.Windows;

        /// <summary>
        /// To get the file name of a ZipArchiveEntry, we should be parsing the FullName based
        /// on the path specifications and requirements of the OS that ZipArchive was created on.
        /// This method takes in a FullName and the platform of the ZipArchiveEntry and returns
        /// the platform-correct file name.
        /// </summary>
        internal static string ParseFileName(string path, ZipVersionMadeByPlatform madeByPlatform)
        {
            return Path.GetFileName(path);
        }
    }
}
