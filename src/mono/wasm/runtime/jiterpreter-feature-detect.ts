import {
    WasmValtype, WasmOpcode, WasmSimdOpcode
} from "./jiterpreter-opcodes";
import {
    WasmBuilder, getRawCwrap
} from "./jiterpreter-support";

export let doJitCallBuilder : WasmBuilder | undefined = undefined;

export function compileDoJitCall () : WebAssembly.Module | undefined {
    doJitCallBuilder = new WasmBuilder(0);
    if (doJitCallBuilder.getExceptionTag() === undefined)
        return undefined;

    doJitCallBuilder.defineType("jit_call_cb", {
        "cb_data": WasmValtype.i32,
    }, WasmValtype.void, true);
    doJitCallBuilder.defineType("begin_catch", {
        "ptr": WasmValtype.i32,
    }, WasmValtype.void, true);
    doJitCallBuilder.defineType("end_catch", {
    }, WasmValtype.void, true);
    doJitCallBuilder.defineType("do_jit_call", {
        "unused": WasmValtype.i32,
        "cb_data": WasmValtype.i32,
        "thrown": WasmValtype.i32,
    }, WasmValtype.void, true);
    doJitCallBuilder.defineImportedFunction("i", "jit_call_cb", "jit_call_cb", true);
    doJitCallBuilder.defineImportedFunction("i", "begin_catch", "begin_catch", true, getRawCwrap("mono_jiterp_begin_catch"));
    doJitCallBuilder.defineImportedFunction("i", "end_catch", "end_catch", true, getRawCwrap("mono_jiterp_end_catch"));
    doJitCallBuilder.defineFunction({
        type: "do_jit_call",
        name: "do_jit_call_indirect",
        export: true,
        locals: {}
    }, () => {
        doJitCallBuilder = doJitCallBuilder!;
        doJitCallBuilder.block(WasmValtype.void, WasmOpcode.try_);
        doJitCallBuilder.local("cb_data");
        doJitCallBuilder.callImport("jit_call_cb");
        doJitCallBuilder.appendU8(WasmOpcode.catch_);
        doJitCallBuilder.appendULeb(doJitCallBuilder.getTypeIndex("__cpp_exception"));
        doJitCallBuilder.callImport("begin_catch");
        doJitCallBuilder.callImport("end_catch");
        doJitCallBuilder.local("thrown");
        doJitCallBuilder.i32_const(1);
        doJitCallBuilder.appendU8(WasmOpcode.i32_store);
        doJitCallBuilder.appendMemarg(0, 0);
        doJitCallBuilder.endBlock();
        doJitCallBuilder.appendU8(WasmOpcode.end);
    });
    // Magic number and version
    doJitCallBuilder.appendU32(0x6d736100);
    doJitCallBuilder.appendU32(1);
    doJitCallBuilder.generateTypeSection();
    doJitCallBuilder.emitImportsAndFunctions(false);
    const buffer = doJitCallBuilder.getArrayView();
    return new WebAssembly.Module(buffer);
}

export function compileSimdFeatureDetect () : WebAssembly.Module {
    const builder = new WasmBuilder(0);
    builder.defineType("test", {}, WasmValtype.void, true);
    builder.defineFunction({
        type: "test",
        name: "test",
        export: true,
        locals: {}
    }, () => {
        builder.i32_const(0);
        builder.appendSimd(WasmSimdOpcode.i32x4_splat);
        builder.appendU8(WasmOpcode.drop);
        builder.appendU8(WasmOpcode.end);
    });
    // Magic number and version
    builder.appendU32(0x6d736100);
    builder.appendU32(1);
    builder.generateTypeSection();
    builder.emitImportsAndFunctions(false);
    const buffer = builder.getArrayView();
    return new WebAssembly.Module(buffer);
}
