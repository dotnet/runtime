/**
 * \file
 */

#ifndef __UTILS_MONO_STRING_H__
#define __UTILS_MONO_STRING_H__
#include <glib.h>
/*
 * This definition is used to we remember later to implement this properly
 *
 * Currently we merely call into the ascii comparison, but we should be
 * instead doing case folding and comparing the result.
 */
#define mono_utf8_strcasecmp g_ascii_strcasecmp

#endif /* __UTILS_MONO_STRING_H__ */
