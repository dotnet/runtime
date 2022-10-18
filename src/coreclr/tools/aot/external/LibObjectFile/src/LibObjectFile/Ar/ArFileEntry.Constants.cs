// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Ar
{
    public abstract partial class ArFile
    {
        /// <summary>
        /// Size in bytes of an AR file entry
        /// </summary>
        public const int FileEntrySizeInBytes = 60;

        /// <summary>
        /// Offset of the filename in the entry
        /// </summary>
        public const int FieldNameOffset = 0;

        /// <summary>
        /// Length in bytes of the filename in the entry
        /// </summary>
        public const int FieldNameLength = 16;

        /// <summary>
        /// Offset of the timestamp in the entry
        /// </summary>
        public const int FieldTimestampOffset = 16;

        /// <summary>
        /// Length in bytes of the timestamp in the entry
        /// </summary>
        public const int FieldTimestampLength = 12;

        /// <summary>
        /// Offset of the owner ID in the entry
        /// </summary>
        public const int FieldOwnerIdOffset = 28;

        /// <summary>
        /// Length in bytes of the timestamp in the entry
        /// </summary>
        public const int FieldOwnerIdLength = 6;

        /// <summary>
        /// Offset of the group ID in the entry
        /// </summary>
        public const int FieldGroupIdOffset = 34;

        /// <summary>
        /// Length in bytes of the timestamp in the entry
        /// </summary>
        public const int FieldGroupIdLength = 6;

        /// <summary>
        /// Offset of the file mode in the entry
        /// </summary>
        public const int FieldFileModeOffset = 40;

        /// <summary>
        /// Length in bytes of the timestamp in the entry
        /// </summary>
        public const int FieldFileModeLength = 8;

        /// <summary>
        /// Offset of the file size in the entry
        /// </summary>
        public const int FieldFileSizeOffset = 48;

        /// <summary>
        /// Length in bytes of the timestamp in the entry
        /// </summary>
        public const int FieldFileSizeLength = 10;

        /// <summary>
        /// Offset of the end characters in the entry
        /// </summary>
        public const int FieldEndCharactersOffset = 58;

        /// <summary>
        /// Length in bytes of the end characters in the entry
        /// </summary>
        public const int FieldEndCharactersLength = 2;
   }
}