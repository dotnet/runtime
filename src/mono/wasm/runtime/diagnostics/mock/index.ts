// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import monoDiagnosticsMock from "consts:monoDiagnosticsMock";

import { createMockEnvironment } from "./environment";
import type { MockEnvironment, MockScriptConnection } from "./export-types";
import { assertNever } from "../../types/internal";
import { mono_log_debug, mono_log_warn } from "../../logging";

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

type MockConnectionScript = (engine: MockScriptConnection) => Promise<void>;
export type MockScript = (env: MockEnvironment) => MockConnectionScript[];

let MockImplConstructor: new (script: MockScript) => Mock;
export function mock(script: MockScript): Mock {
    if (monoDiagnosticsMock) {
        if (!MockImplConstructor) {
            class MockScriptEngineSocketImpl implements MockRemoteSocket {
                constructor(private readonly engine: MockScriptEngineImpl) { }
                send(data: string | ArrayBuffer): void {
                    mono_log_debug(`mock ${this.engine.ident} client sent: `, data);
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
                    mono_log_debug(`mock ${this.engine.ident} client added listener for ${event}`);
                    this.engine.eventTarget.addEventListener(event, listener, options);
                }
                removeEventListener(event: string, listener: EventListenerOrEventListenerObject): void {
                    mono_log_debug(`mock ${this.engine.ident} client removed listener for ${event}`);
                    this.engine.eventTarget.removeEventListener(event, listener);
                }
                close(): void {
                    mono_log_debug(`mock ${this.engine.ident} client closed`);
                    this.engine.mockReplyEventTarget.dispatchEvent(new CloseEvent("close"));
                }
                dispatchEvent(ev: Event): boolean {
                    return this.engine.eventTarget.dispatchEvent(ev);
                }
            }

            class MockScriptEngineImpl implements MockScriptConnection {
                readonly socket: MockRemoteSocket;
                // eventTarget that the MockReplySocket will dispatch to
                readonly eventTarget: EventTarget = new EventTarget();
                // eventTarget that the MockReplySocket with send() to
                readonly mockReplyEventTarget: EventTarget = new EventTarget();
                constructor(readonly ident: number) {
                    this.socket = new MockScriptEngineSocketImpl(this);
                }

                reply(data: ArrayBuffer | Uint8Array) {
                    mono_log_debug(`mock ${this.ident} reply:`, data);
                    let sendData: ArrayBuffer;
                    if (typeof data === "object" && data instanceof ArrayBuffer) {
                        sendData = new ArrayBuffer(data.byteLength);
                        const sendDataView = new Uint8Array(sendData);
                        const dataView = new Uint8Array(data);
                        sendDataView.set(dataView);
                    } else if (typeof data === "object" && data instanceof Uint8Array) {
                        sendData = new ArrayBuffer(data.byteLength);
                        const sendDataView = new Uint8Array(sendData);
                        sendDataView.set(data);
                    } else {
                        mono_log_warn(`mock ${this.ident} reply got wrong kind of reply data, expected ArrayBuffer`, data);
                        assertNever(data);
                    }
                    this.eventTarget.dispatchEvent(new MessageEvent("message", { data: sendData }));
                }

                processSend(onMessage: (data: ArrayBuffer) => any): Promise<void> {
                    mono_log_debug(`mock ${this.ident} processSend`);

                    return new Promise<void>((resolve, reject) => {
                        this.mockReplyEventTarget.addEventListener("close", () => {
                            resolve();
                        });
                        this.mockReplyEventTarget.addEventListener("message", (event: any) => {
                            const data = event.data;
                            if (typeof data === "string") {
                                mono_log_warn(`mock ${this.ident} waitForSend got string:`, data);
                                reject(new Error("mock script connection received string data"));
                            }

                            mono_log_debug(`mock ${this.ident} processSend got:`, data.byteLength);

                            onMessage(data);
                        });
                    });
                }

                async waitForSend<T = void>(filter: (data: ArrayBuffer) => boolean, extract?: (data: ArrayBuffer) => T): Promise<T> {
                    mono_log_debug(`mock ${this.ident} waitForSend`);

                    const data = await new Promise<ArrayBuffer>((resolve) => {
                        this.mockReplyEventTarget.addEventListener("message", (event: any) => {
                            const data = event.data;
                            if (typeof data === "string") {
                                mono_log_warn(`mock ${this.ident} waitForSend got string:`, data);
                                throw new Error("mock script connection received string data");
                            }
                            mono_log_debug(`mock ${this.ident} waitForSend got:`, data.byteLength);

                            resolve(data);
                        }, { once: true });
                    });
                    if (!filter(data)) {
                        throw new Error("Unexpected data");
                    }
                    if (extract) {
                        return extract(data);
                    }
                    return undefined as any as T;
                }
            }

            MockImplConstructor = class MockImpl implements Mock {
                openCount: number;
                engines: MockScriptEngineImpl[];
                connectionScripts: MockConnectionScript[];
                constructor(public readonly mockScript: MockScript) {
                    const env: MockEnvironment = createMockEnvironment();
                    this.connectionScripts = mockScript(env);
                    this.openCount = 0;
                    const count = this.connectionScripts.length;
                    this.engines = new Array<MockScriptEngineImpl>(count);
                    for (let i = 0; i < count; ++i) {
                        this.engines[i] = new MockScriptEngineImpl(i);
                    }
                }
                open(): MockRemoteSocket {
                    const i = this.openCount++;
                    mono_log_debug(`mock ${i} open`);
                    return this.engines[i].socket;
                }

                async run(): Promise<void> {
                    const scripts = this.connectionScripts;
                    await Promise.all(scripts.map((script, i) => script(this.engines[i])));
                }
            };
        }
        return new MockImplConstructor(script);
    } else {
        return undefined as unknown as Mock;
    }
}


