// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Xml.Xsl.XPath
{
    // order is importent. We are using them as an index in OperatorGroup & QilOperator & XPathOperatorToQilNodeType arrays
    // (ValEq - Eq) == (ValGe - Ge)
    internal enum XPathOperator
    {
        /*Unknown   */
        Unknown = 0,
        // XPath 1.0 operators:
        /*Logical   */
        Or,
        And,
        /*Equality  */
        Eq,
        Ne,
        /*Relational*/
        Lt,
        Le,
        Gt,
        Ge,
        /*Arithmetic*/
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        /*Negate    */
        UnaryMinus,
        /*Union     */
        Union,
        LastXPath1Operator = Union,
        /* XQuery & XPath 2.0 Operators: */
        UnaryPlus,
        Idiv,
        Is,
        After,
        Before,
        Range,
        Except,
        Intersect,
        ValEq,
        ValNe,
        ValLt,
        ValLe,
        ValGt,
        ValGe
    }
}
