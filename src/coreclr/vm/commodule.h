// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
////////////////////////////////////////////////////////////////////////////////



#ifndef _COMModule_H_
#define _COMModule_H_

#include "invokeutil.h"

class Module;

class COMModule
{
public:
    // GetTypes will return an array containing all of the types
    // that are defined within this Module.
    static FCDECL1(Object*, GetTypes,  ReflectModuleBaseObject* pModuleUNSAFE);

    static FCDECL1(Object*,     GetMethods,             ReflectModuleBaseObject* refThisUNSAFE);
};

// GetTypeRef
// This function will return the class token for the named element.
extern "C" mdTypeRef QCALLTYPE ModuleBuilder_GetTypeRef(QCall::ModuleHandle pModule,
                                LPCWSTR wszFullName,
                                QCall::ModuleHandle pRefedModule,
                                INT32 tkResolution);

// SetFieldRVAContent
// This function is used to set the FieldRVA with the content data
extern "C" void QCALLTYPE ModuleBuilder_SetFieldRVAContent(QCall::ModuleHandle pModule, INT32 tkField, LPCBYTE pContent, INT32 length);


//GetArrayMethodToken
extern "C" INT32 QCALLTYPE ModuleBuilder_GetArrayMethodToken(QCall::ModuleHandle pModule,
                                    INT32 tkTypeSpec,
                                    LPCWSTR wszMethodName,
                                    LPCBYTE pSignature,
                                    INT32 sigLength);

// GetMemberRefToken
// This function will return the MemberRef token
extern "C" INT32 QCALLTYPE ModuleBuilder_GetMemberRef(QCall::ModuleHandle pModule, QCall::ModuleHandle pRefedModule, INT32 tr, INT32 token);

// This function return a MemberRef token given a MethodInfo describing an array method
extern "C" INT32 QCALLTYPE ModuleBuilder_GetMemberRefOfMethodInfo(QCall::ModuleHandle pModule, INT32 tr, MethodDesc * method);


// GetMemberRefOfFieldInfo
// This function will return a memberRef token given a FieldInfo
extern "C" mdMemberRef QCALLTYPE ModuleBuilder_GetMemberRefOfFieldInfo(QCall::ModuleHandle pModule, mdTypeDef tr, QCall::TypeHandle th, mdFieldDef tkField);

// GetMemberRefFromSignature
// This function will return the MemberRef token given the signature from managed code
extern "C" INT32 QCALLTYPE ModuleBuilder_GetMemberRefFromSignature(QCall::ModuleHandle pModule,
                                            INT32 tr,
                                            LPCWSTR wszMemberName,
                                            LPCBYTE pSignature,
                                            INT32 sigLength);

// GetTokenFromTypeSpec
extern "C" mdTypeSpec QCALLTYPE ModuleBuilder_GetTokenFromTypeSpec(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength);

// GetType
// Given a class type, this method will look for that type
//  with in the module.
extern "C" void QCALLTYPE RuntimeModule_GetType(QCall::ModuleHandle pModule, LPCWSTR wszName, BOOL bThrowOnError, BOOL bIgnoreCase, QCall::ObjectHandleOnStack retType, QCall::ObjectHandleOnStack keepAlive);

// GetStringConstant
// If this is a dynamic module, this routine will define a new
//  string constant or return the token of an existing constant.
extern "C" mdString QCALLTYPE ModuleBuilder_GetStringConstant(QCall::ModuleHandle pModule, LPCWSTR pwzValue, INT32 iLength);

extern "C" void QCALLTYPE ModuleBuilder_SetModuleName(QCall::ModuleHandle pModule, LPCWSTR wszModuleName);

extern "C" void QCALLTYPE RuntimeModule_GetScopeName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString);

extern "C" void QCALLTYPE RuntimeModule_GetFullyQualifiedName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString);

extern "C" HINSTANCE QCALLTYPE MarshalNative_GetHINSTANCE(QCall::ModuleHandle pModule);

#endif
