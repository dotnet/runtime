// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;

using Internal.LowLevelLinq;
using Internal.Reflection.Core;
using Internal.Reflection.Augments;
using Internal.Reflection.Core.Execution;
using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.CustomAttributes.NativeFormat
{
    //
    // The Runtime's implementation of CustomAttributeData for normal metadata-based attributes
    //
    internal sealed class NativeFormatCustomAttributeData : RuntimeCustomAttributeData
    {
        internal NativeFormatCustomAttributeData(MetadataReader reader, CustomAttributeHandle customAttributeHandle)
        {
            _reader = reader;
            _customAttribute = customAttributeHandle.GetCustomAttribute(reader);
        }

        public sealed override Type AttributeType
        {
            get
            {
                Type lazyAttributeType = _lazyAttributeType;
                if (lazyAttributeType == null)
                {
                    lazyAttributeType = _lazyAttributeType = _customAttribute.GetAttributeTypeHandle(_reader).Resolve(_reader, new TypeContext(null, null));
                }
                return lazyAttributeType;
            }
        }

        public sealed override ConstructorInfo Constructor
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
                Justification = "Metadata generation ensures custom attribute constructors are resolvable.")]
            get
            {
                MetadataReader reader = _reader;
                HandleType constructorHandleType = _customAttribute.Constructor.HandleType;

                if (constructorHandleType == HandleType.QualifiedMethod)
                {
                    QualifiedMethod qualifiedMethod = _customAttribute.Constructor.ToQualifiedMethodHandle(reader).GetQualifiedMethod(reader);
                    TypeDefinitionHandle declaringType = qualifiedMethod.EnclosingType;
                    MethodHandle methodHandle = qualifiedMethod.Method;
                    NativeFormatRuntimeNamedTypeInfo attributeType = NativeFormatRuntimeNamedTypeInfo.GetRuntimeNamedTypeInfo(reader, declaringType, default(RuntimeTypeHandle));
                    return RuntimePlainConstructorInfo<NativeFormatMethodCommon>.GetRuntimePlainConstructorInfo(new NativeFormatMethodCommon(methodHandle, attributeType, attributeType));
                }
                else if (constructorHandleType == HandleType.MemberReference)
                {
                    MemberReference memberReference = _customAttribute.Constructor.ToMemberReferenceHandle(reader).GetMemberReference(reader);

                    // There is no chance a custom attribute type will be an open type specification so we can safely pass in the empty context here.
                    TypeContext typeContext = new TypeContext(Array.Empty<RuntimeTypeInfo>(), Array.Empty<RuntimeTypeInfo>());
                    RuntimeTypeInfo attributeType = memberReference.Parent.Resolve(reader, typeContext);
                    MethodSignature sig = memberReference.Signature.ParseMethodSignature(reader);
                    HandleCollection parameters = sig.Parameters;
                    int numParameters = parameters.Count;
                    if (numParameters == 0)
                        return ResolveAttributeConstructor(attributeType, Array.Empty<Type>());

                    Type[] expectedParameterTypes = new Type[numParameters];
                    int index = 0;
                    foreach (Handle _parameterHandle in parameters)
                    {
                        Handle parameterHandle = _parameterHandle;
                        expectedParameterTypes[index++] = parameterHandle.Resolve(reader, attributeType.TypeContext);
                    }
                    return ResolveAttributeConstructor(attributeType, expectedParameterTypes);
                }
                else
                {
                    throw new BadImageFormatException();
                }
            }
        }

        //
        // If throwIfMissingMetadata is false, returns null rather than throwing a missing metadata exception.
        //
        internal sealed override IList<CustomAttributeTypedArgument> GetConstructorArguments(bool throwIfMissingMetadata)
        {
            int index = 0;

            HandleCollection parameterTypeSignatureHandles;
            HandleType handleType = _customAttribute.Constructor.HandleType;
            switch (handleType)
            {
                case HandleType.QualifiedMethod:
                    parameterTypeSignatureHandles = _customAttribute.Constructor.ToQualifiedMethodHandle(_reader).GetQualifiedMethod(_reader).Method.GetMethod(_reader).Signature.GetMethodSignature(_reader).Parameters;
                    break;

                case HandleType.MemberReference:
                    parameterTypeSignatureHandles = _customAttribute.Constructor.ToMemberReferenceHandle(_reader).GetMemberReference(_reader).Signature.ToMethodSignatureHandle(_reader).GetMethodSignature(_reader).Parameters;
                    break;
                default:
                    throw new BadImageFormatException();
            }
            Handle[] ctorTypeHandles = parameterTypeSignatureHandles.ToArray();

            LowLevelListWithIList<CustomAttributeTypedArgument> customAttributeTypedArguments = new LowLevelListWithIList<CustomAttributeTypedArgument>();
            foreach (Handle fixedArgumentHandle in _customAttribute.FixedArguments)
            {
                Handle typeHandle = ctorTypeHandles[index];
                Exception? exception = null;
                RuntimeTypeInfo? argumentType = typeHandle.TryResolve(_reader, new TypeContext(null, null), ref exception);
                if (argumentType == null)
                {
                    if (throwIfMissingMetadata)
                        throw exception!;
                    return null;
                }

                Exception e = fixedArgumentHandle.TryParseConstantValue(_reader, out object? value);
                CustomAttributeTypedArgument customAttributeTypedArgument;
                if (e != null)
                {
                    if (throwIfMissingMetadata)
                        throw e;
                    else
                        return null;
                }
                else
                {
                    customAttributeTypedArgument = WrapInCustomAttributeTypedArgument(value, argumentType);
                }

                customAttributeTypedArguments.Add(customAttributeTypedArgument);
                index++;
            }

            return customAttributeTypedArguments;
        }

        //
        // If throwIfMissingMetadata is false, returns null rather than throwing a missing metadata exception.
        //
        internal sealed override IList<CustomAttributeNamedArgument> GetNamedArguments(bool throwIfMissingMetadata)
        {
            LowLevelListWithIList<CustomAttributeNamedArgument> customAttributeNamedArguments = new LowLevelListWithIList<CustomAttributeNamedArgument>();
            foreach (NamedArgumentHandle namedArgumentHandle in _customAttribute.NamedArguments)
            {
                NamedArgument namedArgument = namedArgumentHandle.GetNamedArgument(_reader);
                string memberName = namedArgument.Name.GetString(_reader);
                bool isField = (namedArgument.Flags == NamedArgumentMemberKind.Field);

                Exception? exception = null;
                RuntimeTypeInfo? argumentType = namedArgument.Type.TryResolve(_reader, new TypeContext(null, null), ref exception);
                if (argumentType == null)
                {
                    if (throwIfMissingMetadata)
                        throw exception!;
                    else
                        return null;
                }

                object? value;
                Exception e = namedArgument.Value.TryParseConstantValue(_reader, out value);
                if (e != null)
                {
                    if (throwIfMissingMetadata)
                        throw e;
                    else
                        return null;
                }
                CustomAttributeTypedArgument typedValue = WrapInCustomAttributeTypedArgument(value, argumentType);

                customAttributeNamedArguments.Add(CreateCustomAttributeNamedArgument(this.AttributeType, memberName, isField, typedValue));
            }
            return customAttributeNamedArguments;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Metadata generation ensures fields/properties referenced from attributes are preserved.")]
        private static CustomAttributeNamedArgument CreateCustomAttributeNamedArgument(Type attributeType, string memberName, bool isField, CustomAttributeTypedArgument typedValue)
        {
            MemberInfo? memberInfo;

            if (isField)
                memberInfo = attributeType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            else
                memberInfo = attributeType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);

            if (memberInfo == null)
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(attributeType);

            return new CustomAttributeNamedArgument(memberInfo, typedValue);
        }

        // Equals/GetHashCode no need to override (they just implement reference equality but desktop never unified these things.)

        private readonly MetadataReader _reader;
        private readonly CustomAttribute _customAttribute;

        private volatile Type _lazyAttributeType;
    }
}
