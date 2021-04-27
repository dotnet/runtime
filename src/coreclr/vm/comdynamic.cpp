// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
////////////////////////////////////////////////////////////////////////////////
// COMDynamic.h
//  This module defines the native methods that are used for Dynamic IL generation

////////////////////////////////////////////////////////////////////////////////


#include "common.h"
#include "field.h"
#include "comdynamic.h"
#include "commodule.h"
#include "reflectclasswriter.h"
#include "corerror.h"
#include "iceefilegen.h"
#include "ceefilegenwriter.h"
#include "typekey.h"


//This structure is used in SetMethodIL to walk the exceptions.
//It maps to System.Reflection.Emit.ExceptionHandler class
//DO NOT MOVE ANY OF THE FIELDS
#include <pshpack1.h>
struct ExceptionInstance {
    INT32 m_exceptionType;
    INT32 m_start;
    INT32 m_end;
    INT32 m_filterOffset;
    INT32 m_handle;
    INT32 m_handleEnd;
    INT32 m_type;
};
#include <poppack.h>


//*************************************************************
//
// Defining a type into metadata of this dynamic module
//
//*************************************************************
INT32 QCALLTYPE COMDynamicWrite::DefineGenericParam(QCall::ModuleHandle pModule,
                                                    LPCWSTR wszFullName,
                                                    INT32 tkParent,
                                                    INT32 attributes,
                                                    INT32 position,
                                                    INT32 * pConstraintTokens)
{
    QCALL_CONTRACT;

    mdTypeDef           classE = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    IfFailThrow(pRCW->GetEmitter()->DefineGenericParam(
        tkParent, position, attributes, wszFullName, 0, (mdToken *)pConstraintTokens, &classE));

    END_QCALL;

    return (INT32)classE;
}

INT32 QCALLTYPE COMDynamicWrite::DefineType(QCall::ModuleHandle pModule,
                                            LPCWSTR wszFullName,
                                            INT32 tkParent,
                                            INT32 attributes,
                                            INT32 tkEnclosingType,
                                            INT32 * pInterfaceTokens)
{
    QCALL_CONTRACT;

    mdTypeDef           classE = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    HRESULT hr;

    if (RidFromToken(tkEnclosingType))
    {
        // defining nested type
        hr = pRCW->GetEmitter()->DefineNestedType(wszFullName,
                                                  attributes,
                                                  tkParent == 0 ? mdTypeRefNil : tkParent,
                                                  (mdToken *)pInterfaceTokens,
                                                  tkEnclosingType,
                                                  &classE);
    }
    else
    {
        // top level type
        hr = pRCW->GetEmitter()->DefineTypeDef(wszFullName,
                                               attributes,
                                               tkParent == 0 ? mdTypeRefNil : tkParent,
                                               (mdToken *)pInterfaceTokens,
                                               &classE);
    }

    if (hr == META_S_DUPLICATE)
    {
        COMPlusThrow(kArgumentException, W("Argument_DuplicateTypeName"));
    }

    if (FAILED(hr)) {
        _ASSERTE(hr == E_OUTOFMEMORY || !"DefineTypeDef Failed");
        COMPlusThrowHR(hr);
    }

    AllocMemTracker amTracker;
    pModule->GetClassLoader()->AddAvailableClassDontHaveLock(pModule,
                                                    classE,
                                                    &amTracker);
    amTracker.SuppressRelease();

    END_QCALL;

    return (INT32)classE;
}

// This function will reset the parent class in metadata
void QCALLTYPE COMDynamicWrite::SetParentType(QCall::ModuleHandle pModule, INT32 tdType, INT32 tkParent)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    IfFailThrow( pRCW->GetEmitHelper()->SetTypeParent(tdType, tkParent) );

    END_QCALL;
}

// This function will add another interface impl
void QCALLTYPE COMDynamicWrite::AddInterfaceImpl(QCall::ModuleHandle pModule, INT32 tdType, INT32 tkInterface)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    IfFailThrow( pRCW->GetEmitHelper()->AddInterfaceImpl(tdType, tkInterface) );

    END_QCALL;
}

// This function will create a method within the class
INT32 QCALLTYPE COMDynamicWrite::DefineMethodSpec(QCall::ModuleHandle pModule, INT32 tkParent, LPCBYTE pSignature, INT32 sigLength)
{
    QCALL_CONTRACT;

    mdMethodDef memberE = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Define the Method
    IfFailThrow( pRCW->GetEmitter()->DefineMethodSpec(tkParent,         //ParentTypeDef
                                                      (PCCOR_SIGNATURE)pSignature, //Blob value of a COM+ signature
                                                      sigLength,            //Size of the signature blob
                                                      &memberE) );              //[OUT]methodToken

    END_QCALL;

    return (INT32) memberE;
}

INT32 QCALLTYPE COMDynamicWrite::DefineMethod(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, LPCBYTE pSignature, INT32 sigLength, INT32 attributes)
{
    QCALL_CONTRACT;

    mdMethodDef memberE = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Define the Method
    IfFailThrow( pRCW->GetEmitter()->DefineMethod(tkParent,        //ParentTypeDef
                                                  wszName,         //Name of Member
                                                  attributes,               //Member Attributes (public, etc);
                                                  (PCCOR_SIGNATURE)pSignature,  //Blob value of a COM+ signature
                                                  sigLength,            //Size of the signature blob
                                                  0,                        //Code RVA
                                                  miIL | miManaged,         //Implementation Flags is default to managed IL
                                                  &memberE) );              //[OUT]methodToken

    END_QCALL;

    return (INT32) memberE;
}

/*================================DefineField=================================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
mdFieldDef QCALLTYPE COMDynamicWrite::DefineField(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, LPCBYTE pSignature, INT32 sigLength, INT32 attr)
{
    QCALL_CONTRACT;

    mdFieldDef retVal = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    //Emit the field.
    IfFailThrow( pRCW->GetEmitter()->DefineField(tkParent,
                                                 wszName, attr,
                                                 (PCCOR_SIGNATURE)pSignature, sigLength,
                                                 ELEMENT_TYPE_VOID, NULL,
                                                 (ULONG) -1, &retVal) );


    END_QCALL;

    return retVal;
}

// This method computes the same result as COR_ILMETHOD_SECT_EH::Size(...) but
// does so in a way that detects overflow if the number of exception clauses is
// too great (in which case an OOM exception is thrown). We do this rather than
// modifying COR_ILMETHOD_SECT_EH::Size because that routine is published in the
// SDK and can't take breaking changes and because the overflow support (and
// exception mechanism) we're using is only available to the VM.
UINT32 ExceptionHandlingSize(unsigned uNumExceptions, COR_ILMETHOD_SECT_EH_CLAUSE_FAT* pClauses)
{
    STANDARD_VM_CONTRACT;

    if (uNumExceptions == 0)
        return 0;

    // Speculatively compute the size for the slim version of the header.
    S_UINT32 uSmallSize = S_UINT32(sizeof(COR_ILMETHOD_SECT_EH_SMALL)) +
        (S_UINT32(sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL)) * (S_UINT32(uNumExceptions - 1)));

    if (uSmallSize.IsOverflow())
        COMPlusThrowOM();

    if (uSmallSize.Value() > COR_ILMETHOD_SECT_SMALL_MAX_DATASIZE)
        goto FatCase;

    // Check whether any of the clauses won't fit in the slim case.
    for (UINT32 i = 0; i < uNumExceptions; i++) {
        COR_ILMETHOD_SECT_EH_CLAUSE_FAT* pFatClause = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)&pClauses[i];
        if (pFatClause->GetTryOffset() > 0xFFFF ||
            pFatClause->GetTryLength() > 0xFF ||
            pFatClause->GetHandlerOffset() > 0xFFFF ||
            pFatClause->GetHandlerLength() > 0xFF) {
            goto FatCase;
        }
    }

    _ASSERTE(uSmallSize.Value() == COR_ILMETHOD_SECT_EH::Size(uNumExceptions, pClauses));
    return uSmallSize.Value();

 FatCase:
    S_UINT32 uFatSize = S_UINT32(sizeof(COR_ILMETHOD_SECT_EH_FAT)) +
        (S_UINT32(sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT)) * (S_UINT32(uNumExceptions - 1)));

    if (uFatSize.IsOverflow())
        COMPlusThrowOM();

    _ASSERTE(uFatSize.Value() == COR_ILMETHOD_SECT_EH::Size(uNumExceptions, pClauses));
    return uFatSize.Value();
}


// SetMethodIL -- This function will create a method within the class
void QCALLTYPE COMDynamicWrite::SetMethodIL(QCall::ModuleHandle pModule,
                                            INT32 tk,
                                            BOOL fIsInitLocal,
                                            LPCBYTE pBody,
                                            INT32 cbBody,
                                            LPCBYTE pLocalSig,
                                            INT32 sigLength,
                                            UINT16 maxStackSize,
                                            ExceptionInstance * pExceptions,
                                            INT32 numExceptions,
                                            INT32 * pTokenFixups,
                                            INT32 numTokenFixups)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    _ASSERTE(pLocalSig);

    PCCOR_SIGNATURE pcSig = (PCCOR_SIGNATURE)pLocalSig;
    _ASSERTE(*pcSig == IMAGE_CEE_CS_CALLCONV_LOCAL_SIG);

    mdSignature pmLocalSigToken;
    if (sigLength==2 && pcSig[0]==0 && pcSig[1]==0)
    {
        //This is an empty local variable sig
        pmLocalSigToken=0;
    }
    else
    {
        IfFailThrow(pRCW->GetEmitter()->GetTokenFromSig( pcSig, sigLength, &pmLocalSigToken));
    }

    COR_ILMETHOD_FAT fatHeader;

    // set fatHeader.Flags to CorILMethod_InitLocals if user wants to zero init the stack frame.
    //
    fatHeader.SetFlags(fIsInitLocal ? CorILMethod_InitLocals : 0);
    fatHeader.SetMaxStack(maxStackSize);
    fatHeader.SetLocalVarSigTok(pmLocalSigToken);
    fatHeader.SetCodeSize(cbBody);
    bool moreSections            = (numExceptions != 0);

    unsigned codeSizeAligned     = fatHeader.GetCodeSize();
    if (moreSections)
        codeSizeAligned = AlignUp(codeSizeAligned, 4); // to insure EH section aligned
    unsigned headerSize          = COR_ILMETHOD::Size(&fatHeader, numExceptions != 0);

    //Create the exception handlers.
    CQuickArray<COR_ILMETHOD_SECT_EH_CLAUSE_FAT> clauses;
    if (numExceptions > 0)
    {
        clauses.AllocThrows(numExceptions);

        for (int i = 0; i < numExceptions; i++)
        {
            clauses[i].SetFlags((CorExceptionFlag)(pExceptions[i].m_type));
            clauses[i].SetTryOffset(pExceptions[i].m_start);
            clauses[i].SetTryLength(pExceptions[i].m_end - pExceptions[i].m_start);
            clauses[i].SetHandlerOffset(pExceptions[i].m_handle);
            clauses[i].SetHandlerLength(pExceptions[i].m_handleEnd - pExceptions[i].m_handle);
            if (pExceptions[i].m_type == COR_ILEXCEPTION_CLAUSE_FILTER)
            {
                clauses[i].SetFilterOffset(pExceptions[i].m_filterOffset);
            }
            else if (pExceptions[i].m_type!=COR_ILEXCEPTION_CLAUSE_FINALLY)
            {
                clauses[i].SetClassToken(pExceptions[i].m_exceptionType);
            }
            else
            {
                clauses[i].SetClassToken(mdTypeRefNil);
            }
        }
    }

    unsigned ehSize          = ExceptionHandlingSize(numExceptions, clauses.Ptr());
    S_UINT32 totalSizeSafe   = S_UINT32(headerSize) + S_UINT32(codeSizeAligned) + S_UINT32(ehSize);
    if (totalSizeSafe.IsOverflow())
        COMPlusThrowOM();
    UINT32 totalSize = totalSizeSafe.Value();
    ICeeGenInternal* pGen = pRCW->GetCeeGen();
    BYTE* buf = NULL;
    ULONG methodRVA;
    pGen->AllocateMethodBuffer(totalSize, &buf, &methodRVA);
    if (buf == NULL)
        COMPlusThrowOM();

    _ASSERTE(buf != NULL);
    _ASSERTE((((size_t) buf) & 3) == 0);   // header is dword aligned

#ifdef _DEBUG
    BYTE* endbuf = &buf[totalSize];
#endif

    BYTE * startBuf = buf;

    // Emit the header
    buf += COR_ILMETHOD::Emit(headerSize, &fatHeader, moreSections, buf);

    //Emit the code
    //The fatHeader.CodeSize is a workaround to see if we have an interface or an
    //abstract method.  Force enough verification in native to ensure that
    //this is true.
    if (fatHeader.GetCodeSize()!=0) {
        memcpy(buf, pBody, fatHeader.GetCodeSize());
    }
    buf += codeSizeAligned;

    // Emit the eh
    CQuickArray<ULONG> ehTypeOffsets;
    if (numExceptions > 0)
    {
        // Allocate space for the the offsets to the TypeTokens in the Exception headers
        // in the IL stream.
        ehTypeOffsets.AllocThrows(numExceptions);

        // Emit the eh.  This will update the array ehTypeOffsets with offsets
        // to Exception type tokens.  The offsets are with reference to the
        // beginning of eh section.
        buf += COR_ILMETHOD_SECT_EH::Emit(ehSize, numExceptions, clauses.Ptr(),
                                          false, buf, ehTypeOffsets.Ptr());
    }
    _ASSERTE(buf == endbuf);

    //Get the IL Section.
    HCEESECTION ilSection;
    IfFailThrow(pGen->GetIlSection(&ilSection));

    // Token Fixup data...
    ULONG ilOffset = methodRVA + headerSize;

    //Add all of the relocs based on the info which I saved from ILGenerator.

    //Add the Token Fixups
    for (int iTokenFixup=0; iTokenFixup<numTokenFixups; iTokenFixup++)
    {
        IfFailThrow(pGen->AddSectionReloc(ilSection, pTokenFixups[iTokenFixup] + ilOffset, ilSection, srRelocMapToken));
    }

    // Add token fixups for exception type tokens.
    for (int iException=0; iException < numExceptions; iException++)
    {
        if (ehTypeOffsets[iException] != (ULONG) -1)
        {
            IfFailThrow(pGen->AddSectionReloc(
                                             ilSection,
                                             ehTypeOffsets[iException] + codeSizeAligned + ilOffset,
                                             ilSection, srRelocMapToken));
        }
    }

    //nasty interface workaround.  What does this mean for abstract methods?
    if (fatHeader.GetCodeSize() != 0)
    {
        // add the starting address of the il blob to the il blob hash table
        // we need to find this information from out of process for debugger inspection
        // APIs so we have to store this information where we can get it later
        pModule->SetDynamicIL(mdToken(tk), TADDR(startBuf), FALSE);

        DWORD       dwImplFlags;

        //Set the RVA of the method.
        IfFailThrow(pRCW->GetMDImport()->GetMethodImplProps(tk, NULL, &dwImplFlags));
        dwImplFlags |= (miManaged | miIL);
        IfFailThrow(pRCW->GetEmitter()->SetMethodProps(tk, (DWORD) -1, methodRVA, dwImplFlags));
    }

    END_QCALL;
}

void QCALLTYPE COMDynamicWrite::TermCreateClass(QCall::ModuleHandle pModule, INT32 tk, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;

    TypeHandle typeHnd;

    BEGIN_QCALL;

    _ASSERTE(pModule->GetReflectionModule()->GetClassWriter());

    // Use the same service, regardless of whether we are generating a normal
    // class, or the special class for the module that holds global functions
    // & methods.
    pModule->GetReflectionModule()->AddClass(tk);

    // manually load the class if it is not the global type
    if (!IsNilToken(tk))
    {
        TypeKey typeKey(pModule, tk);
        typeHnd = pModule->GetClassLoader()->LoadTypeHandleForTypeKey(&typeKey, TypeHandle());
    }

    if (!typeHnd.IsNull())
    {
        GCX_COOP();
        retType.Set(typeHnd.GetManagedClassObject());
    }

    END_QCALL;

    return;
}

/*============================SetPInvokeData============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
void QCALLTYPE COMDynamicWrite::SetPInvokeData(QCall::ModuleHandle pModule, LPCWSTR wszDllName, LPCWSTR wszFunctionName, INT32 token, INT32 linkFlags)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    mdModuleRef mrImportDll = mdTokenNil;
    IfFailThrow(pRCW->GetEmitter()->DefineModuleRef(wszDllName, &mrImportDll));

    IfFailThrow(pRCW->GetEmitter()->DefinePinvokeMap(
        token,                        // the method token
        linkFlags,                      // the mapping flags
        wszFunctionName,                // function name
        mrImportDll));

    IfFailThrow(pRCW->GetEmitter()->SetMethodProps(token, (DWORD) -1, 0x0, miIL));

    END_QCALL;
}

/*============================DefineProperty============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
INT32 QCALLTYPE COMDynamicWrite::DefineProperty(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, INT32 attr, LPCBYTE pSignature, INT32 sigLength)
{
    QCALL_CONTRACT;

    mdProperty      pr = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Define the Property
    IfFailThrow(pRCW->GetEmitter()->DefineProperty(
            tkParent,                       // ParentTypeDef
            wszName,                        // Name of Member
            attr,                     // property Attributes (prDefaultProperty, etc);
            (PCCOR_SIGNATURE)pSignature,    // Blob value of a COM+ signature
            sigLength,                // Size of the signature blob
            ELEMENT_TYPE_VOID,              // don't specify the default value
            0,                              // no default value
            (ULONG) -1,                     // optional length
            mdMethodDefNil,                 // no setter
            mdMethodDefNil,                 // no getter
            NULL,                           // no other methods
            &pr));

    END_QCALL;

    return (INT32)pr;
}

/*============================DefineEvent============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
INT32 QCALLTYPE COMDynamicWrite::DefineEvent(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, INT32 attr, INT32 tkEventType)
{
    QCALL_CONTRACT;

    mdProperty      ev = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Define the Event
    IfFailThrow(pRCW->GetEmitHelper()->DefineEventHelper(
            tkParent,               // ParentTypeDef
            wszName,                // Name of Member
            attr,                       // property Attributes (prDefaultProperty, etc);
            tkEventType,            // the event type. Can be TypeDef or TypeRef
            &ev));

    END_QCALL;

    return (INT32)ev;
}

/*============================DefineMethodSemantics============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
void QCALLTYPE COMDynamicWrite::DefineMethodSemantics(QCall::ModuleHandle pModule, INT32 tkAssociation, INT32 attr, INT32 tkMethod)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Define the MethodSemantics
    IfFailThrow(pRCW->GetEmitHelper()->DefineMethodSemanticsHelper(
            tkAssociation,
            attr,
            tkMethod));

    END_QCALL;
}

/*============================SetMethodImpl============================
** To set a Method's Implementation flags
==============================================================================*/
void QCALLTYPE COMDynamicWrite::SetMethodImpl(QCall::ModuleHandle pModule, INT32 tkMethod, INT32 attr)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Set the methodimpl flags
    IfFailThrow(pRCW->GetEmitter()->SetMethodImplFlags(
            tkMethod,
            attr));                // change the impl flags

    END_QCALL;
}

/*============================DefineMethodImpl============================
** Define a MethodImpl record
==============================================================================*/
void QCALLTYPE COMDynamicWrite::DefineMethodImpl(QCall::ModuleHandle pModule, UINT32 tkType, UINT32 tkBody, UINT32 tkDecl)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Set the methodimpl flags
    IfFailThrow(pRCW->GetEmitter()->DefineMethodImpl(
            tkType,
            tkBody,
            tkDecl));                  // change the impl flags

    END_QCALL;
}

/*============================GetTokenFromSig============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
INT32 QCALLTYPE COMDynamicWrite::GetTokenFromSig(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength)
{
    QCALL_CONTRACT;

    mdSignature retVal = 0;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    _ASSERTE(pSignature);

    // Define the signature
    IfFailThrow(pRCW->GetEmitter()->GetTokenFromSig(
            pSignature,                     // Signature blob
            sigLength,                      // blob length
            &retVal));                      // returned token

    END_QCALL;

    return (INT32)retVal;
}

/*============================SetParamInfo============================
**Action: Helper to set parameter information
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
INT32 QCALLTYPE COMDynamicWrite::SetParamInfo(QCall::ModuleHandle pModule, UINT32 tkMethod, UINT32 iSequence, UINT32 iAttributes, LPCWSTR wszParamName)
{
    QCALL_CONTRACT;

    mdParamDef retVal = 0;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Set the methodimpl flags
    IfFailThrow(pRCW->GetEmitter()->DefineParam(
            tkMethod,
            iSequence,            // sequence of the parameter
            wszParamName,
            iAttributes,          // change the impl flags
            ELEMENT_TYPE_VOID,
            0,
            (ULONG) -1,
            &retVal));

    END_QCALL;

    return (INT32)retVal;
}


/*============================SetConstantValue============================
**Action: Helper to set constant value to field or parameter
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
void QCALLTYPE COMDynamicWrite::SetConstantValue(QCall::ModuleHandle pModule, UINT32 tk, DWORD valueCorType, LPVOID pValue)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    HRESULT hr;

    if (TypeFromToken(tk) == mdtFieldDef)
    {
        hr = pRCW->GetEmitter()->SetFieldProps(
            tk,                         // [IN] The FieldDef.
            UINT32_MAX,                 // [IN] Field attributes.
            valueCorType,               // [IN] Flag for the value type, selected ELEMENT_TYPE_*
            pValue,                     // [IN] Constant value.
            (ULONG) -1);                // [IN] Optional length.
    }
    else if (TypeFromToken(tk) == mdtProperty)
    {
        hr = pRCW->GetEmitter()->SetPropertyProps(
            tk,                         // [IN] The PropertyDef.
            UINT32_MAX,                 // [IN] Property attributes.
            valueCorType,               // [IN] Flag for the value type, selected ELEMENT_TYPE_*
            pValue,                     // [IN] Constant value.
            (ULONG) -1,                 // [IN] Optional length.
            mdMethodDefNil,             // [IN] Getter method.
            mdMethodDefNil,             // [IN] Setter method.
            NULL);                      // [IN] Other methods.
    }
    else
    {
        hr = pRCW->GetEmitter()->SetParamProps(
            tk,                   // [IN] The ParamDef.
            NULL,
            UINT32_MAX,                 // [IN] Parameter attributes.
            valueCorType,               // [IN] Flag for the value type, selected ELEMENT_TYPE_*
            pValue,                     // [IN] Constant value.
            (ULONG) -1);                // [IN] Optional length.
    }
    if (FAILED(hr)) {
        _ASSERTE(!"Set default value is failing");
        COMPlusThrow(kArgumentException, W("Argument_BadConstantValue"));
    }

    END_QCALL;
}

/*============================SetFieldLayoutOffset============================
**Action: set fieldlayout of a field
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
void QCALLTYPE COMDynamicWrite::SetFieldLayoutOffset(QCall::ModuleHandle pModule, INT32 tkField, INT32 iOffset)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Set the field layout
    IfFailThrow(pRCW->GetEmitHelper()->SetFieldLayoutHelper(
            tkField,                  // field
            iOffset));                // layout offset

    END_QCALL;
}


/*============================SetClassLayout============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
void QCALLTYPE COMDynamicWrite::SetClassLayout(QCall::ModuleHandle pModule, INT32 tk, INT32 iPackSize, UINT32 iTotalSize)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter* pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    // Define the packing size and total size of a class
    IfFailThrow(pRCW->GetEmitter()->SetClassLayout(
            tk,                     // Typedef
            iPackSize,                // packing size
            NULL,                     // no field layout
            iTotalSize));           // total size for the type

    END_QCALL;
}

// Helper function for COMDynamicWrite::DefineCustomAttribute
void UpdateRuntimeStateForAssemblyCustomAttribute(Module* pModule, mdToken tkCustomAttribute, mdToken token, mdToken conTok, LPCBYTE pBlob, INT32 cbBlob)
{
    WRAPPER_NO_CONTRACT;

    LPCUTF8 szNamespace;
    LPCUTF8 szName;

    HRESULT hr = pModule->GetMDImport()->GetNameOfCustomAttribute(tkCustomAttribute, &szNamespace, &szName);
    if (FAILED(hr))
    {
        // If the type name cannot be acquired, then this isn't an interesting CustomAttribute
        return;
    }

    if (szNamespace == NULL || szName == NULL)
    {
        // If either of the namespace or name are NULL, then this isn't an interesting attribute
        return;
    }

    // Debuggable attribute processing
    if ((strcmp(szNamespace, DEBUGGABLE_ATTRIBUTE_TYPE_NAMESPACE) == 0) && (strcmp(szName, DEBUGGABLE_ATTRIBUTE_TYPE_NAME) == 0))
    {
        DWORD flags     = 0;
        DWORD mask      = ~(DACF_OBSOLETE_TRACK_JIT_INFO | DACF_IGNORE_PDBS | DACF_ALLOW_JIT_OPTS) & DACF_CONTROL_FLAGS_MASK;

        if ((cbBlob != 6) && (cbBlob != 8))
        {
            _ASSERTE(!"COMDynamicWrite::CWInternalCreateCustomAttribute - unexpected size for DebuggableAttribute\n");
        }
        else if ( !((pBlob[0] == 1) && (pBlob[1] == 0)) )
        {
            _ASSERTE(!"COMDynamicWrite::CWInternalCreateCustomAttribute - bad format for DebuggableAttribute\n");
        }

        if (pBlob[2] & 0x1)
        {
            flags |= DACF_OBSOLETE_TRACK_JIT_INFO;
        }

        if (pBlob[2] & 0x2)
        {
            flags |= DACF_IGNORE_PDBS;
        }

        if ( ((pBlob[2] & 0x1) == 0) || (pBlob[3] == 0) )
        {
            flags |= DACF_ALLOW_JIT_OPTS;
        }

        Assembly*       pAssembly       = pModule->GetAssembly();
        DomainAssembly* pDomainAssembly = pAssembly->GetDomainAssembly();

        DWORD actualFlags;
        actualFlags =  ((DWORD)pDomainAssembly->GetDebuggerInfoBits() & mask) | flags;
        pDomainAssembly->SetDebuggerInfoBits((DebuggerAssemblyControlFlags)actualFlags);

        actualFlags = ((DWORD)pAssembly->GetDebuggerInfoBits() & mask) | flags;
        pAssembly->SetDebuggerInfoBits((DebuggerAssemblyControlFlags)actualFlags);

        ModuleIterator i = pAssembly->IterateModules();
        while (i.Next())
        {
            actualFlags = ((DWORD)(i.GetModule()->GetDebuggerInfoBits()) & mask) | flags;
            i.GetModule()->SetDebuggerInfoBits((DebuggerAssemblyControlFlags)actualFlags);
        }
    }

    // InternalsVisibleTo and IgnoresAccessChecksTo attribute processing
    if (((strcmp(szNamespace, FRIEND_ASSEMBLY_TYPE_NAMESPACE) == 0) && (strcmp(szName, FRIEND_ASSEMBLY_TYPE_NAME) == 0)) ||
        ((strcmp(szNamespace, SUBJECT_ASSEMBLY_TYPE_NAMESPACE) == 0) && (strcmp(szName, SUBJECT_ASSEMBLY_TYPE_NAME) == 0)))
    {
        Assembly* pAssembly = pModule->GetAssembly();
        pAssembly->UpdateCachedFriendAssemblyInfo();
    }
}

void QCALLTYPE COMDynamicWrite::DefineCustomAttribute(QCall::ModuleHandle pModule, INT32 token, INT32 conTok, LPCBYTE pBlob, INT32 cbBlob)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter* pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    HRESULT hr;
    mdCustomAttribute retToken;

    hr = pRCW->GetEmitter()->DefineCustomAttribute(
            token,
            conTok,
            pBlob,
            cbBlob,
            &retToken);

    if (FAILED(hr))
    {
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_CA);
    }

    if (token == TokenFromRid(1, mdtAssembly))
    {
        UpdateRuntimeStateForAssemblyCustomAttribute(pModule, retToken, token, conTok, pBlob, cbBlob);
    }

    END_QCALL;
}
