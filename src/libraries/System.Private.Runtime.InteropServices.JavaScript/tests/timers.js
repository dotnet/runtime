class TimersHelper {
    install() {
        const measuredCallbackName = "mono_wasm_set_timeout_exec";
        globalThis.registerCount = 0;
        globalThis.hitCount = 0;
        console.log("install")
        if (!globalThis.originalSetTimeout) {
            globalThis.originalSetTimeout = globalThis.setTimeout;
        }
        globalThis.setTimeout = (cb, time) => {
            var start = Date.now().valueOf();
            if (cb.name === measuredCallbackName) {
                globalThis.registerCount++;
                console.log(`registerCount: ${globalThis.registerCount} now:${start} delay:${time}`)
            }
            return globalThis.originalSetTimeout(() => {
                if (cb.name === measuredCallbackName) {
                    var hit = Date.now().valueOf();
                    globalThis.hitCount++;
                    var delta = hit - start;
                    console.log(`hitCount: ${globalThis.hitCount} now:${hit} delay:${time} delta:${delta}`)
                }
                cb();
            }, time);
        };
    }

    getRegisterCount() {
        console.log(`registerCount: ${globalThis.registerCount} `)
        return globalThis.registerCount;
    }

    getHitCount() {
        console.log(`hitCount: ${globalThis.hitCount} `)
        return globalThis.hitCount;
    }

    cleanup() {
        console.log(`cleanup registerCount: ${globalThis.registerCount} hitCount: ${globalThis.hitCount} `)
        globalThis.setTimeout = globalThis.originalSetTimeout;
    }
}

globalThis.timersHelper = new TimersHelper();
