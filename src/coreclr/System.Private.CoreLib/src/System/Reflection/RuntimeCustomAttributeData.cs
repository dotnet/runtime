// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if NATIVEAOT
using System.Reflection.Runtime.EventInfos;
using System.Reflection.Runtime.FieldInfos;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
using System.Reflection.Runtime.Modules;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.PropertyInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Extensions.NonPortable;

using ResolutionScope = Internal.Metadata.NativeFormat.MetadataReader;
#else
using ResolutionScope = System.Reflection.RuntimeModule;
#endif

namespace System.Reflection
{
    internal sealed class RuntimeCustomAttributeData : CustomAttributeData
    {
#if NATIVEAOT
        internal static IList<CustomAttributeData> GetCustomAttributes(
            MetadataReader? reader, CustomAttributeHandleCollection customAttributeHandles)
        {
            if (reader is null || customAttributeHandles.Count == 0)
                return Array.Empty<CustomAttributeData>();

            CustomAttributeData[] customAttributes = new CustomAttributeData[customAttributeHandles.Count];
            int index = 0;
            foreach (CustomAttributeHandle customAttributeHandle in customAttributeHandles)
                customAttributes[index++] = new RuntimeCustomAttributeData(reader, customAttributeHandle);

            return Array.AsReadOnly(customAttributes);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Metadata generation ensures custom attribute constructors are resolvable.")]
        private static ConstructorInfo ResolveAttributeConstructor(MetadataReader reader, CustomAttribute customAttribute)
        {
            if (customAttribute.Constructor.HandleType == HandleType.QualifiedMethod)
            {
                QualifiedMethod qualifiedMethod = customAttribute.Constructor.ToQualifiedMethodHandle(reader).GetQualifiedMethod(reader);
                TypeDefinitionHandle declaringType = qualifiedMethod.EnclosingType;
                MethodHandle methodHandle = qualifiedMethod.Method;
                NativeFormatRuntimeNamedTypeInfo namedAttributeType = NativeFormatRuntimeNamedTypeInfo.GetRuntimeNamedTypeInfo(reader, declaringType, default(RuntimeTypeHandle));
                return RuntimePlainConstructorInfo<NativeFormatMethodCommon>.GetRuntimePlainConstructorInfo(new NativeFormatMethodCommon(methodHandle, namedAttributeType, namedAttributeType));
            }

            MemberReference memberReference = customAttribute.Constructor.ToMemberReferenceHandle(reader).GetMemberReference(reader);

            // There is no chance a custom attribute type will be an open type specification so we can safely pass in the empty context here.
            TypeContext typeContext = new TypeContext(Array.Empty<RuntimeTypeInfo>(), Array.Empty<RuntimeTypeInfo>());
            RuntimeTypeInfo attributeType = memberReference.Parent.Resolve(reader, typeContext);
            MethodSignature signature = memberReference.Signature.ParseMethodSignature(reader);
            HandleCollection signatureParameters = signature.Parameters;
            Type[] expectedParameterTypes = new Type[signatureParameters.Count];
            int index = 0;
            foreach (Handle parameterHandle in signatureParameters)
            {
                expectedParameterTypes[index++] = parameterHandle.Resolve(reader, attributeType.TypeContext).ToType();
            }

            foreach (ConstructorInfo candidate in attributeType.ToType().GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ReadOnlySpan<ParameterInfo> candidateParameters = candidate.GetParametersAsSpan();
                if (expectedParameterTypes.Length != candidateParameters.Length)
                    continue;

                bool matches = true;
                for (int i = 0; i < expectedParameterTypes.Length; i++)
                {
                    if (!expectedParameterTypes[i].Equals(candidateParameters[i].ParameterType))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return candidate;
            }

            throw new MissingMethodException();
        }
#endif
        #region Internal Static Members
        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeType target)
        {
            Debug.Assert(target is not null);

#if NATIVEAOT
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
#endif
            RuntimeType.ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeFieldInfo target)
        {
            Debug.Assert(target is not null);

#if NATIVEAOT
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
#endif
            RuntimeType.ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeMethodInfo target)
        {
            Debug.Assert(target is not null);

#if NATIVEAOT
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
#endif
            RuntimeType.ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeConstructorInfo target)
        {
            Debug.Assert(target is not null);

#if NATIVEAOT
            return GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
#endif
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeEventInfo target)
        {
            Debug.Assert(target is not null);

#if NATIVEAOT
            return GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
#endif
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimePropertyInfo target)
        {
            Debug.Assert(target is not null);

#if NATIVEAOT
            return GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            return GetCustomAttributes(target.GetRuntimeModule(), target.MetadataToken);
#endif
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeModule target)
        {
            Debug.Assert(target is not null);

            if (target.IsResource())
                return new List<CustomAttributeData>();

#if NATIVEAOT
            return GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            return GetCustomAttributes(target, target.MetadataToken);
#endif
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeAssembly target)
        {
            Debug.Assert(target is not null);

            // No pseudo attributes for RuntimeAssembly

#if NATIVEAOT
            return GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            return GetCustomAttributes((RuntimeModule)target.ManifestModule, RuntimeAssembly.GetToken(target));
#endif
        }

        internal static IList<CustomAttributeData> GetCustomAttributesInternal(RuntimeParameterInfo target)
        {
            Debug.Assert(target is not null);

            RuntimeType.ListBuilder<Attribute> pcas = default;
#if NATIVEAOT
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetMetadataReader(), target.GetCustomAttributeHandles());
#else
            IList<CustomAttributeData> cad = GetCustomAttributes(target.GetRuntimeModule()!, target.MetadataToken);
#endif
            PseudoCustomAttribute.GetCustomAttributes(target, (RuntimeType)typeof(object), ref pcas);
            return pcas.Count > 0 ? GetCombinedList(cad, ref pcas) : cad;
        }

        private static ReadOnlyCollection<CustomAttributeData> GetCombinedList(IList<CustomAttributeData> customAttributes, ref RuntimeType.ListBuilder<Attribute> pseudoAttributes)
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

#if !NATIVEAOT
        #region Private Static Methods
        private static IList<CustomAttributeData> GetCustomAttributes(RuntimeModule module, int tkTarget)
        {
            CustomAttributeRecord[] records = GetCustomAttributeRecords(module, tkTarget);
            if (records.Length == 0)
            {
                return Array.Empty<CustomAttributeData>();
            }

            CustomAttributeData[] customAttributes = new CustomAttributeData[records.Length];
            for (int i = 0; i < records.Length; i++)
                customAttributes[i] = new RuntimeCustomAttributeData(module, records[i].tkCtor, in records[i].blob);

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
                return [];
            }

            CustomAttributeRecord[] records = new CustomAttributeRecord[tkCustomAttributeTokens.Length];

            for (int i = 0; i < records.Length; i++)
            {
                scope.GetCustomAttributeProps(tkCustomAttributeTokens[i],
                    out records[i].tkCtor.Value, out records[i].blob);
            }
            GC.KeepAlive(module);

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

            return default;
        }
        #endregion
#endif

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
        private static Type CustomAttributeEncodingToType(CustomAttributeEncoding encodedType)
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
#if !NATIVEAOT
        private static RuntimeType ResolveType(RuntimeModule scope, string typeName)
        {
            RuntimeType type = TypeNameResolver.GetTypeReferencedByCustomAttribute(typeName, scope);
            Debug.Assert(type is not null);
            return type;
        }
#endif
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

#if !NATIVEAOT
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
#endif

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

    internal sealed class CustomAttributeEncodedArgument
    {
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
#if NATIVEAOT
            HandleCollection.Enumerator fixedArguments = parser.Attribute.FixedArguments.GetEnumerator();
#endif
            foreach (CustomAttributeCtorParameter p in customAttributeCtorParameters)
            {
#if NATIVEAOT
                if (!fixedArguments.MoveNext())
                {
                    throw new BadImageFormatException();
                }

                p.EncodedArgument = parser.ParseValue(fixedArguments.Current, p.CustomAttributeType);
#else
                p.EncodedArgument = ParseCustomAttributeValue(
                    ref parser,
                    p.CustomAttributeType,
                    module);
#endif
            }
        }

        private static void ParseNamedArgs(
            ref CustomAttributeDataParser parser,
            CustomAttributeNamedParameter[] customAttributeNamedParameters,
            ResolutionScope module)
        {
#if NATIVEAOT
            foreach (NamedArgumentHandle namedArgumentHandle in parser.Attribute.NamedArguments)
            {
                NamedArgument namedArgument = namedArgumentHandle.GetNamedArgument(module);
#else
            // Parse the named arguments in the custom attribute.
            int argCount = parser.GetI2();

            for (int i = 0; i < argCount; ++i)
            {
                // Determine if a field or property.
                CustomAttributeEncoding namedArgFieldOrProperty = parser.GetTag();
                if (namedArgFieldOrProperty is not CustomAttributeEncoding.Field
                    && namedArgFieldOrProperty is not CustomAttributeEncoding.Property)
                {
                    throw new BadImageFormatException(SR.Arg_CustomAttributeFormatException);
                }
#endif

                // Parse the encoded type for the named argument.
#if NATIVEAOT
                RuntimeType argumentType = (RuntimeType)namedArgument.Type.Resolve(module, default).ToType();
                CustomAttributeType argType = new CustomAttributeType(argumentType);
                string? argName = namedArgument.Name.GetString(module);
#else
                CustomAttributeType argType = ParseCustomAttributeType(ref parser, module);

                string? argName = parser.GetString();
#endif

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

#if NATIVEAOT
                parameterToUpdate.EncodedArgument = parser.ParseValue(namedArgument.Value, argType);
#else
                parameterToUpdate.EncodedArgument = ParseCustomAttributeValue(ref parser, argType, module);
#endif
            }
        }

#if !NATIVEAOT
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
                case CustomAttributeEncoding.Type:
                    arg.StringValue = parser.GetString();
                    break;
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
                // We cannot determine the underlying type without loading the enum.
                string enumTypeMaybe = parser.GetString() ?? throw new BadImageFormatException();
                enumType = TypeNameResolver.GetTypeReferencedByCustomAttribute(enumTypeMaybe, module);
                if (!enumType.IsEnum)
                {
                    throw new BadImageFormatException();
                }

                enumTag = RuntimeCustomAttributeData.TypeToCustomAttributeEncoding((RuntimeType)enumType.GetEnumUnderlyingType());
            }
            return new CustomAttributeType(tag, arrayTag, enumTag, enumType);
        }

        /// <summary>
        /// Used to parse CustomAttribute data. See ECMA-335 II.23.3.
        /// </summary>
        private ref struct CustomAttributeDataParser
        {
            private int _curr;
            private ReadOnlySpan<byte> _blob;

            public CustomAttributeDataParser(ConstArray attributeBlob)
            {
                unsafe
                {
                    _blob = new ReadOnlySpan<byte>((void*)attributeBlob.Signature, attributeBlob.Length);
                }
                _curr = 0;
            }

            private ReadOnlySpan<byte> PeekData(int size) => _blob.Slice(_curr, size);

            private ReadOnlySpan<byte> ReadData(int size)
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

            public ushort GetProlog() => GetU2();

            public bool ValidateProlog()
            {
                ushort val = GetProlog();
                return val == 0x0001;
            }

            public string? GetString()
            {
                byte packedLengthBegin = PeekData(sizeof(byte))[0];

                // Check if the embedded string indicates a 'null' string (0xff).
                if (packedLengthBegin == 0xff) // ECMA 335- II.23.3
                {
                    // Consume the indicator.
                    ReadData(1);
                    return null;
                }

                // Not a null string, return a non-null string value.
                // The embedded string a UTF-8 prefixed by an ECMA-335 packed integer.
                int length = GetPackedLength(packedLengthBegin);
                if (length == 0)
                {
                    return string.Empty;
                }

                ReadOnlySpan<byte> utf8ByteSpan = ReadData(length);
                return Encoding.UTF8.GetString(utf8ByteSpan);
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

                throw new OverflowException();
            }
        }
#else
        /// <summary>
        /// Used to parse NativeFormat custom attribute data.
        /// </summary>
        private readonly struct CustomAttributeDataParser
        {
            private readonly CustomAttribute _attribute;
            private readonly MetadataReader _reader;

            public CustomAttributeDataParser(CustomAttribute attribute, MetadataReader reader)
            {
                _attribute = attribute;
                _reader = reader;
            }

            public CustomAttribute Attribute => _attribute;

            public bool ValidateProlog() => _reader is not null;

            public CustomAttributeEncodedArgument ParseValue(Handle value, CustomAttributeType type)
            {
                CustomAttributeType attributeType = type.EncodedType is CustomAttributeEncoding.Object
                    ? GetCustomAttributeType(value)
                    : type;
                if (value.HandleType is HandleType.ConstantEnumValue)
                {
                    value = value.ToConstantEnumValueHandle(_reader).GetConstantEnumValue(_reader).Value;
                }

                CustomAttributeEncodedArgument argument = new CustomAttributeEncodedArgument(attributeType);

                switch (value.HandleType)
                {
                    case HandleType.ConstantBooleanValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantBooleanValueHandle(_reader).GetConstantBooleanValue(_reader).Value ? 1 : 0 };
                        break;
                    case HandleType.ConstantCharValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantCharValueHandle(_reader).GetConstantCharValue(_reader).Value };
                        break;
                    case HandleType.ConstantByteValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantByteValueHandle(_reader).GetConstantByteValue(_reader).Value };
                        break;
                    case HandleType.ConstantSByteValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = (byte)value.ToConstantSByteValueHandle(_reader).GetConstantSByteValue(_reader).Value };
                        break;
                    case HandleType.ConstantInt16Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = (ushort)value.ToConstantInt16ValueHandle(_reader).GetConstantInt16Value(_reader).Value };
                        break;
                    case HandleType.ConstantUInt16Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantUInt16ValueHandle(_reader).GetConstantUInt16Value(_reader).Value };
                        break;
                    case HandleType.ConstantInt32Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantInt32ValueHandle(_reader).GetConstantInt32Value(_reader).Value };
                        break;
                    case HandleType.ConstantUInt32Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = (int)value.ToConstantUInt32ValueHandle(_reader).GetConstantUInt32Value(_reader).Value };
                        break;
                    case HandleType.ConstantInt64Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte8 = value.ToConstantInt64ValueHandle(_reader).GetConstantInt64Value(_reader).Value };
                        break;
                    case HandleType.ConstantUInt64Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte8 = (long)value.ToConstantUInt64ValueHandle(_reader).GetConstantUInt64Value(_reader).Value };
                        break;
                    case HandleType.ConstantSingleValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = BitConverter.SingleToInt32Bits(value.ToConstantSingleValueHandle(_reader).GetConstantSingleValue(_reader).Value) };
                        break;
                    case HandleType.ConstantDoubleValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte8 = BitConverter.DoubleToInt64Bits(value.ToConstantDoubleValueHandle(_reader).GetConstantDoubleValue(_reader).Value) };
                        break;
                    case HandleType.ConstantStringValue:
                        argument.StringValue = value.ToConstantStringValueHandle(_reader).GetConstantStringValue(_reader).Value;
                        break;
                    case HandleType.TypeDefinition:
                    case HandleType.TypeReference:
                    case HandleType.TypeSpecification:
                        argument.TypeValue = value.Resolve(_reader, default).ToType();
                        break;
                    case HandleType.ConstantReferenceValue:
                        break;
                    default:
                        ParseArrayValue(value, attributeType, argument);
                        break;
                }

                return argument;
            }

            private void ParseArrayValue(
                Handle value,
                CustomAttributeType arrayType,
                CustomAttributeEncodedArgument argument)
            {
                if (value.HandleType is HandleType.ConstantEnumArray)
                {
                    value = value.ToConstantEnumArrayHandle(_reader).GetConstantEnumArray(_reader).Value;
                }

                if (value.HandleType is HandleType.ConstantReferenceValue)
                    return;

                CustomAttributeType elementType = new CustomAttributeType(
                    arrayType.EncodedArrayType,
                    CustomAttributeEncoding.Undefined,
                    arrayType.EncodedEnumType,
                    arrayType.EnumType);
                argument.ArrayValue = value.HandleType switch
                {
                    HandleType.ConstantBooleanArray => ToEncodedArguments(value.ToConstantBooleanArrayHandle(_reader).GetConstantBooleanArray(_reader).Value, elementType),
                    HandleType.ConstantCharArray => ToEncodedArguments(value.ToConstantCharArrayHandle(_reader).GetConstantCharArray(_reader).Value, elementType),
                    HandleType.ConstantByteArray => ToEncodedArguments(value.ToConstantByteArrayHandle(_reader).GetConstantByteArray(_reader).Value, elementType),
                    HandleType.ConstantSByteArray => ToEncodedArguments(value.ToConstantSByteArrayHandle(_reader).GetConstantSByteArray(_reader).Value, elementType),
                    HandleType.ConstantInt16Array => ToEncodedArguments(value.ToConstantInt16ArrayHandle(_reader).GetConstantInt16Array(_reader).Value, elementType),
                    HandleType.ConstantUInt16Array => ToEncodedArguments(value.ToConstantUInt16ArrayHandle(_reader).GetConstantUInt16Array(_reader).Value, elementType),
                    HandleType.ConstantInt32Array => ToEncodedArguments(value.ToConstantInt32ArrayHandle(_reader).GetConstantInt32Array(_reader).Value, elementType),
                    HandleType.ConstantUInt32Array => ToEncodedArguments(value.ToConstantUInt32ArrayHandle(_reader).GetConstantUInt32Array(_reader).Value, elementType),
                    HandleType.ConstantInt64Array => ToEncodedArguments(value.ToConstantInt64ArrayHandle(_reader).GetConstantInt64Array(_reader).Value, elementType),
                    HandleType.ConstantUInt64Array => ToEncodedArguments(value.ToConstantUInt64ArrayHandle(_reader).GetConstantUInt64Array(_reader).Value, elementType),
                    HandleType.ConstantSingleArray => ToEncodedArguments(value.ToConstantSingleArrayHandle(_reader).GetConstantSingleArray(_reader).Value, elementType),
                    HandleType.ConstantDoubleArray => ToEncodedArguments(value.ToConstantDoubleArrayHandle(_reader).GetConstantDoubleArray(_reader).Value, elementType),
                    HandleType.ConstantStringArray => ToEncodedArguments(value.ToConstantStringArrayHandle(_reader).GetConstantStringArray(_reader).Value, elementType),
                    HandleType.ConstantHandleArray => ToEncodedArguments(value.ToConstantHandleArrayHandle(_reader).GetConstantHandleArray(_reader).Value, elementType),
                    _ => throw new BadImageFormatException()
                };
            }

            private CustomAttributeEncodedArgument[] ToEncodedArguments(HandleCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (Handle value in values)
                {
                    result[index++] = ParseValue(value, elementType);
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(BooleanCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (bool value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value ? 1 : 0 });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(CharCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (char value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(ByteCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (byte value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(SByteCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (sbyte value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = (byte)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(Int16Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (short value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = (ushort)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(UInt16Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (ushort value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(Int32Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (int value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(UInt32Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (uint value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = (int)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(Int64Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (long value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte8 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(UInt64Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (ulong value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte8 = (long)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(SingleCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (float value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = BitConverter.SingleToInt32Bits(value) });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(DoubleCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (double value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte8 = BitConverter.DoubleToInt64Bits(value) });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument CreatePrimitiveArgument(CustomAttributeType type, PrimitiveValue value)
                => new CustomAttributeEncodedArgument(type) { PrimitiveValue = value };

            private CustomAttributeType GetCustomAttributeType(Handle value)
            {
                return value.HandleType switch
                {
                    HandleType.ConstantBooleanValue => CreateType(CustomAttributeEncoding.Boolean),
                    HandleType.ConstantCharValue => CreateType(CustomAttributeEncoding.Char),
                    HandleType.ConstantByteValue => CreateType(CustomAttributeEncoding.Byte),
                    HandleType.ConstantSByteValue => CreateType(CustomAttributeEncoding.SByte),
                    HandleType.ConstantInt16Value => CreateType(CustomAttributeEncoding.Int16),
                    HandleType.ConstantUInt16Value => CreateType(CustomAttributeEncoding.UInt16),
                    HandleType.ConstantInt32Value => CreateType(CustomAttributeEncoding.Int32),
                    HandleType.ConstantUInt32Value => CreateType(CustomAttributeEncoding.UInt32),
                    HandleType.ConstantInt64Value => CreateType(CustomAttributeEncoding.Int64),
                    HandleType.ConstantUInt64Value => CreateType(CustomAttributeEncoding.UInt64),
                    HandleType.ConstantSingleValue => CreateType(CustomAttributeEncoding.Float),
                    HandleType.ConstantDoubleValue => CreateType(CustomAttributeEncoding.Double),
                    // A null object argument is reported as a null string to match CoreCLR.
                    HandleType.ConstantStringValue or HandleType.ConstantReferenceValue => CreateType(CustomAttributeEncoding.String),
                    HandleType.TypeDefinition or HandleType.TypeReference or HandleType.TypeSpecification => CreateType(CustomAttributeEncoding.Type),
                    HandleType.ConstantEnumValue => CreateEnumType(
                        value.ToConstantEnumValueHandle(_reader).GetConstantEnumValue(_reader).Type,
                        isArray: false),
                    HandleType.ConstantBooleanArray => CreateArrayType(CustomAttributeEncoding.Boolean),
                    HandleType.ConstantCharArray => CreateArrayType(CustomAttributeEncoding.Char),
                    HandleType.ConstantByteArray => CreateArrayType(CustomAttributeEncoding.Byte),
                    HandleType.ConstantSByteArray => CreateArrayType(CustomAttributeEncoding.SByte),
                    HandleType.ConstantInt16Array => CreateArrayType(CustomAttributeEncoding.Int16),
                    HandleType.ConstantUInt16Array => CreateArrayType(CustomAttributeEncoding.UInt16),
                    HandleType.ConstantInt32Array => CreateArrayType(CustomAttributeEncoding.Int32),
                    HandleType.ConstantUInt32Array => CreateArrayType(CustomAttributeEncoding.UInt32),
                    HandleType.ConstantInt64Array => CreateArrayType(CustomAttributeEncoding.Int64),
                    HandleType.ConstantUInt64Array => CreateArrayType(CustomAttributeEncoding.UInt64),
                    HandleType.ConstantSingleArray => CreateArrayType(CustomAttributeEncoding.Float),
                    HandleType.ConstantDoubleArray => CreateArrayType(CustomAttributeEncoding.Double),
                    HandleType.ConstantStringArray => CreateArrayType(CustomAttributeEncoding.String),
                    HandleType.ConstantHandleArray => CreateArrayType(CustomAttributeEncoding.Object),
                    HandleType.ConstantEnumArray => CreateEnumType(
                        value.ToConstantEnumArrayHandle(_reader).GetConstantEnumArray(_reader).ElementType,
                        isArray: true),
                    _ => throw new BadImageFormatException()
                };
            }

            private CustomAttributeType CreateEnumType(Handle enumTypeHandle, bool isArray)
            {
                RuntimeType enumType = (RuntimeType)enumTypeHandle.Resolve(_reader, default).ToType();
                if (!enumType.IsEnum)
                {
                    throw new BadImageFormatException();
                }

                CustomAttributeEncoding underlyingType =
                    RuntimeCustomAttributeData.TypeToCustomAttributeEncoding((RuntimeType)enumType.GetEnumUnderlyingType());
                return isArray
                    ? new CustomAttributeType(CustomAttributeEncoding.Array, CustomAttributeEncoding.Enum, underlyingType, enumType)
                    : new CustomAttributeType(CustomAttributeEncoding.Enum, CustomAttributeEncoding.Undefined, underlyingType, enumType);
            }

            private static CustomAttributeType CreateType(CustomAttributeEncoding type)
                => new CustomAttributeType(
                    type,
                    CustomAttributeEncoding.Undefined,
                    CustomAttributeEncoding.Undefined,
                    enumType: null);

            private static CustomAttributeType CreateArrayType(CustomAttributeEncoding elementType)
                => new CustomAttributeType(
                    CustomAttributeEncoding.Array,
                    elementType,
                    CustomAttributeEncoding.Undefined,
                    enumType: null);
        }
#endif
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

#if NATIVEAOT
    internal static partial class RuntimeCustomAttribute
#else
    internal static unsafe partial class CustomAttribute
#endif
    {
        internal static bool IsDefined(RuntimeType type, RuntimeType? caType, bool inherit)
        {
            Debug.Assert(type is not null);

            if (type.GetElementType() is not null)
                return false;

            if (PseudoCustomAttribute.IsDefined(type, caType))
                return true;

#if NATIVEAOT
            if (IsCustomAttributeDefined(type.GetMetadataReader(), type.GetCustomAttributeHandles(), caType))
#else
            if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType))
#endif
                return true;

            if (!inherit)
                return false;

            type = (type.BaseType as RuntimeType)!;

            while (type is not null)
            {
#if NATIVEAOT
                if (IsCustomAttributeDefined(type.GetMetadataReader(), type.GetCustomAttributeHandles(), caType, inherit))
#else
                if (IsCustomAttributeDefined(type.GetRuntimeModule(), type.MetadataToken, caType, 0, inherit))
#endif
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

#if NATIVEAOT
            if (IsCustomAttributeDefined(method.GetMetadataReader(), method.GetCustomAttributeHandles(), caType))
#else
            if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType))
#endif
                return true;

            if (!inherit)
                return false;

            method = method.GetParentDefinition()!;

            while (method is not null)
            {
#if NATIVEAOT
                if (IsCustomAttributeDefined(method.GetMetadataReader(), method.GetCustomAttributeHandles(), caType, inherit))
#else
                if (IsCustomAttributeDefined(method.GetRuntimeModule(), method.MetadataToken, caType, 0, inherit))
#endif
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

#if NATIVEAOT
            return IsCustomAttributeDefined(ctor.GetMetadataReader(), ctor.GetCustomAttributeHandles(), caType);
#else
            return IsCustomAttributeDefined(ctor.GetRuntimeModule(), ctor.MetadataToken, caType);
#endif
        }

        internal static bool IsDefined(RuntimePropertyInfo property, RuntimeType caType)
        {
            Debug.Assert(property is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimePropertyInfo

#if NATIVEAOT
            return IsCustomAttributeDefined(property.GetMetadataReader(), property.GetCustomAttributeHandles(), caType);
#else
            return IsCustomAttributeDefined(property.GetRuntimeModule(), property.MetadataToken, caType);
#endif
        }

        internal static bool IsDefined(RuntimeEventInfo e, RuntimeType caType)
        {
            Debug.Assert(e is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeEventInfo

#if NATIVEAOT
            return IsCustomAttributeDefined(e.GetMetadataReader(), e.GetCustomAttributeHandles(), caType);
#else
            return IsCustomAttributeDefined(e.GetRuntimeModule(), e.MetadataToken, caType);
#endif
        }

        internal static bool IsDefined(RuntimeFieldInfo field, RuntimeType caType)
        {
            Debug.Assert(field is not null);
            Debug.Assert(caType is not null);

            if (PseudoCustomAttribute.IsDefined(field, caType))
                return true;

#if NATIVEAOT
            return IsCustomAttributeDefined(field.GetMetadataReader(), field.GetCustomAttributeHandles(), caType);
#else
            return IsCustomAttributeDefined(field.GetRuntimeModule(), field.MetadataToken, caType);
#endif
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Debug.Assert(parameter is not null);
            Debug.Assert(caType is not null);

            if (PseudoCustomAttribute.IsDefined(parameter, caType))
                return true;

#if NATIVEAOT
            return IsCustomAttributeDefined(parameter.GetMetadataReader(), parameter.GetCustomAttributeHandles(), caType);
#else
            return IsCustomAttributeDefined(parameter.GetRuntimeModule()!, parameter.MetadataToken, caType);
#endif
        }

        internal static bool IsDefined(RuntimeAssembly assembly, RuntimeType caType)
        {
            Debug.Assert(assembly is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeAssembly
#if NATIVEAOT
            return IsCustomAttributeDefined(assembly.GetMetadataReader(), assembly.GetCustomAttributeHandles(), caType);
#else
            return IsCustomAttributeDefined((assembly.ManifestModule as RuntimeModule)!, RuntimeAssembly.GetToken(assembly), caType);
#endif
        }

        internal static bool IsDefined(RuntimeModule module, RuntimeType caType)
        {
            Debug.Assert(module is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeModule

#if NATIVEAOT
            return IsCustomAttributeDefined(module.GetMetadataReader(), module.GetCustomAttributeHandles(), caType);
#else
            return IsCustomAttributeDefined(module, module.MetadataToken, caType);
#endif
        }

#if NATIVEAOT
        private static bool IsCustomAttributeDefined(
            MetadataReader? reader,
            CustomAttributeHandleCollection customAttributeHandles,
            RuntimeType attributeFilterType,
            bool mustBeInheritable = false)
        {
            if (reader is null)
                return false;

            RuntimeType.ListBuilder<object> derivedAttributes = default;
            foreach (CustomAttributeHandle customAttributeHandle in customAttributeHandles)
            {
                if (FilterCustomAttributeRecord(
                    customAttributeHandle,
                    reader,
                    attributeFilterType,
                    mustBeInheritable,
                    ref derivedAttributes))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FilterCustomAttributeRecord(
            CustomAttributeHandle customAttributeHandle,
            MetadataReader reader,
            RuntimeType attributeFilterType,
            bool mustBeInheritable,
            ref RuntimeType.ListBuilder<object> derivedAttributes)
        {
            CustomAttribute customAttribute = customAttributeHandle.GetCustomAttribute(reader);
            Handle attributeTypeHandle = customAttribute.GetAttributeTypeHandle(reader);
            RuntimeType attributeType = (RuntimeType)attributeTypeHandle.Resolve(reader, new TypeContext(null, null)).ToType();

            if (!MatchesTypeFilter(attributeType, attributeFilterType))
                return false;

            return AttributeUsageCheck(attributeType, mustBeInheritable, ref derivedAttributes);
        }
#endif

        internal static object[] GetCustomAttributes(RuntimeType type, RuntimeType caType, bool inherit)
        {
            Debug.Assert(type is not null);
            Debug.Assert(caType is not null);

            if (type.GetElementType() is not null)
                return CreateAttributeArrayHelper(caType, 0);

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                type = (type.GetGenericTypeDefinition() as RuntimeType)!;

            RuntimeType.ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(type, caType, ref pcas);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !GetAttributeUsage(caType).Inherited))
            {
#if NATIVEAOT
                object[] attributes = GetCustomAttributes(type.GetMetadataReader(), type.GetCustomAttributeHandles(), pcas.Count, caType);
#else
                object[] attributes = GetCustomAttributes(type.GetRuntimeModule(), type.MetadataToken, pcas.Count, caType);
#endif
                if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
                return attributes;
            }

            RuntimeType.ListBuilder<object> result = default;
            bool mustBeInheritable = false;

            for (int i = 0; i < pcas.Count; i++)
                result.Add(pcas[i]);

            do
            {
#if NATIVEAOT
                AddCustomAttributes(ref result, type.GetMetadataReader(), type.GetCustomAttributeHandles(), caType, mustBeInheritable, result);
#else
                AddCustomAttributes(ref result, type.GetRuntimeModule(), type.MetadataToken, caType, mustBeInheritable, result);
#endif
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

            RuntimeType.ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(method, caType, ref pcas);

            // if we are asked to go up the hierarchy chain we have to do it now and regardless of the
            // attribute usage for the specific attribute because a derived attribute may override the usage...
            // ... however if the attribute is sealed we can rely on the attribute usage
            if (!inherit || (caType.IsSealed && !GetAttributeUsage(caType).Inherited))
            {
#if NATIVEAOT
                object[] attributes = GetCustomAttributes(method.GetMetadataReader(), method.GetCustomAttributeHandles(), pcas.Count, caType);
#else
                object[] attributes = GetCustomAttributes(method.GetRuntimeModule(), method.MetadataToken, pcas.Count, caType);
#endif
                if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
                return attributes;
            }

            RuntimeType.ListBuilder<object> result = default;
            bool mustBeInheritable = false;

            for (int i = 0; i < pcas.Count; i++)
                result.Add(pcas[i]);

            while (method != null)
            {
#if NATIVEAOT
                AddCustomAttributes(ref result, method.GetMetadataReader(), method.GetCustomAttributeHandles(), caType, mustBeInheritable, result);
#else
                AddCustomAttributes(ref result, method.GetRuntimeModule(), method.MetadataToken, caType, mustBeInheritable, result);
#endif
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

#if NATIVEAOT
            return GetCustomAttributes(ctor.GetMetadataReader(), ctor.GetCustomAttributeHandles(), 0, caType);
#else
            return GetCustomAttributes(ctor.GetRuntimeModule(), ctor.MetadataToken, 0, caType);
#endif
        }

        internal static object[] GetCustomAttributes(RuntimePropertyInfo property, RuntimeType caType)
        {
            Debug.Assert(property is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimePropertyInfo

#if NATIVEAOT
            return GetCustomAttributes(property.GetMetadataReader(), property.GetCustomAttributeHandles(), 0, caType);
#else
            return GetCustomAttributes(property.GetRuntimeModule(), property.MetadataToken, 0, caType);
#endif
        }

        internal static object[] GetCustomAttributes(RuntimeEventInfo e, RuntimeType caType)
        {
            Debug.Assert(e is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeEventInfo

#if NATIVEAOT
            return GetCustomAttributes(e.GetMetadataReader(), e.GetCustomAttributeHandles(), 0, caType);
#else
            return GetCustomAttributes(e.GetRuntimeModule(), e.MetadataToken, 0, caType);
#endif
        }

        internal static object[] GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType)
        {
            Debug.Assert(field is not null);
            Debug.Assert(caType is not null);

            RuntimeType.ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(field, caType, ref pcas);
#if NATIVEAOT
            object[] attributes = GetCustomAttributes(field.GetMetadataReader(), field.GetCustomAttributeHandles(), pcas.Count, caType);
#else
            object[] attributes = GetCustomAttributes(field.GetRuntimeModule(), field.MetadataToken, pcas.Count, caType);
#endif
            if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
            return attributes;
        }

        internal static object[] GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType)
        {
            Debug.Assert(parameter is not null);
            Debug.Assert(caType is not null);

            RuntimeType.ListBuilder<Attribute> pcas = default;
            PseudoCustomAttribute.GetCustomAttributes(parameter, caType, ref pcas);
#if NATIVEAOT
            object[] attributes = GetCustomAttributes(parameter.GetMetadataReader(), parameter.GetCustomAttributeHandles(), pcas.Count, caType);
#else
            object[] attributes = GetCustomAttributes(parameter.GetRuntimeModule()!, parameter.MetadataToken, pcas.Count, caType);
#endif
            if (pcas.Count > 0) pcas.CopyTo(attributes, attributes.Length - pcas.Count);
            return attributes;
        }

        internal static object[] GetCustomAttributes(RuntimeAssembly assembly, RuntimeType caType)
        {
            Debug.Assert(assembly is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeAssembly

#if NATIVEAOT
            return GetCustomAttributes(assembly.GetMetadataReader(), assembly.GetCustomAttributeHandles(), 0, caType);
#else
            int assemblyToken = RuntimeAssembly.GetToken(assembly);
            return GetCustomAttributes((assembly.ManifestModule as RuntimeModule)!, assemblyToken, 0, caType);
#endif
        }

        internal static object[] GetCustomAttributes(RuntimeModule module, RuntimeType caType)
        {
            Debug.Assert(module is not null);
            Debug.Assert(caType is not null);

            // No pseudo attributes for RuntimeModule

#if NATIVEAOT
            return GetCustomAttributes(module.GetMetadataReader(), module.GetCustomAttributeHandles(), 0, caType);
#else
            return GetCustomAttributes(module, module.MetadataToken, 0, caType);
#endif
        }

#if NATIVEAOT
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:MethodParameterDoesntMeetThisParameterRequirements",
            Justification = "Linker guarantees presence of all the constructor parameters, property setters and fields which are accessed by any " +
                            "attribute instantiation which is present in the code linker has analyzed.")]
        private static void AddCustomAttributes(
            ref RuntimeType.ListBuilder<object> attributes,
            MetadataReader? reader,
            CustomAttributeHandleCollection customAttributeHandles,
            RuntimeType? attributeFilterType,
            bool mustBeInheritable,
            RuntimeType.ListBuilder<object> derivedAttributes)
        {
            if (reader is null)
                return;

            foreach (CustomAttributeHandle customAttributeHandle in customAttributeHandles)
            {
                if (!FilterCustomAttributeRecord(
                    customAttributeHandle,
                    reader,
                    attributeFilterType!,
                    mustBeInheritable,
                    ref derivedAttributes))
                {
                    continue;
                }

                attributes.Add(new RuntimeCustomAttributeData(reader, customAttributeHandle).Instantiate());
            }
        }
#else
        internal static bool IsAttributeDefined(RuntimeModule decoratedModule, int decoratedMetadataToken, int attributeCtorToken)
        {
            return IsCustomAttributeDefined(decoratedModule, decoratedMetadataToken, null, attributeCtorToken, false);
        }

        internal static bool IsCustomAttributeDefined(
            RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType? attributeFilterType)
        {
            return IsCustomAttributeDefined(decoratedModule, decoratedMetadataToken, attributeFilterType, 0, false);
        }

        private static bool IsCustomAttributeDefined(
            RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType? attributeFilterType, int attributeCtorToken, bool mustBeInheritable)
        {
            MetadataImport scope = decoratedModule.MetadataImport;

            scope.EnumCustomAttributes(decoratedMetadataToken, out MetadataEnumResult attributeTokens);

            if (attributeTokens.Length == 0)
            {
                return false;
            }

            CustomAttributeRecord record = default;
            if (attributeFilterType is not null)
            {
                Debug.Assert(attributeCtorToken == 0);

                RuntimeType.ListBuilder<object> derivedAttributes = default;

                for (int i = 0; i < attributeTokens.Length; i++)
                {
                    scope.GetCustomAttributeProps(attributeTokens[i],
                        out record.tkCtor.Value, out record.blob);

                    if (FilterCustomAttributeRecord(record.tkCtor, in scope,
                        decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable, ref derivedAttributes,
                        out _, out _, out _))
                    {
                        return true;
                    }
                }
            }
            else
            {
                Debug.Assert(attributeFilterType is null);
                Debug.Assert(!MetadataToken.IsNullToken(attributeCtorToken));

                for (int i = 0; i < attributeTokens.Length; i++)
                {
                    scope.GetCustomAttributeProps(attributeTokens[i],
                        out record.tkCtor.Value, out record.blob);

                    if (record.tkCtor == attributeCtorToken)
                    {
                        return true;
                    }
                }
            }
            GC.KeepAlive(decoratedModule);

            return false;
        }
#endif

        private static object[] GetCustomAttributes(
#if NATIVEAOT
            MetadataReader? reader, CustomAttributeHandleCollection customAttributeHandles,
#else
            RuntimeModule decoratedModule, int decoratedMetadataToken,
#endif
            int pcaCount, RuntimeType attributeFilterType)
        {
            RuntimeType.ListBuilder<object> attributes = default;

#if NATIVEAOT
            AddCustomAttributes(ref attributes, reader, customAttributeHandles, attributeFilterType, false, default);
#else
            AddCustomAttributes(ref attributes, decoratedModule, decoratedMetadataToken, attributeFilterType, false, default);
#endif

            object[] result = CreateAttributeArrayHelper(attributeFilterType, attributes.Count + pcaCount);
            for (int i = 0; i < attributes.Count; i++)
            {
                result[i] = attributes[i];
            }
            return result;
        }

#if !NATIVEAOT
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:MethodParameterDoesntMeetThisParameterRequirements",
            Justification = "Linker guarantees presence of all the constructor parameters, property setters and fields which are accessed by any " +
                            "attribute instantiation which is present in the code linker has analyzed." +
                            "As such the reflection usage in this method will never fail as those methods/fields will be present.")]
        private static void AddCustomAttributes(
            ref RuntimeType.ListBuilder<object> attributes,
            RuntimeModule decoratedModule, int decoratedMetadataToken,
            RuntimeType? attributeFilterType, bool mustBeInheritable,
            // The derivedAttributes list must be passed by value so that it is not modified with the discovered attributes
            RuntimeType.ListBuilder<object> derivedAttributes)
        {
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
                {
                    continue;
                }

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
                        int data = Unsafe.ReadUnaligned<int>((void*)blobStart);
                        if (!BitConverter.IsLittleEndian)
                        {
                            // Metadata is always written in little-endian format. Must account for this on
                            // big-endian platforms.
                            data = BinaryPrimitives.ReverseEndianness(data);
                        }

                        const int CustomAttributeVersion = 0x0001;
                        if ((data & 0xffff) != CustomAttributeVersion)
                        {
                            throw new CustomAttributeFormatException();
                        }
                        cNamedArgs = data >> 16;

                        blobStart = (IntPtr)((byte*)blobStart + 4); // skip version and namedArgs count
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Module.ResolveMethod and Module.ResolveType are marked as RequiresUnreferencedCode because they rely on tokens" +
                            "which are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break." +
                            "The usage here is not like that as all these tokens come from existing metadata loaded from some IL" +
                            "and so trimming has no effect (the tokens are read AFTER trimming occurred).")]
        private static bool FilterCustomAttributeRecord(
            MetadataToken caCtorToken,
            in MetadataImport scope,
            RuntimeModule decoratedModule,
            MetadataToken decoratedToken,
            RuntimeType attributeFilterType,
            bool mustBeInheritable,
            ref RuntimeType.ListBuilder<object> derivedAttributes,
            out RuntimeType attributeType,
            out IRuntimeMethodInfo? ctorWithParameters,
            out bool isVarArg)
        {
            ctorWithParameters = null;
            isVarArg = false;

            // Resolve attribute type from ctor parent token found in decorated decoratedModule scope
            attributeType = (decoratedModule.ResolveType(scope.GetParentToken(caCtorToken), null, null) as RuntimeType)!;

            // Test attribute type against user provided attribute type filter
            if (!MatchesTypeFilter(attributeType, attributeFilterType))
                return false;

            // Ensure if attribute type must be inheritable that it is inheritable
            // Ensure that to consider a duplicate attribute type AllowMultiple is true
            if (!AttributeUsageCheck(attributeType, mustBeInheritable, ref derivedAttributes))
                return false;

            // Resolve the attribute ctor
            ConstArray ctorSig = scope.GetMethodSignature(caCtorToken);
            isVarArg = (ctorSig[0] & 0x05) != 0;
            bool ctorHasParameters = ctorSig[1] != 0;

            if (ctorHasParameters)
            {
                // Resolve method ctor token found in decorated decoratedModule scope
                // See https://github.com/dotnet/runtime/issues/11637 for why we fast-path non-generics here (fewer allocations)
                if (attributeType.IsGenericType)
                {
                    ctorWithParameters = decoratedModule.ResolveMethod(caCtorToken, attributeType.GenericTypeArguments, null)!.MethodHandle.GetMethodInfo();
                }
                else
                {
                    ctorWithParameters = new ModuleHandle(decoratedModule).ResolveMethodHandle(caCtorToken).GetMethodInfo();
                }
            }

            // Visibility checks
            MetadataToken tkParent = default;

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
                                                    default;

            RuntimeTypeHandle attributeTypeHandle = attributeType.TypeHandle;

            bool result = RuntimeMethodHandle.IsCAVisibleFromDecoratedType(new QCallTypeHandle(ref attributeTypeHandle),
                                                                    ctorWithParameters is not null ? IRuntimeMethodInfo.GetValue(ctorWithParameters) : RuntimeMethodHandleInternal.EmptyHandle,
                                                                    new QCallTypeHandle(ref parentTypeHandle),
                                                                    new QCallModule(ref decoratedModule)) != Interop.BOOL.FALSE;

            GC.KeepAlive(ctorWithParameters);
            return result;
        }

#endif

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
            RuntimeType attributeType, bool mustBeInheritable, ref RuntimeType.ListBuilder<object> derivedAttributes)
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

#if !NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CustomAttribute_ParseAttributeUsageAttribute")]
        [SuppressGCTransition]
        private static partial int ParseAttributeUsageAttribute(
            IntPtr pData,
            int cData,
            int* pTargets,
            int* pAllowMultiple,
            int* pInherited);

        private static bool ParseAttributeUsageAttribute(
            ConstArray blob,
            out AttributeTargets attrTargets,
            out bool allowMultiple,
            out bool inherited)
        {
            int attrTargetsLocal = 0;
            int allowMultipleLocal = 0;
            int inheritedLocal = 0;
            int result = ParseAttributeUsageAttribute(blob.Signature, blob.Length, &attrTargetsLocal, &allowMultipleLocal, &inheritedLocal);
            attrTargets = (AttributeTargets)attrTargetsLocal;
            allowMultiple = allowMultipleLocal != 0;
            inherited = inheritedLocal != 0;
            return result != 0;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CustomAttribute_CreateCustomAttributeInstance")]
        private static partial void CreateCustomAttributeInstance(
            QCallModule pModule,
            ObjectHandleOnStack type,
            ObjectHandleOnStack pCtor,
            ref IntPtr ppBlob,
            IntPtr pEndBlob,
            out int pcNamedArgs,
            ObjectHandleOnStack instance);

        private static object CreateCustomAttributeInstance(RuntimeModule module, RuntimeType type, IRuntimeMethodInfo ctor, ref IntPtr blob, IntPtr blobEnd, out int namedArgs)
        {
            if (module is null)
            {
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            }

            object? result = null;
            CreateCustomAttributeInstance(
                new QCallModule(ref module),
                ObjectHandleOnStack.Create(ref type),
                ObjectHandleOnStack.Create(ref ctor),
                ref blob,
                blobEnd,
                out namedArgs,
                ObjectHandleOnStack.Create(ref result));
            return result!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CustomAttribute_CreatePropertyOrFieldData", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void CreatePropertyOrFieldData(
            QCallModule pModule,
            ref IntPtr ppBlobStart,
            IntPtr pBlobEnd,
            StringHandleOnStack name,
            [MarshalAs(UnmanagedType.Bool)] out bool bIsProperty,
            ObjectHandleOnStack type,
            ObjectHandleOnStack value);

        private static void GetPropertyOrFieldData(
            RuntimeModule module, ref IntPtr blobStart, IntPtr blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value)
        {
            if (module is null)
            {
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            }

            string? nameLocal = null;
            RuntimeType? typeLocal = null;
            object? valueLocal = null;
            CreatePropertyOrFieldData(
                new QCallModule(ref module),
                ref blobStart,
                blobEnd,
                new StringHandleOnStack(ref nameLocal),
                out isProperty,
                ObjectHandleOnStack.Create(ref typeLocal),
                ObjectHandleOnStack.Create(ref valueLocal));
            name = nameLocal!;
            type = typeLocal;
            value = valueLocal;
        }
#endif
    }

    internal static class PseudoCustomAttribute
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

#if !NATIVEAOT
        [Conditional("DEBUG")]
        private static void VerifyPseudoCustomAttribute(RuntimeType pca)
        {
            // If any of these are invariants are no longer true will have to
            // re-architect the PCA product logic and test cases.
            Debug.Assert(pca.BaseType == typeof(Attribute), "Pseudo CA Error - Incorrect base type");
            AttributeUsageAttribute usage = CustomAttribute.GetAttributeUsage(pca);
            Debug.Assert(!usage.Inherited, "Pseudo CA Error - Unexpected Inherited value");
            if (pca == typeof(TypeForwardedToAttribute))
            {
                Debug.Assert(usage.AllowMultiple, "Pseudo CA Error - Unexpected AllowMultiple value");
            }
            else
            {
                Debug.Assert(!usage.AllowMultiple, "Pseudo CA Error - Unexpected AllowMultiple value");
            }
        }
#endif
        #endregion

        #region Internal Static
        internal static void GetCustomAttributes(RuntimeType type, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
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

        internal static void GetCustomAttributes(RuntimeMethodInfo method, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
        {
            Debug.Assert(method is not null);
            Debug.Assert(caType is not null);

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType))
                return;

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

        internal static void GetCustomAttributes(RuntimeParameterInfo parameter, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
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
#if !NATIVEAOT
            if (all || caType == typeof(MarshalAsAttribute))
            {
                if (GetMarshalAsCustomAttribute(parameter) is not null) return true;
            }
#endif

            return false;
        }

        internal static void GetCustomAttributes(RuntimeFieldInfo field, RuntimeType caType, ref RuntimeType.ListBuilder<Attribute> pcas)
        {
            Debug.Assert(field is not null);
            Debug.Assert(caType is not null);

            bool all = caType == typeof(object) || caType == typeof(Attribute);
            if (!all && !s_pca.Contains(caType))
                return;

            Attribute? pca;

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

#if !NATIVEAOT
        private static DllImportAttribute? GetDllImportCustomAttribute(RuntimeMethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
                return null;

            RuntimeModule module = method.Module.ModuleHandle.GetRuntimeModule();
            MetadataImport scope = module.MetadataImport;
            int token = method.MetadataToken;
            scope.GetPInvokeMap(token, out PInvokeAttributes flags, out string entryPoint, out string dllName);
            GC.KeepAlive(module);

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
            ConstArray nativeType = scope.MetadataImport.GetFieldMarshal(token);

            if (nativeType.Length == 0)
                return null;

            return MetadataImport.GetMarshalAs(nativeType, scope);
        }

        private static FieldOffsetAttribute? GetFieldOffsetCustomAttribute(RuntimeFieldInfo field)
        {
            if (field.DeclaringType is not null)
            {
                RuntimeModule module = field.GetRuntimeModule();
                if (module.MetadataImport.GetFieldOffset(field.DeclaringType.MetadataToken, field.MetadataToken, out int fieldOffset))
                {
                    return new FieldOffsetAttribute(fieldOffset);
                }
                GC.KeepAlive(module);
            }
            return null;
        }

        internal static StructLayoutAttribute? GetStructLayoutCustomAttribute(RuntimeType type)
        {
            if (type.IsActualInterface || type.HasElementType || type.IsGenericParameter)
                return null;

            LayoutKind layoutKind = LayoutKind.Auto;
            switch (type.Attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.ExplicitLayout: layoutKind = LayoutKind.Explicit; break;
                case TypeAttributes.AutoLayout: layoutKind = LayoutKind.Auto; break;
                case TypeAttributes.SequentialLayout: layoutKind = LayoutKind.Sequential; break;
                case TypeAttributes.ExtendedLayout: layoutKind = LayoutKind.Extended; break;
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
            RuntimeModule module = type.GetRuntimeModule();
            module.MetadataImport.GetClassLayout(type.MetadataToken, out int pack, out int size);
            GC.KeepAlive(module);

            StructLayoutAttribute attribute = new StructLayoutAttribute(layoutKind);

            attribute.Pack = pack;
            attribute.Size = size;
            attribute.CharSet = charSet;

            return attribute;
        }
#else
        private static FieldOffsetAttribute? GetFieldOffsetCustomAttribute(RuntimeFieldInfo field)
        {
            return field.DeclaringType!.IsExplicitLayout ?
                new FieldOffsetAttribute(field.ExplicitLayoutFieldOffsetData) :
                null;
        }
#endif
    }
}
