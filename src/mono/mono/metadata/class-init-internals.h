/*
 * \file Implementation details for class initialization
 */

#ifndef __MONO_METADATA_CLASS_INIT_INTERNALS_H__
#define __MONO_METADATA_CLASS_INIT_INTERNALS_H__

#include <mono/metadata/class.h>

void
mono_class_setup_interface_id_nolock (MonoClass *klass);

int
mono_class_setup_interface_offsets_internal (MonoClass *klass, int cur_slot, gboolean overwrite);

int
mono_class_setup_count_virtual_methods (MonoClass *klass);

gboolean
mono_class_setup_need_stelemref_method (MonoClass *klass);

gboolean
mono_class_setup_method_has_preserve_base_overrides_attribute (MonoMethod *method);


#endif /* __MONO_METADATA_CLASS_INIT_INTERNALS_H__ */
