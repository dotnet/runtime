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
