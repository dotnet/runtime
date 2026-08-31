#ifndef __MONO_BITUTILS_H__
#define __MONO_BITUTILS_H__

#include <glib.h>

int mono_lzcnt32 (guint32 x);
int mono_lzcnt64 (guint64 x);
int mono_tzcnt32 (guint32 x);
int mono_tzcnt64 (guint64 x);

#endif