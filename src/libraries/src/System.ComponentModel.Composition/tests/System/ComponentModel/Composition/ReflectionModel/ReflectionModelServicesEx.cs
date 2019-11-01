// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq.Expressions;
using System.Reflection;

// NOTE : this is a helper class for exosig the EditorFactory functionality to tests until ExportFactory can be moved where it belongs
namespace System.ComponentModel.Composition.ReflectionModel
{
    public static class ReflectionModelServicesEx
    {
        public static ContractBasedImportDefinition CreateImportDefinition(
            Lazy<ParameterInfo> parameter,
            string contractName,
            string requiredTypeIdentity,
            IEnumerable<KeyValuePair<string, Type>> requiredMetadata,
            ImportCardinality cardinality,
            CreationPolicy requiredCreationPolicy,
            bool isExportFactory,
            ICompositionElement origin)
        {
            return ReflectionModelServicesEx.CreateImportDefinition(parameter, contractName, requiredTypeIdentity, requiredMetadata, cardinality, requiredCreationPolicy, MetadataServices.EmptyMetadata, isExportFactory, origin);
        }

        public static ContractBasedImportDefinition CreateImportDefinition(
            Lazy<ParameterInfo> parameter,
            string contractName,
            string requiredTypeIdentity,
            IEnumerable<KeyValuePair<string, Type>> requiredMetadata,
            ImportCardinality cardinality,
            CreationPolicy requiredCreationPolicy,
            IDictionary<string, object> metadata,
            bool isExportFactory,
            ICompositionElement origin)
        {
            return ReflectionModelServices.CreateImportDefinition(parameter, contractName, requiredTypeIdentity, requiredMetadata, cardinality, requiredCreationPolicy, metadata, isExportFactory, origin);
        }

        public static ContractBasedImportDefinition CreateImportDefinition(
                        LazyMemberInfo importingMember,
                        string contractName,
                        string requiredTypeIdentity,
                        IEnumerable<KeyValuePair<string, Type>> requiredMetadata,
                        ImportCardinality cardinality,
                        bool isRecomposable,
                        CreationPolicy requiredCreationPolicy,
                        bool isExportFactory,
                        ICompositionElement origin)
        {
            return ReflectionModelServicesEx.CreateImportDefinition(importingMember, contractName, requiredTypeIdentity, requiredMetadata, cardinality, isRecomposable, requiredCreationPolicy, MetadataServices.EmptyMetadata, isExportFactory, origin);
        }

        public static ContractBasedImportDefinition CreateImportDefinition(
                LazyMemberInfo importingMember,
                string contractName,
                string requiredTypeIdentity,
                IEnumerable<KeyValuePair<string, Type>> requiredMetadata,
                ImportCardinality cardinality,
                bool isRecomposable,
                CreationPolicy requiredCreationPolicy,
                IDictionary<string, object> metadata,
                bool isExportFactory,
                ICompositionElement origin)
        {
            return ReflectionModelServices.CreateImportDefinition(importingMember, contractName, requiredTypeIdentity, requiredMetadata, cardinality, isRecomposable, requiredCreationPolicy, metadata, isExportFactory, origin);
        }

        public static bool IsExportFactoryImportDefinition(ImportDefinition importDefinition)
        {
            return ReflectionModelServices.IsExportFactoryImportDefinition(importDefinition);
        }

        public static ContractBasedImportDefinition CreateExportFactoryImportDefinition(ContractBasedImportDefinition productImportDefinition)
        {
            return new ExportFactoryImportDefinition(productImportDefinition);
        }

        private class ExportFactoryImportDefinition : ContractBasedImportDefinition, IPartCreatorImportDefinition
        {
            private readonly ContractBasedImportDefinition _productImportDefinition;

            public ExportFactoryImportDefinition(ContractBasedImportDefinition productImportDefinition)
                : base(CompositionConstants.PartCreatorContractName, CompositionConstants.PartCreatorTypeIdentity, productImportDefinition.RequiredMetadata,
                    productImportDefinition.Cardinality, productImportDefinition.IsRecomposable, false, CreationPolicy.Any)
            {
                _productImportDefinition = productImportDefinition;
            }

            public ContractBasedImportDefinition ProductImportDefinition
            {
                get
                {
                    return _productImportDefinition;
                }
            }

            public override Expression<Func<ExportDefinition, bool>> Constraint
            {
                get
                {
                    return CreateExportFactoryConstraint(base.Constraint, this._productImportDefinition);
                }
            }

            public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
            {
                if (!base.IsConstraintSatisfiedBy(exportDefinition))
                {
                    return false;
                }

                return IsProductConstraintSatisfiedBy(this._productImportDefinition, exportDefinition);
            }

            private static bool IsProductConstraintSatisfiedBy(ImportDefinition productImportDefinition, ExportDefinition exportDefinition)
            {
                object productValue = null;
                if (exportDefinition.Metadata.TryGetValue(CompositionConstants.ProductDefinitionMetadataName, out productValue))
                {
                    ExportDefinition productDefinition = productValue as ExportDefinition;

                    if (productDefinition != null)
                    {
                        return productImportDefinition.IsConstraintSatisfiedBy(productDefinition);
                    }
                }

                return false;
            }

            private static readonly PropertyInfo _exportDefinitionMetadataProperty = typeof(ExportDefinition).GetProperty("Metadata");
            private static readonly MethodInfo _metadataContainsKeyMethod = typeof(IDictionary<string, object>).GetMethod("ContainsKey");
            private static readonly MethodInfo _metadataItemMethod = typeof(IDictionary<string, object>).GetMethod("get_Item");

            private static Expression<Func<ExportDefinition, bool>> CreateExportFactoryConstraint(Expression<Func<ExportDefinition, bool>> baseConstraint, ImportDefinition productImportDefinition)
            {
                ParameterExpression exportDefinitionParameter = baseConstraint.Parameters[0];

                // exportDefinition.Metadata
                Expression metadataExpression = Expression.Property(exportDefinitionParameter, _exportDefinitionMetadataProperty);

                // exportDefinition.Metadata.ContainsKey("ProductDefinition")
                Expression containsProductExpression = Expression.Call(
                    metadataExpression,
                    _metadataContainsKeyMethod,
                    Expression.Constant(CompositionConstants.ProductDefinitionMetadataName));

                // exportDefinition.Metadata["ProductDefinition"]
                Expression productExportDefinitionExpression = Expression.Call(
                        metadataExpression,
                        _metadataItemMethod,
                        Expression.Constant(CompositionConstants.ProductDefinitionMetadataName));

                // ProductImportDefinition.Contraint((ExportDefinition)exportDefinition.Metadata["ProductDefinition"])
                Expression productMatchExpression =
                    Expression.Invoke(productImportDefinition.Constraint,
                        Expression.Convert(productExportDefinitionExpression, typeof(ExportDefinition)));

                // baseContraint(exportDefinition) &&
                // exportDefinition.Metadata.ContainsKey("ProductDefinition") &&
                // ProductImportDefinition.Contraint((ExportDefinition)exportDefinition.Metadata["ProductDefinition"])
                Expression<Func<ExportDefinition, bool>> constraint =
                     Expression.Lambda<Func<ExportDefinition, bool>>(
                        Expression.AndAlso(
                            baseConstraint.Body,
                            Expression.AndAlso(
                               containsProductExpression,
                               productMatchExpression)),
                        exportDefinitionParameter);

                return constraint;
            }
        }

    }
}
