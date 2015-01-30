//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Just the subset of functionality from the MscorlibBinder necessary for exceptions.
#ifndef _FRAMEWORKEXCEPTIONLOADER_H_
#define _FRAMEWORKEXCEPTIONLOADER_H_

#include "runtimeexceptionkind.h"

class MethodTable;

// For loading exception types that are not defined in mscorlib.dll
class FrameworkExceptionLoader
{
  public:
    //
    // Utilities for exceptions
    //

    static MethodTable *GetException(RuntimeExceptionKind kind);

    static void GetExceptionName(RuntimeExceptionKind kind, SString & exceptionName);
};

#endif // _FRAMEWORKEXCEPTIONLOADER_H_
