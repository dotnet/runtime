// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// zapsig.h
// ---------------------------------------------------------------------------
//
// This module contains helper functions used to encode and manipulate
// signatures for scenarios where runtime-specific signatures
// including specific generic instantiations are persisted,
// like Ready-To-Run decoding, IBC, and Multi-core JIT recording/playback
//
// ---------------------------------------------------------------------------


#ifndef ZAPSIG_H
#define ZAPSIG_H

#include "common.h"

typedef DWORD(*ENCODEMODULE_CALLBACK)(LPVOID pModuleContext, CORINFO_MODULE_HANDLE moduleHandle);
typedef DWORD (*EncodeModuleCallback)(void* pModuleContext, Module *pReferencedModule);
enum {
    // return value when EncodeModule fails
    ENCODE_MODULE_FAILED         = 0xffffffff,
    // no module index override is needed
    MODULE_INDEX_NONE            = 0xfffffffe
};

typedef void(*DEFINETOKEN_CALLBACK)(LPVOID pModuleContext, CORINFO_MODULE_HANDLE moduleHandle, DWORD index, mdTypeRef* token);
typedef void (*TokenDefinitionCallback)(void* pModuleContext, Module *pReferencedModule, DWORD index, mdToken* refToken);

class ZapSig
{
public:
    enum ExternalTokens
    {
        IllegalValue,
        NormalTokens,
        IbcTokens,
        MulticoreJitTokens
    };

    struct Context
    {
        Module *        pInfoModule;              // The tokens in this ZapSig are expressed relative to context.pInfoModule

        // This is a code:Module* when we are resolving Ngen fixups or doing an Ibc Profiling run.
        // And this is a MulticoreJitProfilePlayer or a MulticoreJitRecorder when we are doing multicorejit
        void *          pModuleContext;
                                                  // and is a code:ZapImportTable* when we are running ngen
        ExternalTokens  externalTokens;           // When we see a ELEMENT_TYPE_MODULE_ZAPSIG this tells us what type of token follows.

        Module * GetZapSigModule() const        { return (Module*) pModuleContext; }

        Context(
                Module* _pInfoModule,
                void* _pModuleContext, ExternalTokens _externalTokens)
            : pInfoModule(_pInfoModule),
              pModuleContext(_pModuleContext),
              externalTokens(_externalTokens)
        { LIMITED_METHOD_CONTRACT; _ASSERTE(externalTokens != IllegalValue); }

        Context(
                Module* _pInfoModule,
                Module* _pZapSigModule)
            : pInfoModule(_pInfoModule),
              pModuleContext((void*) _pZapSigModule),
              externalTokens(NormalTokens)
        { }
    };

public:

    ZapSig(
           Module *                _pInfoModule,
           void *                  _pModuleContext,
           ExternalTokens          _externalTokens,
           EncodeModuleCallback    _pfnEncodeModule,
           TokenDefinitionCallback _pfnTokenDefinition)

        : context(_pInfoModule, _pModuleContext, _externalTokens),
          pfnEncodeModule(_pfnEncodeModule),
          pfnTokenDefinition(_pfnTokenDefinition)
    {}

    // Static methods

    // Compare a type handle with a signature whose tokens are resolved with respect to pModule
    // pZapSigContext is used to resolve ELEMENT_TYPE_MODULE_ZAPSIG encodings
    static BOOL CompareSignatureToTypeHandle(PCCOR_SIGNATURE  pSig,
        Module*          pModule,
        TypeHandle       handle,
        const ZapSig::Context *  pZapSigContext);

    // Instance methods

    // Create a signature for a typeHandle
    // It can be decoded using MetaSig::GetTypeHandleThrowing
    // The tokens are espressed relative to this->pInfoModule
    // When (handle.GetModule() != this->pInfoModule), we escape
    // the signature with ELEMENT_TYPE_MODULE_ZAPSIG <id-num>
    // followed by a <token> to encode a temporary change of module
    // For Ibc Signatures the <token> is one of the ibc defined tokens
    // For Ngen Fixup signatures the <token> is for the external module
    //
    BOOL GetSignatureForTypeHandle(TypeHandle typeHandle,
                                   SigBuilder * pSigBuilder);

    static BOOL CompareTypeHandleFieldToTypeHandle(TypeHandle *pTypeHnd, TypeHandle typeHnd2);

private:
    BOOL GetSignatureForTypeDesc(TypeDesc * desc, SigBuilder * pSigBuilder);

    // Returns element type when the typeHandle can be encoded using
    // using a single CorElementType value
    // This includes using ELEMENT_TYPE_CANON_ZAPSIG for the System.__Canon type
    //
    static CorElementType TryEncodeUsingShortcut(/* in  */ MethodTable * pMT);

    // Copy single type signature, adding ELEMENT_TYPE_MODULE_ZAPSIG to types that are encoded using tokens.
    // The source signature originates from the module with index specified by the parameter moduleIndex.
    // Passing moduleIndex set to MODULE_INDEX_NONE results in pure copy of the signature.
    //
    static void CopyTypeSignature(SigParser* pSigParser, SigBuilder* pSigBuilder, DWORD moduleIndex);

private:

    ZapSig::Context           context;

    EncodeModuleCallback      pfnEncodeModule;    // Function Pointer to the EncodeModuleHelper
    TokenDefinitionCallback   pfnTokenDefinition; // Function Pointer to the DefineTokenHelper

public:
    //--------------------------------------------------------------------
    // Static helper encode/decode helper methods

    static Module *DecodeModuleFromIndex(Module *fromModule,
        DWORD index);

    static Module *DecodeModuleFromIndexIfLoaded(Module *fromModule,
        DWORD index);

    // referencingModule is the module that references the type.
    // fromModule is the module in which the type is defined.
    // pBuffer contains the signature encoding for the type.
    // level is the class load level (see classloadlevel.h) to which the type should be loaded
    static TypeHandle DecodeType(
        Module              *referencingModule,
        Module              *fromModule,
        PCCOR_SIGNATURE     pBuffer,
        ClassLoadLevel      level = CLASS_LOADED,
        PCCOR_SIGNATURE     *ppAfterSig = NULL);

    static MethodDesc *DecodeMethod(
        Module              *referencingModule,
        Module              *fromModule,
        PCCOR_SIGNATURE     pBuffer,
        TypeHandle          *ppTH = NULL);

    static MethodDesc *DecodeMethod(
        Module              *pInfoModule,
        PCCOR_SIGNATURE     pBuffer,
        SigTypeContext      *pContext,
        ZapSig::Context     *pZapSigContext,
        TypeHandle          *ppTH = NULL,
        PCCOR_SIGNATURE     *ppOwnerTypeSpecWithVars = NULL,
        PCCOR_SIGNATURE     *ppMethodSpecWithVars = NULL,
        PCCOR_SIGNATURE     *ppAfterSig = NULL,
        BOOL                actualOwnerRequired = FALSE);

    static FieldDesc *DecodeField(
        Module              *referencingModule,
        Module              *fromModule,
        PCCOR_SIGNATURE     pBuffer,
        TypeHandle          *ppTH = NULL);

    static FieldDesc *DecodeField(
        Module              *pReferencingModule,
        Module              *pInfoModule,
        PCCOR_SIGNATURE     pBuffer,
        SigTypeContext      *pContext,
        TypeHandle          *ppTH = NULL);

    static BOOL EncodeMethod(
        MethodDesc             *pMethod,
        Module                 *pInfoModule,
        SigBuilder             *pSigBuilder,
        LPVOID                 pReferencingModule,
        ENCODEMODULE_CALLBACK  pfnEncodeModule,
        DEFINETOKEN_CALLBACK   pfnDefineToken,
        CORINFO_RESOLVED_TOKEN *pResolvedToken = NULL,
        CORINFO_RESOLVED_TOKEN *pConstrainedResolvedToken = NULL,
        BOOL                   fEncodeUsingResolvedTokenSpecStreams = FALSE);
};

#endif // ZAPGSIG_H
