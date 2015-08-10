// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal partial class Interop
{
    internal partial class mincore
    {
        internal const int MAX_PATH = 260;

        // Technically the maximum file/directory name is whatever GetVolumeInformation tells you in
        // MaximumComponentLength. For most file systems this is 255 (UDF is the notable exception at
        // 254).
        //
        // CreateDirectory will refuse directories that are over MAX_PATH - 12 (8.3 filename length).
        // This count includes the drive and NULL. This limitation existed to allow "del *.*" to work
        // successfully and now appears to be moot as you can create files in a directory that is over
        // 248 (up to MAX_PATH) and "del *.*" on them on both FAT and NTFS.
        //
        // Using extended syntax (\\?\) will allow creation of directory names that are full length
        // (e.g. 255 characters). MKDIR/MD, however, does not check extended syntax and will fail out
        // ANY string that is longer than OR equal to MAX_PATH. This effectively makes the longest
        // directory you can create 252 characters, excluding the prefix and drive (\\?\C:\).
        internal const int MAX_DIRECTORY_PATH = 248;
        internal const int CREDUI_MAX_USERNAME_LENGTH = 513;
    }
}
