
#include <glib.h>

enum {
	VAL_I32,
	VAL_I64,
	VAL_DOUBLE,
	VAL_NATI,
	VAL_MP,
	VAL_TP,
	VAL_OBJ
};

typedef struct {
	union {
		gint32 i;
		gint64 l;
		double f;
		/* native size integer and pointer types */
		gpointer p;
	} data;
	int type;
} stackval;

