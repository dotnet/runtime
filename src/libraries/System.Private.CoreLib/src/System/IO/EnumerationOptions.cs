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
    /// <summary>Provides file and directory enumeration options.</summary>
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

        /// <summary>Initializes a new instance of the <see cref="EnumerationOptions" /> class with the recommended default options.</summary>
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

        /// <summary>Gets or sets a value that indicates whether to recurse into subdirectories while enumerating. The default is <see langword="false" />.</summary>
        /// <value><see langword="true" /> to recurse into subdirectories; otherwise, <see langword="false" />.</value>
        public bool RecurseSubdirectories { get; set; }

        /// <summary>Gets or sets a value that indicates whether to skip files or directories when access is denied (for example, <see cref="System.UnauthorizedAccessException" /> or <see cref="System.Security.SecurityException" />). The default is <see langword="true" />.</summary>
        /// <value><see langword="true" /> to skip innacessible files or directories; otherwise, <see langword="false" />.</value>
        public bool IgnoreInaccessible { get; set; }

        /// <summary>Gets or sets the suggested buffer size, in bytes. The default is 0 (no suggestion).</summary>
        /// <value>The buffer size.</value>
        /// <remarks>Not all platforms use user allocated buffers, and some require either fixed buffers or a buffer that has enough space to return a full result.
        /// One scenario where this option is useful is with remote share enumeration on Windows. Having a large buffer may result in better performance as more results can be batched over the wire (for example, over a network share).
        /// A "large" buffer, for example, would be 16K. Typical is 4K.
        /// The suggested buffer size will not be used if it has no meaning for the native APIs on the current platform or if it would be too small for getting at least a single result.</remarks>
        public int BufferSize { get; set; }

        /// <summary>Gets or sets the attributes to skip. The default is <c>FileAttributes.Hidden | FileAttributes.System</c>.</summary>
        /// <value>The attributes to skip.</value>
        public FileAttributes AttributesToSkip { get; set; }

        /// <summary>Gets or sets the match type.</summary>
        /// <value>One of the enumeration values that indicates the match type.</value>
        /// <remarks>For APIs that allow specifying a match expression, this property allows you to specify how to interpret the match expression.
        /// The default is simple matching where '*' is always 0 or more characters and '?' is a single character.</remarks>
        public MatchType MatchType { get; set; }

        /// <summary>Gets or sets the case matching behavior.</summary>
        /// <value>One of the enumeration values that indicates the case matching behavior.</value>
        /// <remarks>For APIs that allow specifying a match expression, this property allows you to specify the case matching behavior.
        /// The default is to match platform defaults, which are gleaned from the case sensitivity of the temporary folder.</remarks>
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

        /// <summary>Gets or sets a value that indicates whether to return the special directory entries "." and "..".</summary>
        /// <value><see langword="true" /> to return the special directory entries "." and ".."; otherwise, <see langword="false" />.</value>
        public bool ReturnSpecialDirectories { get; set; }
    }
}
