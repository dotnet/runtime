// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
////////////////////////////////////////////////////////////////////////////////
// COMDynamic.h
//  This module defines the native methods that are used for Dynamic IL generation

////////////////////////////////////////////////////////////////////////////////

#ifndef _COMDYNAMIC_H_
#define _COMDYNAMIC_H_

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
    void QCALLTYPE DefineCustomAttribute(QCall::ModuleHandle pModule, INT32 token, INT32 conTok, LPCBYTE pBlob, INT32 cbBlob);

    // functions to set ParamInfo
    static
    INT32 QCALLTYPE SetParamInfo(QCall::ModuleHandle pModule, UINT32 tkMethod, UINT32 iSequence, UINT32 iAttributes, LPCWSTR wszParamName);

    // functions to set default value
    static
    void QCALLTYPE SetConstantValue(QCall::ModuleHandle pModule, UINT32 tk, DWORD valueType, LPVOID pValue);
};

#endif  // _COMDYNAMIC_H_
