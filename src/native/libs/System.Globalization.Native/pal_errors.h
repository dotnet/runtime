// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

/*
* These values should be kept in sync with
* Interop.GlobalizationInterop.ResultCode
*/
typedef enum
{
    Success = 0,
    UnknownError = 1,
    InsufficientBuffer = 2,
    OutOfMemory = 3
} ResultCode;
