/**
 * \file
 */

#ifndef __MONO_SUPPORT_S390X_H__
#define __MONO_SUPPORT_S390X_H__

#define S390_SET(loc, dr, v)					\
	do {							\
		guint64 val = (guint64) v;			\
		if (s390_is_imm16(val)) {			\
			s390_lghi(loc, dr, val);		\
		} else if (s390_is_uimm16(val)) {		\
			s390_llill(loc, dr, val);		\
		} else if (s390_is_imm32(val)) {		\
			s390_lgfi(loc, dr, val);		\
		} else if (s390_is_uimm32(val)) {		\
			s390_llilf(loc, dr, val);		\
		} else {					\
			guint32 hi = (val) >> 32;		\
			guint32 lo = (val) & 0xffffffff;	\
			s390_iihf(loc, dr, hi);			\
			s390_iilf(loc, dr, lo);			\
		}						\
	} while (0)

#define S390_LONG(loc, opy, op, r, ix, br, off)				\
	if (s390_is_imm20(off)) {					\
		s390_##opy (loc, r, ix, br, off);			\
	} else {							\
		if (ix == 0) {						\
			S390_SET(loc, s390_r13, off);			\
			s390_la (loc, s390_r13, s390_r13, br, 0);	\
		} else {						\
			s390_la   (loc, s390_r13, ix, br, 0);		\
			S390_SET  (loc, s390_r0, off);			\
			s390_agr  (loc, s390_r13, s390_r0);		\
		}							\
		s390_##op (loc, r, 0, s390_r13, 0);			\
	}

#define S390_SET_MASK(loc, dr, v)				\
	do {							\
		if (s390_is_imm16 (v)) {			\
			s390_lghi (loc, dr, v);			\
		} else if (s390_is_imm32 (v)) {			\
			s390_lgfi (loc, dr, v);			\
		} else {					\
			gint64 val = (gint64) v;		\
			guint32 hi = (val) >> 32;		\
			guint32 lo = (val) & 0xffffffff;	\
			s390_iilf(loc, dr, lo);			\
			s390_iihf(loc, dr, hi);			\
		}						\
	} while (0)

#define S390_CALL_TEMPLATE(loc, r)				\
	do {							\
		s390_iihf (loc, r, 0);				\
		s390_iilf (loc, r, 0);				\
		s390_basr (loc, s390_r14, r);			\
	} while (0)

#define S390_BR_TEMPLATE(loc, r)				\
	do {							\
		s390_iihf (loc, r, 0);				\
		s390_iilf (loc, r, 0);				\
		s390_br   (loc, r);				\
	} while (0)

#define S390_LOAD_TEMPLATE(loc, r)				\
	do {							\
		s390_iihf (loc, r, 0);				\
		s390_iilf (loc, r, 0);				\
	} while (0)

#define S390_EMIT_CALL(loc, t)					\
	do {							\
		gint64 val = (gint64) t;			\
		guint32 hi = (val) >> 32;			\
		guint32 lo = (val) & 0xffffffff;		\
		uintptr_t p = (uintptr_t) loc;			\
		p += 2;						\
		*(guint32 *) p = hi;				\
		p += 6;						\
		*(guint32 *) p = lo;				\
	} while (0)

#define S390_EMIT_LOAD(loc, v)					\
	do {							\
		gint64 val = (gint64) v;			\
		guint32 hi = (val) >> 32;			\
		guint32 lo = (val) & 0xffffffff;		\
		uintptr_t p = (uintptr_t) loc;			\
		p += 2;						\
		*(guint32 *) p = hi;				\
		p += 6;						\
		*(guint32 *) p = lo;				\
	} while (0)

#endif	/* __MONO_SUPPORT_S390X_H__ */
