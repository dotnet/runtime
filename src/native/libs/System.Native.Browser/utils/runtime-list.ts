// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { RuntimeAPI } from "./types";

let runtimeList: RuntimeList;

class RuntimeList {
    private list: { [runtimeId: number]: WeakRef<RuntimeAPI> } = {};

    public registerRuntime(api: RuntimeAPI): number {
        if (api.runtimeId === undefined) {
            api.runtimeId = Object.keys(this.list).length;
        }
        this.list[api.runtimeId] = new (globalThis as any).WeakRef(api);
        return api.runtimeId;
    }

    public getRuntime(runtimeId: number): RuntimeAPI | undefined {
        const wr = this.list[runtimeId];
        return wr ? wr.deref() : undefined;
    }
}

export function registerRuntime(api: RuntimeAPI): number {
    const globalThisAny = globalThis as any;
    // this code makes it possible to find dotnet runtime on a page via global namespace, even when there are multiple runtimes at the same time
    if (!globalThisAny.getDotnetRuntime) {
        globalThisAny.getDotnetRuntime = (runtimeId: string) => globalThisAny.getDotnetRuntime.__list.getRuntime(runtimeId);
        globalThisAny.getDotnetRuntime.__list = runtimeList = new RuntimeList();
    } else {
        runtimeList = globalThisAny.getDotnetRuntime.__list;
    }

    return runtimeList.registerRuntime(api);
}
