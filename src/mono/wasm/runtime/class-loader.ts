import { MonoAssembly, MonoClass, MonoType, MonoTypeNull, MonoAssemblyNull } from "./types";
import cwraps from "./cwraps";

const _assembly_cache_by_name = new Map<string, MonoAssembly>();
const _class_cache_by_assembly = new Map<MonoAssembly, Map<string, Map<string, MonoClass>>>();
let _corlib: MonoAssembly = MonoAssemblyNull;

export function assembly_load(name: string): MonoAssembly {
    if (_assembly_cache_by_name.has(name))
        return <MonoAssembly>_assembly_cache_by_name.get(name);

    const result = cwraps.mono_wasm_assembly_load(name);
    _assembly_cache_by_name.set(name, result);
    return result;
}

function _find_cached_class(assembly: MonoAssembly, namespace: string, name: string): MonoClass | undefined {
    let namespaces = _class_cache_by_assembly.get(assembly);
    if (!namespaces)
        _class_cache_by_assembly.set(assembly, namespaces = new Map());

    let classes = namespaces.get(namespace);
    if (!classes) {
        classes = new Map<string, MonoClass>();
        namespaces.set(namespace, classes);
    }

    return classes.get(name);
}

function _set_cached_class(assembly: MonoAssembly, namespace: string, name: string, ptr: MonoClass) {
    const namespaces = _class_cache_by_assembly.get(assembly);
    if (!namespaces)
        throw new Error("internal error");
    const classes = namespaces.get(namespace);
    if (!classes)
        throw new Error("internal error");
    classes.set(name, ptr);
}

export function find_corlib_class(namespace: string, name: string, throw_on_failure?: boolean | undefined): MonoClass {
    if (!_corlib)
        _corlib = cwraps.mono_wasm_get_corlib();
    let result = _find_cached_class(_corlib, namespace, name);
    if (result !== undefined)
        return result;
    result = cwraps.mono_wasm_assembly_find_class(_corlib, namespace, name);
    if (throw_on_failure && !result)
        throw new Error(`Failed to find corlib class ${namespace}.${name}`);
    _set_cached_class(_corlib, namespace, name, result);
    return result;
}

export function find_class_in_assembly(assembly_name: string, namespace: string, name: string, throw_on_failure?: boolean | undefined): MonoClass {
    const assembly = assembly_load(assembly_name);
    let result = _find_cached_class(assembly, namespace, name);
    if (result !== undefined)
        return result;
    result = cwraps.mono_wasm_assembly_find_class(assembly, namespace, name);
    if (throw_on_failure && !result)
        throw new Error(`Failed to find class ${namespace}.${name} in ${assembly_name}`);
    _set_cached_class(assembly, namespace, name, result);
    return result;
}

export function find_corlib_type(namespace: string, name: string, throw_on_failure?: boolean | undefined): MonoType {
    const classPtr = find_corlib_class(namespace, name, throw_on_failure);
    if (!classPtr)
        return MonoTypeNull;
    return cwraps.mono_wasm_class_get_type(classPtr);
}

export function find_type_in_assembly(assembly_name: string, namespace: string, name: string, throw_on_failure?: boolean | undefined): MonoType {
    const classPtr = find_class_in_assembly(assembly_name, namespace, name, throw_on_failure);
    if (!classPtr)
        return MonoTypeNull;
    return cwraps.mono_wasm_class_get_type(classPtr);
}
