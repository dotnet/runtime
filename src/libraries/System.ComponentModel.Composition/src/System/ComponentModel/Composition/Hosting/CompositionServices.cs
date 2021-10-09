// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    internal static class CompositionServices
    {
        internal static readonly Type InheritedExportAttributeType = typeof(InheritedExportAttribute);
        internal static readonly Type ExportAttributeType = typeof(ExportAttribute);
        internal static readonly Type AttributeType = typeof(Attribute);
        internal static readonly Type ObjectType = typeof(object);

        private static readonly string[] reservedMetadataNames = new string[]
        {
            CompositionConstants.PartCreationPolicyMetadataName
        };

        internal static Type GetDefaultTypeFromMember(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;

                case MemberTypes.NestedType:
                case MemberTypes.TypeInfo:
                    return ((Type)member);

                case MemberTypes.Field:
                default:
                    if (member.MemberType != MemberTypes.Field)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                    return ((FieldInfo)member).FieldType;
            }
        }

        internal static Type AdjustSpecifiedTypeIdentityType(this Type specifiedContractType, MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Method)
            {
                return specifiedContractType;
            }
            else
            {
                return specifiedContractType.AdjustSpecifiedTypeIdentityType(member.GetDefaultTypeFromMember());
            }
        }

        internal static Type AdjustSpecifiedTypeIdentityType(this Type specifiedContractType, Type? memberType)
        {
            if (specifiedContractType == null)
            {
                throw new ArgumentNullException(nameof(specifiedContractType));
            }

            if ((memberType != null) && memberType.IsGenericType && specifiedContractType.IsGenericType)
            {
                // if the memeber type is closed and the specified contract type is open and they have exatly the same number of parameters
                // we will close the specfied contract type
                if (specifiedContractType.ContainsGenericParameters && !memberType.ContainsGenericParameters)
                {
                    var typeGenericArguments = memberType.GetGenericArguments();
                    var metadataTypeGenericArguments = specifiedContractType.GetGenericArguments();

                    if (typeGenericArguments.Length == metadataTypeGenericArguments.Length)
                    {
                        return specifiedContractType.MakeGenericType(typeGenericArguments);
                    }
                }
                // if both member type and the contract type are open generic types, make sure that their parameters are ordered the same way
                else if (specifiedContractType.ContainsGenericParameters && memberType.ContainsGenericParameters)
                {
                    var memberGenericParameters = memberType.GetPureGenericParameters();
                    if (specifiedContractType.GetPureGenericArity() == memberGenericParameters.Count)
                    {
                        return specifiedContractType.GetGenericTypeDefinition().MakeGenericType(memberGenericParameters.ToArray());
                    }
                }
            }

            return specifiedContractType;
        }

        private static string AdjustTypeIdentity(string originalTypeIdentity, Type typeIdentityType)
        {
            return GenericServices.GetGenericName(originalTypeIdentity, GenericServices.GetGenericParametersOrder(typeIdentityType), GenericServices.GetPureGenericArity(typeIdentityType));
        }

        internal static void GetContractInfoFromExport(this MemberInfo member, ExportAttribute export, out Type? typeIdentityType, out string contractName)
        {
            typeIdentityType = member.GetTypeIdentityTypeFromExport(export);
            if (!string.IsNullOrEmpty(export.ContractName))
            {
                contractName = export.ContractName;
            }
            else
            {
                contractName = member.GetTypeIdentityFromExport(typeIdentityType);
            }
        }

        internal static string GetTypeIdentityFromExport(this MemberInfo member, Type? typeIdentityType)
        {
            if (typeIdentityType != null)
            {
                string typeIdentity = AttributedModelServices.GetTypeIdentity(typeIdentityType);
                if (typeIdentityType.ContainsGenericParameters)
                {
                    typeIdentity = AdjustTypeIdentity(typeIdentity, typeIdentityType);
                }
                return typeIdentity;
            }
            else
            {
                MethodInfo? method = member as MethodInfo;
                if (method == null)
                {
                    throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                }
                return AttributedModelServices.GetTypeIdentity(method);
            }
        }

        private static Type? GetTypeIdentityTypeFromExport(this MemberInfo member, ExportAttribute export)
        {
            if (export.ContractType != null)
            {
                return export.ContractType.AdjustSpecifiedTypeIdentityType(member);
            }
            else
            {
                return (member.MemberType != MemberTypes.Method) ? member.GetDefaultTypeFromMember() : null;
            }
        }

        internal static bool IsContractNameSameAsTypeIdentity(this ExportAttribute export)
        {
            return string.IsNullOrEmpty(export.ContractName);
        }

        internal static Type GetContractTypeFromImport(this IAttributedImport import, ImportType importType)
        {
            if (import.ContractType != null)
            {
                return import.ContractType.AdjustSpecifiedTypeIdentityType(importType.ContractType);
            }

            return importType.ContractType;
        }

        internal static string GetContractNameFromImport(this IAttributedImport import, ImportType importType)
        {
            if (!string.IsNullOrEmpty(import.ContractName))
            {
                return import.ContractName;
            }

            Type contractType = import.GetContractTypeFromImport(importType);

            return AttributedModelServices.GetContractName(contractType);
        }

        internal static string? GetTypeIdentityFromImport(this IAttributedImport import, ImportType importType)
        {
            Type contractType = import.GetContractTypeFromImport(importType);

            // For our importers we treat object as not having a type identity
            if (contractType == CompositionServices.ObjectType)
            {
                return null;
            }

            return AttributedModelServices.GetTypeIdentity(contractType);
        }

        internal static IDictionary<string, object?> GetPartMetadataForType(this Type type, CreationPolicy creationPolicy)
        {
            IDictionary<string, object?> dictionary = new Dictionary<string, object?>(StringComparers.MetadataKeyNames);

            if (creationPolicy != CreationPolicy.Any)
            {
                dictionary.Add(CompositionConstants.PartCreationPolicyMetadataName, creationPolicy);
            }

            foreach (PartMetadataAttribute partMetadata in type.GetAttributes<PartMetadataAttribute>())
            {
                if (reservedMetadataNames.Contains(partMetadata.Name, StringComparers.MetadataKeyNames)
                    || dictionary.ContainsKey(partMetadata.Name))
                {
                    // Perhaps we should log an error here so that people know this value is being ignored.
                    continue;
                }

                dictionary.Add(partMetadata.Name, partMetadata.Value);
            }

            // metadata for generic types
            if (type.ContainsGenericParameters)
            {
                // Register the part as generic
                dictionary.Add(CompositionConstants.IsGenericPartMetadataName, true);

                // Add arity
                Type[] genericArguments = type.GetGenericArguments();
                dictionary.Add(CompositionConstants.GenericPartArityMetadataName, genericArguments.Length);

                // add constraints
                bool hasConstraints = false;
                object?[] genericParameterConstraints = new object?[genericArguments.Length];
                GenericParameterAttributes[] genericParameterAttributes = new GenericParameterAttributes[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Type genericArgument = genericArguments[i];

                    Type[]? constraints = genericArgument.GetGenericParameterConstraints();
                    if (constraints.Length == 0)
                    {
                        constraints = null;
                    }

                    GenericParameterAttributes attributes = genericArgument.GenericParameterAttributes;

                    if ((constraints != null) || (attributes != GenericParameterAttributes.None))
                    {
                        genericParameterConstraints[i] = constraints;
                        genericParameterAttributes[i] = attributes;
                        hasConstraints = true;
                    }
                }

                if (hasConstraints)
                {
                    dictionary.Add(CompositionConstants.GenericParameterConstraintsMetadataName, genericParameterConstraints);
                    dictionary.Add(CompositionConstants.GenericParameterAttributesMetadataName, genericParameterAttributes);
                }
            }

            if (dictionary.Count == 0)
            {
                return MetadataServices.EmptyMetadata;
            }
            else
            {
                return dictionary;
            }
        }

        internal static void TryExportMetadataForMember(this MemberInfo member, out IDictionary<string, object?> dictionary)
        {
            dictionary = new Dictionary<string, object?>();

            foreach (var attr in member.GetAttributes<Attribute>())
            {
                var provider = attr as ExportMetadataAttribute;

                if (provider != null)
                {
                    if (reservedMetadataNames.Contains(provider.Name, StringComparers.MetadataKeyNames))
                    {
                        throw ExceptionBuilder.CreateDiscoveryException(SR.Discovery_ReservedMetadataNameUsed, member.GetDisplayName(), provider.Name);
                    }

                    // we pass "null" for valueType which would make it inferred. We don;t have additional type information when metadata
                    // goes through the ExportMetadataAttribute path
                    if (!dictionary.TryContributeMetadataValue(provider.Name, provider.Value, null, provider.IsMultiple))
                    {
                        throw ExceptionBuilder.CreateDiscoveryException(SR.Discovery_DuplicateMetadataNameValues, member.GetDisplayName(), provider.Name);
                    }
                }
                else
                {
                    Type attrType = attr.GetType();
                    // Perf optimization, relies on short circuit evaluation, often a property attribute is an ExportAttribute
                    if ((attrType != CompositionServices.ExportAttributeType) && attrType.IsAttributeDefined<MetadataAttributeAttribute>(true))
                    {
                        bool allowsMultiple = false;
                        AttributeUsageAttribute? usage = attrType.GetFirstAttribute<AttributeUsageAttribute>(true);

                        if (usage != null)
                        {
                            allowsMultiple = usage.AllowMultiple;
                        }

                        foreach (PropertyInfo pi in attrType.GetProperties())
                        {
                            if (pi.DeclaringType == CompositionServices.ExportAttributeType || pi.DeclaringType == CompositionServices.AttributeType)
                            {
                                // Don't contribute metadata properies from the base attribute types.
                                continue;
                            }

                            if (reservedMetadataNames.Contains(pi.Name, StringComparers.MetadataKeyNames))
                            {
                                throw ExceptionBuilder.CreateDiscoveryException(SR.Discovery_ReservedMetadataNameUsed, pi.GetDisplayName(), pi.Name);
                            }

                            object? value = pi.GetValue(attr, null);

                            if (value != null && !IsValidAttributeType(value.GetType()))
                            {
                                throw ExceptionBuilder.CreateDiscoveryException(SR.Discovery_MetadataContainsValueWithInvalidType, pi.GetDisplayName(), value.GetType().GetDisplayName());
                            }

                            if (!dictionary.TryContributeMetadataValue(pi.Name, value, pi.PropertyType, allowsMultiple))
                            {
                                throw ExceptionBuilder.CreateDiscoveryException(SR.Discovery_DuplicateMetadataNameValues, member.GetDisplayName(), pi.Name);
                            }
                        }
                    }
                }
            }

            // Need Keys.ToArray because we alter the dictionary in the loop
            foreach (var key in dictionary.Keys.ToArray())
            {
                var list = dictionary[key] as MetadataList;
                if (list != null)
                {
                    dictionary[key] = list.ToArray();
                }
            }

            return;
        }

        private static bool TryContributeMetadataValue(this IDictionary<string, object?> dictionary, string name, object? value, Type? valueType, bool allowsMultiple)
        {
            if (!dictionary.TryGetValue(name, out object? metadataValue))
            {
                if (allowsMultiple)
                {
                    var list = new MetadataList();
                    list.Add(value, valueType);
                    value = list;
                }

                dictionary.Add(name, value);
            }
            else
            {
                var list = metadataValue as MetadataList;
                if (!allowsMultiple || list == null)
                {
                    // Either single value already found when should be multiple
                    // or a duplicate name already exists
                    dictionary.Remove(name);
                    return false;
                }

                list.Add(value, valueType);
            }
            return true;
        }

        private sealed class MetadataList
        {
            private Type? _arrayType;
            private bool _containsNulls;
            private static readonly Type ObjectType = typeof(object);
            private static readonly Type TypeType = typeof(Type);
            private readonly Collection<object?> _innerList = new Collection<object?>();

            public void Add(object? item, Type? itemType)
            {
                _containsNulls |= (item == null);

                // if we've been passed typeof(object), we basically have no type inmformation
                if (itemType == ObjectType)
                {
                    itemType = null;
                }

                // if we have no type information, get it from the item, if we can
                if ((itemType == null) && (item != null))
                {
                    itemType = item.GetType();
                }

                // Types are special, because the are abstract classes, so if the item casts to Type, we assume System.Type
                if (item is Type)
                {
                    itemType = TypeType;
                }

                // only try to call this if we got a meaningful type
                if (itemType != null)
                {
                    InferArrayType(itemType);
                }

                _innerList.Add(item);
            }

            private void InferArrayType(Type itemType)
            {
                if (itemType == null)
                {
                    throw new ArgumentNullException(nameof(itemType));
                }

                if (_arrayType == null)
                {
                    // this is the first typed element we've been given, it sets the type of the array
                    _arrayType = itemType;
                }
                else
                {
                    // if there's a disagreement on the array type, we flip to Object
                    // NOTE : we can try to do better in the future to find common base class, but given that we support very limited set of types
                    // in metadata right now, it's a moot point
                    if (_arrayType != itemType)
                    {
                        _arrayType = ObjectType;
                    }
                }
            }

            public Array ToArray()
            {
                if (_arrayType == null)
                {
                    // if the array type has not been set, assume Object
                    _arrayType = ObjectType;
                }
                else if (_containsNulls && _arrayType.IsValueType)
                {
                    // if the array type is a value type and we have seen nulls, then assume Object
                    _arrayType = ObjectType;
                }

                Array array = Array.CreateInstance(_arrayType, _innerList.Count);

                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(_innerList[i], i);
                }
                return array;
            }
        }

        //UNDONE: Need to add these warnings somewhere...Dev10:472538 should address
        //internal static CompositionResult MatchRequiredMetadata(this IDictionary<string, object> metadata, IEnumerable<string> requiredMetadata, string contractName)
        //{
        //    Assumes.IsTrue(metadata != null);

        //    var result = CompositionResult.SucceededResult;

        //    var missingMetadata = (requiredMetadata == null) ? null : requiredMetadata.Except<string>(metadata.Keys);
        //    if (missingMetadata != null && missingMetadata.Any())
        //    {
        //        result = result.MergeIssue(
        //            CompositionError.CreateIssueAsWarning(CompositionErrorId.RequiredMetadataNotFound,
        //            SR.RequiredMetadataNotFound,
        //            contractName,
        //            string.Join(", ", missingMetadata.ToArray())));

        //        return new CompositionResult(false, result.Issues);
        //    }

        //    return result;
        //}

        internal static IEnumerable<KeyValuePair<string, Type>> GetRequiredMetadata(Type? metadataViewType)
        {
            if ((metadataViewType == null) ||
                ExportServices.IsDefaultMetadataViewType(metadataViewType) ||
                ExportServices.IsDictionaryConstructorViewType(metadataViewType) ||
                !metadataViewType.IsInterface)
            {
                return Enumerable.Empty<KeyValuePair<string, Type>>();
            }

            // A metadata view is required to be an Intrerface, and therefore only properties are allowed
            List<PropertyInfo> properties = metadataViewType.GetAllProperties().
                Where(property => property.GetFirstAttribute<DefaultValueAttribute>() == null).
                ToList();

            // NOTE : this is a carefully found balance between eager and delay-evaluation - the properties are filtered once and upfront
            // whereas the key/Type pairs are created every time. The latter is fine as KVPs are structs and as such copied on access regardless.
            // This also allows us to avoid creation of List<KVP> which - at least according to FxCop - leads to isues with NGEN
            return properties.Select(property => new KeyValuePair<string, Type>(property.Name, property.PropertyType));
        }

        internal static IDictionary<string, object?> GetImportMetadata(ImportType importType, IAttributedImport attributedImport)
        {
            return GetImportMetadata(importType.ContractType, attributedImport);
        }

        internal static IDictionary<string, object?> GetImportMetadata(Type type, IAttributedImport? attributedImport)
        {
            Dictionary<string, object?>? metadata = null;

            //Prior to V4.5 MEF did not support ImportMetadata
            if (type.IsGenericType)
            {
                metadata = new Dictionary<string, object?>();

                if (type.ContainsGenericParameters)
                {
                    metadata[CompositionConstants.GenericImportParametersOrderMetadataName] = GenericServices.GetGenericParametersOrder(type);
                }
                else
                {
                    metadata[CompositionConstants.GenericContractMetadataName] = ContractNameServices.GetTypeIdentity(type.GetGenericTypeDefinition());
                    metadata[CompositionConstants.GenericParametersMetadataName] = type.GetGenericArguments();
                }
            }

            // Default value is ImportSource.Any
            if (attributedImport != null && attributedImport.Source != ImportSource.Any)
            {
                if (metadata == null)
                {
                    metadata = new Dictionary<string, object?>();
                }
                metadata[CompositionConstants.ImportSourceMetadataName] = attributedImport.Source;
            }

            if (metadata != null)
            {
                return metadata.AsReadOnly();
            }
            else
            {
                return MetadataServices.EmptyMetadata;
            }
        }

        internal static object? GetExportedValueFromComposedPart(ImportEngine? engine, ComposablePart part, ExportDefinition definition)
        {
            if (engine != null)
            {
                try
                {
                    engine.SatisfyImports(part);
                }
                catch (CompositionException ex)
                {
                    throw ExceptionBuilder.CreateCannotGetExportedValue(part, definition, ex);
                }
            }

            try
            {
                return part.GetExportedValue(definition);
            }
            catch (ComposablePartException ex)
            {
                throw ExceptionBuilder.CreateCannotGetExportedValue(part, definition, ex);
            }
        }

        internal static bool IsRecomposable(this ComposablePart part)
        {
            return part.ImportDefinitions.Any(import => import.IsRecomposable);
        }

        internal static CompositionResult TryInvoke(Action action)
        {
            try
            {
                action();
                return CompositionResult.SucceededResult;
            }
            catch (CompositionException ex)
            {
                return new CompositionResult(ex.Errors);
            }
        }

        internal static CompositionResult TryFire<TEventArgs>(EventHandler<TEventArgs> _delegate, object? sender, TEventArgs e)
            where TEventArgs : EventArgs
        {
            CompositionResult result = CompositionResult.SucceededResult;
            foreach (EventHandler<TEventArgs> _subscriber in _delegate.GetInvocationList())
            {
                try
                {
                    _subscriber.Invoke(sender, e);
                }
                catch (CompositionException ex)
                {
                    result = result.MergeErrors(ex.Errors);
                }
            }

            return result;
        }

        internal static CreationPolicy GetRequiredCreationPolicy(this ImportDefinition definition)
        {
            if (definition is ContractBasedImportDefinition contractDefinition)
            {
                return contractDefinition.RequiredCreationPolicy;
            }

            return CreationPolicy.Any;
        }

        /// <summary>
        ///     Returns a value indicating whether cardinality is
        ///     <see cref="ImportCardinality.ZeroOrOne"/> or
        ///     <see cref="ImportCardinality.ExactlyOne"/>.
        /// </summary>
        internal static bool IsAtMostOne(this ImportCardinality cardinality)
        {
            return cardinality == ImportCardinality.ZeroOrOne || cardinality == ImportCardinality.ExactlyOne;
        }

        private static bool IsValidAttributeType(Type type)
        {
            return IsValidAttributeType(type, true);
        }

        private static bool IsValidAttributeType(Type type, bool arrayAllowed)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // Definitions of valid attribute type taken from C# 3.0 Specification section 17.1.3.

            // One of the following types: bool, byte, char, double, float, int, long, sbyte, short, string, uint, ulong, ushort.
            if (type.IsPrimitive)
            {
                return true;
            }

            if (type == typeof(string))
            {
                return true;
            }

            // An enum type, provided it has public accessibility and the types in which it is nested (if any) also have public accessibility
            if (type.IsEnum && type.IsVisible)
            {
                return true;
            }

            if (typeof(Type).IsAssignableFrom(type))
            {
                return true;
            }

            // Single-dimensional arrays of the above types.
            if (arrayAllowed && type.IsArray &&
                type.GetArrayRank() == 1 &&
                IsValidAttributeType(type.GetElementType()!, false))
            {
                return true;
            }

            return false;
        }
    }
}
