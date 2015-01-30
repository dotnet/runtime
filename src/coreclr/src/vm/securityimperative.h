//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 


//


#ifndef __ComSecurityRuntime_h__
#define __ComSecurityRuntime_h__

#include "common.h"

#include "object.h"
#include "util.hpp"

// Forward declarations to avoid pulling in too many headers.
class Frame;
enum StackWalkAction;

//-----------------------------------------------------------
// The SecurityRuntime implements all the native methods
// for the managed class System.Security.SecurityRuntime
//-----------------------------------------------------------
namespace SecurityRuntime
{
//public:
    // private helper for getting a security object
    FCDECL2(Object*, GetSecurityObjectForFrame, StackCrawlMark* stackMark, CLR_BOOL create);
//protected:
    void CheckBeforeAllocConsole(AppDomain* pDomain, Assembly* pAssembly);
};

#endif /* __ComSecurityRuntime_h__ */

