// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Extensions;


namespace System.Xml.Serialization.LegacyNetCF
{
    internal static class SerializationStrings
    {
        public static string ArrayOf = "ArrayOf";
        public static string Array = "Array";
        public static string ArrayItem = "Item";
    }




    internal enum InternalDateTimeSerializationMode
    {
        Unspecified = 0,
        Local = 1,
        RoundTrip = 2
    }

    internal static class SerializationHelper
    {
        private static Type[] s_emptyTypeArray = new Type[0];
        private static object[] s_emptyObjectArray = new object[0];
        private static string[] s_allDateTimeFormats = new string[] {
                                                                        "yyyy-MM-ddTHH:mm:ss.fffffffzzzzzz",
                                                                        "yyyy",
                                                                        "---dd",
                                                                        "---ddZ",
                                                                        "---ddzzzzzz",
                                                                        "--MM-dd",
                                                                        "--MM-ddZ",
                                                                        "--MM-ddzzzzzz",
                                                                        "--MM--",
                                                                        "--MM--Z",
                                                                        "--MM--zzzzzz",
                                                                        "yyyy-MM",
                                                                        "yyyy-MMZ",
                                                                        "yyyy-MMzzzzzz",
                                                                        "yyyyzzzzzz",
                                                                        "yyyy-MM-dd",
                                                                        "yyyy-MM-ddZ",
                                                                        "yyyy-MM-ddzzzzzz",

                                                                        "HH:mm:ss",
                                                                        "HH:mm:ss.f",
                                                                        "HH:mm:ss.ff",
                                                                        "HH:mm:ss.fff",
                                                                        "HH:mm:ss.ffff",
                                                                        "HH:mm:ss.fffff",
                                                                        "HH:mm:ss.ffffff",
                                                                        "HH:mm:ss.fffffff",
                                                                        "HH:mm:ssZ",
                                                                        "HH:mm:ss.fZ",
                                                                        "HH:mm:ss.ffZ",
                                                                        "HH:mm:ss.fffZ",
                                                                        "HH:mm:ss.ffffZ",
                                                                        "HH:mm:ss.fffffZ",
                                                                        "HH:mm:ss.ffffffZ",
                                                                        "HH:mm:ss.fffffffZ",
                                                                        "HH:mm:sszzzzzz",
                                                                        "HH:mm:ss.fzzzzzz",
                                                                        "HH:mm:ss.ffzzzzzz",
                                                                        "HH:mm:ss.fffzzzzzz",
                                                                        "HH:mm:ss.ffffzzzzzz",
                                                                        "HH:mm:ss.fffffzzzzzz",
                                                                        "HH:mm:ss.ffffffzzzzzz",
                                                                        "HH:mm:ss.fffffffzzzzzz",
                                                                        "yyyy-MM-ddTHH:mm:ss",
                                                                        "yyyy-MM-ddTHH:mm:ss.f",
                                                                        "yyyy-MM-ddTHH:mm:ss.ff",
                                                                        "yyyy-MM-ddTHH:mm:ss.fff",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffff",
                                                                        "yyyy-MM-ddTHH:mm:ss.fffff",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffffff",
                                                                        "yyyy-MM-ddTHH:mm:ss.fffffff",
                                                                        "yyyy-MM-ddTHH:mm:ssZ",
                                                                        "yyyy-MM-ddTHH:mm:ss.fZ",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffZ",
                                                                        "yyyy-MM-ddTHH:mm:ss.fffZ",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffffZ",
                                                                        "yyyy-MM-ddTHH:mm:ss.fffffZ",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffffffZ",
                                                                        "yyyy-MM-ddTHH:mm:ss.fffffffZ",
                                                                        "yyyy-MM-ddTHH:mm:sszzzzzz",
                                                                        "yyyy-MM-ddTHH:mm:ss.fzzzzzz",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffzzzzzz",
                                                                        "yyyy-MM-ddTHH:mm:ss.fffzzzzzz",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffffzzzzzz",
                                                                        "yyyy-MM-ddTHH:mm:ss.fffffzzzzzz",
                                                                        "yyyy-MM-ddTHH:mm:ss.ffffffzzzzzz",
        };

        private static string[] s_allDateFormats = new string[] {
                                                                    "yyyy-MM-ddzzzzzz",
                                                                    "yyyy-MM-dd",
                                                                    "yyyy-MM-ddZ",
                                                                    "yyyy",
                                                                    "---dd",
                                                                    "---ddZ",
                                                                    "---ddzzzzzz",
                                                                    "--MM-dd",
                                                                    "--MM-ddZ",
                                                                    "--MM-ddzzzzzz",
                                                                    "--MM--",
                                                                    "--MM--Z",
                                                                    "--MM--zzzzzz",
                                                                    "yyyy-MM",
                                                                    "yyyy-MMZ",
                                                                    "yyyy-MMzzzzzz",
                                                                    "yyyyzzzzzz",
        };

        private static string[] s_allTimeFormats = new string[] {
                                                                    "HH:mm:ss.fffffffzzzzzz",
                                                                    "HH:mm:ss",
                                                                    "HH:mm:ss.f",
                                                                    "HH:mm:ss.ff",
                                                                    "HH:mm:ss.fff",
                                                                    "HH:mm:ss.ffff",
                                                                    "HH:mm:ss.fffff",
                                                                    "HH:mm:ss.ffffff",
                                                                    "HH:mm:ss.fffffff",
                                                                    "HH:mm:ssZ",
                                                                    "HH:mm:ss.fZ",
                                                                    "HH:mm:ss.ffZ",
                                                                    "HH:mm:ss.fffZ",
                                                                    "HH:mm:ss.ffffZ",
                                                                    "HH:mm:ss.fffffZ",
                                                                    "HH:mm:ss.ffffffZ",
                                                                    "HH:mm:ss.fffffffZ",
                                                                    "HH:mm:sszzzzzz",
                                                                    "HH:mm:ss.fzzzzzz",
                                                                    "HH:mm:ss.ffzzzzzz",
                                                                    "HH:mm:ss.fffzzzzzz",
                                                                    "HH:mm:ss.ffffzzzzzz",
                                                                    "HH:mm:ss.fffffzzzzzz",
                                                                    "HH:mm:ss.ffffffzzzzzz",
        };

        // Creates an instance of a given type using the default, parameterless constructor
        public static object CreateInstance(Type type)
        {
            if (type == Globals.TypeOfDBNull)
            {
                FieldInfo DBNull_Value = Globals.TypeOfDBNull.GetField("Value", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return DBNull_Value.GetValue(null);
            }

            // Special case XElement
            // codegen the same as 'internal XElement : this("default") { }'
            if (type.FullName == "System.Xml.Linq.XElement")
            {
                Type xName = type.GetTypeInfo().Assembly.GetType("System.Xml.Linq.XName");
                if (xName != null)
                {
                    MethodInfo XName_op_Implicit = xName.GetMethod(
                        "op_Implicit",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        new Type[] { typeof(String) }
                        );
                    ConstructorInfo XElement_ctor = type.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        new Type[] { xName }
                        );
                    if (XName_op_Implicit != null && XElement_ctor != null)
                    {
                        //ilg.Ldstr( "default" );
                        //ilg.Call( XName_op_Implicit );
                        //ilg.New( XElement_ctor );
                        return XElement_ctor.Invoke(new object[] { XName_op_Implicit.Invoke(null, new object[] { "default" }) });
                    }
                }
            }

            try
            {
                ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, new Type[0]);
                LogicalType.ValidateSecurity(ctor);
                return Activator.CreateInstance(type);
            }
            catch (MissingMethodException)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlConstructorInaccessible, type));
            }
        }

        // Sets the value of a given member, either field or property, on a target object
        public static void SetValue(object target, MemberInfo member, object value)
        {
            if (null == target || null == member)
                return;

            if (member is PropertyInfo)
            {
                ((PropertyInfo)member).SetValue(target, value, null);
            }
            else
            {
                ((FieldInfo)member).SetValue(target, value);
            }
        }

        // Gets the value of a given member, either field or property, on a target object
        public static object GetValue(object src, MemberInfo member)
        {
            if (null == src || null == member)
                return null;

            if (member is FieldInfo)
            {
                FieldInfo f = (FieldInfo)member;
                return f.GetValue(src);
            }
            else
            {
                PropertyInfo p = (PropertyInfo)member;
                return p.GetValue(src, null);
            }
        }

        // True is the type is a ArrayLike. An ArrayLike is an Array, ICollection, or IEnumerable
        public static bool isLogicalArray(LogicalType type)
        {
            return type != null && type.TypeID == TypeID.ArrayLike;
        }

        public static bool isBuiltInBinary(LogicalType type)
        {
            return type != null && type.Type == typeof(byte[]) && type.TypeID == TypeID.Compound && type.Serializer == SerializerType.Custom;
        }

        // True if the type is an array.         
        public static bool isArray(Type type)
        {
            return typeof(Array).IsAssignableFrom(type);
        }

        // True if the type is an implementation of ICollection.         
        public static bool isCollection(Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type) && !IsSerializationPrimitive(type) && !typeof(XmlNode).IsAssignableFrom(type);
        }


        // True if the type is an implementation of IEnumerable.         
        public static bool isEnumerable(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) && !IsSerializationPrimitive(type) && !typeof(XmlNode).IsAssignableFrom(type);
        }


        // Returns the default value for a give type. If the type is an
        // enum then zero is returned. If the type is  primitive then the
        // default value of the primitive type, determined by default(Type),
        // is returned, and if the type is a value type then use create a new
        // instance of the value type using the Activator. The default(Type) 
        // expressions is a constant expression since the Type is a primitive
        // type. 
        public static object GetDefaultValue(Type type)
        {
            if (type.GetTypeInfo().IsPrimitive)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Boolean: return default(bool);
                    case TypeCode.Byte: return default(byte);
                    case TypeCode.Char: return default(char);
                    case TypeCode.Decimal: return default(decimal);
                    case TypeCode.Double: return default(double);
                    case TypeCode.Int16: return default(short);
                    case TypeCode.Int32: return default(int);
                    case TypeCode.Int64: return default(long);
                    case TypeCode.SByte: return default(sbyte);
                    case TypeCode.Single: return default(float);
                    case TypeCode.UInt16: return default(ushort);
                    case TypeCode.UInt32: return default(uint);
                    case TypeCode.UInt64: return default(ulong);
                }
            }

            if (type.GetTypeInfo().IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        // Determine if a given Type represents a primitive. This is not
        // equivalent to Type.GetTypeInfo().IsPrimitive, since we interpret strings, Guids,
        // DateTimes, and decimals as primitives as well. 
        public static bool IsSerializationPrimitive(Type type)
        {
            if (type.GetTypeInfo().IsPrimitive)
                return true;
            switch (type.GetTypeCode())
            {
                case TypeCode.String:
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                    return true;
            }
            if (type == typeof(Guid))
                return true;
            return false;
        }

        public static object StringToEnumeration(LogicalType type, string value)
        {
            LogicalEnum enumeration = (LogicalEnum)type;
            return enumeration.findValue(value);
        }

        public static string EnumerationToString(LogicalType serializeAs, object value)
        {
            LogicalEnum enumerated = (LogicalEnum)serializeAs;
            return enumerated.findName(value);
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Xml.Serialization.XmlSerializationConfig.GetDateTimeSerializationMode(System.Int32@)",
            Justification = "We don't need to check the result because failure is also fed into the mode variable.")]
        private static int DateTimeSerializationMode
        {
            get
            {
#if !FEATURE_LEGACYNETCF
                int mode;
                XmlSerializationConfig.GetDateTimeSerializationMode( out mode );
                return mode;
#endif
                return (int)InternalDateTimeSerializationMode.Unspecified;
            }
        }

        public static string PrimitiveToString(LogicalType serializeAs, object value)
        {
            if (value == null)
            {
                //null values won't cast, but Convert can handle them.
                value = Convert.ChangeType(value, serializeAs.Type, null);
            }
            DateTime dt;
            switch (serializeAs.TypeID)
            {
                case TypeID.Boolean:
                    return XmlConvert.ToString((bool)value);
                case TypeID.Byte:
                    return XmlConvert.ToString((byte)value);
                case TypeID.Char:
                    return XmlConvert.ToString((ushort)(char)value);
                case TypeID.Decimal:
                    return XmlConvert.ToString((Decimal)value);
                case TypeID.Double:
                    return XmlConvert.ToString((double)value);
                case TypeID.Int16:
                    return XmlConvert.ToString((short)value);
                case TypeID.Int32:
                    return XmlConvert.ToString((int)value);
                case TypeID.Int64:
                    return XmlConvert.ToString((long)value);
                case TypeID.SByte:
                    return XmlConvert.ToString((sbyte)value);
                case TypeID.Single:
                    return XmlConvert.ToString((float)value);
                case TypeID.UInt16:
                    return XmlConvert.ToString((ushort)value);
                case TypeID.UInt32:
                    return XmlConvert.ToString((uint)value);
                case TypeID.UInt64:
                    return XmlConvert.ToString((ulong)value);
                case TypeID.String:
                    return (string)value;
                case TypeID.DateTime:
                    dt = (DateTime)value;
                    if (DateTimeSerializationMode == (int)InternalDateTimeSerializationMode.Local)
                    {
                        // If we're in backward-compatibility mode (saying we only serialize Local), then 
                        // we effectively cut off the detail about the time zone the DateTime is in.
                        return XmlConvert.ToString(dt, "yyyy-MM-ddTHH:mm:ss.fffffffzzzzzz");
                    }
                    else
                    {
                        // for mode DateTimeSerializationMode.Roundtrip and DateTimeSerializationMode.Default
                        return XmlConvert.ToString(dt, XmlDateTimeSerializationMode.RoundtripKind);
                    }
                case TypeID.Date:
                    dt = (DateTime)value;
                    return XmlConvert.ToString(dt, "yyyy-MM-dd");
                case TypeID.Time:
                    dt = (DateTime)value;
                    return XmlConvert.ToString(DateTime.MinValue + dt.TimeOfDay, "HH:mm:ss.fffffffzzzzzz");
                case TypeID.Guid:
                    return XmlConvert.ToString((Guid)value);
                default:
                    Debug.WriteLine("Unknown primitive type" + serializeAs.Type);
                    throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, serializeAs.Type));
            }
        }

        public static object StringToPrimitive(LogicalType type, string value)
        {
            switch (type.TypeID)
            {
                case TypeID.Boolean:
                    return XmlConvert.ToBoolean(value);
                case TypeID.Byte:
                    return XmlConvert.ToByte(value);
                case TypeID.Char:
                    return (char)XmlConvert.ToUInt16(value);
                case TypeID.Decimal:
                    return XmlConvert.ToDecimal(value);
                case TypeID.Double:
                    return XmlConvert.ToDouble(value);
                case TypeID.Int16:
                    return XmlConvert.ToInt16(value);
                case TypeID.Int32:
                    return XmlConvert.ToInt32(value);
                case TypeID.Int64:
                    return XmlConvert.ToInt64(value);
                case TypeID.SByte:
                    return XmlConvert.ToSByte(value);
                case TypeID.Single:
                    return XmlConvert.ToSingle(value);
                case TypeID.UInt16:
                    return XmlConvert.ToUInt16(value);
                case TypeID.UInt32:
                    return XmlConvert.ToUInt32(value);
                case TypeID.UInt64:
                    return XmlConvert.ToUInt64(value);
                case TypeID.String:
                    return value;
                case TypeID.DateTime:
                    if (DateTimeSerializationMode == (int)InternalDateTimeSerializationMode.Local)
                    {
                        return XmlConvert.ToDateTimeOffset(value, s_allDateTimeFormats).DateTime;
                    }
                    else
                    {
                        // for mode DateTimeSerializationMode.Roundtrip and DateTimeSerializationMode.Default
                        return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
                    }
                case TypeID.Date:
                    return XmlConvert.ToDateTimeOffset(value, s_allDateFormats).DateTime;
                case TypeID.Time:
                    return XmlConvert.ToDateTimeOffset(value, s_allTimeFormats).DateTime;
                case TypeID.Guid:
                    return XmlConvert.ToGuid(value);
                default:
                    Debug.Assert(false, "unknown primitive type " + type.Type);
                    throw new InvalidOperationException();
            }
        }
    }
}
