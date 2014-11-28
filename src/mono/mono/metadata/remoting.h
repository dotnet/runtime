/*
 * remoting.h: Remoting support
 *
 * (C) 2014 Xamarin, Inc.  http://www.xamarin.com
 *
 */

#ifndef __MONO_REMOTING_H__
#define __MONO_REMOTING_H__

#include "config.h"
#include <mono/metadata/class.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>

void mono_remoting_init (void) MONO_INTERNAL;

#ifndef DISABLE_REMOTING

MonoMethod *
mono_marshal_get_remoting_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_xappdomain_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_remoting_invoke_for_target (MonoMethod *method, MonoRemotingTarget target_type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_remoting_invoke_with_check (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_stfld_wrapper (MonoType *type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_ldfld_wrapper (MonoType *type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_ldflda_wrapper (MonoType *type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_ldfld_remote_wrapper (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_stfld_remote_wrapper (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_proxy_cancast (MonoClass *klass) MONO_INTERNAL;

#endif

#endif
