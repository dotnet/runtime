/*
 * \file Implementation details for class initialization
 */

#ifndef __MONO_METADATA_CLASS_INIT_INTERNALS_H__
#define __MONO_METADATA_CLASS_INIT_INTERNALS_H__

#include <mono/metadata/class.h>

void
mono_class_setup_interface_id_nolock (MonoClass *klass);

void
mono_class_setup_invalidate_interface_offsets (MonoClass *klass);

enum {
	MONO_SETUP_ITF_OFFSETS_OVERWRITE = 0x01,
	MONO_SETUP_ITF_OFFSETS_BITMAP_ONLY = 0x02,
};

int
mono_class_setup_interface_offsets_internal (MonoClass *klass, int cur_slot, int setup_itf_offsets_flags);

int
mono_class_setup_count_virtual_methods (MonoClass *klass);

gboolean
mono_class_setup_need_stelemref_method (MonoClass *klass);

gboolean
mono_class_setup_method_has_preserve_base_overrides_attribute (MonoMethod *method);

/*
 * Get the class and all its parents and interfaces to at least the
 * MONO_CLASS_READY_APPROX_PARENT ready level, loading assemblies
 * along the way.
 */
void
mono_class_preload_class (MonoClass *klass);


/* Just for class-init, class-init-preload and sre.c - nothing else should be using this */
gboolean
m_class_set_ready_level_at_least (MonoClass *klass, int8_t level);

#endif /* __MONO_METADATA_CLASS_INIT_INTERNALS_H__ */
