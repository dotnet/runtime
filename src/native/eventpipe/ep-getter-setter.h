#ifndef EP_BUILD_GETTER_GET_NAME
#define EP_BUILD_GETTER_GET_NAME(prefix_name, instance_type_name, instance_field_name) \
prefix_name ## _ ## instance_type_name ## _get_ ## instance_field_name
#endif

#ifndef EP_BUILD_GETTER_GET_REF_NAME
#define EP_BUILD_GETTER_GET_REF_NAME(prefix_name, instance_type_name, instance_field_name) \
prefix_name ## _ ## instance_type_name ## _get_ ## instance_field_name ## _ref
#endif

#ifndef EP_BUILD_GETTER_GET_CREF_NAME
#define EP_BUILD_GETTER_GET_CREF_NAME(prefix_name, instance_type_name, instance_field_name) \
prefix_name ## _ ## instance_type_name ## _get_ ## instance_field_name ## _cref
#endif

#ifndef EP_BUILD_GETTER_SIZEOF_NAME
#define EP_BUILD_GETTER_SIZEOF_NAME(prefix_name, instance_type_name, instance_field_name) \
prefix_name ## _ ## instance_type_name ## _sizeof_ ## instance_field_name
#endif

#ifndef EP_BUILD_SETTER_NAME
#define EP_BUILD_SETTER_NAME(prefix_name, instance_type_name, instance_field_name) \
prefix_name ## _ ## instance_type_name ## _set_ ## instance_field_name
#endif

#ifndef EP_DEFINE_INLINE_GETTER_PREFIX
#define EP_DEFINE_INLINE_GETTER_PREFIX(prefix_name, instance_type, instance_type_name, return_type, instance_field_name) \
	static inline return_type EP_BUILD_GETTER_GET_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return instance-> instance_field_name; } \
	static inline size_t EP_BUILD_GETTER_SIZEOF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return sizeof (instance-> instance_field_name); }
#endif

#ifndef EP_DEFINE_INLINE_GETTER
#define EP_DEFINE_INLINE_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_INLINE_GETTER_PREFIX(ep, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef EP_DEFINE_INLINE_GETTER_REF_PREFIX
#define EP_DEFINE_INLINE_GETTER_REF_PREFIX(prefix_name, instance_type, instance_type_name, return_type, instance_field_name) \
	static inline return_type EP_BUILD_GETTER_GET_REF_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance) { return &(instance-> instance_field_name); } \
	static inline const return_type EP_BUILD_GETTER_GET_CREF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return &(instance-> instance_field_name); }
#endif

#ifndef EP_DEFINE_INLINE_GETTER_REF
#define EP_DEFINE_INLINE_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_INLINE_GETTER_REF_PREFIX(ep, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef EP_DEFINE_INLINE_GETTER_ARRAY_REF_PREFIX
#define EP_DEFINE_INLINE_GETTER_ARRAY_REF_PREFIX(prefix_name, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	static inline return_type EP_BUILD_GETTER_GET_REF_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance) { return &(instance-> instance_field); } \
	static inline const_return_type EP_BUILD_GETTER_GET_CREF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return &(instance-> instance_field); }
#endif

#ifndef EP_DEFINE_INLINE_GETTER_ARRAY_REF
#define EP_DEFINE_INLINE_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	EP_DEFINE_INLINE_GETTER_ARRAY_REF_PREFIX(ep, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field)
#endif

#ifndef EP_DEFINE_INLINE_SETTER_PREFIX
#define EP_DEFINE_INLINE_SETTER_PREFIX(prefix_name, instance_type, instance_type_name, instance_field_type, instance_field_name) \
	static inline void EP_BUILD_SETTER_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance, instance_field_type instance_field_name) { instance-> instance_field_name = instance_field_name; }
#endif

#ifndef EP_DEFINE_INLINE_SETTER
#define EP_DEFINE_INLINE_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	EP_DEFINE_INLINE_SETTER_PREFIX(ep, instance_type, instance_type_name, instance_field_type, instance_field_name)
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER_PREFIX
#define EP_DEFINE_NOINLINE_GETTER_PREFIX(prefix_name, instance_type, instance_type_name, return_type, instance_field_name) \
	return_type EP_BUILD_GETTER_GET_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance); \
	size_t EP_BUILD_GETTER_SIZEOF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance);
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER
#define EP_DEFINE_NOINLINE_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_NOINLINE_GETTER_PREFIX(ep, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER_REF_PREFIX
#define EP_DEFINE_NOINLINE_GETTER_REF_PREFIX(prefix_name, instance_type, instance_type_name, return_type, instance_field_name) \
	return_type EP_BUILD_GETTER_GET_REF_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance); \
	const return_type EP_BUILD_GETTER_GET_CREF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance);
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER_REF
#define EP_DEFINE_NOINLINE_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_DEFINE_NOINLINE_GETTER_REF_PREFIX(ep, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER_ARRAY_REF_PREFIX
#define EP_DEFINE_NOINLINE_GETTER_ARRAY_REF_PREFIX(prefix_name, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	return_type EP_BUILD_GETTER_GET_REF_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance); \
	const_return_type EP_BUILD_GETTER_GET_CREF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance);
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER_ARRAY_REF
#define EP_DEFINE_NOINLINE_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	EP_DEFINE_NOINLINE_GETTER_ARRAY_REF_PREFIX(ep, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field)
#endif

#ifndef EP_DEFINE_NOINLINE_SETTER_PREFIX
#define EP_DEFINE_NOINLINE_SETTER_PREFIX(prefix_name, instance_type, instance_type_name, instance_field_type, instance_field_name) \
	void EP_BUILD_SETTER_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance, instance_field_type instance_field_name);
#endif

#ifndef EP_DEFINE_NOINLINE_SETTER
#define EP_DEFINE_NOINLINE_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	EP_DEFINE_NOINLINE_SETTER_PREFIX(ep, instance_type, instance_type_name, instance_field_type, instance_field_name)
#endif

#ifndef EP_IMPL_GETTER_PREFIX
#define EP_IMPL_GETTER_PREFIX(prefix_name, instance_type, instance_type_name, return_type, instance_field_name) \
	return_type EP_BUILD_GETTER_GET_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return instance-> instance_field_name; } \
	size_t EP_BUILD_GETTER_SIZEOF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return sizeof (instance-> instance_field_name); }
#endif

#ifndef EP_IMPL_GETTER
#define EP_IMPL_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_IMPL_GETTER_PREFIX(ep, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef EP_IMPL_GETTER_REF_PREFIX
#define EP_IMPL_GETTER_REF_PREFIX(prefix_name, instance_type, instance_type_name, return_type, instance_field_name) \
	return_type EP_BUILD_GETTER_GET_REF_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance) { return &(instance-> instance_field_name); } \
	const return_type EP_BUILD_GETTER_GET_CREF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return &(instance-> instance_field_name); }
#endif

#ifndef EP_IMPL_GETTER_REF
#define EP_IMPL_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	EP_IMPL_GETTER_REF_PREFIX(ep, instance_type, instance_type_name, return_type, instance_field_name)
#endif

#ifndef EP_IMPL_GETTER_ARRAY_REF_PREFIX
#define EP_IMPL_GETTER_ARRAY_REF_PREFIX(prefix_name, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	return_type EP_BUILD_GETTER_GET_REF_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance) { return &(instance-> instance_field); } \
	const_return_type EP_BUILD_GETTER_GET_CREF_NAME(prefix_name, instance_type_name, instance_field_name) (const instance_type instance) { return &(instance-> instance_field); }
#endif

#ifndef EP_IMPL_GETTER_ARRAY_REF
#define EP_IMPL_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	EP_IMPL_GETTER_ARRAY_REF_PREFIX(ep, instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field)
#endif

#ifndef EP_IMPL_SETTER_PREFIX
#define EP_IMPL_SETTER_PREFIX(prefix_name, instance_type, instance_type_name, instance_field_type, instance_field_name) \
	void EP_BUILD_SETTER_NAME(prefix_name, instance_type_name, instance_field_name) (instance_type instance, instance_field_type instance_field_name) { instance-> instance_field_name = instance_field_name; }
#endif

#ifndef EP_IMPL_SETTER
#define EP_IMPL_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	EP_IMPL_SETTER_PREFIX(ep, instance_type, instance_type_name, instance_field_type, instance_field_name)
#endif

#undef EP_DEFINE_GETTER
#undef EP_DEFINE_GETTER_REF
#undef EP_DEFINE_GETTER_ARRAY_REF
#undef EP_DEFINE_SETTER

#if defined(EP_INLINE_GETTER_SETTER)
#define EP_DEFINE_GETTER EP_DEFINE_INLINE_GETTER
#define EP_DEFINE_GETTER_REF EP_DEFINE_INLINE_GETTER_REF
#define EP_DEFINE_GETTER_ARRAY_REF EP_DEFINE_INLINE_GETTER_ARRAY_REF
#define EP_DEFINE_SETTER EP_DEFINE_INLINE_SETTER
#elif defined(EP_IMPL_GETTER_SETTER)
#define EP_DEFINE_GETTER EP_IMPL_GETTER
#define EP_DEFINE_GETTER_REF EP_IMPL_GETTER_REF
#define EP_DEFINE_GETTER_ARRAY_REF EP_IMPL_GETTER_ARRAY_REF
#define EP_DEFINE_SETTER EP_IMPL_SETTER
#else
#define EP_DEFINE_GETTER EP_DEFINE_NOINLINE_GETTER
#define EP_DEFINE_GETTER_REF EP_DEFINE_NOINLINE_GETTER_REF
#define EP_DEFINE_GETTER_ARRAY_REF EP_DEFINE_NOINLINE_GETTER_ARRAY_REF
#define EP_DEFINE_SETTER EP_DEFINE_NOINLINE_SETTER
#endif
