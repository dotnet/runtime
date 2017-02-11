// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // Used by bindUserDefinedConversion
    internal sealed class UdConvInfo
    {
        public MethWithType mwt;
        public bool fSrcImplicit;
        public bool fDstImplicit;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    // Small wrapper for passing around argument information for the various BindGrpTo methods
    // It is used because most things only need the type, but in the case of METHGRPs and ANONMETHs
    // the expr is also needed to determine if a conversion is possible
    internal sealed class ArgInfos
    {
        public int carg;
        public TypeArray types;
        public bool fHasExprs;
        public List<EXPR> prgexpr;
    }

    internal enum BodyType
    {
        NormalBlock,
        StatementExpression,
        ReturnedExpression
    }


    internal enum ConstCastResult
    {
        Success,      // Constant can be cast to type
        Failure,      // Constant cannot be cast to type
        CheckFailure  // Constant cannot be cast to type because of overflow in checked context
                      // (Note that this only happens when the conversion is explicit; implicit
                      // conversions never overflow, that's why they're implicit.)
    }

    internal enum AggCastResult
    {
        Success, // We found a conversion, stop looking
        Failure, // This conversion doesn't work, keep looking
        Abort    // No possible conversion can work, stop looking
    }

    internal enum UnaryOperatorSignatureFindResult
    {
        Match,
        Continue,
        Return
    }


    internal enum UnaOpKind
    {
        Plus,
        Minus,
        Tilde,
        Bang,
        IncDec,
        Lim
    }

    internal enum UnaOpMask
    {
        None = 0,
        Plus = 1 << UnaOpKind.Plus,
        Minus = 1 << UnaOpKind.Minus,
        Tilde = 1 << UnaOpKind.Tilde,
        Bang = 1 << UnaOpKind.Bang,
        IncDec = 1 << UnaOpKind.IncDec,
        // The different combinations needed in operators.cs
        Signed = Plus | Minus | Tilde,
        Unsigned = Plus | Tilde,
        Real = Plus | Minus,
        Bool = Bang,
    }

    internal enum OpSigFlags
    {
        None = 0,
        Convert = 0x01,    // Convert the operands before calling the bind method
        CanLift = 0x02,    // Operator has a lifted form
        AutoLift = 0x04,   // Standard nullable lifting
        // The different combinations needed in operators.cs
        Value = Convert | CanLift | AutoLift,
        Reference = Convert,
        BoolBit = Convert | CanLift,
    }

    internal enum LiftFlags
    {
        None = 0,
        Lift1 = 0x01,
        Lift2 = 0x02,
        Convert1 = 0x04,
        Convert2 = 0x08,
    }

    internal enum CheckLvalueKind
    {
        Assignment,
        OutParameter,
        Increment,
    }

    internal enum BinOpFuncKind
    {
        BoolBinOp,
        BoolBitwiseOp,
        DecBinOp,
        DelBinOp,
        EnumBinOp,
        IntBinOp,
        PtrBinOp,
        PtrCmpOp,
        RealBinOp,
        RefCmpOp,
        ShiftOp,
        StrBinOp,
        StrCmpOp,
        None
    }

    internal enum UnaOpFuncKind
    {
        BoolUnaOp,
        DecUnaOp,
        EnumUnaOp,
        IntUnaOp,
        RealUnaOp,
        LiftedIncOpCore,
        None
    }

    internal sealed partial class ExpressionBinder
    {
        // ExpressionBinder - General Rules
        // 
        // Express the Contract
        // 
        // Use assertions and naming guidelines to express the contract for methods. 
        // The most common issue is whether an argument may be null or not. If an 
        // argument may not be null, then the method must ASSERT that before any other 
        // code. If an argument may be null then the name of the argument should 
        // include 'Optional'. The exception to this rule is the input parse tree 
        // parameter. If the parse tree may be null, then the method name should 
        // include an 'Opt' suffix. For example bindArgumentList should really be 
        // named bindArgumentListOpt. Abbreviations should be avoided, but the 'Opt' 
        // suffix gets an exception because it is used consistently in the language 
        // spec.
        // 
        // 
        // Error Tolerant
        // 
        // Do not rely on the input parse tree being complete. Erroneous code may 
        // result in parse trees with required children missing, or with unexpected 
        // structure. Find out what the invariants are for the parse tree being 
        // consumed and code defensively.
        // 
        // Similarly, the result of binding children nodes may not be 'OK'. The child 
        // node may have contained some semantic errors and the binding code in the 
        // parent must cope gracefully with the result. For example, an EXPRMEMGRP may 
        // contain no members.
        // 
        // 
        // Error Recovery
        // 
        // Always attempt to bind children nodes even if errors have already been 
        // detected in other children. Always build a new node representing a 'best 
        // guess' at the semantics of the parse tree. Never discard the results of 
        // binding a child node even if the binding has errors. Since a new node is 
        // always produced there should always be a place to add bindings with errors 
        // to the result. 
        // 
        // This ensures that full semantic information for all nodes is produced - 
        // that the expression binder always produces a 'best guess' for every 
        // expression in source.
        // 
        // 
        // Error Reporting
        // 
        // If a child expression has an error, then no new error's for the parent 
        // should be reported, unless there is no way that the new error was caused 
        // by any child errors. If child nodes don't have any errors, and the new 
        // node does have an error, then at least one error must be reported. These 
        // rules ensure that an error is always reported for erroneous code, and that 
        // only the most meaningful error is reported from a set of cascading errors.
        // 
        // 
        // Map Back To the Source
        // 
        // When constructing new expression nodes, attach the appropriate parse tree 
        // node. The attached parse tree is used for:
        //             - error location reporting
        //             - debug sequence points 
        //                         - stepping
        //                         - local variable scopes
        //             - finding the most meaningful expression for a parse tree node
        // 
        // 
        // Meaning not Implementation
        // 
        // The expression trees resulting from the initial binding pass should 
        // represent the semantics of the input source code. There should be a 
        // direct mapping between the newly constructed expression and the input 
        // parse tree.
        // 
        // The Whidbey codebase had the habit of producing expressions in the initial 
        // binding which were closer in representation to the generated IL than the 
        // input source code. In Orcas all transformations which may lose semantic 
        // information about the source must be done after the initial expression 
        // binding phase. 
        // 
        // Special Cases
        // 
        //     -   Constant folding - Constant folding is a semantic losing 
        //         transformation which must be performed to complete expression 
        //         analysis. When creating a folded constant, create an expression 
        //         node representing the unfolded expression, then pass this as a 
        //         child expression of a new constant expression.
        //     -   Color Color - The new Type or Instance expression covers this case. 
        //         It is produced from bindSimpleName.
        //     -   Method Group - Method groups should be preserved in expression trees. 
        //         This includes as children of Call expressions, and delegate construction 
        //         nodes.
        //     -   Type Binding - This is a big one. Whenever a type is bound in an 
        //         expression, the binding of the component parts of the type must be 
        //         preserved. This includes every identifier in a dotted type or 
        //         namespace name, as well as the type binding information for the 
        //         type arguments of constructed types. The semantic information for 
        //         intermediate type binding results is represented by an 
        //         EXPRTYPEORNAMESPACE. Types can be bound in several places in 
        //         expressions:
        //             - Sizeof
        //             - Typeof
        //             - New
        //             - Is/As
        //             - Cast
        //             - Type Arguments supplied to generic method calls.
        //             - Left hand side of a dot operator.
        //             - Parameter types in anonymous methods and lambdas.
        //             - Local Variables
        // 
        // 
        // Want to eventually Have's
        // 
        // Only build the new node once all children have been built.
        // Factory should require all children as arguments.
        // Factory method sets the "Do Children have Errors?" bit - not done manually.
        // Once constructed Expression trees are not mutated - doesn't work easily for statements unfortunately.

        private delegate EXPR PfnBindBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR op1, EXPR op2);
        private delegate EXPR PfnBindUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR op);

        private BindingContext Context;
        public BindingContext GetContext() { return Context; }
        private CNullable m_nullable;

        private static void VSFAIL(string s)
        {
            Debug.Assert(false, s);
        }

        public ExpressionBinder(BindingContext context)
        {
            Context = context;
            m_nullable = new CNullable(GetSymbolLoader(), GetErrorContext(), GetExprFactory());
            g_binopSignatures = new BinOpSig[]
            {
                new BinOpSig (PredefinedType.PT_INT,        PredefinedType.PT_INT,      BinOpMask.Integer,  8, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
                new BinOpSig (PredefinedType.PT_UINT,       PredefinedType.PT_UINT,     BinOpMask.Integer,  7, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
                new BinOpSig (PredefinedType.PT_LONG,       PredefinedType.PT_LONG,     BinOpMask.Integer,  6, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
                new BinOpSig (PredefinedType.PT_ULONG,      PredefinedType.PT_ULONG,    BinOpMask.Integer,  5, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
                /* ERROR */
                new BinOpSig (PredefinedType.PT_ULONG,      PredefinedType.PT_LONG,     BinOpMask.Integer,  4, null,                    OpSigFlags.Value,       BinOpFuncKind.None          ),
                /* ERROR */
                new BinOpSig (PredefinedType.PT_LONG,       PredefinedType.PT_ULONG,    BinOpMask.Integer,  3, null,                    OpSigFlags.Value,       BinOpFuncKind.None          ),
                new BinOpSig (PredefinedType.PT_FLOAT,      PredefinedType.PT_FLOAT,    BinOpMask.Real,     1, BindRealBinOp,           OpSigFlags.Value,       BinOpFuncKind.RealBinOp     ),
                new BinOpSig (PredefinedType.PT_DOUBLE,     PredefinedType.PT_DOUBLE,   BinOpMask.Real,     0, BindRealBinOp,           OpSigFlags.Value,       BinOpFuncKind.RealBinOp     ),
                new BinOpSig (PredefinedType.PT_DECIMAL,    PredefinedType.PT_DECIMAL,  BinOpMask.Real,     0, BindDecBinOp,            OpSigFlags.Value,       BinOpFuncKind.DecBinOp      ),
                new BinOpSig (PredefinedType.PT_STRING,     PredefinedType.PT_STRING,   BinOpMask.Equal,    0, BindStrCmpOp,            OpSigFlags.Reference,   BinOpFuncKind.StrCmpOp      ),
                new BinOpSig (PredefinedType.PT_STRING,     PredefinedType.PT_STRING,   BinOpMask.Add,      2, BindStrBinOp,            OpSigFlags.Reference,   BinOpFuncKind.StrBinOp      ),
                new BinOpSig (PredefinedType.PT_STRING,     PredefinedType.PT_OBJECT,   BinOpMask.Add,      1, BindStrBinOp,            OpSigFlags.Reference,   BinOpFuncKind.StrBinOp      ),
                new BinOpSig (PredefinedType.PT_OBJECT,     PredefinedType.PT_STRING,   BinOpMask.Add,      0, BindStrBinOp,            OpSigFlags.Reference,   BinOpFuncKind.StrBinOp      ),
                new BinOpSig (PredefinedType.PT_INT,        PredefinedType.PT_INT,      BinOpMask.Shift,    3, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
                new BinOpSig (PredefinedType.PT_UINT,       PredefinedType.PT_INT,      BinOpMask.Shift,    2, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
                new BinOpSig (PredefinedType.PT_LONG,       PredefinedType.PT_INT,      BinOpMask.Shift,    1, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
                new BinOpSig (PredefinedType.PT_ULONG,      PredefinedType.PT_INT,      BinOpMask.Shift,    0, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
                new BinOpSig (PredefinedType.PT_BOOL,       PredefinedType.PT_BOOL,     BinOpMask.BoolNorm, 0, BindBoolBinOp,           OpSigFlags.Value,       BinOpFuncKind.BoolBinOp     ),
                // Make boolean logical operators liftable so that they don't give funny short circuiting semantics.
                // This is for DDBugs 677075.
                new BinOpSig (PredefinedType.PT_BOOL,       PredefinedType.PT_BOOL,     BinOpMask.Logical,  0, BindBoolBinOp,           OpSigFlags.BoolBit,     BinOpFuncKind.BoolBinOp     ),
                new BinOpSig (PredefinedType.PT_BOOL,       PredefinedType.PT_BOOL,     BinOpMask.Bitwise,  0, BindLiftedBoolBitwiseOp, OpSigFlags.BoolBit,     BinOpFuncKind.BoolBitwiseOp ),
            };
            g_rguos = new UnaOpSig[]
            {
                new UnaOpSig( PredefinedType.PT_INT,        UnaOpMask.Signed,   7, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
                new UnaOpSig( PredefinedType.PT_UINT,       UnaOpMask.Unsigned, 6, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
                new UnaOpSig( PredefinedType.PT_LONG,       UnaOpMask.Signed,   5, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
                new UnaOpSig( PredefinedType.PT_ULONG,      UnaOpMask.Unsigned, 4, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
                /* ERROR */
                new UnaOpSig( PredefinedType.PT_ULONG,      UnaOpMask.Minus,    3, null,            UnaOpFuncKind.None      ),
                new UnaOpSig( PredefinedType.PT_FLOAT,      UnaOpMask.Real,     1, BindRealUnaOp,   UnaOpFuncKind.RealUnaOp ),
                new UnaOpSig( PredefinedType.PT_DOUBLE,     UnaOpMask.Real,     0, BindRealUnaOp,   UnaOpFuncKind.RealUnaOp ),
                new UnaOpSig( PredefinedType.PT_DECIMAL,    UnaOpMask.Real,     0, BindDecUnaOp,    UnaOpFuncKind.DecUnaOp  ),
                new UnaOpSig( PredefinedType.PT_BOOL,       UnaOpMask.Bool,     0, BindBoolUnaOp,   UnaOpFuncKind.BoolUnaOp ),
                new UnaOpSig( PredefinedType.PT_INT,        UnaOpMask.IncDec,   6, null,            UnaOpFuncKind.None      ),
                new UnaOpSig( PredefinedType.PT_UINT,       UnaOpMask.IncDec,   5, null,            UnaOpFuncKind.None      ),
                new UnaOpSig( PredefinedType.PT_LONG,       UnaOpMask.IncDec,   4, null,            UnaOpFuncKind.None      ),
                new UnaOpSig( PredefinedType.PT_ULONG,      UnaOpMask.IncDec,   3, null,            UnaOpFuncKind.None      ),
                new UnaOpSig( PredefinedType.PT_FLOAT,      UnaOpMask.IncDec,   1, null,            UnaOpFuncKind.None      ),
                new UnaOpSig( PredefinedType.PT_DOUBLE,     UnaOpMask.IncDec,   0, null,            UnaOpFuncKind.None      ),
                new UnaOpSig( PredefinedType.PT_DECIMAL,    UnaOpMask.IncDec,   0, null,            UnaOpFuncKind.None      ),
            };
        }

        private SymbolLoader GetSymbolLoader() { return SymbolLoader; }

        private SymbolLoader SymbolLoader
        {
            get
            {
                return Context.SymbolLoader;
            }
        }

        private CSemanticChecker SemanticChecker
        {
            get
            {
                return Context.SemanticChecker;
            }
        }
        public CSemanticChecker GetSemanticChecker() { return SemanticChecker; }

        private ErrorHandling ErrorContext
        {
            get
            {
                return SymbolLoader.ErrorContext;
            }
        }
        private ErrorHandling GetErrorContext() { return ErrorContext; }

        private BSYMMGR GetGlobalSymbols()
        {
            return GetSymbolLoader().getBSymmgr();
        }

        private TypeManager GetTypes() { return TypeManager; }

        private TypeManager TypeManager { get { return SymbolLoader.TypeManager; } }

        private ExprFactory GetExprFactory() { return ExprFactory; }

        private ExprFactory ExprFactory { get { return Context.GetExprFactory(); } }

        private ConstValFactory GetExprConstants()
        {
            return GetExprFactory().GetExprConstants();
        }

        private AggregateType GetReqPDT(PredefinedType pt)
        {
            return GetReqPDT(pt, GetSymbolLoader());
        }

        private static AggregateType GetReqPDT(PredefinedType pt, SymbolLoader symbolLoader)
        {
            Debug.Assert(pt != PredefinedType.PT_VOID);  // use getVoidType()
            return symbolLoader.GetReqPredefType(pt, true);
        }

        private AggregateType GetOptPDT(PredefinedType pt)
        {
            return GetOptPDT(pt, true);
        }

        private AggregateType GetOptPDT(PredefinedType pt, bool WarnIfNotFound)
        {
            Debug.Assert(pt != PredefinedType.PT_VOID);  // use getVoidType()
            if (WarnIfNotFound)
            {
                return GetSymbolLoader().GetOptPredefTypeErr(pt, true);
            }
            else
            {
                return GetSymbolLoader().GetOptPredefType(pt, true);
            }
        }

        private CType VoidType { get { return GetSymbolLoader().GetTypeManager().GetVoid(); } }

        private CType getVoidType() { return VoidType; }

        private EXPR GenerateAssignmentConversion(EXPR op1, EXPR op2, bool allowExplicit)
        {
            if (allowExplicit)
            {
                return mustCastCore(op2, GetExprFactory().MakeClass(op1.type), 0);
            }
            else
            {
                return mustConvertCore(op2, GetExprFactory().MakeClass(op1.type));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Bind the simple assignment operator =.

        public EXPR bindAssignment(EXPR op1, EXPR op2, bool allowExplicit)
        {
            bool fOp2NotAddrOp = false;
            bool fOp2WasCast = false;

            if (!op1.isANYLOCAL_OK())
            {
                if (!checkLvalue(op1, CheckLvalueKind.Assignment))
                {
                    EXPR rval = GetExprFactory().CreateAssignment(op1, op2);
                    rval.SetError();
                    return rval;
                }
            }
            else
            {
                if (op2.type.IsArrayType())
                {
                    return BindPtrToArray(op1.asANYLOCAL(), op2);
                }
                if (op2.type == GetReqPDT(PredefinedType.PT_STRING))
                {
                    op2 = bindPtrToString(op2);
                }
                else if (op2.kind == ExpressionKind.EK_ADDR)
                {
                    op2.flags |= EXPRFLAG.EXF_ADDRNOCONV;
                }
                else if (op2.isOK())
                {
                    fOp2NotAddrOp = true;
                    fOp2WasCast = (op2.isCAST());
                }
            }

            op2 = GenerateAssignmentConversion(op1, op2, allowExplicit);
            if (op2.isOK() && fOp2NotAddrOp)
            {
                // Only report these errors if the convert succeeded
                if (fOp2WasCast)
                {
                    ErrorContext.Error(ErrorCode.ERR_BadCastInFixed);
                }
                else
                {
                    ErrorContext.Error(ErrorCode.ERR_FixedNotNeeded);
                }
            }
            return GenerateOptimizedAssignment(op1, op2);
        }

        internal EXPR BindArrayIndexCore(BindingFlag bindFlags, EXPR pOp1, EXPR pOp2)
        {
            EXPR pExpr;
            bool bIsError = false;
            if (!pOp1.isOK() || !pOp2.isOK())
            {
                bIsError = true;
            }

            CType pIntType = GetReqPDT(PredefinedType.PT_INT);

            // Array indexing must occur on an array type.
            if (!pOp1.type.IsArrayType())
            {
                Debug.Assert(!pOp1.type.IsPointerType());
                pExpr = bindIndexer(pOp1, pOp2, bindFlags);
                if (bIsError)
                {
                    pExpr.SetError();
                }
                return pExpr;
            }
            ArrayType pArrayType = pOp1.type.AsArrayType();
            checkUnsafe(pArrayType.GetElementType()); // added to the binder so we don't bind to pointer ops
            // Check the rank of the array against the number of indices provided, and
            // convert the indexes to ints

            CType pDestType = chooseArrayIndexType(pOp2);

            if (null == pDestType)
            {
                // using int as the type will allow us to give a better error...
                pDestType = pIntType;
            }

            int rank = pArrayType.rank;
            int cIndices = 0;

            EXPR transformedIndices = pOp2.Map(GetExprFactory(),
                (EXPR x) =>
                {
                    cIndices++;
                    EXPR pTemp = mustConvert(x, pDestType);
                    if (pDestType == pIntType)
                        return pTemp;
#if CSEE
                    EXPRFLAG flag = 0;
#else
                    EXPRFLAG flag = EXPRFLAG.EXF_INDEXEXPR;
#endif
                    EXPRCLASS exprType = GetExprFactory().MakeClass(pDestType);
                    return GetExprFactory().CreateCast(flag, exprType, pTemp);
                });

            if (cIndices != rank)
            {
                ErrorContext.Error(ErrorCode.ERR_BadIndexCount, rank);
                pExpr = GetExprFactory().CreateArrayIndex(pOp1, transformedIndices);
                pExpr.SetError();
                return pExpr;
            }

            // Allocate a new expression, the type is the element type of the array.
            // Array index operations are always lvalues.
            pExpr = GetExprFactory().CreateArrayIndex(pOp1, transformedIndices);
            pExpr.flags |= EXPRFLAG.EXF_LVALUE | EXPRFLAG.EXF_ASSGOP;

            if (bIsError)
            {
                pExpr.SetError();
            }

            return pExpr;
        }

        private EXPRUNARYOP bindPtrToString(EXPR @string)
        {
            CType typeRet = GetTypes().GetPointer(GetReqPDT(PredefinedType.PT_CHAR));

            return GetExprFactory().CreateUnaryOp(ExpressionKind.EK_ADDR, typeRet, @string);
        }

        private EXPRQUESTIONMARK BindPtrToArray(EXPRLOCAL exprLoc, EXPR array)
        {
            CType typeElem = array.type.AsArrayType().GetElementType();
            CType typePtrElem = GetTypes().GetPointer(typeElem);

            // element must be unmanaged...
            if (GetSymbolLoader().isManagedType(typeElem))
            {
                ErrorContext.Error(ErrorCode.ERR_ManagedAddr, typeElem);
            }

            SetExternalRef(typeElem);

            EXPR test = null;
            // we need to wrap the array so we can effectively generate something like this:
            // (((temp = array) != null && temp.Length > 0) ? loc = temp[0] : loc = null)
            // NOTE: The assignment needs to be inside the ExpressionKind.EK_QUESTIONMARK.
            // We can't do loc = (... ? ... : ...) since the CLR type of temp[0] is a managed
            // pointer and null is a UIntPtr - which confuses the JIT. We can't just convert
            // temp[0] to UIntPtr with a conv.u instruction because then if a GC occurs between
            // the time of the cast and the assignment to the local, we're toast.
            EXPRWRAP wrapArray = WrapShortLivedExpression(array).asWRAP();
            EXPR save = GetExprFactory().CreateSave(wrapArray);
            EXPR nullTest = GetExprFactory().CreateBinop(ExpressionKind.EK_NE, GetReqPDT(PredefinedType.PT_BOOL), save, GetExprFactory().CreateConstant(wrapArray.type, ConstValFactory.GetInt(0)));
            EXPR lenTest;

            if (array.type.AsArrayType().rank == 1)
            {
                EXPR len = GetExprFactory().CreateArrayLength(wrapArray);
                lenTest = GetExprFactory().CreateBinop(ExpressionKind.EK_NE, GetReqPDT(PredefinedType.PT_BOOL), len, GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), ConstValFactory.GetInt(0)));
            }
            else
            {
                EXPRCALL call = BindPredefMethToArgs(PREDEFMETH.PM_ARRAY_GETLENGTH, wrapArray, null, null, null);
                lenTest = GetExprFactory().CreateBinop(ExpressionKind.EK_NE, GetReqPDT(PredefinedType.PT_BOOL), call, GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), ConstValFactory.GetInt(0)));
            }

            test = GetExprFactory().CreateBinop(ExpressionKind.EK_LOGAND, GetReqPDT(PredefinedType.PT_BOOL), nullTest, lenTest);

            EXPR list = null;
            EXPR pList = list;
            EXPR pLastList = null;
            for (int cc = 0; cc < array.type.AsArrayType().rank; cc++)
            {
                GetExprFactory().AppendItemToList(GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), ConstValFactory.GetInt(0)), ref pList, ref pLastList);
            }
            Debug.Assert(list != null);

            EXPR exprAddr = GetExprFactory().CreateUnaryOp(ExpressionKind.EK_ADDR, typePtrElem, GetExprFactory().CreateArrayIndex(wrapArray, list));
            exprAddr.flags |= EXPRFLAG.EXF_ADDRNOCONV;
            exprAddr = mustConvert(exprAddr, exprLoc.type, CONVERTTYPE.NOUDC);
            exprAddr = GetExprFactory().CreateAssignment(exprLoc, exprAddr);
            exprAddr.flags |= EXPRFLAG.EXF_ASSGOP;
            exprAddr = GetExprFactory().CreateBinop(ExpressionKind.EK_SEQREV, exprLoc.type, exprAddr, WrapShortLivedExpression(wrapArray)); // free the temp

            EXPR exprnull = GetExprFactory().CreateZeroInit(exprLoc.type);
            exprnull = GetExprFactory().CreateAssignment(exprLoc, exprnull);
            exprnull.flags |= EXPRFLAG.EXF_ASSGOP;

            EXPRBINOP exprRes = GetExprFactory().CreateBinop(ExpressionKind.EK_BINOP, exprAddr.type, exprAddr, exprnull);
            return GetExprFactory().CreateQuestionMark(test, exprRes);
        }


        private EXPR bindIndexer(EXPR pObject, EXPR args, BindingFlag bindFlags)
        {
            CType type = pObject.type;

            if (!type.IsAggregateType() && !type.IsTypeParameterType())
            {
                ErrorContext.Error(ErrorCode.ERR_BadIndexLHS, type);
                MethWithInst mwi = new MethWithInst(null, null);
                EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(pObject, mwi);
                EXPRCALL rval = GetExprFactory().CreateCall(0, type, args, pMemGroup, null);
                rval.SetError();
                return rval;
            }

            Name pName = GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_INDEXERINTERNAL);

            MemberLookup mem = new MemberLookup();
            if (!mem.Lookup(GetSemanticChecker(), type, pObject, ContextForMemberLookup(), pName, 0,
                            (bindFlags & BindingFlag.BIND_BASECALL) != 0 ? (MemLookFlags.BaseCall | MemLookFlags.Indexer) : MemLookFlags.Indexer))
            {
                mem.ReportErrors();
                type = GetTypes().GetErrorSym();
                Symbol pSymbol = null;

                if (mem.SwtInaccessible().Sym != null)
                {
                    Debug.Assert(mem.SwtInaccessible().Sym.IsMethodOrPropertySymbol());
                    type = mem.SwtInaccessible().MethProp().RetType;
                    pSymbol = mem.SwtInaccessible().Sym;
                }

                EXPRMEMGRP memgrp = null;

                if (pSymbol != null)
                {
                    memgrp = GetExprFactory().CreateMemGroup((EXPRFLAG)mem.GetFlags(),
                        pName, BSYMMGR.EmptyTypeArray(), pSymbol.getKind(), mem.GetSourceType(), null/*pMPS*/, mem.GetObject(), mem.GetResults());
                    memgrp.SetInaccessibleBit();
                }
                else
                {
                    MethWithInst mwi = new MethWithInst(null, null);
                    memgrp = GetExprFactory().CreateMemGroup(mem.GetObject(), mwi);
                }

                EXPRCALL rval = GetExprFactory().CreateCall(0, type, args, memgrp, null);
                rval.SetError();
                return rval;
            }

            Debug.Assert(mem.SymFirst().IsPropertySymbol() && mem.SymFirst().AsPropertySymbol().isIndexer());

            EXPRMEMGRP grp = GetExprFactory().CreateMemGroup((EXPRFLAG)mem.GetFlags(),
                pName, BSYMMGR.EmptyTypeArray(), mem.SymFirst().getKind(), mem.GetSourceType(), null/*pMPS*/, mem.GetObject(), mem.GetResults());

            EXPR pResult = BindMethodGroupToArguments(bindFlags, grp, args);
            Debug.Assert(pResult.HasObject());
            if (pResult.getObject() == null)
            {
                // We must be in an error scenario where the object was not allowed. 
                // This can happen if the user tries to access the indexer off the
                // type and not an instance or if the incorrect type/number of arguments 
                // were passed for binding.
                pResult.SetObject(pObject);
                pResult.SetError();
            }
            return pResult;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a cast node with the given expression flags. 
        private void bindSimpleCast(EXPR exprSrc, EXPRTYPEORNAMESPACE typeDest, out EXPR pexprDest)
        {
            bindSimpleCast(exprSrc, typeDest, out pexprDest, 0);
        }

        private void bindSimpleCast(EXPR exprSrc, EXPRTYPEORNAMESPACE exprTypeDest, out EXPR pexprDest, EXPRFLAG exprFlags)
        {
            Debug.Assert(exprTypeDest != null);
            Debug.Assert(exprTypeDest.TypeOrNamespace != null);
            Debug.Assert(exprTypeDest.TypeOrNamespace.IsType());
            CType typeDest = exprTypeDest.TypeOrNamespace.AsType();
            pexprDest = null;
            // If the source is a constant, and cast is really simple (no change in fundamental
            // type, no flags), then create a new constant node with the new type instead of
            // creating a cast node. This allows compile-time constants to be easily recognized.
            EXPR exprConst = exprSrc.GetConst();

            // Make the cast expr anyway, and if we find that we have a constant, then set the cast expr
            // as the original tree for the constant. Otherwise, return the cast expr.

            EXPRCAST exprCast = GetExprFactory().CreateCast(exprFlags, exprTypeDest, exprSrc);
            if (Context.CheckedNormal)
            {
                exprCast.flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
            }

            // Check if we have a compile time constant. If we do, create a constant for it and set the
            // original tree to the cast.

            if (exprConst != null && exprFlags == 0 &&
                exprSrc.type.fundType() == typeDest.fundType() &&
                (!exprSrc.type.isPredefType(PredefinedType.PT_STRING) || exprConst.asCONSTANT().getVal().IsNullRef()))
            {
                EXPRCONSTANT expr = GetExprFactory().CreateConstant(typeDest, exprConst.asCONSTANT().getVal());
                pexprDest = expr;
                return;
            }

            pexprDest = exprCast;
            Debug.Assert(exprCast.GetArgument() != null);
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Binds a call to a method, return type is an error or an EXPRCALL.
        //
        // tree      - ParseTree for error messages
        // pObject    - pObject to call method on
        // pmwi      - Meth to bind to. This will be morphed when we remap to an override.
        // args      - arguments
        // exprFlags - Flags to put on the generated expr

        private EXPRCALL BindToMethod(MethWithInst mwi, EXPR pArguments, EXPRMEMGRP pMemGroup, MemLookFlags flags)
        {
            Debug.Assert(mwi.Sym != null && mwi.Sym.IsMethodSymbol() && (!mwi.Meth().isOverride || mwi.Meth().isHideByName));
            Debug.Assert(pMemGroup != null);

            bool fConstrained;
            bool bIsMatchingStatic;
            EXPR pObject = pMemGroup.GetOptionalObject();
            CType callingObjectType = pObject != null ? pObject.type : null;
            PostBindMethod((flags & MemLookFlags.BaseCall) != 0, ref mwi, pObject);
            pObject = AdjustMemberObject(mwi, pObject, out fConstrained, out bIsMatchingStatic);
            pMemGroup.SetOptionalObject(pObject);

            CType pReturnType = null;
            if ((flags & (MemLookFlags.Ctor | MemLookFlags.NewObj)) == (MemLookFlags.Ctor | MemLookFlags.NewObj))
            {
                pReturnType = mwi.Ats;
            }
            else
            {
                pReturnType = GetTypes().SubstType(mwi.Meth().RetType, mwi.GetType(), mwi.TypeArgs);
            }

            EXPRCALL pResult = GetExprFactory().CreateCall(0, pReturnType, pArguments, pMemGroup, mwi);
            if (!bIsMatchingStatic)
            {
                pResult.SetMismatchedStaticBit();
            }

            if (!pResult.isOK())
            {
                return pResult;
            }

            // Set the return type and flags for constructors.
            if ((flags & MemLookFlags.Ctor) != 0)
            {
                if ((flags & MemLookFlags.NewObj) != 0)
                {
                    pResult.flags |= EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CANTBENULL;
                }
                else
                {
                    Debug.Assert(pResult.type == getVoidType());
                }
            }

            if ((flags & MemLookFlags.BaseCall) != 0)
            {
                pResult.flags |= EXPRFLAG.EXF_BASECALL;
            }
            else if (fConstrained && pObject != null)
            {
                // Use the constrained prefix.
                pResult.flags |= EXPRFLAG.EXF_CONSTRAINED;
            }

            verifyMethodArgs(pResult, callingObjectType);

            return pResult;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Construct the EXPR node which corresponds to a field expression
        // for a given field and pObject pointer.

        internal EXPR BindToField(EXPR pObject, FieldWithType fwt, BindingFlag bindFlags)
        {
            return BindToField(pObject, fwt, bindFlags, null/*OptionalLHS*/);
        }

        ////////////////////////////////////////////////////////////////////////////////

        private EXPR BindToField(EXPR pOptionalObject, FieldWithType fwt, BindingFlag bindFlags, EXPR pOptionalLHS)
        {
            Debug.Assert(fwt.GetType() != null && fwt.Field().getClass() == fwt.GetType().getAggregate());

            CType pFieldType = GetTypes().SubstType(fwt.Field().GetType(), fwt.GetType());
            if (pOptionalObject != null && !pOptionalObject.isOK())
            {
                EXPRFIELD pField = GetExprFactory().CreateField(0, pFieldType, pOptionalObject, 0, fwt, pOptionalLHS);
                pField.SetError();
                return pField;
            }

            EXPR pOriginalObject = pOptionalObject;
            bool bIsMatchingStatic;
            bool pfConstrained;
            pOptionalObject = AdjustMemberObject(fwt, pOptionalObject, out pfConstrained, out bIsMatchingStatic);

            checkUnsafe(pFieldType); // added to the binder so we don't bind to pointer ops

            EXPRFIELD pResult;
            {
                bool isLValue = false;
                if ((pOptionalObject != null && pOptionalObject.type.IsPointerType()) || objectIsLvalue(pOptionalObject))
                {
                    isLValue = true;
                }
                // Exception: a readonly field is not an lvalue unless we're in the constructor/static constructor appropriate
                // for the field.
                if (RespectReadonly() && fwt.Field().isReadOnly)
                {
                    if (ContainingAgg() == null ||
                        !InMethod() || !InConstructor() ||
                        fwt.Field().getClass() != ContainingAgg() ||
                        InStaticMethod() != fwt.Field().isStatic ||
                        (pOptionalObject != null && !isThisPointer(pOptionalObject)) ||
                        InAnonymousMethod())
                    {
                        isLValue = false;
                    }
                }

                pResult = GetExprFactory().CreateField(isLValue ? EXPRFLAG.EXF_LVALUE : 0, pFieldType, pOptionalObject, 0, fwt, pOptionalLHS);
                if (!bIsMatchingStatic)
                {
                    pResult.SetMismatchedStaticBit();
                }

                if (pFieldType.IsErrorType())
                {
                    pResult.SetError();
                }
                Debug.Assert(BindingFlag.BIND_MEMBERSET == (BindingFlag)EXPRFLAG.EXF_MEMBERSET);
                pResult.flags |= (EXPRFLAG)(bindFlags & BindingFlag.BIND_MEMBERSET);
            }

            // If this field is the backing field of a WindowsRuntime event then we need to bind to its
            // invocationlist property which is a delegate containing all the handlers.
            if (pResult.isFIELD() &&
                fwt.Field().isEvent &&
                fwt.Field().getEvent(GetSymbolLoader()) != null &&
                fwt.Field().getEvent(GetSymbolLoader()).IsWindowsRuntimeEvent)
            {
                CType fieldType = fwt.Field().GetType();
                if (fieldType.IsAggregateType())
                {
                    // Access event backing field (EventRegistrationTokenTable<T>) using
                    // EventRegistrationTokenTable<T>.GetOrCreateEventRegistrationTokenTable()
                    // to ensure non-null
                    pResult.setType(GetTypes().GetParameterModifier(pResult.type, false));

                    Name getOrCreateMethodName = GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_GETORCREATEEVENTREGISTRATIONTOKENTABLE);
                    GetSymbolLoader().RuntimeBinderSymbolTable.PopulateSymbolTableWithName(getOrCreateMethodName.Text, null, fieldType.AssociatedSystemType);
                    MethodSymbol getOrCreateMethod = GetSymbolLoader().LookupAggMember(getOrCreateMethodName, fieldType.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol();

                    MethPropWithInst getOrCreatempwi = new MethPropWithInst(getOrCreateMethod, fieldType.AsAggregateType());
                    EXPRMEMGRP getOrCreateGrp = GetExprFactory().CreateMemGroup(null, getOrCreatempwi);

                    EXPR getOrCreateCall = BindToMethod(new MethWithInst(getOrCreatempwi),
                                                        pResult,
                                                        getOrCreateGrp,
                                                        (MemLookFlags)MemLookFlags.None);

                    AggregateSymbol fieldTypeSymbol = fieldType.AsAggregateType().GetOwningAggregate();
                    Name invocationListName = GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_INVOCATIONLIST);

                    // InvocationList might not be populated in the symbol table as no one would have called it.
                    GetSymbolLoader().RuntimeBinderSymbolTable.PopulateSymbolTableWithName(invocationListName.Text, null, fieldType.AssociatedSystemType);
                    PropertySymbol invocationList = GetSymbolLoader().LookupAggMember(
                                                        invocationListName,
                                                        fieldTypeSymbol,
                                                        symbmask_t.MASK_PropertySymbol).AsPropertySymbol();

                    MethPropWithInst mpwi = new MethPropWithInst(invocationList, fieldType.AsAggregateType());
                    EXPRMEMGRP memGroup = GetExprFactory().CreateMemGroup(getOrCreateCall, mpwi);

                    PropWithType pwt = new PropWithType(invocationList, fieldType.AsAggregateType());
                    EXPR propertyExpr = BindToProperty(getOrCreateCall, pwt, bindFlags, null, null, memGroup);
                    return propertyExpr;
                }
            }

            return pResult;
        }

        ////////////////////////////////////////////////////////////////////////////////

        internal EXPR BindToProperty(EXPR pObject, PropWithType pwt, BindingFlag bindFlags, EXPR args, AggregateType pOtherType, EXPRMEMGRP pMemGroup)
        {
            Debug.Assert(pwt.Sym != null &&
                    pwt.Sym.IsPropertySymbol() &&
                    pwt.GetType() != null &&
                    pwt.Prop().getClass() == pwt.GetType().getAggregate());
            Debug.Assert(pwt.Prop().Params.size == 0 || pwt.Prop().isIndexer());
            Debug.Assert(pOtherType == null ||
                    !pwt.Prop().isIndexer() &&
                    pOtherType.getAggregate() == pwt.Prop().RetType.getAggregate());

            bool fConstrained;
            MethWithType mwtGet;
            MethWithType mwtSet;
            EXPR pObjectThrough = null;

            // We keep track of the type of the pObject which we're doing the call through so that we can report 
            // protection access errors later, either below when binding the get, or later when checking that
            // the setter is actually an lvalue.  If we're actually doing a base.prop call then we do not
            // need to ensure that the left side of the dot is an instance of the derived class, otherwise
            // we save it away for later.
            if (0 == (bindFlags & BindingFlag.BIND_BASECALL))
            {
                pObjectThrough = pObject;
            }

            bool bIsMatchingStatic;
            PostBindProperty((bindFlags & BindingFlag.BIND_BASECALL) != 0, pwt, pObject, out mwtGet, out mwtSet);

            if (mwtGet &&
                    (!mwtSet ||
                     mwtSet.GetType() == mwtGet.GetType() ||
                     GetSymbolLoader().HasBaseConversion(mwtGet.GetType(), mwtSet.GetType())
                     )
                )
            {
                pObject = AdjustMemberObject(mwtGet, pObject, out fConstrained, out bIsMatchingStatic);
            }
            else if (mwtSet)
            {
                pObject = AdjustMemberObject(mwtSet, pObject, out fConstrained, out bIsMatchingStatic);
            }
            else
            {
                pObject = AdjustMemberObject(pwt, pObject, out fConstrained, out bIsMatchingStatic);
            }
            pMemGroup.SetOptionalObject(pObject);

            CType pReturnType = GetTypes().SubstType(pwt.Prop().RetType, pwt.GetType());
            Debug.Assert(pOtherType == pReturnType || pOtherType == null);

            if (pObject != null && !pObject.isOK())
            {
                EXPRPROP pResult = GetExprFactory().CreateProperty(pReturnType, pObjectThrough, args, pMemGroup, pwt, null, null);
                if (!bIsMatchingStatic)
                {
                    pResult.SetMismatchedStaticBit();
                }
                pResult.SetError();
                return pResult;
            }

            // if we are doing a get on this thing, and there is no get, and
            // most importantly, we are not leaving the arguments to be bound by the array index
            // then error...
            if ((bindFlags & BindingFlag.BIND_RVALUEREQUIRED) != 0)
            {
                if (!mwtGet)
                {
                    if (pOtherType != null)
                    {
                        return GetExprFactory().MakeClass(pOtherType);
                    }
                    ErrorContext.ErrorRef(ErrorCode.ERR_PropertyLacksGet, pwt);
                }
                else if (((bindFlags & BindingFlag.BIND_BASECALL) != 0) && mwtGet.Meth().isAbstract)
                {
                    // if the get exists, but is abstract, forbid the call as well...
                    if (pOtherType != null)
                    {
                        return GetExprFactory().MakeClass(pOtherType);
                    }
                    ErrorContext.Error(ErrorCode.ERR_AbstractBaseCall, pwt);
                }
                else
                {
                    CType type = null;
                    if (pObjectThrough != null)
                    {
                        type = pObjectThrough.type;
                    }

                    ACCESSERROR error = SemanticChecker.CheckAccess2(mwtGet.Meth(), mwtGet.GetType(), ContextForMemberLookup(), type);
                    if (error != ACCESSERROR.ACCESSERROR_NOERROR)
                    {
                        // if the get exists, but is not accessible, give an error.
                        if (pOtherType != null)
                        {
                            return GetExprFactory().MakeClass(pOtherType);
                        }

                        if (error == ACCESSERROR.ACCESSERROR_NOACCESSTHRU)
                        {
                            ErrorContext.Error(ErrorCode.ERR_BadProtectedAccess, pwt, type, ContextForMemberLookup());
                        }
                        else
                        {
                            ErrorContext.ErrorRef(ErrorCode.ERR_InaccessibleGetter, pwt);
                        }
                    }
                }
            }

            EXPRPROP result = GetExprFactory().CreateProperty(pReturnType, pObjectThrough, args, pMemGroup, pwt, mwtGet, mwtSet);
            if (!bIsMatchingStatic)
            {
                result.SetMismatchedStaticBit();
            }

            Debug.Assert(EXPRFLAG.EXF_BASECALL == (EXPRFLAG)BindingFlag.BIND_BASECALL);
            if ((EXPRFLAG.EXF_BASECALL & (EXPRFLAG)bindFlags) != 0)
            {
                result.flags |= EXPRFLAG.EXF_BASECALL;
            }
            else if (fConstrained && pObject != null)
            {
                // Use the constrained prefix.
                result.flags |= EXPRFLAG.EXF_CONSTRAINED;
            }

            if (result.GetOptionalArguments() != null)
            {
                verifyMethodArgs(result, pObjectThrough != null ? pObjectThrough.type : null);
            }

            if (mwtSet && objectIsLvalue(result.GetMemberGroup().GetOptionalObject()))
            {
                result.flags |= EXPRFLAG.EXF_LVALUE;
            }
            if (pOtherType != null)
            {
                result.flags |= EXPRFLAG.EXF_SAMENAMETYPE;
            }

            return result;
        }

        internal EXPR bindUDUnop(ExpressionKind ek, EXPR arg)
        {
            Name pName = ekName(ek);
            Debug.Assert(pName != null);

            CType typeSrc = arg.type;

        LAgain:
            switch (typeSrc.GetTypeKind())
            {
                case TypeKind.TK_NullableType:
                    typeSrc = typeSrc.StripNubs();
                    goto LAgain;
                case TypeKind.TK_TypeParameterType:
                    typeSrc = typeSrc.AsTypeParameterType().GetEffectiveBaseClass();
                    goto LAgain;
                case TypeKind.TK_AggregateType:
                    if (!typeSrc.isClassType() && !typeSrc.isStructType() || typeSrc.AsAggregateType().getAggregate().IsSkipUDOps())
                        return null;
                    break;
                default:
                    return null;
            }

            ArgInfos info = new ArgInfos();

            info.carg = 1;
            FillInArgInfoFromArgList(info, arg);

            List<CandidateFunctionMember> methFirstList = new List<CandidateFunctionMember>();
            MethodSymbol methCur = null;
            AggregateType atsCur = typeSrc.AsAggregateType();

            for (; ;)
            {
                // Find the next operator.
                methCur = (methCur == null) ?
                          GetSymbolLoader().LookupAggMember(pName, atsCur.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol() :
                          GetSymbolLoader().LookupNextSym(methCur, atsCur.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol();

                if (methCur == null)
                {
                    // Find the next type.
                    // If we've found some applicable methods in a class then we don't need to look any further.
                    if (!methFirstList.IsEmpty())
                        break;
                    atsCur = atsCur.GetBaseClass();
                    if (atsCur == null)
                        break;
                    continue;
                }

                // Only look at operators with 1 args.
                if (!methCur.isOperator || methCur.Params.size != 1)
                    continue;
                Debug.Assert(methCur.typeVars.size == 0);

                TypeArray paramsCur = GetTypes().SubstTypeArray(methCur.Params, atsCur);
                CType typeParam = paramsCur.Item(0);
                NullableType nubParam;

                if (canConvert(arg, typeParam))
                {
                    methFirstList.Add(new CandidateFunctionMember(
                                    new MethPropWithInst(methCur, atsCur, BSYMMGR.EmptyTypeArray()),
                                    paramsCur,
                                    0,
                                    false));
                }
                else if (GetSymbolLoader().FCanLift() && typeParam.IsNonNubValType() &&
                         GetTypes().SubstType(methCur.RetType, atsCur).IsNonNubValType() &&
                         canConvert(arg, nubParam = GetTypes().GetNullable(typeParam)))
                {
                    methFirstList.Add(new CandidateFunctionMember(
                                    new MethPropWithInst(methCur, atsCur, BSYMMGR.EmptyTypeArray()),
                                    GetGlobalSymbols().AllocParams(1, new CType[] { nubParam }),
                                    1,
                                    false));
                }
            }

            if (methFirstList.IsEmpty())
                return null;

            CandidateFunctionMember pmethAmbig1;
            CandidateFunctionMember pmethAmbig2;
            CandidateFunctionMember pmethBest = FindBestMethod(methFirstList, null, info, out pmethAmbig1, out pmethAmbig2);

            if (pmethBest == null)
            {
                // No winner, so its an ambiguous call...
                ErrorContext.Error(ErrorCode.ERR_AmbigCall, pmethAmbig1.mpwi, pmethAmbig2.mpwi);

                EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, pmethAmbig1.mpwi);
                EXPRCALL rval = GetExprFactory().CreateCall(0, null, arg, pMemGroup, null);
                rval.SetError();
                return rval;
            }

            if (SemanticChecker.CheckBogus(pmethBest.mpwi.Meth()))
            {
                ErrorContext.ErrorRef(ErrorCode.ERR_BindToBogus, pmethBest.mpwi);

                EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, pmethBest.mpwi);
                EXPRCALL rval = GetExprFactory().CreateCall(0, null, arg, pMemGroup, null);
                rval.SetError();
                return rval;
            }

            EXPRCALL call;

            if (pmethBest.ctypeLift != 0)
            {
                call = BindLiftedUDUnop(arg, pmethBest.@params.Item(0), pmethBest.mpwi);
            }
            else
            {
                call = BindUDUnopCall(arg, pmethBest.@params.Item(0), pmethBest.mpwi);
            }

            return GetExprFactory().CreateUserDefinedUnaryOperator(ek, call.type, arg, call, pmethBest.mpwi);
        }

        private EXPRCALL BindLiftedUDUnop(EXPR arg, CType typeArg, MethPropWithInst mpwi)
        {
            CType typeRaw = typeArg.StripNubs();
            if (!arg.type.IsNullableType() || !canConvert(arg.type.StripNubs(), typeRaw, CONVERTTYPE.NOUDC))
            {
                // Convert then lift.
                arg = mustConvert(arg, typeArg);
            }
            Debug.Assert(arg.type.IsNullableType());

            CType typeRet = GetTypes().SubstType(mpwi.Meth().RetType, mpwi.GetType());
            if (!typeRet.IsNullableType())
            {
                typeRet = GetTypes().GetNullable(typeRet);
            }

            // First bind the non-lifted version for errors.
            EXPR nonLiftedArg = mustCast(arg, typeRaw);
            EXPRCALL nonLiftedResult = BindUDUnopCall(nonLiftedArg, typeRaw, mpwi);

            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mpwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, typeRet, arg, pMemGroup, null);
            call.mwi = new MethWithInst(mpwi);
            call.castOfNonLiftedResultToLiftedType = mustCast(nonLiftedResult, typeRet, 0);
            call.nubLiftKind = NullableCallLiftKind.Operator;
            return call;
        }

        private EXPRCALL BindUDUnopCall(EXPR arg, CType typeArg, MethPropWithInst mpwi)
        {
            CType typeRet = GetTypes().SubstType(mpwi.Meth().RetType, mpwi.GetType());
            checkUnsafe(typeRet); // added to the binder so we don't bind to pointer ops
            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mpwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, typeRet, mustConvert(arg, typeArg), pMemGroup, null);
            call.mwi = new MethWithInst(mpwi);
            verifyMethodArgs(call, mpwi.GetType());
            return call;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Given a method group or indexer group, bind it to the arguments for an 
        // invocation. This method can change the arguments to bind with Extension 
        // Methods
        private bool BindMethodGroupToArgumentsCore(out GroupToArgsBinderResult pResults, BindingFlag bindFlags, EXPRMEMGRP grp, ref EXPR args, int carg, bool bindingCollectionAdd, bool bHasNamedArgumentSpecifiers)
        {
            ArgInfos pargInfo = new ArgInfos {carg = carg};
            FillInArgInfoFromArgList(pargInfo, args);

            ArgInfos pOriginalArgInfo = new ArgInfos {carg = carg};
            FillInArgInfoFromArgList(pOriginalArgInfo, args);

            GroupToArgsBinder binder = new GroupToArgsBinder(this, bindFlags, grp, pargInfo, pOriginalArgInfo, bHasNamedArgumentSpecifiers, null/*atsDelegate*/);
            bool retval = bindingCollectionAdd ? binder.BindCollectionAddArgs() : binder.Bind(true /*ReportErrors*/);

            pResults = binder.GetResultsOfBind();
            return retval;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Given a method group or indexer group, bind it to the arguments for an 
        // invocation.
        internal EXPR BindMethodGroupToArguments(BindingFlag bindFlags, EXPRMEMGRP grp, EXPR args)
        {
            Debug.Assert(grp.sk == SYMKIND.SK_MethodSymbol || grp.sk == SYMKIND.SK_PropertySymbol && ((grp.flags & EXPRFLAG.EXF_INDEXER) != 0));

            // Count the args.
            bool argTypeErrors;
            int carg = CountArguments(args, out argTypeErrors);
            // We need to store the object because BindMethodGroupToArgumentsCore will 
            // null it out in the case of an extension method, which is then consumed
            // by BindToMethod. After that, we want to set the object back.
            EXPR pObject = grp.GetOptionalObject();

            // If we weren't given a pName, then we couldn't bind the method pName, so we should
            // just bail out of here.

            if (grp.name == null)
            {
                EXPRCALL rval = GetExprFactory().CreateCall(0, GetTypes().GetErrorSym(), args, grp, null);
                rval.SetError();
                return rval;
            }

            // If we have named arguments specified, make sure we have them all appearing after 
            // fixed arguments.
            bool bSeenNamed = false;
            if (!VerifyNamedArgumentsAfterFixed(args, out bSeenNamed))
            {
                EXPRCALL rval = GetExprFactory().CreateCall(0, GetTypes().GetErrorSym(), args, grp, null);
                rval.SetError();
                return rval;
            }

            GroupToArgsBinderResult result;
            if (!BindMethodGroupToArgumentsCore(out result, bindFlags, grp, ref args, carg, false, bSeenNamed))
            {
                Debug.Assert(false, "Why didn't BindMethodGroupToArgumentsCore throw an error?");
                return null;
            }

            EXPR exprRes;
            MethPropWithInst mpwiBest = result.GetBestResult();

            if (grp.sk == SYMKIND.SK_PropertySymbol)
            {
                Debug.Assert((grp.flags & EXPRFLAG.EXF_INDEXER) != 0);
                //PropWithType pwt = new PropWithType(mpwiBest.Prop(), mpwiBest.GetType());

                exprRes = BindToProperty(grp.GetOptionalObject(), new PropWithType(mpwiBest), (bindFlags | (BindingFlag)(grp.flags & EXPRFLAG.EXF_BASECALL)), args, null/*typeOther*/, grp);
            }
            else
            {
                exprRes = BindToMethod(new MethWithInst(mpwiBest), args, grp, (MemLookFlags)grp.flags);
            }
            return exprRes;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private bool VerifyNamedArgumentsAfterFixed(EXPR args, out bool seenNamed)
        {
            EXPR list = args;
            seenNamed = false;
            while (list != null)
            {
                EXPR arg;
                if (list.isLIST())
                {
                    arg = list.asLIST().GetOptionalElement();
                    list = list.asLIST().GetOptionalNextListNode();
                }
                else
                {
                    arg = list;
                    list = null;
                }

                Debug.Assert(arg != null);
                if (arg.isNamedArgumentSpecification())
                {
                    seenNamed = true;
                }
                else
                {
                    if (seenNamed)
                    {
                        GetErrorContext().Error(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument);
                        return false;
                    }
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // This finds a method  and binds it to the args provided.

        private EXPRCALL BindPredefMethToArgs(PREDEFMETH predefMethod, EXPR obj, EXPR args, TypeArray clsTypeArgs, TypeArray methTypeArgs)
        {
            MethodSymbol methSym = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMethod);
            if (methSym == null)
            {
                MethWithInst mwi = new MethWithInst(null, null);
                EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(obj, mwi);
                EXPRCALL rval = GetExprFactory().CreateCall(0, null, args, pMemGroup, null);
                rval.SetError();
                return rval;
            }

            AggregateSymbol agg = methSym.getClass();
            if (clsTypeArgs == null)
            {
                clsTypeArgs = BSYMMGR.EmptyTypeArray();
            }
            AggregateType aggType = GetTypes().GetAggregate(agg, clsTypeArgs);

            MethPropWithInst mpwiBest = new MethPropWithInst(methSym, aggType, methTypeArgs);
            EXPRMEMGRP memgroup = GetExprFactory().CreateMemGroup(obj, mpwiBest);

            EXPRCALL exprRes = BindToMethod(new MethWithInst(mpwiBest), args, memgroup, (MemLookFlags)MemLookFlags.None);

            return exprRes;
        }
        ////////////////////////////////////////////////////////////////////////////////
        // Report a bad operator types error to the user.
        private EXPR BadOperatorTypesError(ExpressionKind ek, EXPR pOperand1, EXPR pOperand2)
        {
            return BadOperatorTypesError(ek, pOperand1, pOperand2, null);
        }

        private EXPR BadOperatorTypesError(ExpressionKind ek, EXPR pOperand1, EXPR pOperand2, CType pTypeErr)
        {
            // This is a hack, but we need to store the operation somewhere... the first argument's as 
            // good a place as any.
            string strOp = pOperand1.errorString;

            pOperand1 = UnwrapExpression(pOperand1);

            if (pOperand1 != null)
            {
                if (pOperand2 != null)
                {
                    pOperand2 = UnwrapExpression(pOperand2);
                    if (pOperand1.type != null &&
                            !pOperand1.type.IsErrorType() &&
                            pOperand2.type != null &&
                            !pOperand2.type.IsErrorType())
                    {
                        ErrorContext.Error(ErrorCode.ERR_BadBinaryOps, strOp, pOperand1.type, pOperand2.type);
                    }
                }
                else if (pOperand1.type != null && !pOperand1.type.IsErrorType())
                {
                    ErrorContext.Error(ErrorCode.ERR_BadUnaryOp, strOp, pOperand1.type);
                }
            }

            if (pTypeErr == null)
            {
                pTypeErr = GetReqPDT(PredefinedType.PT_OBJECT);
            }

            EXPR rval = GetExprFactory().CreateOperator(ek, pTypeErr, pOperand1, pOperand2);
            rval.SetError();
            return rval;
        }

        private EXPR UnwrapExpression(EXPR pExpression)
        {
            EXPR pExpr = pExpression;
            while (pExpr != null && pExpr.isWRAP() && pExpr.asWRAP().GetOptionalExpression() != null)
            {
                pExpr = pExpr.asWRAP().GetOptionalExpression();
            }

            return pExpr;
        }

        private static ErrorCode GetStandardLvalueError(CheckLvalueKind kind)
        {
            switch (kind)
            {
                default:
                    VSFAIL("bad kind");
                    return ErrorCode.ERR_AssgLvalueExpected;
                case CheckLvalueKind.Assignment:
                    return ErrorCode.ERR_AssgLvalueExpected;
                case CheckLvalueKind.OutParameter:
                    return ErrorCode.ERR_RefLvalueExpected;
                case CheckLvalueKind.Increment:
                    return ErrorCode.ERR_IncrementLvalueExpected;
            }
        }

        private void CheckLvalueProp(EXPRPROP prop)
        {
            Debug.Assert(prop != null);
            Debug.Assert(prop.isLvalue());

            // We have an lvalue property.  Give an error if this is an abstract property
            // or an inaccessible property.

            if (prop.isBaseCall() && prop.mwtSet.Meth().isAbstract)
            {
                ErrorContext.Error(ErrorCode.ERR_AbstractBaseCall, prop.mwtSet);
            }
            else
            {
                CType type = null;
                if (prop.GetOptionalObjectThrough() != null)
                {
                    type = prop.GetOptionalObjectThrough().type;
                }

                CheckPropertyAccess(prop.mwtSet, prop.pwtSlot, type);
            }
        }

        private bool CheckPropertyAccess(MethWithType mwt, PropWithType pwtSlot, CType type)
        {
            ACCESSERROR error = SemanticChecker.CheckAccess2(mwt.Meth(), mwt.GetType(), ContextForMemberLookup(), type);
            if (error == ACCESSERROR.ACCESSERROR_NOACCESSTHRU)
            {
                ErrorContext.Error(ErrorCode.ERR_BadProtectedAccess, pwtSlot, type, ContextForMemberLookup());
                return false;
            }
            else if (error == ACCESSERROR.ACCESSERROR_NOACCESS)
            {
                ErrorContext.Error(mwt.Meth().isSetAccessor() ? ErrorCode.ERR_InaccessibleSetter : ErrorCode.ERR_InaccessibleGetter, pwtSlot);
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // A false return means not to process the expr any further - it's totally out
        // of place. For example - a method group or an anonymous method.
        private bool checkLvalue(EXPR expr, CheckLvalueKind kind)
        {
            if (!expr.isOK())
                return false;
            if (expr.isLvalue())
            {
                if (expr.isPROP())
                {
                    CheckLvalueProp(expr.asPROP());
                }
                markFieldAssigned(expr);
                return true;
            }

            switch (expr.kind)
            {
                case ExpressionKind.EK_PROP:
                    if (kind == CheckLvalueKind.OutParameter)
                    {
                        // passing a property as ref or out
                        ErrorContext.Error(ErrorCode.ERR_RefProperty);
                        return true;
                    }
                    if (!expr.asPROP().mwtSet)
                    {
                        // Assigning to a property without a setter.
                        // If we have
                        // bool? b = true; (bool)b = false;
                        // then this is realized immediately as 
                        // b.Value = false; 
                        // and no ExpressionKind.EK_CAST is generated. We'd rather not give a "you're writing
                        // to a read-only property" error in the case where the property access
                        // is not explicit in the source code.  Fortunately in this case the
                        // cast is still hanging around in the parse tree, so we can look for it.

                        // POSSIBLE ERROR: It would be nice to also give this error for other situations
                        // POSSIBLE ERROR: in which the user is attempting to assign to a value, such as
                        // POSSIBLE ERROR: an explicit (bool)b.Value = false;
                        // POSSIBLE ERROR: Unfortunately we cannot use this trick in that situation because
                        // POSSIBLE ERROR: we've already discarded the OperatorKind.OP_CAST node.  (This is an SyntaxKind.Dot).

                        // SPEC VIOLATION: More generally:
                        // SPEC VIOLATION: The spec states that the result of any cast is a value, not a
                        // SPEC VIOLATION: variable. Unfortunately we do not correctly implement this
                        // SPEC VIOLATION: and we probably should not start implementing it because this
                        // SPEC VIOLATION: would be a breaking change.  We currently discard "no op" casts
                        // SPEC VIOLATION: very aggressively rather than generating an ExpressionKind.EK_CAST node.

                        ErrorContext.Error(ErrorCode.ERR_AssgReadonlyProp, expr.asPROP().pwtSlot);
                        return true;
                    }
                    break;

                case ExpressionKind.EK_ARRAYLENGTH:
                    if (kind == CheckLvalueKind.OutParameter)
                    {
                        // passing a property as ref or out
                        ErrorContext.Error(ErrorCode.ERR_RefProperty);
                    }
                    else
                    {
                        // Special case, the length property of an array
                        ErrorContext.Error(ErrorCode.ERR_AssgReadonlyProp, GetSymbolLoader().getPredefinedMembers().GetProperty(PREDEFPROP.PP_ARRAY_LENGTH));
                    }
                    return true;

                case ExpressionKind.EK_BOUNDLAMBDA:
                case ExpressionKind.EK_UNBOUNDLAMBDA:
                case ExpressionKind.EK_CONSTANT:
                    ErrorContext.Error(GetStandardLvalueError(kind));
                    return false;
                case ExpressionKind.EK_MEMGRP:
                    {
                        ErrorCode err = (kind == CheckLvalueKind.OutParameter) ? ErrorCode.ERR_RefReadonlyLocalCause : ErrorCode.ERR_AssgReadonlyLocalCause;
                        ErrorContext.Error(err, expr.asMEMGRP().name, new ErrArgIds(MessageID.MethodGroup));
                        return false;
                    }
                default:
                    break;
            }

            return !TryReportLvalueFailure(expr, kind);
        }

        private void PostBindMethod(bool fBaseCall, ref MethWithInst pMWI, EXPR pObject)
        {
            MethWithInst mwiOrig = pMWI;

            // If it is virtual, find a remap of the method to something more specific.  This
            // may alter where the method is found.
            if (pObject != null && (fBaseCall || pObject.type.isSimpleType() || pObject.type.isSpecialByRefType()))
            {
                RemapToOverride(GetSymbolLoader(), pMWI, pObject.type);
            }

            if (fBaseCall && pMWI.Meth().isAbstract)
            {
                ErrorContext.Error(ErrorCode.ERR_AbstractBaseCall, pMWI);
            }

            if (pMWI.Meth().RetType != null)
            {
                checkUnsafe(pMWI.Meth().RetType);
                bool fCheckParams = false;

                if (pMWI.Meth().isExternal)
                {
                    fCheckParams = true;
                    SetExternalRef(pMWI.Meth().RetType);
                }

                // We need to check unsafe on the parameters as well, since we cannot check in conversion.
                TypeArray pParams = pMWI.Meth().Params;

                for (int i = 0; i < pParams.size; i++)
                {
                    // This is an optimization: don't call this in the vast majority of cases
                    CType type = pParams.Item(i);

                    if (type.isUnsafe())
                    {
                        checkUnsafe(type);
                    }
                    if (fCheckParams && type.IsParameterModifierType())
                    {
                        SetExternalRef(type);
                    }
                }
            }
        }

        private void PostBindProperty(bool fBaseCall, PropWithType pwt, EXPR pObject, out MethWithType pmwtGet, out MethWithType pmwtSet)
        {
            pmwtGet = new MethWithType();
            pmwtSet = new MethWithType();
            // Get the accessors.
            if (pwt.Prop().methGet != null)
            {
                pmwtGet.Set(pwt.Prop().methGet, pwt.GetType());
            }
            else
            {
                pmwtGet.Clear();
            }

            if (pwt.Prop().methSet != null)
            {
                pmwtSet.Set(pwt.Prop().methSet, pwt.GetType());
            }
            else
            {
                pmwtSet.Clear();
            }

            // If it is virtual, find a remap of the method to something more specific.  This
            // may alter where the accessors are found.
            if (fBaseCall && pObject != null)
            {
                if (pmwtGet)
                {
                    RemapToOverride(GetSymbolLoader(), pmwtGet, pObject.type);
                }
                if (pmwtSet)
                {
                    RemapToOverride(GetSymbolLoader(), pmwtSet, pObject.type);
                }
            }

            if (pwt.Prop().RetType != null)
            {
                checkUnsafe(pwt.Prop().RetType);
            }
        }

        private EXPR AdjustMemberObject(SymWithType swt, EXPR pObject, out bool pfConstrained, out bool pIsMatchingStatic)
        {
            // Assert that the type is present and is an instantiation of the member's parent.
            Debug.Assert(swt.GetType() != null && swt.GetType().getAggregate() == swt.Sym.parent.AsAggregateSymbol());
            bool bIsMatchingStatic = IsMatchingStatic(swt, pObject);
            pIsMatchingStatic = bIsMatchingStatic;
            pfConstrained = false;

            bool isStatic = swt.Sym.isStatic;

            // If our static doesn't match, bail out of here.
            if (!bIsMatchingStatic)
            {
                if (isStatic)
                {
                    // If we have a mismatched static, a static method, and the binding flag
                    // that tells us we're binding simple names, then insert a type here instead.
                    if ((pObject.flags & EXPRFLAG.EXF_SIMPLENAME) != 0)
                    {
                        // We've made the static match now.
                        pIsMatchingStatic = true;
                        return null;
                    }
                    else
                    {
                        ErrorContext.ErrorRef(ErrorCode.ERR_ObjectProhibited, swt);
                        return null;
                    }
                }
                else
                {
                    ErrorContext.ErrorRef(ErrorCode.ERR_ObjectRequired, swt);
                    return pObject;
                }
            }

            // At this point, all errors for static invocations have been reported, and
            // the object has been nulled out. So return out of here.
            if (isStatic)
            {
                return null;
            }

            // If we're in a constructor, then bail.
            if (swt.Sym.IsMethodSymbol() && swt.Meth().IsConstructor())
            {
                return pObject;
            }

            if (pObject == null)
            {
                if (InFieldInitializer() && !InStaticMethod() && ContainingAgg() == swt.Sym.parent)
                {
                    ErrorContext.ErrorRef(ErrorCode.ERR_FieldInitRefNonstatic, swt); // give better error message for common mistake <BUGNUM>See VS7:119218</BUGNUM>
                }
                else if (InAnonymousMethod() && !InStaticMethod() && ContainingAgg() == swt.Sym.parent && ContainingAgg().IsStruct())
                {
                    ErrorContext.Error(ErrorCode.ERR_ThisStructNotInAnonMeth);
                }
                else
                {
                    return null;
                }

                // For fields or structs, make a this pointer for us to use.

                EXPRTHISPOINTER thisExpr = GetExprFactory().CreateThis(Context.GetThisPointer(), true);
                thisExpr.SetMismatchedStaticBit();
                if (thisExpr.type == null)
                {
                    thisExpr.setType(GetTypes().GetErrorSym());
                }
                return thisExpr;
            }

            CType typeObj = pObject.type;
            CType typeTmp;

            if (typeObj.IsNullableType() && (typeTmp = typeObj.AsNullableType().GetAts(GetErrorContext())) != null && typeTmp != swt.GetType())
            {
                typeObj = typeTmp;
            }

            if (typeObj.IsTypeParameterType() || typeObj.IsAggregateType())
            {
                AggregateSymbol aggCalled = null;
                aggCalled = swt.Sym.parent.AsAggregateSymbol();
                Debug.Assert(swt.GetType().getAggregate() == aggCalled);

                // If we're invoking code on a struct-valued field, mark the struct as assigned (to
                // avoid warning CS0649).
                if (pObject.isFIELD() && !pObject.asFIELD().fwt.Field().isAssigned && !swt.Sym.IsFieldSymbol() &&
                    typeObj.isStructType() && !typeObj.isPredefined())
                {
                    pObject.asFIELD().fwt.Field().isAssigned = true;
                }

                if (pfConstrained &&
                    (typeObj.IsTypeParameterType() ||
                     typeObj.isStructType() && swt.GetType().IsRefType() && swt.Sym.IsVirtual()))
                {
                    // For calls on type parameters or virtual calls on struct types (not enums),
                    // use the constrained prefix.
                    pfConstrained = true;
                }

                EXPR objNew = tryConvert(pObject, swt.GetType(), CONVERTTYPE.NOUDC);

                // This check ensures that we do not bind to methods in an outer class
                // which are visible, but whose this pointer is of an incorrect type...
                // ... also handles case of calling an pObject method on a RefAny value.
                // WE don't give a great message for this, but it'll do.
                if (objNew == null)
                {
                    if (!pObject.type.isSpecialByRefType())
                    {
                        ErrorContext.Error(ErrorCode.ERR_WrongNestedThis, swt.GetType(), pObject.type);
                    }
                    else
                    {
                        ErrorContext.Error(ErrorCode.ERR_NoImplicitConv, pObject.type, swt.GetType());
                    }
                }
                pObject = objNew;
            }

            return pObject;
        }
        /////////////////////////////////////////////////////////////////////////////////

        private bool IsMatchingStatic(SymWithType swt, EXPR pObject)
        {
            Symbol pSym = swt.Sym;

            // Instance constructors are always ok, static constructors are never ok.
            if (pSym.IsMethodSymbol() && pSym.AsMethodSymbol().IsConstructor())
            {
                return !pSym.AsMethodSymbol().isStatic;
            }

            bool isStatic = swt.Sym.isStatic;

            if (isStatic)
            {
                // If we're static and we don't have an object, or we have an implicit this, 
                // then we're ok. The reason implicit this is ok is because if the user is
                // just typing something like:
                //
                //      Equals(
                //
                // then the implicit this can bind to statics.

                if (pObject == null || ((pObject.flags & EXPRFLAG.EXF_IMPLICITTHIS) != 0))
                {
                    return true;
                }

                if ((pObject.flags & EXPRFLAG.EXF_SAMENAMETYPE) == 0)
                {
                    return false;
                }
            }
            else if (pObject == null)
            {
                // We're not static, and we don't have an object. This is ok in certain scenarios:
                bool bNonStaticField = InFieldInitializer() && !InStaticMethod() && ContainingAgg() == swt.Sym.parent;
                bool bAnonymousMethod = InAnonymousMethod() && !InStaticMethod() && ContainingAgg() == swt.Sym.parent && ContainingAgg().IsStruct();

                if (!bNonStaticField && !bAnonymousMethod)
                {
                    return false;
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // this determines whether the expression as an pObject of a prop or field is an
        // lvalue

        private bool objectIsLvalue(EXPR pObject)
        {
            return (
                       pObject == null ||  // statics are always lvalues

                       isThisPointer(pObject) ||  // the this pointer's fields or props are lvalues

                       (((pObject.flags & EXPRFLAG.EXF_LVALUE) != 0) && (pObject.kind != ExpressionKind.EK_PROP)) ||
                       // things marked as lvalues have props/fields which are lvalues, with one exception:  props of structs
                       // do not have fields/structs as lvalues

                       !pObject.type.isStructOrEnum()
                   // non-struct types are lvalues (such as non-struct method returns)
                   );
        }
        ////////////////////////////////////////////////////////////////////////////////
        // For a base call we need to remap from the virtual to the specific override 
        // to invoke.  This is also used to map a virtual on pObject (like ToString) to 
        // the specific override when the pObject is a simple type (int, bool, char, 
        // etc). In these cases it is safe to assume that any override won't later be 
        // removed.... We start searching from "typeObj" up the superclass hierarchy 
        // until we find a method with an exact signature match.

        private static void RemapToOverride(SymbolLoader symbolLoader, SymWithType pswt, CType typeObj)
        {
            // For a property/indexer we remap the accessors, not the property/indexer.
            // Since every event has both accessors we remap the event instead of the accessors.
            Debug.Assert(pswt && (pswt.Sym.IsMethodSymbol() || pswt.Sym.IsEventSymbol() || pswt.Sym.IsMethodOrPropertySymbol()));
            Debug.Assert(typeObj != null);

            // Don't remap static or interface methods.
            if (typeObj.IsNullableType())
            {
                typeObj = typeObj.AsNullableType().GetAts(symbolLoader.GetErrorContext());
                if (typeObj == null)
                {
                    VSFAIL("Why did GetAts return null?");
                    return;
                }
            }

            // Don't remap non-virtual members
            if (!typeObj.IsAggregateType() || typeObj.isInterfaceType() || !pswt.Sym.IsVirtual())
            {
                return;
            }

            symbmask_t mask = pswt.Sym.mask();

            AggregateType atsObj = typeObj.AsAggregateType();

            // Search for an override version of the method.
            while (atsObj != null && atsObj.getAggregate() != pswt.Sym.parent)
            {
                for (Symbol symT = symbolLoader.LookupAggMember(pswt.Sym.name, atsObj.getAggregate(), mask);
                     symT != null;
                     symT = symbolLoader.LookupNextSym(symT, atsObj.getAggregate(), mask))
                {
                    if (symT.IsOverride() && (symT.SymBaseVirtual() == pswt.Sym || symT.SymBaseVirtual() == pswt.Sym.SymBaseVirtual()))
                    {
                        pswt.Set(symT, atsObj);
                        return;
                    }
                }
                atsObj = atsObj.GetBaseClass();
            }
        }

        private void verifyMethodArgs(EXPR call, CType callingObjectType)
        {
            Debug.Assert(call.isCALL() || call.isPROP());

            EXPR argsPtr = call.getArgs();
            SymWithType swt = call.GetSymWithType();
            MethodOrPropertySymbol mp = swt.Sym.AsMethodOrPropertySymbol();
            TypeArray pTypeArgs = call.isCALL() ? call.asCALL().mwi.TypeArgs : null;
            EXPR newArgs;
            AdjustCallArgumentsForParams(callingObjectType, swt.GetType(), mp, pTypeArgs, argsPtr, out newArgs);
            call.setArgs(newArgs);
        }

        private void AdjustCallArgumentsForParams(CType callingObjectType, CType type, MethodOrPropertySymbol mp, TypeArray pTypeArgs, EXPR argsPtr, out EXPR newArgs)
        {
            Debug.Assert(mp != null);
            Debug.Assert(mp.Params != null);
            newArgs = null;
            EXPR newArgsTail = null;

            MethodOrPropertySymbol mostDerivedMethod = GroupToArgsBinder.FindMostDerivedMethod(GetSymbolLoader(), mp, callingObjectType);

            int paramCount = mp.Params.size;
            TypeArray @params = mp.Params;
            int iDst = 0;
            bool markTypeFromExternCall = mp.IsFMETHSYM() && mp.AsFMETHSYM().isExternal;
            int argCount = ExpressionIterator.Count(argsPtr);

            if (mp.IsFMETHSYM() && mp.AsFMETHSYM().isVarargs)
            {
                paramCount--; // we don't care about the vararg sentinel
            }

            bool bDontFixParamArray = false;

            ExpressionIterator it = new ExpressionIterator(argsPtr);

            if (argsPtr == null)
            {
                if (mp.isParamArray)
                    goto FIXUPPARAMLIST;
                return;
            }
            for (; !it.AtEnd(); it.MoveNext())
            {
                EXPR indir = it.Current();
                // this will splice the optional arguments into the list

                if (indir.type.IsParameterModifierType())
                {
                    if (paramCount != 0)
                        paramCount--;
                    if (markTypeFromExternCall)
                        SetExternalRef(indir.type);
                    GetExprFactory().AppendItemToList(indir, ref newArgs, ref newArgsTail);
                }
                else if (paramCount != 0)
                {
                    if (paramCount == 1 && mp.isParamArray && argCount > mp.Params.size)
                    {
                        // we arrived at the last formal, and we have more than one actual, so
                        // we need to put the rest in an array...
                        goto FIXUPPARAMLIST;
                    }

                    EXPR argument = indir;
                    EXPR rval;
                    if (argument.isNamedArgumentSpecification())
                    {
                        int index = 0;
                        // If we're named, look for the type of the matching name.
                        foreach (Name i in mostDerivedMethod.ParameterNames)
                        {
                            if (i == argument.asNamedArgumentSpecification().Name)
                            {
                                break;
                            }
                            index++;
                        }
                        Debug.Assert(index != mp.Params.size);
                        CType substDestType = GetTypes().SubstType(@params.Item(index), type, pTypeArgs);

                        // If we cant convert the argument and we're the param array argument, then deal with it.
                        if (!canConvert(argument.asNamedArgumentSpecification().Value, substDestType) &&
                            mp.isParamArray && index == mp.Params.size - 1)
                        {
                            // We have a param array, but we're not at the end yet. This will happen
                            // with named arguments when the user specifies a name for the param array,
                            // and its not an actual array.
                            // 
                            // For example:
                            // void Foo(int y, params int[] x);
                            // ...
                            // Foo(x:1, y:1);
                            CType arrayType = GetTypes().SubstType(mp.Params.Item(mp.Params.size - 1), type, pTypeArgs);
                            CType elemType = arrayType.AsArrayType().GetElementType();

                            // Use an EK_ARRINIT even in the empty case so empty param arrays in attributes work.
                            EXPRARRINIT arrayInit = GetExprFactory().CreateArrayInit(0, arrayType, null, null, null);
                            arrayInit.GeneratedForParamArray = true;
                            arrayInit.dimSizes = new int[] { arrayInit.dimSize };
                            arrayInit.dimSize = 1;
                            arrayInit.SetOptionalArguments(argument.asNamedArgumentSpecification().Value);

                            argument.asNamedArgumentSpecification().Value = arrayInit;
                            bDontFixParamArray = true;
                        }
                        else
                        {
                            // Otherwise, force the conversion and get errors if needed.
                            argument.asNamedArgumentSpecification().Value = tryConvert(
                                argument.asNamedArgumentSpecification().Value,
                                substDestType);
                        }
                        rval = argument;
                    }
                    else
                    {
                        CType substDestType = GetTypes().SubstType(@params.Item(iDst), type, pTypeArgs);
                        rval = tryConvert(indir, substDestType);
                    }

                    if (rval == null)
                    {
                        // the last arg failed to fix up, so it must fixup into the array element
                        // if we have a param array (we will be passing a 1 element array...)
                        if (mp.isParamArray && paramCount == 1 && argCount >= mp.Params.size)
                        {
                            goto FIXUPPARAMLIST;
                        }
                        else
                        {
                            // This is either the error case that the args are of the wrong type, 
                            // or that we have some optional arguments being used. Either way,
                            // we wont need to expand the param array.
                            return;
                        }
                    }
                    Debug.Assert(rval != null);
                    indir = rval;
                    GetExprFactory().AppendItemToList(rval, ref newArgs, ref newArgsTail);
                    paramCount--;
                }
                // note that destype might not be valid if we are in varargs, but then we won't ever use it...
                iDst++;

                if (paramCount != 0 && mp.isParamArray && iDst == argCount)
                {
                    // we run out of actuals, but we still have formals, so this is an empty array being passed
                    // into the last param...
                    indir = null;
                    it.MoveNext();
                    goto FIXUPPARAMLIST;
                }
            }

            return;

        FIXUPPARAMLIST:
            if (bDontFixParamArray)
            {
                // We've already fixed the param array for named arguments.
                return;
            }

            // we need to create an array and put it as the last arg...
            CType substitutedArrayType = GetTypes().SubstType(mp.Params.Item(mp.Params.size - 1), type, pTypeArgs);
            if (!substitutedArrayType.IsArrayType() || substitutedArrayType.AsArrayType().rank != 1)
            {
                // Invalid type for params array parameter. Happens in LAF scenarios, e.g.
                //
                // void Foo(int i, params int ar = null) { }
                // ...
                // Foo(1);
                return;
            }

            CType elementType = substitutedArrayType.AsArrayType().GetElementType();

            // Use an EK_ARRINIT even in the empty case so empty param arrays in attributes work.
            EXPRARRINIT exprArrayInit = GetExprFactory().CreateArrayInit(0, substitutedArrayType, null, null, null);
            exprArrayInit.GeneratedForParamArray = true;
            exprArrayInit.dimSizes = new int[] { exprArrayInit.dimSize };

            if (it.AtEnd())
            {
                exprArrayInit.dimSize = 0;
                exprArrayInit.dimSizes[0] = 0;
                exprArrayInit.SetOptionalArguments(null);
                if (argsPtr == null)
                {
                    argsPtr = exprArrayInit;
                }
                else
                {
                    argsPtr = GetExprFactory().CreateList(argsPtr, exprArrayInit);
                }
                GetExprFactory().AppendItemToList(exprArrayInit, ref newArgs, ref newArgsTail);
            }
            else
            {
                // Go through the list - for each argument, do the conversion and append it to the new list.
                EXPR newList = null;
                EXPR newListTail = null;
                int count = 0;

                for (; !it.AtEnd(); it.MoveNext())
                {
                    EXPR expr = it.Current();
                    count++;

                    if (expr.isNamedArgumentSpecification())
                    {
                        expr.asNamedArgumentSpecification().Value = tryConvert(
                            expr.asNamedArgumentSpecification().Value, elementType);
                    }
                    else
                    {
                        expr = tryConvert(expr, elementType);
                    }
                    GetExprFactory().AppendItemToList(expr, ref newList, ref newListTail);
                }

                exprArrayInit.dimSize = count;
                exprArrayInit.dimSizes[0] = count;
                exprArrayInit.SetOptionalArguments(newList);
                GetExprFactory().AppendItemToList(exprArrayInit, ref newArgs, ref newArgsTail);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Sets the isAssigned bit

        private void markFieldAssigned(EXPR expr)
        {
            if (expr.isFIELD() && 0 != (expr.flags & EXPRFLAG.EXF_LVALUE))
            {
                EXPRFIELD field;

                do
                {
                    field = expr.asFIELD();
                    field.fwt.Field().isAssigned = true;
                    expr = field.GetOptionalObject();
                }
                while (field.fwt.Field().getClass().IsStruct() && !field.fwt.Field().isStatic && expr != null && expr.isFIELD());
            }
        }

        private void SetExternalRef(CType type)
        {
            AggregateSymbol agg = type.GetNakedAgg();
            if (null == agg || agg.HasExternReference())
                return;

            agg.SetHasExternReference(true);
            foreach (Symbol sym in agg.Children())
            {
                if (sym.IsFieldSymbol())
                    SetExternalRef(sym.AsFieldSymbol().GetType());
            }
        }


        private static readonly PredefinedType[] s_rgptIntOp =
        {
            PredefinedType.PT_INT,
            PredefinedType.PT_UINT,
            PredefinedType.PT_LONG,
            PredefinedType.PT_ULONG
        };



        internal CType chooseArrayIndexType(EXPR args)
        {
            // first, select the allowable types
            for (int ipt = 0; ipt < s_rgptIntOp.Length; ipt++)
            {
                CType type = GetReqPDT(s_rgptIntOp[ipt]);
                foreach (EXPR arg in args.ToEnumerable())
                {
                    if (!canConvert(arg, type))
                    {
                        goto NEXTI;
                    }
                }
                return type;
            NEXTI:
                ;
            }
            return null;
        }

        internal void FillInArgInfoFromArgList(ArgInfos argInfo, EXPR args)
        {
            CType[] prgtype = new CType[argInfo.carg];
            argInfo.fHasExprs = true;
            argInfo.prgexpr = new List<EXPR>();

            int iarg = 0;
            for (EXPR list = args; list != null; iarg++)
            {
                EXPR arg;
                if (list.isLIST())
                {
                    arg = list.asLIST().GetOptionalElement();
                    list = list.asLIST().GetOptionalNextListNode();
                }
                else
                {
                    arg = list;
                    list = null;
                }

                Debug.Assert(arg != null);

                if (arg.type != null)
                {
                    prgtype[iarg] = (CType)arg.type;
                }
                else
                {
                    prgtype[iarg] = GetTypes().GetErrorSym();
                }
                argInfo.prgexpr.Add(arg);
            }
            Debug.Assert(iarg <= argInfo.carg);
            argInfo.types = GetGlobalSymbols().AllocParams(iarg, prgtype);
        }

        private bool TryGetExpandedParams(TypeArray @params, int count, out TypeArray ppExpandedParams)
        {
            CType[] prgtype;
            if (count < @params.size - 1)
            {
                // The user has specified less arguments than our parameters, but we still
                // need to return our set of types without the param array. This is in the 
                // case that all the parameters are optional.
                prgtype = new CType[@params.size - 1];
                @params.CopyItems(0, @params.size - 1, prgtype);
                ppExpandedParams = GetGlobalSymbols().AllocParams(@params.size - 1, prgtype);
                return true;
            }

            prgtype = new CType[count];
            @params.CopyItems(0, @params.size - 1, prgtype);

            CType type = @params.Item(@params.size - 1);
            CType elementType = null;

            if (!type.IsArrayType())
            {
                ppExpandedParams = null;
                // If we don't have an array sym, we don't have expanded parameters.
                return false;
            }

            // At this point, we have an array sym.
            elementType = type.AsArrayType().GetElementType();

            for (int itype = @params.size - 1; itype < count; itype++)
            {
                prgtype[itype] = elementType;
            }

            ppExpandedParams = GetGlobalSymbols().AllocParams(prgtype);

            return true;
        }

        // Is the method/property callable. Not if it's an override or not user-callable.
        public static bool IsMethPropCallable(MethodOrPropertySymbol sym, bool requireUC)
        {
            // The hide-by-pName option for binding other languages takes precedence over general
            // rules of not binding to overrides.
            return (!sym.isOverride || sym.isHideByName) && (!requireUC || sym.isUserCallable());
        }

        private bool isConvInTable(List<UdConvInfo> convTable, MethodSymbol meth, AggregateType ats, bool fSrc, bool fDst)
        {
            foreach (UdConvInfo conv in convTable)
            {
                if (conv.mwt.Meth() == meth &&
                    conv.mwt.GetType() == ats &&
                    conv.fSrcImplicit == fSrc &&
                    conv.fDstImplicit == fDst)
                {
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Check to see if an integral constant is within range of a integral 
        // destination type.
        private static bool isConstantInRange(EXPRCONSTANT exprSrc, CType typeDest)
        {
            return isConstantInRange(exprSrc, typeDest, false);
        }

        private static bool isConstantInRange(EXPRCONSTANT exprSrc, CType typeDest, bool realsOk)
        {
            FUNDTYPE ftSrc = exprSrc.type.fundType();
            FUNDTYPE ftDest = typeDest.fundType();

            if (ftSrc > FUNDTYPE.FT_LASTINTEGRAL || ftDest > FUNDTYPE.FT_LASTINTEGRAL)
            {
                if (!realsOk)
                {
                    return false;
                }
                else if (ftSrc > FUNDTYPE.FT_LASTNUMERIC || ftDest > FUNDTYPE.FT_LASTNUMERIC)
                {
                    return false;
                }
            }

            // if converting to a float type, this always succeeds...
            if (ftDest > FUNDTYPE.FT_LASTINTEGRAL)
            {
                return true;
            }

            // if converting from float to an integral type, we need to check whether it fits
            if (ftSrc > FUNDTYPE.FT_LASTINTEGRAL)
            {
                double dvalue = (exprSrc.asCONSTANT().getVal().doubleVal);

                switch (ftDest)
                {
                    case FUNDTYPE.FT_I1:
                        if (dvalue > -0x81 && dvalue < 0x80)
                            return true;
                        break;
                    case FUNDTYPE.FT_I2:
                        if (dvalue > -0x8001 && dvalue < 0x8000)
                            return true;
                        break;
                    case FUNDTYPE.FT_I4:
                        if (dvalue > I64(-0x80000001) && dvalue < I64(0x80000000))
                            return true;
                        break;
                    case FUNDTYPE.FT_I8:
                        // 0x7FFFFFFFFFFFFFFFFFFF is rounded to 0x800000000000000000000 in 64 bit double precision
                        // floating point representation. The conversion back to ulong is not possible.
                        if (dvalue >= -9223372036854775808.0 && dvalue < 9223372036854775808.0)
                        {
                            return true;
                        }
                        break;
                    case FUNDTYPE.FT_U1:
                        if (dvalue > -1 && dvalue < 0x100)
                            return true;
                        break;
                    case FUNDTYPE.FT_U2:
                        if (dvalue > -1 && dvalue < 0x10000)
                            return true;
                        break;
                    case FUNDTYPE.FT_U4:
                        if (dvalue > -1 && dvalue < I64(0x100000000))
                            return true;
                        break;
                    case FUNDTYPE.FT_U8:
                        // 0xFFFFFFFFFFFFFFFFFFFF is rounded to 0x100000000000000000000 in 64 bit double precision
                        // floating point representation. The conversion back to ulong is not possible.
                        if (dvalue > -1.0 && dvalue < 18446744073709551616.0)
                        {
                            return true;
                        }
                        break;
                    default:
                        break;
                }
                return false;
            }

            // U8 src is unsigned, so deal with values > MAX_LONG here.
            if (ftSrc == FUNDTYPE.FT_U8)
            {
                ulong value = exprSrc.asCONSTANT().getU64Value();

                switch (ftDest)
                {
                    case FUNDTYPE.FT_I1:
                        if (value <= (ulong)SByte.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_I2:
                        if (value <= (ulong)Int16.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_I4:
                        if (value <= Int32.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_I8:
                        if (value <= Int64.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U1:
                        if (value <= Byte.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U2:
                        if (value <= UInt16.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U4:
                        if (value <= UInt32.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U8:
                        return true;
                    default:
                        break;
                }
            }
            else
            {
                long value = exprSrc.asCONSTANT().getI64Value();

                switch (ftDest)
                {
                    case FUNDTYPE.FT_I1:
                        if (value >= -128 && value <= 127)
                            return true;
                        break;
                    case FUNDTYPE.FT_I2:
                        if (value >= -0x8000 && value <= 0x7fff)
                            return true;
                        break;
                    case FUNDTYPE.FT_I4:
                        if (value >= I64(-0x80000000) && value <= I64(0x7fffffff))
                            return true;
                        break;
                    case FUNDTYPE.FT_I8:
                        return true;
                    case FUNDTYPE.FT_U1:
                        if (value >= 0 && value <= 0xff)
                            return true;
                        break;
                    case FUNDTYPE.FT_U2:
                        if (value >= 0 && value <= 0xffff)
                            return true;
                        break;
                    case FUNDTYPE.FT_U4:
                        if (value >= 0 && value <= I64(0xffffffff))
                            return true;
                        break;
                    case FUNDTYPE.FT_U8:
                        if (value >= 0)
                            return true;
                        break;
                    default:
                        break;
                }
            }
            return false;
        }

        private static readonly PredefinedName[] s_EK2NAME =
        {
            PredefinedName.PN_OPEQUALS,
            PredefinedName.PN_OPCOMPARE,
            PredefinedName.PN_OPTRUE,
            PredefinedName.PN_OPFALSE,
            PredefinedName.PN_OPINCREMENT,
            PredefinedName.PN_OPDECREMENT,
            PredefinedName.PN_OPNEGATION,
            PredefinedName.PN_OPEQUALITY,
            PredefinedName.PN_OPINEQUALITY,
            PredefinedName.PN_OPLESSTHAN,
            PredefinedName.PN_OPLESSTHANOREQUAL,
            PredefinedName.PN_OPGREATERTHAN,
            PredefinedName.PN_OPGREATERTHANOREQUAL,
            PredefinedName.PN_OPPLUS,
            PredefinedName.PN_OPMINUS,
            PredefinedName.PN_OPMULTIPLY,
            PredefinedName.PN_OPDIVISION,
            PredefinedName.PN_OPMODULUS,
            PredefinedName.PN_OPUNARYMINUS,
            PredefinedName.PN_OPUNARYPLUS,
            PredefinedName.PN_OPBITWISEAND,
            PredefinedName.PN_OPBITWISEOR,
            PredefinedName.PN_OPXOR,
            PredefinedName.PN_OPCOMPLEMENT,
            PredefinedName.PN_OPLEFTSHIFT,
            PredefinedName.PN_OPRIGHTSHIFT,
        };

        private Name ekName(ExpressionKind ek)
        {
            Debug.Assert(ek >= ExpressionKind.EK_FIRSTOP && (ek - ExpressionKind.EK_FIRSTOP) < (int)s_EK2NAME.Length);
            return GetSymbolLoader().GetNameManager().GetPredefName(s_EK2NAME[ek - ExpressionKind.EK_FIRSTOP]);
        }

        private void checkUnsafe(CType type)
        {
            checkUnsafe(type, ErrorCode.ERR_UnsafeNeeded, null);
        }

        private void checkUnsafe(CType type, ErrorCode errCode, ErrArg pArg)
        {
            Debug.Assert((errCode != ErrorCode.ERR_SizeofUnsafe) || pArg != null);
            if (type == null || type.isUnsafe())
            {
                if (!isUnsafeContext() && ReportUnsafeErrors())
                {
                    if (pArg != null)
                        ErrorContext.Error(errCode, pArg);
                    else
                        ErrorContext.Error(errCode);
                }
                RecordUnsafeUsage();
            }
        }

        private bool InMethod()
        {
            return Context.InMethod();
        }

        private bool InStaticMethod()
        {
            return Context.InStaticMethod();
        }

        private bool InConstructor()
        {
            return Context.InConstructor();
        }

        private bool InAnonymousMethod()
        {
            return Context.InAnonymousMethod();
        }

        private bool InFieldInitializer()
        {
            return Context.InFieldInitializer();
        }

        ////////////////////////////////////////////////////////////////////////////////
        private Declaration ContextForMemberLookup()
        {
            return Context.ContextForMemberLookup();
        }

        private AggregateSymbol ContainingAgg()
        {
            return Context.ContainingAgg();
        }

        private bool isThisPointer(EXPR expr)
        {
            return Context.IsThisPointer(expr);
        }

        private bool RespectReadonly()
        {
            return Context.RespectReadonly();
        }

        private bool isUnsafeContext()
        {
            return Context.IsUnsafeContext();
        }

        private bool ReportUnsafeErrors()
        {
            return Context.ReportUnsafeErrors();
        }

        private void RecordUnsafeUsage()
        {
            RecordUnsafeUsage(Context);
        }

        private EXPR WrapShortLivedExpression(EXPR expr)
        {
            return GetExprFactory().CreateWrap(null, expr);
        }

        private EXPR GenerateOptimizedAssignment(EXPR op1, EXPR op2)
        {
            return GetExprFactory().CreateAssignment(op1, op2);
        }

        private static void RecordUnsafeUsage(BindingContext context)
        {
            if (!(context.GetUnsafeState() == UNSAFESTATES.UNSAFESTATES_Unsafe) &&
                    !context.GetOutputContext().m_bUnsafeErrorGiven)
            {
                context.GetOutputContext().m_bUnsafeErrorGiven = true;
            }
        }

        internal static int CountArguments(EXPR args, out bool typeErrors)
        {
            int carg = 0;
            typeErrors = false;
            for (EXPR list = args; list != null; carg++)
            {
                EXPR arg;

                if (list.isLIST())
                {
                    arg = list.asLIST().GetOptionalElement();
                    list = list.asLIST().GetOptionalNextListNode();
                }
                else
                {
                    arg = list;
                    list = null;
                }

                Debug.Assert(arg != null);

                if (arg.type == null || arg.type.IsErrorType())
                {
                    typeErrors = true;
                }
            }
            return carg;
        }
    }
}

