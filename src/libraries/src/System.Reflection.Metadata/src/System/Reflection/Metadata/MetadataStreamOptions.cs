// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    [Flags]
    public enum MetadataStreamOptions
    {
        /// <summary>
        /// By default the stream is disposed when <see cref="MetadataReaderProvider"/> is disposed and sections of the PE image are read lazily.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Keep the stream open when the <see cref="MetadataReaderProvider"/> is disposed.
        /// </summary>
        LeaveOpen = 1,

        /// <summary>
        /// Reads PDB metadata into memory right away. 
        /// </summary>
        /// <remarks>
        /// The underlying file may be closed and even deleted after <see cref="MetadataReaderProvider"/> is constructed.
        /// <see cref="MetadataReaderProvider"/> closes the stream automatically by the time the constructor returns unless <see cref="LeaveOpen"/> is specified.
        /// </remarks>
        PrefetchMetadata = 1 << 1,
    }

    internal static class MetadataStreamOptionsExtensions
    {
        public static bool IsValid(this MetadataStreamOptions options)
        {
            return (options & ~(MetadataStreamOptions.LeaveOpen | MetadataStreamOptions.PrefetchMetadata)) == 0;
        }
    }
}
