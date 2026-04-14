// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Defines the functions understood by the value-numbering system.
// ValueNumFuncDef(<name of function>, <arity (-1, 0-4; -1 for variadic HW intrinsics)>,
// <non-null (for gc functions)>)

// clang-format off
ValueNumFuncDef(MemOpaque, 1, false)          // Args: 0: loop num
ValueNumFuncDef(MapSelect, 2, false)          // Args: 0: map, 1: key.
ValueNumFuncDef(MapStore, 4, false)           // Args: 0: map, 1: index (e. g. field handle), 2: value being stored, 3: loop num.
ValueNumFuncDef(MapPhysicalStore, 3, false)   // Args: 0: map, 1: "physical selector": offset and size, 2: value being stored
ValueNumFuncDef(BitCast, 2, false)            // Args: 0: VN of the arg, 1: VN of the target type
ValueNumFuncDef(ZeroObj, 1, false)            // Args: 0: VN of the class handle.

ValueNumFuncDef(PtrToLoc, 2, true)            // Pointer (byref) to a local variable.  Args: VN's of: 0: local's number, 1: offset.
ValueNumFuncDef(PtrToArrElem, 4, false)       // Pointer (byref) to an array element.  Args: 0: array elem type eq class var_types value, VN's of: 1: array, 2: index, 3: offset.
ValueNumFuncDef(PtrToStatic, 3, true)         // Pointer (byref) to a static variable (or possibly a field thereof, if the static variable is a struct).
                                                                   // Args: 0: (VN of) the box's address if the static is "boxed",
                                                                   //       1: (VN of) the field sequence,
                                                                   //       2: (VN of) offset for the constituent struct fields

ValueNumFuncDef(MDArrLength, 2, false)        // MD array len, Args: 0: array, 1: dimension
ValueNumFuncDef(MDArrLowerBound, 2, false)    // MD array lower bound, Args: 0: array, 1: dimension

ValueNumFuncDef(InitVal, 1, false)    // An input arg, or init val of a local Args: 0: a constant VN.

ValueNumFuncDef(Cast, 2, false)           // VNF_Cast: Cast Operation changes the representations size and unsigned-ness.
                                                               //           Args: 0: Source for the cast operation.
                                                               //                 1: Constant integer representing the operation .
                                                               //                    Use VNForCastOper() to construct.
ValueNumFuncDef(CastOvf, 2, false)        // Same as a VNF_Cast but also can throw an overflow exception.

ValueNumFuncDef(CastClass, 2, false)          // Args: 0: Handle of class being cast to, 1: object being cast.
ValueNumFuncDef(IsInstanceOf, 2, false)       // Args: 0: Handle of class being queried, 1: object being queried.
ValueNumFuncDef(ReadyToRunCastClass, 2, false)          // Args: 0: Helper stub address, 1: object being cast.
ValueNumFuncDef(ReadyToRunIsInstanceOf, 2, false)       // Args: 0: Helper stub address, 1: object being queried.
ValueNumFuncDef(TypeHandleToRuntimeType, 1, false)      // Args: 0: TypeHandle to translate
ValueNumFuncDef(TypeHandleToRuntimeTypeHandle, 1, false)      // Args: 0: TypeHandle to translate

ValueNumFuncDef(LdElemA, 3, false)            // Args: 0: array value; 1: index value; 2: type handle of element.

ValueNumFuncDef(ByrefExposedLoad, 3, false)      // Args: 0: type handle/id, 1: pointer value; 2: ByrefExposed heap value

ValueNumFuncDef(GetRefanyVal, 2, false)       // Args: 0: type handle; 1: typedref value.  Returns the value (asserting that the type is right).

ValueNumFuncDef(GetClassFromMethodParam, 1, true)       // Args: 0: method generic argument.
ValueNumFuncDef(GetSyncFromClassHandle, 1, true)        // Args: 0: class handle.
ValueNumFuncDef(LoopCloneChoiceAddr, 0, true)

// How we represent values of expressions with exceptional side effects:
ValueNumFuncDef(ValWithExc, 2, false)         // Args: 0: value number from normal execution; 1: VN for set of possible exceptions.
ValueNumFuncDef(ExcSetCons, 2, false)         // Args: 0: exception; 1: exception set (including EmptyExcSet).  Invariant: "car"s are always in ascending order.

// Various functions that are used to indicate that exceptions may occur
// Currently when the execution is always thrown, the value VNForVoid() is used as Arg0 by OverflowExc and DivideByZeroExc
//
ValueNumFuncDef(NullPtrExc, 1, false)         // Null pointer exception check.  Args: 0: address value,  throws when it is null
ValueNumFuncDef(ArithmeticExc, 2, false)      // Arithmetic exception check, ckfinite and integer division overflow, Args: 0: expression value,
ValueNumFuncDef(OverflowExc, 1, false)        // Integer overflow check. used for checked add,sub and mul Args: 0: expression value,  throws when it overflows
ValueNumFuncDef(ConvOverflowExc, 2, false)    // Cast conversion overflow check.  Args: 0: input value; 1: var_types of the target type
                                                            // - (shifted left one bit; low bit encode whether source is unsigned.)
ValueNumFuncDef(DivideByZeroExc, 1, false)    // Division by zero check.  Args: 0: divisor value, throws when it is zero
ValueNumFuncDef(IndexOutOfRangeExc, 2, false) // Array bounds check, Args: 0: array length; 1: index value, throws when the bounds check fails.
ValueNumFuncDef(InvalidCastExc, 2, false)     // CastClass check, Args: 0: ref value being cast; 1: handle of type being cast to
ValueNumFuncDef(R2RInvalidCastExc, 2, false)  // CastClass check, Args: 0: ref value being cast; 1: entry point of R2R cast helper
ValueNumFuncDef(NewArrOverflowExc, 1, false)  // Raises Integer overflow when Arg 0 is negative
ValueNumFuncDef(DynamicClassInitExc, 1, false)       // Represents exceptions thrown by static constructor for class. Args: 0: VN of DynamicStaticsInfo
ValueNumFuncDef(ThreadClassInitExc, 1, false)       // Represents exceptions thrown by static constructor for class. Args: 0: VN of ThreadStaticsInfo
ValueNumFuncDef(R2RClassInitExc, 1, false)    // Represents exceptions thrown by static constructor for class. Args: 0: VN of R2R entry point
ValueNumFuncDef(ClassInitGenericExc, 2, false)// Represents exceptions thrown by static constructor for class. Args: 0: VN of class handle
ValueNumFuncDef(HelperOpaqueExc, 1, false)    // Represents opaque exceptions could be thrown by a JIT helper.
                                                                   // Args: 0: Input to helper that uniquely determines exceptions thrown.

ValueNumFuncDef(Abs, 1, false)
ValueNumFuncDef(Acos, 1, false)
ValueNumFuncDef(Acosh, 1, false)
ValueNumFuncDef(Asin, 1, false)
ValueNumFuncDef(Asinh, 1, false)
ValueNumFuncDef(Atan, 1, false)
ValueNumFuncDef(Atanh, 1, false)
ValueNumFuncDef(Atan2, 2, false)
ValueNumFuncDef(Cbrt, 1, false)
ValueNumFuncDef(Ceiling, 1, false)
ValueNumFuncDef(Cos, 1, false)
ValueNumFuncDef(Cosh, 1, false)
ValueNumFuncDef(Exp, 1, false)
ValueNumFuncDef(Floor, 1, false)
ValueNumFuncDef(ILogB, 1, false)
ValueNumFuncDef(Log, 1, false)
ValueNumFuncDef(Log2, 1, false)
ValueNumFuncDef(Log10, 1, false)
ValueNumFuncDef(Max, 2, false)
ValueNumFuncDef(MaxMagnitude, 2, false)
ValueNumFuncDef(MaxMagnitudeNumber, 2, false)
ValueNumFuncDef(MaxNumber, 2, false)
ValueNumFuncDef(Min, 2, false)
ValueNumFuncDef(MinMagnitude, 2, false)
ValueNumFuncDef(MinMagnitudeNumber, 2, false)
ValueNumFuncDef(MinNumber, 2, false)
ValueNumFuncDef(Pow, 2, false)
ValueNumFuncDef(RoundDouble, 1, false)
ValueNumFuncDef(RoundInt32, 1, false)
ValueNumFuncDef(RoundSingle, 1, false)
ValueNumFuncDef(Sin, 1, false)
ValueNumFuncDef(Sinh, 1, false)
ValueNumFuncDef(Sqrt, 1, false)
ValueNumFuncDef(Tan, 1, false)
ValueNumFuncDef(Tanh, 1, false)
ValueNumFuncDef(Truncate, 1, false)

ValueNumFuncDef(LeadingZeroCount, 1, false)
ValueNumFuncDef(TrailingZeroCount, 1, false)
ValueNumFuncDef(PopCount, 1, false)

ValueNumFuncDef(ManagedThreadId, 0, false)

ValueNumFuncDef(ObjGetType, 1, true)
ValueNumFuncDef(GetGcstaticBase, 1, true)
ValueNumFuncDef(GetNongcstaticBase, 1, true)
ValueNumFuncDef(GetdynamicGcstaticBase, 1, true)
ValueNumFuncDef(GetdynamicNongcstaticBase, 1, true)
ValueNumFuncDef(GetdynamicGcstaticBaseNoctor, 1, true)
ValueNumFuncDef(GetdynamicNongcstaticBaseNoctor, 1, true)
ValueNumFuncDef(ReadyToRunStaticBaseGC, 1, true)
ValueNumFuncDef(ReadyToRunStaticBaseNonGC, 1, true)
ValueNumFuncDef(ReadyToRunStaticBaseThread, 1, true)
ValueNumFuncDef(ReadyToRunStaticBaseThreadNoctor, 1, true)
ValueNumFuncDef(ReadyToRunStaticBaseThreadNonGC, 1, true)
ValueNumFuncDef(ReadyToRunGenericStaticBase, 2, true)
ValueNumFuncDef(GetpinnedGcstaticBase, 1, true)
ValueNumFuncDef(GetpinnedNongcstaticBase, 1, true)
ValueNumFuncDef(GetpinnedGcstaticBaseNoctor, 1, true)
ValueNumFuncDef(GetpinnedNongcstaticBaseNoctor, 1, true)
ValueNumFuncDef(GetGcthreadstaticBase, 1, true)
ValueNumFuncDef(GetNongcthreadstaticBase, 1, true)
ValueNumFuncDef(GetGcthreadstaticBaseNoctor, 1, true)
ValueNumFuncDef(GetNongcthreadstaticBaseNoctor, 1, true)
ValueNumFuncDef(GetdynamicGcthreadstaticBase, 1, true)
ValueNumFuncDef(GetdynamicNongcthreadstaticBase, 1, true)
ValueNumFuncDef(GetdynamicGcthreadstaticBaseNoctor, 1, true)
ValueNumFuncDef(GetdynamicGcthreadstaticBaseNoctorOptimized, 1, true)
ValueNumFuncDef(GetdynamicNongcthreadstaticBaseNoctor, 1, true)
ValueNumFuncDef(GetdynamicNongcthreadstaticBaseNoctorOptimized, 1, true)
ValueNumFuncDef(GetdynamicNongcthreadstaticBaseNoctorOptimized2, 1, true)
ValueNumFuncDef(GetdynamicNongcthreadstaticBaseNoctorOptimized2NoJitOpt, 1, true)

ValueNumFuncDef(RuntimeHandleMethod, 2, true)
ValueNumFuncDef(RuntimeHandleClass, 2, true)
ValueNumFuncDef(ReadyToRunGenericHandle, 2, true)

ValueNumFuncDef(GetStaticAddrTLS, 1, true)

ValueNumFuncDef(VirtualFuncPtr, 3, true)
ValueNumFuncDef(GVMLookupForSlot, 2, true)
ValueNumFuncDef(ReadyToRunVirtualFuncPtr, 2, true)

ValueNumFuncDef(JitNew, 2, true)
ValueNumFuncDef(JitNewArr, 3, true)
ValueNumFuncDef(JitNewLclArr, 3, true)
ValueNumFuncDef(JitNewMdArr, 4, true)
ValueNumFuncDef(JitReadyToRunNew, 2, true)
ValueNumFuncDef(JitReadyToRunNewArr, 3, true)
ValueNumFuncDef(JitReadyToRunNewLclArr, 3, true)
ValueNumFuncDef(Box, 3, true)
ValueNumFuncDef(BoxNullable, 3, false)

ValueNumFuncDef(InvariantLoad, 1, false)        // Args: 0: (VN of) the address.
ValueNumFuncDef(InvariantNonNullLoad, 1, true)  // Args: 0: (VN of) the address.
ValueNumFuncDef(Unbox, 2, false)
ValueNumFuncDef(Unbox_TypeTest, 2, false)

ValueNumFuncDef(LT_UN, 2, false)      // unsigned or unordered comparisons
ValueNumFuncDef(LE_UN, 2, false)
ValueNumFuncDef(GE_UN, 2, false)
ValueNumFuncDef(GT_UN, 2, false)

ValueNumFuncDef(ADD_OVF, 2, false)     // overflow checking operations
ValueNumFuncDef(SUB_OVF, 2, false)
ValueNumFuncDef(MUL_OVF, 2, false)

ValueNumFuncDef(ADD_UN_OVF, 2, false)  // unsigned overflow checking operations
ValueNumFuncDef(SUB_UN_OVF, 2, false)
ValueNumFuncDef(MUL_UN_OVF, 2, false)

#ifdef FEATURE_SIMD
ValueNumFuncDef(SimdType, 2, false)  // A value number function to compose a SIMD type
#endif

// In VN all HW intrinsics encode an extra arg for the base type (except when
// they are variadic), hence the +1 to the arg count below here.
#if defined(TARGET_XARCH)
#define HARDWARE_INTRINSIC(isa, name, size, argCount, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
ValueNumFuncDef(HWI_##isa##_##name, ((argCount == -1) ? -1 : (argCount + 1)), false)   // All of the HARDWARE_INTRINSICS for x86/x64
#include "hwintrinsiclistxarch.h"
#define VNF_HWI_FIRST VNF_HWI_Vector128_Abs
#define VNF_HWI_LAST  VNF_HWI_AVX512_XnorMask

#elif defined(TARGET_ARM64)
#define HARDWARE_INTRINSIC(isa, name, size, argCount, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
ValueNumFuncDef(HWI_##isa##_##name, ((argCount == -1) ? -1 : (argCount + 1)), false)   // All of the HARDWARE_INTRINSICS for arm64
#include "hwintrinsiclistarm64.h"
#define VNF_HWI_FIRST VNF_HWI_Vector64_Abs
#define VNF_HWI_LAST  VNF_HWI_Sve_ReverseElement_Predicates

#elif defined(TARGET_ARM)
// No Hardware Intrinsics on ARM32

#elif defined(TARGET_LOONGARCH64)
    //TODO-LOONGARCH64-CQ: add LoongArch64's Hardware Intrinsics Instructions if supported.

#elif defined (TARGET_RISCV64)
    // Signed/Unsigned integer min/max intrinsics
    ValueNumFuncDef(MinInt, 2, false)
    ValueNumFuncDef(MaxInt, 2, false)
    ValueNumFuncDef(MinInt_UN, 2, false)
    ValueNumFuncDef(MaxInt_UN, 2, false)

#elif defined(TARGET_WASM)
// No hardware intrinsics on WASM yet.

#else
#error Unsupported platform
#endif

// clang-format on

#undef ValueNumFuncDef
