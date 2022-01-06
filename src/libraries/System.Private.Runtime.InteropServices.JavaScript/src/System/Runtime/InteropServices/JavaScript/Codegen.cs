// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static unsafe class Codegen
    {
        public static readonly int PointerSize = sizeof(IntPtr);
        // HACK: Unless we align all the argument values in the heap by this amount,
        //  certain parameter types will be garbled when received by target C# functions
        public const int IndirectAddressAlignment = 8;

        private static readonly Dictionary<MarshalType, string> FastUnboxHandlers = new Dictionary<MarshalType, string> {
            { MarshalType.INT, "getI32(unboxBuffer)" },
            { MarshalType.POINTER, "getU32(unboxBuffer)" }, // FIXME: Is this right?
            { MarshalType.UINT32, "getU32(unboxBuffer)" },
            { MarshalType.FP32, "getF32(unboxBuffer)" },
            { MarshalType.FP64, "getF64(unboxBuffer)" },
            { MarshalType.BOOL, "getI32(unboxBuffer) !== 0" },
            { MarshalType.CHAR, "String.fromCharCode(getI32(unboxBuffer))" },
        };

        public abstract class BuilderStateBase {
            public MarshalString MarshalString;
            public StringBuilder Output = new StringBuilder();
            public HashSet<string> ClosureReferences = new HashSet<string>();
        }

        public class MarshalBuilderState : BuilderStateBase {
            public HashSet<(string, int)> TypeReferences = new HashSet<(string, int)>();
            public StringBuilder Phase2 = new StringBuilder();
            public Dictionary<string, object> Closure = new Dictionary<string, object>();
            public int ArgIndex, RootIndex, DirectOffset, IndirectOffset;

            public string ArgKey => $"arg{ArgIndex}";

            public MarshalBuilderState () {
                ClosureReferences = new HashSet<string> {
                    "_malloc",
                    "_error",
                };
            }
        }

        public class BoundMethodBuilderState : BuilderStateBase {
            public string? FriendlyName;
            public MethodInfo Method;

            public BoundMethodBuilderState (MethodInfo method) {
                Method = method;
                ClosureReferences = new HashSet<string> {
                    "_error",
                    "mono_wasm_new_root",
                    "_create_temp_frame",
                    "_get_args_root_buffer_for_method_call",
                    "_get_buffer_for_method_call",
                    "_handle_exception_for_call",
                    "_teardown_after_call",
                    "mono_wasm_try_unbox_primitive_and_get_type",
                    "_unbox_mono_obj_root_with_known_nonprimitive_type",
                    "invoke_method",
                    "getI32",
                    "getU32",
                    "getF32",
                    "getF64",
                };
            }
        }

        private static string ToJsBool (bool b) => b ? "true" : "false";

        public static void GenerateSignatureConverter (MarshalBuilderState state) {
            int length = state.MarshalString.ArgumentCount;
            var debugName = string.Concat("converter_", state.MarshalString.Key);
            var variadicName = string.Concat("varConverter_", state.MarshalString.Key);

            // First we generate the individual steps that pack each argument into the buffer and
            //  place pointers to each argument into the args list that is passed when invoking a method.
            var output = state.Output;
            for (int i = 0; i < length; i++) {
                state.ArgIndex = i;
                var ch = state.MarshalString[i];
                EmitMarshalStep(state, ch);
            }

            // Now we capture that list of steps so we can put stuff above it. Generating the list of
            //  steps produced valuable information like how large our buffer needs to be.
            var temp = output.ToString();
            output.Clear();

            // This special comment assigns a URL to this generated function in browser debuggers
            output.AppendLine($"//# sourceURL=https://mono-wasm.invalid/signature/{state.MarshalString.Key}");
            output.AppendLine("\"use strict\";");

            var alignmentMinusOne = IndirectAddressAlignment - 1;
            // HACK: We have to pad out both buffers to ensure that all addresses will have an alignment of 8
            // If we don't do this, passing values to C# functions can fail (typically for doubles)
            var directSize = (state.DirectOffset + alignmentMinusOne) / IndirectAddressAlignment * IndirectAddressAlignment;
            var indirectSize = (state.IndirectOffset + alignmentMinusOne) / IndirectAddressAlignment * IndirectAddressAlignment;
            var totalBufferSize = directSize + indirectSize + IndirectAddressAlignment;
            output.AppendLine($"// '{state.MarshalString.Signature}' {length} argument(s)");
            output.AppendLine($"// direct buffer {state.DirectOffset} byte(s), indirect {state.IndirectOffset} byte(s)");

            if (length > 0) {
                // Now we scan through all the closure references that were generated while emitting
                //  the marshal steps, and pull them out of the closure table into local variables in
                //  the scope of the outer function. This will make them visible to the two inner
                //  inner functions we're generating (which are the actual signature converter + its
                //  variadic wrapper), eliminating any need to do table lookups on every invocation.
                // FIXME: It's possible to end up with a cyclic dependency between converters this way

                // TODO: Sort this for consistent code
                foreach (var key in state.ClosureReferences)
                    output.AppendLine($"const {key} = get_api('{key}');");
                foreach (var tup in state.TypeReferences)
                    output.AppendLine($"const {tup.Item1} = get_type_converter({tup.Item2});");
            }

            output.AppendLine("");
            output.Append($"function {debugName} (buffer, rootBuffer, methodPtr");
            for (int i = 0; i < length; i++)
                output.Append($", arg{i}");
            output.AppendLine(") {");

            if (length > 0) {
                output.AppendLine("  if (!methodPtr) _error('no method provided');");
                if (state.RootIndex > 0)
                    state.Output.AppendLine($"  if (!rootBuffer) _error('no root buffer provided');");
                // When a signature converter is called it may be passed an existing buffer for reuse, but
                //  if not it will allocate one on the fly. The caller is responsible for freeing it.
                output.AppendLine($"  if (!buffer) buffer = _malloc({totalBufferSize});");
                // FIXME: While we're aligning the size of the direct buffer, it's possible 'buffer' itself is not
                //  properly aligned, which would mean indirectBuffer will also not be properly aligned.
                // In my testing emscripten's malloc always produces aligned addresses, but we may want to
                //  detect and handle this by shifting indirectBuffer forward to align it.
                output.AppendLine($"  const directBuffer = buffer, indirectBuffer = directBuffer + {directSize};");
                output.AppendLine(temp);

                // Some marshaling operations need to occur in two phases, so we append the second phase
                //  code right at the end before returning
                if (state.Phase2.Length > 0)
                    output.AppendLine(state.Phase2.ToString());

                output.AppendLine("  return buffer;");
            } else {
                output.AppendLine("  return 0;");
            }
            output.AppendLine("};");

            // Generate a small dispatcher function that will unpack an arguments array to pass
            //  the individual arguments to the signature converter. This is much slower than
            //  taking arguments directly so it is only available as a fallback
            output.AppendLine("");
            output.AppendLine($"function {variadicName} (buffer, rootBuffer, methodPtr, args) {{");
            output.AppendLine($"  if (args.length !== {length}) _error('Expected {length} argument(s)');");
            if (length > 0) {
                output.Append($"  return {debugName}(buffer, rootBuffer, methodPtr");
                for (int i = 0; i < length; i++)
                    output.Append($", args[{i}]");
                output.AppendLine(");");
            } else {
                output.Append("  return 0;");
            }
            output.AppendLine("};");

            var pMethod = state.MarshalString.Method?.MethodHandle.Value ?? IntPtr.Zero;
            var method = state.MarshalString.ContainsAuto
                ? pMethod.ToInt32().ToString()
                : "null";

            // At the end our wrapper function returns the two nested closures along with information
            //  on the signature they're for, so that the JS bindings layer can store everything away
            //  and do relevant setup (allocating the correct sized buffer, etc.)
            output.AppendLine("");
            output.AppendLine("return {");
            output.AppendLine($"  arg_count: {length}, ");
            output.AppendLine($"  args_marshal: '{state.MarshalString.Signature}', ");
            output.AppendLine($"  compiled_function: {debugName}, ");
            output.AppendLine($"  compiled_variadic_function: {variadicName}, ");
            output.AppendLine($"  contains_auto: {ToJsBool(state.MarshalString.ContainsAuto)}, ");
            output.AppendLine($"  is_result_definitely_unmarshaled: {ToJsBool(state.MarshalString.RawReturnValue)}, ");
            output.AppendLine($"  method: {method}, ");
            output.AppendLine($"  name: '{state.MarshalString.Key}', ");
            output.AppendLine($"  needs_root_buffer: {ToJsBool(state.RootIndex > 0)}, ");
            output.AppendLine($"  root_buffer_size: {state.RootIndex}, ");
            output.AppendLine($"  scratchBuffer: 0, ");
            output.AppendLine($"  scratchRootBuffer: null, ");
            output.AppendLine($"  size: {totalBufferSize}, ");
            output.AppendLine("};");
        }

        public static void EmitPrimitiveMarshalStep (MarshalBuilderState state, string setterName) {
            state.ClosureReferences.Add(setterName);
            state.ClosureReferences.Add("setU32");
            var offsetKey = $"offset{state.ArgIndex}";
            state.Output.AppendLine($"  let {offsetKey} = indirectBuffer + {state.IndirectOffset};");
            state.Output.AppendLine($"  {setterName}({offsetKey}, {state.ArgKey});");
            state.Output.AppendLine($"  setU32(directBuffer + {state.DirectOffset}, {offsetKey});");
            state.IndirectOffset += IndirectAddressAlignment;
            state.DirectOffset += PointerSize;
        }

        public static void EmitRawPointerMarshalStep (MarshalBuilderState state) {
            state.ClosureReferences.Add("setU32");
            state.Output.AppendLine($"  setU32(directBuffer + {state.DirectOffset}, {state.ArgKey});");
            state.DirectOffset += PointerSize;
        }

        public static void EmitManagedMarshalStep (MarshalBuilderState state, string? converter) {
            state.ClosureReferences.Add("setU32");

            var key = state.ArgKey;
            if (converter != null) {
                key = $"converted{state.ArgIndex}";
                // Converters can either be a bare function name or raw 'foo(x, ..., y)' JS, where we will replace the '...'
                var parenIndex = converter.IndexOf('(');
                if (parenIndex >= 0) {
                    state.ClosureReferences.Add(converter.Substring(0, parenIndex));
                    state.Output.AppendLine($"  const {key} = {converter.Replace("...", state.ArgKey)};");
                } else {
                    state.ClosureReferences.Add(converter);
                    state.Output.AppendLine($"  const {key} = {converter}({state.ArgKey});");
                }
            }

            state.Output.AppendLine($"  rootBuffer.set({state.RootIndex}, {key});");
            state.Output.AppendLine($"  setU32(directBuffer + {state.DirectOffset}, {key});");
            state.RootIndex += 1;
            state.DirectOffset += PointerSize;
        }

        private static void EmitCustomMarshalStep (MarshalBuilderState state, Type argType) {
            state.ClosureReferences.Add("setU32");
            var typePtr = argType.TypeHandle.Value;
            var converterKey = $"type{typePtr.ToInt32()}";
            state.TypeReferences.Add((converterKey, typePtr.ToInt32()));

            var callArgs = $"{state.ArgKey}, methodPtr, {state.ArgIndex}";
            state.Output.AppendLine($"  rootBuffer.set({state.RootIndex}, {converterKey}({callArgs}));");

            if (argType.IsValueType) {
                state.ClosureReferences.Add("mono_wasm_unbox_rooted");
                var unboxedKey = $"unboxed{state.ArgIndex}";
                // HACK: We need to do all these unboxes last after all the transform steps have run,
                //  because invoking a converter or creating a string instance could cause a GC and move
                //  the rooted object to a new location, invalidating the unbox_rooted return value.
                state.Phase2.AppendLine($"  const {unboxedKey} = mono_wasm_unbox_rooted(rootBuffer.get({state.RootIndex}));");
                state.Phase2.AppendLine($"  setU32(directBuffer + {state.DirectOffset}, {unboxedKey});");
            } else {
                // Note that even though we aren't unboxing, we still read the object address back from
                //  the root buffer, because the conversion steps may have caused a GC and moved the
                //  object after we initially created it.
                state.Phase2.AppendLine($"  setU32(directBuffer + {state.DirectOffset}, rootBuffer.get({state.RootIndex}));");
            }

            state.RootIndex += 1;
            state.DirectOffset += PointerSize;
        }

        public static void EmitMarshalStep (MarshalBuilderState state, ArgsMarshalCharacter ch) {
            // If this slot in the signature uses the Auto type ('a'), we need to select an
            //  appropriate type for the parameter based on the target method's type info
            if (ch == ArgsMarshalCharacter.Auto) {
                var method = state.MarshalString.Method;
                if (method == null)
                    // This either means no method was provided, or we failed to resolve a method
                    //  from the method handle we were provided (this can happen if it's generic)
                    throw new Exception("No method provided when compiling converter");
                var parms = method.GetParameters();
                if (state.ArgIndex >= parms.Length)
                    throw new Exception($"Too many signature characters ({state.MarshalString.ArgumentCount}) for method ({parms.Length} args)");

                var parm = parms[state.ArgIndex];
                var pName = string.IsNullOrEmpty(parm.Name)
                    ? $"#{state.ArgIndex}"
                    : parm.Name;
                var argType = parm.ParameterType;
                var autoMarshalType = Runtime.GetMarshalTypeFromType(argType);

                state.Output.AppendLine($"// #{state.ArgIndex} Auto {argType} {pName} -> {autoMarshalType}");

                switch (autoMarshalType) {
                    // For basic types, we can just select an appropriate MarshalType for them, and then
                    //  use the corresponding signature character as a replacement for the one we're missing
                    default:
                        ch = (ArgsMarshalCharacter)(int)Runtime.GetCallSignatureCharacterForMarshalType(autoMarshalType, null);
                        break;
                    // If the marshal type selector produced bare ValueType or Object, it needs custom marshaling
                    case MarshalType.VT:
                        EmitCustomMarshalStep(state, argType);
                        return;
                    case MarshalType.OBJECT:
                        // Though if it's just bare 'object', we cannot identify the marshaler at compile time here,
                        //  and we need to let the regular js_to_mono_obj path below run to do it at run time.
                        if (argType != typeof(object)) {
                            EmitCustomMarshalStep(state, argType);
                            return;
                        } else {
                            ch = ArgsMarshalCharacter.JSObj;
                            break;
                        }
                }
            } else {
                state.Output.AppendLine($"// #{state.ArgIndex} {ch}");
            }

            switch (ch) {
                case ArgsMarshalCharacter.Int32:
                    EmitPrimitiveMarshalStep(state, "setI32");
                    return;
                case ArgsMarshalCharacter.Int64:
                    EmitPrimitiveMarshalStep(state, "setI64");
                    return;
                case ArgsMarshalCharacter.Float32:
                    EmitPrimitiveMarshalStep(state, "setF32");
                    return;
                case ArgsMarshalCharacter.Float64:
                    EmitPrimitiveMarshalStep(state, "setF64");
                    return;
                case ArgsMarshalCharacter.ByteSpan:
                    EmitPrimitiveMarshalStep(state, "_setSpan");
                    return;
                case ArgsMarshalCharacter.MONOObj:
                    EmitRawPointerMarshalStep(state);
                    return;
                case ArgsMarshalCharacter.String:
                    EmitManagedMarshalStep(state, "js_string_to_mono_string");
                    return;
                case ArgsMarshalCharacter.InternedString:
                    EmitManagedMarshalStep(state, "js_string_to_mono_string_interned");
                    return;
                case ArgsMarshalCharacter.Int32Enum:
                    state.Output.AppendLine($"  if (typeof({state.ArgKey}) !== 'number') _error(`Expected numeric value for enum argument, got '${{{state.ArgKey}}}'`);");
                    EmitPrimitiveMarshalStep(state, "setI32");
                    return;
                case ArgsMarshalCharacter.JSObj:
                    EmitManagedMarshalStep(state, "_js_to_mono_obj(false, ...)");
                    return;
                case ArgsMarshalCharacter.Uri:
                    EmitManagedMarshalStep(state, "_js_to_mono_uri(false, ...)");
                    return;
                case ArgsMarshalCharacter.Auto:
                    state.Output.AppendLine($"  _error('Automatic type selection failed');");
                    return;
                default:
                    throw new NotImplementedException(ch.ToString());
            }
        }

        private static void GenerateFastUnboxCase (BoundMethodBuilderState state, MarshalType type, string? expression) {
            var output = state.Output;
            output.AppendLine($"    case {(int)type}:");
            output.AppendLine($"        return {expression};");
        }

        private static void GenerateFastUnboxBlock (BoundMethodBuilderState state) {
            var output = state.Output;
            var methodReturnType = Runtime.GetMarshalTypeFromType(state.Method.ReturnType);
            bool hasPrimitiveType = FastUnboxHandlers.TryGetValue(methodReturnType, out string? fastHandler);
            // For the common scenario where the return type is a primitive, we want to try and unbox it directly
            //  into our existing heap allocation and then read it out of the heap. Doing this all in one operation
            //  means that we only need to enter a gc safe region twice (instead of 3+ times with the normal,
            //  slower check-type-and-then-unbox flow which has extra checks since unbox verifies the type).
            if (!hasPrimitiveType) {
                output.AppendLine("    if (resultRoot.value === 0)");
                output.AppendLine("      return undefined;");
            }
            output.AppendLine( "    let resultType = mono_wasm_try_unbox_primitive_and_get_type(resultRoot.value, unboxBuffer, unboxBufferSize);");
            output.AppendLine( "    switch (resultType) {");
            // If we know the return type of this method and it's a primitive, we only need to generate the unbox handler for that type
            if (hasPrimitiveType) {
                GenerateFastUnboxCase(state, methodReturnType, fastHandler);
                // This default case should never be hit, but the runtime is returning a boxed object so it's possible if something horrible happens
                output.AppendLine( "    default:");
                output.AppendLine($"        throw new Error('expected method return value to be of type {methodReturnType} but it was ' + resultType);");
            } else {
                // The return type is something we can't fast-unbox or is unknown (i.e. object)
                foreach (var kvp in FastUnboxHandlers)
                    GenerateFastUnboxCase(state, kvp.Key, kvp.Value);
                output.AppendLine( "    default:");
                output.AppendLine( "        return _unbox_mono_obj_root_with_known_nonprimitive_type(resultRoot, resultType, unboxBuffer);");
            }
            output.AppendLine( "    }");
        }

        public static void GenerateBoundMethod (BoundMethodBuilderState state) {
            // input arguments:
            // get_api, token

            int length = state.MarshalString.ArgumentCount;
            var handle = state.Method.MethodHandle.Value;
            var name = state.FriendlyName ?? $"clr_{handle.ToInt32()}";
            var output = state.Output;

            // This special comment assigns a URL to this generated function in browser debuggers
            output.AppendLine($"//# sourceURL=https://mono-wasm.invalid/bound_method/{handle.ToInt32()}");
            output.AppendLine("\"use strict\";");
            output.AppendLine($"//{state.Method?.DeclaringType?.FullName}::{state.Method?.Name}");

            // Unpack various closure values into locals in the outer function that returns the actual
            //  bound method, so that the property lookup doesn't have to occur on every call
            output.AppendLine("const method = token.method;");
            output.AppendLine("const converter = token.converter;");
            output.AppendLine($"const converter_{state.MarshalString.Key} = converter.compiled_function;");
            output.AppendLine("const unboxBuffer = token.unboxBuffer;");
            output.AppendLine("const unboxBufferSize = token.unboxBufferSize;");
            // get_api here will also ensure that every function we reference is available and do
            //  the check now at construction time instead of later when the bound method is called
            foreach (var key in state.ClosureReferences)
                output.AppendLine($"const {key} = get_api('{key}');");

            output.Append($"function {name} (");
            for (int i = 0; i < length; i++) {
                if (i < (length - 1))
                    output.Append($"arg{i}, ");
                else
                    output.AppendLine($"arg{i}) {{");
            }
            if (length == 0)
                output.AppendLine(") {");

            output.AppendLine("  _create_temp_frame();");
            output.AppendLine("  let resultRoot = token.scratchResultRoot;");
            output.AppendLine("  let exceptionRoot = token.scratchExceptionRoot;");
            output.AppendLine("  token.scratchResultRoot = null;");
            output.AppendLine("  token.scratchExceptionRoot = null;");
            output.AppendLine("  if (resultRoot === null)");
            output.AppendLine("    resultRoot = mono_wasm_new_root();");
            output.AppendLine("  if (exceptionRoot === null)");
            output.AppendLine("    exceptionRoot = mono_wasm_new_root();");
            output.AppendLine();

            output.AppendLine( "  let argsRootBuffer = _get_args_root_buffer_for_method_call(converter, token);");
            output.AppendLine( "  let scratchBuffer = _get_buffer_for_method_call(converter, token);");
            output.AppendLine( "  let buffer = 0;");
            output.AppendLine( "  try {");
            output.AppendLine($"    buffer = converter_{state.MarshalString.Key}(");
            output.AppendLine( "      scratchBuffer, argsRootBuffer, method,");
            for (int i = 0; i < length; i++) {
                if (i < (length - 1))
                    output.AppendLine($"      arg{i},");
                else
                    output.AppendLine($"      arg{i}");
            }
            output.AppendLine("    );");
            output.AppendLine();

            output.AppendLine("    resultRoot.value = invoke_method(method, 0, buffer, exceptionRoot.get_address());");
            output.AppendLine("    _handle_exception_for_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);");
            output.AppendLine();

            if (state.MarshalString.RawReturnValue)
                output.AppendLine("    return resultRoot.value;");
            else if ((state.Method?.ReturnType ?? typeof(void)) == typeof(void))
                output.AppendLine("    return;");
            else
                GenerateFastUnboxBlock(state);

            output.AppendLine("  } finally {");
            output.AppendLine("    _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);");
            output.AppendLine("  }");
            output.AppendLine("};");
            output.AppendLine();
            output.AppendLine($"return {name};");
        }
    }
}
