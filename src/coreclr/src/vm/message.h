// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** File:    message.h
**
** Purpose: Encapsulates a function call frame into a message 
**          object with an interface that can enumerate the 
**          arguments of the messagef
**
**

===========================================================*/
#ifndef ___MESSAGE_H___
#define ___MESSAGE_H___

#ifndef FEATURE_REMOTING
#error FEATURE_REMOTING is not set, please do not include message.h
#endif

#include "fcall.h"

//+----------------------------------------------------------
//
//  Struct:     MessageObject
// 
//  Synopsis:   Physical mapping of the System.Runtime.Remoting.Message
//              object.
// 
//
//------------------------------------------------------------
class MessageObject : public Object
{
    friend class MscorlibBinder;

public:
    MetaSig* GetResetMetaSig()
    {
        CONTRACT(MetaSig*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(CheckPointer(pMetaSigHolder));
            POSTCONDITION(CheckPointer(RETVAL));
            SO_TOLERANT;
        }
        CONTRACT_END;

        pMetaSigHolder->Reset();
        RETURN pMetaSigHolder;
    }            

    FramedMethodFrame *GetFrame()
    {
        CONTRACT(FramedMethodFrame *)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        RETURN pFrame;
    }

    MethodDesc *GetMethodDesc()
    {
        CONTRACT(MethodDesc *)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            POSTCONDITION(CheckPointer(RETVAL));
            SO_TOLERANT;
        }
        CONTRACT_END;

        RETURN pMethodDesc;
    }

    MethodDesc *GetDelegateMD()
    {
        CONTRACT(MethodDesc *)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            SO_TOLERANT;
        }
        CONTRACT_END;

        RETURN pDelegateMD;
    }
    
    INT32 GetFlags()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        return iFlags;
    }

private:
    STRINGREF          pMethodName;    // Method name
    BASEARRAYREF       pMethodSig;     // Array of parameter types
    OBJECTREF          pMethodBase;    // Reflection method object
    OBJECTREF          pHashTable;     // hashtable for properties
    STRINGREF          pURI;           // object's URI
    STRINGREF          pTypeName;       // not used in VM, placeholder
    OBJECTREF          pFault;         // exception

    OBJECTREF          pID;            // not used in VM, placeholder
    OBJECTREF          pSrvID;         // not used in VM, placeholder
    OBJECTREF          pArgMapper;     // not used in VM, placeholder
    OBJECTREF          pCallCtx;       // not used in VM, placeholder

    FramedMethodFrame  *pFrame;
    MethodDesc         *pMethodDesc;
    MetaSig            *pMetaSigHolder;
    MethodDesc         *pDelegateMD;
    TypeHandle          thGoverningType;
    INT32               iFlags;
    CLR_BOOL            initDone;       // called the native Init routine
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<MessageObject> MESSAGEREF;
#else
typedef MessageObject* MESSAGEREF;
#endif

// *******
// Note: Needs to be in sync with flags in Message.cs
// *******
enum
{
    MSGFLG_BEGININVOKE = 0x01,
    MSGFLG_ENDINVOKE   = 0x02,
    MSGFLG_CTOR        = 0x04,
    MSGFLG_ONEWAY      = 0x08,
    MSGFLG_FIXEDARGS   = 0x10,
    MSGFLG_VARARGS     = 0x20
};

//+----------------------------------------------------------
//
//  Class:      CMessage
// 
//  Synopsis:   EE counterpart to Microsoft.Runtime.Message.
//              Encapsulates code to read a function call 
//              frame into an interface that can enumerate
//              the parameters.
// 
//
//------------------------------------------------------------
class CMessage
{
public:
    // public fcalls.
    static FCDECL1(INT32, GetArgCount, MessageObject *pMsg);
    static FCDECL2(Object*, GetArg, MessageObject* pMessage, INT32 argNum);
    static FCDECL1(Object*, GetArgs, MessageObject* pMessageUNSAFE);
    static FCDECL3(void, PropagateOutParameters, MessageObject* pMessageUNSAFE, ArrayBase* pOutPrmsUNSAFE, Object* RetValUNSAFE);
    static FCDECL1(Object*, GetReturnValue, MessageObject* pMessageUNSAFE);
    static FCDECL3(void, GetAsyncBeginInfo, MessageObject* pMessageUNSAFE, OBJECTREF* ppACBD, OBJECTREF* ppState);
    static FCDECL1(LPVOID, GetAsyncResult, MessageObject* pMessageUNSAFE);
    static FCDECL1(Object*, GetAsyncObject, MessageObject* pMessageUNSAFE);
    static FCDECL1(void, DebugOut, StringObject* pOutUNSAFE);
    static FCDECL2(FC_BOOL_RET, Dispatch, MessageObject* pMessageUNSAFE, Object* pServerUNSAFE);
    static FCDECL1(FC_BOOL_RET, HasVarArgs, MessageObject * poMessage);

public:
    // public helper
    static void GetObjectFromStack(OBJECTREF* ppDest, PVOID val, const CorElementType eType, TypeHandle ty, BOOL fIsByRef = FALSE, FramedMethodFrame *pFrame = NULL);

private:
    // private helpers
    static PVOID GetStackPtr(INT32 ndx, FramedMethodFrame *pFrame, MetaSig *pSig);       
    static int GetStackOffset (FramedMethodFrame *pFrame, ArgIterator *pArgIter, MetaSig *pSig);
    static INT64 __stdcall CallMethod(const void *pTarget,
                                        INT32 cArgs,
                                        FramedMethodFrame *pFrame,
                                        OBJECTREF pObj);
    static INT64 CopyOBJECTREFToStack(PVOID pvDest, OBJECTREF *pSrc, CorElementType typ, TypeHandle ty, MetaSig *pSig, BOOL fCopyClassContents);
    static LPVOID GetLastArgument(MESSAGEREF *pMsg);
    static void AppendAssemblyName(CQuickBytes &out, const CHAR* str);
};

#endif // ___MESSAGE_H___
