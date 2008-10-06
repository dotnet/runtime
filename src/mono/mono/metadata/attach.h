#ifndef __MONO_ATTACH_H__
#define __MONO_ATTACH_H__

G_BEGIN_DECLS

void
mono_attach_parse_options (char *options) MONO_INTERNAL;

void
mono_attach_init (void) MONO_INTERNAL;

gboolean
mono_attach_start (void) MONO_INTERNAL;

void
mono_attach_maybe_start (void) MONO_INTERNAL;

void
mono_attach_cleanup (void) MONO_INTERNAL;

G_END_DECLS

#endif
