// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Text;

namespace Microsoft.Win32
{
    /// <summary>Registry encapsulation. To get an instance of a RegistryKey use the Registry class's static members then call OpenSubKey.</summary>
    public sealed partial class RegistryKey : MarshalByRefObject, IDisposable
    {
        private static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
        private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
        private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
        private static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
        private static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004));
        private static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));

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

        private volatile SafeRegistryHandle _hkey;
        private volatile string _keyName;
        private readonly bool _remoteKey;
        private volatile StateFlags _state;
        private volatile RegistryKeyPermissionCheck _checkMode;
        private readonly RegistryView _regView = RegistryView.Default;

        /// <summary>
        /// Creates a RegistryKey. This key is bound to hkey, if writable is <b>false</b> then no write operations will be allowed.
        /// </summary>
        private RegistryKey(SafeRegistryHandle hkey, bool writable, RegistryView view) :
            this(hkey, writable, false, false, false, view)
        {
        }

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
            ValidateKeyView(view);

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
        }

        public void Flush()
        {
            FlushCore();
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
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
                        _hkey = null!;
                    }
                }
                else if (IsPerfDataKey())
                {
                    ClosePerfDataKey();
                }
            }
        }

        /// <summary>Creates a new subkey, or opens an existing one.</summary>
        /// <param name="subkey">Name or path to subkey to create or open.</param>
        /// <returns>The subkey, or <b>null</b> if the operation failed.</returns>
        public RegistryKey CreateSubKey(string subkey)
        {
            return CreateSubKey(subkey, _checkMode);
        }

        public RegistryKey CreateSubKey(string subkey, bool writable)
        {
            return CreateSubKey(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(string subkey, bool writable, RegistryOptions options)
        {
            return CreateSubKey(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, options);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck)
        {
            return CreateSubKey(subkey, permissionCheck, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions registryOptions, RegistrySecurity? registrySecurity)
        {
            return CreateSubKey(subkey, permissionCheck, registryOptions);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistrySecurity? registrySecurity)
        {
            return CreateSubKey(subkey, permissionCheck, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions registryOptions)
        {
            ValidateKeyOptions(registryOptions);
            ValidateKeyName(subkey);
            ValidateKeyMode(permissionCheck);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // only keys opened under read mode is not writable
            if (!_remoteKey)
            {
                RegistryKey? key = InternalOpenSubKeyWithoutSecurityChecks(subkey, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree));
                if (key != null)
                {
                    // Key already exits
                    key._checkMode = permissionCheck;
                    return key;
                }
            }

            return CreateSubKeyInternalCore(subkey, permissionCheck, registryOptions);
        }

        /// <summary>
        /// Deletes the specified subkey. Will throw an exception if the subkey has
        /// subkeys. To delete a tree of subkeys use, DeleteSubKeyTree.
        /// </summary>
        /// <param name="subkey">SubKey to delete.</param>
        /// <exception cref="InvalidOperationException">Thrown if the subkey has child subkeys.</exception>
        public void DeleteSubKey(string subkey)
        {
            DeleteSubKey(subkey, true);
        }

        public void DeleteSubKey(string subkey, bool throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // Open the key we are deleting and check for children. Be sure to
            // explicitly call close to avoid keeping an extra HKEY open.
            //
            RegistryKey? key = InternalOpenSubKeyWithoutSecurityChecks(subkey, false);
            if (key != null)
            {
                using (key)
                {
                    if (key.SubKeyCount > 0)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_RegRemoveSubKey);
                    }
                }

                DeleteSubKeyCore(subkey, throwOnMissingSubKey);
            }
            else // there is no key which also means there is no subkey
            {
                if (throwOnMissingSubKey)
                {
                    throw new ArgumentException(SR.Arg_RegSubKeyAbsent);
                }
            }
        }

        /// <summary>Recursively deletes a subkey and any child subkeys.</summary>
        /// <param name="subkey">SubKey to delete.</param>
        public void DeleteSubKeyTree(string subkey)
        {
            DeleteSubKeyTree(subkey, throwOnMissingSubKey: true);
        }

        public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);

            // Security concern: Deleting a hive's "" subkey would delete all
            // of that hive's contents.  Don't allow "".
            if (subkey.Length == 0 && IsSystemKey())
            {
                throw new ArgumentException(SR.Arg_RegKeyDelHive);
            }

            EnsureWriteable();

            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            RegistryKey? key = InternalOpenSubKeyWithoutSecurityChecks(subkey, true);
            if (key != null)
            {
                using (key)
                {
                    if (key.SubKeyCount > 0)
                    {
                        string[] keys = key.GetSubKeyNames();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }

                DeleteSubKeyTreeCore(subkey);
            }
            else if (throwOnMissingSubKey)
            {
                throw new ArgumentException(SR.Arg_RegSubKeyAbsent);
            }
        }

        /// <summary>
        /// An internal version which does no security checks or argument checking.  Skipping the
        /// security checks should give us a slight perf gain on large trees.
        /// </summary>
        private void DeleteSubKeyTreeInternal(string subkey)
        {
            RegistryKey? key = InternalOpenSubKeyWithoutSecurityChecks(subkey, true);
            if (key != null)
            {
                using (key)
                {
                    if (key.SubKeyCount > 0)
                    {
                        string[] keys = key.GetSubKeyNames();
                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }

                DeleteSubKeyTreeCore(subkey);
            }
            else
            {
                throw new ArgumentException(SR.Arg_RegSubKeyAbsent);
            }
        }

        /// <summary>Deletes the specified value from this key.</summary>
        /// <param name="name">Name of value to delete.</param>
        public void DeleteValue(string name)
        {
            DeleteValue(name, true);
        }

        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            EnsureWriteable();
            DeleteValueCore(name, throwOnMissingValue);
        }

        public static RegistryKey OpenBaseKey(RegistryHive hKey, RegistryView view)
        {
            ValidateKeyView(view);
            return OpenBaseKeyCore(hKey, view);
        }

        /// <summary>Retrieves a new RegistryKey that represents the requested key on a foreign machine.</summary>
        /// <param name="hKey">hKey HKEY_* to open.</param>
        /// <param name="machineName">Name the machine to connect to.</param>
        /// <returns>The RegistryKey requested.</returns>
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, string machineName)
        {
            return OpenRemoteBaseKey(hKey, machineName, RegistryView.Default);
        }

        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, string machineName, RegistryView view)
        {
            if (machineName == null)
            {
                throw new ArgumentNullException(nameof(machineName));
            }
            ValidateKeyView(view);

            return OpenRemoteBaseKeyCore(hKey, machineName, view);
        }

        /// <summary>Returns a subkey with read only permissions.</summary>
        /// <param name="name">Name or path of subkey to open.</param>
        /// <returns>The Subkey requested, or <b>null</b> if the operation failed.</returns>
        public RegistryKey? OpenSubKey(string name)
        {
            return OpenSubKey(name, false);
        }

        /// <summary>
        /// Retrieves a subkey. If readonly is <b>true</b>, then the subkey is opened with
        /// read-only access.
        /// </summary>
        /// <param name="name">Name or the path of subkey to open.</param>
        /// <param name="writable">Set to <b>true</b> if you only need readonly access.</param>
        /// <returns>the Subkey requested, or <b>null</b> if the operation failed.</returns>
        public RegistryKey? OpenSubKey(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name);

            return InternalOpenSubKeyCore(name, writable);
        }

        public RegistryKey? OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
        {
            ValidateKeyMode(permissionCheck);

            return OpenSubKey(name, permissionCheck, (RegistryRights)GetRegistryKeyAccess(permissionCheck));
        }

        public RegistryKey? OpenSubKey(string name, RegistryRights rights)
        {
            return OpenSubKey(name, this._checkMode, rights);
        }

        public RegistryKey? OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck, RegistryRights rights)
        {
            ValidateKeyName(name);
            ValidateKeyMode(permissionCheck);

            ValidateKeyRights(rights);

            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash

            return InternalOpenSubKeyCore(name, permissionCheck, (int)rights);
        }

        internal RegistryKey? InternalOpenSubKeyWithoutSecurityChecks(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();

            return InternalOpenSubKeyWithoutSecurityChecksCore(name, writable);
        }

        public RegistrySecurity GetAccessControl()
        {
            return GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public RegistrySecurity GetAccessControl(AccessControlSections includeSections)
        {
            EnsureNotDisposed();
            return new RegistrySecurity(Handle, Name, includeSections);
        }

        public void SetAccessControl(RegistrySecurity registrySecurity)
        {
            EnsureWriteable();
            if (registrySecurity == null)
            {
                throw new ArgumentNullException(nameof(registrySecurity));
            }

            registrySecurity.Persist(Handle, Name);
        }

        /// <summary>Retrieves the count of subkeys.</summary>
        /// <returns>A count of subkeys.</returns>
        public int SubKeyCount
        {
            get
            {
                EnsureNotDisposed();
                return InternalSubKeyCountCore();
            }
        }

        public RegistryView View
        {
            get
            {
                EnsureNotDisposed();
                return _regView;
            }
        }

        public SafeRegistryHandle Handle
        {
            get
            {
                EnsureNotDisposed();
                return IsSystemKey() ? SystemKeyHandle : _hkey;
            }
        }

        public static RegistryKey FromHandle(SafeRegistryHandle handle)
        {
            return FromHandle(handle, RegistryView.Default);
        }

        public static RegistryKey FromHandle(SafeRegistryHandle handle, RegistryView view)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            ValidateKeyView(view);

            return new RegistryKey(handle, writable: true, view: view);
        }

        /// <summary>Retrieves an array of strings containing all the subkey names.</summary>
        /// <returns>All subkey names.</returns>
        public string[] GetSubKeyNames()
        {
            EnsureNotDisposed();
            int subkeys = SubKeyCount;
            return subkeys > 0 ?
                InternalGetSubKeyNamesCore(subkeys) :
                Array.Empty<string>();
        }

        /// <summary>Retrieves the count of values.</summary>
        /// <returns>A count of values.</returns>
        public int ValueCount
        {
            get
            {
                EnsureNotDisposed();
                return InternalValueCountCore();
            }
        }

        /// <summary>Retrieves an array of strings containing all the value names.</summary>
        /// <returns>All value names.</returns>
        public string[] GetValueNames()
        {
            EnsureNotDisposed();

            int values = ValueCount;
            return values > 0 ?
                GetValueNamesCore(values) :
                Array.Empty<string>();
        }

        /// <summary>Retrieves the specified value. <b>null</b> is returned if the value doesn't exist</summary>
        /// <remarks>
        /// Note that <var>name</var> can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.
        /// </remarks>
        /// <param name="name">Name of value to retrieve.</param>
        /// <returns>The data associated with the value.</returns>
        public object? GetValue(string? name)
        {
            return InternalGetValue(name, null, false);
        }

        /// <summary>Retrieves the specified value. <i>defaultValue</i> is returned if the value doesn't exist.</summary>
        /// <remarks>
        /// Note that <var>name</var> can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.
        /// The default values for RegistryKeys are OS-dependent.  NT doesn't
        /// have them by default, but they can exist and be of any type.  On
        /// Win95, the default value is always an empty key of type REG_SZ.
        /// Win98 supports default values of any type, but defaults to REG_SZ.
        /// </remarks>
        /// <param name="name">Name of value to retrieve.</param>
        /// <param name="defaultValue">Value to return if <i>name</i> doesn't exist.</param>
        /// <returns>The data associated with the value.</returns>
        public object? GetValue(string? name, object? defaultValue)
        {
            return InternalGetValue(name, defaultValue, false);
        }

        public object? GetValue(string? name, object? defaultValue, RegistryValueOptions options)
        {
            if (options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }
            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            return InternalGetValue(name, defaultValue, doNotExpand);
        }

        private object? InternalGetValue(string? name, object? defaultValue, bool doNotExpand)
        {
            EnsureNotDisposed();
            return InternalGetValueCore(name, defaultValue, doNotExpand);
        }

        public RegistryValueKind GetValueKind(string? name)
        {
            EnsureNotDisposed();
            return GetValueKindCore(name);
        }

        public string Name
        {
            get
            {
                EnsureNotDisposed();
                return _keyName;
            }
        }

        /// <summary>Sets the specified value.</summary>
        /// <param name="name">Name of value to store data in.</param>
        /// <param name="value">Data to store.</param>
        public void SetValue(string? name, object value)
        {
            SetValue(name, value, RegistryValueKind.Unknown);
        }

        public void SetValue(string? name, object value, RegistryValueKind valueKind)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (name != null && name.Length > MaxValueLength)
            {
                throw new ArgumentException(SR.Arg_RegValStrLenBug, nameof(name));
            }

            if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind))
            {
                throw new ArgumentException(SR.Arg_RegBadKeyKind, nameof(valueKind));
            }

            EnsureWriteable();

            if (valueKind == RegistryValueKind.Unknown)
            {
                // this is to maintain compatibility with the old way of autodetecting the type.
                // SetValue(string, object) will come through this codepath.
                valueKind = CalculateValueKind(value);
            }

            SetValueCore(name, value, valueKind);
        }

        private RegistryValueKind CalculateValueKind(object value)
        {
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days.
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior.
            if (value is int)
            {
                return RegistryValueKind.DWord;
            }
            else if (value is Array)
            {
                if (value is byte[])
                {
                    return RegistryValueKind.Binary;
                }
                else if (value is string[])
                {
                    return RegistryValueKind.MultiString;
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.Arg_RegSetBadArrType, value.GetType().Name));
                }
            }
            else
            {
                return RegistryValueKind.String;
            }
        }

        /// <summary>Retrieves a string representation of this key.</summary>
        /// <returns>A string representing the key.</returns>
        public override string ToString()
        {
            EnsureNotDisposed();
            return _keyName;
        }

        private static string FixupName(string name)
        {
            Debug.Assert(name != null, "[FixupName]name!=null");

            if (!name.Contains('\\'))
            {
                return name;
            }

            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb);
            int temp = sb.Length - 1;
            if (temp >= 0 && sb[temp] == '\\') // Remove trailing slash
            {
                sb.Length = temp;
            }

            return sb.ToString();
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
                    while (i < length && path[i] == '\\')
                    {
                        path[i] = markerChar;
                        i++;
                        fixup = true;
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

        private void EnsureNotDisposed()
        {
            if (_hkey == null)
            {
                throw new ObjectDisposedException(_keyName, SR.ObjectDisposed_RegKeyClosed);
            }
        }

        private void EnsureWriteable()
        {
            EnsureNotDisposed();
            if (!IsWritable())
            {
                throw new UnauthorizedAccessException(SR.UnauthorizedAccess_RegistryNoWrite);
            }
        }

        private RegistryKeyPermissionCheck GetSubKeyPermissionCheck(bool subkeyWritable)
        {
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                return _checkMode;
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
                throw new ArgumentNullException(nameof(name));
            }

            int nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
            int current = 0;
            while (nextSlash != -1)
            {
                if ((nextSlash - current) > MaxKeyLength)
                {
                    throw new ArgumentException(SR.Arg_RegKeyStrLenBug, nameof(name));
                }
                current = nextSlash + 1;
                nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
            }

            if ((name.Length - current) > MaxKeyLength)
            {
                throw new ArgumentException(SR.Arg_RegKeyStrLenBug, nameof(name));
            }
        }

        private static void ValidateKeyMode(RegistryKeyPermissionCheck mode)
        {
            if (mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree)
            {
                throw new ArgumentException(SR.Argument_InvalidRegistryKeyPermissionCheck, nameof(mode));
            }
        }

        private static void ValidateKeyOptions(RegistryOptions options)
        {
            if (options < RegistryOptions.None || options > RegistryOptions.Volatile)
            {
                throw new ArgumentException(SR.Argument_InvalidRegistryOptionsCheck, nameof(options));
            }
        }

        private static void ValidateKeyView(RegistryView view)
        {
            if (view != RegistryView.Default && view != RegistryView.Registry32 && view != RegistryView.Registry64)
            {
                throw new ArgumentException(SR.Argument_InvalidRegistryViewCheck, nameof(view));
            }
        }

        private static void ValidateKeyRights(RegistryRights rights)
        {
            if (0 != (rights & ~RegistryRights.FullControl))
            {
                // We need to throw SecurityException here for compatiblity reason,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(SR.Security_RegistryPermission);
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
    }
}
