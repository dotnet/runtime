#pragma once

//TODO use mono headers directly, so we don't get burned when the struct definitions in this file
//go out of sync with mono's.
//this is not done yet, because it's tricky, as the mono headers define symbols that we also define in UnityFunctions.h,
//so we'd need to find some way to either remove those defines from the mono headers, or somehow mangle them.
#if ENABLE_MONO

struct MonoException;
struct MonoAssembly;
struct MonoObject;
struct MonoClassField;
struct MonoClass;
struct MonoDomain;
struct MonoImage;
struct MonoType;
struct MonoMethodSignature;
struct MonoArray;
struct MonoThread;
struct MonoVTable;
struct MonoProperty;
struct MonoReflectionAssembly;
struct MonoReflectionMethod;
struct MonoReflectionField;
struct MonoAppDomain;
struct MonoCustomAttrInfo;
struct MonoDl;
struct MonoManagedMemorySnapshot;
struct MonoProfiler;
struct MonoMethod;
struct MonoTableInfo;
struct MonoGenericContext;

#if UNITY_STANDALONE || UNITY_EDITOR
struct MonoDlFallbackHandler;
#endif

#if UNITY_EDITOR
struct MonoMethodDesc;
#endif

typedef const void* gconstpointer;
typedef void* gpointer;
typedef int gboolean;
typedef unsigned char guint8;
typedef UInt16 guint16;
typedef unsigned int guint32;
typedef int gint32;
typedef UInt64 guint64;
typedef SInt64 gint64;
typedef unsigned long gulong;
typedef unsigned char   guchar;
typedef UInt16 gunichar2;
struct MonoString
{
    void* monoObjectPart1;
#if !ENABLE_CORECLR
    void* monoObjectPart2;
#endif
    gint32 length;
    gunichar2 firstCharacter;
};

struct MonoMethod
{
    UInt16 flags;
    UInt16 iflags;
};

struct GPtrArray
{
    gpointer *pdata;
    guint32 len;
};

typedef enum
{
    MONO_VERIFIER_MODE_OFF,
    MONO_VERIFIER_PE_ONLY,
    MONO_VERIFIER_MODE_VALID,
    MONO_VERIFIER_MODE_VERIFIABLE,
    MONO_VERIFIER_MODE_STRICT
} MiniVerifierMode;

typedef enum
{
    MONO_TYPE_NAME_FORMAT_IL,
    MONO_TYPE_NAME_FORMAT_REFLECTION,
    MONO_TYPE_NAME_FORMAT_FULL_NAME,
    MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED,
    MONO_TYPE_NAME_FORMAT_REFLECTION_QUALIFIED
} MonoTypeNameFormat;

typedef enum
{
    MONO_GC_MODE_DISABLED = 0,
    MONO_GC_MODE_ENABLED = 1,
    MONO_GC_MODE_MANUAL = 2
}  MonoGCMode;

typedef struct
{
    const char *name;
    const char *culture;
    const char *hash_value;
    const UInt8* public_key;
    // string of 16 hex chars + 1 NULL
    guchar public_key_token[17];
    guint32 hash_alg;
    guint32 hash_len;
    guint32 flags;
    UInt16 major, minor, build, revision;
    // only used and populated by newer Mono
    UInt16 arch;
    UInt8 without_version;
    UInt8 without_culture;
    UInt8 without_public_key_token;
} MonoAssemblyName;

typedef void GFuncRef (void*, void*);
typedef GFuncRef* GFunc;

typedef enum
{
    MONO_UNHANDLED_POLICY_LEGACY,
    MONO_UNHANDLED_POLICY_CURRENT
} MonoRuntimeUnhandledExceptionPolicy;

#if ENABLE_MONO_MEMORY_CALLBACKS
struct MonoMemoryCallbacks;
#endif

// mono/metadata/profiler.h
typedef enum
{
    MONO_PROFILER_CALL_INSTRUMENTATION_NONE = 0,
    MONO_PROFILER_CALL_INSTRUMENTATION_ENTER = 1 << 1,
    MONO_PROFILER_CALL_INSTRUMENTATION_ENTER_CONTEXT = 1 << 2,
    MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE = 1 << 3,
    MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE_CONTEXT = 1 << 4,
    MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL = 1 << 5,
    MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE = 1 << 6,
} MonoProfilerCallInstrumentationFlags;

typedef enum
{
    MONO_PROFILER_CODE_BUFFER_METHOD = 0,
    MONO_PROFILER_CODE_BUFFER_METHOD_TRAMPOLINE = 1,
    MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE = 2,
    MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE = 3,
    MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE = 4,
    MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE = 5,
    MONO_PROFILER_CODE_BUFFER_HELPER = 6,
    MONO_PROFILER_CODE_BUFFER_MONITOR = 7,
    MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE = 8,
    MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING = 9,
} MonoProfilerCodeBufferType;

struct MonoJitInfo
{
    MonoMethod* method;
    void* next_jit_code_hash;
    gpointer code_start;
    guint32 unwind_info;
    int code_size;
};

struct MonoDebugLineNumberEntry
{
    uint32_t il_offset;
    uint32_t native_offset;
};
struct MonoDebugMethodJitInfo
{
    gpointer code_start;
    uint32_t code_size;
    uint32_t prologue_end;
    uint32_t epilogue_begin;
    gpointer wrapper_addr;
    uint32_t num_line_numbers;
    MonoDebugLineNumberEntry *line_numbers;
};

struct MonoDebugSourceLocation
{
    char* source_file;
    UInt32 row;
    UInt32 column;
    UInt32 il_offset;
};

typedef enum
{
    /* the default is to always obey the breakpoint */
    MONO_BREAK_POLICY_ALWAYS,
    /* a nop is inserted instead of a breakpoint */
    MONO_BREAK_POLICY_NEVER,
    /* the breakpoint is executed only if the program has ben started under
     * the debugger (that is if a debugger was attached at the time the method
     * was compiled).
     */
    MONO_BREAK_POLICY_ON_DBG
} MonoBreakPolicy;

typedef MonoBreakPolicy (*MonoBreakPolicyFunc) (MonoMethod *method);

typedef struct
{
    MonoMethod *method;
    uint32_t il_offset;
    uint32_t counter;
    const char *file_name;
    uint32_t line;
    uint32_t column;
} MonoProfilerCoverageData;

#if UNITY_ANDROID
struct MonoFileMap;

typedef MonoFileMap* (*MonoFileMapOpen)     (const char* name);
typedef guint64      (*MonoFileMapSize)     (MonoFileMap *fmap);
typedef int          (*MonoFileMapFd)       (MonoFileMap *fmap);
typedef int          (*MonoFileMapClose)    (MonoFileMap *fmap);
typedef void *       (*MonoFileMapMap)      (size_t length, int flags, int fd, guint64 offset, void **ret_handle);
typedef int          (*MonoFileMapUnmap)    (void *addr, void *handle);

#if PLATFORM_ARCH_32
typedef gint32 mgreg_t;
#elif PLATFORM_ARCH_64
typedef gint64 mgreg_t;
#endif

#if defined(__arm__)
typedef struct
{
    mgreg_t pc;
    mgreg_t regs[16];
    double fregs[16];
    mgreg_t cpsr;
} MonoContext;
#elif defined(i386)
typedef struct
{
    mgreg_t eax;
    mgreg_t ebx;
    mgreg_t ecx;
    mgreg_t edx;
    mgreg_t ebp;
    mgreg_t esp;
    mgreg_t esi;
    mgreg_t edi;
    mgreg_t eip;
#ifdef __APPLE__
    MonoContextSimdReg fregs[X86_XMM_NREG];
#endif
} MonoContext;
#endif

/*
 * Possible frame types returned by the stack walker.
 */
typedef enum
{
    /* Normal managed frames */
    FRAME_TYPE_MANAGED = 0,
    /* Pseudo frame marking the start of a method invocation done by the soft debugger */
    FRAME_TYPE_DEBUGGER_INVOKE = 1,
    /* Frame for transitioning to native code */
    FRAME_TYPE_MANAGED_TO_NATIVE = 2,
    FRAME_TYPE_TRAMPOLINE = 3,
    /* Interpreter frame */
    FRAME_TYPE_INTERP = 4,
    /* Frame for transitioning from interpreter to managed code */
    FRAME_TYPE_INTERP_TO_MANAGED = 5,
    /* same, but with MonoContext */
    FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX = 6,
    FRAME_TYPE_NUM = 7
} MonoStackFrameType;

typedef struct
{
    MonoStackFrameType type;
    /*
     * For FRAME_TYPE_MANAGED, otherwise NULL.
     */
    /*MonoJitInfo*/ void *ji;
    /*
     * Same as ji->method.
     * Not valid if ASYNC_CONTEXT is true.
     */
    MonoMethod *method;
    /*
     * If ji->method is a gshared method, this is the actual method instance.
     * This is only filled if lookup for actual method was requested (MONO_UNWIND_LOOKUP_ACTUAL_METHOD)
     * Not valid if ASYNC_CONTEXT is true.
     */
    MonoMethod *actual_method;
    /* The domain containing the code executed by this frame */
    MonoDomain *domain;
    /* Whenever method is a user level method */
    gboolean managed;
    /*
     * Whenever this frame was loaded in async context.
     */
    gboolean async_context;
    int native_offset;
    /*
     * IL offset of this frame.
     * Only available if the runtime have debugging enabled (--debug switch) and
     *  il offset resultion was requested (MONO_UNWIND_LOOKUP_IL_OFFSET)
     */
    int il_offset;

    /* For FRAME_TYPE_INTERP_EXIT */
    gpointer interp_exit_data;

    /* For FRAME_TYPE_INTERP */
    gpointer interp_frame;

    /*
     * A stack address associated with the frame which can be used
     * to compare frames.
     * This is needed because ctx is not changed when unwinding through
     * interpreter frames, it still refers to the last native interpreter
     * frame.
     */
    gpointer frame_addr;

    /* The next fields are only useful for the jit */
    gpointer lmf;
    guint32 unwind_info_len;
    guint8 *unwind_info;

    mgreg_t **reg_locations;
} MonoStackFrameInfo;

typedef MonoStackFrameInfo StackFrameInfo;

typedef gboolean(*MonoJitStackWalk)(StackFrameInfo *frame, MonoContext *ctx, gpointer data);

typedef enum
{
    MONO_UNWIND_NONE = 0x0,
    MONO_UNWIND_LOOKUP_IL_OFFSET = 0x1,
    /* NOT signal safe */
    MONO_UNWIND_LOOKUP_ACTUAL_METHOD = 0x2,
    /*
     * Store the locations where caller-saved registers are saved on the stack in
     * frame->reg_locations. The pointer is only valid during the call to the unwind
     * callback.
     */
    MONO_UNWIND_REG_LOCATIONS = 0x4,
    MONO_UNWIND_DEFAULT = MONO_UNWIND_LOOKUP_ACTUAL_METHOD,
    MONO_UNWIND_SIGNAL_SAFE = MONO_UNWIND_NONE,
    MONO_UNWIND_LOOKUP_ALL = MONO_UNWIND_LOOKUP_IL_OFFSET | MONO_UNWIND_LOOKUP_ACTUAL_METHOD,
} MonoUnwindOptions;

#endif // UNITY_ANDROID

struct MonoUnityCallstackFilter
{
    const char* name_space;
    const char* class_name;
    const char* method_name;
};

struct MonoUnityCallstackOptions
{
    const char *path_prefix_filter;
    int filter_count;
    const MonoUnityCallstackFilter *line_filters;
};


/*Keep in sync with MonoErrorInternal*/
typedef struct _MonoError
{
    unsigned short error_code;
    unsigned short hidden_0; /*DON'T TOUCH */

    void *hidden_1[12];  /*DON'T TOUCH */
} MonoError;

typedef uintptr_t MonoGCHandle;


typedef void*(*mono_liveness_reallocate_callback)(void* ptr, size_t size, void* state);

#endif //ENABLE_MONO
