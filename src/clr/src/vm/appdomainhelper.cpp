// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 

#include "common.h"

#ifdef FEATURE_REMOTING

#include "appdomainhelper.h"
#include "appdomain.inl"

void AppDomainHelper::CopyEncodingToByteArray(IN PBYTE   pbData,
                                              IN DWORD   cbData,
                                              OUT OBJECTREF* pArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(cbData==0 || pbData!=NULL);
        PRECONDITION(CheckPointer(pArray));
    }
    CONTRACTL_END;
    PREFIX_ASSUME(pArray != NULL);

    U1ARRAYREF pObj;

    if(cbData) {
        pObj = (U1ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_U1,cbData);
        memcpyNoGCRefs(pObj->m_Array, pbData, cbData);
        *pArray = (OBJECTREF) pObj;
    } else
        *pArray = NULL;

    VALIDATEOBJECTREF(*pArray);
}


void AppDomainHelper::CopyByteArrayToEncoding(IN U1ARRAYREF* pArray,
                                              OUT PBYTE*   ppbData,
                                              OUT DWORD*   pcbData)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pArray!=NULL);
        PRECONDITION(ppbData!=NULL);
        PRECONDITION(pcbData!=NULL);
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*pArray);

    if (*pArray == NULL) {
        *ppbData = NULL;
        *pcbData = 0;
        return;
    }

    DWORD size = (*pArray)->GetNumComponents();
    if(size) {
        *ppbData = new BYTE[size];
        *pcbData = size;

        CopyMemory(*ppbData, (*pArray)->GetDirectPointerToNonObjectElements(), size);
    }
}


struct MarshalObjectArgs : public CtxTransitionBaseArgs
{
    OBJECTREF* orObject;
    U1ARRAYREF* porBlob;
};

void MarshalObjectADCallback(MarshalObjectArgs * args)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    MethodDescCallSite marshalObject(METHOD__APP_DOMAIN__MARSHAL_OBJECT);
    
    ARG_SLOT argsCall[] = {
        ObjToArgSlot(*(args->orObject))
    };
    
    *(args->porBlob) = (U1ARRAYREF) marshalObject.Call_RetOBJECTREF(argsCall);
}


// Marshal a single object into a serialized blob.
void AppDomainHelper::MarshalObject(ADID appDomain,
                                    IN OBJECTREF *orObject, // Object must be GC protected
                                    OUT U1ARRAYREF *porBlob)
{

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orObject!=NULL);
        PRECONDITION(porBlob!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject));
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*orObject);

    MarshalObjectArgs args;
    args.orObject = orObject;
    args.porBlob = porBlob;
    
    MakeCallWithPossibleAppDomainTransition(appDomain, (FPAPPDOMAINCALLBACK) MarshalObjectADCallback, &args);

    VALIDATEOBJECTREF(*porBlob);

}

void AppDomainHelper::MarshalObject(IN OBJECTREF *orObject, // Object must be GC protected
                                    OUT U1ARRAYREF *porBlob)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orObject!=NULL);
        PRECONDITION(porBlob!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject));
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*orObject);

    MethodDescCallSite marshalObject(METHOD__APP_DOMAIN__MARSHAL_OBJECT);
    
    ARG_SLOT argsCall[] = {
        ObjToArgSlot(*orObject)
    };
    
    *porBlob = (U1ARRAYREF) marshalObject.Call_RetOBJECTREF(argsCall);

    VALIDATEOBJECTREF(*porBlob);
}

// Marshal a single object into a serialized blob.
void AppDomainHelper::MarshalObject(IN AppDomain *pDomain,
                                    IN OBJECTREF *orObject, // Object must be GC protected
                                    OUT BYTE    **ppbBlob,
                                    OUT DWORD    *pcbBlob)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pDomain!=NULL);
        PRECONDITION(orObject!=NULL);
        PRECONDITION(ppbBlob!=NULL);
        PRECONDITION(pcbBlob!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject));
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*orObject);

    U1ARRAYREF orBlob = NULL;

    GCPROTECT_BEGIN(orBlob);

    MethodDescCallSite marshalObject(METHOD__APP_DOMAIN__MARSHAL_OBJECT);

    ENTER_DOMAIN_PTR(pDomain,ADV_RUNNINGIN)
    {
        ARG_SLOT args[] = 
        {
            ObjToArgSlot(*orObject)
        };

        orBlob = (U1ARRAYREF) marshalObject.Call_RetOBJECTREF(args);
    }
    END_DOMAIN_TRANSITION;
        
    if (orBlob != NULL)
        CopyByteArrayToEncoding(&orBlob,
                                ppbBlob,
                                pcbBlob);
    GCPROTECT_END();
}

// Marshal two objects into serialized blobs.
void AppDomainHelper::MarshalObjects(IN AppDomain *pDomain,
                                    IN OBJECTREF  *orObject1,
                                    IN OBJECTREF  *orObject2,
                                    OUT BYTE    **ppbBlob1,
                                    OUT DWORD    *pcbBlob1,
                                    OUT BYTE    **ppbBlob2,
                                    OUT DWORD    *pcbBlob2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pDomain!=NULL);
        PRECONDITION(orObject1!=NULL);
        PRECONDITION(ppbBlob1!=NULL);
        PRECONDITION(pcbBlob1!=NULL);
        PRECONDITION(orObject2!=NULL);
        PRECONDITION(ppbBlob2!=NULL);
        PRECONDITION(pcbBlob2!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject1));
        PRECONDITION(IsProtectedByGCFrame(orObject2));
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*orObject1);
    VALIDATEOBJECTREF(*orObject2);

    struct _gc {
        U1ARRAYREF  orBlob1;
        U1ARRAYREF  orBlob2;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    MethodDescCallSite marshalObjects(METHOD__APP_DOMAIN__MARSHAL_OBJECTS);

    ENTER_DOMAIN_PTR(pDomain,ADV_RUNNINGIN)
    {
        ARG_SLOT args[] = 
        {
            ObjToArgSlot(*orObject1),
            ObjToArgSlot(*orObject2),
            PtrToArgSlot(&gc.orBlob2),
        };

        gc.orBlob1 = (U1ARRAYREF) marshalObjects.Call_RetOBJECTREF(args);
    }
    END_DOMAIN_TRANSITION;

    if (gc.orBlob1 != NULL)
    {
        CopyByteArrayToEncoding(&gc.orBlob1,
                                ppbBlob1,
                                pcbBlob1);
    }
    
    if (gc.orBlob2 != NULL)
    {
        CopyByteArrayToEncoding(&gc.orBlob2,
                                ppbBlob2,
                                pcbBlob2);
    }
    
    GCPROTECT_END();
}

// Unmarshal a single object from a serialized blob.
// Callers must GC protect both porBlob and porObject.
void AppDomainHelper::UnmarshalObject(IN AppDomain  *pDomain,
                                     IN U1ARRAYREF  *porBlob,  // Object must be GC protected
                                     OUT OBJECTREF  *porObject)  // Object must be GC protected
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pDomain!=NULL);
        PRECONDITION(porBlob!=NULL);
        PRECONDITION(porObject!=NULL);
        PRECONDITION(IsProtectedByGCFrame(porBlob));
        PRECONDITION(IsProtectedByGCFrame(porObject));
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*porBlob);

    MethodDescCallSite unmarshalObject(METHOD__APP_DOMAIN__UNMARSHAL_OBJECT);

    ENTER_DOMAIN_PTR(pDomain,ADV_RUNNINGIN)
    {
        ARG_SLOT args[] = 
        {
            ObjToArgSlot(*porBlob)
        };

        *porObject = unmarshalObject.Call_RetOBJECTREF(args);
    }
    END_DOMAIN_TRANSITION;

    VALIDATEOBJECTREF(*porObject);
}

// Unmarshal a single object from a serialized blob.
void AppDomainHelper::UnmarshalObject(IN AppDomain   *pDomain,
                                     IN BYTE        *pbBlob,
                                     IN DWORD        cbBlob,
                                     OUT OBJECTREF  *porObject)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pDomain!=NULL);
        PRECONDITION(porObject!=NULL);
        PRECONDITION(IsProtectedByGCFrame(porObject));
    }
    CONTRACTL_END;

    OBJECTREF orBlob = NULL;

    MethodDescCallSite unmarshalObject(METHOD__APP_DOMAIN__UNMARSHAL_OBJECT);

    ENTER_DOMAIN_PTR(pDomain,ADV_RUNNINGIN)
    {
        GCPROTECT_BEGIN(orBlob);

        AppDomainHelper::CopyEncodingToByteArray(pbBlob,
                                                cbBlob,
                                                &orBlob);

        ARG_SLOT args[] = 
        {
            ObjToArgSlot(orBlob)
        };

        *porObject = unmarshalObject.Call_RetOBJECTREF(args);

        GCPROTECT_END();
    }
    END_DOMAIN_TRANSITION;

    VALIDATEOBJECTREF(*porObject);
}

// Unmarshal two objects from serialized blobs.
void AppDomainHelper::UnmarshalObjects(IN AppDomain   *pDomain,
                                      IN BYTE        *pbBlob1,
                                      IN DWORD        cbBlob1,
                                      IN BYTE        *pbBlob2,
                                      IN DWORD        cbBlob2,
                                      OUT OBJECTREF  *porObject1,
                                      OUT OBJECTREF  *porObject2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pDomain!=NULL);
        PRECONDITION(porObject1!=NULL);
        PRECONDITION(porObject2!=NULL);
        PRECONDITION(IsProtectedByGCFrame(porObject1));
        PRECONDITION(IsProtectedByGCFrame(porObject2));
    }
    CONTRACTL_END;

    MethodDescCallSite unmarshalObjects(METHOD__APP_DOMAIN__UNMARSHAL_OBJECTS);

    struct _gc {
        OBJECTREF  orBlob1;
        OBJECTREF  orBlob2;
        OBJECTREF  orObject2;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    ENTER_DOMAIN_PTR(pDomain,ADV_RUNNINGIN)
    {

        GCPROTECT_BEGIN(gc);

        AppDomainHelper::CopyEncodingToByteArray(pbBlob1,
                                                cbBlob1,
                                                &gc.orBlob1);

        AppDomainHelper::CopyEncodingToByteArray(pbBlob2,
                                                cbBlob2,
                                                &gc.orBlob2);

        ARG_SLOT args[] = 
        {
            ObjToArgSlot(gc.orBlob1),
            ObjToArgSlot(gc.orBlob2),
            PtrToArgSlot(&gc.orObject2),
        };

        *porObject1 = unmarshalObjects.Call_RetOBJECTREF(args);
        *porObject2 = gc.orObject2;

        GCPROTECT_END();
    }
    END_DOMAIN_TRANSITION;

    VALIDATEOBJECTREF(*porObject1);
    VALIDATEOBJECTREF(*porObject2);
}

// Copy an object from the given appdomain into the current appdomain.
OBJECTREF AppDomainHelper::CrossContextCopyFrom(IN ADID dwDomainId,
                                                IN OBJECTREF *orObject) // Object must be GC protected
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orObject!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject));
    }
    CONTRACTL_END;

    struct _gc
    {
        U1ARRAYREF  orBlob;
        OBJECTREF pResult;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    
    GCPROTECT_BEGIN(gc);
    AppDomainHelper::MarshalObject(dwDomainId, orObject, &gc.orBlob);
    AppDomainHelper::UnmarshalObject(GetAppDomain(), &gc.orBlob, &gc.pResult);
    GCPROTECT_END();
    VALIDATEOBJECTREF(gc.pResult);
    return gc.pResult;
}

// Copy an object from the given appdomain into the current appdomain.
OBJECTREF AppDomainHelper::CrossContextCopyTo(IN ADID dwDomainId,
                                              IN OBJECTREF *orObject) // Object must be GC protected
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orObject!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject));
    }
    CONTRACTL_END;


    struct _gc
    {
        U1ARRAYREF  orBlob;
        OBJECTREF pResult;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    
    GCPROTECT_BEGIN(gc);
    AppDomainHelper::MarshalObject(orObject, &gc.orBlob);
    ENTER_DOMAIN_ID(dwDomainId);
    AppDomainHelper::UnmarshalObject(GetAppDomain(),&gc.orBlob, &gc.pResult);
    END_DOMAIN_TRANSITION;
    GCPROTECT_END();
    VALIDATEOBJECTREF(gc.pResult);
    return gc.pResult;

}

// Copy an object from the given appdomain into the current appdomain.
OBJECTREF AppDomainHelper::CrossContextCopyFrom(IN AppDomain *pDomain,
                                                IN OBJECTREF *orObject) // Object must be GC protected
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orObject!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject));
        PRECONDITION(pDomain!=NULL);
        PRECONDITION(pDomain != GetAppDomain());
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*orObject);

    struct _gc {
        U1ARRAYREF  orBlob;
        OBJECTREF   result;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    ENTER_DOMAIN_PTR(pDomain, ADV_RUNNINGIN);
    AppDomainHelper::MarshalObject(orObject, &gc.orBlob);
    END_DOMAIN_TRANSITION;
    AppDomainHelper::UnmarshalObject(GetAppDomain(),&gc.orBlob, &gc.result);
    GCPROTECT_END();

    VALIDATEOBJECTREF(gc.result);

    return gc.result;
}

// Copy an object to the given appdomain from the current appdomain.
OBJECTREF AppDomainHelper::CrossContextCopyTo(IN AppDomain *pDomain,
                                              IN OBJECTREF *orObject) // Object must be GC protected
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orObject!=NULL);
        PRECONDITION(IsProtectedByGCFrame(orObject));
        PRECONDITION(pDomain!=NULL);
        PRECONDITION(pDomain != GetAppDomain());
    }
    CONTRACTL_END;

    VALIDATEOBJECTREF(*orObject);

    struct _gc {
        U1ARRAYREF  orBlob;
        OBJECTREF   result;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    AppDomainHelper::MarshalObject(orObject, &gc.orBlob);
    AppDomainHelper::UnmarshalObject(pDomain, &gc.orBlob, &gc.result);
    GCPROTECT_END();

    VALIDATEOBJECTREF(gc.result);

    return gc.result;
}

#endif //  FEATURE_REMOTING

