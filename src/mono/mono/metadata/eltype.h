#ifndef _MONO_METADATA_ELTYPE_H_
#define _MONO_METADATA_ELTYPE_H_

/*
 * Encoding for type signatures used in the Metadata
 */
typedef enum {
	ELEMENT_TYPE_END        = 0x00,       /* End of List */
	ELEMENT_TYPE_VOID       = 0x01,
	ELEMENT_TYPE_BOOLEAN    = 0x02,
	ELEMENT_TYPE_CHAR       = 0x03,
	ELEMENT_TYPE_I1         = 0x04,
	ELEMENT_TYPE_U1         = 0x05,
	ELEMENT_TYPE_I2         = 0x06,
	ELEMENT_TYPE_U2         = 0x07,
	ELEMENT_TYPE_I4         = 0x08,
	ELEMENT_TYPE_U4         = 0x09,
	ELEMENT_TYPE_I8         = 0x0a,
	ELEMENT_TYPE_U8         = 0x0b,
	ELEMENT_TYPE_R4         = 0x0c,
	ELEMENT_TYPE_R8         = 0x0d,
	ELEMENT_TYPE_STRING     = 0x0e,
	ELEMENT_TYPE_PTR        = 0x0f,       /* arg: <type> token */
	ELEMENT_TYPE_BYREF      = 0x10,       /* arg: <type> token */
	ELEMENT_TYPE_VALUETYPE  = 0x11,       /* arg: <type> token */
	ELEMENT_TYPE_CLASS      = 0x12,       /* arg: <type> token */
	ELEMENT_TYPE_ARRAY      = 0x14,       /* type, rank, boundsCount, bound1, loCount, lo1 */
	ELEMENT_TYPE_TYPEDBYREF = 0x15,
	ELEMENT_TYPE_I          = 0x18,
	ELEMENT_TYPE_U          = 0x19,
	ELEMENT_TYPE_FNPTR      = 0x1b,	      /* arg: full method signature */
	ELEMENT_TYPE_OBJECT     = 0x1c,
	ELEMENT_TYPE_SZARRAY    = 0x1d,       /* 0-based one-dim-array */
	ELEMENT_TYPE_CMOD_REQD  = 0x1f,       /* arg: typedef or typeref token */
	ELEMENT_TYPE_CMOD_OPT   = 0x20,       /* optional arg: typedef or typref token */
	ELEMENT_TYPE_INTERNAL   = 0x21,       /* CLR internal type */

	ELEMENT_TYPE_MODIFIER   = 0x40,       /* Or with the following types */
	ELEMENT_TYPE_SENTINEL   = 0x41,       /* Sentinel for varargs method signature */
	ELEMENT_TYPE_PINNED     = 0x45,       /* Local var that points to pinned object */
} ElementTypeEnum;

#endif /* _MONO_METADATA_ELTYPE_H_ */
