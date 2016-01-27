// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "clrconfig.h"
#include "compatibilityswitch.h"

FCIMPL2(FC_BOOL_RET, CompatibilitySwitch::IsEnabled, StringObject* switchNameUNSAFE, CLR_BOOL onlyDB)
{
    FCALL_CONTRACT;

    if (!switchNameUNSAFE)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidSwitchName"));

    BOOL result = TRUE;

    STRINGREF name = (STRINGREF) switchNameUNSAFE;
    VALIDATEOBJECTREF(name);

    HELPER_METHOD_FRAME_BEGIN_RET_1(name);

    CLRConfig::ConfigDWORDInfo info;
    info.name = name->GetBuffer();
    if(onlyDB)
    {
        // for public managed apis we ignore checking in registry/config/env
        // only check in windows appcompat DB
        info.options = CLRConfig::IgnoreEnv | 
                       CLRConfig::IgnoreHKLM |
                       CLRConfig::IgnoreHKCU |
                       CLRConfig::IgnoreConfigFiles; 
    }
    else
    {
        // for mscorlib (i.e. which use internal apis) also check in 
        // registry/config/env in addition to windows appcompat DB
        info.options = CLRConfig::EEConfig_default;
    }

    // default value is disabled
    info.defaultValue = 0;
    result = CLRConfig::IsConfigEnabled(info);
    HELPER_METHOD_FRAME_END();
   
    FC_RETURN_BOOL(result);
}
FCIMPLEND


FCIMPL2(StringObject*, CompatibilitySwitch::GetValue, StringObject* switchNameUNSAFE, CLR_BOOL onlyDB) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
        
    if (!switchNameUNSAFE)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidSwitchName"));

    STRINGREF name = (STRINGREF) switchNameUNSAFE;
    VALIDATEOBJECTREF(name);
        
    STRINGREF refName = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(name);
    CLRConfig::ConfigStringInfo info;
    info.name = name->GetBuffer();
    if(onlyDB)
    {
        // for public managed apis we ignore checking in registry/config/env
        // only check in windows appcompat DB
        info.options = CLRConfig::IgnoreEnv | 
                       CLRConfig::IgnoreHKLM |
                       CLRConfig::IgnoreHKCU |
                       CLRConfig::IgnoreConfigFiles; 
    }
    else
    {
        // for mscorlib (i.e. which use internal apis) also check in 
        // registry/config/env in addition to windows appcompat DB
        info.options = CLRConfig::EEConfig_default;
    }
    LPWSTR strVal = CLRConfig::GetConfigValue(info);
    refName = StringObject::NewString(strVal);
    HELPER_METHOD_FRAME_END();            
    
    return (StringObject*)OBJECTREFToObject(refName);
}
FCIMPLEND

FCIMPL0(StringObject*, CompatibilitySwitch::GetAppContextOverrides) {
    CONTRACTL{
        FCALL_CHECK;
    }
    CONTRACTL_END;

    STRINGREF refName = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();
    LPWSTR strVal = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AppContextSwitchOverrides);
    refName = StringObject::NewString(strVal);
    HELPER_METHOD_FRAME_END();

    return (StringObject*)OBJECTREFToObject(refName);
}
FCIMPLEND
