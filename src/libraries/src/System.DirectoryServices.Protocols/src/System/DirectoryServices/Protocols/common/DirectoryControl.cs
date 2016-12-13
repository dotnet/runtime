// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Xml;
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

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

    internal class UtilityHandle
    {
        private static ConnectionHandle s_handle = new ConnectionHandle();

        public static ConnectionHandle GetHandle()
        {
            return s_handle;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class SortKey
    {
        private string _name = null;
        private string _rule = null;
        private bool _order = false;

        public SortKey()
        {
            Utility.CheckOSVersion();
        }

        public SortKey(string attributeName, string matchingRule, bool reverseOrder)
        {
            Utility.CheckOSVersion();

            AttributeName = attributeName;
            _rule = matchingRule;
            _order = reverseOrder;
        }

        public string AttributeName
        {
            get
            {
                return _name;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _name = value;
            }
        }

        public string MatchingRule
        {
            get
            {
                return _rule;
            }
            set
            {
                _rule = value;
            }
        }

        public bool ReverseOrder
        {
            get
            {
                return _order;
            }

            set
            {
                _order = value;
            }
        }
    }

    public class DirectoryControl
    {
        // control value
        internal byte[] directoryControlValue = null;
        // oid of the control
        private string _directoryControlType = "";
        // criticality of the control
        private bool _directoryControlCriticality = true;
        // whether it is a server side control or not
        private bool _directoryControlServerSide = true;

        public DirectoryControl(string type, byte[] value, bool isCritical, bool serverSide)
        {
            Utility.CheckOSVersion();

            _directoryControlType = type;
            if (type == null)
                throw new ArgumentNullException("type");

            if (value != null)
            {
                directoryControlValue = new byte[value.Length];
                for (int i = 0; i < value.Length; i++)
                    directoryControlValue[i] = value[i];
            }
            _directoryControlCriticality = isCritical;
            _directoryControlServerSide = serverSide;
        }

        internal DirectoryControl(XmlElement el)
        {
            XmlNamespaceManager ns = NamespaceUtils.GetDsmlNamespaceManager();

            //
            // Populate the control's criticality
            //
            XmlAttribute attrCriticality = (XmlAttribute)el.SelectSingleNode("@dsml:criticality", ns);

            if (attrCriticality == null)
            {
                // try it without the namespace qualifier, in case the sender omitted it
                attrCriticality = (XmlAttribute)el.SelectSingleNode("@criticality", ns);
            }

            if (attrCriticality == null)
            {
                // DSML v2 defaults criticality to false if not present in the XML
                _directoryControlCriticality = false;
            }
            else
            {
                string s = attrCriticality.Value;

                if ((s == "true") ||
                    (s == "1"))
                {
                    _directoryControlCriticality = true;
                }
                else if ((s == "false") ||
                    (s == "0"))
                {
                    _directoryControlCriticality = false;
                }
                else
                {
                    Debug.WriteLine("Processing response, the control has wrong value of crticality specified");
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.BadControl));
                }
            }

            //
            // Populate the control's name (Type)
            //
            XmlAttribute attrType = (XmlAttribute)el.SelectSingleNode("@dsml:type", ns);

            if (attrType == null)
            {
                // try it without the namespace qualifier, in case the sender omitted it
                attrType = (XmlAttribute)el.SelectSingleNode("@type", ns);
            }

            if (attrType == null)
            {
                // DSML v2 requires the control to specify a type
                Debug.WriteLine("Processing response, the control does not have oid defined");
                throw new DsmlInvalidDocumentException(Res.GetString(Res.BadControl));
            }
            else
            {
                _directoryControlType = attrType.Value;
            }

            //
            // Populate the control's value
            //
            XmlElement elemValue = (XmlElement)el.SelectSingleNode("dsml:controlValue", ns);

            if (elemValue != null)
            {
                try
                {
                    this.directoryControlValue = System.Convert.FromBase64String(elemValue.InnerText);
                }
                catch (FormatException)
                {
                    Debug.WriteLine("Processing response, converting control value failed");
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.BadControl));
                }
            }
        }

        public virtual byte[] GetValue()
        {
            if (directoryControlValue == null)
                return new byte[0];
            else
            {
                byte[] tempValue = new byte[directoryControlValue.Length];
                for (int i = 0; i < directoryControlValue.Length; i++)
                    tempValue[i] = directoryControlValue[i];
                return tempValue;
            }
        }

        public string Type
        {
            get
            {
                return _directoryControlType;
            }
        }

        public bool IsCritical
        {
            get
            {
                return _directoryControlCriticality;
            }
            set
            {
                _directoryControlCriticality = value;
            }
        }

        public bool ServerSide
        {
            get
            {
                return _directoryControlServerSide;
            }
            set
            {
                _directoryControlServerSide = value;
            }
        }

        // Returns an XmlElement representing this control,
        // in DSML v2 format.
        internal XmlElement ToXmlNode(XmlDocument doc)
        {
            // create the <control> element
            XmlElement elem = doc.CreateElement("control", DsmlConstants.DsmlUri);

            // attach the "type" and "criticality" attributes to the <control> element
            XmlAttribute attrType = doc.CreateAttribute("type", null);
            attrType.InnerText = Type;
            elem.Attributes.Append(attrType);

            XmlAttribute attrCrit = doc.CreateAttribute("criticality", null);
            attrCrit.InnerText = IsCritical ? "true" : "false";
            elem.Attributes.Append(attrCrit);

            byte[] controlValue = GetValue();
            if (controlValue.Length != 0)
            {
                // create the <controlValue> element
                XmlElement elemValue = doc.CreateElement("controlValue", DsmlConstants.DsmlUri);

                // append the "xsi:type=xsd:base64Binary" attribute to <controlValue>
                XmlAttribute attrXsiType = doc.CreateAttribute("xsi:type", DsmlConstants.XsiUri);
                attrXsiType.InnerText = "xsd:base64Binary";
                elemValue.Attributes.Append(attrXsiType);

                // add in the base-64 encoded value itself
                string base64value = System.Convert.ToBase64String(controlValue);
                elemValue.InnerText = base64value;

                // attach the <controlValue> element to the <control> element
                elem.AppendChild(elemValue);
            }

            return elem;
        }

        internal static void TransformControls(DirectoryControl[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
            {
                Debug.Assert(controls[i] != null);
                byte[] value = controls[i].GetValue();
                // if it is a page control
                if (controls[i].Type == "1.2.840.113556.1.4.319")
                {
                    object[] result = BerConverter.Decode("{iO}", value);
                    Debug.Assert((result != null) && (result.Length == 2));

                    int size = (int)result[0];
                    byte[] cookie = (byte[])result[1];
                    // user expects cookie with length 0 as paged search is done.
                    if (cookie == null)
                        cookie = new byte[0];

                    PageResultResponseControl pageControl = new PageResultResponseControl(size, cookie, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = pageControl;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.1504")
                {
                    // asq control
                    object[] o = null;
                    if (Utility.IsWin2kOS)
                        o = BerConverter.Decode("{i}", value);
                    else
                        o = BerConverter.Decode("{e}", value);
                    Debug.Assert((o != null) && (o.Length == 1));

                    int result = (int)o[0];
                    AsqResponseControl asq = new AsqResponseControl(result, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = asq;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.841")
                {
                    //dirsync control
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
                    object[] o = null;
                    int result = 0;
                    string attribute = null;
                    bool decodeSucceeded;
                    //sort control

                    if (Utility.IsWin2kOS)
                        // win2k berencoding does not understand enumeration
                        o = BerConverter.TryDecode("{ia}", value, out decodeSucceeded);
                    else
                        o = BerConverter.TryDecode("{ea}", value, out decodeSucceeded);

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
                        if (Utility.IsWin2kOS)
                            // win2k berencoding does not understand enumeration
                            o = BerConverter.Decode("{i}", value);
                        else
                            o = BerConverter.Decode("{e}", value);
                        Debug.Assert(o != null && o.Length == 1);

                        result = (int)o[0];
                    }

                    SortResponseControl sort = new SortResponseControl((ResultCode)result, attribute, controls[i].IsCritical, controls[i].GetValue());
                    controls[i] = sort;
                }
                else if (controls[i].Type == "2.16.840.1.113730.3.4.10")
                {
                    int position;
                    int count;
                    int result;
                    byte[] context = null;
                    object[] o = null;
                    bool decodeSucceeded = false;

                    if (Utility.IsWin2kOS)
                        // win2k berencoding does not understand enumeration
                        o = BerConverter.TryDecode("{iiiO}", value, out decodeSucceeded);
                    else
                        o = BerConverter.TryDecode("{iieO}", value, out decodeSucceeded);

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
                        if (Utility.IsWin2kOS)
                            // win2k berencoding does not understand enumeration
                            o = BerConverter.Decode("{iii}", value);
                        else
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
        private string _name;
        public AsqRequestControl() : base("1.2.840.113556.1.4.1504", null, true, true)
        {
        }

        public AsqRequestControl(string attributeName) : this()
        {
            _name = attributeName;
        }

        public string AttributeName
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public override byte[] GetValue()
        {
            this.directoryControlValue = BerConverter.Encode("{s}", new object[] { _name });
            return base.GetValue();
        }
    }

    public class AsqResponseControl : DirectoryControl
    {
        private ResultCode _result;

        internal AsqResponseControl(int result, bool criticality, byte[] controlValue) : base("1.2.840.113556.1.4.1504", controlValue, criticality, true)
        {
            _result = (ResultCode)result;
        }

        public ResultCode Result
        {
            get
            {
                return _result;
            }
        }
    }

    public class CrossDomainMoveControl : DirectoryControl
    {
        private string _dcName = null;
        public CrossDomainMoveControl() : base("1.2.840.113556.1.4.521", null, true, true)
        {
        }

        public CrossDomainMoveControl(string targetDomainController) : this()
        {
            _dcName = targetDomainController;
        }

        public string TargetDomainController
        {
            get
            {
                return _dcName;
            }
            set
            {
                _dcName = value;
            }
        }

        public override byte[] GetValue()
        {
            if (_dcName != null)
            {
                UTF8Encoding encoder = new UTF8Encoding();
                byte[] bytes = encoder.GetBytes(_dcName);

                // allocate large enough space for the '\0' character
                this.directoryControlValue = new byte[bytes.Length + 2];
                for (int i = 0; i < bytes.Length; i++)
                    this.directoryControlValue[i] = bytes[i];
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
        private ExtendedDNFlag _format = ExtendedDNFlag.HexString;

        public ExtendedDNControl() : base("1.2.840.113556.1.4.529", null, true, true)
        {
        }

        public ExtendedDNControl(ExtendedDNFlag flag) : this()
        {
            Flag = flag;
        }

        public ExtendedDNFlag Flag
        {
            get
            {
                return _format;
            }
            set
            {
                if (value < ExtendedDNFlag.HexString || value > ExtendedDNFlag.StandardString)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ExtendedDNFlag));

                _format = value;
            }
        }
        public override byte[] GetValue()
        {
            this.directoryControlValue = BerConverter.Encode("{i}", new object[] { (int)_format });
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
        private SecurityMasks _flag = SecurityMasks.None;

        public SecurityDescriptorFlagControl() : base("1.2.840.113556.1.4.801", null, true, true) { }

        public SecurityDescriptorFlagControl(SecurityMasks masks) : this()
        {
            SecurityMasks = masks;
        }

        public SecurityMasks SecurityMasks
        {
            get
            {
                return _flag;
            }
            set
            {
                // we don't do validation to the dirsync flag here as underneath API does not check for it and we don't want to put
                // unnecessary limitation on it.

                _flag = value;
            }
        }

        public override byte[] GetValue()
        {
            this.directoryControlValue = BerConverter.Encode("{i}", new object[] { (int)_flag });
            return base.GetValue();
        }
    }

    public class SearchOptionsControl : DirectoryControl
    {
        private SearchOption _flag = SearchOption.DomainScope;
        public SearchOptionsControl() : base("1.2.840.113556.1.4.1340", null, true, true) { }

        public SearchOptionsControl(SearchOption flags) : this()
        {
            SearchOption = flags;
        }

        public SearchOption SearchOption
        {
            get
            {
                return _flag;
            }
            set
            {
                if (value < SearchOption.DomainScope || value > SearchOption.PhantomRoot)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SearchOption));

                _flag = value;
            }
        }

        public override byte[] GetValue()
        {
            this.directoryControlValue = BerConverter.Encode("{i}", new object[] { (int)_flag });
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
        private string _name = null;
        private int _flag = 0;

        public VerifyNameControl() : base("1.2.840.113556.1.4.1338", null, true, true) { }

        public VerifyNameControl(string serverName) : this()
        {
            if (serverName == null)
                throw new ArgumentNullException("serverName");

            _name = serverName;
        }

        public VerifyNameControl(string serverName, int flag) : this(serverName)
        {
            _flag = flag;
        }

        public string ServerName
        {
            get
            {
                return _name;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _name = value;
            }
        }

        public int Flag
        {
            get
            {
                return _flag;
            }
            set
            {
                _flag = value;
            }
        }

        public override byte[] GetValue()
        {
            byte[] tmpValue = null;
            if (ServerName != null)
            {
                UnicodeEncoding unicode = new UnicodeEncoding();
                tmpValue = unicode.GetBytes(ServerName);
            }
            this.directoryControlValue = BerConverter.Encode("{io}", new object[] { _flag, tmpValue });
            return base.GetValue();
        }
    }

    public class DirSyncRequestControl : DirectoryControl
    {
        private byte[] _dirsyncCookie = null;
        private DirectorySynchronizationOptions _flag = DirectorySynchronizationOptions.None;
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
                    return new byte[0];
                else
                {
                    byte[] tempCookie = new byte[_dirsyncCookie.Length];
                    for (int i = 0; i < tempCookie.Length; i++)
                        tempCookie[i] = _dirsyncCookie[i];

                    return tempCookie;
                }
            }
            set
            {
                _dirsyncCookie = value;
            }
        }

        public DirectorySynchronizationOptions Option
        {
            get
            {
                return _flag;
            }
            set
            {
                // we don't do validation to the dirsync flag here as underneath API does not check for it and we don't want to put
                // unnecessary limitation on it.

                _flag = value;
            }
        }

        public int AttributeCount
        {
            get
            {
                return _count;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

                _count = value;
            }
        }

        public override byte[] GetValue()
        {
            object[] o = new object[] { (int)_flag, _count, _dirsyncCookie };
            this.directoryControlValue = BerConverter.Encode("{iio}", o);
            return base.GetValue();
        }
    }

    public class DirSyncResponseControl : DirectoryControl
    {
        private byte[] _dirsyncCookie = null;
        private bool _moreResult = false;
        private int _size = 0;

        internal DirSyncResponseControl(byte[] cookie, bool moreData, int resultSize, bool criticality, byte[] controlValue) : base("1.2.840.113556.1.4.841", controlValue, criticality, true)
        {
            _dirsyncCookie = cookie;
            _moreResult = moreData;
            _size = resultSize;
        }

        public byte[] Cookie
        {
            get
            {
                if (_dirsyncCookie == null)
                    return new byte[0];
                else
                {
                    byte[] tempCookie = new byte[_dirsyncCookie.Length];
                    for (int i = 0; i < tempCookie.Length; i++)
                        tempCookie[i] = _dirsyncCookie[i];

                    return tempCookie;
                }
            }
        }

        public bool MoreData
        {
            get
            {
                return _moreResult;
            }
        }

        public int ResultSize
        {
            get
            {
                return _size;
            }
        }
    }

    public class PageResultRequestControl : DirectoryControl
    {
        private int _size = 512;
        private byte[] _pageCookie = null;

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
            get
            {
                return _size;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

                _size = value;
            }
        }

        public byte[] Cookie
        {
            get
            {
                if (_pageCookie == null)
                    return new byte[0];

                byte[] tempCookie = new byte[_pageCookie.Length];
                for (int i = 0; i < _pageCookie.Length; i++)
                    tempCookie[i] = _pageCookie[i];

                return tempCookie;
            }
            set
            {
                _pageCookie = value;
            }
        }

        public override byte[] GetValue()
        {
            object[] o = new object[] { _size, _pageCookie };
            this.directoryControlValue = BerConverter.Encode("{io}", o);
            return base.GetValue();
        }
    }

    public class PageResultResponseControl : DirectoryControl
    {
        private byte[] _pageCookie = null;
        private int _count = 0;
        internal PageResultResponseControl(int count, byte[] cookie, bool criticality, byte[] controlValue) : base("1.2.840.113556.1.4.319", controlValue, criticality, true)
        {
            _count = count;
            _pageCookie = cookie;
        }

        public byte[] Cookie
        {
            get
            {
                if (_pageCookie == null)
                    return new byte[0];
                else
                {
                    byte[] tempCookie = new byte[_pageCookie.Length];
                    for (int i = 0; i < _pageCookie.Length; i++)
                    {
                        tempCookie[i] = _pageCookie[i];
                    }
                    return tempCookie;
                }
            }
        }

        public int TotalCount
        {
            get
            {
                return _count;
            }
        }
    }

    public class SortRequestControl : DirectoryControl
    {
        private SortKey[] _keys = new SortKey[0];
        public SortRequestControl(params SortKey[] sortKeys) : base("1.2.840.113556.1.4.473", null, true, true)
        {
            if (sortKeys == null)
                throw new ArgumentNullException("sortKeys");

            for (int i = 0; i < sortKeys.Length; i++)
            {
                if (sortKeys[i] == null)
                    throw new ArgumentException(Res.GetString(Res.NullValueArray), "sortKeys");
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
            _keys = new SortKey[1];
            _keys[0] = key;
        }

        public SortKey[] SortKeys
        {
            get
            {
                if (_keys == null)
                    return new SortKey[0];
                else
                {
                    SortKey[] tempKeys = new SortKey[_keys.Length];
                    for (int i = 0; i < _keys.Length; i++)
                    {
                        tempKeys[i] = new SortKey(_keys[i].AttributeName, _keys[i].MatchingRule, _keys[i].ReverseOrder);
                    }

                    return tempKeys;
                }
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == null)
                        throw new ArgumentException(Res.GetString(Res.NullValueArray), "value");
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
            IntPtr control = (IntPtr)0;
            int structSize = Marshal.SizeOf(typeof(SortKey));
            int keyCount = _keys.Length;
            IntPtr memHandle = Utility.AllocHGlobalIntPtrArray(keyCount + 1);

            try
            {
                IntPtr tempPtr = (IntPtr)0;
                IntPtr sortPtr = (IntPtr)0;
                int i = 0;
                for (i = 0; i < keyCount; i++)
                {
                    sortPtr = Marshal.AllocHGlobal(structSize);
                    Marshal.StructureToPtr(_keys[i], sortPtr, false);
                    tempPtr = (IntPtr)((long)memHandle + Marshal.SizeOf(typeof(IntPtr)) * i);
                    Marshal.WriteIntPtr(tempPtr, sortPtr);
                }
                tempPtr = (IntPtr)((long)memHandle + Marshal.SizeOf(typeof(IntPtr)) * i);
                Marshal.WriteIntPtr(tempPtr, (IntPtr)0);

                bool critical = IsCritical;
                int error = Wldap32.ldap_create_sort_control(UtilityHandle.GetHandle(), memHandle, critical ? (byte)1 : (byte)0, ref control);

                if (error != 0)
                {
                    if (Utility.IsLdapError((LdapError)error))
                    {
                        string errorMessage = LdapErrorMappings.MapResultCode(error);
                        throw new LdapException(error, errorMessage);
                    }
                    else
                        throw new LdapException(error);
                }

                LdapControl managedControl = new LdapControl();
                Marshal.PtrToStructure(control, managedControl);
                berval value = managedControl.ldctl_value;
                // reinitialize the value
                directoryControlValue = null;
                if (value != null)
                {
                    directoryControlValue = new byte[value.bv_len];
                    Marshal.Copy(value.bv_val, directoryControlValue, 0, value.bv_len);
                }
            }
            finally
            {
                if (control != (IntPtr)0)
                    Wldap32.ldap_control_free(control);

                if (memHandle != (IntPtr)0)
                {
                    //release the memory from the heap
                    for (int i = 0; i < keyCount; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(memHandle, Marshal.SizeOf(typeof(IntPtr)) * i);
                        if (tempPtr != (IntPtr)0)
                        {
                            // free the marshalled name
                            IntPtr ptr = Marshal.ReadIntPtr(tempPtr);
                            if (ptr != (IntPtr)0)
                                Marshal.FreeHGlobal(ptr);
                            // free the marshalled rule
                            ptr = Marshal.ReadIntPtr(tempPtr, Marshal.SizeOf(typeof(IntPtr)));
                            if (ptr != (IntPtr)0)
                                Marshal.FreeHGlobal(ptr);

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
        private ResultCode _result;
        private string _name;

        internal SortResponseControl(ResultCode result, string attributeName, bool critical, byte[] value) : base("1.2.840.113556.1.4.474", value, critical, true)
        {
            _result = result;
            _name = attributeName;
        }

        public ResultCode Result
        {
            get
            {
                return _result;
            }
        }

        public string AttributeName
        {
            get
            {
                return _name;
            }
        }
    }

    public class VlvRequestControl : DirectoryControl
    {
        private int _before = 0;
        private int _after = 0;
        private int _offset = 0;
        private int _estimateCount = 0;
        private byte[] _target = null;
        private byte[] _context = null;

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
                byte[] bytes = encoder.GetBytes(target);
                _target = bytes;
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
            get
            {
                return _before;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

                _before = value;
            }
        }

        public int AfterCount
        {
            get
            {
                return _after;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

                _after = value;
            }
        }

        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

                _offset = value;
            }
        }

        public int EstimateCount
        {
            get
            {
                return _estimateCount;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.ValidValue), "value");

                _estimateCount = value;
            }
        }

        public byte[] Target
        {
            get
            {
                if (_target == null)
                    return new byte[0];
                else
                {
                    byte[] tempContext = new byte[_target.Length];
                    for (int i = 0; i < tempContext.Length; i++)
                    {
                        tempContext[i] = _target[i];
                    }

                    return tempContext;
                }
            }
            set
            {
                _target = value;
            }
        }

        public byte[] ContextId
        {
            get
            {
                if (_context == null)
                    return new byte[0];
                else
                {
                    byte[] tempContext = new byte[_context.Length];
                    for (int i = 0; i < tempContext.Length; i++)
                    {
                        tempContext[i] = _context[i];
                    }

                    return tempContext;
                }
            }
            set
            {
                _context = value;
            }
        }

        public override byte[] GetValue()
        {
            StringBuilder seq = new StringBuilder(10);
            ArrayList objList = new ArrayList();

            // first encode the before and the after count.
            seq.Append("{ii");
            objList.Add(BeforeCount);
            objList.Add(AfterCount);

            // encode Target if it is not null
            if (Target.Length != 0)
            {
                seq.Append("t");
                objList.Add(0x80 | 0x1);
                seq.Append("o");
                objList.Add(Target);
            }
            else
            {
                seq.Append("t{");
                objList.Add(0xa0);
                seq.Append("ii");
                objList.Add(Offset);
                objList.Add(EstimateCount);
                seq.Append("}");
            }

            // encode the contextID if present
            if (ContextId.Length != 0)
            {
                seq.Append("o");
                objList.Add(ContextId);
            }

            seq.Append("}");
            object[] values = new object[objList.Count];
            for (int i = 0; i < objList.Count; i++)
            {
                values[i] = objList[i];
            }

            directoryControlValue = BerConverter.Encode(seq.ToString(), values);
            return base.GetValue();
        }
    }

    public class VlvResponseControl : DirectoryControl
    {
        private int _position = 0;
        private int _count = 0;
        private byte[] _context = null;
        private ResultCode _result;

        internal VlvResponseControl(int targetPosition, int count, byte[] context, ResultCode result, bool criticality, byte[] value) : base("2.16.840.1.113730.3.4.10", value, criticality, true)
        {
            _position = targetPosition;
            _count = count;
            _context = context;
            _result = result;
        }

        public int TargetPosition
        {
            get
            {
                return _position;
            }
        }

        public int ContentCount
        {
            get
            {
                return _count;
            }
        }

        public byte[] ContextId
        {
            get
            {
                if (_context == null)
                    return new byte[0];
                else
                {
                    byte[] tempContext = new byte[_context.Length];
                    for (int i = 0; i < tempContext.Length; i++)
                    {
                        tempContext[i] = _context[i];
                    }

                    return tempContext;
                }
            }
        }

        public ResultCode Result
        {
            get
            {
                return _result;
            }
        }
    }

    public class QuotaControl : DirectoryControl
    {
        private byte[] _sid = null;

        public QuotaControl() : base("1.2.840.113556.1.4.1852", null, true, true) { }

        public QuotaControl(SecurityIdentifier querySid) : this()
        {
            QuerySid = querySid;
        }

        public SecurityIdentifier QuerySid
        {
            get
            {
                if (_sid == null)
                    return null;
                else
                {
                    return new SecurityIdentifier(_sid, 0);
                }
            }
            set
            {
                if (value == null)
                    _sid = null;
                else
                {
                    _sid = new byte[value.BinaryLength];
                    value.GetBinaryForm(_sid, 0);
                }
            }
        }

        public override byte[] GetValue()
        {
            this.directoryControlValue = BerConverter.Encode("{o}", new object[] { _sid });
            return base.GetValue();
        }
    }

    public class DirectoryControlCollection : CollectionBase
    {
        public DirectoryControlCollection()
        {
            Utility.CheckOSVersion();
        }

        public DirectoryControl this[int index]
        {
            get
            {
                return (DirectoryControl)List[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                List[index] = value;
            }
        }

        public int Add(DirectoryControl control)
        {
            if (control == null)
                throw new ArgumentNullException("control");

            return List.Add(control);
        }

        public void AddRange(DirectoryControl[] controls)
        {
            if (controls == null)
                throw new ArgumentNullException("controls");

            foreach (DirectoryControl c in controls)
            {
                if (c == null)
                {
                    throw new ArgumentException(Res.GetString(Res.ContainNullControl), "controls");
                }
            }

            InnerList.AddRange(controls);
        }

        public void AddRange(DirectoryControlCollection controlCollection)
        {
            if (controlCollection == null)
            {
                throw new ArgumentNullException("controlCollection");
            }
            int currentCount = controlCollection.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                this.Add(controlCollection[i]);
            }
        }

        public bool Contains(DirectoryControl value)
        {
            return List.Contains(value);
        }

        public void CopyTo(DirectoryControl[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(DirectoryControl value)
        {
            return List.IndexOf(value);
        }

        public void Insert(int index, DirectoryControl value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            List.Insert(index, value);
        }

        public void Remove(DirectoryControl value)
        {
            List.Remove(value);
        }

        protected override void OnValidate(Object value)
        {
            if (value == null) throw new ArgumentNullException("value");

            if (!(value is DirectoryControl))
                throw new ArgumentException(Res.GetString(Res.InvalidValueType, "DirectoryControl"), "value");
        }
    }
}
