#ifndef _MONO_METADATA_ENDIAN_H_
#define _MONO_METADATA_ENDIAN_H_ 1

/* FIXME: implement big endian versions */

#define le64_to_cpu(x) (x)
#define le32_to_cpu(x) (x)
#define le16_to_cpu(x) (x)
#define read32(x) le32_to_cpu (*((guint32 *) (x)))
#define read16(x) le16_to_cpu (*((guint16 *) (x)))
#define read64(x) le64_to_cpu (*((guint64 *) (x)))

#endif /* _MONO_METADATA_ENDIAN_H_ */
