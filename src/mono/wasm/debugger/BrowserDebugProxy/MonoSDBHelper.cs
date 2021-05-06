// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal enum TokenType
    {
        mdtModule               = 0x00000000,       //
        mdtTypeRef              = 0x01000000,       //
        mdtTypeDef              = 0x02000000,       //
        mdtFieldDef             = 0x04000000,       //
        mdtMethodDef            = 0x06000000,       //
        mdtParamDef             = 0x08000000,       //
        mdtInterfaceImpl        = 0x09000000,       //
        mdtMemberRef            = 0x0a000000,       //
        mdtCustomAttribute      = 0x0c000000,       //
        mdtPermission           = 0x0e000000,       //
        mdtSignature            = 0x11000000,       //
        mdtEvent                = 0x14000000,       //
        mdtProperty             = 0x17000000,       //
        mdtModuleRef            = 0x1a000000,       //
        mdtTypeSpec             = 0x1b000000,       //
        mdtAssembly             = 0x20000000,       //
        mdtAssemblyRef          = 0x23000000,       //
        mdtFile                 = 0x26000000,       //
        mdtExportedType         = 0x27000000,       //
        mdtManifestResource     = 0x28000000,       //
        mdtGenericParam         = 0x2a000000,       //
        mdtMethodSpec           = 0x2b000000,       //
        mdtGenericParamConstraint = 0x2c000000,

        mdtString               = 0x70000000,       //
        mdtName                 = 0x71000000,       //
        mdtBaseType             = 0x72000000,       // Leave this on the high end value. This does not correspond to metadata table
    }

    internal enum CommandSet {
        VM = 1,
        OBJECT_REF = 9,
        STRING_REF = 10,
        THREAD = 11,
        ARRAY_REF = 13,
        EVENT_REQUEST = 15,
        STACK_FRAME = 16,
        APPDOMAIN = 20,
        ASSEMBLY = 21,
        METHOD = 22,
        TYPE = 23,
        MODULE = 24,
        FIELD = 25,
        EVENT = 64,
        POINTER = 65
    }

    internal enum EventKind {
        VM_START = 0,
        VM_DEATH = 1,
        THREAD_START = 2,
        THREAD_DEATH = 3,
        APPDOMAIN_CREATE = 4, // Not in JDI
        APPDOMAIN_UNLOAD = 5, // Not in JDI
        METHOD_ENTRY = 6,
        METHOD_EXIT = 7,
        ASSEMBLY_LOAD = 8,
        ASSEMBLY_UNLOAD = 9,
        BREAKPOINT = 10,
        STEP = 11,
        TYPE_LOAD = 12,
        EXCEPTION = 13,
        KEEPALIVE = 14,
        USER_BREAK = 15,
        USER_LOG = 16,
        CRASH = 17
    }

    internal enum ModifierKind {
        COUNT = 1,
        THREAD_ONLY = 3,
        LOCATION_ONLY = 7,
        EXCEPTION_ONLY = 8,
        STEP = 10,
        ASSEMBLY_ONLY = 11,
        SOURCE_FILE_ONLY = 12,
        TYPE_NAME_ONLY = 13
    }


    internal enum SuspendPolicy {
        SUSPEND_POLICY_NONE = 0,
        SUSPEND_POLICY_EVENT_THREAD = 1,
        SUSPEND_POLICY_ALL = 2
    }

    internal enum CmdVM {
        VERSION = 1,
        ALL_THREADS = 2,
        SUSPEND = 3,
        RESUME = 4,
        EXIT = 5,
        DISPOSE = 6,
        INVOKE_METHOD = 7,
        SET_PROTOCOL_VERSION = 8,
        ABORT_INVOKE = 9,
        SET_KEEPALIVE = 10,
        GET_TYPES_FOR_SOURCE_FILE = 11,
        GET_TYPES = 12,
        INVOKE_METHODS = 13,
        START_BUFFERING = 14,
        STOP_BUFFERING = 15,
        VM_READ_MEMORY = 16,
        VM_WRITE_MEMORY = 17,
        GET_ASSEMBLY_BY_NAME = 18
    }

    internal enum CmdEvent {
        COMPOSITE = 100
    }

    internal enum CmdThread {
        GET_FRAME_INFO = 1,
        GET_NAME = 2,
        GET_STATE = 3,
        GET_INFO = 4,
        /* FIXME: Merge into GET_INFO when the major protocol version is increased */
        GET_ID = 5,
        /* Ditto */
        GET_TID = 6,
        SET_IP = 7,
        GET_ELAPSED_TIME = 8
    }

    internal enum CmdEventRequest {
        SET = 1,
        CLEAR = 2,
        CLEAR_ALL_BREAKPOINTS = 3
    }

    internal enum CmdAppDomain {
        GET_ROOT_DOMAIN = 1,
        GET_FRIENDLY_NAME = 2,
        GET_ASSEMBLIES = 3,
        GET_ENTRY_ASSEMBLY = 4,
        CREATE_STRING = 5,
        GET_CORLIB = 6,
        CREATE_BOXED_VALUE = 7,
        CREATE_BYTE_ARRAY = 8,
    }

    internal enum CmdAssembly {
        GET_LOCATION = 1,
        GET_ENTRY_POINT = 2,
        GET_MANIFEST_MODULE = 3,
        GET_OBJECT = 4,
        GET_TYPE = 5,
        GET_NAME = 6,
        GET_DOMAIN = 7,
        GET_METADATA_BLOB = 8,
        GET_IS_DYNAMIC = 9,
        GET_PDB_BLOB = 10,
        GET_TYPE_FROM_TOKEN = 11,
        GET_METHOD_FROM_TOKEN = 12,
        HAS_DEBUG_INFO = 13,
    }

    internal enum CmdModule {
        GET_INFO = 1,
        APPLY_CHANGES = 2,
    }

    internal enum CmdMethod {
        GET_NAME = 1,
        GET_DECLARING_TYPE = 2,
        GET_DEBUG_INFO = 3,
        GET_PARAM_INFO = 4,
        GET_LOCALS_INFO = 5,
        GET_INFO = 6,
        GET_BODY = 7,
        RESOLVE_TOKEN = 8,
        GET_CATTRS = 9,
        MAKE_GENERIC_METHOD = 10,
        TOKEN = 11,
        ASSEMBLY = 12
    }

    internal enum CmdType {
        GET_INFO = 1,
        GET_METHODS = 2,
        GET_FIELDS = 3,
        GET_VALUES = 4,
        GET_OBJECT = 5,
        GET_SOURCE_FILES = 6,
        SET_VALUES = 7,
        IS_ASSIGNABLE_FROM = 8,
        GET_PROPERTIES = 9,
        GET_CATTRS = 10,
        GET_FIELD_CATTRS = 11,
        GET_PROPERTY_CATTRS = 12,
        /* FIXME: Merge into GET_SOURCE_FILES when the major protocol version is increased */
        GET_SOURCE_FILES_2 = 13,
        /* FIXME: Merge into GET_VALUES when the major protocol version is increased */
        GET_VALUES_2 = 14,
        CMD_TYPE_GET_METHODS_BY_NAME_FLAGS = 15,
        GET_INTERFACES = 16,
        GET_INTERFACE_MAP = 17,
        IS_INITIALIZED = 18,
        CREATE_INSTANCE = 19,
        GET_VALUE_SIZE = 20
    }

    internal enum CmdField {
        GET_INFO = 1
    }

    internal class MonoBinaryWriter : BinaryWriter
    {
        public MonoBinaryWriter(Stream stream) : base(stream) {}
        public void WriteString(string val)
        {
            Write(val.Length);
            Write(val.ToCharArray());
        }
        public void WriteLong(long val)
        {
            Write((int)((val >> 32) & 0xffffffff));
            Write((int)((val >> 0) & 0xffffffff));
        }
    }


    internal class MonoSDBHelper
    {
        private static int cmd_id;
        private static int GetId() {return cmd_id++;}
        private MonoProxy proxy;

        public MonoSDBHelper(MonoProxy proxy)
        {
            this.proxy = proxy;
        }

        internal async Task<BinaryReader> SendDebuggerAgentCommand(SessionId sessionId, int command_set, int command, MemoryStream parms, CancellationToken token)
        {
            Result res = await proxy.SendMonoCommand(sessionId, MonoCommands.SendDebuggerAgentCommand(GetId(), command_set, command, Convert.ToBase64String(parms.ToArray())), token);
            if (res.IsErr)
                return null;
            byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["res"]?["value"]?.Value<string>());
            var ret_debugger_cmd = new MemoryStream(newBytes);
            var ret_debugger_cmd_reader = new BinaryReader(ret_debugger_cmd);
            return ret_debugger_cmd_reader;
        }

        public async Task<int> GetMethodToken(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.TOKEN, command_params, token);
            return ret_debugger_cmd_reader.ReadInt32() & 0xffffff; //token
        }

        public async Task<int> GetMethodIdByToken(SessionId sessionId, int assembly_id, int method_token, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(assembly_id);
            command_params_writer.Write(method_token | (int)TokenType.mdtMethodDef);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.ASSEMBLY, (int) CmdAssembly.GET_METHOD_FROM_TOKEN, command_params, token);
            return ret_debugger_cmd_reader.ReadInt32();
        }

        public async Task<int> GetAssemblyIdFromMethod(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.ASSEMBLY, command_params, token);
            return ret_debugger_cmd_reader.ReadInt32(); //assembly_id
        }

        public async Task<int> GetAssemblyId(SessionId sessionId, string asm_name, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.WriteString(asm_name);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.VM, (int) CmdVM.GET_ASSEMBLY_BY_NAME, command_params, token);
            return ret_debugger_cmd_reader.ReadInt32();
        }

        public async Task<string> GetAssemblyName(SessionId sessionId, int assembly_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(assembly_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.ASSEMBLY, (int) CmdAssembly.GET_LOCATION, command_params, token);
            var stringSize = ret_debugger_cmd_reader.ReadInt32();
            char[] memoryData = new char[stringSize];
            ret_debugger_cmd_reader.Read(memoryData, 0, stringSize);
            return new string(memoryData);
        }

        public async Task<string> GetMethodName(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.GET_NAME, command_params, token);
            var stringSize = ret_debugger_cmd_reader.ReadInt32();
            char[] memoryData = new char[stringSize];
            ret_debugger_cmd_reader.Read(memoryData, 0, stringSize);
            return new string(memoryData);
        }

        public async Task<int> SetBreakpoint(SessionId sessionId, int method_id, long il_offset, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((byte)EventKind.BREAKPOINT);
            command_params_writer.Write((byte)SuspendPolicy.SUSPEND_POLICY_NONE);
            command_params_writer.Write((byte)1);
            command_params_writer.Write((byte)ModifierKind.LOCATION_ONLY);
            command_params_writer.Write(method_id);
            command_params_writer.WriteLong(il_offset);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.EVENT_REQUEST, (int) CmdEventRequest.SET, command_params, token);
            return ret_debugger_cmd_reader.ReadInt32();
        }

        public async Task<bool> RemoveBreakpoint(SessionId sessionId, int breakpoint_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((byte)EventKind.BREAKPOINT);
            command_params_writer.Write((int) breakpoint_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.EVENT_REQUEST, (int) CmdEventRequest.CLEAR, command_params, token);

            if (ret_debugger_cmd_reader != null)
                return true;
            return false;
        }

        public async Task<bool> Step(SessionId sessionId, int thread_id, StepKind kind, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((byte)EventKind.STEP);
            command_params_writer.Write((byte)SuspendPolicy.SUSPEND_POLICY_NONE);
            command_params_writer.Write((byte)1);
            command_params_writer.Write((byte)ModifierKind.STEP);
            command_params_writer.Write(thread_id);
            command_params_writer.Write((int)0);
            command_params_writer.Write((int)kind);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.EVENT_REQUEST, (int) CmdEventRequest.SET, command_params, token);
            if (ret_debugger_cmd_reader != null)
                return true;
            return false;
        }

        public async Task<bool> ClearSingleStep(SessionId sessionId, int req_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((byte)EventKind.STEP);
            command_params_writer.Write((int) req_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.EVENT_REQUEST, (int) CmdEventRequest.CLEAR, command_params, token);

            if (ret_debugger_cmd_reader != null)
                return true;
            return false;
        }

    }
}
