#include <stdlib.h>
#include <glib.h>
#include "mono-counters.h"

typedef struct _MonoCounter MonoCounter;

struct _MonoCounter {
	MonoCounter *next;
	const char *name;
	void *addr;
	int type;
};

static MonoCounter *counters = NULL;
static int valid_mask = 0;
static int set_mask = 0;

/**
 * mono_counters_enable:
 * @section_mask: a mask listing the sections that will be displayed
 *
 * This is used to track which counters will be displayed.
 */
void
mono_counters_enable (int section_mask)
{
	valid_mask = section_mask & MONO_COUNTER_SECTION_MASK;
}

/**
 * mono_counters_register:
 * @name: The name for this counters.
 * @type: One of the possible MONO_COUNTER types, or MONO_COUNTER_CALLBACK for a function pointer.
 * @addr: The address to register.
 *
 * Register addr as the address of a counter of type type.
 * Note that @name must be a valid string at all times until
 * mono_counters_dump () is called.
 *
 * It may be a function pointer if MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 */
void 
mono_counters_register (const char* name, int type, void *addr)
{
	MonoCounter *counter;
	if (!(type & valid_mask))
		return;
	counter = malloc (sizeof (MonoCounter));
	if (!counter)
		return;
	counter->name = name;
	counter->type = type;
	counter->addr = addr;
	counter->next = NULL;

	set_mask |= type;

	/* Append */
	if (counters) {
		MonoCounter *item = counters;
		while (item->next)
			item = item->next;
		item->next = counter;
	} else {
		counters = counter;
	}
}

typedef int (*IntFunc) (void);
typedef guint (*UIntFunc) (void);
typedef gint64 (*LongFunc) (void);
typedef guint64 (*ULongFunc) (void);
typedef gssize (*PtrFunc) (void);
typedef double (*DoubleFunc) (void);
typedef char* (*StrFunc) (void);

#define ENTRY_FMT "%-36s: "
static void
dump_counter (MonoCounter *counter, FILE *outfile) {
	int intval;
	guint uintval;
	gint64 int64val;
	guint64 uint64val;
	gssize wordval;
	double dval;
	const char *str;
	switch (counter->type & MONO_COUNTER_TYPE_MASK) {
	case MONO_COUNTER_INT:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      intval = ((IntFunc)counter->addr) ();
	      else
		      intval = *(int*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%d\n", counter->name, intval);
	      break;
	case MONO_COUNTER_UINT:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      uintval = ((UIntFunc)counter->addr) ();
	      else
		      uintval = *(guint*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%u\n", counter->name, uintval);
	      break;
	case MONO_COUNTER_LONG:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      int64val = ((LongFunc)counter->addr) ();
	      else
		      int64val = *(gint64*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%lld\n", counter->name, (long long)int64val);
	      break;
	case MONO_COUNTER_ULONG:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      uint64val = ((ULongFunc)counter->addr) ();
	      else
		      uint64val = *(guint64*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%llu\n", counter->name, (unsigned long long)uint64val);
	      break;
	case MONO_COUNTER_WORD:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      wordval = ((PtrFunc)counter->addr) ();
	      else
		      wordval = *(gssize*)counter->addr;
#if SIZEOF_VOID_P == 8
	      fprintf (outfile, ENTRY_FMT "%lld\n", counter->name, (gint64)wordval);
#else
	      fprintf (outfile, ENTRY_FMT "%d\n", counter->name, (gint)wordval);
#endif
	      break;
	case MONO_COUNTER_DOUBLE:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      dval = ((DoubleFunc)counter->addr) ();
	      else
		      dval = *(double*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%.4f\n", counter->name, dval);
	      break;
	case MONO_COUNTER_STRING:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      str = ((StrFunc)counter->addr) ();
	      else
		      str = *(char**)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%s\n", counter->name, str);
	      break;
	}
}

static const char
section_names [][10] = {
	"JIT",
	"GC",
	"Metadata",
	"Generics",
	"Security"
};

static void
mono_counters_dump_section (int section, FILE *outfile)
{
	MonoCounter *counter = counters;
	while (counter) {
		if (counter->type & section)
			dump_counter (counter, outfile);
		counter = counter->next;
	}
}

/**
 * mono_counters_dump:
 * @section_mask: The sections to dump counters for
 * @outfile: a FILE to dump the results to
 *
 * Displays the counts of all the enabled counters registered. 
 */
void
mono_counters_dump (int section_mask, FILE *outfile)
{
	int i, j;
	section_mask &= valid_mask;
	if (!counters)
		return;
	for (j = 0, i = MONO_COUNTER_JIT; i < MONO_COUNTER_LAST_SECTION; j++, i <<= 1) {
		if ((section_mask & i) && (set_mask & i)) {
			fprintf (outfile, "\n%s statistics\n", section_names [j]);
			mono_counters_dump_section (i, outfile);
		}
	}
}

/**
 * mono_counters_cleanup:
 *
 * Perform any needed cleanup at process exit.
 */
void
mono_counters_cleanup (void)
{
	MonoCounter *counter = counters;
	while (counter) {
		MonoCounter *tmp = counters;
		counter = counter->next;
		free (tmp);
	}
	counters = NULL;
}

