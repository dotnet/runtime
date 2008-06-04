/*
 * domain.c: MonoDomain functions
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Patrik Torstensson
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <sys/stat.h>

#include <mono/metadata/gc-internal.h>

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/rawbuffer.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/threads-types.h>
#include <metadata/threads.h>
#include <metadata/profiler-private.h>
#include <mono/metadata/coree.h>

/* #define DEBUG_DOMAIN_UNLOAD */

/* we need to use both the Tls* functions and __thread because
 * some archs may generate faster jit code with one meachanism
 * or the other (we used to do it because tls slots were GC-tracked,
 * but we can't depend on this).
 */
static guint32 appdomain_thread_id = -1;
 
#ifdef HAVE_KW_THREAD
static __thread MonoDomain * tls_appdomain MONO_TLS_FAST;
#define GET_APPDOMAIN() tls_appdomain
#define SET_APPDOMAIN(x) do { \
	tls_appdomain = x; \
	TlsSetValue (appdomain_thread_id, x); \
} while (FALSE)

#else

#define GET_APPDOMAIN() ((MonoDomain *)TlsGetValue (appdomain_thread_id))
#define SET_APPDOMAIN(x) TlsSetValue (appdomain_thread_id, x);

#endif

#define GET_APPCONTEXT() (mono_thread_current ()->current_appcontext)
#define SET_APPCONTEXT(x) MONO_OBJECT_SETREF (mono_thread_current (), current_appcontext, (x))

static guint16 appdomain_list_size = 0;
static guint16 appdomain_next = 0;
static MonoDomain **appdomains_list = NULL;
static MonoImage *exe_image;

#define mono_appdomains_lock() EnterCriticalSection (&appdomains_mutex)
#define mono_appdomains_unlock() LeaveCriticalSection (&appdomains_mutex)
static CRITICAL_SECTION appdomains_mutex;

static MonoDomain *mono_root_domain = NULL;

/* some statistics */
static int max_domain_code_size = 0;
static int max_domain_code_alloc = 0;
static int total_domain_code_alloc = 0;

/* AppConfigInfo: Information about runtime versions supported by an 
 * aplication.
 */
typedef struct {
	GSList *supported_runtimes;
	char *required_runtime;
	int configuration_count;
	int startup_count;
} AppConfigInfo;

/*
 * AotModuleInfo: Contains information about AOT modules.
 */
typedef struct {
	MonoImage *image;
	gpointer start, end;
} AotModuleInfo;

static const MonoRuntimeInfo *current_runtime = NULL;

static MonoJitInfoFindInAot jit_info_find_in_aot_func = NULL;

/*
 * Contains information about AOT loaded code.
 */
static MonoAotModuleInfoTable *aot_modules = NULL;

/* This is the list of runtime versions supported by this JIT.
 */
static const MonoRuntimeInfo supported_runtimes[] = {
	{"v1.0.3705", "1.0", { {1,0,5000,0}, {7,0,5000,0} }	},
	{"v1.1.4322", "1.0", { {1,0,5000,0}, {7,0,5000,0} }	},
	{"v2.0.50215","2.0", { {2,0,0,0},    {8,0,0,0} }	},
	{"v2.0.50727","2.0", { {2,0,0,0},    {8,0,0,0} }	},
	{"moonlight", "2.1", { {2,0,5,0},    {9,0,0,0} }    },
};


/* The stable runtime version */
#define DEFAULT_RUNTIME_VERSION "v1.1.4322"

/* This is intentionally not in the header file, so people don't misuse it. */
extern void _mono_debug_init_corlib (MonoDomain *domain);

static void
get_runtimes_from_exe (const char *exe_file, MonoImage **exe_image, const MonoRuntimeInfo** runtimes);

static const MonoRuntimeInfo*
get_runtime_by_version (const char *version);

static MonoImage*
mono_jit_info_find_aot_module (guint8* addr);

guint32
mono_domain_get_tls_key (void)
{
	return appdomain_thread_id;
}

gint32
mono_domain_get_tls_offset (void)
{
	int offset = -1;
	MONO_THREAD_VAR_OFFSET (tls_appdomain, offset);
/*	__asm ("jmp 1f; .section writetext, \"awx\"; 1: movl $tls_appdomain@ntpoff, %0; jmp 2f; .previous; 2:" 
		: "=r" (offset));*/
	return offset;
}

#define JIT_INFO_TABLE_FILL_RATIO_NOM		3
#define JIT_INFO_TABLE_FILL_RATIO_DENOM		4
#define JIT_INFO_TABLE_FILLED_NUM_ELEMENTS	(MONO_JIT_INFO_TABLE_CHUNK_SIZE * JIT_INFO_TABLE_FILL_RATIO_NOM / JIT_INFO_TABLE_FILL_RATIO_DENOM)

#define JIT_INFO_TABLE_LOW_WATERMARK(n)		((n) / 2)
#define JIT_INFO_TABLE_HIGH_WATERMARK(n)	((n) * 5 / 6)

#define JIT_INFO_TOMBSTONE_MARKER	((MonoMethod*)NULL)
#define IS_JIT_INFO_TOMBSTONE(ji)	((ji)->method == JIT_INFO_TOMBSTONE_MARKER)

#define JIT_INFO_TABLE_HAZARD_INDEX		0
#define JIT_INFO_HAZARD_INDEX			1

static int
jit_info_table_num_elements (MonoJitInfoTable *table)
{
	int i;
	int num_elements = 0;

	for (i = 0; i < table->num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = table->chunks [i];
		int chunk_num_elements = chunk->num_elements;
		int j;

		for (j = 0; j < chunk_num_elements; ++j) {
			if (!IS_JIT_INFO_TOMBSTONE (chunk->data [j]))
				++num_elements;
		}
	}

	return num_elements;
}

static MonoJitInfoTableChunk*
jit_info_table_new_chunk (void)
{
	MonoJitInfoTableChunk *chunk = g_new0 (MonoJitInfoTableChunk, 1);
	chunk->refcount = 1;

	return chunk;
}

static MonoJitInfoTable *
jit_info_table_new (MonoDomain *domain)
{
	MonoJitInfoTable *table = g_malloc0 (sizeof (MonoJitInfoTable) + sizeof (MonoJitInfoTableChunk*));

	table->domain = domain;
	table->num_chunks = 1;
	table->chunks [0] = jit_info_table_new_chunk ();

	return table;
}

static void
jit_info_table_free (MonoJitInfoTable *table)
{
	int i;
	int num_chunks = table->num_chunks;
	MonoDomain *domain = table->domain;

	mono_domain_lock (domain);

	table->domain->num_jit_info_tables--;
	if (table->domain->num_jit_info_tables <= 1) {
		GSList *list;

		for (list = table->domain->jit_info_free_queue; list; list = list->next)
			g_free (list->data);

		g_slist_free (table->domain->jit_info_free_queue);
		table->domain->jit_info_free_queue = NULL;
	}

	/* At this point we assume that there are no other threads
	   still accessing the table, so we don't have to worry about
	   hazardous pointers. */

	for (i = 0; i < num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = table->chunks [i];
		int num_elements;
		int j;

		if (--chunk->refcount > 0)
			continue;

		num_elements = chunk->num_elements;
		for (j = 0; j < num_elements; ++j) {
			MonoJitInfo *ji = chunk->data [j];

			if (IS_JIT_INFO_TOMBSTONE (ji))
				g_free (ji);
		}

		g_free (chunk);
	}

	mono_domain_unlock (domain);

	g_free (table);
}

/* Can be called with hp==NULL, in which case it acts as an ordinary
   pointer fetch.  It's used that way indirectly from
   mono_jit_info_table_add(), which doesn't have to care about hazards
   because it holds the respective domain lock. */
static gpointer
get_hazardous_pointer (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index)
{
	gpointer p;

	for (;;) {
		/* Get the pointer */
		p = *pp;
		/* If we don't have hazard pointers just return the
		   pointer. */
		if (!hp)
			return p;
		/* Make it hazardous */
		mono_hazard_pointer_set (hp, hazard_index, p);
		/* Check that it's still the same.  If not, try
		   again. */
		if (*pp != p) {
			mono_hazard_pointer_clear (hp, hazard_index);
			continue;
		}
		break;
	}

	return p;
}

/* The jit_info_table is sorted in ascending order by the end
 * addresses of the compiled methods.  The reason why we have to do
 * this is that once we introduce tombstones, it becomes possible for
 * code ranges to overlap, and if we sort by code start and insert at
 * the back of the table, we cannot guarantee that we won't overlook
 * an entry.
 *
 * There are actually two possible ways to do the sorting and
 * inserting which work with our lock-free mechanism:
 *
 * 1. Sort by start address and insert at the front.  When looking for
 * an entry, find the last one with a start address lower than the one
 * you're looking for, then work your way to the front of the table.
 *
 * 2. Sort by end address and insert at the back.  When looking for an
 * entry, find the first one with an end address higher than the one
 * you're looking for, then work your way to the end of the table.
 *
 * We chose the latter out of convenience.
 */
static int
jit_info_table_index (MonoJitInfoTable *table, gint8 *addr)
{
	int left = 0, right = table->num_chunks;

	g_assert (left < right);

	do {
		int pos = (left + right) / 2;
		MonoJitInfoTableChunk *chunk = table->chunks [pos];

		if (addr < chunk->last_code_end)
			right = pos;
		else
			left = pos + 1;
	} while (left < right);
	g_assert (left == right);

	if (left >= table->num_chunks)
		return table->num_chunks - 1;
	return left;
}

static int
jit_info_table_chunk_index (MonoJitInfoTableChunk *chunk, MonoThreadHazardPointers *hp, gint8 *addr)
{
	int left = 0, right = chunk->num_elements;

	while (left < right) {
		int pos = (left + right) / 2;
		MonoJitInfo *ji = get_hazardous_pointer((gpointer volatile*)&chunk->data [pos], hp, JIT_INFO_HAZARD_INDEX);
		gint8 *code_end = (gint8*)ji->code_start + ji->code_size;

		if (addr < code_end)
			right = pos;
		else
			left = pos + 1;
	}
	g_assert (left == right);

	return left;
}

MonoJitInfo*
mono_jit_info_table_find (MonoDomain *domain, char *addr)
{
	MonoJitInfoTable *table;
	MonoJitInfo *ji;
	int chunk_pos, pos;
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	++mono_stats.jit_info_table_lookup_count;

	/* First we have to get the domain's jit_info_table.  This is
	   complicated by the fact that a writer might substitute a
	   new table and free the old one.  What the writer guarantees
	   us is that it looks at the hazard pointers after it has
	   changed the jit_info_table pointer.  So, if we guard the
	   table by a hazard pointer and make sure that the pointer is
	   still there after we've made it hazardous, we don't have to
	   worry about the writer freeing the table. */
	table = get_hazardous_pointer ((gpointer volatile*)&domain->jit_info_table, hp, JIT_INFO_TABLE_HAZARD_INDEX);

	chunk_pos = jit_info_table_index (table, (gint8*)addr);
	g_assert (chunk_pos < table->num_chunks);

	pos = jit_info_table_chunk_index (table->chunks [chunk_pos], hp, (gint8*)addr);

	/* We now have a position that's very close to that of the
	   first element whose end address is higher than the one
	   we're looking for.  If we don't have the exact position,
	   then we have a position below that one, so we'll just
	   search upward until we find our element. */
	do {
		MonoJitInfoTableChunk *chunk = table->chunks [chunk_pos];

		while (pos < chunk->num_elements) {
			ji = get_hazardous_pointer ((gpointer volatile*)&chunk->data [pos], hp, JIT_INFO_HAZARD_INDEX);

			++pos;

			if (IS_JIT_INFO_TOMBSTONE (ji)) {
				mono_hazard_pointer_clear (hp, JIT_INFO_HAZARD_INDEX);
				continue;
			}
			if ((gint8*)addr >= (gint8*)ji->code_start
					&& (gint8*)addr < (gint8*)ji->code_start + ji->code_size) {
				mono_hazard_pointer_clear (hp, JIT_INFO_TABLE_HAZARD_INDEX);
				mono_hazard_pointer_clear (hp, JIT_INFO_HAZARD_INDEX);
				return ji;
			}

			/* If we find a non-tombstone element which is already
			   beyond what we're looking for, we have to end the
			   search. */
			if ((gint8*)addr < (gint8*)ji->code_start)
				break;
		}

		++chunk_pos;
		pos = 0;
	 } while (chunk_pos < table->num_chunks);

	mono_hazard_pointer_clear (hp, JIT_INFO_TABLE_HAZARD_INDEX);
	mono_hazard_pointer_clear (hp, JIT_INFO_HAZARD_INDEX);

	/* maybe it is shared code, so we also search in the root domain */
	ji = NULL;
	if (domain != mono_root_domain)
		ji = mono_jit_info_table_find (mono_root_domain, addr);

	if (ji == NULL) {
		/* Maybe its an AOT module */
		MonoImage *image = mono_jit_info_find_aot_module ((guint8*)addr);
		if (image)
			ji = jit_info_find_in_aot_func (domain, image, addr);
	}
	
	return ji;
}

static G_GNUC_UNUSED void
jit_info_table_check (MonoJitInfoTable *table)
{
	int i;

	for (i = 0; i < table->num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = table->chunks [i];
		int j;

		g_assert (chunk->refcount > 0 /* && chunk->refcount <= 8 */);
		if (chunk->refcount > 10)
			printf("warning: chunk refcount is %d\n", chunk->refcount);
		g_assert (chunk->num_elements <= MONO_JIT_INFO_TABLE_CHUNK_SIZE);

		for (j = 0; j < chunk->num_elements; ++j) {
			MonoJitInfo *this = chunk->data [j];
			MonoJitInfo *next;

			g_assert ((gint8*)this->code_start + this->code_size <= chunk->last_code_end);

			if (j < chunk->num_elements - 1)
				next = chunk->data [j + 1];
			else if (i < table->num_chunks - 1) {
				int k;

				for (k = i + 1; k < table->num_chunks; ++k)
					if (table->chunks [k]->num_elements > 0)
						break;

				if (k >= table->num_chunks)
					return;

				g_assert (table->chunks [k]->num_elements > 0);
				next = table->chunks [k]->data [0];
			} else
				return;

			g_assert ((gint8*)this->code_start + this->code_size <= (gint8*)next->code_start + next->code_size);
		}
	}
}

static MonoJitInfoTable*
jit_info_table_realloc (MonoJitInfoTable *old)
{
	int i;
	int num_elements = jit_info_table_num_elements (old);
	int required_size;
	int num_chunks;
	int new_chunk, new_element;
	MonoJitInfoTable *new;

	/* number of needed places for elements needed */
	required_size = (int)((long)num_elements * JIT_INFO_TABLE_FILL_RATIO_DENOM / JIT_INFO_TABLE_FILL_RATIO_NOM);
	num_chunks = (required_size + MONO_JIT_INFO_TABLE_CHUNK_SIZE - 1) / MONO_JIT_INFO_TABLE_CHUNK_SIZE;

	new = g_malloc (sizeof (MonoJitInfoTable) + sizeof (MonoJitInfoTableChunk*) * num_chunks);
	new->domain = old->domain;
	new->num_chunks = num_chunks;

	for (i = 0; i < num_chunks; ++i)
		new->chunks [i] = jit_info_table_new_chunk ();

	new_chunk = 0;
	new_element = 0;
	for (i = 0; i < old->num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = old->chunks [i];
		int chunk_num_elements = chunk->num_elements;
		int j;

		for (j = 0; j < chunk_num_elements; ++j) {
			if (!IS_JIT_INFO_TOMBSTONE (chunk->data [j])) {
				g_assert (new_chunk < num_chunks);
				new->chunks [new_chunk]->data [new_element] = chunk->data [j];
				if (++new_element >= JIT_INFO_TABLE_FILLED_NUM_ELEMENTS) {
					new->chunks [new_chunk]->num_elements = new_element;
					++new_chunk;
					new_element = 0;
				}
			}
		}
	}

	if (new_chunk < num_chunks) {
		g_assert (new_chunk == num_chunks - 1);
		new->chunks [new_chunk]->num_elements = new_element;
		g_assert (new->chunks [new_chunk]->num_elements > 0);
	}

	for (i = 0; i < num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = new->chunks [i];
		MonoJitInfo *ji = chunk->data [chunk->num_elements - 1];

		new->chunks [i]->last_code_end = (gint8*)ji->code_start + ji->code_size;
	}

	return new;
}

static void
jit_info_table_split_chunk (MonoJitInfoTableChunk *chunk, MonoJitInfoTableChunk **new1p, MonoJitInfoTableChunk **new2p)
{
	MonoJitInfoTableChunk *new1 = jit_info_table_new_chunk ();
	MonoJitInfoTableChunk *new2 = jit_info_table_new_chunk ();

	g_assert (chunk->num_elements == MONO_JIT_INFO_TABLE_CHUNK_SIZE);

	new1->num_elements = MONO_JIT_INFO_TABLE_CHUNK_SIZE / 2;
	new2->num_elements = MONO_JIT_INFO_TABLE_CHUNK_SIZE - new1->num_elements;

	memcpy ((void*)new1->data, (void*)chunk->data, sizeof (MonoJitInfo*) * new1->num_elements);
	memcpy ((void*)new2->data, (void*)(chunk->data + new1->num_elements), sizeof (MonoJitInfo*) * new2->num_elements);

	new1->last_code_end = (gint8*)new1->data [new1->num_elements - 1]->code_start
		+ new1->data [new1->num_elements - 1]->code_size;
	new2->last_code_end = (gint8*)new2->data [new2->num_elements - 1]->code_start
		+ new2->data [new2->num_elements - 1]->code_size;

	*new1p = new1;
	*new2p = new2;
}

static MonoJitInfoTable*
jit_info_table_copy_and_split_chunk (MonoJitInfoTable *table, MonoJitInfoTableChunk *chunk)
{
	MonoJitInfoTable *new_table = g_malloc (sizeof (MonoJitInfoTable)
		+ sizeof (MonoJitInfoTableChunk*) * (table->num_chunks + 1));
	int i, j;

	new_table->domain = table->domain;
	new_table->num_chunks = table->num_chunks + 1;

	j = 0;
	for (i = 0; i < table->num_chunks; ++i) {
		if (table->chunks [i] == chunk) {
			jit_info_table_split_chunk (chunk, &new_table->chunks [j], &new_table->chunks [j + 1]);
			j += 2;
		} else {
			new_table->chunks [j] = table->chunks [i];
			++new_table->chunks [j]->refcount;
			++j;
		}
	}

	g_assert (j == new_table->num_chunks);

	return new_table;
}

static MonoJitInfoTableChunk*
jit_info_table_purify_chunk (MonoJitInfoTableChunk *old)
{
	MonoJitInfoTableChunk *new = jit_info_table_new_chunk ();
	int i, j;

	j = 0;
	for (i = 0; i < old->num_elements; ++i) {
		if (!IS_JIT_INFO_TOMBSTONE (old->data [i]))
			new->data [j++] = old->data [i];
	}

	new->num_elements = j;
	if (new->num_elements > 0)
		new->last_code_end = (gint8*)new->data [j - 1]->code_start + new->data [j - 1]->code_size;
	else
		new->last_code_end = old->last_code_end;

	return new;
}

static MonoJitInfoTable*
jit_info_table_copy_and_purify_chunk (MonoJitInfoTable *table, MonoJitInfoTableChunk *chunk)
{
	MonoJitInfoTable *new_table = g_malloc (sizeof (MonoJitInfoTable)
		+ sizeof (MonoJitInfoTableChunk*) * table->num_chunks);
	int i, j;

	new_table->domain = table->domain;
	new_table->num_chunks = table->num_chunks;

	j = 0;
	for (i = 0; i < table->num_chunks; ++i) {
		if (table->chunks [i] == chunk)
			new_table->chunks [j++] = jit_info_table_purify_chunk (table->chunks [i]);
		else {
			new_table->chunks [j] = table->chunks [i];
			++new_table->chunks [j]->refcount;
			++j;
		}
	}

	g_assert (j == new_table->num_chunks);

	return new_table;
}

/* As we add an element to the table the case can arise that the chunk
 * to which we need to add is already full.  In that case we have to
 * allocate a new table and do something about that chunk.  We have
 * several strategies:
 *
 * If the number of elements in the table is below the low watermark
 * or above the high watermark, we reallocate the whole table.
 * Otherwise we only concern ourselves with the overflowing chunk:
 *
 * If there are no tombstones in the chunk then we split the chunk in
 * two, each half full.
 *
 * If the chunk does contain tombstones, we just make a new copy of
 * the chunk without the tombstones, which will have room for at least
 * the one element we have to add.
 */
static MonoJitInfoTable*
jit_info_table_chunk_overflow (MonoJitInfoTable *table, MonoJitInfoTableChunk *chunk)
{
	int num_elements = jit_info_table_num_elements (table);
	int i;

	if (num_elements < JIT_INFO_TABLE_LOW_WATERMARK (table->num_chunks * MONO_JIT_INFO_TABLE_CHUNK_SIZE)
			|| num_elements > JIT_INFO_TABLE_HIGH_WATERMARK (table->num_chunks * MONO_JIT_INFO_TABLE_CHUNK_SIZE)) {
		//printf ("reallocing table\n");
		return jit_info_table_realloc (table);
	}

	/* count the number of non-tombstone elements in the chunk */
	num_elements = 0;
	for (i = 0; i < chunk->num_elements; ++i) {
		if (!IS_JIT_INFO_TOMBSTONE (chunk->data [i]))
			++num_elements;
	}

	if (num_elements == MONO_JIT_INFO_TABLE_CHUNK_SIZE) {
		//printf ("splitting chunk\n");
		return jit_info_table_copy_and_split_chunk (table, chunk);
	}

	//printf ("purifying chunk\n");
	return jit_info_table_copy_and_purify_chunk (table, chunk);
}

/* We add elements to the table by first making space for them by
 * shifting the elements at the back to the right, one at a time.
 * This results in duplicate entries during the process, but during
 * all the time the table is in a sorted state.  Also, when an element
 * is replaced by another one, the element that replaces it has an end
 * address that is equal to or lower than that of the replaced
 * element.  That property is necessary to guarantee that when
 * searching for an element we end up at a position not higher than
 * the one we're looking for (i.e. we either find the element directly
 * or we end up to the left of it).
 */
void
mono_jit_info_table_add (MonoDomain *domain, MonoJitInfo *ji)
{
	MonoJitInfoTable *table;
	int chunk_pos, pos;
	MonoJitInfoTableChunk *chunk;
	int num_elements;
	int i;

	g_assert (ji->method != NULL);

	mono_domain_lock (domain);

	++mono_stats.jit_info_table_insert_count;

	table = domain->jit_info_table;

 restart:
	chunk_pos = jit_info_table_index (table, (gint8*)ji->code_start + ji->code_size);
	g_assert (chunk_pos < table->num_chunks);
	chunk = table->chunks [chunk_pos];

	if (chunk->num_elements >= MONO_JIT_INFO_TABLE_CHUNK_SIZE) {
		MonoJitInfoTable *new_table = jit_info_table_chunk_overflow (table, chunk);

		/* Debugging code, should be removed. */
		//jit_info_table_check (new_table);

		domain->jit_info_table = new_table;
		mono_memory_barrier ();
		domain->num_jit_info_tables++;
		mono_thread_hazardous_free_or_queue (table, (MonoHazardousFreeFunc)jit_info_table_free);
		table = new_table;

		goto restart;
	}

	/* Debugging code, should be removed. */
	//jit_info_table_check (table);

	num_elements = chunk->num_elements;

	pos = jit_info_table_chunk_index (chunk, NULL, (gint8*)ji->code_start + ji->code_size);

	/* First we need to size up the chunk by one, by copying the
	   last item, or inserting the first one, if the table is
	   empty. */
	if (num_elements > 0)
		chunk->data [num_elements] = chunk->data [num_elements - 1];
	else
		chunk->data [0] = ji;
	mono_memory_write_barrier ();
	chunk->num_elements = ++num_elements;

	/* Shift the elements up one by one. */
	for (i = num_elements - 2; i >= pos; --i) {
		mono_memory_write_barrier ();
		chunk->data [i + 1] = chunk->data [i];
	}

	/* Now we have room and can insert the new item. */
	mono_memory_write_barrier ();
	chunk->data [pos] = ji;

	/* Set the high code end address chunk entry. */
	chunk->last_code_end = (gint8*)chunk->data [chunk->num_elements - 1]->code_start
		+ chunk->data [chunk->num_elements - 1]->code_size;

	/* Debugging code, should be removed. */
	//jit_info_table_check (table);

	mono_domain_unlock (domain);
}

static MonoJitInfo*
mono_jit_info_make_tombstone (MonoJitInfo *ji)
{
	MonoJitInfo *tombstone = g_new0 (MonoJitInfo, 1);

	tombstone->code_start = ji->code_start;
	tombstone->code_size = ji->code_size;
	tombstone->method = JIT_INFO_TOMBSTONE_MARKER;

	return tombstone;
}

/*
 * LOCKING: domain lock
 */
static void
mono_jit_info_free_or_queue (MonoDomain *domain, MonoJitInfo *ji)
{
	if (domain->num_jit_info_tables <= 1) {
		/* Can it actually happen that we only have one table
		   but ji is still hazardous? */
		mono_thread_hazardous_free_or_queue (ji, g_free);
	} else {
		domain->jit_info_free_queue = g_slist_prepend (domain->jit_info_free_queue, ji);
	}
}

void
mono_jit_info_table_remove (MonoDomain *domain, MonoJitInfo *ji)
{
	MonoJitInfoTable *table;
	MonoJitInfoTableChunk *chunk;
	gpointer start = ji->code_start;
	int chunk_pos, pos;

	mono_domain_lock (domain);
	table = domain->jit_info_table;

	++mono_stats.jit_info_table_remove_count;

	chunk_pos = jit_info_table_index (table, start);
	g_assert (chunk_pos < table->num_chunks);

	pos = jit_info_table_chunk_index (table->chunks [chunk_pos], NULL, start);

	do {
		chunk = table->chunks [chunk_pos];

		while (pos < chunk->num_elements) {
			if (chunk->data [pos] == ji)
				goto found;

			g_assert (IS_JIT_INFO_TOMBSTONE (chunk->data [pos]));
			g_assert ((guint8*)chunk->data [pos]->code_start + chunk->data [pos]->code_size
				<= (guint8*)ji->code_start + ji->code_size);

			++pos;
		}

		++chunk_pos;
		pos = 0;
	} while (chunk_pos < table->num_chunks);

 found:
	g_assert (chunk->data [pos] == ji);

	chunk->data [pos] = mono_jit_info_make_tombstone (ji);

	/* Debugging code, should be removed. */
	//jit_info_table_check (table);

	mono_jit_info_free_or_queue (domain, ji);

	mono_domain_unlock (domain);
}

static MonoAotModuleInfoTable*
mono_aot_module_info_table_new (void)
{
	return g_array_new (FALSE, FALSE, sizeof (gpointer));
}

static int
aot_info_table_index (MonoAotModuleInfoTable *table, char *addr)
{
	int left = 0, right = table->len;

	while (left < right) {
		int pos = (left + right) / 2;
		AotModuleInfo *ainfo = g_array_index (table, gpointer, pos);
		char *start = ainfo->start;
		char *end = ainfo->end;

		if (addr < start)
			right = pos;
		else if (addr >= end) 
			left = pos + 1;
		else
			return pos;
	}

	return left;
}

void
mono_jit_info_add_aot_module (MonoImage *image, gpointer start, gpointer end)
{
	AotModuleInfo *ainfo = g_new0 (AotModuleInfo, 1);
	int pos;

	ainfo->image = image;
	ainfo->start = start;
	ainfo->end = end;

	mono_appdomains_lock ();

	if (!aot_modules)
		aot_modules = mono_aot_module_info_table_new ();

	pos = aot_info_table_index (aot_modules, start);

	g_array_insert_val (aot_modules, pos, ainfo);

	mono_appdomains_unlock ();
}

static MonoImage*
mono_jit_info_find_aot_module (guint8* addr)
{
	guint left = 0, right;

	if (!aot_modules)
		return NULL;

	mono_appdomains_lock ();

	right = aot_modules->len;
	while (left < right) {
		guint pos = (left + right) / 2;
		AotModuleInfo *ai = g_array_index (aot_modules, gpointer, pos);

		if (addr < (guint8*)ai->start)
			right = pos;
		else if (addr >= (guint8*)ai->end)
			left = pos + 1;
		else {
			mono_appdomains_unlock ();
			return ai->image;
		}
	}

	mono_appdomains_unlock ();

	return NULL;
}

void
mono_install_jit_info_find_in_aot (MonoJitInfoFindInAot func)
{
	jit_info_find_in_aot_func = func;
}

gpointer
mono_jit_info_get_code_start (MonoJitInfo* ji)
{
	return ji->code_start;
}

int
mono_jit_info_get_code_size (MonoJitInfo* ji)
{
	return ji->code_size;
}

MonoMethod*
mono_jit_info_get_method (MonoJitInfo* ji)
{
	return ji->method;
}

static gpointer
jit_info_key_extract (gpointer value)
{
	MonoJitInfo *info = (MonoJitInfo*)value;

	return info->method;
}

static gpointer*
jit_info_next_value (gpointer value)
{
	MonoJitInfo *info = (MonoJitInfo*)value;

	return (gpointer*)&info->next_jit_code_hash;
}

void
mono_jit_code_hash_init (MonoInternalHashTable *jit_code_hash)
{
	mono_internal_hash_table_init (jit_code_hash,
				       mono_aligned_addr_hash,
				       jit_info_key_extract,
				       jit_info_next_value);
}

MonoGenericJitInfo*
mono_jit_info_get_generic_jit_info (MonoJitInfo *ji)
{
	if (ji->has_generic_jit_info)
		return (MonoGenericJitInfo*)&ji->clauses [ji->num_clauses];
	else
		return NULL;
}

/*
 * mono_jit_info_get_generic_sharing_context:
 * @ji: a jit info
 *
 * Returns the jit info's generic sharing context, or NULL if it
 * doesn't have one.
 */
MonoGenericSharingContext*
mono_jit_info_get_generic_sharing_context (MonoJitInfo *ji)
{
	MonoGenericJitInfo *gi = mono_jit_info_get_generic_jit_info (ji);

	if (gi)
		return gi->generic_sharing_context;
	else
		return NULL;
}

/*
 * mono_jit_info_set_generic_sharing_context:
 * @ji: a jit info
 * @gsctx: a generic sharing context
 *
 * Sets the jit info's generic sharing context.  The jit info must
 * have memory allocated for the context.
 */
void
mono_jit_info_set_generic_sharing_context (MonoJitInfo *ji, MonoGenericSharingContext *gsctx)
{
	MonoGenericJitInfo *gi = mono_jit_info_get_generic_jit_info (ji);

	g_assert (gi);

	gi->generic_sharing_context = gsctx;
}

/**
 * mono_string_equal:
 * @s1: First string to compare
 * @s2: Second string to compare
 *
 * Returns FALSE if the strings differ.
 */
gboolean
mono_string_equal (MonoString *s1, MonoString *s2)
{
	int l1 = mono_string_length (s1);
	int l2 = mono_string_length (s2);

	if (s1 == s2)
		return TRUE;
	if (l1 != l2)
		return FALSE;

	return memcmp (mono_string_chars (s1), mono_string_chars (s2), l1 * 2) == 0; 
}

/**
 * mono_string_hash:
 * @s: the string to hash
 *
 * Returns the hash for the string.
 */
guint
mono_string_hash (MonoString *s)
{
	const guint16 *p = mono_string_chars (s);
	int i, len = mono_string_length (s);
	guint h = 0;

	for (i = 0; i < len; i++) {
		h = (h << 5) - h + *p;
		p++;
	}

	return h;	
}

static gboolean
mono_ptrarray_equal (gpointer *s1, gpointer *s2)
{
	int len = GPOINTER_TO_INT (s1 [0]);
	if (len != GPOINTER_TO_INT (s2 [0]))
		return FALSE;

	return memcmp (s1 + 1, s2 + 1, len * sizeof(gpointer)) == 0; 
}

static guint
mono_ptrarray_hash (gpointer *s)
{
	int i;
	int len = GPOINTER_TO_INT (s [0]);
	guint hash = 0;
	
	for (i = 1; i < len; i++)
		hash += GPOINTER_TO_UINT (s [i]);

	return hash;	
}

/*
 * Allocate an id for domain and set domain->domain_id.
 * LOCKING: must be called while holding appdomains_mutex.
 * We try to assign low numbers to the domain, so it can be used
 * as an index in data tables to lookup domain-specific info
 * with minimal memory overhead. We also try not to reuse the
 * same id too quickly (to help debugging).
 */
static int
domain_id_alloc (MonoDomain *domain)
{
	int id = -1, i;
	if (!appdomains_list) {
		appdomain_list_size = 2;
		appdomains_list = mono_gc_alloc_fixed (appdomain_list_size * sizeof (void*), NULL);
	}
	for (i = appdomain_next; i < appdomain_list_size; ++i) {
		if (!appdomains_list [i]) {
			id = i;
			break;
		}
	}
	if (id == -1) {
		for (i = 0; i < appdomain_next; ++i) {
			if (!appdomains_list [i]) {
				id = i;
				break;
			}
		}
	}
	if (id == -1) {
		MonoDomain **new_list;
		int new_size = appdomain_list_size * 2;
		if (new_size >= (1 << 16))
			g_assert_not_reached ();
		id = appdomain_list_size;
		new_list = mono_gc_alloc_fixed (new_size * sizeof (void*), NULL);
		memcpy (new_list, appdomains_list, appdomain_list_size * sizeof (void*));
		mono_gc_free_fixed (appdomains_list);
		appdomains_list = new_list;
		appdomain_list_size = new_size;
	}
	domain->domain_id = id;
	appdomains_list [id] = domain;
	appdomain_next++;
	if (appdomain_next > appdomain_list_size)
		appdomain_next = 0;
	return id;
}

static guint32 domain_gc_bitmap [sizeof(MonoDomain)/4/32 + 1];
static gpointer domain_gc_desc = NULL;
static guint32 domain_shadow_serial = 0L;

MonoDomain *
mono_domain_create (void)
{
	MonoDomain *domain;
	guint32 shadow_serial;
  
	mono_appdomains_lock ();
	shadow_serial = domain_shadow_serial++;
  
	if (!domain_gc_desc) {
		unsigned int i, bit = 0;
		for (i = G_STRUCT_OFFSET (MonoDomain, MONO_DOMAIN_FIRST_OBJECT); i < G_STRUCT_OFFSET (MonoDomain, MONO_DOMAIN_FIRST_GC_TRACKED); i += sizeof (gpointer)) {
			bit = i / sizeof (gpointer);
			domain_gc_bitmap [bit / 32] |= 1 << (bit % 32);
		}
		domain_gc_desc = mono_gc_make_descr_from_bitmap ((gsize*)domain_gc_bitmap, bit + 1);
	}
	mono_appdomains_unlock ();

	domain = mono_gc_alloc_fixed (sizeof (MonoDomain), domain_gc_desc);
	domain->shadow_serial = shadow_serial;
	domain->domain = NULL;
	domain->setup = NULL;
	domain->friendly_name = NULL;
	domain->search_path = NULL;

	mono_profiler_appdomain_event (domain, MONO_PROFILE_START_LOAD);

	domain->mp = mono_mempool_new ();
	domain->code_mp = mono_code_manager_new ();
	domain->env = mono_g_hash_table_new_type ((GHashFunc)mono_string_hash, (GCompareFunc)mono_string_equal, MONO_HASH_KEY_VALUE_GC);
	domain->domain_assemblies = NULL;
	domain->class_vtable_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->proxy_vtable_hash = g_hash_table_new ((GHashFunc)mono_ptrarray_hash, (GCompareFunc)mono_ptrarray_equal);
	domain->static_data_array = NULL;
	mono_jit_code_hash_init (&domain->jit_code_hash);
	domain->ldstr_table = mono_g_hash_table_new ((GHashFunc)mono_string_hash, (GCompareFunc)mono_string_equal);
	domain->num_jit_info_tables = 1;
	domain->jit_info_table = jit_info_table_new (domain);
	domain->jit_info_free_queue = NULL;
	domain->class_init_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->jump_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->finalizable_objects_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->jit_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->delegate_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	InitializeCriticalSection (&domain->lock);
	InitializeCriticalSection (&domain->assemblies_lock);

	domain->shared_generics_hash = NULL;

	mono_appdomains_lock ();
	domain_id_alloc (domain);
	mono_appdomains_unlock ();

	mono_debug_domain_create (domain);

	mono_profiler_appdomain_loaded (domain, MONO_PROFILE_OK);
	
	return domain;
}

/**
 * mono_init_internal:
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * If exe_filename is not NULL, the method will determine the required runtime
 * from the exe configuration file or the version PE field.
 * If runtime_version is not NULL, that runtime version will be used.
 * Either exe_filename or runtime_version must be provided.
 *
 * Returns: the initial domain.
 */
static MonoDomain *
mono_init_internal (const char *filename, const char *exe_filename, const char *runtime_version)
{
	static MonoDomain *domain = NULL;
	MonoAssembly *ass = NULL;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	const MonoRuntimeInfo* runtimes [G_N_ELEMENTS (supported_runtimes) + 1];
	int n;

	if (domain)
		g_assert_not_reached ();

#if defined(PLATFORM_WIN32) && !defined(_WIN64)
	mono_load_coree (exe_filename);
#endif

	mono_perfcounters_init ();

	mono_counters_register ("Max native code in a domain", MONO_COUNTER_INT|MONO_COUNTER_JIT, &max_domain_code_size);
	mono_counters_register ("Max code space allocated in a domain", MONO_COUNTER_INT|MONO_COUNTER_JIT, &max_domain_code_alloc);
	mono_counters_register ("Total code space allocated", MONO_COUNTER_INT|MONO_COUNTER_JIT, &total_domain_code_alloc);

	mono_gc_base_init ();

	appdomain_thread_id = TlsAlloc ();

	InitializeCriticalSection (&appdomains_mutex);

	mono_metadata_init ();
	mono_raw_buffer_init ();
	mono_images_init ();
	mono_assemblies_init ();
	mono_classes_init ();
	mono_loader_init ();
	mono_reflection_init ();

	/* FIXME: When should we release this memory? */
	MONO_GC_REGISTER_ROOT (appdomains_list);

	domain = mono_domain_create ();
	mono_root_domain = domain;

	SET_APPDOMAIN (domain);
	
	/* Get a list of runtimes supported by the exe */
	if (exe_filename != NULL) {
		/*
		 * This function will load the exe file as a MonoImage. We need to close it, but
		 * that would mean it would be reloaded later. So instead, we save it to
		 * exe_image, and close it during shutdown.
		 */
		get_runtimes_from_exe (exe_filename, &exe_image, runtimes);
#ifdef PLATFORM_WIN32
		if (!exe_image) {
			exe_image = mono_assembly_open_from_bundle (exe_filename, NULL, FALSE);
			if (!exe_image)
				exe_image = mono_image_open (exe_filename, NULL);
		}
		mono_fixup_exe_image (exe_image);
#endif
	} else if (runtime_version != NULL) {
		runtimes [0] = get_runtime_by_version (runtime_version);
		runtimes [1] = NULL;
	}

	if (runtimes [0] == NULL) {
		const MonoRuntimeInfo *default_runtime = get_runtime_by_version (DEFAULT_RUNTIME_VERSION);
		runtimes [0] = default_runtime;
		runtimes [1] = NULL;
		g_print ("WARNING: The runtime version supported by this application is unavailable.\n");
		g_print ("Using default runtime: %s\n", default_runtime->runtime_version);
	}

	/* The selected runtime will be the first one for which there is a mscrolib.dll */
	for (n = 0; runtimes [n] != NULL && ass == NULL; n++) {
		current_runtime = runtimes [n];
		ass = mono_assembly_load_corlib (current_runtime, &status);
		if (status != MONO_IMAGE_OK && status != MONO_IMAGE_ERROR_ERRNO)
			break;

	}
	
	/* Now that we have a runtime, set the policy for unhandled exceptions */
	if (mono_get_runtime_info ()->framework_version [0] < '2') {
		mono_runtime_unhandled_exception_policy_set (MONO_UNHANLED_POLICY_LEGACY);
	}

	if ((status != MONO_IMAGE_OK) || (ass == NULL)) {
		switch (status){
		case MONO_IMAGE_ERROR_ERRNO: {
			char *corlib_file = g_build_filename (mono_assembly_getrootdir (), "mono", current_runtime->framework_version, "mscorlib.dll", NULL);
			g_print ("The assembly mscorlib.dll was not found or could not be loaded.\n");
			g_print ("It should have been installed in the `%s' directory.\n", corlib_file);
			g_free (corlib_file);
			break;
		}
		case MONO_IMAGE_IMAGE_INVALID:
			g_print ("The file %s/mscorlib.dll is an invalid CIL image\n",
				 mono_assembly_getrootdir ());
			break;
		case MONO_IMAGE_MISSING_ASSEMBLYREF:
			g_print ("Missing assembly reference in %s/mscorlib.dll\n",
				 mono_assembly_getrootdir ());
			break;
		case MONO_IMAGE_OK:
			/* to suppress compiler warning */
			break;
		}
		
		exit (1);
	}
	mono_defaults.corlib = mono_assembly_get_image (ass);

	mono_defaults.object_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Object");
	g_assert (mono_defaults.object_class != 0);

	mono_defaults.void_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Void");
	g_assert (mono_defaults.void_class != 0);

	mono_defaults.boolean_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Boolean");
	g_assert (mono_defaults.boolean_class != 0);

	mono_defaults.byte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Byte");
	g_assert (mono_defaults.byte_class != 0);

	mono_defaults.sbyte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "SByte");
	g_assert (mono_defaults.sbyte_class != 0);

	mono_defaults.int16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int16");
	g_assert (mono_defaults.int16_class != 0);

	mono_defaults.uint16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt16");
	g_assert (mono_defaults.uint16_class != 0);

	mono_defaults.int32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int32");
	g_assert (mono_defaults.int32_class != 0);

	mono_defaults.uint32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt32");
	g_assert (mono_defaults.uint32_class != 0);

	mono_defaults.uint_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UIntPtr");
	g_assert (mono_defaults.uint_class != 0);

	mono_defaults.int_class = mono_class_from_name (
                mono_defaults.corlib, "System", "IntPtr");
	g_assert (mono_defaults.int_class != 0);

	mono_defaults.int64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int64");
	g_assert (mono_defaults.int64_class != 0);

	mono_defaults.uint64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt64");
	g_assert (mono_defaults.uint64_class != 0);

	mono_defaults.single_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Single");
	g_assert (mono_defaults.single_class != 0);

	mono_defaults.double_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Double");
	g_assert (mono_defaults.double_class != 0);

	mono_defaults.char_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Char");
	g_assert (mono_defaults.char_class != 0);

	mono_defaults.string_class = mono_class_from_name (
                mono_defaults.corlib, "System", "String");
	g_assert (mono_defaults.string_class != 0);

	mono_defaults.enum_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Enum");
	g_assert (mono_defaults.enum_class != 0);

	mono_defaults.array_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Array");
	g_assert (mono_defaults.array_class != 0);

	mono_defaults.delegate_class = mono_class_from_name (
		mono_defaults.corlib, "System", "Delegate");
	g_assert (mono_defaults.delegate_class != 0 );

	mono_defaults.multicastdelegate_class = mono_class_from_name (
		mono_defaults.corlib, "System", "MulticastDelegate");
	g_assert (mono_defaults.multicastdelegate_class != 0 );

	mono_defaults.asyncresult_class = mono_class_from_name (
		mono_defaults.corlib, "System.Runtime.Remoting.Messaging", 
		"AsyncResult");
	g_assert (mono_defaults.asyncresult_class != 0 );

	mono_defaults.waithandle_class = mono_class_from_name (
		mono_defaults.corlib, "System.Threading", "WaitHandle");
	g_assert (mono_defaults.waithandle_class != 0 );

	mono_defaults.typehandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeTypeHandle");
	g_assert (mono_defaults.typehandle_class != 0);

	mono_defaults.methodhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeMethodHandle");
	g_assert (mono_defaults.methodhandle_class != 0);

	mono_defaults.fieldhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeFieldHandle");
	g_assert (mono_defaults.fieldhandle_class != 0);

	mono_defaults.systemtype_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Type");
	g_assert (mono_defaults.systemtype_class != 0);

	mono_defaults.monotype_class = mono_class_from_name (
                mono_defaults.corlib, "System", "MonoType");
	g_assert (mono_defaults.monotype_class != 0);

	mono_defaults.exception_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Exception");
	g_assert (mono_defaults.exception_class != 0);

	mono_defaults.threadabortexception_class = mono_class_from_name (
                mono_defaults.corlib, "System.Threading", "ThreadAbortException");
	g_assert (mono_defaults.threadabortexception_class != 0);

	mono_defaults.thread_class = mono_class_from_name (
                mono_defaults.corlib, "System.Threading", "Thread");
	g_assert (mono_defaults.thread_class != 0);

	mono_defaults.appdomain_class = mono_class_from_name (
                mono_defaults.corlib, "System", "AppDomain");
	g_assert (mono_defaults.appdomain_class != 0);

	mono_defaults.transparent_proxy_class = mono_class_from_name (
                mono_defaults.corlib, "System.Runtime.Remoting.Proxies", "TransparentProxy");
	g_assert (mono_defaults.transparent_proxy_class != 0);

	mono_defaults.real_proxy_class = mono_class_from_name (
                mono_defaults.corlib, "System.Runtime.Remoting.Proxies", "RealProxy");
	g_assert (mono_defaults.real_proxy_class != 0);

	mono_defaults.mono_method_message_class = mono_class_from_name (
                mono_defaults.corlib, "System.Runtime.Remoting.Messaging", "MonoMethodMessage");
	g_assert (mono_defaults.mono_method_message_class != 0);

	mono_defaults.field_info_class = mono_class_from_name (
		mono_defaults.corlib, "System.Reflection", "FieldInfo");
	g_assert (mono_defaults.field_info_class != 0);

	mono_defaults.method_info_class = mono_class_from_name (
		mono_defaults.corlib, "System.Reflection", "MethodInfo");
	g_assert (mono_defaults.method_info_class != 0);

	mono_defaults.stringbuilder_class = mono_class_from_name (
		mono_defaults.corlib, "System.Text", "StringBuilder");
	g_assert (mono_defaults.stringbuilder_class != 0);

	mono_defaults.math_class = mono_class_from_name (
	        mono_defaults.corlib, "System", "Math");
	g_assert (mono_defaults.math_class != 0);

	mono_defaults.stack_frame_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Diagnostics", "StackFrame");
	g_assert (mono_defaults.stack_frame_class != 0);

	mono_defaults.stack_trace_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Diagnostics", "StackTrace");
	g_assert (mono_defaults.stack_trace_class != 0);

	mono_defaults.marshal_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.InteropServices", "Marshal");
	g_assert (mono_defaults.marshal_class != 0);

	mono_defaults.iserializeable_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Serialization", "ISerializable");
	g_assert (mono_defaults.iserializeable_class != 0);

	mono_defaults.serializationinfo_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Serialization", "SerializationInfo");
	g_assert (mono_defaults.serializationinfo_class != 0);

	mono_defaults.streamingcontext_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Serialization", "StreamingContext");
	g_assert (mono_defaults.streamingcontext_class != 0);

	mono_defaults.typed_reference_class =  mono_class_from_name (
	        mono_defaults.corlib, "System", "TypedReference");
	g_assert (mono_defaults.typed_reference_class != 0);

	mono_defaults.argumenthandle_class =  mono_class_from_name (
	        mono_defaults.corlib, "System", "RuntimeArgumentHandle");
	g_assert (mono_defaults.argumenthandle_class != 0);

	mono_defaults.marshalbyrefobject_class =  mono_class_from_name (
	        mono_defaults.corlib, "System", "MarshalByRefObject");
	g_assert (mono_defaults.marshalbyrefobject_class != 0);

	mono_defaults.monitor_class =  mono_class_from_name (
	        mono_defaults.corlib, "System.Threading", "Monitor");
	g_assert (mono_defaults.monitor_class != 0);

	mono_defaults.iremotingtypeinfo_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Remoting", "IRemotingTypeInfo");
	g_assert (mono_defaults.iremotingtypeinfo_class != 0);

	mono_defaults.runtimesecurityframe_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Security", "RuntimeSecurityFrame");

	mono_defaults.executioncontext_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Threading", "ExecutionContext");

	mono_defaults.internals_visible_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.CompilerServices", "InternalsVisibleToAttribute");

	/*
	 * mscorlib needs a little help, only now it can load its friends list (after we have
	 * loaded the InternalsVisibleToAttribute), load it now
	 */
	mono_assembly_load_friends (ass);
	
	mono_defaults.safehandle_class = mono_class_from_name (
		mono_defaults.corlib, "System.Runtime.InteropServices", "SafeHandle");

	mono_defaults.handleref_class = mono_class_from_name (
		mono_defaults.corlib, "System.Runtime.InteropServices", "HandleRef");

	mono_defaults.attribute_class = mono_class_from_name (
		mono_defaults.corlib, "System", "Attribute");

	mono_defaults.customattribute_data_class = mono_class_from_name (
		mono_defaults.corlib, "System.Reflection", "CustomAttributeData");

	/* these are initialized lazily when COM features are used */
	mono_defaults.variant_class = NULL;
	mono_defaults.com_object_class = NULL;
	mono_defaults.com_interop_proxy_class = NULL;
	mono_defaults.iunknown_class = NULL;
	mono_defaults.idispatch_class = NULL;

	/*
	 * Note that mono_defaults.generic_*_class is only non-NULL if we're
	 * using the 2.0 corlib.
	 */
	mono_class_init (mono_defaults.array_class);
	mono_defaults.generic_nullable_class = mono_class_from_name (
		mono_defaults.corlib, "System", "Nullable`1");
	mono_defaults.generic_ilist_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Collections.Generic", "IList`1");

	domain->friendly_name = g_path_get_basename (filename);

	_mono_debug_init_corlib (domain);

	return domain;
}

/**
 * mono_init:
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the default runtime version.
 *
 * Returns: the initial domain.
 */
MonoDomain *
mono_init (const char *domain_name)
{
	return mono_init_internal (domain_name, NULL, DEFAULT_RUNTIME_VERSION);
}

/**
 * mono_init_from_assembly:
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the runtime version required by the
 * provided executable. The version is determined by looking at the exe 
 * configuration file and the version PE field)
 *
 * Returns: the initial domain.
 */
MonoDomain *
mono_init_from_assembly (const char *domain_name, const char *filename)
{
	return mono_init_internal (domain_name, filename, NULL);
}

/**
 * mono_init_version:
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the provided rutime version.
 *
 * Returns: the initial domain.
 */
MonoDomain *
mono_init_version (const char *domain_name, const char *version)
{
	return mono_init_internal (domain_name, NULL, version);
}

/**
 * mono_init_com_types:
 *
 * Initializes all types needed for COM Interop in mono_defaults structure. 
 */
void 
mono_init_com_types (void)
{
	static gboolean initialized = FALSE;

	if (initialized)
		return;
	
	/* FIXME: do I need some threading protection here */

	g_assert (mono_defaults.corlib);

	mono_defaults.variant_class = mono_class_from_name (
	        mono_defaults.corlib, "System", "Variant");
	g_assert (mono_defaults.variant_class != 0);

	mono_defaults.com_object_class = mono_class_from_name (
	        mono_defaults.corlib, "System", "__ComObject");
	g_assert (mono_defaults.com_object_class != 0);

	mono_defaults.com_interop_proxy_class = mono_class_from_name (
	        mono_defaults.corlib, "Mono.Interop", "ComInteropProxy");
	g_assert (mono_defaults.com_interop_proxy_class != 0);

	mono_defaults.iunknown_class = mono_class_from_name (
	        mono_defaults.corlib, "Mono.Interop", "IUnknown");
	g_assert (mono_defaults.iunknown_class != 0);

	mono_defaults.idispatch_class = mono_class_from_name (
	        mono_defaults.corlib, "Mono.Interop", "IDispatch");
	g_assert (mono_defaults.idispatch_class != 0);

	initialized = TRUE;
}

/**
 * mono_cleanup:
 *
 * Cleans up all metadata modules. 
 */
void
mono_cleanup (void)
{
	mono_close_exe_image ();

	mono_loader_cleanup ();
	mono_classes_cleanup ();
	mono_assemblies_cleanup ();
	mono_images_cleanup ();
	mono_debug_cleanup ();
	mono_raw_buffer_cleanup ();
	mono_metadata_cleanup ();

	TlsFree (appdomain_thread_id);
	DeleteCriticalSection (&appdomains_mutex);
}

void
mono_close_exe_image (void)
{
	if (exe_image)
		mono_image_close (exe_image);
}

/**
 * mono_get_root_domain:
 *
 * The root AppDomain is the initial domain created by the runtime when it is
 * initialized.  Programs execute on this AppDomain, but can create new ones
 * later.   Currently there is no unmanaged API to create new AppDomains, this
 * must be done from managed code.
 *
 * Returns: the root appdomain, to obtain the current domain, use mono_domain_get ()
 */
MonoDomain*
mono_get_root_domain (void)
{
	return mono_root_domain;
}

/**
 * mono_domain_get:
 *
 * Returns: the current domain, to obtain the root domain use
 * mono_get_root_domain().
 */
MonoDomain *
mono_domain_get ()
{
	return GET_APPDOMAIN ();
}

/**
 * mono_domain_set_internal:
 * @domain: the new domain
 *
 * Sets the current domain to @domain.
 */
void
mono_domain_set_internal (MonoDomain *domain)
{
	SET_APPDOMAIN (domain);
	SET_APPCONTEXT (domain->default_context);
}

void
mono_domain_foreach (MonoDomainFunc func, gpointer user_data)
{
	int i, size;
	MonoDomain **copy;

	/*
	 * Create a copy of the data to avoid calling the user callback
	 * inside the lock because that could lead to deadlocks.
	 * We can do this because this function is not perf. critical.
	 */
	mono_appdomains_lock ();
	size = appdomain_list_size;
	copy = mono_gc_alloc_fixed (appdomain_list_size * sizeof (void*), NULL);
	memcpy (copy, appdomains_list, appdomain_list_size * sizeof (void*));
	mono_appdomains_unlock ();

	for (i = 0; i < size; ++i) {
		if (copy [i])
			func (copy [i], user_data);
	}

	mono_gc_free_fixed (copy);
}

/**
 * mono_domain_assembly_open:
 * @domain: the application domain
 * @name: file name of the assembly
 *
 * fixme: maybe we should integrate this with mono_assembly_open ??
 */
MonoAssembly *
mono_domain_assembly_open (MonoDomain *domain, const char *name)
{
	MonoAssembly *ass;
	GSList *tmp;

	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (strcmp (name, ass->aname.name) == 0) {
			mono_domain_assemblies_unlock (domain);
			return ass;
		}
	}
	mono_domain_assemblies_unlock (domain);

	if (!(ass = mono_assembly_open (name, NULL)))
		return NULL;

	return ass;
}

MonoJitInfo*
mono_domain_lookup_shared_generic (MonoDomain *domain, MonoMethod *method)
{
	if (!domain->shared_generics_hash)
		return NULL;

	return g_hash_table_lookup (domain->shared_generics_hash, method);
}

void
mono_domain_register_shared_generic (MonoDomain *domain, MonoMethod *method, MonoJitInfo *jit_info)
{
	if (!domain->shared_generics_hash)
		domain->shared_generics_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	g_assert (domain->shared_generics_hash);

	g_hash_table_insert (domain->shared_generics_hash, method, jit_info);
}

static void
dynamic_method_info_free (gpointer key, gpointer value, gpointer user_data)
{
	MonoJitDynamicMethodInfo *di = value;
	mono_code_manager_destroy (di->code_mp);
	g_free (di);
}

static void
delete_jump_list (gpointer key, gpointer value, gpointer user_data)
{
	g_slist_free (value);
}

void
mono_domain_free (MonoDomain *domain, gboolean force)
{
	int code_size, code_alloc;
	GSList *tmp;
	if ((domain == mono_root_domain) && !force) {
		g_warning ("cant unload root domain");
		return;
	}

	mono_profiler_appdomain_event (domain, MONO_PROFILE_START_UNLOAD);

	mono_debug_domain_unload (domain);

	mono_appdomains_lock ();
	appdomains_list [domain->domain_id] = NULL;
	mono_appdomains_unlock ();

	/* FIXME: free delegate_hash_table when it's used */
	if (domain->search_path) {
		g_strfreev (domain->search_path);
		domain->search_path = NULL;
	}
	domain->create_proxy_for_type_method = NULL;
	domain->private_invoke_method = NULL;
	domain->default_context = NULL;
	domain->out_of_memory_ex = NULL;
	domain->null_reference_ex = NULL;
	domain->stack_overflow_ex = NULL;
	domain->entry_assembly = NULL;
	/* must do this early as it accesses fields and types */
	if (domain->special_static_fields) {
		mono_alloc_special_static_data_free (domain->special_static_fields);
		g_hash_table_destroy (domain->special_static_fields);
		domain->special_static_fields = NULL;
	}
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *ass = tmp->data;
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Unloading domain %s %p, assembly %s %p, refcount=%d\n", domain->friendly_name, domain, ass->aname.name, ass, ass->ref_count);
		mono_assembly_close (ass);
	}
	g_slist_free (domain->domain_assemblies);
	domain->domain_assemblies = NULL;

	g_free (domain->friendly_name);
	domain->friendly_name = NULL;
	mono_g_hash_table_destroy (domain->env);
	domain->env = NULL;
	g_hash_table_destroy (domain->class_vtable_hash);
	domain->class_vtable_hash = NULL;
	g_hash_table_destroy (domain->proxy_vtable_hash);
	domain->proxy_vtable_hash = NULL;
	if (domain->static_data_array) {
		mono_gc_free_fixed (domain->static_data_array);
		domain->static_data_array = NULL;
	}
	mono_internal_hash_table_destroy (&domain->jit_code_hash);
	if (domain->dynamic_code_hash) {
		g_hash_table_foreach (domain->dynamic_code_hash, dynamic_method_info_free, NULL);
		g_hash_table_destroy (domain->dynamic_code_hash);
		domain->dynamic_code_hash = NULL;
	}
	mono_g_hash_table_destroy (domain->ldstr_table);
	domain->ldstr_table = NULL;

	/*
	 * There might still be jit info tables of this domain which
	 * are not freed.  Since the domain cannot be in use anymore,
	 * this will free them.
	 */
	mono_thread_hazardous_try_free_all ();
	g_assert (domain->num_jit_info_tables == 1);
	jit_info_table_free (domain->jit_info_table);
	domain->jit_info_table = NULL;
	g_assert (!domain->jit_info_free_queue);

	/* collect statistics */
	code_alloc = mono_code_manager_size (domain->code_mp, &code_size);
	total_domain_code_alloc += code_alloc;
	max_domain_code_alloc = MAX (max_domain_code_alloc, code_alloc);
	max_domain_code_size = MAX (max_domain_code_size, code_size);

#ifdef DEBUG_DOMAIN_UNLOAD
	mono_mempool_invalidate (domain->mp);
	mono_code_manager_invalidate (domain->code_mp);
#else
	mono_mempool_destroy (domain->mp);
	domain->mp = NULL;
	mono_code_manager_destroy (domain->code_mp);
	domain->code_mp = NULL;
#endif	
	if (domain->jump_target_hash) {
		g_hash_table_foreach (domain->jump_target_hash, delete_jump_list, NULL);
		g_hash_table_destroy (domain->jump_target_hash);
		domain->jump_target_hash = NULL;
	}
	if (domain->type_hash) {
		mono_g_hash_table_destroy (domain->type_hash);
		domain->type_hash = NULL;
	}
	if (domain->refobject_hash) {
		mono_g_hash_table_destroy (domain->refobject_hash);
		domain->refobject_hash = NULL;
	}
	if (domain->type_init_exception_hash) {
		mono_g_hash_table_destroy (domain->type_init_exception_hash);
		domain->type_init_exception_hash = NULL;
	}
	g_hash_table_destroy (domain->class_init_trampoline_hash);
	domain->class_init_trampoline_hash = NULL;
	g_hash_table_destroy (domain->jump_trampoline_hash);
	domain->jump_trampoline_hash = NULL;
	g_hash_table_destroy (domain->finalizable_objects_hash);
	domain->finalizable_objects_hash = NULL;
	g_hash_table_destroy (domain->jit_trampoline_hash);
	domain->jit_trampoline_hash = NULL;
	g_hash_table_destroy (domain->delegate_trampoline_hash);
	domain->delegate_trampoline_hash = NULL;
	if (domain->shared_generics_hash) {
		g_hash_table_destroy (domain->shared_generics_hash);
		domain->shared_generics_hash = NULL;
	}

	DeleteCriticalSection (&domain->assemblies_lock);
	DeleteCriticalSection (&domain->lock);
	domain->setup = NULL;

	/* FIXME: anything else required ? */

	mono_profiler_appdomain_event (domain, MONO_PROFILE_END_UNLOAD);

	mono_gc_free_fixed (domain);

	if ((domain == mono_root_domain))
		mono_root_domain = NULL;
}

/**
 * mono_domain_get_id:
 * @domainid: the ID
 *
 * Returns: the a domain for a specific domain id.
 */
MonoDomain * 
mono_domain_get_by_id (gint32 domainid) 
{
	MonoDomain * domain;

	mono_appdomains_lock ();
	if (domainid < appdomain_list_size)
		domain = appdomains_list [domainid];
	else
		domain = NULL;
	mono_appdomains_unlock ();

	return domain;
}

gint32
mono_domain_get_id (MonoDomain *domain)
{
	return domain->domain_id;
}

void 
mono_context_set (MonoAppContext * new_context)
{
	SET_APPCONTEXT (new_context);
}

MonoAppContext * 
mono_context_get (void)
{
	return GET_APPCONTEXT ();
}

/* LOCKING: the caller holds the lock for this domain */
void
mono_domain_add_class_static_data (MonoDomain *domain, MonoClass *klass, gpointer data, guint32 *bitmap)
{
	/* The first entry in the array is the index of the next free slot
	 * and the total size of the array
	 */
	int next;
	if (domain->static_data_array) {
		int size = GPOINTER_TO_INT (domain->static_data_array [1]);
		next = GPOINTER_TO_INT (domain->static_data_array [0]);
		if (next >= size) {
			gpointer *new_array = mono_gc_alloc_fixed (sizeof (gpointer) * (size * 2), NULL);
			memcpy (new_array, domain->static_data_array, sizeof (gpointer) * size);
			size *= 2;
			new_array [1] = GINT_TO_POINTER (size);
			mono_gc_free_fixed (domain->static_data_array);
			domain->static_data_array = new_array;
		}
	} else {
		int size = 32;
		gpointer *new_array = mono_gc_alloc_fixed (sizeof (gpointer) * size, NULL);
		next = 2;
		new_array [0] = GINT_TO_POINTER (next);
		new_array [1] = GINT_TO_POINTER (size);
		domain->static_data_array = new_array;
	}
	domain->static_data_array [next++] = data;
	domain->static_data_array [0] = GINT_TO_POINTER (next);
}

MonoImage*
mono_get_corlib (void)
{
	return mono_defaults.corlib;
}

MonoClass*
mono_get_object_class (void)
{
	return mono_defaults.object_class;
}

MonoClass*
mono_get_byte_class (void)
{
	return mono_defaults.byte_class;
}

MonoClass*
mono_get_void_class (void)
{
	return mono_defaults.void_class;
}

MonoClass*
mono_get_boolean_class (void)
{
	return mono_defaults.boolean_class;
}

MonoClass*
mono_get_sbyte_class (void)
{
	return mono_defaults.sbyte_class;
}

MonoClass*
mono_get_int16_class (void)
{
	return mono_defaults.int16_class;
}

MonoClass*
mono_get_uint16_class (void)
{
	return mono_defaults.uint16_class;
}

MonoClass*
mono_get_int32_class (void)
{
	return mono_defaults.int32_class;
}

MonoClass*
mono_get_uint32_class (void)
{
	return mono_defaults.uint32_class;
}

MonoClass*
mono_get_intptr_class (void)
{
	return mono_defaults.int_class;
}

MonoClass*
mono_get_uintptr_class (void)
{
	return mono_defaults.uint_class;
}

MonoClass*
mono_get_int64_class (void)
{
	return mono_defaults.int64_class;
}

MonoClass*
mono_get_uint64_class (void)
{
	return mono_defaults.uint64_class;
}

MonoClass*
mono_get_single_class (void)
{
	return mono_defaults.single_class;
}

MonoClass*
mono_get_double_class (void)
{
	return mono_defaults.double_class;
}

MonoClass*
mono_get_char_class (void)
{
	return mono_defaults.char_class;
}

MonoClass*
mono_get_string_class (void)
{
	return mono_defaults.string_class;
}

MonoClass*
mono_get_enum_class (void)
{
	return mono_defaults.enum_class;
}

MonoClass*
mono_get_array_class (void)
{
	return mono_defaults.array_class;
}

MonoClass*
mono_get_thread_class (void)
{
	return mono_defaults.thread_class;
}

MonoClass*
mono_get_exception_class (void)
{
	return mono_defaults.exception_class;
}


static char* get_attribute_value (const gchar **attribute_names, 
					const gchar **attribute_values, 
					const char *att_name)
{
	int n;
	for (n=0; attribute_names[n] != NULL; n++) {
		if (strcmp (attribute_names[n], att_name) == 0)
			return g_strdup (attribute_values[n]);
	}
	return NULL;
}

static void start_element (GMarkupParseContext *context, 
                           const gchar         *element_name,
			   const gchar        **attribute_names,
			   const gchar        **attribute_values,
			   gpointer             user_data,
			   GError             **error)
{
	AppConfigInfo* app_config = (AppConfigInfo*) user_data;
	
	if (strcmp (element_name, "configuration") == 0) {
		app_config->configuration_count++;
		return;
	}
	if (strcmp (element_name, "startup") == 0) {
		app_config->startup_count++;
		return;
	}
	
	if (app_config->configuration_count != 1 || app_config->startup_count != 1)
		return;
	
	if (strcmp (element_name, "requiredRuntime") == 0) {
		app_config->required_runtime = get_attribute_value (attribute_names, attribute_values, "version");
	} else if (strcmp (element_name, "supportedRuntime") == 0) {
		char *version = get_attribute_value (attribute_names, attribute_values, "version");
		app_config->supported_runtimes = g_slist_append (app_config->supported_runtimes, version);
	}
}

static void end_element   (GMarkupParseContext *context,
                           const gchar         *element_name,
			   gpointer             user_data,
			   GError             **error)
{
	AppConfigInfo* app_config = (AppConfigInfo*) user_data;
	
	if (strcmp (element_name, "configuration") == 0) {
		app_config->configuration_count--;
	} else if (strcmp (element_name, "startup") == 0) {
		app_config->startup_count--;
	}
}

static const GMarkupParser 
mono_parser = {
	start_element,
	end_element,
	NULL,
	NULL,
	NULL
};

static AppConfigInfo *
app_config_parse (const char *exe_filename)
{
	AppConfigInfo *app_config;
	GMarkupParseContext *context;
	char *text;
	gsize len;
	struct stat buf;
	const char *bundled_config;
	char *config_filename;

	bundled_config = mono_config_string_for_assembly_file (exe_filename);

	if (bundled_config) {
		text = g_strdup (bundled_config);
		len = strlen (text);
	} else {
		config_filename = g_strconcat (exe_filename, ".config", NULL);

		if (stat (config_filename, &buf) != 0) {
			g_free (config_filename);
			return NULL;
		}
	
		if (!g_file_get_contents (config_filename, &text, &len, NULL)) {
			g_free (config_filename);
			return NULL;
		}
		g_free (config_filename);
	}

	app_config = g_new0 (AppConfigInfo, 1);

	context = g_markup_parse_context_new (&mono_parser, 0, app_config, NULL);
	if (g_markup_parse_context_parse (context, text, len, NULL)) {
		g_markup_parse_context_end_parse (context, NULL);
	}
	g_markup_parse_context_free (context);
	g_free (text);
	return app_config;
}

static void 
app_config_free (AppConfigInfo* app_config)
{
	char *rt;
	GSList *list = app_config->supported_runtimes;
	while (list != NULL) {
		rt = (char*)list->data;
		g_free (rt);
		list = g_slist_next (list);
	}
	g_slist_free (app_config->supported_runtimes);
	g_free (app_config->required_runtime);
	g_free (app_config);
}


static const MonoRuntimeInfo*
get_runtime_by_version (const char *version)
{
	int n;
	int max = G_N_ELEMENTS (supported_runtimes);
	
	for (n=0; n<max; n++) {
		if (strcmp (version, supported_runtimes[n].runtime_version) == 0)
			return &supported_runtimes[n];
	}
	return NULL;
}

static void
get_runtimes_from_exe (const char *exe_file, MonoImage **exe_image, const MonoRuntimeInfo** runtimes)
{
	AppConfigInfo* app_config;
	char *version;
	const MonoRuntimeInfo* runtime = NULL;
	MonoImage *image = NULL;
	
	app_config = app_config_parse (exe_file);
	
	if (app_config != NULL) {
		/* Check supportedRuntime elements, if none is supported, fail.
		 * If there are no such elements, look for a requiredRuntime element.
		 */
		if (app_config->supported_runtimes != NULL) {
			int n = 0;
			GSList *list = app_config->supported_runtimes;
			while (list != NULL) {
				version = (char*) list->data;
				runtime = get_runtime_by_version (version);
				if (runtime != NULL)
					runtimes [n++] = runtime;
				list = g_slist_next (list);
			}
			runtimes [n] = NULL;
			app_config_free (app_config);
			return;
		}
		
		/* Check the requiredRuntime element. This is for 1.0 apps only. */
		if (app_config->required_runtime != NULL) {
			runtimes [0] = get_runtime_by_version (app_config->required_runtime);
			runtimes [1] = NULL;
			app_config_free (app_config);
			return;
		}
		app_config_free (app_config);
	}
	
	/* Look for a runtime with the exact version */
	image = mono_assembly_open_from_bundle (exe_file, NULL, FALSE);

	if (image == NULL)
		image = mono_image_open (exe_file, NULL);

	if (image == NULL) {
		/* The image is wrong or the file was not found. In this case return
		 * a default runtime and leave to the initialization method the work of
		 * reporting the error.
		 */
		runtimes [0] = get_runtime_by_version (DEFAULT_RUNTIME_VERSION);
		runtimes [1] = NULL;
		return;
	}

	*exe_image = image;

	runtimes [0] = get_runtime_by_version (image->version);
	runtimes [1] = NULL;
}


/**
 * mono_get_runtime_info:
 *
 * Returns: the version of the current runtime instance.
 */
const MonoRuntimeInfo*
mono_get_runtime_info (void)
{
	return current_runtime;
}

gchar *
mono_debugger_check_runtime_version (const char *filename)
{
	const MonoRuntimeInfo* runtimes [G_N_ELEMENTS (supported_runtimes) + 1];
	const MonoRuntimeInfo *rinfo;
	MonoImage *image;

	get_runtimes_from_exe (filename, &image, runtimes);
	rinfo = runtimes [0];

	if (!rinfo)
		return g_strdup_printf ("Cannot get runtime version from assembly `%s'", filename);

	if (rinfo != current_runtime)
		return g_strdup_printf ("The Mono Debugger is currently using the `%s' runtime, but "
					"the assembly `%s' requires version `%s'", current_runtime->runtime_version,
					filename, rinfo->runtime_version);

	return NULL;
}
