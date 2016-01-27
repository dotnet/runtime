// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: A collection of path manipulation methods.
**
**
===========================================================*/

using System;
using System.Security.Permissions;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
#if FEATURE_LEGACYSURFACE
using System.Security.Cryptography;
#endif
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.IO {
    // Provides methods for processing directory strings in an ideally
    // cross-platform manner.  Most of the methods don't do a complete
    // full parsing (such as examining a UNC hostname), but they will
    // handle most string operations.  
    [ComVisible(true)]
    public static class Path
    {
        // Platform specific directory separator character.  This is backslash
        // ('\') on Windows and slash ('/') on Unix.
        // 
#if !PLATFORM_UNIX        
        public static readonly char DirectorySeparatorChar = '\\';
        internal const string DirectorySeparatorCharAsString = "\\";
#else
        public static readonly char DirectorySeparatorChar = '/';
        internal const string DirectorySeparatorCharAsString = "/";
#endif // !PLATFORM_UNIX
        
        // Platform specific alternate directory separator character.  
        // There is only one directory separator char on Unix, 
        // so the same definition is used for both Unix and Windows.
        public static readonly char AltDirectorySeparatorChar = '/';
    
        // Platform specific volume separator character.  This is colon (':')
        // on Windows and MacOS, and slash ('/') on Unix.  This is mostly
        // useful for parsing paths like "c:\windows" or "MacVolume:System Folder".  
        // 
#if !PLATFORM_UNIX
        public static readonly char VolumeSeparatorChar = ':';
#else
        public static readonly char VolumeSeparatorChar = '/';
#endif // !PLATFORM_UNIX        
        
        // Platform specific invalid list of characters in a path.
        // See the "Naming a File" MSDN conceptual docs for more details on
        // what is valid in a file name (which is slightly different from what
        // is legal in a path name).
        // Note: This list is duplicated in CheckInvalidPathChars
        [Obsolete("Please use GetInvalidPathChars or GetInvalidFileNameChars instead.")]
#if !PLATFORM_UNIX
        public static readonly char[] InvalidPathChars = { '\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31 };
#else
        public static readonly char[] InvalidPathChars = { '\0' };
#endif // !PLATFORM_UNIX

        // Trim trailing white spaces, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
        // String.WhitespaceChars will trim aggressively than what the underlying FS does (for ex, NTFS, FAT).    
        internal static readonly char[] TrimEndChars = { (char) 0x9, (char) 0xA, (char) 0xB, (char) 0xC, (char) 0xD, (char) 0x20,   (char) 0x85, (char) 0xA0};
        
#if !PLATFORM_UNIX
        private static readonly char[] RealInvalidPathChars = { '\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31 };

        // This is used by HasIllegalCharacters
        private static readonly char[] InvalidPathCharsWithAdditionalChecks = { '\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31, '*', '?' };

        private static readonly char[] InvalidFileNameChars = { '\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31, ':', '*', '?', '\\', '/' };
#else
        private static readonly char[] RealInvalidPathChars = { '\0' };

        // This is used by HasIllegalCharacters
        private static readonly char[] InvalidPathCharsWithAdditionalChecks = { '\0', '*', '?' };

        private static readonly char[] InvalidFileNameChars = { '\0', '/' };
#endif // !PLATFORM_UNIX

#if !PLATFORM_UNIX
        public static readonly char PathSeparator = ';';
#else
        public static readonly char PathSeparator = ':';
#endif // !PLATFORM_UNIX


        // Make this public sometime.
        // The max total path is 260, and the max individual component length is 255. 
        // For example, D:\<256 char file name> isn't legal, even though it's under 260 chars.
#if !PLATFORM_UNIX
        internal static readonly int MaxPath = 260;
#else
        internal static readonly int MaxPath = 1024;
#endif

        private static readonly int MaxDirectoryLength = 255;

        // Windows API definitions
        internal const int MAX_PATH = 260;  // From WinDef.h
        internal const int MAX_DIRECTORY_PATH = 248;   // cannot create directories greater than 248 characters
    
        // Changes the extension of a file path. The path parameter
        // specifies a file path, and the extension parameter
        // specifies a file extension (with a leading period, such as
        // ".exe" or ".cs").
        //
        // The function returns a file path with the same root, directory, and base
        // name parts as path, but with the file extension changed to
        // the specified extension. If path is null, the function
        // returns null. If path does not contain a file extension,
        // the new file extension is appended to the path. If extension
        // is null, any exsiting extension is removed from path.
        //
        public static String ChangeExtension(String path, String extension) {
            if (path != null) {
                CheckInvalidPathChars(path);
    
                String s = path;
                for (int i = path.Length; --i >= 0;) {
                    char ch = path[i];
                    if (ch == '.') {
                        s = path.Substring(0, i);
                        break;
                    }
                    if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar) break;
                }
                if (extension != null && path.Length != 0) {
                    if (extension.Length == 0 || extension[0] != '.') {
                        s = s + ".";
                    }
                    s = s + extension;
                }
                return s;
            }
            return null;
        }

       
        // Returns the directory path of a file path. This method effectively
        // removes the last element of the given file path, i.e. it returns a
        // string consisting of all characters up to but not including the last
        // backslash ("\") in the file path. The returned value is null if the file
        // path is null or if the file path denotes a root (such as "\", "C:", or
        // "\\server\share").
        //
        public static String GetDirectoryName(String path) {
            if (path != null) {
                CheckInvalidPathChars(path);

#if FEATURE_LEGACYNETCF
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
#endif

                string normalizedPath = NormalizePath(path, false);

                // If there are no permissions for PathDiscovery to this path, we should NOT expand the short paths
                // as this would leak information about paths to which the user would not have access to.
                if (path.Length > 0)
                {
                    try
                    {
                        // If we were passed in a path with \\?\ we need to remove it as FileIOPermission does not like it.
                        string tempPath = Path.RemoveLongPathPrefix(path);

                        // FileIOPermission cannot handle paths that contain ? or *
                        // So we only pass to FileIOPermission the text up to them.
                        int pos = 0;
                        while (pos < tempPath.Length && (tempPath[pos] != '?' && tempPath[pos] != '*')) 
                            pos++;

                        // GetFullPath will Demand that we have the PathDiscovery FileIOPermission and thus throw 
                        // SecurityException if we don't. 
                        // While we don't use the result of this call we are using it as a consistent way of 
                        // doing the security checks. 
                        if (pos > 0)
                            Path.GetFullPath(tempPath.Substring(0, pos));
                    }
                    catch (SecurityException) {
                        // If the user did not have permissions to the path, make sure that we don't leak expanded short paths
                        // Only re-normalize if the original path had a ~ in it.
                        if (path.IndexOf("~", StringComparison.Ordinal) != -1)
                        {
                            normalizedPath = NormalizePath(path, /*fullCheck*/ false, /*expandShortPaths*/ false);
                        }
                    }
                    catch (PathTooLongException) { }
                    catch (NotSupportedException) { }  // Security can throw this on "c:\foo:"
                    catch (IOException) { }
                    catch (ArgumentException) { } // The normalizePath with fullCheck will throw this for file: and http:
                }

                path = normalizedPath;

#if FEATURE_LEGACYNETCF
                }
#endif

                int root = GetRootLength(path);
                int i = path.Length;
                if (i > root) {
                    i = path.Length;
                    if (i == root) return null;
                    while (i > root && path[--i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar);                    
                    String dir = path.Substring(0, i);
#if FEATURE_LEGACYNETCF
                    if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {                        
                        if (dir.Length >= MaxPath - 1)
                            throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                    }                     
#endif
                    return dir;
                }
            }
            return null;
        }

        // Gets the length of the root DirectoryInfo or whatever DirectoryInfo markers
        // are specified for the first part of the DirectoryInfo name.
        // 
        internal static int GetRootLength(String path) {
            CheckInvalidPathChars(path);
            
            int i = 0;
            int length = path.Length;

#if !PLATFORM_UNIX
            if (length >= 1 && (IsDirectorySeparator(path[0]))) {
                // handles UNC names and directories off current drive's root.
                i = 1;
                if (length >= 2 && (IsDirectorySeparator(path[1]))) {
                    i = 2;
                    int n = 2;
                    while (i < length && ((path[i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar) || --n > 0)) i++;
                }
            }
            else if (length >= 2 && path[1] == VolumeSeparatorChar) {
                // handles A:\foo.
                i = 2;
                if (length >= 3 && (IsDirectorySeparator(path[2]))) i++;
            }
            return i;
#else
            if (length >= 1 && (IsDirectorySeparator(path[0]))) {
                i = 1;
            }
            return i;
#endif // !PLATFORM_UNIX
        }

        internal static bool IsDirectorySeparator(char c) {
            return (c==DirectorySeparatorChar || c == AltDirectorySeparatorChar);
        }


        public static char[] GetInvalidPathChars()
        {
            return (char[]) RealInvalidPathChars.Clone();
        }

        public static char[] GetInvalidFileNameChars()
        {
            return (char[]) InvalidFileNameChars.Clone();
        }

        // Returns the extension of the given path. The returned value includes the
        // period (".") character of the extension except when you have a terminal period when you get String.Empty, such as ".exe" or
        // ".cpp". The returned value is null if the given path is
        // null or if the given path does not include an extension.
        //
        [Pure]
        public static String GetExtension(String path) {
            if (path==null)
                return null;

            CheckInvalidPathChars(path);
            int length = path.Length;
            for (int i = length; --i >= 0;) {
                char ch = path[i];
                if (ch == '.')
                {
                    if (i != length - 1)
                        return path.Substring(i, length - i);
                    else
                        return String.Empty;
                }
                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                    break;
            }
            return String.Empty;
        }

        // Expands the given path to a fully qualified path. The resulting string
        // consists of a drive letter, a colon, and a root relative path. This
        // function does not verify that the resulting path 
        // refers to an existing file or directory on the associated volume.
        [Pure]
        [System.Security.SecuritySafeCritical]
        public static String GetFullPath(String path) {
            String fullPath = GetFullPathInternal(path);
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, path, fullPath);
            state.EnsureState();
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, fullPath, false, false);
#endif
            return fullPath;
        }

        [System.Security.SecurityCritical]
        internal static String UnsafeGetFullPath(String path)
        {
            String fullPath = GetFullPathInternal(path);
#if !FEATURE_CORECLR
            FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, fullPath, false, false);
#endif
            return fullPath;
        }

        // This method is package access to let us quickly get a string name
        // while avoiding a security check.  This also serves a slightly
        // different purpose - when we open a file, we need to resolve the
        // path into a fully qualified, non-relative path name.  This
        // method does that, finding the current drive &; directory.  But
        // as long as we don't return this info to the user, we're good.  However,
        // the public GetFullPath does need to do a security check.
        internal static String GetFullPathInternal(String path) {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            String newPath = NormalizePath(path, true);

            return newPath;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static String NormalizePath(String path, bool fullCheck) {
            return NormalizePath(path, fullCheck, MaxPath);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static String NormalizePath(String path, bool fullCheck, bool expandShortPaths)
        {
            return NormalizePath(path, fullCheck, MaxPath, expandShortPaths);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static String NormalizePath(String path, bool fullCheck, int maxPathLength) {
            return NormalizePath(path, fullCheck, maxPathLength, true);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static String NormalizePath(String path, bool fullCheck, int maxPathLength, bool expandShortPaths) {

            Contract.Requires(path != null, "path can't be null");
            // If we're doing a full path check, trim whitespace and look for
            // illegal path characters.
            if (fullCheck) {
                // Trim whitespace off the end of the string.
                // Win32 normalization trims only U+0020. 
                path = path.TrimEnd(TrimEndChars);

                // Look for illegal path characters.
                CheckInvalidPathChars(path);
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
            PathHelper newBuffer;
            if (path.Length + 1 <= MaxPath) {
                char* m_arrayPtr = stackalloc char[MaxPath];
                newBuffer = new PathHelper(m_arrayPtr, MaxPath);
            } else {
                newBuffer = new PathHelper(path.Length + Path.MaxPath, maxPathLength);
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

#if !PLATFORM_UNIX
            bool mightBeShortFileName = false;

            // LEGACY: This code is here for backwards compatibility reasons. It 
            // ensures that \\foo.cs\bar.cs stays \\foo.cs\bar.cs instead of being
            // turned into \foo.cs\bar.cs.
            if (path.Length > 0 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar)) {
                newBuffer.Append('\\');
                index++;
                lastSigChar = 0;
            }
#endif

            // Normalize the string, stripping out redundant dots, spaces, and 
            // slashes.
            while (index < path.Length) {
                char currentChar = path[index];

                // We handle both directory separators and dots specially.  For 
                // directory separators, we consume consecutive appearances.  
                // For dots, we consume all dots beyond the second in 
                // succession.  All other characters are added as is.  In 
                // addition we consume all spaces after the last other char
                // in a directory name up until the directory separator.

                if (currentChar == DirectorySeparatorChar || currentChar == AltDirectorySeparatorChar) {
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
                    if (numSigChars == 0) {
                        // Dot and space handling
                        if (numDots > 0) {
                            // Look for ".[space]*" or "..[space]*"
                            int start = lastSigChar + 1;
                            if (path[start] != '.')
                                throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));

                            // Only allow "[dot]+[space]*", and normalize the 
                            // legal ones to "." or ".."
                            if (numDots >= 2) {
                                // Reject "C:..."
                                if (startedWithVolumeSeparator && numDots > 2)

                                    throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));

                                if (path[start + 1] == '.') {
                                    // Search for a space in the middle of the
                                    // dots and throw
                                    for(int i=start + 2; i < start + numDots; i++) {
                                        if (path[i] != '.')
                                            throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                                    }

                                    numDots = 2;
                                }
                                else {
                                    if (numDots > 1)
                                        throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                                    numDots = 1;
                                }
                            }
                                    
                            if (numDots == 2) {
                                newBuffer.Append('.');
                            }

                            newBuffer.Append('.');
                            fixupDirectorySeparator = false;

                            // Continue in this case, potentially writing out '\'.
                        }

                        if (numSpaces > 0 && firstSegment) {
                            // Handle strings like " \\server\share".
                            if (index + 1 < path.Length && 
                                (path[index + 1] == DirectorySeparatorChar || path[index + 1] == AltDirectorySeparatorChar))
                            {
                                newBuffer.Append(DirectorySeparatorChar);
                            }
                        }
                    }
                    numDots = 0;
                    numSpaces = 0;  // Suppress trailing spaces

                    if (!fixupDirectorySeparator) {
                        fixupDirectorySeparator = true;
                        newBuffer.Append(DirectorySeparatorChar);
                    }
                    numSigChars = 0;
                    lastSigChar = index;
                    startedWithVolumeSeparator = false;
                    firstSegment = false;

#if !PLATFORM_UNIX
                    // For short file names, we must try to expand each of them as
                    // soon as possible.  We need to allow people to specify a file
                    // name that doesn't exist using a path with short file names
                    // in it, such as this for a temp file we're trying to create:
                    // C:\DOCUME~1\USERNA~1.RED\LOCALS~1\Temp\bg3ylpzp
                    // We could try doing this afterwards piece by piece, but it's
                    // probably a lot simpler to do it here.
                    if (mightBeShortFileName) {
                        newBuffer.TryExpandShortFileName(); 
                        mightBeShortFileName = false;
                    }
#endif
                    int thisPos = newBuffer.Length - 1;
                    if (thisPos - lastDirectorySeparatorPos > MaxDirectoryLength)
                    {
                        throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                    }
                    lastDirectorySeparatorPos = thisPos;
                } // if (Found directory separator)
                else if (currentChar == '.') {
                    // Reduce only multiple .'s only after slash to 2 dots. For
                    // instance a...b is a valid file name.
                    numDots++;
                    // Don't flush out non-terminal spaces here, because they may in
                    // the end not be significant.  Turn "c:\ . .\foo" -> "c:\foo"
                    // which is the conclusion of removing trailing dots & spaces,
                    // as well as folding multiple '\' characters.
                }
                else if (currentChar == ' ') {
                    numSpaces++;
                }
                else {  // Normal character logic
#if !PLATFORM_UNIX
                    if (currentChar == '~' && expandShortPaths)
                        mightBeShortFileName = true;
#endif

                    fixupDirectorySeparator = false;

#if !PLATFORM_UNIX
                    // To reject strings like "C:...\foo" and "C  :\foo"
                    if (firstSegment && currentChar == VolumeSeparatorChar) {
                        // Only accept "C:", not "c :" or ":"
                        // Get a drive letter or ' ' if index is 0.
                        char driveLetter = (index > 0) ? path[index-1] : ' ';
                        bool validPath = ((numDots == 0) && (numSigChars >= 1) && (driveLetter != ' '));
                        if (!validPath)
                            throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));

                        startedWithVolumeSeparator = true;
                        // We need special logic to make " c:" work, we should not fix paths like "  foo::$DATA"
                        if (numSigChars > 1) { // Common case, simply do nothing
                            int spaceCount = 0; // How many spaces did we write out, numSpaces has already been reset.
                            while((spaceCount < newBuffer.Length) && newBuffer[spaceCount] == ' ') 
                                spaceCount++;
                            if (numSigChars - spaceCount == 1) {
                                //Safe to update stack ptr directly
                                newBuffer.Length = 0;
                                newBuffer.Append(driveLetter); // Overwrite spaces, we need a special case to not break "  foo" as a relative path.
                            }
                        }
                        numSigChars = 0;
                    }
                    else 
#endif // !PLATFORM_UNIX
                    {
                        numSigChars += 1 + numDots + numSpaces;
                    }

                    // Copy any spaces & dots since the last significant character
                    // to here.  Note we only counted the number of dots & spaces,
                    // and don't know what order they're in.  Hence the copy.
                    if (numDots > 0 || numSpaces > 0) {
                        int numCharsToCopy = (lastSigChar >= 0) ? index - lastSigChar - 1 : index;
                        if (numCharsToCopy > 0) {
                            for (int i=0; i<numCharsToCopy; i++) {
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

            if (newBuffer.Length - 1 - lastDirectorySeparatorPos > MaxDirectoryLength)
            {
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            }

            // Drop any trailing dots and spaces from file & directory names, EXCEPT
            // we MUST make sure that "C:\foo\.." is correctly handled.
            // Also handle "C:\foo\." -> "C:\foo", while "C:\." -> "C:\"
            if (numSigChars == 0) {
                if (numDots > 0) {
                    // Look for ".[space]*" or "..[space]*"
                    int start = lastSigChar + 1;
                    if (path[start] != '.')
                        throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));

                    // Only allow "[dot]+[space]*", and normalize the 
                    // legal ones to "." or ".."
                    if (numDots >= 2) {
                        // Reject "C:..."
                        if (startedWithVolumeSeparator && numDots > 2)
                            throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));

                        if (path[start + 1] == '.') {
                            // Search for a space in the middle of the
                            // dots and throw
                            for(int i=start + 2; i < start + numDots; i++) {
                                if (path[i] != '.')
                                    throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                            }
                            
                            numDots = 2;
                        }
                        else {
                            if (numDots > 1)
                                throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                            numDots = 1;
                        }
                    }

                    if (numDots == 2) {
                        newBuffer.Append('.');
                    }

                    newBuffer.Append('.');
                }
            } // if (numSigChars == 0)

            // If we ended up eating all the characters, bail out.
            if (newBuffer.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));

            // Disallow URL's here.  Some of our other Win32 API calls will reject
            // them later, so we might be better off rejecting them here.
            // Note we've probably turned them into "file:\D:\foo.tmp" by now.
            // But for compatibility, ensure that callers that aren't doing a 
            // full check aren't rejected here.
            if (fullCheck) {
                if ( newBuffer.OrdinalStartsWith("http:", false) ||
                     newBuffer.OrdinalStartsWith("file:", false))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_PathUriFormatNotSupported")); 
                }
            }
            
#if !PLATFORM_UNIX
            // If the last part of the path (file or directory name) had a tilde,
            // expand that too.
            if (mightBeShortFileName) {
                newBuffer.TryExpandShortFileName(); 
            }
#endif

            // Call the Win32 API to do the final canonicalization step.
            int result = 1;

            if (fullCheck) {
                // NOTE: Win32 GetFullPathName requires the input buffer to be big enough to fit the initial 
                // path which is a concat of CWD and the relative path, this can be of an arbitrary 
                // size and could be > MAX_PATH (which becomes an artificial limit at this point), 
                // even though the final normalized path after fixing up the relative path syntax 
                // might be well within the MAX_PATH restriction. For ex,
                // "c:\SomeReallyLongDirName(thinkGreaterThan_MAXPATH)\..\foo.txt" which actually requires a
                // buffer well with in the MAX_PATH as the normalized path is just "c:\foo.txt"
                // This buffer requirement seems wrong, it could be a bug or a perf optimization  
                // like returning required buffer length quickly or avoid stratch buffer etc. 
                // Ideally we would get the required buffer length first by calling GetFullPathName
                // once without the buffer and use that in the later call but this doesn't always work
                // due to Win32 GetFullPathName bug. For instance, in Win2k, when the path we are trying to
                // fully qualify is a single letter name (such as "a", "1", ",") GetFullPathName
                // fails to return the right buffer size (i.e, resulting in insufficient buffer). 
                // To workaround this bug we will start with MAX_PATH buffer and grow it once if the 
                // return value is > MAX_PATH. 

                result = newBuffer.GetFullPathName();

#if !PLATFORM_UNIX
                // If we called GetFullPathName with something like "foo" and our
                // command window was in short file name mode (ie, by running edlin or
                // DOS versions of grep, etc), we might have gotten back a short file
                // name.  So, check to see if we need to expand it.
                mightBeShortFileName = false;
                for(int i=0; i < newBuffer.Length && !mightBeShortFileName; i++) {
                    if (newBuffer[i] == '~' && expandShortPaths)
                        mightBeShortFileName = true;
                }

                if (mightBeShortFileName) {
                    bool r = newBuffer.TryExpandShortFileName();
                    // Consider how the path "Doesn'tExist" would expand.  If
                    // we add in the current directory, it too will need to be
                    // fully expanded, which doesn't happen if we use a file
                    // name that doesn't exist.
                    if (!r) {
                        int lastSlash = -1;

                        for (int i = newBuffer.Length - 1; i >= 0; i--) { 
                            if (newBuffer[i] == DirectorySeparatorChar) {
                                lastSlash = i;
                                break;
                            }
                        }

                        if (lastSlash >= 0) {
                            
                            // This bounds check is for safe memcpy but we should never get this far 
                            if (newBuffer.Length >= maxPathLength)
                                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));

                            int lenSavedName = newBuffer.Length - lastSlash - 1;
                            Contract.Assert(lastSlash < newBuffer.Length, "path unexpectedly ended in a '\'");

                            newBuffer.Fixup(lenSavedName, lastSlash);
                        }
                    }
                }
#endif // PLATFORM_UNIX
            }

            if (result != 0) {
                /* Throw an ArgumentException for paths like \\, \\server, \\server\
                   This check can only be properly done after normalizing, so
                   \\foo\.. will be properly rejected.  Also, reject \\?\GLOBALROOT\
                   (an internal kernel path) because it provides aliases for drives. */
                if (newBuffer.Length > 1 && newBuffer[0] == '\\' && newBuffer[1] == '\\') {
                    int startIndex = 2;
                    while (startIndex < result) {
                        if (newBuffer[startIndex] == '\\') {
                            startIndex++;
                            break;
                        }
                        else {
                            startIndex++;
                        }
                    }
                    if (startIndex == result)
                        throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegalUNC"));

                    // Check for \\?\Globalroot, an internal mechanism to the kernel
                    // that provides aliases for drives and other undocumented stuff.
                    // The kernel team won't even describe the full set of what
                    // is available here - we don't want managed apps mucking 
                    // with this for security reasons.
                    if ( newBuffer.OrdinalStartsWith("\\\\?\\globalroot", true))
                        throw new ArgumentException(Environment.GetResourceString("Arg_PathGlobalRoot"));
                }
            }

            // Check our result and form the managed string as necessary.
            if (newBuffer.Length >= maxPathLength)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));

            if (result == 0) {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == 0)
                    errorCode = Win32Native.ERROR_BAD_PATHNAME;
                __Error.WinIOError(errorCode, path);
                return null;  // Unreachable - silence a compiler error.
            }

            return newBuffer.ToStringOrExisting(path);
        }
        internal const int MaxLongPath = 32000;

        private const string LongPathPrefix = @"\\?\";
        private const string UNCPathPrefix = @"\\";
        private const string UNCLongPathPrefixToInsert = @"?\UNC\";
        private const string UNCLongPathPrefix = @"\\?\UNC\";

        internal unsafe static bool HasLongPathPrefix(String path)
        {
            return path.StartsWith(LongPathPrefix, StringComparison.Ordinal);
        }

        internal unsafe static String AddLongPathPrefix(String path)
        {
            if (path.StartsWith(LongPathPrefix, StringComparison.Ordinal))
                return path;

            if (path.StartsWith(UNCPathPrefix, StringComparison.Ordinal))
                return path.Insert(2, UNCLongPathPrefixToInsert); // Given \\server\share in longpath becomes \\?\UNC\server\share  => UNCLongPathPrefix + path.SubString(2); => The actual command simply reduces the operation cost.

            return LongPathPrefix + path;
        }

        internal unsafe static String RemoveLongPathPrefix(String path)
        {
            if (!path.StartsWith(LongPathPrefix, StringComparison.Ordinal))
                return path;

            if (path.StartsWith(UNCLongPathPrefix, StringComparison.OrdinalIgnoreCase))
                return path.Remove(2, 6); // Given \\?\UNC\server\share we return \\server\share => @'\\' + path.SubString(UNCLongPathPrefix.Length) => The actual command simply reduces the operation cost.

            return path.Substring(4);
        }

        internal unsafe static StringBuilder RemoveLongPathPrefix(StringBuilder pathSB)
        {
            string path = pathSB.ToString();
            if (!path.StartsWith(LongPathPrefix, StringComparison.Ordinal))
                return pathSB;

            if (path.StartsWith(UNCLongPathPrefix, StringComparison.OrdinalIgnoreCase))
                return pathSB.Remove(2, 6);  // Given \\?\UNC\server\share we return \\server\share => @'\\' + path.SubString(UNCLongPathPrefix.Length) => The actual command simply reduces the operation cost.

            return pathSB.Remove(0, 4);
        }

        // Returns the name and extension parts of the given path. The resulting
        // string contains the characters of path that follow the last
        // backslash ("\"), slash ("/"), or colon (":") character in 
        // path. The resulting string is the entire path if path 
        // contains no backslash after removing trailing slashes, slash, or colon characters. The resulting 
        // string is null if path is null.
        //
        [Pure]
        public static String GetFileName(String path) {
          if (path != null) {
                CheckInvalidPathChars(path);
    
                int length = path.Length;
                for (int i = length; --i >= 0;) {
                    char ch = path[i];
                    if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                        return path.Substring(i + 1, length - i - 1);

                }
            }
            return path;
        }

        [Pure]
        public static String GetFileNameWithoutExtension(String path) {
            path = GetFileName(path);
            if (path != null)
            {
                int i;
                if ((i=path.LastIndexOf('.')) == -1)
                    return path; // No path extension found
                else
                    return path.Substring(0,i);
            }
            return null;
         }



        // Returns the root portion of the given path. The resulting string
        // consists of those rightmost characters of the path that constitute the
        // root of the path. Possible patterns for the resulting string are: An
        // empty string (a relative path on the current drive), "\" (an absolute
        // path on the current drive), "X:" (a relative path on a given drive,
        // where X is the drive letter), "X:\" (an absolute path on a given drive),
        // and "\\server\share" (a UNC path for a given server and share name).
        // The resulting string is null if path is null.
        //
        [Pure]
        public static String GetPathRoot(String path) {
            if (path == null) return null;
            path = NormalizePath(path, false);
            return path.Substring(0, GetRootLength(path));
        }

        [System.Security.SecuritySafeCritical]
        public static String GetTempPath()
        {
#if !FEATURE_CORECLR
            new EnvironmentPermission(PermissionState.Unrestricted).Demand();
#endif
            StringBuilder sb = new StringBuilder(MaxPath);
            uint r = Win32Native.GetTempPath(MaxPath, sb);
            String path = sb.ToString();
            if (r==0) __Error.WinIOError();
            path = GetFullPathInternal(path);
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, String.Empty, path);
            state.EnsureState();
#endif
            return path;
        }

        internal static bool IsRelative(string path)
        {
            Contract.Assert(path != null, "path can't be null");
#if !PLATFORM_UNIX
            if ((path.Length >= 3 && path[1] == VolumeSeparatorChar && path[2] == DirectorySeparatorChar && 
                   ((path[0] >= 'a' && path[0] <= 'z') || (path[0] >= 'A' && path[0] <= 'Z'))) ||
                  (path.Length >= 2 && path[0] == '\\' && path[1] == '\\'))
#else
            if(path.Length >= 1 && path[0] == VolumeSeparatorChar)
#endif // !PLATFORM_UNIX
                return false;
            else
                return true;
        
        }
                
        // Returns a cryptographically strong random 8.3 string that can be 
        // used as either a folder name or a file name.
#if FEATURE_PAL
        [System.Security.SecuritySafeCritical]
#endif
        public static String GetRandomFileName()
        {
            // 5 bytes == 40 bits == 40/5 == 8 chars in our encoding
            // This gives us exactly 8 chars. We want to avoid the 8.3 short name issue
            byte[] key = new byte[10];

#if FEATURE_PAL
            Win32Native.Random(true, key, key.Length);
#else
            // RNGCryptoServiceProvider is disposable in post-Orcas desktop mscorlibs, but not in CoreCLR's
            // mscorlib, so we need to do a manual using block for it.
            RNGCryptoServiceProvider rng = null;
            try
            {
                rng = new RNGCryptoServiceProvider();

                rng.GetBytes(key);
            }
            finally
            {
                if (rng != null)
                {
                    rng.Dispose();
                }
            }
#endif

            // rndCharArray is expected to be 16 chars
            char[] rndCharArray = Path.ToBase32StringSuitableForDirName(key).ToCharArray();
            rndCharArray[8] = '.';
            return new String(rndCharArray, 0, 12);
        }

        // Returns a unique temporary file name, and creates a 0-byte file by that
        // name on disk.
        [System.Security.SecuritySafeCritical]
        public static String GetTempFileName()
        {
            return InternalGetTempFileName(true);
        }

        [System.Security.SecurityCritical]
        internal static String UnsafeGetTempFileName()
        {
            return InternalGetTempFileName(false);
        }

        [System.Security.SecurityCritical]  
        private static String InternalGetTempFileName(bool checkHost) 
        {
            String path = GetTempPath();

            // Since this can write to the temp directory and theoretically 
            // cause a denial of service attack, demand FileIOPermission to 
            // that directory.

#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, String.Empty, path);
                state.EnsureState();
            }
#else
            new FileIOPermission(FileIOPermissionAccess.Write, path).Demand();
#endif
            StringBuilder sb = new StringBuilder(MaxPath);
            uint r = Win32Native.GetTempFileName(path, "tmp", 0, sb);
            if (r==0) __Error.WinIOError();
            return sb.ToString();
        }
    
        // Tests if a path includes a file extension. The result is
        // true if the characters that follow the last directory
        // separator ('\\' or '/') or volume separator (':') in the path include 
        // a period (".") other than a terminal period. The result is false otherwise.
        //
        [Pure]
        public static bool HasExtension(String path) {
            if (path != null) {
                CheckInvalidPathChars(path);
                
                for (int i = path.Length; --i >= 0;) {
                    char ch = path[i];
                    if (ch == '.') {
                        if ( i != path.Length - 1)
                            return true;
                        else
                            return false;
                    }
                    if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar) break;
                }
            }
            return false;
        }
    
    
        // Tests if the given path contains a root. A path is considered rooted
        // if it starts with a backslash ("\") or a drive letter and a colon (":").
        //
        [Pure]
        public static bool IsPathRooted(String path) {
            if (path != null) {
                CheckInvalidPathChars(path);
    
                int length = path.Length;
#if !PLATFORM_UNIX
                if ((length >= 1 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar)) || (length >= 2 && path[1] == VolumeSeparatorChar))
                    return true;
#else
                if (length >= 1 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar))
                    return true;
#endif
            }
            return false;
        }

        public static String Combine(String path1, String path2) {
            if (path1==null || path2==null)
                throw new ArgumentNullException((path1==null) ? "path1" : "path2");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);

            return CombineNoChecks(path1, path2);
        }

        public static String Combine(String path1, String path2, String path3) {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : (path2 == null) ? "path2" : "path3");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            CheckInvalidPathChars(path3);

            return CombineNoChecks(CombineNoChecks(path1, path2), path3);
        }

        public static String Combine(String path1, String path2, String path3, String path4) {
            if (path1 == null || path2 == null || path3 == null || path4 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : (path2 == null) ? "path2" : (path3 == null) ? "path3" : "path4");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            CheckInvalidPathChars(path3);
            CheckInvalidPathChars(path4);

            return CombineNoChecks(CombineNoChecks(CombineNoChecks(path1, path2), path3), path4);
        }

        public static String Combine(params String[] paths) {
            if (paths == null) {
                throw new ArgumentNullException("paths");
            }
            Contract.EndContractBlock();

            int finalSize = 0;
            int firstComponent = 0;

            // We have two passes, the first calcuates how large a buffer to allocate and does some precondition
            // checks on the paths passed in.  The second actually does the combination.

            for (int i = 0; i < paths.Length; i++) {
                if (paths[i] == null) {
                    throw new ArgumentNullException("paths");
                }

                if (paths[i].Length  == 0) {
                    continue;
                }

                CheckInvalidPathChars(paths[i]);

                if (Path.IsPathRooted(paths[i])) {
                    firstComponent = i;
                    finalSize = paths[i].Length;
                } else {
                    finalSize += paths[i].Length;
                }

                char ch = paths[i][paths[i].Length - 1];
                if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar) 
                    finalSize++;
                }

            StringBuilder finalPath = StringBuilderCache.Acquire(finalSize);

            for (int i = firstComponent; i < paths.Length; i++) {
                if (paths[i].Length == 0) {
                    continue;
                }

                if (finalPath.Length == 0) {
                    finalPath.Append(paths[i]);
                } else {
                    char ch = finalPath[finalPath.Length - 1];
                    if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar) {
                        finalPath.Append(DirectorySeparatorChar);
                    }

                    finalPath.Append(paths[i]);
                }
            }

            return StringBuilderCache.GetStringAndRelease(finalPath);
        }

        private static String CombineNoChecks(String path1, String path2) {
            if (path2.Length == 0)
                return path1;

            if (path1.Length == 0)
                return path2;
                
            if (IsPathRooted(path2))
                return path2;

            char ch = path1[path1.Length - 1];
            if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar) 
                return path1 + DirectorySeparatorCharAsString + path2;
            return path1 + path2;
        }

        private static readonly Char[] s_Base32Char   = {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 
                'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 
                'y', 'z', '0', '1', '2', '3', '4', '5'};

        internal static String ToBase32StringSuitableForDirName(byte[] buff)
        {
            // This routine is optimised to be used with buffs of length 20
            Contract.Assert(((buff.Length % 5) == 0), "Unexpected hash length");

            StringBuilder sb = StringBuilderCache.Acquire();
            byte b0, b1, b2, b3, b4;
            int  l, i;
    
            l = buff.Length;
            i = 0;

            // Create l chars using the last 5 bits of each byte.  
            // Consume 3 MSB bits 5 bytes at a time.

            do
            {
                b0 = (i < l) ? buff[i++] : (byte)0;
                b1 = (i < l) ? buff[i++] : (byte)0;
                b2 = (i < l) ? buff[i++] : (byte)0;
                b3 = (i < l) ? buff[i++] : (byte)0;
                b4 = (i < l) ? buff[i++] : (byte)0;

                // Consume the 5 Least significant bits of each byte
                sb.Append(s_Base32Char[b0 & 0x1F]);
                sb.Append(s_Base32Char[b1 & 0x1F]);
                sb.Append(s_Base32Char[b2 & 0x1F]);
                sb.Append(s_Base32Char[b3 & 0x1F]);
                sb.Append(s_Base32Char[b4 & 0x1F]);
    
                // Consume 3 MSB of b0, b1, MSB bits 6, 7 of b3, b4
                sb.Append(s_Base32Char[(
                        ((b0 & 0xE0) >> 5) | 
                        ((b3 & 0x60) >> 2))]);
    
                sb.Append(s_Base32Char[(
                        ((b1 & 0xE0) >> 5) | 
                        ((b4 & 0x60) >> 2))]);
    
                // Consume 3 MSB bits of b2, 1 MSB bit of b3, b4
                
                b2 >>= 5;
    
                Contract.Assert(((b2 & 0xF8) == 0), "Unexpected set bits");
    
                if ((b3 & 0x80) != 0)
                    b2 |= 0x08;
                if ((b4 & 0x80) != 0)
                    b2 |= 0x10;
    
                sb.Append(s_Base32Char[b2]);

            } while (i < l);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // ".." can only be used if it is specified as a part of a valid File/Directory name. We disallow
        //  the user being able to use it to move up directories. Here are some examples eg 
        //    Valid: a..b  abc..d
        //    Invalid: ..ab   ab..  ..   abc..d\abc..
        //
        internal static void CheckSearchPattern(String searchPattern)
        {
            int index;
            while ((index = searchPattern.IndexOf("..", StringComparison.Ordinal)) != -1) {
                    
                 if (index + 2 == searchPattern.Length) // Terminal ".." . Files names cannot end in ".."
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidSearchPattern"));
                
                 if ((searchPattern[index+2] ==  DirectorySeparatorChar)
                    || (searchPattern[index+2] == AltDirectorySeparatorChar))
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidSearchPattern"));
                
                searchPattern = searchPattern.Substring(index + 2);
            }

        }

        internal static bool HasIllegalCharacters(String path, bool checkAdditional)
        {
            Contract.Requires(path != null);

            if (checkAdditional)
            {
                return path.IndexOfAny(InvalidPathCharsWithAdditionalChecks) >= 0;
            }

            return path.IndexOfAny(RealInvalidPathChars) >= 0;
        }

        internal static void CheckInvalidPathChars(String path, bool checkAdditional = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (Path.HasIllegalCharacters(path, checkAdditional))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
        }

        
        internal static String InternalCombine(String path1, String path2) {
            if (path1==null || path2==null)
                throw new ArgumentNullException((path1==null) ? "path1" : "path2");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            
            if (path2.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"), "path2");
            if (IsPathRooted(path2))
                throw new ArgumentException(Environment.GetResourceString("Arg_Path2IsRooted"), "path2");
            int i = path1.Length;
            if (i == 0) return path2;
            char ch = path1[i - 1];
            if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar) 
                return path1 + DirectorySeparatorCharAsString + path2;
            return path1 + path2;
        }
            
    }
}
