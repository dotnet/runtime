// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_MEMORY_H
#define HAVE_MINIPAL_MEMORY_H

#include <minipal/utils.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

    bool minipal_initialize_flush_process_write_buffers(void);
    void minipal_flush_process_write_buffers(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_MEMORY_H
