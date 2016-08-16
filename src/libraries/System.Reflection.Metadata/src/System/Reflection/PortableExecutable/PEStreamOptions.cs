// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.PortableExecutable
{
    [Flags]
    public enum PEStreamOptions
    {
        /// <summary>
        /// By default the stream is disposed when <see cref="PEReader"/> is disposed and sections of the PE image are read lazily.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Keep the stream open when the <see cref="PEReader"/> is disposed.
        /// </summary>
        LeaveOpen = 1,

        /// <summary>
        /// Reads metadata section into memory right away. 
        /// </summary>
        /// <remarks>
        /// Reading from other sections of the file is not allowed (<see cref="InvalidOperationException"/> is thrown by the <see cref="PEReader"/>).
        /// The underlying file may be closed and even deleted after <see cref="PEReader"/> is constructed.
        /// 
        /// <see cref="PEReader"/> closes the stream automatically by the time the constructor returns unless <see cref="LeaveOpen"/> is specified.
        /// </remarks>
        PrefetchMetadata = 1 << 1,

        /// <summary>
        /// Reads the entire image into memory right away. 
        /// </summary>
        /// <remarks>
        /// <see cref="PEReader"/> closes the stream automatically by the time the constructor returns unless <see cref="LeaveOpen"/> is specified.
        /// </remarks>
        PrefetchEntireImage = 1 << 2,

        /// <summary>
        /// Indicates that the underlying PE image has been loaded into memory by the OS loader.
        /// </summary>
        IsLoadedImage = 1 << 3,
    }

    internal static class PEStreamOptionsExtensions
    {
        public static bool IsValid(this PEStreamOptions options)
        {
            return (options & ~(PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchEntireImage | PEStreamOptions.PrefetchMetadata | PEStreamOptions.IsLoadedImage)) == 0;
        }
    }
}
