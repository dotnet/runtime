/*
 * mono-jni.c: native methods required by the mono JNI implementation.
 *
 *
 */

#include <jni.h>
#include <gmodule.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>

#include <string.h>
#include <stdarg.h>

/*
 * PROTOTYPES
 */

void * native_load_native_library (char *filename);

int native_lookup_symbol (GModule *module, char *symbol_name, gpointer *symbol);
void mono_jni_jnienv_init (
	void *makelocalref_func,
	void *unwrap_func,
	void *makeglobalref_func,
	void *deleteref_func,
	void *getfieldcookie_func,
	void *getmethodcookie_func,
	void *setfieldvalue_func,
	void *getfieldvalue_func,
	void *getclassfromobject_func,
	void *exceptioncheck_func,
	void *getpendingexception_func,
	void *setpendingexception_func,
	void *invokemethod_func,
	void *getmethodarglist_func,
	void *findclass_func,
	void *getjnienv_func,
	void *allocobject_func);

void* mono_jni_get_func_table (void);

void mono_jni_set_jnifunc (int index, void *func);

jint JNICALL GetJavaVM (JNIEnv *env, JavaVM **vm);


void *
native_load_native_library (char *filename)
{
	return g_module_open (filename, 0);
}

int 
native_lookup_symbol (GModule *module, char *symbol_name, gpointer *symbol)
{
	gboolean res = g_module_symbol (module, symbol_name, symbol);
	return res;
}

typedef struct MonoJniFunctions {
	int (*MakeLocalRef) (JNIEnv *env, void *obj);
	void * (*UnwrapRef) (JNIEnv *env, void *ref);
	int (*MakeGlobalRef) (void *obj);
	void (*DeleteRef) (JNIEnv *env, void *ref);
	int (*GetFieldCookie) (void *klass, void *name, void *sig, gboolean is_static);
	int (*GetMethodCookie) (void *klass, void *name, void *sig, gboolean is_static);
	void (*SetFieldValue) (void *cookie, void *obj, void *val);
	void * (*GetFieldValue) (void *cookie, void *obj);
	void * (*GetClassFromObject) (void *obj);
	jboolean (*ExceptionCheck) (JNIEnv *env);
	void * (*GetPendingException) (JNIEnv *env);
	void (*SetPendingException) (JNIEnv *env, void *obj);
	void * (*InvokeMethod) (JNIEnv *env, void *cookie, void *obj, void *args, int virtual);
	void * (*GetMethodArgList) (void *cookie);
	void * (*FindClass) (void *name);
	void * (*GetJniEnv) (void);
	void * (*AllocObject) (void *klass);
} MonoJniFunctions;

static MonoJniFunctions jniFuncs;

void
mono_jni_jnienv_init (
	void *makelocalref_func,
	void *unwrap_func,
	void *makeglobalref_func,
	void *deleteref_func,
	void *getfieldcookie_func,
	void *getmethodcookie_func,
	void *setfieldvalue_func,
	void *getfieldvalue_func,
	void *getclassfromobject_func,
	void *exceptioncheck_func,
	void *getpendingexception_func,
	void *setpendingexception_func,
	void *invokemethod_func,
	void *getmethodarglist_func,
	void *findclass_func,
	void *getjnienv_func,
	void *allocobject_func)
{
	jniFuncs.MakeLocalRef = makelocalref_func;
	jniFuncs.UnwrapRef = unwrap_func;
	jniFuncs.MakeGlobalRef = makeglobalref_func;
	jniFuncs.DeleteRef = deleteref_func;
	jniFuncs.GetFieldCookie = getfieldcookie_func;
	jniFuncs.GetMethodCookie = getmethodcookie_func;
	jniFuncs.SetFieldValue = setfieldvalue_func;
	jniFuncs.GetFieldValue = getfieldvalue_func;
	jniFuncs.GetClassFromObject = getclassfromobject_func;
	jniFuncs.ExceptionCheck = exceptioncheck_func;
	jniFuncs.GetPendingException = getpendingexception_func;
	jniFuncs.SetPendingException = setpendingexception_func;
	jniFuncs.InvokeMethod = invokemethod_func;
	jniFuncs.GetMethodArgList = getmethodarglist_func;
	jniFuncs.FindClass = findclass_func;
	jniFuncs.GetJniEnv = getjnienv_func;
	jniFuncs.AllocObject = allocobject_func;
}

static void *jni_func_table[256];

static void ***jni_ptr = NULL;

static void *vm_func_table[64];

static void ***vm_ptr = NULL;

void*
mono_jni_get_func_table (void)
{
	if (!jni_ptr) {
		jni_ptr = (void***)&jni_func_table;
	}

	return jni_ptr;
}

void
mono_jni_set_jnifunc (int index, void *func)
{
	jni_func_table [index] = func;
}

static MonoString *
StringFromUTF8 (const char* psz)
{
	/* TODO: */
	return mono_string_new (mono_domain_get (), psz);
#if 0
	/* Sun's modified UTF8 encoding is not compatible with System::Text::Encoding::UTF8, so */
	/* we need to roll our own */
	int len, res_len, i;
	int *res;

	len = strlen (psz);
	res = g_malloc (len * sizeof (int));
	res_len = 0;
	for (i = 0; i < len; i++) {
		int c = (unsigned char)*psz++;
		int char2, char3;
		switch (c >> 4)
		{
		case 0: case 1: case 2: case 3: case 4: case 5: case 6: case 7:
			/* 0xxxxxxx */
			break;
		case 12: case 13:
			/* 110x xxxx   10xx xxxx */
			char2 = *psz++;
			i++;
			c = (((c & 0x1F) << 6) | (char2 & 0x3F));
			break;
		case 14:
			/* 1110 xxxx  10xx xxxx  10xx xxxx */
			char2 = *psz++;
			char3 = *psz++;
			i++;
			i++;
			c = (((c & 0x0F) << 12) | ((char2 & 0x3F) << 6) | ((char3 & 0x3F) << 0));
			break;
		}

		res [res_len ++] = c;
	}

	return mono_string_new_utf16 (mono_domain_get (), res, res_len);
#endif
}

/***************************************************************************/
/*                        JNI FUNCTIONS                                    */
/***************************************************************************/


static jint JNICALL GetVersion (JNIEnv *env) { printf ("JNI Function GetVersion is not implemented.\n"); g_assert_not_reached (); return 0; }

static jclass JNICALL DefineClass (JNIEnv *env, const char *name, jobject loader, const jbyte *buf, jsize len) { printf ("JNI Function DefineClass is not implemented.\n"); g_assert_not_reached (); return 0; }
static jclass JNICALL FindClass (JNIEnv *env, const char *name)
{
	return (jclass)(jniFuncs.MakeLocalRef (env, jniFuncs.FindClass (StringFromUTF8(name))));
}

static jmethodID JNICALL FromReflectedMethod (JNIEnv *env, jobject method) { printf ("JNI Function FromReflectedMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jfieldID JNICALL FromReflectedField (JNIEnv *env, jobject field) { printf ("JNI Function FromReflectedField is not implemented.\n"); g_assert_not_reached (); return 0; }

static jobject JNICALL ToReflectedMethod (JNIEnv *env, jclass cls, jmethodID methodID, jboolean isStatic) { printf ("JNI Function ToReflectedMethod is not implemented.\n"); g_assert_not_reached (); return 0; }

static jclass JNICALL GetSuperclass (JNIEnv *env, jclass sub) { printf ("JNI Function GetSuperclass is not implemented.\n"); g_assert_not_reached (); return 0; }
static jboolean JNICALL IsAssignableFrom (JNIEnv *env, jclass sub, jclass sup) { printf ("JNI Function IsAssignableFrom is not implemented.\n"); g_assert_not_reached (); return 0; }

static jobject JNICALL ToReflectedField (JNIEnv *env, jclass cls, jfieldID fieldID, jboolean isStatic) { printf ("JNI Function ToReflectedField is not implemented.\n"); g_assert_not_reached (); return 0; }

static jint JNICALL Throw (JNIEnv *env, jthrowable obj) { printf ("JNI Function Throw is not implemented.\n"); g_assert_not_reached (); return 0; }
static jint JNICALL ThrowNew (JNIEnv *env, jclass clazz, const char *msg) { printf ("JNI Function ThrowNew is not implemented.\n"); g_assert_not_reached (); return 0; }

static jthrowable JNICALL ExceptionOccurred (JNIEnv *env)
{
	return (jthrowable)jniFuncs.MakeLocalRef (env, jniFuncs.GetPendingException (env));
}

static void JNICALL ExceptionDescribe (JNIEnv *env) { printf ("JNI Function ExceptionDescribe is not implemented.\n"); g_assert_not_reached (); }
static void JNICALL ExceptionClear (JNIEnv *env) { printf ("JNI Function ExceptionClear is not implemented.\n"); g_assert_not_reached (); }
static void JNICALL FatalError (JNIEnv *env, const char *msg) { printf ("JNI Function FatalError is not implemented.\n"); g_assert_not_reached (); }

static jint JNICALL PushLocalFrame (JNIEnv *env, jint capacity) { printf ("JNI Function PushLocalFrame is not implemented.\n"); g_assert_not_reached (); return 0; }
static jobject JNICALL PopLocalFrame (JNIEnv *env, jobject result) { printf ("JNI Function PopLocalFrame is not implemented.\n"); g_assert_not_reached (); return 0; }

static jobject JNICALL NewGlobalRef (JNIEnv *env, jobject lobj)
{
	return (jobject)jniFuncs.MakeGlobalRef (jniFuncs.UnwrapRef (env, lobj));
}

static void JNICALL DeleteGlobalRef (JNIEnv *env, jobject gref)
{
	jniFuncs.DeleteRef (env, gref);
}

static void JNICALL DeleteLocalRef (JNIEnv *env, jobject obj)
{
	jniFuncs.DeleteRef (env, obj);
}

static jboolean JNICALL IsSameObject (JNIEnv *env, jobject obj1, jobject obj2) { printf ("JNI Function IsSameObject is not implemented.\n"); g_assert_not_reached (); return 0; }
static jobject JNICALL NewLocalRef (JNIEnv *env, jobject ref) { printf ("JNI Function NewLocalRef is not implemented.\n"); g_assert_not_reached (); return 0; }
static jint JNICALL EnsureLocalCapacity (JNIEnv *env, jint capacity) { printf ("JNI Function EnsureLocalCapacity is not implemented.\n"); g_assert_not_reached (); return 0; }

static jobject JNICALL AllocObject (JNIEnv *env, jclass clazz) { printf ("JNI Function EnsureLocalCapacity is not implemented.\n"); g_assert_not_reached (); return 0; }

static jclass JNICALL GetObjectClass (JNIEnv *env, jobject obj)
{
	g_assert (obj);

	return (jclass)jniFuncs.MakeLocalRef (env, jniFuncs.GetClassFromObject (jniFuncs.UnwrapRef (env, obj)));
}
	
static jboolean JNICALL IsInstanceOf (JNIEnv *env, jobject obj, jclass clazz) { printf ("JNI Function IsInstanceOf is not implemented.\n"); g_assert_not_reached (); return 0; }

static jobject JNICALL CallObjectMethodA (JNIEnv *env, jobject obj, jmethodID methodID, jvalue * args) { printf ("JNI Function CallObjectMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

#define BOXED_VALUE(obj) ((char*)(obj) + sizeof (MonoObject))

static MonoObject *
box_Boolean (jboolean val)
{
	return mono_value_box (mono_domain_get (), mono_get_boolean_class (), &val);
}

static MonoObject *
box_Byte (jbyte val)
{
	/* Sbyte ! */
	return mono_value_box (mono_domain_get (), mono_get_sbyte_class (), &val);
}

static MonoObject *
box_Char (jchar val)
{
	return mono_value_box (mono_domain_get (), mono_get_char_class (), &val);
}

static MonoObject *
box_Short (jshort val)
{
	return mono_value_box (mono_domain_get (), mono_get_int16_class (), &val);
}

static MonoObject *
box_Int (jint val)
{
	return mono_value_box (mono_domain_get (), mono_get_int32_class (), &val);
}

static MonoObject *
box_Long (jlong val)
{
	return mono_value_box (mono_domain_get (), mono_get_int64_class (), &val);
}

static MonoObject *
box_Float (jfloat val)
{
	return mono_value_box (mono_domain_get (), mono_get_single_class (), &val);
}

static MonoObject *
box_Double (jdouble val)
{
	return mono_value_box (mono_domain_get (), mono_get_double_class (), &val);
}

static int 
GetMethodArgs (jmethodID methodID, char* sig)
{
	char *res;

	res = jniFuncs.GetMethodArgList (methodID);
	strcpy (sig, res);
	return strlen (sig);
}

static MonoObject* 
InvokeHelper (JNIEnv *env, jobject object, jmethodID methodID, jvalue* args)
{
	char sig[257];
	int argc, i;
	MonoObject **argarray;
	MonoArray *args2;

/* assert(!pLocalRefs->PendingException); */
	g_assert(methodID);

	argc = GetMethodArgs(methodID, sig);
	argarray = g_new (MonoObject*, argc);
	for(i = 0; i < argc; i++)
	{
		switch(sig[i])
		{
		case 'Z': {
			jboolean val = args[i].z != JNI_FALSE;
			argarray[i] = box_Boolean (val);
			break;
		}
		case 'B':
			argarray[i] = box_Byte (args[i].b);
			break;
		case 'C':
			argarray[i] = box_Char (args[i].c);
			break;
		case 'S':
			argarray[i] = box_Short (args[i].s);
			break;
		case 'I':
			argarray[i] = box_Int (args[i].i);
			break;
		case 'J':
			argarray[i] = box_Long (args[i].j);
			break;
		case 'F':
			argarray[i] = box_Float (args[i].f);
			break;
		case 'D':
			argarray[i] = box_Double (args[i].d);
			break;
		case 'L':
			argarray[i] = jniFuncs.UnwrapRef (env, args[i].l);
			break;
		}
	}

	args2 = mono_array_new (mono_domain_get (), mono_get_object_class (), argc);
	for (i = 0; i < argc; ++i)
		mono_array_set (args2, MonoObject*, i, argarray [i]);

	return jniFuncs.InvokeMethod (env, methodID, jniFuncs.UnwrapRef (env, object), args2, FALSE);
}

#define METHOD_IMPL_MANAGED(Type,type,cpptype) \
static type JNICALL Call##Type##MethodA(JNIEnv *env, jobject obj, jmethodID methodID, jvalue* args)\
{\
	MonoObject* ret = InvokeHelper(env, obj, methodID, args);\
	if(ret)	return *(type*)BOXED_VALUE(ret);\
	return 0;\
}

#define METHOD_IMPL(Type,type) \
static type JNICALL Call##Type##MethodV(JNIEnv *env, jobject obj, jmethodID methodID, va_list args)\
{\
	char sig[257];\
    int i;\
	int argc = GetMethodArgs(methodID, sig);\
	jvalue* argarray = (jvalue*)alloca(argc * sizeof(jvalue));\
	for(i = 0; i < argc; i++)\
	{\
		switch(sig[i])\
		{\
		case 'Z':\
		case 'B':\
		case 'S':\
		case 'C':\
		case 'I':\
			argarray[i].i = va_arg(args, int);\
			break;\
		case 'J':\
			argarray[i].j = va_arg(args, gint64);\
			break;\
		case 'L':\
			argarray[i].l = va_arg(args, jobject);\
			break;\
		case 'D':\
			argarray[i].d = va_arg(args, double);\
			break;\
		case 'F':\
			argarray[i].f = (float)va_arg(args, double);\
			break;\
		}\
	}\
	return Call##Type##MethodA(env, obj, methodID, argarray);\
}\
static type Call##Type##Method(JNIEnv *env, jobject obj, jmethodID methodID, ...) \
{\
	va_list args;\
    type ret;\
	va_start(args, methodID);\
	ret = Call##Type##MethodV(env, obj, methodID, args);\
	va_end(args);\
	return ret;\
}\
static type JNICALL CallNonvirtual##Type##Method (JNIEnv *env, jobject obj, jclass clazz, jmethodID methodID, ...) \
{\
    printf ("JNI Function CallNonvirtual" #Type "Method is not implemented.\n"); g_assert_not_reached ();\
    return 0;\
}\
static type JNICALL CallNonvirtual##Type##MethodV (JNIEnv *env, jobject obj, jclass clazz, jmethodID methodID, va_list args) \
{\
    printf ("JNI Function CallNonvirtual" #Type "MethodV is not implemented.\n"); g_assert_not_reached ();\
    return 0;\
}\
static type JNICALL CallNonvirtual##Type##MethodA (JNIEnv *env, jobject obj, jclass clazz, jmethodID methodID, jvalue *args) \
{\
    printf ("JNI Function CallNonvirtual" #Type "MethodA is not implemented.\n"); g_assert_not_reached ();\
    return 0;\
}

METHOD_IMPL_MANAGED(Boolean,jboolean,gboolean)
METHOD_IMPL_MANAGED(Byte,jbyte,gchar)
METHOD_IMPL_MANAGED(Char,jchar,gunichar2)
METHOD_IMPL_MANAGED(Short,jshort,gint16)
METHOD_IMPL_MANAGED(Int,jint,gint32)
METHOD_IMPL_MANAGED(Long,jlong,gint64)
METHOD_IMPL_MANAGED(Float,jfloat,float)
METHOD_IMPL_MANAGED(Double,jdouble,double)

METHOD_IMPL(Object,jobject)
METHOD_IMPL(Boolean,jboolean)
METHOD_IMPL(Byte,jbyte)
METHOD_IMPL(Char,jchar)
METHOD_IMPL(Short,jshort)
METHOD_IMPL(Int,jint)
METHOD_IMPL(Long,jlong)
METHOD_IMPL(Float,jfloat)
METHOD_IMPL(Double,jdouble)















/* TODO: These should be put into the macros above...  */
static void JNICALL CallVoidMethodA (JNIEnv *env, jobject obj, jmethodID methodID, jvalue * args) { 
	InvokeHelper(env, obj, methodID, args);
}

static void JNICALL CallVoidMethodV (JNIEnv *env, jobject obj, jmethodID methodID, va_list args) { 
	char sig[257];
	int argc, i;
	jvalue* argarray;

	argc = GetMethodArgs(methodID, sig);
	argarray = (jvalue*)alloca(argc * sizeof(jvalue));

	for(i = 0; i < argc; i++)
	{
		switch(sig[i])
		{
		case 'Z':
		case 'B':
		case 'S':
		case 'C':
		case 'I':
			argarray[i].i = va_arg(args, int);
			break;
		case 'J':
			argarray[i].j = va_arg(args, gint64);
			break;
		case 'L':
			argarray[i].l = va_arg(args, jobject);
                        break;
                case 'D':
			argarray[i].d = va_arg(args, double);
			break;
		case 'F':
			argarray[i].f = (float)va_arg(args, double);
			break;
		}
	}
	CallVoidMethodA(env, obj, methodID, argarray);
}

static void JNICALL CallVoidMethod (JNIEnv *env, jobject obj, jmethodID methodID, ...) {
	va_list args;
	va_start(args, methodID);
	CallVoidMethodV(env, obj, methodID, args);
	va_end(args);
}

static void JNICALL CallNonvirtualVoidMethod (JNIEnv *env, jobject obj, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallNonvirtualVoidMethod is not implemented.\n"); g_assert_not_reached (); }
static void JNICALL CallNonvirtualVoidMethodV (JNIEnv *env, jobject obj, jclass clazz, jmethodID methodID,       va_list args) { printf ("JNI Function CallNonvirtualVoidMethodV is not implemented.\n"); g_assert_not_reached (); }
static void JNICALL CallNonvirtualVoidMethodA (JNIEnv *env, jobject obj, jclass clazz, jmethodID methodID,       jvalue * args) { printf ("JNI Function CallNonvirtualVoidMethodA is not implemented.\n"); g_assert_not_reached (); }

static jfieldID FindFieldID(JNIEnv *env, jclass cls, const char* name, const char* sig, gboolean isstatic)
{
	return (jfieldID)jniFuncs.GetFieldCookie (jniFuncs.UnwrapRef (env, cls), StringFromUTF8 (name), StringFromUTF8 (sig), isstatic);
}

static jmethodID FindMethodID(JNIEnv *env, jclass cls, const char* name, const char* sig, gboolean isstatic)
{
	return (jmethodID)jniFuncs.GetMethodCookie (
		jniFuncs.UnwrapRef (env, cls), StringFromUTF8(name), StringFromUTF8(sig), isstatic);
}

static jmethodID JNICALL GetMethodID (JNIEnv *env, jclass clazz, const char *name, const char *sig)
{
	return FindMethodID (env, clazz, name, sig, FALSE);
}


static jfieldID JNICALL GetFieldID (JNIEnv *env, jclass clazz, const char *name, const char *sig)
{
	return FindFieldID (env, clazz, name, sig, FALSE);
}

static jfieldID JNICALL GetStaticFieldID (JNIEnv *env, jclass clazz, const char *name, const char *sig) 
{ 	
	return FindFieldID (env, clazz, name, sig, TRUE);
}

static jmethodID JNICALL GetStaticMethodID (JNIEnv *env, jclass clazz, const char *name, const char *sig)
{
	return FindMethodID (env, clazz, name, sig, TRUE);
}

#define GET_SET_FIELD(Type,type,cpptype) \
static void JNICALL Set##Type##Field(JNIEnv *env, jobject obj, jfieldID fieldID, type val)\
{\
    jniFuncs.SetFieldValue (fieldID, jniFuncs.UnwrapRef (env, obj), \
                            box_##Type (val));\
}\
static type JNICALL Get##Type##Field(JNIEnv *env, jobject obj, jfieldID fieldID)\
{\
    return *(cpptype*)BOXED_VALUE(jniFuncs.GetFieldValue (fieldID, jniFuncs.UnwrapRef (env, obj)));\
}\
static void JNICALL SetStatic##Type##Field (JNIEnv *env, jclass clazz, jfieldID fieldID, jint value)\
{\
    jniFuncs.SetFieldValue (fieldID, NULL, \
                            box_##Type (value));\
}\
static type JNICALL GetStatic##Type##Field(JNIEnv *env, jclass clazz, jfieldID fieldID)\
{\
    return *(cpptype*)BOXED_VALUE(jniFuncs.GetFieldValue (fieldID, NULL));\
}\



/*    return *(cpptype*)BOXED_VALUE (jniFuncs.GetFieldValue (fieldID, jniFuncs.UnwrapRef (obj)));  */

GET_SET_FIELD(Boolean,jboolean,gboolean)
GET_SET_FIELD(Byte,jbyte, gchar)
GET_SET_FIELD(Char,jchar, gunichar2)
GET_SET_FIELD(Short,jshort,gshort)
GET_SET_FIELD(Int,jint,int)
GET_SET_FIELD(Long,jlong, gint64)
GET_SET_FIELD(Float,jfloat,float)
GET_SET_FIELD(Double,jdouble,double)

static jobject JNICALL GetObjectField (JNIEnv *env, jobject obj, jfieldID fieldID)
{
	return (jobject)(jniFuncs.MakeLocalRef (env, jniFuncs.GetFieldValue (fieldID, jniFuncs.UnwrapRef (env, obj))));
}

static void JNICALL SetObjectField (JNIEnv *env, jobject obj, jfieldID fieldID, jobject val)
{
	jniFuncs.SetFieldValue (fieldID, jniFuncs.UnwrapRef (env, obj), jniFuncs.UnwrapRef (env, val));
}

static jobject JNICALL GetStaticObjectField (JNIEnv *env, jclass clazz, jfieldID fieldID)
{
	return (jobject)(jniFuncs.MakeLocalRef (env, jniFuncs.GetFieldValue (fieldID, NULL)));
}	

static void JNICALL SetStaticObjectField (JNIEnv *env, jclass clazz, jfieldID fieldID, jobject value)
{
	jniFuncs.SetFieldValue (fieldID, NULL, jniFuncs.UnwrapRef (env, value));
}	



static jobject JNICALL CallStaticObjectMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticObjectMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jobject JNICALL CallStaticObjectMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticObjectMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jobject JNICALL CallStaticObjectMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticObjectMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jboolean JNICALL CallStaticBooleanMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticBooleanMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jboolean JNICALL CallStaticBooleanMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticBooleanMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jboolean JNICALL CallStaticBooleanMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticBooleanMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jbyte JNICALL CallStaticByteMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticByteMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jbyte JNICALL CallStaticByteMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticByteMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jbyte JNICALL CallStaticByteMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticByteMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jchar JNICALL CallStaticCharMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticCharMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jchar JNICALL CallStaticCharMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticCharMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jchar JNICALL CallStaticCharMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticCharMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jshort JNICALL CallStaticShortMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticShortMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jshort JNICALL CallStaticShortMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticShortMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jshort JNICALL CallStaticShortMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticShortMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jint JNICALL CallStaticIntMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticIntMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jint JNICALL CallStaticIntMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticIntMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jint JNICALL CallStaticIntMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticIntMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jlong JNICALL CallStaticLongMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticLongMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jlong JNICALL CallStaticLongMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticLongMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jlong JNICALL CallStaticLongMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticLongMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jfloat JNICALL CallStaticFloatMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticFloatMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jfloat JNICALL CallStaticFloatMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticFloatMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jfloat JNICALL CallStaticFloatMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticFloatMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static jdouble JNICALL CallStaticDoubleMethod (JNIEnv *env, jclass clazz, jmethodID methodID, ...) { printf ("JNI Function CallStaticDoubleMethod is not implemented.\n"); g_assert_not_reached (); return 0; }
static jdouble JNICALL CallStaticDoubleMethodV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticDoubleMethodV is not implemented.\n"); g_assert_not_reached (); return 0; }
static jdouble JNICALL CallStaticDoubleMethodA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args) { printf ("JNI Function CallStaticDoubleMethodA is not implemented.\n"); g_assert_not_reached (); return 0; }

static void JNICALL CallStaticVoidMethod (JNIEnv *env, jclass cls, jmethodID methodID, ...) { printf ("JNI Function CallStaticVoidMethod is not implemented.\n"); g_assert_not_reached (); }
static void JNICALL CallStaticVoidMethodV (JNIEnv *env, jclass cls, jmethodID methodID, va_list args) { printf ("JNI Function CallStaticVoidMethodV is not implemented.\n"); g_assert_not_reached (); }
static void JNICALL CallStaticVoidMethodA (JNIEnv *env, jclass cls, jmethodID methodID, jvalue * args) { printf ("JNI Function CallStaticVoidMethodA is not implemented.\n"); g_assert_not_reached (); }

static jstring JNICALL NewString (JNIEnv *env, const jchar *unicode, jsize len) { printf ("JNI Function NewString is not implemented.\n"); g_assert_not_reached (); return 0; }
static jsize JNICALL GetStringLength (JNIEnv *env, jstring str) { printf ("JNI Function GetStringLength is not implemented.\n"); g_assert_not_reached (); return 0; }
static const jchar *JNICALL GetStringChars (JNIEnv *env, jstring str, jboolean *isCopy) { printf ("JNI Function GetStringChars is not implemented.\n"); g_assert_not_reached (); return 0; }
static void JNICALL ReleaseStringChars (JNIEnv *env, jstring str, const jchar *chars) { printf ("JNI Function ReleaseStringChars is not implemented.\n"); g_assert_not_reached (); }

static jstring JNICALL NewStringUTF (JNIEnv *env, const char *utf)
{
	return (jstring)jniFuncs.MakeLocalRef (env, StringFromUTF8 (utf));
}

static jsize JNICALL GetStringUTFLength (JNIEnv *env, jstring str) { printf ("JNI Function GetStringUTFLength is not implemented.\n"); g_assert_not_reached (); return 0; }

static const char* JNICALL GetStringUTFChars (JNIEnv *env, jstring str, jboolean *isCopy)
{
	MonoString *s;
	char *buf;
	int i, j, e;

	s = jniFuncs.UnwrapRef (env, str);
	buf = g_malloc (mono_string_length (s) * 3 + 1);

	j = 0;
	for(i = 0, e = mono_string_length (s); i < e; i++)
	{
		jchar ch = mono_string_chars (s)[i];
		if ((ch != 0) && (ch <=0x7f))
		{
			buf[j++] = (char)ch;
		}
		else if (ch <= 0x7FF)
		{
			/* 11 bits or less. */
			unsigned char high_five = ch >> 6;
			unsigned char low_six = ch & 0x3F;
			buf[j++] = high_five | 0xC0; /* 110xxxxx */
			buf[j++] = low_six | 0x80;   /* 10xxxxxx */
		}
		else
		{
			/* possibly full 16 bits. */
			char high_four = ch >> 12;
			char mid_six = (ch >> 6) & 0x3F;
			char low_six = ch & 0x3f;
			buf[j++] = high_four | 0xE0; /* 1110xxxx */
			buf[j++] = mid_six | 0x80;   /* 10xxxxxx */
			buf[j++] = low_six | 0x80;   /* 10xxxxxx*/
		}
	}
	buf[j] = 0;
	if(isCopy)
	{
		*isCopy = JNI_TRUE;
	}

	return buf;
}
	
static void JNICALL ReleaseStringUTFChars (JNIEnv *env, jstring str, const char* chars)
{
	g_free ((char*)chars);
}

static jsize JNICALL GetArrayLength (JNIEnv *env, jarray array)
{
	MonoArray *arr = jniFuncs.UnwrapRef (env, array);
	return mono_array_length (arr);
}

static jobject JNICALL GetObjectArrayElement (JNIEnv *env, jobjectArray array, jsize index) { printf ("JNI Function GetObjectArrayElement is not implemented.\n"); g_assert_not_reached (); return 0; }
static void JNICALL SetObjectArrayElement (JNIEnv *env, jobjectArray array, jsize index, jobject val) { printf ("JNI Function SetObjectArrayElement is not implemented.\n"); g_assert_not_reached (); }

static int new_java_array (JNIEnv *env, MonoClass *eclass, jsize len)
{
	return jniFuncs.MakeLocalRef (env, mono_array_new (mono_domain_get (), eclass, len));
}	

static jobjectArray JNICALL NewObjectArray (JNIEnv *env, jsize len, jclass clazz, jobject init) { printf ("JNI Function NewObjectArray is not implemented.\n"); g_assert_not_reached (); return 0; }

static jbooleanArray JNICALL NewBooleanArray (JNIEnv *env, jsize len)
{
	return (jbooleanArray)new_java_array (env, mono_get_boolean_class (), len);
}	

static jbyteArray JNICALL NewByteArray (JNIEnv *env, jsize len)
{
	return (jbyteArray)new_java_array (env, mono_get_sbyte_class (), len);
}
	
static jcharArray JNICALL NewCharArray (JNIEnv *env, jsize len)
{
	return (jcharArray)new_java_array (env, mono_get_char_class (), len);
}
	
static jshortArray JNICALL NewShortArray (JNIEnv *env, jsize len)
{
	return (jshortArray)new_java_array (env, mono_get_int16_class (), len);
}	

static jintArray JNICALL NewIntArray (JNIEnv *env, jsize len)
{
	return (jintArray)new_java_array (env, mono_get_int32_class (), len);
}

static jlongArray JNICALL NewLongArray (JNIEnv *env, jsize len)
{
	return (jlongArray)new_java_array (env, mono_get_int64_class (), len);
}

static jfloatArray JNICALL NewFloatArray (JNIEnv *env, jsize len)
{
	return (jfloatArray)new_java_array (env, mono_get_single_class (), len);
}

static jdoubleArray JNICALL NewDoubleArray (JNIEnv *env, jsize len)
{
	return (jdoubleArray)new_java_array (env, mono_get_double_class (), len);
}

/* Original version with copy */
#if 0
#define GET_SET_ARRAY_ELEMENTS(Type,type,cpptype) \
static type* JNICALL Get##Type##ArrayElements(JNIEnv *env, type##Array array, jboolean *isCopy)\
{\
	int i; \
	MonoArray *obj; \
    type *res; \
\
	obj = jniFuncs.UnwrapRef ((void*)array); \
	res = g_new (type, mono_array_length (obj) + 1); \
	for (i = 0; i < mono_array_length (obj); ++i) { \
		res [i] = mono_array_get (obj, cpptype, i); \
	} \
\
	if (isCopy) \
		*isCopy = JNI_TRUE; \
	return res; \
} \
\
static void JNICALL Release##Type##ArrayElements(JNIEnv *env, type##Array array, type *elems, jint mode)\
{\
	int i; \
	MonoArray *obj; \
    type *res; \
\
	obj = jniFuncs.UnwrapRef ((void*)array); \
	if(mode == 0)\
	{\
	    for (i = 0; i < mono_array_length (obj); ++i) \
		    mono_array_get (obj, cpptype, i) = elems [i]; \
        g_free (elems); \
	}\
	else if(mode == JNI_COMMIT)\
	{\
	    for (i = 0; i < mono_array_length (obj); ++i) \
		    mono_array_get (obj, cpptype, i) = elems [i]; \
	}\
	else if(mode == JNI_ABORT)\
	{\
        g_free (elems);\
	}\
}
#endif


/*
 * Fast version with no copy. Works because the mono garbage collector is
 * non-copying.
 */
#define GET_SET_ARRAY_ELEMENTS(Type,type,cpptype) \
static type* JNICALL Get##Type##ArrayElements(JNIEnv *env, type##Array array, jboolean *isCopy)\
{\
	MonoArray *obj; \
\
	obj = jniFuncs.UnwrapRef (env, (void*)array); \
    if (isCopy) \
      *isCopy = JNI_FALSE;\
    return (type*)mono_array_addr (obj, cpptype, 0); \
} \
\
static void JNICALL Release##Type##ArrayElements(JNIEnv *env, type##Array array, type *elems, jint mode)\
{\
    return; \
} \
static void JNICALL Get##Type##ArrayRegion (JNIEnv *env, type##Array array, jsize start, jsize l, type *buf) \
{\
    MonoArray *obj; \
	obj = jniFuncs.UnwrapRef (env, (void*)array); \
    memcpy (buf, mono_array_addr (obj, type, start), (sizeof (type) * l)); \
} \
static void JNICALL Set##Type##ArrayRegion (JNIEnv *env, type##Array array, jsize start, jsize l, type *buf) \
{ \
    MonoArray *obj; \
	obj = jniFuncs.UnwrapRef (env, (void*)array); \
    memcpy (mono_array_addr (obj, type, start), buf, (sizeof (type) * l)); \
}

GET_SET_ARRAY_ELEMENTS(Boolean,jboolean,gboolean)
GET_SET_ARRAY_ELEMENTS(Byte,jbyte,gchar)
GET_SET_ARRAY_ELEMENTS(Char,jchar,gunichar2)
GET_SET_ARRAY_ELEMENTS(Short,jshort,short)
GET_SET_ARRAY_ELEMENTS(Int,jint,int)
GET_SET_ARRAY_ELEMENTS(Long,jlong,glong)
GET_SET_ARRAY_ELEMENTS(Float,jfloat,float)
GET_SET_ARRAY_ELEMENTS(Double,jdouble,double)

static void * JNICALL GetPrimitiveArrayCritical (JNIEnv *env, jarray array, jboolean *isCopy) {
	MonoArray *obj;

	obj = jniFuncs.UnwrapRef (env, (void*)array);
    if (isCopy)
      *isCopy = JNI_FALSE;
    return mono_array_addr (obj, void*, 0);
}

static void JNICALL ReleasePrimitiveArrayCritical (JNIEnv *env, jarray array, void *carray, jint mode) {
}

static const jchar * JNICALL GetStringCritical (JNIEnv *env, jstring string, jboolean *isCopy) {
	MonoString *obj;

	obj = jniFuncs.UnwrapRef (env, (void*)string);

	if (isCopy)
		*isCopy = JNI_FALSE;

	return mono_string_chars (obj);
}

static void JNICALL ReleaseStringCritical (JNIEnv *env, jstring string, const jchar *cstring)
{
}

static jobject JNICALL NewObjectA (JNIEnv *env, jclass clazz, jmethodID methodID, jvalue *args)
{
	return (jobject)jniFuncs.MakeLocalRef (env, InvokeHelper (env, NULL, methodID, args));
}

static jobject JNICALL NewObjectV (JNIEnv *env, jclass clazz, jmethodID methodID, va_list args)
{
	char sig[257];
	int i;
	jvalue *argarray;
	int argc;

	argc = GetMethodArgs(methodID, sig);
	argarray = (jvalue*)alloca(argc * sizeof(jvalue));
	for(i = 0; i < argc; i++)
	{
		switch(sig[i])
		{
		case 'Z':
		case 'B':
		case 'S':
		case 'C':
		case 'I':
			argarray[i].i = va_arg(args, int);
			break;
		case 'J':
			argarray[i].j = va_arg(args, gint64);
			break;
		case 'L':
			argarray[i].l = va_arg(args, jobject);
			break;
		case 'D':
			argarray[i].d = va_arg(args, double);
			break;
		case 'F':
			argarray[i].f = (float)va_arg(args, double);
			break;
		}
	}

	return NewObjectA (env, clazz, methodID, argarray);
}

static jobject JNICALL NewObject (JNIEnv *env, jclass clazz, jmethodID methodID, ...)
{
	va_list args;
	jobject o;
	va_start(args, methodID);
	o = NewObjectV(env, clazz, methodID, args);
	va_end(args);
	return o;
}

static jint JNICALL RegisterNatives (JNIEnv *env, jclass clazz, const JNINativeMethod *methods,       jint nMethods) { printf ("JNI Function RegisterNatives is not implemented.\n"); g_assert_not_reached (); return 0; }
static jint JNICALL UnregisterNatives (JNIEnv *env, jclass clazz) { printf ("JNI Function UnregisterNatives is not implemented.\n"); g_assert_not_reached (); return 0; }

static jint JNICALL MonitorEnter (JNIEnv *env, jobject obj) { printf ("JNI Function MonitorEnter is not implemented.\n"); g_assert_not_reached (); return 0; }
static jint JNICALL MonitorExit (JNIEnv *env, jobject obj) { printf ("JNI Function MonitorExit is not implemented.\n"); g_assert_not_reached (); return 0; }

jint JNICALL GetJavaVM (JNIEnv *env, JavaVM **vm)
{
	if (!vm_ptr)
		vm_ptr = (void***)&vm_func_table;

	*vm = (JavaVM*)&vm_ptr;

	return JNI_OK;
}

static void JNICALL GetStringRegion (JNIEnv *env, jstring str, jsize start, jsize len, jchar *buf) { printf ("JNI Function GetStringRegion is not implemented.\n"); g_assert_not_reached (); }
static void JNICALL GetStringUTFRegion (JNIEnv *env, jstring str, jsize start, jsize len, char *buf) { printf ("JNI Function GetStringUTFRegion is not implemented.\n"); g_assert_not_reached (); }

static jweak JNICALL NewWeakGlobalRef (JNIEnv *env, jobject obj) { printf ("JNI Function NewWeakGlobalRef is not implemented.\n"); g_assert_not_reached (); return 0; }
static void JNICALL DeleteWeakGlobalRef (JNIEnv *env, jweak ref) { printf ("JNI Function DeleteWeakGlobalRef is not implemented.\n"); g_assert_not_reached (); }

static jboolean JNICALL ExceptionCheck (JNIEnv *env) { 
	return jniFuncs.ExceptionCheck (env);
}

static jobject JNICALL NewDirectByteBuffer (JNIEnv* env, void* address, jlong capacity) { printf ("JNI Function NewDirectByteBuffer is not implemented.\n"); g_assert_not_reached (); return 0; }
static void* JNICALL GetDirectBufferAddress (JNIEnv* env, jobject buf) { printf ("JNI Function GetDirectBufferAddress is not implemented.\n"); g_assert_not_reached (); return 0; }
static jlong JNICALL GetDirectBufferCapacity (JNIEnv* env, jobject buf) { printf ("JNI Function GetDirectBufferCapacity is not implemented.\n"); g_assert_not_reached (); return 0; }


/***************************************************************************/
/*                         VM FUNCTIONS                                    */
/***************************************************************************/

static jint DestroyJavaVM (void *vm)
{
	g_assert_not_reached ();
	return 0;
}

static jint AttachCurrentThread (void *vm, void **penv, void *args)
{
	g_assert_not_reached ();
	return 0;
}

static jint DetachCurrentThread (void *vm)
{
	g_assert_not_reached ();
	return 0;
}

static jint GetEnv (void *vm, void **penv, jint version)
{
	void *env = jniFuncs.GetJniEnv ();
	if (env) {
		*penv = env;
		return JNI_OK;
	}
	else {
		*penv = NULL;
		return JNI_EDETACHED;
	}
}

static jint AttachCurrentThreadAsDaemon (void *vm, void **penv, void *args)
{
	g_assert_not_reached ();
	return 0;
}



/*****************************************************************************/

static void *jni_func_table[256] = {
	NULL,
	NULL,
	NULL,
	NULL,
	(void*)&GetVersion,
	(void*)&DefineClass,
	(void*)&FindClass,
	(void*)&FromReflectedMethod,
	(void*)&FromReflectedField,
	(void*)&ToReflectedMethod,
	(void*)&GetSuperclass,
	(void*)&IsAssignableFrom,
	(void*)&ToReflectedField,
	(void*)&Throw,
	(void*)&ThrowNew,
	(void*)&ExceptionOccurred,
	(void*)&ExceptionDescribe,
	(void*)&ExceptionClear,
	(void*)&FatalError,
	(void*)&PushLocalFrame,
	(void*)&PopLocalFrame,
	(void*)&NewGlobalRef,
	(void*)&DeleteGlobalRef,
	(void*)&DeleteLocalRef,
	(void*)&IsSameObject,
	(void*)&NewLocalRef,
	(void*)&EnsureLocalCapacity,
	(void*)&AllocObject,
	(void*)&NewObject,
	(void*)&NewObjectV,
	(void*)&NewObjectA,
	(void*)&GetObjectClass,
	(void*)&IsInstanceOf,
	(void*)&GetMethodID,
	(void*)&CallObjectMethod,
	(void*)&CallObjectMethodV,
	(void*)&CallObjectMethodA,
	(void*)&CallBooleanMethod,
	(void*)&CallBooleanMethodV,
	(void*)&CallBooleanMethodA,
	(void*)&CallByteMethod,
	(void*)&CallByteMethodV,
	(void*)&CallByteMethodA,
	(void*)&CallCharMethod,
	(void*)&CallCharMethodV,
	(void*)&CallCharMethodA,
	(void*)&CallShortMethod,
	(void*)&CallShortMethodV,
	(void*)&CallShortMethodA,
	(void*)&CallIntMethod,
	(void*)&CallIntMethodV,
	(void*)&CallIntMethodA,
	(void*)&CallLongMethod,
	(void*)&CallLongMethodV,
	(void*)&CallLongMethodA,
	(void*)&CallFloatMethod,
	(void*)&CallFloatMethodV,
	(void*)&CallFloatMethodA,
	(void*)&CallDoubleMethod,
	(void*)&CallDoubleMethodV,
	(void*)&CallDoubleMethodA,
	(void*)&CallVoidMethod,
	(void*)&CallVoidMethodV,
	(void*)&CallVoidMethodA,
	(void*)&CallNonvirtualObjectMethod,
	(void*)&CallNonvirtualObjectMethodV,
	(void*)&CallNonvirtualObjectMethodA,
	(void*)&CallNonvirtualBooleanMethod,
	(void*)&CallNonvirtualBooleanMethodV,
	(void*)&CallNonvirtualBooleanMethodA,
	(void*)&CallNonvirtualByteMethod,
	(void*)&CallNonvirtualByteMethodV,
	(void*)&CallNonvirtualByteMethodA,
	(void*)&CallNonvirtualCharMethod,
	(void*)&CallNonvirtualCharMethodV,
	(void*)&CallNonvirtualCharMethodA,
	(void*)&CallNonvirtualShortMethod,
	(void*)&CallNonvirtualShortMethodV,
	(void*)&CallNonvirtualShortMethodA,
	(void*)&CallNonvirtualIntMethod,
	(void*)&CallNonvirtualIntMethodV,
	(void*)&CallNonvirtualIntMethodA,
	(void*)&CallNonvirtualLongMethod,
	(void*)&CallNonvirtualLongMethodV,
	(void*)&CallNonvirtualLongMethodA,
	(void*)&CallNonvirtualFloatMethod,
	(void*)&CallNonvirtualFloatMethodV,
	(void*)&CallNonvirtualFloatMethodA,
	(void*)&CallNonvirtualDoubleMethod,
	(void*)&CallNonvirtualDoubleMethodV,
	(void*)&CallNonvirtualDoubleMethodA,
	(void*)&CallNonvirtualVoidMethod,
	(void*)&CallNonvirtualVoidMethodV,
	(void*)&CallNonvirtualVoidMethodA,
	(void*)&GetFieldID,
	(void*)&GetObjectField,
	(void*)&GetBooleanField,
	(void*)&GetByteField,
	(void*)&GetCharField,
	(void*)&GetShortField,
	(void*)&GetIntField,
	(void*)&GetLongField,
	(void*)&GetFloatField,
	(void*)&GetDoubleField,
	(void*)&SetObjectField,
	(void*)&SetBooleanField,
	(void*)&SetByteField,
	(void*)&SetCharField,
	(void*)&SetShortField,
	(void*)&SetIntField,
	(void*)&SetLongField,
	(void*)&SetFloatField,
	(void*)&SetDoubleField,
	(void*)&GetStaticMethodID,
	(void*)&CallStaticObjectMethod,
	(void*)&CallStaticObjectMethodV,
	(void*)&CallStaticObjectMethodA,
	(void*)&CallStaticBooleanMethod,
	(void*)&CallStaticBooleanMethodV,
	(void*)&CallStaticBooleanMethodA,
	(void*)&CallStaticByteMethod,
	(void*)&CallStaticByteMethodV,
	(void*)&CallStaticByteMethodA,
	(void*)&CallStaticCharMethod,
	(void*)&CallStaticCharMethodV,
	(void*)&CallStaticCharMethodA,
	(void*)&CallStaticShortMethod,
	(void*)&CallStaticShortMethodV,
	(void*)&CallStaticShortMethodA,
	(void*)&CallStaticIntMethod,
	(void*)&CallStaticIntMethodV,
	(void*)&CallStaticIntMethodA,
	(void*)&CallStaticLongMethod,
	(void*)&CallStaticLongMethodV,
	(void*)&CallStaticLongMethodA,
	(void*)&CallStaticFloatMethod,
	(void*)&CallStaticFloatMethodV,
	(void*)&CallStaticFloatMethodA,
	(void*)&CallStaticDoubleMethod,
	(void*)&CallStaticDoubleMethodV,
	(void*)&CallStaticDoubleMethodA,
	(void*)&CallStaticVoidMethod,
	(void*)&CallStaticVoidMethodV,
	(void*)&CallStaticVoidMethodA,
	(void*)&GetStaticFieldID,
	(void*)&GetStaticObjectField,
	(void*)&GetStaticBooleanField,
	(void*)&GetStaticByteField,
	(void*)&GetStaticCharField,
	(void*)&GetStaticShortField,
	(void*)&GetStaticIntField,
	(void*)&GetStaticLongField,
	(void*)&GetStaticFloatField,
	(void*)&GetStaticDoubleField,
	(void*)&SetStaticObjectField,
	(void*)&SetStaticBooleanField,
	(void*)&SetStaticByteField,
	(void*)&SetStaticCharField,
	(void*)&SetStaticShortField,
	(void*)&SetStaticIntField,
	(void*)&SetStaticLongField,
	(void*)&SetStaticFloatField,
	(void*)&SetStaticDoubleField,
	(void*)&NewString,
	(void*)&GetStringLength,
	(void*)&GetStringChars,
	(void*)&ReleaseStringChars,
	(void*)&NewStringUTF,
	(void*)&GetStringUTFLength,
	(void*)&GetStringUTFChars,
	(void*)&ReleaseStringUTFChars,
	(void*)&GetArrayLength,
	(void*)&NewObjectArray,
	(void*)&GetObjectArrayElement,
	(void*)&SetObjectArrayElement,
	(void*)&NewBooleanArray,
	(void*)&NewByteArray,
	(void*)&NewCharArray,
	(void*)&NewShortArray,
	(void*)&NewIntArray,
	(void*)&NewLongArray,
	(void*)&NewFloatArray,
	(void*)&NewDoubleArray,
	(void*)&GetBooleanArrayElements,
	(void*)&GetByteArrayElements,
	(void*)&GetCharArrayElements,
	(void*)&GetShortArrayElements,
	(void*)&GetIntArrayElements,
	(void*)&GetLongArrayElements,
	(void*)&GetFloatArrayElements,
	(void*)&GetDoubleArrayElements,
	(void*)&ReleaseBooleanArrayElements,
	(void*)&ReleaseByteArrayElements,
	(void*)&ReleaseCharArrayElements,
	(void*)&ReleaseShortArrayElements,
	(void*)&ReleaseIntArrayElements,
	(void*)&ReleaseLongArrayElements,
	(void*)&ReleaseFloatArrayElements,
	(void*)&ReleaseDoubleArrayElements,
	(void*)&GetBooleanArrayRegion,
	(void*)&GetByteArrayRegion,
	(void*)&GetCharArrayRegion,
	(void*)&GetShortArrayRegion,
	(void*)&GetIntArrayRegion,
	(void*)&GetLongArrayRegion,
	(void*)&GetFloatArrayRegion,
	(void*)&GetDoubleArrayRegion,
	(void*)&SetBooleanArrayRegion,
	(void*)&SetByteArrayRegion,
	(void*)&SetCharArrayRegion,
	(void*)&SetShortArrayRegion,
	(void*)&SetIntArrayRegion,
	(void*)&SetLongArrayRegion,
	(void*)&SetFloatArrayRegion,
	(void*)&SetDoubleArrayRegion,
	(void*)&RegisterNatives,
	(void*)&UnregisterNatives,
	(void*)&MonitorEnter,
	(void*)&MonitorExit,
	(void*)&GetJavaVM,
	(void*)&GetStringRegion,
	(void*)&GetStringUTFRegion,
	(void*)&GetPrimitiveArrayCritical,
	(void*)&ReleasePrimitiveArrayCritical,
	(void*)&GetStringCritical,
	(void*)&ReleaseStringCritical,
	(void*)&NewWeakGlobalRef,
	(void*)&DeleteWeakGlobalRef,
	(void*)&ExceptionCheck,
	(void*)&NewDirectByteBuffer,
	(void*)&GetDirectBufferAddress,
	(void*)&GetDirectBufferCapacity
};

static void *vm_func_table[64] = {
	NULL,
	NULL,
	NULL,
	(void*)&DestroyJavaVM,
	(void*)&AttachCurrentThread,
	(void*)&DetachCurrentThread,
	(void*)&GetEnv,
	(void*)&AttachCurrentThreadAsDaemon
};

