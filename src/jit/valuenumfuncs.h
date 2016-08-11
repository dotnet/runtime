// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Defines the functions understood by the value-numbering system.
// ValueNumFuncDef(<name of function>, <arity (1-4)>, <is-commutative (for arity = 2)>, <non-null (for gc functions)>,
// <is-shared-static>)

// clang-format off
ValueNumFuncDef(MapStore, 3, false, false, false)
ValueNumFuncDef(MapSelect, 2, false, false, false)

ValueNumFuncDef(FieldSeq, 2, false, false, false)   // Sequence (VN of null == empty) of (VN's of) field handles.
ValueNumFuncDef(ZeroMap, 0, false, false, false)    // The "ZeroMap": indexing at any index yields "zero of the desired type".

ValueNumFuncDef(PtrToLoc, 3, false, false, false)           // Pointer (byref) to a local variable.  Args: VN's of: 0: var num, 1: FieldSeq, 2: Unique value for this PtrToLoc.
ValueNumFuncDef(PtrToArrElem, 4, false, false, false)       // Pointer (byref) to an array element.  Args: 0: array elem type eq class var_types value, VN's of: 1: array, 2: index, 3: FieldSeq.
ValueNumFuncDef(PtrToStatic, 1, false, false, false)        // Pointer (byref) to a static variable (or possibly a field thereof, if the static variable is a struct).  Args: 0: FieldSeq, first element
                                                     // of which is the static var.
ValueNumFuncDef(Phi, 2, false, false, false)        // A phi function.  Only occurs as arg of PhiDef or PhiHeapDef.  Arguments are SSA numbers of var being defined.
ValueNumFuncDef(PhiDef, 3, false, false, false)     // Args: 0: local var # (or -1 for Heap), 1: SSA #, 2: VN of definition.
// Wouldn't need this if I'd made Heap a regular local variable...
ValueNumFuncDef(PhiHeapDef, 2, false, false, false) // Args: 0: VN for basic block pointer, 1: VN of definition
ValueNumFuncDef(InitVal, 1, false, false, false)    // An input arg, or init val of a local Args: 0: a constant VN.


ValueNumFuncDef(Cast, 2, false, false, false)               // VNF_Cast: Cast Operation changes the representations size and unsigned-ness.
                                                     //           Args: 0: Source for the cast operation.
                                                     //                 1: Constant integer representing the operation .
                                                     //                    Use VNForCastOper() to construct.

ValueNumFuncDef(CastClass, 2, false, false, false)          // Args: 0: Handle of class being cast to, 1: object being cast.
ValueNumFuncDef(IsInstanceOf, 2, false, false, false)       // Args: 0: Handle of class being queried, 1: object being queried.
ValueNumFuncDef(ReadyToRunCastClass, 2, false, false, false)          // Args: 0: Helper stub address, 1: object being cast.
ValueNumFuncDef(ReadyToRunIsInstanceOf, 2, false, false, false)       // Args: 0: Helper stub address, 1: object being queried.

ValueNumFuncDef(LdElemA, 3, false, false, false)            // Args: 0: array value; 1: index value; 2: type handle of element.

ValueNumFuncDef(GetRefanyVal, 2, false, false, false)       // Args: 0: type handle; 1: typedref value.  Returns the value (asserting that the type is right).

ValueNumFuncDef(GetClassFromMethodParam, 1, false, true, false)       // Args: 0: method generic argument.
ValueNumFuncDef(GetSyncFromClassHandle, 1, false, true, false)        // Args: 0: class handle.
ValueNumFuncDef(LoopCloneChoiceAddr, 0, false, true, false)

// How we represent values of expressions with exceptional side effects:
ValueNumFuncDef(ValWithExc, 2, false, false, false)         // Args: 0: value number from normal execution; 1: VN for set of possible exceptions.

ValueNumFuncDef(ExcSetCons, 2, false, false, false)         // Args: 0: exception; 1: exception set (including EmptyExcSet).  Invariant: "car"s are always in ascending order.

// Various exception values.
ValueNumFuncDef(NullPtrExc, 1, false, false, false)         // Null pointer exception.
ValueNumFuncDef(ArithmeticExc, 0, false, false, false)      // E.g., for signed its, MinInt / -1.
ValueNumFuncDef(OverflowExc, 0, false, false, false)        // Integer overflow.
ValueNumFuncDef(ConvOverflowExc, 2, false, false, false)    // Integer overflow produced by converion.  Args: 0: input value; 1: var_types of target type
                                                     // (shifted left one bit; low bit encode whether source is unsigned.) 
ValueNumFuncDef(DivideByZeroExc, 0, false, false, false)    // Division by zero.
ValueNumFuncDef(IndexOutOfRangeExc, 2, false, false, false) // Args: 0: array length; 1: index.  The exception raised if this bounds check fails.
ValueNumFuncDef(InvalidCastExc, 2, false, false, false)     // Args: 0: ref value being cast; 1: handle of type being cast to.  Represents the exception thrown if the cast fails.
ValueNumFuncDef(NewArrOverflowExc, 1, false, false, false)  // Raises Integer overflow when Arg 0 is negative
ValueNumFuncDef(HelperMultipleExc, 0, false, false, false)  // Represents one or more different exceptions that may be thrown by a JitHelper

ValueNumFuncDef(Lng2Dbl, 1, false, false, false)
ValueNumFuncDef(ULng2Dbl, 1, false, false, false)
ValueNumFuncDef(Dbl2Int, 1, false, false, false)
ValueNumFuncDef(Dbl2UInt, 1, false, false, false)
ValueNumFuncDef(Dbl2Lng, 1, false, false, false)
ValueNumFuncDef(Dbl2ULng, 1, false, false, false)
ValueNumFuncDef(FltRound, 1, false, false, false)
ValueNumFuncDef(DblRound, 1, false, false, false)

ValueNumFuncDef(Sin, 1, false, false, false)
ValueNumFuncDef(Cos, 1, false, false, false)
ValueNumFuncDef(Sqrt, 1, false, false, false)
ValueNumFuncDef(Abs, 1, false, false, false)
ValueNumFuncDef(RoundDouble, 1, false, false, false)
ValueNumFuncDef(RoundFloat, 1, false, false, false)
ValueNumFuncDef(RoundInt, 1, false, false, false)
ValueNumFuncDef(Cosh, 1, false, false, false)
ValueNumFuncDef(Sinh, 1, false, false, false)
ValueNumFuncDef(Tan, 1, false, false, false)
ValueNumFuncDef(Tanh, 1, false, false, false)
ValueNumFuncDef(Asin, 1, false, false, false)
ValueNumFuncDef(Acos, 1, false, false, false)
ValueNumFuncDef(Atan, 1, false, false, false)
ValueNumFuncDef(Atan2, 2, false, false, false)
ValueNumFuncDef(Log10, 1, false, false, false)
ValueNumFuncDef(Pow, 2, false, false, false)
ValueNumFuncDef(Exp, 1, false, false, false)
ValueNumFuncDef(Ceiling, 1, false, false, false)
ValueNumFuncDef(Floor, 1, false, false, false)

ValueNumFuncDef(ManagedThreadId, 0, false, false, false)

ValueNumFuncDef(ObjGetType, 1, false, false, false)
ValueNumFuncDef(GetgenericsGcstaticBase, 1, false, true, true)
ValueNumFuncDef(GetgenericsNongcstaticBase, 1, false, true, true)
ValueNumFuncDef(GetsharedGcstaticBase, 2, false, true, true)
ValueNumFuncDef(GetsharedNongcstaticBase, 2, false, true, true)
ValueNumFuncDef(GetsharedGcstaticBaseNoctor, 1, false, true, true)
ValueNumFuncDef(GetsharedNongcstaticBaseNoctor, 1, false, true, true)
ValueNumFuncDef(ReadyToRunStaticBase, 1, false, true, true)
ValueNumFuncDef(GetsharedGcstaticBaseDynamicclass, 2, false, true, true)
ValueNumFuncDef(GetsharedNongcstaticBaseDynamicclass, 2, false, true, true)
ValueNumFuncDef(GetgenericsGcthreadstaticBase, 1, false, true, true)
ValueNumFuncDef(GetgenericsNongcthreadstaticBase, 1, false, true, true)
ValueNumFuncDef(GetsharedGcthreadstaticBase, 2, false, true, true)
ValueNumFuncDef(GetsharedNongcthreadstaticBase, 2, false, true, true)
ValueNumFuncDef(GetsharedGcthreadstaticBaseNoctor, 2, false, true, true)
ValueNumFuncDef(GetsharedNongcthreadstaticBaseNoctor, 2, false, true, true)
ValueNumFuncDef(GetsharedGcthreadstaticBaseDynamicclass, 2, false, true, true)
ValueNumFuncDef(GetsharedNongcthreadstaticBaseDynamicclass, 2, false, true, true)

ValueNumFuncDef(ClassinitSharedDynamicclass, 2, false, false, false)
ValueNumFuncDef(RuntimeHandleMethod, 2, false, true, false)
ValueNumFuncDef(RuntimeHandleClass, 2, false, true, false)

ValueNumFuncDef(GetStaticAddrContext, 1, false, true, false)
ValueNumFuncDef(GetStaticAddrTLS, 1, false, true, false)

ValueNumFuncDef(JitNew, 2, false, true, false)
ValueNumFuncDef(JitNewArr, 3, false, true, false)
ValueNumFuncDef(JitReadyToRunNew, 2, false, true, false)
ValueNumFuncDef(JitReadyToRunNewArr, 3, false, true, false)
ValueNumFuncDef(BoxNullable, 3, false, false, false)

ValueNumFuncDef(LT_UN, 2, false, false, false)
ValueNumFuncDef(LE_UN, 2, false, false, false)
ValueNumFuncDef(GE_UN, 2, false, false, false)
ValueNumFuncDef(GT_UN, 2, false, false, false)
ValueNumFuncDef(ADD_UN, 2, true, false, false)
ValueNumFuncDef(SUB_UN, 2, false, false, false)
ValueNumFuncDef(MUL_UN, 2, true, false, false)
ValueNumFuncDef(DIV_UN, 2, false, false, false)
ValueNumFuncDef(MOD_UN, 2, false, false, false)

ValueNumFuncDef(StrCns, 2, false, true, false)

ValueNumFuncDef(Unbox, 2, false, true, false)
// clang-format on

#undef ValueNumFuncDef
