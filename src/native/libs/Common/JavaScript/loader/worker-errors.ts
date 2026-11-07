// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export interface WorkerError {
    name: string;
    message: string;
    stack?: string;
    cause?: WorkerError;
    causeValue?: unknown;
    status?: number;
    errors?: WorkerError[];
}

function isError(reason: unknown): reason is Error {
    const tag = Object.prototype.toString.call(reason);
    return reason instanceof Error || tag === "[object Error]" || tag === "[object DOMException]";
}

export function serializeWorkerError(reason: unknown, json = false, seen = new Set<object>()): WorkerError {
    if (!isError(reason)) return { name: "Error", message: String(reason) };
    if (seen.has(reason)) return { name: "Error", message: "Circular error cause" };
    seen.add(reason);
    const result: WorkerError = { name: reason.name, message: reason.message, stack: reason.stack };
    if (isError(reason.cause)) result.cause = serializeWorkerError(reason.cause, json, seen);
    else if (reason.cause !== undefined) {
        try {
            result.causeValue = structuredClone(reason.cause);
            if (json) JSON.stringify(result.causeValue);
        } catch (error) {
            delete result.causeValue;
            result.cause = { name: "DataCloneError", message: `Error cause could not be transferred: ${String(error)}` };
        }
    }
    if ("status" in reason && typeof reason.status === "number") result.status = reason.status;
    if ("errors" in reason && Array.isArray(reason.errors)) result.errors = reason.errors.map(error => serializeWorkerError(error, json, seen));
    seen.delete(reason);
    return result;
}

export function deserializeWorkerError(value: WorkerError): Error {
    let result: Error;
    switch (value.name) {
        case "AggregateError": result = new AggregateError((value.errors ?? []).map(deserializeWorkerError), value.message); break;
        case "TypeError": result = new TypeError(value.message); break;
        case "RangeError": result = new RangeError(value.message); break;
        case "ReferenceError": result = new ReferenceError(value.message); break;
        case "SyntaxError": result = new SyntaxError(value.message); break;
        case "URIError": result = new URIError(value.message); break;
        case "EvalError": result = new EvalError(value.message); break;
        default: result = new Error(value.message); break;
    }
    result.name = value.name;
    if (value.stack !== undefined) result.stack = value.stack;
    if (value.cause) result.cause = deserializeWorkerError(value.cause);
    else if ("causeValue" in value) result.cause = value.causeValue;
    if (value.status !== undefined) Object.assign(result, { status: value.status });
    return result;
}
