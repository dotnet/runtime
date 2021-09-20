// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Defines the functions understood by the value-numbering system.
// ValueNumFuncDef(<name of function>, <arity (1-4)>, <is-commutative (for arity = 2)>, <non-null (for gc functions)>,
// <is-shared-static>)

// clang-format off
ValueNumFuncDef(MemOpaque, 1, false, false, false)  // Args: 0: loop num
ValueNumFuncDef(MapStore, 4, false, false, false)   // Args: 0: map, 1: index (e. g. field handle), 2: value being stored, 3: loop num.
ValueNumFuncDef(MapSelect, 2, false, false, false)  // Args: 0: map, 1: key.

ValueNumFuncDef(FieldSeq, 2, false, false, false)   // Sequence (VN of null == empty) of (VN's of) field handles.
ValueNumFuncDef(NotAField, 0, false, false, false)  // Value number function for FieldSeqStore::NotAField.
ValueNumFuncDef(ZeroMap, 0, false, false, false)    // The "ZeroMap": indexing at any index yields "zero of the desired type".

ValueNumFuncDef(PtrToLoc, 2, false, false, false)           // Pointer (byref) to a local variable.  Args: VN's of: 0: var num, 1: FieldSeq.
ValueNumFuncDef(PtrToArrElem, 4, false, false, false)       // Pointer (byref) to an array element.  Args: 0: array elem type eq class var_types value, VN's of: 1: array, 2: index, 3: FieldSeq.
ValueNumFuncDef(PtrToStatic, 1, false, false, false)        // Pointer (byref) to a static variable (or possibly a field thereof, if the static variable is a struct).  Args: 0: FieldSeq, first element
                                                     // of which is the static var.
ValueNumFuncDef(Phi, 2, false, false, false)        // A phi function.  Only occurs as arg of PhiDef or PhiMemoryDef.  Arguments are SSA numbers of var being defined.
ValueNumFuncDef(PhiDef, 3, false, false, false)     // Args: 0: local var # (or -1 for memory), 1: SSA #, 2: VN of definition.
// Wouldn't need this if I'd made memory a regular local variable...
ValueNumFuncDef(PhiMemoryDef, 2, false, false, false) // Args: 0: VN for basic block pointer, 1: VN of definition
ValueNumFuncDef(InitVal, 1, false, false, false)    // An input arg, or init val of a local Args: 0: a constant VN.



ValueNumFuncDef(Cast, 2, false, false, false)           // VNF_Cast: Cast Operation changes the representations size and unsigned-ness.
                                                        //           Args: 0: Source for the cast operation.
                                                        //                 1: Constant integer representing the operation .
                                                        //                    Use VNForCastOper() to construct.
ValueNumFuncDef(CastOvf, 2, false, false, false)        // Same as a VNF_Cast but also can throw an overflow exception.

ValueNumFuncDef(CastClass, 2, false, false, false)          // Args: 0: Handle of class being cast to, 1: object being cast.
ValueNumFuncDef(IsInstanceOf, 2, false, false, false)       // Args: 0: Handle of class being queried, 1: object being queried.
ValueNumFuncDef(ReadyToRunCastClass, 2, false, false, false)          // Args: 0: Helper stub address, 1: object being cast.
ValueNumFuncDef(ReadyToRunIsInstanceOf, 2, false, false, false)       // Args: 0: Helper stub address, 1: object being queried.
ValueNumFuncDef(TypeHandleToRuntimeType, 1, false, false, false)      // Args: 0: TypeHandle to translate
ValueNumFuncDef(TypeHandleToRuntimeTypeHandle, 1, false, false, false)      // Args: 0: TypeHandle to translate

ValueNumFuncDef(AreTypesEquivalent, 2, false, false, false) // Args: 0: first TypeHandle, 1: second TypeHandle

ValueNumFuncDef(LdElemA, 3, false, false, false)            // Args: 0: array value; 1: index value; 2: type handle of element.

ValueNumFuncDef(ByrefExposedLoad, 3, false, false, false)      // Args: 0: type handle/id, 1: pointer value; 2: ByrefExposed heap value

ValueNumFuncDef(GetRefanyVal, 2, false, false, false)       // Args: 0: type handle; 1: typedref value.  Returns the value (asserting that the type is right).

ValueNumFuncDef(GetClassFromMethodParam, 1, false, true, false)       // Args: 0: method generic argument.
ValueNumFuncDef(GetSyncFromClassHandle, 1, false, true, false)        // Args: 0: class handle.
ValueNumFuncDef(LoopCloneChoiceAddr, 0, false, true, false)

// How we represent values of expressions with exceptional side effects:
ValueNumFuncDef(ValWithExc, 2, false, false, false)         // Args: 0: value number from normal execution; 1: VN for set of possible exceptions.
ValueNumFuncDef(ExcSetCons, 2, false, false, false)         // Args: 0: exception; 1: exception set (including EmptyExcSet).  Invariant: "car"s are always in ascending order.

// Various functions that are used to indicate that an exceptions may occur
// Curremtly  when the execution is always thrown, the value VNForVoid() is used as Arg0 by OverflowExc and DivideByZeroExc
//
ValueNumFuncDef(NullPtrExc, 1, false, false, false)         // Null pointer exception check.  Args: 0: address value,  throws when it is null
ValueNumFuncDef(ArithmeticExc, 2, false, false, false)      // Arithmetic exception check, ckfinite and integer division overflow, Args: 0: expression value,
ValueNumFuncDef(OverflowExc, 1, false, false, false)        // Integer overflow check. used for checked add,sub and mul Args: 0: expression value,  throws when it overflows
ValueNumFuncDef(ConvOverflowExc, 2, false, false, false)    // Cast conversion overflow check.  Args: 0: input value; 1: var_types of the target type
                                                            // - (shifted left one bit; low bit encode whether source is unsigned.)
ValueNumFuncDef(DivideByZeroExc, 1, false, false, false)    // Division by zero check.  Args: 0: divisor value, throws when it is zero
ValueNumFuncDef(IndexOutOfRangeExc, 2, false, false, false) // Array bounds check, Args: 0: array length; 1: index value, throws when the bounds check fails.
ValueNumFuncDef(InvalidCastExc, 2, false, false, false)     // CastClass check, Args: 0: ref value being cast; 1: handle of type being cast to, throws when the cast fails.
ValueNumFuncDef(NewArrOverflowExc, 1, false, false, false)  // Raises Integer overflow when Arg 0 is negative
ValueNumFuncDef(HelperMultipleExc, 0, false, false, false)  // Represents one or more different exceptions that could be thrown by a Jit Helper method

ValueNumFuncDef(Lng2Dbl, 1, false, false, false)
ValueNumFuncDef(ULng2Dbl, 1, false, false, false)
ValueNumFuncDef(Dbl2Int, 1, false, false, false)
ValueNumFuncDef(Dbl2UInt, 1, false, false, false)
ValueNumFuncDef(Dbl2Lng, 1, false, false, false)
ValueNumFuncDef(Dbl2ULng, 1, false, false, false)
ValueNumFuncDef(Dbl2IntOvf, 1, false, false, false)
ValueNumFuncDef(Dbl2UIntOvf, 1, false, false, false)
ValueNumFuncDef(Dbl2LngOvf, 1, false, false, false)
ValueNumFuncDef(Dbl2ULngOvf, 1, false, false, false)
ValueNumFuncDef(FltRound, 1, false, false, false)
ValueNumFuncDef(DblRound, 1, false, false, false)

ValueNumFuncDef(Abs, 1, false, false, false)
ValueNumFuncDef(Acos, 1, false, false, false)
ValueNumFuncDef(Acosh, 1, false, false, false)
ValueNumFuncDef(Asin, 1, false, false, false)
ValueNumFuncDef(Asinh, 1, false, false, false)
ValueNumFuncDef(Atan, 1, false, false, false)
ValueNumFuncDef(Atanh, 1, false, false, false)
ValueNumFuncDef(Atan2, 2, false, false, false)
ValueNumFuncDef(Cbrt, 1, false, false, false)
ValueNumFuncDef(Ceiling, 1, false, false, false)
ValueNumFuncDef(Cos, 1, false, false, false)
ValueNumFuncDef(Cosh, 1, false, false, false)
ValueNumFuncDef(Exp, 1, false, false, false)
ValueNumFuncDef(Floor, 1, false, false, false)
ValueNumFuncDef(FMod, 2, false, false, false)
ValueNumFuncDef(ILogB, 1, false, false, false)
ValueNumFuncDef(Log, 1, false, false, false)
ValueNumFuncDef(Log2, 1, false, false, false)
ValueNumFuncDef(Log10, 1, false, false, false)
ValueNumFuncDef(Pow, 2, false, false, false)
ValueNumFuncDef(RoundDouble, 1, false, false, false)
ValueNumFuncDef(RoundInt32, 1, false, false, false)
ValueNumFuncDef(RoundSingle, 1, false, false, false)
ValueNumFuncDef(Sin, 1, false, false, false)
ValueNumFuncDef(Sinh, 1, false, false, false)
ValueNumFuncDef(Sqrt, 1, false, false, false)
ValueNumFuncDef(Tan, 1, false, false, false)
ValueNumFuncDef(Tanh, 1, false, false, false)

ValueNumFuncDef(ManagedThreadId, 0, false, false, false)

ValueNumFuncDef(ObjGetType, 1, false, false, false)
ValueNumFuncDef(GetgenericsGcstaticBase, 1, false, true, true)
ValueNumFuncDef(GetgenericsNongcstaticBase, 1, false, true, true)
ValueNumFuncDef(GetsharedGcstaticBase, 2, false, true, true)
ValueNumFuncDef(GetsharedNongcstaticBase, 2, false, true, true)
ValueNumFuncDef(GetsharedGcstaticBaseNoctor, 1, false, true, true)
ValueNumFuncDef(GetsharedNongcstaticBaseNoctor, 1, false, true, true)
ValueNumFuncDef(ReadyToRunStaticBase, 1, false, true, true)
ValueNumFuncDef(ReadyToRunGenericStaticBase, 2, false, true, true)
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
ValueNumFuncDef(ReadyToRunGenericHandle, 2, false, true, false)

ValueNumFuncDef(GetStaticAddrContext, 1, false, true, false)
ValueNumFuncDef(GetStaticAddrTLS, 1, false, true, false)

ValueNumFuncDef(JitNew, 2, false, true, false)
ValueNumFuncDef(JitNewArr, 3, false, true, false)
ValueNumFuncDef(JitReadyToRunNew, 2, false, true, false)
ValueNumFuncDef(JitReadyToRunNewArr, 3, false, true, false)
ValueNumFuncDef(Box, 3, false, false, false)
ValueNumFuncDef(BoxNullable, 3, false, false, false)

ValueNumFuncDef(LazyStrCns, 2, false, true, false)  // lazy-initialized string literal (helper)
ValueNumFuncDef(NonNullIndirect, 1, false, true, false)  // this indirect is expected to always return a non-null value
ValueNumFuncDef(Unbox, 2, false, true, false)

ValueNumFuncDef(LT_UN, 2, false, false, false)      // unsigned or unordered comparisons
ValueNumFuncDef(LE_UN, 2, false, false, false)
ValueNumFuncDef(GE_UN, 2, false, false, false)
ValueNumFuncDef(GT_UN, 2, false, false, false)

ValueNumFuncDef(ADD_OVF, 2, true, false, false)     // overflow checking operations
ValueNumFuncDef(SUB_OVF, 2, false, false, false)
ValueNumFuncDef(MUL_OVF, 2, true, false, false)

ValueNumFuncDef(ADD_UN_OVF, 2, true, false, false)  // unsigned overflow checking operations
ValueNumFuncDef(SUB_UN_OVF, 2, false, false, false)
ValueNumFuncDef(MUL_UN_OVF, 2, true, false, false)

#ifdef FEATURE_SIMD
ValueNumFuncDef(SimdType, 2, false, false, false)  // A value number function to compose a SIMD type
#endif

#define SIMD_INTRINSIC(m, i, id, n, r, argCount, arg1, arg2, arg3, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) \
ValueNumFuncDef(SIMD_##id, argCount, false, false, false)   // All of the SIMD intrinsic  (Consider isCommutativeSIMDIntrinsic)
#include "simdintrinsiclist.h"
#define VNF_SIMD_FIRST VNF_SIMD_None

#if defined(TARGET_XARCH)
#define HARDWARE_INTRINSIC(isa, name, size, argCount, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
ValueNumFuncDef(HWI_##isa##_##name, argCount, false, false, false)   // All of the HARDWARE_INTRINSICS for x86/x64
#include "hwintrinsiclistxarch.h"
#define VNF_HWI_FIRST VNF_HWI_Vector128_As

#elif defined (TARGET_ARM64)
#define HARDWARE_INTRINSIC(isa, name, size, argCount, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
ValueNumFuncDef(HWI_##isa##_##name, argCount, false, false, false)   // All of the HARDWARE_INTRINSICS for arm64
#include "hwintrinsiclistarm64.h"
#define VNF_HWI_FIRST VNF_HWI_Vector64_As

#elif defined (TARGET_ARM)
// No Hardware Intrinsics on ARM32
#else
#error Unsupported platform
#endif

// clang-format on

#undef ValueNumFuncDef
