// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal enum OperatorKind : uint
    {
        OP_NONE,

        // Binary
        OP_ASSIGN,
        OP_ADDEQ,
        OP_SUBEQ,
        OP_MULEQ,
        OP_DIVEQ,
        OP_MODEQ,
        OP_ANDEQ,
        OP_XOREQ,
        OP_OREQ,
        OP_LSHIFTEQ,
        OP_RSHIFTEQ,
        OP_QUESTION,
        OP_VALORDEF,
        OP_LOGOR,
        OP_LOGAND,
        OP_BITOR,
        OP_BITXOR,
        OP_BITAND,
        OP_EQ,
        OP_NEQ,
        OP_LT,
        OP_LE,
        OP_GT,
        OP_GE,
        OP_IS,
        OP_AS,
        OP_LSHIFT,
        OP_RSHIFT,
        OP_ADD,
        OP_SUB,
        OP_MUL,
        OP_DIV,
        OP_MOD,

        // Unary
        OP_NOP,
        OP_UPLUS,
        OP_NEG,
        OP_BITNOT,
        OP_LOGNOT,
        OP_PREINC,
        OP_PREDEC,
        OP_TYPEOF,
        OP_CHECKED,
        OP_UNCHECKED,

        OP_MAKEREFANY,
        OP_REFVALUE,
        OP_REFTYPE,
        OP_ARGS,

        OP_CAST,
        OP_INDIR,
        OP_ADDR,

        OP_COLON,
        OP_THIS,
        OP_BASE,
        OP_NULL,
        OP_TRUE,
        OP_FALSE,
        OP_CALL,
        OP_DEREF,
        OP_PAREN,
        OP_POSTINC,
        OP_POSTDEC,
        OP_DOT,
        OP_IMPLICIT,
        OP_EXPLICIT,

        OP_EQUALS,
        OP_COMPARE,

        OP_DEFAULT,

        OP_LAST
    }
}
