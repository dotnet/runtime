// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Computes the x86 stdcall callee-popped argument byte count (cbStackPop) for
/// a managed method given its <c>MethodDesc</c>.
///
/// Mirrors <c>MethodDesc::CbStackPop</c> in <c>src/coreclr/vm/method.cpp</c>,
/// which delegates to <c>ArgIteratorTemplate::CbStackPop</c> /
/// <c>::SizeOfArgStack</c> in <c>src/coreclr/vm/callingconvention.h</c>.
///
/// This is a minimal subset of the native ArgIterator targeted only at x86's
/// callee-popped convention: we count how many bytes of argument stack the
/// callee will pop on return. That's what the transition Frame's
/// <c>UpdateRegDisplay_Impl</c> needs to recover the caller's SP.
///
/// Limitations (intentional, kept simple):
///  - Value-type-in-register optimization (the recursive single-field unwrap
///    in native <c>IsArgumentInRegister</c>) is approximated by treating value
///    types as stack-passed always. This may over-count cbStackPop by a few
///    bytes in rare cases, but on x86 the resulting SP is still &gt;= the true
///    caller SP and walks above this frame continue to track via EBP chain.
///  - HasParamType / HasAsyncContinuation: handled, since both can end up on
///    the stack on x86 when the two argument registers (ECX, EDX) are full.
/// </summary>
internal static class X86ArgIterator
{
    private const int NumArgumentRegisters = 2; // ECX, EDX
    private const int PointerSize = 4;

    /// <summary>
    /// Returns the cbStackPop (in bytes) for the method identified by
    /// <paramref name="methodDescPtr"/>, or 0 if the value could not be
    /// computed (e.g. caller can't recover module/signature).
    /// </summary>
    public static uint Compute(Target target, TargetPointer methodDescPtr)
    {
        if (methodDescPtr == TargetPointer.Null)
            return 0;

        try
        {
            return ComputeCore(target, methodDescPtr);
        }
        catch
        {
            // Best-effort: any failure to resolve metadata/signature degrades to 0,
            // which matches the pre-fix behavior for these Frame types.
            return 0;
        }
    }

    private static uint ComputeCore(Target target, TargetPointer methodDescPtr)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        MethodDescHandle md = rts.GetMethodDescHandle(methodDescPtr);

        // Resolve module + token to read the signature blob.
        TargetPointer mt = rts.GetMethodTable(md);
        TypeHandle owningType = rts.GetTypeHandle(mt);
        TargetPointer modulePtr = rts.GetModule(owningType);
        ModuleHandle moduleHandle = target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);

        MetadataReader mdReader = target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;
        uint token = rts.GetMethodToken(md);
        if (token == 0)
            return 0;

        MethodDefinition methodDef = mdReader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)token));

        SignatureTypeProvider<TypeHandle> provider = new(target, moduleHandle);
        BlobReader blobReader = mdReader.GetBlobReader(methodDef.Signature);
        RuntimeSignatureDecoder<TypeHandle, TypeHandle> decoder = new(provider, target, mdReader, owningType);
        MethodSignature<TypeHandle> sig = decoder.DecodeMethodSignature(ref blobReader);

        bool isInstance = !methodDef.Attributes.HasFlag(System.Reflection.MethodAttributes.Static);
        // hasParamType corresponds to the runtime's "ParamTypeArg" - a hidden arg that carries
        // the generic context. It's needed when the method needs explicit generic context that
        // cannot be derived from `this` or from being a closed instance: matches native
        // MethodDesc::RequiresInstArg.
        bool hasParamType = false;
        try
        {
            GenericContextLoc loc = target.Contracts.RuntimeTypeSystem.GetGenericContextLoc(md);
            hasParamType = (loc == GenericContextLoc.InstArgMethodDesc) || (loc == GenericContextLoc.InstArgMethodTable);
        }
        catch { /* default false */ }
        bool isAsync = false;
        try { isAsync = rts.IsAsyncMethod(md); }
        catch { /* contract may be absent; default false */ }
        bool isVarArg = (sig.Header.CallingConvention == SignatureCallingConvention.VarArgs);
        bool hasRetBufArg = NeedsReturnBuffer(target, sig.ReturnType);

        int numRegistersUsed = 0;
        uint stackBytes = 0;

        if (isInstance) numRegistersUsed++;
        if (hasRetBufArg) numRegistersUsed++;

        if (isVarArg)
        {
            // Vararg cookie consumes a stack slot and fills the remaining argument registers.
            stackBytes += PointerSize;
            numRegistersUsed = NumArgumentRegisters;
        }

        // Walk fixed parameters in order. Each one either consumes the next
        // argument register (if eligible) or contributes to the stack size.
        foreach (TypeHandle paramType in sig.ParameterTypes)
        {
            CorElementType corType = rts.GetSignatureCorElementType(paramType);

            if (numRegistersUsed < NumArgumentRegisters && IsRegisterEligible(corType))
            {
                numRegistersUsed++;
                continue;
            }

            int argSize = GetArgSize(target, rts, paramType, corType);
            stackBytes += (uint)StackElemSize(argSize);
        }

        // HasAsyncContinuation and HasParamType: tail spots that take the
        // remaining argument register if any, else go on the stack.
        if (isAsync)
        {
            if (numRegistersUsed >= NumArgumentRegisters)
                stackBytes += PointerSize;
            else
                numRegistersUsed++;
        }
        if (hasParamType)
        {
            if (numRegistersUsed >= NumArgumentRegisters)
                stackBytes += PointerSize;
            // else: register slot consumed; cbStackPop unchanged
        }

        return stackBytes;
    }

    // Eligible types for register passing on x86. Source of truth:
    // gElementTypeInfo[].m_enregister in src/coreclr/vm/siginfo.cpp.
    private static bool IsRegisterEligible(CorElementType t)
    {
        switch (t)
        {
            case CorElementType.Boolean:
            case CorElementType.Char:
            case CorElementType.I1:
            case CorElementType.U1:
            case CorElementType.I2:
            case CorElementType.U2:
            case CorElementType.I4:
            case CorElementType.U4:
            case CorElementType.String:
            case CorElementType.Ptr:
            case CorElementType.Byref:
            case CorElementType.Class:
            case CorElementType.Var:
            case CorElementType.MVar:
            case CorElementType.Array:
            case CorElementType.I:
            case CorElementType.U:
            case CorElementType.FnPtr:
            case CorElementType.Object:
            case CorElementType.SzArray:
                return true;
            // I8/U8/R4/R8/ValueType/TypedByRef/GenericInst etc. go on stack on x86.
            default:
                return false;
        }
    }

    private static int GetArgSize(Target target, IRuntimeTypeSystem rts, TypeHandle paramType, CorElementType corType)
    {
        switch (corType)
        {
            case CorElementType.I1:
            case CorElementType.U1:
            case CorElementType.Boolean:
                return 1;
            case CorElementType.I2:
            case CorElementType.U2:
            case CorElementType.Char:
                return 2;
            case CorElementType.I4:
            case CorElementType.U4:
            case CorElementType.R4:
                return 4;
            case CorElementType.I8:
            case CorElementType.U8:
            case CorElementType.R8:
                return 8;
            case CorElementType.TypedByRef:
                return PointerSize * 2;
            case CorElementType.ValueType:
            case CorElementType.GenericInst:
                // Value-type size = instance size = BaseSize - sizeof(ObjHeader) - sizeof(MethodTable*).
                try
                {
                    uint baseSize = rts.GetBaseSize(paramType);
                    int instanceSize = (int)baseSize - 2 * target.PointerSize;
                    return instanceSize > 0 ? instanceSize : PointerSize;
                }
                catch
                {
                    return PointerSize;
                }
            default:
                return PointerSize;
        }
    }

    // Round up to STACK_SLOT_SIZE (= sizeof(void*) on x86 = 4).
    private static int StackElemSize(int byteSize)
    {
        return (byteSize + (PointerSize - 1)) & ~(PointerSize - 1);
    }

    // Native equivalent: ArgIterator::HasRetBuffArg. Value types larger than a
    // pointer (and not in the small "enregister" set) need a return buffer.
    private static bool NeedsReturnBuffer(Target target, TypeHandle returnType)
    {
        try
        {
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            CorElementType retType = rts.GetSignatureCorElementType(returnType);
            if (retType != CorElementType.ValueType && retType != CorElementType.GenericInst
                && retType != CorElementType.TypedByRef)
            {
                return false;
            }

            if (retType == CorElementType.TypedByRef)
                return true;

            uint baseSize = rts.GetBaseSize(returnType);
            int instanceSize = (int)baseSize - 2 * target.PointerSize;
            // x86 enregisters return-by-value structs of size 1/2/4/8 in EAX[:EDX].
            return instanceSize != 1 && instanceSize != 2 && instanceSize != 4 && instanceSize != 8;
        }
        catch
        {
            // Be conservative: if we can't resolve, assume no retbuf.
            return false;
        }
    }
}
