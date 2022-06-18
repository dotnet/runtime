// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// pthread_t in C
export type pthread_ptr = number;

/// a symbol that we use as a key on messages on the global worker-to-main channel to identify our own messages
/// we can't use an actual JS Symbol because those don't transfer between workers.
export const monoSymbol = "__mono_message_please_dont_collide__"; //Symbol("mono");

export type MonoMessageBody = {
    mono_cmd: string;
}

/// Messages on the global worker-to-main channel have this shape
export interface MonoMessage<T extends MonoMessageBody> {
    [monoSymbol]: T;
}

export interface MonoMessageBodyChannelCreated extends MonoMessageBody {
    mono_cmd: "channel_created";
    port: MessagePort;
}

export function makeChannelCreatedMonoMessage(port: MessagePort): MonoMessage<MonoMessageBodyChannelCreated> {
    return {
        [monoSymbol]: {
            mono_cmd: "channel_created",
            port: port
        }
    };
}
