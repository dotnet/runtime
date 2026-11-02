// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

using System.Reflection.Runtime.EventInfos;
using System.Reflection.Runtime.FieldInfos;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
using System.Reflection.Runtime.Modules;
using System.Reflection.Runtime.PropertyInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;

using Internal.Metadata.NativeFormat;

namespace System.Reflection
{
    internal readonly struct QCustomAttributeList(MetadataReader? reader, CustomAttributeHandleCollection handles)
    {
        public MetadataReader? Reader { get; } = reader;
        public CustomAttributeHandleCollection Handles { get; } = handles;

        public QCustomAttributeList(RuntimeType target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimeFieldInfo target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimeMethodInfo target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimeConstructorInfo target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimeEventInfo target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimePropertyInfo target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimeModule target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimeAssembly target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }

        public QCustomAttributeList(RuntimeParameterInfo target)
            : this(target.GetMetadataReader(), target.GetCustomAttributeHandles()) { }
    }

    internal sealed partial class RuntimeCustomAttributeData : CustomAttributeData
    {
        private static IList<CustomAttributeData> GetCustomAttributes(QCustomAttributeList attributeList)
        {
            MetadataReader? reader = attributeList.Reader;
            CustomAttributeHandleCollection customAttributeHandles = attributeList.Handles;
            if (reader is null || customAttributeHandles.Count == 0)
                return Array.Empty<CustomAttributeData>();

            CustomAttributeData[] customAttributes = new CustomAttributeData[customAttributeHandles.Count];
            int index = 0;
            foreach (CustomAttributeHandle customAttributeHandle in customAttributeHandles)
                customAttributes[index++] = new RuntimeCustomAttributeData(reader, customAttributeHandle);

            return Array.AsReadOnly(customAttributes);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
            Justification = "Metadata generation ensures custom attribute constructors are resolvable.")]
        private static ConstructorInfo ResolveAttributeConstructor(MetadataReader reader, CustomAttribute customAttribute)
        {
            // There is no chance a custom attribute type will be an open type specification so we can safely pass in the empty context here.
            RuntimeType attributeType = (RuntimeType)customAttribute.GetAttributeTypeHandle(reader)
                .Resolve(reader, new TypeContext(null, null))
                .ToType();
            return ResolveAttributeConstructor(reader, customAttribute, attributeType);
        }

        internal static ConstructorInfo ResolveAttributeConstructor(
            MetadataReader reader,
            CustomAttribute customAttribute,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            RuntimeType attributeType)
        {
            RuntimeTypeInfo attributeTypeInfo = attributeType.GetRuntimeTypeInfo();

            if (customAttribute.Constructor.HandleType == HandleType.QualifiedMethod)
            {
                QualifiedMethod qualifiedMethod = customAttribute.Constructor.ToQualifiedMethodHandle(reader).GetQualifiedMethod(reader);
                MethodHandle methodHandle = qualifiedMethod.Method;
                NativeFormatRuntimeNamedTypeInfo namedAttributeType = (NativeFormatRuntimeNamedTypeInfo)attributeTypeInfo;
                NativeFormatMethodCommon methodCommon = new NativeFormatMethodCommon(methodHandle, namedAttributeType, attributeTypeInfo);
                return attributeTypeInfo.GetConstructorWithSameMetadataDefinitionAs(methodCommon);
            }

            MemberReference memberReference = customAttribute.Constructor.ToMemberReferenceHandle(reader).GetMemberReference(reader);
            MethodSignature signature = memberReference.Signature.ParseMethodSignature(reader);
            HandleCollection signatureParameters = signature.Parameters;
            Type[] expectedParameterTypes = new Type[signatureParameters.Count];
            int index = 0;
            foreach (Handle parameterHandle in signatureParameters)
            {
                expectedParameterTypes[index++] = parameterHandle.Resolve(reader, attributeTypeInfo.TypeContext).ToType();
            }

            foreach (ConstructorInfo candidate in attributeType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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
    }

    public readonly partial struct CustomAttributeTypedArgument
    {
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs.")]
        internal static object? ConvertToRuntimeValue(CustomAttributeEncodedArgument encodedArg)
        {
            CustomAttributeEncoding encodedType = encodedArg.CustomAttributeType.EncodedType;

            if (encodedType == CustomAttributeEncoding.Undefined)
                throw new ArgumentException(null, nameof(encodedArg));

            if (encodedType == CustomAttributeEncoding.Enum)
            {
                return Enum.ToObject(
                    encodedArg.CustomAttributeType.EnumType!,
                    EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedArg.CustomAttributeType.EncodedEnumType));
            }

            if (encodedType == CustomAttributeEncoding.String)
                return encodedArg.StringValue;

            if (encodedType == CustomAttributeEncoding.Type)
                return encodedArg.TypeValue;

            if (encodedType == CustomAttributeEncoding.Array)
            {
                if (encodedArg.ArrayValue is null)
                    return null;

                CustomAttributeEncoding encodedElementType = encodedArg.CustomAttributeType.EncodedArrayType;
                Type elementType = encodedElementType == CustomAttributeEncoding.Enum ?
                    encodedArg.CustomAttributeType.EnumType! :
                    CustomAttributeEncodingToType(encodedElementType);
                Array array = Array.CreateInstanceFromArrayType(elementType.MakeArrayType(), encodedArg.ArrayValue.Length);

                for (int i = 0; i < encodedArg.ArrayValue.Length; i++)
                {
                    array.SetValue(ConvertToRuntimeValue(encodedArg.ArrayValue[i]), i);
                }

                return array;
            }

            return EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedType);
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs.")]
        internal static Type GetRuntimeType(CustomAttributeType type)
        {
            bool isArray = type.EncodedType == CustomAttributeEncoding.Array;
            CustomAttributeEncoding encoding = isArray ? type.EncodedArrayType : type.EncodedType;
            Type result = encoding == CustomAttributeEncoding.Enum ? type.EnumType! : CustomAttributeEncodingToType(encoding);
            return isArray ? result.MakeArrayType() : result;
        }
    }

    internal sealed partial class CustomAttributeEncodedArgument
    {
        internal static object? ParseValue(ref CustomAttributeDataParser parser, CustomAttributeType type)
        {
            try
            {
                return CustomAttributeTypedArgument.ConvertToRuntimeValue(parser.ParseArgument(type));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                throw new CustomAttributeFormatException(ex.Message, ex);
            }
        }
    }

    internal static partial class RuntimeCustomAttribute
    {
        private static bool IsCustomAttributeDefined(
            QCustomAttributeList customAttributes,
            RuntimeType attributeFilterType,
            bool mustBeInheritable = false)
        {
            MetadataReader? reader = customAttributes.Reader;
            if (reader is null)
                return false;

            ListBuilder<object> derivedAttributes = default;
            foreach (CustomAttributeHandle customAttributeHandle in customAttributes.Handles)
            {
                CustomAttribute customAttribute = customAttributeHandle.GetCustomAttribute(reader);
                if (FilterCustomAttributeRecord(
                    customAttribute,
                    reader,
                    attributeFilterType,
                    mustBeInheritable,
                    ref derivedAttributes,
                    out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FilterCustomAttributeRecord(
            CustomAttribute customAttribute,
            MetadataReader reader,
            RuntimeType attributeFilterType,
            bool mustBeInheritable,
            ref ListBuilder<object> derivedAttributes,
            out RuntimeType attributeType)
        {
            Handle attributeTypeHandle = customAttribute.GetAttributeTypeHandle(reader);
            attributeType = (RuntimeType)attributeTypeHandle.Resolve(reader, new TypeContext(null, null)).ToType();

            if (!MatchesTypeFilter(attributeType, attributeFilterType))
                return false;

            return AttributeUsageCheck(attributeType, mustBeInheritable, ref derivedAttributes);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:MethodParameterDoesntMeetThisParameterRequirements",
            Justification = "Linker guarantees presence of all the property setters and fields which are accessed by any " +
                            "attribute instantiation which is present in the code linker has analyzed.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:MethodParameterDoesntMeetThisParameterRequirements",
            Justification = "Linker guarantees presence of all the constructor parameters which are accessed by any " +
                            "attribute instantiation which is present in the code linker has analyzed.")]
        private static void AddCustomAttributes(
            ref ListBuilder<object> attributes,
            QCustomAttributeList customAttributes,
            RuntimeType? attributeFilterType,
            bool mustBeInheritable,
            ListBuilder<object> derivedAttributes)
        {
            MetadataReader? reader = customAttributes.Reader;
            if (reader is null)
                return;

            foreach (CustomAttributeHandle customAttributeHandle in customAttributes.Handles)
            {
                CustomAttribute customAttribute = customAttributeHandle.GetCustomAttribute(reader);

                if (!FilterCustomAttributeRecord(
                    customAttribute,
                    reader,
                    attributeFilterType!,
                    mustBeInheritable,
                    ref derivedAttributes,
                    out RuntimeType attributeType))
                {
                    continue;
                }

                // TODO-NativeAOT: Match CoreCLR's custom attribute constructor visibility checks.
                ConstructorInfo constructor = RuntimeCustomAttributeData.ResolveAttributeConstructor(reader, customAttribute, attributeType);
                ReadOnlySpan<ParameterInfo> constructorParameters = constructor.GetParametersAsSpan();
                CustomAttributeEncodedArgument.CustomAttributeDataParser parser = new(customAttribute, reader);
                object?[]? invokeArguments = null;
                if (constructorParameters.Length != 0)
                {
                    invokeArguments = new object?[constructorParameters.Length];
                    for (int i = 0; i < constructorParameters.Length; i++)
                    {
                        invokeArguments[i] = CustomAttributeEncodedArgument.ParseValue(
                            ref parser, new CustomAttributeType((RuntimeType)constructorParameters[i].ParameterType));
                    }
                }

                object attribute = constructor.Invoke(BindingFlags.Default, binder: null, invokeArguments, culture: null);
                int namedCount = parser.GetNamedArgumentCount();
                for (int i = 0; i < namedCount; i++)
                {
                    CustomAttributeType argumentType = parser.GetNamedArgument(out string? argumentName, out bool isProperty);
                    string name = argumentName!;
                    Type type = CustomAttributeTypedArgument.GetRuntimeType(argumentType);
                    object? value = CustomAttributeEncodedArgument.ParseValue(ref parser, argumentType);

                    try
                    {
                        if (isProperty)
                        {
                            PropertyInfo? property = attributeType.GetProperty(name, type, []) ??
                                throw new CustomAttributeFormatException(SR.Format(SR.RFLCT_InvalidPropFail, name));
                            MethodInfo setMethod = property.GetSetMethod(true)!;

                            // Public properties may have non-public setter methods
                            if (!setMethod.IsPublic)
                            {
                                continue;
                            }

                            property.SetValue(attribute, value, BindingFlags.Default, binder: null, index: null, culture: null);
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

                Debug.Assert(parser.Remaining == 0);
                attributes.Add(attribute);
            }
        }
    }

    internal static partial class PseudoCustomAttribute
    {
        private static FieldOffsetAttribute? GetFieldOffsetCustomAttribute(RuntimeFieldInfo field)
        {
            return field.DeclaringType!.IsExplicitLayout ?
                new FieldOffsetAttribute(field.ExplicitLayoutFieldOffsetData) :
                null;
        }
    }
}
