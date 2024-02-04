// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Defines the functions understood by the value-numbering system.
// ValueNumFuncDef(<name of function>, <arity (1-4)>, <is-commutative (for arity = 2)>, <non-null (for gc functions)>,
// <is-shared-static>, <encodes-extra-type-arg>)

// clang-format off
ValueNumFuncDef(MemOpaque, 1, false, false, false, false)          // Args: 0: loop num
ValueNumFuncDef(MapSelect, 2, false, false, false, false)          // Args: 0: map, 1: key.
ValueNumFuncDef(MapStore, 4, false, false, false, false)           // Args: 0: map, 1: index (e. g. field handle), 2: value being stored, 3: loop num.
ValueNumFuncDef(MapPhysicalStore, 3, false, false, false, false)   // Args: 0: map, 1: "physical selector": offset and size, 2: value being stored
ValueNumFuncDef(BitCast, 2, false, false, false, false)            // Args: 0: VN of the arg, 1: VN of the target type
ValueNumFuncDef(ZeroObj, 1, false, false, false, false)            // Args: 0: VN of the class handle.
ValueNumFuncDef(PhiDef, 3, false, false, false, false)             // Args: 0: local var # (or -1 for memory), 1: SSA #, 2: VN of definition.
ValueNumFuncDef(PhiMemoryDef, 2, false, false, false, false)       // Args: 0: VN for basic block pointer, 1: VN of definition
ValueNumFuncDef(Phi, 2, false, false, false, false)                // A phi function.  Only occurs as arg of PhiDef or PhiMemoryDef.  Arguments are SSA numbers of var being defined.

ValueNumFuncDef(PtrToLoc, 2, false, true, false, false)            // Pointer (byref) to a local variable.  Args: VN's of: 0: local's number, 1: offset.
ValueNumFuncDef(PtrToArrElem, 4, false, false, false, false)       // Pointer (byref) to an array element.  Args: 0: array elem type eq class var_types value, VN's of: 1: array, 2: index, 3: offset.
ValueNumFuncDef(PtrToStatic, 3, false, true, false, false)         // Pointer (byref) to a static variable (or possibly a field thereof, if the static variable is a struct).
                                                                   // Args: 0: (VN of) the box's address if the static is "boxed",
                                                                   //       1: (VN of) the field sequence,
                                                                   //       2: (VN of) offset for the constituent struct fields

ValueNumFuncDef(MDArrLength, 2, false, false, false, false)        // MD array len, Args: 0: array, 1: dimension
ValueNumFuncDef(MDArrLowerBound, 2, false, false, false, false)    // MD array lower bound, Args: 0: array, 1: dimension

ValueNumFuncDef(InitVal, 1, false, false, false, false)    // An input arg, or init val of a local Args: 0: a constant VN.

ValueNumFuncDef(Cast, 2, false, false, false, false)           // VNF_Cast: Cast Operation changes the representations size and unsigned-ness.
                                                               //           Args: 0: Source for the cast operation.
                                                               //                 1: Constant integer representing the operation .
                                                               //                    Use VNForCastOper() to construct.
ValueNumFuncDef(CastOvf, 2, false, false, false, false)        // Same as a VNF_Cast but also can throw an overflow exception.

ValueNumFuncDef(CastClass, 2, false, false, false, false)          // Args: 0: Handle of class being cast to, 1: object being cast.
ValueNumFuncDef(IsInstanceOf, 2, false, false, false, false)       // Args: 0: Handle of class being queried, 1: object being queried.
ValueNumFuncDef(ReadyToRunCastClass, 2, false, false, false, false)          // Args: 0: Helper stub address, 1: object being cast.
ValueNumFuncDef(ReadyToRunIsInstanceOf, 2, false, false, false, false)       // Args: 0: Helper stub address, 1: object being queried.
ValueNumFuncDef(TypeHandleToRuntimeType, 1, false, false, false, false)      // Args: 0: TypeHandle to translate
ValueNumFuncDef(TypeHandleToRuntimeTypeHandle, 1, false, false, false, false)      // Args: 0: TypeHandle to translate

ValueNumFuncDef(LdElemA, 3, false, false, false, false)            // Args: 0: array value; 1: index value; 2: type handle of element.

ValueNumFuncDef(ByrefExposedLoad, 3, false, false, false, false)      // Args: 0: type handle/id, 1: pointer value; 2: ByrefExposed heap value

ValueNumFuncDef(GetRefanyVal, 2, false, false, false, false)       // Args: 0: type handle; 1: typedref value.  Returns the value (asserting that the type is right).

ValueNumFuncDef(GetClassFromMethodParam, 1, false, true, false, false)       // Args: 0: method generic argument.
ValueNumFuncDef(GetSyncFromClassHandle, 1, false, true, false, false)        // Args: 0: class handle.
ValueNumFuncDef(LoopCloneChoiceAddr, 0, false, true, false, false)

// How we represent values of expressions with exceptional side effects:
ValueNumFuncDef(ValWithExc, 2, false, false, false, false)         // Args: 0: value number from normal execution; 1: VN for set of possible exceptions.
ValueNumFuncDef(ExcSetCons, 2, false, false, false, false)         // Args: 0: exception; 1: exception set (including EmptyExcSet).  Invariant: "car"s are always in ascending order.

// Various functions that are used to indicate that an exceptions may occur
// Curremtly  when the execution is always thrown, the value VNForVoid() is used as Arg0 by OverflowExc and DivideByZeroExc
//
ValueNumFuncDef(NullPtrExc, 1, false, false, false, false)         // Null pointer exception check.  Args: 0: address value,  throws when it is null
ValueNumFuncDef(ArithmeticExc, 2, false, false, false, false)      // Arithmetic exception check, ckfinite and integer division overflow, Args: 0: expression value,
ValueNumFuncDef(OverflowExc, 1, false, false, false, false)        // Integer overflow check. used for checked add,sub and mul Args: 0: expression value,  throws when it overflows
ValueNumFuncDef(ConvOverflowExc, 2, false, false, false, false)    // Cast conversion overflow check.  Args: 0: input value; 1: var_types of the target type
                                                            // - (shifted left one bit; low bit encode whether source is unsigned.)
ValueNumFuncDef(DivideByZeroExc, 1, false, false, false, false)    // Division by zero check.  Args: 0: divisor value, throws when it is zero
ValueNumFuncDef(IndexOutOfRangeExc, 2, false, false, false, false) // Array bounds check, Args: 0: array length; 1: index value, throws when the bounds check fails.
ValueNumFuncDef(InvalidCastExc, 2, false, false, false, false)     // CastClass check, Args: 0: ref value being cast; 1: handle of type being cast to, throws when the cast fails.
ValueNumFuncDef(NewArrOverflowExc, 1, false, false, false, false)  // Raises Integer overflow when Arg 0 is negative
ValueNumFuncDef(HelperMultipleExc, 0, false, false, false, false)  // Represents one or more different exceptions that could be thrown by a Jit Helper method

ValueNumFuncDef(FltRound, 1, false, false, false, false)
ValueNumFuncDef(DblRound, 1, false, false, false, false)

ValueNumFuncDef(Abs, 1, false, false, false, false)
ValueNumFuncDef(Acos, 1, false, false, false, false)
ValueNumFuncDef(Acosh, 1, false, false, false, false)
ValueNumFuncDef(Asin, 1, false, false, false, false)
ValueNumFuncDef(Asinh, 1, false, false, false, false)
ValueNumFuncDef(Atan, 1, false, false, false, false)
ValueNumFuncDef(Atanh, 1, false, false, false, false)
ValueNumFuncDef(Atan2, 2, false, false, false, false)
ValueNumFuncDef(Cbrt, 1, false, false, false, false)
ValueNumFuncDef(Ceiling, 1, false, false, false, false)
ValueNumFuncDef(Cos, 1, false, false, false, false)
ValueNumFuncDef(Cosh, 1, false, false, false, false)
ValueNumFuncDef(Exp, 1, false, false, false, false)
ValueNumFuncDef(Floor, 1, false, false, false, false)
ValueNumFuncDef(FMod, 2, false, false, false, false)
ValueNumFuncDef(ILogB, 1, false, false, false, false)
ValueNumFuncDef(Log, 1, false, false, false, false)
ValueNumFuncDef(Log2, 1, false, false, false, false)
ValueNumFuncDef(Log10, 1, false, false, false, false)
ValueNumFuncDef(Max, 2, false, false, false, false)
ValueNumFuncDef(MaxMagnitude, 2, false, false, false, false)
ValueNumFuncDef(MaxMagnitudeNumber, 2, false, false, false, false)
ValueNumFuncDef(MaxNumber, 2, false, false, false, false)
ValueNumFuncDef(Min, 2, false, false, false, false)
ValueNumFuncDef(MinMagnitude, 2, false, false, false, false)
ValueNumFuncDef(MinMagnitudeNumber, 2, false, false, false, false)
ValueNumFuncDef(MinNumber, 2, false, false, false, false)
ValueNumFuncDef(Pow, 2, false, false, false, false)
ValueNumFuncDef(RoundDouble, 1, false, false, false, false)
ValueNumFuncDef(RoundInt32, 1, false, false, false, false)
ValueNumFuncDef(RoundSingle, 1, false, false, false, false)
ValueNumFuncDef(Sin, 1, false, false, false, false)
ValueNumFuncDef(Sinh, 1, false, false, false, false)
ValueNumFuncDef(Sqrt, 1, false, false, false, false)
ValueNumFuncDef(Tan, 1, false, false, false, false)
ValueNumFuncDef(Tanh, 1, false, false, false, false)
ValueNumFuncDef(Truncate, 1, false, false, false, false)

ValueNumFuncDef(ManagedThreadId, 0, false, false, false, false)

ValueNumFuncDef(ObjGetType, 1, false, true, false, false)
ValueNumFuncDef(GetgenericsGcstaticBase, 1, false, true, true, false)
ValueNumFuncDef(GetgenericsNongcstaticBase, 1, false, true, true, false)
ValueNumFuncDef(GetsharedGcstaticBase, 2, false, true, true, false)
ValueNumFuncDef(GetsharedNongcstaticBase, 2, false, true, true, false)
ValueNumFuncDef(GetsharedGcstaticBaseNoctor, 1, false, true, true, false)
ValueNumFuncDef(GetsharedNongcstaticBaseNoctor, 1, false, true, true, false)
ValueNumFuncDef(ReadyToRunStaticBaseGC, 1, false, true, true, false)
ValueNumFuncDef(ReadyToRunStaticBaseNonGC, 1, false, true, true, false)
ValueNumFuncDef(ReadyToRunStaticBaseThread, 1, false, true, true, false)
ValueNumFuncDef(ReadyToRunStaticBaseThreadNoctor, 1, false, true, true, false)
ValueNumFuncDef(ReadyToRunStaticBaseThreadNonGC, 1, false, true, true, false)
ValueNumFuncDef(ReadyToRunGenericStaticBase, 2, false, true, true, false)
ValueNumFuncDef(GetsharedGcstaticBaseDynamicclass, 2, false, true, true, false)
ValueNumFuncDef(GetsharedNongcstaticBaseDynamicclass, 2, false, true, true, false)
ValueNumFuncDef(GetgenericsGcthreadstaticBase, 1, false, true, true, false)
ValueNumFuncDef(GetgenericsNongcthreadstaticBase, 1, false, true, true, false)
ValueNumFuncDef(GetsharedGcthreadstaticBase, 2, false, true, true, false)
ValueNumFuncDef(GetsharedNongcthreadstaticBase, 2, false, true, true, false)
ValueNumFuncDef(GetsharedGcthreadstaticBaseNoctor, 2, false, true, true, false)
ValueNumFuncDef(GetsharedGcthreadstaticBaseNoctorOptimized, 1, false, true, true, false)
ValueNumFuncDef(GetsharedNongcthreadstaticBaseNoctor, 2, false, true, true, false)
ValueNumFuncDef(GetsharedNongcthreadstaticBaseNoctorOptimized, 1, false, true, true, false)
ValueNumFuncDef(GetsharedGcthreadstaticBaseDynamicclass, 2, false, true, true, false)
ValueNumFuncDef(GetsharedNongcthreadstaticBaseDynamicclass, 2, false, true, true, false)

ValueNumFuncDef(ClassinitSharedDynamicclass, 2, false, false, false, false)
ValueNumFuncDef(RuntimeHandleMethod, 2, false, true, false, false)
ValueNumFuncDef(RuntimeHandleClass, 2, false, true, false, false)
ValueNumFuncDef(ReadyToRunGenericHandle, 2, false, true, false, false)

ValueNumFuncDef(GetStaticAddrTLS, 1, false, true, false, false)

ValueNumFuncDef(JitNew, 2, false, true, false, false)
ValueNumFuncDef(JitNewArr, 3, false, true, false, false)
ValueNumFuncDef(JitNewMdArr, 4, false, true, false, false)
ValueNumFuncDef(JitReadyToRunNew, 2, false, true, false, false)
ValueNumFuncDef(JitReadyToRunNewArr, 3, false, true, false, false)
ValueNumFuncDef(Box, 3, false, true, false, false)
ValueNumFuncDef(BoxNullable, 3, false, false, false, false)

ValueNumFuncDef(LazyStrCns, 2, false, true, false, false)            // Lazy-initialized string literal (helper)
ValueNumFuncDef(InvariantLoad, 1, false, false, false, false)        // Args: 0: (VN of) the address.
ValueNumFuncDef(InvariantNonNullLoad, 1, false, true, false, false)  // Args: 0: (VN of) the address.
ValueNumFuncDef(Unbox, 2, false, false, false, false)

ValueNumFuncDef(LT_UN, 2, false, false, false, false)      // unsigned or unordered comparisons
ValueNumFuncDef(LE_UN, 2, false, false, false, false)
ValueNumFuncDef(GE_UN, 2, false, false, false, false)
ValueNumFuncDef(GT_UN, 2, false, false, false, false)

ValueNumFuncDef(ADD_OVF, 2, true, false, false, false)     // overflow checking operations
ValueNumFuncDef(SUB_OVF, 2, false, false, false, false)
ValueNumFuncDef(MUL_OVF, 2, true, false, false, false)

ValueNumFuncDef(ADD_UN_OVF, 2, true, false, false, false)  // unsigned overflow checking operations
ValueNumFuncDef(SUB_UN_OVF, 2, false, false, false, false)
ValueNumFuncDef(MUL_UN_OVF, 2, true, false, false, false)

#ifdef FEATURE_SIMD
ValueNumFuncDef(SimdType, 2, false, false, false, false)  // A value number function to compose a SIMD type
#endif

#if defined(TARGET_XARCH)
#define HARDWARE_INTRINSIC(isa, name, size, argCount, extra, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
ValueNumFuncDef(HWI_##isa##_##name, argCount, ((flag) & HW_Flag_Commutative) >> 0, false, false, extra)   // All of the HARDWARE_INTRINSICS for x86/x64
#include "hwintrinsiclistxarch.h"
#define VNF_HWI_FIRST VNF_HWI_Vector128_Abs

#elif defined (TARGET_ARM64)
#define HARDWARE_INTRINSIC(isa, name, size, argCount, extra, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
ValueNumFuncDef(HWI_##isa##_##name, argCount, ((flag) & HW_Flag_Commutative) >> 0, false, false, extra)   // All of the HARDWARE_INTRINSICS for arm64
#include "hwintrinsiclistarm64.h"
#define VNF_HWI_FIRST VNF_HWI_Vector64_Abs

#elif defined (TARGET_ARM)
// No Hardware Intrinsics on ARM32

#elif defined (TARGET_LOONGARCH64)
    //TODO-LOONGARCH64-CQ: add LoongArch64's Hardware Intrinsics Instructions if supported.

#elif defined (TARGET_RISCV64)
    //TODO-RISCV64-CQ: add RISCV64's Hardware Intrinsics Instructions if supported.

#else
#error Unsupported platform
#endif

// clang-format on

#undef ValueNumFuncDef
