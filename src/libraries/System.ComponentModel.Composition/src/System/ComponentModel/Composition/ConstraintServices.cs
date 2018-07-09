// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq.Expressions;
using System.Reflection;

namespace System.ComponentModel.Composition
{
    internal static class ConstraintServices
    {
        // NOTE : these are here as Reflection member search is pretty expensive, and we want that to be done once.
        // Also, making these static would cause this class to fail loading if we rename members of ExportDefinition.
        private static readonly PropertyInfo _exportDefinitionContractNameProperty = typeof(ExportDefinition).GetProperty("ContractName");
        private static readonly PropertyInfo _exportDefinitionMetadataProperty = typeof(ExportDefinition).GetProperty("Metadata");
        private static readonly MethodInfo _metadataContainsKeyMethod = typeof(IDictionary<string, object>).GetMethod("ContainsKey");
        private static readonly MethodInfo _metadataItemMethod = typeof(IDictionary<string, object>).GetMethod("get_Item");
        private static readonly MethodInfo _metadataEqualsMethod = typeof(object).GetMethod("Equals", new Type[] { typeof(object) });
        private static readonly MethodInfo _typeIsInstanceOfTypeMethod = typeof(Type).GetMethod("IsInstanceOfType");

        public static Expression<Func<ExportDefinition, bool>> CreateConstraint(string contractName, string requiredTypeIdentity, IEnumerable<KeyValuePair<string, Type>> requiredMetadata, CreationPolicy requiredCreationPolicy)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(ExportDefinition), "exportDefinition");

            Expression constraintBody = ConstraintServices.CreateContractConstraintBody(contractName, parameter);

            if (!string.IsNullOrEmpty(requiredTypeIdentity))
            {
                Expression typeIdentityConstraintBody = ConstraintServices.CreateTypeIdentityContraint(requiredTypeIdentity, parameter);

                constraintBody = Expression.AndAlso(constraintBody, typeIdentityConstraintBody);
            }

            if (requiredMetadata != null)
            {
                Expression metadataConstraintBody = ConstraintServices.CreateMetadataConstraintBody(requiredMetadata, parameter);
                if (metadataConstraintBody != null)
                {
                    constraintBody = Expression.AndAlso(constraintBody, metadataConstraintBody);
                }
            }

            if (requiredCreationPolicy != CreationPolicy.Any)
            {
                Expression policyConstraintBody = ConstraintServices.CreateCreationPolicyContraint(requiredCreationPolicy, parameter);

                constraintBody = Expression.AndAlso(constraintBody, policyConstraintBody);
            }

            Expression<Func<ExportDefinition, bool>> constraint = Expression.Lambda<Func<ExportDefinition, bool>>(constraintBody, parameter);
            return constraint;
        }

        private static Expression CreateContractConstraintBody(string contractName, ParameterExpression parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            // export.ContractName=<contract>;
            return Expression.Equal(
                    Expression.Property(parameter, ConstraintServices._exportDefinitionContractNameProperty),
                    Expression.Constant(contractName ?? string.Empty, typeof(string)));
        }

        private static Expression CreateMetadataConstraintBody(IEnumerable<KeyValuePair<string, Type>> requiredMetadata, ParameterExpression parameter)
        {
            if (requiredMetadata == null)
            {
                throw new ArgumentNullException(nameof(requiredMetadata));
            }

            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            Expression body = null;
            foreach (KeyValuePair<string, Type> requiredMetadataItem in requiredMetadata)
            {
                // export.Metadata.ContainsKey(<metadataItem>)
                Expression metadataItemExpression = CreateMetadataContainsKeyExpression(parameter, requiredMetadataItem.Key);

                body = (body != null) ? Expression.AndAlso(body, metadataItemExpression) : metadataItemExpression;
                body = Expression.AndAlso(body, CreateMetadataOfTypeExpression(parameter, requiredMetadataItem.Key, requiredMetadataItem.Value));
            }

            return body;
        }

        private static Expression CreateCreationPolicyContraint(CreationPolicy policy, ParameterExpression parameter)
        {
            if(policy == CreationPolicy.Any)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            if(parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            //    !definition.Metadata.ContainsKey(CompositionConstants.PartCreationPolicyMetadataName) ||
            //        CreationPolicy.Any.Equals(definition.Metadata[CompositionConstants.PartCreationPolicyMetadataName]) ||
            //        policy.Equals(definition.Metadata[CompositionConstants.PartCreationPolicyMetadataName]);

            return  Expression.MakeBinary(ExpressionType.OrElse,
                        Expression.MakeBinary(ExpressionType.OrElse,
                            Expression.Not(CreateMetadataContainsKeyExpression(parameter, CompositionConstants.PartCreationPolicyMetadataName)),
                            CreateMetadataValueEqualsExpression(parameter, CreationPolicy.Any, CompositionConstants.PartCreationPolicyMetadataName)),
                        CreateMetadataValueEqualsExpression(parameter, policy, CompositionConstants.PartCreationPolicyMetadataName));
        }

        private static Expression CreateTypeIdentityContraint(string requiredTypeIdentity, ParameterExpression parameter)
        {
            if(requiredTypeIdentity == null)
            {
                throw new ArgumentNullException(requiredTypeIdentity);
            }

            if(parameter == null)
            {
                throw new ArgumentException(nameof(parameter));
            }

            //    definition.Metadata.ContainsKey(CompositionServices.ExportTypeIdentity) &&
            //        requiredTypeIdentity.Equals(definition.Metadata[CompositionConstants.ExportTypeIdentityMetadataName]);

            return  Expression.MakeBinary(ExpressionType.AndAlso,
                        CreateMetadataContainsKeyExpression(parameter, CompositionConstants.ExportTypeIdentityMetadataName),
                        CreateMetadataValueEqualsExpression(parameter, requiredTypeIdentity, CompositionConstants.ExportTypeIdentityMetadataName));
        }

        private static Expression CreateMetadataContainsKeyExpression(ParameterExpression parameter, string constantKey)
        {
            if (parameter == null)
            {
                throw new ArgumentException(nameof(parameter));
            }

            if (constantKey == null)
            {
                throw new ArgumentException(nameof(constantKey));
            }

            // definition.Metadata.ContainsKey(constantKey)
            return  Expression.Call(
                        Expression.Property(parameter, ConstraintServices._exportDefinitionMetadataProperty),
                        ConstraintServices._metadataContainsKeyMethod,
                        Expression.Constant(constantKey));
        }

        private static Expression CreateMetadataOfTypeExpression(ParameterExpression parameter, string constantKey, Type constantType)
        {
            if(parameter == null)
            {
                throw new ArgumentException(nameof(parameter));
            }

            if (constantKey == null)
            {
                throw new ArgumentException(nameof(constantKey));
            }

            if (constantType == null)
            {
                throw new ArgumentException(nameof(constantType));
            }

            // constantType.IsInstanceOfType(definition.Metadata[constantKey])
            return Expression.Call(
                            Expression.Constant(constantType, typeof(Type)),
                            ConstraintServices._typeIsInstanceOfTypeMethod,
                            Expression.Call(
                                Expression.Property(parameter, ConstraintServices._exportDefinitionMetadataProperty),
                                ConstraintServices._metadataItemMethod,
                                Expression.Constant(constantKey))
                            );
        }

        private static Expression CreateMetadataValueEqualsExpression(ParameterExpression parameter, object constantValue, string metadataName)
        {
            if (parameter == null)
            {
                throw new ArgumentException(nameof(parameter));
            }

            if (constantValue == null)
            {
                throw new ArgumentException(nameof(constantValue));
            }

            // constantValue.Equals(definition.Metadata[CompositionServices.PartCreationPolicyMetadataName])
            return  Expression.Call(
                        Expression.Constant(constantValue),
                        ConstraintServices._metadataEqualsMethod,
                        Expression.Call(
                            Expression.Property(parameter, ConstraintServices._exportDefinitionMetadataProperty),
                            ConstraintServices._metadataItemMethod,
                            Expression.Constant(metadataName)));
        }

        public static Expression<Func<ExportDefinition, bool>> CreatePartCreatorConstraint(Expression<Func<ExportDefinition, bool>> baseConstraint, ImportDefinition productImportDefinition)
        {
            ParameterExpression exportDefinitionParameter = baseConstraint.Parameters[0];

            // exportDefinition.Metadata
            Expression metadataExpression = Expression.Property(exportDefinitionParameter, ConstraintServices._exportDefinitionMetadataProperty);

            // exportDefinition.Metadata.ContainsKey("ProductDefinition")
            Expression containsProductExpression = Expression.Call(
                metadataExpression,
                ConstraintServices._metadataContainsKeyMethod,
                Expression.Constant(CompositionConstants.ProductDefinitionMetadataName));

            // exportDefinition.Metadata["ProductDefinition"]
            Expression productExportDefinitionExpression = Expression.Call(
                    metadataExpression,
                    ConstraintServices._metadataItemMethod,
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
