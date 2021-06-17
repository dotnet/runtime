// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "clrconfig.h"
#include "compatibilityswitch.h"

FCIMPL1(StringObject*, CompatibilitySwitch::GetValue, StringObject* switchNameUNSAFE) {
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
    info.options = CLRConfig::LookupOptions::Default;
    LPWSTR strVal = CLRConfig::GetConfigValue(info);
    refName = StringObject::NewString(strVal);
    HELPER_METHOD_FRAME_END();

    return (StringObject*)OBJECTREFToObject(refName);
}
FCIMPLEND
