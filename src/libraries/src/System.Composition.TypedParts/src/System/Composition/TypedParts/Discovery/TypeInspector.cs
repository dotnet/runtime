// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.TypedParts.ActivationFeatures;
using System.Linq;
using System.Reflection;

namespace System.Composition.TypedParts.Discovery
{
    internal class TypeInspector
    {
        private static readonly IDictionary<string, object> s_noMetadata = new Dictionary<string, object>();

        private readonly ActivationFeature[] _activationFeatures;
        private readonly AttributedModelProvider _attributeContext;

        public TypeInspector(AttributedModelProvider attributeContext, ActivationFeature[] activationFeatures)
        {
            _attributeContext = attributeContext;
            _activationFeatures = activationFeatures;
        }

        public bool InspectTypeForPart(TypeInfo type, out DiscoveredPart part)
        {
            part = null;

            if (type.IsAbstract || !type.IsClass || _attributeContext.GetDeclaredAttribute<PartNotDiscoverableAttribute>(type.AsType(), type) != null)
                return false;

            foreach (var export in DiscoverExports(type))
            {
                part = part ?? new DiscoveredPart(type, _attributeContext, _activationFeatures);
                part.AddDiscoveredExport(export);
            }

            return part != null;
        }

        private IEnumerable<DiscoveredExport> DiscoverExports(TypeInfo partType)
        {
            foreach (var export in DiscoverInstanceExports(partType))
                yield return export;

            foreach (var export in DiscoverPropertyExports(partType))
                yield return export;
        }

        private IEnumerable<DiscoveredExport> DiscoverInstanceExports(TypeInfo partType)
        {
            var partTypeAsType = partType.AsType();
            foreach (var export in _attributeContext.GetDeclaredAttributes<ExportAttribute>(partTypeAsType, partType))
            {
                IDictionary<string, object> metadata = new Dictionary<string, object>();
                ReadMetadataAttribute(export, metadata);

                var applied = _attributeContext.GetDeclaredAttributes(partTypeAsType, partType);
                ReadLooseMetadata(applied, metadata);

                var contractType = export.ContractType ?? partTypeAsType;
                CheckInstanceExportCompatibility(partType, contractType.GetTypeInfo());

                var exportKey = new CompositionContract(contractType, export.ContractName);

                if (metadata.Count == 0)
                    metadata = s_noMetadata;

                yield return new DiscoveredInstanceExport(exportKey, metadata);
            }
        }

        private IEnumerable<DiscoveredExport> DiscoverPropertyExports(TypeInfo partType)
        {
            var partTypeAsType = partType.AsType();
            foreach (var property in partTypeAsType.GetRuntimeProperties()
                .Where(pi => pi.CanRead && pi.GetMethod.IsPublic && !pi.GetMethod.IsStatic))
            {
                foreach (var export in _attributeContext.GetDeclaredAttributes<ExportAttribute>(partTypeAsType, property))
                {
                    IDictionary<string, object> metadata = new Dictionary<string, object>();
                    ReadMetadataAttribute(export, metadata);

                    var applied = _attributeContext.GetDeclaredAttributes(partTypeAsType, property);
                    ReadLooseMetadata(applied, metadata);

                    var contractType = export.ContractType ?? property.PropertyType;
                    CheckPropertyExportCompatibility(partType, property, contractType.GetTypeInfo());

                    var exportKey = new CompositionContract(export.ContractType ?? property.PropertyType, export.ContractName);

                    if (metadata.Count == 0)
                        metadata = s_noMetadata;

                    yield return new DiscoveredPropertyExport(exportKey, metadata, property);
                }
            }
        }

        private void ReadLooseMetadata(object[] appliedAttributes, IDictionary<string, object> metadata)
        {
            foreach (var attribute in appliedAttributes)
            {
                if (attribute is ExportAttribute)
                    continue;

                var ema = attribute as ExportMetadataAttribute;
                if (ema != null)
                {
                    var valueType = ema.Value?.GetType() ?? typeof(object);
                    AddMetadata(metadata, ema.Name, valueType, ema.Value);
                }
                else
                {
                    ReadMetadataAttribute((Attribute)attribute, metadata);
                }
            }
        }

        private void AddMetadata(IDictionary<string, object> metadata, string name, Type valueType, object value)
        {
            object existingValue;
            if (!metadata.TryGetValue(name, out existingValue))
            {
                metadata.Add(name, value);
                return;
            }

            var existingArray = existingValue as Array;
            if (existingArray != null)
            {
                var newArray = Array.CreateInstance(valueType, existingArray.Length + 1);
                Array.Copy(existingArray, newArray, existingArray.Length);
                newArray.SetValue(value, existingArray.Length);
                metadata[name] = newArray;
            }
            else
            {
                var newArray = Array.CreateInstance(valueType, 2);
                newArray.SetValue(existingValue, 0);
                newArray.SetValue(value, 1);
                metadata[name] = newArray;
            }
        }

        private void ReadMetadataAttribute(Attribute attribute, IDictionary<string, object> metadata)
        {
            var attrType = attribute.GetType();

            // Note, we don't support ReflectionContext in this scenario as
            if (attrType.GetTypeInfo().GetCustomAttribute<MetadataAttributeAttribute>(true) == null)
                return;

            foreach (var prop in attrType
                .GetRuntimeProperties()
                .Where(p => p.DeclaringType == attrType && p.CanRead))
            {
                AddMetadata(metadata, prop.Name, prop.PropertyType, prop.GetValue(attribute, null));
            }
        }

        private static void CheckPropertyExportCompatibility(TypeInfo partType, PropertyInfo property, TypeInfo contractType)
        {
            if (partType.IsGenericTypeDefinition)
            {
                CheckGenericContractCompatibility(partType, property.PropertyType.GetTypeInfo(), contractType);
            }
            else if (!contractType.IsAssignableFrom(property.PropertyType.GetTypeInfo()))
            {
                string message = SR.Format(SR.TypeInspector_ExportedContractTypeNotAssignable,
                                                contractType.Name, property.Name, partType.Name);
                throw new CompositionFailedException(message);
            }
        }

        private static void CheckGenericContractCompatibility(TypeInfo partType, TypeInfo exportingMemberType, TypeInfo contractType)
        {
            if (!contractType.IsGenericTypeDefinition)
            {
                string message = SR.Format(SR.TypeInspector_NoExportNonGenericContract, partType.Name, contractType.Name);
                throw new CompositionFailedException(message);
            }

            var compatible = false;

            foreach (var ifce in GetAssignableTypes(exportingMemberType))
            {
                if (ifce == contractType || (ifce.IsGenericType && ifce.GetGenericTypeDefinition() == contractType.AsType()))
                {
                    var mappedType = ifce;
                    if (!(mappedType == partType || mappedType.GenericTypeArguments.SequenceEqual(partType.GenericTypeParameters)))
                    {
                        string message = SR.Format(SR.TypeInspector_ArgumentMissmatch, contractType.Name, partType.Name);
                        throw new CompositionFailedException(message);
                    }

                    compatible = true;
                    break;
                }
            }

            if (!compatible)
            {
                string message = SR.Format(SR.TypeInspector_ExportNotCompatible, exportingMemberType.Name, partType.Name, contractType.Name);
                throw new CompositionFailedException(message);
            }
        }

        private static IEnumerable<TypeInfo> GetAssignableTypes(TypeInfo exportingMemberType)
        {
            foreach (var ifce in exportingMemberType.ImplementedInterfaces)
                yield return ifce.GetTypeInfo();

            var b = exportingMemberType;
            while (b != null)
            {
                yield return b;
                b = b.BaseType?.GetTypeInfo();
            }
        }

        private static void CheckInstanceExportCompatibility(TypeInfo partType, TypeInfo contractType)
        {
            if (partType.IsGenericTypeDefinition)
            {
                CheckGenericContractCompatibility(partType, partType, contractType);
            }
            else if (!contractType.IsAssignableFrom(partType))
            {
                string message = SR.Format(SR.TypeInspector_ContractNotAssignable, contractType.Name, partType.Name);
                throw new CompositionFailedException(message);
            }
        }
    }
}
