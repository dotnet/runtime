// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.Reflection 
{
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CustomAttributeData
    {
        #region Public Static Members
        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Module target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            Contract.EndContractBlock();

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Assembly target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            Contract.EndContractBlock();

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            Contract.EndContractBlock();

            return target.GetCustomAttributesData();
        }
        #endregion

        #region Internal Static Members
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeType target)
        {
            Contract.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);

            int pcaCount = 0;
            Attribute[] a = PseudoCustomAttribute.GetCustomAttributes((RuntimeType)target, typeof(object) as RuntimeType, true, out pcaCount);

            if (pcaCount == 0)
                return cad;

            CustomAttributeData[] pca = new CustomAttributeData[cad.Count + pcaCount];
            cad.CopyTo(pca, pcaCount);
            for (int i = 0; i < pcaCount; i++)
            {
                pca[i] = new CustomAttributeData(a[i]);
            }

            return Array.AsReadOnly(pca);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeFieldInfo target)
        {
            Contract.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);

            int pcaCount = 0;
            Attribute[] a = PseudoCustomAttribute.GetCustomAttributes((RuntimeFieldInfo)target, typeof(object) as RuntimeType, out pcaCount);

            if (pcaCount == 0)
                return cad;

            CustomAttributeData[] pca = new CustomAttributeData[cad.Count + pcaCount];
            cad.CopyTo(pca, pcaCount);
            for (int i = 0; i < pcaCount; i++)
            {
                pca[i] = new CustomAttributeData(a[i]);
            }

            return Array.AsReadOnly(pca);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeMethodInfo target)
        {
            Contract.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);

            int pcaCount = 0;
            Attribute[] a = PseudoCustomAttribute.GetCustomAttributes((RuntimeMethodInfo)target, typeof(object) as RuntimeType, true, out pcaCount);

            if (pcaCount == 0)
                return cad;

            CustomAttributeData[] pca = new CustomAttributeData[cad.Count + pcaCount];
            cad.CopyTo(pca, pcaCount);
            for (int i = 0; i < pcaCount; i++)
            {
                pca[i] = new CustomAttributeData(a[i]);
            }

            return Array.AsReadOnly(pca);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeConstructorInfo target)
        {
            Contract.Assert(target != null);

            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeEventInfo target)
        {
            Contract.Assert(target != null);

            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimePropertyInfo target)
        {
            Contract.Assert(target != null);

            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeModule target)
        {
            Contract.Assert(target != null);

            if (target.IsResource())
                return new List<CustomAttributeData>();

            return GetCustomAttributes(target, target.MetadataToken);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeAssembly target)
        {
            Contract.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes((RuntimeModule)target.ManifestModule, RuntimeAssembly.GetToken(target.GetNativeHandle()));

            int pcaCount = 0;
            Attribute[] a = PseudoCustomAttribute.GetCustomAttributes(target, typeof(object) as RuntimeType, false, out pcaCount);

            if (pcaCount == 0)
                return cad;

            CustomAttributeData[] pca = new CustomAttributeData[cad.Count + pcaCount];
            cad.CopyTo(pca, pcaCount);
            for (int i = 0; i < pcaCount; i++)
            {
                pca[i] = new CustomAttributeData(a[i]);
            }

            return Array.AsReadOnly(pca);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeParameterInfo target)
        {
            Contract.Assert(target != null);

            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);

            int pcaCount = 0;
            Attribute[] a = PseudoCustomAttribute.GetCustomAttributes(target, typeof(object) as RuntimeType, out pcaCount);

            if (pcaCount == 0)
                return cad;

            CustomAttributeData[] pca = new CustomAttributeData[cad.Count + pcaCount];
            cad.CopyTo(pca, pcaCount);
            for (int i = 0; i < pcaCount; i++)
                pca[i] = new CustomAttributeData(a[i]);

            return Array.AsReadOnly(pca);
        }
        #endregion

        #region Private Static Methods
        private static CustomAttributeEncoding TypeToCustomAttributeEncoding(RuntimeType type)
        {
            if (type == (RuntimeType)typeof(int))
                return CustomAttributeEncoding.Int32;

            if (type.IsEnum)
                return CustomAttributeEncoding.Enum;

            if (type == (RuntimeType)typeof(string))
                return CustomAttributeEncoding.String;

            if (type == (RuntimeType)typeof(Type))
                return CustomAttributeEncoding.Type;

            if (type == (RuntimeType)typeof(object))
                return CustomAttributeEncoding.Object;

            if (type.IsArray)
                return CustomAttributeEncoding.Array;

            if (type == (RuntimeType)typeof(char))
                return CustomAttributeEncoding.Char;

            if (type == (RuntimeType)typeof(bool))
                return CustomAttributeEncoding.Boolean;

            if (type == (RuntimeType)typeof(byte))
                return CustomAttributeEncoding.Byte;

            if (type == (RuntimeType)typeof(sbyte))
                return CustomAttributeEncoding.SByte;

            if (type == (RuntimeType)typeof(short))
                return CustomAttributeEncoding.Int16;

            if (type == (RuntimeType)typeof(ushort))
                return CustomAttributeEncoding.UInt16;

            if (type == (RuntimeType)typeof(uint))
                return CustomAttributeEncoding.UInt32;

            if (type == (RuntimeType)typeof(long))
                return CustomAttributeEncoding.Int64;

            if (type == (RuntimeType)typeof(ulong))
                return CustomAttributeEncoding.UInt64;

            if (type == (RuntimeType)typeof(float))
                return CustomAttributeEncoding.Float;

            if (type == (RuntimeType)typeof(double))
                return CustomAttributeEncoding.Double;

            // System.Enum is neither an Enum nor a Class
            if (type == (RuntimeType)typeof(Enum))
                return CustomAttributeEncoding.Object;

            if (type.IsClass)
                return CustomAttributeEncoding.Object;

            if (type.IsInterface)
                return CustomAttributeEncoding.Object;

            if (type.IsValueType)
                return CustomAttributeEncoding.Undefined;

            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidKindOfTypeForCA"), "type");
        }
        private static CustomAttributeType InitCustomAttributeType(RuntimeType parameterType)
        {
            CustomAttributeEncoding encodedType = CustomAttributeData.TypeToCustomAttributeEncoding(parameterType);
            CustomAttributeEncoding encodedArrayType = CustomAttributeEncoding.Undefined;
            CustomAttributeEncoding encodedEnumType = CustomAttributeEncoding.Undefined;
            string enumName = null;

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
        [System.Security.SecurityCritical]  // auto-generated
        private static IList<CustomAttributeData> GetCustomAttributes(RuntimeModule module, int tkTarget)
        {
            CustomAttributeRecord[] records = GetCustomAttributeRecords(module, tkTarget);

            CustomAttributeData[] customAttributes = new CustomAttributeData[records.Length];
            for (int i = 0; i < records.Length; i++)
                customAttributes[i] = new CustomAttributeData(module, records[i]);

            return Array.AsReadOnly(customAttributes);
        } 
        #endregion 

        #region Internal Static Members
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static CustomAttributeRecord[] GetCustomAttributeRecords(RuntimeModule module, int targetToken)
        {
            MetadataImport scope = module.MetadataImport;

            MetadataEnumResult tkCustomAttributeTokens;
            scope.EnumCustomAttributes(targetToken, out tkCustomAttributeTokens);

            if (tkCustomAttributeTokens.Length == 0)
            {
                return Array.Empty<CustomAttributeRecord>();
            }

            CustomAttributeRecord[] records = new CustomAttributeRecord[tkCustomAttributeTokens.Length];

            for (int i = 0; i < records.Length; i++)
            {
                scope.GetCustomAttributeProps(
                    tkCustomAttributeTokens[i], out records[i].tkCtor.Value, out records[i].blob);
            }

            return records;
        }

        internal static CustomAttributeTypedArgument Filter(IList<CustomAttributeData> attrs, Type caType, int parameter)
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

        #region Private Data Members
        private ConstructorInfo m_ctor;
        private RuntimeModule m_scope;
        private MemberInfo[] m_members;
        private CustomAttributeCtorParameter[] m_ctorParams;
        private CustomAttributeNamedParameter[] m_namedParams;
        private IList<CustomAttributeTypedArgument> m_typedCtorArgs;
        private IList<CustomAttributeNamedArgument> m_namedArgs;
        #endregion

        #region Constructor
        protected CustomAttributeData()
        {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private CustomAttributeData(RuntimeModule scope, CustomAttributeRecord caRecord)
        {
            m_scope = scope;
            m_ctor = (RuntimeConstructorInfo)RuntimeType.GetMethodBase(scope, caRecord.tkCtor);

            ParameterInfo[] parameters = m_ctor.GetParametersNoCopy();            
            m_ctorParams = new CustomAttributeCtorParameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                m_ctorParams[i] = new CustomAttributeCtorParameter(InitCustomAttributeType((RuntimeType)parameters[i].ParameterType));

            FieldInfo[] fields = m_ctor.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

            CustomAttributeEncodedArgument.ParseAttributeArguments(caRecord.blob, ref m_ctorParams, ref m_namedParams, m_scope);
        }
        #endregion

        #region Pseudo Custom Attribute Constructor
        internal CustomAttributeData(Attribute attribute)
        {
            if (attribute is DllImportAttribute)
                Init((DllImportAttribute)attribute);
            else if (attribute is FieldOffsetAttribute)
                Init((FieldOffsetAttribute)attribute);
            else if (attribute is MarshalAsAttribute)
                Init((MarshalAsAttribute)attribute);
            else if (attribute is TypeForwardedToAttribute)
                Init((TypeForwardedToAttribute)attribute);
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
                new CustomAttributeNamedArgument(type.GetField("EntryPoint"), dllImport.EntryPoint),
                new CustomAttributeNamedArgument(type.GetField("CharSet"), dllImport.CharSet),
                new CustomAttributeNamedArgument(type.GetField("ExactSpelling"), dllImport.ExactSpelling),
                new CustomAttributeNamedArgument(type.GetField("SetLastError"), dllImport.SetLastError),
                new CustomAttributeNamedArgument(type.GetField("PreserveSig"), dllImport.PreserveSig),
                new CustomAttributeNamedArgument(type.GetField("CallingConvention"), dllImport.CallingConvention),
                new CustomAttributeNamedArgument(type.GetField("BestFitMapping"), dllImport.BestFitMapping),
                new CustomAttributeNamedArgument(type.GetField("ThrowOnUnmappableChar"), dllImport.ThrowOnUnmappableChar)

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
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("ArraySubType"), marshalAs.ArraySubType);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SizeParamIndex"), marshalAs.SizeParamIndex);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SizeConst"), marshalAs.SizeConst);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("IidParameterIndex"), marshalAs.IidParameterIndex);
            namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SafeArraySubType"), marshalAs.SafeArraySubType);
            if (marshalAs.MarshalType != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalType"), marshalAs.MarshalType);
            if (marshalAs.MarshalTypeRef != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalTypeRef"), marshalAs.MarshalTypeRef);
            if (marshalAs.MarshalCookie != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("MarshalCookie"), marshalAs.MarshalCookie);
            if (marshalAs.SafeArrayUserDefinedSubType != null)
                namedArgs[i++] = new CustomAttributeNamedArgument(type.GetField("SafeArrayUserDefinedSubType"), marshalAs.SafeArrayUserDefinedSubType);

            m_namedArgs = Array.AsReadOnly(namedArgs);
        }
        private void Init(TypeForwardedToAttribute forwardedTo)
        {
            Type type = typeof(TypeForwardedToAttribute);

            Type[] sig = new Type[] { typeof(Type) };
            m_ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, sig, null);

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
            for (int i = 0; i < ConstructorArguments.Count; i ++)
                ctorArgs += String.Format(CultureInfo.CurrentCulture, i == 0 ? "{0}" : ", {0}", ConstructorArguments[i]);

            string namedArgs = "";
            for (int i = 0; i < NamedArguments.Count; i ++)
                namedArgs += String.Format(CultureInfo.CurrentCulture, i == 0 && ctorArgs.Length == 0 ? "{0}" : ", {0}", NamedArguments[i]);

            return String.Format(CultureInfo.CurrentCulture, "[{0}({1}{2})]", Constructor.DeclaringType.FullName, ctorArgs, namedArgs);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj == (object)this;
        }
        #endregion

        #region Public Members
        public Type AttributeType { get { return Constructor.DeclaringType; } }

        [System.Runtime.InteropServices.ComVisible(true)]
        public virtual ConstructorInfo Constructor { get { return m_ctor; } }
        
        [System.Runtime.InteropServices.ComVisible(true)]
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

                        typedCtorArgs[i] = new CustomAttributeTypedArgument(m_scope, m_ctorParams[i].CustomAttributeEncodedArgument);
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
                        return null;

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

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct CustomAttributeNamedArgument
    {
        #region Public Static Members
        public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
        {
            return !left.Equals(right);
        }
        #endregion

        #region Private Data Members
        private MemberInfo m_memberInfo;
        private CustomAttributeTypedArgument m_value;
        #endregion

        #region Constructor
        public CustomAttributeNamedArgument(MemberInfo memberInfo, object value)
        {
            if (memberInfo == null)
                throw new ArgumentNullException("memberInfo");

            Type type = null;
            FieldInfo field = memberInfo as FieldInfo;
            PropertyInfo property = memberInfo as PropertyInfo;

            if (field != null)
                type = field.FieldType;
            else if (property != null)
                type = property.PropertyType;
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidMemberForNamedArgument"));

            m_memberInfo = memberInfo;
            m_value = new CustomAttributeTypedArgument(type, value);
        }

        public CustomAttributeNamedArgument(MemberInfo memberInfo, CustomAttributeTypedArgument typedArgument)
        {
            if (memberInfo == null)
                throw new ArgumentNullException("memberInfo");

            m_memberInfo = memberInfo;
            m_value = typedArgument;
        }
        #endregion

        #region Object Override
        public override string ToString()
        {
            if (m_memberInfo == null)
                return base.ToString();

            return String.Format(CultureInfo.CurrentCulture, "{0} = {1}", MemberInfo.Name, TypedValue.ToString(ArgumentType != typeof(object))); 
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
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

        #region Public Members
        public MemberInfo MemberInfo { get { return m_memberInfo; } }
        public CustomAttributeTypedArgument TypedValue { get { return m_value; } }
        public string MemberName { get { return MemberInfo.Name; } }
        public bool IsField { get { return MemberInfo is FieldInfo; } }
        #endregion

    }

    [Serializable]
    [ComVisible(true)]
    public struct CustomAttributeTypedArgument
    {
        #region Public Static Members
        public static bool operator ==(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right)
        {
            return !left.Equals(right);
        }
        #endregion

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

                default :
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)encodedType), "encodedType");
            }
        }

        [SecuritySafeCritical]
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
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)val), "val");
            }
        }
        private static RuntimeType ResolveType(RuntimeModule scope, string typeName)
        {
            RuntimeType type = RuntimeTypeHandle.GetTypeByNameUsingCARules(typeName, scope);

            if (type == null)
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_CATypeResolutionFailed"), typeName));

            return type;
        }
        #endregion

        #region Private Data Members
        private object m_value;
        private Type m_argumentType;
        #endregion

        #region Constructor
        public CustomAttributeTypedArgument(Type argumentType, object value)
        {
            // value can be null.
            if (argumentType == null)
                throw new ArgumentNullException("argumentType");

            m_value = (value == null) ? null : CanonicalizeValue(value);
            m_argumentType = argumentType;
        }

        public CustomAttributeTypedArgument(object value)
        {
            // value cannot be null.
            if (value == null)
                throw new ArgumentNullException("value");

            m_value = CanonicalizeValue(value);
            m_argumentType = value.GetType();
        }

        private static object CanonicalizeValue(object value)
        {
            Contract.Assert(value != null);

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
                throw new ArgumentException("encodedArg");

            else if (encodedType == CustomAttributeEncoding.Enum)
            {
                m_argumentType = ResolveType(scope, encodedArg.CustomAttributeType.EnumName);
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
                    elementType = ResolveType(scope, encodedArg.CustomAttributeType.EnumName);
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

        #region Object Overrides
        public override string ToString() { return ToString(false); }

        internal string ToString(bool typed)
        {
            if (m_argumentType == null)
                return base.ToString();

            if (ArgumentType.IsEnum)
                return String.Format(CultureInfo.CurrentCulture, typed ? "{0}" : "({1}){0}", Value, ArgumentType.FullName);

            else if (Value == null)
                return String.Format(CultureInfo.CurrentCulture, typed ? "null" : "({0})null", ArgumentType.Name);

            else if (ArgumentType == typeof(string))
                return String.Format(CultureInfo.CurrentCulture, "\"{0}\"", Value);

            else if (ArgumentType == typeof(char))
                return String.Format(CultureInfo.CurrentCulture, "'{0}'", Value);

            else if (ArgumentType == typeof(Type))
                return String.Format(CultureInfo.CurrentCulture, "typeof({0})", ((Type)Value).FullName);

            else if (ArgumentType.IsArray)
            {
                string result = null;
                IList<CustomAttributeTypedArgument> array = Value as IList<CustomAttributeTypedArgument>;

                Type elementType = ArgumentType.GetElementType();
                result = String.Format(CultureInfo.CurrentCulture, @"new {0}[{1}] {{ ", elementType.IsEnum ? elementType.FullName : elementType.Name, array.Count);

                for (int i = 0; i < array.Count; i++)
                    result += String.Format(CultureInfo.CurrentCulture, i == 0 ? "{0}" : ", {0}", array[i].ToString(elementType != typeof(object)));

                return result += " }";
            }

            return String.Format(CultureInfo.CurrentCulture, typed ? "{0}" : "({1}){0}", Value, ArgumentType.Name);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj == (object)this;
        }
        #endregion

        #region Public Members
        public Type ArgumentType
        {
            get
            {
                return m_argumentType;
            }
        }
        public object Value 
        { 
            get 
            {
                return m_value;
            } 
        }
        #endregion
    }

    [Serializable]
    internal struct CustomAttributeRecord
    {
        internal ConstArray blob;
        internal MetadataToken tkCtor;
    }

    [Serializable]
    internal enum CustomAttributeEncoding : int
    {
        Undefined = 0,
        Boolean = CorElementType.Boolean,
        Char = CorElementType.Char,
        SByte = CorElementType.I1,
        Byte = CorElementType.U1,
        Int16 = CorElementType.I2,
        UInt16 = CorElementType.U2,
        Int32 = CorElementType.I4,
        UInt32 = CorElementType.U4,
        Int64 = CorElementType.I8,
        UInt64 = CorElementType.U8,
        Float = CorElementType.R4,
        Double = CorElementType.R8,
        String = CorElementType.String,
        Array = CorElementType.SzArray,
        Type = 0x50,
        Object = 0x51,
        Field = 0x53,
        Property = 0x54,
        Enum = 0x55
    }

    [Serializable]
    [StructLayout(LayoutKind.Auto)]
    internal struct CustomAttributeEncodedArgument
    { 
        #region Parser
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ParseAttributeArguments(
            IntPtr pCa, 
            int cCa, 
            ref CustomAttributeCtorParameter[] CustomAttributeCtorParameters,
            ref CustomAttributeNamedParameter[] CustomAttributeTypedArgument,
            RuntimeAssembly assembly);

        [System.Security.SecurityCritical]  // auto-generated
        internal static void ParseAttributeArguments(ConstArray attributeBlob, 
            ref CustomAttributeCtorParameter[] customAttributeCtorParameters,
            ref CustomAttributeNamedParameter[] customAttributeNamedParameters,
            RuntimeModule customAttributeModule)
        {
            if (customAttributeModule == null)
                throw new ArgumentNullException("customAttributeModule");
            Contract.EndContractBlock();

            Contract.Assert(customAttributeCtorParameters != null);
            Contract.Assert(customAttributeNamedParameters != null);

            if (customAttributeCtorParameters.Length != 0 || customAttributeNamedParameters.Length != 0)
            {
                unsafe
                {
                    ParseAttributeArguments(
                        attributeBlob.Signature,
                        (int)attributeBlob.Length,
                        ref customAttributeCtorParameters,
                        ref customAttributeNamedParameters,
                        (RuntimeAssembly)customAttributeModule.Assembly);
                }
            }
        }
        #endregion

        #region Private Data Members
        private long m_primitiveValue;
        private CustomAttributeEncodedArgument[] m_arrayValue;
        private string m_stringValue;
        private CustomAttributeType m_type;
        #endregion

        #region Public Members
        public CustomAttributeType CustomAttributeType { get { return m_type; } }
        public long PrimitiveValue { get { return m_primitiveValue; } }
        public CustomAttributeEncodedArgument[] ArrayValue { get { return m_arrayValue; } }
        public string StringValue { get { return m_stringValue; } }
        #endregion
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Auto)]
    internal struct CustomAttributeNamedParameter
    {
        #region Private Data Members
        private string m_argumentName;
        private CustomAttributeEncoding m_fieldOrProperty;
        private CustomAttributeEncoding m_padding;
        private CustomAttributeType m_type;
        private CustomAttributeEncodedArgument m_encodedArgument;
        #endregion

        #region Constructor
        public CustomAttributeNamedParameter(string argumentName, CustomAttributeEncoding fieldOrProperty, CustomAttributeType type)
        {
            if (argumentName == null)
                throw new ArgumentNullException("argumentName");
            Contract.EndContractBlock();

            m_argumentName = argumentName;
            m_fieldOrProperty = fieldOrProperty;
            m_padding = fieldOrProperty;
            m_type = type;
            m_encodedArgument = new CustomAttributeEncodedArgument();
        }
        #endregion

        #region Public Members
        public CustomAttributeEncodedArgument EncodedArgument { get { return m_encodedArgument; } }
        #endregion
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Auto)]
    internal struct CustomAttributeCtorParameter
    {
        #region Private Data Members
        private CustomAttributeType m_type;
        private CustomAttributeEncodedArgument m_encodedArgument;
        #endregion

        #region Constructor
        public CustomAttributeCtorParameter(CustomAttributeType type)
        {
            m_type = type;
            m_encodedArgument = new CustomAttributeEncodedArgument();
        }
        #endregion

        #region Public Members
        public CustomAttributeEncodedArgument CustomAttributeEncodedArgument { get { return m_encodedArgument; } }
        #endregion
    }

    // Note: This is a managed representation of a frame type defined in vm\frames.h; please ensure the layout remains
    // synchronized.
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityContextFrame
    {
        IntPtr m_GSCookie;      // This is actually at a negative offset in the real frame definition
        IntPtr __VFN_table;     // This is the real start of the SecurityContextFrame
        IntPtr m_Next;
        IntPtr m_Assembly;

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void Push(RuntimeAssembly assembly);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public extern void Pop();
    }

    [Serializable]
    [StructLayout(LayoutKind.Auto)]
    internal struct CustomAttributeType
    {
        #region Private Data Members
        /// The most complicated type is an enum[] in which case...
        private string m_enumName; // ...enum name
        private CustomAttributeEncoding m_encodedType; // ...array
        private CustomAttributeEncoding m_encodedEnumType; // ...enum
        private CustomAttributeEncoding m_encodedArrayType; // ...enum type
        private CustomAttributeEncoding m_padding; 
        #endregion

        #region Constructor
        public CustomAttributeType(CustomAttributeEncoding encodedType, CustomAttributeEncoding encodedArrayType, 
            CustomAttributeEncoding encodedEnumType, string enumName)
        {
            m_encodedType = encodedType;
            m_encodedArrayType = encodedArrayType;
            m_encodedEnumType = encodedEnumType;
            m_enumName = enumName;
            m_padding = m_encodedType; 
        }
        #endregion

        #region Public Members
        public CustomAttributeEncoding EncodedType { get { return m_encodedType; } }
        public CustomAttributeEncoding EncodedEnumType { get { return m_encodedEnumType; } }
        public CustomAttributeEncoding EncodedArrayType { get { return m_encodedArrayType; } }
        [System.Runtime.InteropServices.ComVisible(true)]
        public string EnumName { get { return m_enumName; } }
        #endregion
    }

    internal unsafe static class CustomAttribute
    {
        #region Private Data Members
        private static RuntimeType Type_RuntimeType = (RuntimeType)typeof(RuntimeType);
        private static RuntimeType Type_Type = (RuntimeType)typeof(Type);
        #endregion

        #region Internal Static Members
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeType type, RuntimeType caType, bool inherit)
        {
            Contract.Requires(type != null);

            if (type.GetElementType() != null) 
                return false;

            if (PseudoCustomAttribute.IsDefined(type, caType))
                return true;

            if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType))
                return true;

            if (!inherit)
                return false;

            type = type.BaseType as RuntimeType;

            while (type != null)
            {
                if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType, 0, inherit))
                    return true;

                type = type.BaseType as RuntimeType;
            }

            return false;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
        {
            Contract.Requires(method != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(method, caType))
                return true;

            if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType))
                return true;

            if (!inherit)
                return false;

            method = method.GetParentDefinition();

            while (method != null)
            {
                if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType, 0, inherit))
                    return true;

                method = method.GetParentDefinition();
            }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeConstructorInfo ctor, RuntimeType caType)
        {
            Contract.Requires(ctor != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(ctor, caType))
                return true;

            return IsCustomAttributeDefined(ctor.GetRuntimeModule(), ctor.MetadataToken, caType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimePropertyInfo property, RuntimeType caType)
        {
            Contract.Requires(property != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(property, caType))
                return true;

            return IsCustomAttributeDefined(property.GetRuntimeModule(), property.MetadataToken, caType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeEventInfo e, RuntimeType caType)
        {
            Contract.Requires(e != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(e, caType))
                return true;

            return IsCustomAttributeDefined(e.GetRuntimeModule(), e.MetadataToken, caType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType caType)
        {
            Contract.Requires(field != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(field, caType))
                return true;

            return IsCustomAttributeDefined(field.GetRuntimeModule(), field.MetadataToken, caType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Contract.Requires(parameter != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(parameter, caType))
                return true;

            return IsCustomAttributeDefined(parameter.GetRuntimeModule(), parameter.MetadataToken, caType);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsDefined(RuntimeAssembly assembly, RuntimeType caType) 
        {
            Contract.Requires(assembly != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(assembly, caType))
                return true;

            return IsCustomAttributeDefined(assembly.ManifestModule as RuntimeModule, RuntimeAssembly.GetToken(assembly.GetNativeHandle()), caType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeModule module, RuntimeType caType)
        {
            Contract.Requires(module != null);
            Contract.Requires(caType != null);

            if (PseudoCustomAttribute.IsDefined(module, caType))
                return true;

            return IsCustomAttributeDefined(module, module.MetadataToken, caType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeType type, RuntimeType caType, bool inherit)
        {
            Contract.Requires(type != null);
            Contract.Requires(caType != null);

            if (type.GetElementType() != null) 
                return (caType.IsValueType) ? EmptyArray<Object>.Value : CreateAttributeArrayHelper(caType, 0);

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                type = type.GetGenericTypeDefinition() as RuntimeType;

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(type, caType, true, out pcaCount);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...           
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !CustomAttribute.GetAttributeUsage(caType).Inherited))
            {
                object[] attributes = GetCustomAttributes(type.GetRuntimeModule(), type.MetadataToken, pcaCount, caType, !AllowCriticalCustomAttributes(type));
                if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
                return attributes;
            }

            List<object> result = new List<object>();
            bool mustBeInheritable = false;
            bool useObjectArray = (caType == null || caType.IsValueType || caType.ContainsGenericParameters);
            Type arrayType = useObjectArray ? typeof(object) : caType;

            while (pcaCount > 0)
                result.Add(pca[--pcaCount]);

            while (type != (RuntimeType)typeof(object) && type != null)
            {
                object[] attributes = GetCustomAttributes(type.GetRuntimeModule(), type.MetadataToken, 0, caType, mustBeInheritable, result, !AllowCriticalCustomAttributes(type));
                mustBeInheritable = true;
                for (int i = 0; i < attributes.Length; i++)
                    result.Add(attributes[i]);

                type = type.BaseType as RuntimeType;
            }

            object[] typedResult = CreateAttributeArrayHelper(arrayType, result.Count);
            Array.Copy(result.ToArray(), 0, typedResult, 0, result.Count);
            return typedResult;
        }

        private static bool AllowCriticalCustomAttributes(RuntimeType type)
        {
            if (type.IsGenericParameter) 
            {
                // Generic parameters don't have transparency state, so look at the 
                // declaring method/type. One of declaringMethod or declaringType
                // must be set.
                MethodBase declaringMethod = type.DeclaringMethod;
                if (declaringMethod != null)
                {
                    return AllowCriticalCustomAttributes(declaringMethod); 
                }
                else
                {
                    type = type.DeclaringType as RuntimeType;
                    Contract.Assert(type != null);
                }
            }

            return !type.IsSecurityTransparent || SpecialAllowCriticalAttributes(type);
        }

        private static bool SpecialAllowCriticalAttributes(RuntimeType type)
        {
            // Types participating in Type Equivalence are always transparent.
            // See TokenSecurityDescriptor::VerifySemanticDataComputed in securitymeta.cpp.
            // Because of that we allow critical attributes applied to full trust equivalent types.
            // DeclaringType is null for global methods and fields and the global type never participates in type equivalency.

#if FEATURE_CORECLR
            return false;
#else
            return type != null && type.Assembly.IsFullyTrusted && RuntimeTypeHandle.IsEquivalentType(type);
#endif //!FEATURE_CORECLR
        }

        private static bool AllowCriticalCustomAttributes(MethodBase method)
        {
            Contract.Requires(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            return !method.IsSecurityTransparent ||
                SpecialAllowCriticalAttributes((RuntimeType)method.DeclaringType);
        }

        private static bool AllowCriticalCustomAttributes(RuntimeFieldInfo field)
        {
            return !field.IsSecurityTransparent ||
                SpecialAllowCriticalAttributes((RuntimeType)field.DeclaringType);
        }

        private static bool AllowCriticalCustomAttributes(RuntimeParameterInfo parameter)
        {
            // Since parameters have no transparency state, we look at the defining method instead.
            return AllowCriticalCustomAttributes(parameter.DefiningMethod);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, bool inherit)
        {
            Contract.Requires(method != null);
            Contract.Requires(caType != null);

            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                method = method.GetGenericMethodDefinition() as RuntimeMethodInfo;

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(method, caType, true, out pcaCount);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...           
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !CustomAttribute.GetAttributeUsage(caType).Inherited))
            {
                object[] attributes = GetCustomAttributes(method.GetRuntimeModule(), method.MetadataToken, pcaCount, caType, !AllowCriticalCustomAttributes(method));
                if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
                return attributes;
            }

            List<object> result = new List<object>();
            bool mustBeInheritable = false;
            bool useObjectArray = (caType == null || caType.IsValueType || caType.ContainsGenericParameters);
            Type arrayType = useObjectArray ? typeof(object) : caType;

            while (pcaCount > 0) 
                result.Add(pca[--pcaCount]);
                
            while (method != null)
            {
                object[] attributes = GetCustomAttributes(method.GetRuntimeModule(), method.MetadataToken, 0, caType, mustBeInheritable, result, !AllowCriticalCustomAttributes(method));
                mustBeInheritable = true;
                for (int i = 0; i < attributes.Length; i++)
                    result.Add(attributes[i]);

                method = method.GetParentDefinition();
            }

            object[] typedResult = CreateAttributeArrayHelper(arrayType, result.Count);
            Array.Copy(result.ToArray(), 0, typedResult, 0, result.Count);
            return typedResult;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeConstructorInfo ctor, RuntimeType caType)
        {
            Contract.Requires(ctor != null);
            Contract.Requires(caType != null);

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(ctor, caType, true, out pcaCount);
            object[] attributes = GetCustomAttributes(ctor.GetRuntimeModule(), ctor.MetadataToken, pcaCount, caType, !AllowCriticalCustomAttributes(ctor));
            if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
            return attributes;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimePropertyInfo property, RuntimeType caType)
        {
            Contract.Requires(property != null);
            Contract.Requires(caType != null);

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(property, caType, out pcaCount);
            // Since properties and events have no transparency state, logically we should check the declaring types.
            // But then if someone wanted to apply critical attributes on a property/event he would need to make the type critical,
            // which would also implicitly made all the members critical.
            // So we check the containing assembly instead. If the assembly can contain critical code we allow critical attributes on properties/events.
            bool disallowCriticalCustomAttributes = property.GetRuntimeModule().GetRuntimeAssembly().IsAllSecurityTransparent();

            object[] attributes = GetCustomAttributes(property.GetRuntimeModule(), property.MetadataToken, pcaCount, caType, disallowCriticalCustomAttributes);
            if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
            return attributes;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeEventInfo e, RuntimeType caType)
        {
            Contract.Requires(e != null);
            Contract.Requires(caType != null);

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(e, caType, out pcaCount);
            // Since properties and events have no transparency state, logically we should check the declaring types.
            // But then if someone wanted to apply critical attributes on a property/event he would need to make the type critical,
            // which would also implicitly made all the members critical.
            // So we check the containing assembly instead. If the assembly can contain critical code we allow critical attributes on properties/events.
            bool disallowCriticalCustomAttributes = e.GetRuntimeModule().GetRuntimeAssembly().IsAllSecurityTransparent();
            object[] attributes = GetCustomAttributes(e.GetRuntimeModule(), e.MetadataToken, pcaCount, caType, disallowCriticalCustomAttributes);
            if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
            return attributes;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType)
        {
            Contract.Requires(field != null);
            Contract.Requires(caType != null);

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(field, caType, out pcaCount);
            object[] attributes = GetCustomAttributes(field.GetRuntimeModule(), field.MetadataToken, pcaCount, caType, !AllowCriticalCustomAttributes(field));
            if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
            return attributes;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Contract.Requires(parameter != null);
            Contract.Requires(caType != null);

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(parameter, caType, out pcaCount);
            object[] attributes = GetCustomAttributes(parameter.GetRuntimeModule(), parameter.MetadataToken, pcaCount, caType, !AllowCriticalCustomAttributes(parameter)); 
            if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
            return attributes;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeAssembly assembly, RuntimeType caType) 
        {
            Contract.Requires(assembly != null);
            Contract.Requires(caType != null);

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(assembly, caType, true, out pcaCount);
            int assemblyToken = RuntimeAssembly.GetToken(assembly.GetNativeHandle());
            bool isAssemblySecurityTransparent = assembly.IsAllSecurityTransparent();
            object[] attributes = GetCustomAttributes(assembly.ManifestModule as RuntimeModule, assemblyToken, pcaCount, caType, isAssemblySecurityTransparent);
            if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
            return attributes;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object[] GetCustomAttributes(RuntimeModule module, RuntimeType caType) 
        {
            Contract.Requires(module != null);
            Contract.Requires(caType != null);

            int pcaCount = 0;
            Attribute[] pca = PseudoCustomAttribute.GetCustomAttributes(module, caType, out pcaCount);
            bool isModuleSecurityTransparent = module.GetRuntimeAssembly().IsAllSecurityTransparent();
            object[] attributes = GetCustomAttributes(module, module.MetadataToken, pcaCount, caType, isModuleSecurityTransparent);
            if (pcaCount > 0) Array.Copy(pca, 0, attributes, attributes.Length - pcaCount, pcaCount);
            return attributes;
        }

        [System.Security.SecuritySafeCritical]
        internal static bool IsAttributeDefined(RuntimeModule decoratedModule, int decoratedMetadataToken, int attributeCtorToken)
        {
            return IsCustomAttributeDefined(decoratedModule, decoratedMetadataToken, null, attributeCtorToken, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static bool IsCustomAttributeDefined(
            RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType attributeFilterType)
        {
            return IsCustomAttributeDefined(decoratedModule, decoratedMetadataToken, attributeFilterType, 0, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static bool IsCustomAttributeDefined(
            RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType attributeFilterType, int attributeCtorToken, bool mustBeInheritable)
        {
            if (decoratedModule.Assembly.ReflectionOnly)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyCA"));
            Contract.EndContractBlock();

            CustomAttributeRecord[] car = CustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedMetadataToken);

            if (attributeFilterType != null)
            {
                Contract.Assert(attributeCtorToken == 0);

                MetadataImport scope = decoratedModule.MetadataImport;
                RuntimeType attributeType;
                IRuntimeMethodInfo ctor;
                bool ctorHasParameters, isVarArg;

                // Optimization for the case where attributes decorate entities in the same assembly in which case 
                // we can cache the successful APTCA check between the decorated and the declared assembly.
                Assembly lastAptcaOkAssembly = null;

                for (int i = 0; i < car.Length; i++)
                {
                    CustomAttributeRecord caRecord = car[i];

                    if (FilterCustomAttributeRecord(caRecord, scope, ref lastAptcaOkAssembly,
                        decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable, null, null,
                        out attributeType, out ctor, out ctorHasParameters, out isVarArg))
                        return true;
                }
            }
            else
            {
                Contract.Assert(attributeFilterType == null);
                Contract.Assert(!MetadataToken.IsNullToken(attributeCtorToken));

                for (int i = 0; i < car.Length; i++)
                {
                    CustomAttributeRecord caRecord = car[i];

                    if (caRecord.tkCtor == attributeCtorToken)
                        return true;
                }
            }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static object[] GetCustomAttributes(
            RuntimeModule decoratedModule, int decoratedMetadataToken, int pcaCount, RuntimeType attributeFilterType, bool isDecoratedTargetSecurityTransparent)
        {
            return GetCustomAttributes(decoratedModule, decoratedMetadataToken, pcaCount, attributeFilterType, false, null, isDecoratedTargetSecurityTransparent);
        }

        [System.Security.SecurityCritical]
        private unsafe static object[] GetCustomAttributes(
            RuntimeModule decoratedModule, int decoratedMetadataToken, int pcaCount, 
            RuntimeType attributeFilterType, bool mustBeInheritable, IList derivedAttributes, bool isDecoratedTargetSecurityTransparent)
        {
            if (decoratedModule.Assembly.ReflectionOnly)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyCA"));
            Contract.EndContractBlock();

            MetadataImport scope = decoratedModule.MetadataImport;
            CustomAttributeRecord[] car = CustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedMetadataToken);

            bool useObjectArray = (attributeFilterType == null || attributeFilterType.IsValueType || attributeFilterType.ContainsGenericParameters);
            Type arrayType = useObjectArray ? typeof(object) : attributeFilterType;

            if (attributeFilterType == null && car.Length == 0)
                return CreateAttributeArrayHelper(arrayType, 0);

            object[] attributes = CreateAttributeArrayHelper(arrayType, car.Length);
            int cAttributes = 0;

            // Custom attribute security checks are done with respect to the assembly *decorated* with the 
            // custom attribute as opposed to the *caller of GetCustomAttributes*.
            // Since this assembly might not be on the stack and the attribute ctor or property setters we're about to invoke may
            // make security demands, we push a frame on the stack as a proxy for the decorated assembly (this frame will be picked
            // up an interpreted by the security stackwalker).
            // Once we push the frame it will be automatically popped in the event of an exception, so no need to use CERs or the
            // like.
            SecurityContextFrame frame = new SecurityContextFrame();
            frame.Push(decoratedModule.GetRuntimeAssembly());

            // Optimization for the case where attributes decorate entities in the same assembly in which case 
            // we can cache the successful APTCA check between the decorated and the declared assembly.
            Assembly lastAptcaOkAssembly = null;

            for (int i = 0; i < car.Length; i++)
            {
                object attribute = null;
                CustomAttributeRecord caRecord = car[i];

                IRuntimeMethodInfo ctor = null;
                RuntimeType attributeType = null;
                bool ctorHasParameters, isVarArg;
                int cNamedArgs = 0;

                IntPtr blobStart = caRecord.blob.Signature;
                IntPtr blobEnd = (IntPtr)((byte*)blobStart + caRecord.blob.Length);
                int blobLen = (int)((byte*)blobEnd - (byte*)blobStart);

                if (!FilterCustomAttributeRecord(caRecord, scope, ref lastAptcaOkAssembly, 
                                                 decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable, 
                                                 attributes, derivedAttributes,
                                                 out attributeType, out ctor, out ctorHasParameters, out isVarArg))
                    continue;

                if (ctor != null)
                {
                    // Linktime demand checks 
                    // decoratedMetadataToken needed as it may be "transparent" in which case we do a full stack walk
                    RuntimeMethodHandle.CheckLinktimeDemands(ctor, decoratedModule, isDecoratedTargetSecurityTransparent);
                }
                else
                {

                }

                // Leverage RuntimeConstructorInfo standard .ctor verfication
                RuntimeConstructorInfo.CheckCanCreateInstance(attributeType, isVarArg); 

                // Create custom attribute object
                if (ctorHasParameters)
                {
                    attribute = CreateCaObject(decoratedModule, ctor, ref blobStart, blobEnd, out cNamedArgs); 
                }
                else
                {
                    attribute = RuntimeTypeHandle.CreateCaInstance(attributeType, ctor);

                    // It is allowed by the ECMA spec to have an empty signature blob
                    if (blobLen == 0)
                        cNamedArgs = 0;
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
                            throw new CustomAttributeFormatException();
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
                    #region // Initialize named properties and fields
                    string name;
                    bool isProperty;
                    RuntimeType type;
                    object value;
                    
                    IntPtr blobItr = caRecord.blob.Signature;

                    GetPropertyOrFieldData(decoratedModule, ref blobStart, blobEnd, out name, out isProperty, out type, out value);

                    try
                    {
                        if (isProperty)
                        {
                            #region // Initialize property
                            if (type == null && value != null)
                            {
                                type = (RuntimeType)value.GetType();
                                if (type == Type_RuntimeType)
                                    type = Type_Type;
                            }

                            RuntimePropertyInfo property = null;

                            if (type == null)
                                property = attributeType.GetProperty(name) as RuntimePropertyInfo;
                            else
                                property = attributeType.GetProperty(name, type, Type.EmptyTypes) as RuntimePropertyInfo;

                            // Did we get a valid property reference?
                            if (property == null)
                            {
                                throw new CustomAttributeFormatException(
                                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString(
                                        isProperty ? "RFLCT.InvalidPropFail" : "RFLCT.InvalidFieldFail"), name));
                            }

                            RuntimeMethodInfo setMethod = property.GetSetMethod(true) as RuntimeMethodInfo;
                            
                            // Public properties may have non-public setter methods
                            if (!setMethod.IsPublic)
                                continue;

                            RuntimeMethodHandle.CheckLinktimeDemands(setMethod, decoratedModule, isDecoratedTargetSecurityTransparent);

                            setMethod.UnsafeInvoke(attribute, BindingFlags.Default, null, new object[] { value }, null);
                            #endregion
                        }
                        else
                        {
                            RtFieldInfo field = attributeType.GetField(name) as RtFieldInfo;

                            if (isDecoratedTargetSecurityTransparent)
                            {
                                RuntimeFieldHandle.CheckAttributeAccess(field.FieldHandle, decoratedModule.GetNativeHandle());
                            }

                            field.CheckConsistency(attribute);
                            field.UnsafeSetValue(attribute, value, BindingFlags.Default, Type.DefaultBinder, null);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new CustomAttributeFormatException(
                            String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString(
                                isProperty ? "RFLCT.InvalidPropFail" : "RFLCT.InvalidFieldFail"), name), e);
                    }
                    #endregion
                }

                if (!blobStart.Equals(blobEnd))
                    throw new CustomAttributeFormatException();

                attributes[cAttributes++] = attribute;
            }

            // The frame will be popped automatically if we take an exception any time after we pushed it. So no need of a catch or
            // finally or CERs here.
            frame.Pop();

            if (cAttributes == car.Length && pcaCount == 0)
                return attributes;

            object[] result = CreateAttributeArrayHelper(arrayType, cAttributes + pcaCount);
            Array.Copy(attributes, 0, result, 0, cAttributes);
            return result;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static bool FilterCustomAttributeRecord(
            CustomAttributeRecord caRecord,
            MetadataImport scope,
            ref Assembly lastAptcaOkAssembly, 
            RuntimeModule decoratedModule,
            MetadataToken decoratedToken,
            RuntimeType attributeFilterType,
            bool mustBeInheritable,
            object[] attributes,
            IList derivedAttributes,
            out RuntimeType attributeType,
            out IRuntimeMethodInfo ctor,
            out bool ctorHasParameters,
            out bool isVarArg)
        {
            ctor = null;
            attributeType = null;
            ctorHasParameters = false;
            isVarArg = false;
            
            IntPtr blobStart = caRecord.blob.Signature;
            IntPtr blobEnd = (IntPtr)((byte*)blobStart + caRecord.blob.Length);

            // Resolve attribute type from ctor parent token found in decorated decoratedModule scope
            attributeType = decoratedModule.ResolveType(scope.GetParentToken(caRecord.tkCtor), null, null) as RuntimeType;


            // Test attribute type against user provided attribute type filter
            if (!(attributeFilterType.IsAssignableFrom(attributeType)))
                return false;

            // Ensure if attribute type must be inheritable that it is inhertiable
            // Ensure that to consider a duplicate attribute type AllowMultiple is true
            if (!AttributeUsageCheck(attributeType, mustBeInheritable, attributes, derivedAttributes))
                return false;

            // Windows Runtime attributes aren't real types - they exist to be read as metadata only, and as such
            // should be filtered out of the GetCustomAttributes path.
            if ((attributeType.Attributes & TypeAttributes.WindowsRuntime) == TypeAttributes.WindowsRuntime)
            {
                return false;
            }

#if FEATURE_APTCA
            // APTCA checks
            RuntimeAssembly attributeAssembly = (RuntimeAssembly)attributeType.Assembly;
            RuntimeAssembly decoratedModuleAssembly = (RuntimeAssembly)decoratedModule.Assembly;

            if (attributeAssembly != lastAptcaOkAssembly && 
                !RuntimeAssembly.AptcaCheck(attributeAssembly, decoratedModuleAssembly))
                return false;

            // Cache last successful APTCA check (optimization)
            lastAptcaOkAssembly = decoratedModuleAssembly;
#endif // FEATURE_APTCA

            // Resolve the attribute ctor
            ConstArray ctorSig = scope.GetMethodSignature(caRecord.tkCtor);
            isVarArg = (ctorSig[0] & 0x05) != 0;
            ctorHasParameters = ctorSig[1] != 0;

            if (ctorHasParameters)
            {
                // Resolve method ctor token found in decorated decoratedModule scope
                ctor = ModuleHandle.ResolveMethodHandleInternal(decoratedModule.GetNativeHandle(), caRecord.tkCtor);
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
                Contract.Assert(decoratedToken.IsModule || decoratedToken.IsAssembly, 
                                "The decoratedToken must be either an assembly, a module, a type, or a member.");
            }

            // If the attribute is on a type, member, or parameter we check access against the (declaring) type,
            // otherwise we check access against the module.
            RuntimeTypeHandle parentTypeHandle = tkParent.IsTypeDef ?
                                                    decoratedModule.ModuleHandle.ResolveTypeHandle(tkParent) :
                                                    new RuntimeTypeHandle();

            return RuntimeMethodHandle.IsCAVisibleFromDecoratedType(attributeType.TypeHandle, ctor, parentTypeHandle, decoratedModule);
        }
        #endregion

        #region Private Static Methods
        [System.Security.SecurityCritical]  // auto-generated
        private static bool AttributeUsageCheck(
            RuntimeType attributeType, bool mustBeInheritable, object[] attributes, IList derivedAttributes)
        {
            AttributeUsageAttribute attributeUsageAttribute = null;

            if (mustBeInheritable)
            {
                attributeUsageAttribute = CustomAttribute.GetAttributeUsage(attributeType);

                if (!attributeUsageAttribute.Inherited)
                    return false;
            }

            // Legacy: AllowMultiple ignored for none inheritable attributes

            if (derivedAttributes == null)
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

        [System.Security.SecurityCritical]  // auto-generated
        internal static AttributeUsageAttribute GetAttributeUsage(RuntimeType decoratedAttribute)
        {
            RuntimeModule decoratedModule = decoratedAttribute.GetRuntimeModule();
            MetadataImport scope = decoratedModule.MetadataImport;
            CustomAttributeRecord[] car = CustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedAttribute.MetadataToken);

            AttributeUsageAttribute attributeUsageAttribute = null;

            for (int i = 0; i < car.Length; i++)
            {
                CustomAttributeRecord caRecord = car[i];
                RuntimeType attributeType = decoratedModule.ResolveType(scope.GetParentToken(caRecord.tkCtor), null, null) as RuntimeType;

                if (attributeType != (RuntimeType)typeof(AttributeUsageAttribute))
                    continue;

                if (attributeUsageAttribute != null)
                    throw new FormatException(String.Format(
                        CultureInfo.CurrentUICulture, Environment.GetResourceString("Format_AttributeUsage"), attributeType));

                AttributeTargets targets;
                bool inherited, allowMultiple;
                ParseAttributeUsageAttribute(caRecord.blob, out targets, out inherited, out allowMultiple);
                attributeUsageAttribute = new AttributeUsageAttribute(targets, allowMultiple, inherited);
            }

            if (attributeUsageAttribute == null)
                return AttributeUsageAttribute.Default;

            return attributeUsageAttribute;
        }
        #endregion

        #region Private Static FCalls
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _ParseAttributeUsageAttribute(
            IntPtr pCa, int cCa, out int targets, out bool inherited, out bool allowMultiple);
        [System.Security.SecurityCritical]  // auto-generated
        private static void ParseAttributeUsageAttribute(
            ConstArray ca, out AttributeTargets targets, out bool inherited, out bool allowMultiple)
        {
            int _targets;
            _ParseAttributeUsageAttribute(ca.Signature, ca.Length, out _targets, out inherited, out allowMultiple);
            targets = (AttributeTargets)_targets;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static unsafe extern Object _CreateCaObject(RuntimeModule pModule, IRuntimeMethodInfo pCtor, byte** ppBlob, byte* pEndBlob, int* pcNamedArgs);
        [System.Security.SecurityCritical]  // auto-generated
        private static unsafe Object CreateCaObject(RuntimeModule module, IRuntimeMethodInfo ctor, ref IntPtr blob, IntPtr blobEnd, out int namedArgs)
        {
            byte* pBlob = (byte*)blob;
            byte* pBlobEnd = (byte*)blobEnd;
            int cNamedArgs; 
            object ca = _CreateCaObject(module, ctor, &pBlob, pBlobEnd, &cNamedArgs);
            blob = (IntPtr)pBlob;
            namedArgs = cNamedArgs;
            return ca;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private unsafe extern static void _GetPropertyOrFieldData(
            RuntimeModule pModule, byte** ppBlobStart, byte* pBlobEnd, out string name, out bool bIsProperty, out RuntimeType type, out object value);
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe static void GetPropertyOrFieldData(
            RuntimeModule module, ref IntPtr blobStart, IntPtr blobEnd, out string name, out bool isProperty, out RuntimeType type, out object value)
        {
            byte* pBlobStart = (byte*)blobStart;
            _GetPropertyOrFieldData(
                module.GetNativeHandle(), &pBlobStart, (byte*)blobEnd, out name, out isProperty, out type, out value);
            blobStart = (IntPtr)pBlobStart;
        }

        [System.Security.SecuritySafeCritical]
        private static object[] CreateAttributeArrayHelper(Type elementType, int elementCount)
        {
            return (object[])Array.UnsafeCreateInstance(elementType, elementCount);
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
        private static Dictionary<RuntimeType, RuntimeType> s_pca;
        private static int s_pcasCount;
        #endregion

        #region FCalls
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe private static extern void _GetSecurityAttributes(RuntimeModule module, int token, bool assembly, out object[] securityAttributes);
        [System.Security.SecurityCritical]  // auto-generated
        unsafe internal static void GetSecurityAttributes(RuntimeModule module, int token, bool assembly, out object[] securityAttributes)
        {
            _GetSecurityAttributes(module.GetNativeHandle(), token, assembly, out securityAttributes);
        }
        #endregion

        #region Static Constructor
        [System.Security.SecurityCritical]  // auto-generated
        static PseudoCustomAttribute()
        {
            RuntimeType[] pcas = new RuntimeType[]
            {
                // See //depot/DevDiv/private/Main/ndp/clr/src/MD/Compiler/CustAttr.cpp
                typeof(FieldOffsetAttribute) as RuntimeType, // field
                typeof(SerializableAttribute) as RuntimeType, // class, struct, enum, delegate
                typeof(MarshalAsAttribute) as RuntimeType, // parameter, field, return-value
                typeof(ComImportAttribute) as RuntimeType, // class, interface 
                typeof(NonSerializedAttribute) as RuntimeType, // field, inherited
                typeof(InAttribute) as RuntimeType, // parameter
                typeof(OutAttribute) as RuntimeType, // parameter
                typeof(OptionalAttribute) as RuntimeType, // parameter
                typeof(DllImportAttribute) as RuntimeType, // method
                typeof(PreserveSigAttribute) as RuntimeType, // method
                typeof(TypeForwardedToAttribute) as RuntimeType, // assembly
            };

            s_pcasCount = pcas.Length;
            Dictionary<RuntimeType, RuntimeType> temp_pca = new Dictionary<RuntimeType, RuntimeType>(s_pcasCount);

            for (int i = 0; i < s_pcasCount; i++)
            {
                VerifyPseudoCustomAttribute(pcas[i]);
                temp_pca[pcas[i]] = pcas[i];
            }
            s_pca = temp_pca;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [Conditional("_DEBUG")]
        private static void VerifyPseudoCustomAttribute(RuntimeType pca)
        {
            // If any of these are invariants are no longer true will have to 
            // re-architect the PCA product logic and test cases -- you've been warned!
            Contract.Assert(pca.BaseType == (RuntimeType)typeof(Attribute), "Pseudo CA Error");
            AttributeUsageAttribute usage = CustomAttribute.GetAttributeUsage(pca);
            Contract.Assert(usage.Inherited == false, "Pseudo CA Error");
            //AllowMultiple is true for TypeForwardedToAttribute
            //Contract.Assert(usage.AllowMultiple == false, "Pseudo CA Error");
        }
        #endregion

        #region Internal Static
        internal static bool IsSecurityAttribute(RuntimeType type)
        {
#pragma warning disable 618
            return type == (RuntimeType)typeof(SecurityAttribute) || type.IsSubclassOf(typeof(SecurityAttribute));
#pragma warning restore 618
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Attribute[] GetCustomAttributes(RuntimeType type, RuntimeType caType, bool includeSecCa, out int count)
        {
            Contract.Requires(type != null);
            Contract.Requires(caType != null);

            count = 0;

            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null && !IsSecurityAttribute(caType))
                return Array.Empty<Attribute>();

            List<Attribute> pcas = new List<Attribute>();
            Attribute pca = null;

            if (all || caType == (RuntimeType)typeof(SerializableAttribute))
            {
                pca = SerializableAttribute.GetCustomAttribute(type);
                if (pca != null) pcas.Add(pca);
            }
            if (all || caType == (RuntimeType)typeof(ComImportAttribute))
            {
                pca = ComImportAttribute.GetCustomAttribute(type);
                if (pca != null) pcas.Add(pca);
            }
            if (includeSecCa && (all || IsSecurityAttribute(caType)))
            {
                if (!type.IsGenericParameter && type.GetElementType() == null)
                {
                    if (type.IsGenericType)
                        type = (RuntimeType)type.GetGenericTypeDefinition();

                    object[] securityAttributes;
                    GetSecurityAttributes(type.Module.ModuleHandle.GetRuntimeModule(), type.MetadataToken, false, out securityAttributes);
                    if (securityAttributes != null)
                        foreach (object a in securityAttributes)
                            if (caType == a.GetType() || a.GetType().IsSubclassOf(caType))
                                pcas.Add((Attribute)a);
                }
            }

            count = pcas.Count;
            return pcas.ToArray();
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeType type, RuntimeType caType)
        {
            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null && !IsSecurityAttribute(caType))
                return false;

            if (all || caType == (RuntimeType)typeof(SerializableAttribute)) 
            { 
                if (SerializableAttribute.IsDefined(type)) return true;
            }
            if (all || caType == (RuntimeType)typeof(ComImportAttribute)) 
            { 
                if (ComImportAttribute.IsDefined(type)) return true;
            }
            if (all || IsSecurityAttribute(caType))
            {
                int count;
                if (GetCustomAttributes(type, caType, true, out count).Length != 0)
                    return true;
            }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Attribute[] GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, bool includeSecCa, out int count)
        {
            Contract.Requires(method != null);
            Contract.Requires(caType != null);

            count = 0;

            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null && !IsSecurityAttribute(caType))
                return Array.Empty<Attribute>();

            List<Attribute> pcas = new List<Attribute>();
            Attribute pca = null;

            if (all || caType == (RuntimeType)typeof(DllImportAttribute))
            {
                pca = DllImportAttribute.GetCustomAttribute(method);
                if (pca != null) pcas.Add(pca);
            }
            if (all || caType == (RuntimeType)typeof(PreserveSigAttribute))
            {
                pca = PreserveSigAttribute.GetCustomAttribute(method);
                if (pca != null) pcas.Add(pca);
            }
            if (includeSecCa && (all || IsSecurityAttribute(caType)))
            {
                object[] securityAttributes;

                GetSecurityAttributes(method.Module.ModuleHandle.GetRuntimeModule(), method.MetadataToken, false, out securityAttributes);
                if (securityAttributes != null)
                    foreach (object a in securityAttributes)
                        if (caType == a.GetType() || a.GetType().IsSubclassOf(caType))
                            pcas.Add((Attribute)a);
            }

            count = pcas.Count;
            return pcas.ToArray();
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeMethodInfo method, RuntimeType caType)
        {
            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null)
                return false;

            if (all || caType == (RuntimeType)typeof(DllImportAttribute))
            {
                if (DllImportAttribute.IsDefined(method)) return true;
            }
            if (all || caType == (RuntimeType)typeof(PreserveSigAttribute))
            {
                if (PreserveSigAttribute.IsDefined(method)) return true;
            }
            if (all || IsSecurityAttribute(caType))
            {
                int count;

                if (GetCustomAttributes(method, caType, true, out count).Length != 0)
                    return true;
            }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Attribute[] GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType, out int count)
        {
            Contract.Requires(parameter != null);
            Contract.Requires(caType != null);

            count = 0;

            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null)
                return null;

            Attribute[] pcas = new Attribute[s_pcasCount];
            Attribute pca = null;

            if (all || caType == (RuntimeType)typeof(InAttribute))
            {
                pca = InAttribute.GetCustomAttribute(parameter);
                if (pca != null) pcas[count++] = pca;
            }
            if (all || caType == (RuntimeType)typeof(OutAttribute))
            {
                pca = OutAttribute.GetCustomAttribute(parameter);
                if (pca != null) pcas[count++] = pca;
            }
            if (all || caType == (RuntimeType)typeof(OptionalAttribute))
            {
                pca = OptionalAttribute.GetCustomAttribute(parameter);
                if (pca != null) pcas[count++] = pca;
            }
            if (all || caType == (RuntimeType)typeof(MarshalAsAttribute))
            {
                pca = MarshalAsAttribute.GetCustomAttribute(parameter);
                if (pca != null) pcas[count++] = pca;
            }
            return pcas;
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null)
                return false;


            if (all || caType == (RuntimeType)typeof(InAttribute))
            {
                if (InAttribute.IsDefined(parameter)) return true;
            }
            if (all || caType == (RuntimeType)typeof(OutAttribute))
            {
                if (OutAttribute.IsDefined(parameter)) return true;
            }
            if (all || caType == (RuntimeType)typeof(OptionalAttribute))
            {
                if (OptionalAttribute.IsDefined(parameter)) return true;
            }
            if (all || caType == (RuntimeType)typeof(MarshalAsAttribute))
            {
                if (MarshalAsAttribute.IsDefined(parameter)) return true;
            }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Attribute[] GetCustomAttributes(RuntimeAssembly assembly, RuntimeType caType, bool includeSecCa, out int count)
        {
            count = 0;

            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);

            if (!all && s_pca.GetValueOrDefault(caType) == null && !IsSecurityAttribute(caType))
                return Array.Empty<Attribute>();

            List<Attribute> pcas = new List<Attribute>();
            if (includeSecCa && (all || IsSecurityAttribute(caType)))
            {
                object[] securityAttributes;

                GetSecurityAttributes(assembly.ManifestModule.ModuleHandle.GetRuntimeModule(), RuntimeAssembly.GetToken(assembly.GetNativeHandle()), true, out securityAttributes);
                if (securityAttributes != null)
                    foreach (object a in securityAttributes)
                        if (caType == a.GetType() || a.GetType().IsSubclassOf(caType))
                            pcas.Add((Attribute)a);
            }

            //TypeForwardedToAttribute.GetCustomAttribute(assembly) throws FileNotFoundException if the forwarded-to
            //assemblies are not present. This breaks many V4 scenarios because some framework types are forwarded 
            //to assemblies not included in the client SKU. Let's omit TypeForwardedTo for now until we have a better solution.

            //if (all || caType == (RuntimeType)typeof(TypeForwardedToAttribute))
            //{
            //    TypeForwardedToAttribute[] attrs = TypeForwardedToAttribute.GetCustomAttribute(assembly);
            //    pcas.AddRange(attrs);
            //}

            count = pcas.Count;
            return pcas.ToArray();
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeAssembly assembly, RuntimeType caType)
        {
            int count;
            return GetCustomAttributes(assembly, caType, true, out count).Length > 0;
        }

        internal static Attribute[] GetCustomAttributes(RuntimeModule module, RuntimeType caType, out int count)
        {
            count = 0;
            return null;
        }
        internal static bool IsDefined(RuntimeModule module, RuntimeType caType)
        {
            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Attribute[] GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType, out int count)
        {
            Contract.Requires(field != null);
            Contract.Requires(caType != null);

            count = 0;

            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null)
                return null;

            Attribute[] pcas = new Attribute[s_pcasCount];
            Attribute pca = null;

            if (all || caType == (RuntimeType)typeof(MarshalAsAttribute))
            {
                pca = MarshalAsAttribute.GetCustomAttribute(field);
                if (pca != null) pcas[count++] = pca;
            }
            if (all || caType == (RuntimeType)typeof(FieldOffsetAttribute))
            {
                pca = FieldOffsetAttribute.GetCustomAttribute(field);
                if (pca != null) pcas[count++] = pca;
            }
            if (all || caType == (RuntimeType)typeof(NonSerializedAttribute))
            {
                pca = NonSerializedAttribute.GetCustomAttribute(field);
                if (pca != null) pcas[count++] = pca;
            }
            return pcas;
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType caType)
        {
            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);
            if (!all && s_pca.GetValueOrDefault(caType) == null)
                return false;

            if (all || caType == (RuntimeType)typeof(MarshalAsAttribute))
            {
                if (MarshalAsAttribute.IsDefined(field)) return true;
            }
            if (all || caType == (RuntimeType)typeof(FieldOffsetAttribute))
            {
                if (FieldOffsetAttribute.IsDefined(field)) return true;
            }
            if (all || caType == (RuntimeType)typeof(NonSerializedAttribute)) 
            { 
                if (NonSerializedAttribute.IsDefined(field)) return true;
            }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Attribute[] GetCustomAttributes(RuntimeConstructorInfo ctor, RuntimeType caType, bool includeSecCa, out int count)
        {
            count = 0;

            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);

            if (!all && s_pca.GetValueOrDefault(caType) == null && !IsSecurityAttribute(caType))
                return Array.Empty<Attribute>();

            List<Attribute> pcas = new List<Attribute>();

            if (includeSecCa && (all || IsSecurityAttribute(caType)))
            {
                object[] securityAttributes;

                GetSecurityAttributes(ctor.Module.ModuleHandle.GetRuntimeModule(), ctor.MetadataToken, false, out securityAttributes);
                if (securityAttributes != null)
                    foreach (object a in securityAttributes)
                        if (caType == a.GetType() || a.GetType().IsSubclassOf(caType))
                            pcas.Add((Attribute)a);
            }

            count = pcas.Count;
            return pcas.ToArray();
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDefined(RuntimeConstructorInfo ctor, RuntimeType caType)
        {
            bool all = caType == (RuntimeType)typeof(object) || caType == (RuntimeType)typeof(Attribute);

            if (!all && s_pca.GetValueOrDefault(caType) == null)
                return false;

            if (all || IsSecurityAttribute(caType))
            {
                int count;

                if (GetCustomAttributes(ctor, caType, true, out count).Length != 0)
                    return true;
            }

            return false;
        }

        internal static Attribute[] GetCustomAttributes(RuntimePropertyInfo property, RuntimeType caType, out int count)
        {
            count = 0;
            return null;
        }
        internal static bool IsDefined(RuntimePropertyInfo property, RuntimeType caType)
        {
            return false;
        }

        internal static Attribute[] GetCustomAttributes(RuntimeEventInfo e, RuntimeType caType, out int count)
        {
            count = 0;
            return null;
        }
        internal static bool IsDefined(RuntimeEventInfo e, RuntimeType caType)
        {
            return false;
        }
        #endregion
    }
}
