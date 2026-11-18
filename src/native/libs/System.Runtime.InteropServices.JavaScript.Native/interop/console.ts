// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetApi, dotnetBrowserUtilsExports, dotnetLogger, Module } from "./cross-module";

class ConsoleChannel {
    private readonly sink: (message: string) => void;
    private bytes = new Uint8Array(0);
    private byteCount = 0;

    constructor(sink: (message: string) => void) {
        this.sink = sink;
    }

    get hasPending(): boolean {
        return this.byteCount !== 0;
    }

    writeBytes(bytes: Uint8Array): void {
        let start = 0;
        for (let newline: number; (newline = bytes.indexOf(10, start)) !== -1; start = newline + 1) {
            const line = bytes.subarray(start, newline);
            if (this.byteCount === 0) {
                const filtered = line.includes(0) ? line.filter(value => value !== 0) : line;
                const sink = this.sink;
                sink(dotnetBrowserUtilsExports.utf8ToStringRelaxed(filtered));
            } else {
                this.appendBytes(line);
                this.emitLine();
            }
        }
        if (start < bytes.length) {
            this.appendBytes(bytes.subarray(start));
        }
    }

    finish(): void {
        if (this.byteCount !== 0) {
            this.emitLine();
        }
    }

    private appendBytes(bytes: Uint8Array): void {
        const filtered = bytes.includes(0) ? bytes.filter(value => value !== 0) : bytes;
        if (filtered.length === 0) {
            return;
        }

        const requiredCapacity = this.byteCount + filtered.length;
        if (requiredCapacity > this.bytes.length) {
            const expanded = new Uint8Array(Math.max(requiredCapacity, this.bytes.length * 2, 256));
            expanded.set(this.bytes.subarray(0, this.byteCount));
            this.bytes = expanded;
        }
        this.bytes.set(filtered, this.byteCount);
        this.byteCount = requiredCapacity;
    }

    private emitLine(): void {
        const bytes = this.bytes.subarray(0, this.byteCount);
        this.bytes = new Uint8Array(0);
        this.byteCount = 0;
        const sink = this.sink;
        sink(dotnetBrowserUtilsExports.utf8ToStringRelaxed(bytes));
    }
}

export function createConsoleChannels() {
    const stdout = new ConsoleChannel(Module.print);
    const stderr = new ConsoleChannel(Module.printErr);
    let active = false;

    function drain(): void {
        let drainStdout = true;
        let drainStderr = true;
        for (let pass = 0; (drainStdout && stdout.hasPending) || (drainStderr && stderr.hasPending); pass++) {
            if (pass === 10000) {
                dotnetLogger.warn("Managed console drain exceeded the reentrancy limit.");
                break;
            }
            if (drainStdout && stdout.hasPending) {
                try {
                    stdout.finish();
                } catch (error) {
                    drainStdout = false;
                    dotnetLogger.warn("Failed to flush managed stdout: ", error);
                }
            }
            if (drainStderr && stderr.hasPending) {
                try {
                    stderr.finish();
                } catch (error) {
                    drainStderr = false;
                    dotnetLogger.warn("Failed to flush managed stderr: ", error);
                }
            }
        }
    }

    function activate(): void {
        if (!active) {
            dotnetApi.INTERNAL.consoleInstallExit();
            dotnetApi.INTERNAL.consoleDrain = drain;
            active = true;
        }
    }

    return {
        consoleClear(): void {
            // eslint-disable-next-line no-console
            console.clear();
        },
        consoleWriteBytes(value: Uint8Array, isError: boolean): void {
            // Copy before invoking the sink, which may reenter managed code or grow Wasm memory.
            const bytes = value.slice() as Uint8Array;
            activate();
            (isError ? stderr : stdout).writeBytes(bytes);
        },
    };
}
