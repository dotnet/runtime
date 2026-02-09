// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import { dotnetBrowserUtilsExports, dotnetApi, dotnetAssert, dotnetLogger, VoidPtrNull, Module } from "./cross-module";

import type { BindingClosureJS, BoundMarshalerToJs, JSFnHandle, JSFunctionSignature, JSHandle, JSMarshalerArguments, VoidPtr, WrappedJSFunction } from "./types";

import { MarshalerType, MeasuredBlock } from "./types";
import { getSig, getSignatureArgumentCount, getSignatureFunctionName, getSignatureHandle, getSignatureModuleName, getSignatureType, getSignatureVersion, isReceiverShouldFree, jsInteropState } from "./marshal";
import { assertJsInterop, assertRuntimeRunning, endMeasure, fixupPointer, normalizeException, startMeasure } from "./utils";
import { bindArgMarshalToJs } from "./marshal-to-js";
import { boundJsFunctionSymbol, getJSObjectFromJSHandle, importedJsFunctionSymbol, jsImportWrapperByFnHandle } from "./gc-handles";
import { bindArgMarshalToCs, marshalExceptionToCs } from "./marshal-to-cs";
import { wrapAsCancelablePromise } from "./cancelable-promise";

export const importedModulesPromises: Map<string, Promise<any>> = new Map();
export const importedModules: Map<string, Promise<any>> = new Map();

export function setModuleImports(moduleName: string, moduleImports: any): void {
    importedModules.set(moduleName, moduleImports);
    dotnetLogger.debug(() => `added module imports '${moduleName}'`);
}

export function bindJSImportST(signature: JSFunctionSignature): VoidPtr {
    try {
        signature = fixupPointer(signature, 0);
        bindJsImport(signature);
        return VoidPtrNull;
    } catch (ex: any) {
        return dotnetBrowserUtilsExports.stringToUTF16Ptr(normalizeException(ex));
    }
}

export function invokeJSImportST(functionHandle: JSFnHandle, args: JSMarshalerArguments) {
    assertRuntimeRunning();
    args = fixupPointer(args, 0);
    const boundFn = jsImportWrapperByFnHandle[<any>functionHandle];
    dotnetAssert.fastCheck(boundFn, () => `Imported function handle expected ${functionHandle}`);
    boundFn(args);
}

function bindJsImport(signature: JSFunctionSignature): Function {
    assertJsInterop();
    const mark = startMeasure();

    const version = getSignatureVersion(signature);
    dotnetAssert.fastCheck(version === 2, () => `Signature version ${version} mismatch.`);

    const jsFunctionName = getSignatureFunctionName(signature)!;
    const jsModuleName = getSignatureModuleName(signature)!;
    const functionHandle = getSignatureHandle(signature);

    dotnetLogger.debug(() => `Binding [JSImport] ${jsFunctionName} from ${jsModuleName} module`);

    const fn = lookupJsImport(jsFunctionName, jsModuleName);
    const argsCount = getSignatureArgumentCount(signature);

    const argMarshalers: (BoundMarshalerToJs)[] = new Array(argsCount);
    const argCleanup: (Function | undefined)[] = new Array(argsCount);
    let hasCleanup = false;
    for (let index = 0; index < argsCount; index++) {
        const sig = getSig(signature, index + 2);
        const marshalerType = getSignatureType(sig);
        const argMarshaler = bindArgMarshalToJs(sig, marshalerType, index + 2);
        dotnetAssert.check(argMarshaler, "ERR42: argument marshaler must be resolved");
        argMarshalers[index] = argMarshaler;
        if (marshalerType === MarshalerType.Span) {
            argCleanup[index] = (jsArg: any) => {
                if (jsArg) {
                    jsArg.dispose();
                }
            };
            hasCleanup = true;
        }
    }
    const resSig = getSig(signature, 1);
    const resmarshalerType = getSignatureType(resSig);
    const resConverter = bindArgMarshalToCs(resSig, resmarshalerType, 1);

    const isDiscardNoWait = resmarshalerType == MarshalerType.DiscardNoWait;
    const isAsync = resmarshalerType == MarshalerType.Task || resmarshalerType == MarshalerType.TaskPreCreated;

    const closure: BindingClosureJS = {
        fn,
        fqn: jsModuleName + ":" + jsFunctionName,
        argsCount,
        argMarshalers,
        resConverter,
        hasCleanup,
        argCleanup,
        isDiscardNoWait,
        isAsync,
        isDisposed: false,
    };
    let boundFn: WrappedJSFunction;
    if (isAsync || isDiscardNoWait || hasCleanup) {
        boundFn = bindFn(closure);
    } else {
        if (argsCount == 0 && !resConverter) {
            boundFn = bind_fn_0V(closure);
        } else if (argsCount == 1 && !resConverter) {
            boundFn = bind_fn_1V(closure);
        } else if (argsCount == 1 && resConverter) {
            boundFn = bind_fn_1R(closure);
        } else if (argsCount == 2 && resConverter) {
            boundFn = bind_fn_2R(closure);
        } else {
            boundFn = bindFn(closure);
        }
    }

    let wrappedFn: WrappedJSFunction = boundFn;


    // this is just to make debugging easier by naming the function in the stack trace.
    // It's not CSP compliant and possibly not performant, that's why it's only enabled in debug builds
    // in Release configuration, it would be a trimmed by rollup
    if (BuildConfiguration === "Debug" && !jsInteropState.cspPolicy) {
        try {
            const fname = jsFunctionName.replaceAll(".", "_");
            const url = `//# sourceURL=https://dotnet/JSImport/${fname}`;
            const body = `return (function JSImport_${fname}(){ return fn.apply(this, arguments)});`;
            wrappedFn = new Function("fn", url + "\r\n" + body)(wrappedFn);
        } catch (ex) {
            jsInteropState.cspPolicy = true;
        }
    }

    (<any>wrappedFn)[importedJsFunctionSymbol] = closure;

    jsImportWrapperByFnHandle[functionHandle] = wrappedFn;

    endMeasure(mark, MeasuredBlock.bindJsFunction, jsFunctionName);

    return wrappedFn;
}

function bind_fn_0V(closure: BindingClosureJS) {
    const fn = closure.fn;
    const fqn = closure.fqn;
    (<any>closure) = null;
    return function boundFn_0V(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            // call user function
            fn();
        } catch (ex) {
            marshalExceptionToCs(<any>args, ex);
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1V(closure: BindingClosureJS) {
    const fn = closure.fn;
    const marshaler1 = closure.argMarshalers[0]!;
    const fqn = closure.fqn;
    (<any>closure) = null;
    return function boundFn_1V(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            const arg1 = marshaler1(args);
            // call user function
            fn(arg1);
        } catch (ex) {
            marshalExceptionToCs(<any>args, ex);
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1R(closure: BindingClosureJS) {
    const fn = closure.fn;
    const marshaler1 = closure.argMarshalers[0]!;
    const resConverter = closure.resConverter!;
    const fqn = closure.fqn;
    (<any>closure) = null;
    return function boundFn_1R(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            const arg1 = marshaler1(args);
            // call user function
            const jsResult = fn(arg1);
            resConverter(args, jsResult);
        } catch (ex) {
            marshalExceptionToCs(<any>args, ex);
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_2R(closure: BindingClosureJS) {
    const fn = closure.fn;
    const marshaler1 = closure.argMarshalers[0]!;
    const marshaler2 = closure.argMarshalers[1]!;
    const resConverter = closure.resConverter!;
    const fqn = closure.fqn;
    (<any>closure) = null;
    return function boundFn_2R(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            const arg1 = marshaler1(args);
            const arg2 = marshaler2(args);
            // call user function
            const jsResult = fn(arg1, arg2);
            resConverter(args, jsResult);
        } catch (ex) {
            marshalExceptionToCs(<any>args, ex);
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bindFn(closure: BindingClosureJS) {
    const argsCount = closure.argsCount;
    const argMarshalers = closure.argMarshalers;
    const resConverter = closure.resConverter;
    const argCleanup = closure.argCleanup;
    const hasCleanup = closure.hasCleanup;
    const fn = closure.fn;
    const fqn = closure.fqn;
    (<any>closure) = null;
    return function boundFn(args: JSMarshalerArguments) {
        const receiverShouldFree = isReceiverShouldFree(args);
        const mark = startMeasure();
        try {
            const jsArgs = new Array(argsCount);
            for (let index = 0; index < argsCount; index++) {
                const marshaler = argMarshalers[index]!;
                const jsArg = marshaler(args);
                jsArgs[index] = jsArg;
            }

            // call user function
            const jsResult = fn(...jsArgs);

            if (resConverter) {
                resConverter(args, jsResult);
            }

            if (hasCleanup) {
                for (let index = 0; index < argsCount; index++) {
                    const cleanup = argCleanup[index];
                    if (cleanup) {
                        cleanup(jsArgs[index]);
                    }
                }
            }
        } catch (ex) {
            marshalExceptionToCs(<any>args, ex);
        } finally {
            if (receiverShouldFree) {
                Module._free(args as any);
            }
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function lookupJsImport(functionName: string, jsModuleName: string | null): Function {
    dotnetAssert.check(functionName && typeof functionName === "string", "functionName must be string");

    let scope: any = {};
    const parts = functionName.split(".");
    if (jsModuleName) {
        scope = importedModules.get(jsModuleName);
        dotnetAssert.fastCheck(scope, () => `ES6 module ${jsModuleName} was not imported yet, please call JSHost.ImportAsync() first in order to invoke ${functionName}.`);
    } else if (parts[0] === "INTERNAL") {
        scope = dotnetApi.INTERNAL;
        parts.shift();
    } else if (parts[0] === "globalThis") {
        scope = globalThis;
        parts.shift();
    }

    for (let i = 0; i < parts.length - 1; i++) {
        const part = parts[i];
        const newscope = scope[part];
        if (!newscope) {
            throw new Error(`${part} not found while looking up ${functionName}`);
        }
        scope = newscope;
    }

    const fname = parts[parts.length - 1];
    const fn = scope[fname];

    if (typeof (fn) !== "function") {
        throw new Error(`${functionName} must be a Function but was ${typeof fn}`);
    }

    // if the function was already bound to some object it would stay bound to original object. That's good.
    return fn.bind(scope);
}

export function invokeJSFunction(functionJSHandle: JSHandle, args: JSMarshalerArguments): void {
    assertRuntimeRunning();
    const boundFn = getJSObjectFromJSHandle(functionJSHandle);
    dotnetAssert.fastCheck(boundFn && typeof (boundFn) === "function" && boundFn[boundJsFunctionSymbol], () => `Bound function handle expected ${functionJSHandle}`);
    args = fixupPointer(args, 0);
    boundFn(args);
}

export function setProperty(self: any, name: string, value: any): void {
    dotnetAssert.check(self, "Null reference");
    self[name] = value;
}

export function getProperty(self: any, name: string): any {
    dotnetAssert.check(self, "Null reference");
    return self[name];
}

export function hasProperty(self: any, name: string): boolean {
    dotnetAssert.check(self, "Null reference");
    return name in self;
}

export function getTypeOfProperty(self: any, name: string): string {
    dotnetAssert.check(self, "Null reference");
    return typeof self[name];
}

export function getGlobalThis(): any {
    return globalThis;
}

export function getDotnetInstance(): any {
    return dotnetApi;
}

export function dynamicImport(module_name: string, module_url: string): Promise<any> {
    assertJsInterop();
    dotnetAssert.check(module_name && typeof module_name === "string", "module_name must be string");
    dotnetAssert.check(module_url && typeof module_url === "string", "module_url must be string");
    let promise = importedModulesPromises.get(module_name);
    const newPromise = !promise;
    if (newPromise) {
        dotnetLogger.debug(() => `importing ES6 module '${module_name}' from '${module_url}'`);
        promise = import(/*! webpackIgnore: true */module_url);
        importedModulesPromises.set(module_name, promise);
    }

    return wrapAsCancelablePromise(async function register() {
        const module = await promise;
        if (newPromise) {
            importedModules.set(module_name, module);
            dotnetLogger.debug(() => `imported ES6 module '${module_name}' from '${module_url}'`);
        }
        return module;
    });
}
