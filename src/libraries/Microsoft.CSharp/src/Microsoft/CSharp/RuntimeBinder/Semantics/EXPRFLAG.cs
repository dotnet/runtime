// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    [Flags]
    internal enum EXPRFLAG
    {
        // These are specific to various node types.
        // Order these by value. If you need a new flag, search for the first value that isn't currently valid on your expr kind.

        // 0x1
        EXF_BINOP = 0x1,   // On** Many Non Statement Exprs!** This gets its own BIT!

        // 0x2
        EXF_CTOR = 0x2,                 // Only on EXPRMEMGRP, indicates a constructor.
        EXF_NEEDSRET = 0x2,             // Only on EXPRBLOCK
        EXF_ASLEAVE = 0x2,              // Only on EXPRGOTO, EXPRRETURN, means use leave instead of br instruction
        EXF_ISFAULT = 0x2,              // Only on EXPRTRY, used for fabricated try/fault (no user code)
        EXF_HASHTABLESWITCH = 0x2,      // Only on EXPRFlatSwitch
        EXF_BOX = 0x2,                  // Only on EXPRCAST, indicates a boxing operation (value type -> object)
        EXF_ARRAYCONST = 0x2,           // Only on EXPRARRINIT, indicates that we should init using a memory block
        EXF_MEMBERSET = 0x2,            // Only on EXPRFIELD, indicates that the reference is for set purposes
        EXF_OPENTYPE = 0x2,             // Only on EXPRTYPEOF. Indicates that the type is an open type.
        EXF_LABELREFERENCED = 0x2,      // Only on EXPRLABEL. Indicates the label was targeted by a goto.
        EXF_GENERATEDQMARK = 0x2,       // only on EK_QMARK

        // 0x4
        EXF_INDEXER = 0x4,              // Only on EXPRMEMGRP, indicates an indexer.
        EXF_GOTOCASE = 0x4,             // Only on EXPRGOTO, means goto case or goto default
        EXF_REMOVEFINALLY = 0x4,        // Only on EXPRTRY, means that the try-finally should be converted to normal code
        EXF_UNBOX = 0x4,                // Only on EXPRCAST, indicates a unboxing operation (object -> value type)
        EXF_ARRAYALLCONST = 0x4,        // Only on EXPRARRINIT, indicated that all elems are constant (must also have ARRAYCONST set)
        EXF_CTORPREAMBLE = 0x4,         // Only on EXPRBLOCK, indicates that the block is the preamble of a constructor - contains field inits and base ctor call
        EXF_USERLABEL = 0x4,            // Only on EXPRLABEL, indicates that this is a source-code label, not a compiler-generated label

        // 0x8
        EXF_OPERATOR = 0x8,             // Only on EXPRMEMGRP, indicates an operator.
        EXF_ISPOSTOP = 0x8,             // Only on EXPRMULTI, indicates <x>++
        EXF_FINALLYBLOCKED = 0x8,       // Only on EXPRTRY, EXPRGOTO, EXPRRETURN, means that FINALLY block end is unreachable
        EXF_REFCHECK = 0x8,             // Only on EXPRCAST, indicates an reference checked cast is required
        EXF_WRAPASTEMP = 0x8,           // Only on EXPRWRAP, indicates that this wrap represents an actual local

        // 0x10
        EXF_LITERALCONST = 0x10,        // Only on EXPRCONSTANT, means this was not a folded constant
        EXF_BADGOTO = 0x10,             // Only on EXPRGOTO, indicates an unrealizable goto
        EXF_RETURNISYIELD = 0x10,       // Only on EXPRRETURN, means this is really a yield, and flow continues
        EXF_ISFINALLY = 0x10,           // Only on EXPRTRY
        EXF_NEWOBJCALL = 0x10,          // Only on EXPRCALL and EXPRMEMGRP, to indicate new <...>(...)
        EXF_INDEXEXPR = 0x10,           // Only on EXPRCAST, indicates a special cast for array indexing
        EXF_REPLACEWRAP = 0x10,         // Only on EXPRWRAP, it means the wrap should be replaced with its expr (during rewriting)

        // 0x20
        EXF_UNREALIZEDGOTO = 0x20,      // Only on EXPRGOTO, means target unknown
        EXF_CONSTRAINED = 0x20,         // Only on EXPRCALL, EXPRPROP, indicates a call through a method or prop on a type variable or value type
        EXF_FORCE_BOX = 0x20,           // Only on EXPRCAST, GENERICS: indicates a "forcing" boxing operation (if type parameter boxed then nop, i.e. object -> object, else value type -> object)
        EXF_SIMPLENAME = 0x20,          // Only on EXPRMEMGRP, We're binding a dynamic simple name.

        // 0x40
        EXF_ASFINALLYLEAVE = 0x40,      // Only on EXPRGOTO, EXPRRETURN, means leave through a finally, ASLEAVE must also be set
        EXF_BASECALL = 0x40,            // Only on EXPRCALL, EXPRFNCPTR, EXPRPROP, EXPREVENT, and EXPRMEMGRP, indicates a "base.XXXX" call
        EXF_FORCE_UNBOX = 0x40,         // Only on EXPRCAST, GENERICS: indicates a "forcing" unboxing operation (if type parameter boxed then castclass, i.e. object -> object, else object -> value type)
        EXF_ADDRNOCONV = 0x40,          // Only on EXPRBINOP, with kind == EK_ADDR, indicates that a conv.u should NOT be emitted.

        // 0x80
        EXF_GOTONOTBLOCKED = 0x80,      // Only on EXPRGOTO, means the goto is known to not pass through a finally which does not terminate
        EXF_DELEGATE = 0x80,            // Only on EXPRMEMGRP, indicates an implicit invocation of a delegate: d() vs d.Invoke().
        EXF_STATIC_CAST = 0x80,         // Only on EXPRCAST, indicates a static cast is required. We implement with stloc, ldloc to a temp of the correct type.

        // 0x100
        EXF_USERCALLABLE = 0x100,       // Only on EXPRMEMGRP, indicates a user callable member group.
        EXF_UNBOXRUNTIME = 0x100,       // Only on EXPRCAST, indicates that the runtime binder should unbox this.

        // 0x200
        EXF_NEWSTRUCTASSG = 0x200,      // Only on EXPRCALL, indicates that this is a constructor call which assigns to object
        EXF_GENERATEDSTMT = 0x200,      // Only on statement exprs. Indicates that the statement is compiler generated
        // so we shouldn't report things like "unreachable code" on it.

        // 0x400
        EXF_IMPLICITSTRUCTASSG = 0x400, // Only on EXPRCALL, indicates that this an implicit struct assg call
        EXF_MARKING = 0x400,            // Only on statement exprs. Indicates that we're currently marking
        // its children for reachability (it's up the stack).

        //*** The following are usable on multiple node types.***
        // 0x000800 and above

        EXF_UNREACHABLEBEGIN = 0x000800,    // indicates an unreachable statement
        EXF_UNREACHABLEEND = 0x001000,      // indicates the end of the statement is unreachable
        EXF_USEORIGDEBUGINFO = 0x002000,    // Only set on EXPRDEBUGNOOP, but tested generally. Indicates foreach node should not be overridden to in token
        EXF_LASTBRACEDEBUGINFO = 0x004000,  // indicates override tree to set debuginfo on last brace
        EXF_NODEBUGINFO = 0x008000,         // indicates no debug info for this statement
        EXF_IMPLICITTHIS = 0x010000,        // indicates a compiler provided this pointer (in the EE, when doing autoexp, this can be anything)
        EXF_CANTBENULL = 0x020000,          // indicate this expression can't ever be null (e.g., "this").
        EXF_CHECKOVERFLOW = 0x040000,       // indicates that operation should be checked for overflow
        EXF_PUSH_OP_FIRST = 0x100000,       // On any expr, indicates that the first operand must be placed on the stack before
        // anything else - this is needed for multi-ops involving string concat.
        EXF_ASSGOP = 0x200000,              // On any non stmt exprs, indicates assignment node...
        EXF_LVALUE = 0x400000,              // On any exprs. An lvalue - whether it's legal to assign.

        // THIS IS THE HIGHEST FLAG:

        // Indicates that the expression came from a LocalVariableSymbol, FieldSymbol, or PropertySymbol whose type has the same name so
        // it's OK to use the type instead of the element if using the element would generate an error.
        EXF_SAMENAMETYPE = 0x800000,

        EXF_MASK_ANY = EXF_UNREACHABLEBEGIN | EXF_UNREACHABLEEND |
                        EXF_USEORIGDEBUGINFO | EXF_LASTBRACEDEBUGINFO | EXF_NODEBUGINFO |
                        EXF_IMPLICITTHIS | EXF_CANTBENULL | EXF_CHECKOVERFLOW |
                        EXF_PUSH_OP_FIRST | EXF_ASSGOP | EXF_LVALUE | EXF_SAMENAMETYPE
,

        // Used to mask the cast flags off an EXPRCAST.
        EXF_CAST_ALL = EXF_BOX | EXF_UNBOX | EXF_REFCHECK | EXF_INDEXEXPR | EXF_FORCE_BOX | EXF_FORCE_UNBOX | EXF_STATIC_CAST
    }
}
