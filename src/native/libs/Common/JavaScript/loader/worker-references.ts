// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { MemoryViewManager } from "./shared-signaling";

type Side = "ui" | "worker";
type WireValue = null | boolean | string | number | WireValue[];
type Reference = { owner: Side, id: number, lease: number };
type ReferenceEntry = { value: object, leases: Set<number> };
export type WorkerReferenceMessage =
    | { type: "reference-release", id: number, lease: number }
    | { type: "reference-settlement", id: number, rejected: boolean, value: string };

const transports = new WeakMap<MemoryViewManager, WorkerReferenceTransport>();

export function isWorkerNativePromise(value: object): boolean {
    try {
        Reflect.apply(Promise.prototype.then, value, [() => undefined, () => undefined]);
        return true;
    } catch (error) {
        if (error instanceof TypeError) return false;
        throw error;
    }
}

export function workerReferences(memory: MemoryViewManager): WorkerReferenceTransport {
    let transport = transports.get(memory);
    if (!transport) {
        transport = new WorkerReferenceTransport(memory);
        transports.set(memory, transport);
    }
    return transport;
}

class WorkerReferenceTransport {
    private side?: Side;
    private invoke?: (request: string) => string;
    private post?: (message: WorkerReferenceMessage) => void;
    private stopped?: Error;
    private nextId = 1;
    private nextLease = 1;
    private readonly owned = new Map<number, ReferenceEntry>();
    private readonly identities = new WeakMap<object, number>();
    private readonly memoryViews = new WeakSet<object>();
    private readonly remote = new WeakMap<object, Reference>();
    private readonly proxies = new Map<number, WeakRef<object>>();
    private readonly promises = new Map<number, { resolve:(value: unknown) => void, reject: (error: unknown) => void }>();
    private readonly outgoingPromises = new Set<number>();
    private readonly finalizers = new FinalizationRegistry<Reference>(reference => {
        if (!this.stopped) {
            if (!this.proxies.get(reference.id)?.deref()) this.proxies.delete(reference.id);
            this.send({ type: "reference-release", id: reference.id, lease: reference.lease });
        }
    });

    constructor(private readonly memory: MemoryViewManager) { }

    configure(side: Side, invoke: (request: string) => string, post: (message: WorkerReferenceMessage) => void): void {
        this.side = side;
        this.invoke = invoke;
        this.post = post;
    }

    private send(message: WorkerReferenceMessage): void {
        if (this.stopped) throw this.stopped;
        if (!this.post) throw new Error("withWorker(): reference transport is not configured");
        this.post(message);
    }

    encode(value: unknown, byReference = false, memoryView = false): string {
        if (this.stopped) throw this.stopped;
        if (memoryView && value !== null && typeof value === "object") this.memoryViews.add(value);
        const leases: Reference[] = [];
        try {
            return JSON.stringify(this.pack(value, new Set(), leases, byReference));
        } catch (error) {
            for (const reference of leases) {
                if (reference.lease) this.release(reference.id, reference.lease);
                else this.outgoingPromises.delete(reference.id);
            }
            throw error;
        }
    }

    cancelEncoding(text: string): void {
        const cancel = (value: WireValue): void => {
            if (!Array.isArray(value)) return;
            if (value[0] === "reference" && value[1] === this.side && typeof value[2] === "number" && typeof value[3] === "number") {
                this.release(value[2], value[3]);
            } else if (value[0] === "promise" && typeof value[1] === "number") {
                this.outgoingPromises.delete(value[1]);
            } else if (value[0] === "array" && Array.isArray(value[1])) value[1].forEach(cancel);
        };
        cancel(JSON.parse(text));
    }

    cancelResponse(text: string): void {
        const response = JSON.parse(text);
        if (!Array.isArray(response) || typeof response[1] !== "string") throw new Error("withWorker(): invalid reference response");
        this.cancelEncoding(response[1]);
    }

    decode(text: string): unknown {
        if (this.stopped) throw this.stopped;
        return this.unpack(JSON.parse(text));
    }

    private pack(value: unknown, ancestors: Set<object>, leases: Reference[], byReference = false): WireValue {
        if (value === null || typeof value === "boolean" || typeof value === "string") return value;
        if (value === undefined) return ["undefined"];
        if (typeof value === "number") {
            return Number.isFinite(value) && !Object.is(value, -0) ? value : ["number", Object.is(value, -0) ? "-0" : String(value)];
        }
        if (typeof value === "bigint") return ["bigint", String(value)];
        if (typeof value !== "object" && typeof value !== "function") throw new Error("withWorker(): unsupported JavaScript value");
        const existing = this.remote.get(value);
        if (existing) return ["reference", existing.owner, existing.id, 0, "object"];
        if (!byReference && value instanceof Date) return ["date", this.pack(value.getTime(), ancestors, leases)];
        if (!byReference && ArrayBuffer.isView(value)) {
            if (value instanceof DataView) throw new Error("withWorker(): DataView is not a marshaler value");
            const type = value instanceof Uint8Array ? "Uint8Array" : value instanceof Int32Array ? "Int32Array"
                : value instanceof Float32Array ? "Float32Array" : value instanceof Float64Array ? "Float64Array" : undefined;
            if (!type) throw new Error("withWorker(): unsupported native typed array");
            return ["typed", type, Array.from(new Uint8Array(value.buffer, value.byteOffset, value.byteLength))];
        }
        if (!byReference && Array.isArray(value)) {
            if (ancestors.has(value)) throw new Error("withWorker(): cyclic arrays are not supported by the native marshaler");
            ancestors.add(value);
            try {
                return ["array", value.map(item => this.pack(item, ancestors, leases))];
            } finally {
                ancestors.delete(value);
            }
        }
        if (!byReference && typeof value === "object" && typeof Reflect.get(value, "then") === "function") {
            if (!isWorkerNativePromise(value)) throw new Error("withWorker(): custom thenables are not supported");
            const id = this.nextId++;
            this.outgoingPromises.add(id);
            leases.push({ owner: this.side ?? "ui", id, lease: 0 });
            const settle = (result: unknown, rejected: boolean) => {
                if (!this.outgoingPromises.delete(id) || this.stopped) return;
                let encoded: string;
                try {
                    encoded = this.encode(result);
                } catch (error) {
                    rejected = true;
                    encoded = this.encode(error);
                }
                this.send({ type: "reference-settlement", id, rejected, value: encoded });
            };
            Reflect.apply(Promise.prototype.then, value, [
                (result: unknown) => settle(result, false),
                (error: unknown) => settle(error, true),
            ]);
            return ["promise", id];
        }
        if (!this.side) throw new Error("withWorker(): reference transport is not configured");
        let id = this.identities.get(value);
        if (id === undefined) {
            id = this.nextId++;
            this.identities.set(value, id);
        }
        let entry = this.owned.get(id);
        if (!entry) {
            entry = { value, leases: new Set() };
            this.owned.set(id, entry);
        }
        const lease = this.nextLease++;
        entry.leases.add(lease);
        leases.push({ owner: this.side, id, lease });
        const kind = typeof value === "function" ? "function" : value instanceof TypeError ? "TypeError"
            : value instanceof RangeError ? "RangeError" : value instanceof ReferenceError ? "ReferenceError"
                : value instanceof SyntaxError ? "SyntaxError" : value instanceof EvalError ? "EvalError"
                    : value instanceof URIError ? "URIError" : value instanceof AggregateError ? "AggregateError" : value instanceof Error ? "error"
                        : Array.isArray(value) ? "array" : value instanceof Date ? "date"
                            : ArrayBuffer.isView(value) ? value.constructor.name : "object";
        const reference: WireValue[] = ["reference", this.side, id, lease, kind];
        if (this.memoryViews.has(value)) {
            const view = Reflect.get(value, "_viewType");
            const pointer = Reflect.get(value, "_pointer");
            const length = Reflect.get(value, "_length");
            if (!Number.isInteger(view) || !Number.isInteger(pointer) || !Number.isInteger(length)) {
                throw new Error("withWorker(): invalid native memory view metadata");
            }
            reference.push(view, pointer, length);
        }
        return reference;
    }

    private unpack(value: WireValue): unknown {
        if (!Array.isArray(value)) return value;
        const [tag, data] = value;
        switch (tag) {
            case "undefined": return undefined;
            case "number": return data === "-0" ? -0 : Number(data);
            case "bigint": return BigInt(String(data));
            case "date": return new Date(Number(this.unpack(data)));
            case "array":
                if (!Array.isArray(data)) throw new Error("withWorker(): invalid array payload");
                return data.map(item => this.unpack(item));
            case "typed": {
                const bytes = value[2];
                if (!Array.isArray(bytes) || !bytes.every(byte => typeof byte === "number")) throw new Error("withWorker(): invalid typed array payload");
                const buffer = new Uint8Array(bytes).buffer;
                switch (data) {
                    case "Uint8Array": return new Uint8Array(buffer);
                    case "Int32Array": return new Int32Array(buffer);
                    case "Float32Array": return new Float32Array(buffer);
                    case "Float64Array": return new Float64Array(buffer);
                    default: throw new Error("withWorker(): unsupported native typed array");
                }
            }
            case "promise": {
                if (typeof data !== "number" || this.promises.has(data)) throw new Error("withWorker(): invalid Promise identity");
                return new Promise((resolve, reject) => this.promises.set(data, { resolve, reject }));
            }
            case "reference": return this.reference(value);
            default: throw new Error("withWorker(): invalid value payload");
        }
    }

    private reference(value: WireValue[]): object {
        const [, owner, id, lease, kind, view, pointer, length] = value;
        if ((owner !== "ui" && owner !== "worker") || typeof id !== "number" || typeof lease !== "number") {
            throw new Error("withWorker(): invalid reference payload");
        }
        if (owner === this.side) {
            const entry = this.owned.get(id);
            if (!entry) throw new Error("withWorker(): reference has been released");
            return entry.value;
        }
        const cached = this.proxies.get(id)?.deref();
        if (cached) {
            this.send({ type: "reference-release", id, lease });
            return cached;
        }
        const target = kind === "function" ? () => undefined : kind === "error" ? new Error()
            : kind === "TypeError" ? new TypeError() : kind === "RangeError" ? new RangeError()
                : kind === "ReferenceError" ? new ReferenceError() : kind === "SyntaxError" ? new SyntaxError()
                    : kind === "EvalError" ? new EvalError() : kind === "URIError" ? new URIError() : kind === "AggregateError" ? new AggregateError([])
                        : kind === "array" ? [] : kind === "date" ? new Date(0)
                            : kind === "Uint8Array" ? new Uint8Array(0) : kind === "Int32Array" ? new Int32Array(0)
                                : kind === "Float32Array" ? new Float32Array(0) : kind === "Float64Array" ? new Float64Array(0) : {};
        const proxy = new Proxy(target, {
            get: (target, key) => {
                if (typeof key === "symbol") return Reflect.get(target, key);
                if (typeof view === "number" && typeof pointer === "number" && typeof length === "number") {
                    const memoryView = this.memoryView(id, view, pointer, length);
                    if (key in memoryView) return Reflect.get(memoryView, key);
                }
                return this.call(["get", id, key]);
            },
            set: (target, key, item) => {
                if (typeof key === "symbol") return Reflect.set(target, key, item);
                return this.call(["set", id, key, this.encode(item)]) === true;
            },
            has: (target, key) => typeof key === "symbol" ? Reflect.has(target, key) : this.call(["has", id, key]) === true,
            deleteProperty: (target, key) => typeof key === "symbol" ? Reflect.deleteProperty(target, key)
                : this.call(["delete", id, key]) === true,
            apply: (_target, receiver, args) => {
                const encodedReceiver = this.encode(receiver);
                let encodedArgs: string;
                try {
                    encodedArgs = this.encode(args);
                } catch (error) {
                    this.cancelEncoding(encodedReceiver);
                    throw error;
                }
                return this.call(["apply", id, encodedReceiver, encodedArgs]);
            },
            ownKeys: () => {
                const keys = this.call(["keys", id]);
                if (!Array.isArray(keys) || !keys.every(key => typeof key === "string")) throw new Error("withWorker(): invalid property keys");
                return [...new Set([...keys, ...Reflect.ownKeys(target).filter(key => !Reflect.getOwnPropertyDescriptor(target, key)?.configurable)])];
            },
            getOwnPropertyDescriptor: (target, key) => {
                const local = Reflect.getOwnPropertyDescriptor(target, key);
                return typeof key === "symbol" || local?.configurable === false ? local
                    : this.call(["own", id, key]) ? { configurable: true, enumerable: true, writable: true } : undefined;
            },
        });
        const reference: Reference = { owner, id, lease };
        this.remote.set(proxy, reference);
        this.proxies.set(id, new WeakRef(proxy));
        this.finalizers.register(proxy, reference);
        return proxy;
    }

    private memoryView(id: number, type: number, pointer: number, length: number) {
        const check = () => {
            if (this.call(["get", id, "isDisposed"])) throw new Error("ObjectDisposedException");
            const buffer = this.memory.refreshIfNeeded().u8.buffer;
            switch (type) {
                case 0: return new Uint8Array(buffer, pointer, length);
                case 1: return new Int32Array(buffer, pointer, length);
                case 2: return new Float64Array(buffer, pointer, length);
                case 3: return new Float32Array(buffer, pointer, length);
                default: throw new Error("withWorker(): invalid memory view element type");
            }
        };
        const byteLength = () => {
            check();
            const result = this.call(["get", id, "byteLength"]);
            if (typeof result !== "number") throw new Error("withWorker(): invalid memory view byte length");
            return result;
        };
        return {
            get length() {
                return check().length;
            },
            get byteLength() {
                return byteLength();
            },
            slice: (start?: number, end?: number) => check().slice(start || 0, end ? end >>> 0 : undefined),
            set: (source: Uint8Array | Int32Array | Float32Array | Float64Array, offset = 0) => {
                const target = check();
                if (target.constructor !== source.constructor) throw new Error("withWorker(): memory view element type mismatch");
                target.set(source, offset);
            },
            copyTo: (target: Uint8Array | Int32Array | Float32Array | Float64Array, offset = 0) => {
                const source = check();
                if (target.constructor !== source.constructor) throw new Error("withWorker(): memory view element type mismatch");
                target.set(source.subarray(offset));
            },
        };
    }

    private call(request: WireValue[]): unknown {
        if (this.stopped) throw this.stopped;
        if (!this.invoke) throw new Error("withWorker(): reference transport is not configured");
        let text: string;
        try {
            text = this.invoke(JSON.stringify(request));
        } catch (error) {
            if (request[0] === "apply" && typeof request[2] === "string") this.cancelEncoding(request[2]);
            if ((request[0] === "apply" || request[0] === "set") && typeof request[3] === "string") this.cancelEncoding(request[3]);
            throw error;
        }
        const response = JSON.parse(text);
        if (!Array.isArray(response) || typeof response[0] !== "boolean" || typeof response[1] !== "string") {
            throw new Error("withWorker(): invalid reference response");
        }
        const result = this.decode(response[1]);
        if (!response[0]) throw result;
        return result;
    }

    dispatch(request: string): string {
        try {
            const [operation, id, key, payload] = JSON.parse(request);
            const target = this.owned.get(id)?.value;
            if (!target) throw new Error("withWorker(): reference has been released");
            let result: unknown;
            switch (operation) {
                case "get": result = Reflect.get(target, key); break;
                case "set": result = Reflect.set(target, key, this.decode(payload)); break;
                case "apply": {
                    if (typeof target !== "function") throw new Error("withWorker(): reference is not callable");
                    const args = this.decode(payload);
                    if (!Array.isArray(args)) throw new Error("withWorker(): invalid callback arguments");
                    result = Reflect.apply(target, this.decode(key), args);
                    break;
                }
                case "keys": result = Object.keys(target); break;
                case "has": result = Reflect.has(target, key); break;
                case "own": result = Object.prototype.hasOwnProperty.call(target, key); break;
                case "delete": result = Reflect.deleteProperty(target, key); break;
                default: throw new Error("withWorker(): invalid reference operation");
            }
            return JSON.stringify([true, this.encode(result, operation === "get")]);
        } catch (error) {
            return JSON.stringify([false, this.encode(error)]);
        }
    }

    receive(message: WorkerReferenceMessage): void {
        if (this.stopped) return;
        if (message.type === "reference-release") this.release(message.id, message.lease);
        else {
            const promise = this.promises.get(message.id);
            if (!promise) throw new Error("withWorker(): unknown Promise completion");
            this.promises.delete(message.id);
            try {
                const value = this.decode(message.value);
                if (message.rejected) promise.reject(value);
                else promise.resolve(value);
            } catch (error) {
                promise.reject(error);
            }
        }
    }

    private release(id: number, lease: number): void {
        const entry = this.owned.get(id);
        if (entry?.leases.delete(lease) && entry.leases.size === 0) this.owned.delete(id);
    }

    stop(error: Error): void {
        this.stopped = error;
        for (const promise of this.promises.values()) promise.reject(error);
        this.promises.clear();
        this.outgoingPromises.clear();
        this.owned.clear();
        this.proxies.clear();
    }
}
