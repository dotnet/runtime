#ifndef __MONO_RAWBUFFER_H__
#define __MONO_RAWBUFFER_H__

void mono_raw_buffer_init (void);

void *mono_raw_buffer_load (int fd, int writable, guint32 base, size_t size);
void mono_raw_buffer_update (void *buffer, size_t size);
void  mono_raw_buffer_free (void *buffer);

#endif /* __MONO_RAWBUFFER_H__ */
