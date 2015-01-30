//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
#ifndef CLR_STANDALONE_BINDER
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

    // Loads a type from the given Framework assembly.
    static MethodTable *LoadTypeFromRedirectedAssembly(WinMDAdapter::FrameworkAssemblyIndex index, LPCWSTR wzTypeName);

    // Loads a method from the given Framework assembly.
    static MethodDesc *LoadMethodFromRedirectedAssembly(WinMDAdapter::FrameworkAssemblyIndex index, LPCWSTR wzTypeName, LPCUTF8 szMethodName);

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
#endif // !CLR_STANDALONE_BINDER

    // Returns the redirection index if the MethodTable* is a redirected interface.
    static inline bool ResolveRedirectedInterface(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex * pIndex);

#ifndef CLR_STANDALONE_BINDER

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
        const WinMDAdapter::FrameworkAssemblyIndex m_AssemblyIndex;
        const LPCWSTR m_wzWinRTInterfaceTypeName;
        const LPCWSTR m_wzCLRStubClassTypeName;
        const LPCWSTR m_wzWinRTStubClassTypeName;
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
#endif // !CLR_STANDALONE_BINDER
};


// Provides functionality related to redirecting WinRT delegates.
class WinRTDelegateRedirector
{
public:
    static MethodTable *GetWinRTTypeForRedirectedDelegateIndex(WinMDAdapter::RedirectedTypeIndex index);

    static bool WinRTDelegateRedirector::ResolveRedirectedDelegate(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex *pIndex);
};

#endif // WINRT_DELEGATE_REDIRECTOR_H
