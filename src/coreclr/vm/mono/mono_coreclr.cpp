#include "common.h"

#include "MonoCoreClr.h"
#include <coreclrhost.h>

#include "../../gc/gcscan.h"
#include "../../gc/objecthandle.h"
#include "assembly.hpp"
#include "assemblynative.hpp"
#include "caparser.h"
#include "ecall.h"
#include "mscoree.h"
#include "stringliteralmap.h"
#include "threads.h"
#include "threadsuspend.h"
#include "typeparse.h"
#include "typestring.h"
#include "profilepriv.h"

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

// we only need domain reload for Editor
// #define UNITY_SUPPORT_DOMAIN_UNLOAD 1

// Match the behavior of Unity.  We need to flush immediately because this is used to log stack traces
// on the managed side of the embedding api before we call Environment.Exit.  If we were to let the msg be buffered we may not
// always see the stack trace before the application exits.
#ifdef WIN32
int __cdecl print_and_flush(const char* msg, va_list args)
#else
int print_and_flush(const char* msg, va_list args)
#endif
{
    auto result = vprintf(msg, args);
    printf("\n");
    fflush(stdout);
    return result;
}

static vprintf_func our_vprintf = print_and_flush;

void unity_log(const char *format, ...)
{
    va_list args;
    va_start (args, format);
    our_vprintf (format, args);
    va_end (args);
    our_vprintf ("\n", nullptr);
}

#ifdef WIN32
#define EXPORT_API __declspec(dllexport)
#define EXPORT_CC __cdecl
#define PATH_SEPARATOR ';'
#else
#define EXPORT_API __attribute__((visibility("default")))
#define EXPORT_CC
#define PATH_SEPARATOR ':'
#endif

//#define TRACE_API(format,...) { printf("%s (" format ")\n", __func__, __VA_ARGS__); fflush(stdout); }
#define TRACE_API(format,...)

void* g_CLRRuntimeHost;
unsigned int g_RootDomainId;

typedef intptr_t ManagedStringPtr_t;

struct HostStructNative
{
    void (*unity_log)(const char *format);
    gboolean (*return_handles_from_api)();
};
HostStructNative* g_HostStructNative;

//MonoImage *gCoreCLRHelperAssembly;
//MonoClass* gALCWrapperClass;
//MonoObject* gALCWrapperObject;
//MonoMethod* gALCWrapperLoadFromAssemblyPathMethod;
//MonoMethod* gALCWrapperLoadFromAssemblyDataMethod;
//MonoMethod* gALCWrapperDomainUnloadNotificationMethod;
//MonoMethod* gALCWrapperInitUnloadMethod;
//MonoMethod* gALCWrapperFinishUnloadMethod;
//MonoMethod* gALCWrapperCheckRootForUnloadingMethod;
//MonoMethod* gALCWrapperCheckAssemblyForUnloadingMethod;
//MonoMethod* gALCWrapperAddPathMethod;

thread_local MonoDomain *gCurrentDomain = NULL;
MonoDomain *gRootDomain = NULL;
EXTERN_C IMAGE_DOS_HEADER __ImageBase;

static SString* s_AssemblyPaths;

#define ASSERT_NOT_IMPLEMENTED printf("Function not implemented: %s\n", __func__);

#define FIELD_ATTRIBUTE_PRIVATE               0x0001
#define FIELD_ATTRIBUTE_FAMILY                0x0004
#define FIELD_ATTRIBUTE_PUBLIC                0x0006

thread_local int g_isManaged = 0;

typedef Assembly MonoAssembly_clr;
typedef Assembly MonoImage_clr;
typedef Object MonoObject_clr;
typedef FieldDesc MonoClassField_clr; // struct MonoClassField;
typedef MethodTable MonoClass_clr; //struct MonoClass;
typedef AppDomain MonoDomain_clr; //struct MonoDomain;
typedef MethodDesc MonoMethod_clr;
typedef OBJECTREF MonoObjectRef_clr;
typedef TypeHandle MonoType_clr;
typedef ArrayBase MonoArray_clr;
typedef Thread MonoThread_clr;
typedef MethodDesc MonoMethodSignature_clr;

static inline MonoType_clr MonoType_clr_from_MonoType(MonoType* type)
{
    return MonoType_clr::FromPtr(type);
}

static inline MonoType* MonoType_clr_to_MonoType(MonoType_clr type)
{
    return (MonoType*)type.AsPtr();
}

static void get_dirname(char* source)
{
    for (size_t i = strlen(source) - 1; i >= 0; i--)
    {
        if (source[i] == '/' || source[i] == '\\')
        {
            source[i + 1] = '\0';
            return;
        }
    }
}

extern "C" EXPORT_API int EXPORT_CC mono_array_element_size(MonoClass* classOfArray)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(classOfArray != NULL);
    }
    CONTRACTL_END;

    return reinterpret_cast<MonoClass_clr*>(classOfArray)->GetArrayElementTypeHandle().GetSize();
}

extern "C" EXPORT_API MonoImage* EXPORT_CC mono_assembly_get_image(MonoAssembly *assembly)
{
    TRACE_API("%p", assembly);
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(assembly != NULL);
    }
    CONTRACTL_END;

    // Assume for now that Assembly == Image
    return (MonoImage*)assembly;
}

extern "C" EXPORT_API gint32 EXPORT_CC mono_class_array_element_size(MonoClass *ac)
{
    CONTRACTL{
        STANDARD_VM_CHECK;
        PRECONDITION(ac != nullptr);
    } CONTRACTL_END;
    auto ac_clr = (MonoClass_clr*)ac;

    // TODO: Is it really the method to use?
    DWORD s = ac_clr->IsValueType() ? ac_clr->GetNumInstanceFieldBytes() : sizeof(void*);// ac_clr->GetBaseSize();
    return s;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_from_mono_type(MonoType *image)
{
    MonoClass_clr* klass = MonoType_clr_from_MonoType(image).GetMethodTable();
    return (MonoClass*)klass;
}

// remove once usages in this file are removed
static MonoClass* mono_class_get_element_class(MonoClass *klass)
{
    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    return (MonoClass*)reinterpret_cast<MonoClass_clr*>(klass)->GetArrayElementTypeHandle().GetMethodTable();
}

extern "C" EXPORT_API MonoMethod* EXPORT_CC mono_class_get_methods(MonoClass* klass, gpointer *iter)
{
    TRACE_API("%p, %p", klass, iter);

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    if (!iter)
    {
        return NULL;
    }

    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;

    MethodTable::IntroducedMethodIterator* iterator = (MethodTable::IntroducedMethodIterator*)*iter;
    if (iterator == NULL)
    {
        // TODO: Using the option FALSE to iterate methods through a non-canonical type.
        // Not sure exactly what does this mean
        iterator = new MethodTable::IntroducedMethodIterator(klass_clr, 0);
        *iter = iterator;
    }

    if (!iterator->IsValid())
    {
        *iter = NULL;
        delete iterator;
        return NULL;
    }

    auto method = iterator->GetMethodDesc();
    method->EnsureActive();
    iterator->Next();
    return (MonoMethod*)method;
}

extern "C" EXPORT_API const char* EXPORT_CC mono_class_get_name(MonoClass *klass)
{
	MonoClass_clr* clazz = (MonoClass_clr*)klass;
    if (clazz->IsArray())
    {
        const char *elementName = mono_class_get_name(mono_class_get_element_class(klass));
        int rank = clazz->GetRank();
        SString arrayName(SString::Utf8, elementName);
        arrayName += '[';
        for (int i=0; i<rank-1; i++)
            arrayName += ',';
        arrayName += ']';
        static char buf[512] = {0};
        strcpy(buf, arrayName.GetUTF8());
        return buf;
    }

	LPCUTF8 name, namespaze;
	clazz->GetMDImport()->GetNameOfTypeDef(clazz->GetCl(), &name, &namespaze);

    if (name)
        return name;

    DefineFullyQualifiedNameForClass();
    name = GetFullyQualifiedNameForClass(clazz);
    return name;
}

extern "C" EXPORT_API const char* EXPORT_CC mono_class_get_namespace(MonoClass *klass)
{
	MonoClass_clr* clazz = (MonoClass_clr*)klass;
	LPCUTF8 name, namespaze;
	clazz->GetMDImport()->GetNameOfTypeDef(clazz->GetCl(), &name, &namespaze);
    return namespaze;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_get_parent(MonoClass *klass)
{
    MonoClass_clr* parent = reinterpret_cast<MonoClass_clr*>(klass)->GetParentMethodTable();
    return (MonoClass*)parent;
}

extern "C" EXPORT_API MonoProperty* EXPORT_CC mono_class_get_property_from_name(MonoClass *klass, const char *name)
{
    // CoreCLR does not have easy support for iterating on properties on a MethodTable.
    // So instead, we look for the property's "get" method. This will not work for set-only
    // properties, but is sufficient for our needs for now.
    return (MonoProperty*)MemberLoader::FindPropertyMethod((MonoClass_clr*)klass, name, PropertyGet);
}

extern "C" EXPORT_API MonoType* EXPORT_CC mono_class_get_type(MonoClass *klass)
{
    TypeHandle h(reinterpret_cast<MonoClass_clr*>(klass));
    return (MonoType*)h.AsPtr();
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_class_get_type_token(MonoClass *klass)
{
    return (guint32)reinterpret_cast<MonoClass_clr*>(klass)->GetCl();
}

extern "C" EXPORT_API void* EXPORT_CC mono_class_get_userdata(MonoClass* klass)
{
    TRACE_API("%p", klass);

    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    return ((MonoClass_clr*)klass)->m_pUserData;
}

extern "C" EXPORT_API int EXPORT_CC mono_class_get_userdata_offset()
{
    //TRACE_API("", NULL);

    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return offsetof(MethodTable, m_pUserData);
}

extern "C" EXPORT_API gint32 EXPORT_CC mono_class_instance_size(MonoClass *klass)
{
    return (guint32)reinterpret_cast<MonoClass_clr*>(klass)->GetNumInstanceFieldBytes() + sizeof(void*);
}

extern "C" EXPORT_API void EXPORT_CC mono_class_set_userdata(MonoClass* klass, void* userdata)
{
    TRACE_API("%p, %p", klass, userdata);

    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    ((MonoClass_clr*)klass)->m_pUserData = userdata;
}

typedef void (*MonoDebuggerAttachFunc)(gboolean attached);
extern "C" EXPORT_API void EXPORT_CC mono_debugger_install_attach_detach_callback (MonoDebuggerAttachFunc func)
{
}

extern "C" EXPORT_API void EXPORT_CC mono_debugger_set_generate_debug_info(gboolean enable)
{
}

extern "C" EXPORT_API const char* EXPORT_CC mono_field_get_name(MonoClassField *field)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END
    auto field_clr = (MonoClassField_clr*)field;
    return field_clr->GetName();
}

extern "C" EXPORT_API int EXPORT_CC mono_field_get_offset(MonoClassField *field)
{
    TRACE_API("%p", field);

    auto field_clr = (MonoClassField_clr*)field;
    if (field_clr->IsStatic())
    {
        return 0;
    }

    auto result = field_clr->GetOffset();
    result += sizeof(Object);

    return result;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_field_get_parent(MonoClassField *field)
{
    FieldDesc* fieldDesc = (FieldDesc*)field;
    return (MonoClass*)fieldDesc->GetApproxEnclosingMethodTable();
}

static inline OBJECTHANDLE handle_from_uintptr(uintptr_t p)
{
    // mask off bit that is set for pinned in managed
    p &= (~(uintptr_t)1);
    return (OBJECTHANDLE)p;
}

static inline uintptr_t handle_to_uintptr(OBJECTHANDLE h, bool pinned)
{
    uintptr_t p = (uintptr_t)h;
    // managed code expects lowest bit set for pinned handles
    if (pinned)
        p |= (uintptr_t)1;
    return p;
}

// The embedding api has moved to managed, however, there is still a usage by another native embedding api that will need to be moved to managed
// before this can be removed
MonoClass* mono_get_object_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__OBJECT);
}

static ASSEMBLYREF ASSEMBLYREF_From_AssemblyIntPtrHandle(MonoImage* assemblyIntPtrHandle)
{
    OBJECTHANDLE assemblyHandle = handle_from_uintptr((uintptr_t)assemblyIntPtrHandle);
    OBJECTREF assemblyObjectRef = ObjectFromHandle(assemblyHandle);
    return ASSEMBLYREF(assemblyObjectRef);
}

extern "C" EXPORT_API int32_t EXPORT_CC mono_image_get_table_rows(MonoImage* assemblyIntPtrHandle, int32_t table_token)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();

    ASSEMBLYREF assemblyRef = ASSEMBLYREF_From_AssemblyIntPtrHandle(assemblyIntPtrHandle);
    return assemblyRef->GetAssembly()->GetMDImport()->GetCountWithTokenKind(table_token);
}

extern "C" EXPORT_API void EXPORT_CC mono_assembly_get_assemblyref(MonoImage* assemblyIntPtrHandle, int32_t idx, MonoAssemblyName* aname)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();

    ASSEMBLYREF assemblyRef = ASSEMBLYREF_From_AssemblyIntPtrHandle(assemblyIntPtrHandle);

    // Only extract the assembly name
    DWORD token = TokenFromRid(idx, mdtAssemblyRef);
    assemblyRef->GetAssembly()->GetMDImport()->GetAssemblyRefProps(token, NULL, NULL, &aname->name, NULL, NULL, NULL, NULL);
}

extern "C" EXPORT_API void EXPORT_CC coreclr_image_get_custom_attribute_data(MonoImage* assemblyIntPtrHandle, int idx, guint32* type_token, guint32* parent_type_token)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();

    ASSEMBLYREF assemblyRef = ASSEMBLYREF_From_AssemblyIntPtrHandle(assemblyIntPtrHandle);
    IMDInternalImport* mdImport = assemblyRef->GetAssembly()->GetMDImport();

    DWORD token = TokenFromRid(idx + 1, mdtCustomAttribute);
    *type_token = 0;
    *parent_type_token = 0;

    mdImport->GetCustomAttributeProps(token, type_token);
    mdImport->GetParentToken(token, parent_type_token);
}

typedef int32_t (*initialize_scripting_runtime_func)();
typedef void (*unity_log_func)(const char* format);

extern "C" EXPORT_API void EXPORT_CC coreclr_initialize_domain(void* runtimeHost, unsigned int rootDomainId)
{
    AppDomain *pCurDomain = SystemDomain::GetCurrentDomain();

    // Disable Windows message processing during waits
    // On Windows by default waits will processing some messages (COM, WM_PAINT, ...) leading to reentrancy issues
    pCurDomain->SetForceTrivialWaitOperations();

    gRootDomain = gCurrentDomain = (MonoDomain*)pCurDomain;

    g_CLRRuntimeHost = (ICLRRuntimeHost*)runtimeHost;
    g_RootDomainId = rootDomainId;
}

// This is a stop gap helper to assist with scripting core initializing itself until the entirety of coreclr initialization can be moved
// into scripting core.
extern "C" EXPORT_API void* EXPORT_CC unity_coreclr_create_delegate(const char* assemblyName, const char* typeName, const char* methodName)
{
    void* func;
    HRESULT hr = coreclr_create_delegate(g_CLRRuntimeHost, g_RootDomainId, assemblyName, typeName, methodName, (void**)&func);
    if(FAILED(hr))
    {
        return nullptr;
    }

    return (void*)func;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_metadata_signature_equal(MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
    if (mono_signature_get_param_count(sig1) != mono_signature_get_param_count(sig2))
        return FALSE;
    if (mono_signature_get_return_type(sig1) != mono_signature_get_return_type(sig2))
        return FALSE;
    if (mono_signature_is_instance(sig1) != mono_signature_is_instance(sig2))
        return FALSE;

    gpointer iter1 = NULL;
    gpointer iter2 = NULL;
    bool match = true;
    while (MonoType *paramType1 = mono_signature_get_params(sig1, &iter1))
    {
        MonoType *paramType2 = mono_signature_get_params(sig2, &iter2);
        if (paramType1 != paramType2)
            match = false;
    }
    return match;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_metadata_type_equal (MonoType * t1, MonoType * t2)
{
    MonoType_clr* type1 = (MonoType_clr*)t1;
    MonoType_clr* type2 = (MonoType_clr*)t2;
    return type1->IsEquivalentTo(*type2);
}

extern "C" EXPORT_API char* EXPORT_CC mono_method_full_name(MonoMethod* method, gboolean signature)
{
    auto methodclr = reinterpret_cast<MonoMethod_clr*>(method);
 	LPCUTF8 name, namespaze;
    auto mt = methodclr->GetMethodTable();
	mt->GetMDImport()->GetNameOfTypeDef(mt->GetCl(), &name, &namespaze);

    InlineSString<256> fullName(SString::Utf8);
    if (namespaze != NULL)
    {
        fullName += InlineSString<256>(SString::Utf8, namespaze);
        fullName += '.';
    }
    fullName += InlineSString<256>(SString::Utf8, name);
    fullName += ':';
    fullName +=  InlineSString<256>(SString::Utf8, methodclr->GetName());

    if (signature)
    {
        fullName += InlineSString<2>(SString::Utf8, " (");

        MonoMethodSignature* sig = mono_method_signature(method);
        gpointer iter = NULL;

        MonoType *paramType = mono_signature_get_params(sig, &iter);
        if (paramType)
        {
            fullName += InlineSString<256>(SString::Utf8, mono_type_get_name(paramType));
            while ((paramType = mono_signature_get_params(sig, &iter)))
            {
                fullName += ',';
                fullName += InlineSString<256>(SString::Utf8, mono_type_get_name(paramType));
            }
        }

        fullName += ')';
    }
    return _strdup(fullName.GetUTF8());
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_method_get_class(MonoMethod *method)
{
    auto method_clr = (MonoMethod_clr*)method;
    auto class_clr = (MonoClass_clr*)method_clr->GetClass()->GetMethodTable();
    return (MonoClass*)class_clr;
}

extern "C" EXPORT_API MonoMethod* EXPORT_CC mono_method_get_last_managed()
{
    return (MonoMethod*)(intptr_t)g_isManaged;
}

extern "C" EXPORT_API const char* EXPORT_CC mono_method_get_name(MonoMethod *method)
{
    return reinterpret_cast<MonoMethod_clr*>(method)->GetName();
}

extern "C" EXPORT_API MonoMethodSignature* EXPORT_CC mono_method_signature(MonoMethod *method)
{
    return (MonoMethodSignature*)method;
}

MonoObject* EXPORT_CC mono_runtime_invoke_with_nested_object(MonoMethod *method, void *obj, void *parentobj, void **params, MonoException **exc);

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_runtime_invoke(MonoMethod *method, void *obj, void **params, MonoException **exc)
{
    TRACE_API("%p, %p, %p, %p", method, obj, params, exc);

    if (obj == nullptr)
        return mono_runtime_invoke_with_nested_object(method, nullptr, nullptr, params, exc);
    MonoClass_clr * klass = (MonoClass_clr*)reinterpret_cast<MonoObject_clr*>(obj)->GetMethodTable();
    auto method_clr = (MonoMethod_clr*)method;
    if (klass->IsValueType())// && !method_clr->IsVtableMethod())
        return mono_runtime_invoke_with_nested_object(method, (char*)obj + sizeof(Object), obj, params, exc);
    else
        return mono_runtime_invoke_with_nested_object(method, obj, obj, params, exc);
}

MonoObject* EXPORT_CC mono_runtime_invoke_with_nested_object(MonoMethod *method, void *obj, void *parentobj, void **params, MonoException **exc)
{
    TRACE_API("%p, %p, %p, %p, %p", method, obj, parentobj, params, exc);

    GCX_COOP();

    auto method_clr = (MonoMethod_clr*)method;

    MetaSig     methodSig(method_clr);
    DWORD numArgs = methodSig.NumFixedArgs();
    ArgIterator argIt(&methodSig);

    const int MAX_ARG_SLOT = 128;
    ARG_SLOT argslots[MAX_ARG_SLOT];

    DWORD slotIndex = 0;
    if (methodSig.HasThis())
    {
        if (obj != parentobj && method_clr->IsVtableMethod())
            obj = (char*)obj - sizeof(Object) ;
        argslots[0] = PtrToArgSlot(obj);
        slotIndex++;
    }

    PVOID pRetBufStackCopy = NULL;
    auto retTH = methodSig.GetRetTypeHandleNT();
    CorElementType retType = retTH.GetInternalCorElementType();

    auto hasReturnBufferArg = argIt.HasRetBuffArg();
    if (hasReturnBufferArg)
    {
        SIZE_T sz = retTH.GetMethodTable()->GetNumInstanceFieldBytes();
        pRetBufStackCopy = _alloca(sz);
        memset(pRetBufStackCopy, 0, sz);
        argslots[slotIndex] = PtrToArgSlot(pRetBufStackCopy);
        slotIndex++;
    }

    for (DWORD argIndex = 0; argIndex < numArgs; argIndex++, slotIndex++)
    {
        int ofs = argIt.GetNextOffset();
        _ASSERTE(ofs != TransitionBlock::InvalidOffset);
        auto stackSize = argIt.GetArgSize();

        auto argTH = methodSig.GetLastTypeHandleNT();
        auto argType = argTH.GetInternalCorElementType();

        // TODO: Factorize ValueType detection and Managed detection
        switch (argType)
        {
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_BOOLEAN:      // boolean
        case ELEMENT_TYPE_I1:           // byte
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:           // short
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:         // char
        case ELEMENT_TYPE_I4:           // int
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:           // long
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:           // float
        case ELEMENT_TYPE_R8:           // double
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
            switch (stackSize)
            {
            case 1:
            case 2:
            case 4:
                argslots[slotIndex] = *(INT32*)params[argIndex];
                break;

            case 8:
                argslots[slotIndex] = *(INT64*)params[argIndex];
                break;

            default:
                if (stackSize > sizeof(ARG_SLOT))
                {
                    argslots[slotIndex] = PtrToArgSlot(params[argIndex]);
                }
                else
                {
                    CopyMemory(&argslots[slotIndex], params[argIndex], stackSize);
                }
                break;
            }
            break;
        case ELEMENT_TYPE_BYREF:
            argslots[slotIndex] = PtrToArgSlot(params[argIndex]);
            break;
        case ELEMENT_TYPE_PTR:
            argslots[slotIndex] = PtrToArgSlot(params[argIndex]);
            break;
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_VAR:
            argslots[slotIndex] = ObjToArgSlot(ObjectToOBJECTREF((MonoObject_clr*)params[argIndex]));
            break;
        default:
            assert(false && "This argType is not supported");
            break;
        }
    }

    // TODO: Convert params to ARG_SLOT

    g_isManaged++;
    ARG_SLOT result = NULL;
    EX_TRY
    {
        MonoClass_clr * klass = (MonoClass_clr*)mono_method_get_class(method);

        OBJECTREF objref = ObjectToOBJECTREF((Object*)parentobj);
        MethodDescCallSite invoker((MonoMethod_clr*)method, &objref);
        result = invoker.Call_RetArgSlot(argslots);
    }
    EX_CATCH
    {
        SString sstr;
        GET_EXCEPTION()->GetMessage(sstr);
        printf("Exception calling %s: %s\n", mono_method_get_name(method), sstr.GetUTF8());
        fflush(stdout);

        if (exc && GET_EXCEPTION()->IsType(CLRException::GetType()))
            *exc = (MonoException*)OBJECTREFToObject(((CLRException*)GET_EXCEPTION())->GetThrowable());
    }
    EX_END_CATCH(SwallowAllExceptions)
    g_isManaged--;

    methodSig.Reset();
    if (methodSig.IsReturnTypeVoid())
    {
        return nullptr;
    }

    // Check reflectioninvocation.cpp
    // TODO: Handle
    switch (retType)
    {
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_BOOLEAN:      // boolean
        case ELEMENT_TYPE_I1:           // byte
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:           // short
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:         // char
        case ELEMENT_TYPE_I4:           // int
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:           // long
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:           // float
        case ELEMENT_TYPE_R8:           // double
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_PTR:
            if (hasReturnBufferArg)
            {
                return (MonoObject*)OBJECTREFToObject(retTH.GetMethodTable()->Box(pRetBufStackCopy));
            }
            else
            {
                return (MonoObject*)OBJECTREFToObject(retTH.GetMethodTable()->Box(&result));
            }
            break;
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_VAR:
            return (MonoObject*)OBJECTREFToObject(ArgSlotToObj(result));
            break;
        default:
            assert(false && "This retType is not supported");
            break;
    }
    return nullptr;
}

extern "C" EXPORT_API void EXPORT_CC mono_set_assemblies_path(const char* name)
{
    s_AssemblyPaths = new SString(SString::Utf8, name);
}

extern "C" EXPORT_API void EXPORT_CC mono_set_assemblies_path_null_separated (const char* name)
{
    s_AssemblyPaths = new SString();
    while (*name != NULL)
    {
        size_t l = strlen(name);
        s_AssemblyPaths->AppendUTF8(name);
        s_AssemblyPaths->AppendUTF8(PATH_SEPARATOR);
        name += l+1;
    }
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_signature_get_param_count(MonoMethodSignature *sig)
{
    MonoMethodSignature_clr* msig = (MonoMethodSignature_clr*)sig;
    MetaSig metasig(msig);
    return metasig.NumFixedArgs();
}

extern "C" EXPORT_API MonoType* EXPORT_CC mono_signature_get_params(MonoMethodSignature *sig, gpointer *iter)
{
    MonoMethodSignature_clr* signature = (MonoMethodSignature_clr*)sig;
    MetaSig* metasig = (MetaSig*)*iter;
    if (metasig == NULL)
    {
        metasig = new MetaSig(signature);
        *iter = metasig;
    }

    CorElementType argType = metasig->NextArg();
    if (argType == ELEMENT_TYPE_END)
    {
        delete metasig;
        //*iter = NULL; // match mono behavior
        return NULL;
    }

    TypeHandle typeHandle = metasig->GetLastTypeHandleThrowing();
    return (MonoType*)typeHandle.AsPtr();
}

extern "C" EXPORT_API MonoType* EXPORT_CC mono_signature_get_return_type(MonoMethodSignature *sig)
{
    MonoMethodSignature_clr* signature = (MonoMethodSignature_clr*)sig;
    MetaSig msig(signature);
    TypeHandle reth = msig.GetRetTypeHandleThrowing();
    return (MonoType*)reth.AsPtr();
}

extern "C" EXPORT_API char EXPORT_CC mono_signature_is_instance(MonoMethodSignature *sig)
{
    MonoMethodSignature_clr* sig_clr = (MonoMethodSignature_clr*)sig;
    MetaSig msig(sig_clr);
    return (char)msig.HasThis();
}

extern "C" EXPORT_API MonoThread* EXPORT_CC mono_thread_attach(MonoDomain *domain)
{
    MonoThread_clr* currentThread = GetThreadNULLOk();

    if (currentThread == nullptr)
    {
        currentThread = SetupThreadNoThrow();
    }

    assert(currentThread != nullptr);
    gCurrentDomain = domain;

    return (MonoThread*)currentThread;
}

extern "C" EXPORT_API MonoThread* EXPORT_CC mono_thread_current(void)
{
    return (MonoThread*)GetThread();
}

extern "C" EXPORT_API void EXPORT_CC mono_thread_detach(MonoThread *thread)
{
    CONTRACTL{
        PRECONDITION(thread != nullptr);
    } CONTRACTL_END;
    auto thread_clr = (MonoThread_clr*)thread;
    gCurrentDomain = NULL;
    // TODO: FALSE or TRUE there?
    thread_clr->DetachThread(FALSE);
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_thread_has_sufficient_execution_stack (void)
{
    // TODO: Investigate if we need to implement this
    return TRUE;
}

extern "C" EXPORT_API char* EXPORT_CC mono_type_get_name(MonoType *type)
{
    // To be compatible with Mono behavior.
    return mono_type_get_name_full(type, MonoTypeNameFormat::MONO_TYPE_NAME_FORMAT_IL);
}

// The method can be moved to C# code, but will require introduction
// of core_clr_FreeHGlobal method to properly free an allocated string
extern "C" EXPORT_API char* EXPORT_CC mono_type_get_name_full(MonoType *type, MonoTypeNameFormat monoFormat)
{
    TRACE_API("%p %d", type, format);

    DWORD format = TypeString::FormatBasic;
    switch (monoFormat)
    {
        case MonoTypeNameFormat::MONO_TYPE_NAME_FORMAT_IL:
            // The closest managed equivalent is Type.ToString()
            format = TypeString::FormatNamespace;
            break;
        case MonoTypeNameFormat::MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED:
            // The managed equivalent is Type.AssemblyQualifiedName
            format = TypeString::FormatNamespace | TypeString::FormatAssembly | TypeString::FormatFullInst;
            break;

        default:
            ASSERT_NOT_IMPLEMENTED;
            return NULL;
    }

    TypeHandle handle = TypeHandle::FromPtr((PTR_VOID)type);
    SString ssBuf;
    TypeString::AppendType(ssBuf, handle, format);

    return _strdup(ssBuf.GetUTF8());
}

extern "C" EXPORT_API int EXPORT_CC mono_type_get_type(MonoType *type)
{
retry:

    if (type == mono_class_get_type((MonoClass*)CoreLibBinder::GetClass(CLASS__STRING)))
        return MONO_TYPE_STRING;
    if (type == mono_class_get_type(mono_get_object_class()))
        return MONO_TYPE_OBJECT;

    TypeHandle typeHandle = TypeHandle::FromPtr((PTR_VOID)type);

    // TODO: not sure this is correct, but without this
    // generic instance types map to MONO_TYPE_CLASS
    if (typeHandle.HasInstantiation())
        return MONO_TYPE_GENERICINST;

    // TODO: Different behavior than mono
    // It seems that CLR is collapsing type like
    // ELEMENT_TYPE_OBJECT into ELEMENT_TYPE_CLASS
    auto elementType = typeHandle.GetVerifierCorElementType();

    switch(elementType)
    {
        case ELEMENT_TYPE_VOID:
            return MONO_TYPE_VOID;
        case ELEMENT_TYPE_END:
            return MONO_TYPE_END;
        case ELEMENT_TYPE_PTR:
            return MONO_TYPE_PTR;
        case ELEMENT_TYPE_BYREF:
            // mono exposes this as the underlying type
            type =  (MonoType*)typeHandle.GetTypeParam().AsPtr();
            goto retry;
            //return MONO_TYPE_BYREF;
        case ELEMENT_TYPE_STRING:
            return MONO_TYPE_STRING;
        case ELEMENT_TYPE_R4:
            return MONO_TYPE_R4;
        case ELEMENT_TYPE_R8:
            return MONO_TYPE_R8;
        case ELEMENT_TYPE_I8:
            return MONO_TYPE_I8;
        case ELEMENT_TYPE_I4:
            return MONO_TYPE_I4;
        case ELEMENT_TYPE_I2:
            return MONO_TYPE_I2;
        case ELEMENT_TYPE_I1:
            return MONO_TYPE_I1;
        case ELEMENT_TYPE_U8:
            return MONO_TYPE_U8;
        case ELEMENT_TYPE_U4:
            return MONO_TYPE_U4;
        case ELEMENT_TYPE_U2:
            return MONO_TYPE_U2;
        case ELEMENT_TYPE_U1:
            return MONO_TYPE_U1;
        case ELEMENT_TYPE_CLASS:
            return MONO_TYPE_CLASS;
        case ELEMENT_TYPE_BOOLEAN:
            return MONO_TYPE_BOOLEAN;
        case ELEMENT_TYPE_CHAR:
            return MONO_TYPE_CHAR;
        case ELEMENT_TYPE_VALUETYPE:
            return MONO_TYPE_VALUETYPE;
        case ELEMENT_TYPE_VAR:
            return MONO_TYPE_VAR;
        case ELEMENT_TYPE_ARRAY:
            return MONO_TYPE_ARRAY;
        case ELEMENT_TYPE_GENERICINST:
            return MONO_TYPE_GENERICINST;
        case ELEMENT_TYPE_TYPEDBYREF:
            return MONO_TYPE_TYPEDBYREF;
        case ELEMENT_TYPE_I:
            return MONO_TYPE_I;
        case ELEMENT_TYPE_U:
            return MONO_TYPE_U;
        case ELEMENT_TYPE_FNPTR:
            return MONO_TYPE_FNPTR;
        case ELEMENT_TYPE_OBJECT:
            return MONO_TYPE_OBJECT;
        case ELEMENT_TYPE_SZARRAY:
            return MONO_TYPE_SZARRAY;
        case ELEMENT_TYPE_MVAR:
            return MONO_TYPE_MVAR;
        case ELEMENT_TYPE_CMOD_REQD:
            return MONO_TYPE_CMOD_REQD;
        case ELEMENT_TYPE_CMOD_OPT:
            return MONO_TYPE_CMOD_OPT;
        case ELEMENT_TYPE_INTERNAL:
            return MONO_TYPE_INTERNAL;
        case ELEMENT_TYPE_MODIFIER:
            return MONO_TYPE_MODIFIER;
        case ELEMENT_TYPE_SENTINEL:
            return MONO_TYPE_SENTINEL;
        case ELEMENT_TYPE_PINNED:
            return MONO_TYPE_PINNED;
        default:
            return 0;
    }
}

extern "C" EXPORT_API uint32_t EXPORT_CC mono_unity_allocation_granularity ()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

#if defined(HOST_OSX) || defined(HOST_UNIX)
extern "C" EXPORT_API int EXPORT_CC mono_unity_backtrace_from_context(void* context, void* array[], int count)
{
    // Not implemented yet. Returning no frames allows code to continue without
    // stack trace support.
    return 0;
}
#endif

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_class_has_failure (MonoClass * klass)
{
#ifndef DACCESS_COMPILE
    MonoClass_clr* clrClass = reinterpret_cast<MonoClass_clr*>(klass);
    // If the class is inited, it can't have a failure (see MethodTable::DoRunClassInitThrowing() for details)
    if (clrClass->IsClassInited())
        return FALSE;

    // Otherwise it is either not loaded or failed init
    return clrClass->IsInitError();
#else
    // No way to check this without IsInitError missing
    return FALSE;
#endif
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_g_free(void* p)
{
    free(p);
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_gc_handles_foreach_get_target (MonoDataFunc callback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API MonoException* EXPORT_CC mono_unity_loader_get_last_error_and_error_prepare_exception()
{
    //ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_set_vprintf_func(vprintf_func func)
{
    our_vprintf = func;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_type_get_name_full_chunked(MonoType * type, MonoDataFunc appendCallback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC coreclr_unity_profiler_register(const CLSID* classId, const guint16* profilerDllPathUtf16)
{
    STATIC_CONTRACT_NOTHROW;

    StoredProfilerNode *profilerData = new StoredProfilerNode();
    profilerData->guid = *classId;
    profilerData->path.Set(reinterpret_cast<LPCWSTR>(profilerDllPathUtf16));

    g_profControlBlock.storedProfilers.InsertHead(profilerData);
}
