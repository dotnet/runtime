#ifndef __INTERPRETER_MINTOPS_H
#define __INTERPRETER_MINTOPS_H

typedef enum
{
	MintOpNoArgs,
	MintOpShortInt,
	MintOpUShortInt,
	MintOpInt,
	MintOpLongInt,
	MintOpFloat,
	MintOpDouble,
	MintOpBranch,
	MintOpShortBranch,
	MintOpSwitch,
	MintOpMethodToken,
	MintOpFieldToken,
	MintOpClassToken,
	MintOpTwoShorts,
	MintOpShortAndInt
} MintOpArgType;

#define OPDEF(a,b,c,d) \
	a,
enum {
#include "mintops.def"
	MINT_LASTOP
};
#undef OPDEF

#if NO_UNALIGNED_ACCESS
#define READ32(x) (((guint16 *)(x)) [0] << 16 | ((guint16 *)(x)) [1])
#define READ64(x) ((guint64)((guint16 *)(x)) [0] << 48 | \
                   (guint64)((guint16 *)(x)) [1] << 32 | \
                   (guint64)((guint16 *)(x)) [2] << 16 | \
                   (guint64)((guint16 *)(x)) [3])
#else /* unaligned access OK */
#define READ32(x) (*(guint32 *)(x))
#define READ64(x) (*(guint64 *)(x))
#endif

extern const char *mono_interp_opname[];
extern unsigned char mono_interp_oplen[];
extern MintOpArgType mono_interp_opargtype[];
extern const guint16 *mono_interp_dis_mintop(const unsigned short *base, const guint16 *ip);

#endif

