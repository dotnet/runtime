import { MonoString } from "./export-types";
import { INTERNAL, Module } from "./imports";
import { js_to_cs_marshalers } from "./marshal-to-cs";
import { cs_to_js_marshalers, marshal_exception_to_js } from "./marshal-to-js";
import {
    get_arg, get_sig_buffer_offset, get_sig_type, get_sig_use_root, get_sig,
    set_arg_type, set_extra_buffer, set_root,
    JavaScriptMarshalerArguments, JavaScriptMarshalerArgSize, JavaScriptMarshalerSignature, get_signature_buffer_length, get_signature_argument_count, is_args_exception, get_custom_marshalers, get_signature_type,
} from "./marshal";
import { parseFQN, wrap_error } from "./method-calls";
import { mono_wasm_new_root_buffer, WasmRootBuffer } from "./roots";
import { conv_string } from "./strings";
import { assert, MonoStringNull, MonoTypeNull, NativePointerNull } from "./types";
import { Int32Ptr, NativePointer, VoidPtr } from "./types/emscripten";
import cwraps, { wrap_c_function } from "./cwraps";
import { find_method, _get_type_name } from "./method-binding";

const exportedMethods = new Map<string, Function>();

// TODO replace mono_bind_static_method with this
export function mono_bind_static_method2(fqn: string): Function {
    const fn = exportedMethods.get(fqn);
    assert(fn, () => `Function ${fqn} has to be marked with [JSExportAttribute]`);
    return fn;
}

const bound_function_symbol = Symbol.for("wasm bound_cs_function");

export function mono_wasm_bind_cs_function(fully_qualified_name: MonoString, signature_hash: number, export_as_name: MonoString, signature: JavaScriptMarshalerSignature, is_exception: Int32Ptr): MonoString {
    const anyModule = Module as any;
    try {
        //TODO generate code

        const args_count = get_signature_argument_count(signature);
        const extra_buffer_length = get_signature_buffer_length(signature);
        const js_fqn = conv_string(fully_qualified_name);
        assert(js_fqn, "fully_qualified_name must be string");


        const { assembly, namespace, classname, methodname } = parseFQN(js_fqn);

        const asm = cwraps.mono_wasm_assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);

        const klass = cwraps.mono_wasm_assembly_find_class(asm, namespace, classname);
        if (!klass)
            throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);

        const wrapper_name = `__Wrapper_${methodname}_${signature_hash}`;
        const method = find_method(klass, wrapper_name, -1);
        if (!method)
            throw new Error(`Could not find method: ${wrapper_name} in ${klass} [${assembly}]`);

        const { cs_to_js_custom_marshalers, js_to_cs_custom_marshalers } = get_custom_marshalers(signature);

        const closure: any = {
            method, get_arg, signature,
            stackSave: anyModule.stackSave, stackAlloc: anyModule.stackAlloc, stackRestore: anyModule.stackRestore,
            mono_wasm_new_root_buffer, init_void, init_result, init_argument, marshal_exception_to_js, is_args_exception,
            mono_wasm_invoke_method_bound: wrap_c_function("mono_wasm_invoke_method_bound"),
        };
        const bound_js_function_name = "_bound_" + `${namespace}_${classname}_${methodname}`.replaceAll(".", "_");
        let body = `//# sourceURL=https://mono-wasm.invalid/${bound_js_function_name} \n`;
        let converter_names = "";
        for (let index = 0; index < args_count; index++) {
            converter_names += `, converter${index + 1}`;
        }
        const res_mono_type = get_signature_type(signature, 1);
        if (res_mono_type !== MonoTypeNull) {
            converter_names += ", resultconverter";
        }
        body += `const { method, get_arg, signature, stackSave, stackAlloc, stackRestore, mono_wasm_new_root_buffer, init_void, init_result, init_argument, marshal_exception_to_js, is_args_exception, mono_wasm_invoke_method_bound ${converter_names} } = closure;\n`;
        body += `return function ${bound_js_function_name} () {\n`;
        body += "let roots = null;\n";
        body += "const sp = stackSave();\n";
        body += "try {\n";
        body += `  roots = mono_wasm_new_root_buffer(${args_count + 2}); // TODO, optimize to pool allocation\n`;
        body += `  const args = stackAlloc(${(args_count + 2) * JavaScriptMarshalerArgSize + extra_buffer_length});\n`;
        body += `  const extra = args + ${(args_count + 2) * JavaScriptMarshalerArgSize};\n`;
        if (res_mono_type !== MonoTypeNull) {
            body += "  init_result(roots, args, extra, signature);\n";
        } else {
            body += "  init_void(roots, args, extra, signature);\n";
        }
        for (let index = 0; index < args_count; index++) {
            body += `  init_argument(${1 + index}, roots, args, extra, signature);\n`;
        }
        for (let index = 0; index < args_count; index++) {
            const arg_offset = (index + 2) * JavaScriptMarshalerArgSize;
            const mono_type = get_signature_type(signature, index + 2);
            let converter = js_to_cs_marshalers.get(mono_type);
            if (!converter) {
                converter = js_to_cs_custom_marshalers.get(mono_type);
            }
            assert(converter && typeof converter === "function", () => `Unknow converter for type ${_get_type_name(mono_type)} at ${index} `);
            const converter_name = `converter${index + 1}`;
            closure[converter_name] = converter;

            body += `  ${converter_name}(args + ${arg_offset}, arguments[${index}]); // ${_get_type_name(mono_type)} \n`;
        }
        body += "  const fail = mono_wasm_invoke_method_bound(method, args);\n";
        body += "  if (fail) throw new Error(\"ERR22: Unexpected error: \" + conv_string(fail));\n";
        body += "  if (is_args_exception(args)) throw marshal_exception_to_js(get_arg(args, 0));\n";

        if (res_mono_type === MonoTypeNull) {
            // TODO emit assert
        } else {
            let converter = cs_to_js_marshalers.get(res_mono_type);
            if (!converter) {
                converter = cs_to_js_custom_marshalers.get(res_mono_type);
            }
            assert(converter && typeof converter === "function", () => `Unknow converter for type ${res_mono_type} at ret`);
            closure["resultconverter"] = converter;

            body += `  return resultconverter(args + ${JavaScriptMarshalerArgSize}); // ${_get_type_name(res_mono_type)} \n`;
        }

        body += "} finally {\n";
        body += "  stackRestore(sp);\n";
        body += "  if(roots) roots.release()\n";
        body += "}}";
        //console.log("-------");
        //console.log(body);
        //console.log("-------");
        const factory = new Function("closure", body);
        const bound_fn = factory(closure);
        bound_fn[bound_function_symbol] = true;

        exportedMethods.set(js_fqn, bound_fn);

        if (export_as_name) {
            const js_export_name = conv_string(export_as_name);
            assert(js_export_name, "export_as_name must be string");
            _walk_global_scope_to_set_function(js_export_name, bound_fn);
        }

        return MonoStringNull;
    }
    catch (ex) {
        return wrap_error(is_exception, ex);
    }
}

function init_void(roots: WasmRootBuffer, args: JavaScriptMarshalerArguments) {
    assert(args && (<any>args) % 8 == 0, "Arg alignment");
    const exc = get_arg(args, 0);
    const excRoot = roots.get_address(0);
    set_arg_type(exc, MonoTypeNull);
    set_root(exc, excRoot);

    const res = get_arg(args, 1);
    set_arg_type(res, MonoTypeNull);
    set_root(res, NativePointerNull);
    set_extra_buffer(res, NativePointerNull);
}

function init_result(roots: WasmRootBuffer, args: JavaScriptMarshalerArguments, extra: NativePointer, signature: JavaScriptMarshalerSignature) {
    assert(args && (<any>args) % 8 == 0, "Arg alignment");
    const exc = get_arg(args, 0);
    const excRoot = roots.get_address(0);
    set_arg_type(exc, MonoTypeNull);
    set_root(exc, excRoot);

    const res = get_arg(args, 1);
    const resSig = get_sig(signature, 1);

    set_arg_type(res, get_sig_type(resSig));
    const bufferOffset = get_sig_buffer_offset(resSig);
    const useRoot = get_sig_use_root(resSig);
    if (bufferOffset != -1) {
        set_extra_buffer(res, <any>extra + bufferOffset);
    }
    if (useRoot != 0) {
        set_root(res, roots.get_address(1));
    }
}
// arg1 is at position=1
function init_argument(position: number, roots: WasmRootBuffer, args: JavaScriptMarshalerArguments, extra: VoidPtr, signature: JavaScriptMarshalerSignature) {
    assert(args && (<any>args) % 8 == 0, "Arg alignment");

    const arg = get_arg(args, position + 1);
    const sig = get_sig(signature, position + 1);

    set_arg_type(arg, get_sig_type(sig));
    const bufferOffset = get_sig_buffer_offset(sig);
    const useRoot = get_sig_use_root(sig);
    if (bufferOffset != -1) {
        set_extra_buffer(arg, <any>extra + bufferOffset);
    }
    if (useRoot != 0) {
        set_root(arg, roots.get_address(position + 1));
    }
}

function _walk_global_scope_to_set_function(str: string, fn: Function): void {
    let scope: any = globalThis;
    const parts = str.split(".");
    if (parts[0] === "INTERNAL") {
        scope = INTERNAL;
        parts.shift();
    }

    for (let i = 0; i < parts.length - 1; i++) {
        const part = parts[i];
        let newscope = scope[part];
        if (!newscope) {
            newscope = {};
            scope[part] = newscope;
        }
        assert(newscope, () => `${part} not found while looking up ${str}`);
        scope = newscope;
    }

    const fname = parts[parts.length - 1];
    scope[fname] = fn;
}
