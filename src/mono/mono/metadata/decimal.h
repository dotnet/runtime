#include "mono/metadata/object.h"

/* representation for C# type decimal */
/* FIXME: handling of little/big endian. This is the layout of MSC */
typedef struct
{
	union {
		guint32 ss32;
		struct {
			unsigned int reserved1 : 16;
			unsigned int scale : 8; 
			unsigned int reserved2 : 7;
	    		unsigned int sign : 1; 
		} signscale;
	} u;
	guint32 hi32;
	guint32 lo32;
	guint32 mid32;
} decimal_repr;


/* function prototypes */
gint32 mono_decimalIncr(/*[In, Out]*/decimal_repr* pA, /*[In]*/decimal_repr* pB);
gint32 mono_double2decimal(/*[Out]*/decimal_repr* pA, double val, gint32 digits, gint32 sign);
gint32 mono_string2decimal(/*[Out]*/decimal_repr* pA, MonoString* str, gint32 decrDecimal, gint32 sign);
void mono_decimal2string(/*[In]*/decimal_repr* pA, int digits, int decimals,
								 MonoArray* buf, gint32 bufSize, gint32* pDecPos, gint32* pSign);
gint32 mono_decimal2UInt64(/*[In]*/decimal_repr* pD, guint64* pResult);
gint32 mono_decimal2Int64(/*[In]*/decimal_repr* pD, gint64* pResult);
void mono_decimalFloorAndTrunc(/*[In, Out]*/decimal_repr* pD, gint32 floorFlag);
void mono_decimalRound(/*[In, Out]*/decimal_repr* pD, gint32 decimals);
gint32 mono_decimalMult(/*[In, Out]*/decimal_repr* pA, /*[In]*/decimal_repr* pB);
gint32 mono_decimalDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB);
gint32 mono_decimalIntDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB);
gint32 mono_decimalCompare(/*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB);
double mono_decimal2double(/*[In]*/decimal_repr* pA);
gint32 mono_decimalSetExponent(/*[In, Out]*/decimal_repr* pA, gint32 exp);
