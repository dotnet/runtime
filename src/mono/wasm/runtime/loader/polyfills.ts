
import type { DotnetModuleInternal } from "../types/internal";
import { INTERNAL, ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, loaderHelpers, ENVIRONMENT_IS_WEB } from "./globals";

let node_fs: any | undefined = undefined;
let node_url: any | undefined = undefined;

export async function detect_features_and_polyfill(module: DotnetModuleInternal): Promise<void> {

    loaderHelpers.scriptUrl = normalizeFileUrl(/* webpackIgnore: true */import.meta.url);
    loaderHelpers.scriptDirectory = normalizeDirectoryUrl(loaderHelpers.scriptUrl);
    loaderHelpers.locateFile = (path) => {
        if (isPathAbsolute(path)) return path;
        return loaderHelpers.scriptDirectory + path;
    };
    loaderHelpers.downloadResource = module.downloadResource;
    loaderHelpers.fetch_like = fetch_like;
    // eslint-disable-next-line no-console
    loaderHelpers.out = console.log;
    // eslint-disable-next-line no-console
    loaderHelpers.err = console.error;
    loaderHelpers.getApplicationEnvironment = module.getApplicationEnvironment;

    if (ENVIRONMENT_IS_WEB && globalThis.navigator) {
        const navigator: any = globalThis.navigator;
        const brands = navigator.userAgentData && navigator.userAgentData.brands;
        if (brands && brands.length > 0) {
            loaderHelpers.isChromium = brands.some((b: any) => b.brand === "Google Chrome" || b.brand === "Microsoft Edge" || b.brand === "Chromium");
        }
        else if (navigator.userAgent) {
            loaderHelpers.isChromium = navigator.userAgent.includes("Chrome");
            loaderHelpers.isFirefox = navigator.userAgent.includes("Firefox");
        }
    }

    if (ENVIRONMENT_IS_NODE) {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        INTERNAL.require = await import(/* webpackIgnore: true */"module").then(mod => mod.createRequire(/* webpackIgnore: true */import.meta.url));
    } else {
        INTERNAL.require = Promise.resolve(() => { throw new Error("require not supported"); });
    }

    if (typeof globalThis.URL === "undefined") {
        globalThis.URL = class URL {
            private url;
            constructor(url: string) {
                this.url = url;
            }
            toString() {
                return this.url;
            }
        } as any;
    }
}

const hasFetch = typeof (globalThis.fetch) === "function";
export async function fetch_like(url: string, init?: RequestInit): Promise<Response> {
    try {
        if (ENVIRONMENT_IS_NODE) {
            const isFileUrl = url.startsWith("file://");
            if (!isFileUrl && hasFetch) {
                return globalThis.fetch(url, init || { credentials: "same-origin" });
            }
            if (!node_fs) {
                node_url = INTERNAL.require("url");
                node_fs = INTERNAL.require("fs");
            }
            if (isFileUrl) {
                url = node_url.fileURLToPath(url);
            }

            const arrayBuffer = await node_fs.promises.readFile(url);
            return <Response><any>{
                ok: true,
                headers: {
                    length: 0,
                    get: () => null
                },
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(arrayBuffer),
                text: () => { throw new Error("NotImplementedException"); }
            };
        }
        else if (hasFetch) {
            return globalThis.fetch(url, init || { credentials: "same-origin" });
        }
        else if (typeof (read) === "function") {
            // note that it can't open files with unicode names, like Stra<unicode char - Latin Small Letter Sharp S>e.xml
            // https://bugs.chromium.org/p/v8/issues/detail?id=12541
            return <Response><any>{
                ok: true,
                url,
                headers: {
                    length: 0,
                    get: () => null
                },
                arrayBuffer: () => {
                    return new Uint8Array(read(url, "binary"));
                },
                json: () => {
                    return JSON.parse(read(url, "utf8"));
                },
                text: () => read(url, "utf8")
            };
        }
    }
    catch (e: any) {
        return <Response><any>{
            ok: false,
            url,
            status: 500,
            headers: {
                length: 0,
                get: () => null
            },
            statusText: "ERR28: " + e,
            arrayBuffer: () => { throw e; },
            json: () => { throw e; },
            text: () => { throw e; }
        };
    }
    throw new Error("No fetch implementation available");
}

function normalizeFileUrl(filename: string) {
    // unix vs windows
    // remove query string
    return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
}

function normalizeDirectoryUrl(dir: string) {
    return dir.slice(0, dir.lastIndexOf("/")) + "/";
}

const protocolRx = /^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//;
const windowsAbsoluteRx = /[a-zA-Z]:[\\/]/;
function isPathAbsolute(path: string): boolean {
    if (ENVIRONMENT_IS_NODE || ENVIRONMENT_IS_SHELL) {
        // unix /x.json
        // windows \x.json
        // windows C:\x.json
        // windows C:/x.json
        return path.startsWith("/") || path.startsWith("\\") || path.indexOf("///") !== -1 || windowsAbsoluteRx.test(path);
    }

    // anything with protocol is always absolute
    // windows file:///C:/x.json
    // windows http://C:/x.json
    return protocolRx.test(path);
}
