// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: winrtredirector.h
//

//

//
// ============================================================================

#ifndef WINRT_DELEGATE_REDIRECTOR_H
#define WINRT_DELEGATE_REDIRECTOR_H

#include "winrttypenameconverter.h"

// Provides functionality related to redirecting WinRT interfaces.
// @TODO: This should take advantage of the macros in WinRTProjectedTypes.h instead of hardcoding
// the knowledge about redirected interfaces here.
class WinRTInterfaceRedirector
{
public:
    // Returns a MethodDesc to be used as an interop stub for the given redirected interface/slot/direction.
    static MethodDesc *GetStubMethodForRedirectedInterface(
        WinMDAdapter::RedirectedTypeIndex   interfaceIndex,                // redirected interface index
        int                                 slot,                          // slot number of the method for which a stub is needed
        TypeHandle::InteropKind             interopKind,                   // Interop_ManagedToNative (stub for RCW) or Interop_NativeToManaged (stub for CCW)
        BOOL                                fICollectionStub,              // need stub for ICollection`1 (only valid with Interop_ManagedToNative)
        Instantiation                       methodInst = Instantiation()); // requested method instantiation if the stub method is generic

    // Returns a MethodDesc to be used as an interop stub for the given method and direction.
    static MethodDesc *GetStubMethodForRedirectedInterfaceMethod(MethodDesc *pMD, TypeHandle::InteropKind interopKind);

    // Returns MethodTable (typical instantiation) of the Framework copy of the specified redirected WinRT interface.
    static MethodTable *GetWinRTTypeForRedirectedInterfaceIndex(WinMDAdapter::RedirectedTypeIndex index);
    
    // Loads a method from the given Framework assembly.
    static MethodDesc *LoadMethodFromRedirectedAssembly(LPCUTF8 szAssemblyQualifiedTypeName, LPCUTF8 szMethodName);

    // Lists WinRT-legal types assignable from .NET reference types that are projected from WinRT structures/arrays/delegates.
    enum WinRTLegalStructureBaseType
    {
        BaseType_None,
        BaseType_Object,            // System.Object                                (assignable from Type, string, Exception)
        BaseType_IEnumerable,       // System.Collections.IEnumerable               (assignable from string)
        BaseType_IEnumerableOfChar  // System.Collections.Generic.IEnumerable<char> (assignable from string)
    };

    // Determines if the generic argument in the given instantiation is a WinRT-legal base type of a WinRT structure type.
    static WinRTLegalStructureBaseType GetStructureBaseType(Instantiation inst)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(inst.GetNumArgs() == 1);

        if (!inst[0].IsTypeDesc())
        {
            MethodTable *pInstArgMT = inst[0].AsMethodTable();
            
            if (pInstArgMT == g_pObjectClass)
                return BaseType_Object;

            if (pInstArgMT == MscorlibBinder::GetExistingClass(CLASS__IENUMERABLE))
                return BaseType_IEnumerable;

            if (pInstArgMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IENUMERABLEGENERIC)) &&
                pInstArgMT->GetInstantiation()[0].GetSignatureCorElementType() == ELEMENT_TYPE_CHAR)
                return BaseType_IEnumerableOfChar;
        }
        return BaseType_None;
    }

    // Returns the redirection index if the MethodTable* is a redirected interface.
    static inline bool ResolveRedirectedInterface(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex * pIndex);

#ifdef _DEBUG
    static void VerifyRedirectedInterfaceStubs();
#endif // _DEBUG

private:
    static inline int GetStubInfoIndex(WinMDAdapter::RedirectedTypeIndex index)
    {
        LIMITED_METHOD_CONTRACT;

        switch (index)
        {
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IEnumerable:                  return 0;
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IList:                        return 1;
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IDictionary:                  return 2;
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IReadOnlyList:                return 3;
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IReadOnlyDictionary:          return 4;
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_IEnumerable:                          return 5;
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_IList:                                return 6;
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_INotifyCollectionChanged: return 7;
            case WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_INotifyPropertyChanged:            return 8;
            case WinMDAdapter::RedirectedTypeIndex_System_Windows_Input_ICommand:                           return 9;
            case WinMDAdapter::RedirectedTypeIndex_System_IDisposable:                                      return 10;
            default:
                UNREACHABLE();
        }
    }

    struct RedirectedInterfaceStubInfo
    {
        const BinderClassID m_WinRTInterface;
        const int m_iCLRMethodCount;
        const BinderMethodID *m_rCLRStubMethods;
        const int m_iWinRTMethodCount;
        const BinderMethodID *m_rWinRTStubMethods;
    };

    struct NonMscorlibRedirectedInterfaceInfo
    {
        const LPCUTF8 m_szWinRTInterfaceAssemblyQualifiedTypeName;
        const LPCUTF8 m_szCLRStubClassAssemblyQualifiedTypeName;
        const LPCUTF8 m_szWinRTStubClassAssemblyQualifiedTypeName;
        const LPCUTF8 *m_rszMethodNames;
    };

    enum
    {
        s_NumRedirectedInterfaces = 11
    };

    // Describes stubs used for marshaling of redirected interfaces.
    const static RedirectedInterfaceStubInfo s_rInterfaceStubInfos[2 * s_NumRedirectedInterfaces];
    const static NonMscorlibRedirectedInterfaceInfo s_rNonMscorlibInterfaceInfos[3];

    const static int NON_MSCORLIB_MARKER = 0x80000000;
};


// Provides functionality related to redirecting WinRT delegates.
class WinRTDelegateRedirector
{
public:
    static MethodTable *GetWinRTTypeForRedirectedDelegateIndex(WinMDAdapter::RedirectedTypeIndex index);

    static bool ResolveRedirectedDelegate(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex *pIndex);
};

#endif // WINRT_DELEGATE_REDIRECTOR_H
