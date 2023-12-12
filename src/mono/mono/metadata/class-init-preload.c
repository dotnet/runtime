// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#include <config.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-init-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/custom-attrs-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/metadata-update.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/unlocked.h>

/*
 * Class preloading: Assembly loading without the loader lock.
 *
 * Historically, Mono used a single global loader lock to protect a class that was being created (in
 * mono_class_create_from_typedef).  In order to correctly set up the parent of a class, the loader
 * lock was held recursively, and the parent class was also initialized.  This can trigger assembly
 * loading, and assembly loading can trigger managed callbacks.  As a result we would call managed
 * code while holding the loader lock, which could create deadlocks (for example if the managed code
 * waited for another thread that needed to do class initialization).
 *
 * What we do now is we allocate a MonoClass in the MONO_CLASS_READY_BAREBONES state and then try to
 * pre-load its parent and interfaces, without holding the loader lock.  However we need to avoid
 * cycles (both invalid IL like: class SubClass : SubClass; and also valid IL like class SubClass :
 * ParentClass<SubClass>).  So we use the readiness levels as a depth first search visited bit.  We
 * first put the class into MONO_CLASS_READY_PRELOAD_STARTED then visit its parent and interfaces
 * and if we ever see PRELOAD_STARTED again, we just return, avoiding a cycle.  When we're done
 * preloading a class we put it into the MONO_CLASS_READY_APPROX_PARENT state to signal that it has
 * been fully pre-loaded.
 *
 * At that point we can take the global loader lock and proceed with normal initialization, without
 * triggering assembly loading callbacks.
 *
 * Note that while we're doing the preloading, we can't form a generic instance yet - we can preload
 * the gtd, and we can preload the instantiation types, but we can't call
 * mono_class_from_mono_type_internal on a MonoType for a ginst (and similarly for array, pointer
 * etc types) because we dont' have incremental initialization there.  The preloading has to be
 * careful not to try and fully initialize any types.
 *
 */
void
mono_class_preload_class (MonoClass *klass)
{
	/* we should only come here without the loader lock */
	g_assert (!mono_loader_lock_tracking() || !mono_loader_lock_is_owned_by_self());
	/* if someone already started visiting this class, don't visit it again */
	if (m_class_ready_level_at_least (klass, MONO_CLASS_READY_PRELOAD_STARTED))
		return;
	/* try to set the level - if someone beat us, let them preload */
	if (!m_class_set_ready_level_at_least (klass, MONO_CLASS_READY_PRELOAD_STARTED))
		return;

	/* TODO: do work */

	
	m_class_set_ready_level_at_least (klass, MONO_CLASS_READY_APPROX_PARENT);
}
