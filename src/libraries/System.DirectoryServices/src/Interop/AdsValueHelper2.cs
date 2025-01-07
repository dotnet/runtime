// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemTime
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DnWithBinary
    {
        public int dwLength;
        public IntPtr lpBinaryValue;       // GUID of directory object
        public IntPtr pszDNString;         // Distinguished Name
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DnWithString
    {
        public IntPtr pszStringValue;      // associated value
        public IntPtr pszDNString;         // Distinguished Name
    }

    /// <summary>
    /// Helper class for dealing with struct AdsValue.
    /// </summary>
    internal sealed class AdsValueHelper
    {
        public AdsValue adsvalue;
        private GCHandle _pinnedHandle;

        public AdsValueHelper(AdsValue adsvalue)
        {
            this.adsvalue = adsvalue;
        }

        public AdsValueHelper(object managedValue)
        {
            AdsType adsType = GetAdsTypeForManagedType(managedValue.GetType());
            SetValue(managedValue, adsType);
        }

        public AdsValueHelper(object managedValue, AdsType adsType)
        {
            SetValue(managedValue, adsType);
        }

        public long LowInt64
        {
            get => (uint)adsvalue.generic.a + (((long)adsvalue.generic.b) << 32);
            set
            {
                adsvalue.generic.a = (int)(value & 0xFFFFFFFF);
                adsvalue.generic.b = (int)(value >> 32);
            }
        }

        ~AdsValueHelper()
        {
            if (_pinnedHandle.IsAllocated)
            {
                _pinnedHandle.Free();
            }
        }

        private AdsType GetAdsTypeForManagedType(Type type)
        {
            // Consider this code is only exercised by DirectorySearcher
            // it just translates the types needed by such a component, if more managed
            // types are to be used in the future, this function needs to be expanded.
            if (type == typeof(int))
            {
                return AdsType.ADSTYPE_INTEGER;
            }

            if (type == typeof(long))
            {
                return AdsType.ADSTYPE_LARGE_INTEGER;
            }

            if (type == typeof(bool))
            {
                return AdsType.ADSTYPE_BOOLEAN;
            }

            return AdsType.ADSTYPE_UNKNOWN;
        }

        public AdsValue GetStruct() => adsvalue;

        private static ushort LowOfInt(int i) => unchecked((ushort)(i & 0xFFFF));

        private static ushort HighOfInt(int i) => unchecked((ushort)((i >> 16) & 0xFFFF));

        public object GetValue()
        {
            switch ((AdsType)adsvalue.dwType)
            {
                // Common for DNS and LDAP.
                case AdsType.ADSTYPE_UTC_TIME:
                    {
                        var st = new SystemTime()
                        {
                            wYear = LowOfInt(adsvalue.generic.a),
                            wMonth = HighOfInt(adsvalue.generic.a),
                            wDayOfWeek = LowOfInt(adsvalue.generic.b),
                            wDay = HighOfInt(adsvalue.generic.b),
                            wHour = LowOfInt(adsvalue.generic.c),
                            wMinute = HighOfInt(adsvalue.generic.c),
                            wSecond = LowOfInt(adsvalue.generic.d),
                            wMilliseconds = HighOfInt(adsvalue.generic.d)
                        };

                        return new DateTime(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
                    }

                case AdsType.ADSTYPE_DN_WITH_BINARY:
                    {
                        var dnb = new DnWithBinary();
                        Marshal.PtrToStructure(adsvalue.pointer.value, dnb);

                        byte[] bytes = new byte[dnb.dwLength];
                        Marshal.Copy(dnb.lpBinaryValue, bytes, 0, dnb.dwLength);

                        var strb = new StringBuilder();
                        var binaryPart = new StringBuilder();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            string s = bytes[i].ToString("X", CultureInfo.InvariantCulture);
                            if (s.Length == 1)
                            {
                                binaryPart.Append('0');
                            }

                            binaryPart.Append(s);
                        }

                        strb.Append("B:");
                        strb.Append(binaryPart.Length);
                        strb.Append(':');
                        strb.Append(binaryPart);
                        strb.Append(':');
                        strb.Append(Marshal.PtrToStringUni(dnb.pszDNString));

                        return strb.ToString();
                    }

                case AdsType.ADSTYPE_DN_WITH_STRING:
                    {
                        var dns = new DnWithString();
                        Marshal.PtrToStructure(adsvalue.pointer.value, dns);
                        string strValue = Marshal.PtrToStringUni(dns.pszStringValue) ?? string.Empty;

                        return $"S:{strValue.Length}:{strValue}:{Marshal.PtrToStringUni(dns.pszDNString)}";
                    }

                case AdsType.ADSTYPE_DN_STRING:
                case AdsType.ADSTYPE_CASE_EXACT_STRING:
                case AdsType.ADSTYPE_CASE_IGNORE_STRING:
                case AdsType.ADSTYPE_PRINTABLE_STRING:
                case AdsType.ADSTYPE_NUMERIC_STRING:
                case AdsType.ADSTYPE_OBJECT_CLASS:
                    // The value is a String.
                    return Marshal.PtrToStringUni(adsvalue.pointer.value)!;

                case AdsType.ADSTYPE_BOOLEAN:
                    // The value is a bool.
                    return adsvalue.generic.a != 0;

                case AdsType.ADSTYPE_INTEGER:
                    // The value is an int.
                    return adsvalue.generic.a;

                case AdsType.ADSTYPE_NT_SECURITY_DESCRIPTOR:
                case AdsType.ADSTYPE_OCTET_STRING:
                case AdsType.ADSTYPE_PROV_SPECIFIC:
                    // The value is a byte[].
                    int len = adsvalue.octetString.length;
                    byte[] value = new byte[len];
                    Marshal.Copy(adsvalue.octetString.value, value, 0, len);
                    return value;

                case AdsType.ADSTYPE_INVALID:
                    throw new InvalidOperationException(SR.DSConvertTypeInvalid);

                case AdsType.ADSTYPE_LARGE_INTEGER:
                    return LowInt64;

                // Not used in LDAP
                case AdsType.ADSTYPE_CASEIGNORE_LIST:
                case AdsType.ADSTYPE_OCTET_LIST:
                case AdsType.ADSTYPE_PATH:
                case AdsType.ADSTYPE_POSTALADDRESS:
                case AdsType.ADSTYPE_TIMESTAMP:
                case AdsType.ADSTYPE_NETADDRESS:
                case AdsType.ADSTYPE_FAXNUMBER:
                case AdsType.ADSTYPE_EMAIL:

                case AdsType.ADSTYPE_BACKLINK:
                case AdsType.ADSTYPE_HOLD:
                case AdsType.ADSTYPE_TYPEDNAME:
                case AdsType.ADSTYPE_REPLICAPOINTER:
                case AdsType.ADSTYPE_UNKNOWN:
                    return new NotImplementedException(SR.Format(SR.DSAdsvalueTypeNYI, "0x" + Convert.ToString(adsvalue.dwType, 16)));

                default:
                    return new ArgumentException(SR.Format(SR.DSConvertFailed, "0x" + Convert.ToString(LowInt64, 16), "0x" + Convert.ToString(adsvalue.dwType, 16)));
            }
        }

        public object GetVlvValue()
        {
            var vlv = new AdsVLV();
            Marshal.PtrToStructure(adsvalue.octetString.value, vlv);

            byte[]? bytes = null;
            if (vlv.contextID != (IntPtr)0 && vlv.contextIDlength != 0)
            {
                bytes = new byte[vlv.contextIDlength];
                Marshal.Copy(vlv.contextID, bytes, 0, vlv.contextIDlength);
            }

            return new DirectoryVirtualListView
            {
                Offset = vlv.offset,
                ApproximateTotal = vlv.contentCount,
                DirectoryVirtualListViewContext = new DirectoryVirtualListViewContext(bytes)
            };
        }

        private void SetValue(object managedValue, AdsType adsType)
        {
            adsvalue = new AdsValue()
            {
                dwType = (int)adsType
            };

            switch (adsType)
            {
                case AdsType.ADSTYPE_INTEGER:
                    adsvalue.generic.a = (int)managedValue;
                    adsvalue.generic.b = 0;
                    break;
                case AdsType.ADSTYPE_LARGE_INTEGER:
                    LowInt64 = (long)managedValue;
                    break;
                case AdsType.ADSTYPE_BOOLEAN:
                    LowInt64 = (bool)managedValue ? -1 : 0;
                    break;
                case AdsType.ADSTYPE_CASE_IGNORE_STRING:
                    _pinnedHandle = GCHandle.Alloc(managedValue, GCHandleType.Pinned);
                    adsvalue.pointer.value = _pinnedHandle.AddrOfPinnedObject();
                    break;
                case AdsType.ADSTYPE_PROV_SPECIFIC:
                    byte[] bytes = (byte[])managedValue;
                    // Filling in an ADS_PROV_SPECIFIC struct.
                    // 1st dword (our member a) is DWORD dwLength.
                    // 2nd dword (our member b) is byte *lpValue.
                    adsvalue.octetString.length = bytes.Length;
                    _pinnedHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    adsvalue.octetString.value = _pinnedHandle.AddrOfPinnedObject();
                    break;
                default:
                    throw new NotImplementedException(SR.Format(SR.DSAdsvalueTypeNYI, "0x" + Convert.ToString((int)adsType, 16)));
            }
        }
    }
}
