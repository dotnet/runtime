// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if NATIVEAOT
using System.Reflection.Runtime.EventInfos;
using System.Reflection.Runtime.FieldInfos;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.Modules;
using System.Reflection.Runtime.PropertyInfos;

using Internal.Metadata.NativeFormat;

using IRuntimeMethodInfo = System.Reflection.Runtime.MethodInfos.RuntimeConstructorInfo;
using ResolutionScope = Internal.Metadata.NativeFormat.MetadataReader;
#else
using ResolutionScope = System.Reflection.RuntimeModule;
#endif

namespace System.Reflection
{
    internal sealed partial class RuntimeCustomAttributeData : CustomAttributeData
    {
        #region Internal Static Members
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeType target)
        {
            Debug.Assert(target is not null);

            IList<CustomAttributeData> cad = GetCustomAttributes(new QCustomAttributeList(target));
            ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeFieldInfo target)
        {
            Debug.Assert(target is not null);

            IList<CustomAttributeData> cad = GetCustomAttributes(new QCustomAttributeList(target));
            ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeMethodInfo target)
        {
            Debug.Assert(target is not null);

            IList<CustomAttributeData> cad = GetCustomAttributes(new QCustomAttributeList(target));
            ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeConstructorInfo target)
        {
            Debug.Assert(target is not null);

            return GetCustomAttributes(new QCustomAttributeList(target));
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeEventInfo target)
        {
            Debug.Assert(target is not null);

            return GetCustomAttributes(new QCustomAttributeList(target));
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimePropertyInfo target)
        {
            Debug.Assert(target is not null);

            return GetCustomAttributes(new QCustomAttributeList(target));
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeModule target)
        {
            Debug.Assert(target is not null);

            if (target.IsResource())
                return new List<CustomAttributeData>();

            return GetCustomAttributes(new QCustomAttributeList(target));
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeAssembly target)
        {
            Debug.Assert(target is not null);

            // No pseudo attributes for RuntimeAssembly

            return GetCustomAttributes(new QCustomAttributeList(target));
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeParameterInfo target)
        {
            Debug.Assert(target is not null);

            ListBuilder<Attribute> pcas = default;
            IList<CustomAttributeData> cad = GetCustomAttributes(new QCustomAttributeList(target));
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        private static ReadOnlyCollection<CustomAttributeData> GetCombinedList(IList<CustomAttributeData> customAttributes, ref ListBuilder<Attribute> pseudoAttributes)
        {
            Debug.Assert(pseudoAttributes.Count != 0);

            CustomAttributeData[] pca = new CustomAttributeData[customAttributes.Count + pseudoAttributes.Count];
            customAttributes.CopyTo(pca, pseudoAttributes.Count);
            for (int i = 0; i < pseudoAttributes.Count; i++)
            {
                pca[i] = new RuntimeCustomAttributeData(pseudoAttributes[i]);
            }

            return Array.AsReadOnly(pca);
        }
        #endregion

        internal static CustomAttributeEncoding TypeToCustomAttributeEncoding(RuntimeType type)
        {
            if (type == typeof(int))
                return CustomAttributeEncoding.Int32;

            if (type.IsActualEnum)
                return CustomAttributeEncoding.Enum;

            if (type == typeof(string))
                return CustomAttributeEncoding.String;

            if (type == typeof(Type))
                return CustomAttributeEncoding.Type;

            if (type == typeof(object))
                return CustomAttributeEncoding.Object;

            if (type.IsArray)
                return CustomAttributeEncoding.Array;

            if (type == typeof(char))
                return CustomAttributeEncoding.Char;

            if (type == typeof(bool))
                return CustomAttributeEncoding.Boolean;

            if (type == typeof(byte))
                return CustomAttributeEncoding.Byte;

            if (type == typeof(sbyte))
                return CustomAttributeEncoding.SByte;

            if (type == typeof(short))
                return CustomAttributeEncoding.Int16;

            if (type == typeof(ushort))
                return CustomAttributeEncoding.UInt16;

            if (type == typeof(uint))
                return CustomAttributeEncoding.UInt32;

            if (type == typeof(long))
                return CustomAttributeEncoding.Int64;

            if (type == typeof(ulong))
                return CustomAttributeEncoding.UInt64;

            if (type == typeof(float))
                return CustomAttributeEncoding.Float;

            if (type == typeof(double))
                return CustomAttributeEncoding.Double;

            // System.Enum is neither an Enum nor a Class
            if (type == typeof(Enum))
                return CustomAttributeEncoding.Object;

            if (type.IsClass)
                return CustomAttributeEncoding.Object;

            if (type.IsActualInterface)
                return CustomAttributeEncoding.Object;

            if (type.IsActualValueType)
                return CustomAttributeEncoding.Undefined;

            throw new ArgumentException(SR.Argument_InvalidKindOfTypeForCA, nameof(type));
        }

        private ConstructorInfo m_ctor = null!;
        private readonly ResolutionScope m_scope = null!;
        private readonly CustomAttributeCtorParameter[] m_ctorParams = null!;
        private readonly CustomAttributeNamedParameter[] m_namedParams = null!;
        private IList<CustomAttributeTypedArgument> m_typedCtorArgs = null!;
        private IList<CustomAttributeNamedArgument> m_namedArgs = null!;

        #region Constructor
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Property setters and fields which are accessed by any attribute instantiation which is present in the code linker has analyzed." +
                            "As such enumerating all fields and properties may return different results after trimming" +
                            "but all those which are needed to actually have data will be there.")]
#if NATIVEAOT
        internal RuntimeCustomAttributeData(ResolutionScope reader, CustomAttributeHandle customAttributeHandle)
#else
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
            Justification = "We're getting a MethodBase of a constructor that we found in the metadata. The attribute constructor won't be trimmed.")]
        private RuntimeCustomAttributeData(ResolutionScope scope, MetadataToken caCtorToken, in ConstArray blob)
#endif
        {
#if NATIVEAOT
            m_scope = reader;
            CustomAttribute blob = customAttributeHandle.GetCustomAttribute(reader);
            m_ctor = ResolveAttributeConstructor(reader, blob);
#else
            m_scope = scope;
            m_ctor = (RuntimeConstructorInfo)RuntimeType.GetMethodBase(m_scope, caCtorToken)!;

            if (m_ctor.DeclaringType!.IsGenericType)
            {
                MetadataImport metadataScope = m_scope.MetadataImport;
                Type attributeType = m_scope.ResolveType(metadataScope.GetParentToken(caCtorToken), null, null);
                m_ctor = (RuntimeConstructorInfo)m_scope.ResolveMethod(caCtorToken, attributeType.GenericTypeArguments, null)!.MethodHandle.GetMethodInfo();
            }
#endif

            ReadOnlySpan<ParameterInfo> parameters = m_ctor.GetParametersAsSpan();
            if (parameters.Length != 0)
            {
                m_ctorParams = new CustomAttributeCtorParameter[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                    m_ctorParams[i] = new CustomAttributeCtorParameter(new CustomAttributeType((RuntimeType)parameters[i].ParameterType));
            }
            else
            {
                m_ctorParams = [];
            }

            FieldInfo[] fields = m_ctor.DeclaringType!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo[] properties = m_ctor.DeclaringType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Allocate collections for members and names params.
            m_namedParams = new CustomAttributeNamedParameter[properties.Length + fields.Length];

            int idx = 0;
            foreach (FieldInfo fi in fields)
            {
                m_namedParams[idx++] = new CustomAttributeNamedParameter(
                    fi,
                    CustomAttributeEncoding.Field,
                    new CustomAttributeType((RuntimeType)fi.FieldType));
            }

            foreach (PropertyInfo pi in properties)
            {
                m_namedParams[idx++] = new CustomAttributeNamedParameter(
                    pi,
                    CustomAttributeEncoding.Property,
                    new CustomAttributeType((RuntimeType)pi.PropertyType));
            }

            CustomAttributeEncodedArgument.ParseAttributeArguments(blob, m_ctorParams, m_namedParams, m_scope);
        }
        #endregion

        #region Pseudo Custom Attribute Constructor
        internal RuntimeCustomAttributeData(Attribute attribute)
        {
           if (attribute is DllImportAttribute dllImportAttribute)
               Init(dllImportAttribute);
           else if (attribute is FieldOffsetAttribute fieldOffsetAttribute)
               Init(fieldOffsetAttribute);
           else if (attribute is MarshalAsAttribute marshalAsAttribute)
               Init(marshalAsAttribute);
           else if (attribute is TypeForwardedToAttribute typeForwardedToAttribute)
               Init(typeForwardedToAttribute);
           else
               Init(attribute);
        }
        private void Init(DllImportAttribute dllImport)
        {
            Type type = typeof(DllImportAttribute);
            m_ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            m_typedCtorArgs = Array.AsReadOnly(new CustomAttributeTypedArgument[]
            {
                new CustomAttributeTypedArgument(dllImport.Value),
            });

            m_namedArgs = Array.AsReadOnly(new CustomAttributeNamedArgument[]
            {
                new CustomAttributeNamedArgument(type.GetField("EntryPoint")!, dllImport.EntryPoint),
                new CustomAttributeNamedArgument(type.GetField("CharSet")!, dllImport.CharSet),
                new CustomAttributeNamedArgument(type.GetField("ExactSpelling")!, dllImport.ExactSpelling),
                new CustomAttributeNamedArgument(type.GetField("SetLastError")!, dllImport.SetLastError),
                new CustomAttributeNamedArgument(type.GetField("PreserveSig")!, dllImport.PreserveSig),
                new CustomAttributeNamedArgument(type.GetField("CallingConvention")!, dllImport.CallingConvention),
                new CustomAttributeNamedArgument(type.GetField("BestFitMapping")!, dllImport.BestFitMapping),
                new CustomAttributeNamedArgument(type.GetField("ThrowOnUnmappableChar")!, dllImport.ThrowOnUnmappableChar)
            });
        }
        private void Init(FieldOffsetAttribute fieldOffset)
        {
            m_ctor = typeof(FieldOffsetAttribute).GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            m_typedCtorArgs = Array.AsReadOnly(new CustomAttributeTypedArgument[] {
                new CustomAttributeTypedArgument(fieldOffset.Value)
            });
            m_namedArgs = Array.Empty<CustomAttributeNamedArgument>();
        }
        private void Init(MarshalAsAttribute marshalAs)
        {
            Type type = typeof(MarshalAsAttribute);
            m_ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            m_typedCtorArgs = Array.AsReadOnly(new CustomAttributeTypedArgument[]
            {
                new CustomAttributeTypedArgument(marshalAs.Value),
            });

            int i = 3; // ArraySubType, SizeParamIndex, SizeConst
            if (marshalAs.MarshalType is not null) i++;
            if (marshalAs.MarshalTypeRef is not null) i++;
            if (marshalAs.MarshalCookie is not null) i++;
            i++; // IidParameterIndex
            i++; // SafeArraySubType
            if (marshalAs.SafeArrayUserDefinedSubType is not null) i++;
            CustomAttributeNamedArgument[] namedArgs = new CustomAttributeNamedArgument[i];

            // For compatibility with previous runtimes, we always include the following 5 attributes, regardless
            // of if they apply to the UnmanagedType being marshaled or not.
            i = 0;
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("ArraySubType")!, marshalAs.ArraySubType);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SizeParamIndex")!, marshalAs.SizeParamIndex);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SizeConst")!, marshalAs.SizeConst);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("IidParameterIndex")!, marshalAs.IidParameterIndex);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SafeArraySubType")!, marshalAs.SafeArraySubType);
            if (marshalAs.MarshalType is not null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalType")!, marshalAs.MarshalType);
            if (marshalAs.MarshalTypeRef is not null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalTypeRef")!, marshalAs.MarshalTypeRef);
            if (marshalAs.MarshalCookie is not null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalCookie")!, marshalAs.MarshalCookie);
            if (marshalAs.SafeArrayUserDefinedSubType is not null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SafeArrayUserDefinedSubType")!, marshalAs.SafeArrayUserDefinedSubType);

            m_namedArgs = Array.AsReadOnly(namedArgs);
        }
        private void Init(TypeForwardedToAttribute forwardedTo)
        {
            Type type = typeof(TypeForwardedToAttribute);

            Type[] sig = [typeof(Type)];
            m_ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, sig, null)!;

            CustomAttributeTypedArgument[] typedArgs = [new CustomAttributeTypedArgument(typeof(Type), forwardedTo.Destination)];
            m_typedCtorArgs = Array.AsReadOnly(typedArgs);

            m_namedArgs = Array.Empty<CustomAttributeNamedArgument>();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "The pca object had to be created by the single ctor on the Type. So the ctor couldn't have been trimmed.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ComImportAttribute))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(InAttribute))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(NonSerializedAttribute))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(OptionalAttribute))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(OutAttribute))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(PreserveSigAttribute))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SerializableAttribute))]
        private void Init(object pca)
        {
            Type type = pca.GetType();

#if DEBUG
            // Ensure there is only a single constructor for 'pca', so it is safe to suppress IL2075
            ConstructorInfo[] allCtors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(allCtors.Length == 1);
            Debug.Assert(allCtors[0].GetParametersAsSpan().Length == 0);
#endif

            m_ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            m_typedCtorArgs = Array.Empty<CustomAttributeTypedArgument>();
            m_namedArgs = Array.Empty<CustomAttributeNamedArgument>();
        }
        #endregion

        #region Public Members
        public override ConstructorInfo Constructor => m_ctor;

        public override IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get
            {
                if (m_typedCtorArgs is null)
                {
                    if (m_ctorParams.Length != 0)
                    {
                        CustomAttributeTypedArgument[] typedCtorArgs = new CustomAttributeTypedArgument[m_ctorParams.Length];

                        for (int i = 0; i < typedCtorArgs.Length; i++)
                        {
                            CustomAttributeEncodedArgument encodedArg = m_ctorParams[i].EncodedArgument!;

                            typedCtorArgs[i] = new CustomAttributeTypedArgument(m_scope, encodedArg);
                        }

                        m_typedCtorArgs = Array.AsReadOnly(typedCtorArgs);
                    }
                    else
                    {
                        m_typedCtorArgs = Array.Empty<CustomAttributeTypedArgument>();
                    }
                }

                return m_typedCtorArgs;
            }
        }

        public override IList<CustomAttributeNamedArgument> NamedArguments
        {
            get
            {
                if (m_namedArgs is null)
                {
                    int cNamedArgs = 0;
                    if (m_namedParams is not null)
                    {
                        foreach (CustomAttributeNamedParameter p in m_namedParams)
                        {
                            if (p.EncodedArgument is not null
                                && p.EncodedArgument.CustomAttributeType.EncodedType != CustomAttributeEncoding.Undefined)
                            {
                                cNamedArgs++;
                            }
                        }
                    }

                    if (cNamedArgs != 0)
                    {
                        CustomAttributeNamedArgument[] namedArgs = new CustomAttributeNamedArgument[cNamedArgs];

                        int j = 0;
                        foreach (CustomAttributeNamedParameter p in m_namedParams!)
                        {
                            if (p.EncodedArgument is not null
                                && p.EncodedArgument.CustomAttributeType.EncodedType != CustomAttributeEncoding.Undefined)
                            {
                                Debug.Assert(p.MemberInfo is not null);
                                namedArgs[j++] = new CustomAttributeNamedArgument(
                                    p.MemberInfo,
                                    new CustomAttributeTypedArgument(m_scope, p.EncodedArgument));
                            }
                        }

                        m_namedArgs = Array.AsReadOnly(namedArgs);
                    }
                    else
                    {
                        m_namedArgs = Array.Empty<CustomAttributeNamedArgument>();
                    }
                }

                return m_namedArgs;
            }
        }
        #endregion
    }

    public readonly partial struct CustomAttributeTypedArgument
    {
        #region Private Static Methods
        internal static Type CustomAttributeEncodingToType(CustomAttributeEncoding encodedType)
        {
            return encodedType switch
            {
                CustomAttributeEncoding.Enum => typeof(Enum),
                CustomAttributeEncoding.Int32 => typeof(int),
                CustomAttributeEncoding.String => typeof(string),
                CustomAttributeEncoding.Type => typeof(Type),
                CustomAttributeEncoding.Array => typeof(Array),
                CustomAttributeEncoding.Char => typeof(char),
                CustomAttributeEncoding.Boolean => typeof(bool),
                CustomAttributeEncoding.SByte => typeof(sbyte),
                CustomAttributeEncoding.Byte => typeof(byte),
                CustomAttributeEncoding.Int16 => typeof(short),
                CustomAttributeEncoding.UInt16 => typeof(ushort),
                CustomAttributeEncoding.UInt32 => typeof(uint),
                CustomAttributeEncoding.Int64 => typeof(long),
                CustomAttributeEncoding.UInt64 => typeof(ulong),
                CustomAttributeEncoding.Float => typeof(float),
                CustomAttributeEncoding.Double => typeof(double),
                CustomAttributeEncoding.Object => typeof(object),
                _ => throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)encodedType), nameof(encodedType)),
            };
        }

        private static object EncodedValueToRawValue(PrimitiveValue val, CustomAttributeEncoding encodedType)
        {
            return encodedType switch
            {
                CustomAttributeEncoding.Boolean => (byte)val.Byte4 != 0,
                CustomAttributeEncoding.Char => (char)val.Byte4,
                CustomAttributeEncoding.Byte => (byte)val.Byte4,
                CustomAttributeEncoding.SByte => (sbyte)val.Byte4,
                CustomAttributeEncoding.Int16 => (short)val.Byte4,
                CustomAttributeEncoding.UInt16 => (ushort)val.Byte4,
                CustomAttributeEncoding.Int32 => val.Byte4,
                CustomAttributeEncoding.UInt32 => (uint)val.Byte4,
                CustomAttributeEncoding.Int64 => val.Byte8,
                CustomAttributeEncoding.UInt64 => (ulong)val.Byte8,
                CustomAttributeEncoding.Float => BitConverter.Int32BitsToSingle(val.Byte4),
                CustomAttributeEncoding.Double => BitConverter.Int64BitsToDouble(val.Byte8),
                _ => throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, val.Byte8), nameof(val))
            };
        }

        #endregion

#if NATIVEAOT
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
#endif
        internal CustomAttributeTypedArgument(ResolutionScope scope, CustomAttributeEncodedArgument encodedArg)
        {
            CustomAttributeEncoding encodedType = encodedArg.CustomAttributeType.EncodedType;

            if (encodedType == CustomAttributeEncoding.Undefined)
                throw new ArgumentException(null, nameof(encodedArg));

            if (encodedType == CustomAttributeEncoding.Enum)
            {
                _argumentType = encodedArg.CustomAttributeType.EnumType!;
                _value = EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedArg.CustomAttributeType.EncodedEnumType);
            }
            else if (encodedType == CustomAttributeEncoding.String)
            {
                _argumentType = typeof(string);
                _value = encodedArg.StringValue;
            }
            else if (encodedType == CustomAttributeEncoding.Type)
            {
                _argumentType = typeof(Type);

#if NATIVEAOT
                _value = encodedArg.TypeValue;
#else
                _value = null;

                if (encodedArg.StringValue is not null)
                    _value = ResolveType(scope, encodedArg.StringValue);
#endif
            }
            else if (encodedType == CustomAttributeEncoding.Array)
            {
                encodedType = encodedArg.CustomAttributeType.EncodedArrayType;
                Type elementType;

                if (encodedType == CustomAttributeEncoding.Enum)
                {
                    elementType = encodedArg.CustomAttributeType.EnumType!;
                }
                else
                {
                    elementType = CustomAttributeEncodingToType(encodedType);
                }

                _argumentType = elementType.MakeArrayType();

                if (encodedArg.ArrayValue is null)
                {
                    _value = null;
                }
                else
                {
                    CustomAttributeTypedArgument[] arrayValue = new CustomAttributeTypedArgument[encodedArg.ArrayValue.Length];
                    for (int i = 0; i < arrayValue.Length; i++)
                        arrayValue[i] = new CustomAttributeTypedArgument(scope, encodedArg.ArrayValue[i]);

                    _value = Array.AsReadOnly(arrayValue);
                }
            }
            else
            {
                _argumentType = CustomAttributeEncodingToType(encodedType);
                _value = EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedType);
            }
        }
    }

    // See CorSerializationType in corhdr.h
    internal enum CustomAttributeEncoding : int
    {
        Undefined = 0,
        Boolean = CorElementType.ELEMENT_TYPE_BOOLEAN,
        Char = CorElementType.ELEMENT_TYPE_CHAR,
        SByte = CorElementType.ELEMENT_TYPE_I1,
        Byte = CorElementType.ELEMENT_TYPE_U1,
        Int16 = CorElementType.ELEMENT_TYPE_I2,
        UInt16 = CorElementType.ELEMENT_TYPE_U2,
        Int32 = CorElementType.ELEMENT_TYPE_I4,
        UInt32 = CorElementType.ELEMENT_TYPE_U4,
        Int64 = CorElementType.ELEMENT_TYPE_I8,
        UInt64 = CorElementType.ELEMENT_TYPE_U8,
        Float = CorElementType.ELEMENT_TYPE_R4,
        Double = CorElementType.ELEMENT_TYPE_R8,
        String = CorElementType.ELEMENT_TYPE_STRING,
        Array = CorElementType.ELEMENT_TYPE_SZARRAY,
        Type = 0x50,
        Object = 0x51,
        Field = 0x53,
        Property = 0x54,
        Enum = 0x55
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct PrimitiveValue
    {
        /// <safety>Overlaps only Byte8; both views are non-reference integers (int and long), so reinterpreting one as the other cannot forge a managed reference or read out of bounds.</safety>
        [FieldOffset(0)]
        public safe int Byte4;

        /// <safety>Overlaps only Byte4; both views are non-reference integers (long and int), so reinterpreting one as the other cannot forge a managed reference.</safety>
        [FieldOffset(0)]
        public safe long Byte8;
    }

    internal sealed partial class CustomAttributeEncodedArgument
    {
        private static CustomAttributeEncodedArgument ParseCustomAttributeValue(
            ref CustomAttributeDataParser parser,
            CustomAttributeType type,
            ResolutionScope module)
        {
            CustomAttributeType attributeType = type.EncodedType == CustomAttributeEncoding.Object
                ? ParseCustomAttributeType(ref parser, module)
                : type;

            CustomAttributeEncodedArgument arg = new(attributeType);

            CustomAttributeEncoding underlyingType = attributeType.EncodedType == CustomAttributeEncoding.Enum
                ? attributeType.EncodedEnumType
                : attributeType.EncodedType;

            switch (underlyingType)
            {
                case CustomAttributeEncoding.Boolean:
                case CustomAttributeEncoding.Byte:
                case CustomAttributeEncoding.SByte:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = parser.GetU1() };
                    break;
                case CustomAttributeEncoding.Char:
                case CustomAttributeEncoding.Int16:
                case CustomAttributeEncoding.UInt16:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = parser.GetU2() };
                    break;
                case CustomAttributeEncoding.Int32:
                case CustomAttributeEncoding.UInt32:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = parser.GetI4() };
                    break;
                case CustomAttributeEncoding.Int64:
                case CustomAttributeEncoding.UInt64:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte8 = parser.GetI8() };
                    break;
                case CustomAttributeEncoding.Float:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = BitConverter.SingleToInt32Bits(parser.GetR4()) };
                    break;
                case CustomAttributeEncoding.Double:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte8 = BitConverter.DoubleToInt64Bits(parser.GetR8()) };
                    break;
                case CustomAttributeEncoding.String:
#if !NATIVEAOT
                case CustomAttributeEncoding.Type:
#endif
                    arg.StringValue = parser.GetString();
                    break;
#if NATIVEAOT
                case CustomAttributeEncoding.Type:
                    arg.TypeValue = parser.GetTypeValue();
                    break;
#endif
                case CustomAttributeEncoding.Array:
                {
                    arg.ArrayValue = null;
                    int len = parser.GetI4();
                    if (len != -1) // indicates array is null - ECMA-335 II.23.3.
                    {
                        attributeType = new CustomAttributeType(
                            attributeType.EncodedArrayType,
                            CustomAttributeEncoding.Undefined, // Array type
                            attributeType.EncodedEnumType,
                            attributeType.EnumType);
                        arg.ArrayValue = new CustomAttributeEncodedArgument[len];
                        for (int i = 0; i < len; ++i)
                        {
                            arg.ArrayValue[i] = ParseCustomAttributeValue(ref parser, attributeType, module);
                        }
                    }
                    break;
                }
                default:
                    throw new BadImageFormatException();
            }

            return arg;
        }

        private static CustomAttributeType ParseNamedArgumentTarget(
            ref CustomAttributeDataParser parser, ResolutionScope module, out string? argumentName)
        {
            // Determine if a field or property.
            CustomAttributeEncoding namedArgFieldOrProperty = parser.GetTag();
            if (namedArgFieldOrProperty is not CustomAttributeEncoding.Field
                && namedArgFieldOrProperty is not CustomAttributeEncoding.Property)
            {
                throw new BadImageFormatException(SR.Arg_CustomAttributeFormatException);
            }

            CustomAttributeType argumentType = ParseCustomAttributeType(ref parser, module);
            argumentName = parser.GetString();
            return argumentType;
        }

        private static CustomAttributeType ParseCustomAttributeType(ref CustomAttributeDataParser parser, ResolutionScope module)
        {
            CustomAttributeEncoding arrayTag = CustomAttributeEncoding.Undefined;
            CustomAttributeEncoding enumTag = CustomAttributeEncoding.Undefined;
            Type? enumType = null;

            CustomAttributeEncoding tag = parser.GetTag();
            if (tag is CustomAttributeEncoding.Array)
            {
                arrayTag = parser.GetTag();
            }

            // Load the enum type if needed.
            if (tag is CustomAttributeEncoding.Enum
                || (tag is CustomAttributeEncoding.Array
                    && arrayTag is CustomAttributeEncoding.Enum))
            {
#if NATIVEAOT
                enumType = parser.GetTypeValue();
                Debug.Assert(enumType is not null && enumType.IsEnum);
#else
                // We cannot determine the underlying type without loading the enum.
                string enumTypeMaybe = parser.GetString() ?? throw new BadImageFormatException();
                enumType = TypeNameResolver.GetTypeReferencedByCustomAttribute(enumTypeMaybe, module);
                if (!enumType.IsEnum)
                {
                    throw new BadImageFormatException();
                }
#endif

                enumTag = RuntimeCustomAttributeData.TypeToCustomAttributeEncoding((RuntimeType)enumType.GetEnumUnderlyingType());
            }
            return new CustomAttributeType(tag, arrayTag, enumTag, enumType);
        }

        /// <summary>
        /// Used to parse CustomAttribute data. See ECMA-335 II.23.3.
        /// </summary>
        internal ref struct CustomAttributeDataParser
        {
            private int _curr;
            private readonly ReadOnlySpan<byte> _blob;

#if NATIVEAOT
            private readonly MetadataReader _reader;

            public CustomAttributeDataParser(CustomAttribute attribute, MetadataReader reader)
                : this(attribute.Blob.AsSpan(), reader)
            {
            }

            internal CustomAttributeDataParser(ReadOnlySpan<byte> blob, MetadataReader reader)
            {
                _blob = blob;
                _curr = 0;
                _reader = reader;
            }
#else
            public CustomAttributeDataParser(ConstArray attributeBlob)
            {
                unsafe
                {
                    _blob = new ReadOnlySpan<byte>((void*)attributeBlob.Signature, attributeBlob.Length);
                }
                _curr = 0;
            }

            internal CustomAttributeDataParser(ReadOnlySpan<byte> blob)
            {
                _blob = blob;
                _curr = 0;
            }
#endif

            internal int Consumed => _curr;
            internal int Remaining => _blob.Length - _curr;

            private ReadOnlySpan<byte> PeekData(int size)
            {
                if ((uint)size > (uint)Remaining)
                {
                    throw new CustomAttributeFormatException();
                }

                return _blob.Slice(_curr, size);
            }

            internal ReadOnlySpan<byte> ReadData(int size)
            {
                ReadOnlySpan<byte> tmp = PeekData(size);
                Debug.Assert(size <= (_blob.Length - _curr));
                _curr += size;
                return tmp;
            }

            public byte GetU1()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(byte));
                return tmp[0];
            }

            public sbyte GetI1() => (sbyte)GetU1();

            public ushort GetU2()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(ushort));
                return BinaryPrimitives.ReadUInt16LittleEndian(tmp);
            }

            public short GetI2() => (short)GetU2();

            public uint GetU4()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(uint));
                return BinaryPrimitives.ReadUInt32LittleEndian(tmp);
            }

            public int GetI4() => (int)GetU4();

            public ulong GetU8()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(ulong));
                return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
            }

            public long GetI8() => (long)GetU8();

            public float GetR4()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(float));
                return BinaryPrimitives.ReadSingleLittleEndian(tmp);
            }

            public CustomAttributeEncoding GetTag()
            {
                return (CustomAttributeEncoding)GetI1();
            }

            public double GetR8()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(double));
                return BinaryPrimitives.ReadDoubleLittleEndian(tmp);
            }

            public bool ValidateProlog()
            {
#if NATIVEAOT
                return true;
#else
                ushort val = GetU2();
                return val == 0x0001;
#endif
            }

            public int GetNamedArgumentCount() => GetI2();

#if NATIVEAOT
            public string? GetString()
            {
                BlobReader blob = new BlobReader(_blob.Slice(_curr));
                ConstantStringValueHandle handle = blob.ReadStringHandle();
                _curr += blob.Offset;
                return handle.GetStringOrNull(_reader);
            }

            public RuntimeType? GetTypeValue()
            {
                BlobReader blob = new BlobReader(_blob.Slice(_curr));
                Handle handle = blob.ReadTypeHandle();
                _curr += blob.Offset;
                return handle.IsNil ? null : handle.Resolve(_reader, default).ToType();
            }

#else
            public string? GetString()
            {
                ReadOnlySpan<byte> utf8Bytes = GetStringBytes(out bool isNull);
                return isNull ? null : utf8Bytes.IsEmpty ? string.Empty : Encoding.UTF8.GetString(utf8Bytes);
            }

            internal ReadOnlySpan<byte> GetStringBytes(out bool isNull)
            {
                byte packedLengthBegin = PeekData(sizeof(byte))[0];

                // Check if the embedded string indicates a 'null' string (0xff).
                if (packedLengthBegin == 0xff) // ECMA 335- II.23.3
                {
                    // Consume the indicator.
                    ReadData(1);
                    isNull = true;
                    return default;
                }

                isNull = false;
                int length = GetPackedLength(packedLengthBegin);
                return ReadData(length);
            }

            private int GetPackedLength(byte firstByte)
            {
                if ((firstByte & 0x80) == 0)
                {
                    // Consume one byte.
                    ReadData(1);
                    return firstByte & 0x7f;
                }

                int len;
                ReadOnlySpan<byte> data;
                if ((firstByte & 0xC0) == 0x80)
                {
                    // Consume the bytes.
                    data = ReadData(2);
                    len = (data[0] & 0x3f) << 8;
                    return len + data[1];
                }

                if ((firstByte & 0xE0) == 0xC0)
                {
                    // Consume the bytes.
                    data = ReadData(4);
                    len = (data[0] & 0x1f) << 24;
                    len += data[1] << 16;
                    len += data[2] << 8;
                    return len + data[3];
                }

                throw new CustomAttributeFormatException();
            }
#endif
        }

        internal static void ParseAttributeArguments(
#if NATIVEAOT
            CustomAttribute attributeData,
#else
            ConstArray attributeData,
#endif
            CustomAttributeCtorParameter[] customAttributeCtorParameters,
            CustomAttributeNamedParameter[] customAttributeNamedParameters,
            ResolutionScope customAttributeModule)
        {
            ArgumentNullException.ThrowIfNull(customAttributeModule);

            Debug.Assert(customAttributeCtorParameters is not null);
            Debug.Assert(customAttributeNamedParameters is not null);

            if (customAttributeCtorParameters.Length != 0 || customAttributeNamedParameters.Length != 0)
            {
#if NATIVEAOT
                CustomAttributeDataParser parser = new CustomAttributeDataParser(attributeData, customAttributeModule);
#else
                CustomAttributeDataParser parser = new CustomAttributeDataParser(attributeData);
#endif
                try
                {
                    if (!parser.ValidateProlog())
                    {
                        throw new BadImageFormatException(SR.Arg_CustomAttributeFormatException);
                    }

                    ParseCtorArgs(ref parser, customAttributeCtorParameters, customAttributeModule);
                    ParseNamedArgs(ref parser, customAttributeNamedParameters, customAttributeModule);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    throw new CustomAttributeFormatException(ex.Message, ex);
                }
            }
        }

        internal CustomAttributeEncodedArgument(CustomAttributeType type)
        {
            CustomAttributeType = type;
        }

        public CustomAttributeType CustomAttributeType { get; }
        public PrimitiveValue PrimitiveValue { get; set; }
        public CustomAttributeEncodedArgument[]? ArrayValue { get; set; }
        public string? StringValue { get; set; }
#if NATIVEAOT
        public Type? TypeValue { get; set; }
#endif

        private static void ParseCtorArgs(
            ref CustomAttributeDataParser parser,
            CustomAttributeCtorParameter[] customAttributeCtorParameters,
            ResolutionScope module)
        {
            foreach (CustomAttributeCtorParameter p in customAttributeCtorParameters)
            {
                p.EncodedArgument = ParseCustomAttributeValue(
                    ref parser,
                    p.CustomAttributeType,
                    module);
            }
        }

        private static void ParseNamedArgs(
            ref CustomAttributeDataParser parser,
            CustomAttributeNamedParameter[] customAttributeNamedParameters,
            ResolutionScope module)
        {
            int argCount = parser.GetNamedArgumentCount();

            for (int i = 0; i < argCount; ++i)
            {
                // Parse the encoded type for the named argument.
                CustomAttributeType argType = ParseNamedArgumentTarget(ref parser, module, out string? argName);

                // Argument name must be non-null and non-empty.
                if (string.IsNullOrEmpty(argName))
                {
                    throw new BadImageFormatException(SR.Arg_CustomAttributeFormatException);
                }

                // Update the appropriate named argument element.
                CustomAttributeNamedParameter? parameterToUpdate = null;
                foreach (CustomAttributeNamedParameter namedParam in customAttributeNamedParameters)
                {
                    CustomAttributeType namedArgType = namedParam.CustomAttributeType;
                    if (namedArgType.EncodedType != CustomAttributeEncoding.Object)
                    {
                        if (namedArgType.EncodedType != argType.EncodedType)
                        {
                            continue;
                        }

                        // Match array type
                        if (argType.EncodedType is CustomAttributeEncoding.Array
                            && namedArgType.EncodedArrayType is not CustomAttributeEncoding.Object
                            && argType.EncodedArrayType != namedArgType.EncodedArrayType)
                        {
                            continue;
                        }
                    }

                    // Match name
                    if (!namedParam.MemberInfo.Name.Equals(argName))
                    {
                        continue;
                    }

                    // If enum, match enum name.
                    if (namedArgType.EncodedType is CustomAttributeEncoding.Enum
                        || (namedArgType.EncodedType is CustomAttributeEncoding.Array
                            && namedArgType.EncodedArrayType is CustomAttributeEncoding.Enum))
                    {
                        if (!ReferenceEquals(argType.EnumType, namedArgType.EnumType))
                        {
                            continue;
                        }

                        Debug.Assert(namedArgType.EncodedEnumType == argType.EncodedEnumType);
                    }

                    // Found a match
                    parameterToUpdate = namedParam;
                    break;
                }

                if (parameterToUpdate is null)
                {
                    throw new BadImageFormatException(SR.Arg_CustomAttributeUnknownNamedArgument);
                }

                if (parameterToUpdate.EncodedArgument is not null)
                {
                    throw new BadImageFormatException(SR.Arg_CustomAttributeDuplicateNamedArgument);
                }

                parameterToUpdate.EncodedArgument = ParseCustomAttributeValue(ref parser, argType, module);
            }
        }

    }

    internal sealed class CustomAttributeCtorParameter(CustomAttributeType type)
    {
        public CustomAttributeType CustomAttributeType => type;
        public CustomAttributeEncodedArgument? EncodedArgument { get; set; }
    }

    internal sealed class CustomAttributeNamedParameter(MemberInfo memberInfo, CustomAttributeEncoding fieldOrProperty, CustomAttributeType type)
    {
        public MemberInfo MemberInfo => memberInfo;
        public CustomAttributeType CustomAttributeType => type;
        public CustomAttributeEncoding FieldOrProperty => fieldOrProperty;
        public CustomAttributeEncodedArgument? EncodedArgument { get; set; }
    }

    internal sealed class CustomAttributeType
    {
        public CustomAttributeType(
            CustomAttributeEncoding encodedType,
            CustomAttributeEncoding encodedArrayType,
            CustomAttributeEncoding encodedEnumType,
            Type? enumType)
        {
            EncodedType = encodedType;
            EncodedArrayType = encodedArrayType;
            EncodedEnumType = encodedEnumType;
            EnumType = enumType;
        }

        public CustomAttributeType(RuntimeType parameterType)
        {
            Debug.Assert(parameterType is not null);
            CustomAttributeEncoding encodedType = RuntimeCustomAttributeData.TypeToCustomAttributeEncoding(parameterType);
            CustomAttributeEncoding encodedArrayType = CustomAttributeEncoding.Undefined;
            CustomAttributeEncoding encodedEnumType = CustomAttributeEncoding.Undefined;
            Type? enumType = null;

            if (encodedType == CustomAttributeEncoding.Array)
            {
                parameterType = (RuntimeType)parameterType.GetElementType()!;
                encodedArrayType = RuntimeCustomAttributeData.TypeToCustomAttributeEncoding(parameterType);
            }

            if (encodedType == CustomAttributeEncoding.Enum
                || encodedArrayType == CustomAttributeEncoding.Enum)
            {
                enumType = parameterType;
                encodedEnumType = RuntimeCustomAttributeData.TypeToCustomAttributeEncoding((RuntimeType)Enum.GetUnderlyingType(parameterType));
            }

            EncodedType = encodedType;
            EncodedArrayType = encodedArrayType;
            EncodedEnumType = encodedEnumType;
            EnumType = enumType;
        }

        public CustomAttributeEncoding EncodedType { get; }
        public CustomAttributeEncoding EncodedEnumType { get; }
        public CustomAttributeEncoding EncodedArrayType { get; }

        /// The most complicated type is an enum[] in which case...
        public Type? EnumType { get; }
    }

    internal static unsafe partial class RuntimeCustomAttribute
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:MethodParameterDoesntMeetThisParameterRequirements",
            Justification = "Linker guarantees presence of all the constructor parameters, property setters and fields which are accessed by any " +
                            "attribute instantiation which is present in the code linker has analyzed." +
                            "As such the reflection usage in this method will never fail as those methods/fields will be present.")]
#if NATIVEAOT
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:MethodParameterDoesntMeetThisParameterRequirements",
            Justification = "Linker guarantees presence of all the constructor parameters which are accessed by any " +
                            "attribute instantiation which is present in the code linker has analyzed.")]
#endif
        private static void AddCustomAttributes(
            ref ListBuilder<object> attributes,
            QCustomAttributeList customAttributes,
            RuntimeType? attributeFilterType, bool mustBeInheritable,
            // The derivedAttributes list must be passed by value so that it is not modified with the discovered attributes
            ListBuilder<object> derivedAttributes)
        {
#if NATIVEAOT
            MetadataReader? decoratedModule = customAttributes.Reader;
            if (decoratedModule is null)
                return;

            foreach (CustomAttributeHandle customAttributeHandle in customAttributes.Handles)
            {
                CustomAttribute customAttribute = customAttributeHandle.GetCustomAttribute(decoratedModule);

                if (!FilterCustomAttributeRecord(
                    customAttribute,
                    decoratedModule,
                    attributeFilterType!,
                    mustBeInheritable,
                    ref derivedAttributes,
                    out RuntimeType attributeType))
#else
            RuntimeModule decoratedModule = customAttributes.Module;
            int decoratedMetadataToken = customAttributes.MetadataToken;
            CustomAttributeRecord[] car = RuntimeCustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedMetadataToken);

            if (attributeFilterType is null && car.Length == 0)
            {
                return;
            }

            MetadataImport scope = decoratedModule.MetadataImport;
            for (int i = 0; i < car.Length; i++)
            {
                ref CustomAttributeRecord caRecord = ref car[i];

                IntPtr blobStart = caRecord.blob.Signature;
                IntPtr blobEnd = (IntPtr)((byte*)blobStart + caRecord.blob.Length);

                if (!FilterCustomAttributeRecord(caRecord.tkCtor, in scope,
                                                 decoratedModule, decoratedMetadataToken, attributeFilterType!, mustBeInheritable,
                                                 ref derivedAttributes,
                                                 out RuntimeType attributeType, out IRuntimeMethodInfo? ctorWithParameters, out bool isVarArg))
#endif
                {
                    continue;
                }

#if NATIVEAOT
                ReadOnlySpan<byte> attributeBlob = customAttribute.Blob.AsSpan();
                // NativeFormat metadata is backed by unmanaged memory.
                IntPtr blobStart = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetReference(attributeBlob));
                IntPtr blobEnd = blobStart + attributeBlob.Length;

                // TODO-NativeAOT: Match CoreCLR's custom attribute constructor visibility checks.
                RuntimeConstructorInfo constructor = (RuntimeConstructorInfo)RuntimeCustomAttributeData.ResolveAttributeConstructor(decoratedModule, customAttribute, attributeType);
                IRuntimeMethodInfo? ctorWithParameters = constructor.GetParametersAsSpan().IsEmpty ? null : constructor;
                bool isVarArg = (constructor.CallingConvention & CallingConventions.VarArgs) != 0;
#endif
                // Leverage RuntimeConstructorInfo standard .ctor verification
                RuntimeConstructorInfo.CheckCanCreateInstance(attributeType, isVarArg);

                // Create custom attribute object
                int cNamedArgs;
                object attribute;
                if (ctorWithParameters is not null)
                {
                    attribute = CreateCustomAttributeInstance(decoratedModule, attributeType, ctorWithParameters, ref blobStart, blobEnd, out cNamedArgs);
                }
                else
                {
                    attribute = attributeType.CreateInstanceDefaultCtor(publicOnly: false, wrapExceptions: false)!;

                    // It is allowed by the ECMA spec to have an empty signature blob
                    int blobLen = (int)((byte*)blobEnd - (byte*)blobStart);
                    if (blobLen == 0)
                    {
                        cNamedArgs = 0;
                    }
                    else
                    {
#if NATIVEAOT
                        var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blobStart, blobEnd), decoratedModule);
#else
                        var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blobStart, blobEnd));
#endif
                        if (!parser.ValidateProlog())
                        {
                            throw new CustomAttributeFormatException();
                        }
                        cNamedArgs = parser.GetI2();
                        blobStart += parser.Consumed;
                    }
                }

                for (int j = 0; j < cNamedArgs; j++)
                {
                    GetPropertyOrFieldData(decoratedModule, ref blobStart, blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value);

                    try
                    {
                        if (isProperty)
                        {
                            if (type is null && value is not null)
                            {
                                type = (RuntimeType)value.GetType();
                                if (type == typeof(RuntimeType))
                                {
                                    type = (RuntimeType)typeof(Type);
                                }
                            }

                            RuntimePropertyInfo? property = (RuntimePropertyInfo?)(type is null ?
                                attributeType.GetProperty(name) :
                                attributeType.GetProperty(name, type, [])) ??
                                throw new CustomAttributeFormatException(SR.Format(SR.RFLCT_InvalidPropFail, name));
                            RuntimeMethodInfo setMethod = property.GetSetMethod(true)!;

                            // Public properties may have non-public setter methods
                            if (!setMethod.IsPublic)
                            {
                                continue;
                            }

                            setMethod.InvokePropertySetter(attribute, BindingFlags.Default, null, value, null);
                        }
                        else
                        {
                            FieldInfo field = attributeType.GetField(name)!;
                            field.SetValue(attribute, value, BindingFlags.Default, Type.DefaultBinder, null);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new CustomAttributeFormatException(
                            SR.Format(isProperty ? SR.RFLCT_InvalidPropFail : SR.RFLCT_InvalidFieldFail, name), e);
                    }
                }

                if (blobStart != blobEnd)
                {
                    throw new CustomAttributeFormatException();
                }

                attributes.Add(attribute);
            }
            GC.KeepAlive(decoratedModule);
        }

        private static object CreateCustomAttributeInstance(ResolutionScope module, RuntimeType type, IRuntimeMethodInfo ctor, ref IntPtr blob, IntPtr blobEnd, out int namedArgs)
        {
#if NATIVEAOT
            RuntimeConstructorInfo constructor = ctor;
#else
            // A shared generic constructor handle does not preserve its declaring type's instantiation.
            if (RuntimeType.GetMethodBase(type, ctor) is not RuntimeConstructorInfo constructor)
            {
                throw new CustomAttributeFormatException();
            }
#endif

            RuntimeType[] argumentTypes = constructor.ArgumentTypes;
            int argumentCount = argumentTypes.Length;
#if NATIVEAOT
            var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blob, blobEnd), module);
#else
            var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blob, blobEnd));
#endif
            if (!parser.ValidateProlog())
            {
                throw new CustomAttributeFormatException();
            }

            // Keep fixed primitives unboxed. Only the bounded common case uses stack storage.
            const int MaxStackAttributeArguments = 4;
            InlineArray4<object?> referenceStorage = default;
            InlineArray4<ulong> primitiveStorage = default;
            InlineArray4<IntPtr> byrefStorage = default;
            Span<object?> references = argumentCount <= MaxStackAttributeArguments
                ? ((Span<object?>)referenceStorage).Slice(0, argumentCount)
                : new object?[argumentCount];
            Span<ulong> primitives = argumentCount <= MaxStackAttributeArguments
                ? primitiveStorage
                : new ulong[argumentCount];
            Span<IntPtr> byrefs = argumentCount <= MaxStackAttributeArguments
                ? byrefStorage
                : new IntPtr[argumentCount];

            fixed (IntPtr* argumentStorage = byrefs)
            {
                GCFrameRegistration registration = new((void**)argumentStorage, (uint)argumentCount, areByRefs: true);
                try
                {
                    GCFrameRegistration.RegisterForGCReporting(&registration);
                    for (int i = 0; i < argumentCount; i++)
                    {
                        RuntimeType argumentType = argumentTypes[i];
                        CustomAttributeEncoding encoding = GetPrimitiveEncoding(argumentType);
                        int size = GetPrimitiveSize(encoding);
                        if (size != 0)
                        {
                            Span<byte> value = MemoryMarshal.AsBytes(primitives.Slice(i, 1)).Slice(0, size);
                            ReadPrimitiveData(ref parser, encoding).CopyTo(value);
                            if (!BitConverter.IsLittleEndian)
                            {
                                value.Reverse();
                            }
                            StoreArgumentReference(argumentStorage + i, ref MemoryMarshal.GetReference(value));
                        }
                        else
                        {
                            references[i] = ReadAttributeValue(ref parser, argumentType, module);
                            StoreArgumentReference(argumentStorage + i, ref references[i]);
                        }
                    }

                    namedArgs = parser.Remaining == 0 ? 0 : parser.GetU2();
                    if (namedArgs == 0 && parser.Remaining != 0)
                    {
                        throw new CustomAttributeFormatException();
                    }

                    blob += parser.Consumed;
                    return constructor.InvokeDirectByRef(argumentStorage);
                }
                finally
                {
                    GCFrameRegistration.UnregisterForGCReporting(&registration);
                    GC.KeepAlive(module);
                    GC.KeepAlive(type);
                }
            }
        }

        // The caller registers this native vector as byrefs for the lifetime of the invocation.
        private static void StoreArgumentReference<T>(IntPtr* storage, scoped ref T value) =>
            *storage = (IntPtr)Unsafe.AsPointer(ref value);

        private static void GetPropertyOrFieldData(
            ResolutionScope module, ref IntPtr blobStart, IntPtr blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value)
        {
            try
            {
#if NATIVEAOT
                var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blobStart, blobEnd), module);
#else
                var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blobStart, blobEnd));
#endif
                CustomAttributeEncoding memberKind = parser.GetTag();
                if (memberKind is not CustomAttributeEncoding.Field and not CustomAttributeEncoding.Property)
                {
                    throw new CustomAttributeFormatException();
                }
                isProperty = memberKind == CustomAttributeEncoding.Property;

                CustomAttributeEncoding encoding = parser.GetTag();
                RuntimeType declaredType = ReadAttributeType(ref parser, encoding, module);
                name = parser.GetString()!;
                value = ReadAttributeValue(ref parser, declaredType, module);

                type = encoding is CustomAttributeEncoding.Object or CustomAttributeEncoding.Enum ? declaredType : null;
                if (value is null && encoding is CustomAttributeEncoding.String or CustomAttributeEncoding.Type or CustomAttributeEncoding.Array)
                {
                    // A null enum array leaves type unspecified for the existing named-member lookup.
                    if (encoding != CustomAttributeEncoding.Array || !declaredType.GetElementType()!.IsEnum)
                    {
                        type = declaredType;
                    }
                }

                blobStart += parser.Consumed;
            }
            finally
            {
                GC.KeepAlive(module);
            }
        }

        private static ReadOnlySpan<byte> GetAttributeBlob(IntPtr start, IntPtr end)
        {
            nuint length = (nuint)end - (nuint)start;
            if (length > int.MaxValue)
            {
                throw new CustomAttributeFormatException();
            }

            return new ReadOnlySpan<byte>((void*)start, (int)length);
        }

        private static CustomAttributeEncoding GetPrimitiveEncoding(RuntimeType type) =>
            RuntimeCustomAttributeData.TypeToCustomAttributeEncoding(type.IsActualEnum
                ? (RuntimeType)type.GetEnumUnderlyingType()
                : type);

        private static int GetPrimitiveSize(CustomAttributeEncoding encoding) => encoding switch
        {
            CustomAttributeEncoding.Boolean or CustomAttributeEncoding.SByte or CustomAttributeEncoding.Byte => 1,
            CustomAttributeEncoding.Char or CustomAttributeEncoding.Int16 or CustomAttributeEncoding.UInt16 => 2,
            CustomAttributeEncoding.Int32 or CustomAttributeEncoding.UInt32 or CustomAttributeEncoding.Float => 4,
            CustomAttributeEncoding.Int64 or CustomAttributeEncoding.UInt64 or CustomAttributeEncoding.Double => 8,
            _ => 0
        };

        private static ReadOnlySpan<byte> ReadPrimitiveData(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, CustomAttributeEncoding encoding)
        {
            int size = GetPrimitiveSize(encoding);
            if (size == 0)
            {
                throw new CustomAttributeFormatException();
            }

            return parser.ReadData(size);
        }

        private static object? ReadAttributeValue(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, RuntimeType type, ResolutionScope module)
        {
            CustomAttributeEncoding encoding = GetPrimitiveEncoding(type);
            int size = GetPrimitiveSize(encoding);
            if (size != 0)
            {
                ReadOnlySpan<byte> data = ReadPrimitiveData(ref parser, encoding);
                if (BitConverter.IsLittleEndian || size == 1)
                {
                    return RuntimeHelpers.Box(ref MemoryMarshal.GetReference(data), type.TypeHandle);
                }

                InlineArray8<byte> reversedStorage = default;
                Span<byte> reversed = ((Span<byte>)reversedStorage).Slice(0, size);
                data.CopyTo(reversed);
                reversed.Reverse();
                return RuntimeHelpers.Box(ref MemoryMarshal.GetReference(reversed), type.TypeHandle);
            }

            if (type == typeof(string))
            {
                return parser.GetString();
            }

            if (type == typeof(Type))
            {
                return ReadAttributeTypeName(ref parser, module);
            }

            if (type == typeof(object))
            {
                RuntimeType valueType = ReadAttributeType(ref parser, parser.GetTag(), module);
                return ReadAttributeValue(ref parser, valueType, module);
            }

            if (type.IsSZArray)
            {
                return ReadAttributeArray(ref parser, (RuntimeType)type.GetElementType()!, module);
            }

            throw new CustomAttributeFormatException();
        }

#if NATIVEAOT
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs.")]
#endif
        private static RuntimeType ReadAttributeType(
            ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, CustomAttributeEncoding encoding, ResolutionScope module, bool allowArray = true)
        {
            if (GetPrimitiveSize(encoding) != 0 || encoding is CustomAttributeEncoding.String or CustomAttributeEncoding.Type or CustomAttributeEncoding.Object)
            {
                return (RuntimeType)CustomAttributeTypedArgument.CustomAttributeEncodingToType(encoding);
            }

            if (encoding == CustomAttributeEncoding.Enum)
            {
                RuntimeType? enumType = ReadAttributeTypeName(ref parser, module);
                if (enumType is null || !enumType.IsActualEnum)
                {
                    throw new CustomAttributeFormatException();
                }
                return enumType;
            }

            if (allowArray && encoding == CustomAttributeEncoding.Array)
            {
                RuntimeType elementType = ReadAttributeType(ref parser, parser.GetTag(), module, allowArray: false);
                return (RuntimeType)elementType.MakeArrayType();
            }

            throw new CustomAttributeFormatException();
        }

        private static RuntimeType? ReadAttributeTypeName(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, ResolutionScope module)
        {
#if NATIVEAOT
            return parser.GetTypeValue();
#else
            ReadOnlySpan<byte> utf8Name = parser.GetStringBytes(out bool isNull);
            if (isNull)
            {
                return null;
            }
            if (utf8Name.IsEmpty)
            {
                throw new CustomAttributeFormatException();
            }

            // Type names in the native metadata path are null-terminated, unlike string values.
            int terminator = utf8Name.IndexOf((byte)0);
            if (terminator >= 0)
            {
                utf8Name = utf8Name.Slice(0, terminator);
            }

            const int StackNameLength = 128;
            char[]? rented = null;
            Span<char> name = utf8Name.Length <= StackNameLength
                ? stackalloc char[StackNameLength]
                : (rented = System.Buffers.ArrayPool<char>.Shared.Rent(utf8Name.Length));
            try
            {
                int written = Encoding.UTF8.GetChars(utf8Name, name);
                return TypeNameResolver.GetTypeHelper(name.Slice(0, written), module.GetRuntimeAssembly(),
                    throwOnError: true, requireAssemblyQualifiedName: false);
            }
            finally
            {
                if (rented is not null)
                {
                    System.Buffers.ArrayPool<char>.Shared.Return(rented);
                }
            }
#endif
        }

#if NATIVEAOT
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs.")]
#endif
        private static Array? ReadAttributeArray(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, RuntimeType elementType, ResolutionScope module)
        {
            int length = parser.GetI4();
            if (length == -1)
            {
                return null;
            }
            if (length < 0)
            {
                throw new OverflowException();
            }

            CustomAttributeEncoding encoding = GetPrimitiveEncoding(elementType);
            int elementSize = GetPrimitiveSize(encoding);
            if (elementSize != 0)
            {
                if (length > parser.Remaining / elementSize)
                {
                    throw new CustomAttributeFormatException();
                }

                Array array = Array.CreateInstance(elementType, length);
                Span<byte> data = new(ref MemoryMarshal.GetArrayDataReference(array), length * elementSize);
                if (BitConverter.IsLittleEndian || elementSize == 1)
                {
                    parser.ReadData(data.Length).CopyTo(data);
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        Span<byte> element = data.Slice(i * elementSize, elementSize);
                        ReadPrimitiveData(ref parser, encoding).CopyTo(element);
                        element.Reverse();
                    }
                }
                return array;
            }

            if (elementType != typeof(string) && elementType != typeof(Type) &&
                elementType != typeof(object) && !elementType.IsSZArray)
            {
                throw new CustomAttributeFormatException();
            }

            // Every reference element consumes at least a string/tag/array-length byte.
            if (length > parser.Remaining)
            {
                throw new CustomAttributeFormatException();
            }
            var references = (object?[])Array.CreateInstance(elementType, length);
            for (int i = 0; i < references.Length; i++)
            {
                references[i] = ReadAttributeValue(ref parser, elementType, module);
            }
            return references;
        }

        internal static bool IsDefined(RuntimeType type, RuntimeType? caType, bool inherit)
        {
            Debug.Assert(type is not null);

            if (type.GetElementType() is not null)
                return false;

            if (PseudoCustomAttribute.IsDefined(type, caType))
                return true;

            if (IsCustomAttributeDefined(new QCustomAttributeList(type), caType))
                return true;

            if (!inherit)
                return false;

            type = (type.BaseType as RuntimeType)!;

            while (type is not null)
            {
                if (IsCustomAttributeDefined(new QCustomAttributeList(type), caType, inherit))
                    return true;

                type = (type.BaseType as RuntimeType)!;
            }

            return false;
        }

        internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
        {
            Debug.Assert(method is not null);
            Debug.Assert(caType is not null);

            if (PseudoCustomAttribute.IsDefined(method, caType))
                return true;

            if (IsCustomAttributeDefined(new QCustomAttributeList(method), caType))
                return true;

            if (!inherit)
                return false;

            method = method.GetParentDefinition()!;

            while (method is not null)
            {
                if (IsCustomAttributeDefined(new QCustomAttributeList(method), caType, inherit))
                    return true;

                method = method.GetParentDefinition()!;
            }

            return false;
        }

        internal static bool IsDefined(RuntimeConstructorInfo ctor, RuntimeType caType)
        {
            Debug.Assert(ctor is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeConstructorInfo

            return IsCustomAttributeDefined(new QCustomAttributeList(ctor), caType);
        }

        internal static bool IsDefined(RuntimePropertyInfo property, RuntimeType caType)
        {
            Debug.Assert(property is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimePropertyInfo

            return IsCustomAttributeDefined(new QCustomAttributeList(property), caType);
        }

        internal static bool IsDefined(RuntimeEventInfo e, RuntimeType caType)
        {
            Debug.Assert(e is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeEventInfo

            return IsCustomAttributeDefined(new QCustomAttributeList(e), caType);
        }

        internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType caType)
        {
            Debug.Assert(field is not null);
            Debug.Assert(caType is not null);

            if (PseudoCustomAttribute.IsDefined(field, caType))
                return true;

            return IsCustomAttributeDefined(new QCustomAttributeList(field), caType);
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Debug.Assert(parameter is not null);
            Debug.Assert(caType is not null);

            if (PseudoCustomAttribute.IsDefined(parameter, caType))
                return true;

            return IsCustomAttributeDefined(new QCustomAttributeList(parameter), caType);
        }

        internal static bool IsDefined(RuntimeAssembly assembly, RuntimeType caType)
        {
            Debug.Assert(assembly is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeAssembly
            return IsCustomAttributeDefined(new QCustomAttributeList(assembly), caType);
        }

        internal static bool IsDefined(RuntimeModule module, RuntimeType caType)
        {
            Debug.Assert(module is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeModule

            return IsCustomAttributeDefined(new QCustomAttributeList(module), caType);
        }

        internal static object[] GetCustomAttributes(RuntimeType type, RuntimeType caType, bool inherit)
        {
            Debug.Assert(type is not null);
            Debug.Assert(caType is not null);

            if (type.GetElementType() is not null)
                return CreateAttributeArrayHelper(caType, 0);

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                type = (type.GetGenericTypeDefinition() as RuntimeType)!;

            ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(type, caType, ref pcas);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !GetAttributeUsage(caType).Inherited))
            {
                object[] attributes = GetCustomAttributes(new QCustomAttributeList(type), pcas.Count, caType);
                if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
                return attributes;
            }

            ListBuilder<object> result = default;
            bool mustBeInheritable = false;

            for (int i = 0; i < pcas.Count; i++)
                result.Add(pcas[i]);

            do
            {
                AddCustomAttributes(ref result, new QCustomAttributeList(type), caType, mustBeInheritable, result);
                mustBeInheritable = true;
                type = (type.BaseType as RuntimeType)!;
            } while (type != (RuntimeType)typeof(object) && type != null);

            object[] typedResult = CreateAttributeArrayHelper(caType, result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                typedResult[i] = result[i];
            }
            return typedResult;
        }

        internal static object[] GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
        {
            Debug.Assert(method is not null);
            Debug.Assert(caType is not null);

            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                method = (method.GetGenericMethodDefinition() as RuntimeMethodInfo)!;

            ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(method, caType, ref pcas);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !GetAttributeUsage(caType).Inherited))
            {
                object[] attributes = GetCustomAttributes(new QCustomAttributeList(method), pcas.Count, caType);
                if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
                return attributes;
            }

            ListBuilder<object> result = default;
            bool mustBeInheritable = false;

            for (int i = 0; i < pcas.Count; i++)
                result.Add(pcas[i]);

            while (method != null)
            {
                AddCustomAttributes(ref result, new QCustomAttributeList(method), caType, mustBeInheritable, result);
                mustBeInheritable = true;
                method = method.GetParentDefinition()!;
            }

            object[] typedResult = CreateAttributeArrayHelper(caType, result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                typedResult[i] = result[i];
            }
            return typedResult;
        }

        internal static object[] GetCustomAttributes(RuntimeConstructorInfo ctor, RuntimeType caType)
        {
            Debug.Assert(ctor != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeConstructorInfo

            return GetCustomAttributes(new QCustomAttributeList(ctor), 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimePropertyInfo property, RuntimeType caType)
        {
            Debug.Assert(property is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimePropertyInfo

            return GetCustomAttributes(new QCustomAttributeList(property), 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimeEventInfo e, RuntimeType caType)
        {
            Debug.Assert(e is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeEventInfo

            return GetCustomAttributes(new QCustomAttributeList(e), 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType)
        {
            Debug.Assert(field is not null);
            Debug.Assert(caType is not null);

            ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(field, caType, ref pcas);
            object[] attributes = GetCustomAttributes(new QCustomAttributeList(field), pcas.Count, caType);
            if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
            return attributes;
        }

        internal static object[] GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Debug.Assert(parameter is not null);
            Debug.Assert(caType is not null);

            ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(parameter, caType, ref pcas);
            object[] attributes = GetCustomAttributes(new QCustomAttributeList(parameter), pcas.Count, caType);
            if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
            return attributes;
        }

        internal static object[] GetCustomAttributes(RuntimeAssembly assembly, RuntimeType caType)
        {
            Debug.Assert(assembly is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeAssembly

            return GetCustomAttributes(new QCustomAttributeList(assembly), 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimeModule module, RuntimeType caType)
        {
            Debug.Assert(module is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeModule

            return GetCustomAttributes(new QCustomAttributeList(module), 0, caType);
        }

        private static object[] GetCustomAttributes(
            QCustomAttributeList customAttributes,
            int pcaCount, RuntimeType attributeFilterType)
        {
            ListBuilder<object> attributes = default;

            AddCustomAttributes(ref attributes, customAttributes, attributeFilterType, false, default);

            object[] result = CreateAttributeArrayHelper(attributeFilterType, attributes.Count + pcaCount);
            for (int i = 0; i < attributes.Count; i++)
            {
                result[i] = attributes[i];
            }
            return result;
        }

        private static bool MatchesTypeFilter(RuntimeType attributeType, RuntimeType attributeFilterType)
        {
            if (attributeFilterType.IsGenericTypeDefinition)
            {
                for (RuntimeType? type = attributeType; type != null; type = (RuntimeType?)type.BaseType)
                {
                    if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == attributeFilterType)
                    {
                        return true;
                    }
                }
                return false;
            }

            return attributeFilterType.IsAssignableFrom(attributeType);
        }
        private static bool AttributeUsageCheck(
            RuntimeType attributeType, bool mustBeInheritable, ref ListBuilder<object> derivedAttributes)
        {
            AttributeUsageAttribute? attributeUsageAttribute = null;

            if (mustBeInheritable)
            {
                attributeUsageAttribute = GetAttributeUsage(attributeType);

                if (!attributeUsageAttribute.Inherited)
                    return false;
            }

            // Legacy: AllowMultiple ignored for none inheritable attributes
            if (derivedAttributes.Count == 0)
                return true;

            for (int i = 0; i < derivedAttributes.Count; i++)
            {
                if (derivedAttributes[i].GetType() == attributeType)
                {
                    attributeUsageAttribute ??= GetAttributeUsage(attributeType);
                    return attributeUsageAttribute.AllowMultiple;
                }
            }

            return true;
        }

#if !NATIVEAOT
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Module.ResolveType is marked as RequiresUnreferencedCode because it relies on tokens" +
                            "which are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break." +
                            "The usage here is not like that as all these tokens come from existing metadata loaded from some IL" +
                            "and so trimming has no effect (the tokens are read AFTER trimming occurred).")]
#endif
        internal static AttributeUsageAttribute GetAttributeUsage(RuntimeType decoratedAttribute)
        {
#if NATIVEAOT
            return Attribute.InternalGetAttributeUsage(decoratedAttribute);
#else
            RuntimeModule decoratedModule = decoratedAttribute.GetRuntimeModule();
            MetadataImport scope = decoratedModule.MetadataImport;
            CustomAttributeRecord[] car = RuntimeCustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedAttribute.MetadataToken);

            AttributeUsageAttribute? attributeUsageAttribute = null;

            for (int i = 0; i < car.Length; i++)
            {
                ref CustomAttributeRecord caRecord = ref car[i];
                RuntimeType? attributeType = decoratedModule.ResolveType(scope.GetParentToken(caRecord.tkCtor), null, null) as RuntimeType;

                if (attributeType != (RuntimeType)typeof(AttributeUsageAttribute))
                    continue;

                if (attributeUsageAttribute is not null)
                    throw new FormatException(SR.Format(SR.Format_AttributeUsage, attributeType));

                if (!ParseAttributeUsageAttribute(
                    caRecord.blob,
                    out AttributeTargets attrTargets,
                    out bool allowMultiple,
                    out bool inherited))
                {
                    throw new CustomAttributeFormatException();
                }

                attributeUsageAttribute = new AttributeUsageAttribute(attrTargets, allowMultiple: allowMultiple, inherited: inherited);
            }

            return attributeUsageAttribute ?? AttributeUsageAttribute.Default;
#endif
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Array.CreateInstance is only used with reference types here and is therefore safe.")]
        internal static object[] CreateAttributeArrayHelper(RuntimeType caType, int elementCount)
        {
            bool useAttributeArray = false;
            bool useObjectArray = false;

            if (caType == typeof(Attribute))
            {
                useAttributeArray = true;
            }
            else if (caType.IsActualValueType)
            {
                useObjectArray = true;
            }
            else if (caType.ContainsGenericParameters)
            {
                if (caType.IsSubclassOf(typeof(Attribute)))
                {
                    useAttributeArray = true;
                }
                else
                {
                    useObjectArray = true;
                }
            }

            if (useAttributeArray)
            {
                return elementCount == 0 ? [] : new Attribute[elementCount];
            }
            if (useObjectArray)
            {
                return elementCount == 0 ? [] : new object[elementCount];
            }
#if NATIVEAOT
            return (object[])Array.CreateInstance(caType, elementCount);
#else
            return elementCount == 0 ? caType.GetEmptyArray() : (object[])Array.CreateInstance(caType, elementCount);
#endif
        }

    }

    internal static partial class PseudoCustomAttribute
    {
        #region Private Static Data Members
        // Here we can avoid the need to take a lock when using Dictionary by rearranging
        // the only method that adds values to the Dictionary. For more details on
        // Dictionary versus Hashtable thread safety:
        // See code:Dictionary#DictionaryVersusHashtableThreadSafety
        private static readonly HashSet<RuntimeType> s_pca = CreatePseudoCustomAttributeHashSet();
        #endregion

        #region Static Constructor
        private static HashSet<RuntimeType> CreatePseudoCustomAttributeHashSet()
        {
            Type[] pcas =
            [
                // See https://github.com/dotnet/runtime/blob/main/src/coreclr/md/compiler/custattr_emit.cpp
                typeof(FieldOffsetAttribute), // field
                typeof(SerializableAttribute), // class, struct, enum, delegate
                typeof(MarshalAsAttribute), // parameter, field, return-value
                typeof(ComImportAttribute), // class, interface
                typeof(NonSerializedAttribute), // field, inherited
                typeof(InAttribute), // parameter
                typeof(OutAttribute), // parameter
                typeof(OptionalAttribute), // parameter
                typeof(DllImportAttribute), // method
                typeof(PreserveSigAttribute), // method
                typeof(TypeForwardedToAttribute), // assembly
            ];

            HashSet<RuntimeType> set = new HashSet<RuntimeType>(pcas.Length);
            foreach (RuntimeType runtimeType in pcas)
            {
#if !NATIVEAOT
                VerifyPseudoCustomAttribute(runtimeType);
#endif
                set.Add(runtimeType);
            }
            return set;
        }

        #endregion

        #region Internal Static
        internal static void GetCustomAttributes(RuntimeType type, RuntimeType caType, ref ListBuilder<Attribute> pcas)
        {
            Debug.Assert(type is not null);
            Debug.Assert(caType is not null);

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType))
                return;

#pragma warning disable SYSLIB0050 // Legacy serialization infrastructure is obsolete
            if (all || caType == typeof(SerializableAttribute))
            {
                if ((type.Attributes & TypeAttributes.Serializable) != 0)
                    pcas.Add(new SerializableAttribute());
            }
#pragma warning restore SYSLIB0050
            if (all || caType == typeof(ComImportAttribute))
            {
                if ((type.Attributes & TypeAttributes.Import) != 0)
                    pcas.Add(new ComImportAttribute());
            }
        }
        internal static bool IsDefined(RuntimeType type, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType!))
                return false;

#pragma warning disable SYSLIB0050 // Legacy serialization infrastructure is obsolete
            if (all || caType == typeof(SerializableAttribute))
            {
                if ((type.Attributes & TypeAttributes.Serializable) != 0)
                    return true;
            }
#pragma warning restore SYSLIB0050
            if (all || caType == typeof(ComImportAttribute))
            {
                if ((type.Attributes & TypeAttributes.Import) != 0)
                    return true;
            }

            return false;
        }

        internal static void GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, ref ListBuilder<Attribute> pcas)
        {
            Debug.Assert(method is not null);
            Debug.Assert(caType is not null);

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType))
                return;

            // Native AOT doesn't have the metadata to materialize this attribute https://github.com/dotnet/runtimelab/issues/830
#if !NATIVEAOT
            if (all || caType == typeof(DllImportAttribute))
            {
                Attribute? pca = GetDllImportCustomAttribute(method);
                if (pca is not null) pcas.Add(pca);
            }
#endif
            if (all || caType == typeof(PreserveSigAttribute))
            {
                if ((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0)
                    pcas.Add(new PreserveSigAttribute());
            }
        }
        internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType!))
                return false;

            // Native AOT doesn't have the metadata to materialize this attribute https://github.com/dotnet/runtimelab/issues/830
#if !NATIVEAOT
            if (all || caType == typeof(DllImportAttribute))
            {
                if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
                    return true;
            }
#endif
            if (all || caType == typeof(PreserveSigAttribute))
            {
                if ((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0)
                    return true;
            }

            return false;
        }

        internal static void GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType, ref ListBuilder<Attribute> pcas)
        {
            Debug.Assert(parameter is not null);
            Debug.Assert(caType is not null);

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType))
                return;

            if (all || caType == typeof(InAttribute))
            {
                if (parameter.IsIn)
                    pcas.Add(new InAttribute());
            }
            if (all || caType == typeof(OutAttribute))
            {
                if (parameter.IsOut)
                    pcas.Add(new OutAttribute());
            }
            if (all || caType == typeof(OptionalAttribute))
            {
                if (parameter.IsOptional)
                    pcas.Add(new OptionalAttribute());
            }
            // Native AOT doesn't have the metadata to materialize this attribute https://github.com/dotnet/runtimelab/issues/830
#if !NATIVEAOT
            if (all || caType == typeof(MarshalAsAttribute))
            {
                Attribute? pca = GetMarshalAsCustomAttribute(parameter);
                if (pca is not null) pcas.Add(pca);
            }
#endif
        }
        internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType!))
                return false;

            if (all || caType == typeof(InAttribute))
            {
                if (parameter.IsIn) return true;
            }
            if (all || caType == typeof(OutAttribute))
            {
                if (parameter.IsOut) return true;
            }
            if (all || caType == typeof(OptionalAttribute))
            {
                if (parameter.IsOptional) return true;
            }
            // Native AOT doesn't have the metadata to materialize this attribute https://github.com/dotnet/runtimelab/issues/830
#if !NATIVEAOT
            if (all || caType == typeof(MarshalAsAttribute))
            {
                if (GetMarshalAsCustomAttribute(parameter) is not null) return true;
            }
#endif

            return false;
        }

        internal static void GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType, ref ListBuilder<Attribute> pcas)
        {
            Debug.Assert(field is not null);
            Debug.Assert(caType is not null);

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType))
                return;

            Attribute? pca;

            // Native AOT doesn't have the metadata to materialize this attribute https://github.com/dotnet/runtimelab/issues/830
#if !NATIVEAOT
            if (all || caType == typeof(MarshalAsAttribute))
            {
                pca = GetMarshalAsCustomAttribute(field);
                if (pca is not null) pcas.Add(pca);
            }
#endif
            if (all || caType == typeof(FieldOffsetAttribute))
            {
                pca = GetFieldOffsetCustomAttribute(field);
                if (pca is not null) pcas.Add(pca);
            }
#pragma warning disable SYSLIB0050 // Legacy serialization infrastructure is obsolete
            if (all || caType == typeof(NonSerializedAttribute))
            {
                if ((field.Attributes & FieldAttributes.NotSerialized) != 0)
                    pcas.Add(new NonSerializedAttribute());
            }
#pragma warning restore SYSLIB0050
        }
        internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType!))
                return false;

            // Native AOT doesn't have the metadata to materialize this attribute https://github.com/dotnet/runtimelab/issues/830
#if !NATIVEAOT
            if (all || caType == typeof(MarshalAsAttribute))
            {
                if (GetMarshalAsCustomAttribute(field) is not null) return true;
            }
#endif
            if (all || caType == typeof(FieldOffsetAttribute))
            {
                if (GetFieldOffsetCustomAttribute(field) is not null) return true;
            }
#pragma warning disable SYSLIB0050 // Legacy serialization infrastructure is obsolete
            if (all || caType == typeof(NonSerializedAttribute))
            {
                if ((field.Attributes & FieldAttributes.NotSerialized) != 0)
                    return true;
            }
#pragma warning restore SYSLIB0050

            return false;
        }
        #endregion

    }
}
