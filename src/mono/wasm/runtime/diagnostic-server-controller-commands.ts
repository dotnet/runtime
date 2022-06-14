
import type { EventPipeSessionDiagnosticServerID } from "./types";

export type { EventPipeSessionDiagnosticServerID } from "./types";


export type EventPipeSessionIDImpl = number;

export type DiagnosticServerControlCommand = DiagnosticServerControlCommandStart | DiagnosticServerControlCommandSetSessionID;
export type DiagnosticServerControlCommandStart = {
    type: "start";
};

export type DiagnosticServerControlCommandSetSessionID = {
    type: "set_session_id";
    diagnostic_server_id: EventPipeSessionDiagnosticServerID;
    session_id: EventPipeSessionIDImpl;
}
