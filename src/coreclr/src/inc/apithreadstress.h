// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ---------------------------------------------------------------------------
// APIThreadStress.h  (API thread stresser)
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// This class provides a simple base to wrap "thread stress" logic around an API,
// which will (in thread stress mode) cause an API to "fork" onto many threads
// executing the same operation simulatenously.  This can help to expose race
// conditions.
//
// Usage:
//
// First, subtype APIThreadStress and override Invoke to implement the operation.
// You will likely need to add data members for the arguments.
//
// Next, inside the API, write code like this:
//
// void MyRoutine(int a1, void *a2)
// {
//      class stress : APIThreadStress
//      {
//          int a1;
//          void *a2;
//          stress(int a1, void *a2) : a1(a1), a2(a2) 
//               { DoThreadStress(); }
//          void Invoke() { MyRoutine(a1, a2); }
//      } ts (a1, a2);
//
//      // implementation
//
//      // perhaps we have a common sub-point in the routine where we want the threads to 
//      // queue up and race again
//
//      ts.SyncThreadStress();
//
//      // more implementation    
//  }
// ---------------------------------------------------------------------------


#ifndef _APITHREADSTRESS_H_
#define _APITHREADSTRESS_H_

#include "utilcode.h"

class APIThreadStress
{
 public:
    BOOL DoThreadStress() { return FALSE; }
    static void SyncThreadStress() { }
    static void SetThreadStressCount(int count) { }
};

#endif  // _APITHREADSTRESS_H_
