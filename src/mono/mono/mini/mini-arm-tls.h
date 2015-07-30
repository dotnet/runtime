#ifndef __MONO_MINI_ARM_TLS_H__
#define __MONO_MINI_ARM_TLS_H__

/* Fast inlined tls getters/setters */

int mono_fast_get_tls_key (int);
void mono_fast_set_tls_key (int, int);

/* Fallback tls getters/setters */

int mono_fallback_get_tls_key (int);
void mono_fallback_set_tls_key (int, int);

/* End of thunks */

void mono_fast_get_tls_key_end (void);
void mono_fast_set_tls_key_end (void);

#endif
