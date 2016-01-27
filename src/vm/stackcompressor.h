// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 


#ifndef __stackcompressor_h__
#define __stackcompressor_h__
#ifdef FEATURE_COMPRESSEDSTACK

#include "common.h"


#include "newcompressedstack.h"

#ifndef DACCESS_COMPILE

class StackCompressor
{
       
public:
    static DWORD StackCompressor::GetCSInnerAppDomainAssertCount(COMPRESSEDSTACKREF csRef);
    static DWORD StackCompressor::GetCSInnerAppDomainOverridesCount(COMPRESSEDSTACKREF csRef);
    static void* SetAppDomainStack(Thread* pThread, void* curr);
    static void RestoreAppDomainStack(Thread* pThread, void* appDomainStack);

    static void Destroy(void *stack);
    static OBJECTREF GetCompressedStack( StackCrawlMark* stackMark = NULL, BOOL fWalkStack = TRUE );
    
    
};
#endif // DACCESS_COMPILE
#endif // #ifdef FEATURE_COMPRESSEDSTACK

#endif // __stackcompressor_h__

