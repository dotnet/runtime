// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace System.DirectoryServices.Protocols
{
    public enum ExtendedDNFlag
    {
        HexString = 0,
        StandardString = 1
    }

    [Flags]
    public enum SecurityMasks
    {
        None = 0,
        Owner = 1,
        Group = 2,
        Dacl = 4,
        Sacl = 8
    }

    [Flags]
    public enum DirectorySynchronizationOptions : long
    {
        None = 0,
        ObjectSecurity = 0x1,
        ParentsFirst = 0x0800,
        PublicDataOnly = 0x2000,
        IncrementalValues = 0x80000000
    }

    public enum SearchOption
    {
        DomainScope = 1,
        PhantomRoot = 2
    }

    internal static class UtilityHandle
    {
        private static readonly ConnectionHandle s_handle = new ConnectionHandle();

        public static ConnectionHandle GetHandle() => s_handle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class SortKey
    {
        private string _name;
        private string _rule;
        private bool _order;

        public SortKey()
        {
        }

        public SortKey(string attributeName, string matchingRule, bool reverseOrder)
        {
            AttributeName = attributeName;
            _rule = matchingRule;
            _order = reverseOrder;
        }

        public string AttributeName
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string MatchingRule
        {
            get => _rule;
            set => _rule = value;
        }

        public bool ReverseOrder
        {
            get => _order;
            set => _order = value;
        }
    }

    public class DirectoryControl
    {
        internal byte[] _directoryControlValue;

        public DirectoryControl(string type, byte[] value, bool isCritical, bool serverSide)
        {
            ArgumentNullException.ThrowIfNull(type);

            Type = type;

            if (value != null)
            {
                _directoryControlValue = new byte[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    _directoryControlValue[i] = value[i];
                }
            }
            IsCritical = isCritical;
            ServerSide = serverSide;
        }

        public virtual byte[] GetValue()
        {
            if (_directoryControlValue == null)
            {
                return Array.Empty<byte>();
            }

            byte[] tempValue = new byte[_directoryControlValue.Length];
            for (int i = 0; i < _directoryControlValue.Length; i++)
            {
                tempValue[i] = _directoryControlValue[i];
            }
            return tempValue;
        }

        public string Type { get; }

        public bool IsCritical { get; set; }

        public bool ServerSide { get; set; }

        internal static void TransformControls(DirectoryControl[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
            {
                Debug.Assert(controls[i] != null);
                byte[] value = controls[i].GetValue();
                if (controls[i].Type == "1.2.840.113556.1.4.319")
                {
                    // The control is a PageControl.
                    object[] result = BerConverter.Decode("{iO}", value);
                    Debug.Assert((result != null) && (result.Length == 2));

                    int size = (int)result[0];
                    // user expects cookie with length 0 as paged search is done.
                    byte[] cookie = (byte[])result[1] ?? Array.Empty<byte>();

                    PageResultResponseControl pageControl = new PageResultResponseControl(size, cookie, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = pageControl;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.1504")
                {
                    // The control is an AsqControl.
                    object[] o = BerConverter.Decode("{e}", value);
                    Debug.Assert((o != null) && (o.Length == 1));

                    int result = (int)o[0];
                    AsqResponseControl asq = new AsqResponseControl(result, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = asq;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.841")
                {
                    // The control is a DirSyncControl.
                    object[] o = BerConverter.Decode("{iiO}", value);
                    Debug.Assert(o != null && o.Length == 3);

                    int moreData = (int)o[0];
                    int count = (int)o[1];
                    byte[] dirsyncCookie = (byte[])o[2];

                    DirSyncResponseControl dirsync = new DirSyncResponseControl(dirsyncCookie, (moreData == 0 ? false : true), count, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = dirsync;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.474")
                {
                    // The control is a SortControl.
                    int result;
                    string attribute = null;
                    object[] o = BerConverter.TryDecode("{ea}", value, out bool decodeSucceeded);

                    // decode might fail as AD for example never returns attribute name, we don't want to unnecessarily throw and catch exception
                    if (decodeSucceeded)
                    {
                        Debug.Assert(o != null && o.Length == 2);
                        result = (int)o[0];
                        attribute = (string)o[1];
                    }
                    else
                    {
                        // decoding might fail as attribute is optional
                        o = BerConverter.Decode("{e}", value);
                        Debug.Assert(o != null && o.Length == 1);

                        result = (int)o[0];
                    }

                    SortResponseControl sort = new SortResponseControl((ResultCode)result, attribute, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = sort;
                }
                else if (controls[i].Type == "2.16.840.1.113730.3.4.10")
                {
                    // The control is a VlvResponseControl.
                    int position;
                    int count;
                    int result;
                    byte[] context = null;
                    object[] o = BerConverter.TryDecode("{iieO}", value, out bool decodeSucceeded);

                    if (decodeSucceeded)
                    {
                        Debug.Assert(o != null && o.Length == 4);
                        position = (int)o[0];
                        count = (int)o[1];
                        result = (int)o[2];
                        context = (byte[])o[3];
                    }
                    else
                    {
                        o = BerConverter.Decode("{iie}", value);
                        Debug.Assert(o != null && o.Length == 3);
                        position = (int)o[0];
                        count = (int)o[1];
                        result = (int)o[2];
                    }

                    VlvResponseControl vlv = new VlvResponseControl(position, count, context, (ResultCode)result, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = vlv;
                }
            }
        }
    }

    public class AsqRequestControl : DirectoryControl
    {
        public AsqRequestControl() : base("1.2.840.113556.1.4.1504", null, true, true)
        {
        }

        public AsqRequestControl(string attributeName) : this()
        {
            AttributeName = attributeName;
        }

        public string AttributeName { get; set; }

        public override byte[] GetValue()
        {
            _directoryControlValue = BerConverter.Encode("{s}", new object[] { AttributeName });
            return base.GetValue();
        }
    }

    public class AsqResponseControl : DirectoryControl
    {
        internal AsqResponseControl(int result, bool criticality, byte[] controlValue) : base("1.2.840.113556.1.4.1504", controlValue, criticality, true)
        {
            Result = (ResultCode)result;
        }

        public ResultCode Result { get; }
    }

    public class CrossDomainMoveControl : DirectoryControl
    {
        public CrossDomainMoveControl() : base("1.2.840.113556.1.4.521", null, true, true)
        {
        }

        public CrossDomainMoveControl(string targetDomainController) : this()
        {
            TargetDomainController = targetDomainController;
        }

        public string TargetDomainController { get; set; }

        public override byte[] GetValue()
        {
            if (TargetDomainController != null)
            {
                UTF8Encoding encoder = new UTF8Encoding();
                byte[] bytes = encoder.GetBytes(TargetDomainController);

                // Allocate large enough space for the '\0' character.
                _directoryControlValue = new byte[bytes.Length + 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    _directoryControlValue[i] = bytes[i];
                }
            }
            return base.GetValue();
        }
    }

    public class DomainScopeControl : DirectoryControl
    {
        public DomainScopeControl() : base("1.2.840.113556.1.4.1339", null, true, true)
        {
        }
    }

    public class ExtendedDNControl : DirectoryControl
    {
        private ExtendedDNFlag _flag = ExtendedDNFlag.HexString;

        public ExtendedDNControl() : base("1.2.840.113556.1.4.529", null, true, true)
        {
        }

        public ExtendedDNControl(ExtendedDNFlag flag) : this()
        {
            Flag = flag;
        }

        public ExtendedDNFlag Flag
        {
            get => _flag;
            set
            {
                if (value < ExtendedDNFlag.HexString || value > ExtendedDNFlag.StandardString)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ExtendedDNFlag));

                _flag = value;
            }
        }
        public override byte[] GetValue()
        {
            _directoryControlValue = BerConverter.Encode("{i}", new object[] { (int)Flag });
            return base.GetValue();
        }
    }

    public class LazyCommitControl : DirectoryControl
    {
        public LazyCommitControl() : base("1.2.840.113556.1.4.619", null, true, true) { }
    }

    public class DirectoryNotificationControl : DirectoryControl
    {
        public DirectoryNotificationControl() : base("1.2.840.113556.1.4.528", null, true, true) { }
    }

    public class PermissiveModifyControl : DirectoryControl
    {
        public PermissiveModifyControl() : base("1.2.840.113556.1.4.1413", null, true, true) { }
    }

    public class SecurityDescriptorFlagControl : DirectoryControl
    {
        public SecurityDescriptorFlagControl() : base("1.2.840.113556.1.4.801", null, true, true) { }

        public SecurityDescriptorFlagControl(SecurityMasks masks) : this()
        {
            SecurityMasks = masks;
        }

        // We don't do validation to the dirsync flag here as underneath API does not check for it and we don't want to put
        // unnecessary limitation on it.
        public SecurityMasks SecurityMasks { get; set; }

        public override byte[] GetValue()
        {
            _directoryControlValue = BerConverter.Encode("{i}", new object[] { (int)SecurityMasks });
            return base.GetValue();
        }
    }

    public class SearchOptionsControl : DirectoryControl
    {
        private SearchOption _searchOption = SearchOption.DomainScope;
        public SearchOptionsControl() : base("1.2.840.113556.1.4.1340", null, true, true) { }

        public SearchOptionsControl(SearchOption flags) : this()
        {
            SearchOption = flags;
        }

        public SearchOption SearchOption
        {
            get => _searchOption;
            set
            {
                if (value < SearchOption.DomainScope || value > SearchOption.PhantomRoot)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(SearchOption));

                _searchOption = value;
            }
        }

        public override byte[] GetValue()
        {
            _directoryControlValue = BerConverter.Encode("{i}", new object[] { (int)SearchOption });
            return base.GetValue();
        }
    }

    public class ShowDeletedControl : DirectoryControl
    {
        public ShowDeletedControl() : base("1.2.840.113556.1.4.417", null, true, true) { }
    }

    public class TreeDeleteControl : DirectoryControl
    {
        public TreeDeleteControl() : base("1.2.840.113556.1.4.805", null, true, true) { }
    }

    public class VerifyNameControl : DirectoryControl
    {
        private string _serverName;

        public VerifyNameControl() : base("1.2.840.113556.1.4.1338", null, true, true) { }

        public VerifyNameControl(string serverName) : this()
        {
            ArgumentNullException.ThrowIfNull(serverName);

            _serverName = serverName;
        }

        public VerifyNameControl(string serverName, int flag) : this(serverName)
        {
            Flag = flag;
        }

        public string ServerName
        {
            get => _serverName;
            set => _serverName = value ?? throw new ArgumentNullException(nameof(value));
        }

        public int Flag { get; set; }

        public override byte[] GetValue()
        {
            byte[] tmpValue = null;
            if (ServerName != null)
            {
                UnicodeEncoding unicode = new UnicodeEncoding();
                tmpValue = unicode.GetBytes(ServerName);
            }

            _directoryControlValue = BerConverter.Encode("{io}", new object[] { Flag, tmpValue });
            return base.GetValue();
        }
    }

    public class DirSyncRequestControl : DirectoryControl
    {
        private byte[] _dirsyncCookie;
        private int _count = 1048576;

        public DirSyncRequestControl() : base("1.2.840.113556.1.4.841", null, true, true) { }
        public DirSyncRequestControl(byte[] cookie) : this()
        {
            _dirsyncCookie = cookie;
        }

        public DirSyncRequestControl(byte[] cookie, DirectorySynchronizationOptions option) : this(cookie)
        {
            Option = option;
        }

        public DirSyncRequestControl(byte[] cookie, DirectorySynchronizationOptions option, int attributeCount) : this(cookie, option)
        {
            AttributeCount = attributeCount;
        }

        public byte[] Cookie
        {
            get
            {
                if (_dirsyncCookie == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempCookie = new byte[_dirsyncCookie.Length];
                for (int i = 0; i < tempCookie.Length; i++)
                {
                    tempCookie[i] = _dirsyncCookie[i];
                }

                return tempCookie;
            }
            set => _dirsyncCookie = value;
        }

        // We don't do validation to the dirsync flag here as underneath API does not check for it and we don't want to put
        // unnecessary limitation on it.
        public DirectorySynchronizationOptions Option { get; set; }

        public int AttributeCount
        {
            get => _count;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                _count = value;
            }
        }

        public override byte[] GetValue()
        {
            object[] o = new object[] { (int)Option, AttributeCount, _dirsyncCookie };
            _directoryControlValue = BerConverter.Encode("{iio}", o);
            return base.GetValue();
        }
    }

    public class DirSyncResponseControl : DirectoryControl
    {
        private readonly byte[] _dirsyncCookie;

        internal DirSyncResponseControl(byte[] cookie, bool moreData, int resultSize, bool criticality, byte[] controlValue) : base("1.2.840.113556.1.4.841", controlValue, criticality, true)
        {
            _dirsyncCookie = cookie;
            MoreData = moreData;
            ResultSize = resultSize;
        }

        public byte[] Cookie
        {
            get
            {
                if (_dirsyncCookie == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempCookie = new byte[_dirsyncCookie.Length];
                for (int i = 0; i < tempCookie.Length; i++)
                {
                    tempCookie[i] = _dirsyncCookie[i];
                }

                return tempCookie;
            }
        }

        public bool MoreData { get; }

        public int ResultSize { get; }
    }

    public class PageResultRequestControl : DirectoryControl
    {
        private int _size = 512;
        private byte[] _pageCookie;

        public PageResultRequestControl() : base("1.2.840.113556.1.4.319", null, true, true) { }

        public PageResultRequestControl(int pageSize) : this()
        {
            PageSize = pageSize;
        }

        public PageResultRequestControl(byte[] cookie) : this()
        {
            _pageCookie = cookie;
        }

        public int PageSize
        {
            get => _size;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                _size = value;
            }
        }

        public byte[] Cookie
        {
            get
            {
                if (_pageCookie == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempCookie = new byte[_pageCookie.Length];
                for (int i = 0; i < _pageCookie.Length; i++)
                {
                    tempCookie[i] = _pageCookie[i];
                }

                return tempCookie;
            }
            set => _pageCookie = value;
        }

        public override byte[] GetValue()
        {
            object[] o = new object[] { PageSize, _pageCookie };
            _directoryControlValue = BerConverter.Encode("{io}", o);
            return base.GetValue();
        }
    }

    public class PageResultResponseControl : DirectoryControl
    {
        private readonly byte[] _pageCookie;

        internal PageResultResponseControl(int count, byte[] cookie, bool criticality, byte[] controlValue) : base("1.2.840.113556.1.4.319", controlValue, criticality, true)
        {
            TotalCount = count;
            _pageCookie = cookie;
        }

        public byte[] Cookie
        {
            get
            {
                if (_pageCookie == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempCookie = new byte[_pageCookie.Length];
                for (int i = 0; i < _pageCookie.Length; i++)
                {
                    tempCookie[i] = _pageCookie[i];
                }
                return tempCookie;
            }
        }

        public int TotalCount { get; }
    }

    public class SortRequestControl : DirectoryControl
    {
        private SortKey[] _keys = Array.Empty<SortKey>();
        public SortRequestControl(params SortKey[] sortKeys) : base("1.2.840.113556.1.4.473", null, true, true)
        {
            ArgumentNullException.ThrowIfNull(sortKeys);

            for (int i = 0; i < sortKeys.Length; i++)
            {
                if (sortKeys[i] == null)
                {
                    throw new ArgumentException(SR.NullValueArray, nameof(sortKeys));
                }
            }

            _keys = new SortKey[sortKeys.Length];
            for (int i = 0; i < sortKeys.Length; i++)
            {
                _keys[i] = new SortKey(sortKeys[i].AttributeName, sortKeys[i].MatchingRule, sortKeys[i].ReverseOrder);
            }
        }

        public SortRequestControl(string attributeName, bool reverseOrder) : this(attributeName, null, reverseOrder)
        {
        }

        public SortRequestControl(string attributeName, string matchingRule, bool reverseOrder) : base("1.2.840.113556.1.4.473", null, true, true)
        {
            SortKey key = new SortKey(attributeName, matchingRule, reverseOrder);
            _keys = new SortKey[] { key };
        }

        public SortKey[] SortKeys
        {
            get
            {
                if (_keys == null)
                {
                    return Array.Empty<SortKey>();
                }

                SortKey[] tempKeys = new SortKey[_keys.Length];
                for (int i = 0; i < _keys.Length; i++)
                {
                    tempKeys[i] = new SortKey(_keys[i].AttributeName, _keys[i].MatchingRule, _keys[i].ReverseOrder);
                }
                return tempKeys;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == null)
                    {
                        throw new ArgumentException(SR.NullValueArray, nameof(value));
                    }
                }

                _keys = new SortKey[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    _keys[i] = new SortKey(value[i].AttributeName, value[i].MatchingRule, value[i].ReverseOrder);
                }
            }
        }

        public override byte[] GetValue()
        {
            SortKeyInterop[] nativeSortKeys = new SortKeyInterop[_keys.Length];
            for (int i = 0; i < _keys.Length; ++i)
            {
                nativeSortKeys[i] = new SortKeyInterop(_keys[i]);
            }

            IntPtr control = IntPtr.Zero;
            int structSize = Marshal.SizeOf(typeof(SortKeyInterop));
            int keyCount = nativeSortKeys.Length;
            IntPtr memHandle = Utility.AllocHGlobalIntPtrArray(keyCount + 1);

            try
            {
                IntPtr tempPtr = IntPtr.Zero;
                IntPtr sortPtr = IntPtr.Zero;
                int i = 0;
                for (i = 0; i < keyCount; i++)
                {
                    sortPtr = Marshal.AllocHGlobal(structSize);
                    Marshal.StructureToPtr(nativeSortKeys[i], sortPtr, false);
                    tempPtr = (IntPtr)((long)memHandle + IntPtr.Size * i);
                    Marshal.WriteIntPtr(tempPtr, sortPtr);
                }
                tempPtr = (IntPtr)((long)memHandle + IntPtr.Size * i);
                Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);

                bool critical = IsCritical;
                int error = LdapPal.CreateDirectorySortControl(UtilityHandle.GetHandle(), memHandle, critical ? (byte)1 : (byte)0, ref control);

                if (error != 0)
                {
                    if (LdapErrorMappings.IsLdapError(error))
                    {
                        string errorMessage = LdapErrorMappings.MapResultCode(error);
                        throw new LdapException(error, errorMessage);
                    }
                    else
                    {
                        throw new LdapException(error);
                    }
                }

                LdapControl managedControl = new LdapControl();
                Marshal.PtrToStructure(control, managedControl);
                BerVal value = managedControl.ldctl_value;
                // reinitialize the value
                _directoryControlValue = null;
                if (value != null)
                {
                    _directoryControlValue = new byte[value.bv_len];
                    Marshal.Copy(value.bv_val, _directoryControlValue, 0, value.bv_len);
                }
            }
            finally
            {
                if (control != IntPtr.Zero)
                {
                    LdapPal.FreeDirectoryControl(control);
                }

                if (memHandle != IntPtr.Zero)
                {
                    //release the memory from the heap
                    for (int i = 0; i < keyCount; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(memHandle, IntPtr.Size * i);
                        if (tempPtr != IntPtr.Zero)
                        {
                            // free the marshalled name
                            IntPtr ptr = Marshal.ReadIntPtr(tempPtr);
                            if (ptr != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(ptr);
                            }
                            // free the marshalled rule
                            ptr = Marshal.ReadIntPtr(tempPtr, IntPtr.Size);
                            if (ptr != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(ptr);
                            }

                            Marshal.FreeHGlobal(tempPtr);
                        }
                    }
                    Marshal.FreeHGlobal(memHandle);
                }
            }

            return base.GetValue();
        }
    }

    public class SortResponseControl : DirectoryControl
    {
        internal SortResponseControl(ResultCode result, string attributeName, bool critical, byte[] value) : base("1.2.840.113556.1.4.474", value, critical, true)
        {
            Result = result;
            AttributeName = attributeName;
        }

        public ResultCode Result { get; }

        public string AttributeName { get; }
    }

    public class VlvRequestControl : DirectoryControl
    {
        private int _before;
        private int _after;
        private int _offset;
        private int _estimateCount;
        private byte[] _target;
        private byte[] _context;

        public VlvRequestControl() : base("2.16.840.1.113730.3.4.9", null, true, true) { }

        public VlvRequestControl(int beforeCount, int afterCount, int offset) : this()
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Offset = offset;
        }

        public VlvRequestControl(int beforeCount, int afterCount, string target) : this()
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            if (target != null)
            {
                UTF8Encoding encoder = new UTF8Encoding();
                _target = encoder.GetBytes(target);
            }
        }

        public VlvRequestControl(int beforeCount, int afterCount, byte[] target) : this()
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Target = target;
        }

        public int BeforeCount
        {
            get => _before;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                _before = value;
            }
        }

        public int AfterCount
        {
            get => _after;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                _after = value;
            }
        }

        public int Offset
        {
            get => _offset;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                _offset = value;
            }
        }

        public int EstimateCount
        {
            get => _estimateCount;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.ValidValue, nameof(value));
                }

                _estimateCount = value;
            }
        }

        public byte[] Target
        {
            get
            {
                if (_target == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempContext = new byte[_target.Length];
                for (int i = 0; i < tempContext.Length; i++)
                {
                    tempContext[i] = _target[i];
                }
                return tempContext;
            }
            set => _target = value;
        }

        public byte[] ContextId
        {
            get
            {
                if (_context == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempContext = new byte[_context.Length];
                for (int i = 0; i < tempContext.Length; i++)
                {
                    tempContext[i] = _context[i];
                }
                return tempContext;
            }
            set => _context = value;
        }

        public override byte[] GetValue()
        {
            var seq = new StringBuilder(10);
            var objList = new ArrayList();

            // first encode the before and the after count.
            seq.Append("{ii");
            objList.Add(BeforeCount);
            objList.Add(AfterCount);

            // encode Target if it is not null
            if (Target.Length != 0)
            {
                seq.Append('t');
                objList.Add(0x80 | 0x1);
                seq.Append('o');
                objList.Add(Target);
            }
            else
            {
                seq.Append("t{");
                objList.Add(0xa0);
                seq.Append("ii");
                objList.Add(Offset);
                objList.Add(EstimateCount);
                seq.Append('}');
            }

            // encode the contextID if present
            if (ContextId.Length != 0)
            {
                seq.Append('o');
                objList.Add(ContextId);
            }

            seq.Append('}');
            object[] values = new object[objList.Count];
            for (int i = 0; i < objList.Count; i++)
            {
                values[i] = objList[i];
            }

            _directoryControlValue = BerConverter.Encode(seq.ToString(), values);
            return base.GetValue();
        }
    }

    public class VlvResponseControl : DirectoryControl
    {
        private readonly byte[] _context;

        internal VlvResponseControl(int targetPosition, int count, byte[] context, ResultCode result, bool criticality, byte[] value) : base("2.16.840.1.113730.3.4.10", value, criticality, true)
        {
            TargetPosition = targetPosition;
            ContentCount = count;
            _context = context;
            Result = result;
        }

        public int TargetPosition { get; }

        public int ContentCount { get; }

        public byte[] ContextId
        {
            get
            {
                if (_context == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tempContext = new byte[_context.Length];
                for (int i = 0; i < tempContext.Length; i++)
                {
                    tempContext[i] = _context[i];
                }
                return tempContext;
            }
        }

        public ResultCode Result { get; }
    }

    [SupportedOSPlatform("windows")]
    public partial class QuotaControl : DirectoryControl
    {
        private byte[] _sid;

        public QuotaControl() : base("1.2.840.113556.1.4.1852", null, true, true) { }

        public QuotaControl(SecurityIdentifier querySid) : this()
        {
            QuerySid = querySid;
        }

        public override byte[] GetValue()
        {
            _directoryControlValue = BerConverter.Encode("{o}", new object[] { _sid });
            return base.GetValue();
        }
    }

    public class DirectoryControlCollection : CollectionBase
    {
        public DirectoryControlCollection()
        {
        }

        public DirectoryControl this[int index]
        {
            get => (DirectoryControl)List[index];
            set => List[index] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public int Add(DirectoryControl control)
        {
            ArgumentNullException.ThrowIfNull(control);

            return List.Add(control);
        }

        public void AddRange(DirectoryControl[] controls)
        {
            ArgumentNullException.ThrowIfNull(controls);

            foreach (DirectoryControl control in controls)
            {
                if (control == null)
                {
                    throw new ArgumentException(SR.ContainNullControl, nameof(controls));
                }
            }

            InnerList.AddRange(controls);
        }

        public void AddRange(DirectoryControlCollection controlCollection)
        {
            ArgumentNullException.ThrowIfNull(controlCollection);

            int currentCount = controlCollection.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                Add(controlCollection[i]);
            }
        }

        public bool Contains(DirectoryControl value) => List.Contains(value);

        public void CopyTo(DirectoryControl[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(DirectoryControl value) => List.IndexOf(value);

        public void Insert(int index, DirectoryControl value)
        {
            ArgumentNullException.ThrowIfNull(value);

            List.Insert(index, value);
        }

        public void Remove(DirectoryControl value) => List.Remove(value);

        protected override void OnValidate(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!(value is DirectoryControl))
            {
                throw new ArgumentException(SR.Format(SR.InvalidValueType, nameof(DirectoryControl)), nameof(value));
            }
        }
    }
}
