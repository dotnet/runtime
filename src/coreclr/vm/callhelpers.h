// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*============================================================
**
** File:    callhelpers.h
** Purpose: Provides helpers for making managed calls
**

===========================================================*/
#ifndef __CALLHELPERS_H__
#define __CALLHELPERS_H__

struct CallDescrData
{
    //
    // Input arguments
    //
    LPVOID                      pSrc;
    UINT32                      numStackSlots;
#ifdef CALLDESCR_ARGREGS
    const ArgumentRegisters *   pArgumentRegisters;
#endif
#ifdef CALLDESCR_FPARGREGS
    const FloatArgumentRegisters * pFloatArgumentRegisters;
#endif
#ifdef CALLDESCR_REGTYPEMAP
    UINT64                      dwRegTypeMap;
#endif
    UINT32                      fpReturnSize;
    PCODE                       pTarget;

#ifdef CALLDESCR_RETBUFFARGREG
    // Pointer to return buffer arg location
    UINT64*                     pRetBuffArg;
#endif

    //
    // Return value
    //
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
#ifdef TARGET_ARM64
    // Use NEON128 to ensure proper alignment for vectors.
    DECLSPEC_ALIGN(16) NEON128 returnValue[ENREGISTERED_RETURNTYPE_MAXSIZE / sizeof(NEON128)];
#else
    // Use UINT64 to ensure proper alignment
    UINT64 returnValue[ENREGISTERED_RETURNTYPE_MAXSIZE / sizeof(UINT64)];
#endif
#else
    UINT64 returnValue;
#endif
};

#define NUMBER_RETURNVALUE_SLOTS (ENREGISTERED_RETURNTYPE_MAXSIZE / sizeof(ARG_SLOT))

#if !defined(DACCESS_COMPILE)

extern "C" void STDCALL CallDescrWorkerInternal(CallDescrData * pCallDescrData);

#if !defined(HOST_64BIT) && defined(_DEBUG)
void CallDescrWorker(CallDescrData * pCallDescrData);
#else
#define CallDescrWorker(pCallDescrData) CallDescrWorkerInternal(pCallDescrData)
#endif

void CallDescrWorkerWithHandler(
                CallDescrData *   pCallDescrData,
                BOOL              fCriticalCall = FALSE);

// Helper for VM->managed calls with simple signatures.
void * DispatchCallSimple(
                    SIZE_T *pSrc,
                    DWORD numStackSlotsToCopy,
                    PCODE pTargetAddress,
                    DWORD dwDispatchCallSimpleFlags);

bool IsCerRootMethod(MethodDesc *pMD);

class MethodDescCallSite
{
private:
    MethodDesc* m_pMD;
    PCODE       m_pCallTarget;
    MetaSig     m_methodSig;
    ArgIterator m_argIt;

#ifdef _DEBUG
    NOINLINE void LogWeakAssert()
    {
        LIMITED_METHOD_CONTRACT;
        LOG((LF_ASSERT, LL_WARNING, "%s::%s\n", m_pMD->m_pszDebugClassName, m_pMD->m_pszDebugMethodName));
    }
#endif // _DEBUG

    void DefaultInit(OBJECTREF* porProtectedThis)
    {
        CONTRACTL
        {
            MODE_ANY;
            GC_TRIGGERS;
            THROWS;
        }
        CONTRACTL_END;

#ifdef _DEBUG
        //
        // Make sure we are passing in a 'this' if and only if it is required
        //
        if (m_pMD->IsVtableMethod())
        {
            CONSISTENCY_CHECK_MSG(NULL != porProtectedThis, "You did not pass in the 'this' object for a vtable method");
        }
        else
        {
            if (NULL != porProtectedThis)
            {
                if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AssertOnUnneededThis))
                {
                    CONSISTENCY_CHECK_MSG(NULL == porProtectedThis, "You passed in a 'this' object to a non-vtable method.");
                }
                else
                {
                    LogWeakAssert();
                }

            }
        }
#endif // _DEBUG

        m_pCallTarget = m_pMD->GetCallTarget(porProtectedThis);

        m_argIt.ForceSigWalk();
    }

    void DefaultInit(TypeHandle th)
    {
        CONTRACTL
        {
            MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        }
        CONTRACTL_END;

        m_pCallTarget = m_pMD->GetCallTarget(NULL, th);

        m_argIt.ForceSigWalk();
}

#ifdef FEATURE_INTERPRETER
public:
    void CallTargetWorker(const ARG_SLOT *pArguments, ARG_SLOT *pReturnValue, int cbReturnValue, bool transitionToPreemptive = false);
#else
    void CallTargetWorker(const ARG_SLOT *pArguments, ARG_SLOT *pReturnValue, int cbReturnValue);
#endif

public:
    // Used to avoid touching metadata for CoreLib methods.
    // instance methods must pass in the 'this' object
    // static methods must pass null
    MethodDescCallSite(BinderMethodID id, OBJECTREF* porProtectedThis = NULL) :
        m_pMD(
            CoreLibBinder::GetMethod(id)
            ),
        m_methodSig(id),
        m_argIt(&m_methodSig)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        DefaultInit(porProtectedThis);
    }

    // Used to avoid touching metadata for CoreLib methods.
    // instance methods must pass in the 'this' object
    // static methods must pass null
    MethodDescCallSite(BinderMethodID id, OBJECTHANDLE hThis) :
        m_pMD(
            CoreLibBinder::GetMethod(id)
            ),
        m_methodSig(id),
        m_argIt(&m_methodSig)
    {
        WRAPPER_NO_CONTRACT;

        DefaultInit((OBJECTREF*)hThis);
    }

    // instance methods must pass in the 'this' object
    // static methods must pass null
    MethodDescCallSite(MethodDesc* pMD, OBJECTREF* porProtectedThis = NULL) :
        m_pMD(pMD),
        m_methodSig(pMD),
        m_argIt(&m_methodSig)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        if (porProtectedThis == NULL)
        {
            // We don't have a "this" pointer - ensure that we have activated the containing module
            m_pMD->EnsureActive();
        }

        DefaultInit(porProtectedThis);
    }

    // instance methods must pass in the 'this' object
    // static methods must pass null
    MethodDescCallSite(MethodDesc* pMD, OBJECTHANDLE hThis) :
        m_pMD(pMD),
        m_methodSig(pMD),
        m_argIt(&m_methodSig)
    {
        WRAPPER_NO_CONTRACT;

        if (hThis == NULL)
        {
            // We don't have a "this" pointer - ensure that we have activated the containing module
            m_pMD->EnsureActive();
        }

        DefaultInit((OBJECTREF*)hThis);
    }

    // instance methods must pass in the 'this' object
    // static methods must pass null
    MethodDescCallSite(MethodDesc* pMD, LPHARDCODEDMETASIG pwzSignature, OBJECTREF* porProtectedThis = NULL) :
        m_pMD(pMD),
        m_methodSig(pwzSignature),
        m_argIt(&m_methodSig)
    {
        WRAPPER_NO_CONTRACT;

        if (porProtectedThis == NULL)
        {
            // We don't have a "this" pointer - ensure that we have activated the containing module
            m_pMD->EnsureActive();
        }

        DefaultInit(porProtectedThis);
    }

    MethodDescCallSite(MethodDesc* pMD, TypeHandle th) :
        m_pMD(pMD),
        m_methodSig(pMD, th),
        m_argIt(&m_methodSig)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        // We don't have a "this" pointer - ensure that we have activated the containing module
        m_pMD->EnsureActive();

        DefaultInit(th);
    }

    //
    // Only use this constructor if you're certain you know where
    // you're going and it cannot be affected by generics/virtual
    // dispatch/etc..
    //
    MethodDescCallSite(MethodDesc* pMD, PCODE pCallTarget) :
        m_pMD(pMD),
        m_pCallTarget(pCallTarget),
        m_methodSig(pMD),
        m_argIt(&m_methodSig)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_pMD->EnsureActive();

        m_argIt.ForceSigWalk();
    }

#ifdef FEATURE_INTERPRETER
    MethodDescCallSite(MethodDesc* pMD, MetaSig* pSig, PCODE pCallTarget) :
        m_pMD(pMD),
        m_pCallTarget(pCallTarget),
        m_methodSig(*pSig),
        m_argIt(pSig)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_pMD->EnsureActive();

        m_argIt.ForceSigWalk();
    }
#endif // FEATURE_INTERPRETER

    MetaSig* GetMetaSig()
    {
        return &m_methodSig;
    }

    //
    // Call_RetXXX definition macros:
    //
    // These macros provide type protection for the return value from calls to managed
    // code. This should help to prevent errors like what we're seeing on 64bit where
    // the JIT64 is returning the BOOL as 1byte with the rest of the ARG_SLOT still
    // polluted by the remnants of its last value. Previously we would cast to a (BOOL)
    // and end up having if((BOOL)pMD->Call(...)) statements always being true.
    //

    // Use OTHER_ELEMENT_TYPE when defining CallXXX_RetXXX variations where the return type
    // is not in CorElementType (like LPVOID) or the return type can be one of a number of
    // CorElementTypes, like XXX_RetObjPtr which is used for all kinds of Object* return
    // types, or XXX_RetArgSlot which is unspecified.
#define OTHER_ELEMENT_TYPE -1

// Note "permitvaluetypes" is not really used for anything
#define MDCALLDEF(wrappedmethod, permitvaluetypes, ext, rettype, eltype)            \
        FORCEINLINE rettype wrappedmethod##ext (const ARG_SLOT* pArguments)         \
        {                                                                           \
            WRAPPER_NO_CONTRACT;                                                    \
            {                                                                       \
                GCX_FORBID();  /* arg array is not protected */                     \
                CONSISTENCY_CHECK(eltype == OTHER_ELEMENT_TYPE ||                   \
                                  eltype == m_methodSig.GetReturnType());           \
            }                                                                       \
            ARG_SLOT retval;                                                        \
            CallTargetWorker(pArguments, &retval, sizeof(retval));                  \
            return *(rettype *)ArgSlotEndiannessFixup(&retval, sizeof(rettype));     \
        }

#define MDCALLDEF_ARGSLOT(wrappedmethod, ext)                                       \
        FORCEINLINE void wrappedmethod##ext (const ARG_SLOT* pArguments, ARG_SLOT *pReturnValue, int cbReturnValue) \
        {                                                                           \
            CallTargetWorker(pArguments, pReturnValue, cbReturnValue);              \
            /* Bigendian layout not support */                                      \
        }

#define MDCALLDEF_REFTYPE(wrappedmethod,  permitvaluetypes, ext, ptrtype, reftype)              \
        FORCEINLINE reftype wrappedmethod##ext (const ARG_SLOT* pArguments)                     \
        {                                                                                       \
            ARG_SLOT retval;                                                                    \
            CallTargetWorker(pArguments, &retval, sizeof(retval));                              \
            return ObjectTo##reftype(*(ptrtype *)                                               \
                        ArgSlotEndiannessFixup(&retval, sizeof(ptrtype)));                       \
        }


    // The MDCALLDEF_XXX_VOID macros take a customized assertion and calls the worker without
    // returning a value, this is the macro that _should_ be used to define the CallXXX variations
    // (without _RetXXX extension) so that misuse will be caught at compile time.

#define MDCALLDEF_VOID(wrappedmethod, permitvaluetypes)                 \
        FORCEINLINE void wrappedmethod (const ARG_SLOT* pArguments)     \
        {                                                               \
            WRAPPER_NO_CONTRACT;                                        \
            CallTargetWorker(pArguments, NULL, 0);                      \
        }

#define MDCALLDEFF_STD_RETTYPES(wrappedmethod,permitvaluetypes)                                         \
        MDCALLDEF_VOID(wrappedmethod,permitvaluetypes)                                                  \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetBool,   CLR_BOOL,      ELEMENT_TYPE_BOOLEAN)  \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetChar,   CLR_CHAR,      ELEMENT_TYPE_CHAR)     \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetI1,     CLR_I1,        ELEMENT_TYPE_I1)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetU1,     CLR_U1,        ELEMENT_TYPE_U1)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetI2,     CLR_I2,        ELEMENT_TYPE_I2)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetU2,     CLR_U2,        ELEMENT_TYPE_U2)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetI4,     CLR_I4,        ELEMENT_TYPE_I4)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetU4,     CLR_U4,        ELEMENT_TYPE_U4)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetI8,     CLR_I8,        ELEMENT_TYPE_I8)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetU8,     CLR_U8,        ELEMENT_TYPE_U8)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetR4,     CLR_R4,        ELEMENT_TYPE_R4)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetR8,     CLR_R8,        ELEMENT_TYPE_R8)       \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetI,      CLR_I,         ELEMENT_TYPE_I)        \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetU,      CLR_U,         ELEMENT_TYPE_U)        \
        MDCALLDEF(wrappedmethod,permitvaluetypes,     _RetArgSlot,ARG_SLOT,      OTHER_ELEMENT_TYPE)


    public:
        //--------------------------------------------------------------------
        // Invoke a method. Arguments are packaged up in right->left order
        // which each array element corresponding to one argument.
        //
        // Can throw a COM+ exception.
        //
        // All the appropriate "virtual" semantics (include thunking like context
        // proxies) occurs inside Call.
        //
        // Call should never be called on interface MethodDesc's. The exception
        // to this rule is when calling on a COM object. In that case the call
        // needs to go through an interface MD and CallOnInterface is there
        // for that.
        //--------------------------------------------------------------------

        //
        // NOTE on Call methods
        //  MethodDesc::Call uses a virtual portable calling convention
        //  Arguments are put left-to-right in the ARG_SLOT array, in the following order:
        //    - this pointer (if any)
        //    - return buffer address (if signature.HasRetBuffArg())
        //    - all other fixed arguments (left-to-right)
        //  Vararg is not supported yet.
        //
        //  The args that fit in an ARG_SLOT are inline. The ones that don't fit in an ARG_SLOT are allocated somewhere else
        //      (usually on the stack) and a pointer to that area is put in the corresponding ARG_SLOT
        // ARG_SLOT is guaranteed to be big enough to fit all basic types and pointer types. Basically, one has
        //      to check only for aggregate value-types and 80-bit floating point values or greater.
        //
        // Calls with value type parameters must use the CallXXXWithValueTypes
        // variants.  Using the WithValueTypes variant indicates that the caller
        // has gc-protected the contents of value types of size greater than
        // ENREGISTERED_PARAMTYPE_MAXSIZE (when it is defined, which is currently
        // only on AMD64).  ProtectValueClassFrame can be used to accomplish this,
        // see CallDescrWithObjectArray in stackbuildersink.cpp.
        //
        // Not all usages of MethodDesc::CallXXX have been ported to the new convention. The end goal is to port them all and get
        //      rid of the non-portable BYTE* version.
        //
        // We have converted all usage of CallXXX in the runtime to some more specific CallXXX_RetXXX type (CallXXX usages
        // where the return value is unused remain CallXXX). In most cases we were able to use something more specific than
        // CallXXX_RetArgSlot (which is the equivalent of the old behavior). It is recommended that as you add usages of
        // CallXXX in the future you try to avoid CallXXX_RetArgSlot whenever possible.
        //
        // If the return value is unused you can use the CallXXX syntax which has a void return and is not protected
        // by any assertions around the return value type. This should protect against people trying to use the old
        // semantics of ->Call as if they try to assign the return value to something they'll get a compile time error.
        //
        // If you are unable to be sure of the return type at runtime and are just blindly casting then continue to use
        // CallXXX_RetArgSlot, Do not for instance use CallXXX_RetI4 as a mechanism to cast the result to an I4 as it will
        // also try to assert the fact that the callee managed method actually does return an I4.
        //

        // All forms of CallXXX should have at least the CallXXX_RetArgSlot definition which maps to the old behavior
        //  -  MDCALL_ARG_____STD_RETTYPES includes CallXXX_RetArgSlot
        //  -  MDCALL_ARG_SIG_STD_RETTYPES includes CallXXX_RetArgSlot

        // XXX Call_RetXXX(const ARG_SLOT* pArguments);
        MDCALLDEFF_STD_RETTYPES(Call, FALSE)
        MDCALLDEF(              Call, FALSE, _RetHR,        HRESULT,       OTHER_ELEMENT_TYPE)
        MDCALLDEF(              Call, FALSE, _RetObjPtr,    Object*,       OTHER_ELEMENT_TYPE)
        MDCALLDEF_REFTYPE(      Call, FALSE, _RetOBJECTREF, Object*,       OBJECTREF)
        MDCALLDEF_REFTYPE(      Call, FALSE, _RetSTRINGREF, StringObject*, STRINGREF)
        MDCALLDEF(              Call, FALSE, _RetLPVOID,    LPVOID,        OTHER_ELEMENT_TYPE)

        // XXX CallWithValueTypes_RetXXX(const ARG_SLOT* pArguments);
        MDCALLDEF_VOID(     CallWithValueTypes, TRUE)
        MDCALLDEF_ARGSLOT(  CallWithValueTypes, _RetArgSlot)
        MDCALLDEF_REFTYPE(  CallWithValueTypes, TRUE,   _RetOBJECTREF,  Object*,    OBJECTREF)
        MDCALLDEF(          CallWithValueTypes, TRUE,   _RetOleColor,   OLE_COLOR,  OTHER_ELEMENT_TYPE)
#undef OTHER_ELEMENT_TYPE
#undef MDCALL_ARG_SIG_STD_RETTYPES
#undef MDCALLDEF
#undef MDCALLDEF_REFTYPE
#undef MDCALLDEF_VOID
}; // MethodDescCallSite


#ifdef CALLDESCR_REGTYPEMAP
void FillInRegTypeMap(int argOffset, CorElementType typ, BYTE * pMap);
#endif // CALLDESCR_REGTYPEMAP


/***********************************************************************/
/* Macros used to indicate a call to managed code is starting/ending   */
/***********************************************************************/

#ifdef TARGET_UNIX
// Install a native exception holder that doesn't catch any exceptions but its presence
// in a stack range of native frames indicates that there was a call from native to
// managed code. It is used by the DispatchManagedException to detect the case when
// the INSTALL_MANAGED_EXCEPTION_DISPATCHER was not at the managed to native boundary.
// For example in the PreStubWorker, which can be called from both native and managed
// code.
#define INSTALL_CALL_TO_MANAGED_EXCEPTION_HOLDER() \
    NativeExceptionHolderNoCatch __exceptionHolder;    \
    __exceptionHolder.Push();
#else // TARGET_UNIX
#define INSTALL_CALL_TO_MANAGED_EXCEPTION_HOLDER()
#endif // TARGET_UNIX

enum EEToManagedCallFlags
{
    EEToManagedDefault                  = 0x0000,
    EEToManagedCriticalCall             = 0x0001,
};

#define BEGIN_CALL_TO_MANAGED()                                                 \
    BEGIN_CALL_TO_MANAGEDEX(EEToManagedDefault)

#define BEGIN_CALL_TO_MANAGEDEX(flags)                                          \
{                                                                               \
    MAKE_CURRENT_THREAD_AVAILABLE();                                            \
    DECLARE_CPFH_EH_RECORD(CURRENT_THREAD);                                     \
    _ASSERTE(CURRENT_THREAD);                                                   \
    _ASSERTE((CURRENT_THREAD->m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);     \
    /* This bit should never be set when we call into managed code.  The */     \
    /* stack walking code explicitly clears this around any potential calls */  \
    /* into managed code. */                                                    \
    _ASSERTE(!IsStackWalkerThread());                                           \
    /* If this isn't a critical transition, we need to check to see if a */     \
    /* thread abort has been requested */                                       \
    if (!(flags & EEToManagedCriticalCall))                                     \
    {                                                                           \
        if (CURRENT_THREAD->IsAbortRequested()) {                               \
            CURRENT_THREAD->HandleThreadAbort();                                \
        }                                                                       \
    }                                                                           \
    INSTALL_CALL_TO_MANAGED_EXCEPTION_HOLDER();                                 \
    INSTALL_COMPLUS_EXCEPTION_HANDLER_NO_DECLARE();

#define END_CALL_TO_MANAGED()                                                   \
    UNINSTALL_COMPLUS_EXCEPTION_HANDLER();                                      \
}

/***********************************************************************/
/* Macros that provide abstraction to the usage of DispatchCallSimple    */
/***********************************************************************/

enum DispatchCallSimpleFlags
{
    DispatchCallSimple_CriticalCall                  = 0x0001,
    DispatchCallSimple_CatchHandlerFoundNotification = 0x0002,
};

#define ARGHOLDER_TYPE LPVOID
#define OBJECTREF_TO_ARGHOLDER(x) (LPVOID)OBJECTREFToObject(x)
#define STRINGREF_TO_ARGHOLDER(x) (LPVOID)STRINGREFToObject(x)
#define PTR_TO_ARGHOLDER(x) (LPVOID)x
#define DWORD_TO_ARGHOLDER(x)   (LPVOID)(SIZE_T)x
#define BOOL_TO_ARGHOLDER(x) DWORD_TO_ARGHOLDER(!!(x))

#define INIT_VARIABLES(count)                               \
        DWORD   __numArgs = count;                          \
        DWORD   __dwDispatchCallSimpleFlags = 0;            \

#define PREPARE_NONVIRTUAL_CALLSITE(id) \
        static PCODE s_pAddr##id = NULL;                    \
        PCODE __pSlot = VolatileLoad(&s_pAddr##id);         \
        if ( __pSlot == NULL )                              \
        {                                                   \
            MethodDesc *pMeth = CoreLibBinder::GetMethod(id);   \
            _ASSERTE(pMeth);                                \
            __pSlot = pMeth->GetMultiCallableAddrOfCode();  \
            VolatileStore(&s_pAddr##id, __pSlot);           \
        }

#define PREPARE_VIRTUAL_CALLSITE(id, objref)                \
        MethodDesc *__pMeth = CoreLibBinder::GetMethod(id);     \
        PCODE __pSlot = __pMeth->GetCallTarget(&objref);

#define PREPARE_VIRTUAL_CALLSITE_USING_METHODDESC(pMD, objref)                \
        PCODE __pSlot = pMD->GetCallTarget(&objref);

#ifdef _DEBUG
#define SIMPLE_VIRTUAL_METHOD_CHECK(slotNumber, methodTable)                     \
        {                                                                        \
            MethodDesc* __pMeth = methodTable->GetMethodDescForSlot(slotNumber); \
            _ASSERTE(__pMeth);                                                   \
            _ASSERTE(!__pMeth->HasMethodInstantiation() &&                       \
                     !__pMeth->GetMethodTable()->IsInterface());                 \
        }
#else
#define SIMPLE_VIRTUAL_METHOD_CHECK(slotNumber, objref)
#endif

// a simple virtual method is a non-interface/non-generic method
// Note: objref has to be protected!
#define PREPARE_SIMPLE_VIRTUAL_CALLSITE(id, objref)                              \
        static WORD s_slot##id = MethodTable::NO_SLOT;                           \
        WORD __slot = VolatileLoad(&s_slot##id);                                 \
        if (__slot == MethodTable::NO_SLOT)                                      \
        {                                                                        \
            MethodDesc *pMeth = CoreLibBinder::GetMethod(id);                        \
            _ASSERTE(pMeth);                                                     \
            __slot = pMeth->GetSlot();                                           \
            VolatileStore(&s_slot##id, __slot);                                  \
        }                                                                        \
        PREPARE_SIMPLE_VIRTUAL_CALLSITE_USING_SLOT(__slot, objref)               \

// a simple virtual method is a non-interface/non-generic method
#define PREPARE_SIMPLE_VIRTUAL_CALLSITE_USING_SLOT(slotNumber, objref)           \
        MethodTable* __pObjMT = (objref)->GetMethodTable();                      \
        SIMPLE_VIRTUAL_METHOD_CHECK(slotNumber, __pObjMT);                       \
        PCODE __pSlot = (PCODE) __pObjMT->GetRestoredSlot(slotNumber);

#define PREPARE_NONVIRTUAL_CALLSITE_USING_METHODDESC(pMD)   \
        PCODE __pSlot = (pMD)->GetSingleCallableAddrOfCode();

#define PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(pCode)       \
        PCODE __pSlot = pCode;

#define CRITICAL_CALLSITE                                   \
        __dwDispatchCallSimpleFlags |= DispatchCallSimple_CriticalCall;

// This flag should be used for callsites that catch exception up the stack inside the VM. The most common causes are
// such as END_DOMAIN_TRANSITION or EX_CATCH. Catching exceptions in the managed code is properly instrumented and
// does not need this notification.
//
// The notification is what enables both the managed 'unhandled exception' dialog and the 'user unhandled' dialog when
// JMC is turned on. Many things that VS puts up the unhandled exception dialog for are actually cases where the native
// exception was caught, for example catching exceptions at the thread base. JMC requires further accuracy - in that case
// VS is checking to see if an exception escaped particular ranges of managed code frames.
#define CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE            \
        __dwDispatchCallSimpleFlags |= DispatchCallSimple_CatchHandlerFoundNotification;

#define PERFORM_CALL    \
        void * __retval = NULL;                         \
        __retval = DispatchCallSimple(__pArgs,          \
                           __numStackSlotsToCopy,       \
                           __pSlot,                     \
                           __dwDispatchCallSimpleFlags);\

#ifdef CALLDESCR_ARGREGS

#if defined(TARGET_X86)

// Arguments on x86 are passed backward
#define ARGNUM_0    1
#define ARGNUM_1    0
#define ARGNUM_N(n)    (__numArgs - (n) + 1)

#else

#define ARGNUM_0    0
#define ARGNUM_1    1
#define ARGNUM_N(n)    n

#endif

#define PRECALL_PREP(args)  \
        DWORD __numStackSlotsToCopy = (__numArgs > NUM_ARGUMENT_REGISTERS) ? (__numArgs - NUM_ARGUMENT_REGISTERS) : 0; \
        SIZE_T * __pArgs = (SIZE_T *)args;

#define DECLARE_ARGHOLDER_ARRAY(arg, count)             \
        INIT_VARIABLES(count)                           \
        ARGHOLDER_TYPE arg[(count <= NUM_ARGUMENT_REGISTERS ? NUM_ARGUMENT_REGISTERS : count)];

#else   // CALLDESCR_ARGREGS

#define ARGNUM_0    0
#define ARGNUM_1    1
#define ARGNUM_N(n)    n

#define PRECALL_PREP(args)                              \
        DWORD __numStackSlotsToCopy = (__numArgs > NUM_ARGUMENT_REGISTERS) ? __numArgs : NUM_ARGUMENT_REGISTERS; \
        SIZE_T * __pArgs = (SIZE_T *)args;

#define DECLARE_ARGHOLDER_ARRAY(arg, count)             \
        INIT_VARIABLES(count)                           \
        ARGHOLDER_TYPE arg[(count <= NUM_ARGUMENT_REGISTERS ? NUM_ARGUMENT_REGISTERS : count)];

#endif  // CALLDESCR_ARGREGS


#define CALL_MANAGED_METHOD(ret, rettype, args)         \
        PRECALL_PREP(args)                              \
        PERFORM_CALL                                    \
        ret = *(rettype *)(&__retval);

#define CALL_MANAGED_METHOD_NORET(args)                 \
        PRECALL_PREP(args)                              \
        PERFORM_CALL

#define CALL_MANAGED_METHOD_RETREF(ret, reftype, args)  \
        PRECALL_PREP(args)                              \
        PERFORM_CALL                                    \
        ret = (reftype)ObjectToOBJECTREF((Object *)__retval);

#define ARGNUM_2 ARGNUM_N(2)
#define ARGNUM_3 ARGNUM_N(3)
#define ARGNUM_4 ARGNUM_N(4)
#define ARGNUM_5 ARGNUM_N(5)
#define ARGNUM_6 ARGNUM_N(6)
#define ARGNUM_7 ARGNUM_N(7)
#define ARGNUM_8 ARGNUM_N(8)


void CallDefaultConstructor(OBJECTREF ref);

#endif //!DACCESS_COMPILE

#endif // __CALLHELPERS_H__
