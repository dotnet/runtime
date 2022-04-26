// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#if TARGET_ARM
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                        // CallDescrWorker has FloatArgumentRegisters parameter
#define CALLDESCR_FPARGREGSARERETURNREGS           // The return value floating point registers are the same as the argument registers
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define FEATURE_HFA
#elif TARGET_ARM64
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                        // CallDescrWorker has FloatArgumentRegisters parameter
#define CALLDESCR_FPARGREGSARERETURNREGS           // The return value floating point registers are the same as the argument registers
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define ENREGISTERED_PARAMTYPE_MAXSIZE
#define FEATURE_HFA
#elif TARGET_X86
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#define CALLINGCONVENTION_CALLEE_POPS
#elif TARGET_AMD64
#if UNIXAMD64
#define UNIX_AMD64_ABI
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#else
#endif
#define CALLDESCR_FPARGREGS                        // CallDescrWorker has FloatArgumentRegisters parameter
#define CALLDESCR_FPARGREGSARERETURNREGS           // The return value floating point registers are the same as the argument registers
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define ENREGISTERED_PARAMTYPE_MAXSIZE
#elif TARGET_WASM
#else
#error Unknown architecture!
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.Augments;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.Runtime;
using Internal.Runtime.CompilerServices;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.Runtime.CallConverter;

using ArgIterator = Internal.Runtime.CallConverter.ArgIterator;

namespace Internal.Runtime.TypeLoader
{
    public class CallConverterThunk
    {
        private static LowLevelList<IntPtr> s_allocatedThunks = new LowLevelList<IntPtr>();

        private static volatile object s_thunkPoolHeap;

        internal static IntPtr CommonInputThunkStub = IntPtr.Zero;
#if CALLDESCR_FPARGREGSARERETURNREGS
#else
#if TARGET_X86
        internal static IntPtr ReturnFloatingPointReturn4Thunk = IntPtr.Zero;
        internal static IntPtr ReturnFloatingPointReturn8Thunk = IntPtr.Zero;
#endif
#endif
        internal static IntPtr ReturnVoidReturnThunk = IntPtr.Zero;
        internal static IntPtr ReturnIntegerPointReturnThunk = IntPtr.Zero;

#if TARGET_X86
        // Correctness of using this data structure relies on thread static structs being allocated in a location which cannot be moved by the GC
        [ThreadStatic]
        internal static ReturnBlock t_NonArgRegisterReturnSpace;
#endif

        // CallingConventionConverter_CommonCallingStub indirection information structure
        // This is filled in during the class constructor for this type, and holds data
        // that is constant across all uses of the call conversion thunks. A pointer to
        // this is passed to each invocation of the common calling stub.
        internal struct CallingConventionConverter_CommonCallingStub_PointerData
        {
            public IntPtr ManagedCallConverterThunk;
            public IntPtr UniversalThunk;
        }

        // Wrapper class used for reference type parameters passed byref in dynamic invoker thunks
        internal class DynamicInvokeByRefArgObjectWrapper
        {
            internal object _object;
        }

        internal static CallingConventionConverter_CommonCallingStub_PointerData s_commonStubData;

        [DllImport("*", ExactSpelling = true, EntryPoint = "CallingConventionConverter_GetStubs")]
        private static extern unsafe void CallingConventionConverter_GetStubs(out IntPtr returnVoidStub,
                                                                      out IntPtr returnIntegerStub,
                                                                      out IntPtr commonStub
#if CALLDESCR_FPARGREGSARERETURNREGS
#else
                                                                     , out IntPtr returnFloatingPointReturn4Thunk,
                                                                       out IntPtr returnFloatingPointReturn8Thunk
#endif
                                                                     );

#if TARGET_ARM
        [DllImport("*", ExactSpelling = true, EntryPoint = "CallingConventionConverter_SpecifyCommonStubData")]
        private static extern unsafe void CallingConventionConverter_SpecifyCommonStubData(IntPtr commonStubData);
#endif

        private static bool s_callConverterThunk = CallConverterThunk_LazyCctor();

        private static unsafe bool CallConverterThunk_LazyCctor()
        {
#if TARGET_UNIX
            // TODO
#else
            CallingConventionConverter_GetStubs(out ReturnVoidReturnThunk, out ReturnIntegerPointReturnThunk, out CommonInputThunkStub
#if CALLDESCR_FPARGREGSARERETURNREGS
#else
                                               , out ReturnFloatingPointReturn4Thunk, out ReturnFloatingPointReturn8Thunk
#endif
                                                );
            s_commonStubData.ManagedCallConverterThunk = (IntPtr)(delegate*<IntPtr, IntPtr, IntPtr>)&CallConversionThunk;
            s_commonStubData.UniversalThunk = RuntimeAugments.GetUniversalTransitionThunk();
#if TARGET_ARM
            fixed (CallingConventionConverter_CommonCallingStub_PointerData* commonStubData = &s_commonStubData)
            {
                CallingConventionConverter_SpecifyCommonStubData((IntPtr)commonStubData);
            }
#endif //TARGET_ARM

#endif // TARGET_UNIX
            return true;
        }

        internal static bool GetByRefIndicatorAtIndex(int index, bool[] lookup)
        {
            if (lookup == null)
                return false;

            if (index < lookup.Length)
                return lookup[index];

            return false;
        }

        public enum ThunkKind
        {
            StandardToStandardInstantiating,
            StandardToGenericInstantiating,
            StandardToGenericInstantiatingIfNotHasThis,
            StandardToGeneric,
            StandardToGenericPassthruInstantiating,
            StandardToGenericPassthruInstantiatingIfNotHasThis,
            GenericToStandard,
            StandardUnboxing,
            StandardUnboxingAndInstantiatingGeneric,
            GenericToStandardWithTargetPointerArg,
            GenericToStandardWithTargetPointerArgAndParamArg,
            GenericToStandardWithTargetPointerArgAndMaybeParamArg,
            DelegateInvokeOpenStaticThunk,
            DelegateInvokeClosedStaticThunk,
            DelegateInvokeOpenInstanceThunk,
            DelegateInvokeInstanceClosedOverGenericMethodThunk,
            DelegateMulticastThunk,
            DelegateObjectArrayThunk,
            DelegateDynamicInvokeThunk,
            ReflectionDynamicInvokeThunk,
        }

        // WARNING: These constants are also declared in System.Private.CoreLib\src\System\Delegate.cs
        // Do not change their values unless you change the values decalred in Delegate.cs
        private const int MulticastThunk = 0;
        private const int ClosedStaticThunk = 1;
        private const int OpenStaticThunk = 2;
        private const int ClosedInstanceThunkOverGenericMethod = 3;
        private const int DelegateInvokeThunk = 4;
        private const int OpenInstanceThunk = 5;
        private const int ObjectArrayThunk = 6;


        public static unsafe IntPtr MakeThunk(ThunkKind thunkKind,
                                              IntPtr targetPointer,
                                              IntPtr instantiatingArg,
                                              bool hasThis, RuntimeTypeHandle[] parameters,
                                              bool[] byRefParameters,
                                              bool[] paramsByRefForced)
        {
            // Build thunk data
            TypeHandle thReturnType = new TypeHandle(GetByRefIndicatorAtIndex(0, byRefParameters), parameters[0]);
            TypeHandle[] thParameters = null;
            if (parameters.Length > 1)
            {
                thParameters = new TypeHandle[parameters.Length - 1];
                for (int i = 1; i < parameters.Length; i++)
                {
                    thParameters[i - 1] = new TypeHandle(GetByRefIndicatorAtIndex(i, byRefParameters), parameters[i]);
                }
            }

            int callConversionInfo = CallConversionInfo.RegisterCallConversionInfo(thunkKind, targetPointer, instantiatingArg, hasThis, thReturnType, thParameters, paramsByRefForced);
            return FindExistingOrAllocateThunk(callConversionInfo);
        }

        public static unsafe IntPtr MakeThunk(ThunkKind thunkKind, IntPtr targetPointer, RuntimeSignature methodSignature, IntPtr instantiatingArg, RuntimeTypeHandle[] typeArgs, RuntimeTypeHandle[] methodArgs)
        {
            int callConversionInfo = CallConversionInfo.RegisterCallConversionInfo(thunkKind, targetPointer, methodSignature, instantiatingArg, typeArgs, methodArgs);
            return FindExistingOrAllocateThunk(callConversionInfo);
        }

        internal static unsafe IntPtr MakeThunk(ThunkKind thunkKind, IntPtr targetPointer, IntPtr instantiatingArg, ArgIteratorData argIteratorData, bool[] paramsByRefForced)
        {
            int callConversionInfo = CallConversionInfo.RegisterCallConversionInfo(thunkKind, targetPointer, instantiatingArg, argIteratorData, paramsByRefForced);
            return FindExistingOrAllocateThunk(callConversionInfo);
        }

        private static unsafe IntPtr FindExistingOrAllocateThunk(int callConversionInfo)
        {
            IntPtr thunk = IntPtr.Zero;

            lock (s_allocatedThunks)
            {
                if (callConversionInfo < s_allocatedThunks.Count && s_allocatedThunks[callConversionInfo] != IntPtr.Zero)
                    return s_allocatedThunks[callConversionInfo];

                if (s_thunkPoolHeap == null)
                {
                    s_thunkPoolHeap = RuntimeAugments.CreateThunksHeap(CommonInputThunkStub);
                    Debug.Assert(s_thunkPoolHeap != null);
                }

                thunk = RuntimeAugments.AllocateThunk(s_thunkPoolHeap);
                Debug.Assert(thunk != IntPtr.Zero);

                fixed (CallingConventionConverter_CommonCallingStub_PointerData* commonStubData = &s_commonStubData)
                {
                    RuntimeAugments.SetThunkData(s_thunkPoolHeap, thunk, new IntPtr(callConversionInfo), new IntPtr(commonStubData));

                    if (callConversionInfo >= s_allocatedThunks.Count)
                    {
                        s_allocatedThunks.Expand(count: callConversionInfo + 1);
                    }
                    Debug.Assert(s_allocatedThunks[callConversionInfo] == IntPtr.Zero);
                    s_allocatedThunks[callConversionInfo] = thunk;
                }
            }

            return thunk;
        }

        public static unsafe IntPtr GetDelegateThunk(Delegate delegateObject, int thunkKind)
        {
            RuntimeTypeHandle delegateType = RuntimeAugments.GetRuntimeTypeHandleFromObjectReference(delegateObject);
            Debug.Assert(RuntimeAugments.IsGenericType(delegateType));

            RuntimeTypeHandle[] typeArgs;
            RuntimeAugments.GetGenericInstantiation(delegateType, out typeArgs);
            Debug.Assert(typeArgs != null && typeArgs.Length > 0);

            RuntimeSignature invokeMethodSignature;
            bool gotInvokeMethodSignature = TypeBuilder.TryGetDelegateInvokeMethodSignature(delegateType, out invokeMethodSignature);

            if (!gotInvokeMethodSignature)
            {
                Environment.FailFast("Unable to compute delegate invoke signature");
            }

            switch (thunkKind)
            {
                case DelegateInvokeThunk:
                    return CallConverterThunk.MakeThunk(CallConverterThunk.ThunkKind.DelegateDynamicInvokeThunk, IntPtr.Zero, invokeMethodSignature, IntPtr.Zero, typeArgs, null);

                case ObjectArrayThunk:
                    return CallConverterThunk.MakeThunk(CallConverterThunk.ThunkKind.DelegateObjectArrayThunk, IntPtr.Zero, invokeMethodSignature, IntPtr.Zero, typeArgs, null);

                case MulticastThunk:
                    return CallConverterThunk.MakeThunk(CallConverterThunk.ThunkKind.DelegateMulticastThunk, IntPtr.Zero, invokeMethodSignature, IntPtr.Zero, typeArgs, null);

                case OpenInstanceThunk:
                    return CallConverterThunk.MakeThunk(CallConverterThunk.ThunkKind.DelegateInvokeOpenInstanceThunk, IntPtr.Zero, invokeMethodSignature, IntPtr.Zero, typeArgs, null);

                case ClosedInstanceThunkOverGenericMethod:
                    return CallConverterThunk.MakeThunk(CallConverterThunk.ThunkKind.DelegateInvokeInstanceClosedOverGenericMethodThunk, IntPtr.Zero, invokeMethodSignature, IntPtr.Zero, typeArgs, null);

                case ClosedStaticThunk:
                    return CallConverterThunk.MakeThunk(CallConverterThunk.ThunkKind.DelegateInvokeClosedStaticThunk, IntPtr.Zero, invokeMethodSignature, IntPtr.Zero, typeArgs, null);

                case OpenStaticThunk:
                    return CallConverterThunk.MakeThunk(CallConverterThunk.ThunkKind.DelegateInvokeOpenStaticThunk, IntPtr.Zero, invokeMethodSignature, IntPtr.Zero, typeArgs, null);

                default:
                    Environment.FailFast("Invalid delegate thunk kind");
                    return IntPtr.Zero;
            }
        }

        public static unsafe bool TryGetNonUnboxingFunctionPointerFromUnboxingAndInstantiatingStub(IntPtr potentialStub, RuntimeTypeHandle exactType, out IntPtr nonUnboxingMethod)
        {
            IntPtr callConversionId;
            object thunkPoolHeap = s_thunkPoolHeap;
            if (thunkPoolHeap == null || !RuntimeAugments.TryGetThunkData(thunkPoolHeap, potentialStub, out callConversionId, out _))
            {
                // This isn't a call conversion stub
                nonUnboxingMethod = IntPtr.Zero;
                return false;
            }

            CallConversionInfo conversionInfo = CallConversionInfo.GetConverter(callConversionId.ToInt32());
            if (conversionInfo.IsUnboxingThunk)
            {
                // In this case the call converter is serving as an unboxing/instantiating stub
                // This case is not yet handled, and we don't need support for it yet.
                throw NotImplemented.ByDesign;
            }

            IntPtr underlyingTargetMethod;
            IntPtr newInstantiatingArg;

            if (conversionInfo.CalleeHasParamType)
            {
                // In this case the call converter is an instantiating stub wrapping an unboxing thunk.
                // Use the redhawk GetCodeTarget to see through the unboxing stub and get the real underlying method
                // and the instantiation arg does not need changing.
                underlyingTargetMethod = RuntimeAugments.GetCodeTarget(conversionInfo.TargetFunctionPointer);
                newInstantiatingArg = conversionInfo.InstantiatingStubArgument;
            }
            else
            {
                // At this point we've got a standard to generic converter wrapping an unboxing and instantiating
                // stub. We need to convert that into a fat function pointer directly calling the underlying method
                // or a calling convention converter instantiating stub wrapping the underlying method
                IntPtr underlyingUnboxingAndInstantiatingMethod = RuntimeAugments.GetCodeTarget(conversionInfo.TargetFunctionPointer);
                if (!TypeLoaderEnvironment.TryGetTargetOfUnboxingAndInstantiatingStub(underlyingUnboxingAndInstantiatingMethod, out underlyingTargetMethod))
                {
                    // We aren't wrapping an unboxing and instantiating stub. This should never happen
                    throw new NotSupportedException();
                }

                newInstantiatingArg = exactType.ToIntPtr();
            }

            Debug.Assert(conversionInfo.CallerForcedByRefData == null);

            bool canUseFatFunctionPointerInsteadOfThunk = true;
            if (conversionInfo.CalleeForcedByRefData != null)
            {
                foreach (bool forcedByRef in conversionInfo.CalleeForcedByRefData)
                {
                    if (forcedByRef)
                    {
                        canUseFatFunctionPointerInsteadOfThunk = false;
                        break;
                    }
                }
            }

            if (canUseFatFunctionPointerInsteadOfThunk)
            {
                nonUnboxingMethod = FunctionPointerOps.GetGenericMethodFunctionPointer(underlyingTargetMethod, newInstantiatingArg);
                return true;
            }
            else
            {
                // Construct a new StandardToGenericInstantiating thunk around the underlyingTargetMethod
                nonUnboxingMethod = MakeThunk(ThunkKind.StandardToGenericInstantiating,
                                 underlyingTargetMethod,
                                 newInstantiatingArg,
                                 conversionInfo.ArgIteratorData,
                                 conversionInfo.CalleeForcedByRefData);
                return true;
            }
        }

        public static unsafe bool TryGetCallConversionTargetPointerAndInstantiatingArg(IntPtr potentialStub, out IntPtr methodTarget, out IntPtr instantiatingArg)
        {
            methodTarget = instantiatingArg = IntPtr.Zero;

            IntPtr callConversionId;
            object thunkPoolHeap = s_thunkPoolHeap;
            if (thunkPoolHeap == null || !RuntimeAugments.TryGetThunkData(thunkPoolHeap, potentialStub, out callConversionId, out _))
            {
                // This isn't a call conversion stub
                return false;
            }

            CallConversionInfo conversionInfo = CallConversionInfo.GetConverter(callConversionId.ToInt32());
            if (!conversionInfo.HasKnownTargetPointerAndInstantiatingArgument)
                return false;

            methodTarget = conversionInfo.TargetFunctionPointer;
            instantiatingArg = conversionInfo.InstantiatingStubArgument;
            return true;
        }

        // This struct shares a layout with CallDescrData in the MRT codebase.
        internal unsafe struct CallDescrData
        {
            //
            // Input arguments
            //
            public void* pSrc;
            public int numStackSlots;
            public uint fpReturnSize;
            // Both of the following pointers are always present to reduce the spread of ifdefs in the C++ and ASM definitions of the struct
            public ArgumentRegisters* pArgumentRegisters;               // Not used by AMD64
            public FloatArgumentRegisters* pFloatArgumentRegisters;     // Not used by X86
            public void* pTarget;

            //
            // Return value
            //
            public void* pReturnBuffer;
        }

        // This function fills a piece of memory in a GC safe way.  It makes the guarantee
        // that it will fill memory in at least pointer sized chunks whenever possible.
        // Unaligned memory at the beginning and remaining bytes at the end are written bytewise.
        // We must make this guarantee whenever we clear memory in the GC heap that could contain
        // object references.  The GC or other user threads can read object references at any time,
        // clearing them bytewise can result in a read on another thread getting incorrect data.
        internal static unsafe void gcSafeMemzeroPointer(byte* pointer, int size)
        {
            byte* memBytes = pointer;
            byte* endBytes = (pointer + size);

            // handle unaligned bytes at the beginning
            while (!ArgIterator.IS_ALIGNED(new IntPtr(memBytes), (int)IntPtr.Size) && (memBytes < endBytes))
                *memBytes++ = (byte)0;

            // now write pointer sized pieces
            long nPtrs = (endBytes - memBytes) / IntPtr.Size;
            IntPtr* memPtr = (IntPtr*)memBytes;
            for (int i = 0; i < nPtrs; i++)
                *memPtr++ = IntPtr.Zero;

            // handle remaining bytes at the end
            memBytes = (byte*)memPtr;
            while (memBytes < endBytes)
                *memBytes++ = (byte)0;
        }

        internal static unsafe void memzeroPointer(byte* pointer, int size)
        {
            for (int i = 0; i < size; i++)
                pointer[i] = 0;
        }

        internal static unsafe void memzeroPointerAligned(byte* pointer, int size)
        {
            size = ArgIterator.ALIGN_UP(size, IntPtr.Size);
            size /= IntPtr.Size;

            for (int i = 0; i < size; i++)
            {
                ((IntPtr*)pointer)[i] = IntPtr.Zero;
            }
        }

        private static unsafe bool isPointerAligned(void* pointer)
        {
            if (sizeof(IntPtr) == 4)
            {
                return ((int)pointer & 3) == 0;
            }

            Debug.Assert(sizeof(IntPtr) == 8);
            return ((long)pointer & 7) == 0;
        }

#if CCCONVERTER_TRACE
        private static int s_numConversionsExecuted = 0;
#endif

        [DebuggerGuidedStepThroughAttribute]
        private static unsafe IntPtr CallConversionThunk(IntPtr callerTransitionBlockParam, IntPtr callConversionId)
        {
            CallConversionParameters conversionParams = default(CallConversionParameters);

            try
            {
                conversionParams = new CallConversionParameters(CallConversionInfo.GetConverter(callConversionId.ToInt32()), callerTransitionBlockParam);

#if CCCONVERTER_TRACE
                System.Threading.Interlocked.Increment(ref s_numConversionsExecuted);
                CallingConventionConverterLogger.WriteLine("CallConversionThunk executing... COUNT = " + s_numConversionsExecuted.LowLevelToString());
                CallingConventionConverterLogger.WriteLine("Executing thunk of type " + conversionParams._conversionInfo.ThunkKindString() + ": ");
#endif

                if (conversionParams._conversionInfo.IsMulticastDelegate)
                {
                    MulticastDelegateInvoke(ref conversionParams);
                    System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                else
                {
                    // Create a transition block on the stack.
                    // Note that SizeOfFrameArgumentArray does overflow checks with sufficient margin to prevent overflows here
                    int nStackBytes = conversionParams._calleeArgs.SizeOfFrameArgumentArray();
                    int dwAllocaSize = TransitionBlock.GetNegSpaceSize() + sizeof(TransitionBlock) + nStackBytes;

                    RuntimeAugments.RunFunctionWithConservativelyReportedBuffer(dwAllocaSize, &InvokeTarget, ref conversionParams);
                    System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }


                return conversionParams._invokeReturnValue;
            }
            finally
            {
                CallConversionParameters.ResetPinnedObjects();
            }
        }

        [DebuggerGuidedStepThroughAttribute]
        private static unsafe IntPtr MulticastDelegateInvoke(ref CallConversionParameters conversionParams)
        {
            // Create a transition block on the stack.
            // Note that SizeOfFrameArgumentArray does overflow checks with sufficient margin to prevent overflows here
            int nStackBytes = conversionParams._calleeArgs.SizeOfFrameArgumentArray();
            int dwAllocaSize = TransitionBlock.GetNegSpaceSize() + sizeof(TransitionBlock) + nStackBytes;

            for (int i = 0; i < conversionParams.MulticastDelegateCallCount; i++)
            {
                conversionParams.PrepareNextMulticastDelegateCall(i);
                conversionParams._copyReturnValue = (i == (conversionParams.MulticastDelegateCallCount - 1));

                RuntimeAugments.RunFunctionWithConservativelyReportedBuffer(dwAllocaSize, &InvokeTarget, ref conversionParams);
                System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            }

            return conversionParams._invokeReturnValue;
        }

        [DebuggerGuidedStepThroughAttribute]
        private static unsafe void InvokeTarget(void* allocatedStackBuffer, ref CallConversionParameters conversionParams)
        {
            byte* callerTransitionBlock = conversionParams._callerTransitionBlock;
            byte* calleeTransitionBlock = ((byte*)allocatedStackBuffer) + TransitionBlock.GetNegSpaceSize();

            //
            // Setup some of the special parameters on the output transition block
            //
            void* thisPointer = conversionParams.ThisPointer;
            void* callerRetBuffer = conversionParams.CallerReturnBuffer;
            void* VASigCookie = conversionParams.VarArgSigCookie;
            void* instantiatingStubArgument = (void*)conversionParams.InstantiatingStubArgument;
            {
                Debug.Assert((thisPointer != null && conversionParams._calleeArgs.HasThis()) || (thisPointer == null && !conversionParams._calleeArgs.HasThis()));
                if (thisPointer != null)
                {
                    *((void**)(calleeTransitionBlock + ArgIterator.GetThisOffset())) = thisPointer;
                }

                Debug.Assert((callerRetBuffer != null && conversionParams._calleeArgs.HasRetBuffArg()) || (callerRetBuffer == null && !conversionParams._calleeArgs.HasRetBuffArg()));
                if (callerRetBuffer != null)
                {
                    *((void**)(calleeTransitionBlock + conversionParams._calleeArgs.GetRetBuffArgOffset())) = callerRetBuffer;
                }

                Debug.Assert((VASigCookie != null && conversionParams._calleeArgs.IsVarArg()) || (VASigCookie == null && !conversionParams._calleeArgs.IsVarArg()));
                if (VASigCookie != null)
                {
                    *((void**)(calleeTransitionBlock + conversionParams._calleeArgs.GetVASigCookieOffset())) = VASigCookie;
                }

                Debug.Assert((instantiatingStubArgument != null && conversionParams._calleeArgs.HasParamType()) || (instantiatingStubArgument == null && !conversionParams._calleeArgs.HasParamType()));
                if (instantiatingStubArgument != null)
                {
                    *((void**)(calleeTransitionBlock + conversionParams._calleeArgs.GetParamTypeArgOffset())) = instantiatingStubArgument;
                }
            }
#if CCCONVERTER_TRACE
            if (thisPointer != null) CallingConventionConverterLogger.WriteLine("    ThisPtr  = " + new IntPtr(thisPointer).LowLevelToString());
            if (callerRetBuffer != null) CallingConventionConverterLogger.WriteLine("    RetBuf   = " + new IntPtr(callerRetBuffer).LowLevelToString());
            if (VASigCookie != null) CallingConventionConverterLogger.WriteLine("    VASig    = " + new IntPtr(VASigCookie).LowLevelToString());
            if (instantiatingStubArgument != null) CallingConventionConverterLogger.WriteLine("    InstArg  = " + new IntPtr(instantiatingStubArgument).LowLevelToString());
#endif

            object[] argumentsAsObjectArray = null;
            IntPtr pinnedResultObject = IntPtr.Zero;
            CallDescrData callDescrData = default(CallDescrData);
            IntPtr functionPointerToCall = conversionParams.FunctionPointerToCall;

            //
            // Setup the rest of the parameters on the ouput transition block by copying them from the input transition block
            //
            int ofsCallee;
            int ofsCaller;
            TypeHandle thDummy;
            TypeHandle thArgType;
            TypeHandle thRetType;
            IntPtr argPtr;
#if CALLDESCR_FPARGREGS
            FloatArgumentRegisters* pFloatArgumentRegisters = null;
#endif
            {
                uint arg = 0;

                while (true)
                {
                    // Setup argument offsets.
                    ofsCallee = conversionParams._calleeArgs.GetNextOffset();
                    ofsCaller = int.MaxValue;

                    // Check to see if we've handled all the arguments that we are to pass to the callee.
                    if (TransitionBlock.InvalidOffset == ofsCallee)
                    {
                        if (!conversionParams._conversionInfo.IsAnyDynamicInvokerThunk)
                            ofsCaller = conversionParams._callerArgs.GetNextOffset();
                        break;
                    }

#if CALLDESCR_FPARGREGS
                    // Under CALLDESCR_FPARGREGS -ve offsets indicate arguments in floating point registers. If we
                    // have at least one such argument we point the call worker at the floating point area of the
                    // frame (we leave it null otherwise since the worker can perform a useful optimization if it
                    // knows no floating point registers need to be set up).
                    if ((ofsCallee < 0) && (pFloatArgumentRegisters == null))
                        pFloatArgumentRegisters = (FloatArgumentRegisters*)(calleeTransitionBlock +
                                                                            TransitionBlock.GetOffsetOfFloatArgumentRegisters());
#endif

                    byte* pDest = calleeTransitionBlock + ofsCallee;
                    byte* pSrc = null;

                    int stackSizeCallee = int.MaxValue;
                    int stackSizeCaller = int.MaxValue;

                    bool isCalleeArgPassedByRef = false;
                    bool isCallerArgPassedByRef = false;

                    //
                    // Compute size and pointer to caller's arg
                    //
                    {
                        if (conversionParams._conversionInfo.IsClosedStaticDelegate)
                        {
                            if (arg == 0)
                            {
                                // Do not advance the caller's ArgIterator yet
                                argPtr = conversionParams.ClosedStaticDelegateThisPointer;
                                pSrc = (byte*)&argPtr;
                                stackSizeCaller = IntPtr.Size;
                                isCallerArgPassedByRef = false;
                            }
                            else
                            {
                                ofsCaller = conversionParams._callerArgs.GetNextOffset();
                                pSrc = callerTransitionBlock + ofsCaller;
                                stackSizeCaller = conversionParams._callerArgs.GetArgSize();
                                isCallerArgPassedByRef = conversionParams._callerArgs.IsArgPassedByRef();
                            }

                            stackSizeCallee = conversionParams._calleeArgs.GetArgSize();
                            isCalleeArgPassedByRef = conversionParams._calleeArgs.IsArgPassedByRef();
                        }
                        else if (conversionParams._conversionInfo.IsAnyDynamicInvokerThunk)
                        {
                            // The caller's ArgIterator for delegate or reflection dynamic invoke thunks has a different (and special) signature than
                            // the target method called by the delegate. We do not use it when setting up the callee's transition block.
                            // Input arguments are not read from the caller's transition block, but form the dynamic invoke infrastructure.
                            // Get all arguments info from the callee's ArgIterator instead.

                            int index;
                            InvokeUtils.DynamicInvokeParamLookupType paramLookupType;

                            RuntimeTypeHandle argumentRuntimeTypeHandle;
                            CorElementType argType = conversionParams._calleeArgs.GetArgType(out thArgType);
                            Debug.Assert(!thArgType.IsNull());

                            if (argType == CorElementType.ELEMENT_TYPE_BYREF)
                            {
                                TypeHandle thByRefArgType;
                                conversionParams._calleeArgs.GetByRefArgType(out thByRefArgType);
                                Debug.Assert(!thByRefArgType.IsNull());

                                argumentRuntimeTypeHandle = thByRefArgType.GetRuntimeTypeHandle();
                            }
                            else
                            {
                                // We need to check the exact type handle of the argument being passed during reflection invoke scenarios.
                                argumentRuntimeTypeHandle = thArgType.GetRuntimeTypeHandle();
                            }

                            IntPtr argSetupStatePtr = conversionParams.GetArgSetupStateDataPointer();
                            object invokeParam = InvokeUtils.DynamicInvokeParamHelperCore(
                                argSetupStatePtr,
                                argumentRuntimeTypeHandle,
                                out paramLookupType,
                                out index,
                                argType == CorElementType.ELEMENT_TYPE_BYREF ? InvokeUtils.DynamicInvokeParamType.Ref : InvokeUtils.DynamicInvokeParamType.In);

                            if (paramLookupType == InvokeUtils.DynamicInvokeParamLookupType.ValuetypeObjectReturned)
                            {
                                CallConversionParameters.s_pinnedGCHandles._dynamicInvokeArgHandle.Target = invokeParam;
                                argPtr = CallConversionParameters.s_pinnedGCHandles._dynamicInvokeArgHandle.GetRawTargetAddress() + IntPtr.Size;
                            }
                            else
                            {
                                Debug.Assert(paramLookupType == InvokeUtils.DynamicInvokeParamLookupType.IndexIntoObjectArrayReturned);
                                Debug.Assert((invokeParam is object[]) && (uint)index < (uint)((object[])invokeParam).Length);

                                CallConversionParameters.s_pinnedGCHandles._dynamicInvokeArgHandle.Target = ((object[])invokeParam)[index];
                                pinnedResultObject = CallConversionParameters.s_pinnedGCHandles._dynamicInvokeArgHandle.GetRawTargetAddress();

                                if (conversionParams._calleeArgs.IsArgPassedByRef())
                                {
                                    // We need to keep track of the array of parameters used by the InvokeUtils infrastructure, so we can copy
                                    // back results of byref parameters
                                    conversionParams._dynamicInvokeParams = conversionParams._dynamicInvokeParams ?? (object[])invokeParam;

                                    // Use wrappers to pass objects byref (Wrappers can handle both null and non-null input byref parameters)
                                    conversionParams._dynamicInvokeByRefObjectArgs = conversionParams._dynamicInvokeByRefObjectArgs ?? new DynamicInvokeByRefArgObjectWrapper[conversionParams._dynamicInvokeParams.Length];

                                    // The wrapper objects need to be pinned while we take the address of the byref'd object, and copy it to the callee
                                    // transition block (which is conservatively reported). Once the copy is done, we can safely unpin the wrapper object.
                                    if (pinnedResultObject == IntPtr.Zero)
                                    {
                                        // Input byref parameter has a null value
                                        conversionParams._dynamicInvokeByRefObjectArgs[index] = new DynamicInvokeByRefArgObjectWrapper();
                                    }
                                    else
                                    {
                                        // Input byref parameter has a non-null value
                                        conversionParams._dynamicInvokeByRefObjectArgs[index] = new DynamicInvokeByRefArgObjectWrapper { _object = conversionParams._dynamicInvokeParams[index] };
                                    }

                                    CallConversionParameters.s_pinnedGCHandles._dynamicInvokeArgHandle.Target = conversionParams._dynamicInvokeByRefObjectArgs[index];
                                    argPtr = CallConversionParameters.s_pinnedGCHandles._dynamicInvokeArgHandle.GetRawTargetAddress() + IntPtr.Size;
                                }
                                else
                                {
                                    argPtr = new IntPtr(&pinnedResultObject);
                                }
                            }

                            if (conversionParams._calleeArgs.IsArgPassedByRef())
                            {
                                pSrc = (byte*)&argPtr;
                            }
                            else
                            {
                                pSrc = (byte*)argPtr;
                            }

                            stackSizeCaller = stackSizeCallee = conversionParams._calleeArgs.GetArgSize();
                            isCallerArgPassedByRef = isCalleeArgPassedByRef = conversionParams._calleeArgs.IsArgPassedByRef();
                        }
                        else
                        {
                            ofsCaller = conversionParams._callerArgs.GetNextOffset();
                            pSrc = callerTransitionBlock + ofsCaller;

                            stackSizeCallee = conversionParams._calleeArgs.GetArgSize();
                            stackSizeCaller = conversionParams._callerArgs.GetArgSize();

                            isCalleeArgPassedByRef = conversionParams._calleeArgs.IsArgPassedByRef();
                            isCallerArgPassedByRef = conversionParams._callerArgs.IsArgPassedByRef();
                        }
                    }
                    Debug.Assert(stackSizeCallee == stackSizeCaller);


                    if (conversionParams._conversionInfo.IsObjectArrayDelegateThunk)
                    {
                        // Box (if needed) and copy arguments to an object array instead of the callee's transition block

                        argumentsAsObjectArray = argumentsAsObjectArray ?? new object[conversionParams._callerArgs.NumFixedArgs()];

                        conversionParams._callerArgs.GetArgType(out thArgType);
                        Debug.Assert(!thArgType.IsNull());

                        if (!thArgType.IsValueType())
                        {
                            Debug.Assert(!isCallerArgPassedByRef);
                            Debug.Assert(conversionParams._callerArgs.GetArgSize() == IntPtr.Size);
                            argumentsAsObjectArray[arg] = Unsafe.As<IntPtr, object>(ref *(IntPtr*)pSrc);
                        }
                        else
                        {
                            if (isCallerArgPassedByRef)
                            {
                                argumentsAsObjectArray[arg] = RuntimeAugments.Box(thArgType.GetRuntimeTypeHandle(), new IntPtr(*((void**)pSrc)));
                            }
                            else
                            {
                                argumentsAsObjectArray[arg] = RuntimeAugments.Box(thArgType.GetRuntimeTypeHandle(), new IntPtr(pSrc));
                            }
                        }
                    }
                    else
                    {
                        if (isCalleeArgPassedByRef == isCallerArgPassedByRef)
                        {
                            // Argument copies without adjusting calling convention.
                            if (isCalleeArgPassedByRef)
                            {
                                *((IntPtr*)pDest) = *(IntPtr*)pSrc;
                            }
                            else
                            {
                                CorElementType argElemType = conversionParams._calleeArgs.GetArgType(out thArgType);
                                ExtendingCopy_NoWriteBarrier(pSrc, pDest, argElemType, stackSizeCaller);
                            }
                        }
                        else
                        {
                            // Calling convention adjustment. Used to handle conversion from universal shared generic form to standard
                            // calling convention and vice versa
                            if (isCalleeArgPassedByRef)
                            {
                                // Pass as the byref pointer a pointer to the position in the transition block of the input argument
                                *((void**)pDest) = pSrc;
                            }
                            else
                            {
                                // Copy into the destination the data pointed at by the pointer in the source(caller) data.
                                byte* pRealSrc = *(byte**)pSrc;
                                CorElementType argElemType = conversionParams._calleeArgs.GetArgType(out thArgType);
                                ExtendingCopy_NoWriteBarrier(pRealSrc, pDest, argElemType, stackSizeCaller);
                            }
                        }

#if CCCONVERTER_TRACE
                        CallingConventionConverterLogger.WriteLine("    Arg" + arg.LowLevelToString() + " " +
                            (isCalleeArgPassedByRef ? "ref = " : "    = ") +
                            new IntPtr(*(void**)pDest).LowLevelToString() +
                            " - RTTH = " + conversionParams._calleeArgs.GetEETypeDebugName((int)arg) +
                            " - StackSize = " + stackSizeCallee.LowLevelToString());
#endif
                    }

                    if (conversionParams._conversionInfo.IsAnyDynamicInvokerThunk)
                    {
                        // The calleeTransitionBlock is GC-protected, so we can now safely unpin the return value of DynamicInvokeParamHelperCore,
                        // since we just copied it to the callee TB.
                        CallConversionParameters.s_pinnedGCHandles._dynamicInvokeArgHandle.Target = null;
                    }

                    arg++;
                }
            }

            if (conversionParams._conversionInfo.IsAnyDynamicInvokerThunk)
            {
                IntPtr argSetupStatePtr = conversionParams.GetArgSetupStateDataPointer();
                InvokeUtils.DynamicInvokeArgSetupPtrComplete(argSetupStatePtr);
            }

            uint fpReturnSize = conversionParams._calleeArgs.GetFPReturnSize();

            if (conversionParams._conversionInfo.IsObjectArrayDelegateThunk)
            {
                Debug.Assert(conversionParams._callerArgs.HasRetBuffArg() == conversionParams._calleeArgs.HasRetBuffArg());

                pinnedResultObject = conversionParams.InvokeObjectArrayDelegate(argumentsAsObjectArray);
            }
            else
            {
                if ((TransitionBlock.InvalidOffset != ofsCaller) != conversionParams._conversionInfo.CallerHasExtraParameterWhichIsFunctionTarget &&
                    !conversionParams._conversionInfo.IsAnyDynamicInvokerThunk)
                {
                    // The condition on the loop above is only verifying that callee has reach the end of its arguments.
                    // Here we check to see that caller has done so as well.
                    Environment.FailFast("Argument mismatch between caller and callee");
                }
                if (conversionParams._conversionInfo.CallerHasExtraParameterWhichIsFunctionTarget && !conversionParams._conversionInfo.CalleeMayHaveParamType)
                {
                    int stackSizeCaller = conversionParams._callerArgs.GetArgSize();
                    Debug.Assert(stackSizeCaller == IntPtr.Size);
                    void* pSrc = callerTransitionBlock + ofsCaller;
                    functionPointerToCall = *((IntPtr*)pSrc);

                    ofsCaller = conversionParams._callerArgs.GetNextOffset();
                    if (TransitionBlock.InvalidOffset != ofsCaller)
                    {
                        Environment.FailFast("Argument mismatch between caller and callee");
                    }
                }

                callDescrData.pSrc = calleeTransitionBlock + sizeof(TransitionBlock);
                callDescrData.numStackSlots = conversionParams._calleeArgs.SizeOfFrameArgumentArray() / ArchitectureConstants.STACK_ELEM_SIZE;
#if CALLDESCR_ARGREGS
                callDescrData.pArgumentRegisters = (ArgumentRegisters*)(calleeTransitionBlock + TransitionBlock.GetOffsetOfArgumentRegisters());
#endif
#if CALLDESCR_FPARGREGS
                callDescrData.pFloatArgumentRegisters = pFloatArgumentRegisters;
#endif
                callDescrData.fpReturnSize = fpReturnSize;
                callDescrData.pTarget = (void*)functionPointerToCall;

                ReturnBlock returnBlockForIgnoredData = default(ReturnBlock);
                if (conversionParams._callerArgs.HasRetBuffArg() == conversionParams._calleeArgs.HasRetBuffArg())
                {
                    // If there is no return buffer explictly in use, return to a buffer which is conservatively reported
                    // by the universal transition frame.
                    //  OR
                    // If there IS a return buffer in use, the function doesn't really return anything in the normal
                    // return value registers, but CallDescrThunk will always copy a pointer sized chunk into the
                    // ret buf. Make that ok by giving it a valid location to stash bits.
                    callDescrData.pReturnBuffer = (void*)(callerTransitionBlock + TransitionBlock.GetOffsetOfReturnValuesBlock());
                }
                else if (conversionParams._calleeArgs.HasRetBuffArg())
                {
                    // This is the case when the caller doesn't have a return buffer argument, but the callee does.
                    // In that case the return value captured by CallDescrWorker is ignored.

                    // When CallDescrWorkerInternal is called, have it return values into a temporary unused buffer
                    // In actuality its returning its return information into the return value block already, but that return buffer
                    // was setup as a passed in argument instead of being filled in by the CallDescrWorker function directly.
                    callDescrData.pReturnBuffer = (void*)&returnBlockForIgnoredData;
                }
                else
                {
                    // If there is no return buffer explictly in use by the callee, return to a buffer which is conservatively reported
                    // by the universal transition frame.

                    // This is the case where HasRetBuffArg is false for the callee, but the caller has a return buffer.
                    // In this case we need to capture the direct return value from callee into a buffer which may contain
                    // a gc reference (or not), and then once the call is complete, copy the value into the return buffer
                    // passed by the caller. (Do not directly use the return buffer provided by the caller, as CallDescrWorker
                    // does not properly use a write barrier, and the actual return buffer provided may be on the GC heap.)
                    callDescrData.pReturnBuffer = (void*)(callerTransitionBlock + TransitionBlock.GetOffsetOfReturnValuesBlock());
                }

                //////////////////////////////////////////////////////////////
                ////  Call the Callee
                //////////////////////////////////////////////////////////////
                RuntimeAugments.CallDescrWorker(new IntPtr(&callDescrData));
                System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            }

            // For dynamic invoke thunks, we need to copy back values of reference type parameters that were passed byref
            if (conversionParams._conversionInfo.IsAnyDynamicInvokerThunk && conversionParams._dynamicInvokeParams != null)
            {
                for (int i = 0; i < conversionParams._dynamicInvokeParams.Length; i++)
                {
                    if (conversionParams._dynamicInvokeByRefObjectArgs[i] == null)
                        continue;

                    object byrefObjectArgValue = conversionParams._dynamicInvokeByRefObjectArgs[i]._object;
                    conversionParams._dynamicInvokeParams[i] = byrefObjectArgValue;
                }
            }

            if (!conversionParams._copyReturnValue)
                return;

            bool forceByRefUnused;
            CorElementType returnType;
            // Note that the caller's ArgIterator for delegate dynamic invoke thunks has a different (and special) signature than
            // the target method called by the delegate. Use the callee's ArgIterator instead to get the return type info
            if (conversionParams._conversionInfo.IsAnyDynamicInvokerThunk)
            {
                returnType = conversionParams._calleeArgs.GetReturnType(out thRetType, out forceByRefUnused);
            }
            else
            {
                returnType = conversionParams._callerArgs.GetReturnType(out thRetType, out forceByRefUnused);
            }
            Debug.Assert(!thRetType.IsNull());
            int returnSize = TypeHandle.GetElemSize(returnType, thRetType);

            // Unbox result of object array delegate call
            if (conversionParams._conversionInfo.IsObjectArrayDelegateThunk && thRetType.IsValueType() && pinnedResultObject != IntPtr.Zero)
                pinnedResultObject += IntPtr.Size;

            // Process return values
            if ((conversionParams._callerArgs.HasRetBuffArg() && !conversionParams._calleeArgs.HasRetBuffArg()) ||
                (conversionParams._callerArgs.HasRetBuffArg() && conversionParams._conversionInfo.IsObjectArrayDelegateThunk))
            {
                // We should never get here for dynamic invoke thunks
                Debug.Assert(!conversionParams._conversionInfo.IsAnyDynamicInvokerThunk);

                // The CallDescrWorkerInternal function will have put the return value into the return buffer, as register return values are
                // extended to the size of a register, we can't just ask the CallDescrWorker to write directly into the return buffer.
                // Thus we copy only the correct amount of data here to the real target address
                byte* incomingRetBufPointer = *((byte**)(callerTransitionBlock + conversionParams._callerArgs.GetRetBuffArgOffset()));

                void* sourceBuffer = conversionParams._conversionInfo.IsObjectArrayDelegateThunk ? (void*)pinnedResultObject : callDescrData.pReturnBuffer;
                Debug.Assert(sourceBuffer != null || conversionParams._conversionInfo.IsObjectArrayDelegateThunk);

                if (sourceBuffer == null)
                {
                    // object array delegate thunk result is a null object. We'll fill the return buffer with 'returnSize' zeros in that case
                    gcSafeMemzeroPointer(incomingRetBufPointer, returnSize);
                }
                else
                {
                    // Because we are copying into a caller provided buffer, we can't use a simple memory copy, we need to use a
                    // gc protected copy as the actual return buffer may be on the heap.

                    bool useGCSafeCopy = false;

                    if ((returnType == CorElementType.ELEMENT_TYPE_CLASS) || thRetType.IsValueType())
                    {
                        // The GC Safe copy assumes that memory pointers are pointer-aligned and copy length is a multiple of pointer-size
                        if (isPointerAligned(incomingRetBufPointer) && isPointerAligned(sourceBuffer) && (returnSize % sizeof(IntPtr) == 0))
                        {
                            useGCSafeCopy = true;
                        }
                    }

                    if (useGCSafeCopy)
                    {
                        RuntimeAugments.BulkMoveWithWriteBarrier(new IntPtr(incomingRetBufPointer), new IntPtr(sourceBuffer), returnSize);
                    }
                    else
                    {
                        Buffer.MemoryCopy(sourceBuffer, incomingRetBufPointer, returnSize, returnSize);
                    }
                }

#if CALLINGCONVENTION_CALLEE_POPS
                // Don't setup the callee pop argument until after copying into the ret buff. We may be using the location
                // of the callee pop argument to keep track of the ret buff location
                SetupCallerPopArgument(callerTransitionBlock, conversionParams._callerArgs);
#endif
#if TARGET_X86
                SetupCallerActualReturnData(callerTransitionBlock);
                // On X86 the return buffer pointer is returned in eax.
                t_NonArgRegisterReturnSpace.returnValue = new IntPtr(incomingRetBufPointer);
                conversionParams._invokeReturnValue = ReturnIntegerPointReturnThunk;
                return;
#else
                // Because the return value was really returned on the heap, simply return as if void was returned.
                conversionParams._invokeReturnValue = ReturnVoidReturnThunk;
                return;
#endif
            }
            else
            {
#if CALLINGCONVENTION_CALLEE_POPS
                SetupCallerPopArgument(callerTransitionBlock, conversionParams._callerArgs);
#endif

                // The CallDescrWorkerInternal function will have put the return value into the return buffer.
                // Here we copy the return buffer data into the argument registers for the return thunks.
                //
                // A return thunk takes an argument(by value) that is what is to be returned.
                //
                // The simplest case is the one where there is no return value
                bool dummyBool;
                if (conversionParams._callerArgs.GetReturnType(out thDummy, out dummyBool) == CorElementType.ELEMENT_TYPE_VOID)
                {
                    conversionParams._invokeReturnValue = ReturnVoidReturnThunk;
                    return;
                }

                // The second simplest case is when there is a return buffer argument for both the caller and callee
                // In that case, we simply treat this as if we are returning void
#if TARGET_X86
                // Except on X86 where the return buffer is returned in the eax register, and looks like an integer return
#else
                if (conversionParams._callerArgs.HasRetBuffArg() && conversionParams._calleeArgs.HasRetBuffArg())
                {
                    Debug.Assert(!conversionParams._conversionInfo.IsObjectArrayDelegateThunk);
                    Debug.Assert(!conversionParams._conversionInfo.IsAnyDynamicInvokerThunk);
                    conversionParams._invokeReturnValue = ReturnVoidReturnThunk;
                    return;
                }
#endif

                void* returnValueToCopy = (void*)(callerTransitionBlock + TransitionBlock.GetOffsetOfReturnValuesBlock());

                if (conversionParams._conversionInfo.IsObjectArrayDelegateThunk)
                {
                    if (thRetType.IsValueType())
                    {
                        returnValueToCopy = (void*)pinnedResultObject;
#if TARGET_X86
                        Debug.Assert(returnSize <= sizeof(ReturnBlock));

                        if (returnValueToCopy == null)
                        {
                            // object array delegate thunk result is a null object. We'll fill the return buffer with 'returnSize' zeros in that case
                            memzeroPointer((byte*)(&((TransitionBlock*)callerTransitionBlock)->m_returnBlock), sizeof(ReturnBlock));
                        }
                        else
                        {
                            ExtendingCopy_WriteBarrier(returnValueToCopy, &((TransitionBlock*)callerTransitionBlock)->m_returnBlock, returnType, returnSize);
                        }
#endif
                    }
                    else
                    {
                        returnValueToCopy = (void*)&pinnedResultObject;
#if TARGET_X86
                        ((TransitionBlock*)callerTransitionBlock)->m_returnBlock.returnValue = pinnedResultObject;
#endif
                    }
                }
                else if (conversionParams._conversionInfo.IsAnyDynamicInvokerThunk &&
                    (thRetType.IsValueType() || thRetType.IsPointerType() || returnType == CorElementType.ELEMENT_TYPE_BYREF))
                {
                    Debug.Assert(returnValueToCopy != null);

                    if (returnType == CorElementType.ELEMENT_TYPE_VOID)
                    {
                        // Invokers returning void need to return a null object
                        returnValueToCopy = null;
                    }
                    else
                    {
                        if (!conversionParams._callerArgs.HasRetBuffArg() && conversionParams._calleeArgs.HasRetBuffArg())
                            returnValueToCopy = (void*)(new IntPtr(*((void**)returnValueToCopy)) + IntPtr.Size);

                        if (returnType == CorElementType.ELEMENT_TYPE_BYREF)
                        {
                            // If this is a byref return, we're going to dereference the result
                            returnValueToCopy = *(void**)returnValueToCopy;
                        }

                        RuntimeTypeHandle returnTypeRuntimeTypeHandle = thRetType.GetRuntimeTypeHandle();

                        // Need to box value type before returning it
                        object returnValue;
                        if (returnType == CorElementType.ELEMENT_TYPE_BYREF && returnValueToCopy == null)
                        {
                            // This is a byref return and dereferencing it would result in a NullReferenceException.
                            // Set the return value to a sentinel that InvokeUtils will recognize.
                            // Can't throw from here or we would wrap this in a TargetInvocationException.
                            returnValue = InvokeUtils.NullByRefValueSentinel;
                        }
                        else if (RuntimeAugments.IsUnmanagedPointerType(returnTypeRuntimeTypeHandle))
                        {
                            returnValue = System.Reflection.Pointer.Box(*(void**)returnValueToCopy, Type.GetTypeFromHandle(returnTypeRuntimeTypeHandle));
                        }
                        else if (RuntimeAugments.IsValueType(returnTypeRuntimeTypeHandle))
                        {
                            returnValue = RuntimeAugments.Box(returnTypeRuntimeTypeHandle, new IntPtr(returnValueToCopy));
                        }
                        else
                        {
                            // byref return of a reference type
                            Debug.Assert(returnType == CorElementType.ELEMENT_TYPE_BYREF);
                            returnValue = Unsafe.As<byte, object>(ref *(byte*)returnValueToCopy);
                        }
                        CallConversionParameters.s_pinnedGCHandles._returnObjectHandle.Target = returnValue;
                        pinnedResultObject = CallConversionParameters.s_pinnedGCHandles._returnObjectHandle.GetRawTargetAddress();
                        returnValueToCopy = (void*)&pinnedResultObject;
                    }
                    // Since we've changed the returnValueToCopy here, we need to update the idea of what we are returning
                    returnType = CorElementType.ELEMENT_TYPE_OBJECT;
                    thRetType = default(TypeHandle);
                    returnSize = TypeHandle.GetElemSize(returnType, thRetType);

#if TARGET_X86
                    ((TransitionBlock*)callerTransitionBlock)->m_returnBlock.returnValue = pinnedResultObject;
#endif
                }

                // Handle floating point returns

                // The previous fpReturnSize was the callee fpReturnSize. Now reset to the caller return size to handle
                // returning to the caller.
                fpReturnSize = conversionParams._callerArgs.GetFPReturnSize();
                if (fpReturnSize != 0)
                {
                    // We should never get here for delegate dynamic invoke thunks (the return type is always a boxed object)
                    Debug.Assert(!conversionParams._conversionInfo.IsAnyDynamicInvokerThunk);

#if CALLDESCR_FPARGREGSARERETURNREGS
                    Debug.Assert(fpReturnSize <= sizeof(FloatArgumentRegisters));
                    memzeroPointerAligned(callerTransitionBlock + TransitionBlock.GetOffsetOfFloatArgumentRegisters(), sizeof(FloatArgumentRegisters));
                    if (returnValueToCopy == null)
                    {
                        // object array delegate thunk result is a null object. We'll fill the return buffer with 'returnSize' zeros in that case
                        Debug.Assert(conversionParams._conversionInfo.IsObjectArrayDelegateThunk);
                        memzeroPointer(callerTransitionBlock + TransitionBlock.GetOffsetOfFloatArgumentRegisters(), (int)fpReturnSize);
                    }
                    else
                    {
                        Buffer.MemoryCopy(returnValueToCopy,
                                            callerTransitionBlock + TransitionBlock.GetOffsetOfFloatArgumentRegisters(),
                                            (int)fpReturnSize,
                                            (int)fpReturnSize);
                    }
                    conversionParams._invokeReturnValue = ReturnVoidReturnThunk;
                    return;
#else
#if CALLDESCR_FPARGREGS
#error Case not yet handled
#endif
                    Debug.Assert(fpReturnSize <= sizeof(ArgumentRegisters));

#if TARGET_X86
                    SetupCallerActualReturnData(callerTransitionBlock);
                    t_NonArgRegisterReturnSpace = ((TransitionBlock*)callerTransitionBlock)->m_returnBlock;
#elif TARGET_WASM
                    throw new NotImplementedException();
#else
#error Platform not implemented
#endif

#if !TARGET_WASM
                    if (fpReturnSize == 4)
                    {
                        conversionParams._invokeReturnValue = ReturnFloatingPointReturn4Thunk;
                    }
                    else
                    {
                        conversionParams._invokeReturnValue = ReturnFloatingPointReturn8Thunk;
                    }
                    return;
#endif // !TARGET_WASM
#endif
                }

#if TARGET_X86
                SetupCallerActualReturnData(callerTransitionBlock);
                t_NonArgRegisterReturnSpace = ((TransitionBlock*)callerTransitionBlock)->m_returnBlock;
                conversionParams._invokeReturnValue = ReturnIntegerPointReturnThunk;
                return;
#else
                // If we reach here, we are returning value in the integer registers.
                if (returnValueToCopy == null)
                {
                    // Return result is a null object. We'll fill the return buffer with 'returnSize' zeros in that case
                    memzeroPointer(callerTransitionBlock + TransitionBlock.GetOffsetOfArgumentRegisters(), returnSize);
                }
                else
                {
                    ExtendingCopy_WriteBarrier(returnValueToCopy, callerTransitionBlock + TransitionBlock.GetOffsetOfArgumentRegisters(), returnType, returnSize);
                }
                conversionParams._invokeReturnValue = ReturnIntegerPointReturnThunk;
#endif
            }
        }

        //
        // Converting by-ref values to non-by-ref form requires the converter to be capable of taking a pointer to a small integer
        // value anywhere in memory and then copying the referenced value into an ABI-compliant pointer-sized "slot" which
        // faithfully communicates the value.  In such cases, the argument slot prepared by the converter must conform to all
        // sign/zero-extension rules mandated by the ABI.
        //
        // ARM32 requires all less-than-pointer-sized values to be sign/zero-extended when they are placed into pointer-sized
        // slots (i.e., requires "producer-oriented" sign/zero-extension).  x86/amd64 do not have this requirement (i.e., the
        // unused high bytes of the pointer-sized slot are ignored by the consumer and are allowed to take on any value); however
        // to reduce the need for ever more #ifs in this file, this behavior will not be #if'd away. (Its not wrong, its just unnecessary)
        //
        private static unsafe void ExtendingCopy_WriteBarrier(void* pSrcVoid, void* pDestVoid, CorElementType type, int typeSize)
        {
            byte* pSrc = (byte*)pSrcVoid;
            byte* pDest = (byte*)pDestVoid;

            if (SignExtendType(type))
                SignExtend(pSrc, pDest, typeSize);
            else if (ZeroExtendType(type))
                ZeroExtend(pSrc, pDest, typeSize);
            else if (isPointerAligned(pSrc) && isPointerAligned(pDest) && (typeSize % sizeof(IntPtr) == 0))
                RuntimeAugments.BulkMoveWithWriteBarrier(new IntPtr(pDest), new IntPtr(pSrc), typeSize);
            else
                Buffer.MemoryCopy(pSrc, pDest, typeSize, typeSize);
        }

        //
        // Converting by-ref values to non-by-ref form requires the converter to be capable of taking a pointer to a small integer
        // value anywhere in memory and then copying the referenced value into an ABI-compliant pointer-sized "slot" which
        // faithfully communicates the value.  In such cases, the argument slot prepared by the converter must conform to all
        // sign/zero-extension rules mandated by the ABI.
        //
        // ARM32 requires all less-than-pointer-sized values to be sign/zero-extended when they are placed into pointer-sized
        // slots (i.e., requires "producer-oriented" sign/zero-extension).  x86/amd64 do not have this requirement (i.e., the
        // unused high bytes of the pointer-sized slot are ignored by the consumer and are allowed to take on any value); however
        // to reduce the need for ever more #ifs in this file, this behavior will not be #if'd away. (Its not wrong, its just unnecessary)
        //
        private static unsafe void ExtendingCopy_NoWriteBarrier(void* pSrcVoid, void* pDestVoid, CorElementType type, int typeSize)
        {
            byte* pSrc = (byte*)pSrcVoid;
            byte* pDest = (byte*)pDestVoid;

            if (SignExtendType(type))
                SignExtend(pSrc, pDest, typeSize);
            else if (ZeroExtendType(type))
                ZeroExtend(pSrc, pDest, typeSize);
            else
                Buffer.MemoryCopy(pSrc, pDest, typeSize, typeSize);
        }

        private static bool SignExtendType(CorElementType type)
        {
            switch (type)
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_I2:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I4:
#endif
                    return true;

            }

            return false;
        }

        private static bool ZeroExtendType(CorElementType type)
        {
            switch (type)
            {
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                case CorElementType.ELEMENT_TYPE_CHAR:
                case CorElementType.ELEMENT_TYPE_U2:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_U4:
#endif
                    return true;

            }

            return false;
        }

        internal static unsafe void SignExtend(void* pSrcVoid, void* pDestVoid, int size)
        {
            byte* pSrc = (byte*)pSrcVoid;
            byte* pDest = (byte*)pDestVoid;

            switch (size)
            {
                case 1:
                    *((IntPtr*)pDest) = new IntPtr(*(sbyte*)pSrc);
                    break;

                case 2:
                    *((IntPtr*)pDest) = new IntPtr(*(short*)pSrc);
                    break;

#if TARGET_64BIT
                // On 64 bit platforms, a 32 bit parameter may require truncation/extension
                case 4:
                    *((IntPtr*)pDest) = new IntPtr(*(int*)pSrc);
                    break;
#endif
                default:
                    Debug.Fail("Should only be called for sizes where sign extension is a meaningful concept");
                    break;
            }
        }

        internal static unsafe void ZeroExtend(void* pSrcVoid, void* pDestVoid, int size)
        {
            byte* pSrc = (byte*)pSrcVoid;
            byte* pDest = (byte*)pDestVoid;

            switch (size)
            {
                case 1:
                    *((UIntPtr*)pDest) = new UIntPtr(*(byte*)pSrc);
                    break;

                case 2:
                    *((UIntPtr*)pDest) = new UIntPtr(*(ushort*)pSrc);
                    break;

#if TARGET_64BIT
                // On 64 bit platforms, a 32 bit parameter may require truncation/extension
                case 4:
                    *((UIntPtr*)pDest) = new UIntPtr(*(uint*)pSrc);
                    break;
#endif
                default:
                    Debug.Fail("Should only be called for sizes where sign extension is a meaningful concept");
                    break;
            }
        }

#if CALLINGCONVENTION_CALLEE_POPS
        private static unsafe void SetupCallerPopArgument(byte* callerTransitionBlock, ArgIterator callerArgs)
        {
            int argStackPopSize = callerArgs.CbStackPop();

#if TARGET_X86
            // In a callee pops architecture, we must specify how much stack space to pop to reset the frame
            // to the ReturnValue thunk.
            ((TransitionBlock*)callerTransitionBlock)->m_argumentRegisters.ecx = new IntPtr(argStackPopSize);
#else
#error handling of how callee pop is handled is not yet implemented for this platform
#endif
        }
#endif

#if TARGET_X86
        internal static unsafe void SetupCallerActualReturnData(byte* callerTransitionBlock)
        {
            // X86 needs to pass callee pop information to the return value thunks, so, since it
            // only has 2 argument registers and may/may not need to return 8 bytes of data, put the return
            // data in a seperate thread local store passed in the other available register (edx)

            fixed (ReturnBlock* actualReturnDataStructAddress = &t_NonArgRegisterReturnSpace)
            {
                ((TransitionBlock*)callerTransitionBlock)->m_argumentRegisters.edx = new IntPtr(actualReturnDataStructAddress);
            }
        }
#endif
    }

    internal static class CallingConventionConverterLogger
    {
        [Conditional("CCCONVERTER_TRACE")]
        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
