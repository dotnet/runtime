#ifndef _MONO_STRENC_INTERNALS_H_
#define _MONO_STRENC_INTERNALS_H_

#include <glib.h>
#include <mono/utils/mono-error.h>

gchar *mono_unicode_to_external_checked (const gunichar2 *uni, MonoError *err);

#endif /* _MONO_STRENC_INTERNALS_H_ */
