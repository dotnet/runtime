// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: COMtoCLRCall.h
//

//


#ifndef __COMTOCLRCALL_H__
#define __COMTOCLRCALL_H__

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "util.hpp"
#include "spinlock.h"

enum ComCallFlags
{
    enum_IsVirtual                  = 0x0001,   // If true the method is virtual on the managed side
    enum_IsFieldCall                = 0x0002,   // is field call
    enum_IsGetter                   = 0x0004,   // is field call getter
    enum_NativeInfoInitialized      = 0x0008,   // Has the native info been initialized
    enum_NativeR4Retval             = 0x0010,   // Native ret val is an R4
    enum_NativeR8Retval             = 0x0020,   // Native ret val is an R8
    enum_NativeHResultRetVal        = 0x0040,   // Native ret val is an HRESULT
    enum_NativeBoolRetVal           = 0x0080,   // Native ret val is 0 in the case of failure
    enum_NativeVoidRetVal           = 0x0100,   // Native ret val is void
    // unused                       = 0x0200,
    enum_HasMarshalError            = 0x0400,   // The signature is not marshalable and m_StackBytes is a guess
    enum_IsDelegateInvoke           = 0x0800,   // The method is an 'Invoke' on a delegate
    // unused                       = 0x1000,
    // unused                       = 0x2000,
    // unused                       = 0x4000,
    // unused                       = 0x8000,
    // unused                       = 0x10000
};


//=======================================================================
// class com call
//=======================================================================

#if !defined(DACCESS_COMPILE)
class ComCall
{
public:
    // Encapsulate a SpinLockHolder, so that clients of our lock don't have to know
    // the details of our implementation.
    class LockHolder : public SpinLockHolder
    {
    public:
        LockHolder()
            : SpinLockHolder(ComCall::s_plock)
        {
            WRAPPER_NO_CONTRACT;
        }
    };


    //---------------------------------------------------------
    // One-time init
    //---------------------------------------------------------
    static void Init();

    //
    static void PopulateComCallMethodDesc(ComCallMethodDesc *pCMD, DWORD *pdwStubFlags);

    // helper to create a generic stub for com calls
    static Stub* CreateGenericComCallStub(BOOL isFieldAccess);

    //---------------------------------------------------------
    // Either creates or retrieves from the cache, a stub to
    // invoke com to com+
    // Each call refcounts the returned stub.
    // This routines throws an exception rather than returning
    // NULL.
    //---------------------------------------------------------
    static PCODE GetComCallMethodStub(ComCallMethodDesc *pMD);

    // pCallMD is either interface or class method - the one returned by
    // code:ComCallMethodDesc.GetCallMethodDesc on the ComCallMethodDesc
    // that owns the stub; pFD is the target field
    static MethodDesc* GetILStubMethodDesc(MethodDesc *pCallMD, DWORD dwStubFlags);
    static MethodDesc* GetILStubMethodDesc(FieldDesc *pFD, DWORD dwStubFlags);

private:
    ComCall() {LIMITED_METHOD_CONTRACT;};     // prevent "new"'s on this class

    static SpinLock* s_plock;
};
#endif // DACCESS_COMPILE

//-----------------------------------------------------------------------
// Operations specific to ComCall methods. This is not a code:MethodDesc.
//-----------------------------------------------------------------------

class ComCallMethodDesc
{
    friend void InvokeStub(ComCallMethodDesc *pCMD, PCODE pManagedTarget, OBJECTREF orThis, ComMethodFrame *pFrame, Thread *pThread, UINT64* pRetValOut);

public:
    // init method
    void InitMethod(MethodDesc *pMD, MethodDesc *pInterfaceMD);

    // init field
    void InitField(FieldDesc* pField, BOOL isGetter);

    // is field call
    BOOL IsFieldCall()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_flags & enum_IsFieldCall);
    }

    BOOL IsMethodCall()
    {
        WRAPPER_NO_CONTRACT;
        return !IsFieldCall();
    }

    // is field getter
    BOOL IsFieldGetter()
    {
        CONTRACT (BOOL)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsFieldCall());
        }
        CONTRACT_END;

        RETURN (m_flags & enum_IsGetter);
    }

    // is a virtual method
    BOOL IsVirtual()
    {
        CONTRACT (BOOL)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsMethodCall());
        }
        CONTRACT_END;

        RETURN (m_flags & enum_IsVirtual);
    }

    BOOL IsNativeR4RetVal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_NativeR4Retval;
    }

    BOOL IsNativeR8RetVal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_NativeR8Retval;
    }

    BOOL IsNativeFloatingPointRetVal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & (enum_NativeR4Retval | enum_NativeR8Retval);
    }

    BOOL IsNativeHResultRetVal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_NativeHResultRetVal;
    }

    BOOL IsNativeBoolRetVal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_NativeBoolRetVal;
    }

    BOOL IsNativeVoidRetVal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_NativeVoidRetVal;
    }

    BOOL HasMarshalError()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_HasMarshalError;
    }

    BOOL IsDelegateInvoke()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_IsDelegateInvoke;
    }

    BOOL IsNativeInfoInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags & enum_NativeInfoInitialized;
    }

    DWORD GetFlags()
    {
        LIMITED_METHOD_CONTRACT;
        return m_flags;
    }

    // get method desc
    MethodDesc* GetMethodDesc()
    {
        CONTRACT (MethodDesc*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(!IsFieldCall());
            PRECONDITION(CheckPointer(m_pMD));
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pMD;
    }

    // get interface method desc
    MethodDesc* GetInterfaceMethodDesc()
    {
        CONTRACT (MethodDesc *)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(!IsFieldCall());
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            SUPPORTS_DAC;
        }
        CONTRACT_END;

        RETURN m_pInterfaceMD;
    }

    // get interface method desc if non-NULL, class method desc otherwise
    MethodDesc* GetCallMethodDesc()
    {
        WRAPPER_NO_CONTRACT;

        MethodDesc *pMD = GetInterfaceMethodDesc();
        if (pMD == NULL)
            pMD = GetMethodDesc();
        _ASSERTE(pMD != NULL);

        return pMD;
    }

    // get field desc
    FieldDesc* GetFieldDesc()
    {
        CONTRACT (FieldDesc*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsFieldCall());
            PRECONDITION(CheckPointer(m_pFD));
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pFD;
    }

    // get module
    Module* GetModule();

    PCODE *GetAddrOfILStubField()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_pILStub;
    }

    PCODE GetILStub()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pILStub;
    }

    // get slot number for the method
    unsigned GetSlot()
    {
        CONTRACT (unsigned)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsMethodCall());
            PRECONDITION(CheckPointer(m_pMD));
        }
        CONTRACT_END;

        RETURN m_pMD->GetSlot();
    }

    // get num stack bytes to pop
    UINT16 GetNumStackBytes()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(m_flags & enum_NativeInfoInitialized);
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        return m_StackBytes;
    }

    static DWORD GetOffsetOfReturnThunk()
    {
        LIMITED_METHOD_CONTRACT;
        return -COMMETHOD_PREPAD;
    }

    static DWORD GetOffsetOfMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        return ((DWORD) offsetof(class ComCallMethodDesc, m_pMD));
    }

    //get call sig
    PCCOR_SIGNATURE GetSig(DWORD *pcbSigSize = NULL)
    {
        CONTRACT (PCCOR_SIGNATURE)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(IsMethodCall());
            PRECONDITION(CheckPointer(m_pMD));
        }
        CONTRACT_END;

        PCCOR_SIGNATURE pSig;
        DWORD cbSigSize;

        m_pMD->GetSig(&pSig, &cbSigSize);

        if (pcbSigSize != NULL)
        {
            *pcbSigSize = cbSigSize;
        }

        RETURN pSig;
    }

    // Discard all the resources owned by this ComCallMethodDesc.
    void Destruct()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

#ifdef TARGET_X86
        if (m_pwStubStackSlotOffsets != NULL)
            delete [] m_pwStubStackSlotOffsets;
#endif // TARGET_X86
    }

    static void ReleaseComCallMethodDesc(ComCallMethodDesc *pCMD)
    {
        WRAPPER_NO_CONTRACT;
        pCMD->Destruct();
    }

    PCODE CreateCOMToCLRStub(DWORD dwStubFlags, MethodDesc **ppStubMD);
    void InitRuntimeNativeInfo(MethodDesc *pStubMD);

private:
    // Initialize the member's native type information (size of native stack, native retval flags, etc).
    void InitNativeInfo();

    // see ComCallFlags enum above
    DWORD   m_flags;
    union
    {
        struct
        {
            MethodDesc*    m_pMD;
            PTR_MethodDesc m_pInterfaceMD;
        };
        FieldDesc*  m_pFD;
    };

    PCODE m_pILStub;        // IL stub for COM to CLR call, invokes GetCallMethodDesc()

    // Platform specific data needed for efficient IL stub invocation:
#ifdef TARGET_X86
    union
    {
        struct
        {
            // Index of the stack slot that gets stuffed into EDX when calling the stub.
            UINT16  m_wSourceSlotEDX;

            // Number of stack slots expected by the IL stub.
            UINT16  m_wStubStackSlotCount;
        };
        // Combination of m_wSourceSlotEDX and m_wStubStackSlotCount for atomic updates.
        UINT32 m_dwSlotInfo;
    };

    // This is an array of m_wStubStackSlotCount numbers where each element is the offset
    // on the source stack where the particular stub stack slot should be copied from.
    UINT16  *m_pwStubStackSlotOffsets;
#endif // TARGET_X86

    // Number of stack bytes pushed by the unmanaged caller.
    UINT16  m_StackBytes;
};

typedef Holder<ComCallMethodDesc *, DoNothing<ComCallMethodDesc *>, ComCallMethodDesc::ReleaseComCallMethodDesc> ComCallMethodDescHolder;

#endif // __COMTOCLRCALL_H__
