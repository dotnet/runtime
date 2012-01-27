/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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
#ifndef __MONO_SGEN_SSB_H__
#define __MONO_SGEN_SSB_H__


void mono_sgen_ssb_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value) MONO_INTERNAL;
void mono_sgen_ssb_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value) MONO_INTERNAL;
void mono_sgen_ssb_wbarrier_arrayref_copy (gpointer dest_ptr, gpointer src_ptr, int count) MONO_INTERNAL;
void mono_sgen_ssb_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass) MONO_INTERNAL;
void mono_sgen_ssb_wbarrier_object_copy (MonoObject* obj, MonoObject *src) MONO_INTERNAL;
void mono_sgen_ssb_wbarrier_generic_nostore (gpointer ptr) MONO_INTERNAL;

void mono_sgen_ssb_cleanup_thread (SgenThreadInfo *p) MONO_INTERNAL;

#endif

