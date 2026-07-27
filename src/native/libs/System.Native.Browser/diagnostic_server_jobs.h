// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stddef.h>

typedef size_t (*ds_job_cb)(void *data);

void SystemJS_DiagnosticServerQueueJob (ds_job_cb cb, void *data);
void SystemJS_ExecuteDiagnosticServerCallback (void);

