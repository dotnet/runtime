//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
////////////////////////////////////////////////////////////////////////////////
// COMDynamic.h
//  This module defines the native methods that are used for Dynamic IL generation  

////////////////////////////////////////////////////////////////////////////////

#ifndef _COMDYNAMIC_H_
#define _COMDYNAMIC_H_

#include "iceefilegen.h"
#include "dbginterface.h"

typedef enum PEFileKinds {
    Dll = 0x1,
    ConsoleApplication = 0x2,
    WindowApplication = 0x3,
} PEFileKinds;

struct ExceptionInstance;

// COMDynamicWrite
// This class defines all the methods that implement the dynamic IL creation process
//  inside reflection.  
class COMDynamicWrite
{
private:

    static void UpdateMethodRVAs(IMetaDataEmit*, IMetaDataImport*, ICeeFileGen *, HCEEFILE, mdTypeDef td, HCEESECTION sdataSection);

public:
    // This function will create the class's metadata definition  
    static
    INT32 QCALLTYPE DefineType(QCall::ModuleHandle pModule,
                               LPCWSTR wszFullName, 
                               INT32 tkParent,                               
                               INT32 attributes,
                               INT32 tkEnclosingType,
                               INT32 * pInterfaceTokens);

    static
    INT32 QCALLTYPE DefineGenericParam(QCall::ModuleHandle pModule,
                                       LPCWSTR wszFullName, 
                                       INT32 tkParent, 
                                       INT32 attributes, 
                                       INT32 position, 
                                       INT32 * pConstraintTokens);

    // This function will reset the parent class in metadata
    static
    void QCALLTYPE SetParentType(QCall::ModuleHandle pModule, INT32 tdType, INT32 tkParent);

    // This function will add another interface impl
    static
    void QCALLTYPE AddInterfaceImpl(QCall::ModuleHandle pModule, INT32 tdType, INT32 tkInterface);

    // This function will create a method within the class
    static 
    INT32 QCALLTYPE DefineMethod(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, LPCBYTE pSignature, INT32 sigLength, INT32 attributes);

    static
    INT32 QCALLTYPE DefineMethodSpec(QCall::ModuleHandle pModule, INT32 tkParent, LPCBYTE pSignature, INT32 sigLength);
    
    // This function will create a method within the class   
    static
    void QCALLTYPE SetMethodIL(QCall::ModuleHandle pModule,
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
                               INT32 numTokenFixups);

    static
    void QCALLTYPE TermCreateClass(QCall::ModuleHandle pModule, INT32 tk, QCall::ObjectHandleOnStack retType);

    static
    mdFieldDef QCALLTYPE DefineField(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, LPCBYTE pSignature, INT32 sigLength, INT32 attr);

    static
    void QCALLTYPE PreSavePEFile(QCall::ModuleHandle pModule, INT32 portableExecutableKind, INT32 imageFileMachine);
    
    static
    void QCALLTYPE SavePEFile(QCall::ModuleHandle pModule, LPCWSTR wszPeName, UINT32 entryPoint, UINT32 fileKind, BOOL isManifestFile);

#ifndef FEATURE_CORECLR
    static 
    void QCALLTYPE DefineNativeResourceFile(QCall::ModuleHandle pModule, LPCWSTR pwzFileName, INT32 portableExecutableKind, INT32 imageFileMachine);

    static 
    void QCALLTYPE DefineNativeResourceBytes(QCall::ModuleHandle pModule, LPCBYTE pbResource, INT32 cbResource, INT32 portableExecutableKind, INT32 imageFileMachine);

    static
    void QCALLTYPE AddResource(QCall::ModuleHandle pModule, LPCWSTR pName, LPCBYTE pResBytes, INT32 resByteCount, UINT32 uFileTk, UINT32 iAttribute, INT32 portableExecutableKind, INT32 imageFileMachine);
#endif // !FEATURE_CORECLR

    // not an ecall!
    static HRESULT EmitDebugInfoBegin(
        Module *pModule,
        ICeeFileGen *pCeeFileGen,
        HCEEFILE ceeFile,
        HCEESECTION pILSection,
        const WCHAR *filename,
        ISymUnmanagedWriter *pWriter);

    // not an ecall!
    static HRESULT EmitDebugInfoEnd(
        Module *pModule,
        ICeeFileGen *pCeeFileGen,
        HCEEFILE ceeFile,
        HCEESECTION pILSection,
        const WCHAR *filename,
        ISymUnmanagedWriter *pWriter);

    static
    void QCALLTYPE SetPInvokeData(QCall::ModuleHandle pModule, LPCWSTR wszDllName, LPCWSTR wszFunctionName, INT32 token, INT32 linkFlags);

    static
    INT32 QCALLTYPE DefineProperty(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, INT32 attr, LPCBYTE pSignature, INT32 sigLength);

    static
    INT32 QCALLTYPE DefineEvent(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, INT32 attr, INT32 tkEventType);

    // functions to set Setter, Getter, Reset, TestDefault, and other methods
    static
    void QCALLTYPE DefineMethodSemantics(QCall::ModuleHandle pModule, INT32 tkAssociation, INT32 attr, INT32 tkMethod);

    // functions to set method's implementation flag
    static
    void QCALLTYPE SetMethodImpl(QCall::ModuleHandle pModule, INT32 tkMethod, INT32 attr);

    // functions to create MethodImpl record
    static
    void QCALLTYPE DefineMethodImpl(QCall::ModuleHandle pModule, UINT32 tkType, UINT32 tkBody, UINT32 tkDecl);

    // GetTokenFromSig's argument
    static
    INT32 QCALLTYPE GetTokenFromSig(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength);

    // Set Field offset
    static
    void QCALLTYPE SetFieldLayoutOffset(QCall::ModuleHandle pModule, INT32 tkField, INT32 iOffset);

    // Set classlayout info
    static
    void QCALLTYPE SetClassLayout(QCall::ModuleHandle pModule, INT32 tk, INT32 iPackSize, UINT32 iTotalSize);

    // Set a custom attribute
    static
    void QCALLTYPE DefineCustomAttribute(QCall::ModuleHandle pModule, INT32 token, INT32 conTok, LPCBYTE pBlob, INT32 cbBlob, BOOL toDisk, BOOL updateCompilerFlags);

    // functions to set ParamInfo
    static
    INT32 QCALLTYPE SetParamInfo(QCall::ModuleHandle pModule, UINT32 tkMethod, UINT32 iSequence, UINT32 iAttributes, LPCWSTR wszParamName);

#ifndef FEATURE_CORECLR
    // functions to set FieldMarshal
    static
    void QCALLTYPE SetFieldMarshal(QCall::ModuleHandle pModule, UINT32 tk, LPCBYTE pMarshal, INT32 cbMarshal);
#endif
    // functions to set default value
    static
    void QCALLTYPE SetConstantValue(QCall::ModuleHandle pModule, UINT32 tk, DWORD valueType, LPVOID pValue);

    // functions to add declarative security
    static 
    void QCALLTYPE AddDeclarativeSecurity(QCall::ModuleHandle pModule, INT32 tk, DWORD action, LPCBYTE pBlob, INT32 cbBlob);
};



//*********************************************************************
//
// This CSymMapToken class implemented the IMapToken. It is used in catching
// token remap information from Merge and send the notifcation to CeeFileGen
// and SymbolWriter
//
//*********************************************************************
class CSymMapToken : public IMapToken
{
public:
    STDMETHODIMP QueryInterface(REFIID riid, PVOID *pp);
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();
    STDMETHODIMP Map(mdToken tkImp, mdToken tkEmit);
    CSymMapToken(ISymUnmanagedWriter *pWriter, IMapToken *pMapToken);
    ~CSymMapToken();
private:
    LONG        m_cRef;
    ISymUnmanagedWriter *m_pWriter;
    IMapToken   *m_pMapToken;
};

#endif  // _COMDYNAMIC_H_   
