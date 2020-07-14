// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_logger.h"
#include <syslog.h>

void SystemNative_SysLog(SysLogPriority priority, const char* message, const char* arg1)
{
    syslog((int)(LOG_USER | priority), message, arg1);
}
