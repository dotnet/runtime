// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import { parseJSExportNames } from "../../Common/JavaScript/export-names";

import type { BindingClosureCS, BoundMarshalerToCs, CSFnHandle, DeferredBindingCS, JSFunctionSignature } from "./types";
import type { NativePointer } from "../types";

import { dotnetApi, dotnetAssert, dotnetLogger, Module } from "./cross-module";

import { bindAssemblyExports, invokeJSExport } from "./managed-exports";
import { allocStackFrame, getSig, getSignatureType, getSignatureArgumentCount, getSignatureVersion, jsInteropState } from "./marshal";
import { bindArgMarshalToCs } from "./marshal-to-cs";
import { bindArgMarshalToJs, endMarshalTaskToJs, releaseEagerTaskHolder } from "./marshal-to-js";
import { assertJsInterop, assertRuntimeRunning, endMeasure, isRuntimeRunning, startMeasure } from "./utils";
import { JSMarshalerSignatureHeaderSize, JSMarshalerTypeSize, MarshalerType, MeasuredBlock } from "./types";
import { boundCsFunctionSymbol, deferredCsFunctions, exportsByAssembly } from "./gc-handles";

export async function getAssemblyExports(assemblyName: string): Promise<any> {
    assertJsInterop();
    if (assemblyName.endsWith(".dll")) {
        assemblyName = assemblyName.substring(0, assemblyName.length - 4);
    }
    const result = exportsByAssembly.get(assemblyName);
    if (!result) {
        await bindAssemblyExports(assemblyName);
    }

    return exportsByAssembly.get(assemblyName) || {};
}

export function registerLazyCsFunctions(assemblyName: string, exportNames: string[], methodHandles: ArrayLike<CSFnHandle>, signatures: ArrayLike<JSFunctionSignature>): void {
    assertRuntimeRunning();
    const names = parseJSExportNames(assemblyName, exportNames);
    dotnetAssert.check(names.length === methodHandles.length && names.length === signatures.length, "JSExport signatures do not match the catalog.");
    const snapshots = Array.from(signatures, signature => {
        dotnetAssert.check(getSignatureVersion(signature) === 2, "JSExport signature version mismatch.");
        const size = JSMarshalerSignatureHeaderSize + (getSignatureArgumentCount(signature) + 2) * JSMarshalerTypeSize;
        const start = Number(signature) >>> 0;
        return dotnetApi.localHeapViewU8().slice(start, start + size);
    });
    if (!exportsByAssembly.has(assemblyName)) {
        const exports = {};
        exportsByAssembly.set(assemblyName, exports);
        exportsByAssembly.set(assemblyName + ".dll", exports);
    }
    for (const { methodId, namespaceName, shortClassName, methodName, signatureHash, isDefault } of names) {
        const key = getCsFunctionKey(assemblyName, namespaceName, shortClassName, methodName, signatureHash);
        const existing = deferredCsFunctions.get(key);
        if (existing) {
            walkExportsToSeFunction(assemblyName, namespaceName, shortClassName, methodName, signatureHash, existing.wrapper!, isDefault);
            continue;
        }

        const index = Number(methodId);
        const binding: DeferredBindingCS = { signature: snapshots[index], isBinding: false, isDisposed: false };
        const deferredFn = function (this: unknown, ...args: unknown[]) {
            assertRuntimeRunning();
            dotnetAssert.check(!binding.isDisposed, `The JSExport '${key}' is disposed.`);
            if (!binding.boundFn) {
                dotnetAssert.check(!binding.isBinding, `Reentrant binding of JSExport '${key}'.`);
                const signature = binding.signature;
                dotnetAssert.check(signature, `Missing signature for JSExport '${key}'.`);
                binding.isBinding = true;
                const sp = Module.stackSave();
                try {
                    const pointer: NativePointer = Module.stackAlloc(signature.length);
                    dotnetApi.localHeapViewU8().set(signature, Number(pointer) >>> 0);
                    bindCsFunction(methodHandles[index], assemblyName, namespaceName, shortClassName, methodName, signatureHash, pointer as JSFunctionSignature);
                } finally {
                    if (isRuntimeRunning()) Module.stackRestore(sp);
                    binding.isBinding = false;
                }
            }
            const boundFn = binding.boundFn;
            dotnetAssert.check(boundFn, `JSExport '${key}' did not publish its binding.`);
            return boundFn.apply(this, args);
        };
        binding.wrapper = deferredFn;
        deferredCsFunctions.set(key, binding);
        walkExportsToSeFunction(assemblyName, namespaceName, shortClassName, methodName, signatureHash, deferredFn, isDefault);
    }
}

function getCsFunctionKey(assemblyName: string, namespaceName: string, shortClassName: string, methodName: string, signatureHash: number): string {
    return `[${assemblyName}] ${namespaceName}.${shortClassName}:${methodName}.${signatureHash}`;
}

export function bindCsFunction(methodHandle: CSFnHandle, assemblyName: string, namespaceName: string, shortClassName: string, methodName: string, signatureHash: number, signature: JSFunctionSignature): void {
    const fullyQualifiedName = `[${assemblyName}] ${namespaceName}.${shortClassName}:${methodName}`;
    const mark = startMeasure();
    dotnetLogger.debug(() => `Binding [JSExport] ${namespaceName}.${shortClassName}:${methodName} from ${assemblyName} assembly`);
    const version = getSignatureVersion(signature);
    dotnetAssert.fastCheck(version === 2, () => `Signature version ${version} mismatch.`);


    const argsCount = getSignatureArgumentCount(signature);

    const argMarshalers: (BoundMarshalerToCs)[] = new Array(argsCount);
    for (let index = 0; index < argsCount; index++) {
        const sig = getSig(signature, index + 2);
        const marshalerType = getSignatureType(sig);
        const argMarshaler = bindArgMarshalToCs(sig, marshalerType, index + 2);
        dotnetAssert.check(argMarshaler, "ERR43: argument marshaler must be resolved");
        argMarshalers[index] = argMarshaler;
    }

    const resSig = getSig(signature, 1);
    let resMarshalerType = getSignatureType(resSig);

    const isAsync = resMarshalerType == MarshalerType.Task;
    const isDiscardNoWait = resMarshalerType == MarshalerType.DiscardNoWait;
    if (isAsync) {
        resMarshalerType = MarshalerType.TaskPreCreated;
    }
    const resConverter = bindArgMarshalToJs(resSig, resMarshalerType, 1);

    const closure: BindingClosureCS = {
        methodHandle,
        fullyQualifiedName,
        argsCount,
        argMarshalers,
        resConverter,
        isAsync,
        isDiscardNoWait,
        isDisposed: false,
    };
    let boundFn: Function;

    if (isAsync) {
        if (argsCount == 1 && resConverter) {
            boundFn = bindFn_1RA(closure);
        } else if (argsCount == 2 && resConverter) {
            boundFn = bindFn_2RA(closure);
        } else {
            boundFn = bindFn(closure);
        }
    } else if (isDiscardNoWait) {
        boundFn = bindFn(closure);
    } else {
        if (argsCount == 0 && !resConverter) {
            boundFn = bindFn_0V(closure);
        } else if (argsCount == 1 && !resConverter) {
            boundFn = bindFn_1V(closure);
        } else if (argsCount == 1 && resConverter) {
            boundFn = bindFn_1R(closure);
        } else if (argsCount == 2 && resConverter) {
            boundFn = bindFn_2R(closure);
        } else {
            boundFn = bindFn(closure);
        }
    }

    // this is just to make debugging easier.
    // It's not CSP compliant and possibly not performant, that's why it's only enabled in debug builds
    // in Release configuration, it would be a trimmed by rollup
    if (BuildConfiguration === "Debug" && !jsInteropState.cspPolicy) {
        try {
            const url = `//# sourceURL=https://dotnet/JSExport/${methodName}`;
            const body = `return (function JSExport_${methodName}(){ return fn.apply(this, arguments)});`;
            boundFn = new Function("fn", url + "\r\n" + body)(boundFn);
        } catch (ex) {
            jsInteropState.cspPolicy = true;
        }
    }

    (<any>boundFn)[boundCsFunctionSymbol] = closure;

    const deferred = deferredCsFunctions.get(getCsFunctionKey(assemblyName, namespaceName, shortClassName, methodName, signatureHash));
    if (deferred) {
        deferred.boundFn = boundFn;
        deferred.signature = undefined;
        Object.assign(deferred.wrapper!, { [boundCsFunctionSymbol]: closure });
    } else {
        walkExportsToSeFunction(assemblyName, namespaceName, shortClassName, methodName, signatureHash, boundFn);
    }
    endMeasure(mark, MeasuredBlock.bindCsFunction, fullyQualifiedName);
}

function bindFn_0V(closure: BindingClosureCS) {
    const method = closure.methodHandle;
    const fqn = closure.fullyQualifiedName;
    (<any>closure) = null;
    return function boundFn_0V() {
        const mark = startMeasure();
        assertRuntimeRunning();
        const sp = Module.stackSave();
        try {
            const size = 2;
            const args = allocStackFrame(size);
            // call C# side
            invokeJSExport(method, args);
        } finally {
            if (isRuntimeRunning()) Module.stackRestore(sp);

            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bindFn_1V(closure: BindingClosureCS) {
    const method = closure.methodHandle;
    const marshaler1 = closure.argMarshalers[0]!;
    const fqn = closure.fullyQualifiedName;
    return function boundFn_1V(arg1: any) {
        const mark = startMeasure();
        assertRuntimeRunning();
        const sp = Module.stackSave();
        try {
            const size = 3;
            const args = allocStackFrame(size);
            marshaler1(args, arg1);

            // call C# side
            invokeJSExport(method, args);
        } finally {
            if (isRuntimeRunning()) Module.stackRestore(sp);

            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bindFn_1R(closure: BindingClosureCS) {
    const method = closure.methodHandle;
    const marshaler1 = closure.argMarshalers[0]!;
    const resConverter = closure.resConverter!;
    const fqn = closure.fullyQualifiedName;
    return function boundFn_1R(arg1: any) {
        const mark = startMeasure();
        assertRuntimeRunning();
        const sp = Module.stackSave();
        try {
            const size = 3;
            const args = allocStackFrame(size);
            marshaler1(args, arg1);

            // call C# side
            invokeJSExport(method, args);

            const jsResult = resConverter(args);
            return jsResult;
        } finally {
            if (isRuntimeRunning()) Module.stackRestore(sp);

            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bindFn_1RA(closure: BindingClosureCS) {
    const methodHandle = closure.methodHandle;
    const marshaler1 = closure.argMarshalers[0]!;
    const resConverter = closure.resConverter!;
    const fqn = closure.fullyQualifiedName;
    (<any>closure) = null;
    return function bindFn_1RA(arg1: any) {
        const mark = startMeasure();
        assertRuntimeRunning();
        const sp = Module.stackSave();
        try {
            const size = 3;
            const args = allocStackFrame(size);
            marshaler1(args, arg1);

            // pre-allocate the promise
            let promise = resConverter(args);

            try {
                // call C# side
                invokeJSExport(methodHandle, args);
            } catch (ex) {
                // the throw unwinds past endMarshalTaskToJs, which would otherwise adopt it
                releaseEagerTaskHolder(promise);
                throw ex;
            }

            // in case the C# side returned synchronously
            promise = endMarshalTaskToJs(args, undefined, promise);

            return promise;
        } finally {
            if (isRuntimeRunning()) Module.stackRestore(sp);

            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bindFn_2R(closure: BindingClosureCS) {
    const method = closure.methodHandle;
    const marshaler1 = closure.argMarshalers[0]!;
    const marshaler2 = closure.argMarshalers[1]!;
    const resConverter = closure.resConverter!;
    const fqn = closure.fullyQualifiedName;
    (<any>closure) = null;
    return function boundFn_2R(arg1: any, arg2: any) {
        const mark = startMeasure();
        assertRuntimeRunning();
        const sp = Module.stackSave();
        try {
            const size = 4;
            const args = allocStackFrame(size);
            marshaler1(args, arg1);
            marshaler2(args, arg2);

            // call C# side
            invokeJSExport(method, args);

            const jsResult = resConverter(args);
            return jsResult;
        } finally {
            if (isRuntimeRunning()) Module.stackRestore(sp);

            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bindFn_2RA(closure: BindingClosureCS) {
    const methodHandle = closure.methodHandle;
    const marshaler1 = closure.argMarshalers[0]!;
    const marshaler2 = closure.argMarshalers[1]!;
    const resConverter = closure.resConverter!;
    const fqn = closure.fullyQualifiedName;
    (<any>closure) = null;
    return function bindFn_2RA(arg1: any, arg2: any) {
        const mark = startMeasure();
        assertRuntimeRunning();
        const sp = Module.stackSave();
        try {
            const size = 4;
            const args = allocStackFrame(size);
            marshaler1(args, arg1);
            marshaler2(args, arg2);

            // pre-allocate the promise
            let promise = resConverter(args);

            try {
                // call C# side
                invokeJSExport(methodHandle, args);
            } catch (ex) {
                // the throw unwinds past endMarshalTaskToJs, which would otherwise adopt it
                releaseEagerTaskHolder(promise);
                throw ex;
            }

            // in case the C# side returned synchronously
            promise = endMarshalTaskToJs(args, undefined, promise);

            return promise;
        } finally {
            if (isRuntimeRunning()) Module.stackRestore(sp);

            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bindFn(closure: BindingClosureCS) {
    const argsCount = closure.argsCount;
    const argMarshalers = closure.argMarshalers;
    const resConverter = closure.resConverter;
    const methodHandle = closure.methodHandle;
    const fqn = closure.fullyQualifiedName;
    const isAsync = closure.isAsync;
    const isDiscardNoWait = closure.isDiscardNoWait;
    return function boundFn(...jsArgs: any[]) {
        const mark = startMeasure();
        assertRuntimeRunning();
        const sp = Module.stackSave();
        try {
            const size = 2 + argsCount;
            const args = allocStackFrame(size);
            for (let index = 0; index < argsCount; index++) {
                const marshaler = argMarshalers[index];
                if (marshaler) {
                    const jsArg = jsArgs[index];
                    marshaler(args, jsArg);
                }
            }
            let jsResult = undefined;
            if (isAsync) {
                // pre-allocate the promise
                jsResult = resConverter!(args);
            }

            // call C# side
            if (isAsync) {
                try {
                    invokeJSExport(methodHandle, args);
                } catch (ex) {
                    // the throw unwinds past endMarshalTaskToJs, which would otherwise adopt it
                    releaseEagerTaskHolder(jsResult);
                    throw ex;
                }
                // in case the C# side returned synchronously
                jsResult = endMarshalTaskToJs(args, undefined, jsResult);
            } else if (isDiscardNoWait) {
                // call C# side, fire and forget
                invokeJSExport(methodHandle, args);
            } else {
                invokeJSExport(methodHandle, args);
                if (resConverter) {
                    jsResult = resConverter(args);
                }
            }
            return jsResult;
        } finally {
            if (isRuntimeRunning()) Module.stackRestore(sp);

            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function walkExportsToSeFunction(assembly: string, namespace: string, classname: string, methodname: string, signatureHash: number, fn: Function, isDefault = true): void {
    const parts = `${namespace}.${classname}`.replace(/\+/g, ".").replace(/\//g, ".").split(".");
    let scope: any = undefined;
    let assemblyScope = exportsByAssembly.get(assembly);
    if (!assemblyScope) {
        assemblyScope = {};
        exportsByAssembly.set(assembly, assemblyScope);
        exportsByAssembly.set(assembly + ".dll", assemblyScope);
    }
    scope = assemblyScope;
    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];
        if (part != "") {
            let newscope = scope[part];
            if (typeof newscope === "undefined") {
                newscope = {};
                scope[part] = newscope;
            }
            dotnetAssert.fastCheck(newscope, () => `${part} not found while looking up ${classname}`);
            scope = newscope;
        }
    }

    if (isDefault && !scope[methodname]) {
        scope[methodname] = fn;
    }
    scope[`${methodname}.${signatureHash}`] = fn;
}
