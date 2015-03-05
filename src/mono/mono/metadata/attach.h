#ifndef __MONO_ATTACH_H__
#define __MONO_ATTACH_H__

#include <glib.h>
#include <mono/utils/mono-compiler.h>

G_BEGIN_DECLS

void
mono_attach_parse_options (char *options);

void
mono_attach_init (void);

gboolean
mono_attach_start (void);

void
mono_attach_maybe_start (void);

void
mono_attach_cleanup (void);

G_END_DECLS

#endif
