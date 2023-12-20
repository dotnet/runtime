// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { MockRemoteSocket } from "../mock";

// the common bits that we depend on from a real WebSocket or a MockRemoteSocket used for testing
export interface CommonSocket {
    addEventListener<T extends keyof WebSocketEventMap>(type: T, listener: (this: CommonSocket, ev: WebSocketEventMap[T]) => any, options?: boolean | AddEventListenerOptions): void;
    addEventListener(event: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions): void;
    removeEventListener<T extends keyof WebSocketEventMap>(type: T, listener: (this: CommonSocket, ev: WebSocketEventMap[T]) => any): void;
    removeEventListener(event: string, listener: EventListenerOrEventListenerObject): void;
    dispatchEvent(evt: Event): boolean;
    // send is more general and can send a string, but we should only be sending binary data
    send(data: ArrayBuffer | Uint8Array /*| Blob | DataView*/): void;
    close(): void;
}


type AssignableTo<T, Q> = Q extends T ? true : false;

function static_assert<Cond extends boolean>(x: Cond): asserts x is Cond { /*empty*/ }

{
    static_assert<AssignableTo<CommonSocket, WebSocket>>(true);
    static_assert<AssignableTo<CommonSocket, MockRemoteSocket>>(true);

    static_assert<AssignableTo<{ x: number }, { y: number }>>(false); // sanity check that static_assert works
}

