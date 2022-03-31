import { mono_wasm_get_jsobj_from_js_handle, mono_wasm_get_js_handle } from "./gc-handles";
import { js_to_cs_marshalers, marshal_exception_to_cs } from "./marshal-to-cs";
import { cs_to_js_marshalers } from "./marshal-to-js";
import { get_signature_argument_count, get_signature_type, get_custom_marshalers, JavaScriptMarshalerArguments as JavaScriptMarshalerArguments, JavaScriptMarshalerArgSize, JavaScriptMarshalerSignature as JavaScriptMarshalerSignature } from "./marshal";
import { setI32 } from "./memory";
import { _get_type_name } from "./method-binding";
import { wrap_error } from "./method-calls";
import { conv_string } from "./strings";
import { assert, JSHandle, MonoString, MonoStringNull, MonoTypeNull } from "./types";
import { Int32Ptr } from "./types/emscripten";
import { INTERNAL } from "./imports";

const bound_function_symbol = Symbol.for("wasm bound_js_function");

export function mono_wasm_bind_js_function(function_name: MonoString, signature: JavaScriptMarshalerSignature, function_js_handle: Int32Ptr, is_exception: Int32Ptr): MonoString {
    try {
        const fn = mono_wasm_lookup_function(function_name);
        const args_count = get_signature_argument_count(signature);

        const { cs_to_js_custom_marshalers, js_to_cs_custom_marshalers } = get_custom_marshalers(signature);

        const closure: any = { fn, assert, marshal_exception_to_cs };
        const js_function_name = conv_string(function_name)!;
        const bound_js_function_name = "_bound_" + js_function_name.replaceAll(".", "_");
        let body = `//# sourceURL=https://mono-wasm.invalid/${bound_js_function_name} \n`;
        let converter_names = "";
        for (let index = 0; index < args_count; index++) {
            converter_names += `, converter${index + 1}`;
        }
        const res_mono_type = get_signature_type(signature, 1);
        if (res_mono_type !== MonoTypeNull) {
            converter_names += ", resultconverter";
        }

        body += `const { fn, assert, marshal_exception_to_cs ${converter_names} } = closure;\n`;
        body += `return function ${bound_js_function_name} (buffer) { try {\n`;

        let pass_args = "";
        for (let index = 0; index < args_count; index++) {
            const arg_offset = (index + 2) * JavaScriptMarshalerArgSize;
            const mono_type = get_signature_type(signature, index + 2);
            let converter = cs_to_js_marshalers.get(mono_type);
            if (!converter) {
                converter = cs_to_js_custom_marshalers.get(mono_type);
            }
            assert(converter, () => `Unknow converter for type ${_get_type_name(mono_type)} at ${index}`);
            const converter_name = `converter${index + 1}`;
            closure[converter_name] = converter;

            body += `  const arg${index + 1}_js = ${converter_name}(buffer + ${arg_offset}); // ${_get_type_name(mono_type)} \n`;

            if (pass_args === "") {
                pass_args += `arg${index + 1}_js`;
            } else {
                pass_args += `, arg${index + 1}_js`;
            }
        }
        body += `  const res_js = fn(${pass_args});\n`;

        if (res_mono_type === MonoTypeNull) {
            body += `  if (res_js !== undefined) throw new Error('Function ${js_function_name} returned unexpected value, C# signature is void');\n`;
        } else {
            let converter = js_to_cs_marshalers.get(res_mono_type);
            if (!converter) {
                converter = js_to_cs_custom_marshalers.get(res_mono_type);
            }
            assert(converter, () => `Unknow converter for type ${res_mono_type} at ret`);
            closure["resultconverter"] = converter;

            body += `  resultconverter(buffer + ${JavaScriptMarshalerArgSize}, res_js); // ${_get_type_name(res_mono_type)} \n`;
        }

        body += "} catch (ex) {\n";
        body += "  marshal_exception_to_cs(buffer, ex);\n";
        body += "}}";
        //console.log("-------");
        //console.log(body);
        //console.log("-------");
        const factory = new Function("closure", body);
        const bound_fn = factory(closure);
        bound_fn[bound_function_symbol] = true;
        const bound_function_handle = mono_wasm_get_js_handle(bound_fn)!;
        setI32(function_js_handle, <any>bound_function_handle);
        return MonoStringNull;
    } catch (ex) {
        return wrap_error(is_exception, ex);
    }
}

export function mono_wasm_invoke_bound_function(bound_function_js_handle: JSHandle, args: JavaScriptMarshalerArguments): void {
    const bound_fn = mono_wasm_get_jsobj_from_js_handle(bound_function_js_handle);
    assert(bound_fn && typeof (bound_fn) === "function" && bound_fn[bound_function_symbol], () => `Bound function handle expected ${bound_function_js_handle}`);
    bound_fn(args);
}

function mono_wasm_lookup_function(function_id: MonoString): Function {
    const js_function_name = conv_string(function_id);
    assert(js_function_name, () => "js_function_name must be string");

    let scope: any = globalThis;
    const parts = js_function_name.split(".");
    if (parts[0] === "INTERNAL") {
        scope = INTERNAL;
        parts.shift();
    }

    for (let i = 0; i < parts.length - 1; i++) {
        const part = parts[i];
        const newscope = scope[part];
        assert(newscope, () => `${part} not found while looking up ${js_function_name}`);
        scope = newscope;
    }

    const fname = parts[parts.length - 1];
    const fn = scope[fname];

    assert(typeof (fn) === "function", () => `${js_function_name} must be a Function but was ${typeof fn}`);
    return fn;
}
