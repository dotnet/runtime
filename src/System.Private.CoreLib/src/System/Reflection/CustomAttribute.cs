// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection
{
    public class CustomAttributeData
    {
        #region Public Static Members
        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Module target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Assembly target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }
        #endregion

        #region Internal Static Members
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeType target)
        {
            Debug.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), out RuntimeType.ListBuilder<Attribute> pcas);
            return GetCombinedList(cad, ref pcas);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeFieldInfo target)
        {
            Debug.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), out RuntimeType.ListBuilder<Attribute> pcas);
            return GetCombinedList(cad, ref pcas);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeMethodInfo target)
        {
            Debug.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), out RuntimeType.ListBuilder<Attribute> pcas);
            return GetCombinedList(cad, ref pcas);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeConstructorInfo target)
        {
            Debug.Assert(target != null);

            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeEventInfo target)
        {
            Debug.Assert(target != null);

            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimePropertyInfo target)
        {
            Debug.Assert(target != null);

            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeModule target)
        {
            Debug.Assert(target != null);

            if (target.IsResource())
                return new List<CustomAttributeData>();

            return GetCustomAttributes(target, target.MetadataToken);
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeAssembly target)
        {
            Debug.Assert(target != null);

            // No pseudo attributes for RuntimeAssembly

            return GetCustomAttributes((RuntimeModule)target.ManifestModule!, RuntimeAssembly.GetToken(target.GetNativeHandle()));
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeParameterInfo target)
        {
            Debug.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule()!, target.MetadataToken);
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), out RuntimeType.ListBuilder<Attribute> pcas);
            return GetCombinedList(cad, ref pcas);
        }

        private static IList<CustomAttributeData> GetCombinedList(IList<CustomAttributeData> customAttributes, ref RuntimeType.ListBuilder<Attribute> pseudoAttributes)
        {
            if (pseudoAttributes.Count == 0)
                return customAttributes;

            CustomAttributeData[] pca = new CustomAttributeData[customAttributes.Count + pseudoAttributes.Count];
            customAttributes.CopyTo(pca, pseudoAttributes.Count);
            for (int i = 0; i < pseudoAttributes.Count; i++)
            {
                pca[i] = new CustomAttributeData(pseudoAttributes[i]);
            }

            return Array.AsReadOnly(pca);
        }
        #endregion

        #region Private Static Methods
        private static CustomAttributeEncoding TypeToCustomAttributeEncoding(RuntimeType type)
        {
            if (type == typeof(int))
                return CustomAttributeEncoding.Int32;

            if (type.IsEnum)
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

            if (type.IsInterface)
                return CustomAttributeEncoding.Object;

            if (type.IsValueType)
                return CustomAttributeEncoding.Undefined;

            throw new ArgumentException(SR.Argument_InvalidKindOfTypeForCA, nameof(type));
        }
        private static CustomAttributeType InitCustomAttributeType(RuntimeType parameterType)
        {
            CustomAttributeEncoding encodedType = CustomAttributeData.TypeToCustomAttributeEncoding(parameterType);
            CustomAttributeEncoding encodedArrayType = CustomAttributeEncoding.Undefined;
            CustomAttributeEncoding encodedEnumType = CustomAttributeEncoding.Undefined;
            string? enumName = null;

            if (encodedType == CustomAttributeEncoding.Array)
            {
                parameterType = (RuntimeType)parameterType.GetElementType();
                encodedArrayType = CustomAttributeData.TypeToCustomAttributeEncoding(parameterType);
            }

            if (encodedType == CustomAttributeEncoding.Enum || encodedArrayType == CustomAttributeEncoding.Enum)
            {
                encodedEnumType = TypeToCustomAttributeEncoding((RuntimeType)Enum.GetUnderlyingType(parameterType));
                enumName = parameterType.AssemblyQualifiedName;
            }

            return new CustomAttributeType(encodedType, encodedArrayType, encodedEnumType, enumName);
        }
        private static IList<CustomAttributeData> GetCustomAttributes(RuntimeModule module, int tkTarget)
        {
            CustomAttributeRecord[] records = GetCustomAttributeRecords(module, tkTarget);

            CustomAttributeData[] customAttributes = new CustomAttributeData[records.Length];
            for (int i = 0; i < records.Length; i++)
                customAttributes[i] = new CustomAttributeData(module, records[i].tkCtor, in records[i].blob);

            return Array.AsReadOnly(customAttributes);
        }
        #endregion

        #region Internal Static Members
        internal static CustomAttributeRecord[] GetCustomAttributeRecords(RuntimeModule module, int targetToken)
        {
            MetadataImport scope = module.MetadataImport;

            scope.EnumCustomAttributes(targetToken, out MetadataEnumResult tkCustomAttributeTokens);

            if (tkCustomAttributeTokens.Length == 0)
            {
                return Array.Empty<CustomAttributeRecord>();
            }

            CustomAttributeRecord[] records = new CustomAttributeRecord[tkCustomAttributeTokens.Length];

            for (int i = 0; i < records.Length; i++)
            {
                scope.GetCustomAttributeProps(tkCustomAttributeTokens[i], 
                    out records[i].tkCtor.Value, out records[i].blob);
            }

            return records;
        }

        internal static CustomAttributeTypedArgument Filter(IList<CustomAttributeData> attrs, Type? caType, int parameter)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                if (attrs[i].Constructor.DeclaringType == caType)
                {
                    return attrs[i].ConstructorArguments[parameter];
                }
            }

            return new CustomAttributeTypedArgument();
        }
        #endregion

        private ConstructorInfo m_ctor = null!;
        private readonly RuntimeModule m_scope = null!;
        private readonly MemberInfo[] m_members = null!;
        private readonly CustomAttributeCtorParameter[] m_ctorParams = null!;
        private readonly CustomAttributeNamedParameter[] m_namedParams = null!;
        private IList<CustomAttributeTypedArgument> m_typedCtorArgs = null!;
        private IList<CustomAttributeNamedArgument> m_namedArgs = null!;

        #region Constructor
        protected CustomAttributeData()
        {
        }

        private CustomAttributeData(RuntimeModule scope, MetadataToken caCtorToken, in ConstArray blob)
        {
            m_scope = scope;
            m_ctor = (RuntimeConstructorInfo)RuntimeType.GetMethodBase(scope, caCtorToken)!;

            ParameterInfo[] parameters = m_ctor.GetParametersNoCopy();
            m_ctorParams = new CustomAttributeCtorParameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                m_ctorParams[i] = new CustomAttributeCtorParameter(InitCustomAttributeType((RuntimeType)parameters[i].ParameterType));

            FieldInfo[] fields = m_ctor.DeclaringType!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo[] properties = m_ctor.DeclaringType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m_namedParams = new CustomAttributeNamedParameter[properties.Length + fields.Length];
            for (int i = 0; i < fields.Length; i++)
                m_namedParams[i] = new CustomAttributeNamedParameter(
                    fields[i].Name, CustomAttributeEncoding.Field, InitCustomAttributeType((RuntimeType)fields[i].FieldType));
            for (int i = 0; i < properties.Length; i++)
                m_namedParams[i + fields.Length] = new CustomAttributeNamedParameter(
                    properties[i].Name, CustomAttributeEncoding.Property, InitCustomAttributeType((RuntimeType)properties[i].PropertyType));

            m_members = new MemberInfo[fields.Length + properties.Length];
            fields.CopyTo(m_members, 0);
            properties.CopyTo(m_members, fields.Length);

            CustomAttributeEncodedArgument.ParseAttributeArguments(blob, ref m_ctorParams, ref m_namedParams, m_scope);
        }
        #endregion

        #region Pseudo Custom Attribute Constructor
        internal CustomAttributeData(Attribute attribute)
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
            m_namedArgs = Array.AsReadOnly(Array.Empty<CustomAttributeNamedArgument>());
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
            if (marshalAs.MarshalType != null) i++;
            if (marshalAs.MarshalTypeRef != null) i++;
            if (marshalAs.MarshalCookie != null) i++;
            i++; // IidParameterIndex
            i++; // SafeArraySubType
            if (marshalAs.SafeArrayUserDefinedSubType != null) i++;
            CustomAttributeNamedArgument[] namedArgs = new CustomAttributeNamedArgument[i];

            // For compatibility with previous runtimes, we always include the following 5 attributes, regardless
            // of if they apply to the UnmanagedType being marshaled or not.
            i = 0;
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("ArraySubType")!, marshalAs.ArraySubType);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SizeParamIndex")!, marshalAs.SizeParamIndex);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SizeConst")!, marshalAs.SizeConst);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("IidParameterIndex")!, marshalAs.IidParameterIndex);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SafeArraySubType")!, marshalAs.SafeArraySubType);
            if (marshalAs.MarshalType != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalType")!, marshalAs.MarshalType);
            if (marshalAs.MarshalTypeRef != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalTypeRef")!, marshalAs.MarshalTypeRef);
            if (marshalAs.MarshalCookie != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalCookie")!, marshalAs.MarshalCookie);
            if (marshalAs.SafeArrayUserDefinedSubType != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SafeArrayUserDefinedSubType")!, marshalAs.SafeArrayUserDefinedSubType);

            m_namedArgs = Array.AsReadOnly(namedArgs);
        }
        private void Init(TypeForwardedToAttribute forwardedTo)
        {
            Type type = typeof(TypeForwardedToAttribute);

            Type[] sig = new Type[] { typeof(Type) };
            m_ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, sig, null)!;

            CustomAttributeTypedArgument[] typedArgs = new CustomAttributeTypedArgument[1];
            typedArgs[0] = new CustomAttributeTypedArgument(typeof(Type), forwardedTo.Destination);
            m_typedCtorArgs = Array.AsReadOnly(typedArgs);

            CustomAttributeNamedArgument[] namedArgs = Array.Empty<CustomAttributeNamedArgument>();
            m_namedArgs = Array.AsReadOnly(namedArgs);
        }
        private void Init(object pca)
        {
            m_ctor = pca.GetType().GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            m_typedCtorArgs = Array.AsReadOnly(Array.Empty<CustomAttributeTypedArgument>());
            m_namedArgs = Array.AsReadOnly(Array.Empty<CustomAttributeNamedArgument>());
        }
        #endregion

        #region Object Override
        public override string ToString()
        {
            string ctorArgs = "";
            for (int i = 0; i < ConstructorArguments.Count; i++)
                ctorArgs += string.Format(i == 0 ? "{0}" : ", {0}", ConstructorArguments[i]);

            string namedArgs = "";
            for (int i = 0; i < NamedArguments.Count; i++)
                namedArgs += string.Format(i == 0 && ctorArgs.Length == 0 ? "{0}" : ", {0}", NamedArguments[i]);

            return string.Format("[{0}({1}{2})]", Constructor.DeclaringType!.FullName, ctorArgs, namedArgs);
        }
        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object? obj) => obj == (object)this;
        #endregion

        #region Public Members
        public virtual Type? AttributeType { get { return Constructor.DeclaringType; } }

        public virtual ConstructorInfo Constructor { get { return m_ctor; } }

        public virtual IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get
            {
                if (m_typedCtorArgs == null)
                {
                    CustomAttributeTypedArgument[] typedCtorArgs = new CustomAttributeTypedArgument[m_ctorParams.Length];

                    for (int i = 0; i < typedCtorArgs.Length; i++)
                    {
                        CustomAttributeEncodedArgument encodedArg = m_ctorParams[i].CustomAttributeEncodedArgument;

                        typedCtorArgs[i] = new CustomAttributeTypedArgument(m_scope, encodedArg);
                    }

                    m_typedCtorArgs = Array.AsReadOnly(typedCtorArgs);
                }

                return m_typedCtorArgs;
            }
        }

        public virtual IList<CustomAttributeNamedArgument> NamedArguments
        {
            get
            {
                if (m_namedArgs == null)
                {
                    if (m_namedParams == null)
                        return null!;

                    int cNamedArgs = 0;
                    for (int i = 0; i < m_namedParams.Length; i++)
                    {
                        if (m_namedParams[i].EncodedArgument.CustomAttributeType.EncodedType != CustomAttributeEncoding.Undefined)
                            cNamedArgs++;
                    }

                    CustomAttributeNamedArgument[] namedArgs = new CustomAttributeNamedArgument[cNamedArgs];

                    for (int i = 0, j = 0; i < m_namedParams.Length; i++)
                    {
                        if (m_namedParams[i].EncodedArgument.CustomAttributeType.EncodedType != CustomAttributeEncoding.Undefined)
                            namedArgs[j++] = new CustomAttributeNamedArgument(
                                m_members[i], new CustomAttributeTypedArgument(m_scope, m_namedParams[i].EncodedArgument));
                    }

                    m_namedArgs = Array.AsReadOnly(namedArgs);
                }

                return m_namedArgs;
            }
        }
        #endregion
    }

    public readonly struct CustomAttributeNamedArgument
    {
        public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => left.Equals(right);
        public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right) => !left.Equals(right);

        private readonly MemberInfo m_memberInfo;
        private readonly CustomAttributeTypedArgument m_value;

        #region Constructor
        public CustomAttributeNamedArgument(MemberInfo memberInfo, object? value)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            Type type;
            if (memberInfo is FieldInfo field)
            {
                type = field.FieldType;
            }
            else if (memberInfo is PropertyInfo property)
            {
                type = property.PropertyType;
            }
            else
            {
                throw new ArgumentException(SR.Argument_InvalidMemberForNamedArgument);
            }

            m_memberInfo = memberInfo;
            m_value = new CustomAttributeTypedArgument(type, value);
        }

        public CustomAttributeNamedArgument(MemberInfo memberInfo, CustomAttributeTypedArgument typedArgument)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            m_memberInfo = memberInfo;
            m_value = typedArgument;
        }
        #endregion

        #region Object Override
        public override string ToString()
        {
            if (m_memberInfo == null)
                return base.ToString();

            return string.Format("{0} = {1}", MemberInfo.Name, TypedValue.ToString(ArgumentType != typeof(object)));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            return obj == (object)this;
        }
        #endregion

        #region Internal Members
        internal Type ArgumentType
        {
            get
            {
                return m_memberInfo is FieldInfo ?
                    ((FieldInfo)m_memberInfo).FieldType :
                    ((PropertyInfo)m_memberInfo).PropertyType;
            }
        }
        #endregion

        public MemberInfo MemberInfo => m_memberInfo;
        public CustomAttributeTypedArgument TypedValue => m_value;
        public string MemberName => MemberInfo.Name;
        public bool IsField => MemberInfo is FieldInfo;
    }

    public readonly struct CustomAttributeTypedArgument
    {
        public static bool operator ==(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right) => left.Equals(right);
        public static bool operator !=(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right) => !left.Equals(right);

        #region Private Static Methods
        private static Type CustomAttributeEncodingToType(CustomAttributeEncoding encodedType)
        {
            switch (encodedType)
            {
                case (CustomAttributeEncoding.Enum):
                    return typeof(Enum);

                case (CustomAttributeEncoding.Int32):
                    return typeof(int);

                case (CustomAttributeEncoding.String):
                    return typeof(string);

                case (CustomAttributeEncoding.Type):
                    return typeof(Type);

                case (CustomAttributeEncoding.Array):
                    return typeof(Array);

                case (CustomAttributeEncoding.Char):
                    return typeof(char);

                case (CustomAttributeEncoding.Boolean):
                    return typeof(bool);

                case (CustomAttributeEncoding.SByte):
                    return typeof(sbyte);

                case (CustomAttributeEncoding.Byte):
                    return typeof(byte);

                case (CustomAttributeEncoding.Int16):
                    return typeof(short);

                case (CustomAttributeEncoding.UInt16):
                    return typeof(ushort);

                case (CustomAttributeEncoding.UInt32):
                    return typeof(uint);

                case (CustomAttributeEncoding.Int64):
                    return typeof(long);

                case (CustomAttributeEncoding.UInt64):
                    return typeof(ulong);

                case (CustomAttributeEncoding.Float):
                    return typeof(float);

                case (CustomAttributeEncoding.Double):
                    return typeof(double);

                case (CustomAttributeEncoding.Object):
                    return typeof(object);

                default:
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)encodedType), nameof(encodedType));
            }
        }

        private static object EncodedValueToRawValue(long val, CustomAttributeEncoding encodedType)
        {
            switch (encodedType)
            {
                case CustomAttributeEncoding.Boolean:
                    return (byte)val != 0;

                case CustomAttributeEncoding.Char:
                    return (char)val;

                case CustomAttributeEncoding.Byte:
                    return (byte)val;

                case CustomAttributeEncoding.SByte:
                    return (sbyte)val;

                case CustomAttributeEncoding.Int16:
                    return (short)val;

                case CustomAttributeEncoding.UInt16:
                    return (ushort)val;

                case CustomAttributeEncoding.Int32:
                    return (int)val;

                case CustomAttributeEncoding.UInt32:
                    return (uint)val;

                case CustomAttributeEncoding.Int64:
                    return (long)val;

                case CustomAttributeEncoding.UInt64:
                    return (ulong)val;

                case CustomAttributeEncoding.Float:
                    unsafe { return *(float*)&val; }

                case CustomAttributeEncoding.Double:
                    unsafe { return *(double*)&val; }

                default:
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)val), nameof(val));
            }
        }
        private static RuntimeType ResolveType(RuntimeModule scope, string typeName)
        {
            RuntimeType type = RuntimeTypeHandle.GetTypeByNameUsingCARules(typeName, scope);

            if (type == null)
                throw new InvalidOperationException(
                    SR.Format(SR.Arg_CATypeResolutionFailed, typeName));

            return type;
        }
        #endregion

        private readonly object? m_value;
        private readonly Type m_argumentType;

        #region Constructor
        public CustomAttributeTypedArgument(Type argumentType, object? value)
        {
            // value can be null.
            if (argumentType == null)
                throw new ArgumentNullException(nameof(argumentType));

            m_value = (value is null) ? null : CanonicalizeValue(value);
            m_argumentType = argumentType;
        }

        public CustomAttributeTypedArgument(object value)
        {
            // value cannot be null.
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            m_value = CanonicalizeValue(value);
            m_argumentType = value.GetType();
        }

        private static object CanonicalizeValue(object value)
        {
            Debug.Assert(value != null);

            if (value.GetType().IsEnum)
            {
                return ((Enum)value).GetValue();
            }
            return value;
        }

        internal CustomAttributeTypedArgument(RuntimeModule scope, CustomAttributeEncodedArgument encodedArg)
        {
            CustomAttributeEncoding encodedType = encodedArg.CustomAttributeType.EncodedType;

            if (encodedType == CustomAttributeEncoding.Undefined)
                throw new ArgumentException(null, nameof(encodedArg));

            if (encodedType == CustomAttributeEncoding.Enum)
            {
                m_argumentType = ResolveType(scope, encodedArg.CustomAttributeType.EnumName!);
                m_value = EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedArg.CustomAttributeType.EncodedEnumType);
            }
            else if (encodedType == CustomAttributeEncoding.String)
            {
                m_argumentType = typeof(string);
                m_value = encodedArg.StringValue;
            }
            else if (encodedType == CustomAttributeEncoding.Type)
            {
                m_argumentType = typeof(Type);

                m_value = null;

                if (encodedArg.StringValue != null)
                    m_value = ResolveType(scope, encodedArg.StringValue);
            }
            else if (encodedType == CustomAttributeEncoding.Array)
            {
                encodedType = encodedArg.CustomAttributeType.EncodedArrayType;
                Type elementType;

                if (encodedType == CustomAttributeEncoding.Enum)
                {
                    elementType = ResolveType(scope, encodedArg.CustomAttributeType.EnumName!);
                }
                else
                {
                    elementType = CustomAttributeEncodingToType(encodedType);
                }

                m_argumentType = elementType.MakeArrayType();

                if (encodedArg.ArrayValue == null)
                {
                    m_value = null;
                }
                else
                {
                    CustomAttributeTypedArgument[] arrayValue = new CustomAttributeTypedArgument[encodedArg.ArrayValue.Length];
                    for (int i = 0; i < arrayValue.Length; i++)
                        arrayValue[i] = new CustomAttributeTypedArgument(scope, encodedArg.ArrayValue[i]);

                    m_value = Array.AsReadOnly(arrayValue);
                }
            }
            else
            {
                m_argumentType = CustomAttributeEncodingToType(encodedType);
                m_value = EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedType);
            }
        }
        #endregion

        public override string ToString() => ToString(false);

        internal string ToString(bool typed)
        {
            if (m_argumentType == null)
                return base.ToString();

            if (ArgumentType.IsEnum)
                return string.Format(typed ? "{0}" : "({1}){0}", Value, ArgumentType.FullName);

            else if (Value == null)
                return string.Format(typed ? "null" : "({0})null", ArgumentType.Name);

            else if (ArgumentType == typeof(string))
                return string.Format("\"{0}\"", Value);

            else if (ArgumentType == typeof(char))
                return string.Format("'{0}'", Value);

            else if (ArgumentType == typeof(Type))
                return string.Format("typeof({0})", ((Type)Value!).FullName);

            else if (ArgumentType.IsArray)
            {
                IList<CustomAttributeTypedArgument> array = (IList<CustomAttributeTypedArgument>)Value!;

                Type elementType = ArgumentType.GetElementType()!;
                string result = string.Format(@"new {0}[{1}] {{ ", elementType.IsEnum ? elementType.FullName : elementType.Name, array.Count);

                for (int i = 0; i < array.Count; i++)
                {
                    result += string.Format(i == 0 ? "{0}" : ", {0}", array[i].ToString(elementType != typeof(object)));
                }

                result += " }";

                return result;
            }

            return string.Format(typed ? "{0}" : "({1}){0}", Value, ArgumentType.Name);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object? obj) => obj == (object)this;

        public Type ArgumentType => m_argumentType;
        public object? Value => m_value;
    }

    [StructLayout(LayoutKind.Auto)]
    internal struct CustomAttributeRecord
    {
        internal ConstArray blob;
        internal MetadataToken tkCtor;

        public CustomAttributeRecord(int token, ConstArray blob)
        {
            tkCtor = new MetadataToken(token);
            this.blob = blob;
        }
    }

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

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct CustomAttributeEncodedArgument
    {
        private readonly long m_primitiveValue;
        private readonly CustomAttributeEncodedArgument[] m_arrayValue;
        private readonly string m_stringValue;
        private readonly CustomAttributeType m_type;

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void ParseAttributeArguments(
            IntPtr pCa,
            int cCa,
            ref CustomAttributeCtorParameter[] CustomAttributeCtorParameters,
            ref CustomAttributeNamedParameter[] CustomAttributeTypedArgument,
            RuntimeAssembly assembly);

        internal static void ParseAttributeArguments(ConstArray attributeBlob,
            ref CustomAttributeCtorParameter[] customAttributeCtorParameters,
            ref CustomAttributeNamedParameter[] customAttributeNamedParameters,
            RuntimeModule customAttributeModule)
        {
            if (customAttributeModule == null)
                throw new ArgumentNullException(nameof(customAttributeModule));

            Debug.Assert(customAttributeCtorParameters != null);
            Debug.Assert(customAttributeNamedParameters != null);

            if (customAttributeCtorParameters.Length != 0 || customAttributeNamedParameters.Length != 0)
            {
                ParseAttributeArguments(
                    attributeBlob.Signature,
                    (int)attributeBlob.Length,
                    ref customAttributeCtorParameters,
                    ref customAttributeNamedParameters,
                    (RuntimeAssembly)customAttributeModule.Assembly);
            }
        }

        public CustomAttributeType CustomAttributeType => m_type;
        public long PrimitiveValue => m_primitiveValue;
        public CustomAttributeEncodedArgument[] ArrayValue => m_arrayValue;
        public string StringValue => m_stringValue;
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct CustomAttributeNamedParameter
    {
        private readonly string m_argumentName;
        private readonly CustomAttributeEncoding m_fieldOrProperty;
        private readonly CustomAttributeEncoding m_padding;
        private readonly CustomAttributeType m_type;
        private readonly CustomAttributeEncodedArgument m_encodedArgument;

        public CustomAttributeNamedParameter(string argumentName, CustomAttributeEncoding fieldOrProperty, CustomAttributeType type)
        {
            if (argumentName == null)
                throw new ArgumentNullException(nameof(argumentName));

            m_argumentName = argumentName;
            m_fieldOrProperty = fieldOrProperty;
            m_padding = fieldOrProperty;
            m_type = type;
            m_encodedArgument = new CustomAttributeEncodedArgument();
        }

        public CustomAttributeEncodedArgument EncodedArgument => m_encodedArgument;
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct CustomAttributeCtorParameter
    {
        private readonly CustomAttributeType m_type;
        private readonly CustomAttributeEncodedArgument m_encodedArgument;

        public CustomAttributeCtorParameter(CustomAttributeType type)
        {
            m_type = type;
            m_encodedArgument = new CustomAttributeEncodedArgument();
        }

        public CustomAttributeEncodedArgument CustomAttributeEncodedArgument => m_encodedArgument;
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct CustomAttributeType
    {
        /// The most complicated type is an enum[] in which case...
        private readonly string? m_enumName; // ...enum name
        private readonly CustomAttributeEncoding m_encodedType; // ...array
        private readonly CustomAttributeEncoding m_encodedEnumType; // ...enum
        private readonly CustomAttributeEncoding m_encodedArrayType; // ...enum type
        private readonly CustomAttributeEncoding m_padding;

        public CustomAttributeType(CustomAttributeEncoding encodedType, CustomAttributeEncoding encodedArrayType,
            CustomAttributeEncoding encodedEnumType, string? enumName)
        {
            m_encodedType = encodedType;
            m_encodedArrayType = encodedArrayType;
            m_encodedEnumType = encodedEnumType;
            m_enumName = enumName;
            m_padding = m_encodedType;
        }

        public CustomAttributeEncoding EncodedType => m_encodedType;
        public CustomAttributeEncoding EncodedEnumType => m_encodedEnumType;
        public CustomAttributeEncoding EncodedArrayType => m_encodedArrayType;
        public string? EnumName => m_enumName;
    }

    internal static unsafe class CustomAttribute
    {
        private static readonly RuntimeType Type_RuntimeType = (RuntimeType)typeof(RuntimeType);
        private static readonly RuntimeType Type_Type = (RuntimeType)typeof(Type);

        #region Internal Static Members
        internal static bool IsDefined(RuntimeType type, RuntimeType? caType, bool inherit)
        {
            Debug.Assert(type != null);

            if (type.GetElementType() != null)
                return false;

            if (PseudoCustomAttribute.IsDefined(type, caType))
                return true;

            if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType))
                return true;

            if (!inherit)
                return false;

            type = (type.BaseType as RuntimeType)!;

            while (type != null)
            {
                if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType, 0, inherit))
                    return true;

                type = (type.BaseType as RuntimeType)!;
            }

            return false;
        }

        internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
        {
            Debug.Assert(method != null);
            Debug.Assert(caType != null);

            if (PseudoCustomAttribute.IsDefined(method, caType))
                return true;

            if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType))
                return true;

            if (!inherit)
                return false;

            method = method.GetParentDefinition()!;

            while (method != null)
            {
                if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType, 0, inherit))
                    return true;

                method = method.GetParentDefinition()!;
            }

            return false;
        }

        internal static bool IsDefined(RuntimeConstructorInfo ctor, RuntimeType caType)
        {
            Debug.Assert(ctor != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeConstructorInfo

            return IsCustomAttributeDefined(ctor.GetRuntimeModule(), ctor.MetadataToken, caType);
        }

        internal static bool IsDefined(RuntimePropertyInfo property, RuntimeType caType)
        {
            Debug.Assert(property != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimePropertyInfo

            return IsCustomAttributeDefined(property.GetRuntimeModule(), property.MetadataToken, caType);
        }

        internal static bool IsDefined(RuntimeEventInfo e, RuntimeType caType)
        {
            Debug.Assert(e != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeEventInfo

            return IsCustomAttributeDefined(e.GetRuntimeModule(), e.MetadataToken, caType);
        }

        internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType caType)
        {
            Debug.Assert(field != null);
            Debug.Assert(caType != null);

            if (PseudoCustomAttribute.IsDefined(field, caType))
                return true;

            return IsCustomAttributeDefined(field.GetRuntimeModule(), field.MetadataToken, caType);
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Debug.Assert(parameter != null);
            Debug.Assert(caType != null);

            if (PseudoCustomAttribute.IsDefined(parameter, caType))
                return true;

            return IsCustomAttributeDefined(parameter.GetRuntimeModule()!, parameter.MetadataToken, caType);
        }

        internal static bool IsDefined(RuntimeAssembly assembly, RuntimeType caType)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeAssembly
            return IsCustomAttributeDefined((assembly.ManifestModule as RuntimeModule)!, RuntimeAssembly.GetToken(assembly.GetNativeHandle()), caType);
        }

        internal static bool IsDefined(RuntimeModule module, RuntimeType caType)
        {
            Debug.Assert(module != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeModule

            return IsCustomAttributeDefined(module, module.MetadataToken, caType);
        }

        internal static object[] GetCustomAttributes(RuntimeType type, RuntimeType caType, bool inherit)
        {
            Debug.Assert(type != null);
            Debug.Assert(caType != null);

            if (type.GetElementType() != null)
                return (caType.IsValueType) ? Array.Empty<object>() : CreateAttributeArrayHelper(caType, 0);

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                type = (type.GetGenericTypeDefinition() as RuntimeType)!;

            PseudoCustomAttribute.GetCustomAttributes(type, caType, out RuntimeType.ListBuilder<Attribute> pcas);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...           
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !CustomAttribute.GetAttributeUsage(caType).Inherited))
            {
                object[] attributes = GetCustomAttributes(type.GetRuntimeModule(), type.MetadataToken, pcas.Count, caType);
                if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
                return attributes;
            }

            RuntimeType.ListBuilder<object> result = new RuntimeType.ListBuilder<object>();
            bool mustBeInheritable = false;
            bool useObjectArray = (caType.IsValueType || caType.ContainsGenericParameters);
            RuntimeType arrayType = useObjectArray ? (RuntimeType)typeof(object) : caType;

            for (var i = 0; i < pcas.Count; i++)
                result.Add(pcas[i]);

            while (type != (RuntimeType)typeof(object) && type != null)
            {
                AddCustomAttributes(ref result, type.GetRuntimeModule(), type.MetadataToken, caType, mustBeInheritable, result);
                mustBeInheritable = true;
                type = (type.BaseType as RuntimeType)!;
            }

            object[] typedResult = CreateAttributeArrayHelper(arrayType, result.Count);
            for (var i = 0; i < result.Count; i++)
            {
                typedResult[i] = result[i];
            }
            return typedResult;
        }

        internal static object[] GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
        {
            Debug.Assert(method != null);
            Debug.Assert(caType != null);

            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                method = (method.GetGenericMethodDefinition() as RuntimeMethodInfo)!;

            PseudoCustomAttribute.GetCustomAttributes(method, caType, out RuntimeType.ListBuilder<Attribute> pcas);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...           
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !CustomAttribute.GetAttributeUsage(caType).Inherited))
            {
                object[] attributes = GetCustomAttributes(method.GetRuntimeModule(), method.MetadataToken, pcas.Count, caType);
                if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
                return attributes;
            }

            RuntimeType.ListBuilder<object> result = new RuntimeType.ListBuilder<object>();
            bool mustBeInheritable = false;
            bool useObjectArray = (caType.IsValueType || caType.ContainsGenericParameters);
            RuntimeType arrayType = useObjectArray ? (RuntimeType)typeof(object) : caType;

            for (var i = 0; i < pcas.Count; i++)
                result.Add(pcas[i]);

            while (method != null)
            {
                AddCustomAttributes(ref result, method.GetRuntimeModule(), method.MetadataToken, caType, mustBeInheritable, result);
                mustBeInheritable = true;
                method = method.GetParentDefinition()!;
            }

            object[] typedResult = CreateAttributeArrayHelper(arrayType, result.Count);
            for (var i = 0; i < result.Count; i++)
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

            return GetCustomAttributes(ctor.GetRuntimeModule(), ctor.MetadataToken, 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimePropertyInfo property, RuntimeType caType)
        {
            Debug.Assert(property != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimePropertyInfo

            return GetCustomAttributes(property.GetRuntimeModule(), property.MetadataToken, 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimeEventInfo e, RuntimeType caType)
        {
            Debug.Assert(e != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeEventInfo

            return GetCustomAttributes(e.GetRuntimeModule(), e.MetadataToken, 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType)
        {
            Debug.Assert(field != null);
            Debug.Assert(caType != null);

            PseudoCustomAttribute.GetCustomAttributes(field, caType, out RuntimeType.ListBuilder<Attribute> pcas);
            object[] attributes = GetCustomAttributes(field.GetRuntimeModule(), field.MetadataToken, pcas.Count, caType);
            if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
            return attributes;
        }

        internal static object[] GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Debug.Assert(parameter != null);
            Debug.Assert(caType != null);

            PseudoCustomAttribute.GetCustomAttributes(parameter, caType, out RuntimeType.ListBuilder<Attribute> pcas);
            object[] attributes = GetCustomAttributes(parameter.GetRuntimeModule()!, parameter.MetadataToken, pcas.Count, caType);
            if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
            return attributes;
        }

        internal static object[] GetCustomAttributes(RuntimeAssembly assembly, RuntimeType caType)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeAssembly

            int assemblyToken = RuntimeAssembly.GetToken(assembly.GetNativeHandle());
            return GetCustomAttributes((assembly.ManifestModule as RuntimeModule)!, assemblyToken, 0, caType);
        }

        internal static object[] GetCustomAttributes(RuntimeModule module, RuntimeType caType)
        {
            Debug.Assert(module != null);
            Debug.Assert(caType != null);

            // No pseudo attributes for RuntimeModule

            return GetCustomAttributes(module, module.MetadataToken, 0, caType);
        }

        internal static bool IsAttributeDefined(RuntimeModule decoratedModule, int decoratedMetadataToken, int attributeCtorToken)
        {
            return IsCustomAttributeDefined(decoratedModule, decoratedMetadataToken, null, attributeCtorToken, false);
        }

        private static bool IsCustomAttributeDefined(
            RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType? attributeFilterType)
        {
            return IsCustomAttributeDefined(decoratedModule, decoratedMetadataToken, attributeFilterType, 0, false);
        }

        private static bool IsCustomAttributeDefined(
            RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType? attributeFilterType, int attributeCtorToken, bool mustBeInheritable)
        {
            CustomAttributeRecord[] car = CustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedMetadataToken);

            if (attributeFilterType != null)
            {
                Debug.Assert(attributeCtorToken == 0);

                MetadataImport scope = decoratedModule.MetadataImport;
                RuntimeType.ListBuilder<object> derivedAttributes = default;

                for (int i = 0; i < car.Length; i++)
                {
                    if (FilterCustomAttributeRecord(car[i].tkCtor, in scope,
                        decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable, ref derivedAttributes,
                        out _, out _, out _, out _))
                        return true;
                }
            }
            else
            {
                Debug.Assert(attributeFilterType == null);
                Debug.Assert(!MetadataToken.IsNullToken(attributeCtorToken));

                for (int i = 0; i < car.Length; i++)
                {
                    if (car[i].tkCtor == attributeCtorToken)
                        return true;
                }
            }

            return false;
        }

        private static object[] GetCustomAttributes(
            RuntimeModule decoratedModule, int decoratedMetadataToken, int pcaCount, RuntimeType? attributeFilterType)
        {
            RuntimeType.ListBuilder<object> attributes = new RuntimeType.ListBuilder<object>();

            AddCustomAttributes(ref attributes, decoratedModule, decoratedMetadataToken, attributeFilterType, false, new RuntimeType.ListBuilder<object>());

            bool useObjectArray = attributeFilterType == null || attributeFilterType.IsValueType || attributeFilterType.ContainsGenericParameters;
            RuntimeType arrayType = useObjectArray ? (RuntimeType)typeof(object) : attributeFilterType!;

            object[] result = CreateAttributeArrayHelper(arrayType, attributes.Count + pcaCount);
            for (var i = 0; i < attributes.Count; i++)
            {
                result[i] = attributes[i];
            }
            return result;
        }

        private static void AddCustomAttributes(
            ref RuntimeType.ListBuilder<object> attributes,
            RuntimeModule decoratedModule, int decoratedMetadataToken,
            RuntimeType? attributeFilterType, bool mustBeInheritable,
            // The derivedAttributes list must be passed by value so that it is not modified with the discovered attributes
            RuntimeType.ListBuilder<object> derivedAttributes)
        {
            CustomAttributeRecord[] car = CustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedMetadataToken);

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
                                                 out RuntimeType attributeType, out IRuntimeMethodInfo? ctor, out bool ctorHasParameters, out bool isVarArg))
                {
                    continue;
                }

                // Leverage RuntimeConstructorInfo standard .ctor verification
                RuntimeConstructorInfo.CheckCanCreateInstance(attributeType, isVarArg);

                // Create custom attribute object
                int cNamedArgs;
                object attribute;
                if (ctorHasParameters)
                {
                    attribute = CreateCaObject(decoratedModule, attributeType, ctor!, ref blobStart, blobEnd, out cNamedArgs);
                }
                else
                {
                    attribute = RuntimeTypeHandle.CreateCaInstance(attributeType, ctor);

                    // It is allowed by the ECMA spec to have an empty signature blob
                    int blobLen = (int)((byte*)blobEnd - (byte*)blobStart);
                    if (blobLen == 0)
                    {
                        cNamedArgs = 0;
                    }
                    else
                    {
                        // Metadata is always written in little-endian format. Must account for this on
                        // big-endian platforms.
#if BIGENDIAN
                        const int CustomAttributeVersion = 0x0100;
#else
                        const int CustomAttributeVersion = 0x0001;
#endif
                        if (Marshal.ReadInt16(blobStart) != CustomAttributeVersion)
                        {
                            throw new CustomAttributeFormatException();
                        }

                        blobStart = (IntPtr)((byte*)blobStart + 2); // skip version prefix

                        cNamedArgs = Marshal.ReadInt16(blobStart);
                        blobStart = (IntPtr)((byte*)blobStart + 2); // skip namedArgs count
#if BIGENDIAN
                        cNamedArgs = ((cNamedArgs & 0xff00) >> 8) | ((cNamedArgs & 0x00ff) << 8);
#endif
                    }
                }

                for (int j = 0; j < cNamedArgs; j++)
                {
                    GetPropertyOrFieldData(decoratedModule, ref blobStart, blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value);

                    try
                    {
                        if (isProperty)
                        {
                            if (type is null && value != null)
                            {
                                type = (RuntimeType)value.GetType();
                                if (type == Type_RuntimeType)
                                {
                                    type = Type_Type;
                                }
                            }

                            PropertyInfo? property = type is null ? 
                                attributeType.GetProperty(name) : 
                                attributeType.GetProperty(name, type, Type.EmptyTypes);

                            // Did we get a valid property reference?
                            if (property == null)
                            {
                                throw new CustomAttributeFormatException(
                                    SR.Format(SR.RFLCT_InvalidPropFail, name));
                            }

                            MethodInfo setMethod = property.GetSetMethod(true)!;

                            // Public properties may have non-public setter methods
                            if (!setMethod.IsPublic)
                            {
                                continue;
                            }

                            setMethod.Invoke(attribute, BindingFlags.Default, null, new object?[] { value }, null);
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
        }

        private static bool FilterCustomAttributeRecord(
            MetadataToken caCtorToken,
            in MetadataImport scope,
            RuntimeModule decoratedModule,
            MetadataToken decoratedToken,
            RuntimeType attributeFilterType,
            bool mustBeInheritable,
            ref RuntimeType.ListBuilder<object> derivedAttributes,
            out RuntimeType attributeType,
            out IRuntimeMethodInfo? ctor,
            out bool ctorHasParameters,
            out bool isVarArg)
        {
            ctor = null;
            ctorHasParameters = false;
            isVarArg = false;

            // Resolve attribute type from ctor parent token found in decorated decoratedModule scope
            attributeType = (decoratedModule.ResolveType(scope.GetParentToken(caCtorToken), null, null) as RuntimeType)!;

            // Test attribute type against user provided attribute type filter
            if (!(attributeFilterType.IsAssignableFrom(attributeType)))
                return false;

            // Ensure if attribute type must be inheritable that it is inheritable
            // Ensure that to consider a duplicate attribute type AllowMultiple is true
            if (!AttributeUsageCheck(attributeType, mustBeInheritable, ref derivedAttributes))
                return false;

            // Windows Runtime attributes aren't real types - they exist to be read as metadata only, and as such
            // should be filtered out of the GetCustomAttributes path.
            if ((attributeType.Attributes & TypeAttributes.WindowsRuntime) == TypeAttributes.WindowsRuntime)
            {
                return false;
            }

            // Resolve the attribute ctor
            ConstArray ctorSig = scope.GetMethodSignature(caCtorToken);
            isVarArg = (ctorSig[0] & 0x05) != 0;
            ctorHasParameters = ctorSig[1] != 0;

            if (ctorHasParameters)
            {
                // Resolve method ctor token found in decorated decoratedModule scope
                // See https://github.com/dotnet/coreclr/issues/21456 for why we fast-path non-generics here (fewer allocations)
                if (attributeType.IsGenericType)
                {
                    ctor = decoratedModule.ResolveMethod(caCtorToken, attributeType.GenericTypeArguments, null)!.MethodHandle.GetMethodInfo();
                }
                else
                {
                    ctor = ModuleHandle.ResolveMethodHandleInternal(decoratedModule.GetNativeHandle(), caCtorToken);
                }
            }
            else
            {
                // Resolve method ctor token from decorated decoratedModule scope
                ctor = attributeType.GetTypeHandleInternal().GetDefaultConstructor();

                if (ctor == null && !attributeType.IsValueType)
                    throw new MissingMethodException(".ctor");
            }

            // Visibility checks
            MetadataToken tkParent = new MetadataToken();

            if (decoratedToken.IsParamDef)
            {
                tkParent = new MetadataToken(scope.GetParentToken(decoratedToken));
                tkParent = new MetadataToken(scope.GetParentToken(tkParent));
            }
            else if (decoratedToken.IsMethodDef || decoratedToken.IsProperty || decoratedToken.IsEvent || decoratedToken.IsFieldDef)
            {
                tkParent = new MetadataToken(scope.GetParentToken(decoratedToken));
            }
            else if (decoratedToken.IsTypeDef)
            {
                tkParent = decoratedToken;
            }
            else if (decoratedToken.IsGenericPar)
            {
                tkParent = new MetadataToken(scope.GetParentToken(decoratedToken));

                // decoratedToken is a generic parameter on a method. Get the declaring Type of the method.
                if (tkParent.IsMethodDef)
                    tkParent = new MetadataToken(scope.GetParentToken(tkParent));
            }
            else
            {
                // We need to relax this when we add support for other types of decorated tokens.
                Debug.Assert(decoratedToken.IsModule || decoratedToken.IsAssembly,
                                "The decoratedToken must be either an assembly, a module, a type, or a member.");
            }

            // If the attribute is on a type, member, or parameter we check access against the (declaring) type,
            // otherwise we check access against the module.
            RuntimeTypeHandle parentTypeHandle = tkParent.IsTypeDef ?
                                                    decoratedModule.ModuleHandle.ResolveTypeHandle(tkParent) :
                                                    new RuntimeTypeHandle();

            RuntimeTypeHandle attributeTypeHandle = attributeType.TypeHandle;

            bool result = RuntimeMethodHandle.IsCAVisibleFromDecoratedType(JitHelpers.GetQCallTypeHandleOnStack(ref attributeTypeHandle),
                                                                    ctor != null ? ctor.Value : RuntimeMethodHandleInternal.EmptyHandle,
                                                                    JitHelpers.GetQCallTypeHandleOnStack(ref parentTypeHandle),
                                                                    JitHelpers.GetQCallModuleOnStack(ref decoratedModule)) != Interop.BOOL.FALSE;

            GC.KeepAlive(ctor);
            return result;
        }
        #endregion

        #region Private Static Methods
        private static bool AttributeUsageCheck(
            RuntimeType attributeType, bool mustBeInheritable, ref RuntimeType.ListBuilder<object> derivedAttributes)
        {
            AttributeUsageAttribute? attributeUsageAttribute = null;

            if (mustBeInheritable)
            {
                attributeUsageAttribute = CustomAttribute.GetAttributeUsage(attributeType);

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
                    if (attributeUsageAttribute == null)
                        attributeUsageAttribute = CustomAttribute.GetAttributeUsage(attributeType);

                    return attributeUsageAttribute.AllowMultiple;
                }
            }

            return true;
        }

        internal static AttributeUsageAttribute GetAttributeUsage(RuntimeType decoratedAttribute)
        {
            RuntimeModule decoratedModule = decoratedAttribute.GetRuntimeModule();
            MetadataImport scope = decoratedModule.MetadataImport;
            CustomAttributeRecord[] car = CustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedAttribute.MetadataToken);

            AttributeUsageAttribute? attributeUsageAttribute = null;

            for (int i = 0; i < car.Length; i++)
            {
                ref CustomAttributeRecord caRecord = ref car[i];
                RuntimeType? attributeType = decoratedModule.ResolveType(scope.GetParentToken(caRecord.tkCtor), null, null) as RuntimeType;

                if (attributeType != (RuntimeType)typeof(AttributeUsageAttribute))
                    continue;

                if (attributeUsageAttribute != null)
                    throw new FormatException(SR.Format(SR.Format_AttributeUsage, attributeType));

                ParseAttributeUsageAttribute(caRecord.blob, out AttributeTargets targets, out bool inherited, out bool allowMultiple);
                attributeUsageAttribute = new AttributeUsageAttribute(targets, allowMultiple, inherited);
            }

            if (attributeUsageAttribute == null)
                return AttributeUsageAttribute.Default;

            return attributeUsageAttribute;
        }
        #endregion

        #region Private Static FCalls
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _ParseAttributeUsageAttribute(
            IntPtr pCa, int cCa, out int targets, out bool inherited, out bool allowMultiple);
        private static void ParseAttributeUsageAttribute(
            ConstArray ca, out AttributeTargets targets, out bool inherited, out bool allowMultiple)
        {
            _ParseAttributeUsageAttribute(ca.Signature, ca.Length, out int _targets, out inherited, out allowMultiple);
            targets = (AttributeTargets)_targets;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe object _CreateCaObject(RuntimeModule pModule, RuntimeType type, IRuntimeMethodInfo pCtor, byte** ppBlob, byte* pEndBlob, int* pcNamedArgs);
        private static unsafe object CreateCaObject(RuntimeModule module, RuntimeType type, IRuntimeMethodInfo ctor, ref IntPtr blob, IntPtr blobEnd, out int namedArgs)
        {
            byte* pBlob = (byte*)blob;
            byte* pBlobEnd = (byte*)blobEnd;
            int cNamedArgs;
            object ca = _CreateCaObject(module, type, ctor, &pBlob, pBlobEnd, &cNamedArgs);
            blob = (IntPtr)pBlob;
            namedArgs = cNamedArgs;
            return ca;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetPropertyOrFieldData(
            RuntimeModule pModule, byte** ppBlobStart, byte* pBlobEnd, out string name, out bool bIsProperty, out RuntimeType type, out object value);
        private static unsafe void GetPropertyOrFieldData(
            RuntimeModule module, ref IntPtr blobStart, IntPtr blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value)
        {
            byte* pBlobStart = (byte*)blobStart;
            _GetPropertyOrFieldData(
                module.GetNativeHandle(), &pBlobStart, (byte*)blobEnd, out name, out isProperty, out type, out value);
            blobStart = (IntPtr)pBlobStart;
        }

        private static object[] CreateAttributeArrayHelper(RuntimeType elementType, int elementCount)
        {
            // If we have 0 elements, don't allocate a new array
            if (elementCount == 0)
            {
                return elementType.GetEmptyArray();
            }

            return (object[])Array.CreateInstance(elementType, elementCount);
        }
        #endregion
    }

    internal static class PseudoCustomAttribute
    {
        #region Private Static Data Members
        // Here we can avoid the need to take a lock when using Dictionary by rearranging
        // the only method that adds values to the Dictionary. For more details on 
        // Dictionary versus Hashtable thread safety:
        // See code:Dictionary#DictionaryVersusHashtableThreadSafety
        private static readonly Dictionary<RuntimeType, RuntimeType> s_pca = CreatePseudoCustomAttributeDictionary();
        #endregion

        #region Static Constructor
        private static Dictionary<RuntimeType, RuntimeType> CreatePseudoCustomAttributeDictionary()
        {
            Type[] pcas = new Type[]
            {
                // See https://github.com/dotnet/coreclr/blob/master/src/md/compiler/custattr_emit.cpp
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
            };

            Dictionary<RuntimeType, RuntimeType> dict = new Dictionary<RuntimeType, RuntimeType>(pcas.Length);
            foreach (RuntimeType runtimeType in pcas)
            {
                VerifyPseudoCustomAttribute(runtimeType);
                dict[runtimeType] = runtimeType;
            }
            return dict;
        }

        [Conditional("DEBUG")]
        private static void VerifyPseudoCustomAttribute(RuntimeType pca)
        {
            // If any of these are invariants are no longer true will have to 
            // re-architect the PCA product logic and test cases -- you've been warned!
            Debug.Assert(pca.BaseType == typeof(Attribute), "Pseudo CA Error");
            AttributeUsageAttribute usage = CustomAttribute.GetAttributeUsage(pca);
            Debug.Assert(usage.Inherited == false, "Pseudo CA Error");
            //AllowMultiple is true for TypeForwardedToAttribute
            //Debug.Assert(usage.AllowMultiple == false, "Pseudo CA Error");
        }
        #endregion

        #region Internal Static
        internal static void GetCustomAttributes(RuntimeType type, RuntimeType caType, out RuntimeType.ListBuilder<Attribute> pcas)
        {
            Debug.Assert(type != null);
            Debug.Assert(caType != null);
            pcas = new RuntimeType.ListBuilder<Attribute>();

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType))
                return;

            if (all || caType == typeof(SerializableAttribute))
            {
                if ((type.Attributes & TypeAttributes.Serializable) != 0)
                    pcas.Add(new SerializableAttribute());
            }
            if (all || caType == typeof(ComImportAttribute))
            {
                if ((type.Attributes & TypeAttributes.Import) != 0)
                    pcas.Add(new ComImportAttribute());
            }
        }
        internal static bool IsDefined(RuntimeType type, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType!))
                return false;

            if (all || caType == typeof(SerializableAttribute))
            {
                if ((type.Attributes & TypeAttributes.Serializable) != 0)
                    return true;
            }
            if (all || caType == typeof(ComImportAttribute))
            {
                if ((type.Attributes & TypeAttributes.Import) != 0)
                    return true;
            }

            return false;
        }

        internal static void GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, out RuntimeType.ListBuilder<Attribute> pcas)
        {
            Debug.Assert(method != null);
            Debug.Assert(caType != null);
            pcas = new RuntimeType.ListBuilder<Attribute>();

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType))
                return;

            if (all || caType == typeof(DllImportAttribute))
            {
                Attribute? pca = GetDllImportCustomAttribute(method);
                if (pca != null) pcas.Add(pca);
            }
            if (all || caType == typeof(PreserveSigAttribute))
            {
                if ((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0)
                    pcas.Add(new PreserveSigAttribute());
            }
        }
        internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType!))
                return false;

            if (all || caType == typeof(DllImportAttribute))
            {
                if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
                    return true;
            }
            if (all || caType == typeof(PreserveSigAttribute))
            {
                if ((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0)
                    return true;
            }

            return false;
        }

        internal static void GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType, out RuntimeType.ListBuilder<Attribute> pcas)
        {
            Debug.Assert(parameter != null);
            Debug.Assert(caType != null);
            pcas = new RuntimeType.ListBuilder<Attribute>();

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType))
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
            if (all || caType == typeof(MarshalAsAttribute))
            {
                Attribute? pca = GetMarshalAsCustomAttribute(parameter);
                if (pca != null) pcas.Add(pca);
            }
        }
        internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType!))
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
            if (all || caType == typeof(MarshalAsAttribute))
            {
                if (GetMarshalAsCustomAttribute(parameter) != null) return true;
            }

            return false;
        }

        internal static void GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType, out RuntimeType.ListBuilder<Attribute> pcas)
        {
            Debug.Assert(field != null);
            Debug.Assert(caType != null);

            pcas = new RuntimeType.ListBuilder<Attribute>();

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType))
                return;

            Attribute? pca;

            if (all || caType == typeof(MarshalAsAttribute))
            {
                pca = GetMarshalAsCustomAttribute(field);
                if (pca != null) pcas.Add(pca);
            }
            if (all || caType == typeof(FieldOffsetAttribute))
            {
                pca = GetFieldOffsetCustomAttribute(field);
                if (pca != null) pcas.Add(pca);
            }
            if (all || caType == typeof(NonSerializedAttribute))
            {
                if ((field.Attributes & FieldAttributes.NotSerialized) != 0)
                    pcas.Add(new NonSerializedAttribute());
            }
        }
        internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType? caType)
        {
            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.ContainsKey(caType!))
                return false;

            if (all || caType == typeof(MarshalAsAttribute))
            {
                if (GetMarshalAsCustomAttribute(field) != null) return true;
            }
            if (all || caType == typeof(FieldOffsetAttribute))
            {
                if (GetFieldOffsetCustomAttribute(field) != null) return true;
            }
            if (all || caType == typeof(NonSerializedAttribute))
            {
                if ((field.Attributes & FieldAttributes.NotSerialized) != 0)
                    return true;
            }

            return false;
        }
        #endregion

        private static DllImportAttribute? GetDllImportCustomAttribute(RuntimeMethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
                return null;

            MetadataImport scope = ModuleHandle.GetMetadataImport(method.Module.ModuleHandle.GetRuntimeModule());
            int token = method.MetadataToken;

            scope.GetPInvokeMap(token, out PInvokeAttributes flags, out string entryPoint, out string dllName);

            CharSet charSet = CharSet.None;

            switch (flags & PInvokeAttributes.CharSetMask)
            {
                case PInvokeAttributes.CharSetNotSpec: charSet = CharSet.None; break;
                case PInvokeAttributes.CharSetAnsi: charSet = CharSet.Ansi; break;
                case PInvokeAttributes.CharSetUnicode: charSet = CharSet.Unicode; break;
                case PInvokeAttributes.CharSetAuto: charSet = CharSet.Auto; break;

                // Invalid: default to CharSet.None
                default: break;
            }

            CallingConvention callingConvention = CallingConvention.Cdecl;

            switch (flags & PInvokeAttributes.CallConvMask)
            {
                case PInvokeAttributes.CallConvWinapi: callingConvention = CallingConvention.Winapi; break;
                case PInvokeAttributes.CallConvCdecl: callingConvention = CallingConvention.Cdecl; break;
                case PInvokeAttributes.CallConvStdcall: callingConvention = CallingConvention.StdCall; break;
                case PInvokeAttributes.CallConvThiscall: callingConvention = CallingConvention.ThisCall; break;
                case PInvokeAttributes.CallConvFastcall: callingConvention = CallingConvention.FastCall; break;

                // Invalid: default to CallingConvention.Cdecl
                default: break;
            }

            DllImportAttribute attribute = new DllImportAttribute(dllName);

            attribute.EntryPoint = entryPoint;
            attribute.CharSet = charSet;
            attribute.SetLastError = (flags & PInvokeAttributes.SupportsLastError) != 0;
            attribute.ExactSpelling = (flags & PInvokeAttributes.NoMangle) != 0;
            attribute.PreserveSig = (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;
            attribute.CallingConvention = callingConvention;
            attribute.BestFitMapping = (flags & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled;
            attribute.ThrowOnUnmappableChar = (flags & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled;

            return attribute;
        }

        private static MarshalAsAttribute? GetMarshalAsCustomAttribute(RuntimeParameterInfo parameter)
        {
            return GetMarshalAsCustomAttribute(parameter.MetadataToken, parameter.GetRuntimeModule()!);
        }

        private static MarshalAsAttribute? GetMarshalAsCustomAttribute(RuntimeFieldInfo field)
        {
            return GetMarshalAsCustomAttribute(field.MetadataToken, field.GetRuntimeModule());
        }

        private static MarshalAsAttribute? GetMarshalAsCustomAttribute(int token, RuntimeModule scope)
        {
            ConstArray nativeType = ModuleHandle.GetMetadataImport(scope.GetNativeHandle()).GetFieldMarshal(token);

            if (nativeType.Length == 0)
                return null;

            MetadataImport.GetMarshalAs(nativeType,
                out UnmanagedType unmanagedType, out VarEnum safeArraySubType, out string? safeArrayUserDefinedTypeName, out UnmanagedType arraySubType, out int sizeParamIndex,
                out int sizeConst, out string? marshalTypeName, out string? marshalCookie, out int iidParamIndex);

            RuntimeType? safeArrayUserDefinedType = string.IsNullOrEmpty(safeArrayUserDefinedTypeName) ? null :
                RuntimeTypeHandle.GetTypeByNameUsingCARules(safeArrayUserDefinedTypeName, scope);
            RuntimeType? marshalTypeRef = null;

            try
            {
                marshalTypeRef = marshalTypeName == null ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(marshalTypeName, scope);
            }
            catch (TypeLoadException)
            {
                // The user may have supplied a bad type name string causing this TypeLoadException
                // Regardless, we return the bad type name
                Debug.Assert(marshalTypeName != null);
            }

            MarshalAsAttribute attribute = new MarshalAsAttribute(unmanagedType);

            attribute.SafeArraySubType = safeArraySubType;
            attribute.SafeArrayUserDefinedSubType = safeArrayUserDefinedType;
            attribute.IidParameterIndex = iidParamIndex;
            attribute.ArraySubType = arraySubType;
            attribute.SizeParamIndex = (short)sizeParamIndex;
            attribute.SizeConst = sizeConst;
            attribute.MarshalType = marshalTypeName;
            attribute.MarshalTypeRef = marshalTypeRef;
            attribute.MarshalCookie = marshalCookie;

            return attribute;
        }

        private static FieldOffsetAttribute? GetFieldOffsetCustomAttribute(RuntimeFieldInfo field)
        {
            if (field.DeclaringType != null &&
                field.GetRuntimeModule().MetadataImport.GetFieldOffset(field.DeclaringType.MetadataToken, field.MetadataToken, out int fieldOffset))
                return new FieldOffsetAttribute(fieldOffset);

            return null;
        }

        internal static StructLayoutAttribute? GetStructLayoutCustomAttribute(RuntimeType type)
        {
            if (type.IsInterface || type.HasElementType || type.IsGenericParameter)
                return null;

            LayoutKind layoutKind = LayoutKind.Auto;
            switch (type.Attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.ExplicitLayout: layoutKind = LayoutKind.Explicit; break;
                case TypeAttributes.AutoLayout: layoutKind = LayoutKind.Auto; break;
                case TypeAttributes.SequentialLayout: layoutKind = LayoutKind.Sequential; break;
                default: Debug.Fail("Unreachable code"); break;
            }

            CharSet charSet = CharSet.None;
            switch (type.Attributes & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: charSet = CharSet.Ansi; break;
                case TypeAttributes.AutoClass: charSet = CharSet.Auto; break;
                case TypeAttributes.UnicodeClass: charSet = CharSet.Unicode; break;
                default: Debug.Fail("Unreachable code"); break;
            }
            type.GetRuntimeModule().MetadataImport.GetClassLayout(type.MetadataToken, out int pack, out int size);

            // Metadata parameter checking should not have allowed 0 for packing size.
            // The runtime later converts a packing size of 0 to 8 so do the same here
            // because it's more useful from a user perspective. 
            if (pack == 0)
                pack = 8; // DEFAULT_PACKING_SIZE

            StructLayoutAttribute attribute = new StructLayoutAttribute(layoutKind);

            attribute.Pack = pack;
            attribute.Size = size;
            attribute.CharSet = charSet;

            return attribute;
        }
    }
}
