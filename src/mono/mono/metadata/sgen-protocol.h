enum {
	SGEN_PROTOCOL_COLLECTION,
	SGEN_PROTOCOL_ALLOC,
	SGEN_PROTOCOL_COPY,
	SGEN_PROTOCOL_PIN,
	SGEN_PROTOCOL_WBARRIER,
	SGEN_PROTOCOL_GLOBAL_REMSET,
	SGEN_PROTOCOL_PTR_UPDATE,
	SGEN_PROTOCOL_CLEANUP,
	SGEN_PROTOCOL_EMPTY,
	SGEN_PROTOCOL_THREAD_RESTART,
	SGEN_PROTOCOL_THREAD_REGISTER,
	SGEN_PROTOCOL_THREAD_UNREGISTER,
	SGEN_PROTOCOL_MISSING_REMSET
};

typedef struct {
	int generation;
} SGenProtocolCollection;

typedef struct {
	gpointer obj;
	gpointer vtable;
	int size;
} SGenProtocolAlloc;

typedef struct {
	gpointer from;
	gpointer to;
	gpointer vtable;
	int size;
} SGenProtocolCopy;

typedef struct {
	gpointer obj;
	gpointer vtable;
	int size;
} SGenProtocolPin;

typedef struct {
	gpointer ptr;
	gpointer value;
	gpointer value_vtable;
} SGenProtocolWBarrier;

typedef struct {
	gpointer ptr;
	gpointer value;
	gpointer value_vtable;
} SGenProtocolGlobalRemset;

typedef struct {
	gpointer ptr;
	gpointer old_value;
	gpointer new_value;
	gpointer vtable;
	int size;
} SGenProtocolPtrUpdate;

typedef struct {
	gpointer ptr;
	gpointer vtable;
	int size;
} SGenProtocolCleanup;

typedef struct {
	gpointer start;
	int size;
} SGenProtocolEmpty;

typedef struct {
	gpointer thread;
} SGenProtocolThreadRestart;

typedef struct {
	gpointer thread;
} SGenProtocolThreadRegister;

typedef struct {
	gpointer thread;
} SGenProtocolThreadUnregister;

typedef struct {
	gpointer obj;
	gpointer obj_vtable;
	int offset;
	gpointer value;
	gpointer value_vtable;
	int value_pinned;
} SGenProtocolMissingRemset;

/* missing: finalizers, dislinks, roots, non-store wbarriers */
