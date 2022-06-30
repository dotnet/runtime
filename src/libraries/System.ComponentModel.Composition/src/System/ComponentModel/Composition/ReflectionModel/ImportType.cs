// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using Microsoft.Internal.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Composition.ReflectionModel
{
    // Describes the import type of a Reflection-based import definition
    internal sealed class ImportType
    {
        private static readonly Type LazyOfTType = typeof(Lazy<>);
        private static readonly Type LazyOfTMType = typeof(Lazy<,>);
        private static readonly Type ExportFactoryOfTType = typeof(ExportFactory<>);

        private readonly Type _type;
        private readonly bool _isAssignableCollectionType;
        private Type _contractType;
        private Func<Export, object>? _castSingleValue;
        private readonly bool _isOpenGeneric;

        [ThreadStatic]
        internal static Dictionary<Type, Func<Export, object>?>? _castSingleValueCache;

        private static Dictionary<Type, Func<Export, object>?> CastSingleValueCache
        {
            get
            {
                return _castSingleValueCache ??= new Dictionary<Type, Func<Export, object>?>();
            }
        }

        public ImportType(Type type, ImportCardinality cardinality)
        {
            ArgumentNullException.ThrowIfNull(type);

            _type = type;
            Type contractType = type;

            if (cardinality == ImportCardinality.ZeroOrMore)
            {
                _isAssignableCollectionType = IsTypeAssignableCollectionType(type);
                contractType = CheckForCollection(type);
            }

            // This sets contract type, metadata and the cast function
            _isOpenGeneric = type.ContainsGenericParameters;
            Initialize(contractType);
        }

        public bool IsAssignableCollectionType
        {
            get { return _isAssignableCollectionType; }
        }

        public Type? ElementType { get; private set; }

        public Type ActualType
        {
            get { return _type; }
        }

        public bool IsPartCreator { get; private set; }

        public Type ContractType { get { return _contractType; } }

        public Func<Export, object>? CastExport
        {
            get
            {
                if (_isOpenGeneric)
                {
                    throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                }
                return _castSingleValue;
            }
        }

        public Type? MetadataViewType { get; private set; }

        private Type CheckForCollection(Type type)
        {
            ElementType = CollectionServices.GetEnumerableElementType(type);
            if (ElementType != null)
            {
                return ElementType;
            }
            return type;
        }

        private static bool IsGenericDescendentOf(Type? type, Type baseGenericTypeDefinition)
        {
            if (type == typeof(object) || type == null)
            {
                return false;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == baseGenericTypeDefinition)
            {
                return true;
            }

            return IsGenericDescendentOf(type.BaseType, baseGenericTypeDefinition);
        }

        public static bool IsDescendentOf(Type type, Type baseType)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(baseType);

            if (!baseType.IsGenericTypeDefinition)
            {
                return baseType.IsAssignableFrom(type);
            }

            return IsGenericDescendentOf(type, baseType.GetGenericTypeDefinition());
        }

        [MemberNotNull(nameof(_contractType))]
        private void Initialize(Type type)
        {
            if (!type.IsGenericType)
            {
                // no cast function, the original type is the contract type
                _contractType = type;
                return;
            }

            Type[] arguments = type.GetGenericArguments();
            Type genericType = type.GetGenericTypeDefinition().UnderlyingSystemType;

            // Look up the cast function
            if (!CastSingleValueCache.TryGetValue(type, out _castSingleValue))
            {
                if (!TryGetCastFunction(genericType, _isOpenGeneric, arguments, out _castSingleValue))
                {
                    // in this case, even though the type is generic, it's nothing we have recognized,
                    // thereforeit's the same as the non-generic case
                    _contractType = type;
                    return;
                }

                CastSingleValueCache.Add(type, _castSingleValue);
            }

            // we have found the cast function, which means, that we have found either Lazy of EF
            // in this case the contract is always argument[0] and the metadata view is always argument[1]
            IsPartCreator = !IsLazyGenericType(genericType) && (genericType != null);
            _contractType = arguments[0];
            if (arguments.Length == 2)
            {
                MetadataViewType = arguments[1];
            }
        }

        private static bool IsLazyGenericType(Type genericType)
        {
            return (genericType == LazyOfTType) || (genericType == LazyOfTMType);
        }

        private static bool TryGetCastFunction(Type genericType, bool isOpenGeneric, Type[] arguments, out Func<Export, object>? castFunction)
        {
            castFunction = null;

            if (genericType == LazyOfTType)
            {
                if (!isOpenGeneric)
                {
                    castFunction = ExportServices.CreateStronglyTypedLazyFactory(arguments[0].UnderlyingSystemType, null);
                }
                return true;
            }

            if (genericType == LazyOfTMType)
            {
                if (!isOpenGeneric)
                {
                    castFunction = ExportServices.CreateStronglyTypedLazyFactory(arguments[0].UnderlyingSystemType, arguments[1].UnderlyingSystemType);
                }
                return true;
            }

            if (genericType != null && IsDescendentOf(genericType, ExportFactoryOfTType))
            {
                if (arguments.Length == 1)
                {
                    if (!isOpenGeneric)
                    {
                        castFunction = new ExportFactoryCreator(genericType).CreateStronglyTypedExportFactoryFactory(arguments[0].UnderlyingSystemType, null);
                    }
                    return true;
                }
                else if (arguments.Length == 2)
                {
                    if (!isOpenGeneric)
                    {
                        castFunction = new ExportFactoryCreator(genericType).CreateStronglyTypedExportFactoryFactory(arguments[0].UnderlyingSystemType, arguments[1].UnderlyingSystemType);
                    }
                    return true;
                }
                else
                {
                    throw ExceptionBuilder.ExportFactory_TooManyGenericParameters(genericType.FullName!);
                }
            }

            return false;
        }

        private static bool IsTypeAssignableCollectionType(Type type)
        {
            if (type.IsArray || CollectionServices.IsEnumerableOfT(type))
            {
                return true;
            }

            return false;
        }
    }
}
