#ifndef __MONO_IO_PORTABILITY_H
#define __MONO_IO_PORTABILITY_H

#include <glib.h>
#include <mono/utils/mono-compiler.h>

enum {
        PORTABILITY_NONE        = 0x00,
        PORTABILITY_UNKNOWN     = 0x01,
        PORTABILITY_DRIVE       = 0x02,
        PORTABILITY_CASE        = 0x04,
};

void mono_portability_helpers_init (void) MONO_INTERNAL;
gchar *mono_portability_find_file (const gchar *pathname, gboolean last_exists) MONO_INTERNAL;

extern int __mono_io_portability_helpers MONO_INTERNAL;

#define IS_PORTABILITY_NONE (__mono_io_portability_helpers & PORTABILITY_NONE)
#define IS_PORTABILITY_UNKNOWN (__mono_io_portability_helpers & PORTABILITY_UNKNOWN)
#define IS_PORTABILITY_DRIVE (__mono_io_portability_helpers & PORTABILITY_DRIVE)
#define IS_PORTABILITY_CASE (__mono_io_portability_helpers & PORTABILITY_CASE)
#define IS_PORTABILITY_SET (__mono_io_portability_helpers > 0)

#endif
