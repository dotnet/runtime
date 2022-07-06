

export interface MockRemoteSocket extends EventTarget {
    addEventListener<T extends keyof WebSocketEventMap>(type: T, listener: (this: MockRemoteSocket, ev: WebSocketEventMap[T]) => any, options?: boolean | AddEventListenerOptions): void;
    addEventListener(event: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions): void;
    removeEventListener(event: string, listener: EventListenerOrEventListenerObject): void;
    send(data: string | ArrayBuffer | Uint8Array | Blob | DataView): void;
    close(): void;
}

export interface Mock {
    open(): MockRemoteSocket;
    run(): Promise<void>;
}

class MockScriptEngineSocketImpl implements MockRemoteSocket {
    constructor(private readonly engine: MockScriptEngineImpl) { }
    send(data: string | ArrayBuffer): void {
        if (this.engine.trace) {
            console.debug(`mock ${this.engine.ident} client sent: `, data);
        }
        let event: MessageEvent<string | ArrayBuffer> | null = null;
        if (typeof data === "string") {
            event = new MessageEvent("message", { data });
        } else {
            const message = new ArrayBuffer(data.byteLength);
            const messageView = new Uint8Array(message);
            const dataView = new Uint8Array(data);
            messageView.set(dataView);
            event = new MessageEvent("message", { data: message });
        }
        this.engine.mockReplyEventTarget.dispatchEvent(event);
    }
    addEventListener<T extends keyof WebSocketEventMap>(event: T, listener: (event: WebSocketEventMap[T]) => any, options?: boolean | AddEventListenerOptions): void;
    addEventListener(event: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions): void {
        if (this.engine.trace) {
            console.debug(`mock ${this.engine.ident} client added listener for ${event}`);
        }
        this.engine.eventTarget.addEventListener(event, listener, options);
    }
    removeEventListener(event: string, listener: EventListenerOrEventListenerObject): void {
        if (this.engine.trace) {
            console.debug(`mock ${this.engine.ident} client removed listener for ${event}`);
        }
        this.engine.eventTarget.removeEventListener(event, listener);
    }
    close(): void {
        if (this.engine.trace) {
            console.debug(`mock ${this.engine.ident} client closed`);
        }
        this.engine.mockReplyEventTarget.dispatchEvent(new CloseEvent("close"));
    }
    dispatchEvent(): boolean {
        throw new Error("don't call dispatchEvent on a MockRemoteSocket");
    }
}

class MockScriptEngineImpl implements MockScriptEngine {
    readonly socket: MockRemoteSocket;
    // eventTarget that the MockReplySocket will dispatch to
    readonly eventTarget: EventTarget = new EventTarget();
    // eventTarget that the MockReplySocket with send() to
    readonly mockReplyEventTarget: EventTarget = new EventTarget();
    constructor(readonly trace: boolean, readonly ident: number) {
        this.socket = new MockScriptEngineSocketImpl(this);
    }

    reply(data: string | ArrayBuffer) {
        if (this.trace) {
            console.debug(`mock ${this.ident} reply:`, data);
        }
        this.eventTarget.dispatchEvent(new MessageEvent("message", { data }));
    }

    async waitForSend(filter: (data: string | ArrayBuffer) => boolean): Promise<void> {
        const trace = this.trace;
        if (trace) {
            console.debug(`mock ${this.ident} waitForSend`);
        }
        const event = await new Promise<MessageEvent<string | ArrayBuffer>>((resolve) => {
            this.mockReplyEventTarget.addEventListener("message", (event) => {
                if (trace) {
                    console.debug(`mock ${this.ident} waitForSend got:`, event);
                }
                resolve(event as MessageEvent<string | ArrayBuffer>);
            }, { once: true });
        });
        if (!filter(event.data)) {
            throw new Error("Unexpected data");
        }
        return;
    }
}

interface MockOptions {
    readonly trace: boolean;
}

class MockImpl {
    openCount: number;
    engines: MockScriptEngineImpl[];
    readonly trace: boolean;
    constructor(public readonly script: ((engine: MockScriptEngine) => Promise<void>)[], options?: MockOptions) {
        this.openCount = 0;
        this.trace = options?.trace ?? false;
        const count = script.length;
        this.engines = new Array<MockScriptEngineImpl>(count);
        for (let i = 0; i < count; ++i) {
            this.engines[i] = new MockScriptEngineImpl(this.trace, i);
        }
    }
    open(): MockRemoteSocket {
        const i = this.openCount++;
        if (this.trace) {
            console.debug(`mock ${i} open`);
        }
        return this.engines[i].socket;
    }

    async run(): Promise<void> {
        await Promise.all(this.script.map((script, i) => script(this.engines[i])));
    }
}

export interface MockScriptEngine {
    waitForSend(filter: (data: string | ArrayBuffer) => boolean): Promise<void>;
    waitForSend<T>(filter: (data: string | ArrayBuffer) => boolean, extract: (data: string | ArrayBuffer) => T): Promise<T>;
    reply(data: string | ArrayBuffer): void;
}
export function mock(script: ((engine: MockScriptEngine) => Promise<void>)[], options?: MockOptions): Mock {
    return new MockImpl(script, options);
}
