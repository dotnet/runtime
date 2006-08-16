/*
 * ghashtable.c: Hashtable implementation
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *
 * This is based on the System.Collections.Hashtable from Mono
 * implemented originally by Sergey Chaban (serge@wildwestsoftware.com)
 *
 * (C) 2006 Novell, Inc.
 */
#include <glib.h>
#include <math.h>

typedef struct {
	gpointer key;
	gpointer value;
} Slot;

struct _GHashTable {
	GHashFunc     hash_func;
	GEqualFunc    key_equal_func;

	float load_factor;
	Slot *table;
	int   table_size;
	int   threshold;
	int   in_use;
};

static int prime_tbl[] = {
	11, 19, 37, 73, 109, 163, 251, 367, 557, 823, 1237,
	1861, 2777, 4177, 6247, 9371, 14057, 21089, 31627,
	47431, 71143, 106721, 160073, 240101, 360163,
	540217, 810343, 1215497, 1823231, 2734867, 4102283,
	6153409, 9230113, 13845163
};

static gboolean
test_prime (int x)
{
	if ((x & 1) != 0) {
		int n;
		for (n = 3; n< (int)sqrt (x); n += 2) {
			if ((x % n) == 0)
				return FALSE;
		}
		return TRUE;
	}
	// There is only one even prime - 2.
	return (x == 2);
}

static int
calc_prime (int x)
{
	int i;
	
	for (i = (x & (~1))-1; i< G_MAXINT32; i += 2) {
		if (test_prime (i))
			return i;
	}
	return x;
}

static int
to_prime (int x)
{
	int i;
	
	for (i = 0; i < G_N_ELEMENTS (prime_tbl); i++) {
		if (x <= prime_tbl [i])
			return prime_tbl [i];
	}
	return calc_prime (x);
}

static void
adjust_threshold (GHashTable *hash)
{
	int size = table->size;

	hash->threshold = (int) hash->table_size * hash->load_factor;
	if (hash->threshold >= hash->table_size)
		threshold = hash->table_size-1;
}
	
static void
set_table (GHashTable *hash, Slot *table)
{
	hash->table = table;
	adjust_threshold (hash);
}

GHashTable *
g_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func)
{
	GHashTable *table = g_new0 (GHashTable, 1);

	table->hash_func = hash_func;
	table->key_equal_func = key_equal_func;

	table->load_factor = 0.75;
	
	table->table_size = to_prime (1);
	set_table (table, g_new0 (slot, table->table_size));
	
	return table;
}

void
g_hash_table_insert (GHashTable *hash, gpointer key, gpointer value)
{
	Slot *table, *entry;
	guint size, spot;
	int h, free_index;
	
	g_return_if_fail (hash != NULL);
	
	if (hash->in_use >= hash->threshold)
		rehash (hash);

	size = (guint) hash->table_size;
	h = get_hash (key) & G_MAXINT32;
	spot = (guint) h;
	step = (guint) ((spot>>5)+1) % (size-1)+1;

	table = hash->table;
	free_index = -1;

	for (i = 0; i < size; i++){
		int indx = (int) (spot % size);
		entry = table [indx];
		
	}
	
}
