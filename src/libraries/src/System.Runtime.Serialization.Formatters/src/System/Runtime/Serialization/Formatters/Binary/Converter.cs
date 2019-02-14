// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Globalization;
using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal static class Converter
    {
        internal static readonly Type s_typeofISerializable = typeof(ISerializable);
        internal static readonly Type s_typeofString = typeof(string);
        internal static readonly Type s_typeofConverter = typeof(Converter);
        internal static readonly Type s_typeofBoolean = typeof(bool);
        internal static readonly Type s_typeofByte = typeof(byte);
        internal static readonly Type s_typeofChar = typeof(char);
        internal static readonly Type s_typeofDecimal = typeof(decimal);
        internal static readonly Type s_typeofDouble = typeof(double);
        internal static readonly Type s_typeofInt16 = typeof(short);
        internal static readonly Type s_typeofInt32 = typeof(int);
        internal static readonly Type s_typeofInt64 = typeof(long);
        internal static readonly Type s_typeofSByte = typeof(sbyte);
        internal static readonly Type s_typeofSingle = typeof(float);
        internal static readonly Type s_typeofTimeSpan = typeof(TimeSpan);
        internal static readonly Type s_typeofDateTime = typeof(DateTime);
        internal static readonly Type s_typeofUInt16 = typeof(ushort);
        internal static readonly Type s_typeofUInt32 = typeof(uint);
        internal static readonly Type s_typeofUInt64 = typeof(ulong);
        internal static readonly Type s_typeofObject = typeof(object);
        internal static readonly Type s_typeofSystemVoid = typeof(void);

        // In netfx the default assembly is mscorlib.dll --> typeof(string).Assembly.
        // In Core type string lives in System.Private.Corelib.dll which doesn't 
        // contain all the types which are living in mscorlib in netfx. Therefore we
        // use our mscorlib facade which also contains manual type forwards for deserialization.
        internal static readonly Assembly s_urtAssembly = Assembly.Load("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        internal static readonly string s_urtAssemblyString = s_urtAssembly.FullName;

        internal static readonly Assembly s_urtAlternativeAssembly = s_typeofString.Assembly;
        internal static readonly string s_urtAlternativeAssemblyString = s_urtAlternativeAssembly.FullName;

        // Arrays
        internal static readonly Type s_typeofTypeArray = typeof(Type[]);
        internal static readonly Type s_typeofObjectArray = typeof(object[]);
        internal static readonly Type s_typeofStringArray = typeof(string[]);
        internal static readonly Type s_typeofBooleanArray = typeof(bool[]);
        internal static readonly Type s_typeofByteArray = typeof(byte[]);
        internal static readonly Type s_typeofCharArray = typeof(char[]);
        internal static readonly Type s_typeofDecimalArray = typeof(decimal[]);
        internal static readonly Type s_typeofDoubleArray = typeof(double[]);
        internal static readonly Type s_typeofInt16Array = typeof(short[]);
        internal static readonly Type s_typeofInt32Array = typeof(int[]);
        internal static readonly Type s_typeofInt64Array = typeof(long[]);
        internal static readonly Type s_typeofSByteArray = typeof(sbyte[]);
        internal static readonly Type s_typeofSingleArray = typeof(float[]);
        internal static readonly Type s_typeofTimeSpanArray = typeof(TimeSpan[]);
        internal static readonly Type s_typeofDateTimeArray = typeof(DateTime[]);
        internal static readonly Type s_typeofUInt16Array = typeof(ushort[]);
        internal static readonly Type s_typeofUInt32Array = typeof(uint[]);
        internal static readonly Type s_typeofUInt64Array = typeof(ulong[]);
        internal static readonly Type s_typeofMarshalByRefObject = typeof(MarshalByRefObject);

        private const int PrimitiveTypeEnumLength = 17; //Number of PrimitiveTypeEnums

        private static volatile Type[] s_typeA;
        private static volatile Type[] s_arrayTypeA;
        private static volatile string[] s_valueA;
        private static volatile TypeCode[] s_typeCodeA;
        private static volatile InternalPrimitiveTypeE[] s_codeA;

        internal static InternalPrimitiveTypeE ToCode(Type type) =>
                type == null ? ToPrimitiveTypeEnum(TypeCode.Empty) :
                type.IsPrimitive ? ToPrimitiveTypeEnum(Type.GetTypeCode(type)) :
                ReferenceEquals(type, s_typeofDateTime) ? InternalPrimitiveTypeE.DateTime :
                ReferenceEquals(type, s_typeofTimeSpan) ? InternalPrimitiveTypeE.TimeSpan :
                ReferenceEquals(type, s_typeofDecimal) ? InternalPrimitiveTypeE.Decimal :
                InternalPrimitiveTypeE.Invalid;

        internal static bool IsWriteAsByteArray(InternalPrimitiveTypeE code)
        {
            switch (code)
            {
                case InternalPrimitiveTypeE.Boolean:
                case InternalPrimitiveTypeE.Char:
                case InternalPrimitiveTypeE.Byte:
                case InternalPrimitiveTypeE.Double:
                case InternalPrimitiveTypeE.Int16:
                case InternalPrimitiveTypeE.Int32:
                case InternalPrimitiveTypeE.Int64:
                case InternalPrimitiveTypeE.SByte:
                case InternalPrimitiveTypeE.Single:
                case InternalPrimitiveTypeE.UInt16:
                case InternalPrimitiveTypeE.UInt32:
                case InternalPrimitiveTypeE.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        internal static int TypeLength(InternalPrimitiveTypeE code)
        {
            switch (code)
            {
                case InternalPrimitiveTypeE.Boolean: return 1;
                case InternalPrimitiveTypeE.Char: return 2;
                case InternalPrimitiveTypeE.Byte: return 1;
                case InternalPrimitiveTypeE.Double: return 8;
                case InternalPrimitiveTypeE.Int16: return 2;
                case InternalPrimitiveTypeE.Int32: return 4;
                case InternalPrimitiveTypeE.Int64: return 8;
                case InternalPrimitiveTypeE.SByte: return 1;
                case InternalPrimitiveTypeE.Single: return 4;
                case InternalPrimitiveTypeE.UInt16: return 2;
                case InternalPrimitiveTypeE.UInt32: return 4;
                case InternalPrimitiveTypeE.UInt64: return 8;
                default: return 0;
            }
        }

        internal static InternalNameSpaceE GetNameSpaceEnum(InternalPrimitiveTypeE code, Type type, WriteObjectInfo objectInfo, out string typeName)
        {
            InternalNameSpaceE nameSpaceEnum = InternalNameSpaceE.None;
            typeName = null;

            if (code != InternalPrimitiveTypeE.Invalid)
            {
                switch (code)
                {
                    case InternalPrimitiveTypeE.Boolean:
                    case InternalPrimitiveTypeE.Char:
                    case InternalPrimitiveTypeE.Byte:
                    case InternalPrimitiveTypeE.Double:
                    case InternalPrimitiveTypeE.Int16:
                    case InternalPrimitiveTypeE.Int32:
                    case InternalPrimitiveTypeE.Int64:
                    case InternalPrimitiveTypeE.SByte:
                    case InternalPrimitiveTypeE.Single:
                    case InternalPrimitiveTypeE.UInt16:
                    case InternalPrimitiveTypeE.UInt32:
                    case InternalPrimitiveTypeE.UInt64:
                    case InternalPrimitiveTypeE.DateTime:
                    case InternalPrimitiveTypeE.TimeSpan:
                        nameSpaceEnum = InternalNameSpaceE.XdrPrimitive;
                        typeName = "System." + ToComType(code);
                        break;

                    case InternalPrimitiveTypeE.Decimal:
                        nameSpaceEnum = InternalNameSpaceE.UrtSystem;
                        typeName = "System." + ToComType(code);
                        break;
                }
            }

            if ((nameSpaceEnum == InternalNameSpaceE.None) && type != null)
            {
                if (ReferenceEquals(type, s_typeofString))
                {
                    nameSpaceEnum = InternalNameSpaceE.XdrString;
                }
                else
                {
                    if (objectInfo == null)
                    {
                        typeName = type.FullName;
                        nameSpaceEnum = type.Assembly == s_urtAssembly ? InternalNameSpaceE.UrtSystem : InternalNameSpaceE.UrtUser;
                    }
                    else
                    {
                        typeName = objectInfo.GetTypeFullName();
                        nameSpaceEnum = objectInfo.GetAssemblyString().Equals(s_urtAssemblyString) ? InternalNameSpaceE.UrtSystem : InternalNameSpaceE.UrtUser;
                    }
                }
            }

            return nameSpaceEnum;
        }

        internal static Type ToArrayType(InternalPrimitiveTypeE code)
        {
            if (s_arrayTypeA == null)
            {
                InitArrayTypeA();
            }
            return s_arrayTypeA[(int)code];
        }

        private static void InitTypeA()
        {
            var typeATemp = new Type[PrimitiveTypeEnumLength];
            typeATemp[(int)InternalPrimitiveTypeE.Invalid] = null;
            typeATemp[(int)InternalPrimitiveTypeE.Boolean] = s_typeofBoolean;
            typeATemp[(int)InternalPrimitiveTypeE.Byte] = s_typeofByte;
            typeATemp[(int)InternalPrimitiveTypeE.Char] = s_typeofChar;
            typeATemp[(int)InternalPrimitiveTypeE.Decimal] = s_typeofDecimal;
            typeATemp[(int)InternalPrimitiveTypeE.Double] = s_typeofDouble;
            typeATemp[(int)InternalPrimitiveTypeE.Int16] = s_typeofInt16;
            typeATemp[(int)InternalPrimitiveTypeE.Int32] = s_typeofInt32;
            typeATemp[(int)InternalPrimitiveTypeE.Int64] = s_typeofInt64;
            typeATemp[(int)InternalPrimitiveTypeE.SByte] = s_typeofSByte;
            typeATemp[(int)InternalPrimitiveTypeE.Single] = s_typeofSingle;
            typeATemp[(int)InternalPrimitiveTypeE.TimeSpan] = s_typeofTimeSpan;
            typeATemp[(int)InternalPrimitiveTypeE.DateTime] = s_typeofDateTime;
            typeATemp[(int)InternalPrimitiveTypeE.UInt16] = s_typeofUInt16;
            typeATemp[(int)InternalPrimitiveTypeE.UInt32] = s_typeofUInt32;
            typeATemp[(int)InternalPrimitiveTypeE.UInt64] = s_typeofUInt64;
            s_typeA = typeATemp;
        }
        
        private static void InitArrayTypeA()
        {
            var arrayTypeATemp = new Type[PrimitiveTypeEnumLength];
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Invalid] = null;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Boolean] = s_typeofBooleanArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Byte] = s_typeofByteArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Char] = s_typeofCharArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Decimal] = s_typeofDecimalArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Double] = s_typeofDoubleArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Int16] = s_typeofInt16Array;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Int32] = s_typeofInt32Array;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Int64] = s_typeofInt64Array;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.SByte] = s_typeofSByteArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.Single] = s_typeofSingleArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.TimeSpan] = s_typeofTimeSpanArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.DateTime] = s_typeofDateTimeArray;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.UInt16] = s_typeofUInt16Array;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.UInt32] = s_typeofUInt32Array;
            arrayTypeATemp[(int)InternalPrimitiveTypeE.UInt64] = s_typeofUInt64Array;
            s_arrayTypeA = arrayTypeATemp;
        }

        internal static Type ToType(InternalPrimitiveTypeE code)
        {
            if (s_typeA == null)
            {
                InitTypeA();
            }
            return s_typeA[(int)code];
        }

        internal static Array CreatePrimitiveArray(InternalPrimitiveTypeE code, int length)
        {
            switch (code)
            {
                case InternalPrimitiveTypeE.Boolean: return new bool[length];
                case InternalPrimitiveTypeE.Byte: return new byte[length];
                case InternalPrimitiveTypeE.Char: return new char[length];
                case InternalPrimitiveTypeE.Decimal: return new decimal[length];
                case InternalPrimitiveTypeE.Double: return new double[length];
                case InternalPrimitiveTypeE.Int16: return new short[length];
                case InternalPrimitiveTypeE.Int32: return new int[length];
                case InternalPrimitiveTypeE.Int64: return new long[length];
                case InternalPrimitiveTypeE.SByte: return new sbyte[length];
                case InternalPrimitiveTypeE.Single: return new float[length];
                case InternalPrimitiveTypeE.TimeSpan: return new TimeSpan[length];
                case InternalPrimitiveTypeE.DateTime: return new DateTime[length];
                case InternalPrimitiveTypeE.UInt16: return new ushort[length];
                case InternalPrimitiveTypeE.UInt32: return new uint[length];
                case InternalPrimitiveTypeE.UInt64: return new ulong[length];
                default: return null;
            }
        }

        internal static bool IsPrimitiveArray(Type type, out object typeInformation)
        {
            bool bIsPrimitive = true;

            if (ReferenceEquals(type, s_typeofBooleanArray)) typeInformation = InternalPrimitiveTypeE.Boolean;
            else if (ReferenceEquals(type, s_typeofByteArray)) typeInformation = InternalPrimitiveTypeE.Byte;
            else if (ReferenceEquals(type, s_typeofCharArray)) typeInformation = InternalPrimitiveTypeE.Char;
            else if (ReferenceEquals(type, s_typeofDoubleArray)) typeInformation = InternalPrimitiveTypeE.Double;
            else if (ReferenceEquals(type, s_typeofInt16Array)) typeInformation = InternalPrimitiveTypeE.Int16;
            else if (ReferenceEquals(type, s_typeofInt32Array)) typeInformation = InternalPrimitiveTypeE.Int32;
            else if (ReferenceEquals(type, s_typeofInt64Array)) typeInformation = InternalPrimitiveTypeE.Int64;
            else if (ReferenceEquals(type, s_typeofSByteArray)) typeInformation = InternalPrimitiveTypeE.SByte;
            else if (ReferenceEquals(type, s_typeofSingleArray)) typeInformation = InternalPrimitiveTypeE.Single;
            else if (ReferenceEquals(type, s_typeofUInt16Array)) typeInformation = InternalPrimitiveTypeE.UInt16;
            else if (ReferenceEquals(type, s_typeofUInt32Array)) typeInformation = InternalPrimitiveTypeE.UInt32;
            else if (ReferenceEquals(type, s_typeofUInt64Array)) typeInformation = InternalPrimitiveTypeE.UInt64;
            else
            {
                typeInformation = null;
                bIsPrimitive = false;
            }

            return bIsPrimitive;
        }

        private static void InitValueA()
        {
            var valueATemp = new string[PrimitiveTypeEnumLength];
            valueATemp[(int)InternalPrimitiveTypeE.Invalid] = null;
            valueATemp[(int)InternalPrimitiveTypeE.Boolean] = "Boolean";
            valueATemp[(int)InternalPrimitiveTypeE.Byte] = "Byte";
            valueATemp[(int)InternalPrimitiveTypeE.Char] = "Char";
            valueATemp[(int)InternalPrimitiveTypeE.Decimal] = "Decimal";
            valueATemp[(int)InternalPrimitiveTypeE.Double] = "Double";
            valueATemp[(int)InternalPrimitiveTypeE.Int16] = "Int16";
            valueATemp[(int)InternalPrimitiveTypeE.Int32] = "Int32";
            valueATemp[(int)InternalPrimitiveTypeE.Int64] = "Int64";
            valueATemp[(int)InternalPrimitiveTypeE.SByte] = "SByte";
            valueATemp[(int)InternalPrimitiveTypeE.Single] = "Single";
            valueATemp[(int)InternalPrimitiveTypeE.TimeSpan] = "TimeSpan";
            valueATemp[(int)InternalPrimitiveTypeE.DateTime] = "DateTime";
            valueATemp[(int)InternalPrimitiveTypeE.UInt16] = "UInt16";
            valueATemp[(int)InternalPrimitiveTypeE.UInt32] = "UInt32";
            valueATemp[(int)InternalPrimitiveTypeE.UInt64] = "UInt64";
            s_valueA = valueATemp;
        }

        internal static string ToComType(InternalPrimitiveTypeE code)
        {
            if (s_valueA == null)
            {
                InitValueA();
            }
            return s_valueA[(int)code];
        }

        private static void InitTypeCodeA()
        {
            var typeCodeATemp = new TypeCode[PrimitiveTypeEnumLength];
            typeCodeATemp[(int)InternalPrimitiveTypeE.Invalid] = TypeCode.Object;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Boolean] = TypeCode.Boolean;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Byte] = TypeCode.Byte;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Char] = TypeCode.Char;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Decimal] = TypeCode.Decimal;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Double] = TypeCode.Double;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Int16] = TypeCode.Int16;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Int32] = TypeCode.Int32;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Int64] = TypeCode.Int64;
            typeCodeATemp[(int)InternalPrimitiveTypeE.SByte] = TypeCode.SByte;
            typeCodeATemp[(int)InternalPrimitiveTypeE.Single] = TypeCode.Single;
            typeCodeATemp[(int)InternalPrimitiveTypeE.TimeSpan] = TypeCode.Object;
            typeCodeATemp[(int)InternalPrimitiveTypeE.DateTime] = TypeCode.DateTime;
            typeCodeATemp[(int)InternalPrimitiveTypeE.UInt16] = TypeCode.UInt16;
            typeCodeATemp[(int)InternalPrimitiveTypeE.UInt32] = TypeCode.UInt32;
            typeCodeATemp[(int)InternalPrimitiveTypeE.UInt64] = TypeCode.UInt64;
            s_typeCodeA = typeCodeATemp;
        }

        // Returns a System.TypeCode from a InternalPrimitiveTypeE
        internal static TypeCode ToTypeCode(InternalPrimitiveTypeE code)
        {
            if (s_typeCodeA == null)
            {
                InitTypeCodeA();
            }
            return s_typeCodeA[(int)code];
        }

        private static void InitCodeA()
        {
            var codeATemp = new InternalPrimitiveTypeE[19];
            codeATemp[(int)TypeCode.Empty] = InternalPrimitiveTypeE.Invalid;
            codeATemp[(int)TypeCode.Object] = InternalPrimitiveTypeE.Invalid;
            codeATemp[(int)TypeCode.DBNull] = InternalPrimitiveTypeE.Invalid;
            codeATemp[(int)TypeCode.Boolean] = InternalPrimitiveTypeE.Boolean;
            codeATemp[(int)TypeCode.Char] = InternalPrimitiveTypeE.Char;
            codeATemp[(int)TypeCode.SByte] = InternalPrimitiveTypeE.SByte;
            codeATemp[(int)TypeCode.Byte] = InternalPrimitiveTypeE.Byte;
            codeATemp[(int)TypeCode.Int16] = InternalPrimitiveTypeE.Int16;
            codeATemp[(int)TypeCode.UInt16] = InternalPrimitiveTypeE.UInt16;
            codeATemp[(int)TypeCode.Int32] = InternalPrimitiveTypeE.Int32;
            codeATemp[(int)TypeCode.UInt32] = InternalPrimitiveTypeE.UInt32;
            codeATemp[(int)TypeCode.Int64] = InternalPrimitiveTypeE.Int64;
            codeATemp[(int)TypeCode.UInt64] = InternalPrimitiveTypeE.UInt64;
            codeATemp[(int)TypeCode.Single] = InternalPrimitiveTypeE.Single;
            codeATemp[(int)TypeCode.Double] = InternalPrimitiveTypeE.Double;
            codeATemp[(int)TypeCode.Decimal] = InternalPrimitiveTypeE.Decimal;
            codeATemp[(int)TypeCode.DateTime] = InternalPrimitiveTypeE.DateTime;
            codeATemp[17] = InternalPrimitiveTypeE.Invalid;
            codeATemp[(int)TypeCode.String] = InternalPrimitiveTypeE.Invalid;
            s_codeA = codeATemp;
        }

        // Returns a InternalPrimitiveTypeE from a System.TypeCode
        internal static InternalPrimitiveTypeE ToPrimitiveTypeEnum(TypeCode typeCode)
        {
            if (s_codeA == null)
            {
                InitCodeA();
            }
            return s_codeA[(int)typeCode];
        }

        // Translates a string into an Object
        internal static object FromString(string value, InternalPrimitiveTypeE code)
        {
            // InternalPrimitiveTypeE needs to be a primitive type
            Debug.Assert((code != InternalPrimitiveTypeE.Invalid), "[Converter.FromString]!InternalPrimitiveTypeE.Invalid ");
            return code != InternalPrimitiveTypeE.Invalid ?
                Convert.ChangeType(value, ToTypeCode(code), CultureInfo.InvariantCulture) :
                value;
        }
    }
}
