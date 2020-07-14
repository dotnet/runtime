// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * Constants for passing to the first parameter of syslog.
 * These are a combination of flags where the lower bits are
 * the priority and the higher bits are the facility. The lower
 * bits cannot be OR'd together; they must be OR'd with the higer bits.
 *
 * These values keep their original definition and are taken from syslog.h
 */
typedef enum
{
    // Priorities
    PAL_LOG_EMERG = 0,   /* system is unusable */
    PAL_LOG_ALERT = 1,   /* action must be taken immediately */
    PAL_LOG_CRIT = 2,    /* critical conditions */
    PAL_LOG_ERR = 3,     /* error conditions */
    PAL_LOG_WARNING = 4, /* warning conditions */
    PAL_LOG_NOTICE = 5,  /* normal but significant condition */
    PAL_LOG_INFO = 6,    /* informational */
    PAL_LOG_DEBUG = 7,   /* debug-level messages */
} SysLogPriority;

/**
 * Write a message to the system logger, which in turn writes the message to the system console, log files, etc.
 * See man 3 syslog for more info
 */
PALEXPORT void SystemNative_SysLog(SysLogPriority priority, const char* message, const char* arg1);
