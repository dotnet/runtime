// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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
    static FCDECL2(StringObject*, GetValue, StringObject *switchNameUNSAFE, CLR_BOOL onlyDB);
};


#endif

