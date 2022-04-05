#include <algorithm>
#include <assert.h>
#include <cstdint>
#include <cstdlib>
#include <iostream>
#include <list>
#include <stdio.h>
#include <string.h>
#include <string>
#ifndef WIN32
#include <dlfcn.h>
#include <unistd.h>
#endif
//#include "unittest-cpp/UnitTest++/UnitTest++.h"
#define CATCH_CONFIG_RUNNER
#include "catch/catch.hpp"

#define UNITY_EDITOR 1
#define USE_CORECLR

typedef signed short SInt16;
typedef unsigned short UInt16;
typedef unsigned char UInt8;
typedef signed char SInt8;
typedef signed int SInt32;
typedef unsigned int UInt32;
typedef signed long long SInt64;
typedef unsigned long long UInt64;
typedef void* mono_register_object_callback;
typedef void* mono_liveness_world_state_callback;
const int MONO_TABLE_TYPEDEF = 2;               // mono/metadata/blob.h
const int MONO_TOKEN_TYPE_DEF = 0x02000000;     // mono/metadata/tokentype.h

void* s_MonoLibrary = nullptr;
std::string g_monoDllPath;

enum Mode
{
    CoreCLR,
    Mono,
};

Mode g_Mode;

#ifdef WIN32
#include <Windows.h>
const int RTLD_LAZY = 0; // not used
void* dlopen(const char* path, int flags)
{
    return ::LoadLibraryA(path);
}
void dlclose(void* handle)
{
    ::FreeLibrary((HMODULE)handle);
}
void* dlsym(void* handle, const char* funcname)
{
    auto sym = ::GetProcAddress((HMODULE)handle, funcname);
    if (!sym)
    {
        printf("Failing to dlsym '%s'\n", funcname);
        //exit(1);
    }
    return sym;
}

static const std::string kNewLine = "\r\n";

#else
static const std::string kNewLine = "\n";
#endif

void* get_handle()
{
    if(s_MonoLibrary == nullptr)
    {
        printf("Loading Mono from '%s'...\n", g_monoDllPath.c_str());
        s_MonoLibrary = dlopen(g_monoDllPath.c_str(), RTLD_LAZY);

        if(s_MonoLibrary == nullptr)
        {
            assert(false && "Failed to load mono\n");
            exit(1);
        }
    }
    return s_MonoLibrary;
}

typedef wchar_t mono_char; // used by CoreCLR

void* get_method(const char* functionName)
{
    void* func = dlsym(get_handle(), functionName);
    if(func == nullptr)
    {
        printf("Failed to load function '%s'\n", functionName);
        // Don't hard exit as some functions are not exported while still exposed by MonoFunctions.h
        // So we might get a null access exception if we are using a function
        // that was not found, but we can identify them when it is failing
        // exit(1);
        return nullptr;
    }
    return func;
}

#define DO_API(r,n,p) typedef r (*type_##n)p; type_##n n;

#include "../../src/coreclr/vm/mono/MonoCoreClr.h"

#undef DO_API

MonoDomain *g_domain;
MonoAssembly *g_assembly;

// shim to map UnitTest++ to Catch
#define TEST(x) TEST_CASE(#x)
#define CHECK_EQUAL(x, y) REQUIRE((x) == (y))


#define CHECK_EQUAL_STR(x, y) REQUIRE(strcmp((x), (y)) == 0)

TEST(Sanity)
{
   CHECK_EQUAL(1, 1);
}

#define kTestDLLNameSpace "TestDll"
#define kTestClassName "TestClass"
#define kInvalidName "DoesNotExist"

#define GET_AND_CHECK(what, code) \
    auto what = code; \
    CHECK(what != nullptr)

static void get_dirname(char* source)
{
    for (int i = strlen(source) - 1; i >= 0; i--)
    {
        if (source[i] == '/' || source[i] == '\\')
        {
            source[i] = '\0';
            return;
        }
    }
}

#if WIN32
char* realpath(const char *path, char *resolved_path)
{
    char* result = (char*)malloc(1024);
    if (GetFullPathNameA((LPCSTR)path, 1024, result, NULL) == 0)
    {
        fprintf(stderr, "Fontconfig warning: GetFullPathNameA failed.\n");
        return NULL;
    }
    return result;
}
#endif

static std::string abs_path_from_unity_root(const char* relative_to_this_file)
{
    char* base = getenv("UNITY_ROOT");
    if (base == nullptr)
    {
        printf("Please supply UNITY_ROOT environment variable, so we can find your mono installation.\n");
        exit(1);
    }
    char* concat = new char[strlen(base) + strlen(relative_to_this_file) + 2];
    strcpy(concat, base);
    strcat(concat, "/");
    strcat(concat, relative_to_this_file);
    char* resolved = realpath(concat, nullptr);
    delete[] concat;
    if (resolved == nullptr) {
        perror("Failed to get absolute path");
        return "";
    }
    std::string result(resolved);
    free(resolved);
    return result;
}

static std::string abs_path_from_file(const char* relative_to_this_file)
{
    char* base = strdup(__FILE__);
    get_dirname(base);
    char* concat = new char[strlen(base) + strlen(relative_to_this_file) + 2];
    strcpy(concat, base);
    strcat(concat, "/");
    strcat(concat, relative_to_this_file);
    char* resolved = realpath(concat, nullptr);
    free(base);
    delete[] concat;
    if (resolved == nullptr)
    {
        perror("Failed to get absolute path");
        return "";
    }
    std::string result(resolved);
    free(resolved);
    return result;
}

MonoClass* GetClassHelper(const char* namespaze, const char* classname)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(klass, mono_class_from_name(image, namespaze, classname));
    return klass;
}
MonoMethod* GetMethodHelper(const char* namespaze, const char* classname, const char* methodname, int args)
{
    GET_AND_CHECK(method, mono_class_get_method_from_name (GetClassHelper(namespaze, classname), methodname, args));
    return method;
}

MonoObject* CreateObjectHelper(const char* namespaze, const char* classname)
{
    GET_AND_CHECK(obj, mono_object_new(g_domain, GetClassHelper(namespaze, classname)));
    return obj;
}

void* scripting_array_element_ptr(MonoArray* array, int i, size_t element_size)
{
    GET_AND_CHECK(arrayClass, mono_object_get_class((MonoObject*)array));

    size_t SCRIPTING_ARRAY_HEADERSIZE = g_Mode == CoreCLR ? sizeof(void*) * 2 * mono_class_get_rank(arrayClass): sizeof(void*) * 4;
    return SCRIPTING_ARRAY_HEADERSIZE + i * element_size + (char*)array;
}

TEST(mono_assembly_get_image_returns_value)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    CHECK_EQUAL(g_assembly, mono_image_get_assembly(image));
}

TEST(mono_assembly_loaded_works)
{
    MonoAssemblyName assemblyName;
    mono_assembly_name_parse("coreclr-test", &assemblyName);
    CHECK_EQUAL(g_assembly, mono_assembly_loaded (&assemblyName));

    mono_assembly_name_parse("not loaded", &assemblyName);
    CHECK(mono_assembly_loaded (&assemblyName) == nullptr);

    mono_assembly_name_free(&assemblyName);
}

TEST(mono_class_from_name_returns_class)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(klass, mono_class_from_name(image, kTestDLLNameSpace, kTestClassName));
    CHECK(strcmp(kTestDLLNameSpace, mono_class_get_namespace(klass)) == 0);
    CHECK(strcmp(kTestClassName, mono_class_get_name(klass)) == 0);
    CHECK_EQUAL(image, mono_class_get_image(klass));
}

TEST(mono_class_from_returns_null_if_class_does_not_exist)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    MonoClass* klass = mono_class_from_name(image, kTestDLLNameSpace, kInvalidName);
    CHECK(klass == NULL);
}

TEST(mono_class_get_method_from_name_returns_method)
{
    const char* methodname = "StaticMethodReturningInt";
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, methodname, 0));
    CHECK(strcmp(methodname, mono_method_get_name(method)) == 0);
    CHECK(klass == mono_method_get_class(method));
}

TEST(mono_method_full_name_returns_full_name)
{
    const char* methodname = "StaticMethodWithObjectOutArg";
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, methodname, 2));
    CHECK(strcmp("TestDll.TestClass:StaticMethodWithObjectOutArg", mono_method_full_name(method, false)) == 0);
    if (g_Mode == CoreCLR)
        CHECK(strcmp("TestDll.TestClass:StaticMethodWithObjectOutArg (System.Object,System.Object&)", mono_method_full_name(method, true)) == 0);
    else
        CHECK(strcmp("TestDll.TestClass:StaticMethodWithObjectOutArg (object,object&)", mono_method_full_name(method, true)) == 0);
}

TEST(mono_class_get_method_from_name_returns_null_if_method_does_not_exist)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoMethod* method = mono_class_get_method_from_name (klass, kInvalidName, 0);
    CHECK(method == NULL);
}

TEST(mono_class_get_property_from_name_returns_static_property)
{
    const char* propertyname = "StaticIntProperty";
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(property, mono_class_get_property_from_name (klass, propertyname));
    GET_AND_CHECK(method, mono_property_get_get_method(property));
    CHECK(strcmp("get_StaticIntProperty", mono_method_get_name(method)) == 0);
    CHECK_EQUAL(klass, mono_method_get_class(method));
}

TEST(mono_class_get_property_from_name_returns_instance_property)
{
    const char* propertyname = "IntProperty";
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(property, mono_class_get_property_from_name (klass, propertyname));
    GET_AND_CHECK(method, mono_property_get_get_method(property));
    CHECK(strcmp("get_IntProperty", mono_method_get_name(method)) == 0);
    CHECK_EQUAL(klass, mono_method_get_class(method));
}

TEST(mono_class_get_property_from_name_returns_instance_property_of_base_class)
{
    const char* propertyname = "IntProperty";
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "DerivedClass");
    GET_AND_CHECK(property, mono_class_get_property_from_name (klass, propertyname));
    GET_AND_CHECK(method, mono_property_get_get_method(property));
    CHECK(strcmp("get_IntProperty", mono_method_get_name(method)) == 0);
    CHECK_EQUAL(base, mono_method_get_class(method));
}

TEST(mono_type_get_name_returns_name)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(type, mono_class_get_type(klass));
    GET_AND_CHECK(name , mono_type_get_name(type));
    CHECK(strcmp("TestDll.TestClass", name) == 0);
    mono_unity_g_free(name);
}

TEST(mono_type_get_name_full_returns_assembly_qualified_name)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(type, mono_class_get_type(klass));
    GET_AND_CHECK(name , mono_type_get_name_full(type, MonoTypeNameFormat::MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED));
    CHECK(strcmp("TestDll.TestClass, coreclr-test, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", name) == 0);
    mono_unity_g_free(name);
}

TEST(mono_runtime_object_init_calls_constructor)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "TestClassWithConstructor", "GetI", 0);
    MonoObject* obj = CreateObjectHelper(kTestDLLNameSpace, "TestClassWithConstructor");
    {
        MonoObject* returnValue = mono_runtime_invoke(method, obj, nullptr, nullptr);
        int int_result = *(int*)mono_object_unbox(returnValue);
        CHECK_EQUAL(0, int_result);
    }
    mono_runtime_object_init(obj);
    {
        MonoObject* returnValue = mono_runtime_invoke(method, obj, nullptr, nullptr);
        int int_result = *(int*)mono_object_unbox(returnValue);
        CHECK_EQUAL(42, int_result);
    }
}

TEST(mono_runtime_invoke_can_invoke_static_method_with_no_args)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodReturningInt", 0);
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(42, int_result);
}

TEST(mono_runtime_invoke_can_invoke_private_method)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticPrivateMethodReturningInt", 0);
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(42, int_result);
}

TEST(mono_runtime_invoke_can_invoke_static_method_with_two_args)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithTwoArgsReturningInt", 2);
    int param1 = 10;
    int param2 = 15;
    void* params[2] = { &param1, &param2 };
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(25, int_result);
}

TEST(mono_runtime_invoke_can_invoke_static_method_with_two_float_args)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithTwoArgsReturningFloat", 2);
    float param1 = 10.0f;
    float param2 = 15.0f;
    void* params[2] = { &param1, &param2 };
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);
    float result = *(float*)mono_object_unbox(returnValue);

    CHECK_EQUAL(25.0f, result);
}

TEST(mono_runtime_invoke_can_invoke_static_method_returning_object)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithTwoArgsReturningObject", 1);
    MonoObject* testObj = CreateObjectHelper(kTestDLLNameSpace, kTestClassName);
    void* params[1] = { testObj };
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);

    CHECK_EQUAL(testObj, returnValue);
}

TEST(mono_runtime_invoke_can_invoke_instance_method_with_two_float_args)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "MethodWithTwoArgsReturningFloat", 2);
    MonoObject* testObj = CreateObjectHelper(kTestDLLNameSpace, kTestClassName);
    float param1 = 10.0f;
    float param2 = 15.0f;
    void* params[2] = { &param1, &param2 };
    MonoObject* returnValue = mono_runtime_invoke(method, testObj, params, nullptr);
    float result = *(float*)mono_object_unbox(returnValue);

    CHECK_EQUAL(25.0f, result);
}

typedef struct {
    uint32_t Data1;
    uint16_t  Data2;
    uint16_t  Data3;
    uint8_t  Data4[8];
} MYGUID;

TEST(mono_runtime_invoke_can_invoke_method_returning_struct)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodReturningGUID", 0);
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    MYGUID result = *(MYGUID*)mono_object_unbox(returnValue);

    CHECK_EQUAL(0x81a130d2, result.Data1);
}

TEST(mono_runtime_invoke_can_invoke_with_struct_arg)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithGUIDArg", 1);
    MYGUID guid;
    memset(&guid, 0, sizeof(guid));
    guid.Data1 = 123;
    guid.Data2 = 456;
    guid.Data3 = 789;
    void* params[1] = { &guid };
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);
    MYGUID result = *(MYGUID*)mono_object_unbox(returnValue);

    CHECK_EQUAL(guid.Data1, result.Data1);
    CHECK_EQUAL(guid.Data2, result.Data2);
    CHECK_EQUAL(guid.Data3, result.Data3);
}

TEST(mono_runtime_invoke_can_invoke_with_ptr_arg)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithPtrArg", 1);
    void* param1 = (void*)0x123456789FFFFLL;
    void* params[1] = { param1 };
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);
    void* result = *(void**)mono_object_unbox(returnValue);

    CHECK_EQUAL(param1, result);
}

TEST(mono_runtime_invoke_can_invoke_with_out_arg)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithObjectOutArg", 2);
    MonoObject* testObj = CreateObjectHelper(kTestDLLNameSpace, kTestClassName);
    void* param1 = testObj;
    void* param2 = nullptr;
    void* params[2] = { param1, &param2 };
    mono_runtime_invoke(method, nullptr, params, nullptr);

    CHECK_EQUAL(testObj, param2);
}

TEST(mono_method_get_object_works)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithObjectOutArg", 2);
    GET_AND_CHECK(methodInfoClass, mono_class_from_name(mono_get_corlib(), "System.Reflection", "MethodInfo"));
    GET_AND_CHECK(methodObject, mono_method_get_object(g_domain, method, klass));
    GET_AND_CHECK(methodObjectClass, mono_object_get_class((MonoObject*)methodObject));
    CHECK(mono_class_is_subclass_of(methodObjectClass, methodInfoClass, false));
}

TEST(unity_mono_reflection_method_get_method_works)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithObjectOutArg", 2);
    GET_AND_CHECK(methodInfoClass, mono_class_from_name(mono_get_corlib(), "System.Reflection", "MethodInfo"));
    GET_AND_CHECK(methodObject, mono_method_get_object(g_domain, method, klass));
    GET_AND_CHECK(methodFromObject, unity_mono_reflection_method_get_method(methodObject));
    CHECK_EQUAL(method, methodFromObject);
}

TEST(mono_object_isinst_works_with_same_class)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoObject* testobj = mono_object_new(g_domain, klass);
    CHECK(mono_object_isinst(testobj, klass) != NULL);

    MonoClass* unrelatedclass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    CHECK(mono_object_isinst(testobj, unrelatedclass) == NULL);

    unrelatedclass = GetClassHelper(kTestDLLNameSpace, "InheritedClass");
    CHECK(mono_object_isinst(testobj, unrelatedclass) == NULL);
}

TEST(mono_class_get_parent_returns_base_class)
{
    GET_AND_CHECK(objectClass, mono_class_from_name(mono_get_corlib(), "System", "Object"));
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "InheritedClass");

    CHECK_EQUAL(base, mono_class_get_parent(inherited));
    CHECK_EQUAL(objectClass, mono_class_get_parent(base));
    CHECK(mono_class_get_parent(objectClass) == nullptr);
}

TEST(mono_unity_class_is_abstract_works)
{
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "InheritedClass");
    CHECK(mono_unity_class_is_abstract(base));
    CHECK(!mono_unity_class_is_abstract(inherited));
}

TEST(mono_class_is_generic_works)
{
    MonoClass *nongeneric = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoClass *generic = GetClassHelper(kTestDLLNameSpace, "GenericClass`1");
    CHECK(mono_class_is_generic(generic));
    CHECK(!mono_class_is_generic(nongeneric));
}

TEST(mono_class_is_blittable_works)
{
    CHECK(mono_class_is_blittable(mono_get_int32_class()));
    // Mono has a different definition of "blittable". This should be fine for now,
    // as we special-case these in the editor.
    CHECK_EQUAL(g_Mode == CoreCLR, mono_class_is_blittable(GetClassHelper(kTestDLLNameSpace, "TestStructWithFields")));
    CHECK(!mono_class_is_blittable(GetClassHelper(kTestDLLNameSpace, "TestClass")));
}

TEST(mono_class_is_subclass_of_works_with_base_class)
{
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "InheritedClass");
    CHECK(mono_class_is_subclass_of(inherited, base, false));
    CHECK(mono_class_is_subclass_of(base, base, false));
    CHECK(!mono_class_is_subclass_of(base, inherited, false));
}

TEST(type_forwarder_lookup_results_in_identical_class)
{
    MonoClass *directLookup = GetClassHelper(kTestDLLNameSpace, kTestClassName);
#if defined(_DEBUG)
    std::string testDllPath = abs_path_from_file("../../artifacts/bin/forwarder-test/Debug/net6.0/forwarder-test.dll");
#else
    std::string testDllPath = abs_path_from_file("../../artifacts/bin/forwarder-test/Release/net6.0/forwarder-test.dll");
#endif
    MonoAssembly *forwarderAssembly = mono_domain_assembly_open (g_domain, testDllPath.c_str());
    GET_AND_CHECK(forwarderImage, mono_assembly_get_image(forwarderAssembly));
    GET_AND_CHECK(directImage, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(forwarderLookup, mono_class_from_name(forwarderImage, kTestDLLNameSpace, kTestClassName));
    CHECK_EQUAL(directLookup, forwarderLookup);
    CHECK_EQUAL(directImage, mono_class_get_image(forwarderLookup));
    CHECK(forwarderImage != directImage);
}

#if 0 //JON
TEST(will_find_dependency_assembly_next_to_loaded_assembly)
{
    // Calls a method which calls a method from another assembly which we did not explictly load.
    // We need to make sure that we can load any assemblies next to the one we loaded.
    std::string testDllPath = abs_path_from_file("../dll-with-dependency/bin/Debug/netcoreapp3.0/dll-with-dependency.dll");
    GET_AND_CHECK(dll_with_dependency_assembly, mono_domain_assembly_open (g_domain, testDllPath.c_str()));
    GET_AND_CHECK(dll_with_dependency_image, mono_assembly_get_image(dll_with_dependency_assembly));
    GET_AND_CHECK(class_with_dependency, mono_class_from_name(dll_with_dependency_image, "dll_with_dependency", "ClassWithDependency"));
    GET_AND_CHECK(obj, mono_object_new(g_domain, class_with_dependency));
    mono_runtime_object_init(obj);
    GET_AND_CHECK(method, mono_class_get_method_from_name (class_with_dependency, "ToString", 0));
    GET_AND_CHECK(virtualmethod, mono_object_get_virtual_method (obj, method));
    MonoObject* returnValue = mono_runtime_invoke(virtualmethod, obj, nullptr, nullptr);
    char* str = mono_string_to_utf8((MonoString*)returnValue);
    CHECK_EQUAL("Hello", str);
    mono_unity_g_free(str);
}
#endif

TEST(can_get_types_from_image_table)
{
    MonoClass *testclass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    int rows = mono_image_get_table_rows(image, MONO_TABLE_TYPEDEF);
    CHECK(rows > 0);
    bool found = false;
    for (int i=0; i<rows; i++)
    {
        GET_AND_CHECK(klass, mono_unity_class_get(image, MONO_TOKEN_TYPE_DEF | (i + 1)));
        if (klass == testclass)
            found = true;
    }
    CHECK(found);
}

TEST(mono_class_is_subclass_of_works_with_arrays)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoClass *arrayklass = mono_array_class_get(klass, 1);
    CHECK(mono_class_is_subclass_of(arrayklass, arrayklass, false));
}

TEST(mono_class_is_subclass_of_works_with_interfaces)
{
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, "TestInterface");
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "ClassImplementingInterface");
    CHECK(mono_class_is_subclass_of(inherited, base, true));
    CHECK(!mono_class_is_subclass_of(inherited, base, false));
    CHECK(!mono_class_is_subclass_of(base, inherited, true));
}

TEST(mono_class_is_inflated_works)
{
    MonoClass *noninflated = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoClass *genericinstance = GetClassHelper(kTestDLLNameSpace, "GenericStringInstance");
    GET_AND_CHECK(genericinstanceparent, mono_class_get_parent(genericinstance));
    CHECK(mono_class_is_inflated(genericinstanceparent));
    CHECK(!mono_class_is_inflated(genericinstance));
    CHECK(!mono_class_is_inflated(noninflated));
}

TEST(mono_unity_class_get_generic_type_definition_works)
{
    MonoClass *noninflated = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoClass *genericinstance = GetClassHelper(kTestDLLNameSpace, "GenericStringInstance");
    GET_AND_CHECK(genericinstanceparent, mono_class_get_parent(genericinstance));
    GET_AND_CHECK(generic_type, mono_unity_class_get_generic_type_definition(genericinstanceparent));
    CHECK(strcmp("GenericClass`1", mono_class_get_name(genericinstanceparent)) == 0);

    GET_AND_CHECK(genericinstanceparent_type, mono_class_get_type(genericinstanceparent));
    CHECK(
        strcmp(
            g_Mode == CoreCLR ? "TestDll.GenericClass`1[System.String]" : "TestDll.GenericClass<System.String>"
            , mono_type_get_name(genericinstanceparent_type)
        ) == 0);

    CHECK(strcmp("GenericClass`1", mono_class_get_name(generic_type)) == 0);

    GET_AND_CHECK(generic_type_type, mono_class_get_type(generic_type));
    CHECK(
        strcmp(
            g_Mode == CoreCLR ? "TestDll.GenericClass`1[T]" : "TestDll.GenericClass<T>"
            , mono_type_get_name(generic_type_type)
        ) == 0);

    CHECK(mono_class_is_inflated(genericinstanceparent));
    CHECK(!mono_class_is_inflated(generic_type));
    CHECK(!mono_class_is_generic(genericinstanceparent));
    CHECK(mono_class_is_generic(generic_type));
}

TEST(mono_class_get_flags_works)
{
    CHECK_EQUAL(TYPE_ATTRIBUTE_PUBLIC | TYPE_ATTRIBUTE_BEFORE_FIELD_INIT,
        mono_class_get_flags(GetClassHelper(kTestDLLNameSpace, kTestClassName)));
    CHECK_EQUAL(TYPE_ATTRIBUTE_PUBLIC | TYPE_ATTRIBUTE_ABSTRACT | TYPE_ATTRIBUTE_BEFORE_FIELD_INIT,
        mono_class_get_flags(GetClassHelper(kTestDLLNameSpace, "BaseClass")));
    CHECK_EQUAL(TYPE_ATTRIBUTE_BEFORE_FIELD_INIT,
        mono_class_get_flags(GetClassHelper(kTestDLLNameSpace, "TestAttribute")));
}

TEST(mono_class_instance_size_works)
{
    size_t objectsize = g_Mode == CoreCLR ? 8 : 16;
    CHECK_EQUAL(objectsize + 1, mono_class_instance_size(mono_get_byte_class()));
    CHECK_EQUAL(objectsize + 2, mono_class_instance_size(mono_get_int16_class()));
    CHECK_EQUAL(objectsize + 4, mono_class_instance_size(mono_get_int32_class()));
    CHECK_EQUAL(objectsize, mono_class_instance_size(mono_get_object_class()));
    CHECK_EQUAL(objectsize + 12, mono_class_instance_size(GetClassHelper(kTestDLLNameSpace, "TestClassWithFields")));
}

TEST(can_get_base_classes)
{
    CHECK(strcmp("Boolean", mono_class_get_name(mono_get_boolean_class())) == 0);
    CHECK(strcmp("Char", mono_class_get_name(mono_get_char_class())) == 0);
    CHECK(strcmp("Byte", mono_class_get_name(mono_get_byte_class())) == 0);
    CHECK(strcmp("Int16", mono_class_get_name(mono_get_int16_class())) == 0);
    CHECK(strcmp("Int32", mono_class_get_name(mono_get_int32_class())) == 0);
    CHECK(strcmp("Int64", mono_class_get_name(mono_get_int64_class())) == 0);
    CHECK(strcmp("Single", mono_class_get_name(mono_get_single_class())) == 0);
    CHECK(strcmp("Double", mono_class_get_name(mono_get_double_class())) == 0);
    CHECK(strcmp("Object", mono_class_get_name(mono_get_object_class())) == 0);
    CHECK(strcmp("String", mono_class_get_name(mono_get_string_class())) == 0);
    CHECK(strcmp("Array", mono_class_get_name(mono_get_array_class())) == 0);
    CHECK(strcmp("Exception", mono_class_get_name(mono_get_exception_class())) == 0);
}

TEST(mono_string_new_wrapper_creates_valid_string)
{
    const char* cstr = "Hello, World!";
    MonoString *str = mono_string_new_wrapper(cstr);
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithStringArg", 1);
    void* param1 = str;
    void* params[1] = { param1 };
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);
    int result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(strlen(cstr), result);
}

TEST(mono_class_get_fields_retrieves_all_fields)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithFields");

    gpointer ptr = nullptr;
    int count = 0;
    std::string fieldnames;
    MonoClassField* field;
    while ((field = mono_class_get_fields(klass, &ptr)) != nullptr)
    {
        GET_AND_CHECK(fieldname, mono_field_get_name(field));
        CHECK(strcmp("System.Int32", mono_type_get_name(mono_field_get_type(field))) == 0);
        fieldnames += fieldname;
        count++;
    }
    CHECK_EQUAL(4, count);
    // CoreCLR reports static fields after non-static fields.
    CHECK(strcmp(g_Mode == CoreCLR ? "xywz" : "xyzw", fieldnames.c_str()) == 0);
}

TEST(can_get_type_of_generic_field)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "GenericClass`1");

    gpointer ptr = nullptr;
    GET_AND_CHECK(field, mono_class_get_fields(klass, &ptr));
    CHECK(strcmp("genericField", mono_field_get_name(field)) == 0);
    CHECK(strcmp(g_Mode == CoreCLR ? "!0" : "T", mono_type_get_name(mono_field_get_type(field))) == 0);
    field = mono_class_get_fields(klass, &ptr);
    CHECK(field != NULL);
    CHECK(strcmp("genericArrayField", mono_field_get_name(field)) == 0);
    CHECK(strcmp(g_Mode == CoreCLR ? "!0[]" : "T[]", mono_type_get_name(mono_field_get_type(field))) == 0);
    field = mono_class_get_fields(klass, &ptr);
    CHECK(field == NULL);
}

TEST(can_get_type_of_generic_instance_field)
{
    MonoClass* instanceklass = GetClassHelper(kTestDLLNameSpace, "GenericStringInstance");
    GET_AND_CHECK(klass, mono_class_get_parent(instanceklass));

    GET_AND_CHECK(klass_type, mono_class_get_type(klass));
    CHECK(MONO_TYPE_GENERICINST == mono_type_get_type(klass_type));

    gpointer ptr = nullptr;
    GET_AND_CHECK(field, mono_class_get_fields(klass, &ptr));
    CHECK(field != NULL);
    CHECK(strcmp("genericField", mono_field_get_name(field)) == 0);
    if (g_Mode == CoreCLR )
    {
        CHECK(strcmp("System.String", mono_type_get_name(mono_field_get_type_specific(field, klass))) == 0);
        CHECK(strcmp("System.__Canon", mono_type_get_name(mono_field_get_type(field))) == 0);
    }
    else
        CHECK(strcmp("System.String", mono_type_get_name(mono_field_get_type(field))) == 0);
    field = mono_class_get_fields(klass, &ptr);
    CHECK(field != NULL);
    CHECK(strcmp("genericArrayField", mono_field_get_name(field)) == 0);
    if (g_Mode == CoreCLR )
    {
        CHECK(strcmp("System.String[]", mono_type_get_name(mono_field_get_type_specific(field, klass))) == 0);
        CHECK(strcmp("System.__Canon[]", mono_type_get_name(mono_field_get_type(field))) == 0);
    }
    else
        CHECK(strcmp("System.String[]", mono_type_get_name(mono_field_get_type(field))) == 0);
    field = mono_class_get_fields(klass, &ptr);
    CHECK(field == NULL);
}

TEST(mono_class_get_interfaces_retrieves_all_interfaces)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassImplementingInterface");
    MonoClass* testInterface = GetClassHelper(kTestDLLNameSpace, "TestInterface");

    gpointer ptr = nullptr;
    MonoClass* monoInterface = mono_class_get_interfaces(klass, &ptr);
    CHECK_EQUAL(testInterface, monoInterface);
    monoInterface = mono_class_get_interfaces(klass, &ptr);
    CHECK(monoInterface == NULL);
}

TEST(mono_class_get_interfaces_may_retrieve_parent_interfaces)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassDerivingFromClassImplementingInterface");

    gpointer ptr = nullptr;
    MonoClass* monoInterface = mono_class_get_interfaces(klass, &ptr);

    // Behavior here is different between mono and coreclr.
    // Mono will not report parent interfaces. CoreCLR will. It is not easy to make CoreCLR
    // match mono, as the information is not available to CoreCLR at that point.
    if (g_Mode == CoreCLR )
        CHECK(monoInterface != NULL);
    else
        CHECK(monoInterface == NULL);
}

TEST(can_get_type_of_generic_parameter)
{
    if (g_Mode == CoreCLR )
    {
        MonoClass* instanceklass = GetClassHelper(kTestDLLNameSpace, "GenericStringInstance");
        GET_AND_CHECK(klass, mono_class_get_parent(instanceklass));
        GET_AND_CHECK(instance_type, mono_class_get_type(klass));
        CHECK(strcmp("TestDll.GenericClass`1[System.String]", mono_type_get_name(instance_type)) == 0);
        CHECK_EQUAL(1, mono_type_get_num_generic_args(instance_type));
        GET_AND_CHECK(genericarg, mono_type_get_generic_arg(instance_type, 0));
        CHECK(strcmp("System.String", mono_type_get_name(genericarg)) == 0);
    }
}

TEST(mono_field_get_offset_retrieves_field_offset)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithFields");
    GET_AND_CHECK(field0, mono_class_get_field_from_name(klass, "x"));
    GET_AND_CHECK(field1, mono_class_get_field_from_name(klass, "y"));
    size_t field0_offset = mono_field_get_offset(field0);
    size_t field1_offset = mono_field_get_offset(field1);
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    GET_AND_CHECK(method, mono_class_get_method_from_name(klass, "SetupFields", 0));
    mono_runtime_invoke(method, obj, nullptr, nullptr);
    CHECK_EQUAL(123, *(int*)((char*)obj + field0_offset));
    CHECK_EQUAL(456, *(int*)((char*)obj + field1_offset));
}

TEST(sequential_layout_is_respected)
{
    // CoreCLR does not respect sequential layout for non-blittable types
    if (g_Mode == CoreCLR)
        return;

    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassWithSequentialLayout");
    gpointer ptr = nullptr;
    int count = 0;
    MonoClassField* field;
    size_t lastOffset = 0;
    while ((field = mono_class_get_fields(klass, &ptr)) != nullptr)
    {
        size_t offset = mono_field_get_offset(field);
        CHECK(offset > lastOffset);
        lastOffset = offset;
    }
    CHECK(lastOffset > 0);
}

TEST(explicit_layout_is_respected)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassWithExplicitLayout");
    gpointer ptr = nullptr;
    MonoClassField* field;
    size_t offset = 0;
    size_t SCRIPTING_OBJECT_HEADERSIZE = g_Mode == CoreCLR ? sizeof(void*) : sizeof(void*) * 2;

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 0, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 8, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 16, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 20, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 24, offset);

    field = mono_class_get_fields(klass, &ptr);
    CHECK(field == NULL);
}

TEST(explicit_layout_is_correctly_calculated_for_derived_class)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "DerivedClassWithExplicitLayout");
    gpointer ptr = nullptr;
    MonoClassField* field;
    size_t offset = 0;
    size_t SCRIPTING_OBJECT_HEADERSIZE = g_Mode == CoreCLR ? sizeof(void*) : sizeof(void*) * 2;
    size_t parentSize = 32;

    // CoreCLR treats explicit layout for derived classes different than mono or il2cpp do.
    // It will add the base type size to the offset. This is a problem, because it causes a
    // different layout than with mono. So, in this case, where the FieldOffset attributes
    // already include the parent size, we need to add the parent size a second time for CoreCLR.
    // We need to figure out how we want to deal with this, but for now, we test the behavior we have.
    if (g_Mode == CoreCLR)
        parentSize *= 2;

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 0, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 8, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 16, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 20, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 24, offset);

    field = mono_class_get_fields(klass, &ptr);
    CHECK(field == NULL);
}

TEST(mono_gc_wbarrier_set_field_can_set_reference_field)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithReferenceField");
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    GET_AND_CHECK(method, mono_class_get_method_from_name(klass, "GetField", 0));
    MonoObject* returnValue = mono_runtime_invoke(method, obj, nullptr, nullptr);
    CHECK(returnValue == nullptr);

    GET_AND_CHECK(field, mono_class_get_field_from_name(klass, "reference"));
    int field_offset = mono_field_get_offset(field);
    mono_gc_wbarrier_set_field(obj, (char*)obj + field_offset, obj);

    returnValue = mono_runtime_invoke(method, obj, nullptr, nullptr);
    CHECK_EQUAL(obj, returnValue);
}

TEST(mono_field_set_value_can_set_reference_field)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithReferenceField");
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    GET_AND_CHECK(method, mono_class_get_method_from_name(klass, "GetField", 0));
    MonoObject* returnValue = mono_runtime_invoke(method, obj, nullptr, nullptr);
    CHECK(returnValue == nullptr);

    GET_AND_CHECK(field, mono_class_get_field_from_name(klass, "reference"));
    mono_field_set_value(obj, field, obj);

    returnValue = mono_runtime_invoke(method, obj, nullptr, nullptr);
    CHECK_EQUAL(obj, returnValue);
}

TEST(mono_field_get_value_can_get_reference_field)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithReferenceField");
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));

    GET_AND_CHECK(field, mono_class_get_field_from_name(klass, "reference"));
    int field_offset = mono_field_get_offset(field);
    mono_gc_wbarrier_set_field(obj, (char*)obj + field_offset, obj);

    MonoObject* returnValue;
    mono_field_get_value(obj, field, &returnValue);
    CHECK_EQUAL(obj, returnValue);
}

TEST(mono_field_set_value_can_set_value_field)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithFields");
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));

    GET_AND_CHECK(field, mono_class_get_field_from_name(klass, "y"));
    int value = 23;
    mono_field_set_value(obj, field, &value);

    size_t field_offset = mono_field_get_offset(field);
    CHECK_EQUAL(23, *(int*)((char*)obj + field_offset));
}

TEST(mono_field_get_value_can_get_value_field)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithFields");
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));

    GET_AND_CHECK(field, mono_class_get_field_from_name(klass, "y"));

    size_t field_offset = mono_field_get_offset(field);
    *(int*)((char*)obj + field_offset) = 23;

    MonoObject* returnValue = NULL;
    mono_field_get_value(obj, field, &returnValue);
    CHECK_EQUAL((void*)23, returnValue);
}

TEST(mono_field_get_offset_retrieves_field_offset_from_struct)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestStructWithFields");
    GET_AND_CHECK(field0, mono_class_get_field_from_name(klass, "x"));
    GET_AND_CHECK(field1, mono_class_get_field_from_name(klass, "y"));
    size_t field0_offset = mono_field_get_offset(field0);
    size_t field1_offset = mono_field_get_offset(field1);
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    GET_AND_CHECK(method, mono_class_get_method_from_name(klass, "SetupFields", 0));
    auto structInObject = (MonoObject*)((char*)obj + field0_offset);
    if (g_Mode == CoreCLR)
        mono_runtime_invoke_with_nested_object(method, structInObject, obj, nullptr, nullptr);
    else
        mono_runtime_invoke(method, structInObject, nullptr, nullptr);
    CHECK_EQUAL(123, *(int*)((char*)obj + field0_offset));
    CHECK_EQUAL(456, *(int*)((char*)obj + field1_offset));
}

TEST(mono_field_get_flags_works)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithFields");
    GET_AND_CHECK(field0, mono_class_get_field_from_name(klass, "x"));
    CHECK_EQUAL(FIELD_ATTRIBUTE_PUBLIC, mono_field_get_flags(field0));
    GET_AND_CHECK(field1, mono_class_get_field_from_name(klass, "y"));
    CHECK_EQUAL(FIELD_ATTRIBUTE_PRIVATE, mono_field_get_flags(field1));
    GET_AND_CHECK(field2, mono_class_get_field_from_name(klass, "z"));
    CHECK_EQUAL(FIELD_ATTRIBUTE_PRIVATE | FIELD_ATTRIBUTE_STATIC, mono_field_get_flags(field2));
    GET_AND_CHECK(field3, mono_class_get_field_from_name(klass, "w"));
    CHECK_EQUAL(FIELD_ATTRIBUTE_FAMILY | FIELD_ATTRIBUTE_NOT_SERIALIZED, mono_field_get_flags(field3));
}

TEST(mono_class_get_field_from_name_base_class_works)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "DerivedClassWithFields");
    // Derived class checks
    GET_AND_CHECK(field3, mono_class_get_field_from_name(klass, "a"));
    GET_AND_CHECK(field4, mono_class_get_field_from_name(klass, "b"));
    GET_AND_CHECK(field5, mono_class_get_field_from_name(klass, "c"));
    // Base class checks
    GET_AND_CHECK(field0, mono_class_get_field_from_name(klass, "x"));
    GET_AND_CHECK(field1, mono_class_get_field_from_name(klass, "y"));
    GET_AND_CHECK(field2, mono_class_get_field_from_name(klass, "z"));
}

TEST(mono_class_get_methods_retrieves_all_methods)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithMethods");

    gpointer ptr = nullptr;
    int count = 0;
    std::string methodnames;
    MonoMethod* method;
    while((method = mono_class_get_methods(klass, &ptr)) != nullptr)
    {
        GET_AND_CHECK(methodname, mono_method_get_name(method));
        methodnames += methodname;
        count++;
    }

    CHECK_EQUAL(4, count);
    CHECK_EQUAL("ABC.ctor", methodnames);
}

TEST(mono_method_signature_gets_parameters_from_static_method)
{
    GET_AND_CHECK(method, GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithObjectOutArg", 2));
    GET_AND_CHECK(signature, mono_method_signature(method));
	CHECK_EQUAL(2, mono_signature_get_param_count(signature));
    GET_AND_CHECK(returnType, mono_signature_get_return_type(signature));
    CHECK_EQUAL(MONO_TYPE_VOID, mono_type_get_type(returnType));
    CHECK(!mono_signature_is_instance(signature));
    gpointer iter = NULL;
    MonoType *paramType = mono_signature_get_params(signature, &iter);
    CHECK_EQUAL(MONO_TYPE_OBJECT, mono_type_get_type(paramType));
    CHECK(mono_type_is_byref(paramType) == false);
    paramType = mono_signature_get_params(signature, &iter);
    CHECK_EQUAL(MONO_TYPE_OBJECT, mono_type_get_type(paramType));
    CHECK(mono_type_is_byref(paramType) == true);
    paramType = mono_signature_get_params(signature, &iter);
    CHECK(paramType == nullptr);
}

TEST(mono_method_signature_gets_parameters_from_instance_method)
{
    GET_AND_CHECK(method, GetMethodHelper(kTestDLLNameSpace, kTestClassName, "MethodWithTwoArgsReturningFloat", 2));
    GET_AND_CHECK(signature, mono_method_signature(method));
	CHECK_EQUAL(2, mono_signature_get_param_count(signature));
    GET_AND_CHECK(returnType, mono_signature_get_return_type(signature));
    CHECK_EQUAL(MONO_TYPE_R4, mono_type_get_type(returnType));
    CHECK(mono_signature_is_instance(signature));
    gpointer iter = NULL;
    MonoType *paramType = mono_signature_get_params(signature, &iter);
    CHECK_EQUAL(MONO_TYPE_R4, mono_type_get_type(paramType));
    paramType = mono_signature_get_params(signature, &iter);
    CHECK_EQUAL(MONO_TYPE_R4, mono_type_get_type(paramType));
    paramType = mono_signature_get_params(signature, &iter);
    CHECK(paramType == nullptr);
}

TEST(mono_metadata_signature_equal_can_compare_signatures)
{
    GET_AND_CHECK(method1, GetMethodHelper(kTestDLLNameSpace, kTestClassName, "MethodWithTwoArgsReturningFloat", 2));
    GET_AND_CHECK(signature1, mono_method_signature(method1));
    GET_AND_CHECK(method2, GetMethodHelper(kTestDLLNameSpace, kTestClassName, "AnotherMethodWithTwoArgsReturningFloat", 2));
    GET_AND_CHECK(signature2, mono_method_signature(method2));
    GET_AND_CHECK(method3, GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithTwoArgsReturningFloat", 2));
    GET_AND_CHECK(signature3, mono_method_signature(method3));
    GET_AND_CHECK(method4, GetMethodHelper(kTestDLLNameSpace, kTestClassName, "MethodWithTwoArgsReturningInt", 2));
    GET_AND_CHECK(signature4, mono_method_signature(method4));

    CHECK(mono_metadata_signature_equal(signature1, signature1));
    CHECK(mono_metadata_signature_equal(signature1, signature2));
    CHECK(!mono_metadata_signature_equal(signature1, signature3));
    CHECK(!mono_metadata_signature_equal(signature1, signature4));
}

TEST(mono_object_get_virtual_method_can_call_virtual_method)
{
    MonoMethod *method = GetMethodHelper(kTestDLLNameSpace, "BaseClass", "Method", 0);
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "InheritedClass");
    GET_AND_CHECK(obj, mono_object_new(g_domain, inherited));
    GET_AND_CHECK(virtualmethod, mono_object_get_virtual_method (obj, method));
    MonoObject* returnValue = mono_runtime_invoke(virtualmethod, obj, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);
    CHECK_EQUAL(42, int_result);
}

TEST(mono_object_get_virtual_method_can_call_interface_method)
{
    MonoMethod *method = GetMethodHelper(kTestDLLNameSpace, "TestInterface", "Method", 0);
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "ClassImplementingInterface");
    GET_AND_CHECK(obj, mono_object_new(g_domain, inherited));
    GET_AND_CHECK(virtualmethod, mono_object_get_virtual_method (obj, method));
    MonoObject* returnValue = mono_runtime_invoke(virtualmethod, obj, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);
    CHECK_EQUAL(42, int_result);
}

TEST(can_call_method_on_member_struct)
{
    MonoMethod *method1 = GetMethodHelper(kTestDLLNameSpace, "TestStructWithFields", "SetupFields", 0);
    MonoMethod *method2 = GetMethodHelper(kTestDLLNameSpace, "TestStructWithFields", "SumFields", 0);
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "ClassWithStructFields");
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    GET_AND_CHECK(field0, mono_class_get_field_from_name(klass, "a"));
    int field0_offset = mono_field_get_offset(field0);
    MonoObject* embeddedObjectA = (MonoObject*)((char*)obj + field0_offset);
    MonoObject* returnValue;
    if (g_Mode == Mono)
    {
        mono_runtime_invoke(method1, embeddedObjectA, nullptr, nullptr);
        returnValue = mono_runtime_invoke(method2, embeddedObjectA, nullptr, nullptr);
    }
    else
    {
        mono_runtime_invoke_with_nested_object(method1, embeddedObjectA, obj, nullptr, nullptr);
        returnValue = mono_runtime_invoke_with_nested_object(method2, embeddedObjectA, obj, nullptr, nullptr);
    }
    int int_result = *(int*)mono_object_unbox(returnValue);
    CHECK_EQUAL(579, int_result);
}

#if 0 // JON

TEST(can_call_interface_method_on_member_struct)
{
    MonoMethod *method = GetMethodHelper(kTestDLLNameSpace, "TestInterface", "Method", 0);
    MonoMethod *setupmethod = GetMethodHelper(kTestDLLNameSpace, "ClassWithStructFields", "Setup", 0);
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "StructImplementingInterface");
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "ClassWithStructFields");
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    GET_AND_CHECK(field0, mono_class_get_field_from_name(klass, "c"));
    int field0_offset = mono_field_get_offset(field0);
    GET_AND_CHECK(fieldType, mono_field_get_type(field0));
    GET_AND_CHECK(fieldKlass, mono_type_get_class(fieldType));
    GET_AND_CHECK(fieldDummyObject, mono_object_new(g_domain, fieldKlass));
    GET_AND_CHECK(virtualmethod, mono_object_get_virtual_method (fieldDummyObject, method));
    mono_runtime_invoke(setupmethod, obj, nullptr, nullptr);
    MonoObject* embeddedObjectA = (MonoObject*)((char*)obj + field0_offset);
    MonoObject* returnValue;
    if (g_Mode == Mono)
        returnValue = mono_runtime_invoke(virtualmethod, embeddedObjectA, nullptr, nullptr);
    else
        returnValue = mono_runtime_invoke_with_nested_object(virtualmethod, embeddedObjectA, obj, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);
    CHECK_EQUAL(42, int_result);
}

TEST(mono_object_get_virtual_method_can_call_interface_method_on_struct)
{
    MonoMethod *method = GetMethodHelper(kTestDLLNameSpace, "TestInterface", "Method", 0);
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "StructImplementingInterface");
    MonoMethod *setupmethod = GetMethodHelper(kTestDLLNameSpace, "StructImplementingInterface", "Setup", 0);
    MonoMethod *setupmethod2 = GetMethodHelper(kTestDLLNameSpace, "StructImplementingInterface", "Method", 0);
    GET_AND_CHECK(obj, mono_object_new(g_domain, inherited));
    GET_AND_CHECK(virtualmethod, mono_object_get_virtual_method (obj, method));
    mono_runtime_invoke(setupmethod, obj, nullptr, nullptr);
    mono_runtime_invoke(setupmethod2, obj, nullptr, nullptr);
    MonoObject* returnValue = mono_runtime_invoke(virtualmethod, obj, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);
    CHECK_EQUAL(42, int_result);
}
#endif

TEST(mono_type_is_byref_works)
{
    MonoMethod *method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodWithObjectOutArg", 2);
    GET_AND_CHECK(signature, mono_method_signature(method));
    gpointer iter = NULL;
    MonoType *paramType = mono_signature_get_params(signature, &iter);
    CHECK(!mono_type_is_byref(paramType));
    paramType = mono_signature_get_params(signature, &iter);
    CHECK(mono_type_is_byref(paramType));
}

TEST(mono_get_corlib_returns_corlib_image)
{
    MonoImage *corlib = mono_get_corlib();
    CHECK(mono_image_get_assembly(corlib) != NULL);
    if (g_Mode == CoreCLR)
        CHECK_EQUAL_STR("System.Private.CoreLib", mono_image_get_name(corlib));
    else
        CHECK_EQUAL_STR("mscorlib", mono_image_get_name(corlib));
}

TEST(mono_get_enum_class_returns_enum_class)
{
    GET_AND_CHECK(enumClass, mono_get_enum_class());
    CHECK_EQUAL_STR("Enum", mono_class_get_name(enumClass));
}

TEST(mono_get_corlib_can_get_corlib_type)
{
    GET_AND_CHECK(int32Class, mono_class_from_name(mono_get_corlib(), "System", "Int32"));
    CHECK_EQUAL_STR("Int32", mono_class_get_name(int32Class));
}

TEST(mono_array_class_get_creates_array_class)
{
    GET_AND_CHECK(int32Class, mono_class_from_name(mono_get_corlib(), "System", "Int32"));
    GET_AND_CHECK(int64Class, mono_class_from_name(mono_get_corlib(), "System", "Int64"));

    GET_AND_CHECK(arrayInt32Class, mono_array_class_get(int32Class, 1));
    GET_AND_CHECK(arrayInt64Class, mono_array_class_get(int64Class, 2));

    CHECK_EQUAL(4, mono_array_element_size(arrayInt32Class));
    CHECK_EQUAL(8, mono_array_element_size(arrayInt64Class));

    CHECK_EQUAL(1, mono_class_get_rank(arrayInt32Class));
    CHECK_EQUAL(2, mono_class_get_rank(arrayInt64Class));

    CHECK_EQUAL(int32Class, mono_class_get_element_class(arrayInt32Class));
    CHECK_EQUAL(int64Class, mono_class_get_element_class(arrayInt64Class));

    CHECK_EQUAL_STR("Int32[]", mono_class_get_name(arrayInt32Class));
    CHECK_EQUAL_STR("Int64[,]", mono_class_get_name(arrayInt64Class));
}

TEST(mono_array_new_creates_array_instance)
{
    GET_AND_CHECK(int32Class, mono_class_from_name(mono_get_corlib(), "System", "Int32"));
    GET_AND_CHECK(arrayInt32Class, mono_array_class_get(int32Class, 1));
    GET_AND_CHECK(arrayInt32Instance, mono_array_new(g_domain, int32Class, 5));

// Todo
//    if (coreclr_array_length)
  //      CHECK_EQUAL(5, coreclr_array_length(arrayInt32Instance));
}

TEST(mono_gchandle_new_creates_gc_handle)
{
    MonoObject* testObj = CreateObjectHelper(kTestDLLNameSpace, kTestClassName);

    // Normal
    uintptr_t handle1 = mono_gchandle_new_v2(testObj, false);
    CHECK(handle1 != 0);
    CHECK_EQUAL(testObj, mono_gchandle_get_target_v2(handle1));
    mono_gchandle_free_v2(handle1);
    CHECK(mono_gchandle_get_target_v2(handle1) == nullptr);

    // Pinned
    uintptr_t handle2 = mono_gchandle_new_v2(testObj, true);
    CHECK(handle2 != 0);
    CHECK_EQUAL(testObj, mono_gchandle_get_target_v2(handle2));
    mono_gchandle_free_v2(handle2);
    CHECK(mono_gchandle_get_target_v2(handle2) == nullptr);

    CHECK(handle1 != handle2);
}

TEST(mono_gchandle_new_weakref_creates_weakref)
{
    MonoObject* testObj = CreateObjectHelper(kTestDLLNameSpace, kTestClassName);

    // Normal
    uintptr_t handle1 = mono_gchandle_new_weakref_v2(testObj, false);
    CHECK(handle1 != 0);
    CHECK_EQUAL(testObj, mono_gchandle_get_target_v2(handle1));
    mono_gchandle_free_v2(handle1);
    CHECK(mono_gchandle_get_target_v2(handle1) == nullptr);

    // Track resurrection
    uintptr_t handle2 = mono_gchandle_new_weakref_v2(testObj, true);
    CHECK(handle2 != 0);
    CHECK_EQUAL(testObj, mono_gchandle_get_target_v2(handle2));
    mono_gchandle_free_v2(handle2);
    CHECK(mono_gchandle_get_target_v2(handle2) == nullptr);

    CHECK(handle1 != handle2);
}

#if WIN32
#define NOINLINE __declspec(noinline)
#else
#define NOINLINE __attribute__((noinline))
#endif

// This needs to be a separate function, so the object itself is not alive on the stack
// and can be collected when the function exits.
NOINLINE
uintptr_t SetupTestObjectWeakHandle(const char* _namespace, const char* _class)
{
    MonoObject* testObj = CreateObjectHelper(_namespace, _class);
    uintptr_t handle = mono_gchandle_new_weakref_v2(testObj, false);
    return handle;
}

NOINLINE
uintptr_t SetupTestObjectHandle(const char* _namespace, const char* _class)
{
    MonoObject* testObj = CreateObjectHelper(_namespace, _class);
    uintptr_t handle = mono_gchandle_new_v2(testObj, false);
    CHECK_EQUAL(testObj, mono_gchandle_get_target_v2(handle));
    return handle;
}

NOINLINE
void VerifyCollectTestObjectHandle(guint32 handle, bool shouldBeAlive)
{
    // Clear 10kb of stack memory to avoid any stale stack slots
    // left over from creating the object keeping it alive.
    memset(alloca(1024*10), 0, 1024*10);

    // Since the above is not always reliable:
    // If in CoreCLR, turn off conservative GC for this collection to avoid any
    // stale stack slots left over from creating the object keeping it alive.
    // In mono, we don't have this function, so check for it's existance.
    if (mono_set_gc_conservative)
        mono_set_gc_conservative(false);
    mono_gc_collect(mono_gc_max_generation());
    if (mono_set_gc_conservative)
        mono_set_gc_conservative(true);
    if (shouldBeAlive)
        CHECK(mono_gchandle_get_target_v2(handle) != NULL);
    else
        CHECK(mono_gchandle_get_target_v2(handle) == NULL);
}

#if 0
TEST(weakref_can_be_collected)
{
    uintptr_t handle = SetupTestObjectWeakHandle(kTestDLLNameSpace, kTestClassName);
    VerifyCollectTestObjectHandle(handle, false);
}

TEST(handle_cannot_be_collected)
{
    uintptr_t handle = SetupTestObjectHandle(kTestDLLNameSpace, kTestClassName);
    VerifyCollectTestObjectHandle(handle, true);
}

bool gFinalizedCalled = false;
void FinalizerCalled()
{
    gFinalizedCalled = true;
}

TEST(mono_domain_finalize_calls_finalizers)
{
    mono_add_internal_call("TestDll.TestClassWithFinalizer::FinalizerCalled", reinterpret_cast<gconstpointer>(FinalizerCalled));
    uintptr_t handle = SetupTestObjectWeakHandle(kTestDLLNameSpace, "TestClassWithFinalizer");
    VerifyCollectTestObjectHandle(handle, false);
    mono_domain_finalize(g_domain, -1);
    CHECK(gFinalizedCalled);
    gFinalizedCalled = false;
}

TEST(mono_unity_gc_disable_works)
{
    uintptr_t handle = SetupTestObjectWeakHandle(kTestDLLNameSpace, kTestClassName);
    mono_unity_gc_disable();
    VerifyCollectTestObjectHandle(handle, true);
    mono_unity_gc_enable();
}

TEST(mono_unity_gc_disable_can_be_nested)
{
    CHECK(!mono_unity_gc_is_disabled());
    mono_unity_gc_disable();
    CHECK(mono_unity_gc_is_disabled());
    mono_unity_gc_disable();
    CHECK(mono_unity_gc_is_disabled());
    mono_unity_gc_enable();
    CHECK(mono_unity_gc_is_disabled());
    mono_unity_gc_enable();
    CHECK(!mono_unity_gc_is_disabled());
}

#endif

TEST(mono_class_enum_basetype_works)
{
    MonoClass *testEnum = GetClassHelper(kTestDLLNameSpace, "TestEnum");
    GET_AND_CHECK(testEnumType, mono_class_enum_basetype(testEnum));
    GET_AND_CHECK(testEnumBaseClass, mono_type_get_class(testEnumType));
    CHECK_EQUAL(mono_get_int32_class(), testEnumBaseClass);

    MonoClass *testEnumCustomSize = GetClassHelper(kTestDLLNameSpace, "TestEnumCustomSize");
    GET_AND_CHECK(testEnumCustomSizeType, mono_class_enum_basetype(testEnumCustomSize));
    GET_AND_CHECK(testEnumCustomSizeBaseClass, mono_type_get_class(testEnumCustomSizeType));
    CHECK_EQUAL(mono_get_byte_class(), testEnumCustomSizeBaseClass);
}

int GetCoreLibClassTypeHelper(const char* namespaze, const char* name)
{
    GET_AND_CHECK(klass, mono_class_from_name(mono_get_corlib(), namespaze, name));
    GET_AND_CHECK(type, mono_class_get_type(klass));
    return mono_type_get_type(type);
}

TEST(mono_type_get_type_returns_expected_values)
{
    CHECK_EQUAL(MONO_TYPE_OBJECT, GetCoreLibClassTypeHelper ("System", "Object"));
    CHECK_EQUAL(MONO_TYPE_STRING, GetCoreLibClassTypeHelper ("System", "String"));
    CHECK_EQUAL(MONO_TYPE_I4, GetCoreLibClassTypeHelper ("System", "Int32"));
}

static int get_field_type(MonoClass* klass,const char* fieldName)
{
    return mono_type_get_type(mono_field_get_type( mono_class_get_field_from_name (klass, fieldName)));
}

TEST(mono_type_get_type_returns_expected_values2)
{
    MonoClass* classWithFields = GetClassHelper(kTestDLLNameSpace, "ClassWithFields");
    CHECK_EQUAL(MONO_TYPE_I1, get_field_type (classWithFields, "_sbyte"));
    CHECK_EQUAL(MONO_TYPE_U1, get_field_type (classWithFields, "_byte"));
    CHECK_EQUAL(MONO_TYPE_I2, get_field_type (classWithFields, "_short"));
    CHECK_EQUAL(MONO_TYPE_U2, get_field_type (classWithFields, "_ushort"));
    CHECK_EQUAL(MONO_TYPE_I4, get_field_type (classWithFields, "_int"));
    CHECK_EQUAL(MONO_TYPE_U4, get_field_type (classWithFields, "_uint"));
    CHECK_EQUAL(MONO_TYPE_I8, get_field_type (classWithFields, "_long"));
    CHECK_EQUAL(MONO_TYPE_U8, get_field_type (classWithFields, "_ulong"));


    CHECK_EQUAL(MONO_TYPE_R4, get_field_type (classWithFields, "_float"));
    CHECK_EQUAL(MONO_TYPE_R8, get_field_type (classWithFields, "_double"));

    CHECK_EQUAL(MONO_TYPE_BOOLEAN, get_field_type (classWithFields, "_bool"));
    CHECK_EQUAL(MONO_TYPE_CHAR, get_field_type (classWithFields, "_char"));

    CHECK_EQUAL(MONO_TYPE_STRING, get_field_type (classWithFields, "_string"));
    CHECK_EQUAL(MONO_TYPE_OBJECT, get_field_type (classWithFields, "_object"));
    CHECK_EQUAL(MONO_TYPE_CLASS, get_field_type (classWithFields, "_class"));

}

TEST(mono_class_from_mono_type_returns_class)
{
    GET_AND_CHECK(objectClass, mono_class_from_name(mono_get_corlib(), "System", "Object"));
    GET_AND_CHECK(objectType, mono_class_get_type(objectClass));
    CHECK_EQUAL(objectClass, mono_class_from_mono_type(objectType));
}

TEST(mono_type_get_object_returns_type_object)
{
    GET_AND_CHECK(objectClass, mono_class_from_name(mono_get_corlib(), "System", "Object"));
    GET_AND_CHECK(objectType, mono_class_get_type(objectClass));
    GET_AND_CHECK(objectTypeObject, mono_type_get_object(g_domain, objectType));
}

TEST(mono_class_get_nesting_type_returns_nesting_class)
{
    MonoClass *containingClass = GetClassHelper(kTestDLLNameSpace, "ClassWithNestedClass");
    MonoClass *nestedClass = GetClassHelper(kTestDLLNameSpace, "ClassWithNestedClass/NestedClass");
    CHECK_EQUAL(containingClass, mono_class_get_nesting_type(nestedClass));
    CHECK(mono_class_get_nesting_type(containingClass) == nullptr);
}

TEST(mono_class_get_nesting_type_returns_generic_nesting_class)
{
    MonoClass *containingClass = GetClassHelper(kTestDLLNameSpace, "GenericClassWithNestedClass`1");
    MonoClass *nestedClass = GetClassHelper(kTestDLLNameSpace, "GenericClassWithNestedClass`1/NestedClass");
    CHECK_EQUAL(containingClass, mono_class_get_nesting_type(nestedClass));
    CHECK(mono_class_get_nesting_type(containingClass) == nullptr);
}

TEST(mono_object_get_class_returns_class)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    CHECK_EQUAL(klass, mono_object_get_class(obj));
}

TEST(mono_class_set_userdata_can_be_retrieved)
{
    int userData = 100;
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    mono_class_set_userdata(klass, &userData);

    CHECK_EQUAL(&userData, (int*)mono_class_get_userdata(klass));
    CHECK_EQUAL(&userData, *(int**)(((char*)klass) + mono_class_get_userdata_offset()));
}

TEST(mono_value_box_works)
{
    bool b = true;
    GET_AND_CHECK(bool_class, mono_get_boolean_class());
    GET_AND_CHECK(bool_obj, mono_value_box(g_domain, bool_class, &b));
    CHECK_EQUAL(bool_class, mono_object_get_class(bool_obj));
    CHECK_EQUAL(b, *(bool*)mono_object_unbox(bool_obj));

    int i = 23;
    GET_AND_CHECK(int_class, mono_get_int32_class());
    GET_AND_CHECK(int_obj, mono_value_box(g_domain, int_class, &i));
    CHECK_EQUAL(int_class, mono_object_get_class(int_obj));
    CHECK_EQUAL(i, *(int*)mono_object_unbox(int_obj));
}

TEST(mono_custom_attrs_has_attr_can_check_class_attribute)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithAttribute");
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassTestWithParamsAttribute = GetClassHelper(kTestDLLNameSpace, "TestWithParamsAttribute");
    MonoClass *klassAnotherTestAttribute = GetClassHelper(kTestDLLNameSpace, "AnotherTestAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_class(klassClassWithAttribute));

    CHECK(mono_custom_attrs_has_attr(customAttrInfo, klassTestAttribute));
    CHECK(mono_custom_attrs_has_attr(customAttrInfo, klassTestWithParamsAttribute));
    CHECK(!mono_custom_attrs_has_attr(customAttrInfo, klassAnotherTestAttribute));

    mono_custom_attrs_free(customAttrInfo);
}

TEST(mono_custom_attrs_get_attrs_can_enumerate_attributes)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithAttribute");
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassInheritedTestAttribute = GetClassHelper(kTestDLLNameSpace, "InheritedTestAttribute");
    MonoClass *klassTestWithParamsAttribute = GetClassHelper(kTestDLLNameSpace, "TestWithParamsAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_class(klassClassWithAttribute));

    void* iterator = NULL;
    MonoClass* attributeClass;
    attributeClass = mono_custom_attrs_get_attrs(customAttrInfo, &iterator);
    CHECK_EQUAL(klassTestAttribute, attributeClass);
    attributeClass = mono_custom_attrs_get_attrs(customAttrInfo, &iterator);
    CHECK_EQUAL(klassInheritedTestAttribute, attributeClass);
    attributeClass = mono_custom_attrs_get_attrs(customAttrInfo, &iterator);
    CHECK_EQUAL(klassTestWithParamsAttribute, attributeClass);
    attributeClass = mono_custom_attrs_get_attrs(customAttrInfo, &iterator);
    CHECK(attributeClass == NULL);

    mono_custom_attrs_free(customAttrInfo);
}

TEST(mono_custom_attrs_get_attr_can_get_attribute_instance)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithAttribute");
    MonoClass *klassTestWithParamsAttribute = GetClassHelper(kTestDLLNameSpace, "TestWithParamsAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_class(klassClassWithAttribute));
    GET_AND_CHECK(attributeInstance, mono_custom_attrs_get_attr(customAttrInfo, klassTestWithParamsAttribute));
    CHECK_EQUAL(klassTestWithParamsAttribute, mono_object_get_class(attributeInstance));
    mono_custom_attrs_free(customAttrInfo);
}

TEST(mono_custom_attrs_get_attr_can_get_attribute_instance_for_inherited_attribute_from_base)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithInheritedAttribute");
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassInheritedTestAttribute = GetClassHelper(kTestDLLNameSpace, "InheritedTestAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_class(klassClassWithAttribute));
    GET_AND_CHECK(attributeInstance, mono_custom_attrs_get_attr(customAttrInfo, klassTestAttribute));
    CHECK_EQUAL(klassInheritedTestAttribute, mono_object_get_class(attributeInstance));
    mono_custom_attrs_free(customAttrInfo);
}

TEST(mono_custom_attrs_construct_can_get_attribute_instances)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithAttribute");
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassInheritedTestAttribute = GetClassHelper(kTestDLLNameSpace, "InheritedTestAttribute");
    MonoClass *klassTestWithParamsAttribute = GetClassHelper(kTestDLLNameSpace, "TestWithParamsAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_class(klassClassWithAttribute));
    GET_AND_CHECK(attributeArray, mono_custom_attrs_construct(customAttrInfo));

    GET_AND_CHECK(attribute1Instance, *(MonoObject**)scripting_array_element_ptr(attributeArray, 0, sizeof(MonoObject*)));
    CHECK_EQUAL(klassTestAttribute, mono_object_get_class(attribute1Instance));

    GET_AND_CHECK(attribute2Instance, *(MonoObject**)scripting_array_element_ptr(attributeArray, 1, sizeof(MonoObject*)));
    CHECK_EQUAL(klassInheritedTestAttribute, mono_object_get_class(attribute2Instance));

    GET_AND_CHECK(attribute3Instance, *(MonoObject**)scripting_array_element_ptr(attributeArray, 2, sizeof(MonoObject*)));
    CHECK_EQUAL(klassTestWithParamsAttribute, mono_object_get_class(attribute3Instance));

    mono_custom_attrs_free(customAttrInfo);
}

void GetFieldHelper(MonoClass *klass, MonoObject *obj, const char* fieldName, void* value)
{
    GET_AND_CHECK(field, mono_class_get_field_from_name(klass, fieldName));
    size_t field_offset = mono_field_get_offset(field);
    mono_field_get_value(obj, field, value);
}

TEST(mono_custom_attrs_get_attr_attribute_instance_has_correct_parameters)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithAttribute");
    MonoClass *klassTestWithParamsAttribute = GetClassHelper(kTestDLLNameSpace, "TestWithParamsAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_class(klassClassWithAttribute));
    GET_AND_CHECK(attributeInstance, mono_custom_attrs_get_attr(customAttrInfo, klassTestWithParamsAttribute));

    int i;
    GetFieldHelper(klassTestWithParamsAttribute, attributeInstance, "i", &i);
    CHECK_EQUAL(42, i);

    MonoString* s;
    GetFieldHelper(klassTestWithParamsAttribute, attributeInstance, "s", &s);
    char *utf8 = mono_string_to_utf8(s);
    CHECK_EQUAL_STR("foo", utf8);
    mono_unity_g_free(utf8);

    bool b;
    GetFieldHelper(klassTestWithParamsAttribute, attributeInstance, "b", &b);
    CHECK_EQUAL(true, b);

    float f;
    GetFieldHelper(klassTestWithParamsAttribute, attributeInstance, "f", &f);
    CHECK_EQUAL(1.0f, f);

    mono_custom_attrs_free(customAttrInfo);
}

TEST(mono_custom_attrs_has_attr_can_check_method_attribute)
{
    MonoMethod *methodWithAttribute = GetMethodHelper(kTestDLLNameSpace, "ClassWithAttribute", "MethodWithAttribute", 0);
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassAnotherTestAttribute = GetClassHelper(kTestDLLNameSpace, "AnotherTestAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_method(methodWithAttribute));

    CHECK(mono_custom_attrs_has_attr(customAttrInfo, klassTestAttribute));
    CHECK(!mono_custom_attrs_has_attr(customAttrInfo, klassAnotherTestAttribute));

    mono_custom_attrs_free(customAttrInfo);
}

TEST(mono_custom_attrs_has_attr_can_check_field_attribute)
{
    // TODO
}

TEST(mono_custom_attrs_has_attr_can_check_property_attribute)
{
    // TODO
}

TEST(mono_custom_attrs_has_attr_can_check_assembly_attribute)
{
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassAnotherTestAttribute = GetClassHelper(kTestDLLNameSpace, "AnotherTestAttribute");

    GET_AND_CHECK(customAttrInfo, mono_custom_attrs_from_assembly(g_assembly));

    CHECK(mono_custom_attrs_has_attr(customAttrInfo, klassTestAttribute));
    CHECK(!mono_custom_attrs_has_attr(customAttrInfo, klassAnotherTestAttribute));

    mono_custom_attrs_free(customAttrInfo);
}

#define kHelloString "Hello"
#define kHelloWorldString "Hello, World!"
#define kHelloWorldStringWithEmbeddedNull "Hello\0World"

void CheckString(MonoString* str, const char* expected, size_t len)
{
    GET_AND_CHECK(stringclass, mono_object_get_class((MonoObject*)str));
    CHECK_EQUAL(mono_get_string_class(), stringclass);
    char *utf8 = mono_string_to_utf8(str);
    CHECK_EQUAL(0, strcmp(expected, utf8));

    // mono_string_to_utf8 returns a C string, so it cannot contain \0 characters.
    // Also check if the length of the string matches, to see if we got the characters
    // after the \0 in the kHelloWorldStringWithEmbeddedNull test.
    GET_AND_CHECK(property, mono_class_get_property_from_name (mono_get_string_class(), "Length"));
    GET_AND_CHECK(method, mono_property_get_get_method(property));
    MonoObject* returnValue = mono_runtime_invoke(method, str, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);
    CHECK_EQUAL(len, int_result);

    mono_unity_g_free(utf8);
}

TEST(mono_string_new_wrapper_creates_string)
{
    CheckString(mono_string_new_wrapper(kHelloWorldString), kHelloWorldString, 13);
}

TEST(mono_string_new_len_creates_string)
{
    CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldString, 13), kHelloWorldString, 13);
    CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldString, 5), kHelloString, 5);
    CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldStringWithEmbeddedNull, 11), kHelloWorldStringWithEmbeddedNull, 11);
}

void *ThreadFunc(void *arguments)
{
    CHECK(mono_domain_get() == NULL);
    GET_AND_CHECK(thread, mono_thread_attach(mono_get_root_domain()));
    CHECK_EQUAL(mono_get_root_domain(), mono_domain_get());
    mono_thread_detach(thread);
    CHECK(mono_domain_get() == NULL);
    return NULL;
}

#if !WIN32
TEST(can_use_mono_domain_get_to_check_if_thread_is_attached)
{
    pthread_t thread;
    pthread_create(&thread, NULL, ThreadFunc, NULL);
    pthread_join(thread, NULL);
}
#endif

TEST(can_access_array_elements)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodReturningArray", 0);
    MonoArray* returnValue = (MonoArray*)mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    GET_AND_CHECK(arrayInt32Class, mono_object_get_class((MonoObject*)returnValue));
    CHECK_EQUAL(sizeof(int), mono_array_element_size(arrayInt32Class));
    for (int i=0; i<6; i++)
        CHECK_EQUAL(i + 1, *(int*)scripting_array_element_ptr(returnValue, i, sizeof(int)));
}

TEST(can_access_array_elements_2d)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "StaticMethodReturning2DArray", 0);
    MonoArray* returnValue = (MonoArray*)mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    GET_AND_CHECK(arrayInt32Class, mono_object_get_class((MonoObject*)returnValue));
    CHECK_EQUAL(sizeof(int), mono_array_element_size(arrayInt32Class));
    for (int i=0; i<6; i++)
        CHECK_EQUAL(i + 1, *(int*)scripting_array_element_ptr(returnValue, i, sizeof(int)));
}

int InternalMethod()
{
   return 42;
}

int InternalMethodInNestedClass()
{
   return 23;
}

TEST(can_call_internal_method)
{
    mono_add_internal_call("TestDll.ICallTest::InternalMethod", reinterpret_cast<gconstpointer>(InternalMethod));
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "ICallTest", "CallInternalMethod", 0);
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(42, int_result);
}

TEST(can_call_internal_method_in_nested_class)
{
    mono_add_internal_call("TestDll.ICallTest/NestedClass::InternalMethodInNestedClass", reinterpret_cast<gconstpointer>(InternalMethodInNestedClass));
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "ICallTest", "CallInternalMethodInNestedClass", 0);
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(23, int_result);
}

MonoString* InternalMethodReturnsStackTrace()
{
    MonoInternalCallFrameOpaque frame;
    // In mono, we don't have (or need) this function, so check for it's existance.
    if (mono_enter_internal_call)
        mono_enter_internal_call(&frame);
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "ICallTest", "ReturnStackTrace", 0);
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    if (mono_exit_internal_call)
        mono_exit_internal_call(&frame);
    return (MonoString*)returnValue;
}

#if ENABLE_FAILING_TESTS
TEST(can_get_full_stack_trace_in_internal_method)
{
    mono_add_internal_call("TestDll.ICallTest::InternalMethodReturnsStackTrace", reinterpret_cast<gconstpointer>(InternalMethodReturnsStackTrace));
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "ICallTest", "CallInternalMethodReturnsStackTrace", 0);
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    char* str = mono_string_to_utf8((MonoString*)returnValue);
    CHECK(strstr(str, "ReturnStackTrace"));
    CHECK(strstr(str, "InternalMethodReturnsStackTrace"));
    CHECK(strstr(str, "CallInternalMethodReturnsStackTrace"));
    mono_unity_g_free(str);
}
#endif

static const char* find_plugin_callback(const char* name)
{
    printf("Load plugin %s\n", name);

    if (strcmp(name, "foo.lib") == 0)
        return abs_path_from_file("nativelib/nativelib.dylib").c_str();

    return NULL;
}

#if ENABLE_FAILING_TESTS
TEST(can_call_dllimport_method_with_custom_dlopen_callback)
{
    mono_set_find_plugin_callback((gconstpointer)find_plugin_callback);

    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "ICallTest", "CallDllImportFunction", 2);
    int param1 = 10;
    int param2 = 15;
    void* params[2] = { &param1, &param2 };
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(25, int_result);
}

TEST(mono_runtime_unhandled_exception_policy_set_exception_on_thread_will_not_kill_app)
{
    mono_runtime_unhandled_exception_policy_set(MONO_UNHANDLED_POLICY_LEGACY);
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "ThreadTest", "RunThreadWhichThrows", 0);
    MonoObject* returnValue = (MonoObject*)mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    bool bool_result = *(bool*)mono_object_unbox(returnValue);
    CHECK(bool_result);
}
#endif

#if WIN32
#define sleep Sleep;
#endif

bool g_WaitForGC;
void InternalMethodWhichBlocks()
{
    MonoInternalCallFrameOpaque frame;
    // In mono, we don't have (or need) this function, so check for it's existance.
    if (mono_enter_internal_call)
        mono_enter_internal_call(&frame);

    g_WaitForGC = true;
    while (g_WaitForGC)
        sleep(1);

    if (mono_exit_internal_call)
        mono_exit_internal_call(&frame);
}
#if ENABLE_FAILING_TESTS
// This test simulates a scenario where an icall on a thread needs to be interrupted by the GC or we get a deadlock.
// We have situations like this in Unity. For this reason, our icalls need to be in preemtive mode in CoreCLR.
TEST(internal_method_can_be_interrupted_by_gc)
{
    mono_add_internal_call("TestDll.ICallTest::InternalMethodWhichBlocks", reinterpret_cast<gconstpointer>(InternalMethodWhichBlocks));
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "ThreadTest", "RunThreadWhichBlocksInInternalMethod", 0);
    mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    while (!g_WaitForGC)
        sleep(1);
    mono_gc_collect(mono_gc_max_generation());
    g_WaitForGC = false;
}
#endif

TEST(can_parse_xml_with_win1252_encoding)
{
    MonoMethod* method = GetMethodHelper(kTestDLLNameSpace, "XmlTest", "TestParseXmlWithWin1252Encoding", 0);
    MonoObject* returnValue = (MonoObject*)mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    bool bool_result = *(bool*)mono_object_unbox(returnValue);
    CHECK(bool_result);
}

#if DOMAIN_UNLOAD_TESTS

MonoObject* g_UnloadException = nullptr;
static void UnityDomainUnloadCallback(MonoObject* exc)
{
    g_UnloadException = exc;
}

MonoDomain* LoadTestDllIntoDomain(MonoImage **image)
{
    std::string testDllPath = abs_path_from_file("../unloadable-test-dll/bin/Debug/net461/unloadable-test-dll.dll");

    GET_AND_CHECK(domain, mono_domain_create_appdomain("domain", NULL));

    // Like in the Unity Editor, we use mono_image_open_from_data_with_name to load the reloadable assembly from memory,
    // instead of mono_domain_assembly_open. This allows the editor to change the assembly on disk without causing issues.
    long lSize;
    GET_AND_CHECK(pFile, fopen (testDllPath.c_str() , "rb"));

    fseek (pFile , 0 , SEEK_END);
    lSize = ftell (pFile);
    rewind (pFile);

    GET_AND_CHECK(buffer, (char*) malloc (lSize));
    CHECK_EQUAL(lSize, fread (buffer, 1, lSize, pFile));
    fclose (pFile);

    int status = 0;
    mono_domain_set(domain, true);
    *image = mono_image_open_from_data_with_name((char*)buffer, lSize, true, &status, false, testDllPath.c_str());
    CHECK(*image != NULL);
    CHECK_EQUAL(0, status);
    GET_AND_CHECK(assembly, mono_assembly_load_from_full(*image, testDllPath.c_str(), &status, false));
    CHECK_EQUAL(0, status);
    mono_domain_set(g_domain, true);

    return domain;
}

TEST(can_load_assembly_into_domain_and_call_into_it)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "MethodReturningInt", 0));
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    int int_result = *(int*)mono_object_unbox(returnValue);

    CHECK_EQUAL(42, int_result);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
}

// Since we are loading the unloadable test dll from memory using mono_image_open_from_data_with_name,
// we need to make sure that we correctly associate the path we pass to mono_image_open_from_data_with_name
// as the assembly Location.
TEST(can_load_assembly_into_domain_and_get_assembly_location)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "GetAssemblyLocation", 0));
    MonoString* returnValue = (MonoString*)mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    char *utf8 = mono_string_to_utf8(returnValue);
    CHECK(strstr(utf8, "unloadable-test-dll.dll"));
    mono_unity_g_free(utf8);

    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
}

// Unmodified CoreCLR would assert when a new MethodDesc points to the same internal call
// implementation as an existing entry in the table. But when we reload the ALC containing the
// internal call definition, we get a new MethodDesc, so we need to modify CoreCLR to allow this.
// This test verifies that.
TEST(can_call_internal_method_after_reloading_domain)
{
    mono_add_internal_call("UnloadableTestDll.ICallTest::InternalMethod", reinterpret_cast<gconstpointer>(InternalMethod));

    for (int i=0; i<2; i++)
    {
        MonoImage* image;
        MonoDomain* domain = LoadTestDllIntoDomain(&image);
        GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "ICallTest"));
        GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "CallInternalMethod", 0));
        MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
        int int_result = *(int*)mono_object_unbox(returnValue);
        CHECK_EQUAL(42, int_result);

        g_UnloadException = nullptr;
        mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
        CHECK(g_UnloadException == nullptr);
    }
}

// This needs to be a separate function, so the object itself is not alive on the stack
// and can be collected when the function exits.
NOINLINE
guint32 SetupDomainTestObjectHandle(MonoDomain* domain, MonoClass* klass, bool weak, bool pinned = false)
{
    GET_AND_CHECK(obj, mono_object_new(domain, klass));
    guint32 handle = weak ? mono_gchandle_new_weakref_v2(obj, false) : mono_gchandle_new_v2(obj, false);
    CHECK_EQUAL(obj, mono_gchandle_get_target_v2(handle));
    return handle;
}

NOINLINE
guint32 SetupDomainTestTypeObjectHandle(MonoDomain* domain, MonoClass* klass, bool weak, bool pinned = false)
{
    GET_AND_CHECK(obj, mono_type_get_object(domain, mono_class_get_type(klass)));
    guint32 handle = weak ? mono_gchandle_new_weakref_v2(obj, false) : mono_gchandle_new_v2(obj, pinned);
    CHECK_EQUAL(obj, mono_gchandle_get_target_v2(handle));
    return handle;
}

TEST(unloading_domain_unloads_its_objects)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    guint32 gchandle = SetupDomainTestObjectHandle(domain, klass, true);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(gchandle) == NULL);
}

TEST(unloading_domain_unloads_its_objects_even_if_object_creates_handle_in_finalizer)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "ClassWhichCreatesGCHandleToItselfInFinalizer"));
    guint32 gchandle = SetupDomainTestObjectHandle(domain, klass, true);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(gchandle) == NULL);
}

TEST(unloading_domain_unloads_type_objects)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    guint32 gchandle = SetupDomainTestTypeObjectHandle(domain, klass, true);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(gchandle) == NULL);
}

TEST(unloading_domain_unloads_its_objects_even_if_protected_by_gchandle)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    guint32 gchandle = SetupDomainTestObjectHandle(domain, klass, false);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(gchandle) == NULL);
}

TEST(unloading_domain_unloads_its_objects_even_if_protected_by_pinned_gchandle)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    guint32 gchandle = SetupDomainTestObjectHandle(domain, klass, false, true);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(gchandle) == NULL);
}

TEST(unloading_domain_unloads_its_objects_even_if_in_static_reference)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "SetupStaticRef", 0));
    mono_runtime_invoke(method, nullptr, nullptr, nullptr);

    guint32 gchandle = SetupDomainTestObjectHandle(domain, klass, false);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(gchandle) == NULL);
}

TEST(unloading_domain_unloads_its_objects_even_if_protected_by_gchandle_indirectly)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "SetupGCHandleIndirect", 0));
    mono_runtime_invoke(method, nullptr, nullptr, nullptr);

    guint32 gchandle = SetupDomainTestObjectHandle(domain, klass, false);
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(gchandle) == NULL);
}

TEST(unloading_domain_unloads_its_objects_even_if_protected_by_stack_slot)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(obj, mono_object_new(domain, klass));
    guint32 handle = mono_gchandle_new_weakref_v2(obj, false);
    CHECK_EQUAL(obj, mono_gchandle_get_target_v2(handle));

    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(mono_gchandle_get_target_v2(handle) == NULL);
    CHECK(obj != nullptr); // Stack slot now points to invalid memory
}

bool gUnloadNotificationWasCalled;
void UnloadNotification()
{
    gUnloadNotificationWasCalled = true;
}

TEST(unloading_domain_calls_unload_event)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    mono_domain_set(domain, true);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "SetupUnloadCallback", 0));
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    mono_add_internal_call("UnloadableTestDll.TestClass::UnloadNotification", reinterpret_cast<gconstpointer>(UnloadNotification));

    mono_domain_set(g_domain, true);
    gUnloadNotificationWasCalled = false;
    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
    CHECK(gUnloadNotificationWasCalled);
}

TEST(unloading_domain_works_even_if_it_sets_up_an_exception_handler)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "SetupUnhandledExceptionHandler", 0));
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);

    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
}

#if DOMAIN_UNLOAD_THREAD_TESTS
TEST(unloading_domain_works_even_if_a_thread_is_running)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "CreateThread", 0));
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);

    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
}

TEST(unloading_domain_works_even_if_a_thread_is_running_an_infinite_loop)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "CreateThreadInfiniteLoop", 0));
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, nullptr, nullptr);

    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
}

TEST(unloading_domain_works_even_if_it_sets_up_a_filesystem_watcher)
{
    MonoImage* image;
    MonoDomain* domain = LoadTestDllIntoDomain(&image);
    GET_AND_CHECK(klass, mono_class_from_name(image, "UnloadableTestDll", "TestClass"));
    GET_AND_CHECK(method, mono_class_get_method_from_name (klass, "SetupFileSystemWatcher", 1));
    void* params[1] = { mono_string_new_wrapper(abs_path_from_file("../").c_str())};
    MonoObject* returnValue = mono_runtime_invoke(method, nullptr, params, nullptr);

    g_UnloadException = nullptr;
    mono_unity_domain_unload(domain, UnityDomainUnloadCallback);
    CHECK(g_UnloadException == nullptr);
}
#endif // DOMAIN_UNLOAD_THREAD_TESTS
#endif // DOMAIN_UNLOAD_TESTS

#if ENABLE_FAILING_TESTS
TEST(can_create_exception_from_name)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(ex, mono_exception_from_name_msg(image, kTestDLLNameSpace, "TestException", "Hello"));
    CHECK(mono_class_is_subclass_of(mono_object_get_class((MonoObject*)ex), mono_get_exception_class(), false));
}
#endif

TEST(can_create_argument_null_exception)
{
    GET_AND_CHECK(ex, mono_get_exception_argument_null("MyArg"));
    CHECK(mono_class_is_subclass_of(mono_object_get_class((MonoObject*)ex), mono_get_exception_class(), false));
}

void InternalMethodWhichThrows()
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(ex, mono_exception_from_name_msg(image, kTestDLLNameSpace, "TestException", "Hello"));
    mono_raise_exception(ex);
}

void InternalMethodWhichReturnsExceptionInRefParam(MonoException **e)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(ex, mono_exception_from_name_msg(image, kTestDLLNameSpace, "TestException", "Hello"));
    *e = ex;
}

#if ENABLE_FAILING_TESTS
TEST(can_throw_exception_from_internal_method)
{
    MonoMethod* method;
    if (g_Mode == Mono)
    {
        mono_add_internal_call("TestDll.ICallTest::InternalMethodWhichThrows", reinterpret_cast<gconstpointer>(InternalMethodWhichThrows));
        method = GetMethodHelper(kTestDLLNameSpace, "ICallTest", "CallInternalMethodWhichThrowsAndCatchExceptionMono", 0);
    }
    else
    {
        mono_add_internal_call("TestDll.ICallTest::InternalMethodWhichReturnsExceptionInRefParam", reinterpret_cast<gconstpointer>(InternalMethodWhichReturnsExceptionInRefParam));
        method = GetMethodHelper(kTestDLLNameSpace, "ICallTest", "CallInternalMethodWhichThrowsAndCatchExceptionCoreCLR", 0);
    }
    MonoString* returnValue = (MonoString*)mono_runtime_invoke(method, nullptr, nullptr, nullptr);
    char *utf8 = mono_string_to_utf8(returnValue);
    CHECK_EQUAL("Hello", utf8);
    mono_unity_g_free(utf8);
}
#endif // ENABLE_FAILING_TESTS

#if ENABLE_FAILING_TESTS
#define REMAP_TEST_SRC_PATH_NAME "Foo.txt"
#define REMAP_TEST_SRC_ASSEMBLY_NAME "Foo.dll"

size_t RemapMonoPath(const char* path, char* buffer, size_t bufferLen)
{
    const char* remapped;
    std::string remappedString;
    if (strstr(path, REMAP_TEST_SRC_PATH_NAME) != NULL)
    {
        remappedString = abs_path_from_file("Hello.txt");
    }

    if (strstr(path, REMAP_TEST_SRC_ASSEMBLY_NAME) != NULL)
    {
        remappedString = abs_path_from_file("../unloadable-test-dll/bin/Debug/net461/unloadable-test-dll.dll");
    }

    if (remappedString.empty())
        return 0;

    remapped = remappedString.c_str();

    printf("Remap %s to %s\n", path, remapped);
    size_t lenNeeded = strlen(remapped);
    if (bufferLen >= lenNeeded)
        strcpy(buffer, remapped);
    return lenNeeded;
}

TEST(mono_unity_register_path_remapper_can_remap_file_read)
{
    mono_unity_register_path_remapper (RemapMonoPath);

    MonoMethod* readAllTextMethod = GetMethodHelper(kTestDLLNameSpace, kTestClassName, "ReadAllTextSafe", 1);

    void* params[1] = { mono_string_new_wrapper(REMAP_TEST_SRC_PATH_NAME)};
    MonoString* returnValue = (MonoString*)mono_runtime_invoke(readAllTextMethod, nullptr, params, nullptr);
    CHECK(returnValue != NULL);
    char* utf8 = mono_string_to_utf8(returnValue);

    CHECK_EQUAL("Hello" + kNewLine, utf8);
    mono_unity_g_free(utf8);


    mono_unity_register_path_remapper (NULL);

    returnValue = (MonoString*)mono_runtime_invoke(readAllTextMethod, nullptr, params, nullptr);
    CHECK(returnValue == NULL);
}

TEST(mono_unity_register_path_remapper_can_remap_assembly_load)
{
    MonoAssembly *forwarderAssembly = mono_domain_assembly_open (g_domain, "Foo.dll");
    CHECK(forwarderAssembly == NULL);

    mono_unity_register_path_remapper (RemapMonoPath);

    forwarderAssembly = mono_domain_assembly_open (g_domain, "Foo.dll");
    CHECK(forwarderAssembly != NULL);

    mono_unity_register_path_remapper (NULL);
}
#endif // ENABLE_FAILING_TESTS

void SetupMono(Mode mode)
{
    g_Mode = mode;
#if defined(_DEBUG)
    std::string testDllPath = abs_path_from_file("../../artifacts/bin/coreclr-test/Debug/net6.0/coreclr-test.dll");
#else
    std::string testDllPath = abs_path_from_file("../../artifacts/bin/coreclr-test/Release/net6.0/coreclr-test.dll");
#endif

    std::string monoLibFolder;
    std::string assembliesPaths;
    if (mode == CoreCLR)
    {
#if defined(__APPLE__)
#if defined(_DEBUG)
#ifdef __aarch64__
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Debug/runtimes/osx-arm64/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Debug/runtimes/osx-arm64/native/libcoreclr.dylib");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Debug/runtimes/osx-x64/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Debug/runtimes/osx-x64/native/libcoreclr.dylib");
#endif // __aarch64__
#else
#ifdef __aarch64__
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/native/libcoreclr.dylib");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Release/runtimes/osx-x64/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Release/runtimes/osx-x64/native/libcoreclr.dylib");
#endif // __aarch64__
#endif
#elif defined(__linux__)
        monoLibFolder = "/usr/share/dotnet/shared/Microsoft.NETCore.App/3.1.0";
        g_monoDllPath = "../../bin/Product/Linux.x64.Debug/libcoreclr.so";
#elif defined(WIN32)
#if defined(_DEBUG)
#ifdef _M_AMD64
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Debug/runtimes/win-x64/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Debug/runtimes/win-x64/native/coreclr.dll");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Debug/runtimes/win-x86/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Debug/runtimes/win-x86/native/coreclr.dll");
#endif
#else
#ifdef _M_AMD64
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Release/runtimes/win-x64/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Release/runtimes/win-x64/native/coreclr.dll");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Release/runtimes/win-x86/lib/net7.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Release/runtimes/win-x86/native/coreclr.dll");
#endif
#endif
#else
        printf("Unsupported platform\n");
        g_monoDllPath = "";
#endif
    }
    else
    {
        monoLibFolder = abs_path_from_unity_root("External/MonoBleedingEdge/builds/monodistribution/lib");
#if defined(__APPLE__)
        g_monoDllPath = abs_path_from_unity_root("External/MonoBleedingEdge/builds/embedruntimes/osx/libmonobdwgc-2.0.dylib");
#elif defined(__linux__)
        g_monoDllPath = abs_path_from_unity_root("External/MonoBleedingEdge/builds/embedruntimes/linux64/libmonobdwgc-2.0.so");
#elif defined(WIN32)
        g_monoDllPath = abs_path_from_unity_root("External/MonoBleedingEdge/builds/embedruntimes/win64/mono-2.0-bdwgc.dll");
#endif
    }

    #define DO_API(r,n,p) typedef r (*type_##n)p; n = (type_##n)get_method(#n);
    #include "../../src/coreclr/vm/mono/MonoFunctionsClr.h"
    #undef DO_API

    printf("Setting up directories for Mono...\n");
    mono_set_dirs(monoLibFolder.c_str(), "");

    char* assembliesPathsNullTerm;

    if (mode == CoreCLR)
    {
#if defined(_DEBUG)
        assembliesPaths = abs_path_from_file("../../artifacts/bin/unity-embed-host/Debug/net6.0");
#else
        assembliesPaths = abs_path_from_file("../../artifacts/bin/unity-embed-host/Release/net6.0");
#endif
        auto assembliesPathsChar = assembliesPaths.c_str();
        assembliesPathsNullTerm = new char[strlen(assembliesPathsChar) + 2];
        strcpy(assembliesPathsNullTerm, assembliesPathsChar);
        assembliesPathsNullTerm[strlen(assembliesPathsChar) + 1] = '\0';
        mono_set_assemblies_path_null_separated(assembliesPathsNullTerm);
        delete [] assembliesPathsNullTerm;
    }

    g_domain = mono_jit_init_version("myapp", "v4.0.30319");
    g_assembly = mono_domain_assembly_open(g_domain, testDllPath.c_str());
}

void ShutdownMono()
{
    printf("Cleaning up...\n");
    mono_unity_jit_cleanup(g_domain);

#if JON
    // we cannot close the coreclr library
    dlclose(s_MonoLibrary);
#endif
    s_MonoLibrary = NULL;
}

int RunTests(Mode mode)
{
    SetupMono(mode);

    Catch::Session session;
    int result = session.run();

    ShutdownMono();

    return result;
}

int main(int argc, char * argv[])
{
    if (getenv("UNITY_ROOT") != NULL)
        return RunTests(Mono);

    return RunTests(CoreCLR);
}
