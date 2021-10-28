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
// These methods implement the dynamic IL creation process
//  inside reflection.
// This class exists as a container for methods that need friend access to other types.
class COMDynamicWrite
{
    public:
        static INT32 DefineType(Module* pModule,
                                LPCWSTR wszFullName,
                                INT32 tkParent,
                                INT32 attributes,
                                INT32 tkEnclosingType,
                                INT32 * pInterfaceTokens);
        static void TermCreateClass(Module* pModule, INT32 tk, QCall::ObjectHandleOnStack retType);
};


// This function will create the class's metadata definition

extern "C" INT32 QCALLTYPE COMDynamicWrite_DefineType(QCall::ModuleHandle pModule,
                            LPCWSTR wszFullName,
                            INT32 tkParent,
                            INT32 attributes,
                            INT32 tkEnclosingType,
                            INT32 * pInterfaceTokens);


extern "C" INT32 QCALLTYPE COMDynamicWrite_DefineGenericParam(QCall::ModuleHandle pModule,
                                    LPCWSTR wszFullName,
                                    INT32 tkParent,
                                    INT32 attributes,
                                    INT32 position,
                                    INT32 * pConstraintTokens);

// This function will reset the parent class in metadata

extern "C" void QCALLTYPE COMDynamicWrite_SetParentType(QCall::ModuleHandle pModule, INT32 tdType, INT32 tkParent);

// This function will add another interface impl

extern "C" void QCALLTYPE COMDynamicWrite_AddInterfaceImpl(QCall::ModuleHandle pModule, INT32 tdType, INT32 tkInterface);

// This function will create a method within the class

extern "C" INT32 QCALLTYPE COMDynamicWrite_DefineMethod(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, LPCBYTE pSignature, INT32 sigLength, INT32 attributes);


extern "C" INT32 QCALLTYPE COMDynamicWrite_DefineMethodSpec(QCall::ModuleHandle pModule, INT32 tkParent, LPCBYTE pSignature, INT32 sigLength);

// This function will create a method within the class

extern "C" void QCALLTYPE COMDynamicWrite_SetMethodIL(QCall::ModuleHandle pModule,
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


extern "C" void QCALLTYPE COMDynamicWrite_TermCreateClass(QCall::ModuleHandle pModule, INT32 tk, QCall::ObjectHandleOnStack retType);


extern "C" mdFieldDef QCALLTYPE COMDynamicWrite_DefineField(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, LPCBYTE pSignature, INT32 sigLength, INT32 attr);


extern "C" void QCALLTYPE COMDynamicWrite_SetPInvokeData(QCall::ModuleHandle pModule, LPCWSTR wszDllName, LPCWSTR wszFunctionName, INT32 token, INT32 linkFlags);


extern "C" INT32 QCALLTYPE COMDynamicWrite_DefineProperty(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, INT32 attr, LPCBYTE pSignature, INT32 sigLength);


extern "C" INT32 QCALLTYPE COMDynamicWrite_DefineEvent(QCall::ModuleHandle pModule, INT32 tkParent, LPCWSTR wszName, INT32 attr, INT32 tkEventType);

// functions to set Setter, Getter, Reset, TestDefault, and other methods

extern "C" void QCALLTYPE COMDynamicWrite_DefineMethodSemantics(QCall::ModuleHandle pModule, INT32 tkAssociation, INT32 attr, INT32 tkMethod);

// functions to set method's implementation flag

extern "C" void QCALLTYPE COMDynamicWrite_SetMethodImpl(QCall::ModuleHandle pModule, INT32 tkMethod, INT32 attr);

// functions to create MethodImpl record

extern "C" void QCALLTYPE COMDynamicWrite_DefineMethodImpl(QCall::ModuleHandle pModule, UINT32 tkType, UINT32 tkBody, UINT32 tkDecl);

// GetTokenFromSig's argument

extern "C" INT32 QCALLTYPE COMDynamicWrite_GetTokenFromSig(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength);

// Set Field offset

extern "C" void QCALLTYPE COMDynamicWrite_SetFieldLayoutOffset(QCall::ModuleHandle pModule, INT32 tkField, INT32 iOffset);

// Set classlayout info

extern "C" void QCALLTYPE COMDynamicWrite_SetClassLayout(QCall::ModuleHandle pModule, INT32 tk, INT32 iPackSize, UINT32 iTotalSize);

// Set a custom attribute

extern "C" void QCALLTYPE COMDynamicWrite_DefineCustomAttribute(QCall::ModuleHandle pModule, INT32 token, INT32 conTok, LPCBYTE pBlob, INT32 cbBlob);

// functions to set ParamInfo

extern "C" INT32 QCALLTYPE COMDynamicWrite_SetParamInfo(QCall::ModuleHandle pModule, UINT32 tkMethod, UINT32 iSequence, UINT32 iAttributes, LPCWSTR wszParamName);

// functions to set default value

extern "C" void QCALLTYPE COMDynamicWrite_SetConstantValue(QCall::ModuleHandle pModule, UINT32 tk, DWORD valueType, LPVOID pValue);

#endif  // _COMDYNAMIC_H_
