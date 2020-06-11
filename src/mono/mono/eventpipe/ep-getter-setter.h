
#ifndef EP_DEFINE_INLINE_GETTER
#define EP_DEFINE_INLINE_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	static inline return_type ep_ ## instance_type_name ## _get_ ## instance_field_name (const instance_type instance) { return instance-> instance_field_name; } \
	static inline size_t ep_ ## instance_type_name ## _sizeof_ ## instance_field_name (const instance_type instance) { return sizeof (instance-> instance_field_name); }
#endif

#ifndef EP_DEFINE_INLINE_GETTER_REF
#define EP_DEFINE_INLINE_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	static inline return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance) { return &(instance-> instance_field_name); } \
	static inline const return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance) { return &(instance-> instance_field_name); }
#endif

#ifndef EP_DEFINE_INLINE_GETTER_ARRAY_REF
#define EP_DEFINE_INLINE_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	static inline return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance) { return &(instance-> instance_field); } \
	static inline const_return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance) { return &(instance-> instance_field); }
#endif

#ifndef EP_DEFINE_INLINE_SETTER
#define EP_DEFINE_INLINE_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) static inline void ep_ ## instance_type_name ## _set_ ## instance_field_name (instance_type instance, instance_field_type instance_field_name) { instance-> instance_field_name = instance_field_name; }
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER
#define EP_DEFINE_NOINLINE_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name (const instance_type instance); \
	size_t ep_ ## instance_type_name ## _sizeof_ ## instance_field_name (const instance_type instance);
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER_REF
#define EP_DEFINE_NOINLINE_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance); \
	const return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance);
#endif

#ifndef EP_DEFINE_NOINLINE_GETTER_ARRAY_REF
#define EP_DEFINE_NOINLINE_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance); \
	const_return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance);
#endif

#ifndef EP_DEFINE_NOINLINE_SETTER
#define EP_DEFINE_NOINLINE_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	void ep_ ## instance_type_name ## _set_ ## instance_field_name (instance_type instance, instance_field_type instance_field_name);
#endif

#ifndef EP_IMPL_GETTER
#define EP_IMPL_GETTER(instance_type, instance_type_name, return_type, instance_field_name) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name (const instance_type instance) { return instance-> instance_field_name; } \
	size_t ep_ ## instance_type_name ## _sizeof_ ## instance_field_name (const instance_type instance) { return sizeof (instance-> instance_field_name); }
#endif

#ifndef EP_IMPL_GETTER_REF
#define EP_IMPL_GETTER_REF(instance_type, instance_type_name, return_type, instance_field_name) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance) { return &(instance-> instance_field_name); } \
	const return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance) { return &(instance-> instance_field_name); }
#endif

#ifndef EP_IMPL_GETTER_ARRAY_REF
#define EP_IMPL_GETTER_ARRAY_REF(instance_type, instance_type_name, return_type, const_return_type, instance_field_name, instance_field) \
	return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _ref (instance_type instance) { return &(instance-> instance_field); } \
	const_return_type ep_ ## instance_type_name ## _get_ ## instance_field_name ## _cref (const instance_type instance) { return &(instance-> instance_field); }
#endif

#ifndef EP_IMPL_SETTER
#define EP_IMPL_SETTER(instance_type, instance_type_name, instance_field_type, instance_field_name) \
	void ep_ ## instance_type_name ## _set_ ## instance_field_name (instance_type instance, instance_field_type instance_field_name) { instance-> instance_field_name = instance_field_name; }
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
