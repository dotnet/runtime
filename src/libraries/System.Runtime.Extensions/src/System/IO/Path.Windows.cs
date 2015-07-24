// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static partial class Path
    {
        public static readonly char DirectorySeparatorChar = '\\';
        public static readonly char VolumeSeparatorChar = ':';
        public static readonly char PathSeparator = ';';

        private const string DirectorySeparatorCharAsString = "\\";

        private static readonly char[] InvalidFileNameChars = 
        { 
            '\"', '<', '>', '|', '\0', 
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10, 
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20, 
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30, 
            (char)31, ':', '*', '?', '\\', '/' 
        };

        // Trim trailing white spaces, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
        // string.WhitespaceChars will trim more aggressively than what the underlying FS does (for ex, NTFS, FAT).
        private static readonly char[] TrimEndChars = { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0 };

        // The max total path is 260, and the max individual component length is 255. 
        // For example, D:\<256 char file name> isn't legal, even though it's under 260 chars.
        internal static readonly int MaxPath = 260;
        private static readonly int MaxComponentLength = 255;
        private const int MaxLongPath = 32000;

        private static bool IsDirectoryOrVolumeSeparator(char c)
        {
            return PathInternal.IsDirectorySeparator(c) || VolumeSeparatorChar == c;
        }

        // Expands the given path to a fully qualified path. 
        [Pure]
        [System.Security.SecuritySafeCritical]
        public static string GetFullPath(string path)
        {
            string fullPath = GetFullPathInternal(path);

            // Emulate FileIOPermissions checks, retained for compatibility
            PathInternal.CheckInvalidPathChars(fullPath, true);

            int startIndex = PathInternal.IsExtended(fullPath) ? PathInternal.ExtendedPathPrefix.Length + 2 : 2;
            if (fullPath.Length > startIndex && fullPath.IndexOf(':', startIndex) != -1)
            {
                throw new NotSupportedException(SR.Argument_PathFormatNotSupported);
            }

            return fullPath;
        }

        /// <summary>
        /// Checks for known bad extended paths (paths that start with \\?\)
        /// </summary>
        /// <param name="fullCheck">Check for invalid characters if true.</param>
        /// <returns>'true' if the path passes validity checks.</returns>
        private static bool ValidateExtendedPath(string path, bool fullCheck)
        {
            if (path.Length == PathInternal.ExtendedPathPrefix.Length)
            {
                // Effectively empty and therefore invalid
                return false;
            }

            if (path.StartsWith(PathInternal.UncExtendedPathPrefix, StringComparison.Ordinal))
            {
                // UNC specific checks
                if (path.Length == PathInternal.UncExtendedPathPrefix.Length || path[PathInternal.UncExtendedPathPrefix.Length] == DirectorySeparatorChar)
                {
                    // Effectively empty and therefore invalid (\\?\UNC\ or \\?\UNC\\)
                    return false;
                }

                int serverShareSeparator = path.IndexOf(DirectorySeparatorChar, PathInternal.UncExtendedPathPrefix.Length);
                if (serverShareSeparator == -1 || serverShareSeparator == path.Length - 1)
                {
                    // Need at least a Server\Share
                    return false;
                }
            }

            // Segments can't be empty "\\" or contain *just* "." or ".."
            char twoBack = '?';
            char oneBack = DirectorySeparatorChar;
            char currentCharacter;
            bool periodSegment = false;
            for (int i = PathInternal.ExtendedPathPrefix.Length; i < path.Length; i++)
            {
                currentCharacter = path[i];
                switch (currentCharacter)
                {
                    case '\\':
                        if (oneBack == DirectorySeparatorChar || periodSegment)
                            throw new ArgumentException(SR.Arg_PathIllegal);
                        periodSegment = false;
                        break;
                    case '.':
                        periodSegment = (oneBack == DirectorySeparatorChar || (twoBack == DirectorySeparatorChar && oneBack == '.'));
                        break;
                    default:
                        periodSegment = false;
                        break;
                }

                twoBack = oneBack;
                oneBack = currentCharacter;
            }

            if (periodSegment)
            {
                return false;
            }

            if (fullCheck)
            {
                // Look for illegal path characters.
                PathInternal.CheckInvalidPathChars(path);
            }

            return true;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static string NormalizePath(string path, bool fullCheck, int maxPathLength, bool expandShortPaths)
        {
            Contract.Requires(path != null, "path can't be null");

            // If the path is in extended syntax, we don't need to normalize, but we still do some basic validity checks
            if (PathInternal.IsExtended(path))
            {
                if (!ValidateExtendedPath(path, fullCheck))
                {
                    throw new ArgumentException(SR.Arg_PathIllegal);
                }

                // \\?\GLOBALROOT gives access to devices out of the scope of the current user, we
                // don't want to allow this for security reasons.
                // https://msdn.microsoft.com/en-us/library/windows/desktop/aa365247.aspx#nt_namespaces
                if (path.StartsWith(@"\\?\globalroot", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException(SR.Arg_PathGlobalRoot);

                return path;
            }

            // If we're doing a full path check, trim whitespace and look for
            // illegal path characters.
            if (fullCheck)
            {
                // Trim whitespace off the end of the string.
                // Win32 normalization trims only U+0020.
                path = path.TrimEnd(TrimEndChars);

                // Look for illegal path characters.
                PathInternal.CheckInvalidPathChars(path);
            }

            int index = 0;

            // We prefer to allocate on the stack for workingset/perf gain. If the 
            // starting path is less than MaxPath then we can stackalloc; otherwise we'll
            // use a StringBuilder (PathHelper does this under the hood). The latter may
            // happen in 2 cases:
            // 1. Starting path is greater than MaxPath but it normalizes down to MaxPath.
            // This is relevant for paths containing escape sequences. In this case, we
            // attempt to normalize down to MaxPath, but the caller pays a perf penalty 
            // since StringBuilder is used. 
            // 2. IsolatedStorage, which supports paths longer than MaxPath (value given 
            // by maxPathLength.
            PathHelper newBuffer = null;
            if (path.Length + 1 <= MaxPath)
            {
                char* m_arrayPtr = stackalloc char[MaxPath];
                newBuffer = new PathHelper(m_arrayPtr, MaxPath);
            }
            else
            {
                newBuffer = new PathHelper(path.Length + MaxPath, maxPathLength);
            }

            uint numSpaces = 0;
            uint numDots = 0;
            bool fixupDirectorySeparator = false;
            // Number of significant chars other than potentially suppressible
            // dots and spaces since the last directory or volume separator char
            uint numSigChars = 0;
            int lastSigChar = -1; // Index of last significant character.
            // Whether this segment of the path (not the complete path) started
            // with a volume separator char.  Reject "c:...".
            bool startedWithVolumeSeparator = false;
            bool firstSegment = true;
            int lastDirectorySeparatorPos = 0;

            bool mightBeShortFileName = false;

            // LEGACY: This code is here for backwards compatibility reasons. It 
            // ensures that \\foo.cs\bar.cs stays \\foo.cs\bar.cs instead of being
            // turned into \foo.cs\bar.cs.
            if (path.Length > 0 && PathInternal.IsDirectorySeparator(path[0]))
            {
                newBuffer.Append('\\');
                index++;
                lastSigChar = 0;
            }

            // Normalize the string, stripping out redundant dots, spaces, and 
            // slashes.
            while (index < path.Length)
            {
                char currentChar = path[index];

                // We handle both directory separators and dots specially.  For 
                // directory separators, we consume consecutive appearances.  
                // For dots, we consume all dots beyond the second in 
                // succession.  All other characters are added as is.  In 
                // addition we consume all spaces after the last other char
                // in a directory name up until the directory separator.

                if (PathInternal.IsDirectorySeparator(currentChar))
                {
                    // If we have a path like "123.../foo", remove the trailing dots.
                    // However, if we found "c:\temp\..\bar" or "c:\temp\...\bar", don't.
                    // Also remove trailing spaces from both files & directory names.
                    // This was agreed on with the OS team to fix undeletable directory
                    // names ending in spaces.

                    // If we saw a '\' as the previous last significant character and
                    // are simply going to write out dots, suppress them.
                    // If we only contain dots and slashes though, only allow
                    // a string like [dot]+ [space]*.  Ignore everything else.
                    // Legal: "\.. \", "\...\", "\. \"
                    // Illegal: "\.. .\", "\. .\", "\ .\"
                    if (numSigChars == 0)
                    {
                        // Dot and space handling
                        if (numDots > 0)
                        {
                            // Look for ".[space]*" or "..[space]*"
                            int start = lastSigChar + 1;
                            if (path[start] != '.')
                                throw new ArgumentException(SR.Arg_PathIllegal);

                            // Only allow "[dot]+[space]*", and normalize the 
                            // legal ones to "." or ".."
                            if (numDots >= 2)
                            {
                                // Reject "C:..."
                                if (startedWithVolumeSeparator && numDots > 2)
                                    throw new ArgumentException(SR.Arg_PathIllegal);

                                if (path[start + 1] == '.')
                                {
                                    // Search for a space in the middle of the
                                    // dots and throw
                                    for (int i = start + 2; i < start + numDots; i++)
                                    {
                                        if (path[i] != '.')
                                            throw new ArgumentException(SR.Arg_PathIllegal);
                                    }

                                    numDots = 2;
                                }
                                else
                                {
                                    if (numDots > 1)
                                        throw new ArgumentException(SR.Arg_PathIllegal);
                                    numDots = 1;
                                }
                            }

                            if (numDots == 2)
                            {
                                newBuffer.Append('.');
                            }

                            newBuffer.Append('.');
                            fixupDirectorySeparator = false;
                            // Continue in this case, potentially writing out '\'.
                        }

                        if (numSpaces > 0 && firstSegment)
                        {
                            // Handle strings like " \\server\share".
                            if (index + 1 < path.Length && PathInternal.IsDirectorySeparator(path[index + 1]))
                            {
                                newBuffer.Append(DirectorySeparatorChar);
                            }
                        }
                    }
                    numDots = 0;
                    numSpaces = 0;  // Suppress trailing spaces

                    if (!fixupDirectorySeparator)
                    {
                        fixupDirectorySeparator = true;
                        newBuffer.Append(DirectorySeparatorChar);
                    }
                    numSigChars = 0;
                    lastSigChar = index;
                    startedWithVolumeSeparator = false;
                    firstSegment = false;

                    // For short file names, we must try to expand each of them as
                    // soon as possible.  We need to allow people to specify a file
                    // name that doesn't exist using a path with short file names
                    // in it, such as this for a temp file we're trying to create:
                    // C:\DOCUME~1\USERNA~1.RED\LOCALS~1\Temp\bg3ylpzp
                    // We could try doing this afterwards piece by piece, but it's
                    // probably a lot simpler to do it here.
                    if (mightBeShortFileName)
                    {
                        newBuffer.TryExpandShortFileName();
                        mightBeShortFileName = false;
                    }

                    int thisPos = newBuffer.Length - 1;
                    if (thisPos - lastDirectorySeparatorPos > MaxComponentLength)
                    {
                        throw new PathTooLongException(SR.IO_PathTooLong);
                    }
                    lastDirectorySeparatorPos = thisPos;
                } // if (Found directory separator)
                else if (currentChar == '.')
                {
                    // Reduce only multiple .'s only after slash to 2 dots. For
                    // instance a...b is a valid file name.
                    numDots++;
                    // Don't flush out non-terminal spaces here, because they may in
                    // the end not be significant.  Turn "c:\ . .\foo" -> "c:\foo"
                    // which is the conclusion of removing trailing dots & spaces,
                    // as well as folding multiple '\' characters.
                }
                else if (currentChar == ' ')
                {
                    numSpaces++;
                }
                else
                {  // Normal character logic
                    if (currentChar == '~' && expandShortPaths)
                        mightBeShortFileName = true;

                    fixupDirectorySeparator = false;

                    // To reject strings like "C:...\foo" and "C  :\foo"
                    if (firstSegment && currentChar == VolumeSeparatorChar)
                    {
                        // Only accept "C:", not "c :" or ":"
                        // Get a drive letter or ' ' if index is 0.
                        char driveLetter = (index > 0) ? path[index - 1] : ' ';
                        bool validPath = ((numDots == 0) && (numSigChars >= 1) && (driveLetter != ' '));
                        if (!validPath)
                            throw new ArgumentException(SR.Arg_PathIllegal);

                        startedWithVolumeSeparator = true;
                        // We need special logic to make " c:" work, we should not fix paths like "  foo::$DATA"
                        if (numSigChars > 1)
                        { // Common case, simply do nothing
                            int spaceCount = 0; // How many spaces did we write out, numSpaces has already been reset.
                            while ((spaceCount < newBuffer.Length) && newBuffer[spaceCount] == ' ')
                                spaceCount++;
                            if (numSigChars - spaceCount == 1)
                            {
                                //Safe to update stack ptr directly
                                newBuffer.Length = 0;
                                newBuffer.Append(driveLetter); // Overwrite spaces, we need a special case to not break "  foo" as a relative path.
                            }
                        }
                        numSigChars = 0;
                    }
                    else
                    {
                        numSigChars += 1 + numDots + numSpaces;
                    }

                    // Copy any spaces & dots since the last significant character
                    // to here.  Note we only counted the number of dots & spaces,
                    // and don't know what order they're in.  Hence the copy.
                    if (numDots > 0 || numSpaces > 0)
                    {
                        int numCharsToCopy = (lastSigChar >= 0) ? index - lastSigChar - 1 : index;
                        if (numCharsToCopy > 0)
                        {
                            for (int i = 0; i < numCharsToCopy; i++)
                            {
                                newBuffer.Append(path[lastSigChar + 1 + i]);
                            }
                        }
                        numDots = 0;
                        numSpaces = 0;
                    }

                    newBuffer.Append(currentChar);
                    lastSigChar = index;
                }

                index++;
            } // end while

            if (newBuffer.Length - 1 - lastDirectorySeparatorPos > MaxComponentLength)
            {
                throw new PathTooLongException(SR.IO_PathTooLong);
            }

            // Drop any trailing dots and spaces from file & directory names, EXCEPT
            // we MUST make sure that "C:\foo\.." is correctly handled.
            // Also handle "C:\foo\." -> "C:\foo", while "C:\." -> "C:\"
            if (numSigChars == 0)
            {
                if (numDots > 0)
                {
                    // Look for ".[space]*" or "..[space]*"
                    int start = lastSigChar + 1;
                    if (path[start] != '.')
                        throw new ArgumentException(SR.Arg_PathIllegal);

                    // Only allow "[dot]+[space]*", and normalize the 
                    // legal ones to "." or ".."
                    if (numDots >= 2)
                    {
                        // Reject "C:..."
                        if (startedWithVolumeSeparator && numDots > 2)
                            throw new ArgumentException(SR.Arg_PathIllegal);

                        if (path[start + 1] == '.')
                        {
                            // Search for a space in the middle of the
                            // dots and throw
                            for (int i = start + 2; i < start + numDots; i++)
                            {
                                if (path[i] != '.')
                                    throw new ArgumentException(SR.Arg_PathIllegal);
                            }

                            numDots = 2;
                        }
                        else
                        {
                            if (numDots > 1)
                                throw new ArgumentException(SR.Arg_PathIllegal);
                            numDots = 1;
                        }
                    }

                    if (numDots == 2)
                    {
                        newBuffer.Append('.');
                    }

                    newBuffer.Append('.');
                }
            } // if (numSigChars == 0)

            // If we ended up eating all the characters, bail out.
            if (newBuffer.Length == 0)
                throw new ArgumentException(SR.Arg_PathIllegal);

            // Disallow URL's here.  Some of our other Win32 API calls will reject
            // them later, so we might be better off rejecting them here.
            // Note we've probably turned them into "file:\D:\foo.tmp" by now.
            // But for compatibility, ensure that callers that aren't doing a 
            // full check aren't rejected here.
            if (fullCheck)
            {
                if (newBuffer.OrdinalStartsWith("http:", false) ||
                    newBuffer.OrdinalStartsWith("file:", false))
                {
                    throw new ArgumentException(SR.Argument_PathUriFormatNotSupported);
                }
            }

            // If the last part of the path (file or directory name) had a tilde,
            // expand that too.
            if (mightBeShortFileName)
            {
                newBuffer.TryExpandShortFileName();
            }

            // Call the Win32 API to do the final canonicalization step.
            int result = 1;

            if (fullCheck)
            {
                // NOTE: Win32 GetFullPathName requires the input buffer to be big enough to fit the initial
                // path which is a concat of CWD and the relative path, this can be of an arbitrary
                // size and could be > MaxPath (which becomes an artificial limit at this point),
                // even though the final normalized path after fixing up the relative path syntax
                // might be well within the MaxPath restriction. For ex,
                // "c:\SomeReallyLongDirName(thinkGreaterThan_MAXPATH)\..\foo.txt" which actually requires a
                // buffer well with in the MaxPath as the normalized path is just "c:\foo.txt"
                // This buffer requirement seems wrong, it could be a bug or a perf optimization
                // like returning required buffer length quickly or avoid stratch buffer etc.
                // Either way we need to workaround it here...

                // Ideally we would get the required buffer length first by calling GetFullPathName
                // once without the buffer and use that in the later call but this doesn't always work
                // due to Win32 GetFullPathName bug. For instance, in Win2k, when the path we are trying to
                // fully qualify is a single letter name (such as "a", "1", ",") GetFullPathName
                // fails to return the right buffer size (i.e, resulting in insufficient buffer).
                // To workaround this bug we will start with MaxPath buffer and grow it once if the
                // return value is > MaxPath.

                result = newBuffer.GetFullPathName();

                // If we called GetFullPathName with something like "foo" and our
                // command window was in short file name mode (ie, by running edlin or
                // DOS versions of grep, etc), we might have gotten back a short file
                // name.  So, check to see if we need to expand it.
                mightBeShortFileName = false;
                for (int i = 0; i < newBuffer.Length && !mightBeShortFileName; i++)
                {
                    if (newBuffer[i] == '~' && expandShortPaths)
                        mightBeShortFileName = true;
                }

                if (mightBeShortFileName)
                {
                    bool r = newBuffer.TryExpandShortFileName();
                    // Consider how the path "Doesn'tExist" would expand.  If
                    // we add in the current directory, it too will need to be
                    // fully expanded, which doesn't happen if we use a file
                    // name that doesn't exist.
                    if (!r)
                    {
                        int lastSlash = -1;

                        for (int i = newBuffer.Length - 1; i >= 0; i--)
                        {
                            if (newBuffer[i] == DirectorySeparatorChar)
                            {
                                lastSlash = i;
                                break;
                            }
                        }

                        if (lastSlash >= 0)
                        {
                            // This bounds check is for safe memcpy but we should never get this far 
                            if (newBuffer.Length >= maxPathLength)
                                throw new PathTooLongException(SR.IO_PathTooLong);

                            int lenSavedName = newBuffer.Length - lastSlash - 1;
                            Debug.Assert(lastSlash < newBuffer.Length, "path unexpectedly ended in a '\'");

                            newBuffer.Fixup(lenSavedName, lastSlash);
                        }
                    }
                }
            }

            if (result != 0)
            {
                /* Throw an ArgumentException for paths like \\, \\server, \\server\
                   This check can only be properly done after normalizing, so
                   \\foo\.. will be properly rejected. */
                if (newBuffer.Length > 1 && newBuffer[0] == '\\' && newBuffer[1] == '\\')
                {
                    int startIndex = 2;
                    while (startIndex < result)
                    {
                        if (newBuffer[startIndex] == '\\')
                        {
                            startIndex++;
                            break;
                        }
                        else
                        {
                            startIndex++;
                        }
                    }
                    if (startIndex == result)
                        throw new ArgumentException(SR.Arg_PathIllegalUNC);
                }
            }

            // Check our result and form the managed string as necessary.
            if (newBuffer.Length >= maxPathLength)
                throw new PathTooLongException(SR.IO_PathTooLong);

            if (result == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == 0)
                    errorCode = Interop.mincore.Errors.ERROR_BAD_PATHNAME;
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            }

            string returnVal = newBuffer.ToString();
            if (string.Equals(returnVal, path, StringComparison.Ordinal))
            {
                returnVal = path;
            }
            return returnVal;
        }

        [System.Security.SecuritySafeCritical]
        public static string GetTempPath()
        {
            StringBuilder sb = StringBuilderCache.Acquire(MaxPath);
            uint r = Interop.mincore.GetTempPathW(MaxPath, sb);
            if (r == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return GetFullPathInternal(StringBuilderCache.GetStringAndRelease(sb));
        }

        [System.Security.SecurityCritical]
        private static string InternalGetTempFileName(bool checkHost)
        {
            // checkHost was originally intended for file security checks, but is ignored.

            string path = GetTempPath();

            StringBuilder sb = StringBuilderCache.Acquire(MaxPath);
            uint r = Interop.mincore.GetTempFileNameW(path, "tmp", 0, sb);
            if (r == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // Tests if the given path contains a root. A path is considered rooted
        // if it starts with a backslash ("\") or a drive letter and a colon (":").
        [Pure]
        public static bool IsPathRooted(string path)
        {
            if (path != null)
            {
                PathInternal.CheckInvalidPathChars(path);

                int length = path.Length;
                if ((length >= 1 && PathInternal.IsDirectorySeparator(path[0])) || 
                    (length >= 2 && path[1] == VolumeSeparatorChar))
                    return true;
            }
            return false;
        }
    }
}
