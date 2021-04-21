// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: Debug_MetaData.h
//

//
// This file defines special macros for debugging MetaData (even in special retail builds).
// The level of debugging is set by these (input) macros:
//  * code:#_DEBUG_METADATA
//  * code:#_DEBUG_MDSCHEMA
//
//
//  #_DEBUG_METADATA
//  _DEBUG_METADATA ... Enables debugging information in MetaData implementation. It's useful for debugging
//      retail builds in MetaData (when using CHK build is too slow).
//      Note: Enabled by default if _DEBUG is defined (see code:#DefaultSetting_DEBUG_METADATA), can be
//      enabled externally/explicitly also in retail builds (without _DEBUG defined).
//
//      Defines macros (see code:#Macros_DEBUG_METADATA):
//        * code:#INDEBUG_MD
//        * code:#COMMA_INDEBUG_MD
//        * code:#INDEBUG_MD_COMMA
//
//  #_DEBUG_MDSCHEMA
//  _DEBUG_MDSCHEMA ... Enables additional debugging of MetaData schema.
//      Note: Allowed to be enabled only if _DEBUG is defined (see code:#Check_DEBUG_MDSCHEMA).
//
//      Defines macros (see code:#Macros_DEBUG_MDSCHEMA):
//        * code:#_ASSERTE_MDSCHEMA
//
// ======================================================================================

#pragma once

// Include for CLRConfig class used in Debug_ReportError
#include <utilcode.h>

// --------------------------------------------------------------------------------------
//#DefaultSetting_DEBUG_METADATA
//
// Enable _DEBUG_METADATA by default if _DEBUG is defined (code:#_DEBUG_METADATA).
//
#ifdef _DEBUG
    #define _DEBUG_METADATA
#endif //_DEBUG

// --------------------------------------------------------------------------------------
//#Macros_DEBUG_METADATA
//
// Define macros for MetaData implementation debugging (see code:#_DEBUG_METADATA).
//
#ifdef _DEBUG_METADATA
    //#INDEBUG_MD
    #define INDEBUG_MD(expr)       expr
    //#COMMA_INDEBUG_MD
    #define COMMA_INDEBUG_MD(expr) , expr
    //#INDEBUG_MD_COMMA
    #define INDEBUG_MD_COMMA(expr) expr,

    #define Debug_ReportError(strMessage)                                                           \
        do {                                                                                        \
            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AssertOnBadImageFormat))    \
            { _ASSERTE_MSG(FALSE, (strMessage)); }                                                  \
        } while(0)
    #define Debug_ReportInternalError(strMessage) _ASSERTE_MSG(FALSE, (strMessage))
#else //!_DEBUG_METADATA
    #define INDEBUG_MD(expr)
    #define COMMA_INDEBUG_MD(expr)
    #define INDEBUG_MD_COMMA(expr)

    #define Debug_ReportError(strMessage)
    #define Debug_ReportInternalError(strMessage) _ASSERTE(!(strMessage))
#endif //!_DEBUG_METADATA

// --------------------------------------------------------------------------------------
//#Check_DEBUG_MDSCHEMA
//
// Check that _DEBUG_MDSCHEMA is defined only if _DEBUG is defined (see code:#_DEBUG_MDSCHEMA).
//
#ifdef _DEBUG_MDSCHEMA
    #ifndef _DEBUG
        #error _DEBUG_MDSCHEMA is defined while _DEBUG is not defined.
    #endif //!_DEBUG
#endif //_DEBUG_MDSCHEMA

// --------------------------------------------------------------------------------------
//#Macros_DEBUG_MDSCHEMA
//
// Define macros for MetaData schema debugging (see code:#_DEBUG_MDSCHEMA).
//
#ifdef _DEBUG_MDSCHEMA
    //#_ASSERTE_MDSCHEMA
    // This assert is useful only to catch errors in schema (tables and columns) definitions. It is useful e.g.
    // for verifying consistency between table record classes (e.g. code:MethodDefRecord) and columns'
    // offsets/sizes as defined in code:ColumnDefinition.
    #define _ASSERTE_MDSCHEMA(expr) _ASSERTE(expr)
#else //!_DEBUG_MDSCHEMA
    #define _ASSERTE_MDSCHEMA(expr)
#endif //!_DEBUG_MDSCHEMA
