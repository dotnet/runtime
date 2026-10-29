// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export async function setup(runtime) {
    const exports = (await runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests"))
        .System.Runtime.InteropServices.JavaScript.Tests.WorkerInteropTest;
    const worker = runtime.getConfig().workerOptions !== undefined;
    const voidEvents = [];
    let lastView;
    function check(condition, message) {
        if (!condition) throw new Error(message);
    }
    runtime.setModuleImports("WorkerInteropTest", {
        newObject: () => ({ value: 42 }),
        callFunction: (fn, value) => fn(value),
        callAction: action => action(),
        echoTask: async value => await value,
        editView: (view, fail) => {
            lastView = view;
            const copy = view.slice();
            copy[0] = 9;
            view.set(copy);
            const target = new copy.constructor(copy.length);
            view.copyTo(target);
            check(target[0] === 9 && target[1] === 2, "Memory view copyTo did not update the caller array");
            if (fail) throw new Error("view failure");
            return view;
        },
        viewDisposed: () => {
            check(lastView.isDisposed, "Span outlived its import scope");
            let failed = false;
            try {
                lastView.slice();
            } catch (error) {
                failed = error.message.includes("ObjectDisposedException");
            }
            check(failed, "Disposed Span remains accessible");
            return true;
        },
        disposeView: () => {
            lastView.dispose();
            check(lastView.isDisposed, "ArraySegment dispose did not release its owner");
            let failed = false;
            try {
                lastView.slice();
            } catch (error) {
                failed = error.message.includes("ObjectDisposedException");
            }
            check(failed, "Disposed ArraySegment remains accessible");
            return true;
        },
        disposeManaged: value => {
            value.dispose();
            return value.isDisposed;
        },
        discardContracts: () => {
            voidEvents.length = 0;
            check(exports.DiscardExport("discard") === undefined, "Discard export returned a value");
            check(exports.DiscardNoWaitExport("discard-no-wait") === undefined, "DiscardNoWait export returned a value");
            check(voidEvents.join(",") === "discard,discard-no-wait", "Discard ST prefix changed");
            return 42;
        },
        remainingContracts: async () => {
            for (const pointer of [0, 0x7fffffff, 0x80000000, 0xffffffff]) {
                check(exports.EchoPointer(pointer) === pointer, "IntPtr lost unsigned Wasm32 bits");
            }
            for (const value of [null, 0, 42, -42]) check(exports.EchoNullable(value) === value, "Nullable Int32 changed");
            for (const value of [null, false, true]) check(exports.EchoNullableBoolean(value) === value, "Nullable Boolean changed");
            for (const value of [null, -0, NaN, Infinity, -1.25]) check(Object.is(exports.EchoNullableDouble(value), value), "Nullable Double changed");
            for (const value of [null, -(2n ** 63n), 2n ** 63n - 1n]) check(exports.EchoNullableBigInt(value) === value, "Nullable BigInt changed");
            for (const milliseconds of [-62135596800000, 0, 253402300799999]) {
                const date = new Date(milliseconds);
                check(exports.EchoDate(date).getTime() === milliseconds, "DateTime changed");
                check(exports.EchoOffset(date).getTime() === milliseconds, "DateTimeOffset changed");
                check(exports.EchoNullableDate(date).getTime() === milliseconds, "Nullable DateTime changed");
            }
            check(exports.EchoNullableDate(null) === null, "Nullable DateTime null changed");
            let invalidDate;
            try {
                exports.EchoDate(new Date(NaN));
            } catch (error) {
                invalidDate = error;
            }
            check(invalidDate instanceof RangeError, `Invalid Date changed the native error subtype: ${invalidDate?.name}: ${invalidDate?.message}`);
            for (const [name, value] of [
                ["EchoBytes", new Uint8Array([0, 255, 42])], ["EchoIntegers", new Int32Array([-2147483648, 0, 2147483647])],
                ["EchoSingles", new Float32Array([-0, NaN, Infinity])], ["EchoDoubles", new Float64Array([-0, NaN, Infinity])],
                ["EchoStrings", [null, "", "text\0\u0130\u{1f600}"]],
            ]) {
                const result = exports[name](value);
                check(result.length === value.length && Array.from(result).every((item, index) => Object.is(item, value[index])), name + " changed elements");
                check(exports[name](null) === null, name + " null changed");
            }
            const object = { value: 42, nested: { value: 43 }, get _pointer() {
                throw new Error("Opaque JSObject getters must not be probed as memory-view metadata");
            } };
            const error = new Error("UI reference identity");
            check(exports.EchoJSObject(object) === object, "JSObject identity changed");
            check(exports.EchoObject(object) === object, "Object identity changed");
            check(exports.EchoException(error) === error, "Exception identity changed");
            const managedError = exports.CreateException();
            for (const exception of [error, managedError]) {
                for (const invoke of [
                    () => exports.ThrowException(exception),
                    () => exports.ThrowExceptionAsync(exception),
                ]) {
                    let caught;
                    try {
                        await invoke();
                    } catch (failure) {
                        caught = failure;
                    }
                    check(caught === exception, "Thrown exception identity changed");
                    check(caught.stack.length > 0, "Thrown exception lost its stack");
                }
            }
            managedError.dispose();
            check(exports.ApplyFunction(value => exports.Echo(value + 1), 42) === 43, "Nested callback failed");
            let actions = 0;
            exports.ApplyAction(() => actions++);
            check(actions === 1, "Action failed");
            check(await exports.AwaitTask(Promise.resolve(42)) === 42, "Task argument failed");
            const taskError = new Error("UI Task argument failure");
            const callbackError = new Error("UI callback failure");
            for (const [invoke, expected] of [
                [() => exports.AwaitTask(Promise.reject(taskError)), taskError],
                [() => exports.ApplyFunction(() => {
                    throw callbackError;
                }, 0), callbackError],
            ]) {
                let failed = false;
                try {
                    await invoke();
                } catch (error) {
                    failed = error === expected;
                    error.dispose?.();
                }
                check(failed, "Task/callback failure did not cross the bridge");
            }
            const tasks = Array.from({ length: 70 }, (_, index) => exports.EchoArrayAsync(new Int32Array([index, -index])));
            const results = await Promise.all(tasks);
            check(results.every((value, index) => value[0] === index && value[1] === -index), "Concurrent array Tasks changed");
            check(await exports.EchoObjectAsync(object) === object, "Task<Object> identity changed");
            return 42;
        },
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
        echoInteger: value => value,
        echoIntegerAsync: async value => value,
        invokeByte: value => exports.EchoByte(value),
        invokeByteAsync: value => exports.EchoByteAsync(value),
        invokeChar: value => exports.EchoChar(value),
        invokeCharAsync: value => exports.EchoCharAsync(value),
        invokeInt16: value => exports.EchoInt16(value),
        invokeInt16Async: value => exports.EchoInt16Async(value),
        invokeInt52: value => exports.EchoInt52(value),
        invokeInt52Async: value => exports.EchoInt52Async(value),
        invokeBigInt64: value => exports.EchoBigInt64(value),
        invokeBigInt64Async: value => exports.EchoBigInt64Async(value),
        mixedIntegers: (...args) => exports.MixedIntegers(...args),
        mixedIntegersAsync: async (...args) => exports.MixedIntegers(...args),
        invalidInteger: kind => [256, 65536, 32768, 2 ** 53, 1n << 63n][kind],
        integerContracts: value => {
            for (const [name, invalid, wrongType] of [
                ["Byte", [-1, 256], "1"], ["Char", [-1, 65536], "a"],
                ["Int16", [-32769, 32768], "1"], ["Int52", [-(2 ** 53), 2 ** 53], "1"],
                ["BigInt64", [-(1n << 63n) - 1n, 1n << 63n], 1]
            ]) {
                const fn = exports["Echo" + name];
                const defaultValue = name === "BigInt64" ? 0n : 0;
                check(fn(null) === defaultValue && fn(undefined) === defaultValue, "Integer null/default conversion changed");
                for (const input of [...invalid, wrongType]) {
                    let error;
                    try {
                        fn(input);
                    } catch (caught) {
                        error = caught;
                    }
                    check(error?.message, "Invalid integer was not rejected");
                    if (worker) check(error.message.includes(name), "Integer validation reported the wrong type");
                    error.dispose?.();
                }
            }
            return value;
        },
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
