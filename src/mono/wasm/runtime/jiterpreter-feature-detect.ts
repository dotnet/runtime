import {
    WasmOpcode, WasmSimdOpcode
} from "./jiterpreter-opcodes";
import {
    WasmValtype, WasmBuilder,
} from "./jiterpreter-support";

export function compileDoJitCall () : WebAssembly.Module {
    const builder = new WasmBuilder(0);
    builder.defineType("jit_call_cb", {
        "cb_data": WasmValtype.i32,
    }, WasmValtype.void, true);
    builder.defineType("do_jit_call", {
        "unused": WasmValtype.i32,
        "cb_data": WasmValtype.i32,
        "thrown": WasmValtype.i32,
    }, WasmValtype.void, true);
    builder.defineImportedFunction("i", "jit_call_cb", "jit_call_cb", true);
    builder.defineFunction({
        type: "do_jit_call",
        name: "do_jit_call_indirect",
        export: true,
        locals: {}
    }, () => {
        builder.block(WasmValtype.void, WasmOpcode.try_);
        builder.local("cb_data");
        builder.callImport("jit_call_cb");
        builder.appendU8(WasmOpcode.catch_all);
        builder.local("thrown");
        builder.i32_const(1);
        builder.appendU8(WasmOpcode.i32_store);
        builder.appendMemarg(0, 0);
        builder.endBlock();
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
