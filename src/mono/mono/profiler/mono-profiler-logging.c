#include <config.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/class.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-gc.h>
#include <mono/io-layer/atomic.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include <glib.h>

#include <dlfcn.h>

#define HAS_OPROFILE 0

#if (HAS_OPROFILE)
#include <libopagent.h>
#endif

// Needed for heap analysis
extern gboolean mono_object_is_alive (MonoObject* obj);

typedef enum {
	MONO_PROFILER_FILE_BLOCK_KIND_INTRO = 1,
	MONO_PROFILER_FILE_BLOCK_KIND_END = 2,
	MONO_PROFILER_FILE_BLOCK_KIND_MAPPING = 3,
	MONO_PROFILER_FILE_BLOCK_KIND_LOADED = 4,
	MONO_PROFILER_FILE_BLOCK_KIND_UNLOADED = 5,
	MONO_PROFILER_FILE_BLOCK_KIND_EVENTS = 6,
	MONO_PROFILER_FILE_BLOCK_KIND_STATISTICAL = 7,
	MONO_PROFILER_FILE_BLOCK_KIND_HEAP = 8
} MonoProfilerFileBlockKind;

#define MONO_PROFILER_LOADED_EVENT_MODULE     1
#define MONO_PROFILER_LOADED_EVENT_ASSEMBLY   2
#define MONO_PROFILER_LOADED_EVENT_APPDOMAIN  4
#define MONO_PROFILER_LOADED_EVENT_SUCCESS    8
#define MONO_PROFILER_LOADED_EVENT_FAILURE   16

typedef enum {
	MONO_PROFILER_EVENT_DATA_TYPE_OTHER = 0,
	MONO_PROFILER_EVENT_DATA_TYPE_METHOD = 1,
	MONO_PROFILER_EVENT_DATA_TYPE_CLASS = 2
} MonoProfilerEventDataType;

typedef struct _ProfilerEventData {
	union {
		gpointer address;
		gsize number;
	} data;
	unsigned int data_type:2;
	unsigned int code:3;
	unsigned int kind:1;
	unsigned int value:26;
} ProfilerEventData;

#define EVENT_VALUE_BITS (26)
#define MAX_EVENT_VALUE ((1<<EVENT_VALUE_BITS)-1)

typedef enum {
	MONO_PROFILER_EVENT_METHOD_JIT = 0,
	MONO_PROFILER_EVENT_METHOD_FREED = 1,
	MONO_PROFILER_EVENT_METHOD_CALL = 2
} MonoProfilerMethodEvents;
typedef enum {
	MONO_PROFILER_EVENT_CLASS_LOAD = 0,
	MONO_PROFILER_EVENT_CLASS_UNLOAD = 1,
	MONO_PROFILER_EVENT_CLASS_EXCEPTION = 2,
	MONO_PROFILER_EVENT_CLASS_ALLOCATION = 3
} MonoProfilerClassEvents;
typedef enum {
	MONO_PROFILER_EVENT_RESULT_SUCCESS = 0,
	MONO_PROFILER_EVENT_RESULT_FAILURE = 4
} MonoProfilerEventResult;
#define MONO_PROFILER_EVENT_RESULT_MASK MONO_PROFILER_EVENT_RESULT_FAILURE
typedef enum {
	MONO_PROFILER_EVENT_THREAD = 1,
	MONO_PROFILER_EVENT_GC_COLLECTION = 2,
	MONO_PROFILER_EVENT_GC_MARK = 3,
	MONO_PROFILER_EVENT_GC_SWEEP = 4,
	MONO_PROFILER_EVENT_GC_RESIZE = 5,
	MONO_PROFILER_EVENT_GC_STOP_WORLD = 6,
	MONO_PROFILER_EVENT_GC_START_WORLD = 7
} MonoProfilerEvents;
typedef enum {
	MONO_PROFILER_EVENT_KIND_START = 0,
	MONO_PROFILER_EVENT_KIND_END = 1
} MonoProfilerEventKind;

#define MONO_PROFILER_GET_CURRENT_TIME(t) {\
	struct timeval current_time;\
	gettimeofday (&current_time, NULL);\
	(t) = (((guint64)current_time.tv_sec) * 1000000) + current_time.tv_usec;\
} while (0)

static gboolean use_fast_timer = FALSE;

#if (defined(__i386__) || defined(__x86_64__)) && ! defined(PLATFORM_WIN32)

#if defined(__i386__)
static const guchar cpuid_impl [] = {
	0x55,                   	/* push   %ebp */
	0x89, 0xe5,                	/* mov    %esp,%ebp */
	0x53,                   	/* push   %ebx */
	0x8b, 0x45, 0x08,             	/* mov    0x8(%ebp),%eax */
	0x0f, 0xa2,                	/* cpuid   */
	0x50,                   	/* push   %eax */
	0x8b, 0x45, 0x10,             	/* mov    0x10(%ebp),%eax */
	0x89, 0x18,                	/* mov    %ebx,(%eax) */
	0x8b, 0x45, 0x14,             	/* mov    0x14(%ebp),%eax */
	0x89, 0x08,                	/* mov    %ecx,(%eax) */
	0x8b, 0x45, 0x18,             	/* mov    0x18(%ebp),%eax */
	0x89, 0x10,                	/* mov    %edx,(%eax) */
	0x58,                   	/* pop    %eax */
	0x8b, 0x55, 0x0c,             	/* mov    0xc(%ebp),%edx */
	0x89, 0x02,                	/* mov    %eax,(%edx) */
	0x5b,                   	/* pop    %ebx */
	0xc9,                   	/* leave   */
	0xc3,                   	/* ret     */
};

typedef void (*CpuidFunc) (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx);

static int 
cpuid (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx) {
	int have_cpuid = 0;
#ifndef _MSC_VER
	__asm__  __volatile__ (
		"pushfl\n"
		"popl %%eax\n"
		"movl %%eax, %%edx\n"
		"xorl $0x200000, %%eax\n"
		"pushl %%eax\n"
		"popfl\n"
		"pushfl\n"
		"popl %%eax\n"
		"xorl %%edx, %%eax\n"
		"andl $0x200000, %%eax\n"
		"movl %%eax, %0"
		: "=r" (have_cpuid)
		:
		: "%eax", "%edx"
	);
#else
	__asm {
		pushfd
		pop eax
		mov edx, eax
		xor eax, 0x200000
		push eax
		popfd
		pushfd
		pop eax
		xor eax, edx
		and eax, 0x200000
		mov have_cpuid, eax
	}
#endif
	if (have_cpuid) {
		CpuidFunc func = (CpuidFunc) cpuid_impl;
		func (id, p_eax, p_ebx, p_ecx, p_edx);
		/*
		 * We use this approach because of issues with gcc and pic code, see:
		 * http://gcc.gnu.org/cgi-bin/gnatsweb.pl?cmd=view%20audit-trail&database=gcc&pr=7329
		__asm__ __volatile__ ("cpuid"
			: "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
			: "a" (id));
		*/
		return 1;
	}
	return 0;
}

static void detect_fast_timer (void) {
	int p_eax, p_ebx, p_ecx, p_edx;
	
	if (cpuid (0x1, &p_eax, &p_ebx, &p_ecx, &p_edx)) {
		if (p_edx & 0x10) {
			use_fast_timer = TRUE;
		} else {
			use_fast_timer = FALSE;
		}
	} else {
		use_fast_timer = FALSE;
	}
}
#endif

#if defined(__x86_64__)
static void detect_fast_timer (void) {
	guint32 op = 0x1;
	guint32 eax,ebx,ecx,edx;
	__asm__ __volatile__ ("cpuid" : "=a"(eax), "=b"(ebx), "=c"(ecx), "=d"(edx) : "a"(op));
	if (edx & 0x10) {
		use_fast_timer = TRUE;
	} else {
		use_fast_timer = FALSE;
	}
}
#endif

static __inline__ guint64 rdtsc(void) {
	guint32 hi, lo;
	__asm__ __volatile__ ("rdtsc" : "=a"(lo), "=d"(hi));
	return ((guint64) lo) | (((guint64) hi) << 32);
}
#define MONO_PROFILER_GET_CURRENT_COUNTER(c) {\
	if (use_fast_timer) {\
		(c) = rdtsc ();\
	} else {\
		MONO_PROFILER_GET_CURRENT_TIME ((c));\
	}\
} while (0)
#else
static detect_fast_timer (void) {
	use_fast_timer = FALSE;
}
#define MONO_PROFILER_GET_CURRENT_COUNTER(c) MONO_PROFILER_GET_CURRENT_TIME ((c))
#endif


#define CLASS_LAYOUT_PACKED_BITMAP_SIZE 64
#define CLASS_LAYOUT_NOT_INITIALIZED (0xFFFF)
typedef enum {
	HEAP_CODE_NONE = 0,
	HEAP_CODE_OBJECT = 1,
	HEAP_CODE_FREE_OBJECT_CLASS = 2,
	HEAP_CODE_MASK = 3
} HeapProfilerJobValueCode;
typedef struct _MonoProfilerClassData {
	union {
		guint64 compact;
		guint8 *extended;
	} bitmap;
	struct {
		guint16 slots;
		guint16 references;
	} layout;
} MonoProfilerClassData;

typedef struct _MonoProfilerMethodData {
	gpointer code_start;
	guint32 code_size;
} MonoProfilerMethodData;

typedef struct _ClassIdMappingElement {
	char *name;
	guint32 id;
	MonoClass *klass;
	struct _ClassIdMappingElement *next_unwritten;
	MonoProfilerClassData data;
} ClassIdMappingElement;

typedef struct _MethodIdMappingElement {
	char *name;
	guint32 id;
	MonoMethod *method;
	struct _MethodIdMappingElement *next_unwritten;
	MonoProfilerMethodData data;
} MethodIdMappingElement;

typedef struct _ClassIdMapping {
	GHashTable *table;
	ClassIdMappingElement *unwritten;
	guint32 next_id;
} ClassIdMapping;

typedef struct _MethodIdMapping {
	GHashTable *table;
	MethodIdMappingElement *unwritten;
	guint32 next_id;
} MethodIdMapping;

typedef struct _LoadedElement {
	char *name;
	guint64 load_start_counter;
	guint64 load_end_counter;
	guint64 unload_start_counter;
	guint64 unload_end_counter;
	guint8 loaded;
	guint8 load_written;
	guint8 unloaded;
	guint8 unload_written;
} LoadedElement;

#define PROFILER_HEAP_SHOT_OBJECT_BUFFER_SIZE 1024
#define PROFILER_HEAP_SHOT_HEAP_BUFFER_SIZE 4096
#define PROFILER_HEAP_SHOT_WRITE_BUFFER_SIZE 4096

typedef struct _ProfilerHeapShotObjectBuffer {
	struct _ProfilerHeapShotObjectBuffer *next;
	MonoObject **next_free_slot;
	MonoObject **end;
	MonoObject **first_unprocessed_slot;
	MonoObject *buffer [PROFILER_HEAP_SHOT_OBJECT_BUFFER_SIZE];
} ProfilerHeapShotObjectBuffer;

typedef struct _ProfilerHeapShotHeapBuffer {
	struct _ProfilerHeapShotHeapBuffer *next;
	struct _ProfilerHeapShotHeapBuffer *previous;
	MonoObject **start_slot;
	MonoObject **end_slot;
	MonoObject *buffer [PROFILER_HEAP_SHOT_HEAP_BUFFER_SIZE];
} ProfilerHeapShotHeapBuffer;

typedef struct _ProfilerHeapShotHeapBuffers {
	ProfilerHeapShotHeapBuffer *buffers;
	ProfilerHeapShotHeapBuffer *last;
	ProfilerHeapShotHeapBuffer *current;
	MonoObject **first_free_slot;
} ProfilerHeapShotHeapBuffers;


typedef struct _ProfilerHeapShotWriteBuffer {
	struct _ProfilerHeapShotWriteBuffer *next;
	gpointer buffer [PROFILER_HEAP_SHOT_WRITE_BUFFER_SIZE];
} ProfilerHeapShotWriteBuffer;

typedef struct _ProfilerHeapShotWriteJob {
	struct _ProfilerHeapShotWriteJob *next;
	struct _ProfilerHeapShotWriteJob *next_unwritten;
	gpointer *start;
	gpointer *cursor;
	gpointer *end;
	ProfilerHeapShotWriteBuffer *buffers;
	ProfilerHeapShotWriteBuffer **last_next;
	guint32 full_buffers;
	gboolean heap_shot_was_signalled;
	guint64 start_counter;
	guint64 start_time;
	guint64 end_counter;
	guint64 end_time;
	guint32 collection;
} ProfilerHeapShotWriteJob;

typedef struct _ProfilerPerThreadData {
	ProfilerEventData *events;
	ProfilerEventData *next_free_event;
	ProfilerEventData *end_event;
	ProfilerEventData *first_unwritten_event;
	ProfilerEventData *first_unmapped_event;
	guint64 start_event_counter;
	guint64 last_event_counter;
	gsize thread_id;
	ProfilerHeapShotObjectBuffer *heap_shot_object_buffers;
	struct _ProfilerPerThreadData* next;
} ProfilerPerThreadData;

typedef struct _ProfilerStatisticalData {
	gpointer *addresses;
	int next_free_index;
	int end_index;
	int first_unwritten_index;
} ProfilerStatisticalData;

typedef struct _ProfilerUnmanagedSymbol {
	guint32 offset;
	guint32 size;
	guint32 id;
	guint32 index;
} ProfilerUnmanagedSymbol;

struct _ProfilerExecutableFile;

typedef struct _ProfilerExecutableMemoryRegionData {
	gpointer start;
	gpointer end;
	guint32 file_offset;
	char *file_name;
	guint32 id;
	gboolean is_new;
	
	struct _ProfilerExecutableFile *file;
	guint32 symbols_count;
	guint32 symbols_capacity;
	ProfilerUnmanagedSymbol *symbols;
} ProfilerExecutableMemoryRegionData;

typedef struct _ProfilerExecutableMemoryRegions {
	ProfilerExecutableMemoryRegionData **regions;
	guint32 regions_capacity;
	guint32 regions_count;
	guint32 next_id;
	guint32 next_unmanaged_function_id;
} ProfilerExecutableMemoryRegions;

/* Start of ELF definitions */
#define EI_NIDENT 16
typedef guint16 ElfHalf;
typedef guint32 ElfWord;
typedef gsize ElfAddr;
typedef gsize ElfOff;

typedef struct {
	unsigned char e_ident[EI_NIDENT];
	ElfHalf e_type;
	ElfHalf e_machine;
	ElfWord e_version;
	ElfAddr e_entry;
	ElfOff  e_phoff;
	ElfOff  e_shoff; // Section header table
	ElfWord e_flags;
	ElfHalf e_ehsize; // Header size
	ElfHalf e_phentsize;
	ElfHalf e_phnum;
	ElfHalf e_shentsize; // Section header entry size
	ElfHalf e_shnum; // Section header entries number
	ElfHalf e_shstrndx; // String table index
} ElfHeader;

#if (SIZEOF_VOID_P == 4)
typedef struct {
	ElfWord sh_name;
	ElfWord sh_type;
	ElfWord sh_flags;
	ElfAddr sh_addr; // Address in memory
	ElfOff  sh_offset; // Offset in file
	ElfWord sh_size;
	ElfWord sh_link;
	ElfWord sh_info;
	ElfWord sh_addralign;
	ElfWord sh_entsize;
} ElfSection;
typedef struct {
	ElfWord       st_name;
	ElfAddr       st_value;
	ElfWord       st_size;
	unsigned char st_info; // Use ELF32_ST_TYPE to get symbol type
	unsigned char st_other;
	ElfHalf       st_shndx; // Or one of SHN_ABS, SHN_COMMON or SHN_UNDEF.
} ElfSymbol;
#elif (SIZEOF_VOID_P == 8)
typedef struct {
	ElfWord sh_name;
	ElfWord sh_type;
	ElfOff sh_flags;
	ElfAddr sh_addr; // Address in memory
	ElfOff  sh_offset; // Offset in file
	ElfOff sh_size;
	ElfWord sh_link;
	ElfWord sh_info;
	ElfOff sh_addralign;
	ElfOff sh_entsize;
} ElfSection;
typedef struct {
	ElfWord       st_name;
	unsigned char st_info; // Use ELF_ST_TYPE to get symbol type
	unsigned char st_other;
	ElfHalf       st_shndx; // Or one of SHN_ABS, SHN_COMMON or SHN_UNDEF.
	ElfAddr       st_value;
	ElfAddr       st_size;
} ElfSymbol;
#else
#error Bad size of void pointer
#endif


#define ELF_ST_BIND(i)   ((i)>>4)
#define ELF_ST_TYPE(i)   ((i)&0xf)


typedef enum {
	EI_MAG0 = 0,
	EI_MAG1 = 1,
	EI_MAG2 = 2,
	EI_MAG3 = 3,
	EI_CLASS = 4,
	EI_DATA = 5
} ElfIdentFields;

typedef enum {
	ELF_FILE_TYPE_NONE = 0,
	ELF_FILE_TYPE_REL = 1,
	ELF_FILE_TYPE_EXEC = 2,
	ELF_FILE_TYPE_DYN = 3,
	ELF_FILE_TYPE_CORE = 4
} ElfFileType;

typedef enum {
	ELF_CLASS_NONE = 0,
	ELF_CLASS_32 = 1,
	ELF_CLASS_64 = 2
} ElfIdentClass;

typedef enum {
	ELF_DATA_NONE = 0,
	ELF_DATA_LSB = 1,
	ELF_DATA_MSB = 2
} ElfIdentData;

typedef enum {
	ELF_SHT_NULL = 0,
	ELF_SHT_PROGBITS = 1,
	ELF_SHT_SYMTAB = 2,
	ELF_SHT_STRTAB = 3,
	ELF_SHT_RELA = 4,
	ELF_SHT_HASH = 5,
	ELF_SHT_DYNAMIC = 6,
	ELF_SHT_NOTE = 7,
	ELF_SHT_NOBITS = 8,
	ELF_SHT_REL = 9,
	ELF_SHT_SHLIB = 10,
	ELF_SHT_DYNSYM = 11
} ElfSectionType;

typedef enum {
	ELF_STT_NOTYPE = 0,
	ELF_STT_OBJECT = 1,
	ELF_STT_FUNC = 2,
	ELF_STT_SECTION = 3,
	ELF_STT_FILE = 4
} ElfSymbolType;

typedef enum {
	ELF_SHF_WRITE = 1,
	ELF_SHF_ALLOC = 2,
	ELF_SHF_EXECINSTR = 4,
} ElfSectionFlags;

#define ELF_SHN_UNDEF       0
#define ELF_SHN_LORESERVE   0xff00
#define ELF_SHN_LOPROC      0xff00
#define ELF_SHN_HIPROC      0xff1f
#define ELF_SHN_ABS         0xfff1
#define ELF_SHN_COMMON      0xfff2
#define ELF_SHN_HIRESERVE   0xffff
/* End of ELF definitions */

typedef struct _ProfilerExecutableFileSectionRegion {
	ProfilerExecutableMemoryRegionData *region;
	guint8 *section_address;
	gsize section_offset;
} ProfilerExecutableFileSectionRegion;

typedef struct _ProfilerExecutableFile {
	guint32 reference_count;
	
	/* Used for mmap and munmap */
	int fd;
	guint8 *data;
	size_t length;
	
	/* File data */
	ElfHeader *header;
	guint8 *symbols_start;
	guint32 symbols_count;
	guint32 symbol_size;
	const char *symbols_string_table;
	const char *main_string_table;
	
	ProfilerExecutableFileSectionRegion *section_regions;
	
	struct _ProfilerExecutableFile *next_new_file;
} ProfilerExecutableFile;

typedef struct _ProfilerExecutableFiles {
	GHashTable *table;
	ProfilerExecutableFile *new_files;
} ProfilerExecutableFiles;


#ifndef PLATFORM_WIN32
#include <sys/types.h>
#include <sys/time.h>
#include <sys/stat.h>
#include <unistd.h>
#include <fcntl.h>
#include <pthread.h>
#include <semaphore.h>

#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <errno.h>

#define MUTEX_TYPE pthread_mutex_t
#define INITIALIZE_PROFILER_MUTEX() pthread_mutex_init (&(profiler->mutex), NULL)
#define DELETE_PROFILER_MUTEX() pthread_mutex_destroy (&(profiler->mutex))
#define LOCK_PROFILER() do {/*LOG_WRITER_THREAD ("LOCK_PROFILER");*/ pthread_mutex_lock (&(profiler->mutex));} while (0)
#define UNLOCK_PROFILER() do {/*LOG_WRITER_THREAD ("UNLOCK_PROFILER");*/ pthread_mutex_unlock (&(profiler->mutex));} while (0)

#define THREAD_TYPE pthread_t
#define CREATE_WRITER_THREAD(f) pthread_create (&(profiler->data_writer_thread), NULL, ((void*(*)(void*))f), NULL)
#define EXIT_THREAD() pthread_exit (NULL);
#define WAIT_WRITER_THREAD() pthread_join (profiler->data_writer_thread, NULL)
#define CURRENT_THREAD_ID() (gsize) pthread_self ()

#ifndef HAVE_KW_THREAD
static pthread_key_t pthread_profiler_key;
static pthread_once_t profiler_pthread_once = PTHREAD_ONCE_INIT;
static void
make_pthread_profiler_key (void) {
    (void) pthread_key_create (&pthread_profiler_key, NULL);
}
#define LOOKUP_PROFILER_THREAD_DATA() ((ProfilerPerThreadData*) pthread_getspecific (pthread_profiler_key))
#define SET_PROFILER_THREAD_DATA(x) (void) pthread_setspecific (pthread_profiler_key, (x))
#define ALLOCATE_PROFILER_THREAD_DATA() (void) pthread_once (&profiler_pthread_once, make_pthread_profiler_key)
#define FREE_PROFILER_THREAD_DATA() (void) pthread_key_delete (pthread_profiler_key)
#endif

#define EVENT_TYPE sem_t
#define WRITER_EVENT_INIT() (void) sem_init (&(profiler->statistical_data_writer_event), 0, 0)
#define WRITER_EVENT_DESTROY() (void) sem_destroy (&(profiler->statistical_data_writer_event))
#define WRITER_EVENT_WAIT() (void) sem_wait (&(profiler->statistical_data_writer_event))
#define WRITER_EVENT_RAISE() (void) sem_post (&(profiler->statistical_data_writer_event))

#if 0
#define FILE_HANDLE_TYPE FILE*
#define OPEN_FILE() profiler->file = fopen (profiler->file_name, "wb");
#define WRITE_BUFFER(b,s) fwrite ((b), 1, (s), profiler->file)
#define FLUSH_FILE() fflush (profiler->file)
#define CLOSE_FILE() fclose (profiler->file);
#else
#define FILE_HANDLE_TYPE int
#define OPEN_FILE() profiler->file = open (profiler->file_name, O_WRONLY|O_CREAT|O_TRUNC, 0664);
#define WRITE_BUFFER(b,s) write (profiler->file, (b), (s))
#define FLUSH_FILE()
#define CLOSE_FILE() close (profiler->file);
#endif

#else

#include <windows.h>

#define MUTEX_TYPE CRITICAL_SECTION
#define INITIALIZE_PROFILER_MUTEX() InitializeCriticalSection (&(profiler->mutex))
#define DELETE_PROFILER_MUTEX() DeleteCriticalSection (&(profiler->mutex))
#define LOCK_PROFILER() EnterCriticalSection (&(profiler->mutex))
#define UNLOCK_PROFILER() LeaveCriticalSection (&(profiler->mutex))

#define THREAD_TYPE HANDLE
#define CREATE_WRITER_THREAD(f) CreateThread (NULL, (1*1024*1024), (f), NULL, 0, NULL);
#define EXIT_THREAD() ExitThread (0);
#define WAIT_WRITER_THREAD() WaitForSingleObject (profiler->data_writer_thread, INFINITE)
#define CURRENT_THREAD_ID() (gsize) GetCurrentThreadId ()

#ifndef HAVE_KW_THREAD
static guint32 profiler_thread_id = -1;
#define LOOKUP_PROFILER_THREAD_DATA() ((ProfilerPerThreadData*)TlsGetValue (profiler_thread_id))
#define SET_PROFILER_THREAD_DATA(x) TlsSetValue (profiler_thread_id, (x));
#define ALLOCATE_PROFILER_THREAD_DATA() profiler_thread_id = TlsAlloc ()
#define FREE_PROFILER_THREAD_DATA() TlsFree (profiler_thread_id)
#endif

#define EVENT_TYPE HANDLE
#define WRITER_EVENT_INIT() profiler->statistical_data_writer_event = CreateEvent (NULL, FALSE, FALSE, NULL)
#define WRITER_EVENT_DESTROY() CloseHandle (profiler->statistical_data_writer_event)
#define WRITER_EVENT_WAIT() WaitForSingleObject (profiler->statistical_data_writer_event, INFINITE)
#define WRITER_EVENT_RAISE() SetEvent (profiler->statistical_data_writer_event)

#define FILE_HANDLE_TYPE FILE*
#define OPEN_FILE() profiler->file = fopen (profiler->file_name, "wb");
#define WRITE_BUFFER(b,s) fwrite ((b), 1, (s), profiler->file)
#define FLUSH_FILE() fflush (profiler->file)
#define CLOSE_FILE() fclose (profiler->file);

#endif

#ifdef HAVE_KW_THREAD
static __thread ProfilerPerThreadData * tls_profiler_per_thread_data;
#define LOOKUP_PROFILER_THREAD_DATA() ((ProfilerPerThreadData*) tls_profiler_per_thread_data)
#define SET_PROFILER_THREAD_DATA(x) tls_profiler_per_thread_data = (x)
#define ALLOCATE_PROFILER_THREAD_DATA() /* nop */
#define FREE_PROFILER_THREAD_DATA() /* nop */
#endif

#define GET_PROFILER_THREAD_DATA(data) do {\
	ProfilerPerThreadData *_result = LOOKUP_PROFILER_THREAD_DATA ();\
	if (!_result) {\
		_result = profiler_per_thread_data_new (profiler->per_thread_buffer_size);\
		LOCK_PROFILER ();\
		_result->next = profiler->per_thread_data;\
		profiler->per_thread_data = _result;\
		UNLOCK_PROFILER ();\
		SET_PROFILER_THREAD_DATA (_result);\
	}\
	(data) = _result;\
} while (0)

#define PROFILER_FILE_WRITE_BUFFER_SIZE (profiler->write_buffer_size)
typedef struct _ProfilerFileWriteBuffer {
	struct _ProfilerFileWriteBuffer *next;
	guint8 buffer [];
} ProfilerFileWriteBuffer;

struct _MonoProfiler {
	MUTEX_TYPE mutex;
	
	MonoProfileFlags flags;
	char *file_name;
	char *file_name_suffix;
	FILE_HANDLE_TYPE file;
	
	guint64 start_time;
	guint64 start_counter;
	guint64 end_time;
	guint64 end_counter;
	
	guint64 last_header_counter;
	
	MethodIdMapping *methods;
	ClassIdMapping *classes;
	
	GHashTable *loaded_assemblies;
	GHashTable *loaded_modules;
	GHashTable *loaded_appdomains;
	
	guint32 per_thread_buffer_size;
	guint32 statistical_buffer_size;
	ProfilerPerThreadData* per_thread_data;
	ProfilerStatisticalData *statistical_data;
	ProfilerStatisticalData *statistical_data_ready;
	ProfilerStatisticalData *statistical_data_second_buffer;
	int statistical_call_chain_depth;
	
	THREAD_TYPE data_writer_thread;
	EVENT_TYPE statistical_data_writer_event;
	gboolean terminate_writer_thread;
	gboolean detach_writer_thread;
	
	ProfilerFileWriteBuffer *write_buffers;
	ProfilerFileWriteBuffer *current_write_buffer;
	int write_buffer_size;
	int current_write_position;
	int full_write_buffers;
	
	ProfilerHeapShotWriteJob *heap_shot_write_jobs;
	ProfilerHeapShotHeapBuffers heap;
	
	char *heap_shot_command_file_name;
	int dump_next_heap_snapshots;
	guint64 heap_shot_command_file_access_time;
	gboolean heap_shot_was_signalled;
	guint32 garbage_collection_counter;
	
	ProfilerExecutableMemoryRegions *executable_regions;
	ProfilerExecutableFiles executable_files;
	
	struct {
#if (HAS_OPROFILE)
		gboolean oprofile;
#endif
		gboolean jit_time;
		gboolean unreachable_objects;
		gboolean heap_shot;
	} action_flags;
};
static MonoProfiler *profiler;

#ifndef PLATFORM_WIN32
#include <signal.h>

#ifdef MONO_ARCH_USE_SIGACTION
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, siginfo_t *info, void *context)
#elif defined(__sparc__)
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, void *sigctx)
#else
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy)
#endif

static void
SIG_HANDLER_SIGNATURE (gc_request_handler) {
	profiler->heap_shot_was_signalled = TRUE;
	WRITER_EVENT_RAISE ();
}

static void
add_gc_request_handler (int signal_number)
{
	struct sigaction sa;
	
#ifdef MONO_ARCH_USE_SIGACTION
	sa.sa_sigaction = gc_request_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_SIGINFO;
#else
	sa.sa_handler = gc_request_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
#endif
	
	g_assert (sigaction (signal_number, &sa, NULL) != -1);
}
#endif



#define DEBUG_LOAD_EVENTS 0
#define DEBUG_MAPPING_EVENTS 0
#define DEBUG_LOGGING_PROFILER 0
#define DEBUG_HEAP_PROFILER 0
#define DEBUG_CLASS_BITMAPS 0
#define DEBUG_STATISTICAL_PROFILER 0
#define DEBUG_WRITER_THREAD 0
#if (DEBUG_LOGGING_PROFILER || DEBUG_STATISTICAL_PROFILER || DEBUG_HEAP_PROFILER || DEBUG_WRITER_THREAD)
#define LOG_WRITER_THREAD(m) printf ("WRITER-THREAD-LOG %s\n", m)
#else
#define LOG_WRITER_THREAD(m)
#endif

#if DEBUG_LOGGING_PROFILER
static int event_counter = 0;
#define EVENT_MARK() printf ("[EVENT:%d]", ++ event_counter)
#endif


static ClassIdMappingElement*
class_id_mapping_element_get (MonoClass *klass) {
	return g_hash_table_lookup (profiler->classes->table, (gconstpointer) klass);
}

static MethodIdMappingElement*
method_id_mapping_element_get (MonoMethod *method) {
	return g_hash_table_lookup (profiler->methods->table, (gconstpointer) method);
}

#define BITS_TO_BYTES(v) do {\
	(v) += 7;\
	(v) &= ~7;\
	(v) >>= 3;\
} while (0)

static ClassIdMappingElement*
class_id_mapping_element_new (MonoClass *klass) {
	ClassIdMappingElement *result = g_new (ClassIdMappingElement, 1);
	
	result->name = g_strdup_printf ("%s.%s", mono_class_get_namespace (klass), mono_class_get_name (klass));
	result->klass = klass;
	result->next_unwritten = profiler->classes->unwritten;
	profiler->classes->unwritten = result;
	result->id = profiler->classes->next_id;
	profiler->classes->next_id ++;
	
	result->data.bitmap.compact = 0;
	result->data.layout.slots = CLASS_LAYOUT_NOT_INITIALIZED;
	result->data.layout.references = CLASS_LAYOUT_NOT_INITIALIZED;
	
	g_hash_table_insert (profiler->classes->table, klass, result);
	
#if (DEBUG_MAPPING_EVENTS)
	printf ("Created new CLASS mapping element \"%s\" (%p)[%d]\n", result->name, klass, result->id);
#endif
	return result;
}

static void
class_id_mapping_element_build_layout_bitmap (MonoClass *klass, ClassIdMappingElement *klass_id) {
	MonoClass *parent_class = mono_class_get_parent (klass);
	int number_of_reference_fields = 0;
	int max_offset_of_reference_fields = 0;
	ClassIdMappingElement *parent_id;
	gpointer iter;
	MonoClassField *field;
	
#if (DEBUG_CLASS_BITMAPS)
	printf ("class_id_mapping_element_build_layout_bitmap: building layout for class %s.%s: ", mono_class_get_namespace (klass), mono_class_get_name (klass));
#endif
	
	if (parent_class != NULL) {
		parent_id = class_id_mapping_element_get (parent_class);
		g_assert (parent_id != NULL);
		
		if (parent_id->data.layout.slots == CLASS_LAYOUT_NOT_INITIALIZED) {
#if (DEBUG_CLASS_BITMAPS)
			printf ("[recursively building bitmap for father class]\n");
#endif
			class_id_mapping_element_build_layout_bitmap (parent_class, parent_id);
		}
	} else {
		parent_id = NULL;
	}
	
	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter)) != NULL) {
		MonoType* field_type = mono_field_get_type (field);
		// For now, skip static fields
		if (mono_field_get_flags (field) & 0x0010 /*FIELD_ATTRIBUTE_STATIC*/)
			continue;
		
		if (MONO_TYPE_IS_REFERENCE (field_type)) {
			int field_offset = mono_field_get_offset (field) - sizeof (MonoObject);
			if (field_offset > max_offset_of_reference_fields) {
				max_offset_of_reference_fields = field_offset;
			}
			number_of_reference_fields ++;
		} else {
			MonoClass *field_class = mono_class_from_mono_type (field_type);
			if (field_class && mono_class_is_valuetype (field_class)) {
				ClassIdMappingElement *field_id = class_id_mapping_element_get (field_class);
				g_assert (field_id != NULL);
				
				if (field_id->data.layout.slots == CLASS_LAYOUT_NOT_INITIALIZED) {
					if (field_id != klass_id) {
#if (DEBUG_CLASS_BITMAPS)
						printf ("[recursively building bitmap for field %s]\n", mono_field_get_name (field));
#endif
						class_id_mapping_element_build_layout_bitmap (field_class, field_id);
					} else {
#if (DEBUG_CLASS_BITMAPS)
						printf ("[breaking recursive bitmap build for field %s]", mono_field_get_name (field));
						
#endif
						klass_id->data.bitmap.compact = 0;
						klass_id->data.layout.slots = 0;
						klass_id->data.layout.references = 0;
					}
				}
				
				if (field_id->data.layout.references > 0) {
					int field_offset = mono_field_get_offset (field) - sizeof (MonoObject);
					int max_offset_reference_in_field = (field_id->data.layout.slots - 1) * sizeof (gpointer);
					
					if ((field_offset + max_offset_reference_in_field) > max_offset_of_reference_fields) {
						max_offset_of_reference_fields = field_offset + max_offset_reference_in_field;
					}
					
					number_of_reference_fields += field_id->data.layout.references;
				}
			}
		}
	}
	
#if (DEBUG_CLASS_BITMAPS)
	printf ("[allocating bitmap for class %s.%s (references %d, max offset %d, slots %d)]", mono_class_get_namespace (klass), mono_class_get_name (klass), number_of_reference_fields, max_offset_of_reference_fields, (int)(max_offset_of_reference_fields / sizeof (gpointer)) + 1);
#endif
	if ((number_of_reference_fields == 0) && ((parent_id == NULL) || (parent_id->data.layout.references == 0))) {
#if (DEBUG_CLASS_BITMAPS)
		printf ("[no references at all]");
#endif
		klass_id->data.bitmap.compact = 0;
		klass_id->data.layout.slots = 0;
		klass_id->data.layout.references = 0;
	} else {
		if ((parent_id != NULL) && (parent_id->data.layout.references > 0)) {
#if (DEBUG_CLASS_BITMAPS)
			printf ("[parent %s.%s has %d references in %d slots]", mono_class_get_namespace (parent_class), mono_class_get_name (parent_class), parent_id->data.layout.references, parent_id->data.layout.slots);
#endif
			klass_id->data.layout.slots = parent_id->data.layout.slots;
			klass_id->data.layout.references = parent_id->data.layout.references;
		} else {
#if (DEBUG_CLASS_BITMAPS)
			printf ("[no references from parent]");
#endif
			klass_id->data.layout.slots = 0;
			klass_id->data.layout.references = 0;
		}
		
		if (number_of_reference_fields > 0) {
			klass_id->data.layout.slots += ((max_offset_of_reference_fields / sizeof (gpointer)) + 1);
			klass_id->data.layout.references += number_of_reference_fields;
#if (DEBUG_CLASS_BITMAPS)
			printf ("[adding data, going to %d references in %d slots]", klass_id->data.layout.references, klass_id->data.layout.slots);
#endif
		}
		
		if (klass_id->data.layout.slots <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
#if (DEBUG_CLASS_BITMAPS)
				printf ("[zeroing bitmap]");
#endif
				klass_id->data.bitmap.compact = 0;
			if ((parent_id != NULL) && (parent_id->data.layout.references > 0)) {
#if (DEBUG_CLASS_BITMAPS)
				printf ("[copying compact father bitmap]");
#endif
				klass_id->data.bitmap.compact = parent_id->data.bitmap.compact;
			}
		} else {
			int size_of_bitmap = klass_id->data.layout.slots;
			BITS_TO_BYTES (size_of_bitmap);
#if (DEBUG_CLASS_BITMAPS)
			printf ("[allocating %d bytes for bitmap]", size_of_bitmap);
#endif
			klass_id->data.bitmap.extended = g_malloc0 (size_of_bitmap);
			if ((parent_id != NULL) && (parent_id->data.layout.references > 0)) {
				int size_of_father_bitmap = parent_id->data.layout.slots;
				if (size_of_father_bitmap <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
					int father_slot;
#if (DEBUG_CLASS_BITMAPS)
					printf ("[copying %d bits from father bitmap]", size_of_father_bitmap);
#endif
					for (father_slot = 0; father_slot < size_of_father_bitmap; father_slot ++) {
						if (parent_id->data.bitmap.compact & (((guint64)1) << father_slot)) {
							klass_id->data.bitmap.extended [father_slot >> 3] |= (1 << (father_slot & 7));
						}
					}
				} else {
					BITS_TO_BYTES (size_of_father_bitmap);
#if (DEBUG_CLASS_BITMAPS)
					printf ("[copying %d bytes from father bitmap]", size_of_father_bitmap);
#endif
					memcpy (klass_id->data.bitmap.extended, parent_id->data.bitmap.extended, size_of_father_bitmap);
				}
			}
		}
	}
	
#if (DEBUG_CLASS_BITMAPS)
	printf ("[starting filling iteration]\n");
#endif
	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter)) != NULL) {
		MonoType* field_type = mono_field_get_type (field);
		// For now, skip static fields
		if (mono_field_get_flags (field) & 0x0010 /*FIELD_ATTRIBUTE_STATIC*/)
			continue;
		
#if (DEBUG_CLASS_BITMAPS)
		printf ("[Working on field %s]", mono_field_get_name (field));
#endif
		if (MONO_TYPE_IS_REFERENCE (field_type)) {
			int field_offset = mono_field_get_offset (field) - sizeof (MonoObject);
			int field_slot;
			g_assert ((field_offset % sizeof (gpointer)) == 0);
			field_slot = field_offset / sizeof (gpointer);
			if (klass_id->data.layout.slots <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
				klass_id->data.bitmap.compact |= (((guint64)1) << field_slot);
			} else {
				klass_id->data.bitmap.extended [field_slot >> 3] |= (1 << (field_slot & 7));
			}
#if (DEBUG_CLASS_BITMAPS)
			printf ("[reference at offset %d, slot %d]", field_offset, field_slot);
#endif
		} else {
			MonoClass *field_class = mono_class_from_mono_type (field_type);
			if (field_class && mono_class_is_valuetype (field_class)) {
				ClassIdMappingElement *field_id = class_id_mapping_element_get (field_class);
				int field_offset;
				int field_slot;
				
				g_assert (field_id != NULL);
				field_offset = mono_field_get_offset (field) - sizeof (MonoObject);
				g_assert ((field_id->data.layout.references == 0) || ((field_offset % sizeof (gpointer)) == 0));
				field_slot = field_offset / sizeof (gpointer);
#if (DEBUG_CLASS_BITMAPS)
				printf ("[value type at offset %d, slot %d, with %d references in %d slots]", field_offset, field_slot, field_id->data.layout.references, field_id->data.layout.slots);
#endif
				
				if (field_id->data.layout.references > 0) {
					int sub_field_slot;
					if (field_id->data.layout.slots <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
						for (sub_field_slot = 0; sub_field_slot < field_id->data.layout.slots; sub_field_slot ++) {
							if (field_id->data.bitmap.compact & (((guint64)1) << sub_field_slot)) {
								int actual_slot = field_slot + sub_field_slot;
								if (klass_id->data.layout.slots <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
									klass_id->data.bitmap.compact |= (((guint64)1) << actual_slot);
								} else {
									klass_id->data.bitmap.extended [actual_slot >> 3] |= (1 << (actual_slot & 7));
								}
							}
						}
					} else {
						for (sub_field_slot = 0; sub_field_slot < field_id->data.layout.slots; sub_field_slot ++) {
							if (field_id->data.bitmap.extended [sub_field_slot >> 3] & (1 << (sub_field_slot & 7))) {
								int actual_slot = field_slot + sub_field_slot;
								if (klass_id->data.layout.slots <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
									klass_id->data.bitmap.compact |= (((guint64)1) << actual_slot);
								} else {
									klass_id->data.bitmap.extended [actual_slot >> 3] |= (1 << (actual_slot & 7));
								}
							}
						}
					}
				}
			}
		}
	}
#if (DEBUG_CLASS_BITMAPS)
	do {
		int slot;
		printf ("\nLayot of class \"%s.%s\": references %d, slots %d, bitmap {", mono_class_get_namespace (klass), mono_class_get_name (klass), klass_id->data.layout.references, klass_id->data.layout.slots);
		for (slot = 0; slot < klass_id->data.layout.slots; slot ++) {
			if (klass_id->data.layout.slots <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
				if (klass_id->data.bitmap.compact & (((guint64)1) << slot)) {
					printf (" 1");
				} else {
					printf (" 0");
				}
			} else {
				if (klass_id->data.bitmap.extended [slot >> 3] & (1 << (slot & 7))) {
					printf (" 1");
				} else {
					printf (" 0");
				}
;			}
			
		}
		printf (" }\n");
		
	} while (0);
#endif
}

static MethodIdMappingElement*
method_id_mapping_element_new (MonoMethod *method) {
	MethodIdMappingElement *result = g_new (MethodIdMappingElement, 1);
	char *signature = mono_signature_get_desc (mono_method_signature (method), TRUE);
	
	result->name = g_strdup_printf ("%s (%s)", mono_method_get_name (method), signature);
	g_free (signature);
	result->method = method;
	result->next_unwritten = profiler->methods->unwritten;
	profiler->methods->unwritten = result;
	result->id = profiler->methods->next_id;
	profiler->methods->next_id ++;
	g_hash_table_insert (profiler->methods->table, method, result);
	
	result->data.code_start = NULL;
	result->data.code_size = 0;
	
#if (DEBUG_MAPPING_EVENTS)
	printf ("Created new METHOD mapping element \"%s\" (%p)[%d]\n", result->name, method, result->id);
#endif
	return result;
}


static void
method_id_mapping_element_destroy (gpointer element) {
	MethodIdMappingElement *e = (MethodIdMappingElement*) element;
	if (e->name)
		g_free (e->name);
	g_free (element);
}

static void
class_id_mapping_element_destroy (gpointer element) {
	ClassIdMappingElement *e = (ClassIdMappingElement*) element;
	if (e->name)
		g_free (e->name);
	if ((e->data.layout.slots != CLASS_LAYOUT_NOT_INITIALIZED) && (e->data.layout.slots > CLASS_LAYOUT_PACKED_BITMAP_SIZE))
		g_free (e->data.bitmap.extended);
	g_free (element);
}

static MethodIdMapping*
method_id_mapping_new (void) {
	MethodIdMapping *result = g_new (MethodIdMapping, 1);
	//result->table = g_hash_table_new_full (mono_aligned_addr_hash, NULL, NULL, method_id_mapping_element_destroy);
	result->table = g_hash_table_new_full (g_direct_hash, NULL, NULL, method_id_mapping_element_destroy);
	result->unwritten = NULL;
	result->next_id = 1;
	return result;
}

static ClassIdMapping*
class_id_mapping_new (void) {
	ClassIdMapping *result = g_new (ClassIdMapping, 1);
	//result->table = g_hash_table_new_full (mono_aligned_addr_hash, NULL, NULL, class_id_mapping_element_destroy);
	result->table = g_hash_table_new_full (g_direct_hash, NULL, NULL, class_id_mapping_element_destroy);
	result->unwritten = NULL;
	result->next_id = 1;
	return result;
}

static void
method_id_mapping_destroy (MethodIdMapping *map) {
	g_hash_table_destroy (map->table);
	g_free (map);
}

static void
class_id_mapping_destroy (ClassIdMapping *map) {
	g_hash_table_destroy (map->table);
	g_free (map);
}

#if (DEBUG_LOAD_EVENTS)
static void
print_load_event (const char *event_name, GHashTable *table, gpointer item, LoadedElement *element);
#endif

static LoadedElement*
loaded_element_load_start (GHashTable *table, gpointer item) {
	LoadedElement *element = g_new0 (LoadedElement, 1);
#if (DEBUG_LOAD_EVENTS)
	print_load_event ("LOAD START", table, item, element);
#endif
	MONO_PROFILER_GET_CURRENT_COUNTER (element->load_start_counter);
	g_hash_table_insert (table, item, element);
	return element;
}

static LoadedElement*
loaded_element_load_end (GHashTable *table, gpointer item, char *name) {
	LoadedElement *element = g_hash_table_lookup (table, item);
#if (DEBUG_LOAD_EVENTS)
	print_load_event ("LOAD END", table, item, element);
#endif
	g_assert (element != NULL);
	MONO_PROFILER_GET_CURRENT_COUNTER (element->load_end_counter);
	element->name = name;
	element->loaded = TRUE;
	return element;
}

static LoadedElement*
loaded_element_unload_start (GHashTable *table, gpointer item) {
	LoadedElement *element = g_hash_table_lookup (table, item);
#if (DEBUG_LOAD_EVENTS)
	print_load_event ("UNLOAD START", table, item, element);
#endif
	g_assert (element != NULL);
	MONO_PROFILER_GET_CURRENT_COUNTER (element->unload_start_counter);
	return element;
}

static LoadedElement*
loaded_element_unload_end (GHashTable *table, gpointer item) {
	LoadedElement *element = g_hash_table_lookup (table, item);
#if (DEBUG_LOAD_EVENTS)
	print_load_event ("UNLOAD END", table, item, element);
#endif
	g_assert (element != NULL);
	MONO_PROFILER_GET_CURRENT_COUNTER (element->unload_end_counter);
	element->unloaded = TRUE;
	return element;
}


static void
loaded_element_destroy (gpointer element) {
	if (((LoadedElement*)element)->name)
		g_free (((LoadedElement*)element)->name);
	g_free (element);
}

#if (DEBUG_LOAD_EVENTS)
static void
print_load_event (const char *event_name, GHashTable *table, gpointer item, LoadedElement *element) {
	const char* item_name;
	char* item_info;
	
	if (table == profiler->loaded_assemblies) {
		//item_info = g_strdup_printf("ASSEMBLY %p (dynamic %d)", item, mono_image_is_dynamic (mono_assembly_get_image((MonoAssembly*)item)));
		item_info = g_strdup_printf("ASSEMBLY %p", item);
	} else if (table == profiler->loaded_modules) {
		//item_info = g_strdup_printf("MODULE %p (dynamic %d)", item, mono_image_is_dynamic ((MonoImage*)item));
		item_info = g_strdup_printf("MODULE %p", item);
	} else if (table == profiler->loaded_appdomains) {
		item_info = g_strdup_printf("APPDOMAIN %p (id %d)", item, mono_domain_get_id ((MonoDomain*)item));
	} else {
		item_info = NULL;
		g_assert_not_reached ();
	}
	
	if (element != NULL) {
		item_name = element->name;
	} else {
		item_name = "<NULL>";
	}
	
	printf ("%s EVENT for %s (%s)\n", event_name, item_info, item_name);
	g_free (item_info);
}
#endif

static void
profiler_heap_shot_object_buffers_destroy (ProfilerHeapShotObjectBuffer *buffer) {
	while (buffer != NULL) {
		ProfilerHeapShotObjectBuffer *next = buffer->next;
#if DEBUG_HEAP_PROFILER
		printf ("profiler_heap_shot_object_buffers_destroy: destroyed buffer %p (%p-%p)\n", buffer, & (buffer->buffer [0]), buffer->end);
#endif
		g_free (buffer);
		buffer = next;
	}
}

static ProfilerHeapShotObjectBuffer*
profiler_heap_shot_object_buffer_new (ProfilerPerThreadData *data) {
	ProfilerHeapShotObjectBuffer *buffer;
	ProfilerHeapShotObjectBuffer *result = g_new (ProfilerHeapShotObjectBuffer, 1);
	result->next_free_slot = & (result->buffer [0]);
	result->end = & (result->buffer [PROFILER_HEAP_SHOT_OBJECT_BUFFER_SIZE]);
	result->first_unprocessed_slot = & (result->buffer [0]);
	result->next = data->heap_shot_object_buffers;
	data->heap_shot_object_buffers = result;
#if DEBUG_HEAP_PROFILER
	printf ("profiler_heap_shot_object_buffer_new: created buffer %p (%p-%p)\n", result, result->next_free_slot, result->end);
#endif
	for (buffer = result; buffer != NULL; buffer = buffer->next) {
		ProfilerHeapShotObjectBuffer *last = buffer->next;
		if ((last != NULL) && (last->first_unprocessed_slot == last->end)) {
			buffer->next = NULL;
			profiler_heap_shot_object_buffers_destroy (last);
		}
	}
	
	return result;
}

static ProfilerHeapShotWriteJob*
profiler_heap_shot_write_job_new (gboolean heap_shot_was_signalled, guint32 collection) {
	ProfilerHeapShotWriteJob *job = g_new (ProfilerHeapShotWriteJob, 1);
	job->next = NULL;
	job->next_unwritten = NULL;
	job->buffers = g_new (ProfilerHeapShotWriteBuffer, 1);
	job->buffers->next = NULL;
	job->last_next = & (job->buffers->next);
	job->start = & (job->buffers->buffer [0]);
	job->cursor = job->start;
	job->end = & (job->buffers->buffer [PROFILER_HEAP_SHOT_WRITE_BUFFER_SIZE]);
	job->full_buffers = 0;
	job->heap_shot_was_signalled = heap_shot_was_signalled;
	job->collection = collection;
#if DEBUG_HEAP_PROFILER
	printf ("profiler_heap_shot_write_job_new: created job %p with buffer %p(%p-%p)\n", job, job->buffers, job->start, job->end);
#endif
	return job;
}

static void
profiler_heap_shot_write_job_add_buffer (ProfilerHeapShotWriteJob *job, gpointer value) {
	ProfilerHeapShotWriteBuffer *buffer = g_new (ProfilerHeapShotWriteBuffer, 1);
	buffer->next = NULL;
	*(job->last_next) = buffer;
	job->last_next = & (buffer->next);
	job->full_buffers ++;
	buffer->buffer [0] = value;
	job->start = & (buffer->buffer [0]);
	job->cursor = & (buffer->buffer [1]);
	job->end = & (buffer->buffer [PROFILER_HEAP_SHOT_WRITE_BUFFER_SIZE]);
#if DEBUG_HEAP_PROFILER
	printf ("profiler_heap_shot_write_job_add_buffer: in job %p, added buffer %p(%p-%p) with value %p at address %p (cursor now %p)\n", job, buffer, job->start, job->end, value, &(buffer->buffer [0]), job->cursor);
	do {
		ProfilerHeapShotWriteBuffer *current_buffer;
		for (current_buffer = job->buffers; current_buffer != NULL; current_buffer = current_buffer->next) {
			printf ("profiler_heap_shot_write_job_add_buffer: now job %p has buffer %p\n", job, current_buffer);
		}
	} while (0);
#endif
}

static void
profiler_heap_shot_write_job_free_buffers (ProfilerHeapShotWriteJob *job) {
	ProfilerHeapShotWriteBuffer *buffer = job->buffers;
	
	while (buffer != NULL) {
		ProfilerHeapShotWriteBuffer *next = buffer->next;
#if DEBUG_HEAP_PROFILER
		printf ("profiler_heap_shot_write_job_free_buffers: in job %p, freeing buffer %p\n", job, buffer);
#endif
		g_free (buffer);
		buffer = next;
	}
	
	job->buffers = NULL;
}

static void
profiler_heap_shot_write_block (ProfilerHeapShotWriteJob *job);

static void
profiler_process_heap_shot_write_jobs (void) {
	gboolean done = FALSE;
	
	while (!done) {
		ProfilerHeapShotWriteJob *current_job = profiler->heap_shot_write_jobs;
		ProfilerHeapShotWriteJob *previous_job = NULL;
		ProfilerHeapShotWriteJob *next_job;
		
		done = TRUE;
		while (current_job != NULL) {
			next_job = current_job->next_unwritten;
			
			if (next_job != NULL) {
				if (current_job->buffers != NULL) {
					done = FALSE;
				}
				if (next_job->buffers == NULL) {
					current_job->next_unwritten = NULL;
					next_job = NULL;
				}
			} else {
				if (current_job->buffers != NULL) {
					LOG_WRITER_THREAD ("profiler_process_heap_shot_write_jobs: writing...");
					profiler_heap_shot_write_block (current_job);
					LOG_WRITER_THREAD ("profiler_process_heap_shot_write_jobs: done");
					if (previous_job != NULL) {
						previous_job->next_unwritten = NULL;
					}
				}
			}
			
			previous_job = current_job;
			current_job = next_job;
		}
	}
}

static void
profiler_free_heap_shot_write_jobs (void) {
	ProfilerHeapShotWriteJob *current_job = profiler->heap_shot_write_jobs;
	ProfilerHeapShotWriteJob *next_job;
	
	if (current_job != NULL) {
		while (current_job->next_unwritten != NULL) {
#if DEBUG_HEAP_PROFILER
			printf ("profiler_free_heap_shot_write_jobs: job %p must not be freed\n", current_job);
#endif
			current_job = current_job->next_unwritten;
		}
		
		next_job = current_job->next;
		current_job->next = NULL;
		current_job = next_job;
		
		while (current_job != NULL) {
#if DEBUG_HEAP_PROFILER
			printf ("profiler_free_heap_shot_write_jobs: job %p will be freed\n", current_job);
#endif
			next_job = current_job->next;
			g_free (current_job);
			current_job = next_job;
		}
	}
}

static void
profiler_destroy_heap_shot_write_jobs (void) {
	ProfilerHeapShotWriteJob *current_job = profiler->heap_shot_write_jobs;
	ProfilerHeapShotWriteJob *next_job;
	
	while (current_job != NULL) {
		next_job = current_job->next;
		profiler_heap_shot_write_job_free_buffers (current_job);
		g_free (current_job);
		current_job = next_job;
	}
}

static void
profiler_add_heap_shot_write_job (ProfilerHeapShotWriteJob *job) {
	job->next = profiler->heap_shot_write_jobs;
	job->next_unwritten = job->next;
	profiler->heap_shot_write_jobs = job;
#if DEBUG_HEAP_PROFILER
	printf ("profiler_add_heap_shot_write_job: added job %p\n", job);
#endif
}

#if DEBUG_HEAP_PROFILER
#define STORE_ALLOCATED_OBJECT_MESSAGE1(d,o) printf ("STORE_ALLOCATED_OBJECT[TID %ld]: storing object %p at address %p\n", (d)->thread_id, (o), (d)->heap_shot_object_buffers->next_free_slot)
#define STORE_ALLOCATED_OBJECT_MESSAGE2(d,o) printf ("STORE_ALLOCATED_OBJECT[TID %ld]: storing object %p at address %p in new buffer %p\n", (d)->thread_id, (o), buffer->next_free_slot, buffer)
#else
#define STORE_ALLOCATED_OBJECT_MESSAGE1(d,o)
#define STORE_ALLOCATED_OBJECT_MESSAGE2(d,o)
#endif
#define STORE_ALLOCATED_OBJECT(d,o) do {\
	if ((d)->heap_shot_object_buffers->next_free_slot < (d)->heap_shot_object_buffers->end) {\
		STORE_ALLOCATED_OBJECT_MESSAGE1 ((d), (o));\
		*((d)->heap_shot_object_buffers->next_free_slot) = (o);\
		(d)->heap_shot_object_buffers->next_free_slot ++;\
	} else {\
		ProfilerHeapShotObjectBuffer *buffer = profiler_heap_shot_object_buffer_new (d);\
		STORE_ALLOCATED_OBJECT_MESSAGE2 ((d), (o));\
		*((buffer)->next_free_slot) = (o);\
		(buffer)->next_free_slot ++;\
	}\
} while (0)

static ProfilerPerThreadData*
profiler_per_thread_data_new (guint32 buffer_size)
{
	ProfilerPerThreadData *data = g_new (ProfilerPerThreadData, 1);

	data->events = g_new0 (ProfilerEventData, buffer_size);
	data->next_free_event = data->events;
	data->end_event = data->events + (buffer_size - 1);
	data->first_unwritten_event = data->events;
	data->first_unmapped_event = data->events;
	MONO_PROFILER_GET_CURRENT_COUNTER (data->start_event_counter);
	data->last_event_counter = data->start_event_counter;
	data->thread_id = CURRENT_THREAD_ID ();
	data->heap_shot_object_buffers = NULL;
	if ((profiler->action_flags.unreachable_objects == TRUE) || (profiler->action_flags.heap_shot == TRUE)) {
		profiler_heap_shot_object_buffer_new (data);
	}
	return data;
}

static void
profiler_per_thread_data_destroy (ProfilerPerThreadData *data) {
	g_free (data->events);
	profiler_heap_shot_object_buffers_destroy (data->heap_shot_object_buffers);
	g_free (data);
}

static ProfilerStatisticalData*
profiler_statistical_data_new (MonoProfiler *profiler) {
	int buffer_size = profiler->statistical_buffer_size * (profiler->statistical_call_chain_depth + 1);
	ProfilerStatisticalData *data = g_new (ProfilerStatisticalData, 1);

	data->addresses = g_new0 (gpointer, buffer_size);
	data->next_free_index = 0;
	data->end_index = buffer_size;
	data->first_unwritten_index = 0;
	
	return data;
}

static void
profiler_statistical_data_destroy (ProfilerStatisticalData *data) {
	g_free (data->addresses);
	g_free (data);
}

static void
profiler_add_write_buffer (void) {
	if (profiler->current_write_buffer->next == NULL) {
		profiler->current_write_buffer->next = g_malloc (sizeof (ProfilerFileWriteBuffer) + PROFILER_FILE_WRITE_BUFFER_SIZE);
		profiler->current_write_buffer->next->next = NULL;
		
		//printf ("Added next buffer %p, to buffer %p\n", profiler->current_write_buffer->next, profiler->current_write_buffer);
		
	}
	profiler->current_write_buffer = profiler->current_write_buffer->next;
	profiler->current_write_position = 0;
	profiler->full_write_buffers ++;
}

static void
profiler_free_write_buffers (void) {
	ProfilerFileWriteBuffer *current_buffer = profiler->write_buffers;
	while (current_buffer != NULL) {
		ProfilerFileWriteBuffer *next_buffer = current_buffer->next;
		
		//printf ("Freeing write buffer %p, next is %p\n", current_buffer, next_buffer);
		
		g_free (current_buffer);
		current_buffer = next_buffer;
	}
}

#define WRITE_BYTE(b) do {\
	if (profiler->current_write_position >= PROFILER_FILE_WRITE_BUFFER_SIZE) {\
		profiler_add_write_buffer ();\
	}\
	profiler->current_write_buffer->buffer [profiler->current_write_position] = (b);\
	profiler->current_write_position ++;\
} while (0)


static void
write_current_block (guint16 code) {
	guint32 size = (profiler->full_write_buffers * PROFILER_FILE_WRITE_BUFFER_SIZE) + profiler->current_write_position;
	ProfilerFileWriteBuffer *current_buffer = profiler->write_buffers;
	guint64 current_counter;
	guint32 counter_delta;
	guint8 header [10];
	
	MONO_PROFILER_GET_CURRENT_COUNTER (current_counter);
	if (profiler->last_header_counter != 0) {
		counter_delta = current_counter - profiler->last_header_counter;
	} else {
		counter_delta = 0;
	}
	profiler->last_header_counter = current_counter;
	
	header [0] = code & 0xff;
	header [1] = (code >> 8) & 0xff;
	header [2] = size & 0xff;
	header [3] = (size >> 8) & 0xff;
	header [4] = (size >> 16) & 0xff;
	header [5] = (size >> 24) & 0xff;
	header [6] = counter_delta & 0xff;
	header [7] = (counter_delta >> 8) & 0xff;
	header [8] = (counter_delta >> 16) & 0xff;
	header [9] = (counter_delta >> 24) & 0xff;
	
	WRITE_BUFFER (& (header [0]), 10);
	
	while ((current_buffer != NULL) && (profiler->full_write_buffers > 0)) {
		WRITE_BUFFER (& (current_buffer->buffer [0]), PROFILER_FILE_WRITE_BUFFER_SIZE);
		profiler->full_write_buffers --;
		current_buffer = current_buffer->next;
	}
	if (profiler->current_write_position > 0) {
		WRITE_BUFFER (& (current_buffer->buffer [0]), profiler->current_write_position);
	}
	FLUSH_FILE ();
	
	profiler->current_write_buffer = profiler->write_buffers;
	profiler->current_write_position = 0;
	profiler->full_write_buffers = 0;
}


#define SEVEN_BITS_MASK (0x7f)
#define EIGHT_BIT_MASK (0x80)

static void
write_uint32 (guint32 value) {
	while (value > SEVEN_BITS_MASK) {
		WRITE_BYTE (value & SEVEN_BITS_MASK);
		value >>= 7;
	}
	WRITE_BYTE (value | EIGHT_BIT_MASK);
}
static void
write_uint64 (guint64 value) {
	while (value > SEVEN_BITS_MASK) {
		WRITE_BYTE (value & SEVEN_BITS_MASK);
		value >>= 7;
	}
	WRITE_BYTE (value | EIGHT_BIT_MASK);
}
static void
write_string (const char *string) {
	while (*string != 0) {
		WRITE_BYTE (*string);
		string ++;
	}
	WRITE_BYTE (0);
}

#if DEBUG_HEAP_PROFILER
#define WRITE_HEAP_SHOT_JOB_VALUE_MESSAGE(v,c) printf ("WRITE_HEAP_SHOT_JOB_VALUE: writing value %p at cursor %p\n", (v), (c))
#else
#define WRITE_HEAP_SHOT_JOB_VALUE_MESSAGE(v,c)
#endif
#define WRITE_HEAP_SHOT_JOB_VALUE(j,v) do {\
	if ((j)->cursor < (j)->end) {\
		WRITE_HEAP_SHOT_JOB_VALUE_MESSAGE ((v), ((j)->cursor));\
		*((j)->cursor) = (v);\
		(j)->cursor ++;\
	} else {\
		profiler_heap_shot_write_job_add_buffer (j, v);\
	}\
} while (0)


#undef GUINT_TO_POINTER
#undef GPOINTER_TO_UINT
#if (SIZEOF_VOID_P == 4)
#define GUINT_TO_POINTER(u) ((void*)(guint32)(u))
#define GPOINTER_TO_UINT(p) ((guint32)(void*)(p))
#elif (SIZEOF_VOID_P == 8)
#define GUINT_TO_POINTER(u) ((void*)(guint64)(u))
#define GPOINTER_TO_UINT(p) ((guint64)(void*)(p))
#else
#error Bad size of void pointer
#endif

#define WRITE_HEAP_SHOT_JOB_VALUE_WITH_CODE(j,v,c) WRITE_HEAP_SHOT_JOB_VALUE (j, GUINT_TO_POINTER (GPOINTER_TO_UINT (v)|(c)))

#if DEBUG_HEAP_PROFILER
#define UPDATE_JOB_BUFFER_CURSOR_MESSAGE() printf ("profiler_heap_shot_write_block[UPDATE_JOB_BUFFER_CURSOR]: in job %p, moving to buffer %p and cursor %p\n", job, buffer, cursor)
#else
#define UPDATE_JOB_BUFFER_CURSOR_MESSAGE()
#endif
#define UPDATE_JOB_BUFFER_CURSOR() do {\
	cursor++;\
	if (cursor >= end) {\
		buffer = buffer->next;\
		if (buffer != NULL) {\
			cursor = & (buffer->buffer [0]);\
			if (buffer->next != NULL) {\
				end = & (buffer->buffer [PROFILER_HEAP_SHOT_WRITE_BUFFER_SIZE]);\
			} else {\
				end = job->cursor;\
			}\
		} else {\
			cursor = NULL;\
		}\
	}\
	UPDATE_JOB_BUFFER_CURSOR_MESSAGE ();\
} while (0)

static void
profiler_heap_shot_write_block (ProfilerHeapShotWriteJob *job) {
	ProfilerHeapShotWriteBuffer *buffer;
	gpointer* cursor;
	gpointer* end;
	guint64 start_counter;
	guint64 start_time;
	guint64 end_counter;
	guint64 end_time;
	
	write_uint64 (job->start_counter);
	write_uint64 (job->start_time);
	write_uint64 (job->end_counter);
	write_uint64 (job->end_time);
	write_uint32 (job->collection);
	MONO_PROFILER_GET_CURRENT_COUNTER (start_counter);
	MONO_PROFILER_GET_CURRENT_TIME (start_time);
	write_uint64 (start_counter);
	write_uint64 (start_time);
#if DEBUG_HEAP_PROFILER
	printf ("profiler_heap_shot_write_block: working on job %p...\n", job);
#endif
	buffer = job->buffers;
	cursor = & (buffer->buffer [0]);
	if (buffer->next != NULL) {
		end = & (buffer->buffer [PROFILER_HEAP_SHOT_WRITE_BUFFER_SIZE]);
	} else {
		end = job->cursor;
	}
	if (cursor >= end) {
		cursor = NULL;
	}
#if DEBUG_HEAP_PROFILER
	printf ("profiler_heap_shot_write_block: in job %p, starting at buffer %p and cursor %p\n", job, buffer, cursor);
#endif
	while (cursor != NULL) {
		gpointer value = *cursor;
		HeapProfilerJobValueCode code = GPOINTER_TO_UINT (value) & HEAP_CODE_MASK;
#if DEBUG_HEAP_PROFILER
		printf ("profiler_heap_shot_write_block: got value %p and code %d\n", value, code);
#endif
		
		UPDATE_JOB_BUFFER_CURSOR ();
		if (code == HEAP_CODE_FREE_OBJECT_CLASS) {
			MonoClass *klass = GUINT_TO_POINTER (GPOINTER_TO_UINT (value) & (~ (guint64) HEAP_CODE_MASK));
			//MonoClass *klass = GUINT_TO_POINTER (GPOINTER_TO_UINT (value) % 4);
			ClassIdMappingElement *class_id;
			guint32 size;
			
			class_id = class_id_mapping_element_get (klass);
			if (class_id == NULL) {
				printf ("profiler_heap_shot_write_block: unknown class %p", klass);
			}
			g_assert (class_id != NULL);
			write_uint32 ((class_id->id << 2) | HEAP_CODE_FREE_OBJECT_CLASS);
			
			size = GPOINTER_TO_UINT (*cursor);
			UPDATE_JOB_BUFFER_CURSOR ();
			write_uint32 (size);
#if DEBUG_HEAP_PROFILER
			printf ("profiler_heap_shot_write_block: wrote unreachable object of class %p (id %d, size %d)\n", klass, class_id->id, size);
#endif
		} else if (code == HEAP_CODE_OBJECT) {
			MonoObject *object = GUINT_TO_POINTER (GPOINTER_TO_UINT (value) & (~ (guint64) HEAP_CODE_MASK));
			MonoClass *klass = mono_object_get_class (object);
			ClassIdMappingElement *class_id = class_id_mapping_element_get (klass);
			guint32 size = mono_object_get_size (object);
			guint32 references = GPOINTER_TO_UINT (*cursor);
			UPDATE_JOB_BUFFER_CURSOR ();
			
			if (class_id == NULL) {
				printf ("profiler_heap_shot_write_block: unknown class %p", klass);
			}
			g_assert (class_id != NULL);
			
			write_uint64 (GPOINTER_TO_UINT (value));
			write_uint32 (class_id->id);
			write_uint32 (size);
			write_uint32 (references);
#if DEBUG_HEAP_PROFILER
			printf ("profiler_heap_shot_write_block: writing object %p (references %d)\n", value, references);
#endif
			
			while (references > 0) {
				gpointer reference = *cursor;
				write_uint64 (GPOINTER_TO_UINT (reference));
				UPDATE_JOB_BUFFER_CURSOR ();
				references --;
#if DEBUG_HEAP_PROFILER
				printf ("profiler_heap_shot_write_block:   inside object %p, wrote reference %p)\n", value, reference);
#endif
			}
		} else {
#if DEBUG_HEAP_PROFILER
			printf ("profiler_heap_shot_write_block: unknown code %d in value %p\n", code, value);
#endif
			g_assert_not_reached ();
		}
	}
	write_uint32 (0);
	
	MONO_PROFILER_GET_CURRENT_COUNTER (end_counter);
	MONO_PROFILER_GET_CURRENT_TIME (end_time);
	write_uint64 (end_counter);
	write_uint64 (end_time);
	
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_HEAP);
	
	profiler_heap_shot_write_job_free_buffers (job);
#if DEBUG_HEAP_PROFILER
	printf ("profiler_heap_shot_write_block: work on job %p done.\n", job);
#endif
}

static void
write_element_load_block (LoadedElement *element, guint8 kind, gsize thread_id) {
	WRITE_BYTE (kind);
	write_uint64 (element->load_start_counter);
	write_uint64 (element->load_end_counter);
	write_uint64 (thread_id);
	write_string (element->name);
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_LOADED);
	element->load_written = TRUE;
}

static void
write_element_unload_block (LoadedElement *element, guint8 kind, gsize thread_id) {
	WRITE_BYTE (kind);
	write_uint64 (element->unload_start_counter);
	write_uint64 (element->unload_end_counter);
	write_uint64 (thread_id);
	write_string (element->name);
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_UNLOADED);
	element->unload_written = TRUE;
}

static void
write_clock_data (void) {
	guint64 counter;
	guint64 time;
	
	MONO_PROFILER_GET_CURRENT_COUNTER (counter);
	MONO_PROFILER_GET_CURRENT_TIME (time);
	
	write_uint64 (counter);
	write_uint64 (time);
}

static void
write_mapping_block (gsize thread_id) {
	ClassIdMappingElement *current_class;
	MethodIdMappingElement *current_method;
	
	if ((profiler->classes->unwritten == NULL) && (profiler->methods->unwritten == NULL))
		return;
	
#if (DEBUG_MAPPING_EVENTS)
	printf ("[write_mapping_block][TID %ld] START\n", thread_id);
#endif
	
	write_clock_data ();
	write_uint64 (thread_id);
	
	for (current_class = profiler->classes->unwritten; current_class != NULL; current_class = current_class->next_unwritten) {
		write_uint32 (current_class->id);
		write_string (current_class->name);
#if (DEBUG_MAPPING_EVENTS)
		printf ("mapping CLASS (%d => %s)\n", current_class->id, current_class->name);
#endif
		g_free (current_class->name);
		current_class->name = NULL;
	}
	write_uint32 (0);
	profiler->classes->unwritten = NULL;
	
	for (current_method = profiler->methods->unwritten; current_method != NULL; current_method = current_method->next_unwritten) {
		MonoMethod *method = current_method->method;
		MonoClass *klass = mono_method_get_class (method);
		ClassIdMappingElement *class_element = class_id_mapping_element_get (klass);
		g_assert (class_element != NULL);
		write_uint32 (current_method->id);
		write_uint32 (class_element->id);
		write_string (current_method->name);
#if (DEBUG_MAPPING_EVENTS)
		printf ("mapping METHOD ([%d]%d => %s)\n", class_element?class_element->id:1, current_method->id, current_method->name);
#endif
		g_free (current_method->name);
		current_method->name = NULL;
	}
	write_uint32 (0);
	profiler->methods->unwritten = NULL;
	
	write_clock_data ();
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_MAPPING);
	
#if (DEBUG_MAPPING_EVENTS)
	printf ("[write_mapping_block][TID %ld] END\n", thread_id);
#endif
}

typedef enum {
	MONO_PROFILER_PACKED_EVENT_CODE_METHOD_ENTER = 1,
	MONO_PROFILER_PACKED_EVENT_CODE_METHOD_EXIT_IMPLICIT = 2,
	MONO_PROFILER_PACKED_EVENT_CODE_METHOD_EXIT_EXPLICIT = 3,
	MONO_PROFILER_PACKED_EVENT_CODE_CLASS_ALLOCATION = 4,
	MONO_PROFILER_PACKED_EVENT_CODE_METHOD_EVENT = 5,
	MONO_PROFILER_PACKED_EVENT_CODE_CLASS_EVENT = 6,
	MONO_PROFILER_PACKED_EVENT_CODE_OTHER_EVENT = 7
} MonoProfilerPackedEventCode;
#define MONO_PROFILER_PACKED_EVENT_CODE_BITS 3
#define MONO_PROFILER_PACKED_EVENT_DATA_BITS (8-MONO_PROFILER_PACKED_EVENT_CODE_BITS)
#define MONO_PROFILER_PACKED_EVENT_DATA_MASK ((1<<MONO_PROFILER_PACKED_EVENT_DATA_BITS)-1)

#define MONO_PROFILER_EVENT_MAKE_PACKED_CODE(result,data,base) do {\
	result = ((base)|((data & MONO_PROFILER_PACKED_EVENT_DATA_MASK) << MONO_PROFILER_PACKED_EVENT_CODE_BITS));\
	data >>= MONO_PROFILER_PACKED_EVENT_DATA_BITS;\
} while (0)
#define MONO_PROFILER_EVENT_MAKE_FULL_CODE(result,code,kind,base) do {\
	result = ((base)|((((kind)<<4) | (code)) << MONO_PROFILER_PACKED_EVENT_CODE_BITS));\
} while (0)

static ProfilerEventData*
write_event (ProfilerEventData *event) {
	ProfilerEventData *next = event + 1;
	gboolean write_event_value = TRUE;
	guint8 event_code;
	guint64 event_data;
	guint64 event_value;

	event_value = event->value;
	if (event_value == MAX_EVENT_VALUE) {
		event_value = *((guint64*)next);
		next ++;
	}
	
	if (event->data_type == MONO_PROFILER_EVENT_DATA_TYPE_METHOD) {
		MethodIdMappingElement *element = method_id_mapping_element_get (event->data.address);
		g_assert (element != NULL);
		event_data = element->id;
		
		if (event->code == MONO_PROFILER_EVENT_METHOD_CALL) {
			if (event->kind == MONO_PROFILER_EVENT_KIND_START) {
				MONO_PROFILER_EVENT_MAKE_PACKED_CODE (event_code, event_data, MONO_PROFILER_PACKED_EVENT_CODE_METHOD_ENTER);
			} else {
				MONO_PROFILER_EVENT_MAKE_PACKED_CODE (event_code, event_data, MONO_PROFILER_PACKED_EVENT_CODE_METHOD_EXIT_EXPLICIT);
			}
		} else {
			MONO_PROFILER_EVENT_MAKE_FULL_CODE (event_code, event->code, event->kind, MONO_PROFILER_PACKED_EVENT_CODE_METHOD_EVENT); 
		}
	} else if (event->data_type == MONO_PROFILER_EVENT_DATA_TYPE_CLASS) {
		ClassIdMappingElement *element = class_id_mapping_element_get (event->data.address);
		g_assert (element != NULL);
		event_data = element->id;
		
		if (event->code == MONO_PROFILER_EVENT_CLASS_ALLOCATION) {
			MONO_PROFILER_EVENT_MAKE_PACKED_CODE (event_code, event_data, MONO_PROFILER_PACKED_EVENT_CODE_CLASS_ALLOCATION);
		} else {
			MONO_PROFILER_EVENT_MAKE_FULL_CODE (event_code, event->code, event->kind, MONO_PROFILER_PACKED_EVENT_CODE_CLASS_EVENT);
		}
	} else {
		event_data = event->data.number;
		MONO_PROFILER_EVENT_MAKE_FULL_CODE (event_code, event->code, event->kind, MONO_PROFILER_PACKED_EVENT_CODE_OTHER_EVENT);
	}
	
#if (DEBUG_LOGGING_PROFILER)
	EVENT_MARK ();
	printf ("writing EVENT[%p] data_type:%d, kind:%d, code:%d (%d:%ld:%ld)\n", event,
			event->data_type, event->kind, event->code,
			event_code, event_data, event_value);
#endif
	
	WRITE_BYTE (event_code);
	write_uint64 (event_data);
	if (write_event_value) {
		write_uint64 (event_value);
	}
	
	return next;
}

static void
write_thread_data_block (ProfilerPerThreadData *data) {
	ProfilerEventData *start = data->first_unwritten_event;
	ProfilerEventData *end = data->first_unmapped_event;
	
	if (start == end)
		return;
	
	write_clock_data ();
	write_uint64 (data->thread_id);
	
	write_uint64 (data->start_event_counter);
	
	while (start < end) {
		start = write_event (start);
	}
	WRITE_BYTE (0);
	data->first_unwritten_event = end;
	
	write_clock_data ();
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_EVENTS);
}

static ProfilerExecutableMemoryRegionData*
profiler_executable_memory_region_new (gpointer *start, gpointer *end, guint32 file_offset, char *file_name, guint32 id) {
	ProfilerExecutableMemoryRegionData *result = g_new (ProfilerExecutableMemoryRegionData, 1);
	result->start = start;
	result->end = end;
	result->file_offset = file_offset;
	result->file_name = g_strdup (file_name);
	result->id = id;
	result->is_new = TRUE;
	
	result->file = NULL;
	result->symbols_capacity = id;
	result->symbols_count = id;
	result->symbols = NULL;
	
	return result;
}

static void
executable_file_close (ProfilerExecutableMemoryRegionData *region);

static void
profiler_executable_memory_region_destroy (ProfilerExecutableMemoryRegionData *data) {
	if (data->file_name != NULL) {
		g_free (data->file_name);
	}
	if (data->symbols != NULL) {
		g_free (data->symbols);
	}
	if (data->file != NULL) {
		executable_file_close (data);
	}
	g_free (data);
}

static ProfilerExecutableMemoryRegions*
profiler_executable_memory_regions_new (int next_id, int next_unmanaged_function_id) {
	ProfilerExecutableMemoryRegions *result = g_new (ProfilerExecutableMemoryRegions, 1);
	result->regions = g_new0 (ProfilerExecutableMemoryRegionData*, 32);
	result->regions_capacity = 32;
	result->regions_count = 0;
	result->next_id = next_id;
	result->next_unmanaged_function_id = next_unmanaged_function_id;
	return result;
}

static void
profiler_executable_memory_regions_destroy (ProfilerExecutableMemoryRegions *regions) {
	int i;
	
	for (i = 0; i < regions->regions_count; i++) {
		profiler_executable_memory_region_destroy (regions->regions [i]);
	}
	g_free (regions->regions);
	g_free (regions);
}

static ProfilerExecutableMemoryRegionData*
find_address_region (ProfilerExecutableMemoryRegions *regions, gpointer address) {
	int low_index = 0;
	int high_index = regions->regions_count;
	int middle_index = 0;
	ProfilerExecutableMemoryRegionData *middle_region = regions->regions [0];
	
	if ((regions->regions_count == 0) || (regions->regions [low_index]->start > address) || (regions->regions [high_index - 1]->end < address)) {
		return NULL;
	}
	
	//printf ("find_address_region: Looking for address %p in %d regions (from %p to %p)\n", address, regions->regions_count, regions->regions [low_index]->start, regions->regions [high_index - 1]->end);
	
	while (low_index != high_index) {
		middle_index = low_index + ((high_index - low_index) / 2);
		middle_region = regions->regions [middle_index];
		
		//printf ("find_address_region: Looking for address %p, considering index %d[%p-%p] (%d-%d)\n", address, middle_index, middle_region->start, middle_region->end, low_index, high_index);
		
		if (middle_region->start > address) {
			if (middle_index > 0) {
				high_index = middle_index;
			} else {
				return NULL;
			}
		} else if (middle_region->end < address) {
			if (middle_index < regions->regions_count - 1) {
				low_index = middle_index + 1;
			} else {
				return NULL;
			}
		} else {
			return middle_region;
		}
	}
	
	if ((middle_region == NULL) || (middle_region->start > address) || (middle_region->end < address)) {
		return NULL;
	} else {
		return middle_region;
	}
}

static void
append_region (ProfilerExecutableMemoryRegions *regions, gpointer *start, gpointer *end, guint32 file_offset, char *file_name) {
	if (regions->regions_count >= regions->regions_capacity) {
		ProfilerExecutableMemoryRegionData **new_regions = g_new0 (ProfilerExecutableMemoryRegionData*, regions->regions_capacity * 2);
		memcpy (new_regions, regions->regions, regions->regions_capacity * sizeof (ProfilerExecutableMemoryRegionData*));
		g_free (regions->regions);
		regions->regions = new_regions;
		regions->regions_capacity = regions->regions_capacity * 2;
	}
	regions->regions [regions->regions_count] = profiler_executable_memory_region_new (start, end, file_offset, file_name, regions->next_id);
	regions->regions_count ++;
	regions->next_id ++;
}

static void
restore_old_regions (ProfilerExecutableMemoryRegions *old_regions, ProfilerExecutableMemoryRegions *new_regions) {
	int old_i;
	int new_i;
	
	for (old_i = 0; old_i < old_regions->regions_count; old_i++) {
		ProfilerExecutableMemoryRegionData *old_region = old_regions->regions [old_i];
		for (new_i = 0; new_i < new_regions->regions_count; new_i++) {
			ProfilerExecutableMemoryRegionData *new_region = new_regions->regions [new_i];
			if ((old_region->start == new_region->start) &&
					(old_region->end == new_region->end) &&
					(old_region->file_offset == new_region->file_offset) &&
					! strcmp (old_region->file_name, new_region->file_name)) {
				new_regions->regions [new_i] = old_region;
				old_regions->regions [old_i] = new_region;
				
				// FIXME (sanity check)
				g_assert (new_region->is_new && ! old_region->is_new);
			}
		}
	}
}

static int
compare_regions (const void *a1, const void *a2) {
	ProfilerExecutableMemoryRegionData *r1 = * (ProfilerExecutableMemoryRegionData**) a1;
	ProfilerExecutableMemoryRegionData *r2 = * (ProfilerExecutableMemoryRegionData**) a2;
	return (r1->start < r2->start)? -1 : ((r1->start > r2->start)? 1 : 0);
}

static void
sort_regions (ProfilerExecutableMemoryRegions *regions) {
	qsort (regions->regions, regions->regions_count, sizeof (ProfilerExecutableMemoryRegionData *), compare_regions);
}

static void
executable_file_add_region_reference (ProfilerExecutableFile *file, ProfilerExecutableMemoryRegionData *region) {
	guint8 *section_headers = file->data + file->header->e_shoff;
	int section_index;
	
	for (section_index = 1; section_index < file->header->e_shnum; section_index ++) {
		ElfSection *section_header = (ElfSection*) (section_headers + (file->header->e_shentsize * section_index));
		
		if ((section_header->sh_addr != 0) && (section_header->sh_flags & ELF_SHF_EXECINSTR) &&
				(region->file_offset <= section_header->sh_offset) && (region->file_offset + (((guint8*)region->end)-((guint8*)region->start)) >= (section_header->sh_offset + section_header->sh_size))) {
			ProfilerExecutableFileSectionRegion *section_region = & (file->section_regions [section_index]);
			section_region->region = region;
			section_region->section_address = (gpointer) section_header->sh_addr;
			section_region->section_offset = section_header->sh_offset;
		}
	}
}

static ProfilerExecutableFile*
executable_file_open (ProfilerExecutableMemoryRegionData *region) {
	ProfilerExecutableFiles *files = & (profiler->executable_files);
	ProfilerExecutableFile *file = (ProfilerExecutableFile*) g_hash_table_lookup (files->table, region->file_name);
	if (file == NULL) {
		guint16 test = 0x0102;
		struct stat stat_buffer;
		int symtab_index = 0;
		int strtab_index = 0;
		int dynsym_index = 0;
		int dynstr_index = 0;
		ElfHeader *header;
		guint8 *section_headers;
		int section_index;
		int strings_index;
		
		file = g_new0 (ProfilerExecutableFile, 1);
		region->file = file;
		file->reference_count ++;
		
		file->fd = open (region->file_name, O_RDONLY);
		if (file->fd == -1) {
			//g_warning ("Cannot open file '%s': '%s'", region->file_name, strerror (errno));
			return file;
		} else {
			if (fstat (file->fd, &stat_buffer) != 0) {
				//g_warning ("Cannot stat file '%s': '%s'", region->file_name, strerror (errno));
				return file;
			} else {
				size_t region_length = ((guint8*)region->end) - ((guint8*)region->start);
				file->length = stat_buffer.st_size;
				
				if (file->length == region_length) {
					file->data = region->start;
					close (file->fd);
					file->fd = -1;
				} else {
					file->data = mmap (NULL, file->length, PROT_READ, MAP_PRIVATE, file->fd, 0);
					
					if (file->data == MAP_FAILED) {
						close (file->fd);
						//g_warning ("Cannot map file '%s': '%s'", region->file_name, strerror (errno));
						file->data = NULL;
						return file;
					}
				}
			}
		}
		
		header = (ElfHeader*) file->data;
		
		if ((header->e_ident [EI_MAG0] != 0x7f) || (header->e_ident [EI_MAG1] != 'E') ||
				(header->e_ident [EI_MAG2] != 'L') || (header->e_ident [EI_MAG3] != 'F')) {
			return file;
		}
		
		if (sizeof (gsize) == 4) {
			if (header->e_ident [EI_CLASS] != ELF_CLASS_32) {
				g_warning ("Class is not ELF_CLASS_32 with gsize size %d", (int) sizeof (gsize));
				return file;
			}
		} else if (sizeof (gsize) == 8) {
			if (header->e_ident [EI_CLASS] != ELF_CLASS_64) {
				g_warning ("Class is not ELF_CLASS_64 with gsize size %d", (int) sizeof (gsize));
				return file;
			}
		} else {
			g_warning ("Absurd gsize size %d", (int) sizeof (gsize));
			return file;
		}
		
		if ((*(guint8*)(&test)) == 0x01) {
			if (header->e_ident [EI_DATA] != ELF_DATA_MSB) {
				g_warning ("Data is not ELF_DATA_MSB with first test byte 0x01");
				return file;
			}
		} else if ((*(guint8*)(&test)) == 0x02) {
			if (header->e_ident [EI_DATA] != ELF_DATA_LSB) {
				g_warning ("Data is not ELF_DATA_LSB with first test byte 0x02");
				return file;
			}
		} else {
			g_warning ("Absurd test byte value");
			return file;
		}
		
		/* OK, this is a usable elf file... */
		file->header = header;
		section_headers = file->data + header->e_shoff;
		file->main_string_table = ((const char*) file->data) + (((ElfSection*) (section_headers + (header->e_shentsize * header->e_shstrndx)))->sh_offset);
		
		for (section_index = 0; section_index < header->e_shnum; section_index ++) {
			ElfSection *section_header = (ElfSection*) (section_headers + (header->e_shentsize * section_index));
			
			if (section_header->sh_type == ELF_SHT_SYMTAB) {
				symtab_index = section_index;
			} else if (section_header->sh_type == ELF_SHT_DYNSYM) {
				dynsym_index = section_index;
			} else if (section_header->sh_type == ELF_SHT_STRTAB) {
				if (! strcmp (file->main_string_table + section_header->sh_name, ".strtab")) {
					strtab_index = section_index;
				} else if (! strcmp (file->main_string_table + section_header->sh_name, ".dynstr")) {
					dynstr_index = section_index;
				}
			}
		}
		
		if ((symtab_index != 0) && (strtab_index != 0)) {
			section_index = symtab_index;
			strings_index = strtab_index;
		} else if ((dynsym_index != 0) && (dynstr_index != 0)) {
			section_index = dynsym_index;
			strings_index = dynstr_index;
		} else {
			section_index = 0;
			strings_index = 0;
		}
		
		if (section_index != 0) {
			ElfSection *section_header = (ElfSection*) (section_headers + (header->e_shentsize * section_index));
			file->symbol_size = section_header->sh_entsize;
			file->symbols_count = (guint32) (section_header->sh_size / section_header->sh_entsize);
			file->symbols_start = file->data + section_header->sh_offset;
			file->symbols_string_table = ((const char*) file->data) + (((ElfSection*) (section_headers + (header->e_shentsize * strings_index)))->sh_offset);
		}
		
		file->section_regions = g_new0 (ProfilerExecutableFileSectionRegion, file->header->e_shnum);
	} else {
		region->file = file;
		file->reference_count ++;
	}
	
	if (file->header != NULL) {
		executable_file_add_region_reference (file, region);
	}
	
	if (file->next_new_file == NULL) {
		file->next_new_file = files->new_files;
		files->new_files = file;
	}
	return file;
}

static void
executable_file_free (ProfilerExecutableFile* file) {
	if (file->fd != -1) {
		if (close (file->fd) != 0) {
			g_warning ("Cannot close file: '%s'", strerror (errno));
		}
		if (file->data != NULL) {
			if (munmap (file->data, file->length) != 0) {
				g_warning ("Cannot unmap file: '%s'", strerror (errno));
			}
		}
	}
	if (file->section_regions != NULL) {
		g_free (file->section_regions);
	}
	g_free (file);
}

static void
executable_file_close (ProfilerExecutableMemoryRegionData *region) {
	region->file->reference_count --;
	
	if (region->file->reference_count <= 0) {
		ProfilerExecutableFiles *files = & (profiler->executable_files);
		g_hash_table_remove (files->table, region->file_name);
		executable_file_free (region->file);
		region->file = NULL;
	}
}

static void
executable_file_count_symbols (ProfilerExecutableFile *file) {
	int symbol_index;
	
	for (symbol_index = 0; symbol_index < file->symbols_count; symbol_index ++) {
		ElfSymbol *symbol = (ElfSymbol*) (file->symbols_start + (symbol_index * file->symbol_size));
		
		if ((ELF_ST_TYPE (symbol->st_info) == ELF_STT_FUNC) &&
				(symbol->st_shndx > 0) &&
				(symbol->st_shndx < file->header->e_shnum)) {
			int symbol_section_index = symbol->st_shndx;
			ProfilerExecutableMemoryRegionData *region = file->section_regions [symbol_section_index].region;
			if ((region != NULL) && (region->symbols == NULL)) {
				region->symbols_count ++;
			}
		}
	}
}

static void
executable_memory_regions_prepare_symbol_tables (ProfilerExecutableMemoryRegions *regions) {
	int i;
	for (i = 0; i < regions->regions_count; i++) {
		ProfilerExecutableMemoryRegionData *region = regions->regions [i];
		if ((region->symbols_count > 0) && (region->symbols == NULL)) {
			region->symbols = g_new (ProfilerUnmanagedSymbol, region->symbols_count);
			region->symbols_capacity = region->symbols_count;
			region->symbols_count = 0;
		}
	}
}

static const char*
executable_region_symbol_get_name (ProfilerExecutableMemoryRegionData *region, ProfilerUnmanagedSymbol *symbol) {
	ElfSymbol *elf_symbol = (ElfSymbol*) (region->file->symbols_start + (symbol->index * region->file->symbol_size));
	return region->file->symbols_string_table + elf_symbol->st_name;
}

static void
executable_file_build_symbol_tables (ProfilerExecutableFile *file) {
	int symbol_index;
	
	for (symbol_index = 0; symbol_index < file->symbols_count; symbol_index ++) {
		ElfSymbol *symbol = (ElfSymbol*) (file->symbols_start + (symbol_index * file->symbol_size));
		
		if ((ELF_ST_TYPE (symbol->st_info) == ELF_STT_FUNC) &&
				(symbol->st_shndx > 0) &&
				(symbol->st_shndx < file->header->e_shnum)) {
			int symbol_section_index = symbol->st_shndx;
			ProfilerExecutableFileSectionRegion *section_region = & (file->section_regions [symbol_section_index]);
			ProfilerExecutableMemoryRegionData *region = section_region->region;
			
			if (region != NULL) {
				ProfilerUnmanagedSymbol *new_symbol = & (region->symbols [region->symbols_count]);
				region->symbols_count ++;
				
				new_symbol->id = 0;
				new_symbol->index = symbol_index;
				new_symbol->size = symbol->st_size;
				new_symbol->offset = (((guint8*) symbol->st_value) - section_region->section_address) - (region->file_offset - section_region->section_offset);
			}
		}
	}
}

static int
compare_region_symbols (const void *p1, const void *p2) {
	const ProfilerUnmanagedSymbol *s1 = p1;
	const ProfilerUnmanagedSymbol *s2 = p2;
	return (s1->offset < s2->offset)? -1 : ((s1->offset > s2->offset)? 1 : 0);
}

static void
executable_memory_regions_sort_symbol_tables (ProfilerExecutableMemoryRegions *regions) {
	int i;
	for (i = 0; i < regions->regions_count; i++) {
		ProfilerExecutableMemoryRegionData *region = regions->regions [i];
		if ((region->is_new) && (region->symbols != NULL)) {
			qsort (region->symbols, region->symbols_count, sizeof (ProfilerUnmanagedSymbol), compare_region_symbols);
		}
	}
}

static void
build_symbol_tables (ProfilerExecutableMemoryRegions *regions, ProfilerExecutableFiles *files) {
	int i;
	ProfilerExecutableFile *file;
	
	for (i = 0; i < regions->regions_count; i++) {
		ProfilerExecutableMemoryRegionData *region = regions->regions [i];
		if ((region->is_new) && (region->file == NULL)) {
			executable_file_open (region);
		}
	}
	
	for (file = files->new_files; file != NULL; file = file->next_new_file) {
		executable_file_count_symbols (file);
	}
	
	executable_memory_regions_prepare_symbol_tables (regions);
	
	for (file = files->new_files; file != NULL; file = file->next_new_file) {
		executable_file_build_symbol_tables (file);
	}
	
	executable_memory_regions_sort_symbol_tables (regions);
	
	file = files->new_files;
	while (file != NULL) {
		ProfilerExecutableFile *next_file = file->next_new_file;
		file->next_new_file = NULL;
		file = next_file;
	}
	files->new_files = NULL;
}

static ProfilerUnmanagedSymbol*
executable_memory_region_find_symbol (ProfilerExecutableMemoryRegionData *region, guint32 offset) {
	if (region->symbols_count > 0) {
		ProfilerUnmanagedSymbol *low = region->symbols;
		ProfilerUnmanagedSymbol *high = region->symbols + (region->symbols_count - 1);
		int step = region->symbols_count >> 1;
		ProfilerUnmanagedSymbol *current = region->symbols + step;
		
		do {
			step = (high - low) >> 1;
			
			if (offset < current->offset) {
				high = current;
				current = high - step;
			} else if (offset >= current->offset) {
				if (offset >= (current->offset + current->size)) {
					low = current;
					current = low + step;
				} else {
					return current;
				}
			}
		} while (step > 0);
		
		if ((offset >= current->offset) && (offset < (current->offset + current->size))) {
			return current;
		} else {
			return NULL;
		}
	} else {
		return NULL;
	}
}

//FIXME: make also Win32 and BSD variants
#define MAPS_BUFFER_SIZE 4096

static gboolean
update_regions_buffer (int fd, char *buffer) {
	ssize_t result = read (fd, buffer, MAPS_BUFFER_SIZE);
	
	if (result == MAPS_BUFFER_SIZE) {
		return TRUE;
	} else if (result >= 0) {
		*(buffer + result) = 0;
		return FALSE;
	} else {
		*buffer = 0;
		return FALSE;
	}
}

#define GOTO_NEXT_CHAR(c,b,fd) do {\
	(c)++;\
	if (((c) - (b) >= MAPS_BUFFER_SIZE) || ((*(c) == 0) && ((c) != (b)))) {\
		update_regions_buffer ((fd), (b));\
		(c) = (b);\
	}\
} while (0);

static int hex_digit_value (char c) {
	if ((c >= '0') && (c <= '9')) {
		return c - '0';
	} else if ((c >= 'a') && (c <= 'f')) {
		return c - 'a' + 10;
	} else if ((c >= 'A') && (c <= 'F')) {
		return c - 'A' + 10;
	} else {
		return 0;
	}
}

/*
 * Start address
 * -
 * End address
 * (space)
 * Permissions
 * Offset
 * (space)
 * Device
 * (space)
 * Inode
 * (space)
 * File
 * \n
 */
typedef enum {
	MAP_LINE_PARSER_STATE_INVALID,
	MAP_LINE_PARSER_STATE_START_ADDRESS,
	MAP_LINE_PARSER_STATE_END_ADDRESS,
	MAP_LINE_PARSER_STATE_PERMISSIONS,
	MAP_LINE_PARSER_STATE_OFFSET,
	MAP_LINE_PARSER_STATE_DEVICE,
	MAP_LINE_PARSER_STATE_INODE,
	MAP_LINE_PARSER_STATE_BLANK_BEFORE_FILENAME,
	MAP_LINE_PARSER_STATE_FILENAME,
	MAP_LINE_PARSER_STATE_DONE
} MapLineParserState;

const char *map_line_parser_state [] = {
	"INVALID",
	"START_ADDRESS",
	"END_ADDRESS",
	"PERMISSIONS",
	"OFFSET",
	"DEVICE",
	"INODE",
	"BLANK_BEFORE_FILENAME",
	"FILENAME",
	"DONE"
};

static char*
parse_map_line (ProfilerExecutableMemoryRegions *regions, int fd, char *buffer, char *current) {
	MapLineParserState state = MAP_LINE_PARSER_STATE_START_ADDRESS;
	gsize start_address = 0;
	gsize end_address = 0;
	guint32 offset = 0;
	char *start_filename = NULL;
	char *end_filename = NULL;
	gboolean is_executable = FALSE;
	gboolean done = FALSE;
	
	char c = *current;
	
	while (1) {
		switch (state) {
		case MAP_LINE_PARSER_STATE_START_ADDRESS:
			if (isxdigit (c)) {
				start_address <<= 4;
				start_address |= hex_digit_value (c);
			} else if (c == '-') {
				state = MAP_LINE_PARSER_STATE_END_ADDRESS;
			} else {
				state = MAP_LINE_PARSER_STATE_INVALID;
			}
			break;
		case MAP_LINE_PARSER_STATE_END_ADDRESS:
			if (isxdigit (c)) {
				end_address <<= 4;
				end_address |= hex_digit_value (c);
			} else if (isblank (c)) {
				state = MAP_LINE_PARSER_STATE_PERMISSIONS;
			} else {
				state = MAP_LINE_PARSER_STATE_INVALID;
			}
			break;
		case MAP_LINE_PARSER_STATE_PERMISSIONS:
			if (c == 'x') {
				is_executable = TRUE;
			} else if (isblank (c)) {
				state = MAP_LINE_PARSER_STATE_OFFSET;
			} else if ((c != '-') && ! isalpha (c)) {
				state = MAP_LINE_PARSER_STATE_INVALID;
			}
			break;
		case MAP_LINE_PARSER_STATE_OFFSET:
			if (isxdigit (c)) {
				offset <<= 4;
				offset |= hex_digit_value (c);
			} else if (isblank (c)) {
				state = MAP_LINE_PARSER_STATE_DEVICE;
			} else {
				state = MAP_LINE_PARSER_STATE_INVALID;
			}
			break;
		case MAP_LINE_PARSER_STATE_DEVICE:
			if (isblank (c)) {
				state = MAP_LINE_PARSER_STATE_INODE;
			} else if ((c != ':') && ! isxdigit (c)) {
				state = MAP_LINE_PARSER_STATE_INVALID;
			}
			break;
		case MAP_LINE_PARSER_STATE_INODE:
			if (isblank (c)) {
				state = MAP_LINE_PARSER_STATE_BLANK_BEFORE_FILENAME;
			} else if (! isdigit (c)) {
				state = MAP_LINE_PARSER_STATE_INVALID;
			}
			break;
		case MAP_LINE_PARSER_STATE_BLANK_BEFORE_FILENAME:
			if ((c == '/') || (c == '[')) {
				state = MAP_LINE_PARSER_STATE_FILENAME;
				start_filename = current;
			} else if (! isblank (c)) {
				state = MAP_LINE_PARSER_STATE_INVALID;
			}
			break;
		case MAP_LINE_PARSER_STATE_FILENAME:
			if (c == '\n') {
				state = MAP_LINE_PARSER_STATE_DONE;
				done = TRUE;
				end_filename = current;
			}
			break;
		case MAP_LINE_PARSER_STATE_DONE:
			if (done && is_executable) {
				*end_filename = 0;
				append_region (regions, (gpointer) start_address, (gpointer) end_address, offset, start_filename);
			}
			return current;
		case MAP_LINE_PARSER_STATE_INVALID:
			if (c == '\n') {
				state = MAP_LINE_PARSER_STATE_DONE;
			}
			break;
		}
		
		if (c == 0) {
			return NULL;
		} else if (c == '\n') {
			state = MAP_LINE_PARSER_STATE_DONE;
		}
		
		GOTO_NEXT_CHAR(current, buffer, fd);
		c = *current;
	}
}

static gboolean
scan_process_regions (ProfilerExecutableMemoryRegions *regions) {
	char *buffer;
	char *current;
	int fd;
	
	fd = open ("/proc/self/maps", O_RDONLY);
	if (fd == -1) {
		return FALSE;
	}
	
	buffer = malloc (MAPS_BUFFER_SIZE);
	update_regions_buffer (fd, buffer);
	current = buffer;
	while (current != NULL) {
		current = parse_map_line (regions, fd, buffer, current);
	}
	
	free (buffer);
	
	close (fd);
	return TRUE;
}
//End of Linux code

typedef enum {
	MONO_PROFILER_STATISTICAL_CODE_END = 0,
	MONO_PROFILER_STATISTICAL_CODE_METHOD = 1,
	MONO_PROFILER_STATISTICAL_CODE_UNMANAGED_FUNCTION_ID = 2,
	MONO_PROFILER_STATISTICAL_CODE_UNMANAGED_FUNCTION_NEW_ID = 3,
	MONO_PROFILER_STATISTICAL_CODE_UNMANAGED_FUNCTION_OFFSET_IN_REGION = 4,
	MONO_PROFILER_STATISTICAL_CODE_CALL_CHAIN = 5,
	MONO_PROFILER_STATISTICAL_CODE_REGIONS = 7
} MonoProfilerStatisticalCode;

static void
refresh_memory_regions (void) {
	ProfilerExecutableMemoryRegions *old_regions = profiler->executable_regions;
	ProfilerExecutableMemoryRegions *new_regions = profiler_executable_memory_regions_new (old_regions->next_id, old_regions->next_unmanaged_function_id);
	int i;
	
	LOG_WRITER_THREAD ("Refreshing memory regions...");
	scan_process_regions (new_regions);
	restore_old_regions (old_regions, new_regions);
	sort_regions (new_regions);
	LOG_WRITER_THREAD ("Refreshed memory regions.");
	
	LOG_WRITER_THREAD ("Building symbol tables...");
	build_symbol_tables (new_regions, & (profiler->executable_files));
#if 0
	printf ("Symbol tables done!\n");
	printf ("Region summary...\n");
	for (i = 0; i < new_regions->regions_count; i++) {
		ProfilerExecutableMemoryRegionData *region = new_regions->regions [i];
		printf ("Region %d[%d][NEW:%d] (%p-%p) at %d in file %s\n", i, region->id, region->is_new,
				region->start, region->end, region->file_offset, region->file_name);
	}
	printf ("New symbol tables dump...\n");
	for (i = 0; i < new_regions->regions_count; i++) {
		ProfilerExecutableMemoryRegionData *region = new_regions->regions [i];
		
		if (region->is_new) {
			int symbol_index;
			
			printf ("Region %d[%d][NEW:%d] (%p-%p) at %d in file %s\n", i, region->id, region->is_new,
					region->start, region->end, region->file_offset, region->file_name);
			for (symbol_index = 0; symbol_index < region->symbols_count; symbol_index ++) {
				ProfilerUnmanagedSymbol *symbol = & (region->symbols [symbol_index]);
				printf ("  [%d] Symbol %s (offset %d, size %d)\n", symbol_index,
						executable_region_symbol_get_name (region, symbol),
						symbol->offset, symbol->size);
			}
		}
	}
#endif
	LOG_WRITER_THREAD ("Built symbol tables.");
	
	// This marks the region "sub-block"
	write_uint32 (MONO_PROFILER_STATISTICAL_CODE_REGIONS);
	
	// First write the "removed" regions 
	for (i = 0; i < old_regions->regions_count; i++) {
		ProfilerExecutableMemoryRegionData *region = old_regions->regions [i];
		if (! region->is_new) {
#if DEBUG_STATISTICAL_PROFILER
			printf ("[refresh_memory_regions] Invalidated region %d\n", region->id);
#endif
			write_uint32 (region->id);
		}
	}
	write_uint32 (0);
	
	// Then write the new ones
	for (i = 0; i < new_regions->regions_count; i++) {
		ProfilerExecutableMemoryRegionData *region = new_regions->regions [i];
		if (region->is_new) {
			region->is_new = FALSE;
			
#if DEBUG_STATISTICAL_PROFILER
			printf ("[refresh_memory_regions] Wrote region %d (%p-%p[%d] '%s')\n", region->id, region->start, region->end, region->file_offset, region->file_name);
#endif
			write_uint32 (region->id);
			write_uint64 (GPOINTER_TO_UINT (region->start));
			write_uint32 (GPOINTER_TO_UINT (region->end) - GPOINTER_TO_UINT (region->start));
			write_uint32 (region->file_offset);
			write_string (region->file_name);
		}
	}
	write_uint32 (0);
	
	// Finally, free the old ones, and replace them
	profiler_executable_memory_regions_destroy (old_regions);
	profiler->executable_regions = new_regions;
}

static gboolean
write_statistical_hit (MonoDomain *domain, gpointer address, gboolean regions_refreshed) {
	MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), (char*) address);
	
	if (ji != NULL) {
		MonoMethod *method = mono_jit_info_get_method (ji);
		MethodIdMappingElement *element = method_id_mapping_element_get (method);
		
		if (element != NULL) {
#if DEBUG_STATISTICAL_PROFILER
			printf ("[write_statistical_hit] Wrote method %d\n", element->id);
#endif
			write_uint32 ((element->id << 3) | MONO_PROFILER_STATISTICAL_CODE_METHOD);
		} else {
#if DEBUG_STATISTICAL_PROFILER
			printf ("[write_statistical_hit] Wrote unknown method %p\n", method);
#endif
			write_uint32 (MONO_PROFILER_STATISTICAL_CODE_METHOD);
		}
	} else {
		ProfilerExecutableMemoryRegionData *region = find_address_region (profiler->executable_regions, address);
		
		if (region == NULL && ! regions_refreshed) {
#if DEBUG_STATISTICAL_PROFILER
			printf ("[write_statistical_hit] Cannot find region for address %p, refreshing...\n", address);
#endif
			refresh_memory_regions ();
			regions_refreshed = TRUE;
			region = find_address_region (profiler->executable_regions, address);
		}
		
		if (region != NULL) {
			guint32 offset = ((guint8*)address) - ((guint8*)region->start);
			ProfilerUnmanagedSymbol *symbol = executable_memory_region_find_symbol (region, offset);
			
			if (symbol != NULL) {
				if (symbol->id > 0) {
#if DEBUG_STATISTICAL_PROFILER
					printf ("[write_statistical_hit] Wrote unmanaged symbol %d\n", symbol->id);
#endif
					write_uint32 ((symbol->id << 3) | MONO_PROFILER_STATISTICAL_CODE_UNMANAGED_FUNCTION_ID);
				} else {
					ProfilerExecutableMemoryRegions *regions = profiler->executable_regions;
					const char *symbol_name = executable_region_symbol_get_name (region, symbol);
					symbol->id = regions->next_unmanaged_function_id;
					regions->next_unmanaged_function_id ++;
#if DEBUG_STATISTICAL_PROFILER
					printf ("[write_statistical_hit] Wrote new unmanaged symbol in region %d[%d]\n", region->id, offset);
#endif
					write_uint32 ((region->id << 3) | MONO_PROFILER_STATISTICAL_CODE_UNMANAGED_FUNCTION_NEW_ID);
					write_uint32 (symbol->id);
					write_string (symbol_name);
				}
			} else {
#if DEBUG_STATISTICAL_PROFILER
				printf ("[write_statistical_hit] Wrote unknown unmanaged hit in region %d[%d] (address %p)\n", region->id, offset, address);
#endif
				write_uint32 ((region->id << 3) | MONO_PROFILER_STATISTICAL_CODE_UNMANAGED_FUNCTION_OFFSET_IN_REGION);
				write_uint32 (offset);
			}
		} else {
#if DEBUG_STATISTICAL_PROFILER
			printf ("[write_statistical_hit] Wrote unknown unmanaged hit %p\n", address);
#endif
			write_uint32 (MONO_PROFILER_STATISTICAL_CODE_UNMANAGED_FUNCTION_OFFSET_IN_REGION);
			write_uint64 (GPOINTER_TO_UINT (address));
		}
	}
	
	return regions_refreshed;
}

static void
flush_all_mappings (void);

static void
write_statistical_data_block (ProfilerStatisticalData *data) {
	int start_index = data->first_unwritten_index;
	int end_index = data->next_free_index;
	gboolean regions_refreshed = FALSE;
	int call_chain_depth = profiler->statistical_call_chain_depth;
	MonoDomain *domain = mono_domain_get ();
	int index;
	
	if (end_index > data->end_index)
		end_index = data->end_index;
	
	if (start_index == end_index)
		return;
	
	data->first_unwritten_index = end_index;
	
	write_clock_data ();
	
#if DEBUG_STATISTICAL_PROFILER
	printf ("[write_statistical_data_block] Starting loop at index %d\n", start_index);
#endif
	
	for (index = start_index; index < end_index; index ++) {
		int base_index = index * (call_chain_depth + 1);
		gpointer address = data->addresses [base_index];
		int callers_count;
		
		regions_refreshed = write_statistical_hit (domain, address, regions_refreshed);
		base_index ++;
		
		for (callers_count = 0; callers_count < call_chain_depth; callers_count ++) {
			address = data->addresses [base_index + callers_count];
			if (address == NULL) {
				break;
			}
		}
		
		if (callers_count > 0) {
			write_uint32 ((callers_count << 3) | MONO_PROFILER_STATISTICAL_CODE_CALL_CHAIN);
			
			for (callers_count = 0; callers_count < call_chain_depth; callers_count ++) {
				address = data->addresses [base_index + callers_count];
				if (address != NULL) {
					regions_refreshed = write_statistical_hit (domain, address, regions_refreshed);
				} else {
					break;
				}
			}
		}
	}
	write_uint32 (MONO_PROFILER_STATISTICAL_CODE_END);
	
#if DEBUG_STATISTICAL_PROFILER
	printf ("[write_statistical_data_block] Ending loop at index %d\n", end_index);
#endif
	write_clock_data ();
	
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_STATISTICAL);
}

static void
write_intro_block (void) {
	write_uint32 (1);
	write_string ("mono");
	write_uint32 (profiler->flags);
	write_uint64 (profiler->start_counter);
	write_uint64 (profiler->start_time);
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_INTRO);
}

static void
write_end_block (void) {
	write_uint32 (1);
	write_uint64 (profiler->end_counter);
	write_uint64 (profiler->end_time);
	write_current_block (MONO_PROFILER_FILE_BLOCK_KIND_END);
}

static void
update_mapping (ProfilerPerThreadData *data) {
	ProfilerEventData *start = data->first_unmapped_event;
	ProfilerEventData *end = data->next_free_event;
	data->first_unmapped_event = end;
	
#if (DEBUG_LOGGING_PROFILER)
	printf ("[update_mapping][TID %ld] START\n", data->thread_id);
#endif
	while (start < end) {
#if DEBUG_LOGGING_PROFILER
		printf ("Examining event %p[TID %ld] looking for a new mapping...\n", start, data->thread_id);
#endif
		if (start->data_type == MONO_PROFILER_EVENT_DATA_TYPE_CLASS) {
			ClassIdMappingElement *element = class_id_mapping_element_get (start->data.address);
			if (element == NULL) {
				MonoClass *klass = start->data.address;
				class_id_mapping_element_new (klass);
			}
		} else if (start->data_type == MONO_PROFILER_EVENT_DATA_TYPE_METHOD) {
			MethodIdMappingElement *element = method_id_mapping_element_get (start->data.address);
			if (element == NULL) {
				MonoMethod *method = start->data.address;
				method_id_mapping_element_new (method);
			}
		}
		
		if (start->value == MAX_EVENT_VALUE) {
			start ++;
		}
		start ++;
	}
#if (DEBUG_LOGGING_PROFILER)
	printf ("[update_mapping][TID %ld] END\n", data->thread_id);
#endif
}

static void
flush_all_mappings (void) {
	ProfilerPerThreadData *data;
	
	for (data = profiler->per_thread_data; data != NULL; data = data->next) {
		update_mapping (data);
	}
	for (data = profiler->per_thread_data; data != NULL; data = data->next) {
		write_mapping_block (data->thread_id);
	}
}

static void
flush_full_event_data_buffer (ProfilerPerThreadData *data) {
	LOCK_PROFILER ();
	
	// We flush all mappings because some id definitions could come
	// from other threads
	flush_all_mappings ();
	g_assert (data->first_unmapped_event >= data->end_event);
	
	write_thread_data_block (data);
	
	data->next_free_event = data->events;
	data->first_unwritten_event = data->events;
	data->first_unmapped_event = data->events;
	MONO_PROFILER_GET_CURRENT_COUNTER (data->start_event_counter);
	data->last_event_counter = data->start_event_counter;
	
	UNLOCK_PROFILER ();
}

#define GET_NEXT_FREE_EVENT(d,e) {\
	if ((d)->next_free_event >= (d)->end_event) {\
		flush_full_event_data_buffer (d);\
	}\
	(e) = (d)->next_free_event;\
	(d)->next_free_event ++;\
} while (0)

static void
flush_everything (void) {
	ProfilerPerThreadData *data;
	
	flush_all_mappings ();
	for (data = profiler->per_thread_data; data != NULL; data = data->next) {
		write_thread_data_block (data);
	}
	write_statistical_data_block (profiler->statistical_data);
}

#define RESULT_TO_LOAD_CODE(r) (((r)==MONO_PROFILE_OK)?MONO_PROFILER_LOADED_EVENT_SUCCESS:MONO_PROFILER_LOADED_EVENT_FAILURE)
static void
appdomain_start_load (MonoProfiler *profiler, MonoDomain *domain) {
	LOCK_PROFILER ();
	loaded_element_load_start (profiler->loaded_appdomains, domain);
	UNLOCK_PROFILER ();
}

static void
appdomain_end_load (MonoProfiler *profiler, MonoDomain *domain, int result) {
	char *name;
	LoadedElement *element;
	
	name = g_strdup_printf ("%d", mono_domain_get_id (domain));
	LOCK_PROFILER ();
	element = loaded_element_load_end (profiler->loaded_appdomains, domain, name);
	write_element_load_block (element, MONO_PROFILER_LOADED_EVENT_APPDOMAIN | RESULT_TO_LOAD_CODE (result), CURRENT_THREAD_ID ());
	UNLOCK_PROFILER ();
}

static void
appdomain_start_unload (MonoProfiler *profiler, MonoDomain *domain) {
	LOCK_PROFILER ();
	loaded_element_unload_start (profiler->loaded_appdomains, domain);
	flush_everything ();
	UNLOCK_PROFILER ();
}

static void
appdomain_end_unload (MonoProfiler *profiler, MonoDomain *domain) {
	LoadedElement *element;
	
	LOCK_PROFILER ();
	element = loaded_element_unload_end (profiler->loaded_appdomains, domain);
	write_element_unload_block (element, MONO_PROFILER_LOADED_EVENT_APPDOMAIN, CURRENT_THREAD_ID ());
	UNLOCK_PROFILER ();
}

static void
module_start_load (MonoProfiler *profiler, MonoImage *module) {
	LOCK_PROFILER ();
	loaded_element_load_start (profiler->loaded_modules, module);
	UNLOCK_PROFILER ();
}

static void
module_end_load (MonoProfiler *profiler, MonoImage *module, int result) {
	char *name;
	MonoAssemblyName aname;
	LoadedElement *element;
	
	mono_assembly_fill_assembly_name (module, &aname);
	name = mono_stringify_assembly_name (&aname);
	LOCK_PROFILER ();
	element = loaded_element_load_end (profiler->loaded_modules, module, name);
	write_element_load_block (element, MONO_PROFILER_LOADED_EVENT_MODULE | RESULT_TO_LOAD_CODE (result), CURRENT_THREAD_ID ());
	UNLOCK_PROFILER ();
}

static void
module_start_unload (MonoProfiler *profiler, MonoImage *module) {
	LOCK_PROFILER ();
	loaded_element_unload_start (profiler->loaded_modules, module);
	flush_everything ();
	UNLOCK_PROFILER ();
}

static void
module_end_unload (MonoProfiler *profiler, MonoImage *module) {
	LoadedElement *element;
	
	LOCK_PROFILER ();
	element = loaded_element_unload_end (profiler->loaded_modules, module);
	write_element_unload_block (element, MONO_PROFILER_LOADED_EVENT_MODULE, CURRENT_THREAD_ID ());
	UNLOCK_PROFILER ();
}

static void
assembly_start_load (MonoProfiler *profiler, MonoAssembly *assembly) {
	LOCK_PROFILER ();
	loaded_element_load_start (profiler->loaded_assemblies, assembly);
	UNLOCK_PROFILER ();
}

static void
assembly_end_load (MonoProfiler *profiler, MonoAssembly *assembly, int result) {
	char *name;
	MonoAssemblyName aname;
	LoadedElement *element;
	
	mono_assembly_fill_assembly_name (mono_assembly_get_image (assembly), &aname);
	name = mono_stringify_assembly_name (&aname);
	LOCK_PROFILER ();
	element = loaded_element_load_end (profiler->loaded_assemblies, assembly, name);
	write_element_load_block (element, MONO_PROFILER_LOADED_EVENT_ASSEMBLY | RESULT_TO_LOAD_CODE (result), CURRENT_THREAD_ID ());
	UNLOCK_PROFILER ();
}

static void
assembly_start_unload (MonoProfiler *profiler, MonoAssembly *assembly) {
	LOCK_PROFILER ();
	loaded_element_unload_start (profiler->loaded_assemblies, assembly);
	flush_everything ();
	UNLOCK_PROFILER ();
}
static void
assembly_end_unload (MonoProfiler *profiler, MonoAssembly *assembly) {
	LoadedElement *element;
	
	LOCK_PROFILER ();
	element = loaded_element_unload_end (profiler->loaded_assemblies, assembly);
	write_element_unload_block (element, MONO_PROFILER_LOADED_EVENT_ASSEMBLY, CURRENT_THREAD_ID ());
	UNLOCK_PROFILER ();
}

#if (DEBUG_LOGGING_PROFILER)		
static const char*
class_event_code_to_string (MonoProfilerClassEvents code) {
	switch (code) {
	case MONO_PROFILER_EVENT_CLASS_LOAD: return "LOAD";
	case MONO_PROFILER_EVENT_CLASS_UNLOAD: return "UNLOAD";
	case MONO_PROFILER_EVENT_CLASS_ALLOCATION: return "ALLOCATION";
	case MONO_PROFILER_EVENT_CLASS_EXCEPTION: return "EXCEPTION";
	default: g_assert_not_reached (); return "";
	}
}
static const char*
method_event_code_to_string (MonoProfilerClassEvents code) {
	switch (code) {
	case MONO_PROFILER_EVENT_METHOD_CALL: return "CALL";
	case MONO_PROFILER_EVENT_METHOD_JIT: return "JIT";
	case MONO_PROFILER_EVENT_METHOD_FREED: return "FREED";
	default: g_assert_not_reached (); return "";
	}
}
static const char*
number_event_code_to_string (MonoProfilerEvents code) {
	switch (code) {
	case MONO_PROFILER_EVENT_THREAD: return "THREAD";
	case MONO_PROFILER_EVENT_GC_COLLECTION: return "GC_COLLECTION";
	case MONO_PROFILER_EVENT_GC_MARK: return "GC_MARK";
	case MONO_PROFILER_EVENT_GC_SWEEP: return "GC_SWEEP";
	case MONO_PROFILER_EVENT_GC_RESIZE: return "GC_RESIZE";
	case MONO_PROFILER_EVENT_GC_STOP_WORLD: return "GC_STOP_WORLD";
	case MONO_PROFILER_EVENT_GC_START_WORLD: return "GC_START_WORLD";
	default: g_assert_not_reached (); return "";
	}
}
static const char*
event_result_to_string (MonoProfilerEventResult code) {
	switch (code) {
	case MONO_PROFILER_EVENT_RESULT_SUCCESS: return "SUCCESS";
	case MONO_PROFILER_EVENT_RESULT_FAILURE: return "FAILURE";
	default: g_assert_not_reached (); return "";
	}
}
static const char*
event_kind_to_string (MonoProfilerEventKind code) {
	switch (code) {
	case MONO_PROFILER_EVENT_KIND_START: return "START";
	case MONO_PROFILER_EVENT_KIND_END: return "END";
	default: g_assert_not_reached (); return "";
	}
}
static void
print_event_data (gsize thread_id, ProfilerEventData *event, guint64 value) {
	if (event->data_type == MONO_PROFILER_EVENT_DATA_TYPE_CLASS) {
		printf ("[TID %ld] CLASS[%p] event [%p] %s:%s:%s[%d-%d-%d] %ld (%s.%s)\n",
				thread_id,
				event->data.address,
				event,
				class_event_code_to_string (event->code & ~MONO_PROFILER_EVENT_RESULT_MASK),
				event_result_to_string (event->code & MONO_PROFILER_EVENT_RESULT_MASK),
				event_kind_to_string (event->kind),
				event->data_type,
				event->kind,
				event->code,
				value,
				mono_class_get_namespace ((MonoClass*) event->data.address),
				mono_class_get_name ((MonoClass*) event->data.address));
	} else if (event->data_type == MONO_PROFILER_EVENT_DATA_TYPE_METHOD) {
		printf ("[TID %ld] METHOD[%p] event [%p] %s:%s:%s[%d-%d-%d] %ld (%s.%s:%s (?))\n",
				thread_id,
				event->data.address,
				event,
				method_event_code_to_string (event->code & ~MONO_PROFILER_EVENT_RESULT_MASK),
				event_result_to_string (event->code & MONO_PROFILER_EVENT_RESULT_MASK),
				event_kind_to_string (event->kind),
				event->data_type,
				event->kind,
				event->code,
				value,
				mono_class_get_namespace (mono_method_get_class ((MonoMethod*) event->data.address)),
				mono_class_get_name (mono_method_get_class ((MonoMethod*) event->data.address)),
				mono_method_get_name ((MonoMethod*) event->data.address));
	} else {
		printf ("[TID %ld] NUMBER[%ld] event [%p] %s:%s[%d-%d-%d] %ld\n",
				thread_id,
				(guint64) event->data.number,
				event,
				number_event_code_to_string (event->code),
				event_kind_to_string (event->kind),
				event->data_type,
				event->kind,
				event->code,
				value);
	}
}
#define LOG_EVENT(tid,ev,val) print_event_data ((tid),(ev),(val))
#else
#define LOG_EVENT(tid,ev,val)
#endif

#define RESULT_TO_EVENT_CODE(r) (((r)==MONO_PROFILE_OK)?MONO_PROFILER_EVENT_RESULT_SUCCESS:MONO_PROFILER_EVENT_RESULT_FAILURE)

#define STORE_EVENT_ITEM_COUNTER(p,i,dt,c,k) do {\
	ProfilerPerThreadData *data;\
	ProfilerEventData *event;\
	guint64 counter;\
	guint64 delta;\
	GET_PROFILER_THREAD_DATA (data);\
	GET_NEXT_FREE_EVENT (data, event);\
	MONO_PROFILER_GET_CURRENT_COUNTER (counter);\
	event->data.address = (i);\
	event->data_type = (dt);\
	event->code = (c);\
	event->kind = (k);\
	delta = counter - data->last_event_counter;\
	if (delta < MAX_EVENT_VALUE) {\
		event->value = delta;\
	} else {\
		ProfilerEventData *extension = data->next_free_event;\
		data->next_free_event ++;\
		event->value = MAX_EVENT_VALUE;\
		*(guint64*)extension = delta;\
	}\
	data->last_event_counter = counter;\
	LOG_EVENT (data->thread_id, event, delta);\
} while (0);
#define STORE_EVENT_ITEM_VALUE(p,i,dt,c,k,v) do {\
	ProfilerPerThreadData *data;\
	ProfilerEventData *event;\
	GET_PROFILER_THREAD_DATA (data);\
	GET_NEXT_FREE_EVENT (data, event);\
	event->data.address = (i);\
	event->data_type = (dt);\
	event->code = (c);\
	event->kind = (k);\
	if ((v) < MAX_EVENT_VALUE) {\
		event->value = (v);\
	} else {\
		ProfilerEventData *extension = data->next_free_event;\
		data->next_free_event ++;\
		event->value = MAX_EVENT_VALUE;\
		*(guint64*)extension = (v);\
	}\
	LOG_EVENT (data->thread_id, event, (v));\
}while (0);
#define STORE_EVENT_NUMBER_COUNTER(p,n,dt,c,k) do {\
	ProfilerPerThreadData *data;\
	ProfilerEventData *event;\
	guint64 counter;\
	guint64 delta;\
	GET_PROFILER_THREAD_DATA (data);\
	GET_NEXT_FREE_EVENT (data, event);\
	MONO_PROFILER_GET_CURRENT_COUNTER (counter);\
	event->data.number = (n);\
	event->data_type = (dt);\
	event->code = (c);\
	event->kind = (k);\
	delta = counter - data->last_event_counter;\
	if (delta < MAX_EVENT_VALUE) {\
		event->value = delta;\
	} else {\
		ProfilerEventData *extension = data->next_free_event;\
		data->next_free_event ++;\
		event->value = MAX_EVENT_VALUE;\
		*(guint64*)extension = delta;\
	}\
	data->last_event_counter = counter;\
	LOG_EVENT (data->thread_id, event, delta);\
}while (0);
#define STORE_EVENT_NUMBER_VALUE(p,n,dt,c,k,v) do {\
	ProfilerPerThreadData *data;\
	ProfilerEventData *event;\
	GET_PROFILER_THREAD_DATA (data);\
	GET_NEXT_FREE_EVENT (data, event);\
	event->data.number = (n);\
	event->data_type = (dt);\
	event->code = (c);\
	event->kind = (k);\
	if ((v) < MAX_EVENT_VALUE) {\
		event->value = (v);\
	} else {\
		ProfilerEventData *extension = data->next_free_event;\
		data->next_free_event ++;\
		event->value = MAX_EVENT_VALUE;\
		*(guint64*)extension = (v);\
	}\
	LOG_EVENT (data->thread_id, event, (v));\
}while (0);


static void
class_start_load (MonoProfiler *profiler, MonoClass *klass) {
	STORE_EVENT_ITEM_COUNTER (profiler, klass, MONO_PROFILER_EVENT_DATA_TYPE_CLASS, MONO_PROFILER_EVENT_CLASS_LOAD, MONO_PROFILER_EVENT_KIND_START);
}
static void
class_end_load (MonoProfiler *profiler, MonoClass *klass, int result) {
	STORE_EVENT_ITEM_COUNTER (profiler, klass, MONO_PROFILER_EVENT_DATA_TYPE_CLASS, MONO_PROFILER_EVENT_CLASS_LOAD | RESULT_TO_EVENT_CODE (result), MONO_PROFILER_EVENT_KIND_END);
}
static void
class_start_unload (MonoProfiler *profiler, MonoClass *klass) {
	STORE_EVENT_ITEM_COUNTER (profiler, klass, MONO_PROFILER_EVENT_DATA_TYPE_CLASS, MONO_PROFILER_EVENT_CLASS_UNLOAD, MONO_PROFILER_EVENT_KIND_START);
}
static void
class_end_unload (MonoProfiler *profiler, MonoClass *klass) {
	STORE_EVENT_ITEM_COUNTER (profiler, klass, MONO_PROFILER_EVENT_DATA_TYPE_CLASS, MONO_PROFILER_EVENT_CLASS_UNLOAD, MONO_PROFILER_EVENT_KIND_END);
}

static void
method_start_jit (MonoProfiler *profiler, MonoMethod *method) {
	if (profiler->action_flags.jit_time) {
		STORE_EVENT_ITEM_COUNTER (profiler, method, MONO_PROFILER_EVENT_DATA_TYPE_METHOD, MONO_PROFILER_EVENT_METHOD_JIT, MONO_PROFILER_EVENT_KIND_START);
	}
}
static void
method_end_jit (MonoProfiler *profiler, MonoMethod *method, int result) {
	if (profiler->action_flags.jit_time) {
		STORE_EVENT_ITEM_COUNTER (profiler, method, MONO_PROFILER_EVENT_DATA_TYPE_METHOD, MONO_PROFILER_EVENT_METHOD_JIT | RESULT_TO_EVENT_CODE (result), MONO_PROFILER_EVENT_KIND_END);
	}
}

#if (HAS_OPROFILE)
static void
method_jit_result (MonoProfiler *prof, MonoMethod *method, MonoJitInfo* jinfo, int result) {
	if (profiler->action_flags.oprofile && (result == MONO_PROFILE_OK)) {
		MonoClass *klass = mono_method_get_class (method);
		char *signature = mono_signature_get_desc (mono_method_signature (method), TRUE);
		char *name = g_strdup_printf ("%s.%s:%s (%s)", mono_class_get_namespace (klass), mono_class_get_name (klass), mono_method_get_name (method), signature);
		gpointer code_start = mono_jit_info_get_code_start (jinfo);
		int code_size = mono_jit_info_get_code_size (jinfo);
		
		if (op_write_native_code (name, code_start, code_size)) {
			g_warning ("Problem calling op_write_native_code\n");
		}
		
		g_free (signature);
		g_free (name);
	}
}
#endif


static void
method_enter (MonoProfiler *profiler, MonoMethod *method) {
	STORE_EVENT_ITEM_COUNTER (profiler, method, MONO_PROFILER_EVENT_DATA_TYPE_METHOD, MONO_PROFILER_EVENT_METHOD_CALL, MONO_PROFILER_EVENT_KIND_START);
}
static void
method_leave (MonoProfiler *profiler, MonoMethod *method) {
	STORE_EVENT_ITEM_COUNTER (profiler, method, MONO_PROFILER_EVENT_DATA_TYPE_METHOD, MONO_PROFILER_EVENT_METHOD_CALL, MONO_PROFILER_EVENT_KIND_END);
}

static void
method_free (MonoProfiler *profiler, MonoMethod *method) {
	STORE_EVENT_ITEM_COUNTER (profiler, method, MONO_PROFILER_EVENT_DATA_TYPE_METHOD, MONO_PROFILER_EVENT_METHOD_FREED, 0);
}

static void
thread_start (MonoProfiler *profiler, gsize tid) {
	STORE_EVENT_NUMBER_COUNTER (profiler, tid, MONO_PROFILER_EVENT_DATA_TYPE_OTHER, MONO_PROFILER_EVENT_THREAD, MONO_PROFILER_EVENT_KIND_START);
}
static void
thread_end (MonoProfiler *profiler, gsize tid) {
	STORE_EVENT_NUMBER_COUNTER (profiler, tid, MONO_PROFILER_EVENT_DATA_TYPE_OTHER, MONO_PROFILER_EVENT_THREAD, MONO_PROFILER_EVENT_KIND_END);
}

static void
object_allocated (MonoProfiler *profiler, MonoObject *obj, MonoClass *klass) {
	ProfilerPerThreadData *thread_data;
	
	STORE_EVENT_ITEM_VALUE (profiler, klass, MONO_PROFILER_EVENT_DATA_TYPE_CLASS, MONO_PROFILER_EVENT_CLASS_ALLOCATION, 0, (guint64) mono_object_get_size (obj));
	if (profiler->action_flags.unreachable_objects || profiler->action_flags.heap_shot) {
		GET_PROFILER_THREAD_DATA (thread_data);
		STORE_ALLOCATED_OBJECT (thread_data, obj);
	}
}

static void
statistical_call_chain (MonoProfiler *profiler, int call_chain_depth, guchar **ips, void *context) {
	ProfilerStatisticalData *data;
	int index;
	
	do {
		data = profiler->statistical_data;
		index = InterlockedIncrement (&data->next_free_index);
		
		if (index <= data->end_index) {
			int base_index = (index - 1) * (profiler->statistical_call_chain_depth + 1);
			int call_chain_index = 0;
			
			//printf ("[statistical_call_chain] (%d)\n", call_chain_depth);
			while (call_chain_index < call_chain_depth) {
				//printf ("[statistical_call_chain] [%d] = %p\n", base_index + call_chain_index, ips [call_chain_index]);
				data->addresses [base_index + call_chain_index] = (gpointer) ips [call_chain_index];
				call_chain_index ++;
			}
			while (call_chain_index <= profiler->statistical_call_chain_depth) {
				//printf ("[statistical_call_chain] [%d] = NULL\n", base_index + call_chain_index);
				data->addresses [base_index + call_chain_index] = NULL;
				call_chain_index ++;
			}
		} else {
			/* Check if we are the one that must swap the buffers */
			if (index == data->end_index + 1) {
				ProfilerStatisticalData *new_data;

				/* In the *impossible* case that the writer thread has not finished yet, */
				/* loop waiting for it and meanwhile lose all statistical events... */
				do {
					/* First, wait that it consumed the ready buffer */
					while (profiler->statistical_data_ready != NULL);
					/* Then, wait that it produced the free buffer */
					new_data = profiler->statistical_data_second_buffer;
				} while (new_data == NULL);

				profiler->statistical_data_ready = data;
				profiler->statistical_data = new_data;
				profiler->statistical_data_second_buffer = NULL;
				WRITER_EVENT_RAISE ();
			}
			
			/* Loop again, hoping to acquire a free slot this time */
			data = NULL;
		}
	} while (data == NULL);
}

static void
statistical_hit (MonoProfiler *profiler, guchar *ip, void *context) {
	ProfilerStatisticalData *data;
	int index;
	
	do {
		data = profiler->statistical_data;
		index = InterlockedIncrement (&data->next_free_index);
		
		if (index <= data->end_index) {
			data->addresses [index - 1] = (gpointer) ip;
		} else {
			/* Check if we are the one that must swap the buffers */
			if (index == data->end_index + 1) {
				ProfilerStatisticalData *new_data;

				/* In the *impossible* case that the writer thread has not finished yet, */
				/* loop waiting for it and meanwhile lose all statistical events... */
				do {
					/* First, wait that it consumed the ready buffer */
					while (profiler->statistical_data_ready != NULL);
					/* Then, wait that it produced the free buffer */
					new_data = profiler->statistical_data_second_buffer;
				} while (new_data == NULL);

				profiler->statistical_data_ready = data;
				profiler->statistical_data = new_data;
				profiler->statistical_data_second_buffer = NULL;
				WRITER_EVENT_RAISE ();
			}
			
			/* Loop again, hoping to acquire a free slot this time */
			data = NULL;
		}
	} while (data == NULL);
}

static MonoProfilerEvents
gc_event_code_from_profiler_event (MonoGCEvent event) {
	switch (event) {
	case MONO_GC_EVENT_START:
	case MONO_GC_EVENT_END:
		return MONO_PROFILER_EVENT_GC_COLLECTION;
	case MONO_GC_EVENT_MARK_START:
	case MONO_GC_EVENT_MARK_END:
		return MONO_PROFILER_EVENT_GC_MARK;
	case MONO_GC_EVENT_RECLAIM_START:
	case MONO_GC_EVENT_RECLAIM_END:
		return MONO_PROFILER_EVENT_GC_SWEEP;
	case MONO_GC_EVENT_PRE_STOP_WORLD:
	case MONO_GC_EVENT_POST_STOP_WORLD:
		return MONO_PROFILER_EVENT_GC_STOP_WORLD;
	case MONO_GC_EVENT_PRE_START_WORLD:
	case MONO_GC_EVENT_POST_START_WORLD:
		return MONO_PROFILER_EVENT_GC_START_WORLD;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

static MonoProfilerEventKind
gc_event_kind_from_profiler_event (MonoGCEvent event) {
	switch (event) {
	case MONO_GC_EVENT_START:
	case MONO_GC_EVENT_MARK_START:
	case MONO_GC_EVENT_RECLAIM_START:
	case MONO_GC_EVENT_PRE_STOP_WORLD:
	case MONO_GC_EVENT_PRE_START_WORLD:
		return MONO_PROFILER_EVENT_KIND_START;
	case MONO_GC_EVENT_END:
	case MONO_GC_EVENT_MARK_END:
	case MONO_GC_EVENT_RECLAIM_END:
	case MONO_GC_EVENT_POST_START_WORLD:
	case MONO_GC_EVENT_POST_STOP_WORLD:
		return MONO_PROFILER_EVENT_KIND_END;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

#define HEAP_SHOT_COMMAND_FILE_MAX_LENGTH 64
static void
profiler_heap_shot_process_command_file (void) {
	//FIXME: Port to Windows as well
	struct stat stat_buf;
	int fd;
	char buffer [HEAP_SHOT_COMMAND_FILE_MAX_LENGTH + 1];
	
	if (profiler->heap_shot_command_file_name == NULL)
		return;
	if (stat (profiler->heap_shot_command_file_name, &stat_buf) != 0)
		return;
	if (stat_buf.st_size > HEAP_SHOT_COMMAND_FILE_MAX_LENGTH)
		return;
	if ((stat_buf.st_mtim.tv_sec * 1000000) < profiler->heap_shot_command_file_access_time)
		return;
	
	fd = open (profiler->heap_shot_command_file_name, O_RDONLY);
	if (fd < 0) {
		return;
	} else {
		if (read (fd, &(buffer [0]), stat_buf.st_size) != stat_buf.st_size) {
			return;
		} else {
			buffer [stat_buf.st_size] = 0;
			profiler->dump_next_heap_snapshots = atoi (buffer);
			MONO_PROFILER_GET_CURRENT_TIME (profiler->heap_shot_command_file_access_time);
		}
		close (fd);
	}
}

static gboolean
dump_current_heap_snapshot (void) {
	gboolean result;
	
	if (profiler->heap_shot_was_signalled) {
		result = TRUE;
	} else {
		profiler_heap_shot_process_command_file ();
		if (profiler->dump_next_heap_snapshots > 0) {
			profiler->dump_next_heap_snapshots--;
			result = TRUE;
		} else if (profiler->dump_next_heap_snapshots < 0) {
			result = TRUE;
		} else {
			result = FALSE;
		}
	}
	
	return result;
}

static void
profiler_heap_buffers_setup (ProfilerHeapShotHeapBuffers *heap) {
	heap->buffers = g_new (ProfilerHeapShotHeapBuffer, 1);
	heap->buffers->previous = NULL;
	heap->buffers->next = NULL;
	heap->buffers->start_slot = &(heap->buffers->buffer [0]);
	heap->buffers->end_slot = &(heap->buffers->buffer [PROFILER_HEAP_SHOT_HEAP_BUFFER_SIZE]);
	heap->last = heap->buffers;
	heap->current = heap->buffers;
	heap->first_free_slot = & (heap->buffers->buffer [0]);
}
static void
profiler_heap_buffers_clear (ProfilerHeapShotHeapBuffers *heap) {
	heap->buffers = NULL;
	heap->last = NULL;
	heap->current = NULL;
	heap->first_free_slot = NULL;
}
static void
profiler_heap_buffers_free (ProfilerHeapShotHeapBuffers *heap) {
	ProfilerHeapShotHeapBuffer *current = heap->buffers;
	while (current != NULL) {
		ProfilerHeapShotHeapBuffer *next = current->next;
		g_free (current);
		current = next;
	}
	profiler_heap_buffers_clear (heap);
}

static int
report_object_references (gpointer *start, ClassIdMappingElement *layout, ProfilerHeapShotWriteJob *job) {
	int reported_references = 0;
	int slot;
	
	for (slot = 0; slot < layout->data.layout.slots; slot ++) {
		gboolean slot_has_reference;
		if (layout->data.layout.slots <= CLASS_LAYOUT_PACKED_BITMAP_SIZE) {
			if (layout->data.bitmap.compact & (((guint64)1) << slot)) {
				slot_has_reference = TRUE;
			} else {
				slot_has_reference = FALSE;
			}
		} else {
			if (layout->data.bitmap.extended [slot >> 3] & (1 << (slot & 7))) {
				slot_has_reference = TRUE;
			} else {
				slot_has_reference = FALSE;
			}
		}
		
		if (slot_has_reference) {
			gpointer field = start [slot];
			
			if ((field != NULL) && mono_object_is_alive (field)) {
				reported_references ++;
				WRITE_HEAP_SHOT_JOB_VALUE (job, field);
			}
		}
	}
	
	return reported_references;
}

static void
profiler_heap_report_object_reachable (ProfilerHeapShotWriteJob *job, MonoObject *obj) {
	if (profiler->action_flags.heap_shot && (job != NULL)) {
		MonoClass *klass = mono_object_get_class (obj);
		int reference_counter = 0;
		gpointer *reference_counter_location;
		
		WRITE_HEAP_SHOT_JOB_VALUE_WITH_CODE (job, obj, HEAP_CODE_OBJECT);
#if DEBUG_HEAP_PROFILER
		printf ("profiler_heap_report_object_reachable: reported object %p at cursor %p\n", obj, (job->cursor - 1));
#endif
		WRITE_HEAP_SHOT_JOB_VALUE (job, NULL);
		reference_counter_location = job->cursor - 1;
		
		if (mono_class_get_rank (klass)) {
			MonoArray *array = (MonoArray *) obj;
			MonoClass *element_class = mono_class_get_element_class (klass);
			ClassIdMappingElement *element_id = class_id_mapping_element_get (element_class);
			
			g_assert (element_id != NULL);
			if (element_id->data.layout.slots == CLASS_LAYOUT_NOT_INITIALIZED) {
				class_id_mapping_element_build_layout_bitmap (element_class, element_id);
			}
			if (! mono_class_is_valuetype (element_class)) {
				int length = mono_array_length (array);
				int i;
				for (i = 0; i < length; i++) {
					MonoObject *array_element = mono_array_get (array, MonoObject*, i);
					if ((array_element != NULL) && mono_object_is_alive (array_element)) {
						reference_counter ++;
						WRITE_HEAP_SHOT_JOB_VALUE (job, array_element);
					}
				}
			} else if (element_id->data.layout.references > 0) {
				int length = mono_array_length (array);
				int array_element_size = mono_array_element_size (klass);
				int i;
				for (i = 0; i < length; i++) {
					gpointer array_element_address = mono_array_addr_with_size (array, array_element_size, i);
					reference_counter += report_object_references (array_element_address, element_id, job);
				}
			}
		} else {
			ClassIdMappingElement *class_id = class_id_mapping_element_get (klass);
			if (class_id == NULL) {
				printf ("profiler_heap_report_object_reachable: class %p (%s.%s) has no id\n", klass, mono_class_get_namespace (klass), mono_class_get_name (klass));
			}
			g_assert (class_id != NULL);
			if (class_id->data.layout.slots == CLASS_LAYOUT_NOT_INITIALIZED) {
				class_id_mapping_element_build_layout_bitmap (klass, class_id);
			}
			if (class_id->data.layout.references > 0) {
				reference_counter += report_object_references ((gpointer)(((char*)obj) + sizeof (MonoObject)), class_id, job);
			}
		}
		
		*reference_counter_location = GINT_TO_POINTER (reference_counter);
#if DEBUG_HEAP_PROFILER
		printf ("profiler_heap_report_object_reachable: updated reference_counter_location %p with value %d\n", reference_counter_location, reference_counter);
#endif
	}
}
static void
profiler_heap_report_object_unreachable (ProfilerHeapShotWriteJob *job, MonoObject *obj) {
	if (job != NULL) {
		MonoClass *klass = mono_object_get_class (obj);
		guint32 size = mono_object_get_size (obj);
		
#if DEBUG_HEAP_PROFILER
		printf ("profiler_heap_report_object_unreachable: at job %p writing klass %p\n", job, klass);
#endif
		WRITE_HEAP_SHOT_JOB_VALUE_WITH_CODE (job, klass, HEAP_CODE_FREE_OBJECT_CLASS);
	
#if DEBUG_HEAP_PROFILER
		printf ("profiler_heap_report_object_unreachable: at job %p writing size %p\n", job, GUINT_TO_POINTER (size));
#endif
		WRITE_HEAP_SHOT_JOB_VALUE (job, GUINT_TO_POINTER (size));
	}
}

static void
profiler_heap_add_object (ProfilerHeapShotHeapBuffers *heap, ProfilerHeapShotWriteJob *job, MonoObject *obj) {
	if (heap->first_free_slot >= heap->current->end_slot) {
		if (heap->current->next != NULL) {
			heap->current = heap->current->next;
		} else {
			ProfilerHeapShotHeapBuffer *buffer = g_new (ProfilerHeapShotHeapBuffer, 1);
			buffer->previous = heap->last;
			buffer->next = NULL;
			buffer->start_slot = &(buffer->buffer [0]);
			buffer->end_slot = &(buffer->buffer [PROFILER_HEAP_SHOT_HEAP_BUFFER_SIZE]);
			heap->current = buffer;
			heap->last->next = buffer;
			heap->last = buffer;
		}
		heap->first_free_slot = &(heap->current->buffer [0]);
	}
	
	*(heap->first_free_slot) = obj;
	heap->first_free_slot ++;
	profiler_heap_report_object_reachable (job, obj);
}

static MonoObject*
profiler_heap_pop_object_from_end (ProfilerHeapShotHeapBuffers *heap, ProfilerHeapShotWriteJob *job, MonoObject** current_slot) {
	while (heap->first_free_slot != current_slot) {
		MonoObject* obj;
		
		if (heap->first_free_slot > heap->current->start_slot) {
			heap->first_free_slot --;
		} else {
			heap->current = heap->current->previous;
			g_assert (heap->current != NULL);
			heap->first_free_slot = heap->current->end_slot - 1;
		}
		
		obj = *(heap->first_free_slot);
		
		if (mono_object_is_alive (obj)) {
			profiler_heap_report_object_reachable (job, obj);
			return obj;
		} else {
			profiler_heap_report_object_unreachable (job, obj);
		}
	}
	return NULL;
}

static void
profiler_heap_scan (ProfilerHeapShotHeapBuffers *heap, ProfilerHeapShotWriteJob *job) {
	ProfilerHeapShotHeapBuffer *current_buffer = heap->buffers;
	MonoObject** current_slot = current_buffer->start_slot;
	
	while (current_slot != heap->first_free_slot) {
		MonoObject *obj = *current_slot;
		if (mono_object_is_alive (obj)) {
			profiler_heap_report_object_reachable (job, obj);
		} else {
			profiler_heap_report_object_unreachable (job, obj);
			*current_slot = profiler_heap_pop_object_from_end (heap, job, current_slot);
		}
		
		if (*current_slot != NULL) {
			current_slot ++;
			
			if (current_slot == current_buffer->end_slot) {
				current_buffer = current_buffer->next;
				g_assert (current_buffer != NULL);
				current_slot = current_buffer->start_slot;
			}
		}
	}
}

static void
handle_heap_profiling (MonoProfiler *profiler, MonoGCEvent ev) {
	static gboolean create_heap_shot_write_job;
	
	switch (ev) {
	case MONO_GC_EVENT_PRE_STOP_WORLD:
		// Get the lock, so we are sure nobody is flushing events during the collection,
		// and we can update all mappings (building the class descriptors).
		LOCK_PROFILER ();
		break;
	case MONO_GC_EVENT_POST_STOP_WORLD:
		create_heap_shot_write_job = dump_current_heap_snapshot ();
		if (create_heap_shot_write_job) {
			ProfilerPerThreadData *data;
			// Update all mappings, so that we have built all the class descriptors.
			flush_all_mappings ();
			// Also write all event buffers, so that allocations are recorded.
			for (data = profiler->per_thread_data; data != NULL; data = data->next) {
				write_thread_data_block (data);
			}
		}
		// Release lock...
		UNLOCK_PROFILER ();
		break;
	case MONO_GC_EVENT_MARK_END: {
		ProfilerHeapShotWriteJob *job;
		ProfilerPerThreadData *data;
		
		if (create_heap_shot_write_job) {
			job = profiler_heap_shot_write_job_new (profiler->heap_shot_was_signalled, profiler->garbage_collection_counter);
			profiler->heap_shot_was_signalled = FALSE;
			MONO_PROFILER_GET_CURRENT_COUNTER (job->start_counter);
			MONO_PROFILER_GET_CURRENT_TIME (job->start_time);
		} else {
			job = NULL;
		}
		
		profiler_heap_scan (&(profiler->heap), job);
		
		for (data = profiler->per_thread_data; data != NULL; data = data->next) {
			ProfilerHeapShotObjectBuffer *buffer;
			for (buffer = data->heap_shot_object_buffers; buffer != NULL; buffer = buffer->next) {
				MonoObject **cursor;
				for (cursor = buffer->first_unprocessed_slot; cursor < buffer->next_free_slot; cursor ++) {
					MonoObject *obj = *cursor;
#if DEBUG_HEAP_PROFILER
					printf ("gc_event: in object buffer %p(%p-%p) cursor at %p has object %p ", buffer, &(buffer->buffer [0]), buffer->end, cursor, obj);
#endif
					if (mono_object_is_alive (obj)) {
#if DEBUG_HEAP_PROFILER
						printf ("(object is alive, adding to heap)\n");
#endif
						profiler_heap_add_object (&(profiler->heap), job, obj);
					} else {
#if DEBUG_HEAP_PROFILER
						printf ("(object is unreachable, reporting in job)\n");
#endif
						profiler_heap_report_object_unreachable (job, obj);
					}
				}
				buffer->first_unprocessed_slot = cursor;
			}
		}
		
		if (job != NULL) {
			MONO_PROFILER_GET_CURRENT_COUNTER (job->end_counter);
			MONO_PROFILER_GET_CURRENT_TIME (job->end_time);
			
			profiler_add_heap_shot_write_job (job);
			profiler_free_heap_shot_write_jobs ();
			WRITER_EVENT_RAISE ();
		}
		break;
	}
	default:
		break;
	}
}

static void
gc_event (MonoProfiler *profiler, MonoGCEvent ev, int generation) {
	gboolean do_heap_profiling = profiler->action_flags.unreachable_objects || profiler->action_flags.heap_shot;
	guint32 event_value;
	
	if (ev == MONO_GC_EVENT_START) {
		profiler->garbage_collection_counter ++;
	}
	
	event_value = (profiler->garbage_collection_counter << 8) | generation;
	
	if (do_heap_profiling && (ev == MONO_GC_EVENT_POST_STOP_WORLD)) {
		handle_heap_profiling (profiler, ev);
	}
	STORE_EVENT_NUMBER_COUNTER (profiler, event_value, MONO_PROFILER_EVENT_DATA_TYPE_OTHER, gc_event_code_from_profiler_event (ev), gc_event_kind_from_profiler_event (ev));
	if (do_heap_profiling && (ev != MONO_GC_EVENT_POST_STOP_WORLD)) {
		handle_heap_profiling (profiler, ev);
	}
}

static void
gc_resize (MonoProfiler *profiler, gint64 new_size) {
	profiler->garbage_collection_counter ++;
	STORE_EVENT_NUMBER_VALUE (profiler, new_size, MONO_PROFILER_EVENT_DATA_TYPE_OTHER, MONO_PROFILER_EVENT_GC_RESIZE, 0, profiler->garbage_collection_counter);
}

/* called at the end of the program */
static void
profiler_shutdown (MonoProfiler *prof)
{
	ProfilerPerThreadData* current_thread_data;
	
	LOG_WRITER_THREAD ("profiler_shutdown: zeroing relevant flags");
	mono_profiler_set_events (0);
	//profiler->flags = 0;
	//profiler->action_flags.unreachable_objects = FALSE;
	//profiler->action_flags.heap_shot = FALSE;
	
	LOG_WRITER_THREAD ("profiler_shutdown: asking stats thread to exit");
	profiler->terminate_writer_thread = TRUE;
	WRITER_EVENT_RAISE ();
	LOG_WRITER_THREAD ("profiler_shutdown: waiting for stats thread to exit");
	WAIT_WRITER_THREAD ();
	LOG_WRITER_THREAD ("profiler_shutdown: stats thread should be dead now");
	WRITER_EVENT_DESTROY ();
	
	LOCK_PROFILER ();
	
	MONO_PROFILER_GET_CURRENT_TIME (profiler->end_time);
	MONO_PROFILER_GET_CURRENT_COUNTER (profiler->end_counter);
	
	flush_everything ();
	write_end_block ();
	FLUSH_FILE ();
	CLOSE_FILE();
	UNLOCK_PROFILER ();
	g_free (profiler->file_name);
	if (profiler->file_name_suffix != NULL) {
		g_free (profiler->file_name_suffix);
	}
	
	method_id_mapping_destroy (profiler->methods);
	class_id_mapping_destroy (profiler->classes);
	g_hash_table_destroy (profiler->loaded_assemblies);
	g_hash_table_destroy (profiler->loaded_modules);
	g_hash_table_destroy (profiler->loaded_appdomains);
	
	FREE_PROFILER_THREAD_DATA ();
	
	for (current_thread_data = profiler->per_thread_data; current_thread_data != NULL; current_thread_data = current_thread_data->next) {
		profiler_per_thread_data_destroy (current_thread_data);
	}
	if (profiler->statistical_data != NULL) {
		profiler_statistical_data_destroy (profiler->statistical_data);
	}
	if (profiler->statistical_data_ready != NULL) {
		profiler_statistical_data_destroy (profiler->statistical_data_ready);
	}
	if (profiler->statistical_data_second_buffer != NULL) {
		profiler_statistical_data_destroy (profiler->statistical_data_second_buffer);
	}
	if (profiler->executable_regions != NULL) {
		profiler_executable_memory_regions_destroy (profiler->executable_regions);
	}
	
	profiler_heap_buffers_free (&(profiler->heap));
	if (profiler->heap_shot_command_file_name != NULL) {
		g_free (profiler->heap_shot_command_file_name);
	}
	
	profiler_free_write_buffers ();
	profiler_destroy_heap_shot_write_jobs ();
	
	DELETE_PROFILER_MUTEX ();
	
#if (HAS_OPROFILE)
	if (profiler->action_flags.oprofile) {
		op_close_agent ();
	}
#endif
	
	g_free (profiler);
	profiler = NULL;
}

#define DEFAULT_ARGUMENTS "s"
static void
setup_user_options (const char *arguments) {
	gchar **arguments_array, **current_argument;
#ifndef PLATFORM_WIN32
	int gc_request_signal_number = 0;
#endif
	detect_fast_timer ();
	
	profiler->file_name = NULL;
	profiler->file_name_suffix = NULL;
	profiler->per_thread_buffer_size = 10000;
	profiler->statistical_buffer_size = 10000;
	profiler->statistical_call_chain_depth = 0;
	profiler->write_buffer_size = 1024;
	profiler->heap_shot_command_file_name = NULL;
	profiler->dump_next_heap_snapshots = 0;
	profiler->heap_shot_command_file_access_time = 0;
	profiler->heap_shot_was_signalled = FALSE;
	profiler->flags = MONO_PROFILE_APPDOMAIN_EVENTS|
			MONO_PROFILE_ASSEMBLY_EVENTS|
			MONO_PROFILE_MODULE_EVENTS|
			MONO_PROFILE_CLASS_EVENTS|
			MONO_PROFILE_METHOD_EVENTS;
	
	if (arguments == NULL) {
		arguments = DEFAULT_ARGUMENTS;
	} else if (strstr (arguments, ":")) {
		arguments = strstr (arguments, ":") + 1;
		if (arguments [0] == 0) {
			arguments = DEFAULT_ARGUMENTS;
		}
	}
	
	arguments_array = g_strsplit (arguments, ",", -1);
	
	for (current_argument = arguments_array; ((current_argument != NULL) && (current_argument [0] != 0)); current_argument ++) {
		char *argument = *current_argument;
		char *equals = strstr (argument, "=");
		
		if (equals != NULL) {
			int equals_position = equals - argument;
			
			if (! (strncmp (argument, "per-thread-buffer-size", equals_position) && strncmp (argument, "tbs", equals_position))) {
				int value = atoi (equals + 1);
				if (value > 0) {
					profiler->per_thread_buffer_size = value;
				}
			} else if (! (strncmp (argument, "statistical", equals_position) && strncmp (argument, "stat", equals_position) && strncmp (argument, "s", equals_position))) {
				int value = atoi (equals + 1);
				if (value > 0) {
					if (value > 16) {
						value = 16;
					}
					profiler->statistical_call_chain_depth = value;
					profiler->flags |= MONO_PROFILE_STATISTICAL|MONO_PROFILE_JIT_COMPILATION;
					profiler->action_flags.jit_time = TRUE;
				}
			} else if (! (strncmp (argument, "statistical-thread-buffer-size", equals_position) && strncmp (argument, "sbs", equals_position))) {
				int value = atoi (equals + 1);
				if (value > 0) {
					profiler->statistical_buffer_size = value;
				}
			} else if (! (strncmp (argument, "write-buffer-size", equals_position) && strncmp (argument, "wbs", equals_position))) {
				int value = atoi (equals + 1);
				if (value > 0) {
					profiler->write_buffer_size = value;
				}
			} else if (! (strncmp (argument, "output", equals_position) && strncmp (argument, "out", equals_position) && strncmp (argument, "o", equals_position) && strncmp (argument, "O", equals_position))) {
				if (strlen (equals + 1) > 0) {
					profiler->file_name = g_strdup (equals + 1);
				}
			} else if (! (strncmp (argument, "output-suffix", equals_position) && strncmp (argument, "suffix", equals_position) && strncmp (argument, "os", equals_position) && strncmp (argument, "OS", equals_position))) {
				if (strlen (equals + 1) > 0) {
					profiler->file_name_suffix = g_strdup (equals + 1);
				}
			} else if (! (strncmp (argument, "gc-commands", equals_position) && strncmp (argument, "gc-c", equals_position) && strncmp (argument, "gcc", equals_position))) {
				if (strlen (equals + 1) > 0) {
					profiler->heap_shot_command_file_name = g_strdup (equals + 1);
				}
			} else if (! (strncmp (argument, "gc-dumps", equals_position) && strncmp (argument, "gc-d", equals_position) && strncmp (argument, "gcd", equals_position))) {
				if (strlen (equals + 1) > 0) {
					profiler->dump_next_heap_snapshots = atoi (equals + 1);
				}
#ifndef PLATFORM_WIN32
			} else if (! (strncmp (argument, "gc-signal", equals_position) && strncmp (argument, "gc-s", equals_position) && strncmp (argument, "gcs", equals_position))) {
				if (strlen (equals + 1) > 0) {
					char *signal_name = equals + 1;
					if (! strcasecmp (signal_name, "SIGUSR1")) {
						gc_request_signal_number = SIGUSR1;
					} else if (! strcasecmp (signal_name, "SIGUSR2")) {
						gc_request_signal_number = SIGUSR2;
					} else if (! strcasecmp (signal_name, "SIGPROF")) {
						gc_request_signal_number = SIGPROF;
					} else {
						gc_request_signal_number = atoi (signal_name);
					}
				}
#endif
			} else {
				g_warning ("Cannot parse valued argument %s\n", argument);
			}
		} else {
			if (! (strcmp (argument, "jit") && strcmp (argument, "j"))) {
				profiler->flags |= MONO_PROFILE_JIT_COMPILATION;
				profiler->action_flags.jit_time = TRUE;
			} else if (! (strcmp (argument, "allocations") && strcmp (argument, "alloc") && strcmp (argument, "a"))) {
				profiler->flags |= MONO_PROFILE_ALLOCATIONS|MONO_PROFILE_GC;
			} else if (! (strcmp (argument, "gc") && strcmp (argument, "g"))) {
				profiler->flags |= MONO_PROFILE_GC;
			} else if (! (strcmp (argument, "heap-shot") && strcmp (argument, "heap") && strcmp (argument, "h"))) {
				profiler->flags |= MONO_PROFILE_ALLOCATIONS|MONO_PROFILE_GC;
				profiler->action_flags.unreachable_objects = TRUE;
				profiler->action_flags.heap_shot = TRUE;
			} else if (! (strcmp (argument, "unreachable") && strcmp (argument, "free") && strcmp (argument, "f"))) {
				profiler->flags |= MONO_PROFILE_ALLOCATIONS|MONO_PROFILE_GC;
				profiler->action_flags.unreachable_objects = TRUE;
			} else if (! (strcmp (argument, "threads") && strcmp (argument, "t"))) {
				profiler->flags |= MONO_PROFILE_THREADS;
			} else if (! (strcmp (argument, "enter-leave") && strcmp (argument, "calls") && strcmp (argument, "c"))) {
				profiler->flags |= MONO_PROFILE_ENTER_LEAVE;
			} else if (! (strcmp (argument, "statistical") && strcmp (argument, "stat") && strcmp (argument, "s"))) {
				profiler->flags |= MONO_PROFILE_STATISTICAL|MONO_PROFILE_JIT_COMPILATION;
				profiler->action_flags.jit_time = TRUE;
			} else if (! (strcmp (argument, "force-accurate-timer") && strcmp (argument, "fac"))) {
				use_fast_timer = FALSE;
#if (HAS_OPROFILE)
			} else if (! (strcmp (argument, "oprofile") && strcmp (argument, "oprof"))) {
				profiler->flags |= MONO_PROFILE_JIT_COMPILATION;
				profiler->action_flags.oprofile = TRUE;
				if (op_open_agent ()) {
					g_warning ("Problem calling op_open_agent\n");
				}
#endif
			} else if (strcmp (argument, "logging")) {
				g_warning ("Cannot parse flag argument %s\n", argument);
			}
		}
	}
	
	g_free (arguments_array);
	
#ifndef PLATFORM_WIN32
	if (gc_request_signal_number != 0) {
		if (((gc_request_signal_number == SIGPROF) && ! (profiler->flags & MONO_PROFILE_STATISTICAL)) ||
				(gc_request_signal_number == SIGUSR1) ||
				(gc_request_signal_number == SIGUSR2)) {
			add_gc_request_handler (gc_request_signal_number);
		} else {
			g_error ("Cannot use signal %d", gc_request_signal_number);
		}
	}
#endif
	
	if (profiler->file_name == NULL) {
		char *program_name = g_get_prgname ();
		
		if (program_name != NULL) {
			char *name_buffer = g_strdup (program_name);
			char *name_start = name_buffer;
			char *cursor;
			
			/* Jump over the last '/' */
			cursor = strrchr (name_buffer, '/');
			if (cursor == NULL) {
				cursor = name_buffer;
			} else {
				cursor ++;
			}
			name_start = cursor;
			
			/* Then jump over the last '\\' */
			cursor = strrchr (name_start, '\\');
			if (cursor == NULL) {
				cursor = name_start;
			} else {
				cursor ++;
			}
			name_start = cursor;
			
			/* Finally, find the last '.' */
			cursor = strrchr (name_start, '.');
			if (cursor != NULL) {
				*cursor = 0;
			}
			
			if (profiler->file_name_suffix == NULL) {
				profiler->file_name = g_strdup_printf ("%s.mprof", name_start);
			} else {
				profiler->file_name = g_strdup_printf ("%s-%s.mprof", name_start, profiler->file_name_suffix);
			}
			g_free (name_buffer);
		} else {
			profiler->file_name = g_strdup_printf ("%s.mprof", "profiler-log");
		}
	}
}

static gboolean
thread_detach_callback (MonoThread *thread) {
	LOG_WRITER_THREAD ("thread_detach_callback: asking writer thread to detach");
	profiler->detach_writer_thread = TRUE;
	WRITER_EVENT_RAISE ();
	LOG_WRITER_THREAD ("thread_detach_callback: done");
	return FALSE;
}

static guint32
data_writer_thread (gpointer nothing) {
	static gboolean thread_attached = FALSE;
	static gboolean thread_detached = FALSE;
	static MonoThread *this_thread = NULL;
	
	for (;;) {
		ProfilerStatisticalData *statistical_data;
		gboolean done;
		
		LOG_WRITER_THREAD ("data_writer_thread: going to sleep");
		WRITER_EVENT_WAIT ();
		LOG_WRITER_THREAD ("data_writer_thread: just woke up");
		
		if (! thread_attached) {
			if (! profiler->terminate_writer_thread) {
				MonoDomain * root_domain = mono_get_root_domain ();
				if (root_domain != NULL) {
					LOG_WRITER_THREAD ("data_writer_thread: attaching thread");
					this_thread = mono_thread_attach (root_domain);
					mono_thread_set_manage_callback (this_thread, thread_detach_callback);
					thread_attached = TRUE;
				} else {
					g_error ("Cannot get root domain\n");
				}
			} else {
				/* Execution was too short, pretend we attached and detached. */
				thread_attached = TRUE;
				thread_detached = TRUE;
			}
		}
		
		if (profiler->heap_shot_was_signalled) {
			LOG_WRITER_THREAD ("data_writer_thread: starting requested collection");
			mono_gc_collect (mono_gc_max_generation ());
			LOG_WRITER_THREAD ("data_writer_thread: requested collection done");
		}
		
		statistical_data = profiler->statistical_data_ready;
		done = (statistical_data == NULL) && (profiler->heap_shot_write_jobs == NULL);
		
		if (!done) {
			LOG_WRITER_THREAD ("data_writer_thread: acquiring lock and writing data");
			LOCK_PROFILER ();
			
			// This makes sure that all method ids are in place
			LOG_WRITER_THREAD ("data_writer_thread: writing mapping...");
			flush_all_mappings ();
			LOG_WRITER_THREAD ("data_writer_thread: wrote mapping");
			
			if ((statistical_data != NULL) && ! thread_detached) {
				LOG_WRITER_THREAD ("data_writer_thread: writing statistical data...");
				profiler->statistical_data_ready = NULL;
				write_statistical_data_block (statistical_data);
				statistical_data->next_free_index = 0;
				statistical_data->first_unwritten_index = 0;
				profiler->statistical_data_second_buffer = statistical_data;
				LOG_WRITER_THREAD ("data_writer_thread: wrote statistical data");
			}
			
			profiler_process_heap_shot_write_jobs ();
			
			UNLOCK_PROFILER ();
			LOG_WRITER_THREAD ("data_writer_thread: wrote data and released lock");
		}
		
		if (profiler->detach_writer_thread) {
			if (this_thread != NULL) {
				LOG_WRITER_THREAD ("data_writer_thread: detaching thread");
				mono_thread_detach (this_thread);
				this_thread = NULL;
				profiler->detach_writer_thread = FALSE;
				thread_detached = TRUE;
			} else {
				LOG_WRITER_THREAD ("data_writer_thread: warning: thread has already been detached");
			}
		}
		
		if (profiler->terminate_writer_thread) {
		LOG_WRITER_THREAD ("data_writer_thread: exiting thread");
			EXIT_THREAD ();
		}
	}
	return 0;
}

void
mono_profiler_startup (const char *desc);

/* the entry point (mono_profiler_load?) */
void
mono_profiler_startup (const char *desc)
{
	profiler = g_new0 (MonoProfiler, 1);
	
	setup_user_options ((desc != NULL) ? desc : "");
	
	INITIALIZE_PROFILER_MUTEX ();
	MONO_PROFILER_GET_CURRENT_TIME (profiler->start_time);
	MONO_PROFILER_GET_CURRENT_COUNTER (profiler->start_counter);
	profiler->last_header_counter = 0;
	
	profiler->methods = method_id_mapping_new ();
	profiler->classes = class_id_mapping_new ();
	profiler->loaded_assemblies = g_hash_table_new_full (g_direct_hash, NULL, NULL, loaded_element_destroy);
	profiler->loaded_modules = g_hash_table_new_full (g_direct_hash, NULL, NULL, loaded_element_destroy);
	profiler->loaded_appdomains = g_hash_table_new_full (g_direct_hash, NULL, NULL, loaded_element_destroy);
	
	profiler->statistical_data = profiler_statistical_data_new (profiler);
	profiler->statistical_data_second_buffer = profiler_statistical_data_new (profiler);
	
	profiler->write_buffers = g_malloc (sizeof (ProfilerFileWriteBuffer) + PROFILER_FILE_WRITE_BUFFER_SIZE);
	profiler->write_buffers->next = NULL;
	profiler->current_write_buffer = profiler->write_buffers;
	profiler->current_write_position = 0;
	profiler->full_write_buffers = 0;
	
	profiler->executable_regions = profiler_executable_memory_regions_new (1, 1);
	
	profiler->executable_files.table = g_hash_table_new (g_str_hash, g_str_equal); 
	profiler->executable_files.new_files = NULL; 
	
	profiler->heap_shot_write_jobs = NULL;
	if (profiler->action_flags.unreachable_objects || profiler->action_flags.heap_shot) {
		profiler_heap_buffers_setup (&(profiler->heap));
	} else {
		profiler_heap_buffers_clear (&(profiler->heap));
	}
	profiler->garbage_collection_counter = 0;
	
	WRITER_EVENT_INIT ();
	LOG_WRITER_THREAD ("mono_profiler_startup: creating writer thread");
	CREATE_WRITER_THREAD (data_writer_thread);
	LOG_WRITER_THREAD ("mono_profiler_startup: created writer thread");

	ALLOCATE_PROFILER_THREAD_DATA ();
	
	OPEN_FILE ();
	
	write_intro_block ();
	
	mono_profiler_install (profiler, profiler_shutdown);
	
	mono_profiler_install_appdomain (appdomain_start_load, appdomain_end_load,
			appdomain_start_unload, appdomain_end_unload);
	mono_profiler_install_assembly (assembly_start_load, assembly_end_load,
			assembly_start_unload, assembly_end_unload);
	mono_profiler_install_module (module_start_load, module_end_load,
			module_start_unload, module_end_unload);
	mono_profiler_install_class (class_start_load, class_end_load,
			class_start_unload, class_end_unload);
	mono_profiler_install_jit_compile (method_start_jit, method_end_jit);
	mono_profiler_install_enter_leave (method_enter, method_leave);
	mono_profiler_install_method_free (method_free);
	mono_profiler_install_thread (thread_start, thread_end);
	mono_profiler_install_allocation (object_allocated);
	mono_profiler_install_statistical (statistical_hit);
	mono_profiler_install_statistical_call_chain (statistical_call_chain, profiler->statistical_call_chain_depth);
	mono_profiler_install_gc (gc_event, gc_resize);
#if (HAS_OPROFILE)
	mono_profiler_install_jit_end (method_jit_result);
#endif
	
	mono_profiler_set_events (profiler->flags);
}

