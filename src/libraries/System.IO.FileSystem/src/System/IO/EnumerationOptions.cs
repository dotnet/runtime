// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    public class EnumerationOptions
    {
        private int _maxRecursionDepth;

        internal const int DefaultMaxRecursionDepth = int.MaxValue;

        /// <summary>
        /// For internal use. These are the options we want to use if calling the existing Directory/File APIs where you don't
        /// explicitly specify EnumerationOptions.
        /// </summary>
        internal static EnumerationOptions Compatible { get; } =
            new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = false };

        private static EnumerationOptions CompatibleRecursive { get; } =
            new EnumerationOptions { RecurseSubdirectories = true, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = false };

        /// <summary>
        /// Internal singleton for default options.
        /// </summary>
        internal static EnumerationOptions Default { get; } = new EnumerationOptions();

        /// <summary>
        /// Default constructor. Constructs the options class with recommended default options.
        /// </summary>
        public EnumerationOptions()
        {
            IgnoreInaccessible = true;
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System;
            MaxRecursionDepth = DefaultMaxRecursionDepth;
        }

        /// <summary>
        /// Converts SearchOptions to FindOptions. Throws if undefined SearchOption.
        /// </summary>
        internal static EnumerationOptions FromSearchOption(SearchOption searchOption)
        {
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return searchOption == SearchOption.AllDirectories ? CompatibleRecursive : Compatible;
        }

        /// <summary>
        /// Should we recurse into subdirectories while enumerating?
        /// Default is false.
        /// </summary>
        public bool RecurseSubdirectories { get; set; }

        /// <summary>
        /// Skip files/directories when access is denied (e.g. AccessDeniedException/SecurityException).
        /// Default is true.
        /// </summary>
        public bool IgnoreInaccessible { get; set; }

        /// <summary>
        /// Suggested buffer size, in bytes. Default is 0 (no suggestion).
        /// </summary>
        /// <remarks>
        /// Not all platforms use user allocated buffers, and some require either fixed buffers or a
        /// buffer that has enough space to return a full result. One scenario where this option is
        /// useful is with remote share enumeration on Windows. Having a large buffer may result in
        /// better performance as more results can be batched over the wire (e.g. over a network
        /// share). A "large" buffer, for example, would be 16K. Typical is 4K.
        ///
        /// We will not use the suggested buffer size if it has no meaning for the native APIs on the
        /// current platform or if it would be too small for getting at least a single result.
        /// </remarks>
        public int BufferSize { get; set; }

        /// <summary>
        /// Skip entries with the given attributes. Default is FileAttributes.Hidden | FileAttributes.System.
        /// </summary>
        public FileAttributes AttributesToSkip { get; set; }

        /// <summary>
        /// For APIs that allow specifying a match expression this will allow you to specify how
        /// to interpret the match expression.
        /// </summary>
        /// <remarks>
        /// The default is simple matching where '*' is always 0 or more characters and '?' is a single character.
        /// </remarks>
        public MatchType MatchType { get; set; }

        /// <summary>
        /// For APIs that allow specifying a match expression this will allow you to specify case matching behavior.
        /// </summary>
        /// <remarks>
        /// Default is to match platform defaults, which are gleaned from the case sensitivity of the temporary folder.
        /// </remarks>
        public MatchCasing MatchCasing { get; set; }

        /// <summary>Gets or sets a value that indicates the maximum directory depth to recurse while enumerating, when <see cref="RecurseSubdirectories" /> is set to <see langword="true" />.</summary>
        /// <value>A number that represents the maximum directory depth to recurse while enumerating. The default value is <see cref="int.MaxValue" />.</value>
        /// <remarks>If <see cref="MaxRecursionDepth" /> is set to a negative number, the default value <see cref="int.MaxValue" /> is used.
        /// If <see cref="MaxRecursionDepth" /> is set to zero, enumeration returns the contents of the initial directory.</remarks>
        public int MaxRecursionDepth
        {
            get => _maxRecursionDepth;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);
                }

                _maxRecursionDepth = value;
            }
        }

        /// <summary>
        /// Set to true to return "." and ".." directory entries.
        /// </summary>
        public bool ReturnSpecialDirectories { get; set; }
    }
}
