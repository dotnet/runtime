import { ENVIRONMENT_IS_NODE, Module, requirePromise } from "./imports";

let node_fs: any | undefined = undefined;
let node_url: any | undefined = undefined;

export async function fetch_like(url: string): Promise<Response> {
    try {
        if (ENVIRONMENT_IS_NODE) {
            if (!node_fs) {
                const node_require = await requirePromise;
                node_url = node_require("url");
                node_fs = node_require("fs");
            }
            if (url.startsWith("file://")) {
                url = node_url.fileURLToPath(url);
            }

            const arrayBuffer = await node_fs.promises.readFile(url);
            return <Response><any>{
                ok: true,
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(arrayBuffer)
            };
        }
        else if (typeof (globalThis.fetch) === "function") {
            return globalThis.fetch(url, { credentials: "same-origin" });
        }
        else if (typeof (read) === "function") {
            // note that it can't open files with unicode names, like Straße.xml
            // https://bugs.chromium.org/p/v8/issues/detail?id=12541
            const arrayBuffer = new Uint8Array(read(url, "binary"));
            return <Response><any>{
                ok: true,
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(Module.UTF8ArrayToString(arrayBuffer, 0, arrayBuffer.length))
            };
        }
    }
    catch (e: any) {
        return <Response><any>{
            ok: false,
            url,
            arrayBuffer: () => { throw e; },
            json: () => { throw e; }
        };
    }
    throw new Error("No fetch implementation available");
}

export function readAsync_like(url: string, onload: Function, onerror: Function): void {
    fetch_like(url).then((res: Response) => {
        onload(res.arrayBuffer());
    }).catch((err) => {
        onerror(err);
    });
}