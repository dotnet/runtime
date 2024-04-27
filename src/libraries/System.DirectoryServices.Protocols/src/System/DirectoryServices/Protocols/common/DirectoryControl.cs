// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;

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
        internal static readonly UTF8Encoding s_utf8Encoding = new(false, true);

        internal byte[] _directoryControlValue;

        public DirectoryControl(string type, byte[] value, bool isCritical, bool serverSide)
        {
            ArgumentNullException.ThrowIfNull(type);

            Type = type;

            if (value != null)
            {
                _directoryControlValue = value.AsSpan().ToArray();
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

            return _directoryControlValue.AsSpan().ToArray();
        }

        public string Type { get; }

        public bool IsCritical { get; set; }

        public bool ServerSide { get; set; }

        internal static void TransformControls(DirectoryControl[] controls)
        {
            Span<byte> scratchSpace = stackalloc byte[256];

            for (int i = 0; i < controls.Length; i++)
            {
                Debug.Assert(controls[i] != null);
                byte[] value = controls[i]._directoryControlValue ?? Array.Empty<byte>();
                Span<byte> asnSpan = value;
                bool asnReadSuccessful;

                if (controls[i].Type == "1.2.840.113556.1.4.319")
                {
                    // The control is a PageControl, as described in RFC 2696.
                    byte[] cookie = Array.Empty<byte>();

                    AsnDecoder.ReadSequence(asnSpan, AsnEncodingRules.BER, out int sequenceContentOffset, out int sequenceContentLength, out _);
                    Debug.Assert(sequenceContentLength > 0);

                    asnReadSuccessful = AsnDecoder.TryReadInt32(asnSpan.Slice(sequenceContentOffset), AsnEncodingRules.BER, out int size, out int bytesConsumed);
                    Debug.Assert(asnReadSuccessful);
                    asnSpan = asnSpan.Slice(sequenceContentOffset + bytesConsumed);

                    asnReadSuccessful = AsnDecoder.TryReadEncodedValue(asnSpan, AsnEncodingRules.BER, out _, out int _, out int cookieLength, out _);
                    if (asnReadSuccessful)
                    {
                        cookie = new byte[cookieLength];

                        // user expects cookie with length 0 as paged search is done.
                        asnReadSuccessful = AsnDecoder.TryReadOctetString(asnSpan, cookie, AsnEncodingRules.BER, out _, out _);
                        Debug.Assert(asnReadSuccessful);
                    }

                    PageResultResponseControl pageControl = new PageResultResponseControl(size, cookie, controls[i].IsCritical, value);
                    controls[i] = pageControl;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.1504")
                {
                    // The control is an AsqControl, as described in MS-ADTS section 3.1.1.3.4.1.18
                    AsnDecoder.ReadSequence(asnSpan, AsnEncodingRules.BER, out int sequenceContentOffset, out int sequenceContentLength, out _);
                    Debug.Assert(sequenceContentLength > 0);

                    ResultCode result = AsnDecoder.ReadEnumeratedValue<ResultCode>(asnSpan.Slice(sequenceContentOffset), AsnEncodingRules.BER, out _);

                    AsqResponseControl asq = new AsqResponseControl(result, controls[i].IsCritical, value);
                    controls[i] = asq;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.841")
                {
                    // The control is a DirSyncControl, as described in MS-ADTS section 3.1.1.3.4.1.3
                    byte[] dirsyncCookie;

                    AsnDecoder.ReadSequence(asnSpan, AsnEncodingRules.BER, out int sequenceContentOffset, out int sequenceContentLength, out _);
                    Debug.Assert(sequenceContentLength > 0);
                    asnSpan = asnSpan.Slice(sequenceContentOffset);

                    asnReadSuccessful = AsnDecoder.TryReadInt32(asnSpan, AsnEncodingRules.BER, out int moreData, out int bytesConsumed);
                    Debug.Assert(asnReadSuccessful);
                    asnSpan = asnSpan.Slice(bytesConsumed);

                    asnReadSuccessful = AsnDecoder.TryReadInt32(asnSpan, AsnEncodingRules.BER, out int count, out bytesConsumed);
                    Debug.Assert(asnReadSuccessful);
                    asnSpan = asnSpan.Slice(bytesConsumed);

                    dirsyncCookie = AsnDecoder.ReadOctetString(asnSpan, AsnEncodingRules.BER, out _);

                    DirSyncResponseControl dirsync = new DirSyncResponseControl(dirsyncCookie, moreData != 0, count, controls[i].IsCritical, value);
                    controls[i] = dirsync;
                }
                else if (controls[i].Type == "1.2.840.113556.1.4.474")
                {
                    // The control is a SortControl, as described in RFC 2891.
                    ResultCode result;
                    string attribute = null;

                    AsnDecoder.ReadSequence(asnSpan, AsnEncodingRules.BER, out int sequenceContentOffset, out int sequenceContentLength, out _);
                    Debug.Assert(sequenceContentLength > 0);
                    asnSpan = asnSpan.Slice(sequenceContentOffset);

                    result = AsnDecoder.ReadEnumeratedValue<ResultCode>(asnSpan, AsnEncodingRules.BER, out int bytesConsumed);
                    asnSpan = asnSpan.Slice(bytesConsumed);

                    // Attribute name is optional: AD for example never returns attribute name
                    asnReadSuccessful = AsnDecoder.TryReadEncodedValue(asnSpan, AsnEncodingRules.BER, out Asn1Tag octetStringTag, out _, out int octetStringLength, out _);
                    if (asnReadSuccessful)
                    {
                        Span<byte> attributeNameBuffer = octetStringLength < scratchSpace.Length ? scratchSpace.Slice(0, octetStringLength) : new byte[octetStringLength];

                        _ = AsnDecoder.TryReadOctetString(asnSpan, attributeNameBuffer, AsnEncodingRules.BER, out _, out _);
                        attribute = s_utf8Encoding.GetString(attributeNameBuffer);
                    }

                    SortResponseControl sort = new SortResponseControl(result, attribute, controls[i].IsCritical, value);
                    controls[i] = sort;
                }
                else if (controls[i].Type == "2.16.840.1.113730.3.4.10")
                {
                    // The control is a VlvResponseControl, as described in MS-ADTS 3.1.1.3.4.1.17
                    ResultCode result;
                    byte[] context = null;

                    AsnDecoder.ReadSequence(asnSpan, AsnEncodingRules.BER, out int sequenceContentOffset, out int sequenceContentLength, out _);
                    Debug.Assert(sequenceContentLength > 0);
                    asnSpan = asnSpan.Slice(sequenceContentOffset);

                    asnReadSuccessful = AsnDecoder.TryReadInt32(asnSpan, AsnEncodingRules.BER, out int position, out int bytesConsumed);
                    Debug.Assert(asnReadSuccessful);
                    asnSpan = asnSpan.Slice(bytesConsumed);

                    asnReadSuccessful = AsnDecoder.TryReadInt32(asnSpan, AsnEncodingRules.BER, out int count, out bytesConsumed);
                    Debug.Assert(asnReadSuccessful);
                    asnSpan = asnSpan.Slice(bytesConsumed);

                    result = AsnDecoder.ReadEnumeratedValue<ResultCode>(asnSpan, AsnEncodingRules.BER, out bytesConsumed);
                    asnSpan = asnSpan.Slice(bytesConsumed);

                    asnReadSuccessful = AsnDecoder.TryReadEncodedValue(asnSpan, AsnEncodingRules.BER, out _, out _, out int octetStringLength, out _);
                    if (asnReadSuccessful)
                    {
                        context = new byte[octetStringLength];
                        _ = AsnDecoder.TryReadOctetString(asnSpan, context, AsnEncodingRules.BER, out _, out _);
                    }

                    VlvResponseControl vlv = new VlvResponseControl(position, count, context, result, controls[i].IsCritical, value);
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
            int sizeEstimate = 4 + (AttributeName?.Length ?? 0);
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.18.
             * ASQRequestValue ::= SEQUENCE {
             *                      sourceAttribute     OCTET STRING }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                if (!string.IsNullOrEmpty(AttributeName))
                {
                    int octetStringLength = s_utf8Encoding.GetByteCount(AttributeName);
                    Span<byte> tmpValue = octetStringLength < 256 ? stackalloc byte[octetStringLength] : new byte[octetStringLength];

                    s_utf8Encoding.GetBytes(AttributeName, tmpValue);
                    writer.WriteOctetString(tmpValue);
                }
                else
                {
                    writer.WriteOctetString([]);
                }
            }
            _directoryControlValue = writer.Encode();
            return base.GetValue();
        }
    }

    public class AsqResponseControl : DirectoryControl
    {
        internal AsqResponseControl(ResultCode result, bool criticality, byte[] controlValue) : base("1.2.840.113556.1.4.1504", controlValue, criticality, true)
        {
            Result = result;
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
            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.2.
             * "When sending this control to the DC, the controlValue field is set to a UTF-8 string
             * containing the fully qualified domain name of a DC in the domain to which the object
             * is to be moved. The string is not BER-encoded."
             */
            if (TargetDomainController != null)
            {
                int byteCount = s_utf8Encoding.GetByteCount(TargetDomainController);

                // Allocate large enough space for the '\0' character.
                _directoryControlValue = new byte[byteCount + 2];
                s_utf8Encoding.GetBytes(TargetDomainController, _directoryControlValue);
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
            int sizeEstimate = 8;
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.5.
             * ExtendedDNRequestValue ::= SEQUENCE {
             *                              Flag     INTEGER }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                writer.WriteInteger((int)Flag);
            }
            _directoryControlValue = writer.Encode();

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
            int sizeEstimate = 8;
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.11.
             * SDFlagsRequestValue ::= SEQUENCE {
             *                          Flags     INTEGER }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                writer.WriteInteger((int)SecurityMasks);
            }
            _directoryControlValue = writer.Encode();

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
            int sizeEstimate = 8;
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.12.
             * SearchOptionsRequestValue ::= SEQUENCE {
             *                                  Flags     INTEGER }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                writer.WriteInteger((int)SearchOption);
            }
            _directoryControlValue = writer.Encode();

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
            int sizeEstimate = 10 + 2 * (ServerName?.Length ?? 0);
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.16.
             * VerifyNameRequestValue ::= SEQUENCE {
             *                              Flags       INTEGER,
             *                              ServerName  OCTET STRING }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                writer.WriteInteger(Flag);

                if (!string.IsNullOrEmpty(ServerName))
                {
                    int serverNameLength = Encoding.Unicode.GetByteCount(ServerName);
                    Span<byte> tmpValue = serverNameLength < 256 ? stackalloc byte[serverNameLength] : new byte[serverNameLength];

                    Encoding.Unicode.GetBytes(ServerName, tmpValue);
                    writer.WriteOctetString(tmpValue);
                }
                else
                {
                    writer.WriteOctetString([]);
                }
            }
            _directoryControlValue = writer.Encode();

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

                return _dirsyncCookie.AsSpan().ToArray();
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
            int sizeEstimate = 16 + (_dirsyncCookie?.Length ?? 0);
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.3.
             * DirSyncRequestValue ::= SEQUENCE {
             *                          Flags       INTEGER,
             *                          MaxBytes    INTEGER,
             *                          Cookie  OCTET STRING }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                writer.WriteInteger((int)Option);
                writer.WriteInteger(AttributeCount);
                writer.WriteOctetString(_dirsyncCookie ?? []);
            }
            _directoryControlValue = writer.Encode();

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

                return _dirsyncCookie.AsSpan().ToArray();
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

                return _pageCookie.AsSpan().ToArray();
            }
            set => _pageCookie = value;
        }

        public override byte[] GetValue()
        {
            int sizeEstimate = 6 + (_pageCookie?.Length ?? 1);
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in RFC2696.
             * realSearchControlValue ::= SEQUENCE {
             *                              size    INTEGER,
             *                              cookie  OCTET STRING }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                writer.WriteInteger(PageSize);
                writer.WriteOctetString(_pageCookie);
            }
            _directoryControlValue = writer.Encode();

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

                return _pageCookie.AsSpan().ToArray();
            }
        }

        public int TotalCount { get; }
    }

    public class SortRequestControl : DirectoryControl
    {
        private static readonly Asn1Tag s_orderingRuleTag = new(TagClass.ContextSpecific, 0, false);
        private static readonly Asn1Tag s_reverseOrderTag = new(TagClass.ContextSpecific, 1, false);

        private int _keysAsnLength;
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

            _keysAsnLength = 0;
            _keys = new SortKey[sortKeys.Length];
            for (int i = 0; i < sortKeys.Length; i++)
            {
                _keys[i] = new SortKey(sortKeys[i].AttributeName, sortKeys[i].MatchingRule, sortKeys[i].ReverseOrder);
                _keysAsnLength += 13 + (sortKeys[i].AttributeName?.Length ?? 0) + (sortKeys[i].MatchingRule?.Length ?? 0);
            }
        }

        public SortRequestControl(string attributeName, bool reverseOrder) : this(attributeName, null, reverseOrder)
        {
        }

        public SortRequestControl(string attributeName, string matchingRule, bool reverseOrder) : base("1.2.840.113556.1.4.473", null, true, true)
        {
            SortKey key = new SortKey(attributeName, matchingRule, reverseOrder);
            _keys = new SortKey[] { key };
            _keysAsnLength = 13 + (attributeName?.Length ?? 0) + (matchingRule?.Length ?? 0);
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

                _keysAsnLength = 0;
                _keys = new SortKey[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    _keys[i] = new SortKey(value[i].AttributeName, value[i].MatchingRule, value[i].ReverseOrder);
                    _keysAsnLength += 13 + (value[i].AttributeName?.Length ?? 0) + (value[i].MatchingRule?.Length ?? 0);
                }
            }
        }

        public override byte[] GetValue()
        {
            int sizeEstimate = 12 + _keysAsnLength;
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in RFC2891.
             * SortKeyList ::= SEQUENCE OF SEQUENCE {
             *                  attributeType   AttributeDescription,
             *                  orderingRule    [0] MatchingRuleId OPTIONAL,
             *                  reverseOrder    [1] BOOLEAN DEFAULT FALSE }
             * */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                Span<byte> scratchSpace = stackalloc byte[256];

                for (int i = 0; i < _keys.Length; i++)
                {
                    SortKey key = _keys[i];

                    using (AsnWriter.Scope keySequence = writer.PushSequence())
                    {
                        if (!string.IsNullOrEmpty(key.AttributeName))
                        {
                            int octetStringLength = s_utf8Encoding.GetByteCount(key.AttributeName);
                            Span<byte> tmpValue = octetStringLength < scratchSpace.Length ? scratchSpace.Slice(0, octetStringLength) : new byte[octetStringLength];

                            s_utf8Encoding.GetBytes(key.AttributeName, tmpValue);
                            writer.WriteOctetString(tmpValue);
                        }
                        else
                        {
                            writer.WriteOctetString([]);
                        }

                        if (!string.IsNullOrEmpty(key.MatchingRule))
                        {
                            int octetStringLength = s_utf8Encoding.GetByteCount(key.MatchingRule);
                            Span<byte> tmpValue = octetStringLength < scratchSpace.Length ? scratchSpace.Slice(0, octetStringLength) : new byte[octetStringLength];

                            s_utf8Encoding.GetBytes(key.MatchingRule, tmpValue);
                            writer.WriteOctetString(tmpValue, s_orderingRuleTag);
                        }

                        if (key.ReverseOrder)
                        {
                            writer.WriteBoolean(key.ReverseOrder, s_reverseOrderTag);
                        }
                    }
                }
            }
            _directoryControlValue = writer.Encode();

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
        private static readonly Asn1Tag s_byOffsetChoiceTag = new(TagClass.ContextSpecific, 0, true);
        private static readonly Asn1Tag s_greaterThanOrEqualChoiceTag = new(TagClass.ContextSpecific, 1, false);

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
                _target = s_utf8Encoding.GetBytes(target);
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

                return _target.AsSpan().ToArray();
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

                return _context.AsSpan().ToArray();
            }
            set => _context = value;
        }

        public override byte[] GetValue()
        {
            int sizeEstimate = 16 + (_target?.Length ?? 12) + (_context?.Length ?? 1);
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.17.
             * VLVRequestValue ::= SEQUENCE {
             *                      beforeCount     INTEGER,
             *                      afterCount      INTEGER,
             *                      CHOICE {
             *                          byoffset    [0] SEQUENCE {
             *                              offset          INTEGER,
             *                              contentCount    INTEGER },
             *                          greaterThanOrEqual  [1] AssertionValue },
             *                      contextID       OCTET STRING OPTIONAL }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                // first encode the before and the after count.
                writer.WriteInteger(BeforeCount);
                writer.WriteInteger(AfterCount);

                // encode Target if it is not null
                if (_target != null && _target.Length > 0)
                {
                    writer.WriteOctetString(_target, s_greaterThanOrEqualChoiceTag);
                }
                else
                {
                    using (AsnWriter.Scope innerSequence = writer.PushSequence(s_byOffsetChoiceTag))
                    {
                        writer.WriteInteger(Offset);
                        writer.WriteInteger(EstimateCount);
                    }
                }

                // encode the contextID if present
                if (_context != null && _context.Length > 0)
                {
                    writer.WriteOctetString(_context);
                }

            }
            _directoryControlValue = writer.Encode();

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

                return _context.AsSpan().ToArray();
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
            int sizeEstimate = 4 + (_sid?.Length ?? 0);
            AsnWriter writer = new(AsnEncodingRules.BER, sizeEstimate);

            /* This is as laid out in MS-ADTS, 3.1.1.3.4.1.19.
             * QuotaRequestValue ::= SEQUENCE {
             *                          querySID OCTET STRING }
             */
            using (AsnWriter.Scope outerSequence = writer.PushSequence())
            {
                writer.WriteOctetString(_sid);
            }
            _directoryControlValue = writer.Encode();

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
