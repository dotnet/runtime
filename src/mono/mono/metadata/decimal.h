#include "mono/metadata/object.h"
#include "mono/utils/mono-compiler.h"

/* representation for C# type decimal */
/* This is the layout of MSC */
typedef struct
{
	union {
		guint32 ss32;
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	    struct {
		    unsigned int sign      : 1;
		    unsigned int reserved2 : 7;
		    unsigned int scale     : 8;
		    unsigned int reserved1 : 16;
	    } signscale;
#else
	    struct {
		    unsigned int reserved1 : 16;
		    unsigned int scale     : 8;
		    unsigned int reserved2 : 7;
		    unsigned int sign      : 1;
	    } signscale;
#endif
	} u;
    guint32 hi32;
    guint32 lo32;
    guint32 mid32;
} decimal_repr;


/* function prototypes */
gint32 mono_decimalIncr(/*[In, Out]*/decimal_repr* pA, /*[In]*/decimal_repr* pB) MONO_INTERNAL;
gint32 mono_double2decimal(/*[Out]*/decimal_repr* pA, double val, gint32 digits) MONO_INTERNAL;
gint32 mono_decimal2UInt64(/*[In]*/decimal_repr* pD, guint64* pResult) MONO_INTERNAL;
gint32 mono_decimal2Int64(/*[In]*/decimal_repr* pD, gint64* pResult) MONO_INTERNAL;
void mono_decimalFloorAndTrunc(/*[In, Out]*/decimal_repr* pD, gint32 floorFlag) MONO_INTERNAL;
void mono_decimalRound(/*[In, Out]*/decimal_repr* pD, gint32 decimals) MONO_INTERNAL;
gint32 mono_decimalMult(/*[In, Out]*/decimal_repr* pA, /*[In]*/decimal_repr* pB) MONO_INTERNAL;
gint32 mono_decimalDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB) MONO_INTERNAL;
gint32 mono_decimalIntDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB) MONO_INTERNAL;
gint32 mono_decimalCompare(/*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB) MONO_INTERNAL;
double mono_decimal2double(/*[In]*/decimal_repr* pA) MONO_INTERNAL;
gint32 mono_decimalSetExponent(/*[In, Out]*/decimal_repr* pA, gint32 texp) MONO_INTERNAL;

gint32 mono_string2decimal(/*[Out]*/decimal_repr* pA, /*[In]*/MonoString* s, gint32 decrDecimal, gint32 sign) MONO_INTERNAL;

