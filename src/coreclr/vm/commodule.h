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
    // IsTransient
    // Determine if a Module is transient
    static
    BOOL QCALLTYPE IsTransient(QCall::ModuleHandle pModule);

    // GetTypeRef
    // This function will return the class token for the named element.
    static
    mdTypeRef QCALLTYPE GetTypeRef(QCall::ModuleHandle pModule,
                                   LPCWSTR wszFullName,
                                   QCall::ModuleHandle pRefedModule,
                                   LPCWSTR wszRefedModuleFileName,
                                   INT32 tkResolution);

    // SetFieldRVAContent
    // This function is used to set the FieldRVA with the content data
    static
    void QCALLTYPE SetFieldRVAContent(QCall::ModuleHandle pModule, INT32 tkField, LPCBYTE pContent, INT32 length);


    //GetArrayMethodToken
    static
    INT32 QCALLTYPE GetArrayMethodToken(QCall::ModuleHandle pModule,
                                        INT32 tkTypeSpec,
                                        LPCWSTR wszMethodName,
                                        LPCBYTE pSignature,
                                        INT32 sigLength);

    // GetMemberRefToken
    // This function will return the MemberRef token
    static
    INT32 QCALLTYPE GetMemberRef(QCall::ModuleHandle pModule, QCall::ModuleHandle pRefedModule, INT32 tr, INT32 token);

    // This function return a MemberRef token given a MethodInfo describing a array method
    static
    INT32 QCALLTYPE GetMemberRefOfMethodInfo(QCall::ModuleHandle pModule, INT32 tr, MethodDesc * method);


    // GetMemberRefOfFieldInfo
    // This function will return a memberRef token given a FieldInfo
    static
    mdMemberRef QCALLTYPE GetMemberRefOfFieldInfo(QCall::ModuleHandle pModule, mdTypeDef tr, QCall::TypeHandle th, mdFieldDef tkField);

    // GetMemberRefFromSignature
    // This function will return the MemberRef token given the signature from managed code
    static
    INT32 QCALLTYPE GetMemberRefFromSignature(QCall::ModuleHandle pModule,
                                              INT32 tr,
                                              LPCWSTR wszMemberName,
                                              LPCBYTE pSignature,
                                              INT32 sigLength);

    // GetTokenFromTypeSpec
    static
    mdTypeSpec QCALLTYPE GetTokenFromTypeSpec(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength);

    // GetType
    // Given a class type, this method will look for that type
    //  with in the module.
    static
    void QCALLTYPE GetType(QCall::ModuleHandle pModule, LPCWSTR wszName, BOOL bThrowOnError, BOOL bIgnoreCase, QCall::ObjectHandleOnStack retType, QCall::ObjectHandleOnStack keepAlive);

    // Get class will return an array contain all of the classes
    //  that are defined within this Module.
    static FCDECL1(Object*, GetTypes,  ReflectModuleBaseObject* pModuleUNSAFE);

    // GetStringConstant
    // If this is a dynamic module, this routine will define a new
    //  string constant or return the token of an existing constant.
    static
    mdString QCALLTYPE GetStringConstant(QCall::ModuleHandle pModule, LPCWSTR pwzValue, INT32 iLength);


    static
    void QCALLTYPE SetModuleName(QCall::ModuleHandle pModule, LPCWSTR wszModuleName);

    static FCDECL1(FC_BOOL_RET, IsResource, ReflectModuleBaseObject* pModuleUNSAFE);

    static FCDECL1(Object*,     GetMethods,             ReflectModuleBaseObject* refThisUNSAFE);

    static
    void QCALLTYPE GetScopeName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString);

    static
    void QCALLTYPE GetFullyQualifiedName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString);

    static
    HINSTANCE QCALLTYPE GetHINSTANCE(QCall::ModuleHandle pModule);

    static void DefineTypeRefHelper(
        IMetaDataEmit       *pEmit,         // given emit scope
        mdTypeDef           td,             // given typedef in the emit scope
        mdTypeRef           *ptr);          // return typeref

};

class COMPunkSafeHandle
{
  public:
    static FCDECL0(void*, nGetDReleaseTarget);
};

#endif
