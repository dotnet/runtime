//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

#ifdef STRESS_THREAD

class APIThreadStress
{
 public:
    APIThreadStress();
    ~APIThreadStress();

    BOOL DoThreadStress();
    static void SyncThreadStress();

    static void SetThreadStressCount(int count);

 protected:
    virtual void Invoke() {LIMITED_METHOD_CONTRACT;};

 private:
    static DWORD WINAPI StartThread(void *arg);

    static int s_threadStressCount;     

    int       m_threadCount;
    HANDLE    *m_hThreadArray;
    BOOL      m_setupOK;
    LONG      m_runCount;
    HANDLE    m_syncEvent;

};

#else // STRESS_THREAD

class APIThreadStress
{
 public:
    BOOL DoThreadStress() { return FALSE; }
    static void SyncThreadStress() { }
    static void SetThreadStressCount(int count) { }
};

#endif // STRESS_THREAD

#endif  // _APITHREADSTRESS_H_
