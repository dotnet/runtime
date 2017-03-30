// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Interop
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Security.Permissions;

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
    internal class DnWithBinary
    {
        public int dwLength;
        public IntPtr lpBinaryValue;       // GUID of directory object
        public IntPtr pszDNString;         // Distinguished Name
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class DnWithString
    {
        public IntPtr pszStringValue;      // associated value
        public IntPtr pszDNString;         // Distinguished Name
    }

    // helper class for dealing with struct AdsValue.
    internal class AdsValueHelper
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
            get
            {
                return (long)((uint)adsvalue.generic.a + (((long)adsvalue.generic.b) << 32));
            }
            set
            {
                adsvalue.generic.a = (int)(value & 0xFFFFFFFF);
                adsvalue.generic.b = (int)(value >> 32);
            }
        }

        ~AdsValueHelper()
        {
            if (_pinnedHandle.IsAllocated)
                _pinnedHandle.Free();
        }

        private AdsType GetAdsTypeForManagedType(Type type)
        {
            //Consider this code is only excercised by DirectorySearcher
            //it just translates the types needed by such a component, if more managed
            //types are to be used in the future, this function needs to be expanded.
            if (type == typeof(int))
                return AdsType.ADSTYPE_INTEGER;
            if (type == typeof(long))
                return AdsType.ADSTYPE_LARGE_INTEGER;
            if (type == typeof(bool))
                return AdsType.ADSTYPE_BOOLEAN;

            return AdsType.ADSTYPE_UNKNOWN;
        }

        public AdsValue GetStruct()
        {
            return adsvalue;
        }

        private static ushort LowOfInt(int i)
        {
            return unchecked((ushort)(i & 0xFFFF));
        }

        private static ushort HighOfInt(int i)
        {
            return unchecked((ushort)((i >> 16) & 0xFFFF));
        }

        public object GetValue()
        {
            switch ((AdsType)adsvalue.dwType)
            {
                // Common for DNS and LDAP 
                case AdsType.ADSTYPE_UTC_TIME:
                    {
                        SystemTime st = new SystemTime();

                        st.wYear = LowOfInt(adsvalue.generic.a);
                        st.wMonth = HighOfInt(adsvalue.generic.a);
                        st.wDayOfWeek = LowOfInt(adsvalue.generic.b);
                        st.wDay = HighOfInt(adsvalue.generic.b);
                        st.wHour = LowOfInt(adsvalue.generic.c);
                        st.wMinute = HighOfInt(adsvalue.generic.c);
                        st.wSecond = LowOfInt(adsvalue.generic.d);
                        st.wMilliseconds = HighOfInt(adsvalue.generic.d);

                        return new DateTime(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
                    }

                case AdsType.ADSTYPE_DN_WITH_BINARY:
                    {
                        DnWithBinary dnb = new DnWithBinary();
                        Marshal.PtrToStructure(adsvalue.pointer.value, dnb);
                        byte[] bytes = new byte[dnb.dwLength];
                        Marshal.Copy(dnb.lpBinaryValue, bytes, 0, dnb.dwLength);
                        StringBuilder strb = new StringBuilder();
                        StringBuilder binaryPart = new StringBuilder();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            string s = bytes[i].ToString("X", CultureInfo.InvariantCulture);
                            if (s.Length == 1)
                                binaryPart.Append("0");
                            binaryPart.Append(s);
                        }

                        strb.Append("B:");
                        strb.Append(binaryPart.Length);
                        strb.Append(":");
                        strb.Append(binaryPart.ToString());
                        strb.Append(":");
                        strb.Append(Marshal.PtrToStringUni(dnb.pszDNString));
                        return strb.ToString();
                    }

                case AdsType.ADSTYPE_DN_WITH_STRING:
                    {
                        DnWithString dns = new DnWithString();
                        Marshal.PtrToStructure(adsvalue.pointer.value, dns);
                        string strValue = Marshal.PtrToStringUni(dns.pszStringValue);
                        if (strValue == null)
                            strValue = "";

                        StringBuilder strb = new StringBuilder();
                        strb.Append("S:");
                        strb.Append(strValue.Length);
                        strb.Append(":");
                        strb.Append(strValue);
                        strb.Append(":");
                        strb.Append(Marshal.PtrToStringUni(dns.pszDNString));
                        return strb.ToString();
                    }

                case AdsType.ADSTYPE_DN_STRING:
                case AdsType.ADSTYPE_CASE_EXACT_STRING:
                case AdsType.ADSTYPE_CASE_IGNORE_STRING:
                case AdsType.ADSTYPE_PRINTABLE_STRING:
                case AdsType.ADSTYPE_NUMERIC_STRING:
                case AdsType.ADSTYPE_OBJECT_CLASS:
                    // string
                    return Marshal.PtrToStringUni(adsvalue.pointer.value);

                case AdsType.ADSTYPE_BOOLEAN:
                    // bool
                    return adsvalue.generic.a != 0;

                case AdsType.ADSTYPE_INTEGER:
                    // int
                    return adsvalue.generic.a;

                case AdsType.ADSTYPE_NT_SECURITY_DESCRIPTOR:
                case AdsType.ADSTYPE_OCTET_STRING:
                case AdsType.ADSTYPE_PROV_SPECIFIC:
                    // byte[]
                    int len = adsvalue.octetString.length;
                    byte[] value = new byte[len];
                    Marshal.Copy(adsvalue.octetString.value, value, 0, len);
                    return value;

                case AdsType.ADSTYPE_INVALID:
                    throw new InvalidOperationException(SR.DSConvertTypeInvalid);

                case AdsType.ADSTYPE_LARGE_INTEGER:
                    return LowInt64;

                // not used in LDAP
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
                    return new NotImplementedException(String.Format(CultureInfo.CurrentCulture, SR.DSAdsvalueTypeNYI , "0x" + Convert.ToString(adsvalue.dwType, 16)));

                default:
                    return new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.DSConvertFailed , "0x" + Convert.ToString(LowInt64, 16), "0x" + Convert.ToString(adsvalue.dwType, 16)));
            }
        }

        public object GetVlvValue()
        {
            AdsVLV vlv = new AdsVLV();
            Marshal.PtrToStructure(adsvalue.octetString.value, vlv);
            byte[] bytes = null;
            if (vlv.contextID != (IntPtr)0 && vlv.contextIDlength != 0)
            {
                bytes = new byte[vlv.contextIDlength];
                Marshal.Copy(vlv.contextID, bytes, 0, vlv.contextIDlength);
            }
            DirectoryVirtualListView vlvResponse = new DirectoryVirtualListView();
            vlvResponse.Offset = vlv.offset;
            vlvResponse.ApproximateTotal = vlv.contentCount;
            DirectoryVirtualListViewContext context = new DirectoryVirtualListViewContext(bytes);
            vlvResponse.DirectoryVirtualListViewContext = context;

            return vlvResponse;
        }

        private unsafe void SetValue(object managedValue, AdsType adsType)
        {
            adsvalue = new AdsValue();
            adsvalue.dwType = (int)adsType;
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
                    if ((bool)managedValue)
                        LowInt64 = -1;
                    else
                        LowInt64 = 0;
                    break;
                case AdsType.ADSTYPE_CASE_IGNORE_STRING:
                    _pinnedHandle = GCHandle.Alloc(managedValue, GCHandleType.Pinned);
                    adsvalue.pointer.value = _pinnedHandle.AddrOfPinnedObject();
                    break;
                case AdsType.ADSTYPE_PROV_SPECIFIC:
                    byte[] bytes = (byte[])managedValue;
                    // filling in an ADS_PROV_SPECIFIC struct.
                    // 1st dword (our member a) is DWORD dwLength.
                    // 2nd dword (our member b) is byte *lpValue.
                    adsvalue.octetString.length = bytes.Length;
                    _pinnedHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    adsvalue.octetString.value = _pinnedHandle.AddrOfPinnedObject();
                    break;
                default:
                    throw new NotImplementedException(String.Format(CultureInfo.CurrentCulture, SR.DSAdsvalueTypeNYI , "0x" + Convert.ToString((int)adsType, 16)));
            }
        }
    }
}

