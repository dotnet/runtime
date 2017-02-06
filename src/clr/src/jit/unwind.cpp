// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              UnwindInfo                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if FEATURE_EH_FUNCLETS

//------------------------------------------------------------------------
// Compiler::unwindGetFuncLocations: Get the start/end emitter locations for this
// function or funclet. If 'getHotSectionData' is true, get the start/end locations
// for the hot section. Otherwise, get the data for the cold section.
//
// Note that we grab these locations before the prolog and epilogs are generated, so the
// locations must remain correct after the prolog and epilogs are generated.
//
// For the prolog, instructions are put in the special, preallocated, prolog instruction group.
// We don't want to expose the emitPrologIG unnecessarily (locations are actually pointers to
// emitter instruction groups). Since we know the offset of the start of the function/funclet,
// where the prolog is, will be zero, we use a nullptr start location to indicate that.
//
// There is no instruction group beyond the end of the end of the function, so there is no
// location to indicate that. Once again, use nullptr for that.
//
// Intermediate locations point at the first instruction group of a funclet, which is a
// placeholder IG. These are converted to real IGs, not deleted and replaced, so the location
// remains valid.
//
// Arguments:
//    func              - main function or funclet to get locations for.
//    getHotSectionData - 'true' to get the hot section data, 'false' to get the cold section data.
//    ppStartLoc        - OUT parameter. Set to the start emitter location.
//    ppEndLoc          - OUT parameter. Set to the end   emitter location (the location immediately
//                        the range; the 'end' location is not inclusive).
//
// Notes:
//    A start location of nullptr means the beginning of the code.
//    An end location of nullptr means the end of the code.
//
void Compiler::unwindGetFuncLocations(FuncInfoDsc*             func,
                                      bool                     getHotSectionData,
                                      /* OUT */ emitLocation** ppStartLoc,
                                      /* OUT */ emitLocation** ppEndLoc)
{
    if (func->funKind == FUNC_ROOT)
    {
        // Since all funclets are pulled out of line, the main code size is everything
        // up to the first handler. If the function is hot/cold split, we need to get the
        // appropriate sub-range.

        if (getHotSectionData)
        {
            *ppStartLoc = nullptr; // nullptr emit location means the beginning of the code. This is to handle the first
                                   // fragment prolog.

            if (fgFirstColdBlock != nullptr)
            {
                // The hot section only goes up to the cold section
                assert(fgFirstFuncletBB == nullptr);

                *ppEndLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(fgFirstColdBlock));
            }
            else
            {
                if (fgFirstFuncletBB != nullptr)
                {
                    *ppEndLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(fgFirstFuncletBB));
                }
                else
                {
                    *ppEndLoc = nullptr; // nullptr end location means the end of the code
                }
            }
        }
        else
        {
            assert(fgFirstFuncletBB == nullptr); // TODO-CQ: support hot/cold splitting in functions with EH
            assert(fgFirstColdBlock != nullptr); // There better be a cold section!

            *ppStartLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(fgFirstColdBlock));
            *ppEndLoc   = nullptr; // nullptr end location means the end of the code
        }
    }
    else
    {
        assert(getHotSectionData); // TODO-CQ: support funclets in cold section

        EHblkDsc* HBtab = ehGetDsc(func->funEHIndex);

        if (func->funKind == FUNC_FILTER)
        {
            assert(HBtab->HasFilter());
            *ppStartLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdFilter));
            *ppEndLoc   = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdHndBeg));
        }
        else
        {
            assert(func->funKind == FUNC_HANDLER);
            *ppStartLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdHndBeg));
            *ppEndLoc   = (HBtab->ebdHndLast->bbNext == nullptr)
                            ? nullptr
                            : new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdHndLast->bbNext));
        }
    }
}

#endif // FEATURE_EH_FUNCLETS

#if defined(_TARGET_AMD64_)

// See unwindAmd64.cpp

#elif defined(_TARGET_ARM64_)

// See unwindArm64.cpp

#elif defined(_TARGET_ARM_)

// See unwindArm.cpp

#elif defined(_TARGET_X86_)

// See unwindX86.cpp

#else // _TARGET_*

#error Unsupported or unset target architecture

#endif // _TARGET_*
