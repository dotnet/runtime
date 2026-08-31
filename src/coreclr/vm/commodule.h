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
    FCDECL1(static Object*,     GetMethods,             ReflectModuleBaseObject* refThisUNSAFE);
};

// GetTypeRef
// This function will return the class token for the named element.
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetTypeRef(QCall::ModuleHandle pModule,
                                LPCWSTR wszFullName,
                                QCall::ModuleHandle pRefedModule,
                                INT32 tkResolution,
                                mdTypeRef* pReturnValue);

// SetFieldRVAContent
// This function is used to set the FieldRVA with the content data
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_SetFieldRVAContent(QCall::ModuleHandle pModule, INT32 tkField, LPCBYTE pContent, INT32 length);


//GetArrayMethodToken
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetArrayMethodToken(QCall::ModuleHandle pModule,
                                    INT32 tkTypeSpec,
                                    LPCWSTR wszMethodName,
                                    LPCBYTE pSignature,
                                    INT32 sigLength,
                                    INT32* pReturnValue);

// GetMemberRefToken
// This function will return the MemberRef token
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetMemberRef(QCall::ModuleHandle pModule, QCall::ModuleHandle pRefedModule, INT32 tr, INT32 token, INT32* pReturnValue);

// This function return a MemberRef token given a MethodInfo describing an array method
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetMemberRefOfMethodInfo(QCall::ModuleHandle pModule, INT32 tr, MethodDesc * method, INT32* pReturnValue);


// GetMemberRefOfFieldInfo
// This function will return a memberRef token given a FieldInfo
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetMemberRefOfFieldInfo(QCall::ModuleHandle pModule, mdTypeDef tr, QCall::TypeHandle th, mdFieldDef tkField, mdMemberRef* pReturnValue);

// GetMemberRefFromSignature
// This function will return the MemberRef token given the signature from managed code
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetMemberRefFromSignature(QCall::ModuleHandle pModule,
                                            INT32 tr,
                                            LPCWSTR wszMemberName,
                                            LPCBYTE pSignature,
                                            INT32 sigLength,
                                            INT32* pReturnValue);

// GetTokenFromTypeSpec
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetTokenFromTypeSpec(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength, mdTypeSpec* pReturnValue);

// GetStringConstant
// If this is a dynamic module, this routine will define a new
//  string constant or return the token of an existing constant.
extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_GetStringConstant(QCall::ModuleHandle pModule, LPCWSTR pwzValue, INT32 iLength, mdString* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE ModuleBuilder_SetModuleName(QCall::ModuleHandle pModule, LPCWSTR wszModuleName);

extern "C" QCallExceptionStatus QCALLTYPE RuntimeModule_GetScopeName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString);

extern "C" QCallExceptionStatus QCALLTYPE RuntimeModule_GetFullyQualifiedName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString);

// GetTypes will return an array containing all of the types that are defined within this Module.
extern "C" QCallExceptionStatus QCALLTYPE RuntimeModule_GetTypes(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retTypes, QCall::ObjectHandleOnStack retExceptions);

extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetHINSTANCE(QCall::ModuleHandle pModule, HINSTANCE* pReturnValue);

#endif
