//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// 

#ifndef __unwinder_h__
#define __unwinder_h__


//---------------------------------------------------------------------------------------
//
// OOPStackUnwinder is the abstract base class for unwinding stack frames.  Each of the two 64-bit platforms 
// has its own derived class.  Although the name of this class and its derived classes have changed, they
// are actually borrowed from dbghelp.dll.  (StackWalk64() is built on top of these classes.)  We have ripped
// out everything we don't need such as symbol lookup and various state, and keep just enough code to support 
// VirtualUnwind().  The managed debugging infrastructure can't call RtlVirtualUnwind() because it doesn't 
// work from out-of-processr
//
// Notes:
//    To see what we have changed in the borrowed source, you can diff the original version and our version.
//    For example, on X64, you can diff clr\src\Debug\daccess\amd64\dbs_stack_x64.cpp (the original) and 
//    clr\src\Debug\daccess\amd64\unwinder_amd64.cpp.
//

class OOPStackUnwinder
{
public:
    // Unwind the given CONTEXT to the caller CONTEXT.  The CONTEXT will be overwritten.  
    // Derived classes are expected to override this function for each processor architecture.
    virtual BOOL Unwind(T_CONTEXT * pContext) = 0;
                                  
protected:
#if !defined(_TARGET_ARM_) && !defined(_TARGET_ARM64_)
    // the unwind info on ARM is significantly different than AMD64, as such we don't need this function.
    // Read an UNWIND_INFO structure given its address.  The UNWIND_INFO structure is variable sized.
    virtual UNWIND_INFO * GetUnwindInfo(TADDR taUnwindInfo);
#endif // !_TARGET_ARM_ && !_TARGET_ARM64_

    // This is a simple wrapper over ReadMemory().  Unlike ReadMemory(), it fails if we don't successfully 
    // read all the specified bytes.
    virtual HRESULT ReadAllMemory(                       DWORD64 address,
                                  __in_ecount(cbRequest) PVOID   pbBuffer,
                                                         DWORD   cbRequest);

    // Read the specified memory.
    virtual HRESULT ReadMemory(                       DWORD64 address,
                               __in_ecount(cbRequest) PVOID   pbBuffer,
                                                      DWORD   cbRequest,
                               __out_opt              PDWORD  pcbDone);

    // Given a control PC, return the base of the module it is in.  For jitted managed code, this is the 
    // start of the code heap.
    virtual HRESULT GetModuleBase(      DWORD64  address,
                                  __out PDWORD64 pdwBase);

    // Given a control PC, return the function entry of the functoin it is in.
    virtual HRESULT GetFunctionEntry(                       DWORD64 address,
                                     __out_ecount(cbBuffer) PVOID   pBuffer,
                                                            DWORD   cbBuffer);
};

#endif // __unwinder_h__
