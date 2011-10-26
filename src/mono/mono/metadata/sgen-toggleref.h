/*
 *
 * sgen-toggleref.h: toggleref support for sgen
 *
 * Copyright 2011 Xamarin, Inc.
 *
 * Author:
 *  Rodrigo Kumpera (kumpera@gmail.com)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#ifndef _MONO_SGEN_TOGGLEREF_H_
#define _MONO_SGEN_TOGGLEREF_H_

#include <mono/utils/mono-publib.h>

MONO_BEGIN_DECLS

/* GC toggle ref support */

typedef enum {
	MONO_TOGGLE_REF_DROP,
	MONO_TOGGLE_REF_STRONG,
	MONO_TOGGLE_REF_WEAK
} MonoToggleRefStatus;

void mono_gc_toggleref_register_callback (MonoToggleRefStatus (*proccess_toggleref) (MonoObject *obj));
void mono_gc_toggleref_add (MonoObject *object, mono_bool strong_ref);


MONO_END_DECLS

#endif
