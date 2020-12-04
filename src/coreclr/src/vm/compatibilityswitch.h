// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



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
    static FCDECL1(StringObject*, GetValue, StringObject *switchNameUNSAFE);
};


#endif

