
void *mono_raw_buffer_load (int fd, int writable, int shared, guint32 base, size_t size);
void mono_raw_buffer_update (void *buffer, size_t size);
void  mono_raw_buffer_free (void *buffer);
