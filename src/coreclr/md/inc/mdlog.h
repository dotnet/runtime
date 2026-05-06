// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDLog.h - Meta data logging helper.
//

//
//*****************************************************************************
#ifndef __MDLog_h__
#define __MDLog_h__

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define LOGGING
#endif

#include <log.h>

#define LOGMD LF_METADATA, LL_INFO10000
#define LOG_MDCALL(func) LOG((LF_METADATA, LL_INFO10000, "MD: %s\n", #func))

#define MDSTR(str) ((str) ? str : W("<null>"))
#define MDSTRA(str) ((str) ? str : "<null>")

#endif // __MDLog_h__
