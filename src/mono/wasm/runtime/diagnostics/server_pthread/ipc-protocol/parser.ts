// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Parser from "./base-parser";
import {
    ProtocolClientCommandBase,
    ProcessClientCommandBase,
    EventPipeClientCommandBase,
    EventPipeCommandCollectTracing2,
    EventPipeCollectTracingCommandProvider,
    EventPipeCommandStopTracing,
    ProcessCommandResumeRuntime,
} from "../protocol-client-commands";
import {
    BinaryProtocolCommand,
    ParseResultOk,
    ParseResultFail,
    CommandSetId,
    EventPipeCommandId,
    ProcessCommandId,
} from "./types";
import { mono_log_warn } from "../../../logging";

interface ParseClientCommandResultOk<C = ProtocolClientCommandBase> extends ParseResultOk {
    readonly result: C;
}

export type ParseClientCommandResult<C = ProcessClientCommandBase> = ParseClientCommandResultOk<C> | ParseResultFail;

export function parseBinaryProtocolCommand(cmd: BinaryProtocolCommand): ParseClientCommandResult<ProtocolClientCommandBase> {
    switch (cmd.commandSet) {
        case CommandSetId.Reserved:
            throw new Error("unexpected reserved command_set command");
        case CommandSetId.Dump:
            throw new Error("TODO");
        case CommandSetId.EventPipe:
            return parseEventPipeCommand(cmd);
        case CommandSetId.Profiler:
            throw new Error("TODO");
        case CommandSetId.Process:
            return parseProcessCommand(cmd);
        default:
            return { success: false, error: `unexpected command_set ${cmd.commandSet} command` };
    }
}

function parseEventPipeCommand(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.EventPipe }): ParseClientCommandResult<EventPipeClientCommandBase> {
    switch (cmd.command) {
        case EventPipeCommandId.StopTracing:
            return parseEventPipeStopTracing(cmd);
        case EventPipeCommandId.CollectTracing:
            throw new Error("TODO");
        case EventPipeCommandId.CollectTracing2:
            return parseEventPipeCollectTracing2(cmd);
        default:
            mono_log_warn("unexpected EventPipe command: " + cmd.command);
            return { success: false, error: `unexpected EventPipe command ${cmd.command}` };
    }
}

function parseEventPipeCollectTracing2(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.EventPipe, command: EventPipeCommandId.CollectTracing2 }): ParseClientCommandResult<EventPipeCommandCollectTracing2> {
    const pos = { pos: 0 };
    const buf = cmd.payload;
    const circularBufferMB = Parser.tryParseUint32(buf, pos);
    if (circularBufferMB === undefined) {
        return { success: false, error: "failed to parse circularBufferMB in EventPipe CollectTracing2 command" };
    }
    const format = Parser.tryParseUint32(buf, pos);
    if (format === undefined) {
        return { success: false, error: "failed to parse format in EventPipe CollectTracing2 command" };
    }
    const requestRundown = Parser.tryParseBool(buf, pos);
    if (requestRundown === undefined) {
        return { success: false, error: "failed to parse requestRundown in EventPipe CollectTracing2 command" };
    }
    const numProviders = Parser.tryParseArraySize(buf, pos);
    if (numProviders === undefined) {
        return { success: false, error: "failed to parse numProviders in EventPipe CollectTracing2 command" };
    }
    const providers = new Array<EventPipeCollectTracingCommandProvider>(numProviders);
    for (let i = 0; i < numProviders; i++) {
        const result = parseEventPipeCollectTracingCommandProvider(buf, pos);
        if (!result.success) {
            return result;
        }
        providers[i] = result.result;
    }
    const command: EventPipeCommandCollectTracing2 = { command_set: "EventPipe", command: "CollectTracing2", circularBufferMB, format, requestRundown, providers };
    return { success: true, result: command };
}

function parseEventPipeCollectTracingCommandProvider(buf: Uint8Array, pos: { pos: number }): ParseClientCommandResult<EventPipeCollectTracingCommandProvider> {
    const keywords = Parser.tryParseUint64(buf, pos);
    if (keywords === undefined) {
        return { success: false, error: "failed to parse keywords in EventPipe CollectTracing provider" };
    }
    const logLevel = Parser.tryParseUint32(buf, pos);
    if (logLevel === undefined)
        return { success: false, error: "failed to parse logLevel in EventPipe CollectTracing provider" };
    const providerName = Parser.tryParseUtf16String(buf, pos);
    if (providerName === undefined)
        return { success: false, error: "failed to parse providerName in EventPipe CollectTracing provider" };
    const filterData = Parser.tryParseUtf16String(buf, pos);
    if (filterData === undefined)
        return { success: false, error: "failed to parse filterData in EventPipe CollectTracing provider" };
    const provider: EventPipeCollectTracingCommandProvider = { keywords, logLevel, provider_name: providerName, filter_data: filterData };
    return { success: true, result: provider };
}

function parseEventPipeStopTracing(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.EventPipe, command: EventPipeCommandId.StopTracing }): ParseClientCommandResult<EventPipeCommandStopTracing> {
    const pos = { pos: 0 };
    const buf = cmd.payload;
    const sessionID = Parser.tryParseUint64(buf, pos);
    if (sessionID === undefined) {
        return { success: false, error: "failed to parse sessionID in EventPipe StopTracing command" };
    }
    const [lo, hi] = sessionID;
    if (hi !== 0) {
        return { success: false, error: "sessionID is too large in EventPipe StopTracing command" };
    }
    const command: EventPipeCommandStopTracing = { command_set: "EventPipe", command: "StopTracing", sessionID: lo };
    return { success: true, result: command };
}

function parseProcessCommand(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.Process }): ParseClientCommandResult<ProcessClientCommandBase> {
    switch (cmd.command) {
        case ProcessCommandId.ProcessInfo:
            throw new Error("TODO");
        case ProcessCommandId.ResumeRuntime:
            return parseProcessResumeRuntime(cmd);
        case ProcessCommandId.ProcessEnvironment:
            throw new Error("TODO");
        case ProcessCommandId.ProcessInfo2:
            throw new Error("TODO");
        default:
            mono_log_warn("unexpected Process command: " + cmd.command);
            return { success: false, error: `unexpected Process command ${cmd.command}` };
    }
}

function parseProcessResumeRuntime(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.Process, command: ProcessCommandId.ResumeRuntime }): ParseClientCommandResult<ProcessCommandResumeRuntime> {
    const buf = cmd.payload;
    if (buf.byteLength !== 0) {
        return { success: false, error: "unexpected payload in Process ResumeRuntime command" };
    }
    const command: ProcessCommandResumeRuntime = { command_set: "Process", command: "ResumeRuntime" };
    return { success: true, result: command };
}
