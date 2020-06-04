// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: winrtredirector.inl
//

//

//
// ============================================================================

#if !defined(WINRT_DELEGATE_REDIRECTOR_INL) && defined(WINRT_DELEGATE_REDIRECTOR_H)
#define WINRT_DELEGATE_REDIRECTOR_INL

#ifdef FEATURE_COMINTEROP

/*static*/
inline bool WinRTInterfaceRedirector::ResolveRedirectedInterface(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex * pIndex)
{
    LIMITED_METHOD_CONTRACT;

    WinMDAdapter::RedirectedTypeIndex index;
    WinMDAdapter::WinMDTypeKind kind;

    if (WinRTTypeNameConverter::ResolveRedirectedType(pMT, &index, &kind))
    {
        if ((kind == WinMDAdapter::WinMDTypeKind_Interface || kind == WinMDAdapter::WinMDTypeKind_PInterface) &&
            // filter out KeyValuePair and Nullable which are structures projected from WinRT interfaces
            index != WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_KeyValuePair &&
            index != WinMDAdapter::RedirectedTypeIndex_System_Nullable)
        {
            if (pIndex != NULL)
            {
                *pIndex = index;
            }
            return true;
        }
    }

    return false;
}

/*static */
inline bool WinRTDelegateRedirector::ResolveRedirectedDelegate(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex *pIndex)
{
    LIMITED_METHOD_CONTRACT;

    WinMDAdapter::RedirectedTypeIndex index;
    WinMDAdapter::WinMDTypeKind kind;

    if (WinRTTypeNameConverter::ResolveRedirectedType(pMT, &index, &kind))
    {
        if (kind == WinMDAdapter::WinMDTypeKind_Delegate ||
            kind == WinMDAdapter::WinMDTypeKind_PDelegate)
        {
            if (pIndex != NULL)
            {
                *pIndex = index;
            }
            return true;
        }
    }

    return false;
}

#endif // FEATURE_COMINTEROP

#endif // WINRT_DELEGATE_REDIRECTOR_INL
