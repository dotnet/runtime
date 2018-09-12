// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  Note on transaction support:
  Eventually we will want to add support for NT's transactions to our
  RegistryKey API's.  When we do this, here's
  the list of API's we need to make transaction-aware:

  RegCreateKeyEx
  RegDeleteKey
  RegDeleteValue
  RegEnumKeyEx
  RegEnumValue
  RegOpenKeyEx
  RegQueryInfoKey
  RegQueryValueEx
  RegSetValueEx

  We can ignore RegConnectRegistry (remote registry access doesn't yet have
  transaction support) and RegFlushKey.  RegCloseKey doesn't require any
  additional work.
 */

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
using System.Text;

namespace Microsoft.Win32
{
    /// <summary>Registry encapsulation. To get an instance of a RegistryKey use the Registry class's static members then call OpenSubKey.</summary>
#if REGISTRY_ASSEMBLY
    public
#else
    internal
#endif
    sealed class RegistryKey : MarshalByRefObject, IDisposable
    {
        // We could use const here, if C# supported ELEMENT_TYPE_I fully.
        internal static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
        internal static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
        internal static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
        internal static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
        internal static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004));
        internal static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));

        /// <summary>Names of keys.  This array must be in the same order as the HKEY values listed above.</summary>
        private static readonly string[] s_hkeyNames = new string[]
        {
            "HKEY_CLASSES_ROOT",
            "HKEY_CURRENT_USER",
            "HKEY_LOCAL_MACHINE",
            "HKEY_USERS",
            "HKEY_PERFORMANCE_DATA",
            "HKEY_CURRENT_CONFIG"
        };

        // MSDN defines the following limits for registry key names & values:
        // Key Name: 255 characters
        // Value name:  16,383 Unicode characters
        // Value: either 1 MB or current available memory, depending on registry format.
        private const int MaxKeyLength = 255;
        private const int MaxValueLength = 16383;

        private volatile SafeRegistryHandle _hkey = null;
        private volatile string _keyName;
        private volatile bool _remoteKey = false;
        private volatile StateFlags _state;
        private volatile RegistryKeyPermissionCheck checkMode;
        private volatile RegistryView _regView = RegistryView.Default;

        /// <summary>
        /// Creates a RegistryKey.
        /// This key is bound to hkey, if writable is <b>false</b> then no write operations
        /// will be allowed. If systemkey is set then the hkey won't be released
        /// when the object is GC'ed.
        /// The remoteKey flag when set to true indicates that we are dealing with registry entries
        /// on a remote machine and requires the program making these calls to have full trust.
        /// </summary>
        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData, RegistryView view)
        {
            _hkey = hkey;
            _keyName = "";
            _remoteKey = remoteKey;
            _regView = view;

            if (systemkey)
            {
                _state |= StateFlags.SystemKey;
            }
            if (writable)
            {
                _state |= StateFlags.WriteAccess;
            }
            if (isPerfData)
            {
                _state |= StateFlags.PerfData;
            }
            ValidateKeyView(view);
        }

        /// <summary>Retrieves a string representation of this key.</summary>
        /// <returns>A string representing the key.</returns>
        public override string ToString()
        {
            EnsureNotDisposed();
            return _keyName;
        }

        public string Name
        {
            get
            {
                EnsureNotDisposed();
                return _keyName;
            }
        }

        // This dummy method is added to have the same implemenatation of Registry class.
        // Its not being used anywhere.
        public RegistryKey CreateSubKey(string subkey)
        {
            return null;
        }

        private static void FixupPath(StringBuilder path)
        {
            Debug.Assert(path != null);
            int length = path.Length;
            bool fixup = false;
            char markerChar = (char)0xFFFF;

            int i = 1;
            while (i < length - 1)
            {
                if (path[i] == '\\')
                {
                    i++;
                    while (i < length)
                    {
                        if (path[i] == '\\')
                        {
                            path[i] = markerChar;
                            i++;
                            fixup = true;
                        }
                        else
                            break;
                    }
                }
                i++;
            }

            if (fixup)
            {
                i = 0;
                int j = 0;
                while (i < length)
                {
                    if (path[i] == markerChar)
                    {
                        i++;
                        continue;
                    }
                    path[j] = path[i];
                    i++;
                    j++;
                }
                path.Length += j - i;
            }
        }

        /// <summary>Retrieves the current state of the dirty property.</summary>
        /// <remarks>A key is marked as dirty if any operation has occurred that modifies the contents of the key.</remarks>
        /// <returns><b>true</b> if the key has been modified.</returns>
        private bool IsDirty() => (_state & StateFlags.Dirty) != 0;

        private bool IsSystemKey() => (_state & StateFlags.SystemKey) != 0;

        private bool IsWritable() => (_state & StateFlags.WriteAccess) != 0;

        private bool IsPerfDataKey() => (_state & StateFlags.PerfData) != 0;

        private void SetDirty() => _state |= StateFlags.Dirty;

        [Flags]
        private enum StateFlags
        {
            /// <summary>Dirty indicates that we have munged data that should be potentially written to disk.</summary>
            Dirty = 0x0001,
            /// <summary>SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened" or "closed".</summary>
            SystemKey = 0x0002,
            /// <summary>Access</summary>
            WriteAccess = 0x0004,
            /// <summary>Indicates if this key is for HKEY_PERFORMANCE_DATA</summary>
            PerfData = 0x0008
        }

        /**
         * Closes this key, flushes it to disk if the contents have been modified.
         */
        public void Close()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_hkey != null)
            {
                if (!IsSystemKey())
                {
                    try
                    {
                        _hkey.Dispose();
                    }
                    catch (IOException)
                    {
                        // we don't really care if the handle is invalid at this point
                    }
                    finally
                    {
                        _hkey = null;
                    }
                }
                else if (disposing && IsPerfDataKey())
                {
                    // System keys should never be closed.  However, we want to call RegCloseKey
                    // on HKEY_PERFORMANCE_DATA when called from PerformanceCounter.CloseSharedResources
                    // (i.e. when disposing is true) so that we release the PERFLIB cache and cause it
                    // to be refreshed (by re-reading the registry) when accessed subsequently. 
                    // This is the only way we can see the just installed perf counter.  
                    // NOTE: since HKEY_PERFORMANCE_DATA is process wide, there is inherent race condition in closing
                    // the key asynchronously. While Vista is smart enough to rebuild the PERFLIB resources
                    // in this situation the down level OSes are not. We have a small window between  
                    // the dispose below and usage elsewhere (other threads). This is By Design. 
                    // This is less of an issue when OS > NT5 (i.e Vista & higher), we can close the perfkey  
                    // (to release & refresh PERFLIB resources) and the OS will rebuild PERFLIB as necessary. 
                    Interop.Advapi32.RegCloseKey(RegistryKey.HKEY_PERFORMANCE_DATA);
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
            int errorCode = Interop.Advapi32.RegDeleteValue(_hkey, name);

            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE  
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            //
            if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND || errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
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

        /**
         * Retrieves a new RegistryKey that represents the requested key. Valid
         * values are:
         *
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER,
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS,
         * HKEY_PERFORMANCE_DATA,
         * HKEY_CURRENT_CONFIG,
         * HKEY_DYN_DATA.
         *
         * @param hKey HKEY_* to open.
         *
         * @return the RegistryKey requested.
         */
        internal static RegistryKey OpenBaseKey(RegistryHive hKey)
        {
            return OpenBaseKey(hKey, RegistryView.Default);
        }

        internal static RegistryKey OpenBaseKey(RegistryHive hKeyHive, RegistryView view)
        {
            IntPtr hKey = (IntPtr)((int)hKeyHive);
            int index = ((int)hKey) & 0x0FFFFFFF;
            Debug.Assert(index >= 0 && index < s_hkeyNames.Length, "index is out of range!");
            Debug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!");

            bool isPerf = hKey == HKEY_PERFORMANCE_DATA;
            // only mark the SafeHandle as ownsHandle if the key is HKEY_PERFORMANCE_DATA.
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, isPerf);

            RegistryKey key = new RegistryKey(srh, true, true, false, isPerf, view);
            key.checkMode = RegistryKeyPermissionCheck.Default;
            key._keyName = s_hkeyNames[index];
            return key;
        }

        /**
         * Retrieves a subkey. If readonly is <b>true</b>, then the subkey is opened with
         * read-only access.
         *
         * @param name Name or path of subkey to open.
         * @param readonly Set to <b>true</b> if you only need readonly access.
         *
         * @return the Subkey requested, or <b>null</b> if the operation failed.
         */
        public RegistryKey OpenSubKey(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash

            SafeRegistryHandle result = null;
            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey,
                name,
                0,
                GetRegistryKeyAccess(writable) | (int)_regView,
                out result);

            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, _remoteKey, false, _regView);
                key.checkMode = GetSubKeyPermissonCheck(writable);
                key._keyName = _keyName + "\\" + name;
                return key;
            }

            // Return null if we didn't find the key.
            if (ret == Interop.Errors.ERROR_ACCESS_DENIED || ret == Interop.Errors.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            }

            return null;
        }

        /**
         * Returns a subkey with read only permissions.
         *
         * @param name Name or path of subkey to open.
         *
         * @return the Subkey requested, or <b>null</b> if the operation failed.
         */
        public RegistryKey OpenSubKey(string name)
        {
            return OpenSubKey(name, false);
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

                while ((result = Interop.Advapi32.RegEnumKeyEx(
                    _hkey,
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

                while ((result = Interop.Advapi32.RegEnumValue(
                    _hkey,
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

        /// <summary>Retrieves the specified value. <b>null</b> is returned if the value doesn't exist</summary>
        /// <remarks>
        /// Note that <var>name</var> can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.
        /// </remarks>
        /// <param name="name">Name of value to retrieve.</param>
        /// <returns>The data associated with the value.</returns>
        public object GetValue(string name)
        {
            return InternalGetValue(name, null, false, true);
        }

        /**
         * Retrieves the specified value. <i>defaultValue</i> is returned if the value doesn't exist.
         *
         * Note that <var>name</var> can be null or "", at which point the
         * unnamed or default value of this Registry key is returned, if any.
         * The default values for RegistryKeys are OS-dependent.  NT doesn't
         * have them by default, but they can exist and be of any type. 
         *
         * @param name Name of value to retrieve.
         * @param defaultValue Value to return if <i>name</i> doesn't exist.
         *
         * @return the data associated with the value.
         */
        public object GetValue(string name, object defaultValue)
        {
            return InternalGetValue(name, defaultValue, false, true);
        }

        public object GetValue(string name, object defaultValue, RegistryValueOptions options)
        {
            if (options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }
            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            return InternalGetValue(name, defaultValue, doNotExpand, true);
        }

        internal object InternalGetValue(string name, object defaultValue, bool doNotExpand, bool checkSecurity)
        {
            if (checkSecurity)
            {
                // Name can be null!  It's the most common use of RegQueryValueEx
                EnsureNotDisposed();
            }

            object data = defaultValue;
            int type = 0;
            int datasize = 0;

            int ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, (byte[])null, ref datasize);

            if (ret != 0)
            {
                if (IsPerfDataKey())
                {
                    int size = 65000;
                    int sizeInput = size;

                    int r;
                    byte[] blob = new byte[size];
                    while (Interop.Errors.ERROR_MORE_DATA == (r = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref sizeInput)))
                    {
                        if (size == int.MaxValue)
                        {
                            // ERROR_MORE_DATA was returned however we cannot increase the buffer size beyond int.MaxValue
                            Win32Error(r, name);
                        }
                        else if (size > (int.MaxValue / 2))
                        {
                            // at this point in the loop "size * 2" would cause an overflow
                            size = int.MaxValue;
                        }
                        else
                        {
                            size *= 2;
                        }
                        sizeInput = size;
                        blob = new byte[size];
                    }
                    if (r != 0)
                        Win32Error(r, name);
                    return blob;
                }
                else
                {
                    // For stuff like ERROR_FILE_NOT_FOUND, we want to return null (data).
                    // Some OS's returned ERROR_MORE_DATA even in success cases, so we 
                    // want to continue on through the function. 
                    if (ret != Interop.Errors.ERROR_MORE_DATA)
                        return data;
                }
            }

            if (datasize < 0)
            {
                // unexpected code path
                Debug.Fail("[InternalGetValue] RegQueryValue returned ERROR_SUCCESS but gave a negative datasize");
                datasize = 0;
            }


            switch (type)
            {
                case Interop.Advapi32.RegistryValues.REG_NONE:
                case Interop.Advapi32.RegistryValues.REG_DWORD_BIG_ENDIAN:
                case Interop.Advapi32.RegistryValues.REG_BINARY:
                    {
                        byte[] blob = new byte[datasize];
                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);
                        data = blob;
                    }
                    break;
                case Interop.Advapi32.RegistryValues.REG_QWORD:
                    {    // also REG_QWORD_LITTLE_ENDIAN
                        if (datasize > 8)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(long)
                            goto case Interop.Advapi32.RegistryValues.REG_BINARY;
                        }
                        long blob = 0;
                        Debug.Assert(datasize == 8, "datasize==8");
                        // Here, datasize must be 8 when calling this
                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;
                case Interop.Advapi32.RegistryValues.REG_DWORD:
                    {    // also REG_DWORD_LITTLE_ENDIAN
                        if (datasize > 4)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(int)
                            goto case Interop.Advapi32.RegistryValues.REG_QWORD;
                        }
                        int blob = 0;
                        Debug.Assert(datasize == 4, "datasize==4");
                        // Here, datasize must be four when calling this
                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;

                case Interop.Advapi32.RegistryValues.REG_SZ:
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

                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);
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

                case Interop.Advapi32.RegistryValues.REG_EXPAND_SZ:
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

                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);

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
                case Interop.Advapi32.RegistryValues.REG_MULTI_SZ:
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

                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);

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
                case Interop.Advapi32.RegistryValues.REG_LINK:
                default:
                    break;
            }

            return data;
        }

        /**
         * Sets the specified value.
         *
         * @param name Name of value to store data in.
         * @param value Data to store.
         */
        public void SetValue(string name, object value)
        {
            SetValue(name, value, RegistryValueKind.Unknown);
        }

        public unsafe void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);

            if (name != null && name.Length > MaxValueLength)
            {
                throw new ArgumentException(SR.Arg_RegValStrLenBug);
            }

            if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind))
                throw new ArgumentException(SR.Arg_RegBadKeyKind, nameof(valueKind));

            EnsureWriteable();

            if (valueKind == RegistryValueKind.Unknown)
            {
                // this is to maintain compatibility with the old way of autodetecting the type.
                // SetValue(string, object) will come through this codepath.
                valueKind = CalculateValueKind(value);
            }

            int ret = 0;
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            string data = value.ToString();
                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                valueKind,
                                data,
                                checked(data.Length * 2 + 2));
                            break;
                        }

                    case RegistryValueKind.MultiString:
                        {
                            // Other thread might modify the input array after we calculate the buffer length.                            
                            // Make a copy of the input array to be safe.
                            string[] dataStrings = (string[])(((string[])value).Clone());
                            int sizeInBytes = 0;

                            // First determine the size of the array
                            //
                            for (int i = 0; i < dataStrings.Length; i++)
                            {
                                if (dataStrings[i] == null)
                                {
                                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull);
                                }
                                sizeInBytes = checked(sizeInBytes + (dataStrings[i].Length + 1) * 2);
                            }
                            sizeInBytes = checked(sizeInBytes + 2);

                            byte[] basePtr = new byte[sizeInBytes];
                            fixed (byte* b = basePtr)
                            {
                                IntPtr currentPtr = new IntPtr((void*)b);

                                // Write out the strings...
                                //
                                for (int i = 0; i < dataStrings.Length; i++)
                                {
                                    // Assumes that the Strings are always null terminated.
                                    string.InternalCopy(dataStrings[i], currentPtr, (checked(dataStrings[i].Length * 2)));
                                    currentPtr = new IntPtr((long)currentPtr + (checked(dataStrings[i].Length * 2)));
                                    *(char*)(currentPtr.ToPointer()) = '\0';
                                    currentPtr = new IntPtr((long)currentPtr + 2);
                                }

                                *(char*)(currentPtr.ToPointer()) = '\0';
                                currentPtr = new IntPtr((long)currentPtr + 2);

                                ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                    name,
                                    0,
                                    RegistryValueKind.MultiString,
                                    basePtr,
                                    sizeInBytes);
                            }
                            break;
                        }

                    case RegistryValueKind.None:
                    case RegistryValueKind.Binary:
                        byte[] dataBytes = (byte[])value;
                        ret = Interop.Advapi32.RegSetValueEx(_hkey,
                            name,
                            0,
                            (valueKind == RegistryValueKind.None ? Interop.Advapi32.RegistryValues.REG_NONE : RegistryValueKind.Binary),
                            dataBytes,
                            dataBytes.Length);
                        break;

                    case RegistryValueKind.DWord:
                        {
                            // We need to use Convert here because we could have a boxed type cannot be
                            // unboxed and cast at the same time.  I.e. ((int)(object)(short) 5) will fail.
                            int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                RegistryValueKind.DWord,
                                ref data,
                                4);
                            break;
                        }

                    case RegistryValueKind.QWord:
                        {
                            long data = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                RegistryValueKind.QWord,
                                ref data,
                                8);
                            break;
                        }
                }
            }
            catch (OverflowException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (InvalidOperationException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (FormatException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }

            if (ret == 0)
            {
                SetDirty();
            }
            else
                Win32Error(ret, null);
        }

        private RegistryValueKind CalculateValueKind(object value)
        {
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days.
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior.
            if (value is int)
                return RegistryValueKind.DWord;
            else if (value is Array)
            {
                if (value is byte[])
                    return RegistryValueKind.Binary;
                else if (value is string[])
                    return RegistryValueKind.MultiString;
                else
                    throw new ArgumentException(SR.Format(SR.Arg_RegSetBadArrType, value.GetType().Name));
            }
            else
                return RegistryValueKind.String;
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
                case Interop.Errors.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str));
                    else
                        throw new UnauthorizedAccessException();

                case Interop.Errors.ERROR_INVALID_HANDLE:
                    /**
                     * For normal RegistryKey instances we dispose the SafeRegHandle and throw IOException.
                     * However, for HKEY_PERFORMANCE_DATA (on a local or remote machine) we avoid disposing the
                     * SafeRegHandle and only throw the IOException.  This is to workaround reentrancy issues
                     * in PerformanceCounter.NextValue() where the API could throw {NullReference, ObjectDisposed, ArgumentNull}Exception
                     * on reentrant calls because of this error code path in RegistryKey
                     *
                     * Normally we'd make our caller synchronize access to a shared RegistryKey instead of doing something like this,
                     * however we shipped PerformanceCounter.NextValue() un-synchronized in v2.0RTM and customers have taken a dependency on 
                     * this behavior (being able to simultaneously query multiple remote-machine counters on multiple threads, instead of 
                     * having serialized access).
                     */
                    if (!IsPerfDataKey())
                    {
                        _hkey.SetHandleAsInvalid();
                        _hkey = null;
                    }
                    goto default;

                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    throw new IOException(SR.Arg_RegKeyNotFound, errorCode);

                default:
                    throw new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode);
            }
        }

        internal static string FixupName(string name)
        {
            Debug.Assert(name != null, "[FixupName]name!=null");
            if (!name.Contains('\\'))
                return name;

            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb);
            int temp = sb.Length - 1;
            if (temp >= 0 && sb[temp] == '\\') // Remove trailing slash
                sb.Length = temp;
            return sb.ToString();
        }

        private void EnsureNotDisposed()
        {
            if (_hkey == null)
            {
                ThrowHelper.ThrowObjectDisposedException(_keyName, ExceptionResource.ObjectDisposed_RegKeyClosed);
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

        private static int GetRegistryKeyAccess(bool isWritable)
        {
            int winAccess;
            if (!isWritable)
            {
                winAccess = Interop.Advapi32.RegistryOperations.KEY_READ;
            }
            else
            {
                winAccess = Interop.Advapi32.RegistryOperations.KEY_READ | Interop.Advapi32.RegistryOperations.KEY_WRITE;
            }

            return winAccess;
        }

        private RegistryKeyPermissionCheck GetSubKeyPermissonCheck(bool subkeyWritable)
        {
            if (checkMode == RegistryKeyPermissionCheck.Default)
            {
                return checkMode;
            }

            if (subkeyWritable)
            {
                return RegistryKeyPermissionCheck.ReadWriteSubTree;
            }
            else
            {
                return RegistryKeyPermissionCheck.ReadSubTree;
            }
        }

        private static void ValidateKeyName(string name)
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

        private static void ValidateKeyView(RegistryView view)
        {
            if (view != RegistryView.Default && view != RegistryView.Registry32 && view != RegistryView.Registry64)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryViewCheck, ExceptionArgument.view);
            }
        }

        // Win32 constants for error handling
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    }

    // the name for this API is meant to mimic FileMode, which has similar values

    internal enum RegistryKeyPermissionCheck
    {
        Default = 0,
        ReadSubTree = 1,
        ReadWriteSubTree = 2
    }
}
