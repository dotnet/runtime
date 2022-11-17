// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COMTHREADPOOL_H
#define _COMTHREADPOOL_H

class ThreadPoolNative
{
public:
    static FCDECL4(INT32, GetNextConfigUInt32Value,
        INT32 configVariableIndex,
        UINT32 *configValueRef,
        BOOL *isBooleanRef,
        LPCWSTR *appContextConfigNameRef);
};

#endif
