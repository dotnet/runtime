#ifndef __MONO_RAWBUFFER_H__
#define __MONO_RAWBUFFER_H__

#include "mono/utils/mono-compiler.h"

void mono_raw_buffer_init (void) MONO_INTERNAL;
void mono_raw_buffer_cleanup (void) MONO_INTERNAL;

void *mono_raw_buffer_load (int fd, int writable, guint32 base, size_t size) MONO_INTERNAL;
void mono_raw_buffer_update (void *buffer, size_t size) MONO_INTERNAL;
void  mono_raw_buffer_free (void *buffer) MONO_INTERNAL;

void mono_raw_buffer_set_make_unreadable (gboolean unreadable) MONO_INTERNAL;
gboolean mono_raw_buffer_is_pagefault (void *ptr) MONO_INTERNAL;
void mono_raw_buffer_handle_pagefault (void *ptr) MONO_INTERNAL;
guint32 mono_raw_buffer_get_n_pagefaults (void) MONO_INTERNAL;

#endif /* __MONO_RAWBUFFER_H__ */
