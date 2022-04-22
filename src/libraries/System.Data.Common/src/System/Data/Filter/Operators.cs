// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    internal static class Operators
    {
        internal const int Noop = 0;

        /* Unary operations */

        internal const int Negative = 1;
        internal const int UnaryPlus = 2;
        internal const int Not = 3;

        /* Binary operations */

        internal const int BetweenAnd = 4;

        internal const int In = 5;
        internal const int Between = 6;

        /* Beginning of Comparison (relationa) operators */
        internal const int EqualTo = 7;     // =
        internal const int GreaterThen = 8; // >
        internal const int LessThen = 9;        // <
        internal const int GreaterOrEqual = 10; // >=
        internal const int LessOrEqual = 11;    // <=
        internal const int NotEqual = 12;       // <>
        /* End of Comparison (relational) operators */

        internal const int Is = 13;
        internal const int Like = 14;

        /* Beginning of arithmetic operators */
        internal const int Plus = 15;           // +
        internal const int Minus = 16;          // -
        internal const int Multiply = 17;       // *
        internal const int Divide = 18;     // /
        //internal final static int IntegerDiv = 19;    // \
        internal const int Modulo = 20;     // %
        //internal final static int Exponent = 21;    // **
        /* End of arithmetic operators */

        /* Beginning of bitwise operators */
        internal const int BitwiseAnd = 22; // &
        internal const int BitwiseOr = 23;      // |
        internal const int BitwiseXor = 24; // ^
        internal const int BitwiseNot = 25; // ~
        /* End of bitwise operators */

        /* Beginning of logical operators */
        internal const int And = 26;        // AND
        internal const int Or = 27;     // OR
        // internal final static int Not is in the unary ops
        /* End of logical operators */

        /* Calls/multi-valued stuff */
        internal const int Proc = 28;
        internal const int Iff = 29;
        internal const int Qual = 30;
        internal const int Dot = 31;

        /* 0-ary "operators" */
        internal const int Null = 32;
        internal const int True = 33;
        internal const int False = 34;

        internal const int Date = 35;           // Date constant
        internal const int GenUniqueId = 36;    // Generate unique ID
        internal const int GenGUID = 37;       // Generate GUID
        internal const int GUID = 38;          // GUID constant

        internal const int IsNot = 39;

        internal static bool IsArithmetical(int op)
        {
            return (op == Plus || op == Minus || op == Multiply || op == Divide || op == Modulo);
        }
        internal static bool IsLogical(int op)
        {
            return (op == And || op == Or || op == Not || op == Is || op == IsNot);
        }
        internal static bool IsRelational(int op)
        {
            return ((EqualTo <= op) && (op <= NotEqual));
        }

        /// <summary>
        ///     Operator priorities
        /// </summary>
        internal const int priStart = 0;
        internal const int priSubstr = 1;
        internal const int priParen = 2;
        internal const int priLow = 3;
        internal const int priImp = 4;
        internal const int priEqv = 5;
        internal const int priXor = 6;
        internal const int priOr = 7;
        internal const int priAnd = 8;
        internal const int priNot = 9;
        internal const int priIs = 10;
        internal const int priBetweenInLike = 11;
        internal const int priBetweenAnd = 12;
        internal const int priRelOp = 13;
        internal const int priConcat = 14;
        internal const int priContains = 15;
        internal const int priPlusMinus = 16;
        internal const int priMod = 17;
        internal const int priIDiv = 18;
        internal const int priMulDiv = 19;
        internal const int priNeg = 20;
        internal const int priExp = 21;
        internal const int priProc = 22;
        internal const int priDot = 23;
        internal const int priMax = 24;

        /// <summary>
        ///     Mapping from Operator to priorities
        ///     CONSIDER: fast, but hard to maintain
        /// </summary>

        private static readonly int[] s_priority = new int[] {
            priStart,  // Noop
            priNeg, priNeg, priNot, // Unary -, +, Not
            priBetweenAnd, priBetweenInLike, priBetweenInLike,
            priRelOp, priRelOp, priRelOp, priRelOp, priRelOp, priRelOp,
            priIs,
            priBetweenInLike,                       // Like

            priPlusMinus, priPlusMinus,             // +, -
            priMulDiv, priMulDiv, priIDiv, priMod,  // *, /, \, Mod
            priExp,                                 // **

            priAnd, priOr, priXor, priNot,
            priAnd, priOr,

            priParen, priProc, priDot, priDot,      // Proc, Iff, Qula, Dot..

            priMax, priMax, priMax, priMax, priMax, priMax, priMax,
            priMax, priMax, priMax, priMax,
            priMax,
        };

        internal static int Priority(int op)
        {
            if (op > s_priority.Length)
                return priMax;
            return s_priority[op];
        }

        /// <summary>
        ///     this is array used for error messages.
        /// </summary>
        private static readonly string[] s_looks = new string[] {
            "", //Noop = 0;

            /* Unary operations */

            "-",    //Negative = 1;
            "+",    //UnaryPlus = 2;
            "Not",  //Not = 3;

            /* Binary operations */

            "BetweenAnd",   //BetweenAnd = 4;

            "In",   //In = 5;
            "Between", //Between = 6;

            /* Beginning of Comparison (relationa) operators */
            "=",    //EqualTo = 7;        // =
            ">", //GreaterThen = 8;    // >
            "<",    //LessThen = 9;        // <
            ">=", //GreaterOrEqual = 10;// >=
            "<=",       //LessOrEqual = 11;    // <=
            "<>", //NotEqual = 12;        // <>
            /* End of Comparison (relational) operators */

            "Is",       //Is = 13;
            "Like", //Like = 14;

            /* Beginning of arithmetic operators */
            "+",    //Plus = 15;            // +
            "-", //Minus = 16;            // -
            "*", //Multiply = 17;        // *
            "/",    //Divide = 18;        // /
            "\\", //IntegerDiv = 19;    // \
            "Mod", //Modulo = 20;        // %
            "**", //Exponent = 21;    // **
            /* End of arithmetic operators */

            /* Beginning of bitwise operators */
            "&",    //BitwiseAnd = 22;    // &
            "|",    //BitwiseOr = 23;        // |
            "^",    //BitwiseXor = 24;    // ^
            "~",    //BitwiseNot = 25;    // ~
            /* End of bitwise operators */

            /* Beginning of logical operators */
            "And",  //And = 26;        // AND
            "Or",       //Or = 27;        // OR
            // Not is in the unary ops
            /* End of logical operators */

            /* Calls/multi-valued stuff */
            "Proc", //Proc = 28;
            "Iff",  //Iff = 29;
            ".",    //Qual = 30;
            ".",    //Dot = 31;

            /* 0-ary "operators" */
            "Null", //Null = 32;
            "True", //True = 33;
            "False", //False = 34;

            "Date", //Date = 35;            // Date constant
            "GenUniqueId()",    //GenUniqueId = 36;    // Generate unique ID
            "GenGuid()",    //GenGUID = 37;        // Generate GUID
            "Guid {..}",    //GUID = 38;            // GUID constant

            "Is Not",   //IsNot = 39;            // internal only
        };

        internal static string ToString(int op)
        {
            string st;

            if (op <= s_looks.Length)
                st = s_looks[op];
            else
                st = "Unknown op";

            return st;
        }
    }
}
