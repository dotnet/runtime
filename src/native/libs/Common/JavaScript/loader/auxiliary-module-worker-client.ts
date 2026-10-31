// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type AuxiliaryWorkerRequest = { id: number, url: string, options?: RequestInit, body?: ReadableStream<Uint8Array>, contentType?: string };
export type AuxiliaryWorkerResult = { id: number, status: number, statusText: string, module?: WebAssembly.Module, error?: string, errorName?: string };
type CompiledAuxiliaryDownload = { ok: true, status: number, statusText: string, module: WebAssembly.Module };
type PendingCompilation = { resolve: (value: Response | CompiledAuxiliaryDownload) => void, reject: (error: Error) => void };
const pending = new Map<number, PendingCompilation>();
let compiler: Worker | undefined;
let nextId = 0;

export function fetchAndCompileAuxiliaryModuleInWorker(workerUrl: string, url: string, options: RequestInit): Promise<Response | CompiledAuxiliaryDownload> {
    const baseUrl = globalThis.document?.baseURI ?? globalThis.location.href;
    return prepareModule(workerUrl, { id: nextId++, url: new URL(url, baseUrl).href, options });
}

export async function compileAuxiliaryResponseInWorker(workerUrl: string, response: Response): Promise<Response | CompiledAuxiliaryDownload> {
    if (!response.body) throw new Error("Auxiliary R2R module response has no body.");
    return prepareModule(workerUrl, { id: nextId++, url: response.url, body: response.body,
        contentType: response.headers.get("Content-Type") ?? "" });
}

function stopCompiler(): void {
    compiler?.terminate();
    compiler = undefined;
}

function failCompilations(error: Error): void {
    for (const operation of pending.values()) operation.reject(error);
    pending.clear();
    stopCompiler();
}

async function prepareModule(workerUrl: string, request: AuxiliaryWorkerRequest): Promise<Response | CompiledAuxiliaryDownload> {
    if (!compiler) {
        try {
            compiler = new Worker(workerUrl, { type: "module", name: "dotnet-auxiliary-compiler" });
        } catch (error) {
            stopCompiler();
            throw error;
        }
        compiler.onerror = event => {
            event.preventDefault();
            failCompilations(new Error(`Auxiliary compilation worker failed: ${event.message}`));
        };
        compiler.onmessageerror = () => failCompilations(new Error("Cannot deserialize auxiliary compilation worker result."));
        compiler.onmessage = (event: MessageEvent<AuxiliaryWorkerResult>) => {
            const result = event.data;
            const operation = pending.get(result.id);
            if (!operation) {
                failCompilations(new Error("Unexpected auxiliary compilation worker result."));
                return;
            }
            pending.delete(result.id);
            if (result.error !== undefined) {
                operation.reject(result.errorName === "CompileError"
                    ? new WebAssembly.CompileError(result.error) : new Error(result.error));
            } else if (result.module) {
                operation.resolve({ ok: true, status: result.status, statusText: result.statusText, module: result.module });
            } else {
                operation.resolve(new Response(null, { status: result.status, statusText: result.statusText }));
            }
            if (pending.size === 0) stopCompiler();
        };
    }
    return new Promise<Response | CompiledAuxiliaryDownload>((resolve, reject) => {
        pending.set(request.id, { resolve, reject });
        try {
            compiler!.postMessage(request, request.body ? [request.body] : []);
        } catch (error) {
            pending.delete(request.id);
            if (pending.size === 0) stopCompiler();
            reject(error);
        }
    });
}
