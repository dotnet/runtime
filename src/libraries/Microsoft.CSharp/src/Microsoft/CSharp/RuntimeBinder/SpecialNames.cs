// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class SpecialNames
    {
        public const string ImplicitConversion = "op_Implicit";
        public const string ExplicitConversion = "op_Explicit";
        public const string Invoke = "Invoke";
        public const string Constructor = ".ctor";
        public const string Indexer = "$Item$";

        // Binary Operators
        public const string CLR_Add = "op_Addition";
        public const string CLR_Subtract = "op_Subtraction";
        public const string CLR_Multiply = "op_Multiply";
        public const string CLR_Division = "op_Division";
        public const string CLR_Modulus = "op_Modulus";
        public const string CLR_LShift = "op_LeftShift";
        public const string CLR_RShift = "op_RightShift";
        public const string CLR_LT = "op_LessThan";
        public const string CLR_GT = "op_GreaterThan";
        public const string CLR_LTE = "op_LessThanOrEqual";
        public const string CLR_GTE = "op_GreaterThanOrEqual";
        public const string CLR_Equality = "op_Equality";
        public const string CLR_Inequality = "op_Inequality";
        public const string CLR_BitwiseAnd = "op_BitwiseAnd";
        public const string CLR_ExclusiveOr = "op_ExclusiveOr";
        public const string CLR_BitwiseOr = "op_BitwiseOr";
        public const string CLR_LogicalNot = "op_LogicalNot";

        // In place binary operators.
        public const string CLR_InPlaceAdd = "op_Addition";
        public const string CLR_InPlaceSubtract = "op_Subtraction";
        public const string CLR_InPlaceMultiply = "op_Multiply";
        public const string CLR_InPlaceDivide = "op_Division";
        public const string CLR_InPlaceModulus = "op_Modulus";
        public const string CLR_InPlaceBitwiseAnd = "op_BitwiseAnd";
        public const string CLR_InPlaceExclusiveOr = "op_ExclusiveOr";
        public const string CLR_InPlaceBitwiseOr = "op_BitwiseOr";
        public const string CLR_InPlaceLShift = "op_LeftShift";
        public const string CLR_InPlaceRShift = "op_RightShift";

        // Unary Operators
        public const string CLR_UnaryNegation = "op_UnaryNegation";
        public const string CLR_UnaryPlus = "op_UnaryPlus";
        public const string CLR_OnesComplement = "op_OnesComplement";
        public const string CLR_True = "op_True";
        public const string CLR_False = "op_False";

        public const string CLR_PreIncrement = "op_Increment";
        public const string CLR_PostIncrement = "op_Increment";
        public const string CLR_PreDecrement = "op_Decrement";
        public const string CLR_PostDecrement = "op_Decrement";
    }
}
