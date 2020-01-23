/**
 * \file
 */

#ifndef __MONO_IO_PORTABILITY_H
#define __MONO_IO_PORTABILITY_H

#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include "config.h"

enum {
        PORTABILITY_NONE        = 0x00,
        PORTABILITY_UNKNOWN     = 0x01,
        PORTABILITY_DRIVE       = 0x02,
        PORTABILITY_CASE        = 0x04
};

#ifdef DISABLE_PORTABILITY

#define mono_portability_helpers_init()
#define mono_portability_find_file(pathname,last_exists) NULL

#define IS_PORTABILITY_NONE FALSE
#define IS_PORTABILITY_UNKNOWN FALSE
#define IS_PORTABILITY_DRIVE FALSE
#define IS_PORTABILITY_CASE FALSE
#define IS_PORTABILITY_SET FALSE

#else

void mono_portability_helpers_init (void);
gchar *mono_portability_find_file (const gchar *pathname, gboolean last_exists);

extern int mono_io_portability_helpers;

#define IS_PORTABILITY_NONE (mono_io_portability_helpers & PORTABILITY_NONE)
#define IS_PORTABILITY_UNKNOWN (mono_io_portability_helpers & PORTABILITY_UNKNOWN)
#define IS_PORTABILITY_DRIVE (mono_io_portability_helpers & PORTABILITY_DRIVE)
#define IS_PORTABILITY_CASE (mono_io_portability_helpers & PORTABILITY_CASE)
#define IS_PORTABILITY_SET (mono_io_portability_helpers > 0)

#endif

#endif
