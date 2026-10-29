// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export async function setup(runtime) {
    const exports = (await runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests"))
        .System.Runtime.InteropServices.JavaScript.Tests.WorkerInteropTest;
    const worker = runtime.getConfig().workerOptions !== undefined;
    const voidEvents = [];
    function check(condition, message) {
        if (!condition) throw new Error(message);
    }
    runtime.setModuleImports("WorkerInteropTest", {
        echo: value => value,
        invokeEcho: value => {
            const result = exports.Echo(value);
            if (typeof result !== "number") throw new Error("Scalar export must return synchronously");
            return result;
        },
        invokeNested: value => exports.Nested(value),
        echoAsync: async value => {
            await new Promise(resolve => setTimeout(resolve, 0));
            return value;
        },
        invokeAsync: value => exports.EchoAsync(value),
        fail: value => {
            if (value === -1) throw new Error("Worker UI import failure");
            return value;
        },
        echoString: value => value,
        invokeString: value => exports.EchoString(value),
        echoStringAsync: async value => {
            await new Promise(resolve => setTimeout(resolve, 0));
            return value;
        },
        invokeStringAsync: value => exports.EchoStringAsync(value),
        nestedString: value => exports.NestedString(value),
        failString: value => exports.ThrowString(value),
        failStringAsync: async () => {
            throw new Error("UI async string failure");
        },
        stringLength: value => exports.StringLength(value),
        largeString: length => exports.LargeString(length),
        growString: length => {
            const before = worker ? runtime.INTERNAL.workerMemoryViews.refreshIfNeeded().u8.length : runtime.localHeapViewU8().length;
            const result = exports.LargeString(length);
            const after = worker ? runtime.INTERNAL.workerMemoryViews.refreshIfNeeded().u8.length : runtime.localHeapViewU8().length;
            if (after <= before) throw new Error("Expected actual native memory growth during string export");
            return result;
        },
        echoBoolean: value => value,
        invokeBoolean: value => exports.EchoBoolean(value),
        echoBooleanAsync: async value => value,
        invokeBooleanAsync: value => exports.EchoBooleanAsync(value),
        echoNumber: value => value,
        echoNumberAsync: async value => value,
        invokeDouble: value => exports.EchoDouble(value),
        invokeDoubleAsync: value => exports.EchoDoubleAsync(value),
        invokeSingle: value => exports.EchoSingle(value),
        invokeSingleAsync: value => exports.EchoSingleAsync(value),
        nullPrimitive: () => null,
        nullPrimitiveAsync: async () => null,
        invalidPrimitive: () => ({}),
        noArgumentValue() {
            check(arguments.length === 0, "Zero-argument import received arguments");
            return 42;
        },
        noArgumentString() {
            check(arguments.length === 0, "Zero-argument String import received arguments");
            return "zero\0argument";
        },
        async noArgumentValueAsync() {
            check(arguments.length === 0, "Zero-argument Task import received arguments");
            return 43;
        },
        async noArgumentStringAsync() {
            check(arguments.length === 0, "Zero-argument String Task import received arguments");
            return "zero\0async";
        },
        void() {
            check(arguments.length === 0, "Void import received arguments");
            voidEvents.push("sync");
            queueMicrotask(() => voidEvents.push("microtask"));
            return 123;
        },
        voidArgument(value) {
            check(arguments.length === 1, "One-argument void import arity changed");
            if (value === "throw") throw new Error("Synchronous UI void failure");
            voidEvents.push(value);
            return 123;
        },
        async voidAsync() {
            check(arguments.length === 0, "Async void import received arguments");
            voidEvents.push("async-prefix");
            await new Promise(resolve => setTimeout(resolve, 0));
            voidEvents.push("async-done");
            return 123;
        },
        async voidAsyncArgument(value) {
            check(arguments.length === 1, "Async void import arity changed");
            await new Promise(resolve => setTimeout(resolve, 0));
            voidEvents.push(value);
            return 123;
        },
        voidTaskMode: value => {
            if (value === 0) return null;
            if (value === 1) return undefined;
            if (value === 2) throw new Error("UI void prefix fault");
            if (value === 3) return 42;
            if (value === 4) return Promise.reject(new Error("UI void Task fault"));
            return Promise.resolve(123);
        },
        voidContracts: value => {
            check(exports.NoArgumentValue("ignored extra") === 42, "Zero-argument export failed");
            check(exports.NoArgumentString() === "zero\0argument", "Zero-argument String export failed");
            const before = exports.VoidCallCount();
            voidEvents.length = 0;
            check(exports.VoidExport("ignored extra") === undefined, "Void export returned a value");
            check(voidEvents.join(",") === "sync", "Void prefix ordering changed");
            check(exports.VoidCallCount() === before + 1, "Void managed prefix did not run");
            check(exports.VoidStringExport("text\0argument") === undefined, "String-to-void export returned a value");
            check(voidEvents[1] === "text\0argument", "String-to-void input changed");
            return value;
        },
        voidAsyncContracts: async value => {
            check(await exports.NoArgumentValueAsync() === 43, "Zero-argument Task value export failed");
            check(await exports.NoArgumentStringAsync() === "zero\0async", "Zero-argument Task String export failed");
            voidEvents.length = 0;
            const before = exports.VoidCallCount();
            const task = exports.VoidAsyncExport();
            check(task instanceof Promise, "Task export must return a Promise");
            check(exports.VoidCallCount() === before + 1, "Task managed prefix did not run");
            check(voidEvents.join(",") === "async-prefix", "Task UI prefix ordering changed");
            check(await task === undefined, "Task-without-result completion must be undefined");
            check(voidEvents.join(",") === "async-prefix,async-done", "Task completion ordering changed");
            const tasks = Array.from({ length: 70 }, (_, index) => exports.VoidStringAsyncExport("task:" + index));
            check((await Promise.all(tasks)).every(result => result === undefined), "Task result was not ignored");
            check(exports.VoidTaskModeExport(0) === null, "Null Task changed");
            check(await exports.VoidTaskModeExport(1) === undefined, "Completed Task changed");
            for (const [mode, message, asynchronous] of [
                [2, "Managed void Task fault", true], [3, "Managed void prefix fault", false]
            ]) {
                let error;
                try {
                    if (asynchronous) await exports.VoidTaskModeExport(mode);
                    else exports.VoidTaskModeExport(mode);
                } catch (caught) {
                    error = caught;
                }
                check(error?.message.includes(message), "Expected Task failure was not propagated");
                error.dispose?.();
            }
            return value;
        },
        sumTwo: (first, second) => exports.SumTwo(first, second),
        joinThree: (first, second, third) => exports.JoinThree(first, second, third),
        mixedSix(...args) {
            check(arguments.length === 6 && typeof this.mixedSixAsync === "function", "Multiple import arity or receiver changed");
            return exports.MixedSix(...args);
        },
        mixedSixAsync: (...args) => exports.MixedSixAsync(...args),
        mixedSixVoid: (...args) => {
            check(exports.MixedSixVoid(...args) === undefined, "Multi-argument void returned a value");
        },
        sixStrings: (...args) => exports.SixStrings(...args),
        nestedMultiple: (first, depth, last) => exports.NestedMultiple(first, depth, last),
        overLimit: (...args) => exports.OverLimit(...args),
        multiContracts: value => {
            check(exports.MixedSix() === "<null>|0|0|false|0|<null>", "Missing argument defaults changed");
            check(exports.MixedSix("a", 1, 2.5, true, 1.25, "z", "ignored") === "a|1|2.5|true|1.25|z", "Extra arguments changed signature");
            for (const [args, message] of [
                [["a", "wrong", 2.5, true, 1.25, "z"], worker ? "Int32" : "Value is not an integer"],
                [["a", 1, 2.5, "wrong", 1.25, "z"], "Boolean"],
                [["a", 1, 2.5, true, 1.25, 42], "String"],
            ]) {
                let error;
                try {
                    exports.MixedSix(...args);
                } catch (caught) {
                    error = caught;
                }
                check(error?.message.includes(message), "Multi-argument validation did not report the wrong argument");
                error.dispose?.();
            }
            if (worker) {
                let error;
                try {
                    exports.OverLimit(1, 2, 3, 4, 5, 6, 7);
                } catch (caught) {
                    error = caught;
                }
                check(error?.message.includes("at most 6"), "Argument limit must fail explicitly");
            } else {
                check(exports.OverLimit(1, 2, 3, 4, 5, 6, 7) === 28, "Ordinary argument support changed");
            }
            return value;
        },
        primitiveContracts: value => {
            for (const [name, defaultValue, message] of [
                ["EchoBoolean", false, "Boolean"], ["EchoDouble", 0, "Number"], ["EchoSingle", 0, "Number"]
            ]) {
                if (!Object.is(exports[name](null), defaultValue) || !Object.is(exports[name](undefined), defaultValue)) {
                    throw new Error("Primitive null/default conversion changed");
                }
                try {
                    exports[name]("wrong");
                    throw new Error("Expected primitive input type validation");
                } catch (error) {
                    if (!error.message.includes(message)) throw error;
                    error.dispose?.();
                }
            }
            const halfway = 1 + 2 ** -24;
            if (!Object.is(exports.EchoSingle(halfway), Math.fround(halfway))
                || !Object.is(exports.EchoSingle(-Number.MIN_VALUE), -0)) {
                throw new Error("Single rounding or signed-zero conversion changed");
            }
            return value;
        },
        mode: () => {
            if (typeof document !== "object" || document.defaultView !== globalThis) {
                throw new Error("The registered import must execute on the UI thread");
            }
            const documentPresent = exports.HasDocument(0);
            if (documentPresent !== (worker ? 0 : 1)) {
                throw new Error("Managed execution is on the wrong thread");
            }
            return worker ? 1 : 0;
        },
    });
    // eslint-disable-next-line no-console
    console.log(`WorkerInteropTest: ${worker ? "worker" : "ordinary"} imports registered`);
}
