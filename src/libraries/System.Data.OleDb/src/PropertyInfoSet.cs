// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    sealed internal class OleDbPropertyInfo
    {
        public Guid _propertySet;
        public Int32 _propertyID;
        public string _description;
        public string _lowercase;
        public Type _type;

        public int _flags;
        public int _vtype;
        public object _supportedValues;

        public object _defaultValue;
    }

    internal sealed class PropertyInfoSet : SafeHandle
    {
        private int setCount;
        private IntPtr descBuffer;

        internal PropertyInfoSet(UnsafeNativeMethods.IDBProperties idbProperties, PropertyIDSet propIDSet) : base(IntPtr.Zero, true)
        {
            OleDbHResult hr;
            int propIDSetCount = propIDSet.Count;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                hr = idbProperties.GetPropertyInfo(propIDSetCount, propIDSet, out this.setCount, out base.handle, out this.descBuffer);
            }
            if ((0 <= hr) && (ADP.PtrZero != handle))
            {
                SafeNativeMethods.Wrapper.ClearErrorInfo();
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return ((IntPtr.Zero == base.handle) && (IntPtr.Zero == this.descBuffer));
            }
        }

        internal Dictionary<string, OleDbPropertyInfo> GetValues()
        {
            Dictionary<string, OleDbPropertyInfo> propertyLookup = null;

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);
                if (ADP.PtrZero != this.handle)
                {
                    propertyLookup = new Dictionary<string, OleDbPropertyInfo>(StringComparer.OrdinalIgnoreCase);

                    IntPtr setPtr = this.handle;
                    tagDBPROPINFO propinfo = new tagDBPROPINFO();
                    tagDBPROPINFOSET propinfoset = new tagDBPROPINFOSET();

                    for (int i = 0; i < setCount; ++i, setPtr = ADP.IntPtrOffset(setPtr, ODB.SizeOf_tagDBPROPINFOSET))
                    {
                        Marshal.PtrToStructure(setPtr, propinfoset);

                        int infoCount = propinfoset.cPropertyInfos;
                        IntPtr infoPtr = propinfoset.rgPropertyInfos;
                        for (int k = 0; k < infoCount; ++k, infoPtr = ADP.IntPtrOffset(infoPtr, ODB.SizeOf_tagDBPROPINFO))
                        {
                            Marshal.PtrToStructure(infoPtr, propinfo);

                            OleDbPropertyInfo propertyInfo = new OleDbPropertyInfo();
                            propertyInfo._propertySet = propinfoset.guidPropertySet;
                            propertyInfo._propertyID = propinfo.dwPropertyID;
                            propertyInfo._flags = propinfo.dwFlags;
                            propertyInfo._vtype = propinfo.vtType;
                            propertyInfo._supportedValues = propinfo.vValue;
                            propertyInfo._description = propinfo.pwszDescription;
                            propertyInfo._lowercase = propinfo.pwszDescription.ToLower(CultureInfo.InvariantCulture);
                            propertyInfo._type = PropertyInfoSet.FromVtType(propinfo.vtType);

                            propertyLookup[propertyInfo._lowercase] = propertyInfo;
                        }
                    }
                }
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return propertyLookup;
        }

        override protected bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once and is non-interrutible.
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                int count = this.setCount;
                for (int i = 0; i < count; ++i)
                {
                    int offset = (i * ODB.SizeOf_tagDBPROPINFOSET);
                    IntPtr infoPtr = Marshal.ReadIntPtr(ptr, offset);
                    if (IntPtr.Zero != infoPtr)
                    {
                        int infoCount = Marshal.ReadInt32(ptr, offset + ADP.PtrSize);

                        for (int k = 0; k < infoCount; ++k)
                        {
                            IntPtr valuePtr = ADP.IntPtrOffset(infoPtr, (k * ODB.SizeOf_tagDBPROPINFO) + ODB.OffsetOf_tagDBPROPINFO_Value);
                            SafeNativeMethods.VariantClear(valuePtr);
                        }
                        SafeNativeMethods.CoTaskMemFree(infoPtr); // was allocated by provider
                    }
                }
                SafeNativeMethods.CoTaskMemFree(ptr);
            }

            ptr = this.descBuffer;
            this.descBuffer = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                SafeNativeMethods.CoTaskMemFree(ptr);
            }
            return true;
        }

        internal static Type FromVtType(int vartype)
        {
            switch ((VarEnum)vartype)
            {
                case VarEnum.VT_EMPTY:
                    return null;
                case VarEnum.VT_NULL:
                    return typeof(System.DBNull);
                case VarEnum.VT_I2:
                    return typeof(System.Int16);
                case VarEnum.VT_I4:
                    return typeof(System.Int32);
                case VarEnum.VT_R4:
                    return typeof(System.Single);
                case VarEnum.VT_R8:
                    return typeof(System.Double);
                case VarEnum.VT_CY:
                    return typeof(System.Decimal);
                case VarEnum.VT_DATE:
                    return typeof(System.DateTime);
                case VarEnum.VT_BSTR:
                    return typeof(System.String);
                case VarEnum.VT_DISPATCH:
                    return typeof(System.Object);
                case VarEnum.VT_ERROR:
                    return typeof(System.Int32);
                case VarEnum.VT_BOOL:
                    return typeof(System.Boolean);
                case VarEnum.VT_VARIANT:
                    return typeof(System.Object);
                case VarEnum.VT_UNKNOWN:
                    return typeof(System.Object);
                case VarEnum.VT_DECIMAL:
                    return typeof(System.Decimal);
                case VarEnum.VT_I1:
                    return typeof(System.SByte);
                case VarEnum.VT_UI1:
                    return typeof(System.Byte);
                case VarEnum.VT_UI2:
                    return typeof(System.UInt16);
                case VarEnum.VT_UI4:
                    return typeof(System.UInt32);
                case VarEnum.VT_I8:
                    return typeof(System.Int64);
                case VarEnum.VT_UI8:
                    return typeof(System.UInt64);
                case VarEnum.VT_INT:
                    return typeof(System.Int32);
                case VarEnum.VT_UINT:
                    return typeof(System.UInt32);
                default:
                    return typeof(System.Object);
            }
        }
    }
}
