// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Insert just the #defines in winver.h, so that the
// C# compiler can include this file after macro preprocessing.
//

#ifdef __cplusplus
#ifndef FXVER_H_
#define FXVER_H_
#define INCLUDE_FXVER_H
#endif
#else
#define RC_INVOKED 1
#define INCLUDE_FXVER_H
#endif

#ifdef INCLUDE_FXVER_H
#undef INCLUDE_FXVER_H

#ifndef RC_INVOKED
#define FXVER_H_RC_INVOKED_ENABLED
#define RC_INVOKED 1
#endif

#include <verrsrc.h>

#ifdef FXVER_H_RC_INVOKED_ENABLED
#undef RC_INVOKED
#undef FXVER_H_RC_INVOKED_ENABLED
#endif

//
// Include the definitions for rmj, rmm, rup, rpt
//

#include <product_version.h>

/*
 * Product version, name and copyright
 */

#include "fxverstrings.h"

/*
 * File version, names, description.
 */

// FX_VER_INTERNALNAME_STR is passed in by the build environment.
#ifndef FX_VER_INTERNALNAME_STR
#define FX_VER_INTERNALNAME_STR     UNKNOWN_FILE
#endif

#define VER_INTERNALNAME_STR        QUOTE_MACRO(FX_VER_INTERNALNAME_STR)
#define VER_ORIGINALFILENAME_STR    QUOTE_MACRO(FX_VER_INTERNALNAME_STR)

// FX_VER_FILEDESCRIPTION_STR is defined in RC files that include fxver.h

#ifndef FX_VER_FILEDESCRIPTION_STR
#define FX_VER_FILEDESCRIPTION_STR  QUOTE_MACRO(FX_VER_INTERNALNAME_STR)
#endif

#define VER_FILEDESCRIPTION_STR     FX_VER_FILEDESCRIPTION_STR

#ifndef FX_VER_FILEVERSION_STR
#define FX_VER_FILEVERSION_STR      FX_FILEVERSION_STR
#endif

#define VER_FILEVERSION_STR         FX_VER_FILEVERSION_STR
#define VER_FILEVERSION_STR_L       VER_PRODUCTVERSION_STR_L

#ifndef FX_VER_FILEVERSION
#define FX_VER_FILEVERSION          VER_DOTFILEVERSION
#endif

#define VER_FILEVERSION             FX_VER_FILEVERSION

//URT_VFT passed in by the build environment.
#ifndef FX_VFT
#define FX_VFT VFT_UNKNOWN
#endif

#define VER_FILETYPE                FX_VFT
#define VER_FILESUBTYPE             VFT2_UNKNOWN

/* default is nodebug */
#if DBG
#define VER_DEBUG                   VS_FF_DEBUG
#else
#define VER_DEBUG                   0
#endif

#define VER_PRERELEASE              0

#define EXPORT_TAG 

// Not setting the private build flag until
// official builds can be detected from native projects
//#if OFFICIAL_BUILD
#define VER_PRIVATE                 0
//#else
//#define VER_PRIVATE                 VS_FF_PRIVATEBUILD
//#endif

#define VER_SPECIALBUILD            0

#define VER_FILEFLAGSMASK           VS_FFI_FILEFLAGSMASK
#define VER_FILEFLAGS               (VER_PRERELEASE|VER_DEBUG|VER_PRIVATE|VER_SPECIALBUILD)
#define VER_FILEOS                  VOS__WINDOWS32

#define VER_COMPANYNAME_STR         "Microsoft Corporation"

#ifdef VER_LANGNEUTRAL
#define VER_VERSION_UNICODE_LANG  "000004B0" /* LANG_NEUTRAL/SUBLANG_NEUTRAL, Unicode CP */
#define VER_VERSION_ANSI_LANG     "000004E4" /* LANG_NEUTRAL/SUBLANG_NEUTRAL, Ansi CP */
#define VER_VERSION_TRANSLATION   0x0000, 0x04B0
#else
#define VER_VERSION_UNICODE_LANG  "040904B0" /* LANG_ENGLISH/SUBLANG_ENGLISH_US, Unicode CP */
#define VER_VERSION_ANSI_LANG     "040904E4" /* LANG_ENGLISH/SUBLANG_ENGLISH_US, Ansi CP */
#define VER_VERSION_TRANSLATION   0x0409, 0x04B0
#endif

#if defined(CSC_INVOKED)
#define VER_COMMENTS_STR        "Flavor=" + QUOTE_MACRO(URTBLDENV_FRIENDLY)
#else
#define VER_COMMENTS_STR        "Flavor=" QUOTE_MACRO(URTBLDENV_FRIENDLY)
#endif

#if defined(__BUILDMACHINE__)
#if defined(__BUILDDATE__)
#define B2(x,y) " (" #x "." #y ")"
#define B1(x,y) B2(x, y)
#define BUILD_MACHINE_TAG B1(__BUILDMACHINE__, __BUILDDATE__)
#else
#define B2(x) " built by: " #x
#define B1(x) B2(x)
#define BUILD_MACHINE_TAG B1(__BUILDMACHINE__)
#endif
#if defined(__BUILDMACHINE_LEN__)
#if __BUILDMACHINE_LEN__ >= 25
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG
#elif __BUILDMACHINE_LEN__ == 24
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG " "
#elif __BUILDMACHINE_LEN__ == 23
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "  "
#elif __BUILDMACHINE_LEN__ == 22
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "   "
#elif __BUILDMACHINE_LEN__ == 21
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "    "
#elif __BUILDMACHINE_LEN__ == 20
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "     "
#elif __BUILDMACHINE_LEN__ == 19
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "      "
#elif __BUILDMACHINE_LEN__ == 18
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "       "
#elif __BUILDMACHINE_LEN__ == 17
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "        "
#elif __BUILDMACHINE_LEN__ == 16
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "         "
#elif __BUILDMACHINE_LEN__ == 15                       
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "          "
#elif __BUILDMACHINE_LEN__ == 14                               
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "           "
#elif __BUILDMACHINE_LEN__ == 13                                 
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "            "
#elif __BUILDMACHINE_LEN__ == 12                               
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "             "
#elif __BUILDMACHINE_LEN__ == 11                               
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "              "
#elif __BUILDMACHINE_LEN__ == 10                               
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "               "
#elif __BUILDMACHINE_LEN__ == 9                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                "
#elif __BUILDMACHINE_LEN__ == 8                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                 "
#elif __BUILDMACHINE_LEN__ == 7                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                  "
#elif __BUILDMACHINE_LEN__ == 6                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                   "
#elif __BUILDMACHINE_LEN__ == 5                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                    "
#elif __BUILDMACHINE_LEN__ == 4                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                     "
#elif __BUILDMACHINE_LEN__ == 3                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                      "
#elif __BUILDMACHINE_LEN__ == 2                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                       "
#elif __BUILDMACHINE_LEN__ == 1                                
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG "                        "
#else
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG
#endif
#else
#define BUILD_MACHINE_TAG_PADDED BUILD_MACHINE_TAG
#endif
#else
#define BUILD_MACHINE_TAG
#define BUILD_MACHINE_TAG_PADDED
#endif

#endif
