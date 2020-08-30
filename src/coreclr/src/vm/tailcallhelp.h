// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef TAILCALL_HELP_H
#define TAILCALL_HELP_H

#include "fcall.h"

struct TailCallInfo;
struct ArgBufferValue;
struct ArgBufferLayout;

class TailCallHelp
{
public:
    static FCDECL2(void*, AllocTailCallArgBuffer, INT32, void*);
    static FCDECL2(void*, GetTailCallInfo, void**, void**);

    static void CreateTailCallHelperStubs(
        MethodDesc* pCallerMD, MethodDesc* pCalleeMD,
        MetaSig& callSiteSig, bool virt, bool thisArgByRef, bool hasInstArg,
        MethodDesc** storeArgsStub, bool* storeArgsNeedsTarget,
        MethodDesc** callTargetStub);

    static MethodDesc* GetOrLoadTailCallDispatcherMD();
    static MethodDesc* GetTailCallDispatcherMD();
private:

    static void LayOutArgBuffer(
        MetaSig& callSiteSig, MethodDesc* calleeMD,
        bool storeTarget, bool thisArgByRef, bool hasInstArg, ArgBufferLayout* layout);
    static TypeHandle NormalizeSigType(TypeHandle tyHnd);
    static bool GenerateGCDescriptor(MethodDesc* pTargetMD, const ArgBufferLayout& values, GCRefMapBuilder* builder);

    static MethodDesc* CreateStoreArgsStub(TailCallInfo& info);
    static void CreateStoreArgsStubSig(const TailCallInfo& info, SigBuilder* sig);

    static MethodDesc* CreateCallTargetStub(const TailCallInfo& info);
    static void CreateCallTargetStubSig(const TailCallInfo& info, SigBuilder* sig);

    static void EmitLoadTyHnd(ILCodeStream* stream, TypeHandle tyHnd);
    static void EmitStoreTyHnd(ILCodeStream* stream, TypeHandle tyHnd);

    static void AppendTypeHandle(SigBuilder& builder, TypeHandle th);

    static PCCOR_SIGNATURE AllocateSignature(LoaderAllocator* alloc, SigBuilder& sig, DWORD* sigLen);
    static void* AllocateBlob(LoaderAllocator* alloc, const void* blob, size_t blobLen);
};

#endif
