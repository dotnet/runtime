#include "ep-getter-setter.h"

#ifndef DS_DEFINE_INLINE_GETTER
#define DS_DEFINE_INLINE_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_INLINE_GETTER_PREFIX(ds, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef DS_DEFINE_INLINE_GETTER_REF
#define DS_DEFINE_INLINE_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_INLINE_GETTER_REF_PREFIX(ds, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef DS_DEFINE_INLINE_GETTER_ARRAY_REF
#define DS_DEFINE_INLINE_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	EP_DEFINE_INLINE_GETTER_ARRAY_REF_PREFIX(ds, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field)
#endif

#ifndef DS_DEFINE_INLINE_SETTER
#define DS_DEFINE_INLINE_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	EP_DEFINE_INLINE_SETTER_PREFIX(ds, instance_type, instance_type_name, instance_field_type, instance_field_name)
#endif

#ifndef DS_DEFINE_NOINLINE_GETTER
#define DS_DEFINE_NOINLINE_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_NOINLINE_GETTER_PREFIX(ds, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef DS_DEFINE_NOINLINE_GETTER_REF
#define DS_DEFINE_NOINLINE_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_NOINLINE_GETTER_REF_PREFIX(ds, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef DS_DEFINE_NOINLINE_GETTER_ARRAY_REF
#define DS_DEFINE_NOINLINE_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	EP_DEFINE_NOINLINE_GETTER_ARRAY_REF_PREFIX(ds, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field)
#endif

#ifndef DS_DEFINE_NOINLINE_SETTER
#define DS_DEFINE_NOINLINE_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	EP_DEFINE_NOINLINE_SETTER_PREFIX(ds, instance_type, instance_type_name, instance_field_type, instance_field_name)
#endif

#ifndef DS_IMPL_GETTER
#define DS_IMPL_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_IMPL_GETTER_PREFIX(ds, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef DS_IMPL_GETTER_REF
#define DS_IMPL_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_IMPL_GETTER_REF_PREFIX(ds, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef DS_IMPL_GETTER_ARRAY_REF
#define DS_IMPL_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	EP_IMPL_GETTER_ARRAY_REF_PREFIX(ds, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field)
#endif

#ifndef DS_IMPL_SETTER
#define DS_IMPL_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	EP_IMPL_SETTER_PREFIX(ds, instance_type, instance_type_name, instance_field_type, instance_field_name)
#endif

#undef DS_DEFINE_GETTER
#undef DS_DEFINE_GETTER_REF
#undef DS_DEFINE_GETTER_ARRAY_REF
#undef DS_DEFINE_SETTER

#if defined(DS_INLINE_GETTER_SETTER)
#define DS_DEFINE_GETTER DS_DEFINE_INLINE_GETTER
#define DS_DEFINE_GETTER_REF DS_DEFINE_INLINE_GETTER_REF
#define DS_DEFINE_GETTER_ARRAY_REF DS_DEFINE_INLINE_GETTER_ARRAY_REF
#define DS_DEFINE_SETTER DS_DEFINE_INLINE_SETTER
#elif defined(DS_IMPL_GETTER_SETTER)
#define DS_DEFINE_GETTER DS_IMPL_GETTER
#define DS_DEFINE_GETTER_REF DS_IMPL_GETTER_REF
#define DS_DEFINE_GETTER_ARRAY_REF DS_IMPL_GETTER_ARRAY_REF
#define DS_DEFINE_SETTER DS_IMPL_SETTER
#else
#define DS_DEFINE_GETTER DS_DEFINE_NOINLINE_GETTER
#define DS_DEFINE_GETTER_REF DS_DEFINE_NOINLINE_GETTER_REF
#define DS_DEFINE_GETTER_ARRAY_REF DS_DEFINE_NOINLINE_GETTER_ARRAY_REF
#define DS_DEFINE_SETTER DS_DEFINE_NOINLINE_SETTER
#endif
