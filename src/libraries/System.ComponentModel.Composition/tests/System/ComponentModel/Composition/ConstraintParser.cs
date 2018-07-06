// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Internal;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ContraintParser
    {
        private static readonly PropertyInfo _exportDefinitionContractNameProperty = typeof(ExportDefinition).GetProperty("ContractName");
        private static readonly PropertyInfo _exportDefinitionMetadataProperty = typeof(ExportDefinition).GetProperty("Metadata");
        private static readonly MethodInfo _metadataContainsKeyMethod = typeof(IDictionary<string, object>).GetMethod("ContainsKey");
        private static readonly MethodInfo _metadataItemMethod = typeof(IDictionary<string, object>).GetMethod("get_Item");
        private static readonly MethodInfo _typeIsInstanceOfTypeMethod = typeof(Type).GetMethod("IsInstanceOfType");

        public static bool TryParseConstraint(Expression<Func<ExportDefinition, bool>> constraint, out string contractName, out IEnumerable<KeyValuePair<string, Type>> requiredMetadata)
        {
            contractName = null;
            requiredMetadata = null;

            List<KeyValuePair<string, Type>> requiredMetadataList = new List<KeyValuePair<string, Type>>();
            foreach (Expression expression in SplitConstraintBody(constraint.Body))
            {
                // First try to parse as a contract, if we don't have one already
                if (contractName == null && TryParseExpressionAsContractConstraintBody(expression, constraint.Parameters[0], out contractName))
                {
                    continue;
                }

                // Then try to parse as a required metadata item name
                string requiredMetadataItemName = null;
                Type requiredMetadataItemType = null;
                if (TryParseExpressionAsMetadataConstraintBody(expression, constraint.Parameters[0], out requiredMetadataItemName, out requiredMetadataItemType))
                {
                    requiredMetadataList.Add(new KeyValuePair<string, Type>(requiredMetadataItemName, requiredMetadataItemType));
                }

                // Just skip the expressions we don't understand  
            }

            // ContractName should have been set already, just need to set metadata
            requiredMetadata = requiredMetadataList;
            return true;
        }

        private static IEnumerable<Expression> SplitConstraintBody(Expression expression)
        {
            Assert.NotNull(expression);

            // The expression we know about should be a set of nested AndAlso's, we
            // need to flatten them into one list. we do this iteratively, as 
            // recursion will create too much of a memory churn.
            Stack<Expression> expressions = new Stack<Expression>();
            expressions.Push(expression);

            while (expressions.Count > 0)
            {
                Expression current = expressions.Pop();
                if (current.NodeType == ExpressionType.AndAlso)
                {
                    BinaryExpression andAlso = (BinaryExpression)current;
                    // Push right first - this preserves the ordering of the expression, which will force
                    // the contract constraint to come up first as the callers are optimized for this form
                    expressions.Push(andAlso.Right);
                    expressions.Push(andAlso.Left);
                    continue;
                }

                yield return current;
            }
        }

        private static bool TryParseExpressionAsContractConstraintBody(Expression expression, Expression parameter, out string contractName)
        {
            contractName = null;

            // The expression should be an '==' expression
            if (expression.NodeType != ExpressionType.Equal)
            {
                return false;
            }

            BinaryExpression contractConstraintExpression = (BinaryExpression)expression;

            // First try item.ContractName == "Value"
            if (TryParseContractNameFromEqualsExpression(contractConstraintExpression.Left, contractConstraintExpression.Right, parameter, out contractName))
            {
                return true;
            }

            // Then try "Value == item.ContractName
            if (TryParseContractNameFromEqualsExpression(contractConstraintExpression.Right, contractConstraintExpression.Left, parameter, out contractName))
            {
                return true;
            }

            return false;
        }

        private static bool TryParseContractNameFromEqualsExpression(Expression left, Expression right, Expression parameter, out string contractName)
        {
            contractName = null;

            // The left should be access to property "Contract" applied to the parameter
            MemberExpression targetMember = left as MemberExpression;
            if (targetMember == null)
            {
                return false;
            }

            if ((targetMember.Member != _exportDefinitionContractNameProperty) || (targetMember.Expression != parameter))
            {
                return false;
            }

            // Right should a constant expression containing the contract name
            ConstantExpression contractNameConstant = right as ConstantExpression;
            if (contractNameConstant == null)
            {
                return false;
            }

            if (!TryParseConstant<string>(contractNameConstant, out contractName))
            {
                return false;
            }

            return true;
        }

        private static bool TryParseExpressionAsMetadataConstraintBody(Expression expression, Expression parameter, out string requiredMetadataKey, out Type requiredMetadataType)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            requiredMetadataKey = null;
            requiredMetadataType = null;

            // Should be a call to Type.IsInstanceofType on definition.Metadata[key]
            MethodCallExpression outerMethodCall = expression as MethodCallExpression;
            if (outerMethodCall == null)
            {
                return false;
            }

            // Make sure that the right method ie being called
            if (outerMethodCall.Method != _typeIsInstanceOfTypeMethod)
            {
                return false;
            }
            if (outerMethodCall.Arguments.Count != 1)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            // 'this' should be a constant expression pointing at a Type object
            ConstantExpression targetType = outerMethodCall.Object as ConstantExpression;
            if (!TryParseConstant<Type>(targetType, out requiredMetadataType))
            {
                return false;
            }

            // SHould be a call to get_Item
            MethodCallExpression methodCall = outerMethodCall.Arguments[0] as MethodCallExpression;
            if (methodCall == null)
            {
                return false;
            }

            if (methodCall.Method != _metadataItemMethod)
            {
                return false;
            }

            // Make sure the method is being called on the right object            
            MemberExpression targetMember = methodCall.Object as MemberExpression;
            if (targetMember == null)
            {
                return false;
            }

            if ((targetMember.Expression != parameter) || (targetMember.Member != _exportDefinitionMetadataProperty))
            {
                return false;
            }

            // There should only ever be one argument; otherwise, 
            // we've got the wrong IDictionary.get_Item method.
            if(methodCall.Arguments.Count != 1)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }
            // Argument should a constant expression containing the metadata key
            ConstantExpression requiredMetadataKeyConstant = methodCall.Arguments[0] as ConstantExpression;
            if (requiredMetadataKeyConstant == null)
            {
                return false;
            }

            if (!TryParseConstant<string>(requiredMetadataKeyConstant, out requiredMetadataKey))
            {
                return false;
            }

            return true;
        }

        private static bool TryParseConstant<T>(ConstantExpression constant, out T result)
            where T : class
        {
            if (constant == null)
            {
                throw new ArgumentNullException(nameof(constant));
            }

            if (constant.Type == typeof(T) && constant.Value != null)
            {
                result = (T)constant.Value;
                return true;
            }

            result = default(T);
            return false;
        }
    }
}
