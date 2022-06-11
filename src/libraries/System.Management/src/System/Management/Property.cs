// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace System.Management
{
    // We use this class to prevent the accidental returning of a boxed value type to a caller
    // If we store a boxed value type in a private field, and return it to the caller through a public
    // property or method, the call can potentially change its value.  The GetSafeObject method does two things
    // 1) If the value is a primitive, we know that it will implement IConvertible.  IConvertible.ToType will
    // copy a boxed primitive
    // 2) In the case of a boxed non-primitive value type, or simply a reference type, we call
    // RuntimeHelpers.GetObjectValue.  This returns reference types right back to the caller, but if passed
    // a boxed non-primitive value type, it will return a boxed copy.  We cannot use GetObjectValue for primitives
    // because its implementation does not copy boxed primitives.
    internal static class ValueTypeSafety
    {
        public static object GetSafeObject(object theValue)
        {
            if (null == theValue)
                return null;
            else if (theValue.GetType().IsPrimitive)
                return ((IConvertible)theValue).ToType(typeof(object), null);
            else
                return RuntimeHelpers.GetObjectValue(theValue);
        }
    }

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents information about a WMI property.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample displays all properties that qualifies the "DeviceID" property
    /// // in Win32_LogicalDisk.DeviceID='C' instance.
    /// class Sample_PropertyData
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementObject disk =
    ///             new ManagementObject("Win32_LogicalDisk.DeviceID=\"C:\"");
    ///         PropertyData diskProperty = disk.Properties["DeviceID"];
    ///         Console.WriteLine("Name: " + diskProperty.Name);
    ///         Console.WriteLine("Type: " + diskProperty.Type);
    ///         Console.WriteLine("Value: " + diskProperty.Value);
    ///         Console.WriteLine("IsArray: " + diskProperty.IsArray);
    ///         Console.WriteLine("IsLocal: " + diskProperty.IsLocal);
    ///         Console.WriteLine("Origin: " + diskProperty.Origin);
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample displays all properties that qualifies the "DeviceID" property
    /// ' in Win32_LogicalDisk.DeviceID='C' instance.
    /// Class Sample_PropertyData
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim disk As New ManagementObject("Win32_LogicalDisk.DeviceID=""C:""")
    ///         Dim diskProperty As PropertyData = disk.Properties("DeviceID")
    ///         Console.WriteLine("Name: " &amp; diskProperty.Name)
    ///         Console.WriteLine("Type: " &amp; diskProperty.Type)
    ///         Console.WriteLine("Value: " &amp; diskProperty.Value)
    ///         Console.WriteLine("IsArray: " &amp; diskProperty.IsArray)
    ///         Console.WriteLine("IsLocal: " &amp; diskProperty.IsLocal)
    ///         Console.WriteLine("Origin: " &amp; diskProperty.Origin)
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class PropertyData
    {
        private readonly ManagementBaseObject parent;  //need access to IWbemClassObject pointer to be able to refresh property info
                                                       //and get property qualifiers
        private readonly string propertyName;

        private object propertyValue;
        private long propertyNullEnumValue;
        private int propertyType;
        private int propertyFlavor;
        private QualifierDataCollection qualifiers;

        internal PropertyData(ManagementBaseObject parent, string propName)
        {
            this.parent = parent;
            this.propertyName = propName;
            qualifiers = null;
            RefreshPropertyInfo();
        }

        //This private function is used to refresh the information from the Wmi object before returning the requested data
        private void RefreshPropertyInfo()
        {
            propertyValue = null;    // Needed so we don't leak this in/out parameter...

            int status = parent.wbemObject.Get_(propertyName, 0, ref propertyValue, ref propertyType, ref propertyFlavor);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        /// <summary>
        ///    <para>Gets or sets the name of the property.</para>
        /// </summary>
        /// <value>
        ///    A string containing the name of the
        ///    property.
        /// </value>
        public string Name
        { //doesn't change for this object so we don't need to refresh
            get { return propertyName ?? ""; }
        }

        /// <summary>
        ///    <para>Gets or sets the current value of the property.</para>
        /// </summary>
        /// <value>
        ///    An object containing the value of the
        ///    property.
        /// </value>
        public object Value
        {
            get
            {
                RefreshPropertyInfo();
                return ValueTypeSafety.GetSafeObject(MapWmiValueToValue(propertyValue,
                        (CimType)(propertyType & ~(int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY),
                        (0 != (propertyType & (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY))));
            }
            set
            {
                RefreshPropertyInfo();

                object newValue = MapValueToWmiValue(value,
                            (CimType)(propertyType & ~(int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY),
                            (0 != (propertyType & (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY)));

                int status = parent.wbemObject.Put_(propertyName, 0, ref newValue, 0);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
                //if succeeded and this object has a path, update the path to reflect the new key value
                //NOTE : we could only do this for key properties but since it's not trivial to find out
                //       whether this property is a key or not, we just do it for any property
                else
                    if (parent.GetType() == typeof(ManagementObject))
                    ((ManagementObject)parent).Path.UpdateRelativePath((string)parent["__RELPATH"]);

            }
        }

        /// <summary>
        ///    <para>Gets or sets the CIM type of the property.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.CimType'/> value
        ///    representing the CIM type of the property.</para>
        /// </value>
        public CimType Type
        {
            get
            {
                RefreshPropertyInfo();
                return (CimType)(propertyType & ~(int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY);
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the property has been defined in the current WMI class.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the property has been defined
        ///    in the current WMI class; otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool IsLocal
        {
            get
            {
                RefreshPropertyInfo();
                return ((propertyFlavor & (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_ORIGIN_PROPAGATED) != 0) ? false : true;
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the property is an array.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the property is an array; otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool IsArray
        {
            get
            {
                RefreshPropertyInfo();
                return ((propertyType & (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY) != 0);
            }
        }

        /// <summary>
        ///    <para>Gets or sets the name of the WMI class in the hierarchy in which the property was introduced.</para>
        /// </summary>
        /// <value>
        ///    A string containing the name of the
        ///    originating WMI class.
        /// </value>
        public string Origin
        {
            get
            {
                string className = null;
                int status = parent.wbemObject.GetPropertyOrigin_(propertyName, out className);

                if (status < 0)
                {
                    if (status == (int)tag_WBEMSTATUS.WBEM_E_INVALID_OBJECT)
                        className = string.Empty;    // Interpret as an unspecified property - return ""
                    else if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return className;
            }
        }


        /// <summary>
        ///    <para>Gets or sets the set of qualifiers defined on the property.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.QualifierDataCollection'/> that represents
        ///    the set of qualifiers defined on the property.</para>
        /// </value>
        public QualifierDataCollection Qualifiers
        {
            get
            {
                if (qualifiers == null)
                    qualifiers = new QualifierDataCollection(parent, propertyName, QualifierType.PropertyQualifier);

                return qualifiers;
            }
        }
        internal long NullEnumValue
        {
            get
            {
                return propertyNullEnumValue;
            }

            set
            {
                propertyNullEnumValue = value;
            }
        }

        /// <summary>
        /// Takes a property value returned from WMI and maps it to an
        /// appropriate managed code representation.
        /// </summary>
        /// <param name="wmiValue"> </param>
        /// <param name="type"> </param>
        /// <param name="isArray"> </param>
        internal static object MapWmiValueToValue(object wmiValue, CimType type, bool isArray)
        {
            object val = null;

            if ((System.DBNull.Value != wmiValue) && (null != wmiValue))
            {
                if (isArray)
                {
                    Array wmiValueArray = (Array)wmiValue;
                    int length = wmiValueArray.Length;

                    switch (type)
                    {
                        case CimType.UInt16:
                            val = new ushort[length];

                            for (int i = 0; i < length; i++)
                                ((ushort[])val)[i] = (ushort)((int)(wmiValueArray.GetValue(i)));
                            break;

                        case CimType.UInt32:
                            val = new uint[length];

                            for (int i = 0; i < length; i++)
                                ((uint[])val)[i] = (uint)((int)(wmiValueArray.GetValue(i)));
                            break;

                        case CimType.UInt64:
                            val = new ulong[length];

                            for (int i = 0; i < length; i++)
                                ((ulong[])val)[i] = Convert.ToUInt64((string)(wmiValueArray.GetValue(i)), (IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(ulong)));
                            break;

                        case CimType.SInt8:
                            val = new sbyte[length];

                            for (int i = 0; i < length; i++)
                                ((sbyte[])val)[i] = (sbyte)((short)(wmiValueArray.GetValue(i)));
                            break;

                        case CimType.SInt64:
                            val = new long[length];

                            for (int i = 0; i < length; i++)
                                ((long[])val)[i] = Convert.ToInt64((string)(wmiValueArray.GetValue(i)), (IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(long)));
                            break;

                        case CimType.Char16:
                            val = new char[length];

                            for (int i = 0; i < length; i++)
                                ((char[])val)[i] = (char)((short)(wmiValueArray.GetValue(i)));
                            break;

                        case CimType.Object:
                            val = new ManagementBaseObject[length];

                            for (int i = 0; i < length; i++)
                                ((ManagementBaseObject[])val)[i] = new ManagementBaseObject(new IWbemClassObjectFreeThreaded(Marshal.GetIUnknownForObject(wmiValueArray.GetValue(i))));
                            break;

                        default:
                            val = wmiValue;
                            break;
                    }
                }
                else
                {
                    val = type switch
                    {
                        CimType.SInt8 => (sbyte)((short)wmiValue),
                        CimType.UInt16 => (ushort)((int)wmiValue),
                        CimType.UInt32 => (uint)((int)wmiValue),
                        CimType.UInt64 => Convert.ToUInt64((string)wmiValue, (IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(ulong))),
                        CimType.SInt64 => Convert.ToInt64((string)wmiValue, (IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(long))),
                        CimType.Char16 => (char)((short)wmiValue),
                        CimType.Object => new ManagementBaseObject(new IWbemClassObjectFreeThreaded(Marshal.GetIUnknownForObject(wmiValue))),
                        _ => wmiValue,
                    };
                }
            }

            return val;
        }

        /// <summary>
        /// Takes a managed code value, together with a desired property
        /// </summary>
        /// <param name="val"> </param>
        /// <param name="type"> </param>
        /// <param name="isArray"> </param>
        internal static object MapValueToWmiValue(object val, CimType type, bool isArray)
        {
            object wmiValue = System.DBNull.Value;
            CultureInfo culInfo = CultureInfo.InvariantCulture;
            if (null != val)
            {
                if (isArray)
                {
                    Array valArray = (Array)val;
                    int length = valArray.Length;

                    switch (type)
                    {
                        case CimType.SInt8:
                            wmiValue = new short[length];
                            for (int i = 0; i < length; i++)
                                ((short[])(wmiValue))[i] = (short)Convert.ToSByte(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(sbyte)));
                            break;

                        case CimType.UInt8:
                            if (val is byte[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new byte[length];
                                for (int i = 0; i < length; i++)
                                    ((byte[])wmiValue)[i] = Convert.ToByte(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(byte)));
                            }
                            break;

                        case CimType.SInt16:
                            if (val is short[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new short[length];
                                for (int i = 0; i < length; i++)
                                    ((short[])(wmiValue))[i] = Convert.ToInt16(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(short)));
                            }
                            break;

                        case CimType.UInt16:
                            wmiValue = new int[length];
                            for (int i = 0; i < length; i++)
                                ((int[])(wmiValue))[i] = (int)(Convert.ToUInt16(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(ushort))));
                            break;

                        case CimType.SInt32:
                            if (val is int[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new int[length];
                                for (int i = 0; i < length; i++)
                                    ((int[])(wmiValue))[i] = Convert.ToInt32(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(int)));
                            }
                            break;

                        case CimType.UInt32:
                            wmiValue = new int[length];
                            for (int i = 0; i < length; i++)
                                ((int[])(wmiValue))[i] = (int)(Convert.ToUInt32(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(uint))));
                            break;

                        case CimType.SInt64:
                            wmiValue = new string[length];
                            for (int i = 0; i < length; i++)
                                ((string[])(wmiValue))[i] = (Convert.ToInt64(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(long)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(long)));
                            break;

                        case CimType.UInt64:
                            wmiValue = new string[length];
                            for (int i = 0; i < length; i++)
                                ((string[])(wmiValue))[i] = (Convert.ToUInt64(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(ulong)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(ulong)));
                            break;

                        case CimType.Real32:
                            if (val is float[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new float[length];
                                for (int i = 0; i < length; i++)
                                    ((float[])(wmiValue))[i] = Convert.ToSingle(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(float)));
                            }
                            break;

                        case CimType.Real64:
                            if (val is double[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new double[length];
                                for (int i = 0; i < length; i++)
                                    ((double[])(wmiValue))[i] = Convert.ToDouble(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(double)));
                            }
                            break;

                        case CimType.Char16:
                            wmiValue = new short[length];
                            for (int i = 0; i < length; i++)
                                ((short[])(wmiValue))[i] = (short)Convert.ToChar(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(char)));
                            break;

                        case CimType.String:
                        case CimType.DateTime:
                        case CimType.Reference:
                            if (val is string[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new string[length];
                                for (int i = 0; i < length; i++)
                                    ((string[])(wmiValue))[i] = (valArray.GetValue(i)).ToString();
                            }
                            break;

                        case CimType.Boolean:
                            if (val is bool[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new bool[length];
                                for (int i = 0; i < length; i++)
                                    ((bool[])(wmiValue))[i] = Convert.ToBoolean(valArray.GetValue(i), (IFormatProvider)culInfo.GetFormat(typeof(bool)));
                            }
                            break;

                        case CimType.Object:
                            wmiValue = new IWbemClassObject_DoNotMarshal[length];

                            for (int i = 0; i < length; i++)
                            {
                                ((IWbemClassObject_DoNotMarshal[])(wmiValue))[i] = (IWbemClassObject_DoNotMarshal)(Marshal.GetObjectForIUnknown(((ManagementBaseObject)valArray.GetValue(i)).wbemObject));
                            }
                            break;

                        default:
                            wmiValue = val;
                            break;
                    }
                }
                else
                {
                    switch (type)
                    {
                        case CimType.SInt8:
                            wmiValue = (short)Convert.ToSByte(val, (IFormatProvider)culInfo.GetFormat(typeof(short)));
                            break;

                        case CimType.UInt8:
                            wmiValue = Convert.ToByte(val, (IFormatProvider)culInfo.GetFormat(typeof(byte)));
                            break;

                        case CimType.SInt16:
                            wmiValue = Convert.ToInt16(val, (IFormatProvider)culInfo.GetFormat(typeof(short)));
                            break;

                        case CimType.UInt16:
                            wmiValue = (int)(Convert.ToUInt16(val, (IFormatProvider)culInfo.GetFormat(typeof(ushort))));
                            break;

                        case CimType.SInt32:
                            wmiValue = Convert.ToInt32(val, (IFormatProvider)culInfo.GetFormat(typeof(int)));
                            break;

                        case CimType.UInt32:
                            wmiValue = (int)Convert.ToUInt32(val, (IFormatProvider)culInfo.GetFormat(typeof(uint)));
                            break;

                        case CimType.SInt64:
                            wmiValue = (Convert.ToInt64(val, (IFormatProvider)culInfo.GetFormat(typeof(long)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(long)));
                            break;

                        case CimType.UInt64:
                            wmiValue = (Convert.ToUInt64(val, (IFormatProvider)culInfo.GetFormat(typeof(ulong)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(ulong)));
                            break;

                        case CimType.Real32:
                            wmiValue = Convert.ToSingle(val, (IFormatProvider)culInfo.GetFormat(typeof(float)));
                            break;

                        case CimType.Real64:
                            wmiValue = Convert.ToDouble(val, (IFormatProvider)culInfo.GetFormat(typeof(double)));
                            break;

                        case CimType.Char16:
                            wmiValue = (short)Convert.ToChar(val, (IFormatProvider)culInfo.GetFormat(typeof(char)));
                            break;

                        case CimType.String:
                        case CimType.DateTime:
                        case CimType.Reference:
                            wmiValue = val.ToString();
                            break;

                        case CimType.Boolean:
                            wmiValue = Convert.ToBoolean(val, (IFormatProvider)culInfo.GetFormat(typeof(bool)));
                            break;

                        case CimType.Object:
                            if (val is ManagementBaseObject)
                            {
                                wmiValue = Marshal.GetObjectForIUnknown(((ManagementBaseObject)val).wbemObject);
                            }
                            else
                            {
                                wmiValue = val;
                            }
                            break;

                        default:
                            wmiValue = val;
                            break;
                    }
                }
            }

            return wmiValue;
        }

        internal static object MapValueToWmiValue(object val, out bool isArray, out CimType type)
        {
            object wmiValue = System.DBNull.Value;
            CultureInfo culInfo = CultureInfo.InvariantCulture;
            isArray = false;
            type = 0;

            if (null != val)
            {
                isArray = val.GetType().IsArray;
                Type valueType = val.GetType();

                if (isArray)
                {
                    Type elementType = valueType.GetElementType();

                    // Casting primitive types to object[] is not allowed
                    if (elementType.IsPrimitive)
                    {
                        if (elementType == typeof(byte))
                        {
                            byte[] arrayValue = (byte[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt8;
                            wmiValue = new short[length];

                            for (int i = 0; i < length; i++)
                                ((short[])wmiValue)[i] = ((IConvertible)((byte)(arrayValue[i]))).ToInt16(null);
                        }
                        else if (elementType == typeof(sbyte))
                        {
                            sbyte[] arrayValue = (sbyte[])val;
                            int length = arrayValue.Length;
                            type = CimType.SInt8;
                            wmiValue = new short[length];

                            for (int i = 0; i < length; i++)
                                ((short[])wmiValue)[i] = ((IConvertible)((sbyte)(arrayValue[i]))).ToInt16(null);
                        }
                        else if (elementType == typeof(bool))
                        {
                            type = CimType.Boolean;
                            wmiValue = (bool[])val;
                        }
                        else if (elementType == typeof(ushort))
                        {
                            ushort[] arrayValue = (ushort[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt16;
                            wmiValue = new int[length];

                            for (int i = 0; i < length; i++)
                                ((int[])wmiValue)[i] = ((IConvertible)((ushort)(arrayValue[i]))).ToInt32(null);
                        }
                        else if (elementType == typeof(short))
                        {
                            type = CimType.SInt16;
                            wmiValue = (short[])val;
                        }
                        else if (elementType == typeof(int))
                        {
                            type = CimType.SInt32;
                            wmiValue = (int[])val;
                        }
                        else if (elementType == typeof(uint))
                        {
                            uint[] arrayValue = (uint[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt32;
                            wmiValue = new string[length];

                            for (int i = 0; i < length; i++)
                                ((string[])wmiValue)[i] = ((uint)(arrayValue[i])).ToString((IFormatProvider)culInfo.GetFormat(typeof(uint)));
                        }
                        else if (elementType == typeof(ulong))
                        {
                            ulong[] arrayValue = (ulong[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt64;
                            wmiValue = new string[length];

                            for (int i = 0; i < length; i++)
                                ((string[])wmiValue)[i] = ((ulong)(arrayValue[i])).ToString((IFormatProvider)culInfo.GetFormat(typeof(ulong)));
                        }
                        else if (elementType == typeof(long))
                        {
                            long[] arrayValue = (long[])val;
                            int length = arrayValue.Length;
                            type = CimType.SInt64;
                            wmiValue = new string[length];

                            for (int i = 0; i < length; i++)
                                ((string[])wmiValue)[i] = ((long)(arrayValue[i])).ToString((IFormatProvider)culInfo.GetFormat(typeof(long)));
                        }
                        else if (elementType == typeof(float))
                        {
                            type = CimType.Real32;
                            wmiValue = (float[])val;
                        }
                        else if (elementType == typeof(double))
                        {
                            type = CimType.Real64;
                            wmiValue = (double[])val;
                        }
                        else if (elementType == typeof(char))
                        {
                            char[] arrayValue = (char[])val;
                            int length = arrayValue.Length;
                            type = CimType.Char16;
                            wmiValue = new short[length];

                            for (int i = 0; i < length; i++)
                                ((short[])wmiValue)[i] = ((IConvertible)((char)(arrayValue[i]))).ToInt16(null);
                        }
                    }
                    else
                    {
                        // Non-primitive types
                        if (elementType == typeof(string))
                        {
                            type = CimType.String;
                            wmiValue = (string[])val;
                        }
                        else
                        {
                            // Check for an embedded object array
                            if (val is ManagementBaseObject[])
                            {
                                Array valArray = (Array)val;
                                int length = valArray.Length;
                                type = CimType.Object;
                                wmiValue = new IWbemClassObject_DoNotMarshal[length];

                                for (int i = 0; i < length; i++)
                                {
                                    ((IWbemClassObject_DoNotMarshal[])(wmiValue))[i] = (IWbemClassObject_DoNotMarshal)(Marshal.GetObjectForIUnknown(((ManagementBaseObject)valArray.GetValue(i)).wbemObject));
                                }
                            }
                        }
                    }
                }
                else    // Non-array values
                {
                    if (valueType == typeof(ushort))
                    {
                        type = CimType.UInt16;
                        wmiValue = ((IConvertible)((ushort)val)).ToInt32(null);
                    }
                    else if (valueType == typeof(uint))
                    {
                        type = CimType.UInt32;
                        if (((uint)val & 0x80000000) != 0)
                            wmiValue = Convert.ToString(val, (IFormatProvider)culInfo.GetFormat(typeof(uint)));
                        else
                            wmiValue = Convert.ToInt32(val, (IFormatProvider)culInfo.GetFormat(typeof(int)));
                    }
                    else if (valueType == typeof(ulong))
                    {
                        type = CimType.UInt64;
                        wmiValue = ((ulong)val).ToString((IFormatProvider)culInfo.GetFormat(typeof(ulong)));
                    }
                    else if (valueType == typeof(sbyte))
                    {
                        type = CimType.SInt8;
                        wmiValue = ((IConvertible)((sbyte)val)).ToInt16(null);
                    }
                    else if (valueType == typeof(byte))
                    {
                        type = CimType.UInt8;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(short))
                    {
                        type = CimType.SInt16;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(int))
                    {
                        type = CimType.SInt32;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(long))
                    {
                        type = CimType.SInt64;
                        wmiValue = val.ToString();
                    }
                    else if (valueType == typeof(bool))
                    {
                        type = CimType.Boolean;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(float))
                    {
                        type = CimType.Real32;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(double))
                    {
                        type = CimType.Real64;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(char))
                    {
                        type = CimType.Char16;
                        wmiValue = ((IConvertible)((char)val)).ToInt16(null);
                    }
                    else if (valueType == typeof(string))
                    {
                        type = CimType.String;
                        wmiValue = val;
                    }
                    else
                    {
                        // Check for an embedded object
                        if (val is ManagementBaseObject)
                        {
                            type = CimType.Object;
                            wmiValue = Marshal.GetObjectForIUnknown(((ManagementBaseObject)val).wbemObject);
                        }
                    }
                }
            }

            return wmiValue;
        }

    }//PropertyData
}
