#ifndef __MONO_RAWBUFFER_H__
#define __MONO_RAWBUFFER_H__

void mono_raw_buffer_init (void);

void *mono_raw_buffer_load (int fd, int writable, guint32 base, size_t size);
void mono_raw_buffer_update (void *buffer, size_t size);
void  mono_raw_buffer_free (void *buffer);

void mono_raw_buffer_set_make_unreadable (gboolean unreadable);
gboolean mono_raw_buffer_is_pagefault (void *ptr);
void mono_raw_buffer_handle_pagefault (void *ptr);
guint32 mono_raw_buffer_get_n_pagefaults (void);

#endif /* __MONO_RAWBUFFER_H__ */
