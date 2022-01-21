// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <mono/metadata/assembly.h>
#include <mono/metadata/object.h>

void mono_wasm_load_runtime (const char *unused, int debug_level);
int mono_wasm_add_assembly (const char *name, const unsigned char *data, unsigned int size);
MonoAssembly* mono_wasm_assembly_load(const char *name);
MonoMethod* mono_wasm_assembly_get_entry_point (MonoAssembly *assembly);
MonoClass* mono_wasm_assembly_find_class (MonoAssembly *assembly, const char *namespace, const char *name);
MonoMethod* mono_wasm_assembly_find_method (MonoClass *klass, const char *name, int arguments);
MonoObject* mono_wasm_invoke_method (MonoMethod *method, MonoObject *this_arg, void *params[], MonoObject **out_exc);
int mono_unbox_int (MonoObject *obj);
void mono_wasm_setenv (const char *name, const char *value);
void add_assembly(const char* base_dir, const char *name);

MonoArray* mono_wasm_obj_array_new (int size);
void mono_wasm_obj_array_set (MonoArray *array, int idx, MonoObject *obj);
MonoArray* mono_wasm_string_array_new (int size);
MonoString *mono_wasm_string_from_js (const char *str);
int mono_wasm_array_length(MonoArray* array);
char *mono_wasm_string_get_utf8 (MonoString *str);

MonoMethod* lookup_dotnet_method(const char* assembly_name, const char* namespace, const char* type_name, const char* method_name, int num_params);
