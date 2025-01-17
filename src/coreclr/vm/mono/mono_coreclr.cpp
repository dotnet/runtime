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
#include "formattype.h"

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

// we only need domain reload for Editor
// #define UNITY_SUPPORT_DOMAIN_UNLOAD 1

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

#define ASSERT_NOT_IMPLEMENTED printf("Function not implemented: %s\n", __func__);

#define FIELD_ATTRIBUTE_PRIVATE               0x0001
#define FIELD_ATTRIBUTE_FAMILY                0x0004
#define FIELD_ATTRIBUTE_PUBLIC                0x0006

thread_local int g_isManaged = 0;

typedef Object MonoObject_clr;
typedef FieldDesc MonoClassField_clr; // struct MonoClassField;
typedef MethodTable MonoClass_clr; //struct MonoClass;
typedef MethodDesc MonoMethod_clr;
typedef TypeHandle MonoType_clr;
typedef Thread MonoThread_clr;

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

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_from_mono_type(MonoType *image)
{
    MonoClass_clr* klass = MonoType_clr_from_MonoType(image).GetMethodTable();
    return (MonoClass*)klass;
}

extern "C" EXPORT_API int EXPORT_CC coreclr_unity_array_element_size(MonoClass* classOfArray)
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

extern "C" EXPORT_API gint32 EXPORT_CC coreclr_unity_class_array_element_size(MonoClass *ac)
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

// remove once usages in this file are removed
static MonoClass* mono_class_get_element_class(MonoClass *klass) // mono_class_get_name still uses this
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

extern "C" EXPORT_API MonoType* EXPORT_CC mono_class_get_type(MonoClass *klass)
{
    TypeHandle h(reinterpret_cast<MonoClass_clr*>(klass));
    return (MonoType*)h.AsPtr();
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_class_get_type_token(MonoClass *klass)
{
    return (guint32)reinterpret_cast<MonoClass_clr*>(klass)->GetCl();
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

static inline OBJECTHANDLE handle_from_uintptr(uintptr_t p)
{
    // mask off bit that is set for pinned in managed
    p &= (~(uintptr_t)1);
    return (OBJECTHANDLE)p;
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

// Still used by: mono_runtime_invoke_with_nested_object
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

extern "C" EXPORT_API void EXPORT_CC coreclr_unity_profiler_register(const CLSID* classId, const guint16* profilerDllPathUtf16)
{
    STATIC_CONTRACT_NOTHROW;

    StoredProfilerNode *profilerData = new StoredProfilerNode();
    profilerData->guid = *classId;
    profilerData->path.Set(reinterpret_cast<LPCWSTR>(profilerDllPathUtf16));

    g_profControlBlock.storedProfilers.InsertHead(profilerData);
}

extern "C" EXPORT_API ObjectHandleID EXPORT_CC coreclr_unity_profiler_class_get_assembly_load_context_handle(ClassID classId)
{
    STATIC_CONTRACT_NOTHROW;

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);

    if (!typeHandle.IsRestored())
        return NULL;

    if (classId == PROFILER_GLOBAL_CLASS)
        return NULL;

    Module* pModule = typeHandle.GetModule();
    if (pModule == NULL)
        return NULL;

    Assembly *pAssembly = pModule->GetAssembly();
    if (pAssembly == NULL)
        return NULL;

    AssemblyBinder* pAssemblyBinder = pAssembly->GetPEAssembly()->GetAssemblyBinder();
    if (pAssemblyBinder->IsDefault())
        return NULL;

    // ManagedAssemblyLoadContext is a handle to the managed AssemblyLoadContext object
    return (ObjectHandleID)pAssemblyBinder->GetManagedAssemblyLoadContext();
}

extern "C" EXPORT_API ObjectHandleID EXPORT_CC coreclr_unity_profiler_get_managed_assembly_load_context(AssemblyID assemblyID)
{
    STATIC_CONTRACT_NOTHROW;

    Assembly *pAssembly = (Assembly*)assemblyID;
    if (pAssembly == NULL)
        return NULL;

    AssemblyBinder* pAssemblyBinder = pAssembly->GetPEAssembly()->GetAssemblyBinder();
    if (pAssemblyBinder->IsDefault())
        return NULL;

    // ManagedAssemblyLoadContext is a handle to the managed AssemblyLoadContext object
    return (ObjectHandleID)pAssemblyBinder->GetManagedAssemblyLoadContext();
}

extern "C" EXPORT_API ObjectHandleID EXPORT_CC coreclr_unity_profiler_assembly_load_context_get_loader_allocator_handle(ObjectID assemblyLoadContextObjectID)
{
    STATIC_CONTRACT_NOTHROW;

    ASSEMBLYLOADCONTEXTREF pAssemblyLoadContext = (ASSEMBLYLOADCONTEXTREF)assemblyLoadContextObjectID;
    if (pAssemblyLoadContext == NULL)
        return NULL;

    AssemblyBinder* pAssemblyBinder = (AssemblyBinder*)pAssemblyLoadContext->GetNativeAssemblyBinder();
    if (pAssemblyBinder == NULL)
        return NULL;

    LoaderAllocator* loaderAllocator = pAssemblyBinder->GetLoaderAllocator();
    if (loaderAllocator == NULL)
        return NULL;

    // ManagedAssemblyLoadContext is a handle to the managed AssemblyLoadContext object
    return (ObjectHandleID)loaderAllocator->GetLoaderAllocatorObjectHandle();
}

extern "C" EXPORT_API gboolean EXPORT_CC coreclr_unity_gc_concurrent_mode(gboolean state)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    IGCHeap * pGCHeap = GCHeapUtilities::GetGCHeap();
    gboolean currentState = pGCHeap->IsConcurrentGCEnabled();
    if (currentState != state)
    {
        if (state)
        {
            pGCHeap->TemporaryEnableConcurrentGC();
        }
        else
        {
            pGCHeap->TemporaryDisableConcurrentGC();
            HRESULT hr = pGCHeap->WaitUntilConcurrentGCCompleteAsync(INFINITE);
            if(FAILED(hr))
                printf("Failed to disable concurrent GC: %x\n", hr);
        }
    }

    return currentState;
}

typedef void (__cdecl *OnFatalErrorFunc)(EXCEPTION_POINTERS* pExceptionPointers);
extern OnFatalErrorFunc g_unityOnFatalError;

extern "C" EXPORT_API void EXPORT_CC coreclr_unity_set_on_fatal_error(OnFatalErrorFunc on_fatal_func)
{
    g_unityOnFatalError = on_fatal_func;
}

typedef void (__cdecl *StackFrameInfoFunc)(const char* moduleName, const char* methodSignature, void* userData);

extern "C" EXPORT_API bool EXPORT_CC coreclr_unity_get_stackframe_info_from_ip(void* ip, StackFrameInfoFunc func, void* userData)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    EECodeInfo codeInfo((PCODE)ip);
    if (codeInfo.IsValid())
    {
        MethodDesc* pResult = codeInfo.GetMethodDesc();
        if (pResult)
        {
            SString namespaceOrClassName, methodName;
            pResult->GetMethodInfoNoSig(namespaceOrClassName, methodName);

            // signature
            CQuickBytes qbOut;
            ULONG cSig = 0;
            PCCOR_SIGNATURE pSig;

            SString methodFullName;
            methodFullName.AppendPrintf(
                (LPCUTF8)"%s::%s",
                namespaceOrClassName.GetUTF8(),
                methodName.GetUTF8());

            pResult->GetSig(&pSig, &cSig);

            PrettyPrintSig(pSig, (DWORD)cSig, methodFullName.GetUTF8(), &qbOut, pResult->GetMDImport(), NULL);
            func(pResult->GetModule()->GetAssembly()->GetSimpleName(), (char *)qbOut.Ptr(), userData);

            return true;
        }
    }

    return false;
} 