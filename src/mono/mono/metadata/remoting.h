/**
 * \file
 * Remoting support
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

void mono_remoting_init (void);

#ifndef DISABLE_REMOTING

MonoMethod *
mono_marshal_get_remoting_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_xappdomain_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_remoting_invoke_for_target (MonoMethod *method, MonoRemotingTarget target_type);

MonoMethod *
mono_marshal_get_remoting_invoke_with_check (MonoMethod *method);

MonoMethod *
mono_marshal_get_stfld_wrapper (MonoType *type);

MonoMethod *
mono_marshal_get_ldfld_wrapper (MonoType *type);

MonoMethod *
mono_marshal_get_ldflda_wrapper (MonoType *type);

MonoMethod *
mono_marshal_get_proxy_cancast (MonoClass *klass);

#endif

#endif
