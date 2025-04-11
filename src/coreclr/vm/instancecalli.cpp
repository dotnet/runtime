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
    _ASSERTE(GetRVA() == 0);

    // Intrinsic must be a static method.
    _ASSERTE(IsStatic());

    MetaSig declarationSig(this);
    UINT argCount = declarationSig.NumFixedArgs();

    // Intrinsic must have at least the "this" argument and the IntPtr function pointer argument.
    _ASSERTE(argCount >= 2);

    SigTypeContext genericContext;
    ILStubLinker sl(
        this->GetModule(),
        GetSignature(),
        &genericContext,
        this,
        (ILStubLinkerFlags)ILSTUB_LINKER_FLAG_NONE);

    ILCodeStream* pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

    // Move to the function pointer argument.
    declarationSig.SkipArg(); // Skip "this".

    CorElementType fnType = declarationSig.NextArg();
    _ASSERTE(fnType == ELEMENT_TYPE_FNPTR);

    SigPointer spToken = declarationSig.GetArgProps();
    spToken.GetByte(NULL); 
    declarationSig.SkipArg();
    SigPointer sigPtrTokenEnd = declarationSig.GetArgProps();

    // Copy the existing signature so we can add HAS_THIS and EXPLICIT_THIS.
    SigBuilder sigBuilder;
    spToken.CopySignature(GetModule(), &sigBuilder, &sigPtrTokenEnd, IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS);

    // Create a token for the new signature.
    uint32_t sigLen;
    PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature((DWORD*)&sigLen);
    mdToken fcnPtrToken = pCode->GetSigToken(pSig, sigLen);

    // Create a local and store the function pointer value in it.
    LocalDesc localDesc(ELEMENT_TYPE_FNPTR);
    localDesc.pSigModule = GetModule();
    localDesc.pSig = pSig;
    DWORD localIndex = pCode->NewLocal(localDesc);
    pCode->EmitLDARG(1);
    pCode->EmitSTLOC(localIndex);

    // Load "this" and each argument to the function pointer.
    pCode->EmitLDARG(0);
    for (UINT i = 2; i < argCount; ++i)
        pCode->EmitLDARG(i);

    // Load the function pointer and call it.
    pCode->EmitLDLOC(localIndex);
    pCode->EmitCALLI(fcnPtrToken, argCount - 1, declarationSig.IsReturnTypeVoid() ? 0 : 1, /*explictThis*/ true);
    pCode->EmitRET();

    // Generate all IL associated data for JIT
    NewHolder<ILStubResolver> ilResolver = new ILStubResolver();
    ilResolver->SetStubMethodDesc(this);

    {
        UINT maxStack;
        size_t cbCode = sl.Link(&maxStack);
        DWORD cbSig = sl.GetLocalSigSize();

        COR_ILMETHOD_DECODER* pILHeader = ilResolver->AllocGeneratedIL(cbCode, cbSig, maxStack);
        BYTE* pbBuffer = (BYTE*)pILHeader->Code;
        BYTE* pbLocalSig = (BYTE*)pILHeader->LocalVarSig;
        _ASSERTE(cbSig == pILHeader->cbLocalVarSig);
        sl.GenerateCode(pbBuffer, cbCode);
        sl.GetLocalSig(pbLocalSig, cbSig);

        // Store the token lookup map
        ilResolver->SetTokenLookupMap(sl.GetTokenLookupMap());
        ilResolver->SetJitFlags(CORJIT_FLAGS(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB));

        *resolver = (DynamicResolver*)ilResolver;
        *methodILDecoder = pILHeader;
    }

    ilResolver.SuppressRelease();
}
