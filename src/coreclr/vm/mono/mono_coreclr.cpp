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
#include "threadlocalpoolallocator.h"
#include "threads.h"
#include "threadsuspend.h"
#include "typeparse.h"
#include "typestring.h"

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

struct HostStruct
{
    intptr_t version;
    intptr_t (*load_assembly_from_data)(const char* data, int64_t length);
    intptr_t (*load_assembly_from_path)(const char* path, int32_t length);
};
HostStruct* g_HostStruct;

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
MonoDomain *gRootDomain;
EXTERN_C IMAGE_DOS_HEADER __ImageBase;

typedef const char*(*UnityFindPluginCallback)(const char*);
static UnityFindPluginCallback unity_find_plugin_callback = NULL;

MonoObject* GetMonoDomainObject(MonoDomain *domain)
{
    return mono_gchandle_get_target_v2((intptr_t)domain);
}

MonoDomain* CreateMonoDomainFromObject(MonoObject *o)
{
    return (MonoDomain*)mono_gchandle_new_v2(o, false);
}

CrstStatic g_add_internal_lock;

static SString* s_AssemblyDir;
static SString* s_EtcDir;
static SString* s_AssemblyPaths;

// Import this function manually as it is not defined in a header
extern "C" HRESULT  GetCLRRuntimeHost(REFIID riid, IUnknown **ppUnk);

#define ASSERT_NOT_IMPLEMENTED printf("Function not implemented: %s\n", __func__);

#define kCoreCLRHelpersDll "unity-embed-host.dll"
#define FIELD_ATTRIBUTE_PRIVATE               0x0001
#define FIELD_ATTRIBUTE_FAMILY                0x0004
#define FIELD_ATTRIBUTE_PUBLIC                0x0006
const int MONO_TABLE_TYPEDEF = 2;               // mono/metadata/blob.h

struct MonoCustomAttrInfo_clr
{
    IMDInternalImport *import;
    mdToken mdDef;
    Assembly *assembly;
};

class GCNativeFrame : public Frame
{
    VPTR_VTABLE_CLASS(GCNativeFrame, Frame)

public:

    GCNativeFrame() {
        stackBase = NULL;
    };

    VOID Pop();

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        for (UINT32 i=0; i<bits.GetCount(); i++)
        {
            UInt64 mask = bits[i];
            for (int j=0; mask != 0; j++, mask >>= 1)
            {
                if (mask & 1)
                {
                    void *ptr = stackBase - (i * sizeof(UInt64) * 8) - j;
                    fn ((PTR_PTR_Object)ptr, sc, 0);
                }
            }
        }
    }

    void PushStackPtr(void **addr)
    {
        if (stackBase < addr)
            stackBase = addr + 1024;
        ptrdiff_t bitOffs = stackBase - addr;
        size_t arrayIndex = bitOffs / (sizeof(UInt64) * 8);
        UInt64 bitIndex = bitOffs % (sizeof(UInt64) * 8);
        size_t count = bits.GetCount();
        if (count < arrayIndex + 1)
        {
            bits.SetCount((COUNT_T)arrayIndex + 1);
            for (size_t i=count;i<=arrayIndex;i++)
                bits[(COUNT_T)i] = 0;
        }
        bits[(COUNT_T)arrayIndex] |= 1LL << bitIndex;
    }

    void PopStackPtr(void **addr)
    {
        ptrdiff_t bitOffs = stackBase - addr;
        size_t arrayIndex = bitOffs / (sizeof(UInt64) * 8);
        UInt64 bitIndex = bitOffs % (sizeof(UInt64) * 8);
        if (bits.GetCount() > arrayIndex)
            bits[(COUNT_T)arrayIndex] &= ~(1LL << bitIndex);
    }

private:
    void **stackBase;
    SArray<UInt64> bits;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(GCNativeFrame)
};

#ifndef __GNUC__
__declspec(thread) GCNativeFrame * pCurrentThreadNativeFrame;
#else // !__GNUC__
thread_local GCNativeFrame * pCurrentThreadNativeFrame;
#endif // !__GNUC__

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

MonoString* InvokeFindPluginCallback(MonoString* path)
{
    if (unity_find_plugin_callback)
    {
        const char* result = unity_find_plugin_callback(mono_string_to_utf8(path));
        if (result != NULL)
        {
            MonoString* result_mono = mono_string_new_wrapper(result);
            return result_mono;
        }
    }
    return NULL;
}

extern "C" EXPORT_API int EXPORT_CC EXPORT_CC coreclr_array_length(MonoArray* array)
{
    ArrayBase* arrayObj = (ArrayBase*)array;

    return arrayObj->GetNumComponents();
}

extern "C" EXPORT_API MonoClass* EXPORT_CC EXPORT_CC coreclr_class_from_systemtypeinstance (MonoObject* systemTypeInstance)
{
    ReflectClassBaseObject* refClass = (ReflectClassBaseObject*)systemTypeInstance;
    {
        GCX_COOP();
        return (MonoClass*)refClass->GetType().AsMethodTable();
    }
}

extern "C" EXPORT_API void EXPORT_CC mono_add_internal_call(const char *name, gconstpointer method)
{
    TRACE_API("%s, %p", name, method);

    assert(name != nullptr);
    assert(method != nullptr);
    CrstHolder lock(&g_add_internal_lock);
    ECall::RegisterICall(name, (PCODE)method);
}

extern "C" EXPORT_API char* EXPORT_CC mono_array_addr_with_size(MonoArray *array, int size, uintptr_t idx)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_array_class_get(MonoClass *eclass, guint32 rank)
{
    CONTRACTL{
        STANDARD_VM_CHECK;
        PRECONDITION(eclass != nullptr);
        PRECONDITION(rank > 0);
    } CONTRACTL_END;

    // TODO: We do not make any caching here
    // Might be a problem compare to mono implem that is caching
    // (clients might expect that for a same eclass+rank, we get the same array class pointer)

    TypeHandle typeHandle(reinterpret_cast<MonoClass_clr*>(eclass));
    auto arrayMT = typeHandle.MakeArray(rank);

    return (MonoClass*)arrayMT.GetMethodTable();
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

extern "C" EXPORT_API MonoArray* EXPORT_CC mono_array_new(MonoDomain *domain, MonoClass *eclass, guint32 n)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(domain != nullptr);
        PRECONDITION(eclass != nullptr);
    } CONTRACTL_END;

    GCX_COOP();
    // TODO: handle large heap flag?
    auto arrayRef = AllocateObjectArray(n, (MonoClass_clr*)eclass);

    auto array_clr = (MonoArray_clr*)OBJECTREFToObject(arrayRef);
    //auto offsetValue = (char*)array_clr->GetDataPtr() - (char*)array_clr;
    return (MonoArray*)array_clr;
}

extern "C" EXPORT_API void EXPORT_CC mono_assembly_close (MonoAssembly * assembly)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_assembly_fill_assembly_name (MonoImage * image, MonoAssemblyName * aname)
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API void EXPORT_CC mono_assembly_foreach (GFunc func, gpointer user_data)
{
    ASSERT_NOT_IMPLEMENTED;
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

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_assembly_get_object(MonoDomain *domain, MonoAssembly *assembly)
{
    FCALL_CONTRACT;
    GCX_COOP();
    return (MonoObject*)OBJECTREFToObject(reinterpret_cast<MonoImage_clr*>(assembly)->GetExposedObject());
}

extern "C" EXPORT_API MonoAssembly* EXPORT_CC mono_assembly_load_from(MonoImage *image, const char*fname, int *status)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoAssembly* EXPORT_CC mono_assembly_load_from_full(MonoImage *image, const char *fname, int *status, gboolean refonly)
{
    // TODO: As we are making MonoImage == MonoAssembly, return it as-is
    return (MonoAssembly*)image;
}

extern "C" EXPORT_API MonoAssembly* EXPORT_CC mono_assembly_loaded(MonoAssemblyName *aname)
{
    TRACE_API("%p", aname);

    AppDomain::AssemblyIterator assemblyIterator = SystemDomain::GetCurrentDomain()->IterateAssembliesEx((AssemblyIterationFlags)(
            kIncludeExecution | kIncludeLoaded | kIncludeCollected));

    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    while (assemblyIterator.Next(pDomainAssembly.This()))
    {
        auto simpleName = pDomainAssembly->GetSimpleName();
        if (strcmp(simpleName, aname->name) == 0)
        {
            return (MonoAssembly*)pDomainAssembly->GetAssembly();
        }
    }
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_assembly_name_free (MonoAssemblyName * assembly)
{
    free((void*)assembly->name);
    assembly->name = NULL;
}

extern "C" EXPORT_API int EXPORT_CC mono_assembly_name_parse(const char* name, MonoAssemblyName *assembly)
{
    assembly->name = _strdup(name);
    return 1;
}

extern "C" EXPORT_API MonoAssembly* EXPORT_CC mono_assembly_open(const char *filename, int *status)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
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

extern "C" EXPORT_API MonoType* EXPORT_CC mono_class_enum_basetype(MonoClass *klass)
{
    // the type loading path now can throw exceptions and trigger GC so comment out for now
        CONTRACTL
    {
  //      NOTHROW;
 //   GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;


    CorElementType type = reinterpret_cast<MonoClass_clr*>(klass)->GetInternalCorElementType();
    switch(type)
    {
    case ELEMENT_TYPE_CHAR:
        return mono_class_get_type(mono_get_char_class());
    case ELEMENT_TYPE_U1:
        return mono_class_get_type(mono_get_byte_class());
    case ELEMENT_TYPE_I2:
        return mono_class_get_type(mono_get_int16_class());
    case ELEMENT_TYPE_I4:
        return mono_class_get_type(mono_get_int32_class());
    case ELEMENT_TYPE_U4:
        return mono_class_get_type((MonoClass*)CoreLibBinder::GetClass(CLASS__UINT32));
    case ELEMENT_TYPE_I8:
        return mono_class_get_type(mono_get_int64_class());
    default:
        printf("mono_class_enum_basetype: Element type %x not implemented!\n", type);
        return NULL;
    }
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_from_mono_type(MonoType *image)
{
    MonoClass_clr* klass = MonoType_clr_from_MonoType(image).GetMethodTable();
    return (MonoClass*)klass;
}

MonoClass * mono_class_from_name(MonoImage *image, const char* name_space, const char *name, bool ignoreCase)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        // We don't support multiple domains
        PRECONDITION(image != nullptr);
        PRECONDITION(name_space != nullptr);
        PRECONDITION(name != nullptr);
    }
    CONTRACTL_END;
    auto assembly = (MonoAssembly_clr*)image;
    DomainAssembly* domainAssembly = assembly->GetDomainAssembly();

    InlineSString<512> fullTypeName(SString::Utf8, name_space);
    fullTypeName.AppendUTF8(".");
    fullTypeName.AppendUTF8(name);
    SString::Iterator i = fullTypeName.Begin();
    while (fullTypeName.Find(i, W('/')))
        fullTypeName.Replace(i, W('+'));

    TypeHandle retTypeHandle = TypeName::GetTypeManaged(fullTypeName.GetUnicode(), domainAssembly, FALSE, ignoreCase, TRUE, NULL, NULL);

    if (!retTypeHandle.IsNull())
    {
        return (MonoClass*)retTypeHandle.AsMethodTable();
    }
    return NULL;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_from_name(MonoImage *image, const char* name_space, const char *name)
{
    TRACE_API("%x, %s, %s", image, name_space, name);

    return mono_class_from_name(image, name_space, name, false);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_from_name_case(MonoImage *image, const char* name_space, const char *name)
{
    TRACE_API("%x, %s, %s", image, name_space, name);

    return mono_class_from_name(image, name_space, name, true);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_get(MonoImage *image, guint32 type_token)
{
    TRACE_API("%p, %x", image, type_token);

    DomainAssembly* domainAssembly = reinterpret_cast<MonoImage_clr*>(image)->GetDomainAssembly();
    MonoClass_clr* klass = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(domainAssembly->GetModule(), (mdToken)type_token, NULL).AsMethodTable();
    return (MonoClass*)klass;
}

extern "C" EXPORT_API MonoType* EXPORT_CC mono_class_get_byref_type(MonoClass *klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_get_element_class(MonoClass *klass)
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

extern "C" EXPORT_API MonoClassField* EXPORT_CC mono_class_get_field_from_name(MonoClass *klass, const char *name)
{
    CONTRACTL
    {
    GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    while (klass)
    {
        MonoClass_clr* mt = reinterpret_cast<MonoClass_clr*>(klass);

        ApproxFieldDescIterator fieldDescIterator(mt, ApproxFieldDescIterator::ALL_FIELDS);
        FieldDesc* pField;

        while ((pField = fieldDescIterator.Next()) != NULL)
        {
            if(strcmp(pField->GetName(), name) == 0)
            {
                return (MonoClassField*)pField;
            }
        }

        klass = mono_class_get_parent(klass);
    }

    return NULL;
}

thread_local ThreadLocalPoolAllocator<ApproxFieldDescIterator,5> g_ApproxFieldDescIteratorAlloc;

extern "C" EXPORT_API MonoClassField* EXPORT_CC mono_class_get_fields(MonoClass* klass, gpointer *iter)
{
    TRACE_API("%p, %p", klass, iter);

    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    if (!iter)
    {
        return NULL;
    }
    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;

    ApproxFieldDescIterator* iterator = (ApproxFieldDescIterator*)*iter;
    if (iterator == nullptr)
    {
        iterator = g_ApproxFieldDescIteratorAlloc.Alloc();
        iterator->Init(klass_clr, ApproxFieldDescIterator::INSTANCE_FIELDS | ApproxFieldDescIterator::STATIC_FIELDS);
        *iter = iterator;
    }

    auto nextField = iterator->Next();
    if (nextField == nullptr)
    {
        *iter = nullptr;
        g_ApproxFieldDescIteratorAlloc.Free(iterator);
        return nullptr;
    }

    return (MonoClassField*)nextField;
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_class_get_flags(MonoClass *klass)
{
    MonoClass_clr* clrClass = reinterpret_cast<MonoClass_clr*>(klass);
    mdTypeDef token = clrClass->GetCl();
    IMDInternalImport *pImport = clrClass->GetMDImport();
    DWORD           dwClassAttrs;
    pImport->GetTypeDefProps(token, &dwClassAttrs, NULL);
    return dwClassAttrs;
}

extern "C" EXPORT_API MonoImage* EXPORT_CC mono_class_get_image(MonoClass *klass)
{
    MonoClass_clr* classClr = (MonoClass_clr*)klass;

    return (MonoImage*)classClr->GetAssembly();
}

// Wrap iterator value in heap allocated value we can return from embedding API
struct MethodTable_InterfaceMapIteratorWrapper
{
    MethodTable::InterfaceMapIterator iter;

    MethodTable_InterfaceMapIteratorWrapper(MonoClass_clr* klass_clr) :
        iter(klass_clr->IterateInterfaceMap())
    {
    }
};

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_get_interfaces(MonoClass* klass, gpointer *iter)
{
    TRACE_API("%p, %p", klass, iter);

    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    if (!iter)
    {
        return NULL;
    }
    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;

    MethodTable_InterfaceMapIteratorWrapper* iterator = (MethodTable_InterfaceMapIteratorWrapper*)*iter;
    if (iterator == nullptr)
    {
        iterator = new MethodTable_InterfaceMapIteratorWrapper(klass_clr);
        *iter = iterator;
    }

    if (!iterator->iter.Next())
    {
        *iter = nullptr;
        delete iterator;
        return nullptr;
    }

    // TODO: this used to be a call to GetInterface, not sure of the difference
    return (MonoClass*)iterator->iter.GetInterfaceApprox();
}

extern "C" EXPORT_API MonoMethod* EXPORT_CC mono_class_get_method_from_name(MonoClass *klass, const char *name, int param_count)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(klass != NULL);
        PRECONDITION(name != NULL);
    }
    CONTRACTL_END;

    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;

    // TODO: Check if there is an API to perform this more efficiently
    while (klass_clr)
    {
        auto iterator = MethodTable::MethodIterator(klass_clr);
        while (iterator.IsValid())
        {
            auto method = iterator.GetMethodDesc();
            if (strcmp(method->GetName(), name) == 0)

            {
                MetaSig     methodSig(method);

                DWORD numArgs = methodSig.NumFixedArgs();
                if (numArgs == (DWORD)param_count)
                {
                    return (MonoMethod*)method;
                }
            }
            iterator.Next();
        }
        klass_clr = klass_clr->GetParentMethodTable();
    }
    return NULL;
}

extern "C" EXPORT_API MonoMethod* EXPORT_CC mono_class_get_methods(MonoClass* klass, gpointer *iter)
{
    TRACE_API("%p, %p", klass, iter);

    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
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
        StackScratchBuffer buffer;
        static char buf[512] = {0};
        strcpy(buf, arrayName.GetUTF8(buffer));
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

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_get_nested_types(MonoClass* klass, gpointer *iter)
{
    TRACE_API("%p, %p", klass, iter);

    CONTRACTL
    {
        THROWS; // new BYTE
        GC_TRIGGERS; // ClassLoader::LoadTypeDefThrowing
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    if (!iter)
    {
        return NULL;
    }

    struct NestedTypesIterator
    {
        ULONG index;
        ULONG count;
        mdTypeDef tokens[];
    };

    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;

    NestedTypesIterator* nestedIterator = (NestedTypesIterator*)*iter;
    if (nestedIterator == NULL)
    {
        mdTypeDef token = klass_clr->GetCl();
        IMDInternalImport *pImport = klass_clr->GetMDImport();
        ULONG nestedCount;
        pImport->GetCountNestedClasses(token, &nestedCount);
        // Early exit if there is no nested classes
        if (nestedCount == 0)
        {
            return NULL;
        }
        SIZE_T sizeOfIterator = sizeof(NestedTypesIterator) + sizeof(mdTypeDef) * nestedCount;
        nestedIterator = (NestedTypesIterator*)new BYTE[sizeOfIterator];
        nestedIterator->index = 0;
        nestedIterator->count = nestedCount;
        *iter = nestedIterator;
        pImport->GetNestedClasses(token, nestedIterator->tokens, nestedCount, &nestedCount);
    }

    if (nestedIterator->index < nestedIterator->count)
    {
        TypeHandle th = ClassLoader::LoadTypeDefThrowing(klass_clr->GetModule(), nestedIterator->tokens[nestedIterator->index]);
        nestedIterator->index++;
        MONO_ASSERTE(!th.IsNull());
        return (MonoClass*)th.GetMethodTable();
    }
    else
    {
        *iter = NULL;
        delete[](BYTE*)nestedIterator;
    }

    return NULL;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_get_nesting_type(MonoClass *klass)
{
    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;
    if (!klass_clr->GetClass()->IsNested())
    {
        return nullptr;
    }
    MonoClass_clr* ret = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(klass_clr->GetModule(), klass_clr->GetEnclosingCl(), NULL, ClassLoader::ThrowIfNotFound, ClassLoader::PermitUninstDefOrRef).AsMethodTable();
    return (MonoClass*)ret;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_class_get_parent(MonoClass *klass)
{
    MonoClass_clr* parent = reinterpret_cast<MonoClass_clr*>(klass)->GetParentMethodTable();
    return (MonoClass*)parent;
}

extern "C" EXPORT_API MonoProperty* EXPORT_CC mono_class_get_properties(MonoClass* klass, gpointer *iter)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoProperty* EXPORT_CC mono_class_get_property_from_name(MonoClass *klass, const char *name)
{
    // CoreCLR does not have easy support for iterating on properties on a MethodTable.
    // So instead, we look for the property's "get" method. This will not work for set-only
    // properties, but is sufficient for our needs for now.
    SString propertyName(SString::Utf8, "get_");
    propertyName += SString(SString::Utf8, name);
    StackScratchBuffer buffer;
    return (MonoProperty*)mono_class_get_method_from_name(klass, propertyName.GetUTF8(buffer), 0);
}

extern "C" EXPORT_API int EXPORT_CC mono_class_get_rank(MonoClass *klass)
{
    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;


    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;
    return klass_clr->IsArray() ? klass_clr->GetRank() : 0;
}

extern "C" EXPORT_API MonoType* EXPORT_CC mono_class_get_type(MonoClass *klass)
{
    TypeHandle h(reinterpret_cast<MonoClass_clr*>(klass));
    return (MonoType*)h.AsPtr();
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_class_get_type_token(MonoClass *klass)
{
    return (guint32)reinterpret_cast<MonoClass_clr*>(klass)->GetTypeID();
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

extern "C" EXPORT_API gboolean EXPORT_CC mono_class_is_blittable(MonoClass * klass)
{
    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;
    return klass_clr->IsBlittable();
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_class_is_enum(MonoClass *klass)
{
    return (gboolean)reinterpret_cast<MonoClass_clr*>(klass)->IsEnum() ? TRUE : FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_class_is_generic(MonoClass* klass)
{
    CONTRACTL{
        PRECONDITION(klass != nullptr);
    } CONTRACTL_END;
    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;
    return klass_clr->IsGenericTypeDefinition() ? TRUE : FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_class_is_inflated(MonoClass* klass)
{
    CONTRACTL{
        PRECONDITION(klass != nullptr);
    } CONTRACTL_END;
    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;
    // TODO: is it really the concept behind inflated? (generic instance?)
    auto isgeneric = klass_clr->GetNumGenericArgs() > 0
        && !klass_clr->IsGenericTypeDefinition();

    return isgeneric ? TRUE : FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_class_is_subclass_of(MonoClass *klass, MonoClass *klassc, gboolean check_interfaces)
{
    MonoClass_clr* clazz = (MonoClass_clr*)klass;
    MonoClass_clr* clazzc = (MonoClass_clr*)klassc;
    do
    {
        if (clazz == clazzc)
            return TRUE;
        if (clazz->IsArray() && clazzc->IsArray())
        {
            if (clazz->GetRank() == clazzc->GetRank() && clazz->GetArrayElementTypeHandle() == clazzc->GetArrayElementTypeHandle())
                return TRUE;
        }
        if (check_interfaces)
        {
            auto ifaceIter = clazz->IterateInterfaceMap();
            while (ifaceIter.Next())
                if (ifaceIter.GetInterfaceApprox() /* TODO: this used to be GetInterface, is this okay? */ == clazzc)
                    return TRUE;
        }
        clazz = clazz->GetParentMethodTable();
    }
    while (clazz != NULL);
    return FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_class_is_valuetype(MonoClass *klass)
{
    MonoClass_clr* clazz = (MonoClass_clr*)klass;
    return (gboolean)clazz->IsValueType()? TRUE : FALSE;
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

extern "C" EXPORT_API MonoVTable* EXPORT_CC mono_class_vtable(MonoDomain *domain, MonoClass *klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_config_parse(const char *filename)
{
    // NOP
}

MonoObject* CreateAttributeInstance(MonoCustomAttrInfo_clr* attributes, mdCustomAttribute mdAttribute, MonoClass *attr_klass)
{
    mdToken tkCtor;
    if (attributes->import->GetCustomAttributeProps(mdAttribute, &tkCtor) != S_OK)
        return NULL;

    if (attr_klass == NULL)
    {
        if (TypeFromToken(tkCtor) == mdtMemberRef || TypeFromToken(tkCtor) == mdtMethodDef)
        {
            mdToken tkType;
            if (attributes->import->GetParentToken(tkCtor, &tkType) == S_OK)
            {
                if (TypeFromToken(tkType) == mdtTypeRef || TypeFromToken(tkType) == mdtTypeDef)
                {
                    DomainAssembly* domainAssembly = attributes->assembly->GetDomainAssembly();
                    attr_klass = (MonoClass*)ClassLoader::LoadTypeDefOrRefThrowing(domainAssembly->GetModule(), tkType,
                                                                    ClassLoader::ReturnNullIfNotFound,
                                                                    ClassLoader::PermitUninstDefOrRef,
                                                                    tdNoTypes).AsMethodTable();
                }
            }
        }
    }

    MonoObject* obj = mono_object_new(mono_domain_get(), attr_klass);

    DomainAssembly* domainAssembly = attributes->assembly->GetDomainAssembly();

    MethodDesc* ctorMethod = NULL;
    if (TypeFromToken(tkCtor) == mdtMemberRef)
    {
        MethodDesc * pMD = NULL;
        FieldDesc * pFD = NULL;
        TypeHandle th;
        MemberLoader::GetDescFromMemberRef(domainAssembly->GetModule(), tkCtor, &ctorMethod, &pFD, NULL, FALSE, &th);
    }
    else
        ctorMethod = domainAssembly->GetModule()->LookupMethodDef(tkCtor);

    const BYTE  *pbAttr;                // Custom attribute data as a BYTE*.
    ULONG       cbAttr;                 // Size of custom attribute data.

    if (attributes->import->GetCustomAttributeAsBlob(mdAttribute, (const void**)&pbAttr, &cbAttr) != S_OK)
        return NULL;

    CustomAttributeParser CA(pbAttr, cbAttr);
    CA.ValidateProlog();

    GCX_COOP();

    MetaSig     methodSig(ctorMethod);
    DWORD numArgs = methodSig.NumFixedArgs();
    ArgIterator argIt(&methodSig);

    const int MAX_ARG_SLOT = 128;
    ARG_SLOT argslots[MAX_ARG_SLOT];
    DWORD slotIndex = 0;
    argslots[0] = PtrToArgSlot(obj);
    slotIndex++;

    for (DWORD argIndex = 0; argIndex < numArgs; argIndex++, slotIndex++)
    {
        int ofs = argIt.GetNextOffset();
        _ASSERTE(ofs != TransitionBlock::InvalidOffset);
        auto stackSize = argIt.GetArgSize();

        auto argTH = methodSig.GetLastTypeHandleNT();
        auto argType = argTH.GetInternalCorElementType();

        switch (argType)
        {
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_BOOLEAN:
            {
                UINT8 u1 = 0;
                CA.GetU1(&u1);
                argslots[slotIndex] = u1;
                break;
            }

        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
            {
                UINT16 u2 = 0;
                CA.GetU2(&u2);
                argslots[slotIndex] = u2;
                break;
            }
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
            {
                UINT32 u4 = 0;
                CA.GetU4(&u4);
                argslots[slotIndex] = u4;
                break;
            }
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
            {
                UINT64 u8 = 0;
                CA.GetU8(&u8);
                argslots[slotIndex] = u8;
                break;
            }
        case ELEMENT_TYPE_R4:
            {
                float f = CA.GetR4();
                argslots[slotIndex] = *(INT32*)(&f);
                break;
            }
        case ELEMENT_TYPE_R8:
            {
                double d = CA.GetR8();
                argslots[slotIndex] = *(INT64*)(&d);
                break;
            }
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_STRING:
            {
                ULONG cbVal;
                LPCUTF8 pStr;
                CA.GetString(&pStr, &cbVal);
                argslots[slotIndex] = ObjToArgSlot(ObjectToOBJECTREF((Object*)mono_string_new_len(mono_domain_get(), pStr, cbVal)));
                break;
            }
        default:
            assert(false && "This argType is not supported");
            break;
        }
    }

    g_isManaged++;
    EX_TRY
    {
        OBJECTREF objref = ObjectToOBJECTREF((Object*)obj);
        MethodDescCallSite invoker(ctorMethod, &objref);
        invoker.Call_RetArgSlot(argslots);
    }
    EX_CATCH
    {
        SString sstr;
        GET_EXCEPTION()->GetMessage(sstr);
        StackScratchBuffer buffer;
        printf("Exc: %s %d %x\n", sstr.GetUTF8(buffer), GET_EXCEPTION()->IsType(CLRException::GetType()), GET_EXCEPTION()->GetInstanceType());
    }
    EX_END_CATCH(SwallowAllExceptions)
    g_isManaged--;

    methodSig.Reset();

    return obj;
}


extern "C" EXPORT_API MonoArray* EXPORT_CC mono_custom_attrs_construct(MonoCustomAttrInfo *ainfo)
{
    MonoCustomAttrInfo_clr* attributes = reinterpret_cast<MonoCustomAttrInfo_clr*>(ainfo);
    HENUMInternal iterator;
    if (attributes->import->EnumInit(mdtCustomAttribute, attributes->mdDef, &iterator) != S_OK)
        return NULL;

    auto count = attributes->import->EnumGetCount(&iterator);

    auto array = mono_array_new(mono_domain_get(), mono_get_object_class(), count);

    mdCustomAttribute mdAttribute;
    int arrayIndex = 0;
    while (attributes->import->EnumNext(&iterator, &mdAttribute))
        ((MonoObject**)((ArrayBase*)array)->GetDataPtr())[arrayIndex++] = CreateAttributeInstance(attributes, mdAttribute, NULL);

    return (MonoArray*)array;
}

thread_local ThreadLocalPoolAllocator<MonoCustomAttrInfo_clr,5> g_AttributeInfoAlloc;

extern "C" EXPORT_API void EXPORT_CC mono_custom_attrs_free(MonoCustomAttrInfo* attr)
{
    g_AttributeInfoAlloc.Free((MonoCustomAttrInfo_clr*)attr);
}

extern "C" EXPORT_API MonoCustomAttrInfo* EXPORT_CC mono_custom_attrs_from_assembly(MonoAssembly *assembly)
{
    TRACE_API("%p", assembly);
    MonoCustomAttrInfo_clr *aInfo = g_AttributeInfoAlloc.Alloc();
    auto clrAssembly = (MonoImage_clr*)assembly;
    aInfo->import = clrAssembly->GetMDImport();
    aInfo->mdDef = clrAssembly->GetManifestToken();
    aInfo->assembly = clrAssembly;
    return (MonoCustomAttrInfo*)aInfo;
}

extern "C" EXPORT_API MonoCustomAttrInfo* EXPORT_CC mono_custom_attrs_from_class(MonoClass *klass)
{
    TRACE_API("%p", klass);
    MonoClass_clr* clrClass = reinterpret_cast<MonoClass_clr*>(klass);
    MonoCustomAttrInfo_clr *aInfo = g_AttributeInfoAlloc.Alloc();
    aInfo->import = clrClass->GetMDImport();
    aInfo->mdDef = clrClass->GetCl();
    aInfo->assembly = clrClass->GetAssembly();
    return (MonoCustomAttrInfo*)aInfo;
}

extern "C" EXPORT_API MonoCustomAttrInfo* EXPORT_CC mono_custom_attrs_from_field(MonoClass *klass, MonoClassField *field)
{
    TRACE_API("%p, %p", klass, field);
    FieldDesc* clrFieldDesc = reinterpret_cast<FieldDesc*>(field);
    MonoCustomAttrInfo_clr *aInfo = g_AttributeInfoAlloc.Alloc();
    aInfo->import = clrFieldDesc->GetMDImport();
    aInfo->mdDef = clrFieldDesc->GetMemberDef();
    aInfo->assembly = clrFieldDesc->GetApproxEnclosingMethodTable_NoLogging()->GetAssembly();
    return (MonoCustomAttrInfo*)aInfo;
}

extern "C" EXPORT_API MonoCustomAttrInfo* EXPORT_CC mono_custom_attrs_from_method(MonoMethod *method)
{
    TRACE_API("%p", method);
    MonoMethod_clr* clrMethod = reinterpret_cast<MonoMethod_clr*>(method);
    MonoCustomAttrInfo_clr *aInfo = g_AttributeInfoAlloc.Alloc();
    aInfo->import = clrMethod->GetMDImport();
    aInfo->mdDef = clrMethod->GetMemberDef();
    aInfo->assembly = clrMethod->GetAssembly();
    return (MonoCustomAttrInfo*)aInfo;
}

extern "C" EXPORT_API MonoCustomAttrInfo* EXPORT_CC mono_custom_attrs_from_property (MonoClass * klass, MonoProperty * property)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_custom_attrs_get_attr(MonoCustomAttrInfo *ainfo, MonoClass *requested_klass)
{
    TRACE_API("%p, %p", ainfo, attr_klass);
    MonoCustomAttrInfo_clr* attributes = reinterpret_cast<MonoCustomAttrInfo_clr*>(ainfo);

    HENUMInternal iterator;
    if (attributes->import->EnumInit(mdtCustomAttribute, attributes->mdDef, &iterator) != S_OK)
        return NULL;

    mdCustomAttribute mdAttribute;
    while (attributes->import->EnumNext(&iterator, &mdAttribute))
    {
        mdToken tkCtor;
        if (attributes->import->GetCustomAttributeProps(mdAttribute, &tkCtor) == S_OK)
        {
            if (TypeFromToken(tkCtor) == mdtMemberRef || TypeFromToken(tkCtor) == mdtMethodDef)
            {
                mdToken tkType;
                if (attributes->import->GetParentToken(tkCtor, &tkType) == S_OK)
                {
                    if (TypeFromToken(tkType) == mdtTypeRef || TypeFromToken(tkType) == mdtTypeDef)
                    {
                        DomainAssembly* domainAssembly = attributes->assembly->GetDomainAssembly();
                        auto attr_klass = (MonoClass*)ClassLoader::LoadTypeDefOrRefThrowing(domainAssembly->GetModule(), tkType,
                                                                        ClassLoader::ReturnNullIfNotFound,
                                                                        ClassLoader::PermitUninstDefOrRef,
                                                                        tdNoTypes).AsMethodTable();

                        if (mono_class_is_subclass_of(attr_klass, requested_klass, false))
                            return CreateAttributeInstance(attributes, mdAttribute, attr_klass);
                    }
                }
            }
        }
    }

    return NULL;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_custom_attrs_get_attrs (MonoCustomAttrInfo * ainfo, void** iterator)
{
    TRACE_API("%p, %p", ainfo, iterator);

    MonoCustomAttrInfo_clr* attributes = reinterpret_cast<MonoCustomAttrInfo_clr*>(ainfo);
    if (*iterator == NULL)
    {
        *iterator = new HENUMInternal();
        if (attributes->import->EnumInit(mdtCustomAttribute, attributes->mdDef, (HENUMInternal*)*iterator) != S_OK)
            return NULL;
    }

    mdCustomAttribute mdAttribute;
    while (attributes->import->EnumNext((HENUMInternal*)*iterator, &mdAttribute))
    {
        mdToken tkCtor;
        if (attributes->import->GetCustomAttributeProps(mdAttribute, &tkCtor) == S_OK)
        {
            if (TypeFromToken(tkCtor) == mdtMemberRef || TypeFromToken(tkCtor) == mdtMethodDef)
            {
                mdToken tkType;
                if (attributes->import->GetParentToken(tkCtor, &tkType) == S_OK)
                {
                    if (TypeFromToken(tkType) == mdtTypeRef || TypeFromToken(tkType) == mdtTypeDef)
                    {
                        DomainAssembly* domainAssembly = attributes->assembly->GetDomainAssembly();
                        MonoClass_clr* klass = ClassLoader::LoadTypeDefOrRefThrowing(domainAssembly->GetModule(), tkType,
                                                                        ClassLoader::ReturnNullIfNotFound,
                                                                        ClassLoader::PermitUninstDefOrRef,
                                                                        tdNoTypes).AsMethodTable();
                        if (klass != NULL)
                            return (MonoClass*)klass;
                    }
                }
            }
        }
    }

    attributes->import->EnumClose((HENUMInternal*)*iterator);
    delete (HENUMInternal*)*iterator;
    return NULL;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_custom_attrs_has_attr(MonoCustomAttrInfo *ainfo, MonoClass *attr_klass)
{
    TRACE_API("%p, %p", ainfo, attr_klass);
    MonoCustomAttrInfo_clr* attributes = reinterpret_cast<MonoCustomAttrInfo_clr*>(ainfo);
    MonoClass_clr* attributeClass = reinterpret_cast<MonoClass_clr*>(attr_klass);

// Reference implementation. This is about 3x slower, but will likely work for any type of attribute representation,
// so if the optimized version below is suspected to not work correctly, try this one:
/*
    LPCUTF8 name, namespaze;
	attributeClass->GetMDImport()->GetNameOfTypeDef(attributeClass->GetCl(), &name, &namespaze);

    InlineSString<512> fullTypeName(SString::Utf8, namespaze);
    fullTypeName.AppendUTF8(".");
    fullTypeName.AppendUTF8(name);

    return S_OK == attributes->import->GetCustomAttributeByName(attributes->mdDef, fullTypeName.GetUTF8NoConvert(), NULL, NULL) ? TRUE : FALSE;
*/

    HENUMInternal iterator;
    if (attributes->import->EnumInit(mdtCustomAttribute, attributes->mdDef, &iterator) != S_OK)
        return false;

    mdCustomAttribute mdAttribute;
    bool found = false;
    while (attributes->import->EnumNext(&iterator, &mdAttribute))
    {
        mdToken tkCtor;
        if (attributes->import->GetCustomAttributeProps(mdAttribute, &tkCtor) == S_OK)
        {
            if (TypeFromToken(tkCtor) == mdtMemberRef || TypeFromToken(tkCtor) == mdtMethodDef)
            {
                mdToken tkType;
                if (attributes->import->GetParentToken(tkCtor, &tkType) == S_OK)
                {
                    if (TypeFromToken(tkType) == mdtTypeDef)
                    {
                        if (tkType == attributeClass->GetCl())
                        {
                            found = true;
                            break;
                        }
                    }
                    else if (TypeFromToken(tkType) == mdtTypeRef)
                    {
                        DomainAssembly* domainAssembly = attributes->assembly->GetDomainAssembly();
                        MonoClass_clr* klass = ClassLoader::LoadTypeDefOrRefThrowing(domainAssembly->GetModule(), tkType,
                                                                        ClassLoader::ReturnNullIfNotFound,
                                                                        ClassLoader::PermitUninstDefOrRef,
                                                                        tdNoTypes).AsMethodTable();
                        if (klass == attributeClass)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
        }
    }

    attributes->import->EnumClose(&iterator);
    return found;
}

extern "C" EXPORT_API void EXPORT_CC mono_debug_free_source_location(MonoDebugSourceLocation* location)
{
}

extern "C" EXPORT_API void EXPORT_CC mono_debug_init(int format)
{
    // NOP
}

extern "C" EXPORT_API MonoDebugSourceLocation* EXPORT_CC mono_debug_lookup_source_location(MonoMethod* method, guint32 address, MonoDomain* domain)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_debug_open_image_from_memory(MonoImage *image, const char *raw_contents, int size)
{
    // NOP
}

typedef void (*MonoDebuggerAttachFunc)(gboolean attached);
extern "C" EXPORT_API void EXPORT_CC mono_debugger_install_attach_detach_callback (MonoDebuggerAttachFunc func)
{
}

extern "C" EXPORT_API void EXPORT_CC mono_debugger_set_generate_debug_info(gboolean enable)
{
}

// DllImport fallback handling to load native libraries from custom locations
typedef void* (*MonoDlFallbackLoad) (const char *name, int flags, char **err, void *user_data);
typedef void* (*MonoDlFallbackSymbol) (void *handle, const char *name, char **err, void *user_data);
typedef void* (*MonoDlFallbackClose) (void *handle, void *user_data);

extern "C" EXPORT_API MonoDlFallbackHandler* EXPORT_CC mono_dl_fallback_register(MonoDlFallbackLoad load_func, MonoDlFallbackSymbol symbol_func, MonoDlFallbackClose close_func, void *user_data)
{
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_dl_fallback_unregister(MonoDlFallbackHandler *handler)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_dllmap_insert (MonoImage * assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
    ASSERT_NOT_IMPLEMENTED;
}

struct LoadedImage{
    char* name;
    MonoImage *image;
    MonoDomain *domain;
    LoadedImage(const char* _name, MonoImage* _image, MonoDomain* _domain)
    {
        image = _image;
        domain = _domain;
        const char * namepos = strrchr(_name, '/');
        if (namepos)
            _name = namepos + 1;
        name = (char*)malloc(strlen(_name) + 1);
        strcpy(name, _name);
        char* suffix = strstr(name, ".dll");
        if (suffix)
            *suffix = '\0';
    }
    LoadedImage() {}
};
SArray<LoadedImage> *g_LoadedImages = NULL;


extern "C" EXPORT_API MonoAssembly* EXPORT_CC mono_domain_assembly_open(MonoDomain *domain, const char *name)
{
    TRACE_API("%x, %s", domain, name);

    auto domainAssembly = (DomainAssembly*)g_HostStruct->load_assembly_from_path(name, (int32_t)strlen(name));

    if (domainAssembly == NULL)
        return NULL;

    auto assembly = domainAssembly->GetAssembly();
    assembly->EnsureActive();

    if (g_LoadedImages == NULL)
        g_LoadedImages = new SArray<LoadedImage>;
    g_LoadedImages->Append(LoadedImage(name, (MonoImage*)assembly, domain));

    return (MonoAssembly*)assembly;
}

extern "C" EXPORT_API MonoDomain* EXPORT_CC mono_domain_create_appdomain(const char *domainname, const char* configfile)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_domain_finalize(MonoDomain *domain, int timeout)
{
    TRACE_API("%p, %d", domain, timeout);

    GCInterface_WaitForPendingFinalizers();
    return TRUE;
}

extern "C" EXPORT_API MonoDomain* EXPORT_CC mono_domain_get()
{
    TRACE_API("", NULL);
    return GetThreadNULLOk() != NULL ? gCurrentDomain : NULL;
}

extern "C" EXPORT_API gint32 EXPORT_CC mono_domain_get_id(MonoDomain *domain)
{
    TRACE_API("", NULL);
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_domain_set(MonoDomain *domain, gboolean force)
{
    gCurrentDomain = domain;
    return true;
}

extern "C" EXPORT_API void EXPORT_CC mono_domain_unload(MonoDomain* domain)
{
    TRACE_API("%p", domain);

#if UNITY_SUPPORT_DOMAIN_UNLOAD

    domain_unload(domain);
#else
    ASSERT_NOT_IMPLEMENTED;
#endif
}
struct MonoInternalCallFrame
{
    FrameWithCookie<HelperMethodFrame> frame;
    bool didSetupFrame;
};

//static_assert(sizeof(MonoInternalCallFrame) <= sizeof(MonoInternalCallFrameOpaque), "MonoInternalCallFrameOpaque needs to be larger");

// We currently need to wrap Unity icalls called from managed code mono_enter/exit_internal_call.
// This has two reasons:
// 1. We want to set up a CoreCLR stack frame for the icall to make call stack unwinding work
// (so we can get managed stack traces which cross native frames, as verified by the
// can_get_full_stack_trace_in_internal_method test).
// 2. We want to switch the thread to preemptive GC mode when running our icalls, to avoid delays and
// deadlocks when the GC waits for the icall to finish.
//
// Now, the problem is that this adds some overhead to calling icalls, which is not insignificant for
// small icalls (like Profiler.BeginSample). In most cases we can run icalls without wrapping them,
// but it is not generally safe to do so. So we need to find a solution to selectively wrap icalls
// only where needed.
extern "C" EXPORT_API void EXPORT_CC mono_enter_internal_call(MonoInternalCallFrameOpaque *_frame)
{
    TRACE_API("%x", _frame);

    FrameWithCookie<HelperMethodFrame>* frame = (FrameWithCookie<HelperMethodFrame>*)_frame;
    memset((void*)frame, 0, sizeof(MonoInternalCallFrame));
    new(frame) FrameWithCookie<HelperMethodFrame>(0, 0);

    // Should we set up the frame? We only need to do this when calling the icall from CoreCLR JITed code, but not when
    // calling it from Burst code (in which case GetThread() may not be valid if the worker thread is not attached).
    ((MonoInternalCallFrame*)_frame)->didSetupFrame = GetThread() != NULL && GetThread()->PreemptiveGCDisabled();

    // FCalls in CoreCLR always run in cooperative mode, as they are not written in a way which is
    // safe to use for the precice GC. However, for Unity ICalls (which use the same transition mechanism),
    // we cannot do that. Our icalls may often take non-trivial amounst of time, and in some cases use locking
    // mechanisms, which can cause a deadlock, if we need to wait for it to exit to start GC on another thread.
    // Because we disable the precise GC in Unity, we should be safe to interrupt our icalls for GC.
    if (((MonoInternalCallFrame*)_frame)->didSetupFrame)
    {
        INDEBUG(static BOOL __haveCheckedRestoreState = FALSE;)
        FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_BEGIN;
        FORLAZYMACHSTATE(CAPTURE_STATE(frame->MachineState(), return);)
        FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_END;
        INDEBUG(frame->SetAddrOfHaveCheckedRestoreState(&__haveCheckedRestoreState));
        frame->Push();

        GetThread()->EnablePreemptiveGC();
    }
}

extern "C" EXPORT_API void EXPORT_CC mono_error_cleanup (MonoError * error)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API unsigned short EXPORT_CC mono_error_get_error_code (MonoError * error)
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API const char* EXPORT_CC mono_error_get_message (MonoError * error)
{

    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API gint32 EXPORT_CC mono_error_ok (MonoError * error)
{
    return true;
}

extern "C" EXPORT_API void EXPORT_CC mono_error_init (MonoError * error)
{

}

extern "C" EXPORT_API MonoException* EXPORT_CC mono_exception_from_name_msg(MonoImage *image, const char *name_space, const char *name, const char *msg)
{
    SString sstr(SString::Utf8, msg);
    GCX_COOP();
    MonoClass *exclass = mono_class_from_name(image, name_space, name);
    MonoObject *exobj = mono_object_new(mono_domain_get(), exclass);
    ((ExceptionObject*)exobj)->SetMessage(AllocateString(sstr));
    return (MonoException*)exobj;
}

extern "C" EXPORT_API MonoException* EXPORT_CC mono_exception_from_name_two_strings(MonoImage *image, const char *name_space, const char *name, const char *msg1, const char *msg2)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_exit_internal_call(MonoInternalCallFrameOpaque *_frame)
{
    TRACE_API("%x", _frame);

    FrameWithCookie<HelperMethodFrame>* frame = (FrameWithCookie<HelperMethodFrame>*)_frame;

    if (((MonoInternalCallFrame*)_frame)->didSetupFrame)
    {
        GetThread()->DisablePreemptiveGC();
        frame->Pop();
    }
    frame->~FrameWithCookie<HelperMethodFrame>();
}

extern "C" EXPORT_API MonoClassField* EXPORT_CC mono_field_from_token (MonoImage * image, uint32_t token, MonoClass** retklass, MonoGenericContext * context)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_field_get_flags(MonoClassField *field)
{
    return ((FieldDesc*)field)->GetAttributes();
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

extern "C" EXPORT_API MonoReflectionField* EXPORT_CC mono_field_get_object (MonoDomain* domain, MonoClass* klass, MonoClassField* field)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
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

extern "C" EXPORT_API MonoType* EXPORT_CC mono_field_get_type(MonoClassField *field)
{
    CONTRACTL
    {
        PRECONDITION(field != NULL);
    }
    CONTRACTL_END;

    auto field_clr = (MonoClassField_clr*)field;

    MonoType_clr typeHandle = field_clr->GetFieldTypeHandleThrowing();

    return MonoType_clr_to_MonoType(typeHandle);
}

extern "C" EXPORT_API MonoType* EXPORT_CC mono_field_get_type_specific(MonoClassField *field, MonoClass* owner)
{
    CONTRACTL
    {
        PRECONDITION(field != NULL);
    }
    CONTRACTL_END;

    auto field_clr = (MonoClassField_clr*)field;
    auto klass_clr = (MonoClass_clr*)owner;

    MonoType_clr typeHandle = field_clr->GetExactFieldType(klass_clr);

    return MonoType_clr_to_MonoType(typeHandle);
}

extern "C" EXPORT_API void EXPORT_CC mono_field_get_value(MonoObject *obj, MonoClassField *field, void *value)
{
    TRACE_API("%p, %p, %p", obj, field, value);

    // TODO: Add contact
    // TODO: obj not protected?
    GCX_COOP();
    OBJECTREF objectRef = ObjectToOBJECTREF((MonoObject_clr*)obj);
    GCPROTECT_BEGIN(objectRef); // Is it really necessary in cooperative mode? for a GetInstanceField?
    {
        auto field_clr = (MonoClassField_clr*)field;
        field_clr->GetInstanceField(objectRef, value);
    }
    GCPROTECT_END();
}

extern "C" EXPORT_API void EXPORT_CC mono_field_set_value(MonoObject *obj, MonoClassField *field, void *value)
{
    TRACE_API("%p, %p, %p", obj, field, value);

    // TODO: Add contact
    // TODO: obj not protected?
    GCX_COOP();
    OBJECTREF objectRef = ObjectToOBJECTREF((MonoObject_clr*)obj);
    auto field_clr = ((MonoClassField_clr*)field);
    GCPROTECT_BEGIN(objectRef); // Is it really necessary in cooperative mode? for a GetInstanceField?
    {
        CorElementType fieldType = field_clr->GetFieldType();
        if (fieldType == ELEMENT_TYPE_CLASS)
            field_clr->SetInstanceField(objectRef, &value);
        else
            field_clr->SetInstanceField(objectRef, value);
    }
    GCPROTECT_END();
}

extern "C" EXPORT_API void EXPORT_CC mono_field_static_get_value(MonoVTable *vt, MonoClassField *field, void *value)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_gc_collect(int generation)
{
    FCALL_CONTRACT;
    _ASSERTE(generation >= -1);
    GCX_COOP();
#if 0
    if (mono_unity_gc_is_disabled())
        return;
#endif
    GCHeapUtilities::GetGCHeap()->GarbageCollect(generation, false, collection_blocking);
}

extern "C" EXPORT_API int EXPORT_CC mono_gc_collect_a_little ()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API gint64 EXPORT_CC mono_gc_get_heap_size()
{
    FCALL_CONTRACT;
    // NOT CORRECT
    return GCHeapUtilities::GetGCHeap()->GetTotalBytesInUse();
}

extern "C" EXPORT_API gint64 EXPORT_CC mono_gc_get_max_time_slice_ns ()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API gint64 EXPORT_CC mono_gc_get_used_size()
{
    FCALL_CONTRACT;
    return GCHeapUtilities::GetGCHeap()->GetTotalBytesInUse();
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_gc_is_incremental ()
{
    return false;
}

extern "C" EXPORT_API int EXPORT_CC mono_gc_max_generation()
{
    FCALL_CONTRACT;
    return GCHeapUtilities::GetGCHeap()->GetMaxGeneration();
}

extern "C" EXPORT_API void EXPORT_CC mono_gc_set_incremental (gboolean value)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_gc_set_max_time_slice_ns (gint64 maxTimeSlice)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_gc_wbarrier_generic_store(gpointer ptr, MonoObject* value)
{
}

extern "C" EXPORT_API void EXPORT_CC mono_gc_wbarrier_set_field (MonoObject * obj, gpointer field_ptr, MonoObject * value)
{
    GCX_COOP();

    SetObjectReference((OBJECTREF*)field_ptr, ObjectToOBJECTREF((MonoObject_clr*)value));
}


static guint32 handleId = 0;
struct MonoHandleInfo
{
    MonoHandleInfo() : Handle(0), Type((HandleType)-1)
    {
    }
    MonoHandleInfo(const MonoHandleInfo& copy) : Handle(copy.Handle), Type(copy.Type)
    {
    }
    uintptr_t Handle;
    HandleType Type;
};

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_gchandle_get_target_v2(uintptr_t gchandle)
{
    GCX_COOP();
    // TODO: This method is not accurate with Cooperative/Preemptive mode

    OBJECTHANDLE objectHandle = (OBJECTHANDLE)gchandle;
    OBJECTREF objref = ObjectFromHandle(objectHandle);
    return (MonoObject*)OBJECTREFToObject(objref);
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_gchandle_is_in_domain_v2(uintptr_t gchandle, MonoDomain *domain)
{
    // we only support one domain, so this should always be true.
    return true;
}

extern "C" EXPORT_API void EXPORT_CC mono_gchandle_free_v2(uintptr_t gchandle)
{
    OBJECTHANDLE objectHandle = (OBJECTHANDLE)gchandle;

    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfUnknownType(objectHandle);
}

extern "C" EXPORT_API uintptr_t EXPORT_CC mono_gchandle_new_v2(MonoObject *obj, gboolean pinned)
{
    TRACE_API("%p, %d", obj, pinned);
    CONTRACTL
    {
        PRECONDITION(obj != NULL);
    }
    CONTRACTL_END;

    GCX_COOP();
    // TODO: This method is not accurate with Cooperative/Preemptive mode

    auto objref = ObjectToOBJECTREF((MonoObject_clr*)obj);
    OBJECTHANDLE rawHandle = pinned ?
        GetAppDomain()->CreatePinningHandle(objref) :
        GetAppDomain()->CreateHandle(objref);

    return (uintptr_t)rawHandle;
}

extern "C" EXPORT_API uintptr_t EXPORT_CC mono_gchandle_new_weakref_v2(MonoObject *obj, gboolean track_resurrection)
{
    CONTRACTL
    {
        PRECONDITION(obj != NULL);
    }
    CONTRACTL_END;

    GCX_COOP();
    // TODO: This method is not accurate with Cooperative/Preemptive mode
    auto objref = ObjectToOBJECTREF((MonoObject_clr*)obj);
    OBJECTHANDLE rawHandle = track_resurrection ?
        GetAppDomain()->CreateLongWeakHandle(objref) :
        GetAppDomain()->CreateShortWeakHandle(objref);

    return (uintptr_t)rawHandle;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_array_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__ARRAY);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_boolean_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__BOOLEAN);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_byte_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__BYTE);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_char_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__CHAR);
}

extern "C" EXPORT_API MonoImage* EXPORT_CC mono_get_corlib()
{
    return (MonoImage*)CoreLibBinder::GetModule()->GetDomainAssembly()->GetAssembly();
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_double_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__DOUBLE);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_enum_class()
{
    MonoImage* img = mono_get_corlib();
    return mono_class_from_name(img, "System", "Enum");
}

extern "C" EXPORT_API MonoException* EXPORT_CC mono_get_exception_argument_null(const char *arg)
{
    GCX_COOP();
    SString sarg(SString::Utf8, arg);
    EEArgumentException* ee = new EEArgumentException(kArgumentNullException, sarg.GetUnicode(), W("ArgumentNull_Generic"));
    return (MonoException*)OBJECTREFToObject(ee->GetThrowable());
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_exception_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__EXCEPTION);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_int16_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__INT16);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_int32_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__INT32);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_int64_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__INT64);
}

extern "C" EXPORT_API MonoMethod* EXPORT_CC mono_get_method (MonoImage * image, guint32 token, MonoClass * klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_object_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__OBJECT);
}

extern "C" EXPORT_API MonoDomain* EXPORT_CC mono_get_root_domain()
{
    TRACE_API("", NULL);
    return gRootDomain;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_single_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__SINGLE);
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_get_string_class()
{
    return (MonoClass*)CoreLibBinder::GetClass(CLASS__STRING);
}

extern "C" EXPORT_API void EXPORT_CC mono_image_close(MonoImage *image)
{
    // NOP
}

extern "C" EXPORT_API MonoAssembly* EXPORT_CC mono_image_get_assembly(MonoImage *image)
{
    return (MonoAssembly*)image;
}

extern "C" EXPORT_API const char* EXPORT_CC mono_image_get_filename(MonoImage *image)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API const char* EXPORT_CC mono_image_get_name(MonoImage *image)
{
    TRACE_API("%p", image);
    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(image != NULL);
    }
    CONTRACTL_END;

    return reinterpret_cast<MonoAssembly_clr*>(image)->GetSimpleName();
}

extern "C" EXPORT_API const MonoTableInfo* EXPORT_CC mono_image_get_table_info (MonoImage * image, int table_id)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API int EXPORT_CC mono_image_get_table_rows(MonoImage *image, int table_id)
{
    if (table_id == MONO_TABLE_TYPEDEF)
    {
        DomainAssembly* domainAssembly = reinterpret_cast<MonoImage_clr*>(image)->GetDomainAssembly();
        return domainAssembly->GetModule()->GetNumTypeDefs() - 1;
    }


    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API MonoImage* EXPORT_CC mono_image_loaded(const char *name)
{
    if (g_LoadedImages == NULL)
        g_LoadedImages = new SArray<LoadedImage>;
    for (COUNT_T i=0; i<g_LoadedImages->GetCount(); i++)
    {
        if (strcmp((*g_LoadedImages)[i].name, name) == 0)
            return (*g_LoadedImages)[i].image;
    }
    return NULL;
}

extern "C" EXPORT_API MonoImage* EXPORT_CC mono_image_open_from_data_full(const void *data, guint32 data_len, gboolean need_copy, int *status, gboolean ref_only)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoImage* EXPORT_CC mono_image_open_from_data_with_name(char *data, guint32 data_len, gboolean need_copy, int *status, gboolean refonly, const char *name)
{
    TRACE_API("%p, %d, %d, %p, %d, %s", data, data_len, need_copy, status, refonly, name);

    gint64 len = data_len;
    auto domainAssembly = (DomainAssembly*)g_HostStruct->load_assembly_from_data(data, len);
    if (domainAssembly == NULL)
        return NULL;

    auto assembly = domainAssembly->GetAssembly();

    assembly->GetDomainAssembly()->SetCustomPath(name);

    assembly->EnsureActive();

    if (g_LoadedImages == NULL)
        g_LoadedImages = new SArray<LoadedImage>;
    g_LoadedImages->Append(LoadedImage(name, (MonoImage*)assembly, mono_domain_get()));

    return (MonoImage*)assembly;
}

extern "C" EXPORT_API const char* EXPORT_CC mono_image_strerror (int status)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_is_debugger_attached(void)
{
    return FALSE;
}


extern "C" EXPORT_API int EXPORT_CC mono_jit_info_get_code_size(void* jit)
{
    ASSERT_NOT_IMPLEMENTED;
    // TODO used 1 by instrumentation unity/mono profiler
    // Runtime\Profiler\Instrumentation\InstrumentationProfiler.cpp(292)
    return 0;
}

extern "C" EXPORT_API void* EXPORT_CC mono_jit_info_get_code_start(void* jit)
{
    ASSERT_NOT_IMPLEMENTED;
    // TODO used 1 by instrumentation unity/mono profiler
    // Runtime\Profiler\Instrumentation\InstrumentationProfiler.cpp(292)
    return NULL;
}

extern "C" EXPORT_API MonoJitInfo* EXPORT_CC mono_jit_info_table_find(MonoDomain* domain, void* ip)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoDomain* EXPORT_CC mono_jit_init(const char *file)
{
    TRACE_API("%s", file);

    return mono_jit_init_version(file, "4.0");
}

typedef int32_t (*initialize_func)(HostStruct* s, int32_t size);

void list_tpa(const SString& searchPath, SString& tpa)
{
    SString searchPattern = searchPath;
    searchPattern += W("/*.dll");
    WIN32_FIND_DATAW findData;
    HANDLE fileHandle = FindFirstFileW(searchPattern.GetUnicode(), &findData);

    if (fileHandle != INVALID_HANDLE_VALUE)
    {
        do
        {
            tpa.Append(searchPath);
            tpa.Append(W("/"));
            tpa.Append(findData.cFileName);
            tpa += PATH_SEPARATOR;
        } while (FindNextFileW(fileHandle, &findData));
        FindClose(fileHandle);
    }
}

extern "C" EXPORT_API MonoDomain* EXPORT_CC mono_jit_init_version(const char *file, const char* runtime_version)
{
    #if defined(TARGET_UNIX)
#if defined(__APPLE__)
    GCHeapUtilities::SetGCName("libunitygc.dylib");
#else
    GCHeapUtilities::SetGCName("libunitygc.so");
#endif
#else
    GCHeapUtilities::SetGCName("unitygc.dll");
#endif

    g_add_internal_lock.Init(CrstMonoICalls);

    HRESULT hr;

    if (!g_CLRRuntimeHost)
    {
        const char* entrypointExecutable = "/dev/null";
#if defined(__APPLE__) || defined(__linux__)
        uint32_t lenActualPath = 0;
        /*if (_NSGetExecutablePath(nullptr, &lenActualPath) == -1)
        {
            // OSX has placed the actual path length in lenActualPath,
            // so re-attempt the operation
            entrypointExecutable = new char[lenActualPath + 1];
            entrypointExecutable[lenActualPath] = '\0';
            if (_NSGetExecutablePath(entrypointExecutable, &lenActualPath) == -1)
            {
                delete [] entrypointExecutable;
                return nullptr;
            }
        }
        else
        {
            return nullptr;
        }*/
#endif

        SString appPath (*s_AssemblyDir);

        SString etcPath (*s_EtcDir);

        SString assemblyPaths (*s_AssemblyPaths);

        SString tpa;
        list_tpa(appPath, tpa);

        SString appPaths;
        appPaths += appPath;
        appPaths += PATH_SEPARATOR;
        appPaths += assemblyPaths;

        SString appNiPaths;
        appNiPaths += appPath;
        appNiPaths+= PATH_SEPARATOR;
        appNiPaths += appPath;

        SString nativeDllSearchDirs;
        nativeDllSearchDirs += appPath;
        nativeDllSearchDirs += PATH_SEPARATOR;
        nativeDllSearchDirs += etcPath;

        LPCSTR property_keys2[] = {
            "TRUSTED_PLATFORM_ASSEMBLIES",
            "APP_PATHS",
            "APP_NI_PATHS",
            "NATIVE_DLL_SEARCH_DIRECTORIES"
        };

        StackScratchBuffer buf1;
        StackScratchBuffer buf2;
        StackScratchBuffer buf3;
        StackScratchBuffer buf4;
        LPCSTR property_values2[] = {
                  tpa.GetUTF8(buf1),
                  appPaths.GetUTF8(buf2),
                  appNiPaths.GetUTF8(buf3),
                  nativeDllSearchDirs.GetUTF8(buf4)
        };

        hr = coreclr_initialize (entrypointExecutable, file, 4, property_keys2, property_values2, &g_CLRRuntimeHost, &g_RootDomainId);

        if(FAILED(hr))
        {
            return nullptr;
        }
    }

    initialize_func init_func;
    hr = coreclr_create_delegate(g_CLRRuntimeHost, g_RootDomainId, "unity-embed-host", "Unity.CoreCLRHelpers.CoreCLRHost", "InitMethod", (void**)&init_func);
    if(FAILED(hr))
    {
        return nullptr;
    }

    g_HostStruct = (HostStruct*)malloc(sizeof(HostStruct));
    memset(g_HostStruct, 0, sizeof(HostStruct));
    g_HostStruct->version = 1;

    size_t size = sizeof(HostStruct);
    hr = init_func(g_HostStruct, (int32_t)size);

    AppDomain *pCurDomain = SystemDomain::GetCurrentDomain();
    gRootDomain = gCurrentDomain = (MonoDomain*)pCurDomain;


    //coreClrHelperAssembly->EnsureActive();
    //gCoreCLRHelperAssembly = (MonoImage*)coreClrHelperAssembly;
    //gALCWrapperClass = mono_class_from_name(gCoreCLRHelperAssembly, "Unity.CoreCLRHelpers", "ALCWrapper");
    //gALCWrapperObject = mono_object_new(NULL, gALCWrapperClass);
    //mono_runtime_object_init(gALCWrapperObject);
    //gALCWrapperLoadFromAssemblyPathMethod = mono_class_get_method_from_name(gALCWrapperClass, "CallLoadFromAssemblyPath", 1);
    //gALCWrapperLoadFromAssemblyDataMethod = mono_class_get_method_from_name(gALCWrapperClass, "CallLoadFromAssemblyData", 2);
    //gALCWrapperDomainUnloadNotificationMethod = mono_class_get_method_from_name(gALCWrapperClass, "DomainUnloadNotification", 0);
    //gALCWrapperInitUnloadMethod = mono_class_get_method_from_name(gALCWrapperClass, "InitUnload", 0);
    //gALCWrapperFinishUnloadMethod = mono_class_get_method_from_name(gALCWrapperClass, "FinishUnload", 1);
    //gALCWrapperCheckRootForUnloadingMethod = mono_class_get_method_from_name(gALCWrapperClass, "CheckRootForUnloading", 2);
    //gALCWrapperCheckAssemblyForUnloadingMethod = mono_class_get_method_from_name(gALCWrapperClass, "CheckAssemblyForUnloading", 1);
    //gALCWrapperAddPathMethod = mono_class_get_method_from_name(gALCWrapperClass, "AddPath", 2);

    //gCurrentDomain = CreateMonoDomainFromObject(gALCWrapperObject);
    //SetupDomainPaths(gALCWrapperObject);
    //gRootDomain = gCurrentDomain;

    mono_add_internal_call("Unity.CoreCLRHelpers.ALCWrapper::InvokeFindPluginCallback", (gconstpointer)InvokeFindPluginCallback);

/*
    FrameWithCookie<GCNativeFrame>* frame = (FrameWithCookie<GCNativeFrame>*)malloc(sizeof(FrameWithCookie<GCNativeFrame>));
    new (frame) FrameWithCookie<GCNativeFrame> ();
    pCurrentThreadNativeFrame = &(*frame);
    frame->Push();*/


    TRACE_API("%s, %s", file, runtime_version);
    return gCurrentDomain;
}

extern "C" EXPORT_API void EXPORT_CC mono_jit_parse_options(int argc, char * argv[])
{
}

extern "C" EXPORT_API void EXPORT_CC mono_metadata_decode_row (const MonoTableInfo * t, int idx, guint32 * res, int res_size)
{
    ASSERT_NOT_IMPLEMENTED;
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
    ASSERT_NOT_IMPLEMENTED;
    return FALSE;
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
    StackScratchBuffer buffer;
    return _strdup(fullName.GetUTF8(buffer));
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

extern "C" EXPORT_API MonoReflectionMethod* EXPORT_CC mono_method_get_object(MonoDomain *domain, MonoMethod *method, MonoClass *refclass)
{
    GCX_COOP();

    MonoMethod_clr* clrMethod = reinterpret_cast<MonoMethod_clr*>(method);

    REFLECTMETHODREF refRet = clrMethod->GetStubMethodInfo();
    _ASSERTE(clrMethod->IsRuntimeMethodHandle());
    MonoObject* stubMethodInfo = (MonoObject*)OBJECTREFToObject(refRet);
    MonoClass* runtimeType = (MonoClass*)ClassLoader::LoadTypeByNameThrowing(CoreLibBinder::GetModule()->GetAssembly(), "System", "RuntimeType").AsMethodTable();
    MonoMethod* getmethodbase = mono_class_get_method_from_name(runtimeType, "GetMethodBase", 1);
    void* params[1] = { stubMethodInfo };
    MonoObject* returnValue = mono_runtime_invoke(getmethodbase, nullptr, params, nullptr);
    return (MonoReflectionMethod*)returnValue;
}

extern "C" EXPORT_API MonoMethodSignature* EXPORT_CC mono_method_signature(MonoMethod *method)
{
    return (MonoMethodSignature*)method;
}

extern "C" EXPORT_API MonoMethodSignature* EXPORT_CC mono_method_signature_checked (MonoMethod * method, MonoError * error)
{
    return (MonoMethodSignature*)method;
}

extern "C" EXPORT_API MonoMethodSignature* EXPORT_CC mono_method_signature_checked_slow (MonoMethod * method, MonoError * error)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_object_get_class(MonoObject *obj)
{
    MonoClass_clr* klass = reinterpret_cast<MonoObject_clr*>(obj)->GetMethodTable();
    return (MonoClass*)klass;
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_object_get_size(MonoObject *obj)
{
    return (guint32)reinterpret_cast<MonoObject_clr*>(obj)->GetSize();
}

extern "C" EXPORT_API MonoMethod* EXPORT_CC mono_object_get_virtual_method(MonoObject *obj, MonoMethod *method)
{
    TRACE_API("%x, %x", obj, method);

    MonoClass * klass = mono_object_get_class(obj);
    MonoType * type = mono_class_get_type(klass);
    if (mono_type_get_type(type) == MONO_TYPE_CLASS)
        return method;

    MonoClass_clr* klass_clr = (MonoClass_clr*)klass;
    MonoMethodSignature* sig = mono_method_signature(method);
    MonoMethod *m2 = mono_class_get_method_from_name(klass, mono_method_get_name(method), mono_signature_get_param_count(sig));

    return m2;
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_object_isinst(MonoObject *obj, MonoClass* klass)
{
    MonoClass* clazz = mono_object_get_class(obj);
    if (mono_class_is_subclass_of(clazz, klass, TRUE))
        return obj;
    return NULL;
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_object_new(MonoDomain *domain, MonoClass *klass)
{
    TRACE_API("%x, %x", domain, klass);

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    {
        GCX_COOP();
        OBJECTREF objectRef = AllocateObject((MethodTable*)klass);
        return (MonoObject*)OBJECTREFToObject(objectRef);
    }
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_object_new_alloc_specific(MonoVTable *vtable)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_object_new_specific(MonoVTable *vtable)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API gpointer EXPORT_CC mono_object_unbox(MonoObject* o)
{
    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(o != NULL);
    }
    CONTRACTL_END;

    return (gpointer)reinterpret_cast<MonoObject_clr*>(o)-> UnBox();
}

extern "C" EXPORT_API int EXPORT_CC mono_parse_default_optimizations(const char* p)
{
    // NOP
    return 0;
}

extern "C" EXPORT_API char* EXPORT_CC mono_pmip(void *ip)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void* EXPORT_CC mono_profiler_create (MonoProfiler* prof)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_install(void *prof, MonoProfileFunc shutdown_callback)
{
    // NOP
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_install_allocation(MonoProfileAllocFunc callback)
{
    // NOP
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_install_enter_leave(MonoProfileMethodFunc enter, MonoProfileMethodFunc fleave)
{
    // NOP
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_install_gc(MonoProfileGCFunc callback, MonoProfileGCResizeFunc heap_resize_callback)
{
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_install_jit_end(MonoProfileJitResult jit_end)
{
    // NOP
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_install_thread(MonoProfileThreadFunc start, MonoProfileThreadFunc end)
{
    // NOP
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_load (const char *desc)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_profiler_set_events(int events)
{
    // NOP
}


extern "C" EXPORT_API MonoMethod* EXPORT_CC mono_property_get_get_method(MonoProperty *prop)
{
    return (MonoMethod*)prop;
}

extern "C" EXPORT_API void EXPORT_CC mono_raise_exception(MonoException *ex)
{
    ASSERT_NOT_IMPLEMENTED;
}


extern "C" EXPORT_API MonoArray* EXPORT_CC mono_reflection_get_custom_attrs_by_type(MonoObject* object, MonoClass* klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}


extern "C" EXPORT_API void EXPORT_CC mono_runtime_cleanup(MonoDomain *domain)
{
    ASSERT_NOT_IMPLEMENTED;
    //TODO not used
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_runtime_delegate_invoke(MonoObject *delegate, void **params, MonoException **exc)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API int EXPORT_CC mono_runtime_exec_main(MonoMethod *method, MonoArray *args, MonoObject **exc)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_runtime_invoke(MonoMethod *method, void *obj, void **params, MonoException **exc)
{
    TRACE_API("%p, %p, %p, %p", method, obj, params, exc);

    if (obj == nullptr)
        return mono_runtime_invoke_with_nested_object(method, nullptr, nullptr, params, exc);
    MonoClass_clr * klass = (MonoClass_clr*)mono_object_get_class((MonoObject*)obj);
    auto method_clr = (MonoMethod_clr*)method;
    if (klass->IsValueType())// && !method_clr->IsVtableMethod())
        return mono_runtime_invoke_with_nested_object(method, (char*)obj + sizeof(Object), obj, params, exc);
    else
        return mono_runtime_invoke_with_nested_object(method, obj, obj, params, exc);
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_runtime_invoke_array(MonoMethod *method, void *obj, MonoArray *params, MonoException **exc)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_runtime_invoke_with_nested_object(MonoMethod *method, void *obj, void *parentobj, void **params, MonoException **exc)
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
        StackScratchBuffer buffer;
        printf("Exception calling %s: %s\n", mono_method_get_name(method), sstr.GetUTF8(buffer));
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

extern "C" EXPORT_API gboolean EXPORT_CC mono_runtime_is_shutting_down()
{
    ASSERT_NOT_IMPLEMENTED;
    //TODO not used
    return FALSE;
}

extern "C" EXPORT_API void EXPORT_CC mono_runtime_object_init(MonoObject *this_obj)
{
    TRACE_API("%x", this_obj);

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(this_obj != NULL);
    }
    CONTRACTL_END;

    GCX_COOP();

    // TODO check what to do with the exception thrown by CallDefaultConstructor
    OBJECTREF objref = ObjectToOBJECTREF((MonoObject_clr*)this_obj);
    GCPROTECT_BEGIN(objref);
    {
        CallDefaultConstructor(objref);
    }
    GCPROTECT_END();
}

extern "C" EXPORT_API void EXPORT_CC mono_runtime_set_shutting_down()
{
    ASSERT_NOT_IMPLEMENTED;
    //TODO used once in Runtime\Mono\MonoManager.cpp CleanupMono()
}

extern "C" EXPORT_API void EXPORT_CC mono_runtime_unhandled_exception_policy_set(MonoRuntimeUnhandledExceptionPolicy policy)
{
    ASSERT_NOT_IMPLEMENTED;
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

extern "C" EXPORT_API void EXPORT_CC mono_set_break_policy(MonoBreakPolicyFunc policy_callback)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_set_crash_chaining (gboolean)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_set_defaults(int verbose_level, guint32 opts)
{
    // NOP
}

extern "C" EXPORT_API void EXPORT_CC mono_set_dirs(const char *assembly_dir, const char *config_dir)
{
    s_AssemblyDir = new SString(SString::Utf8, assembly_dir);
    s_EtcDir = new SString(SString::Utf8, config_dir);
}

extern "C" EXPORT_API void EXPORT_CC
mono_set_find_plugin_callback (gconstpointer find)
{
	unity_find_plugin_callback = (UnityFindPluginCallback)find;
}

extern "C" EXPORT_API void EXPORT_CC mono_set_ignore_version_and_key_when_finding_assemblies_already_loaded(gboolean value)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_set_signal_chaining(gboolean)
{
    // NOP
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
    return msig.HasThis();
}

typedef gboolean(*MonoStackWalk) (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data);
extern "C" EXPORT_API void EXPORT_CC mono_stack_walk(MonoStackWalk func, gpointer user_data)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_stack_walk_no_il (MonoStackWalk start, void* user_data)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API MonoString* EXPORT_CC mono_string_from_utf16(const gunichar2* text)
{
    assert(text != nullptr);
    InlineSString<256> sstr((const WCHAR*)text);
    GCX_COOP();
    return (MonoString*)OBJECTREFToObject(AllocateString(sstr));
}

extern "C" EXPORT_API MonoString* EXPORT_CC mono_string_new_len(MonoDomain *domain, const char *text, guint32 length)
{
    assert(text != nullptr);
    InlineSString<256> sstr(SString::Utf8, text, length);
    GCX_COOP();
    STRINGREF strObj = AllocateString(sstr.GetCount());
    memcpyNoGCRefs(strObj->GetBuffer(), sstr.GetUnicode(), sstr.GetCount() * sizeof(WCHAR));
    return (MonoString*)OBJECTREFToObject(strObj);
}

extern "C" EXPORT_API MonoString* EXPORT_CC mono_string_new_utf16(MonoDomain * domain, const guint16 * text, gint32 length)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoString* EXPORT_CC mono_string_new_wrapper(const char* text)
{
    assert(text != nullptr);
    InlineSString<256> sstr(SString::Utf8, text);
    GCX_COOP();
    return (MonoString*)OBJECTREFToObject(AllocateString(sstr));
}

extern "C" EXPORT_API gunichar2* EXPORT_CC mono_string_to_utf16(MonoString *string_obj)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API char* EXPORT_CC mono_string_to_utf8(MonoString *string_obj)
{
    SString sstr;
    ((StringObject*)string_obj)->GetSString(sstr);
    StackScratchBuffer buffer;
    return _strdup(sstr.GetUTF8(buffer));
}

extern "C" EXPORT_API char* EXPORT_CC mono_stringify_assembly_name(MonoAssemblyName *aname)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoThread* EXPORT_CC mono_thread_attach(MonoDomain *domain)
{
    auto domain_clr = (MonoDomain_clr*)domain;
    MonoThread_clr* currentThread = GetThreadNULLOk();

    if (currentThread == nullptr)
    {
        currentThread = SetupThreadNoThrow();
    }

    assert(currentThread != nullptr);
    //assert(domain_clr->CanThreadEnter(currentThread));
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

extern "C" EXPORT_API MonoThread* EXPORT_CC mono_thread_exit()
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_thread_has_sufficient_execution_stack (void)
{
    ASSERT_NOT_IMPLEMENTED;
    return FALSE;
}

extern "C" EXPORT_API void EXPORT_CC mono_thread_pool_cleanup()
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_thread_pop_appdomain_ref()
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_thread_push_appdomain_ref(MonoDomain *domain)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_thread_set_main(MonoThread* thread)
{
    CONTRACTL{
        PRECONDITION(thread != nullptr);
    } CONTRACTL_END;
    auto thread_clr = (MonoThread_clr*)thread;
    // almost NOP
    //assert(AppDomain::GetCurrentDomain()->CanThreadEnter(thread_clr));
}

extern "C" EXPORT_API void EXPORT_CC mono_thread_suspend_all_other_threads()
{
    ASSERT_NOT_IMPLEMENTED;
    //TODO used once in Runtime\Mono\MonoManager.cpp CleanupMono()
}

extern "C" EXPORT_API void EXPORT_CC mono_threads_set_shutting_down()
{
    ASSERT_NOT_IMPLEMENTED;
    //TODO used once in Runtime\Mono\MonoManager.cpp CleanupMono()
}

extern "C" EXPORT_API void EXPORT_CC mono_trace_set_level_string(const char *value)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_trace_set_log_handler (MonoLogCallback callback, void *user_data)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_trace_set_mask_string(const char *value)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API guint32 EXPORT_CC mono_type_get_attrs (MonoType * type)
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_type_get_class(MonoType *type)
{
    TypeHandle handle = TypeHandle::FromPtr((PTR_VOID)type);
    return (MonoClass*)handle.AsMethodTable();
}

extern "C" EXPORT_API MonoType* EXPORT_CC mono_type_get_generic_arg(MonoType *type, int index)
{
    TypeHandle handle = TypeHandle::FromPtr((PTR_VOID)type);
    Instantiation inst = handle.GetInstantiation();
    return (MonoType*)inst[index].AsPtr();
}

extern "C" EXPORT_API char* EXPORT_CC mono_type_get_name(MonoType *type)
{
    TypeHandle handle = TypeHandle::FromPtr((PTR_VOID)type);
    SString ssBuf;
    handle.GetName(ssBuf);
    StackScratchBuffer buffer;
    return _strdup(ssBuf.GetUTF8(buffer));
}

extern "C" EXPORT_API char* EXPORT_CC mono_type_get_name_full(MonoType *type, MonoTypeNameFormat format)
{
    TRACE_API("%p %d", type, format);
    if (format != MonoTypeNameFormat::MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED)
    {
        ASSERT_NOT_IMPLEMENTED;
        return NULL;
    }

    TypeHandle handle = TypeHandle::FromPtr((PTR_VOID)type);
    SString ssBuf;
    TypeString::AppendType(ssBuf, handle, TypeString::FormatNamespace | TypeString::FormatAssembly | TypeString::FormatFullInst);

    StackScratchBuffer buffer;
    return _strdup(ssBuf.GetUTF8(buffer));
}

extern "C" EXPORT_API int EXPORT_CC mono_type_get_num_generic_args(MonoType *type)
{
    TypeHandle handle = TypeHandle::FromPtr((PTR_VOID)type);
    Instantiation inst = handle.GetInstantiation();
    return inst.GetNumArgs();
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_type_get_object(MonoDomain *domain, MonoType *type)
{
    TypeHandle clrType = TypeHandle::FromPtr(reinterpret_cast<PTR_VOID>(type));
    GCX_COOP();
    return (MonoObject*) OBJECTREFToObject(clrType.GetManagedClassObject());
}

extern "C" EXPORT_API int EXPORT_CC mono_type_get_type(MonoType *type)
{
retry:

    if (type == mono_class_get_type(mono_get_string_class()))
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

extern "C" EXPORT_API gboolean EXPORT_CC mono_type_is_byref (MonoType * type)
{
    TypeHandle clrType = TypeHandle::FromPtr(reinterpret_cast<PTR_VOID>(type));
    return clrType.IsByRef();
}

extern "C" EXPORT_API uint32_t EXPORT_CC mono_unity_allocation_granularity ()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API MonoArray* EXPORT_CC mono_unity_array_new_2d(MonoDomain * domain, MonoClass * eclass, size_t size0, size_t size1)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}


extern "C" EXPORT_API MonoArray* EXPORT_CC mono_unity_array_new_3d(MonoDomain * domain, MonoClass * eclass, size_t size0, size_t size1, size_t size2)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API uint32_t EXPORT_CC mono_unity_array_object_header_size ()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_assembly_mempool_chunk_foreach (MonoAssembly * assembly, MonoDataFunc callback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

#if defined(HOST_OSX) || defined(HOST_UNIX)
extern "C" EXPORT_API int EXPORT_CC mono_unity_backtrace_from_context(void* context, void* array[], int count)
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}
#endif

extern "C" EXPORT_API MonoManagedMemorySnapshot* EXPORT_CC mono_unity_capture_memory_snapshot ()
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_class_field_is_literal (MonoClassField * field)
{
    ASSERT_NOT_IMPLEMENTED;
    return FALSE;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_class_for_each (MonoClassFunc callback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_unity_class_get(MonoImage * image, guint32 type_token)
{
    DomainAssembly* domainAssembly = reinterpret_cast<MonoImage_clr*>(image)->GetDomainAssembly();
    TypeHandle th = domainAssembly->GetModule()->LookupTypeDef(type_token);
    if (th.IsNull())
        th = domainAssembly->GetModule()->LookupFullyCanonicalInstantiation(type_token);
    if (th.IsNull())
    {
        return (MonoClass*)ClassLoader::LoadTypeDefOrRefThrowing(domainAssembly->GetModule(), type_token,
            ClassLoader::ReturnNullIfNotFound,
            ClassLoader::PermitUninstDefOrRef,
            tdNoTypes).AsMethodTable();

    }
    return (MonoClass*)th.AsMethodTable();
}

extern "C" EXPORT_API uint32_t EXPORT_CC mono_unity_class_get_data_size (MonoClass * klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API MonoClass* EXPORT_CC mono_unity_class_get_generic_type_definition (MonoClass * klass)
{
    CONTRACTL{
        PRECONDITION(klass != nullptr);
    } CONTRACTL_END;
    // there must be a better way!
    return mono_class_from_name(mono_class_get_image(klass), mono_class_get_namespace(klass), mono_class_get_name(klass));
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_class_has_failure (MonoClass * klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_class_is_abstract(MonoClass* klass)
{
    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    return reinterpret_cast<MonoClass_clr*>(klass)->IsAbstract() ? TRUE : FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_class_is_interface(MonoClass* klass)
{
    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    PRECONDITION(klass != NULL);
    }
    CONTRACTL_END;

    return reinterpret_cast<MonoClass_clr*>(klass)->IsInterface() ? TRUE : FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_class_is_open_constructed_type (MonoClass * klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return FALSE;
}

extern "C" EXPORT_API MonoVTable* EXPORT_CC mono_unity_class_try_get_vtable (MonoDomain * domain, MonoClass * klass)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoArray* EXPORT_CC mono_unity_custom_attrs_construct (MonoCustomAttrInfo * cinfo, MonoError * error)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_domain_mempool_chunk_foreach (MonoDomain * domain, MonoDataFunc callback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_domain_set_config(MonoDomain * domain, const char *base_dir, const char *config_file_name)
{
    // NOP
}

extern "C" EXPORT_API MonoException* EXPORT_CC mono_unity_error_convert_to_exception (MonoError * error)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoClassField* EXPORT_CC mono_unity_field_from_token_checked (MonoImage * image, guint32 token, MonoClass** retklass, MonoGenericContext * context, MonoError * error)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_free_captured_memory_snapshot (MonoManagedMemorySnapshot * snapshot)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_g_free(void* p)
{
    free(p);
}

int gc_disabled = 0;

extern "C" EXPORT_API void EXPORT_CC mono_unity_gc_disable ()
{
    TRACE_API("", NULL);

    FCALL_CONTRACT;
    GCX_COOP();
    if (gc_disabled == 0)
        GCHeapUtilities::GetGCHeap()->StartNoGCRegion(16*1024*1024, false, 0, true);
    gc_disabled++;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_gc_enable ()
{
    TRACE_API("", NULL);

    FCALL_CONTRACT;
    GCX_COOP();
    if (gc_disabled == 1)
        GCHeapUtilities::GetGCHeap()->EndNoGCRegion();
    gc_disabled--;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_gc_handles_foreach_get_target (MonoDataFunc callback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_gc_heap_foreach (MonoDataFunc callback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API int EXPORT_CC mono_unity_gc_is_disabled ()
{
    return gc_disabled != 0;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_gc_set_mode (MonoGCMode mode)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_image_set_mempool_chunk_foreach (MonoDataFunc callback, void* userdata)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_jit_cleanup(MonoDomain *domain)
{
}

extern "C" EXPORT_API void* EXPORT_CC mono_unity_liveness_allocate_struct(MonoClass* filter, int max_object_count, mono_register_object_callback callback, void* userdata, mono_liveness_reallocate_callback reallocate)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_liveness_calculation_from_root(MonoObject* root, void* state)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_liveness_calculation_from_statics(void* state)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_liveness_finalize(void* state)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_liveness_free_struct(void* state)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API MonoException* EXPORT_CC mono_unity_loader_get_last_error_and_error_prepare_exception()
{
    //ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API int EXPORT_CC mono_unity_managed_callstack(unsigned char* buffer, int bufferSize, const MonoUnityCallstackOptions * opts)
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API uint32_t EXPORT_CC mono_unity_object_header_size()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API uint32_t EXPORT_CC mono_unity_offset_of_array_bounds_in_array_object_header()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API uint32_t EXPORT_CC mono_unity_offset_of_array_length_in_array_object_header()
{
    ASSERT_NOT_IMPLEMENTED;
    return 0;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_root_domain_mempool_chunk_foreach(MonoDataFunc callback, void* userdata)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_runtime_set_main_args(int, const char* argv[])
{
    // NOP
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_set_data_dir (const char * dir)
{
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_set_embeddinghostname(const char* name)
{
    // NOP
}

typedef void(*vprintf_func)(const char* msg, va_list args);
extern "C" EXPORT_API void EXPORT_CC mono_unity_set_vprintf_func(vprintf_func func)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_start_gc_world()
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_stop_gc_world()
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API MonoString* EXPORT_CC mono_unity_string_empty_wrapper()
{
    return mono_string_new_wrapper("");
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_type_get_name_full_chunked(MonoType * type, MonoDataFunc appendCallback, void* userData)
{
    ASSERT_NOT_IMPLEMENTED;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_type_is_pointer_type(MonoType * type)
{
    ASSERT_NOT_IMPLEMENTED;
    return FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC mono_unity_type_is_static(MonoType * type)
{
    ASSERT_NOT_IMPLEMENTED;
    return FALSE;
}

extern "C" EXPORT_API void* EXPORT_CC mono_unity_vtable_get_static_field_data(MonoVTable * vTable)
{
    ASSERT_NOT_IMPLEMENTED;
    return NULL;
}

extern "C" EXPORT_API MonoObject* EXPORT_CC mono_value_box(MonoDomain *domain, MonoClass *klass, gpointer val)
{
    GCX_COOP();

    TRACE_API("%p, %p, %p", domain, klass, val);

    MonoClass_clr* classClr = (MonoClass_clr*)klass;

    return (MonoObject*)OBJECTREFToObject(classClr->Box(val));
}

extern "C" EXPORT_API void EXPORT_CC mono_verifier_set_mode(MiniVerifierMode)
{
    // NOP
    //TODO used in Runtime\Mono\MonoManager.cpp SetSecurityMode()
}


extern "C" EXPORT_API gboolean EXPORT_CC unity_mono_method_is_generic(MonoMethod* method)
{
    CONTRACTL{
        PRECONDITION(method != nullptr);
    } CONTRACTL_END;
    auto method_clr = (MonoMethod_clr*)method;

    return method_clr->IsGenericMethodDefinition() ? TRUE : FALSE;
}

extern "C" EXPORT_API gboolean EXPORT_CC unity_mono_method_is_inflated(MonoMethod* method)
{
    CONTRACTL{
        PRECONDITION(method != nullptr);
    } CONTRACTL_END;
    auto method_clr = (MonoMethod_clr*)method;
    // TODO: is it really the concept behind inflated? (generic instance?)
    auto isgeneric = method_clr->GetNumGenericMethodArgs() > 0
        && !method_clr->IsGenericMethodDefinition();

    return isgeneric ? TRUE : FALSE;
}


extern "C" EXPORT_API MonoMethod* EXPORT_CC unity_mono_reflection_method_get_method(MonoReflectionMethod* mrf)
{
    return (MonoMethod*)((ReflectMethodObject*)mrf)->GetMethod();
}

#ifdef _DEBUG
extern "C" void EXPORT_CC mono_debug_assert_dialog(const char *szFile, int iLine, const char *szExpr)
{
    DbgAssertDialog(szFile, iLine, szExpr);
}
#endif


extern "C" EXPORT_API void EXPORT_CC mono_unity_domain_unload(MonoDomain * domain, MonoUnityExceptionFunc callback)
{
    TRACE_API("%p %p", domain, callback);

#if UNITY_SUPPORT_DOMAIN_UNLOAD
    MonoObject *exc = domain_unload(domain);
    if (exc)
        callback(exc);
#else
    ASSERT_NOT_IMPLEMENTED;
#endif
}


// mono_thread_attach/mono_thread_detach does a full managed thread setup each time.
// This is too slow for wrapping it around any managed job. So, in the "fast" versions,
// we don't actually bother with detaching and attaching the thread at all. However,
// we need to make sure we leave GC in preemptive modewhen detaching, so the thread
// can be suspended by the GC at any point.
extern "C" EXPORT_API void EXPORT_CC mono_unity_thread_fast_attach (MonoDomain * domain)
{
    gCurrentDomain = domain;
}

extern "C" EXPORT_API void EXPORT_CC mono_unity_thread_fast_detach ()
{
    gCurrentDomain = NULL;
    GetThread()->EnablePreemptiveGC();
}

