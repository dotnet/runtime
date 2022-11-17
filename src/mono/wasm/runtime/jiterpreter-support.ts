// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { NativePointer, VoidPtr } from "./types/emscripten";
import { Module } from "./imports";
import { WasmOpcode } from "./jiterpreter-opcodes";
import cwraps from "./cwraps";

export const maxFailures = 4;

// uint16
export declare interface MintOpcodePtr extends NativePointer {
    __brand: "MintOpcodePtr"
}

export class WasmBuilder {
    stack: Array<BlobBuilder>;
    stackSize!: number;
    inSection!: boolean;
    inFunction!: boolean;
    locals = new Map<string, [WasmValtype, number]>();
    functionTypeCount!: number;
    functionTypes!: { [name: string] : [number, { [name: string]: WasmValtype }, WasmValtype, string] };
    functionTypesByShape!: { [shape: string] : number };
    functionTypesByIndex: Array<any> = [];
    importedFunctionCount!: number;
    importedFunctions!: { [name: string] : [number, number, string] };
    importsToEmit!: Array<[string, string, number, number]>;
    argumentCount!: number;
    activeBlocks!: number;
    base!: MintOpcodePtr;
    traceBuf: Array<string> = [];
    branchTargets = new Set<MintOpcodePtr>();
    options!: JiterpreterOptions;

    constructor () {
        this.stack = [new BlobBuilder()];
        this.clear();
    }

    clear () {
        this.options = getOptions();
        this.stackSize = 1;
        this.inSection = false;
        this.inFunction = false;
        this.locals.clear();
        this.functionTypeCount = 0;
        this.functionTypes = {};
        this.functionTypesByShape = {};
        this.functionTypesByIndex.length = 0;
        this.importedFunctionCount = 0;
        this.importedFunctions = {};
        this.importsToEmit = [];
        this.argumentCount = 0;
        this.current.clear();
        this.traceBuf.length = 0;
        this.branchTargets.clear();
        this.activeBlocks = 0;
    }

    push () {
        this.stackSize++;
        if (this.stackSize >= this.stack.length)
            this.stack.push(new BlobBuilder());
        this.current.clear();
    }

    pop () {
        if (this.stackSize <= 1)
            throw new Error("Stack empty");

        const current = this.current;
        this.stackSize--;

        this.appendULeb(current.size);
        const av = current.getArrayView();
        this.appendBytes(av);
    }

    get bytesGeneratedSoFar () {
        return this.stack[0].size;
    }

    get current() {
        return this.stack[this.stackSize - 1];
    }

    get size() {
        return this.current.size;
    }

    appendU8 (value: number | WasmOpcode) {
        if ((value != value >>> 0) || (value > 255))
            throw new Error(`Byte out of range: ${value}`);
        return this.current.appendU8(value);
    }

    appendU32 (value: number) {
        return this.current.appendU32(value);
    }

    appendF32 (value: number) {
        return this.current.appendF32(value);
    }

    appendF64 (value: number) {
        return this.current.appendF64(value);
    }

    appendULeb (value: number | MintOpcodePtr) {
        return this.current.appendULeb(<any>value);
    }

    appendLeb (value: number) {
        return this.current.appendLeb(value);
    }

    appendLebRef (sourceAddress: VoidPtr, signed: boolean) {
        return this.current.appendLebRef(sourceAddress, signed);
    }

    appendBytes (bytes: Uint8Array) {
        return this.current.appendBytes(bytes);
    }

    appendName (text: string) {
        return this.current.appendName(text);
    }

    ret (ip: MintOpcodePtr) {
        this.ip_const(ip);
        this.appendU8(WasmOpcode.return_);
    }

    i32_const (value: number) {
        this.appendU8(WasmOpcode.i32_const);
        this.appendLeb(value);
    }

    ip_const (value: MintOpcodePtr, highBit?: boolean) {
        this.appendU8(WasmOpcode.i32_const);
        let relativeValue = <any>value - <any>this.base;
        if (highBit) {
            // it is impossible to do this in JS as far as i can tell
            // relativeValue |= 0x80000000;
            relativeValue += 0xF000000;
        }
        this.appendLeb(relativeValue);
    }

    i52_const (value: number) {
        this.appendU8(WasmOpcode.i64_const);
        this.appendLeb(value);
    }

    defineType (name: string, parameters: { [name: string]: WasmValtype }, returnType: WasmValtype) {
        if (this.functionTypes[name])
            throw new Error(`Function type ${name} already defined`);

        let index: number;
        let shape = "";
        for (const k in parameters)
            shape += parameters[k] + ",";
        shape += returnType;
        index = this.functionTypesByShape[shape];

        if (!index) {
            index = this.functionTypeCount++;
            this.functionTypesByShape[shape] = index;
            this.functionTypesByIndex[index] = [parameters, returnType];
        }

        this.functionTypes[name] = [
            index, parameters, returnType, `(${JSON.stringify(parameters)}) -> ${returnType}`
        ];
        return index;
    }

    generateTypeSection () {
        this.beginSection(1);
        this.appendULeb(this.functionTypeCount);
        /*
        if (trace > 1)
            console.log(`Generated ${this.functionTypeCount} wasm type(s) from ${Object.keys(this.functionTypes).length} named function types`);
        */
        for (let i = 0; i < this.functionTypesByIndex.length; i++) {
            const parameters = this.functionTypesByIndex[i][0];
            const returnType = this.functionTypesByIndex[i][1];
            this.appendU8(0x60);
            // Parameters
            this.appendULeb(Object.keys(parameters).length);
            for (const k in parameters)
                this.appendU8(parameters[k]);
            // Return type(s)
            if (returnType !== WasmValtype.void) {
                this.appendULeb(1);
                this.appendU8(returnType);
            } else
                this.appendULeb(0);
        }
        this.endSection();
    }

    generateImportSection () {
        // Import section
        this.beginSection(2);
        this.appendULeb(1 + this.importsToEmit.length);

        for (let i = 0; i < this.importsToEmit.length; i++) {
            const tup = this.importsToEmit[i];
            this.appendName(tup[0]);
            this.appendName(tup[1]);
            this.appendU8(tup[2]);
            this.appendULeb(tup[3]);
        }

        this.appendName("i");
        this.appendName("h");
        // memtype (limits = { min=0x01, max=infinity })
        this.appendU8(0x02);
        this.appendU8(0x00);
        // Minimum size is in 64k pages, not bytes
        this.appendULeb(0x01);
    }

    defineImportedFunction (
        module: string, name: string, functionTypeName: string,
        wasmName?: string
    ) {
        const index = this.importedFunctionCount++;
        const type = this.functionTypes[functionTypeName];
        if (!type)
            throw new Error("No function type named " + functionTypeName);
        const typeIndex = type[0];
        this.importedFunctions[name] = [
            index, typeIndex, type[3]
        ];
        this.importsToEmit.push([module, wasmName || name, 0, typeIndex]);
        return index;
    }

    callImport (name: string) {
        const func = this.importedFunctions[name];
        if (!func)
            throw new Error("No imported function named " + name);
        this.appendU8(WasmOpcode.call);
        this.appendULeb(func[0]);
    }

    beginSection (type: number) {
        if (this.inSection)
            this.pop();
        this.appendU8(type);
        this.push();
        this.inSection = true;
    }

    endSection () {
        if (!this.inSection)
            throw new Error("Not in section");
        if (this.inFunction)
            this.endFunction();
        this.pop();
        this.inSection = false;
    }

    beginFunction (
        type: string,
        locals?: {[name: string]: WasmValtype}
    ) {
        if (this.inFunction)
            this.endFunction();
        this.push();

        const signature = this.functionTypes[type];
        this.locals.clear();
        this.branchTargets.clear();
        let counts: any = {};
        const tk = [WasmValtype.i32, WasmValtype.i64, WasmValtype.f32, WasmValtype.f64];

        const assignParameterIndices = (parms: {[name: string] : WasmValtype}) => {
            let result = 0;
            for (const k in parms) {
                const parm = parms[k];
                this.locals.set(k, [parm, result]);
                // console.log(`parm ${k} -> ${result}`);
                result++;
            }
            return result;
        };

        let localGroupCount = 0;

        // We first assign the parameters local indices and then
        //  we assign the named locals indices, because parameters
        //  come first in the local space. Imagine if parameters
        //  had their own opcode and weren't mutable??????
        const assignLocalIndices = (locals: {[name: string] : WasmValtype}, base: number) => {
            Object.assign(counts, {
                [WasmValtype.i32]: 0,
                [WasmValtype.i64]: 0,
                [WasmValtype.f32]: 0,
                [WasmValtype.f64]: 0,
            });
            for (const k in locals) {
                const ty = locals[k];
                if (counts[ty] <= 0)
                    localGroupCount++;
                counts[ty]++;
            }

            const offi32 = 0,
                offi64 = counts[WasmValtype.i32],
                offf32 = offi64 + counts[WasmValtype.i64],
                offf64 = offf32 + counts[WasmValtype.f32];
            Object.assign(counts,{
                [WasmValtype.i32]: 0,
                [WasmValtype.i64]: 0,
                [WasmValtype.f32]: 0,
                [WasmValtype.f64]: 0,
            });
            for (const k in locals) {
                const ty = locals[k];
                let idx = 0;
                switch (ty) {
                    case WasmValtype.i32:
                        idx = (counts[ty]++) + offi32 + base;
                        this.locals.set(k, [ty, idx]);
                        break;
                    case WasmValtype.i64:
                        idx = (counts[ty]++) + offi64 + base;
                        this.locals.set(k, [ty, idx]);
                        break;
                    case WasmValtype.f32:
                        idx = (counts[ty]++) + offf32 + base;
                        this.locals.set(k, [ty, idx]);
                        break;
                    case WasmValtype.f64:
                        idx = (counts[ty]++) + offf64 + base;
                        this.locals.set(k, [ty, idx]);
                        break;
                }
                // console.log(`local ${k} ${locals[k]} -> ${idx}`);
            }
        };

        // Assign indices for the parameter list from the function signature
        const localBaseIndex = assignParameterIndices(signature[1]);
        if (locals)
            // Now if we have any locals, assign indices for those
            assignLocalIndices(locals, localBaseIndex);
        else
            // Otherwise erase the counts table from the parameter assignment
            counts = {};

        // Write the number of types and then write a count for each type
        this.appendULeb(localGroupCount);
        for (let i = 0; i < tk.length; i++) {
            const k = tk[i];
            const c = counts[k];
            if (!c)
                continue;
            // console.log(`${k} x${c}`);
            this.appendULeb(c);
            this.appendU8(<any>k);
        }

        this.inFunction = true;
    }

    endFunction () {
        if (!this.inFunction)
            throw new Error("Not in function");
        if (this.activeBlocks > 0)
            throw new Error(`${this.activeBlocks} unclosed block(s) at end of function`);
        this.pop();
        this.inFunction = false;
    }

    block (type?: WasmValtype, opcode?: WasmOpcode) {
        const result = this.appendU8(opcode || WasmOpcode.block);
        if (type)
            this.appendU8(type);
        else
            this.appendU8(WasmValtype.void);
        this.activeBlocks++;
        return result;
    }

    endBlock () {
        if (this.activeBlocks <= 0)
            throw new Error("No blocks active");
        this.activeBlocks--;
        this.appendU8(WasmOpcode.end);
    }

    arg (name: string | number, opcode?: WasmOpcode) {
        const index = typeof(name) === "string"
            ? (this.locals.has(name) ? this.locals.get(name)![1] : undefined)
            : name;
        if (typeof (index) !== "number")
            throw new Error("No local named " + name);
        if (opcode)
            this.appendU8(opcode);
        this.appendULeb(index);
    }

    local (name: string | number, opcode?: WasmOpcode) {
        const index = typeof(name) === "string"
            ? (this.locals.has(name) ? this.locals.get(name)![1] : undefined)
            : name + this.argumentCount;
        if (typeof (index) !== "number")
            throw new Error("No local named " + name);
        if (opcode)
            this.appendU8(opcode);
        else
            this.appendU8(WasmOpcode.get_local);
        this.appendULeb(index);
    }

    appendMemarg (offset: number, alignPower: number) {
        this.appendULeb(alignPower);
        this.appendULeb(offset);
    }

    /*
        generates either (u32)get_local(ptr) + offset or (u32)ptr1 + offset
    */
    lea (ptr1: string | number, offset: number) {
        if (typeof (ptr1) === "string")
            this.local(ptr1);
        else
            this.i32_const(ptr1);

        this.i32_const(offset);
        // FIXME: How do we make sure this has correct semantics for pointers over 2gb?
        this.appendU8(WasmOpcode.i32_add);
    }

    getArrayView (fullCapacity?: boolean) {
        if (this.stackSize > 1)
            throw new Error("Stack not empty");
        return this.stack[0].getArrayView(fullCapacity);
    }
}

export class BlobBuilder {
    buffer: number;
    view!: DataView;
    size: number;
    capacity: number;
    encoder?: TextEncoder;

    constructor () {
        this.capacity = 32000;
        this.buffer = <any>Module._malloc(this.capacity);
        this.size = 0;
        this.clear();
    }

    // It is necessary for you to call this before using the builder so that the DataView
    //  can be reconstructed in case the heap grew since last use
    clear () {
        // FIXME: This should not be necessary
        Module.HEAPU8.fill(0, this.buffer, this.buffer + this.size);
        this.size = 0;
        this.view = new DataView(Module.HEAPU8.buffer, this.buffer, this.capacity);
    }

    appendU8 (value: number | WasmOpcode) {
        if (this.size >= this.capacity)
            throw new Error("Buffer full");

        const result = this.size;
        Module.HEAPU8[this.buffer + (this.size++)] = value;
        return result;
    }

    appendU16 (value: number) {
        const result = this.size;
        this.view.setUint16(this.size, value, true);
        this.size += 2;
        return result;
    }

    appendI16 (value: number) {
        const result = this.size;
        this.view.setInt16(this.size, value, true);
        this.size += 2;
        return result;
    }

    appendU32 (value: number) {
        const result = this.size;
        this.view.setUint32(this.size, value, true);
        this.size += 4;
        return result;
    }

    appendI32 (value: number) {
        const result = this.size;
        this.view.setInt32(this.size, value, true);
        this.size += 4;
        return result;
    }

    appendF32 (value: number) {
        const result = this.size;
        this.view.setFloat32(this.size, value, true);
        this.size += 4;
        return result;
    }

    appendF64 (value: number) {
        const result = this.size;
        this.view.setFloat64(this.size, value, true);
        this.size += 8;
        return result;
    }

    appendULeb (value: number) {
        if (this.size + 8 >= this.capacity)
            throw new Error("Buffer full");

        const bytesWritten = cwraps.mono_jiterp_encode_leb52(<any>(this.buffer + this.size), value, 0);
        if (bytesWritten < 1)
            throw new Error(`Failed to encode value '${value}' as unsigned leb`);
        this.size += bytesWritten;
        return bytesWritten;
    }

    appendLeb (value: number) {
        if (this.size + 8 >= this.capacity)
            throw new Error("Buffer full");

        const bytesWritten = cwraps.mono_jiterp_encode_leb52(<any>(this.buffer + this.size), value, 1);
        if (bytesWritten < 1)
            throw new Error(`Failed to encode value '${value}' as signed leb`);
        this.size += bytesWritten;
        return bytesWritten;
    }

    appendLebRef (sourceAddress: VoidPtr, signed: boolean) {
        if (this.size + 8 >= this.capacity)
            throw new Error("Buffer full");

        const bytesWritten = cwraps.mono_jiterp_encode_leb64_ref(<any>(this.buffer + this.size), sourceAddress, signed ? 1 : 0);
        if (bytesWritten < 1)
            throw new Error("Failed to encode value as leb");
        this.size += bytesWritten;
        return bytesWritten;
    }

    appendBytes (bytes: Uint8Array) {
        const result = this.size;
        const av = this.getArrayView(true);
        av.set(bytes, this.size);
        this.size += bytes.length;
        return result;
    }

    appendName (text: string) {
        let bytes: any = null;

        if (typeof (TextEncoder) === "function") {
            if (!this.encoder)
                this.encoder = new TextEncoder();
            bytes = this.encoder.encode(text);
        } else {
            bytes = new Uint8Array(text.length);
            for (let i = 0; i < text.length; i++) {
                const ch = text.charCodeAt(i);
                if (ch > 0x7F)
                    throw new Error("Out of range character and no TextEncoder available");
                else
                    bytes[i] = ch;
            }
        }
        this.appendULeb(bytes.length);
        this.appendBytes(bytes);
    }

    getArrayView (fullCapacity?: boolean) {
        return new Uint8Array(Module.HEAPU8.buffer, this.buffer, fullCapacity ? this.capacity : this.size);
    }
}

export const enum WasmValtype {
    void = 0x40,
    i32 = 0x7F,
    i64 = 0x7E,
    f32 = 0x7D,
    f64 = 0x7C,
}

let wasmTable : WebAssembly.Table | undefined;
let wasmNextFunctionIndex = -1, wasmFunctionIndicesFree = 0;

// eslint-disable-next-line prefer-const
export const elapsedTimes = {
    generation: 0,
    compilation: 0
};

export const counters = {
    traceCandidates: 0,
    tracesCompiled: 0,
    entryWrappersCompiled: 0,
    jitCallsCompiled: 0,
    failures: 0
};

export const _now = (globalThis.performance && globalThis.performance.now)
    ? globalThis.performance.now.bind(globalThis.performance)
    : Date.now;

let scratchBuffer : NativePointer = <any>0;

export function copyIntoScratchBuffer (src: NativePointer, size: number) : NativePointer {
    if (!scratchBuffer)
        scratchBuffer = Module._malloc(64);
    if (size > 64)
        throw new Error("Scratch buffer size is 64");

    Module.HEAPU8.copyWithin(<any>scratchBuffer, <any>src, <any>src + size);
    return scratchBuffer;
}

export function getWasmFunctionTable () {
    if (!wasmTable)
        wasmTable = (<any>Module)["asm"]["__indirect_function_table"];
    if (!wasmTable)
        throw new Error("Module did not export the indirect function table");
    return wasmTable;
}

export function addWasmFunctionPointer (f: Function) {
    if (!f)
        throw new Error("Attempting to set null function into table");
    const table = getWasmFunctionTable();
    if (wasmFunctionIndicesFree <= 0) {
        wasmNextFunctionIndex = table.length;
        wasmFunctionIndicesFree = 512;
        table.grow(wasmFunctionIndicesFree);
    }
    const index = wasmNextFunctionIndex;
    wasmNextFunctionIndex++;
    wasmFunctionIndicesFree--;
    table.set(index, f);
    return index;
}

export function append_memset_dest (builder: WasmBuilder, value: number, count: number) {
    // spec: pop n, pop val, pop d, fill from d[0] to d[n] with value val
    builder.i32_const(value);
    builder.i32_const(count);
    builder.appendU8(WasmOpcode.PREFIX_sat);
    builder.appendU8(11);
    builder.appendU8(0);
}

// expects dest then source to have been pushed onto wasm stack
export function append_memmove_dest_src (builder: WasmBuilder, count: number) {
    switch (count) {
        case 1:
            builder.appendU8(WasmOpcode.i32_load8_u);
            builder.appendMemarg(0, 0);
            builder.appendU8(WasmOpcode.i32_store8);
            builder.appendMemarg(0, 0);
            return true;
        case 2:
            builder.appendU8(WasmOpcode.i32_load16_u);
            builder.appendMemarg(0, 0);
            builder.appendU8(WasmOpcode.i32_store16);
            builder.appendMemarg(0, 0);
            return true;
        case 4:
            builder.appendU8(WasmOpcode.i32_load);
            builder.appendMemarg(0, 0);
            builder.appendU8(WasmOpcode.i32_store);
            builder.appendMemarg(0, 0);
            return true;
        case 8:
            builder.appendU8(WasmOpcode.i64_load);
            builder.appendMemarg(0, 0);
            builder.appendU8(WasmOpcode.i64_store);
            builder.appendMemarg(0, 0);
            return true;
        default:
            // spec: pop n, pop s, pop d, copy n bytes from s to d
            builder.i32_const(count);
            // great encoding isn't it
            builder.appendU8(WasmOpcode.PREFIX_sat);
            builder.appendU8(10);
            builder.appendU8(0);
            builder.appendU8(0);
            return true;
    }
}

export function recordFailure () : void {
    counters.failures++;
    if (counters.failures >= maxFailures) {
        console.log(`MONO_WASM: Disabling jiterpreter after ${counters.failures} failures`);
        applyOptions(<any>{
            enableTraces: false,
            enableInterpEntry: false,
            enableJitCall: false
        });
    }
}

export function getRawCwrap (name: string): Function {
    const result = (<any>Module)["asm"][name];
    if (typeof (result) !== "function")
        throw new Error(`raw cwrap ${name} not found`);
    return result;
}

export function importDef (name: string, fn: Function): [string, string, Function] {
    return [name, name, fn];
}

export type JiterpreterOptions = {
    enableAll?: boolean;
    enableTraces: boolean;
    enableInterpEntry: boolean;
    enableJitCall: boolean;
    enableBackwardBranches: boolean;
    enableCallResume: boolean;
    enableWasmEh: boolean;
    // For locations where the jiterpreter heuristic says we will be unable to generate
    //  a trace, insert an entry point opcode anyway. This enables collecting accurate
    //  stats for options like estimateHeat, but raises overhead.
    alwaysGenerate: boolean;
    enableStats: boolean;
    // Continue counting hits for traces that fail to compile and use it to estimate
    //  the relative importance of the opcode that caused them to abort
    estimateHeat: boolean;
    // Count the number of times a trace bails out (branch taken, etc) and for what reason
    countBailouts: boolean;
    minimumTraceLength: number;
}

const optionNames : { [jsName: string] : string } = {
    "enableTraces": "jiterpreter-traces-enabled",
    "enableInterpEntry": "jiterpreter-interp-entry-enabled",
    "enableJitCall": "jiterpreter-jit-call-enabled",
    "enableBackwardBranches": "jiterpreter-backward-branch-entries-enabled",
    "enableCallResume": "jiterpreter-call-resume-enabled",
    "enableWasmEh": "jiterpreter-wasm-eh-enabled",
    "enableStats": "jiterpreter-stats-enabled",
    "alwaysGenerate": "jiterpreter-always-generate",
    "estimateHeat": "jiterpreter-estimate-heat",
    "countBailouts": "jiterpreter-count-bailouts",
    "minimumTraceLength": "jiterpreter-minimum-trace-length",
};

let optionsVersion = -1;
let optionTable : JiterpreterOptions = <any>{};

// applies one or more jiterpreter options to change the current jiterpreter configuration.
export function applyOptions (options: JiterpreterOptions) {
    for (const k in options) {
        const info = optionNames[k];
        if (!info) {
            console.error(`Unrecognized jiterpreter option: ${k}`);
            continue;
        }

        const v = (<any>options)[k];
        if (typeof (v) === "boolean")
            cwraps.mono_jiterp_parse_option((v ? "--" : "--no-") + info);
        else if (typeof (v) === "number")
            cwraps.mono_jiterp_parse_option(`--${info}=${v}`);
        else
            console.error(`Jiterpreter option must be a boolean or a number but was ${typeof(v)} '${v}'`);
    }
}

// returns the current jiterpreter configuration. do not mutate the return value!
export function getOptions () {
    const currentVersion = cwraps.mono_jiterp_get_options_version();
    if (currentVersion !== optionsVersion) {
        updateOptions();
        optionsVersion = currentVersion;
    }
    return optionTable;
}

function updateOptions () {
    const pJson = cwraps.mono_jiterp_get_options_as_json();
    const json = Module.UTF8ToString(<any>pJson);
    Module._free(<any>pJson);
    const blob = JSON.parse(json);

    optionTable = <any>{};
    for (const k in optionNames) {
        const info = optionNames[k];
        (<any>optionTable)[k] = blob[info];
    }
}
