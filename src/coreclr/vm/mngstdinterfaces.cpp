// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  MngStdInterfaces.cpp
**
**
** Purpose: Contains the implementation of the MngStdInterfaces
**          class. This class is used to determine the associated
**
**

===========================================================*/

#include "common.h"

#include "mngstdinterfaces.h"
#include "dispex.h"
#include "class.h"
#include "method.hpp"
#include "runtimecallablewrapper.h"
#include "excep.h"

//
// Declare the static field int the ManagedStdInterfaceMap class.
//

MngStdInterfaceMap *MngStdInterfaceMap::m_pMngStdItfMap=NULL;


//
// Defines used ManagedStdInterfaceMap class implementation.
//

// Use this macro to define an entry in the managed standard interface map.
#define STD_INTERFACE_MAP_ENTRY(TypeName, NativeIID)                                    \
    m_TypeNameToNativeIIDMap.InsertValue((TypeName), (void*)&(NativeIID), TRUE)

//
// Defines used StdMngItfBase class implementation.
//

// The GetInstance method name and signature.
#define GET_INSTANCE_METH_NAME  "GetInstance"
#define GET_INSTANCE_METH_SIG   &gsig_SM_Str_RetICustomMarshaler

// The initial number of buckets in the managed standard interface map.
#define INITIAL_NUM_BUCKETS     64


//
// This method is used to build the managed standard interface map.
//

MngStdInterfaceMap::MngStdInterfaceMap()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    //
    // Initialize the hashtable.
    //

    m_TypeNameToNativeIIDMap.Init(INITIAL_NUM_BUCKETS,NULL,NULL);

    //
    // Define the mapping for the managed standard interfaces.
    //

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
    STD_INTERFACE_MAP_ENTRY(strMngItfName, bCanCastOnNativeItfQI ? NativeItfIID : GUID_NULL);

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl)

#define MNGSTDITF_END_INTERFACE(FriendlyName)

#include "mngstditflist.h"

#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE
}


//
// Helper method to load the types used inside the classes that implement the ECall's for
// the managed standard interfaces.
//

void MngStdItfBase::InitHelper(
                    LPCUTF8 strMngItfTypeName,
                    LPCUTF8 strUComItfTypeName,
                    LPCUTF8 strCMTypeName,
                    LPCUTF8 strCookie,
                    LPCUTF8 strManagedViewName,
                    TypeHandle *pMngItfType,
                    TypeHandle *pUComItfType,
                    TypeHandle *pCustomMarshalerType,
                    TypeHandle *pManagedViewType,
                    OBJECTHANDLE *phndMarshaler)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    // Load the managed interface type.
    *pMngItfType = ClassLoader::LoadTypeByNameThrowing(SystemDomain::SystemAssembly(), NULL, strMngItfTypeName);

    // Run the <clinit> for the managed interface type.
    pMngItfType->GetMethodTable()->CheckRestore();
    pMngItfType->GetMethodTable()->CheckRunClassInitThrowing();

    // Load the UCom type.
    *pUComItfType = ClassLoader::LoadTypeByNameThrowing(SystemDomain::SystemAssembly(), NULL, strUComItfTypeName);

    // Run the <clinit> for the UCom type.
    pUComItfType->GetMethodTable()->CheckRestore();
    pUComItfType->GetMethodTable()->CheckRunClassInitThrowing();

    // Retrieve the custom marshaler type handle.
    *pCustomMarshalerType = ClassLoader::LoadTypeByNameThrowing(SystemDomain::SystemAssembly(), NULL, strCMTypeName);

    // Run the <clinit> for the marshaller.
    pCustomMarshalerType->GetMethodTable()->EnsureInstanceActive();
    pCustomMarshalerType->GetMethodTable()->CheckRunClassInitThrowing();

    // Load the managed view.
    *pManagedViewType = ClassLoader::LoadTypeByNameThrowing(SystemDomain::SystemAssembly(), NULL, strManagedViewName);

    // Run the <clinit> for the managed view.
    pManagedViewType->GetMethodTable()->EnsureInstanceActive();
    pManagedViewType->GetMethodTable()->CheckRunClassInitThrowing();

    // Retrieve the GetInstance method.
    MethodDesc *pGetInstanceMD = MemberLoader::FindMethod(pCustomMarshalerType->GetMethodTable(), GET_INSTANCE_METH_NAME, GET_INSTANCE_METH_SIG);
    _ASSERTE(pGetInstanceMD && "Unable to find specified custom marshaler method");

    // Allocate the string object that will be passed to the GetInstance method.
    STRINGREF strObj = StringObject::NewString(strCookie);
    GCPROTECT_BEGIN(strObj);
    {
        MethodDescCallSite getInstance(pGetInstanceMD, (OBJECTREF*)&strObj);

        // Prepare the arguments that will be passed to GetInstance.
        ARG_SLOT GetInstanceArgs[] = {
            ObjToArgSlot(strObj)
        };

        // Call the static GetInstance method to retrieve the custom marshaler to use.
        OBJECTREF Marshaler = getInstance.Call_RetOBJECTREF(GetInstanceArgs);

        // Cache the handle to the marshaler for faster access.
        (*phndMarshaler) = SystemDomain::GetCurrentDomain()->CreateHandle(Marshaler);
    }
    GCPROTECT_END();
}


//
// Helper method that forwards the calls to either the managed view or to the native component if it
// implements the managed interface.
//

LPVOID MngStdItfBase::ForwardCallToManagedView(
                    OBJECTHANDLE hndMarshaler,
                    MethodDesc *pMngItfMD,
                    MethodDesc *pUComItfMD,
                    MethodDesc *pMarshalNativeToManagedMD,
                    MethodDesc *pMngViewMD,
                    IID *pMngItfIID,
                    IID *pNativeItfIID,
                    ARG_SLOT* pArgs)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    Object* Result = 0;
    ULONG cbRef;
    HRESULT hr;
    IUnknown *pMngItf;
    IUnknown *pNativeItf;
    OBJECTREF ManagedView;
    BOOL      RetValIsProtected = FALSE;
    struct LocalGcRefs {
        OBJECTREF   Obj;
        OBJECTREF   Result;
    } Lr;

    // Retrieve the object that the call was made on.
    Lr.Obj = ArgSlotToObj(pArgs[0]);
    Lr.Result = NULL;
    GCPROTECT_BEGIN(Lr);
    {
        SafeComHolder<IUnknown> pUnk = NULL;

        _ASSERTE(Lr.Obj != NULL);

        MethodTable *pTargetMT = Lr.Obj->GetMethodTable();

        {
            // The target isn't a TP so it better be a COM object.
            _ASSERTE(Lr.Obj->GetMethodTable()->IsComObjectType());

            {
                RCWHolder pRCW(GetThread());
                RCWPROTECT_BEGIN(pRCW, Lr.Obj);

                // Get the IUnknown on the current thread.
                pUnk = pRCW->GetIUnknown();
                _ASSERTE(pUnk);

                RCW_VTABLEPTR(pRCW);

                // Check to see if the component implements the interface natively.
                hr = SafeQueryInterface(pUnk, *pMngItfIID, &pMngItf);
                LogInteropQI(pUnk, *pMngItfIID, hr, "Custom marshaler fwd call QI for managed interface");
                if (SUCCEEDED(hr))
                {
                    // Release our ref-count on the managed interface.
                    cbRef = SafeRelease(pMngItf);
                    LogInteropRelease(pMngItf, cbRef, "Custom marshaler call releasing managed interface");

                    MethodDescCallSite UComItf(pUComItfMD, &Lr.Obj);

                    // The component implements the interface natively so we need to dispatch to it directly.
                    Result = UComItf.Call_RetObjPtr(pArgs);
                    if (UComItf.GetMetaSig()->IsObjectRefReturnType())
                    {
                        Lr.Result = ObjectToOBJECTREF(Result);
                        RetValIsProtected = TRUE;
                    }
                }
                else
                {
                    // QI for the native interface that will be passed to MarshalNativeToManaged.
                    hr = SafeQueryInterface(pUnk, *pNativeItfIID, (IUnknown**)&pNativeItf);
                    LogInteropQI(pUnk, *pNativeItfIID, hr, "Custom marshaler call QI for native interface");
                    _ASSERTE(SUCCEEDED(hr));

                    MethodDescCallSite marshalNativeToManaged(pMarshalNativeToManagedMD, hndMarshaler);

                    // Prepare the arguments that will be passed to GetInstance.
                    ARG_SLOT MarshalNativeToManagedArgs[] = {
                        ObjToArgSlot(ObjectFromHandle(hndMarshaler)),
                        (ARG_SLOT)pNativeItf
                    };

                    // Retrieve the managed view for the current native interface pointer.
                    ManagedView = marshalNativeToManaged.Call_RetOBJECTREF(MarshalNativeToManagedArgs);
                    GCPROTECT_BEGIN(ManagedView);
                    {
                        // Release our ref-count on pNativeItf.
                        cbRef = SafeRelease(pNativeItf);
                        LogInteropRelease(pNativeItf, cbRef, "Custom marshaler fwd call releasing native interface");

                        MethodDescCallSite mngView(pMngViewMD, &ManagedView);

                        // Replace the this in pArgs by the this of the managed view.
                        (*(Object**)pArgs) = OBJECTREFToObject(ManagedView);

                        // Do the actual call to the method in the managed view passing in the args.
                        Result = mngView.Call_RetObjPtr(pArgs);
                        if (mngView.GetMetaSig()->IsObjectRefReturnType())
                        {
                            Lr.Result = ObjectToOBJECTREF(Result);
                            RetValIsProtected = TRUE;
                        }
                    }
                    GCPROTECT_END();
                }
                RCWPROTECT_END(pRCW);
            }
        }
    }
    GCPROTECT_END();

    if (RetValIsProtected)
        Result = OBJECTREFToObject(Lr.Result);

    return (void*)Result;
}


#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl) \
\
    LPVOID __stdcall FriendlyName::ECallMethName##Worker(ARG_SLOT* pArgs) \
    { \
        WRAPPER_NO_CONTRACT; \
        FriendlyName *pMngStdItfInfo = SystemDomain::GetCurrentDomain()->GetMngStdInterfacesInfo()->Get##FriendlyName(); \
        return ForwardCallToManagedView( \
            pMngStdItfInfo->m_hndCustomMarshaler, \
            pMngStdItfInfo->GetMngItfMD(FriendlyName##Methods_##ECallMethName, #MethName, MethSig), \
            pMngStdItfInfo->GetUComItfMD(FriendlyName##Methods_##ECallMethName, #MethName, MethSig), \
            pMngStdItfInfo->GetCustomMarshalerMD(CustomMarshalerMethods_MarshalNativeToManaged), \
            pMngStdItfInfo->GetManagedViewMD(FriendlyName##Methods_##ECallMethName, #MethName, MethSig), \
            &pMngStdItfInfo->m_MngItfIID, \
            &pMngStdItfInfo->m_NativeItfIID, \
            pArgs); \
    }

#define MNGSTDITF_END_INTERFACE(FriendlyName)


#include "mngstditflist.h"


#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE


FCIMPL1(FC_BOOL_RET, StdMngIEnumerator::MoveNext, Object* refThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    LPVOID      retVal = NULL;
    ARG_SLOT    args[1] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE))
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    retVal = MoveNextWorker(args);

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END_POLL();

    // Actual return type is a managed 'bool', so only look at a CLR_BOOL-sized
    // result.  The high bits are undefined on AMD64.  (Note that a narrowing
    // cast to CLR_BOOL will not work since it is the same as checking the
    // size_t result != 0.)
    FC_RETURN_BOOL(*(CLR_BOOL*)StackElemEndiannessFixup(&retVal, sizeof(CLR_BOOL)));
}
FCIMPLEND

FCIMPL1(Object*, StdMngIEnumerator::get_Current, Object* refThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[1] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE))
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    retVal = ObjectToOBJECTREF((Object*)get_CurrentWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL1(void, StdMngIEnumerator::Reset, Object* refThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    ARG_SLOT    args[1] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE))
        };

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    ResetWorker(args);

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND

FCIMPL6(Object*, StdMngIReflect::GetMethod, Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr, Object* refBinderUNSAFE, Object* refTypesArrayUNSAFE, Object* refModifiersArrayUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[6] =
        {
            /* 0 */ ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            /* 1 */ ObjToArgSlot(ObjectToOBJECTREF(refNameUNSAFE)),
            /* 2 */ enumBindingAttr,
            /* 3 */ ObjToArgSlot(ObjectToOBJECTREF(refBinderUNSAFE)),
            /* 4 */ ObjToArgSlot(ObjectToOBJECTREF(refTypesArrayUNSAFE)),
            /* 5 */ ObjToArgSlot(ObjectToOBJECTREF(refModifiersArrayUNSAFE))
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 2);
    GCPROTECT_ARRAY_BEGIN(args[3], 3);

    retVal = ObjectToOBJECTREF((Object*)GetMethodWorker(args));

    GCPROTECT_END();
    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL3(Object*, StdMngIReflect::GetMethod_2,   Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[3] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            ObjToArgSlot(ObjectToOBJECTREF(refNameUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 2);

    retVal = ObjectToOBJECTREF((Object*)GetMethod_2Worker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL2(Object*, StdMngIReflect::GetMethods,    Object* refThisUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[2] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    retVal = ObjectToOBJECTREF((Object*)GetMethodsWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL3(Object*, StdMngIReflect::GetField,      Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[3] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            ObjToArgSlot(ObjectToOBJECTREF(refNameUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 2);

    retVal = ObjectToOBJECTREF((Object*)GetFieldWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL2(Object*, StdMngIReflect::GetFields,     Object* refThisUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[2] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    retVal = ObjectToOBJECTREF((Object*)GetFieldsWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL7(Object*, StdMngIReflect::GetProperty,   Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr, Object* refBinderUNSAFE, Object* refTypeUNSAFE, Object* refTypesArrayUNSAFE, Object* refModifiersArrayUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[7] =
        {
            /* 0 */ ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            /* 1 */ ObjToArgSlot(ObjectToOBJECTREF(refNameUNSAFE)),
            /* 2 */ enumBindingAttr,
            /* 3 */ ObjToArgSlot(ObjectToOBJECTREF(refBinderUNSAFE)),
            /* 4 */ ObjToArgSlot(ObjectToOBJECTREF(refTypeUNSAFE)),
            /* 5 */ ObjToArgSlot(ObjectToOBJECTREF(refTypesArrayUNSAFE)),
            /* 6 */ ObjToArgSlot(ObjectToOBJECTREF(refModifiersArrayUNSAFE))
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 2);
    GCPROTECT_ARRAY_BEGIN(args[3], 4);

    retVal = ObjectToOBJECTREF((Object*)GetFieldWorker(args));

    GCPROTECT_END();
    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL3(Object*, StdMngIReflect::GetProperty_2, Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[3] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            ObjToArgSlot(ObjectToOBJECTREF(refNameUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 2);

    retVal = ObjectToOBJECTREF((Object*)GetProperty_2Worker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL2(Object*, StdMngIReflect::GetProperties, Object* refThisUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[2] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    retVal = ObjectToOBJECTREF((Object*)GetPropertiesWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL3(Object*, StdMngIReflect::GetMember,     Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[3] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            ObjToArgSlot(ObjectToOBJECTREF(refNameUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 2);

    retVal = ObjectToOBJECTREF((Object*)GetMemberWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL2(Object*, StdMngIReflect::GetMembers,    Object* refThisUNSAFE, INT32 enumBindingAttr)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[2] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            enumBindingAttr
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    retVal = ObjectToOBJECTREF((Object*)GetMembersWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL9(Object*, StdMngIReflect::InvokeMember,  Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr, Object* refBinderUNSAFE, Object* refTargetUNSAFE, Object* refArgsArrayUNSAFE, Object* refModifiersArrayUNSAFE, Object* refCultureUNSAFE, Object* refNamedParamsArrayUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[9] =
        {
            /* 0 */ ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE)),
            /* 1 */ ObjToArgSlot(ObjectToOBJECTREF(refNameUNSAFE)),
            /* 2 */ enumBindingAttr,
            /* 3 */ ObjToArgSlot(ObjectToOBJECTREF(refBinderUNSAFE)),
            /* 4 */ ObjToArgSlot(ObjectToOBJECTREF(refTargetUNSAFE)),
            /* 5 */ ObjToArgSlot(ObjectToOBJECTREF(refArgsArrayUNSAFE)),
            /* 6 */ ObjToArgSlot(ObjectToOBJECTREF(refModifiersArrayUNSAFE)),
            /* 7 */ ObjToArgSlot(ObjectToOBJECTREF(refCultureUNSAFE)),
            /* 8 */ ObjToArgSlot(ObjectToOBJECTREF(refNamedParamsArrayUNSAFE))
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 2);
    GCPROTECT_ARRAY_BEGIN(args[3], 6);

    retVal = ObjectToOBJECTREF((Object*)InvokeMemberWorker(args));

    GCPROTECT_END();
    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND

FCIMPL1(Object*, StdMngIReflect::get_UnderlyingSystemType, Object* refThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[1] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE))
        };

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    retVal = ObjectToOBJECTREF((Object*)get_UnderlyingSystemTypeWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND


FCIMPL1(Object*, StdMngIEnumerable::GetEnumerator, Object* refThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF   retVal = NULL;
    ARG_SLOT    args[1] =
        {
            ObjToArgSlot(ObjectToOBJECTREF(refThisUNSAFE))
        };
    OBJECTREF *porefThis = (OBJECTREF *)&args[0];

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    GCPROTECT_ARRAY_BEGIN(args[0], 1);

    // To handle calls via IEnumerable::GetEnumerator on an RCW we use
    // EnumerableToDispatchMarshaler (legacy COM interop)
    retVal = ObjectToOBJECTREF((Object*)GetEnumeratorWorker(args));

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_GC_POLL_AND_RETURN_OBJREF(retVal);
}
FCIMPLEND
