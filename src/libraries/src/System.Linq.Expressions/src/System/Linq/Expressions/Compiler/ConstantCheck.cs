// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Compiler
{
    internal enum AnalyzeTypeIsResult
    {
        KnownFalse,
        KnownTrue,
        KnownAssignable, // need null check only
        Unknown,         // need full runtime check
    }

    internal static class ConstantCheck
    {
        internal static bool IsNull(Expression e)
        {
            if (e.NodeType == ExpressionType.Constant)
            {
                return ((ConstantExpression)e).Value == null;
            }
            return false;
        }


        /// <summary>
        /// If the result of a TypeBinaryExpression is known statically, this
        /// returns the result, otherwise it returns null, meaning we'll need
        /// to perform the IsInst instruction at runtime.
        /// 
        /// The result of this function must be equivalent to IsInst, or
        /// null.
        /// </summary>
        internal static AnalyzeTypeIsResult AnalyzeTypeIs(TypeBinaryExpression typeIs)
        {
            return AnalyzeTypeIs(typeIs.Expression, typeIs.TypeOperand);
        }

        /// <summary>
        /// If the result of an isinst opcode is known statically, this
        /// returns the result, otherwise it returns null, meaning we'll need
        /// to perform the IsInst instruction at runtime.
        /// 
        /// The result of this function must be equivalent to IsInst, or
        /// null.
        /// </summary>
        private static AnalyzeTypeIsResult AnalyzeTypeIs(Expression operand, Type testType)
        {
            Type operandType = operand.Type;

            // Oddly, we allow void operands
            // This is LinqV1 behavior of TypeIs
            if (operandType == typeof(void))
            {
                return AnalyzeTypeIsResult.KnownFalse;
            }

            //
            // Type comparisons treat nullable types as if they were the
            // underlying type. The reason is when you box a nullable it
            // becomes a boxed value of the underlying type, or null.
            //
            Type nnOperandType = operandType.GetNonNullableType();
            Type nnTestType = testType.GetNonNullableType();

            //
            // See if we can determine the answer based on the static types
            //
            // Extensive testing showed that Type.IsAssignableFrom,
            // Type.IsInstanceOfType, and the isinst instruction were all
            // equivalent when used against a live object
            //
            if (nnTestType.IsAssignableFrom(nnOperandType))
            {
                // If the operand is a value type (other than nullable), we
                // know the result is always true.
                if (operandType.GetTypeInfo().IsValueType && !operandType.IsNullableType())
                {
                    return AnalyzeTypeIsResult.KnownTrue;
                }

                // For reference/nullable types, we need to compare to null at runtime
                return AnalyzeTypeIsResult.KnownAssignable;
            }

            // We used to have an if IsSealed, return KnownFalse check here.
            // but that doesn't handle generic types & co/contravariance correctly.
            // So just use IsInst, which we know always gives us the right answer.

            // Otherwise we need a full runtime check
            return AnalyzeTypeIsResult.Unknown;
        }
    }
}
