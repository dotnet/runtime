//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ===========================================================================
// File: zapsig.cpp
//
// Signature encoding for zapper (ngen)
//

// ===========================================================================


#include "common.h"
#ifdef FEATURE_PREJIT
#include "zapsig.h"
#include "typedesc.h"
#ifndef BINDER
#include "compile.h"
#else
#include "mdilmodule.h"
#endif
#include "sigbuilder.h"

#ifndef DACCESS_COMPILE

BOOL ZapSig::GetSignatureForTypeDesc(TypeDesc * desc, SigBuilder * pSigBuilder)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    CorElementType elemType = desc->GetInternalCorElementType();

    if (elemType == ELEMENT_TYPE_VALUETYPE)
    {
        // convert to ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG so that the right
        // thing will happen in code:SigPointer.GetTypeHandleThrowing
        elemType = (CorElementType) ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG;
    }
    else if (elemType == ELEMENT_TYPE_VAR || elemType == ELEMENT_TYPE_MVAR)
    {
        // Enable encoding of type variables for NGen signature only. IBC toolchain is not aware of them yet.
        if (context.externalTokens == ZapSig::NormalTokens)
            elemType = (CorElementType) ELEMENT_TYPE_VAR_ZAPSIG;
    }

    pSigBuilder->AppendElementType(elemType);

    if (desc->HasTypeParam())
    {
        if (!this->GetSignatureForTypeHandle(desc->GetTypeParam(), pSigBuilder))
            return FALSE;

        if (elemType == ELEMENT_TYPE_ARRAY)
        {
            ArrayTypeDesc *pArrayDesc = dac_cast<PTR_ArrayTypeDesc>(desc);
            _ASSERTE(pArrayDesc->GetRank() != 0);
            pSigBuilder->AppendData(pArrayDesc->GetRank());
            pSigBuilder->AppendData(0);
            pSigBuilder->AppendData(0);
        }
    }
    else
    {
        switch (elemType)
        {
        case ELEMENT_TYPE_FNPTR:
            {
                FnPtrTypeDesc *pTD = dac_cast<PTR_FnPtrTypeDesc>(desc);

                // Emit calling convention
                pSigBuilder->AppendByte(pTD->GetCallConv());

                // number of args
                unsigned numArgs = pTD->GetNumArgs();
                pSigBuilder->AppendData(numArgs);

                // return type and args
                TypeHandle *retAndArgTypes = pTD->GetRetAndArgTypesPointer();
                for (DWORD i = 0; i <= numArgs; i++)
                {
                    TypeHandle th = retAndArgTypes[i];
                    // This should be a consequence of the type key being restored
                    CONSISTENCY_CHECK(!th.IsNull() && !th.IsEncodedFixup());
                    if (!this->GetSignatureForTypeHandle(th, pSigBuilder))
                        return FALSE;
                }
            }
            break;

        case ELEMENT_TYPE_MVAR:
            //                    _ASSERTE(!"Cannot encode ET_MVAR in a ZapSig");
            return FALSE;

        case ELEMENT_TYPE_VAR:
            //                    _ASSERTE(!"Cannot encode ET_VAR in a ZapSig");
            return FALSE;

        case ELEMENT_TYPE_VAR_ZAPSIG:
            {
                TypeVarTypeDesc * pTypeVarDesc = dac_cast<PTR_TypeVarTypeDesc>(desc);
#ifdef BINDER
                MdilModule * pVarTypeModule = pTypeVarDesc->GetModule();
#else
                Module * pVarTypeModule = pTypeVarDesc->GetModule();
#endif
                if (pVarTypeModule != this->context.pInfoModule)
                {
                    DWORD index = (*this->pfnEncodeModule)(this->context.pModuleContext, pVarTypeModule);

                    if (index == ENCODE_MODULE_FAILED)
                        return FALSE;

                    // emit the ET_MODULE_ZAPSIG escape
                    pSigBuilder->AppendElementType((CorElementType) ELEMENT_TYPE_MODULE_ZAPSIG);
                    // emit the module index
                    pSigBuilder->AppendData(index);
                }
                pSigBuilder->AppendData(RidFromToken(pTypeVarDesc->GetToken()));
                break;
            }

        default:
            _ASSERTE(!"Bad type");
            return FALSE;
        }
    }

    return TRUE;
}


// Create a signature for a typeHandle 
// It can be decoded using MetaSig::GetTypeHandleThrowing
// The tokens are espressed relative to this->pInfoModule
// When handle.GetModule() != this->pInfoModule), we escape the signature
// with an ELEMENT_TYPE_MODULE_ZAPSIG <id-num> <token> to encode 
// a temporary change of module 
//
// Returns the number of characters written into the buffer.  
// If buffer and bufferMax are NULL, it returns the number of
// characters that would have been written.
// If the buffer isn't big enough it doesn't write past bufferMax
// A return value of 0 indidates a failure to encode the signature
//
BOOL ZapSig::GetSignatureForTypeHandle(TypeHandle      handle,
                                       SigBuilder *    pSigBuilder)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        PRECONDITION(CheckPointer(handle));
        PRECONDITION(CheckPointer(this->context.pInfoModule));
        PRECONDITION(!handle.HasUnrestoredTypeKey());
        MODE_ANY;
    }
    CONTRACTL_END

    if (handle.IsTypeDesc())
        return GetSignatureForTypeDesc(handle.AsTypeDesc(), pSigBuilder);

    MethodTable *pMT = handle.AsMethodTable();

    // Can we encode the type using a short ET encoding?
    // 
    CorElementType elemType = elemType = TryEncodeUsingShortcut(pMT);
    if (elemType != ELEMENT_TYPE_END)
    {
        _ASSERTE(pMT->IsTypicalTypeDefinition());

        // Check for an array type and encode that we are dealing with a MethodTable representation
        if (elemType == ELEMENT_TYPE_SZARRAY || elemType == ELEMENT_TYPE_ARRAY)
        {
            pSigBuilder->AppendElementType((CorElementType)ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG);
            pSigBuilder->AppendElementType(elemType);

            TypeHandle elementType = pMT->GetApproxArrayElementTypeHandle();
            if (!this->GetSignatureForTypeHandle(elementType, pSigBuilder))
                return FALSE;

            if (elemType == ELEMENT_TYPE_ARRAY)
            {
                pSigBuilder->AppendData(pMT->GetRank());
                pSigBuilder->AppendData(0);
                pSigBuilder->AppendData(0);
            }
        }
        else
        {
            pSigBuilder->AppendElementType(elemType);
        }
        
        return TRUE;
    }

    // We could not encode the type using a short encoding  
    // and we have a handle that represents a Class or ValueType

    // We may need to emit an out-of-module escape sequence
    // 
#ifdef BINDER
    MdilModule *pTypeHandleModule = pMT->GetModule();
#else
    Module *pTypeHandleModule = pMT->GetModule_NoLogging();
#endif

    // If the type handle's module is different that the this->pInfoModule 
    // we will need to add an out-of-module escape for the type
    //
    DWORD index = 0;
    mdToken token = pMT->GetCl_NoLogging();
#ifndef BINDER
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    if (pTypeHandleModule != this->context.pInfoModule && !pTypeHandleModule->IsInCurrentVersionBubble())
    {
        pTypeHandleModule = GetAppDomain()->ToCompilationDomain()->GetTargetModule();
        token = pTypeHandleModule->LookupTypeRefByMethodTable(pMT);
    }
#endif
#endif
    if (pTypeHandleModule != this->context.pInfoModule) 
    {
        // During IBC profiling this calls
        //     code:Module.EncodeModuleHelper
        // During ngen this calls
        //     code:ZapImportTable.EncodeModuleHelper)
        //
        index = (*this->pfnEncodeModule)(this->context.pModuleContext, pTypeHandleModule); 

        if (index == ENCODE_MODULE_FAILED)
            return FALSE;

        // emit the ET_MODULE_ZAPSIG escape
        pSigBuilder->AppendElementType((CorElementType) ELEMENT_TYPE_MODULE_ZAPSIG);
        // emit the module index
        pSigBuilder->AppendData(index);
    }

    // Remember if we have an instantiated generic type 
    bool fNeedsInstantiation = pMT->HasInstantiation() && !pMT->IsGenericTypeDefinition();

    // We possibly have an instantiated generic type 
    if (fNeedsInstantiation)
    {
        pSigBuilder->AppendElementType(ELEMENT_TYPE_GENERICINST);
    }

    // Beware of enums!  Can't use GetInternalCorElementType() here.
    pSigBuilder->AppendElementType(pMT->IsValueType() ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS);

    _ASSERTE(!IsNilToken(token));
    if (IsNilToken(token))
        return FALSE;

    if ((index != 0) && (this->pfnTokenDefinition != NULL))
    {
        //
        // We do not want to log the metadata lookups that we perform here
        // 
        IBCLoggingDisabler   disableLogging;

        // During IBC profiling this calls
        //     code:Module::TokenDefinitionHelper
        (*this->pfnTokenDefinition)(this->context.pModuleContext, pTypeHandleModule, index, &token);

        // ibcExternalType tokens are actually encoded as mdtTypeDef tokens in the signature
        _ASSERT(TypeFromToken(token) == ibcExternalType);
        token = TokenFromRid(RidFromToken(token), mdtTypeDef);
    }

    pSigBuilder->AppendToken(token);

    if (fNeedsInstantiation)
    {
        pSigBuilder->AppendData(pMT->GetNumGenericArgs());
        Instantiation inst = pMT->GetInstantiation();
        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            TypeHandle t = inst[i];
            CONSISTENCY_CHECK(!t.IsNull() && !t.IsEncodedFixup());
            if (!this->GetSignatureForTypeHandle(t, pSigBuilder))
                return FALSE;
        }
    }
    return TRUE;
}

#endif // #ifndef DACCESS_COMPILE

//
// Returns element type when the typeHandle can be encoded using
// using a single CorElementType value 
// This includes using ELEMENT_TYPE_CANON_ZAPSIG for the System.__Canon type
// 
/*static */ CorElementType ZapSig::TryEncodeUsingShortcut(/* in  */ MethodTable * pMT)
{
   LIMITED_METHOD_CONTRACT;

    CorElementType elemType = ELEMENT_TYPE_END;  // An illegal value that we check for later

    // Set elemType to a shortcut encoding whenever possible
    // 
    if (pMT->IsTruePrimitive())
        elemType = pMT->GetInternalCorElementType();
    else if (pMT == g_pObjectClass)
        elemType = ELEMENT_TYPE_OBJECT;
    else if (pMT == g_pStringClass)
        elemType = ELEMENT_TYPE_STRING;
    else if (pMT == g_pCanonMethodTableClass)
        elemType = (CorElementType) ELEMENT_TYPE_CANON_ZAPSIG;
    else if (pMT->IsArray())
        elemType = pMT->GetInternalCorElementType();  // either ELEMENT_TYPE_SZARRAY or ELEMENT_TYPE_ARRAY

    return elemType;
}
#ifndef BINDER
//
// Compare a metadata signature element with a type handle
// The type handle must have a fully restored type key, which in turn means that modules for all of its
// components are loaded (e.g. type arguments to an instantiated type).
//
// Hence we can do the signature comparison without incurring any loads or restores.
//
/*static*/ BOOL ZapSig::CompareSignatureToTypeHandle(PCCOR_SIGNATURE          pSig,   
#ifdef BINDER
                                                     MdilModule*              pModule, 
#else
                                                     Module*                  pModule, 
#endif
                                                     TypeHandle               handle,
                                                     const ZapSig::Context *  pZapSigContext)
{
    CONTRACT(BOOL)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pZapSigContext));
        PRECONDITION(CheckPointer(pZapSigContext->pModuleContext));
        PRECONDITION(CheckPointer(pZapSigContext->pInfoModule));
        PRECONDITION(CheckPointer(handle));
        PRECONDITION(CheckPointer(pSig));
        PRECONDITION(!handle.HasUnrestoredTypeKey());
    }
    CONTRACT_END

    mdToken      tk;

    //
    // pOrigModule is the original module that contained this ZapSig
    // 
#ifdef BINDER
    MdilModule *   pOrigModule = pZapSigContext->pInfoModule;
#else
    Module *       pOrigModule = pZapSigContext->pInfoModule;
#endif
    CorElementType sigType     = CorSigUncompressElementType(pSig);
    CorElementType handleType  = handle.GetSignatureCorElementType();

    switch (sigType)
    {
        default:
        {
            // Unknown type!
            _ASSERTE(!"Unknown type in ZapSig::CompareSignatureToTypeHandle");
            RETURN(FALSE);
        }

        case ELEMENT_TYPE_MODULE_ZAPSIG:
        {
            DWORD ix = CorSigUncompressData(pSig);
            CONTRACT_VIOLATION(ThrowsViolation|GCViolation);
            pModule = pZapSigContext->GetZapSigModule()->GetModuleFromIndexIfLoaded(ix);
            if (pModule == NULL)
                RETURN FALSE;
            else
                RETURN(CompareSignatureToTypeHandle(pSig, pModule, handle, pZapSigContext));
        }
    
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_TYPEDBYREF:
            RETURN(sigType == handleType);

        case ELEMENT_TYPE_STRING:   
            RETURN(handle == TypeHandle(g_pStringClass));

        case ELEMENT_TYPE_OBJECT:
            RETURN(handle == TypeHandle(g_pObjectClass));

        case ELEMENT_TYPE_CANON_ZAPSIG:
            RETURN(handle == TypeHandle(g_pCanonMethodTableClass));

        case ELEMENT_TYPE_VAR: 
        case ELEMENT_TYPE_MVAR:
        {
            if (sigType != handleType)
                RETURN(FALSE);

            unsigned varNum = CorSigUncompressData(pSig);
            RETURN(varNum == (dac_cast<PTR_TypeVarTypeDesc>(handle.AsTypeDesc())->GetIndex()));
        }

        case ELEMENT_TYPE_VAR_ZAPSIG:
        {
            if (!handle.IsGenericVariable())
                RETURN(FALSE);

            TypeVarTypeDesc *pTypeVarTypeDesc = handle.AsGenericVariable();

            unsigned rid = CorSigUncompressData(pSig);
            RETURN(TokenFromRid(rid, mdtGenericParam) == pTypeVarTypeDesc->GetToken() && pModule == pTypeVarTypeDesc->GetModule());
        }

        // These take an additional argument, which is the element type
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        {
            if (sigType != handleType)
                RETURN(FALSE);

            RETURN (CompareSignatureToTypeHandle(pSig, pModule, handle.GetTypeParam(), pZapSigContext));
        }

        case ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG:
        {
            if (handle.IsTypeDesc() || !handle.AsMethodTable()->IsArray())
                RETURN(FALSE);

            RETURN (CompareSignatureToTypeHandle(pSig, pModule, handle, pZapSigContext));
        }
        
        case ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
        {
            sigType = CorSigUncompressElementType(pSig);
            _ASSERTE(sigType == ELEMENT_TYPE_VALUETYPE);

            if (!handle.IsNativeValueType()) RETURN(FALSE);
        } // fall-through

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {            
            CorSigUncompressToken(pSig, &tk);
            _ASSERTE(TypeFromToken(tk) == mdtTypeDef);
            RETURN (sigType == handleType && !handle.HasInstantiation() && pModule == handle.GetModule() && handle.GetCl() == tk);
        }

        case ELEMENT_TYPE_FNPTR: 
        {
            if (sigType != handleType)
                RETURN(FALSE);

            FnPtrTypeDesc *pTD = handle.AsFnPtrType();
            DWORD callConv = CorSigUncompressData(pSig);
            if (callConv != pTD->GetCallConv())
                RETURN(FALSE);

            DWORD numArgs = CorSigUncompressData(pSig);
            if (numArgs != pTD->GetNumArgs())
                RETURN(FALSE);

            {
                CONTRACT_VIOLATION(ThrowsViolation|GCViolation);

                for (DWORD i = 0; i <= numArgs; i++)
                {
                    SigPointer sp(pSig);
                    if (!CompareSignatureToTypeHandle(pSig, pOrigModule, pTD->GetRetAndArgTypes()[i], pZapSigContext))
                        RETURN(FALSE);
                    if (FAILED(sp.SkipExactlyOne()))
                    {
                        RETURN(FALSE);
                    }
                    pSig = sp.GetPtr();
                }
            }    
            break;
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            if (!handle.HasInstantiation())
                RETURN(FALSE);

            sigType = CorSigUncompressElementType(pSig);
            if (sigType != handleType)
                RETURN(FALSE);

            pSig += CorSigUncompressToken(pSig, &tk);
            _ASSERTE(TypeFromToken(tk) == mdtTypeDef);
            if (pModule != handle.GetModule() || tk != handle.GetCl())
                RETURN(FALSE);

            DWORD numGenericArgs = CorSigUncompressData(pSig);

            if (numGenericArgs != handle.GetNumGenericArgs())
                RETURN(FALSE);

            Instantiation inst = handle.GetInstantiation();
            for (DWORD i = 0; i < inst.GetNumArgs(); i++)
            {
                SigPointer sp(pSig);
                if (!CompareSignatureToTypeHandle(pSig, pOrigModule, inst[i], pZapSigContext))
                    RETURN(FALSE);
                if (FAILED(sp.SkipExactlyOne()))
                {
                    RETURN(FALSE);
                }
                pSig = sp.GetPtr();
            }
            break;
        }

        case ELEMENT_TYPE_ARRAY:
        {
            if (sigType != handleType)
                RETURN(FALSE);

            if (!CompareSignatureToTypeHandle(pSig, pModule, handle.GetTypeParam(), pZapSigContext))
                RETURN(FALSE);
            SigPointer sp(pSig);
            if (FAILED(sp.SkipExactlyOne()))
                RETURN(FALSE);

            DWORD rank;
            if (FAILED(sp.GetData(&rank)))
                RETURN(FALSE);
            
            if (rank != handle.AsArray()->GetRank())
                RETURN(FALSE);

            break;
        }
    }

    RETURN(TRUE);
}

/*static*/
BOOL ZapSig::CompareFixupToTypeHandle(Module * pModule, TADDR fixup, TypeHandle handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        PRECONDITION(CORCOMPILE_IS_POINTER_TAGGED(fixup));
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END

    Module *pDefiningModule;
    PCCOR_SIGNATURE pSig = pModule->GetEncodedSigIfLoaded(CORCOMPILE_UNTAG_TOKEN(fixup), &pDefiningModule);
    if (pDefiningModule == NULL)
        return FALSE;
        
    ZapSig::Context zapSigContext(pDefiningModule, pModule);
    return ZapSig::CompareSignatureToTypeHandle(pSig, pDefiningModule, handle, &zapSigContext);
}

/*static*/
BOOL ZapSig::CompareTypeHandleFieldToTypeHandle(TypeHandle *pTypeHnd, TypeHandle typeHnd2)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pTypeHnd));
        PRECONDITION(CheckPointer(typeHnd2));
        PRECONDITION(!CORCOMPILE_IS_POINTER_TAGGED((SIZE_T) typeHnd2.AsPtr()));
    }
    CONTRACTL_END

    // Ensure that the compiler won't fetch the value twice
    SIZE_T fixup = VolatileLoadWithoutBarrier((SIZE_T *)pTypeHnd);

    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {
        Module *pContainingModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(pTypeHnd));
        CONSISTENCY_CHECK(pContainingModule != NULL);
        
        Module *pDefiningModule;
        PCCOR_SIGNATURE pSig = pContainingModule->GetEncodedSigIfLoaded(CORCOMPILE_UNTAG_TOKEN(fixup), &pDefiningModule);
        if (pDefiningModule == NULL)
            return FALSE;
        else
        {
            ZapSig::Context    zapSigContext(pDefiningModule, pContainingModule);
            ZapSig::Context *  pZapSigContext = &zapSigContext;
            return CompareSignatureToTypeHandle(pSig, pDefiningModule, typeHnd2, pZapSigContext);
        }
    }
    else
        return TypeHandle::FromTAddr(fixup) == typeHnd2;
}

#ifndef DACCESS_COMPILE
Module *ZapSig::DecodeModuleFromIndexes(Module *fromModule,
                                        DWORD assemblyIndex,
                                        DWORD moduleIndex)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Assembly *pAssembly = NULL;

    if (assemblyIndex == 0)
    {
        pAssembly = fromModule->GetAssembly();
    }
    else
    {
        if (assemblyIndex < fromModule->GetAssemblyRefMax())
        {
            pAssembly = fromModule->LoadAssembly(GetAppDomain(), RidToToken(assemblyIndex, mdtAssemblyRef))->GetAssembly();
        }
        else
        {
            assemblyIndex -= fromModule->GetAssemblyRefMax();

            pAssembly = fromModule->GetNativeMetadataAssemblyRefFromCache(assemblyIndex);

            if(pAssembly == NULL)
            {
                AssemblySpec spec;
                spec.InitializeSpec(TokenFromRid(assemblyIndex, mdtAssemblyRef),
                                    fromModule->GetNativeAssemblyImport(),
                                    NULL,
                                    FALSE);

                pAssembly = spec.LoadAssembly(FILE_LOADED);

                fromModule->SetNativeMetadataAssemblyRefInCache(assemblyIndex, pAssembly);            
            }
        }
    }

    if (moduleIndex == 0)
        return pAssembly->GetManifestModule();
    else
        return pAssembly->GetManifestModule()->LoadModule(GetAppDomain(), RidToToken(moduleIndex, mdtFile))->GetModule();
}

Module *ZapSig::DecodeModuleFromIndexesIfLoaded(Module *fromModule,
                                                DWORD assemblyIndex,
                                                DWORD moduleIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    Assembly *pAssembly = NULL;
    mdAssemblyRef tkAssemblyRef;

    if (assemblyIndex == 0)
        pAssembly = fromModule->GetAssembly();
    else
    {
        if (assemblyIndex < fromModule->GetAssemblyRefMax())
        {
            tkAssemblyRef = RidToToken(assemblyIndex, mdtAssemblyRef);
            pAssembly = fromModule->GetAssemblyIfLoaded(tkAssemblyRef);
        }
        else
        {
            assemblyIndex -= fromModule->GetAssemblyRefMax();
            tkAssemblyRef = RidToToken(assemblyIndex, mdtAssemblyRef);
            IMDInternalImport *  pMDImportOverride = fromModule->GetNativeAssemblyImport(FALSE);
            if (pMDImportOverride != NULL)
            {
                CHAR   szFullName[MAX_CLASS_NAME + 1];
                LPCSTR szWinRtNamespace = NULL;
                LPCSTR szWinRtClassName = NULL;
                
                BOOL fValidAssemblyRef = TRUE;
                LPCSTR pAssemblyName;
                DWORD  dwFlags;
                if (FAILED(pMDImportOverride->GetAssemblyRefProps(tkAssemblyRef, 
                        NULL, 
                        NULL, 
                        &pAssemblyName, 
                        NULL, 
                        NULL, 
                        NULL, 
                        &dwFlags)))
                {   // Unexpected failure reading MetaData
                    fValidAssemblyRef = FALSE;
                }
                
                if (fValidAssemblyRef && IsAfContentType_WindowsRuntime(dwFlags))
                {
                    // Find the encoded type name
                    LPCSTR pTypeName = NULL;
                    if (pAssemblyName != NULL)
                        pTypeName = strchr(pAssemblyName, '!');
                    
                    if (pTypeName != NULL)
                    {
                        pTypeName++;
                        // pTypeName now contains the full type name (namespace + name)
                    
                        strcpy_s(szFullName, _countof(szFullName), pTypeName);
                        LPSTR pszName = strrchr(szFullName, '.');
                    
                        // WinRT types must have a namespace
                        if (pszName != NULL)
                        {
                            // Replace . between namespace and name with null terminator.
                            // This breaks the string into a namespace and name pair.
                            *pszName = '\0';
                            pszName++;
                            
                            szWinRtNamespace = szFullName;
                            szWinRtClassName = pszName;
                        }
                        else
                        {   // Namespace '.' separator not found - invalid type name (namespace has to be always present)
                            fValidAssemblyRef = FALSE;
                        }
                    }
                    else
                    {   // Type name separator in assembly name '!' not found
                        fValidAssemblyRef = FALSE;
                    }
                }
                
                if (fValidAssemblyRef)
                {
                    pAssembly = fromModule->GetAssemblyIfLoaded(
                            tkAssemblyRef, 
                            szWinRtNamespace, 
                            szWinRtClassName, 
                            pMDImportOverride);
                }
            }
        }
    }

    if (pAssembly == NULL)
        return NULL;

    if (moduleIndex == 0)
        return pAssembly->GetManifestModule();
    else
        return pAssembly->GetManifestModule()->GetModuleIfLoaded(RidToToken(moduleIndex, mdtFile), TRUE, TRUE);
}


TypeHandle ZapSig::DecodeType(Module *pEncodeModuleContext,
                              Module *pInfoModule,
                              PCCOR_SIGNATURE pBuffer,
                              ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SigPointer p(pBuffer);

    ZapSig::Context    zapSigContext(pInfoModule, pEncodeModuleContext);
    ZapSig::Context *  pZapSigContext = &zapSigContext;

    SigTypeContext typeContext;    // empty context is OK: encoding should not contain type variables.

    TypeHandle th = p.GetTypeHandleThrowing(pInfoModule,
                                            &typeContext,
                                            ClassLoader::LoadTypes,
                                            level,                                            
                                            level < CLASS_LOADED, // For non-full loads, drop a level when loading generic arguments
                                            NULL,
                                            pZapSigContext);
                                            
    return th;
}

MethodDesc *ZapSig::DecodeMethod(Module *pReferencingModule,
                                 Module *pInfoModule,
                                 PCCOR_SIGNATURE pBuffer,
                                 TypeHandle * ppTH /*=NULL*/)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMethod = NULL;

    SigPointer sig(pBuffer);

    ZapSig::Context    zapSigContext(pInfoModule, (void *)pReferencingModule, ZapSig::NormalTokens);
    ZapSig::Context *  pZapSigContext = &zapSigContext;

    SigTypeContext typeContext;    // empty context is OK: encoding should not contain type variables.

    // decode flags
    DWORD methodFlags;
    IfFailThrow(sig.GetData(&methodFlags));

    TypeHandle thOwner = NULL;

    if ( methodFlags & ENCODE_METHOD_SIG_OwnerType )
    {
        thOwner = sig.GetTypeHandleThrowing(pInfoModule,
                                        &typeContext,
                                        ClassLoader::LoadTypes,
                                        CLASS_LOADED,
                                        FALSE,
                                        NULL,
                                        pZapSigContext);

        IfFailThrow(sig.SkipExactlyOne());
    }

    if ( methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken )
    {
        // get the method desc using slot number
        DWORD slot;
        IfFailThrow(sig.GetData(&slot));

        _ASSERTE(!thOwner.IsNull());

        pMethod = thOwner.GetMethodTable()->GetMethodDescForSlot(slot);
    }
    else
    {
        //
        // decode method token
        //
        RID rid;
        IfFailThrow(sig.GetData(&rid));

        if (methodFlags & ENCODE_METHOD_SIG_MemberRefToken)
        {
            if (thOwner.IsNull())
            {
                TypeHandle th;
                MethodDesc * pMD = NULL;
                FieldDesc * pFD = NULL;

                MemberLoader::GetDescFromMemberRef(pInfoModule, TokenFromRid(rid, mdtMemberRef), &pMD, &pFD, NULL, FALSE, &th);
                _ASSERTE(pMD != NULL);

                thOwner = th;
                pMethod = pMD;
            }
            else
            {
                pMethod = MemberLoader::GetMethodDescFromMemberRefAndType(pInfoModule, TokenFromRid(rid, mdtMemberRef), thOwner.GetMethodTable());
            }
        }
        else
        {
            pMethod = MemberLoader::GetMethodDescFromMethodDef(pInfoModule, TokenFromRid(rid, mdtMethodDef), FALSE);
        }
    }

    if (thOwner.IsNull())
        thOwner = pMethod->GetMethodTable();

    if (ppTH != NULL)
        *ppTH = thOwner;

    // Ensure that the methoddescs dependencies have been walked sufficiently for type forwarders to be resolved.
    // This method is actually basically a nop for dependencies which are ngen'd, but is real work for jitted
    // dependencies. (However, this shouldn't be meaningful work that wouldn't happen in any case very soon.)
    pMethod->PrepareForUseAsADependencyOfANativeImage();

    Instantiation inst;

    // Instantiate the method if needed, or create a stub to a static method in a generic class.
    if (methodFlags & ENCODE_METHOD_SIG_MethodInstantiation)
    {
        DWORD nargs;
        IfFailThrow(sig.GetData(&nargs));
        _ASSERTE(nargs > 0);

        SIZE_T cbMem;

        if (!ClrSafeInt<SIZE_T>::multiply(nargs, sizeof(TypeHandle), cbMem/* passed by ref */))
            ThrowHR(COR_E_OVERFLOW);
                        
        TypeHandle * pInst = (TypeHandle*) _alloca(cbMem);

        for (DWORD i = 0; i < nargs; i++)
        {
            pInst[i] = sig.GetTypeHandleThrowing(pInfoModule,
                                                &typeContext,
                                                ClassLoader::LoadTypes,
                                                CLASS_LOADED,
                                                FALSE,
                                                NULL,
                                                pZapSigContext);
            IfFailThrow(sig.SkipExactlyOne());
        }

        inst = Instantiation(pInst, nargs);
    }
    else
    {
        inst = pMethod->GetMethodInstantiation();
    }


    // This must be called even if nargs == 0, in order to create an instantiating
    // stub for static methods in generic classees if needed, also for BoxedEntryPointStubs
    // in non-generic structs.
    BOOL isInstantiatingStub = (methodFlags & ENCODE_METHOD_SIG_InstantiatingStub);
    BOOL isUnboxingStub = (methodFlags & ENCODE_METHOD_SIG_UnboxingStub);

    pMethod = MethodDesc::FindOrCreateAssociatedMethodDesc(pMethod, thOwner.GetMethodTable(),
                                                            isUnboxingStub,
                                                            inst,
                                                            !(isInstantiatingStub || isUnboxingStub));

    g_IBCLogger.LogMethodDescAccess(pMethod);

    if (methodFlags & ENCODE_METHOD_SIG_Constrained)
    {
        TypeHandle constrainedType = sig.GetTypeHandleThrowing(pInfoModule,
                                                &typeContext,
                                                ClassLoader::LoadTypes,
                                                CLASS_LOADED,
                                                FALSE,
                                                NULL,
                                                pZapSigContext);

        MethodDesc * directMethod = constrainedType.GetMethodTable()->TryResolveConstraintMethodApprox(thOwner.GetMethodTable(), pMethod);
        if (directMethod == NULL)
        {
            // Method on value type was removed. Boxing stub would need to be generated to handle this case.
            _ASSERTE(!"Constrained method resolution failed");

            MemberLoader::ThrowMissingMethodException(constrainedType.GetMethodTable(), NULL, NULL, NULL, 0, NULL);
        }

        pMethod = directMethod;
    }

    return pMethod;
}

FieldDesc * ZapSig::DecodeField(Module *pReferencingModule,
    Module *pInfoModule,
    PCCOR_SIGNATURE pBuffer,
    TypeHandle * ppTH /*=NULL*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    FieldDesc *pField = NULL;

    SigPointer sig(pBuffer);

    DWORD fieldFlags;
    IfFailThrow(sig.GetData(&fieldFlags));

    MethodTable *pOwnerMT = NULL;

    if (fieldFlags & ENCODE_FIELD_SIG_OwnerType)
    {
        ZapSig::Context    zapSigContext(pInfoModule, pReferencingModule);
        ZapSig::Context *  pZapSigContext = &zapSigContext;

        SigTypeContext typeContext;    // empty context is OK: encoding should not contain type variables.

        pOwnerMT = sig.GetTypeHandleThrowing(pInfoModule,
                                        &typeContext,
                                        ClassLoader::LoadTypes,
                                        CLASS_LOADED,
                                        FALSE,
                                        NULL,
                                        pZapSigContext).GetMethodTable();

        IfFailThrow(sig.SkipExactlyOne());
    }

    if (fieldFlags & ENCODE_FIELD_SIG_IndexInsteadOfToken)
    {
        // get the field desc using index
        DWORD fieldIndex;
        IfFailThrow(sig.GetData(&fieldIndex));

        _ASSERTE(pOwnerMT != NULL);

        pField = pOwnerMT->GetFieldDescByIndex(fieldIndex);
        _ASSERTE(pOwnerMT == pField->GetApproxEnclosingMethodTable());
    }
    else
    {
        RID rid;
        IfFailThrow(sig.GetData(&rid));

        if (fieldFlags & ENCODE_FIELD_SIG_MemberRefToken)
        {
            if (pOwnerMT == NULL)
            {
                TypeHandle th;
                MethodDesc * pMD = NULL;
                FieldDesc * pFD = NULL;

                MemberLoader::GetDescFromMemberRef(pInfoModule, TokenFromRid(rid, mdtMemberRef), &pMD, &pFD, NULL, FALSE, &th);
                _ASSERTE(pFD != NULL);

                pField = pFD;
            }
            else
            {
                pField = MemberLoader::GetFieldDescFromMemberRefAndType(pInfoModule, TokenFromRid(rid, mdtMemberRef), pOwnerMT);
            }
        }
        else
        {
            pField = MemberLoader::GetFieldDescFromFieldDef(pInfoModule, TokenFromRid(rid, mdtFieldDef), FALSE);
        }
    }

    if (ppTH != NULL)
        *ppTH = (pOwnerMT != NULL) ? pOwnerMT : pField->GetApproxEnclosingMethodTable();

    return pField;
}

/* static */
BOOL ZapSig::EncodeMethod(
                    MethodDesc *          pMethod,
                    Module *              pInfoModule,
                    SigBuilder *          pSigBuilder,
                    LPVOID                pEncodeModuleContext,
                    ENCODEMODULE_CALLBACK pfnEncodeModule,
                    DEFINETOKEN_CALLBACK  pfnDefineToken,
                    CORINFO_RESOLVED_TOKEN * pResolvedToken,
                    CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    TypeHandle ownerType;

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (pResolvedToken == NULL)
        {
            _ASSERTE(!"CORINFO_RESOLVED_TOKEN required to encode method!");
            ThrowHR(E_FAIL);
        }

        // Encode the referencing method type
        ownerType = TypeHandle(pResolvedToken->hClass);
    }
    else
#endif
    {
        ownerType = pMethod->GetMethodTable_NoLogging();
    }

    ZapSig::ExternalTokens externalTokens = ZapSig::NormalTokens;
    if (pfnDefineToken != NULL)
    {
        externalTokens = ZapSig::IbcTokens;
    }

    ZapSig zapSig(pInfoModule, pEncodeModuleContext, externalTokens,
                    (EncodeModuleCallback)    pfnEncodeModule, 
                    (TokenDefinitionCallback) pfnDefineToken);

    //
    // output the sequence that represents the token for the method
    // 
    mdMethodDef methodToken               = pMethod->GetMemberDef_NoLogging();
    DWORD       methodFlags               = 0;
    BOOL        fMethodNeedsInstantiation = pMethod->HasMethodInstantiation() && !pMethod->IsGenericMethodDefinition();

    if (pMethod->IsUnboxingStub())
        methodFlags |= ENCODE_METHOD_SIG_UnboxingStub;
    if (pMethod->IsInstantiatingStub())
        methodFlags |= ENCODE_METHOD_SIG_InstantiatingStub;
    if (fMethodNeedsInstantiation)
        methodFlags |= ENCODE_METHOD_SIG_MethodInstantiation;

    //
    // For backward compatibility, IBC tokens use slightly different encoding:
    // - Owning type is uncoditionally encoded
    // - Number of method instantiation arguments is not encoded
    //
    if (externalTokens == ZapSig::IbcTokens)
    {
        // The type is always encoded before flags for IBC
        if (!zapSig.GetSignatureForTypeHandle(ownerType, pSigBuilder))
            return FALSE;
    }
    else
    {
        // Assume that the owner type is going to be needed
        methodFlags |= ENCODE_METHOD_SIG_OwnerType;
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (pConstrainedResolvedToken != NULL)
        {
            methodFlags |= ENCODE_METHOD_SIG_Constrained;
        }

        Module * pReferencingModule = (Module *)pResolvedToken->tokenScope;

        if (!pReferencingModule->IsInCurrentVersionBubble())
        {
            // FUTURE: Encoding of new cross-module references for ReadyToRun
            // This warning is hit for recursive cross-module inlining. It is commented out to avoid noise.
            // GetSvcLogger()->Printf(W("ReadyToRun: Method reference outside of current version bubble cannot be encoded\n"));
            ThrowHR(E_FAIL);
        }
        _ASSERTE(pReferencingModule == GetAppDomain()->ToCompilationDomain()->GetTargetModule());

        methodToken = pResolvedToken->token;

        if (TypeFromToken(methodToken) == mdtMethodSpec)
        {
            IfFailThrow(pReferencingModule->GetMDImport()->GetMethodSpecProps(methodToken, &methodToken, NULL, NULL));
        }

        switch (TypeFromToken(methodToken))
        {
        case mdtMethodDef:
            _ASSERTE(pResolvedToken->pTypeSpec == NULL);
            methodFlags &= ~ENCODE_METHOD_SIG_OwnerType;
            break;

        case mdtMemberRef:
            methodFlags |= ENCODE_METHOD_SIG_MemberRefToken;

            if (pResolvedToken->pTypeSpec == NULL)
            {
                methodFlags &= ~ENCODE_METHOD_SIG_OwnerType;
            }
            else
            if (!(methodFlags & ENCODE_METHOD_SIG_InstantiatingStub))
            {
                if (SigPointer(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec).IsPolyType(NULL) == hasNoVars)
                    methodFlags &= ~ENCODE_METHOD_SIG_OwnerType;
            }
            break;

        default:
            _ASSERTE(!"Unexpected method token type!");
            ThrowHR(E_NOTIMPL);
        }
    }
    else
#endif
    if (IsNilToken(methodToken))
    {
        methodFlags |= ENCODE_METHOD_SIG_SlotInsteadOfToken;
    }
    else
    if (!pMethod->GetModule()->IsInCurrentVersionBubble())
    {
        // Using a method defined in another version bubble. We can assume the slot number is stable only for real interface methods.
        if (!ownerType.IsInterface() || pMethod->IsStatic() || pMethod->HasMethodInstantiation())
        {
            // FUTURE TODO: Version resilience
            _ASSERTE(!"References to non-interface methods not yet supported in version resilient images");
            IfFailThrow(E_FAIL);
        }
        methodFlags |= ENCODE_METHOD_SIG_SlotInsteadOfToken;
    }
    else
    {
        Module * pTypeHandleModule = pMethod->GetModule();
        
        if (pTypeHandleModule != pInfoModule) 
        {
            // During IBC profiling this calls
            //     code:Module.EncodeModuleHelper
            // During ngen this calls
            //     code:ZapImportTable.EncodeModuleHelper)
            //
            DWORD index = (*((EncodeModuleCallback) pfnEncodeModule))(pEncodeModuleContext, pTypeHandleModule);
            
            if (index == ENCODE_MODULE_FAILED)
            {
                return FALSE;
            }

            // If the method handle's module is different that the pInfoModule 
            // we need to call the TokenDefinitionCallback function
            // to record the names for the external module tokens
            //
            if ((index != 0) && (pfnDefineToken != NULL))
            {
                //
                // We do not want to log the metadata lookups that we perform here
                // 
                IBCLoggingDisabler   disableLogging;
                
                // During IBC profiling this calls
                //     code:Module::TokenDefinitionHelper()
                (*((TokenDefinitionCallback) pfnDefineToken))(pEncodeModuleContext, pTypeHandleModule, index, &methodToken);
            }
        }
        else
        {
            _ASSERTE(pInfoModule = pMethod->GetModule());
        }

        if (!ownerType.HasInstantiation())
            methodFlags &= ~ENCODE_METHOD_SIG_OwnerType;
    }

    //
    // output the flags
    // 
    pSigBuilder->AppendData(methodFlags);

    if (methodFlags & ENCODE_METHOD_SIG_OwnerType)
    {
        if (!zapSig.GetSignatureForTypeHandle(ownerType, pSigBuilder))
            return FALSE;
    }

    if ((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == 0)
    {
        // emit the rid
        pSigBuilder->AppendData(RidFromToken(methodToken));
    }
    else
    {
        // have no token (e.g. it could be an array), encode slot number
        pSigBuilder->AppendData(pMethod->GetSlot());
    }

    if ((methodFlags & ENCODE_METHOD_SIG_MethodInstantiation) != 0)
    {
        Instantiation inst = pMethod->GetMethodInstantiation();

        // Number of method instantiation arguments is not encoded in IBC tokens - see comment above
        if (externalTokens != ZapSig::IbcTokens)
            pSigBuilder->AppendData(inst.GetNumArgs());

        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            TypeHandle t = inst[i];
            _ASSERTE(!t.IsNull());

            if (!zapSig.GetSignatureForTypeHandle(t, pSigBuilder))
                return FALSE;
        }
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if ((methodFlags & ENCODE_METHOD_SIG_Constrained) != 0)
    {
        if (!zapSig.GetSignatureForTypeHandle(TypeHandle(pConstrainedResolvedToken->hClass), pSigBuilder))
            return FALSE;
    }
#endif

    return TRUE;
}

void ZapSig::EncodeField(
        FieldDesc              *pField,
        Module                 *pInfoModule,
        SigBuilder             *pSigBuilder,
        LPVOID                 pEncodeModuleContext,
        ENCODEMODULE_CALLBACK  pfnEncodeModule,
        CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable * pMT;

    mdMethodDef fieldToken = pField->GetMemberDef();
    DWORD fieldFlags = ENCODE_FIELD_SIG_OwnerType;

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (pResolvedToken == NULL)
        {
            _ASSERTE(!"CORINFO_RESOLVED_TOKEN required to encode field!");
            ThrowHR(E_FAIL);
        }

        // Encode the referencing field type
        pMT = (MethodTable *)(pResolvedToken->hClass);

        Module * pReferencingModule = (Module *)pResolvedToken->tokenScope;

        if (!pReferencingModule->IsInCurrentVersionBubble())
        {
            // FUTURE: Encoding of new cross-module references for ReadyToRun
            // This warning is hit for recursive cross-module inlining. It is commented out to avoid noise.
            // GetSvcLogger()->Printf(W("ReadyToRun: Field reference outside of current version bubble cannot be encoded\n"));
            ThrowHR(E_FAIL);
        }
        _ASSERTE(pReferencingModule == GetAppDomain()->ToCompilationDomain()->GetTargetModule());

        fieldToken = pResolvedToken->token;

        switch (TypeFromToken(fieldToken))
        {
        case mdtFieldDef:
            _ASSERTE(pResolvedToken->pTypeSpec == NULL);
            fieldFlags &= ~ENCODE_FIELD_SIG_OwnerType;
            break;

        case mdtMemberRef:
            fieldFlags |= ENCODE_FIELD_SIG_MemberRefToken;

            if (pResolvedToken->pTypeSpec == NULL)
            {
                fieldFlags &= ~ENCODE_METHOD_SIG_OwnerType;
            }
            else
            {
                if (SigPointer(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec).IsPolyType(NULL) == hasNoVars)
                    fieldFlags &= ~ENCODE_METHOD_SIG_OwnerType;
            }
            break;

        default:
            _ASSERTE(!"Unexpected field token type!");
            ThrowHR(E_NOTIMPL);
        }
    }
    else
#endif
    {
        pMT = pField->GetApproxEnclosingMethodTable();

        fieldFlags |= ENCODE_FIELD_SIG_IndexInsteadOfToken;
    }

    //
    // output the flags
    // 
    pSigBuilder->AppendData(fieldFlags);

    if (fieldFlags & ENCODE_FIELD_SIG_OwnerType)
    {
        ZapSig zapSig(pInfoModule, pEncodeModuleContext, ZapSig::NormalTokens,
                        (EncodeModuleCallback) pfnEncodeModule, NULL);

        //
        // Write class
        //
        BOOL fSuccess;
        fSuccess = zapSig.GetSignatureForTypeHandle(pMT, pSigBuilder);
        _ASSERTE(fSuccess);
    }

    if ((fieldFlags & ENCODE_FIELD_SIG_IndexInsteadOfToken) == 0)
    {
        // emit the rid
        pSigBuilder->AppendData(RidFromToken(fieldToken));
    }
    else
    {
        //
        // Write field index
        //

        DWORD fieldIndex = pMT->GetIndexForFieldDesc(pField);
        _ASSERTE(fieldIndex < DWORD(pMT->GetNumStaticFields() + pMT->GetNumIntroducedInstanceFields()));

        // have no token (e.g. it could be an array), encode slot number
        pSigBuilder->AppendData(fieldIndex);
    }
}

#endif // DACCESS_COMPILE

#endif // !BINDER

#endif // FEATURE_PREJIT
