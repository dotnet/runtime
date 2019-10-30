// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: generics.inl
//

//
// Helper functions for generics implementation
//

//
// ============================================================================

#ifndef GENERICS_INL
#define GENERICS_INL

#ifdef FEATURE_COMINTEROP
#include "winrttypenameconverter.h"
#endif

// Generics helper functions
namespace Generics
{
#ifndef DACCESS_COMPILE
    inline void DetermineCCWTemplateAndGUIDPresenceOnNonCanonicalMethodTable(
        // Input
        MethodTable *pOldMT, BOOL fNewMTContainsGenericVariables,
        // Output
        BOOL *pfHasGuidInfo, BOOL *pfHasCCWTemplate)
    {
        STANDARD_VM_CONTRACT;

#ifdef FEATURE_COMINTEROP 
        WORD wNumInterfaces = static_cast<WORD>(pOldMT->GetNumInterfaces());

        InterfaceInfo_t * pOldIMap = (InterfaceInfo_t *)pOldMT->GetInterfaceMap();

        BOOL fHasGuidInfo = FALSE;

        // Generic WinRT delegates expose a class interface and need the CCW template
        BOOL fHasCCWTemplate = FALSE;
    
        if (!fNewMTContainsGenericVariables)
        {
            if (pOldMT->IsInterface())
            {
                fHasGuidInfo = (pOldMT->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(pOldMT, WinMDAdapter::WinMDTypeKind_PInterface));
            }
            else if (pOldMT->IsDelegate())
            {
                fHasGuidInfo = (pOldMT->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(pOldMT, WinMDAdapter::WinMDTypeKind_PDelegate));
                
                // Generic WinRT delegates expose a class interface and need a CCW template
                fHasCCWTemplate = fHasGuidInfo;
            }
            
            if (!fHasCCWTemplate)
            {
                if (pOldMT->IsInterface())
                {
                    // Interfaces need the CCW template if they are redirected and need variance
                    if (pOldMT->HasVariance() &&
                        (pOldMT->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(pOldMT, WinMDAdapter::WinMDTypeKind_PInterface)))
                        {
                            fHasCCWTemplate = TRUE;
                        }
                }
                else
                {
                    // Other types may need the CCW template if they implement generic interfaces
                    for (WORD iItf = 0; iItf < wNumInterfaces; iItf++)
                    {
                        // If the class implements a generic WinRT interface, it needs its own (per-instantiation)
                        // CCW template as the one on EEClass would be shared and hence useless.
                        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOAD_APPROXPARENTS);
                        MethodTable *pItfMT = pOldIMap[iItf].GetApproxMethodTable(pOldMT->GetLoaderModule());
                        if (pItfMT->HasInstantiation() && 
                            (pItfMT->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(pItfMT, WinMDAdapter::WinMDTypeKind_PInterface)))
                        {
                            fHasCCWTemplate = TRUE;
                            break;
                        }
                    }
                }
            }
        }
#else // FEATURE_COMINTEROP
        BOOL fHasGuidInfo = FALSE;
        BOOL fHasCCWTemplate = FALSE;
#endif // FEATURE_COMINTEROP
        *pfHasGuidInfo = fHasGuidInfo;
        *pfHasCCWTemplate = fHasCCWTemplate;
    }
#endif // DACCESS_COMPILE
}

#endif // GENERICS_INL
