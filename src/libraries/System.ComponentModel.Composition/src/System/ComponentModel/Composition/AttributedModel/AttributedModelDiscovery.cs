// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Composition.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.AttributedModel
{
    internal static class AttributedModelDiscovery
    {
        public static ComposablePartDefinition? CreatePartDefinitionIfDiscoverable(Type type, ICompositionElement? origin)
        {
            AttributedPartCreationInfo creationInfo = new AttributedPartCreationInfo(type, null, false, origin);
            if (!creationInfo.IsPartDiscoverable())
            {
                return null;
            }

            return new ReflectionComposablePartDefinition(creationInfo);
        }

        public static ReflectionComposablePartDefinition CreatePartDefinition(Type type, PartCreationPolicyAttribute? partCreationPolicy, bool ignoreConstructorImports, ICompositionElement? origin)
        {
            ArgumentNullException.ThrowIfNull(type);

            AttributedPartCreationInfo creationInfo = new AttributedPartCreationInfo(type, partCreationPolicy, ignoreConstructorImports, origin);

            return new ReflectionComposablePartDefinition(creationInfo);
        }

        public static ReflectionComposablePart CreatePart(object attributedPart)
        {
            ArgumentNullException.ThrowIfNull(attributedPart);

            // If given an instance then we want to pass the default composition options because we treat it as a shared part
            ReflectionComposablePartDefinition definition = AttributedModelDiscovery.CreatePartDefinition(attributedPart.GetType(), PartCreationPolicyAttribute.Shared, true, (ICompositionElement?)null);

            return new ReflectionComposablePart(definition, attributedPart);
        }

        public static ReflectionComposablePart CreatePart(object attributedPart, ReflectionContext reflectionContext)
        {
            ArgumentNullException.ThrowIfNull(attributedPart);
            ArgumentNullException.ThrowIfNull(reflectionContext);

            // If given an instance then we want to pass the default composition options because we treat it as a shared part
            var mappedType = reflectionContext.MapType(IntrospectionExtensions.GetTypeInfo(attributedPart.GetType()));
            if (mappedType.Assembly.ReflectionOnly)
            {
                throw new ArgumentException(SR.Format(SR.Argument_ReflectionContextReturnsReflectionOnlyType, nameof(reflectionContext)), nameof(reflectionContext));
            }

            ReflectionComposablePartDefinition definition = AttributedModelDiscovery.CreatePartDefinition(mappedType, PartCreationPolicyAttribute.Shared, true, (ICompositionElement?)null);

            return CreatePart(definition, attributedPart);
        }

        public static ReflectionComposablePart CreatePart(ComposablePartDefinition partDefinition, object attributedPart)
        {
            ArgumentNullException.ThrowIfNull(partDefinition);
            ArgumentNullException.ThrowIfNull(attributedPart);

            return new ReflectionComposablePart((ReflectionComposablePartDefinition)partDefinition, attributedPart);
        }

        public static ReflectionParameterImportDefinition CreateParameterImportDefinition(ParameterInfo parameter, ICompositionElement origin)
        {
            Requires.NotNull(parameter, nameof(parameter));

            ReflectionParameter reflectionParameter = parameter.ToReflectionParameter();
            IAttributedImport attributedImport = AttributedModelDiscovery.GetAttributedImport(reflectionParameter, parameter);
            ImportType importType = new ImportType(reflectionParameter.ReturnType, attributedImport.Cardinality);

            if (importType.IsPartCreator)
            {
                return new PartCreatorParameterImportDefinition(
                    new Lazy<ParameterInfo>(() => parameter),
                    origin,
                    new ContractBasedImportDefinition(
                        attributedImport.GetContractNameFromImport(importType),
                        attributedImport.GetTypeIdentityFromImport(importType),
                        CompositionServices.GetRequiredMetadata(importType.MetadataViewType),
                        attributedImport.Cardinality,
                        false,
                        true,
                        CreationPolicy.NonShared,
                        CompositionServices.GetImportMetadata(importType, attributedImport)));
            }
            else
            {
                return new ReflectionParameterImportDefinition(
                    new Lazy<ParameterInfo>(() => parameter),
                    attributedImport.GetContractNameFromImport(importType),
                    attributedImport.GetTypeIdentityFromImport(importType),
                    CompositionServices.GetRequiredMetadata(importType.MetadataViewType),
                    attributedImport.Cardinality,
                    attributedImport.RequiredCreationPolicy,
                    CompositionServices.GetImportMetadata(importType, attributedImport),
                    origin);
            }
        }

        public static ReflectionMemberImportDefinition CreateMemberImportDefinition(MemberInfo member, ICompositionElement origin)
        {
            Requires.NotNull(member, nameof(member));

            ReflectionWritableMember reflectionMember = member.ToReflectionWritableMember();
            IAttributedImport attributedImport = AttributedModelDiscovery.GetAttributedImport(reflectionMember, member);
            ImportType importType = new ImportType(reflectionMember.ReturnType, attributedImport.Cardinality);

            if (importType.IsPartCreator)
            {
                return new PartCreatorMemberImportDefinition(
                    new LazyMemberInfo(member),
                    origin,
                    new ContractBasedImportDefinition(
                        attributedImport.GetContractNameFromImport(importType),
                        attributedImport.GetTypeIdentityFromImport(importType),
                        CompositionServices.GetRequiredMetadata(importType.MetadataViewType),
                        attributedImport.Cardinality,
                        attributedImport.AllowRecomposition,
                        false,
                        CreationPolicy.NonShared,
                        CompositionServices.GetImportMetadata(importType, attributedImport)));
            }
            else
            {
                //Does this Import re-export the value if so, make it a rpe-requisite
                bool isPrerequisite = member.GetAttributes<ExportAttribute>().Length > 0;
                return new ReflectionMemberImportDefinition(
                    new LazyMemberInfo(member),
                    attributedImport.GetContractNameFromImport(importType),
                    attributedImport.GetTypeIdentityFromImport(importType),
                    CompositionServices.GetRequiredMetadata(importType.MetadataViewType),
                    attributedImport.Cardinality,
                    attributedImport.AllowRecomposition,
                    isPrerequisite,
                    attributedImport.RequiredCreationPolicy,
                    CompositionServices.GetImportMetadata(importType, attributedImport),
                    origin);
            }
        }

        private static IAttributedImport GetAttributedImport(ReflectionItem item, ICustomAttributeProvider attributeProvider)
        {
            IAttributedImport[] imports = attributeProvider.GetAttributes<IAttributedImport>(false);

            // For constructor parameters they may not have an ImportAttribute
            if (imports.Length == 0)
            {
                return new ImportAttribute();
            }

            if (imports.Length > 1)
            {
                CompositionTrace.MemberMarkedWithMultipleImportAndImportMany(item);
            }

            // Regardless of how many imports, always return the first one
            return imports[0];
        }
    }
}
