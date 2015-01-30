//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ============================================================
//
// Variables.hpp
//


//
// Defines the Variables class
//
// ============================================================

#ifndef __BINDER__VARIABLES_HPP__
#define __BINDER__VARIABLES_HPP__

#include "bindertypes.hpp"

namespace BINDER_SPACE
{
    class Variables
    {
    public:
        Variables();
        ~Variables();

        HRESULT Init();

        // ApplicationContext string constants
        SString AppBaseURL;
        SString DynamicDirectory;
        SString DynamicBaseDirectory;
        SString AppName;
        SString AppConfigFile;
        SString AppDomainId;

        // AssemblyBinder string constants
        SString httpURLPrefix;

        // AssemblyName string constants
        SString architectureMSIL;
        SString architectureX86;
        SString architectureAMD64;
        SString architectureARM;
        SString architectureARM64;
        SString cultureNeutral;
        SString emptyString;
        SString mscorlib;

#ifdef FEATURE_VERSIONING_LOG
        BOOL fLoggingNeeded;
        SString logPath;
#endif // FEATURE_VERSIONING_LOG
#ifndef DACCESS_COMPILE
#ifdef BINDER_DEBUG_LOG
        CRITSEC_COOKIE m_logCS;
        HandleHolder m_hDebugLogFile;
#endif // BINDER_DEBUG_LOG
#endif
    };

    extern Variables *g_BinderVariables;
};

#endif
