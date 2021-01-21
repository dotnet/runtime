import { Module, MONO, BINDING, runtimeHelpers } from "./modules";
import cwraps from "./cwraps";
import { WasmRoot } from "./roots";
import {
    MonoMethod, MonoObject, MonoObjectNull,
    MonoMethodNull, MonoType, MonoTypeNull,
    MarshalType, MarshalTypeRecord, CustomMarshalerInfo
} from "./types";
import {
    ArgsMarshalString, mono_bind_method, _create_named_function,
    _get_type_aqn, _get_type_name,
    get_method_signature_info, bindings_named_closures,
    TypeConverter, SignatureConverter
} from "./method-binding";
import {
    temp_malloc, _create_temp_frame, _release_temp_frame,
    getI8, getI16, getI32, getI64,
    getU8, getU16, getU32,
    getF32, getF64,
    setI8, setI16, setI32, setI64,
    setU8, setU16, setU32,
    setF32, setF64,
} from "./memory";
import { _unbox_ref_type_root_as_js_object } from "./cs-to-js";
import { js_to_mono_obj } from "./js-to-cs";
import cswraps from "./corebindings";

const _custom_marshaler_info_cache = new Map<MonoType, CustomMarshalerInfo | null>();
const _struct_unboxer_cache = new Map<MonoType, Function | null>();
const _automatic_converter_table = new Map<MonoType, Function | null>();
export const _custom_marshaler_name_table : { [key: string] : string } = {};
const _temp_unbox_buffer_cache = new Map<MonoType, VoidPtr>();
let _has_logged_custom_marshaler_table = false;

function extract_js_obj_root_with_converter_impl (root : WasmRoot<MonoObject>, typePtr : MonoType, unbox_buffer : VoidPtr, optional: boolean) {
    if (root.value === MonoObjectNull)
        return null;

    const converter = _get_struct_unboxer_for_type (typePtr);

    if (converter) {
        let buffer_is_temporary = false;
        if (!unbox_buffer) {
            buffer_is_temporary = true;
            if (_temp_unbox_buffer_cache.has(typePtr)) {
                unbox_buffer = <VoidPtr>_temp_unbox_buffer_cache.get(typePtr);
                _temp_unbox_buffer_cache.delete(typePtr);
            } else {
                unbox_buffer = Module._malloc(runtimeHelpers._unbox_buffer_size);
            }
            // TODO: Verify the MarshalType return value?
            cwraps.mono_wasm_try_unbox_primitive_and_get_type(root.value, unbox_buffer, runtimeHelpers._unbox_buffer_size);
        }
        const objectSize = getI32(<number><any>unbox_buffer + 4);
        const pUnboxedData = <number><any>unbox_buffer + 8;
        _create_temp_frame();
        try {
            // Reftypes have no size because they cannot be copied into the unbox buffer,
            //  so we pass their managed address directly to the converter
            if (objectSize <= 0)
                return converter(root.value);
            else
                return converter(pUnboxedData);
        } finally {
            _release_temp_frame();
            if (buffer_is_temporary) {
                if (_temp_unbox_buffer_cache.has(typePtr))
                    Module._free(unbox_buffer);
                else
                    _temp_unbox_buffer_cache.set(typePtr, unbox_buffer);
            }
        }
    } else if (optional)
        return _unbox_ref_type_root_as_js_object (root);
    else
        throw new Error (`No CustomJavaScriptMarshaler found for type ${_get_type_name(typePtr)}`);
}

export function extract_js_obj_root_with_converter (root : WasmRoot<MonoObject>, typePtr : MonoType, unbox_buffer : VoidPtr) : any {
    return extract_js_obj_root_with_converter_impl(root, typePtr, unbox_buffer, false);
}

export function extract_js_obj_root_with_possible_converter (root : WasmRoot<MonoObject>, typePtr : MonoType, unbox_buffer : VoidPtr) : any {
    return extract_js_obj_root_with_converter_impl(root, typePtr, unbox_buffer, true);
}

export function box_js_obj_with_converter (js_obj : any, typePtr : MonoType) : MonoObject {
    if ((js_obj === null) || (js_obj === undefined))
        return MonoObjectNull;

    if (!typePtr)
        throw new Error("No type pointer provided");

    const converter = _pick_automatic_converter_for_type(typePtr);
    if (!converter)
        throw new Error (`No CustomJavaScriptMarshaler found for type ${_get_type_name(typePtr)}`);

    _create_temp_frame();
    try {
        return <MonoObject>converter(js_obj);
    } finally {
        _release_temp_frame();
    }
}

function _create_interchange_closure (typePtr : MonoType) : any {
    return {
        // Put binding/mono API namespaces in the closure so that interchange filters can use them
        Module,
        MONO,
        BINDING,
        // RuntimeTypeHandle for the type so that type-oriented APIs can be used easily
        typePtr,
        // Special interchange-only API for temporary allocations
        alloca: temp_malloc,
        // Memory accessors
        getI8, getI16, getI32, getI64,
        getU8, getU16, getU32,
        getF32, getF64,
        setI8, setI16, setI32, setI64,
        setU8, setU16, setU32,
        setF32, setF64,
    };
}

function _compile_interchange_to_js (typePtr : MonoType, boundConverter : Function, js : string | undefined, info : CustomMarshalerInfo) : Function {
    if (!js)
        return boundConverter;

    const closure = _create_interchange_closure(typePtr);
    const hasScratchBuffer = (info.scratchBufferSize || 0) > 0;

    let converterKey = boundConverter.name || "boundConverter";
    if (converterKey in closure)
        converterKey += "_";
    closure[converterKey] = boundConverter;

    const filterParams = hasScratchBuffer
        ? ["buffer", "bufferSize"]
        : ["value"];

    const filterName = "interchange_to_js_filter_for_type" + typePtr;

    const filterExpression = _create_named_function(
        filterName, filterParams, js, closure
    );
    closure[filterName] = filterExpression;

    let bodyJs : string;
    if (hasScratchBuffer) {
        bodyJs = `let buffer = alloca(${info.scratchBufferSize});\r\n` +
            `${converterKey}(value, [buffer, ${info.scratchBufferSize}]);\r\n` +
            `let filteredValue = ${filterName}(buffer, ${info.scratchBufferSize});\r\n` +
            "return filteredValue;";
    } else {
        bodyJs = `let convertedValue = ${converterKey}(value), filteredValue = ${filterName}(convertedValue);\r\n` +
            "return filteredValue;";
    }
    const functionName = "interchange_to_js_for_type" + typePtr;
    const result = _create_named_function(
        functionName, ["value"], bodyJs, closure
    );

    return result;
}

function _get_custom_marshaler_info_for_type (typePtr : MonoType) {
    if (!typePtr)
        return null;
    if (!_custom_marshaler_name_table)
        return null;

    let result;
    if (!_custom_marshaler_info_cache.has (typePtr)) {
        const aqn = _get_type_aqn (typePtr);
        if (!aqn.startsWith("System.Object, System.Private.CoreLib, ")) {
            let marshalerAQN = _custom_marshaler_name_table[aqn];
            if (!marshalerAQN) {
                for (const k in _custom_marshaler_name_table) {
                    // Perform a loose match against the assembly-qualified type names,
                    //  because in some cases it is not possible or convenient to
                    //  include the full string (i.e. version, culture, etc)
                    const isMatch = k.startsWith(aqn) || aqn.startsWith(k);
                    if (isMatch) {
                        marshalerAQN = _custom_marshaler_name_table[k];
                        break;
                    }
                }
            }

            if (!marshalerAQN) {
                if (!_has_logged_custom_marshaler_table) {
                    _has_logged_custom_marshaler_table = true;
                    console.log(`WARNING: Type "${aqn}" has no registered custom marshaler. A dump of the marshaler table follows:`);
                    for (const k in _custom_marshaler_name_table)
                        console.log(`  ${k}: ${_custom_marshaler_name_table[k]}`);
                }
                _custom_marshaler_info_cache.set(typePtr, null);
                return null;
            }
            const json = cswraps.get_custom_marshaler_info (typePtr, marshalerAQN);
            result = <CustomMarshalerInfo>JSON.parse(json);
            if (!result)
                throw new Error (`Configured custom marshaler for ${aqn} could not be loaded: ${marshalerAQN}`);
        } else {
            result = null;
        }

        _custom_marshaler_info_cache.set (typePtr, result);
    } else {
        result = _custom_marshaler_info_cache.get (typePtr);
    }

    return result;
}

function _get_struct_unboxer_for_type (typePtr : MonoType) {
    if (!typePtr)
        throw new Error("no type");

    if (!_struct_unboxer_cache.has (typePtr)) {
        const info = _get_custom_marshaler_info_for_type (typePtr);
        if (!info) {
            _struct_unboxer_cache.set (typePtr, null);
            return null;
        }

        if (info.error)
            console.error(`Error while configuring automatic converter for type ${_get_type_name(typePtr)}: ${info.error}`);

        const interchangeToJs = info.interchangeToJs;

        const convMethod = info.outputPtr;
        if (!convMethod) {
            if (info.typePtr)
                console.error(`Automatic converter for type ${_get_type_name(typePtr)} has no suitable ToJavaScript method`);
            // We explicitly store null in the cache so that lookups are not performed again for this type
            _struct_unboxer_cache.set (typePtr, null);
        } else {
            const typeName = _get_type_name(typePtr);
            const signature = (info.scratchBufferSize || 0) > 0
                ? "mb"
                : "m";
            const boundConverter = mono_bind_method (
                convMethod, null, signature, typeName + "$ToJavaScript"
            );

            _struct_unboxer_cache.set (typePtr, _compile_interchange_to_js (typePtr, boundConverter, interchangeToJs, info));
        }
    }

    return _struct_unboxer_cache.get (typePtr);
}

function _compile_js_to_interchange (typePtr : MonoType, boundConverter : Function, js : string | undefined, info : CustomMarshalerInfo) : Function {
    if (!js)
        return boundConverter;

    const closure = _create_interchange_closure(typePtr);
    const hasScratchBuffer = (info.scratchBufferSize || 0) > 0;

    let converterKey = boundConverter.name || "boundConverter";
    if (converterKey in closure)
        converterKey += "_";
    closure[converterKey] = boundConverter;

    const filterParams = hasScratchBuffer
        ? ["value", "buffer", "bufferSize"]
        : ["value"];

    const filterName = "js_to_interchange_filter_for_type" + typePtr;
    const filterExpression = _create_named_function(
        filterName, filterParams, js, closure
    );

    closure[filterName] = filterExpression;
    const functionName = "js_to_interchange_for_type" + typePtr;

    let bodyJs : string;
    if (hasScratchBuffer) {
        bodyJs = `let buffer = alloca(${info.scratchBufferSize});\r\n` +
            `${filterName}(value, buffer, ${info.scratchBufferSize});\r\n` +
            `let span = [buffer, ${info.scratchBufferSize}];\r\n` +
            `let convertedResult = ${converterKey}(span, method, parmIdx);\r\n` +
            "return convertedResult;";
    } else {
        bodyJs = `let filteredValue = ${filterName}(value);\r\n` +
            `let convertedResult = ${converterKey}(filteredValue, method, parmIdx);\r\n` +
            "return convertedResult;";
    }

    const result = _create_named_function(
        functionName,
        ["value", "method", "parmIdx"], bodyJs, closure
    );

    return result;
}

export function _pick_automatic_converter_for_type (typePtr : MonoType) : Function | null {
    if (!typePtr)
        throw new Error("typePtr is null or undefined");

    if (!_automatic_converter_table.has(typePtr)) {
        let info = _get_custom_marshaler_info_for_type(typePtr);
        // HACK
        if (!info)
            info = <CustomMarshalerInfo>{};
        if (info.error)
            console.error(`Error while configuring automatic converter for type ${_get_type_name(typePtr)}: ${info.error}`);

        const jsToInterchange = info.jsToInterchange;

        const convMethod = info.inputPtr;
        if (!convMethod) {
            if (info.typePtr)
                console.error(`Automatic converter for type ${_get_type_name(typePtr)} has no suitable FromJavaScript method`);
            _automatic_converter_table.set(typePtr, null);
            return null;
        }

        // FIXME
        const sigInfo = get_method_signature_info(MonoTypeNull, convMethod);
        if (sigInfo.parameters.length < 1)
            throw new Error("Expected at least one parameter");
        // Return unboxed so it can go directly into the arguments list
        const signature = sigInfo.parameters[0].signatureChar + "!";
        const methodName = _get_type_name(typePtr) + "$FromJavaScript";
        const boundConverter = mono_bind_method(
            convMethod, null, <ArgsMarshalString>signature, methodName
        );

        const result = _compile_js_to_interchange(typePtr, boundConverter, jsToInterchange, info);

        _automatic_converter_table.set(typePtr, result);
        bindings_named_closures.set(`type${typePtr}`, result);
    }

    return _automatic_converter_table.get(typePtr) || null;
}

export function _pick_automatic_converter (methodPtr : MonoMethod, args_marshal : ArgsMarshalString, paramRecord : MarshalTypeRecord) : TypeConverter {
    const needs_unbox = (paramRecord.marshalType === MarshalType.VT);

    if (
        (paramRecord.marshalType === MarshalType.VT) ||
        (paramRecord.marshalType === MarshalType.OBJECT)
    ) {
        const res = _pick_automatic_converter_for_type (paramRecord.typePtr);
        if (res) {
            return {
                convert: res,
                needs_root: !needs_unbox,
                needs_unbox
            };
        }
        if (needs_unbox)
            throw new Error(`found no automatic converter for type ${_get_type_name(paramRecord.typePtr)}`);
    }

    return {
        convert: js_to_mono_obj,
        needs_root: !needs_unbox,
        needs_unbox
    };
}