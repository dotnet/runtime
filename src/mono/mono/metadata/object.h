#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/metadata/class.h>
#include <mono/metadata/threads-types.h>

#if 1
#define mono_assert(expr) 	           G_STMT_START{		  \
     if (!(expr))							  \
       {								  \
              	MonoException *ex;                                        \
                char *msg = g_strdup_printf ("file %s: line %d (%s): "    \
                "assertion failed: (%s)", __FILE__, __LINE__,             \
                __PRETTY_FUNCTION__, #expr);				  \
		ex = mono_get_exception_execution_engine (msg);           \
		g_free (msg);                                             \
                mono_raise_exception (ex);                                \
       };				}G_STMT_END

#define mono_assert_not_reached() 	          G_STMT_START{		  \
     MonoException *ex;                                                   \
     char *msg = g_strdup_printf ("file %s: line %d (%s): "               \
     "should not be reached", __FILE__, __LINE__, __PRETTY_FUNCTION__);	  \
     ex = mono_get_exception_execution_engine (msg);                      \
     g_free (msg);                                                        \
     mono_raise_exception (ex);                                           \
}G_STMT_END
#else
#define mono_assert(expr) g_assert(expr)
#define mono_assert_not_reached() g_assert_not_reached() 
#endif

#define MONO_CHECK_ARG(arg, expr)       	G_STMT_START{		  \
     if (!(expr))							  \
       {								  \
              	MonoException *ex;                                        \
                char *msg = g_strdup_printf ("assertion `%s' failed",     \
		#expr);							  \
                if (arg) {} /* check if the name exists */                \
		ex = mono_get_exception_argument (#arg, msg);             \
		g_free (msg);                                             \
                mono_raise_exception (ex);                                \
       };				}G_STMT_END

#define MONO_CHECK_ARG_NULL(arg) 	    G_STMT_START{		  \
     if (arg == NULL)							  \
       {								  \
              	MonoException *ex;                                        \
                if (arg) {} /* check if the name exists */                \
		ex = mono_get_exception_argument_null (#arg);             \
                mono_raise_exception (ex);                                \
       };				}G_STMT_END

typedef guchar MonoBoolean;

typedef struct _MonoReflectionMethod MonoReflectionMethod;

typedef struct {
	MonoVTable *vtable;
	MonoThreadsSync *synchronisation;
} MonoObject;

typedef struct {
	guint32 length;
	guint32 lower_bound;
} MonoArrayBounds;

typedef struct {
	MonoObject obj;
	/* bounds is NULL for szarrays */
	MonoArrayBounds *bounds;
	/* total number of elements of the array */
	guint32 max_length; 
	/* we use double to ensure proper alignment on platforms that need it */
	double vector [MONO_ZERO_LEN_ARRAY];
} MonoArray;

typedef struct {
	MonoObject obj;
	MonoArray *c_str;
	gint32 length;
} MonoString;

typedef struct {
	MonoObject object;
	MonoType  *type;
} MonoReflectionType;

typedef struct {
	MonoObject object;
	MonoObject *inner_ex;
	MonoString *message;
	MonoString *help_link;
	MonoString *class_name;
	MonoString *stack_trace;
	gint32      hresult;
	MonoString *source;
} MonoException;

typedef struct {
	MonoException base;
} MonoSystemException;

typedef struct {
	MonoSystemException base;
	MonoString *param_name;
} MonoArgumentException;

typedef struct {
	MonoObject   object;
	MonoObject  *async_state;
	MonoObject  *handle;
	MonoObject  *async_delegate;
	gpointer     data;
	MonoBoolean  sync_completed;
	MonoBoolean  completed;
	MonoBoolean  endinvoke_called;
} MonoAsyncResult;

typedef struct {
	MonoObject   object;
	gpointer     handle;
	MonoBoolean  disposed;
} MonoWaitHandle;

typedef struct {
	MonoObject  object;
	MonoReflectionType *class_to_proxy;	
} MonoRealProxy;

typedef struct {
	MonoObject     object;
	MonoRealProxy *rp;	
	MonoClass     *klass; 
} MonoTransparentProxy;

typedef struct {
	MonoObject obj;
	MonoReflectionMethod *method;
	MonoArray  *args;		
	MonoArray  *names;		
	MonoArray  *arg_types;	
	MonoObject *ctx;
	MonoObject *rval;
	MonoObject *exc;
} MonoMethodMessage;

typedef struct {
	gulong new_object_count;
	gulong initialized_class_count;
	gulong used_class_count;
	gulong class_vtable_size;
	gulong class_static_data_size;
} MonoStats;

typedef MonoObject* (*MonoInvokeFunc)        (MonoMethod *method, void *obj, void **params);

#define mono_object_class(obj) (((MonoObject*)(obj))->vtable->klass)
#define mono_object_domain(obj) (((MonoObject*)(obj))->vtable->domain)

#define mono_array_length(array) ((array)->max_length)
#define mono_array_addr(array,type,index) ( ((char*)(array)->vector) + sizeof (type) * (index) )
#define mono_array_addr_with_size(array,size,index) ( ((char*)(array)->vector) + (size) * (index) )
#define mono_array_get(array,type,index) ( *(type*)mono_array_addr ((array), type, (index)) ) 
#define mono_array_set(array,type,index,value)	\
	do {	\
		type *__p = (type *) mono_array_addr ((array), type, (index));	\
		*__p = (value);	\
	} while (0)

#define mono_string_chars(s) ((gushort*)(s)->c_str->vector)
#define mono_string_length(s) ((s)->length)

extern MonoStats mono_stats;

void *
mono_object_allocate        (size_t size);

MonoObject *
mono_object_new             (MonoDomain *domain, MonoClass *klass);

MonoObject *
mono_object_new_from_token  (MonoDomain *domain, MonoImage *image, guint32 token);

MonoArray*
mono_array_new              (MonoDomain *domain, MonoClass *eclass, guint32 n);

MonoArray*
mono_array_new_full         (MonoDomain *domain, MonoClass *array_class,
			     guint32 *lengths, guint32 *lower_bounds);

MonoArray*
mono_array_clone            (MonoArray *array);

MonoString*
mono_string_new_utf16       (MonoDomain *domain, const guint16 *text, gint32 len);

MonoString*
mono_string_new_size		(MonoDomain *domain, gint32 len);

MonoString*
mono_ldstr                  (MonoDomain *domain, MonoImage *image, guint32 str_index);

MonoString*
mono_string_is_interned     (MonoString *str);

MonoString*
mono_string_intern          (MonoString *str);

MonoString*
mono_string_new             (MonoDomain *domain, const char *text);

MonoString*
mono_string_new_wrapper     (const char *text);

MonoString*
mono_string_new_len         (MonoDomain *domain, const char *text, guint length);

char *
mono_string_to_utf8         (MonoString *string_obj);

gunichar2 *
mono_string_to_utf16        (MonoString *string_obj);

void       
mono_object_free            (MonoObject *o);

MonoObject *
mono_value_box              (MonoDomain *domain, MonoClass *klass, gpointer val);
		      
MonoObject *
mono_object_clone           (MonoObject *obj);

MonoObject *
mono_object_isinst          (MonoObject *obj, MonoClass *klass);

typedef void (*MonoExceptionFunc) (MonoException *ex);

void
mono_install_handler        (MonoExceptionFunc func);

void
mono_raise_exception        (MonoException *ex);

void
mono_runtime_object_init    (MonoObject *this);

void
mono_runtime_class_init     (MonoClass *klass);

void        
mono_install_runtime_invoke (MonoInvokeFunc func);

MonoObject*
mono_runtime_invoke         (MonoMethod *method, void *obj, void **params);

MonoObject*
mono_runtime_invoke_array   (MonoMethod *method, void *obj, MonoArray *params);

int
mono_runtime_exec_main      (MonoMethod *method, MonoArray *args);

MonoAsyncResult *
mono_async_result_new       (MonoDomain *domain, HANDLE handle, 
			     MonoObject *state, gpointer data);

MonoWaitHandle *
mono_wait_handle_new        (MonoDomain *domain, HANDLE handle);

void
mono_message_init           (MonoDomain *domain, MonoMethodMessage *this, 
			     MonoReflectionMethod *method, MonoArray *out_args);

MonoObject *
mono_remoting_invoke        (MonoObject *real_proxy, MonoMethodMessage *msg, 
			     MonoObject **exc, MonoArray **out_args);

MonoObject *
mono_message_invoke         (MonoObject *target, MonoMethodMessage *msg, 
			     MonoObject **exc, MonoArray **out_args);

#endif

