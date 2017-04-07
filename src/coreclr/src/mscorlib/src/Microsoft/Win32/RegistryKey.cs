// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  Note on ACL support:
  The key thing to note about ACL's is you set them on a kernel object like a
  registry key, then the ACL only gets checked when you construct handles to 
  them.  So if you set an ACL to deny read access to yourself, you'll still be
  able to read with that handle, but not with new handles.

  Another peculiarity is a Terminal Server app compatibility workaround.  The OS
  will second guess your attempt to open a handle sometimes.  If a certain
  combination of Terminal Server app compat registry keys are set, then the
  OS will try to reopen your handle with lesser permissions if you couldn't
  open it in the specified mode.  So on some machines, we will see handles that
  may not be able to read or write to a registry key.  It's very strange.  But
  the real test of these handles is attempting to read or set a value in an
  affected registry key.
  
  For reference, at least two registry keys must be set to particular values 
  for this behavior:
  HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\RegistryExtensionFlags, the least significant bit must be 1.
  HKLM\SYSTEM\CurrentControlSet\Control\TerminalServer\TSAppCompat must be 1
  There might possibly be an interaction with yet a third registry key as well.

*/

using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Win32
{
    /**
     * Registry encapsulation. To get an instance of a RegistryKey use the
     * Registry class's static members then call OpenSubKey.
     */
    internal sealed class RegistryKey : MarshalByRefObject, IDisposable
    {
        // Use the public Registry.CurrentUser
        internal static readonly RegistryKey CurrentUser =
            GetBaseKey(new IntPtr(unchecked((int)0x80000001)), "HKEY_CURRENT_USER");

        // Use the public Registry.LocalMachine
        internal static readonly RegistryKey LocalMachine =
            GetBaseKey(new IntPtr(unchecked((int)0x80000002)), "HKEY_LOCAL_MACHINE");

        // We could use const here, if C# supported ELEMENT_TYPE_I fully.
        private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
        private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));

        // SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened"
        // or "closed".
        //
        private const int STATE_SYSTEMKEY = 0x0002;

        // Access
        //
        private const int STATE_WRITEACCESS = 0x0004;

        // MSDN defines the following limits for registry key names & values:
        // Key Name: 255 characters
        // Value name:  16,383 Unicode characters
        // Value: either 1 MB or current available memory, depending on registry format.
        private const int MaxKeyLength = 255;
        private const int MaxValueLength = 16383;

        private volatile SafeRegistryHandle hkey = null;
        private volatile int state = 0;
        private volatile string keyName;

        /**
         * Creates a RegistryKey.
         *
         * This key is bound to hkey, if writable is <b>false</b> then no write operations
         * will be allowed. If systemkey is set then the hkey won't be released
         * when the object is GC'ed.
         * The remoteKey flag when set to true indicates that we are dealing with registry entries
         * on a remote machine and requires the program making these calls to have full trust.
         */
        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey)
        {
            this.hkey = hkey;
            keyName = "";
            if (systemkey)
            {
                state |= STATE_SYSTEMKEY;
            }
            if (writable)
            {
                state |= STATE_WRITEACCESS;
            }
        }

        private void Dispose(bool disposing)
        {
            if (hkey != null)
            {
                if (!IsSystemKey())
                {
                    try
                    {
                        hkey.Dispose();
                    }
                    catch (IOException)
                    {
                        // we don't really care if the handle is invalid at this point
                    }
                    finally
                    {
                        hkey = null;
                    }
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            EnsureWriteable();
            int errorCode = Win32Native.RegDeleteValue(hkey, name);

            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            //
            if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND || errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE)
            {
                if (throwOnMissingValue)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyValueAbsent);
                }

                // Otherwise, just return giving no indication to the user.
                // (For compatibility)
            }

            // We really should throw an exception here if errorCode was bad,
            // but we can't for compatibility reasons.
            Debug.Assert(errorCode == 0, "RegDeleteValue failed.  Here's your error code: " + errorCode);
        }

        private static RegistryKey GetBaseKey(IntPtr hKey, string keyName)
        {
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, ownsHandle: false);

            RegistryKey key = new RegistryKey(srh, true, true);
            key.keyName = keyName;
            return key;
        }

        /// <summary>
        /// Retrieves a subkey or null if the operation failed.
        /// </summary>
        /// <param name="writable">True to open writable, otherwise opens the key read-only.</param>
        public RegistryKey OpenSubKey(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();

            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey,
                name,
                0,
                writable ? Win32Native.KEY_READ | Win32Native.KEY_WRITE : Win32Native.KEY_READ,
                out result);

            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false);
                key.keyName = keyName + "\\" + name;
                return key;
            }

            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            }

            return null;
        }

        /// <summary>
        /// Retrieves an array of strings containing all the subkey names.
        /// </summary>
        public string[] GetSubKeyNames()
        {
            EnsureNotDisposed();

            var names = new List<string>();
            char[] name = ArrayPool<char>.Shared.Rent(MaxKeyLength + 1);

            try
            {
                int result;
                int nameLength = name.Length;

                while ((result = Win32Native.RegEnumKeyEx(
                    hkey,
                    names.Count,
                    name,
                    ref nameLength,
                    null,
                    null,
                    null,
                    null)) != Interop.Errors.ERROR_NO_MORE_ITEMS)
                {
                    switch (result)
                    {
                        case Interop.Errors.ERROR_SUCCESS:
                            names.Add(new string(name, 0, nameLength));
                            nameLength = name.Length;
                            break;
                        default:
                            // Throw the error
                            Win32Error(result, null);
                            break;
                    }
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(name);
            }

            return names.ToArray();
        }

        /// <summary>
        /// Retrieves an array of strings containing all the value names.
        /// </summary>
        public unsafe string[] GetValueNames()
        {
            EnsureNotDisposed();
            var names = new List<string>();

            // Names in the registry aren't usually very long, although they can go to as large
            // as 16383 characters (MaxValueLength).
            //
            // Every call to RegEnumValue will allocate another buffer to get the data from
            // NtEnumerateValueKey before copying it back out to our passed in buffer. This can
            // add up quickly- we'll try to keep the memory pressure low and grow the buffer
            // only if needed.

            char[] name = ArrayPool<char>.Shared.Rent(100);

            try
            {
                int result;
                int nameLength = name.Length;

                while ((result = Win32Native.RegEnumValue(
                    hkey,
                    names.Count,
                    name,
                    ref nameLength,
                    IntPtr.Zero,
                    null,
                    null,
                    null)) != Interop.Errors.ERROR_NO_MORE_ITEMS)
                {
                    switch (result)
                    {
                        // The size is only ever reported back correctly in the case
                        // of ERROR_SUCCESS. It will almost always be changed, however.
                        case Interop.Errors.ERROR_SUCCESS:
                            names.Add(new string(name, 0, nameLength));
                            break;
                        case Interop.Errors.ERROR_MORE_DATA:
                            if (IsPerfDataKey())
                            {
                                // Enumerating the values for Perf keys always returns
                                // ERROR_MORE_DATA, but has a valid name. Buffer does need
                                // to be big enough however. 8 characters is the largest
                                // known name. The size isn't returned, but the string is
                                // null terminated.
                                fixed (char* c = &name[0])
                                {
                                    names.Add(new string(c));
                                }
                            }
                            else
                            {
                                char[] oldName = name;
                                int oldLength = oldName.Length;
                                name = null;
                                ArrayPool<char>.Shared.Return(oldName);
                                name = ArrayPool<char>.Shared.Rent(checked(oldLength * 2));
                            }
                            break;
                        default:
                            // Throw the error
                            Win32Error(result, null);
                            break;
                    }

                    // Always set the name length back to the buffer size
                    nameLength = name.Length;
                }
            }
            finally
            {
                if (name != null)
                    ArrayPool<char>.Shared.Return(name);
            }

            return names.ToArray();
        }

        /**
         * Retrieves the specified value. <i>defaultValue</i> is returned if the value doesn't exist.
         *
         * Note that <var>name</var> can be null or "", at which point the
         * unnamed or default value of this Registry key is returned, if any.
         * The default values for RegistryKeys are OS-dependent.  NT doesn't
         * have them by default, but they can exist and be of any type.  On
         * Win95, the default value is always an empty key of type REG_SZ.
         * Win98 supports default values of any type, but defaults to REG_SZ.
         *
         * @param name Name of value to retrieve.
         * @param defaultValue Value to return if <i>name</i> doesn't exist.
         *
         * @return the data associated with the value.
         */
        public object GetValue(string name, object defaultValue = null, bool doNotExpand = false)
        {
            EnsureNotDisposed();

            object data = defaultValue;
            int type = 0;
            int datasize = 0;

            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);

            if (ret != 0)
            {
                // For stuff like ERROR_FILE_NOT_FOUND, we want to return null (data).
                // Some OS's returned ERROR_MORE_DATA even in success cases, so we 
                // want to continue on through the function. 
                if (ret != Win32Native.ERROR_MORE_DATA)
                    return data;
            }

            if (datasize < 0)
            {
                // unexpected code path
                Debug.Assert(false, "[InternalGetValue] RegQueryValue returned ERROR_SUCCESS but gave a negative datasize");
                datasize = 0;
            }

            switch (type)
            {
                case Win32Native.REG_NONE:
                case Win32Native.REG_DWORD_BIG_ENDIAN:
                case Win32Native.REG_BINARY:
                    {
                        byte[] blob = new byte[datasize];
                        ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                        data = blob;
                    }
                    break;
                case Win32Native.REG_QWORD:
                    {    // also REG_QWORD_LITTLE_ENDIAN
                        if (datasize > 8)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(long)
                            goto case Win32Native.REG_BINARY;
                        }
                        long blob = 0;
                        Debug.Assert(datasize == 8, "datasize==8");
                        // Here, datasize must be 8 when calling this
                        ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;
                case Win32Native.REG_DWORD:
                    {    // also REG_DWORD_LITTLE_ENDIAN
                        if (datasize > 4)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(int)
                            goto case Win32Native.REG_QWORD;
                        }
                        int blob = 0;
                        Debug.Assert(datasize == 4, "datasize==4");
                        // Here, datasize must be four when calling this
                        ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;

                case Win32Native.REG_SZ:
                    {
                        if (datasize % 2 == 1)
                        {
                            // handle the case where the registry contains an odd-byte length (corrupt data?)
                            try
                            {
                                datasize = checked(datasize + 1);
                            }
                            catch (OverflowException e)
                            {
                                throw new IOException(SR.Arg_RegGetOverflowBug, e);
                            }
                        }
                        char[] blob = new char[datasize / 2];

                        ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                        if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                        {
                            data = new string(blob, 0, blob.Length - 1);
                        }
                        else
                        {
                            // in the very unlikely case the data is missing null termination, 
                            // pass in the whole char[] to prevent truncating a character
                            data = new string(blob);
                        }
                    }
                    break;

                case Win32Native.REG_EXPAND_SZ:
                    {
                        if (datasize % 2 == 1)
                        {
                            // handle the case where the registry contains an odd-byte length (corrupt data?)
                            try
                            {
                                datasize = checked(datasize + 1);
                            }
                            catch (OverflowException e)
                            {
                                throw new IOException(SR.Arg_RegGetOverflowBug, e);
                            }
                        }
                        char[] blob = new char[datasize / 2];

                        ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);

                        if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                        {
                            data = new string(blob, 0, blob.Length - 1);
                        }
                        else
                        {
                            // in the very unlikely case the data is missing null termination, 
                            // pass in the whole char[] to prevent truncating a character
                            data = new string(blob);
                        }

                        if (!doNotExpand)
                            data = Environment.ExpandEnvironmentVariables((string)data);
                    }
                    break;
                case Win32Native.REG_MULTI_SZ:
                    {
                        if (datasize % 2 == 1)
                        {
                            // handle the case where the registry contains an odd-byte length (corrupt data?)
                            try
                            {
                                datasize = checked(datasize + 1);
                            }
                            catch (OverflowException e)
                            {
                                throw new IOException(SR.Arg_RegGetOverflowBug, e);
                            }
                        }
                        char[] blob = new char[datasize / 2];

                        ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);

                        // make sure the string is null terminated before processing the data
                        if (blob.Length > 0 && blob[blob.Length - 1] != (char)0)
                        {
                            try
                            {
                                char[] newBlob = new char[checked(blob.Length + 1)];
                                for (int i = 0; i < blob.Length; i++)
                                {
                                    newBlob[i] = blob[i];
                                }
                                newBlob[newBlob.Length - 1] = (char)0;
                                blob = newBlob;
                            }
                            catch (OverflowException e)
                            {
                                throw new IOException(SR.Arg_RegGetOverflowBug, e);
                            }
                            blob[blob.Length - 1] = (char)0;
                        }

                        IList<string> strings = new List<string>();
                        int cur = 0;
                        int len = blob.Length;

                        while (ret == 0 && cur < len)
                        {
                            int nextNull = cur;
                            while (nextNull < len && blob[nextNull] != (char)0)
                            {
                                nextNull++;
                            }

                            if (nextNull < len)
                            {
                                Debug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                if (nextNull - cur > 0)
                                {
                                    strings.Add(new string(blob, cur, nextNull - cur));
                                }
                                else
                                {
                                    // we found an empty string.  But if we're at the end of the data, 
                                    // it's just the extra null terminator. 
                                    if (nextNull != len - 1)
                                        strings.Add(string.Empty);
                                }
                            }
                            else
                            {
                                strings.Add(new string(blob, cur, len - cur));
                            }
                            cur = nextNull + 1;
                        }

                        data = new string[strings.Count];
                        strings.CopyTo((string[])data, 0);
                    }
                    break;
                case Win32Native.REG_LINK:
                default:
                    break;
            }

            return data;
        }

        private bool IsSystemKey()
        {
            return (state & STATE_SYSTEMKEY) != 0;
        }

        private bool IsWritable()
        {
            return (state & STATE_WRITEACCESS) != 0;
        }

        private bool IsPerfDataKey()
        {
            return false;
        }

        public unsafe void SetStringValue(string name, string value)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);

            if (name != null && name.Length > MaxValueLength)
            {
                throw new ArgumentException(SR.Arg_RegValStrLenBug);
            }

            EnsureWriteable();

            int result = Win32Native.RegSetValueEx(hkey,
                name,
                0,
                RegistryValueKind.String,
                value,
                checked(value.Length * 2 + 2));

            if (result != 0)
                Win32Error(result, null);
        }

        /**
         * Retrieves a string representation of this key.
         *
         * @return a string representing the key.
         */
        public override string ToString()
        {
            EnsureNotDisposed();
            return keyName;
        }

        /**
         * After calling GetLastWin32Error(), it clears the last error field,
         * so you must save the HResult and pass it to this method.  This method
         * will determine the appropriate exception to throw dependent on your
         * error, and depending on the error, insert a string into the message
         * gotten from the ResourceManager.
         */
        internal void Win32Error(int errorCode, string str)
        {
            switch (errorCode)
            {
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str));
                    else
                        throw new UnauthorizedAccessException();
                case Win32Native.ERROR_FILE_NOT_FOUND:
                    throw new IOException(SR.Arg_RegKeyNotFound, errorCode);

                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
            }
        }

        private void EnsureNotDisposed()
        {
            if (hkey == null)
            {
                ThrowHelper.ThrowObjectDisposedException(keyName, ExceptionResource.ObjectDisposed_RegKeyClosed);
            }
        }

        private void EnsureWriteable()
        {
            EnsureNotDisposed();
            if (!IsWritable())
            {
                ThrowHelper.ThrowUnauthorizedAccessException(ExceptionResource.UnauthorizedAccess_RegistryNoWrite);
            }
        }

        static private void ValidateKeyName(string name)
        {
            if (name == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
            }

            int nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
            int current = 0;
            while (nextSlash != -1)
            {
                if ((nextSlash - current) > MaxKeyLength)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);

                current = nextSlash + 1;
                nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
            }

            if ((name.Length - current) > MaxKeyLength)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
        }

        // Win32 constants for error handling
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    }
}
