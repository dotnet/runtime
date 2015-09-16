// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.IO
{
    // ABOUT:
    // Helps with path normalization; support allocating on the stack or heap
    // 
    // PathHelper can't stackalloc the array for obvious reasons; you must pass
    // in an array of chars allocated on the stack.
    // 
    // USAGE:
    // Suppose you need to represent a char array of length len. Then this is the
    // suggested way to instantiate PathHelper:
    // ***************************************************************************
    // PathHelper pathHelper;
    // if (charArrayLength less than stack alloc threshold == Path.MaxPath)
    //     char* arrayPtr = stackalloc char[Path.MaxPath];
    //     pathHelper = new PathHelper(arrayPtr);
    // else
    //     pathHelper = new PathHelper(capacity, maxPath);
    // ***************************************************************************
    //
    // note in the StringBuilder ctor:
    // - maxPath may be greater than Path.MaxPath (for isolated storage)
    // - capacity may be greater than maxPath. This is even used for non-isolated
    //   storage scenarios where we want to temporarily allow strings greater 
    //   than Path.MaxPath if they can be normalized down to Path.MaxPath. This
    //   can happen if the path contains escape characters "..".
    // 
    unsafe internal class PathHelper
    {   // should not be serialized
        // maximum size, max be greater than max path if contains escape sequence
        private int m_capacity;
        // current length (next character position)
        private int m_length;
        // max path, may be less than capacity
        private int m_maxPath;

        // ptr to stack alloc'd array of chars
        [SecurityCritical]
        private char* m_arrayPtr;

        // StringBuilder
        private StringBuilder m_sb;

        // whether to operate on stack alloc'd or heap alloc'd array 
        private bool useStackAlloc;

        // Whether to skip calls to Win32Native.GetLongPathName becasue we tried before and failed:
        private bool doNotTryExpandShortFileName;

        /// <summary>
        /// Instantiates a PathHelper with a stack alloc'd array of chars.
        /// NOTE: This cannot grow past the initial length and will always fail if over MAX_PATH.
        /// </summary>
        [System.Security.SecurityCritical]
        internal PathHelper(char* charArrayPtr, int length)
        {
            Contract.Requires(charArrayPtr != null);
            // force callers to be aware of this
            Contract.Requires(length == Path.MaxPath);

            this.m_arrayPtr = charArrayPtr;
            this.m_capacity = length;
            this.m_maxPath = Path.MaxPath;
            useStackAlloc = true;
            doNotTryExpandShortFileName = false;
        }

        // Instantiates a PathHelper with a heap alloc'd array of ints. Will create a StringBuilder
        internal PathHelper(int capacity, int maxPath)
        {
            this.m_sb = new StringBuilder(capacity);
            this.m_capacity = capacity;
            this.m_maxPath = maxPath;
            doNotTryExpandShortFileName = false;
        }

        internal int Length
        {
            get
            {
                if (useStackAlloc)
                {
                    return m_length;
                }
                else
                {
                    return m_sb.Length;
                }
            }
            set
            {
                if (useStackAlloc)
                {
                    m_length = value;
                }
                else
                {
                    m_sb.Length = value;
                }
            }
        }

        internal int Capacity
        {
            get
            {
                return m_capacity;
            }
        }

        internal char this[int index]
        {
            [System.Security.SecurityCritical]
            get
            {
                Contract.Requires(index >= 0 && index < Length);
                if (useStackAlloc)
                {
                    return m_arrayPtr[index];
                }
                else
                {
                    return m_sb[index];
                }
            }
            [System.Security.SecurityCritical]
            set
            {
                Contract.Requires(index >= 0 && index < Length);
                if (useStackAlloc)
                {
                    m_arrayPtr[index] = value;
                }
                else
                {
                    m_sb[index] = value;
                }
            }
        }

        [System.Security.SecurityCritical]
        internal unsafe void Append(char value)
        {
            if (Length + 1 >= m_capacity)
                throw new PathTooLongException(SR.IO_PathTooLong);

            if (useStackAlloc)
            {
                m_arrayPtr[Length] = value;
                m_length++;
            }
            else
            {
                m_sb.Append(value);
            }
        }

        [System.Security.SecurityCritical]
        internal unsafe int GetFullPathName()
        {
            if (useStackAlloc)
            {
                char* finalBuffer = stackalloc char[Path.MaxPath + 1];
                int result = Interop.mincore.GetFullPathNameUnsafe(m_arrayPtr, Path.MaxPath + 1, finalBuffer, IntPtr.Zero);

                // If success, the return buffer length does not account for the terminating null character.
                // If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
                // If failure, the return buffer length is zero 
                if (result > Path.MaxPath)
                {
                    char* tempBuffer = stackalloc char[result];
                    finalBuffer = tempBuffer;
                    result = Interop.mincore.GetFullPathNameUnsafe(m_arrayPtr, result, finalBuffer, IntPtr.Zero);
                }

                // Full path is genuinely long
                if (result >= Path.MaxPath)
                    throw new PathTooLongException(SR.IO_PathTooLong);

                Debug.Assert(result < Path.MaxPath, "did we accidently remove a PathTooLongException check?");
                if (result == 0 && m_arrayPtr[0] != '\0')
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                else if (result < Path.MaxPath)
                {
                    // Null terminate explicitly (may be only needed for some cases such as empty strings)
                    // GetFullPathName return length doesn't account for null terminating char...
                    finalBuffer[result] = '\0'; // Safe to write directly as result is < Path.MaxPath
                }

                // We have expanded the paths and GetLongPathName may or may not behave differently from before.
                // We need to call it again to see:
                doNotTryExpandShortFileName = false;

                Wstrcpy(m_arrayPtr, finalBuffer, result);
                // Doesn't account for null terminating char. Think of this as the last
                // valid index into the buffer but not the length of the buffer
                Length = result;
                return result;
            }
            else
            {
                StringBuilder finalBuffer = new StringBuilder(m_capacity + 1);
                int result = Interop.mincore.GetFullPathName(m_sb.ToString(), m_capacity + 1, finalBuffer);

                // If success, the return buffer length does not account for the terminating null character.
                // If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
                // If failure, the return buffer length is zero 
                if (result > m_maxPath)
                {
                    finalBuffer.Length = result;
                    result = Interop.mincore.GetFullPathName(m_sb.ToString(), result, finalBuffer);
                }

                // Fullpath is genuinely long
                if (result >= m_maxPath)
                    throw new PathTooLongException(SR.IO_PathTooLong);

                Debug.Assert(result < m_maxPath, "did we accidentally remove a PathTooLongException check?");
                if (result == 0 && m_sb[0] != '\0')
                {
                    if (Length >= m_maxPath)
                    {
                        throw new PathTooLongException(SR.IO_PathTooLong);
                    }
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                // We have expanded the paths and GetLongPathName may or may not behave differently from before.
                // We need to call it again to see:
                doNotTryExpandShortFileName = false;

                m_sb = finalBuffer;
                return result;
            }
        }

        [System.Security.SecurityCritical]
        internal unsafe bool TryExpandShortFileName()
        {
            if (doNotTryExpandShortFileName)
                return false;

            if (useStackAlloc)
            {
                NullTerminate();
                char* buffer = UnsafeGetArrayPtr();
                char* shortFileNameBuffer = stackalloc char[Path.MaxPath + 1];

                int r = Interop.mincore.GetLongPathNameUnsafe(buffer, shortFileNameBuffer, Path.MaxPath);

                // If success, the return buffer length does not account for the terminating null character.
                // If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
                // If failure, the return buffer length is zero 
                if (r >= Path.MaxPath)
                    throw new PathTooLongException(SR.IO_PathTooLong);

                if (r == 0)
                {
                    // Note: GetLongPathName will return ERROR_INVALID_FUNCTION on a 
                    // path like \\.\PHYSICALDEVICE0 - some device driver doesn't 
                    // support GetLongPathName on that string.  This behavior is 
                    // by design, according to the Core File Services team.
                    // We also get ERROR_NOT_ENOUGH_QUOTA in SQL_CLR_STRESS runs
                    // intermittently on paths like D:\DOCUME~1\user\LOCALS~1\Temp\

                    // We do not need to call GetLongPathName if we know it will fail becasue the path does not exist:
                    int lastErr = Marshal.GetLastWin32Error();
                    if (lastErr == Interop.mincore.Errors.ERROR_FILE_NOT_FOUND || lastErr == Interop.mincore.Errors.ERROR_PATH_NOT_FOUND)
                        doNotTryExpandShortFileName = true;

                    return false;
                }

                // Safe to copy as we have already done Path.MaxPath bound checking 
                Wstrcpy(buffer, shortFileNameBuffer, r);
                Length = r;
                // We should explicitly null terminate as in some cases the long version of the path 
                // might actually be shorter than what we started with because of Win32's normalization
                // Safe to write directly as bufferLength is guaranteed to be < Path.MaxPath
                NullTerminate();
                return true;
            }
            else
            {
                StringBuilder sb = GetStringBuilder();

                String origName = sb.ToString();
                String tempName = origName;
                bool addedPrefix = false;
                if (tempName.Length > Path.MaxPath)
                {
                    tempName = PathInternal.EnsureExtendedPrefix(tempName);
                    addedPrefix = true;
                }
                sb.Capacity = m_capacity;
                sb.Length = 0;
                int r = Interop.mincore.GetLongPathName(tempName, sb, m_capacity);

                if (r == 0)
                {
                    // Note: GetLongPathName will return ERROR_INVALID_FUNCTION on a 
                    // path like \\.\PHYSICALDEVICE0 - some device driver doesn't 
                    // support GetLongPathName on that string.  This behavior is 
                    // by design, according to the Core File Services team.
                    // We also get ERROR_NOT_ENOUGH_QUOTA in SQL_CLR_STRESS runs
                    // intermittently on paths like D:\DOCUME~1\user\LOCALS~1\Temp\

                    // We do not need to call GetLongPathName if we know it will fail becasue the path does not exist:
                    int lastErr = Marshal.GetLastWin32Error();
                    if (Interop.mincore.Errors.ERROR_FILE_NOT_FOUND == lastErr || Interop.mincore.Errors.ERROR_PATH_NOT_FOUND == lastErr)
                        doNotTryExpandShortFileName = true;

                    sb.Length = 0;
                    sb.Append(origName);
                    return false;
                }

                if (addedPrefix)
                {
                    r -= 4;
                    sb = PathInternal.RemoveExtendedPrefix(sb);
                }

                // If success, the return buffer length does not account for the terminating null character.
                // If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
                // If failure, the return buffer length is zero 
                if (r >= m_maxPath)
                    throw new PathTooLongException(SR.IO_PathTooLong);

                Length = sb.Length;
                return true;
            }
        }

        [System.Security.SecurityCritical]
        internal unsafe void Fixup(int lenSavedName, int lastSlash)
        {
            if (useStackAlloc)
            {
                char* savedName = stackalloc char[lenSavedName];
                Wstrcpy(savedName, m_arrayPtr + lastSlash + 1, lenSavedName);
                Length = lastSlash;
                NullTerminate();
                doNotTryExpandShortFileName = false;
                bool r = TryExpandShortFileName();
                // Clean up changes made to the newBuffer.
                Append(Path.DirectorySeparatorChar);
                if (Length + lenSavedName >= Path.MaxPath)
                    throw new PathTooLongException(SR.IO_PathTooLong);
                Wstrcpy(m_arrayPtr + Length, savedName, lenSavedName);
                Length = Length + lenSavedName;
            }
            else
            {
                String savedName = m_sb.ToString(lastSlash + 1, lenSavedName);
                Length = lastSlash;
                doNotTryExpandShortFileName = false;
                bool r = TryExpandShortFileName();
                // Clean up changes made to the newBuffer.
                Append(Path.DirectorySeparatorChar);
                if (Length + lenSavedName >= m_maxPath)
                    throw new PathTooLongException(SR.IO_PathTooLong);
                m_sb.Append(savedName);
            }
        }

        [System.Security.SecurityCritical]
        internal unsafe bool OrdinalStartsWith(String compareTo, bool ignoreCase)
        {
            if (Length < compareTo.Length)
                return false;

            if (useStackAlloc)
            {
                NullTerminate();
                if (ignoreCase)
                {
                    String s = new String(m_arrayPtr, 0, compareTo.Length);
                    return compareTo.Equals(s, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    for (int i = 0; i < compareTo.Length; i++)
                    {
                        if (m_arrayPtr[i] != compareTo[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            else
            {
                if (ignoreCase)
                {
                    return m_sb.ToString().StartsWith(compareTo, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return m_sb.ToString().StartsWith(compareTo, StringComparison.Ordinal);
                }
            }
        }

        [System.Security.SecuritySafeCritical]
        public override String ToString()
        {
            if (useStackAlloc)
            {
                return new String(m_arrayPtr, 0, Length);
            }
            else
            {
                return m_sb.ToString();
            }
        }

        [System.Security.SecurityCritical]
        private unsafe char* UnsafeGetArrayPtr()
        {
            Contract.Requires(useStackAlloc, "This should never be called for PathHelpers wrapping a StringBuilder");
            return m_arrayPtr;
        }

        private StringBuilder GetStringBuilder()
        {
            Contract.Requires(!useStackAlloc, "This should never be called for PathHelpers that wrap a stackalloc'd buffer");
            return m_sb;
        }

        [System.Security.SecurityCritical]
        private unsafe void NullTerminate()
        {
            Contract.Requires(useStackAlloc, "This should never be called for PathHelpers wrapping a StringBuilder");
            m_arrayPtr[m_length] = '\0';
        }

        private static unsafe void Wstrcpy(char* dmem, char* smem, int charCount)
        {
            ulong size = ((uint)charCount) << 1;
            Buffer.MemoryCopy(smem, dmem, size, size);
            return;
        }
    }
}
