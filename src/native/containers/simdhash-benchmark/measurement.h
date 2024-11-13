// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

typedef void * (*setup_func) (void);
typedef void (*measurement_func) (void *data);

typedef struct {
    const char *name;
    setup_func setup;
    measurement_func func, teardown;
} measurement_info;
