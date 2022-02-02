class TimersHelper {
    log(message) {
        // uncomment for debugging
        // console.log(message);
    }
    install() {
        const measuredCallbackName = "mono_wasm_set_timeout_exec";
        globalThis.registerCount = 0;
        globalThis.hitCount = 0;
        this.log("install")
        if (!globalThis.originalSetTimeout) {
            globalThis.originalSetTimeout = globalThis.setTimeout;
        }
        globalThis.setTimeout = (cb, time) => {
            var start = Date.now().valueOf();
            if (cb.name === measuredCallbackName) {
                globalThis.registerCount++;
                this.log(`registerCount: ${globalThis.registerCount} now:${start} delay:${time}`)
            }
            return globalThis.originalSetTimeout(() => {
                if (cb.name === measuredCallbackName) {
                    var hit = Date.now().valueOf();
                    globalThis.hitCount++;
                    this.log(`hitCount: ${globalThis.hitCount} now:${hit} delay:${time} delta:${hit - start}`)
                }
                cb();
            }, time);
        };
    }

    getRegisterCount() {
        this.log(`registerCount: ${globalThis.registerCount} `)
        return globalThis.registerCount;
    }

    getHitCount() {
        this.log(`hitCount: ${globalThis.hitCount} `)
        return globalThis.hitCount;
    }

    cleanup() {
        this.log(`cleanup registerCount: ${globalThis.registerCount} hitCount: ${globalThis.hitCount} `)
        globalThis.setTimeout = globalThis.originalSetTimeout;
    }
}

globalThis.timersHelper = new TimersHelper();
