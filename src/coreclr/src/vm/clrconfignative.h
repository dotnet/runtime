// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _CLRCONFIGNATIVE_H_
#define _CLRCONFIGNATIVE_H_

class ClrConfigNative
{
public:
    static BOOL QCALLTYPE GetConfigBoolValue(LPCWSTR name, BOOL *exist);
};

#endif // _CLRCONFIGNATIVE_H_
