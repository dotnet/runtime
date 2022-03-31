import { MonoObject, MonoString } from "./export-types";
import { MarshalerHelpers } from "./exports";
import { get_js_obj, js_owned_gc_handle_symbol, mono_wasm_get_js_handle } from "./gc-handles";
import { MONO, BINDING, Module } from "./imports";
import { MarshalerToCs } from "./marshal-to-cs";
import { MarshalerToJs } from "./marshal-to-js";
import { getF32, getF64, getI16, getI32, getI64, getU32, getU8, setF32, setF64, setI16, setI32, setI64, setU32, setU8 } from "./memory";
import { _get_type_name } from "./method-binding";
import { wrap_error } from "./method-calls";
import { conv_string } from "./strings";
import { assert, GCHandle, JSHandle, JSHandleNull, MonoStringNull, MonoType, MonoTypeNull } from "./types";
import { Int32Ptr, NativePointer } from "./types/emscripten";

/**
 * Layout of the marshaling buffers is
 * 
 * signature: pointer to [
 *      ArgumentCount: number,
 *      ExtraBufferLength: number,
 *      Exception: { type:MonoType, extraOffset:int, extraLength:int, useRoot:bool, marshaler:JSHandle }
 *      TResult:   { type:MonoType, extraOffset:int, extraLength:int, useRoot:bool, marshaler:JSHandle }
 *      T1:        { type:MonoType, extraOffset:int, extraLength:int, useRoot:bool, marshaler:JSHandle }
 *      T2:        { type:MonoType, extraOffset:int, extraLength:int, useRoot:bool, marshaler:JSHandle }
 *      ...
 *      ] 
 *   - TRes === MonoTypeNull means void function
 * 
 * data: pointer to [
 *      exc:  {type:MonoType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      res:  {type:MonoType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      arg1: {type:MonoType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      arg2: {type:MonoType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      ...
 *      ]
 *   for data see JavaScriptMarshalerArg in C#, discriminated union, 16 bytes
 */


export const JavaScriptMarshalerArgSize = 16;
export const JavaScriptMarshalerSigSize = 20;
export const JavaScriptMarshalerSigOffset = 8;

export interface JavaScriptMarshalerArguments extends NativePointer {
    __brand: "JavaScriptMarshalerArgs"
}

export interface JavaScriptMarshalerSignature extends NativePointer {
    __brand: "JavaScriptMarshalerSignatures"
}

export interface JavaScriptMarshalerSig extends NativePointer {
    __brand: "JavaScriptMarshalerSig"
}

export interface JavaScriptMarshalerArg extends NativePointer {
    __brand: "JavaScriptMarshalerArg"
}

export function get_arg(args: JavaScriptMarshalerArguments, index: number): JavaScriptMarshalerArg {
    assert(args, "Null args");
    return <any>args + (index * JavaScriptMarshalerArgSize);
}

export function is_args_exception(args: JavaScriptMarshalerArguments): boolean {
    assert(args, "Null args");
    const exceptionType = get_arg_type(<any>args);
    return exceptionType !== MonoTypeNull;
}

export function get_sig(signature: JavaScriptMarshalerSignature, index: number): JavaScriptMarshalerSig {
    assert(signature, "Null signatures");
    return <any>signature + (index * JavaScriptMarshalerSigSize) + JavaScriptMarshalerSigOffset;
}

export function get_signature_type(signature: JavaScriptMarshalerSignature, index: number): MonoType {
    assert(signature, "Null signatures");
    const sig = get_sig(signature, index);
    return <any>getU32(sig);
}

export function get_signature_marshaler(signature: JavaScriptMarshalerSignature, index: number): JSHandle {
    assert(signature, "Null signatures");
    const sig = get_sig(signature, index);
    return <any>getU32(<any>sig + 16);
}

export function get_signature_argument_count(signature: JavaScriptMarshalerSignature): number {
    assert(signature, "Null signatures");
    return <any>getU32(signature);
}

export function get_signature_buffer_length(signature: JavaScriptMarshalerSignature): number {
    assert(signature, "Null signatures");
    return <any>getU32(<any>signature + 4);
}

export function get_sig_type(sig: JavaScriptMarshalerSig): MonoType {
    assert(sig, "Null signatures");
    return <any>getU32(sig);
}

export function get_sig_buffer_offset(sig: JavaScriptMarshalerSig): number {
    assert(sig, "Null signatures");
    return <any>getU32(<any>sig + 4);
}

export function get_sig_buffer_length(sig: JavaScriptMarshalerSig): number {
    assert(sig, "Null signatures");
    return <any>getU32(<any>sig + 8);
}

export function get_sig_use_root(sig: JavaScriptMarshalerSig): number {
    assert(sig, "Null signatures");
    return <any>getU32(<any>sig + 12);
}

export function get_arg_type(arg: JavaScriptMarshalerArg): MonoType {
    assert(arg, "Null arg");
    const type = getU32(<any>arg + 12);
    return <any>type;
}

export function set_arg_type(arg: JavaScriptMarshalerArg, type: MonoType): void {
    assert(arg, "Null arg");
    setU32(<any>arg + 12, type);
}

export function get_root_ref(arg: JavaScriptMarshalerArg): MonoObject {
    assert(arg, "Null arg");
    const root = getU32(<any>arg + 8);
    assert(root, "Null root");
    return <any>getU32(root);
}

export function set_root(arg: JavaScriptMarshalerArg, root: NativePointer): void {
    assert(arg, "Null arg");
    setU32(<any>arg + 8, root);
}

export function set_extra_buffer(arg: JavaScriptMarshalerArg, ptr: NativePointer): void {
    assert(arg, "Null arg");
    setU32(<any>arg, ptr);
}

export function get_extra_buffer(arg: JavaScriptMarshalerArg): NativePointer {
    assert(arg, "Null arg");
    return <any>getU32(<any>arg);
}

export function set_root_ref(arg: JavaScriptMarshalerArg, reference: MonoObject): void {
    assert(arg, "Null arg");
    const root = getU32(<any>arg + 8);
    assert(root, "Null root");
    setU32(root, reference);
}

export function get_arg_b8(arg: JavaScriptMarshalerArg): boolean {
    assert(arg, "Null arg");
    return !!getU8(<any>arg);
}

export function get_arg_u8(arg: JavaScriptMarshalerArg): number {
    assert(arg, "Null arg");
    return getU8(<any>arg);
}

export function get_arg_i16(arg: JavaScriptMarshalerArg): number {
    assert(arg, "Null arg");
    return getI16(<any>arg);
}

export function get_arg_i32(arg: JavaScriptMarshalerArg): number {
    assert(arg, "Null arg");
    return getI32(<any>arg);
}

export function get_arg_i64(arg: JavaScriptMarshalerArg): number {
    assert(arg, "Null arg");
    return getI64(<any>arg);
}

export function get_arg_date(arg: JavaScriptMarshalerArg): Date {
    assert(arg, "Null arg");
    const unixTime = getI64(<any>arg);
    const date = new Date(unixTime);
    return date;
}

export function get_arg_f32(arg: JavaScriptMarshalerArg): number {
    assert(arg, "Null arg");
    return getF32(<any>arg);
}

export function get_arg_f64(arg: JavaScriptMarshalerArg): number {
    assert(arg, "Null arg");
    return getF64(<any>arg);
}

export function set_arg_b8(arg: JavaScriptMarshalerArg, value: boolean): void {
    assert(arg, "Null arg");
    setU8(<any>arg, value ? 1 : 0);
}

export function set_arg_u8(arg: JavaScriptMarshalerArg, value: number): void {
    assert(arg, "Null arg");
    setU8(<any>arg, value);
}

export function set_arg_i16(arg: JavaScriptMarshalerArg, value: number): void {
    assert(arg, "Null arg");
    setI16(<any>arg, value);
}

export function set_arg_i32(arg: JavaScriptMarshalerArg, value: number): void {
    assert(arg, "Null arg");
    setI32(<any>arg, value);
}

export function set_arg_i64(arg: JavaScriptMarshalerArg, value: number): void {
    assert(arg, "Null arg");
    setI64(<any>arg, value);
}

export function set_arg_date(arg: JavaScriptMarshalerArg, value: Date): void {
    assert(arg, "Null arg");
    // getTime() is always UTC
    const unixTime = value.getTime();
    setI64(<any>arg, unixTime);
}

export function set_arg_f64(arg: JavaScriptMarshalerArg, value: number): void {
    assert(arg, "Null arg");
    setF64(<any>arg, value);
}

export function set_arg_f32(arg: JavaScriptMarshalerArg, value: number): void {
    assert(arg, "Null arg");
    setF32(<any>arg, value);
}

export function get_js_handle(arg: JavaScriptMarshalerArg): JSHandle {
    assert(arg, "Null arg");
    return <any>getU32(<any>arg + 4);
}

export function set_js_handle(arg: JavaScriptMarshalerArg, jsHandle: JSHandle): void {
    assert(arg, "Null arg");
    setU32(<any>arg + 4, <any>jsHandle);
}

export function get_gc_handle(arg: JavaScriptMarshalerArg): GCHandle {
    assert(arg, "Null arg");
    return <any>getU32(<any>arg + 4);
}

export function set_gc_handle(arg: JavaScriptMarshalerArg, gcHandle: GCHandle): void {
    assert(arg, "Null arg");
    setU32(<any>arg + 4, <any>gcHandle);
}

export class ManagedObject {
    toString(): string {
        return `CsObject(gc_handle: ${(<any>this)[js_owned_gc_handle_symbol]})`;
    }
}

export class ManagedError extends Error {
    constructor(message: string) {
        super(message);
    }

    get stack(): string | undefined {
        //todo implement lazy
        return super.stack;
    }

    toString(): string {
        return `ManagedError(gc_handle: ${(<any>this)[js_owned_gc_handle_symbol]})`;
    }
}

export type KnownTypes = {
    bool: MonoType,
    byte: MonoType,
    int16: MonoType,
    int32: MonoType,
    int64: MonoType,
    double: MonoType,
    float: MonoType,
    task: MonoType,
    intptr: MonoType,
    date_time: MonoType,
    date_time_offset: MonoType,
    string: MonoType,
    ijs_object: MonoType,
    cs_object: MonoType
    exception: MonoType,
    jsexception: MonoType,
}

const marshaler_type_symbol = Symbol.for("wasm marshaler_type");

export function get_custom_marshalers(signature: JavaScriptMarshalerSignature)
    : { cs_to_js_custom_marshalers: Map<MonoType, MarshalerToJs>, js_to_cs_custom_marshalers: Map<MonoType, MarshalerToCs> } {
    const args_count = get_signature_argument_count(signature);
    const cs_to_js_custom_marshalers = new Map<MonoType, MarshalerToJs>();
    const js_to_cs_custom_marshalers = new Map<MonoType, MarshalerToCs>();
    for (let index = 1; index < args_count + 2; index++) {
        const marshaler_js_handle = get_signature_marshaler(signature, index);
        if (marshaler_js_handle !== JSHandleNull) {
            const mono_type = get_signature_type(signature, index);
            const marshaler = get_js_obj(marshaler_js_handle);
            assert(marshaler, () => `Unknow marshaler for type ${_get_type_name(mono_type)} at ${index}`);
            cs_to_js_custom_marshalers.set(mono_type, marshaler.toJavaScript);
            js_to_cs_custom_marshalers.set(mono_type, marshaler.toManaged);
        }
    }
    return { cs_to_js_custom_marshalers, js_to_cs_custom_marshalers };
}

export function mono_wasm_register_custom_marshaller(factory_code: MonoString, mono_type: MonoType, js_handle_out: Int32Ptr, is_exception: Int32Ptr): MonoString {
    try {
        const closure: MarshalerHelpers = {
            mono_type,
            MONO, BINDING,
            get_arg, get_gc_handle, get_js_handle, get_signature_type, get_root_ref, get_arg_type,
            set_gc_handle, set_js_handle, set_root_ref, set_arg_type, get_extra_buffer,
            get_arg_b8, get_arg_u8, get_arg_i16, get_arg_i32, get_arg_i64, get_arg_f64, get_arg_date,
            set_arg_b8, set_arg_u8, set_arg_i16, set_arg_i32, set_arg_i64, set_arg_f64, set_arg_date,
        };

        const js_factory_code = conv_string(factory_code)!;
        assert(js_factory_code, "factory code must be provided");
        const type_name = _get_type_name(mono_type);
        const bound_js_function_name = "_covertor_" + type_name.replaceAll(".", "_");
        let body = `//# sourceURL=https://mono-wasm.invalid/${bound_js_function_name} \n`;
        body += ` const {${Object.keys(closure).toString()}} = closure;\n`;
        body += "return " + js_factory_code;
        //console.log("-------");
        //console.log(body);
        //console.log("-------");

        const factory = new Function("closure", body);
        const marshaller = factory(closure)();
        marshaller[marshaler_type_symbol] = mono_type;

        const js_handle = mono_wasm_get_js_handle(marshaller);
        Module.setValue(js_handle_out, <any>js_handle, "i32");
        return MonoStringNull;
    }
    catch (ex) {
        return wrap_error(is_exception, ex);
    }
}