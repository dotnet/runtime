// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _FLOATSINGLE_H_
#define _FLOATSINGLE_H_

#include <object.h>
#include <fcall.h>

class COMSingle {
public:
    FCDECL1(static float, Abs, float x);
};

#endif // _FLOATSINGLE_H_
