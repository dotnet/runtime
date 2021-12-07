import { ENVIRONMENT_IS_NODE, Module } from "./imports";

export async function fetch_like(url: string): Promise<Response> {
    try {
        if (typeof (globalThis.fetch) === "function") {
            return globalThis.fetch(url, { credentials: "same-origin" });
        }
        else if (ENVIRONMENT_IS_NODE) {
            const node_fs = Module.imports!.require!("fs");
            const node_url = Module.imports!.require!("url");
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
        else if (typeof (read) === "function") {
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