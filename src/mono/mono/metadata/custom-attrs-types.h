/**
 * \file
 */

#ifndef __MONO_CUSTOM_ATTRS_TYPES_H__
#define __MONO_CUSTOM_ATTRS_TYPES_H__

typedef struct _MonoCustomAttrValueArray MonoCustomAttrValueArray;

typedef struct _MonoCustomAttrValue {
	union {
		gpointer primitive; /* int/enum/MonoType/string */
		MonoCustomAttrValueArray *array;
	} value;
	MonoTypeEnum type : 8;
} MonoCustomAttrValue;

struct _MonoCustomAttrValueArray {
	int len;
	MonoCustomAttrValue values[MONO_ZERO_LEN_ARRAY];
};

#endif  /* __MONO_CUSTOM_ATTRS_TYPES_H__ */
