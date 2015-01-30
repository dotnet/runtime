//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



#ifndef _COMPATIBILITYSWITCH_H_
#define _COMPATIBILITYSWITCH_H_

#include "object.h"
#include "typehandle.h"
#include "fcall.h"
#include "field.h"
#include "typectxt.h"

class CompatibilitySwitch
{
public:
    static FCDECL2(FC_BOOL_RET, IsEnabled, StringObject* switchNameUNSAFE, CLR_BOOL onlyDB);
    static FCDECL2(StringObject*, GetValue, StringObject *switchNameUNSAFE, CLR_BOOL onlyDB);
    static FCDECL0(StringObject*, GetAppContextOverrides);
};


#endif

