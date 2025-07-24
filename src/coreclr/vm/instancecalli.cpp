// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

void MethodDesc::GenerateFunctionPointerCall(DynamicResolver** resolver, COR_ILMETHOD_DECODER** methodILDecoder)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(resolver != NULL);
    _ASSERTE(methodILDecoder != NULL);
    _ASSERTE(*resolver == NULL && *methodILDecoder == NULL);
    _ASSERTE(IsIL());
    _ASSERTE(GetRVA() != 0);

    // Intrinsic must be a static method.
    _ASSERTE(IsStatic());

    MetaSig declarationSig(this);
    UINT argCount = declarationSig.NumFixedArgs();

    // Intrinsic must have at least the function pointer and the "this" argument.
    _ASSERTE(argCount >= 2);

    SigTypeContext genericContext;
    ILStubLinker sl(
        this->GetModule(),
        GetSignature(),
        &genericContext,
        this,
        (ILStubLinkerFlags)ILSTUB_LINKER_FLAG_NONE);

    ILCodeStream* pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

    // Copy the first arg which has the function pointer signature into a SigBuilder to add HASTHIS and EXPLICITTHIS
    // and to get a token for the new signature.
    // The signature follows details defined in ECMA-335 - II.23.2.1
    declarationSig.SkipArg();
    SigPointer sp = declarationSig.GetArgProps();

    CorElementType eType;
    sp.GetElemType(&eType); // Skip past the element type to get at the signature.
    _ASSERTE(eType == ELEMENT_TYPE_FNPTR);

    SigBuilder sigBuilder;
    sp.CopySignature(GetModule(), &sigBuilder, IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS);

    // Create a token for the new signature.
    DWORD sigLen;
    PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature((DWORD*)&sigLen);
    mdToken fcnPtrToken = pCode->GetSigToken(pSig, sigLen);

    // Load "this" and each argument to the function pointer.
    for (UINT i = 1; i < argCount; ++i)
        pCode->EmitLDARG(i);

    // Load the function pointer and call it.
    pCode->EmitLDARG(0);
    pCode->EmitCALLI(fcnPtrToken, argCount - 1, declarationSig.IsReturnTypeVoid() ? 0 : 1);
    pCode->EmitRET();

    // Generate all IL associated data for JIT.
    NewHolder<ILStubResolver> ilResolver = new ILStubResolver();
    ilResolver->SetStubMethodDesc(this);

    *methodILDecoder = ilResolver->FinalizeILStub(&sl);
    *resolver = ilResolver.Extract();
}
