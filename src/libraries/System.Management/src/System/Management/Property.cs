// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    class ValueTypeSafety
    {
        public static object GetSafeObject(object theValue)
        {
            if(null == theValue)
                return null;
            else if(theValue.GetType().IsPrimitive)
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
        private ManagementBaseObject parent;  //need access to IWbemClassObject pointer to be able to refresh property info
                                    //and get property qualifiers
        private string propertyName;

        private Object propertyValue;
        private Int64 propertyNullEnumValue = 0;
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
            propertyValue = null;	// Needed so we don't leak this in/out parameter...

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
            get { return propertyName != null ? propertyName : ""; }
        }

        /// <summary>
        ///    <para>Gets or sets the current value of the property.</para>
        /// </summary>
        /// <value>
        ///    An object containing the value of the
        ///    property.
        /// </value>
        public Object Value 
        {
            get { 
                RefreshPropertyInfo(); 
                return ValueTypeSafety.GetSafeObject(MapWmiValueToValue(propertyValue,
                        (CimType)(propertyType & ~(int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY),
                        (0 != (propertyType & (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY))));
            }
            set {
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
        public CimType Type {
            get { 
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
            get { 
                RefreshPropertyInfo();
                return ((propertyFlavor & (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_ORIGIN_PROPAGATED) != 0) ? false : true ; }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the property is an array.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the property is an array; otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool IsArray 
        {
            get { 
                RefreshPropertyInfo();
                return ((propertyType & (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY) != 0);}
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
            get { 
                string className = null;
                int status = parent.wbemObject.GetPropertyOrigin_(propertyName, out className);

                if (status < 0)
                {
                    if (status == (int)tag_WBEMSTATUS.WBEM_E_INVALID_OBJECT)
                        className = String.Empty;	// Interpret as an unspecified property - return ""
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
            get {
                if (qualifiers == null)
                    qualifiers = new QualifierDataCollection(parent, propertyName, QualifierType.PropertyQualifier);

                return qualifiers;
            }
        }
              internal Int64 NullEnumValue
             {
                 get {
                     return propertyNullEnumValue;
                 }
                 
                 set {
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
                            val = new UInt16 [length];
                            
                            for (int i = 0; i < length; i++)
                                ((UInt16[])val) [i] = (UInt16)((Int32)(wmiValueArray.GetValue(i)));
                            break;
                            
                        case CimType.UInt32:
                            val = new UInt32 [length];
                            
                            for (int i = 0; i < length; i++)
                                ((UInt32[])val)[i] = (UInt32)((Int32)(wmiValueArray.GetValue(i)));
                            break;
                        
                        case CimType.UInt64:
                            val = new UInt64 [length];
                            
                            for (int i = 0; i < length; i++)
                                ((UInt64[])val) [i] = Convert.ToUInt64((String)(wmiValueArray.GetValue(i)),(IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(System.UInt64)));
                            break;

                        case CimType.SInt8:
                            val = new SByte [length];
                            
                            for (int i = 0; i < length; i++)
                                ((SByte[])val) [i] = (SByte)((Int16)(wmiValueArray.GetValue(i)));
                            break;

                        case CimType.SInt64:
                            val = new Int64 [length];
                            
                            for (int i = 0; i < length; i++)
                                ((Int64[])val) [i] = Convert.ToInt64((String)(wmiValueArray.GetValue(i)),(IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(System.Int64)));
                            break;

                        case CimType.Char16:
                            val = new Char [length];
                            
                            for (int i = 0; i < length; i++)
                                ((Char[])val) [i] = (Char)((Int16)(wmiValueArray.GetValue(i)));
                            break;

                        case CimType.Object:
                            val = new ManagementBaseObject [length];

                            for (int i = 0; i < length; i++)
                                ((ManagementBaseObject[])val) [i] = new ManagementBaseObject(new IWbemClassObjectFreeThreaded(Marshal.GetIUnknownForObject(wmiValueArray.GetValue(i))));
                            break;
                        
                        default:
                            val = wmiValue;
                            break;
                    }
                }
                else
                {
                    switch (type)
                    {
                        case CimType.SInt8:
                            val = (SByte)((Int16)wmiValue);
                            break;

                        case CimType.UInt16:
                            val = (UInt16)((Int32)wmiValue);
                            break;

                        case CimType.UInt32:
                            val = (UInt32)((Int32)wmiValue);
                            break;
                        
                        case CimType.UInt64:
                            val = Convert.ToUInt64((String)wmiValue,(IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(System.UInt64)));
                            break;

                        case CimType.SInt64:
                            val = Convert.ToInt64((String)wmiValue,(IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(System.Int64)));
                            break;

                        case CimType.Char16:
                            val = (Char)((Int16)wmiValue);
                            break;

                        case CimType.Object:
                            val = new ManagementBaseObject(new IWbemClassObjectFreeThreaded(Marshal.GetIUnknownForObject(wmiValue)));
                            break;
                        
                        default:
                            val = wmiValue;
                            break;
                    }
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
                            wmiValue = new Int16 [length];
                            for (int i = 0; i < length; i++)
                                ((Int16[])(wmiValue))[i] = (Int16)Convert.ToSByte(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.SByte)));
                            break;

                        case CimType.UInt8: 
                            if (val is Byte[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new Byte [length];
                                for (int i = 0; i < length; i++)
                                    ((Byte[])wmiValue)[i] = Convert.ToByte(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Byte)));
                            }
                            break;

                        case CimType.SInt16:
                            if (val is Int16[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new Int16 [length];
                                for (int i = 0; i < length; i++)
                                    ((Int16[])(wmiValue))[i] = Convert.ToInt16(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Int16)));
                            }
                            break;

                        case CimType.UInt16:
                            wmiValue = new Int32 [length];
                            for (int i = 0; i < length; i++)
                                ((Int32[])(wmiValue))[i] = (Int32)(Convert.ToUInt16(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.UInt16))));
                            break;

                        case CimType.SInt32:
                            if (val is Int32[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new Int32 [length];
                                for (int i = 0; i < length; i++)
                                    ((Int32[])(wmiValue))[i] = Convert.ToInt32(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Int32)));
                            }				
                            break;

                        case CimType.UInt32:
                            wmiValue = new Int32 [length];
                            for (int i = 0; i < length; i++)
                                ((Int32[])(wmiValue))[i] = (Int32)(Convert.ToUInt32(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.UInt32))));
                            break;

                        case CimType.SInt64:
                            wmiValue = new String [length];
                            for (int i = 0; i < length; i++)
                                ((String[])(wmiValue))[i] = (Convert.ToInt64(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Int64)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.Int64)));
                            break;

                        case CimType.UInt64:
                            wmiValue = new String [length];
                            for (int i = 0; i < length; i++)
                                ((String[])(wmiValue))[i] = (Convert.ToUInt64(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.UInt64)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.UInt64)));
                            break;

                        case CimType.Real32:
                            if (val is Single[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new Single [length];
                                for (int i = 0; i < length; i++)
                                    ((Single[])(wmiValue))[i] = Convert.ToSingle(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Single)));
                            }				
                            break;

                        case CimType.Real64:
                            if (val is Double[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new Double [length];
                                for (int i = 0; i < length; i++)
                                    ((Double[])(wmiValue))[i] = Convert.ToDouble(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Double)));
                            }				
                            break;

                        case CimType.Char16: 
                            wmiValue = new Int16 [length];
                            for (int i = 0; i < length; i++)
                                ((Int16[])(wmiValue))[i] = (Int16)Convert.ToChar(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Char)));
                            break;

                        case CimType.String:
                        case CimType.DateTime:
                        case CimType.Reference:
                            if (val is String[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new String [length];
                                for (int i = 0; i < length; i++)
                                    ((String[])(wmiValue))[i] = (valArray.GetValue(i)).ToString();
                            }
                            break;

                        case CimType.Boolean:
                            if (val is Boolean[])
                                wmiValue = val;
                            else
                            {
                                wmiValue = new Boolean [length];
                                for (int i = 0; i < length; i++)
                                    ((Boolean[])(wmiValue))[i] = Convert.ToBoolean(valArray.GetValue(i),(IFormatProvider)culInfo.GetFormat(typeof(System.Boolean)));
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
                            wmiValue = (Int16)Convert.ToSByte(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Int16)));
                            break;

                        case CimType.UInt8:
                            wmiValue = Convert.ToByte(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Byte)));
                            break;

                        case CimType.SInt16:
                            wmiValue = Convert.ToInt16(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Int16))); 
                            break;

                        case CimType.UInt16:
                            wmiValue = (Int32)(Convert.ToUInt16(val,(IFormatProvider)culInfo.GetFormat(typeof(System.UInt16))));
                            break;

                        case CimType.SInt32:
                            wmiValue = Convert.ToInt32(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Int32))); 
                            break;

                        case CimType.UInt32:
                            wmiValue = (Int32)Convert.ToUInt32(val,(IFormatProvider)culInfo.GetFormat(typeof(System.UInt32)));
                            break;

                        case CimType.SInt64:
                            wmiValue = (Convert.ToInt64(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Int64)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.Int64)));
                            break;

                        case CimType.UInt64:
                            wmiValue = (Convert.ToUInt64(val,(IFormatProvider)culInfo.GetFormat(typeof(System.UInt64)))).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.UInt64)));
                            break;

                        case CimType.Real32:
                            wmiValue = Convert.ToSingle(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Single)));
                            break;

                        case CimType.Real64:
                            wmiValue = Convert.ToDouble(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Double)));
                            break;

                        case CimType.Char16:
                            wmiValue = (Int16)Convert.ToChar(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Char)));
                            break;

                        case CimType.String:
                        case CimType.DateTime:
                        case CimType.Reference:
                            wmiValue = val.ToString();
                            break;

                        case CimType.Boolean:
                            wmiValue = Convert.ToBoolean(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Boolean)));
                            break;

                        case CimType.Object:
                            if (val is ManagementBaseObject)
                            {
                                wmiValue = Marshal.GetObjectForIUnknown(((ManagementBaseObject) val).wbemObject);
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
                        if (elementType == typeof(System.Byte))
                        {
                            byte[] arrayValue = (byte[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt8;
                            wmiValue = new short[length];

                            for (int i = 0; i < length; i++)
                                ((short[])wmiValue) [i] = ((IConvertible)((System.Byte)(arrayValue[i]))).ToInt16(null);
                        }
                        else if (elementType == typeof(System.SByte))
                        {
                            sbyte[] arrayValue = (sbyte[])val;
                            int length = arrayValue.Length;
                            type = CimType.SInt8;
                            wmiValue = new short[length];

                            for (int i = 0; i < length; i++)
                                ((short[])wmiValue) [i] = ((IConvertible)((System.SByte)(arrayValue[i]))).ToInt16(null);
                        }
                        else if (elementType == typeof(System.Boolean))
                        {
                            type = CimType.Boolean;
                            wmiValue = (bool[])val;
                        }					
                        else if (elementType == typeof(System.UInt16))
                        {
                            ushort[] arrayValue = (ushort[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt16;
                            wmiValue = new int[length];

                            for (int i = 0; i < length; i++)
                                ((int[])wmiValue) [i] = ((IConvertible)((System.UInt16)(arrayValue[i]))).ToInt32(null);
                        }
                        else if (elementType == typeof(System.Int16))
                        {
                            type = CimType.SInt16;
                            wmiValue = (short[])val;
                        }
                        else if (elementType == typeof(System.Int32))
                        {
                            type = CimType.SInt32;
                            wmiValue = (int[])val;
                        }
                        else if (elementType == typeof(System.UInt32))
                        {
                            uint[] arrayValue = (uint[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt32;
                            wmiValue = new string[length];

                            for (int i = 0; i < length; i++)
                                ((string[])wmiValue) [i] = ((System.UInt32)(arrayValue[i])).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.UInt32)));
                        }
                        else if (elementType == typeof(System.UInt64))
                        {
                            ulong[] arrayValue = (ulong[])val;
                            int length = arrayValue.Length;
                            type = CimType.UInt64;
                            wmiValue = new string[length];

                            for (int i = 0; i < length; i++)
                                ((string[])wmiValue) [i] = ((System.UInt64)(arrayValue[i])).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.UInt64)));
                        }
                        else if (elementType == typeof(System.Int64))
                        {
                            long[] arrayValue = (long[])val;
                            int length = arrayValue.Length;
                            type = CimType.SInt64;
                            wmiValue = new string[length];

                            for (int i = 0; i < length; i++)
                                ((string[])wmiValue) [i] = ((long)(arrayValue[i])).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.Int64)));
                        }
                        else if (elementType == typeof(System.Single))
                        {
                            type = CimType.Real32;
                            wmiValue = (System.Single[])val;
                        }
                        else if (elementType == typeof(System.Double))
                        {
                            type = CimType.Real64;
                            wmiValue = (double[])val;
                        }
                        else if (elementType == typeof(System.Char))
                        {
                            char[] arrayValue = (char[])val;
                            int length = arrayValue.Length;
                            type = CimType.Char16;
                            wmiValue = new short[length];

                            for (int i = 0; i < length; i++)
                                ((short[])wmiValue) [i] = ((IConvertible)((System.Char)(arrayValue[i]))).ToInt16(null);
                        }
                    }
                    else
                    {
                        // Non-primitive types
                        if (elementType == typeof(System.String))
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
                else	// Non-array values
                {
                    if (valueType == typeof(System.UInt16))
                    {
                        type = CimType.UInt16;
                        wmiValue = ((IConvertible)((System.UInt16)val)).ToInt32(null);
                    }
                    else if (valueType == typeof(System.UInt32))
                    {
                        type = CimType.UInt32;
                        if (((System.UInt32)val & 0x80000000) != 0)
                            wmiValue = Convert.ToString(val,(IFormatProvider)culInfo.GetFormat(typeof(System.UInt32)));
                        else
                            wmiValue = Convert.ToInt32(val,(IFormatProvider)culInfo.GetFormat(typeof(System.Int32)));
                    }
                    else if (valueType == typeof(System.UInt64))
                    {
                        type = CimType.UInt64;
                        wmiValue = ((System.UInt64)val).ToString((IFormatProvider)culInfo.GetFormat(typeof(System.UInt64)));
                    }
                    else if (valueType == typeof(System.SByte))
                    {
                        type = CimType.SInt8;
                        wmiValue = ((IConvertible)((System.SByte)val)).ToInt16(null);
                    }
                    else if (valueType == typeof(System.Byte))
                    {
                        type = CimType.UInt8;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(System.Int16))
                    {
                        type = CimType.SInt16;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(System.Int32))
                    {
                        type = CimType.SInt32;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(System.Int64))
                    {
                        type = CimType.SInt64;
                        wmiValue = val.ToString();
                    }
                    else if (valueType == typeof(System.Boolean))
                    {
                        type = CimType.Boolean;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(System.Single))
                    {
                        type = CimType.Real32;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(System.Double))
                    {
                        type = CimType.Real64;
                        wmiValue = val;
                    }
                    else if (valueType == typeof(System.Char))
                    {
                        type = CimType.Char16;
                        wmiValue = ((IConvertible)((System.Char)val)).ToInt16(null);
                    }
                    else if (valueType == typeof(System.String))
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
                            wmiValue = Marshal.GetObjectForIUnknown(((ManagementBaseObject) val).wbemObject);
                        }
                    }
                }
            }

            return wmiValue;
        }

    }//PropertyData
}
