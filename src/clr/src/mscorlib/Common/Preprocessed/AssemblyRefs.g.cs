// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\assemblyrefs.cspp"

/*
 * Assembly attributes. This file is preprocessed to generate a .cs file
 * with the correct information.  The original lives in VBL\Tools\DevDiv\
 */

using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"
/**
 * Version strings for Frameworks.
 * 
 */

//
// Insert just the #defines in winver.h, so that the
// C# compiler can include this file after macro preprocessing.
//









// #line 21 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"




//
// winver.h is bad for rc.exe & csc.exe whereas verrsrc.h does not have an include guard
// yet defines the same structure twice if RC_INVOKED is not #defined.
// Temporarily enable RC_INVOKED protection around the #include.
//






// #line 1 "c:\\program files (x86)\\windows kits\\8.1\\include\\um\\verrsrc.h"
// #line 1 "c:\\program files (x86)\\windows kits\\8.1\\include\\shared\\winapifamily.h"
/*



Module Name:

    winapifamily.h

Abstract:

    Master include file for API family partitioning.

*/





// #pragma once
// #line 21 "c:\\program files (x86)\\windows kits\\8.1\\include\\shared\\winapifamily.h"

/*
 * When compiling C and C++ code using SDK header files, the development
 * environment can specify a target platform by #define-ing the
 * pre-processor symbol WINAPI_FAMILY to one of the following values.
 * Each FAMILY value denotes an application family for which a different
 * subset of the total set of header-file-defined APIs are available.
 * Setting the WINAPI_FAMILY value will effectively hide from the
 * editing and compilation environments the existence of APIs that
 * are not applicable to the family of applications targeting a
 * specific platform.
 */

/*
 * The WINAPI_FAMILY values of 0 and 1 are reserved to ensure that
 * an error will occur if WINAPI_FAMILY is set to any
 * WINAPI_PARTITION value (which must be 0 or 1, see below).
 */



/* The value of WINAPI_FAMILY_DESKTOP_APP may change in future SDKs. */
/* Additional WINAPI_FAMILY values may be defined in future SDKs. */

/*
 * For compatibility with Windows 8 header files, the following
 * synonym for WINAPI_FAMILY_PC_APP is temporarily #define'd.
 * Use of this symbol should be considered deprecated.
 */


/*
 * If no WINAPI_FAMILY value is specified, then all APIs available to
 * Windows desktop applications are exposed.
 */


// #line 59 "c:\\program files (x86)\\windows kits\\8.1\\include\\shared\\winapifamily.h"

/*
 * API PARTITONs are part of an indirection mechanism for mapping between
 * individual APIs and the FAMILYs to which they apply.
 * Each PARTITION is a category or subset of named APIs.  PARTITIONs
 * are permitted to have overlapping membership -- some single API
 * might be part of more than one PARTITION.  In support of new
 * FAMILYs that might be added in future SDKs, any single current
 * PARTITION might in that future SDK be split into two or more new PARTITIONs.
 * Accordingly, application developers should avoid taking dependencies on
 * PARTITION names; developers' only dependency upon the symbols defined
 * in this file should be their reliance on the WINAPI_FAMILY names and values.
 */

/*
 * Current PARTITIONS are each #undef'ed below, and then will be #define-ed
 * to be either 1 or 0 or depending on the active WINAPI_FAMILY.
 */







/*
 * The mapping between families and partitions is summarized here.
 * An X indicates that the given partition is active for the given
 * platform/family.
 *
 *                             +---------------+
 *                             |  *Partition*  |
 *                             +---+---+---+---+
 *                             |   |   |   | P |
 *                             |   |   |   | H |
 *                             | D |   |   | O |
 *                             | E |   | P | N |
 *                             | S |   | C | E |
 *                             | K |   | _ | _ |
 *                             | T | A | A | A |
 * +-------------------------+-+ O | P | P | P |
 * |     *Platform/Family*    \| P | P | P | P |
 * +---------------------------+---+---+---+---+
 * | WINAPI_FAMILY_DESKTOP_APP | X | X | X |   |
 * +---------------------------+---+---+---+---+
 * |      WINAPI_FAMILY_PC_APP |   | X | X |   |
 * +---------------------------+---+---+---+---+
 * |   WINAPI_FAMILY_PHONE_APP |   | X |   | X |
 * +---------------------------+---+---+---+---+
 *
 * The table above is encoded in the following expressions,
 * each of which evaluates to 1 or 0.
 *
 * Whenever a new family is added, all of these expressions
 * need to be reconsidered.
 */


// #line 118 "c:\\program files (x86)\\windows kits\\8.1\\include\\shared\\winapifamily.h"





/*
 * For compatibility with Windows Phone 8 header files, the following
 * synonym for WINAPI_PARTITION_PHONE_APP is temporarily #define'd.
 * Use of this symbol should be regarded as deprecated.
 */


/*
 * Header files use the WINAPI_FAMILY_PARTITION macro to assign one or
 * more declarations to some group of partitions.  The macro chooses
 * whether the preprocessor will emit or omit a sequence of declarations
 * bracketed by an #if/#endif pair.  All header file references to the
 * WINAPI_PARTITION_* values should be in the form of occurrences of
 * WINAPI_FAMILY_PARTITION(...).
 *
 * For example, the following usage of WINAPI_FAMILY_PARTITION identifies
 * a sequence of declarations that are part of both the Windows Desktop
 * Partition and the Windows-Phone-Specific Store Partition:
 *
 *     #if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP | WINAPI_PARTITION_PHONE_APP)
 *     ...
 *     #endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP | WINAPI_PARTITION_PHONE_APP)
 *
 * The comment on the closing #endif allow tools as well as people to find the
 * matching #ifdef properly.
 *
 * Usages of WINAPI_FAMILY_PARTITION may be combined, when the partitition definitions are
 * related.  In particular one might use declarations like
 * 
 *     #if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_APP) && !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
 *
 * or
 *
 *     #if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_APP) && !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_PHONE_APP)
 *
 * Direct references to WINAPI_PARTITION_ values (eg #if !WINAPI_FAMILY_PARTITION_...)
 * should not be used.
 */ 


/*
 * Macro used to #define or typedef a symbol used for selective deprecation
 * of individual methods of a COM interfaces that are otherwise available
 * for a given set of partitions.
 */


/*
 * For compatibility with Windows 8 header files, the following
 * symbol is temporarily conditionally #define'd.  Additional symbols
 * like this should be not defined in winapifamily.h, but rather should be
 * introduced locally to the header files of the component that needs them.
 */


// #line 179 "c:\\program files (x86)\\windows kits\\8.1\\include\\shared\\winapifamily.h"

// #line 181 "c:\\program files (x86)\\windows kits\\8.1\\include\\shared\\winapifamily.h"
// #line 2 "c:\\program files (x86)\\windows kits\\8.1\\include\\um\\verrsrc.h"

/****************************************************************************
**                                                                             *
* verrsrc.h -   Version Resource definitions                                  *
*                                                                             *
*               Include file declaring version resources in rc files          *
*                                                                             *
*               *
*                                                                             *
\*****************************************************************************/

// #pragma region Application Family


/* ----- Symbols ----- */




/* ----- VS_VERSION.dwFileFlags ----- */




// #line 27 "c:\\program files (x86)\\windows kits\\8.1\\include\\um\\verrsrc.h"



/* ----- VS_VERSION.dwFileFlags ----- */







/* ----- VS_VERSION.dwFileOS ----- */



















/* ----- VS_VERSION.dwFileType ----- */








/* ----- VS_VERSION.dwFileSubtype for VFT_WINDOWS_DRV ----- */














/* ----- VS_VERSION.dwFileSubtype for VFT_WINDOWS_FONT ----- */




// #line 88 "c:\\program files (x86)\\windows kits\\8.1\\include\\um\\verrsrc.h"
// // #pragma region 

// #pragma region Desktop Family


/* ----- VerFindFile() flags ----- */






/* ----- VerInstallFile() flags ----- */





































































// #line 171 "c:\\program files (x86)\\windows kits\\8.1\\include\\um\\verrsrc.h"
// #pragma region 

// #line 37 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"






//
// Include the definitions for rmj, rmm, rup, rpt
//

// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"




// #line 6 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"

// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\version.h"
// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\ndpversion_generated.h"




// #line 6 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\ndpversion_generated.h"





// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\buildnumber.h"




// #line 6 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\buildnumber.h"

























// #line 12 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\ndpversion_generated.h"
// #line 2 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\version.h"












// #line 8 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"



// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\asm_version.h"











// #line 13 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\asm_version.h"





// #line 19 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\asm_version.h"

// #line 12 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"
// #line 13 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"



















































































































































// #line 161 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"





// #line 167 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"





// #line 173 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"






// #line 180 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\product_version.h"

// #line 48 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

/*
 * Product version, name and copyright
 */

// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"

    
        

// #line 6 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"
            
        

// #line 10 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"
    



// #line 15 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"
// #line 16 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"

// The following copyright is intended for display in the Windows Explorer property box for a DLL or EXE
// See \\lca\pdm\TMGUIDE\Copyright\Crt_Tmk_Notices.xls for copyright guidelines
//

    
        

// #line 25 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"
            
        

// #line 29 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"
    






// #line 37 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"
// #line 38 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"

// VSWhidbey #495749
// Note: The following legal copyright is intended for situations where the copyright symbol doesn't display 
//       properly.  For example, the following copyright should be displayed as part of the logo for DOS command-line 
//       applications.  If you change the format or wording of the following copyright, you should apply the same
//       change to fxresstrings.txt (for managed applications).

    
    
// #line 48 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxverstrings.h"
// #line 54 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

/*
 * File version, names, description.
 */

// FX_VER_INTERNALNAME_STR is passed in by the build environment.







// FX_VER_FILEDESCRIPTION_STR is defined in RC files that include fxver.h



// #line 72 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"





// #line 78 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"






// #line 85 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"



//URT_VFT passed in by the build environment.







/* default is nodebug */




// #line 102 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// DEBUG flag is set for debug build, not set for retail build
// #if defined(DEBUG) || defined(_DEBUG)
// #define VER_DEBUG        VS_FF_DEBUG
// #else  // DEBUG
// #define VER_DEBUG        0
// #endif // DEBUG


/* default is prerelease */


// #line 115 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// #line 117 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// PRERELEASE flag is always set unless building SHIP version
// #ifndef _SHIP
// #define VER_PRERELEASE   VS_FF_PRERELEASE
// #else
// #define VER_PRERELEASE   0
// #endif // _SHIP



// #line 128 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// #line 130 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// PRIVATEBUILD flag is set if not built by build lab
// #ifndef _VSBUILD
// #define VER_PRIVATEBUILD VS_FF_PRIVATEBUILD
// #else  // _VSBUILD
// #define VER_PRIVATEBUILD 0
// #endif // _VSBUILD



// #line 141 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// #line 143 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"




















// #line 164 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"









// #line 174 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"





// #line 180 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"








// #line 189 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"



// #line 193 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"






















































// #line 248 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// #line 250 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"



// #line 254 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"

// #line 256 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\include\\fxver.h"
// #line 24 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\assemblyrefs.cspp"

internal static class FXAssembly {
    internal const string Version = "4.0.0.0";
}















// #line 44 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\assemblyrefs.cspp"

internal static class ThisAssembly {
    internal const string Title = "mscorlib.dll";
    internal const string Description = "mscorlib.dll";
    internal const string DefaultAlias = "mscorlib.dll";
    internal const string Copyright = "\u00A9 Microsoft Corporation.  All rights reserved.";
    internal const string Version = "4.0.0.0";
    internal const string InformationalVersion = "4.0.22306.0";
    internal const string DailyBuildNumberStr = "22306";
    internal const string BuildRevisionStr = "0";
    internal const int DailyBuildNumber = 22306;
}


#pragma warning disable 436
internal static class AssemblyRef {
    internal const string EcmaPublicKey                              = "b77a5c561934e089";
    internal const string EcmaPublicKeyToken                         = "b77a5c561934e089";
    internal const string EcmaPublicKeyFull                          = "00000000000000000400000000000000";

    internal const string SilverlightPublicKey                       = "31bf3856ad364e35";
    internal const string SilverlightPublicKeyToken                  = "31bf3856ad364e35";
    internal const string SilverlightPublicKeyFull                   = "0024000004800000940000000602000000240000525341310004000001000100B5FC90E7027F67871E773A8FDE8938C81DD402BA65B9201D60593E96C492651E889CC13F1415EBB53FAC1131AE0BD333C5EE6021672D9718EA31A8AEBD0DA0072F25D87DBA6FC90FFD598ED4DA35E44C398C454307E8E33B8426143DAEC9F596836F97C8F74750E5975C64E2189F45DEF46B2A2B1247ADC3652BF5C308055DA9";

    internal const string SilverlightPlatformPublicKey               = "7cec85d7bea7798e";
    internal const string SilverlightPlatformPublicKeyToken          = "7cec85d7bea7798e";
    internal const string SilverlightPlatformPublicKeyFull           = "00240000048000009400000006020000002400005253413100040000010001008D56C76F9E8649383049F383C44BE0EC204181822A6C31CF5EB7EF486944D032188EA1D3920763712CCB12D75FB77E9811149E6148E5D32FBAAB37611C1878DDC19E20EF135D0CB2CFF2BFEC3D115810C3D9069638FE4BE215DBF795861920E5AB6F7DB2E2CEEF136AC23D5DD2BF031700AEC232F6C6B1C785B4305C123B37AB";


    internal const string PlatformPublicKey                          = SilverlightPlatformPublicKey;
    internal const string PlatformPublicKeyToken                     = SilverlightPlatformPublicKeyToken;
    internal const string PlatformPublicKeyFull                      = SilverlightPlatformPublicKeyFull;




// #line 81 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\assemblyrefs.cspp"

    internal const string Mscorlib                                   = "mscorlib, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + PlatformPublicKey;
    internal const string SystemData                                 = "System.Data, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    internal const string SystemDataOracleClient                     = "System.Data.OracleClient, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    internal const string System                                     = "System, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + PlatformPublicKey;
    internal const string SystemCore                                 = "System.Core, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + PlatformPublicKey;
    internal const string SystemNumerics                             = "System.Numerics, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + PlatformPublicKey;
    internal const string SystemRuntimeRemoting                      = "System.Runtime.Remoting, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    internal const string SystemThreadingTasksDataflow               = "System.Threading.Tasks.Dataflow, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + PlatformPublicKey;
    internal const string SystemWindowsForms                         = "System.Windows.Forms, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    internal const string SystemXml                                  = "System.Xml, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    
    internal const string MicrosoftPublicKey                         = "b03f5f7f11d50a3a";
    internal const string MicrosoftPublicKeyToken                    = "b03f5f7f11d50a3a";
    internal const string MicrosoftPublicKeyFull                     = "002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293";

    internal const string SharedLibPublicKey                         = "31bf3856ad364e35";
    internal const string SharedLibPublicKeyToken                    = "31bf3856ad364e35";
    internal const string SharedLibPublicKeyFull                     = "0024000004800000940000000602000000240000525341310004000001000100B5FC90E7027F67871E773A8FDE8938C81DD402BA65B9201D60593E96C492651E889CC13F1415EBB53FAC1131AE0BD333C5EE6021672D9718EA31A8AEBD0DA0072F25D87DBA6FC90FFD598ED4DA35E44C398C454307E8E33B8426143DAEC9F596836F97C8F74750E5975C64E2189F45DEF46B2A2B1247ADC3652BF5C308055DA9";

    internal const string SystemComponentModelDataAnnotations        = "System.ComponentModel.DataAnnotations, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemConfiguration                        = "System.Configuration, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemConfigurationInstall                 = "System.Configuration.Install, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemDeployment                           = "System.Deployment, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemDesign                               = "System.Design, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemDirectoryServices                    = "System.DirectoryServices, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemDrawingDesign                        = "System.Drawing.Design, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemDrawing                              = "System.Drawing, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemEnterpriseServices                   = "System.EnterpriseServices, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemManagement                           = "System.Management, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemMessaging                            = "System.Messaging, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemNetHttp                              = "System.Net.Http, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemNetHttpWebRequest                    = "System.Net.Http.WebRequest, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemRuntimeSerializationFormattersSoap   = "System.Runtime.Serialization.Formatters.Soap, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemRuntimeWindowsRuntime                = "System.Runtime.WindowsRuntime, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    internal const string SystemRuntimeWindowsRuntimeUIXaml          = "System.Runtime.WindowsRuntimeUIXaml, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    internal const string SystemSecurity                             = "System.Security, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemServiceModelWeb                      = "System.ServiceModel.Web, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemServiceProcess                       = "System.ServiceProcess, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemWeb                                  = "System.Web, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemWebAbstractions                      = "System.Web.Abstractions, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemWebDynamicData                       = "System.Web.DynamicData, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemWebDynamicDataDesign                 = "System.Web.DynamicData.Design, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemWebEntityDesign                      = "System.Web.Entity.Design, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
    internal const string SystemWebExtensions                        = "System.Web.Extensions, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemWebExtensionsDesign                  = "System.Web.Extensions.Design, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemWebMobile                            = "System.Web.Mobile, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemWebRegularExpressions                = "System.Web.RegularExpressions, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string SystemWebRouting                           = "System.Web.Routing, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    internal const string SystemWebServices                          = "System.Web.Services, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    
    internal const string WindowsBase                                = "WindowsBase, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + SharedLibPublicKey;
    
    internal const string MicrosoftVisualStudio                      = "Microsoft.VisualStudio, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string MicrosoftVisualStudioWindowsForms          = "Microsoft.VisualStudio.Windows.Forms, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string VJSharpCodeProvider                        = "VJSharpCodeProvider, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;

    internal const string ASPBrowserCapsPublicKey                    = "b7bd7678b977bd8f";
    internal const string ASPBrowserCapsFactory                      = "ASP.BrowserCapsFactory, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + ASPBrowserCapsPublicKey;


    // We do not want these sitting in non-VS specific files.  If you need them,
    // add this line to sources:  
    // C_DEFINES=$(C_DEFINES) /DINCLUDE_VSREFS
    //
    // M.VS.dll and M.VSDesigner.dll should also be included here, but it
    // turns out that everyone, from Data, to XSP to Winforms to diagnostics
    // has thrown some designer-specific code into M.VS.dll or M.VSDesigner.dll.
    //
    





// #line 157 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\assemblyrefs.cspp"

    // VS Provided Assemblies... we can't strong bind to these, they 
    // update their assembly versions too often
    //
    internal const string MicrosoftVSDesigner                        = "Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string MicrosoftVisualStudioWeb                   = "Microsoft.VisualStudio.Web, Version=10.0.0.0, Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string MicrosoftWebDesign                         = "Microsoft.Web.Design.Client, Version=10.0.0.0, Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string MicrosoftVSDesignerMobile                  = "Microsoft.VSDesigner.Mobile, Version=8.0.0.0, Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    internal const string MicrosoftJScript                           = "Microsoft.JScript, Version=8.0.0.0, Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    
    //internal const string MicrosoftVSDesigner                        = "Microsoft.VSDesigner, Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;
    //internal const string MicrosoftJScript                           = "Microsoft.JScript, Culture=neutral, PublicKeyToken=" + MicrosoftPublicKey;   
}
#pragma warning restore 436
// #line 172 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\assemblyrefs.cspp"
