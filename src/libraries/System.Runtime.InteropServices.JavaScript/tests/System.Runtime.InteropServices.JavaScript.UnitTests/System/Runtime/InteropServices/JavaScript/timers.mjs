// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function log(message) {
    // uncomment for debugging
    // console.log(message);
}

export function install() {
    const Module = globalThis.App.runtime.Module;
    const measuredCallbackName = "mono_wasm_schedule_timer_tick";
    globalThis.registerCount = 0;
    globalThis.hitCount = 0;
    log("install")
    if (!globalThis.originalSetTimeout) {
        globalThis.originalSetTimeout = Module.safeSetTimeout;
    }

    Module.safeSetTimeout = (cb, time) => {
        var start = Date.now().valueOf();
        if (cb.name === measuredCallbackName) {
            globalThis.registerCount++;
            log(`registerCount: ${globalThis.registerCount} now:${start} delay:${time}`)
        }
        return globalThis.originalSetTimeout(() => {
            if (cb.name === measuredCallbackName) {
                var hit = Date.now().valueOf();
                globalThis.hitCount++;
                log(`hitCount: ${globalThis.hitCount} now:${hit} delay:${time} delta:${hit - start}`)
            }
            return cb();
        }, time);
    };
}

export function getRegisterCount() {
    log(`registerCount: ${globalThis.registerCount} `)
    return globalThis.registerCount;
}

export function getHitCount() {
    log(`hitCount: ${globalThis.hitCount} `)
    return globalThis.hitCount;
}

export function cleanup() {
    log(`cleanup registerCount: ${globalThis.registerCount} hitCount: ${globalThis.hitCount} `)
    const Module = globalThis.App.runtime.Module;
    Module.safeSetTimeout = globalThis.originalSetTimeout;
}
