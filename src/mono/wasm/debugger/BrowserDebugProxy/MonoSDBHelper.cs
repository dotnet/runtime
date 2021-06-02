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
using System.Text.RegularExpressions;

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

    internal enum CmdFrame {
        GET_VALUES = 1,
        GET_THIS = 2,
        SET_VALUES = 3,
        GET_DOMAIN = 4,
        SET_THIS = 5,
        GET_ARGUMENT = 6,
        GET_ARGUMENTS = 7
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

    internal enum CmdPointer{
        GET_VALUE = 1
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
        ASSEMBLY = 12,
        CLASS_TOKEN = 13,
        ASYNC_DEBUG_INFO = 14,
        GET_NAME_FULL = 15
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
        GET_METHODS_BY_NAME_FLAGS = 15,
        GET_INTERFACES = 16,
        GET_INTERFACE_MAP = 17,
        IS_INITIALIZED = 18,
        CREATE_INSTANCE = 19,
        GET_VALUE_SIZE = 20,
        GET_VALUES_ICORDBG = 21,
        GET_PARENTS = 22
    }

    internal enum CmdArray {
        GET_LENGTH = 1,
        GET_VALUES = 2,
        SET_VALUES = 3,
        REF_GET_TYPE = 4
    }


    internal enum CmdField {
        GET_INFO = 1
    }

    internal enum CmdString {
        GET_VALUE = 1,
        GET_LENGTH = 2,
        GET_CHARS = 3
    }

    internal enum CmdObject {
        REF_GET_TYPE = 1,
        REF_GET_VALUES = 2,
        REF_IS_COLLECTED = 3,
        REF_GET_ADDRESS = 4,
        REF_GET_DOMAIN = 5,
        REF_SET_VALUES = 6,
        REF_GET_INFO = 7,
        GET_VALUES_ICORDBG = 8,
        REF_DELEGATE_GET_METHOD = 9,
        REF_IS_DELEGATE = 10
    }

    internal enum ElementType {
        End             = 0x00,
        Void            = 0x01,
        Boolean         = 0x02,
        Char            = 0x03,
        I1              = 0x04,
        U1              = 0x05,
        I2              = 0x06,
        U2              = 0x07,
        I4              = 0x08,
        U4              = 0x09,
        I8              = 0x0a,
        U8              = 0x0b,
        R4              = 0x0c,
        R8              = 0x0d,
        String          = 0x0e,
        Ptr             = 0x0f,
        ByRef           = 0x10,
        ValueType       = 0x11,
        Class           = 0x12,
        Var             = 0x13,
        Array           = 0x14,
        GenericInst     = 0x15,
        TypedByRef      = 0x16,
        I               = 0x18,
        U               = 0x19,
        FnPtr           = 0x1b,
        Object          = 0x1c,
        SzArray         = 0x1d,
        MVar            = 0x1e,
        CModReqD        = 0x1f,
        CModOpt         = 0x20,
        Internal        = 0x21,
        Modifier        = 0x40,
        Sentinel        = 0x41,
        Pinned          = 0x45,

        Type            = 0x50,
        Boxed           = 0x51,
        Enum            = 0x55
    }

    internal enum ValueTypeId {
        VALUE_TYPE_ID_NULL = 0xf0,
        VALUE_TYPE_ID_TYPE = 0xf1,
        VALUE_TYPE_ID_PARENT_VTYPE = 0xf2,
        VALUE_TYPE_ID_FIXED_ARRAY = 0xf3
    }
    internal enum MonoTypeNameFormat{
        MONO_TYPE_NAME_FORMAT_IL,
        MONO_TYPE_NAME_FORMAT_REFLECTION,
        MONO_TYPE_NAME_FORMAT_FULL_NAME,
        MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
    }

    internal enum StepFilter {
        None = 0,
        StaticCtor = 1,
        DebuggerHidden = 2,
        DebuggerStepThrough = 4,
        DebuggerNonUserCode = 8
    }

    internal class MonoBinaryReader : BinaryReader
    {
        public MonoBinaryReader(Stream stream) : base(stream) {}

        internal static unsafe void PutBytesBE (byte *dest, byte *src, int count)
        {
            int i = 0;

            if (BitConverter.IsLittleEndian){
                dest += count;
                for (; i < count; i++)
                    *(--dest) = *src++;
            } else {
                for (; i < count; i++)
                    *dest++ = *src++;
            }
        }

        public override string ReadString()
        {
            var valueLen = ReadInt32();
            char[] value = new char[valueLen];
            Read(value, 0, valueLen);
            return new string(value);
        }
        public unsafe long ReadLong()
        {
            byte[] data = new byte[8];
            Read(data, 0, 8);

            long ret;
            fixed (byte *src = &data[0]){
                PutBytesBE ((byte *) &ret, src, 8);
            }

            return ret;
        }
        public override unsafe sbyte ReadSByte()
        {
            byte[] data = new byte[4];
            Read(data, 0, 4);

            int ret;
            fixed (byte *src = &data[0]){
                PutBytesBE ((byte *) &ret, src, 4);
            }
            return (sbyte)ret;
        }

        public unsafe byte ReadUByte()
        {
            byte[] data = new byte[4];
            Read(data, 0, 4);

            int ret;
            fixed (byte *src = &data[0]){
                PutBytesBE ((byte *) &ret, src, 4);
            }
            return (byte)ret;
        }

        public override unsafe int ReadInt32()
        {
            byte[] data = new byte[4];
            Read(data, 0, 4);
            int ret;
            fixed (byte *src = &data[0]){
                PutBytesBE ((byte *) &ret, src, 4);
            }
            return ret;
        }

        public override unsafe double ReadDouble()
        {
            byte[] data = new byte[8];
            Read(data, 0, 8);

            double ret;
            fixed (byte *src = &data[0]){
                PutBytesBE ((byte *) &ret, src, 8);
            }
            return ret;
        }

        public override unsafe uint ReadUInt32()
        {
            byte[] data = new byte[4];
            Read(data, 0, 4);

            uint ret;
            fixed (byte *src = &data[0]){
                PutBytesBE ((byte *) &ret, src, 4);
            }
            return ret;
        }
        public unsafe ushort ReadUShort()
        {
            byte[] data = new byte[4];
            Read(data, 0, 4);

            uint ret;
            fixed (byte *src = &data[0]){
                PutBytesBE ((byte *) &ret, src, 4);
            }
            return (ushort)ret;
        }
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
        public override void Write(int val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Array.Reverse(bytes, 0, bytes.Length);
            Write(bytes);
        }
        public void WriteObj(DotnetObjectId objectId, MonoSDBHelper sdbHelper)
        {
            if (objectId.Scheme == "object")
            {
                Write((byte)ElementType.Class);
                Write(int.Parse(objectId.Value));
            }
            if (objectId.Scheme == "valuetype")
            {
                Write(sdbHelper.valueTypes[int.Parse(objectId.Value)].valueTypeBuffer);
            }
        }
    }
    internal class FieldTypeClass
    {
        public int Id { get; }
        public string Name { get; }
        public int TypeId { get; }
        public FieldTypeClass(int id, string name, int typeId)
        {
            Id = id;
            Name = name;
            TypeId = typeId;
        }
    }
    internal class ValueTypeClass
    {
        public byte[] valueTypeBuffer;
        public JArray valueTypeJson;
        public JArray valueTypeJsonProps;
        public int typeId;
        public JArray valueTypeProxy;
        public string valueTypeVarName;
        public bool valueTypeAutoExpand;
        public int Id;
        public ValueTypeClass(string varName, byte[] buffer, JArray json, int id, bool expand_properties, int valueTypeId)
        {
            valueTypeBuffer = buffer;
            valueTypeJson = json;
            typeId = id;
            valueTypeJsonProps = null;
            valueTypeProxy = null;
            valueTypeVarName = varName;
            valueTypeAutoExpand = expand_properties;
            Id = valueTypeId;
        }
    }
    internal class PointerValue
    {
        public long address;
        public int typeId;
        public string varName;
        public PointerValue(long address, int typeId, string varName)
        {
            this.address = address;
            this.typeId = typeId;
            this.varName = varName;
        }

    }
    internal class MonoSDBHelper
    {
        internal Dictionary<int, ValueTypeClass> valueTypes = new Dictionary<int, ValueTypeClass>();
        internal Dictionary<int, PointerValue> pointerValues = new Dictionary<int, PointerValue>();
        private static int debugger_object_id;
        private static int cmd_id;
        private static int GetId() {return cmd_id++;}
        private MonoProxy proxy;
        private static int MINOR_VERSION = 61;
        private static int MAJOR_VERSION = 2;
        public MonoSDBHelper(MonoProxy proxy)
        {
            this.proxy = proxy;
        }

        public void ClearCache()
        {
            valueTypes = new Dictionary<int, ValueTypeClass>();
            pointerValues = new Dictionary<int, PointerValue>();
        }

        public async Task<bool> SetProtocolVersion(SessionId sessionId, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(MAJOR_VERSION);
            command_params_writer.Write(MINOR_VERSION);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.VM, (int) CmdVM.SET_PROTOCOL_VERSION, command_params, token);
            return true;
        }
        public async Task<bool> EnableReceiveUserBreakRequest(SessionId sessionId, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((byte)EventKind.USER_BREAK);
            command_params_writer.Write((byte)SuspendPolicy.SUSPEND_POLICY_NONE);
            command_params_writer.Write((byte)0);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.EVENT_REQUEST, (int) CmdEventRequest.SET, command_params, token);
            return true;
        }
        internal async Task<MonoBinaryReader> SendDebuggerAgentCommand(SessionId sessionId, int command_set, int command, MemoryStream parms, CancellationToken token)
        {
            Result res = await proxy.SendMonoCommand(sessionId, MonoCommands.SendDebuggerAgentCommand(GetId(), command_set, command, Convert.ToBase64String(parms.ToArray())), token);
            if (res.IsErr) {
                throw new Exception($"SendDebuggerAgentCommand Error - {(CommandSet)command_set} - {command}");
            }
            byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["value"]?.Value<string>());
            var ret_debugger_cmd = new MemoryStream(newBytes);
            var ret_debugger_cmd_reader = new MonoBinaryReader(ret_debugger_cmd);
            return ret_debugger_cmd_reader;
        }

        internal async Task<MonoBinaryReader> SendDebuggerAgentCommandWithParms(SessionId sessionId, int command_set, int command, MemoryStream parms, int type, string extraParm, CancellationToken token)
        {
            Result res = await proxy.SendMonoCommand(sessionId, MonoCommands.SendDebuggerAgentCommandWithParms(GetId(), command_set, command, Convert.ToBase64String(parms.ToArray()), parms.ToArray().Length, type, extraParm), token);
            if (res.IsErr) {
                throw new Exception("SendDebuggerAgentCommandWithParms Error");
            }
            byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["value"]?.Value<string>());
            var ret_debugger_cmd = new MemoryStream(newBytes);
            var ret_debugger_cmd_reader = new MonoBinaryReader(ret_debugger_cmd);
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
            return ret_debugger_cmd_reader.ReadString();
        }


        public async Task<string> GetAssemblyNameFull(SessionId sessionId, int assembly_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(assembly_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.ASSEMBLY, (int) CmdAssembly.GET_NAME, command_params, token);
            var name = ret_debugger_cmd_reader.ReadString();
            return name.Remove(name.IndexOf(",")) + ".dll";
        }

        public async Task<string> GetMethodName(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.GET_NAME_FULL, command_params, token);
            var methodName = ret_debugger_cmd_reader.ReadString();
            return methodName.Substring(methodName.IndexOf(":")+1);
        }

        public async Task<bool> MethodIsStatic(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.GET_INFO, command_params, token);
            var flags = ret_debugger_cmd_reader.ReadInt32();
            return (flags & 0x0010) > 0; //check method is static
        }

        public async Task<int> GetParamCount(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.GET_PARAM_INFO, command_params, token);
            ret_debugger_cmd_reader.ReadInt32();
            int param_count = ret_debugger_cmd_reader.ReadInt32();
            return param_count;
        }

        public async Task<string> GetReturnType(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.GET_PARAM_INFO, command_params, token);
            ret_debugger_cmd_reader.ReadInt32();
            ret_debugger_cmd_reader.ReadInt32();
            ret_debugger_cmd_reader.ReadInt32();
            var retType = ret_debugger_cmd_reader.ReadInt32();
            var ret = await GetTypeName(sessionId, retType, token);
            return ret;
        }

        public async Task<string> GetParameters(SessionId sessionId, int method_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(method_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.GET_PARAM_INFO, command_params, token);
            ret_debugger_cmd_reader.ReadInt32();
            var paramCount = ret_debugger_cmd_reader.ReadInt32();
            ret_debugger_cmd_reader.ReadInt32();
            var retType = ret_debugger_cmd_reader.ReadInt32();
            var parameters = "(";
            for (int i = 0 ; i < paramCount; i++)
            {
                var paramType = ret_debugger_cmd_reader.ReadInt32();
                parameters += await GetTypeName(sessionId, paramType, token);
                parameters = parameters.Replace("System.Func", "Func");
                if (i + 1 < paramCount)
                    parameters += ",";
            }
            parameters += ")";
            return parameters;
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
            command_params_writer.Write((int)(StepFilter.StaticCtor | StepFilter.DebuggerHidden)); //filter
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.EVENT_REQUEST, (int) CmdEventRequest.SET, command_params, token);
            if (ret_debugger_cmd_reader == null)
                return false;
            var isBPOnManagedCode = ret_debugger_cmd_reader.ReadInt32();
            if (isBPOnManagedCode == 0)
                return false;
            return true;
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

        public async Task<List<FieldTypeClass>> GetTypeFields(SessionId sessionId, int type_id, CancellationToken token)
        {
            var ret = new List<FieldTypeClass>();
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(type_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_FIELDS, command_params, token);
            var nFields = ret_debugger_cmd_reader.ReadInt32();

            for (int i = 0 ; i < nFields; i++)
            {
                int fieldId = ret_debugger_cmd_reader.ReadInt32(); //fieldId
                string fieldNameStr = ret_debugger_cmd_reader.ReadString();
                int typeId = ret_debugger_cmd_reader.ReadInt32(); //typeId
                ret_debugger_cmd_reader.ReadInt32(); //attrs
                if (fieldNameStr.Contains("k__BackingField"))
                {
                    fieldNameStr = fieldNameStr.Replace("k__BackingField", "");
                    fieldNameStr = fieldNameStr.Replace("<", "");
                    fieldNameStr = fieldNameStr.Replace(">", "");
                }
                ret.Add(new FieldTypeClass(fieldId, fieldNameStr, typeId));
            }
            return ret;
        }
        public string ReplaceCommonClassNames(string className)
        {
            className = className.Replace("System.String", "string");
            className = className.Replace("System.Boolean", "bool");
            className = className.Replace("System.Char", "char");
            className = className.Replace("System.Int32", "int");
            className = className.Replace("System.Object", "object");
            className = className.Replace("System.Void", "void");
            className = className.Replace("System.Byte", "byte");
            return className;
        }
        public async Task<string> GetTypeName(SessionId sessionId, int type_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(type_id);
            command_params_writer.Write((int) MonoTypeNameFormat.MONO_TYPE_NAME_FORMAT_REFLECTION);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_INFO, command_params, token);

            ret_debugger_cmd_reader.ReadString();

            ret_debugger_cmd_reader.ReadString();

            string className = ret_debugger_cmd_reader.ReadString();

            className = className.Replace("+", ".");
            className = Regex.Replace(className, @"`\d+", "");
            className = className.Replace("[]", "__SQUARED_BRACKETS__");
            className = className.Replace("[", "<");
            className = className.Replace("]", ">");
            className = className.Replace("__SQUARED_BRACKETS__", "[]");
            className = className.Replace(",", ", ");
            className = ReplaceCommonClassNames(className);
            return className;
        }

        public async Task<string> GetStringValue(SessionId sessionId, int string_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(string_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.STRING_REF, (int) CmdString.GET_VALUE, command_params, token);
            var isUtf16 = ret_debugger_cmd_reader.ReadByte();
            if (isUtf16 == 0) {
                return ret_debugger_cmd_reader.ReadString();
            }
            return null;
        }
        public async Task<int> GetArrayLength(SessionId sessionId, int object_id, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(object_id);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.ARRAY_REF, (int) CmdArray.GET_LENGTH, command_params, token);
            var length = ret_debugger_cmd_reader.ReadInt32();
            length = ret_debugger_cmd_reader.ReadInt32();
            return length;
        }
        public async Task<List<int>> GetTypeIdFromObject(SessionId sessionId, int object_id, bool withParents, CancellationToken token)
        {
            List<int> ret = new List<int>();
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(object_id);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.OBJECT_REF, (int) CmdObject.REF_GET_TYPE, command_params, token);
            var type_id = ret_debugger_cmd_reader.ReadInt32();
            ret.Add(type_id);
            if (withParents)
            {
                command_params = new MemoryStream();
                command_params_writer = new MonoBinaryWriter(command_params);
                command_params_writer.Write(type_id);
                ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_PARENTS, command_params, token);
                var parentsCount = ret_debugger_cmd_reader.ReadInt32();
                for (int i = 0 ; i < parentsCount; i++)
                {
                    ret.Add(ret_debugger_cmd_reader.ReadInt32());
                }
            }
            return ret;
        }

        public async Task<string> GetClassNameFromObject(SessionId sessionId, int object_id, CancellationToken token)
        {
            var type_id = await GetTypeIdFromObject(sessionId, object_id, false, token);
            return await GetTypeName(sessionId, type_id[0], token);
        }

        public async Task<int> GetMethodIdByName(SessionId sessionId, int type_id, string method_name, CancellationToken token)
        {
            var ret = new List<string>();
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((int)type_id);
            command_params_writer.WriteString(method_name);
            command_params_writer.Write((int)(0x10 | 4)); //instance methods
            command_params_writer.Write((int)1); //case sensitive
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_METHODS_BY_NAME_FLAGS, command_params, token);
            var nMethods = ret_debugger_cmd_reader.ReadInt32();
            return ret_debugger_cmd_reader.ReadInt32();
        }

        public async Task<bool> IsDelegate(SessionId sessionId, int objectId, CancellationToken token)
        {
            var ret = new List<string>();
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((int)objectId);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.OBJECT_REF, (int) CmdObject.REF_IS_DELEGATE, command_params, token);
            return ret_debugger_cmd_reader.ReadByte() == 1;
        }

        public async Task<int> GetDelegateMethod(SessionId sessionId, int objectId, CancellationToken token)
        {
            var ret = new List<string>();
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((int)objectId);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.OBJECT_REF, (int) CmdObject.REF_DELEGATE_GET_METHOD, command_params, token);
            return ret_debugger_cmd_reader.ReadInt32();
        }

        public async Task<string> GetDelegateMethodDescription(SessionId sessionId, int objectId, CancellationToken token)
        {
            var methodId = await GetDelegateMethod(sessionId, objectId, token);

            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(methodId);
            //Console.WriteLine("methodId - " + methodId);
            if (methodId == 0)
                return "";
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.GET_NAME, command_params, token);
            var methodName = ret_debugger_cmd_reader.ReadString();

            var returnType = await GetReturnType(sessionId, methodId, token);
            var parameters = await GetParameters(sessionId, methodId, token);

            return $"{returnType} {methodName} {parameters}";
        }
        public async Task<JObject> InvokeMethod(SessionId sessionId, byte[] valueTypeBuffer, int method_id, string varName, CancellationToken token)
        {
            MemoryStream parms = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(parms);
            command_params_writer.Write(method_id);
            command_params_writer.Write(valueTypeBuffer);
            command_params_writer.Write(0);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.VM, (int) CmdVM.INVOKE_METHOD, parms, token);
            ret_debugger_cmd_reader.ReadByte(); //number of objects returned.
            return await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, varName, false, -1, token);
        }
        public async Task<JArray> CreateJArrayForProperties(SessionId sessionId, int typeId, byte[] object_buffer, JArray attributes, bool isAutoExpandable, string objectId, bool isOwn, CancellationToken token)
        {
            JArray ret = new JArray();
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(typeId);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_PROPERTIES, command_params, token);
            var nProperties = ret_debugger_cmd_reader.ReadInt32();
            for (int i = 0 ; i < nProperties; i++)
            {
                ret_debugger_cmd_reader.ReadInt32(); //propertyId
                string propertyNameStr = ret_debugger_cmd_reader.ReadString();
                var getMethodId = ret_debugger_cmd_reader.ReadInt32();
                ret_debugger_cmd_reader.ReadInt32(); //setmethod
                var attrs = ret_debugger_cmd_reader.ReadInt32(); //attrs
                if (getMethodId == 0 || await GetParamCount(sessionId, getMethodId, token) != 0 || await MethodIsStatic(sessionId, getMethodId, token))
                    continue;
                JObject propRet = null;
                if (attributes.Where(attribute => attribute["name"].Value<string>().Equals(propertyNameStr)).Any())
                    continue;
                if (isAutoExpandable)
                {
                    try {
                        propRet = await InvokeMethod(sessionId, object_buffer, getMethodId, propertyNameStr, token);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                else
                {
                    propRet = JObject.FromObject(new {
                            get = new
                            {
                                type = "function",
                                objectId = $"{objectId}:method_id:{getMethodId}",
                                className = "Function",
                                description = "get " + propertyNameStr + " ()",
                                methodId = getMethodId,
                                objectIdValue = objectId
                            },
                            name = propertyNameStr
                        });
                }
                if (isOwn)
                    propRet["isOwn"] = true;
                ret.Add(propRet);
            }
            return ret;
        }
        public async Task<JObject> GetPointerContent(SessionId sessionId, int pointerId, CancellationToken token)
        {
            var ret = new List<string>();
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.WriteLong(pointerValues[pointerId].address);
            command_params_writer.Write(pointerValues[pointerId].typeId);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.POINTER, (int) CmdPointer.GET_VALUE, command_params, token);
            var varName = pointerValues[pointerId].varName;
            if (int.TryParse(varName, out _))
                varName = $"[{varName}]";
            return await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, "*" + varName, false, -1, token);
        }
        public async Task<JArray> GetPropertiesValuesOfValueType(SessionId sessionId, int valueTypeId, CancellationToken token)
        {
            JArray ret = new JArray();
            var valueType = valueTypes[valueTypeId];
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(valueType.typeId);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_PARENTS, command_params, token);
            var parentsCount = ret_debugger_cmd_reader.ReadInt32();
            List<int> typesToGetProperties = new List<int>();
            typesToGetProperties.Add(valueType.typeId);
            for (int i = 0 ; i < parentsCount; i++)
            {
                typesToGetProperties.Add(ret_debugger_cmd_reader.ReadInt32());
            }
            for (int i = 0 ; i < typesToGetProperties.Count; i++)
            {
                var properties = await CreateJArrayForProperties(sessionId, typesToGetProperties[i], valueType.valueTypeBuffer, valueType.valueTypeJson, valueType.valueTypeAutoExpand, $"dotnet:valuetype:{valueType.Id}", i == 0, token);
                ret = new JArray(ret.Union(properties));
            }

            return ret;
        }

        public bool AutoExpandable(string className) {
            if (className == "System.DateTime" ||
                className == "System.DateTimeOffset" ||
                className == "System.TimeSpan")
                return true;
            return false;
        }

        public bool AutoInvokeToString(string className) {
            if (className == "System.DateTime" ||
                className == "System.DateTimeOffset" ||
                className == "System.TimeSpan" ||
                className == "System.Decimal"  ||
                className == "System.Guid")
                return true;
            return false;
        }

        public async Task<JObject> CreateJObjectForVariableValue(SessionId sessionId, MonoBinaryReader ret_debugger_cmd_reader, string name, bool isOwn, int typeIdFromAttribute, CancellationToken token)
        {
            long initialPos = ret_debugger_cmd_reader == null ? 0 : ret_debugger_cmd_reader.BaseStream.Position;
            ElementType etype = (ElementType)ret_debugger_cmd_reader.ReadByte();
            JObject fieldValueType = null;
            JObject ret = null;
            switch (etype) {
                case ElementType.Void:
                    ret = new JObject{{"Type", "void"}};
                    break;
                case ElementType.Boolean:
                {
                    var value = ret_debugger_cmd_reader.ReadInt32();
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "boolean",
                                value = value == 0 ? false : true,
                                description = value == 0 ? "false" : "true"
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.I1:
                {
                    var value = ret_debugger_cmd_reader.ReadSByte();
                    return JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                }
                case ElementType.I2:
                case ElementType.I4:
                {
                    var value = ret_debugger_cmd_reader.ReadInt32();
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.U1:
                {
                    var value = ret_debugger_cmd_reader.ReadUByte();
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.U2:
                {
                    var value = ret_debugger_cmd_reader.ReadUShort();
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.U4:
                {
                    var value = ret_debugger_cmd_reader.ReadUInt32();
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.R4:
                {
                    float value = BitConverter.Int32BitsToSingle(ret_debugger_cmd_reader.ReadInt32());
                    return JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                }
                case ElementType.Char:
                {
                    var value = ret_debugger_cmd_reader.ReadInt32();
                    var description = $"{value.ToString()} '{Convert.ToChar(value)}'";
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "symbol",
                                value = description,
                                description
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.I8:
                {
                    long value = ret_debugger_cmd_reader.ReadLong();
                    return JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                }
                case ElementType.U8:
                {
                    ulong high = (ulong) ret_debugger_cmd_reader.ReadInt32();
                    ulong low = (ulong) ret_debugger_cmd_reader.ReadInt32();
                    var value = ((high << 32) | low);
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.R8:
                {
                    double value = ret_debugger_cmd_reader.ReadDouble();
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "number",
                                value,
                                description = value.ToString()
                            },
                            writable = true,
                            name
                        });
                    break;
                }
                case ElementType.I:
                case ElementType.U:
                    // FIXME: The client and the debuggee might have different word sizes
                    ret = new JObject{{"Type", "void"}};
                    break;
                case ElementType.FnPtr:
                case ElementType.Ptr:
                {
                    string type;
                    string value;
                    long valueAddress = ret_debugger_cmd_reader.ReadLong();
                    var typeId = ret_debugger_cmd_reader.ReadInt32();
                    var className = "";
                    if (etype == ElementType.FnPtr)
                        className = "(*())"; //to keep the old behavior
                    else
                        className = "(" + await GetTypeName(sessionId, typeId, token) + ")";

                    int pointerId = 0;
                    if (valueAddress != 0 && className != "(void*)")
                    {
                        pointerId = Interlocked.Increment(ref debugger_object_id);
                        type = "object";
                        value =  className;
                        pointerValues[pointerId] = new PointerValue(valueAddress, typeId, name);
                    }
                    else
                    {
                        type = "symbol";
                        value = className + " " + valueAddress;
                    }
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type,
                                __custom_type = "pointer",
                                value = value,
                                description = value,
                                className,
                                objectId = $"dotnet:pointer:{pointerId}"
                            },
                            writable = false,
                            name
                        });
                    break;
                }
                case ElementType.String:
                {
                    var string_id = ret_debugger_cmd_reader.ReadInt32();
                    var value = await GetStringValue(sessionId, string_id, token);
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "string",
                                value,
                                description = value.ToString()
                            },
                            writable = false,
                            name
                        });
                    break;
                }
                case ElementType.SzArray:
                case ElementType.Array:
                {
                    var objectId = ret_debugger_cmd_reader.ReadInt32();
                    var value = await GetClassNameFromObject(sessionId, objectId, token);
                    var length = await GetArrayLength(sessionId, objectId, token);
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "object",
                                objectId = "dotnet:array:" + objectId,
                                description = $"{value.ToString()}({length})",
                                className = value.ToString(),
                                subtype = "array"
                            },
                            name
                        });
                    break;
                }
                case ElementType.Class:
                case ElementType.Object:
                {
                    var objectId = ret_debugger_cmd_reader.ReadInt32();
                    var className = "";
                    var type_id = await GetTypeIdFromObject(sessionId, objectId, false, token);
                    className = await GetTypeName(sessionId, type_id[0], token);
                    var description = className.ToString();
                    if (await IsDelegate(sessionId, objectId, token))
                    {
                        if (typeIdFromAttribute != -1)
                        {
                            className = await GetTypeName(sessionId, typeIdFromAttribute, token);
                        }

                        description = await GetDelegateMethodDescription(sessionId, objectId, token);
                        if (description == "")
                        {
                            ret = JObject.FromObject(new {
                                    value = new
                                    {
                                        type = "symbol",
                                        description = className.ToString(),
                                        value = className.ToString()
                                    },
                                    name
                                });
                            break;
                        }
                    }
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "object",
                                objectId = $"dotnet:object:{objectId}", //maybe pass here the typeId and avoid another call to debugger-agent when getting fields
                                description,
                                className,
                            },
                            name
                        });
                    break;
                }
                case ElementType.ValueType:
                {
                    var isEnum = ret_debugger_cmd_reader.ReadByte();
                    var isBoxed = ret_debugger_cmd_reader.ReadByte() == 1;
                    var typeId = ret_debugger_cmd_reader.ReadInt32();
                    var className = await GetTypeName(sessionId, typeId, token);
                    var description = className;
                    var numFields = ret_debugger_cmd_reader.ReadInt32();
                    var fields = await GetTypeFields(sessionId, typeId, token);
                    JArray valueTypeFields = new JArray();
                    if (className.IndexOf("System.Nullable<") == 0) //should we call something on debugger-agent to check???
                    {
                        ret_debugger_cmd_reader.ReadByte(); //ignoring the boolean type
                        var isNull = ret_debugger_cmd_reader.ReadInt32();
                        var value = await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, name, false, -1, token);
                        if (isNull != 0)
                            ret = value;
                        else
                            ret = JObject.FromObject(new {
                                value = new
                                {
                                    type = "object",
                                    subtype = "null",
                                    isValueType = true,
                                    className,
                                    description = className
                                },
                                name
                        });
                        break;
                    }
                    for (int i = 0; i < numFields ; i++)
                    {
                        fieldValueType = await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, fields.ElementAt(i).Name, true, fields.ElementAt(i).TypeId, token);
                        valueTypeFields.Add(fieldValueType);
                    }

                    long endPos = ret_debugger_cmd_reader.BaseStream.Position;
                    var valueTypeId = Interlocked.Increment(ref debugger_object_id);

                    ret_debugger_cmd_reader.BaseStream.Position = initialPos;
                    byte[] valueTypeBuffer = new byte[endPos - initialPos];
                    ret_debugger_cmd_reader.Read(valueTypeBuffer, 0, (int)(endPos - initialPos));
                    ret_debugger_cmd_reader.BaseStream.Position = endPos;
                    valueTypes[valueTypeId] = new ValueTypeClass(name, valueTypeBuffer, valueTypeFields, typeId, AutoExpandable(className), valueTypeId);
                    if (AutoInvokeToString(className) || isEnum == 1) {
                        int method_id = await GetMethodIdByName(sessionId, typeId, "ToString", token);
                        var retMethod = await InvokeMethod(sessionId, valueTypeBuffer, method_id, "methodRet", token);
                        description = retMethod["value"]?["value"].Value<string>();
                        if (className.Equals("System.Guid"))
                            description = description.ToUpper(); //to keep the old behavior
                    }
                    else if (isBoxed && numFields == 1) {
                        fieldValueType["name"] = name;
                        ret = fieldValueType;
                        break;
                    }

                    ret = JObject.FromObject(new {
                        value = new
                        {
                            type = "object",
                            objectId = "dotnet:valuetype:" + valueTypeId,
                            description,
                            className,
                            expanded = true,
                            isValueType = true,
                            isEnum = isEnum == 1 ? true : false
                        },
                        name
                    });
                    break;
                }
                case (ElementType)ValueTypeId.VALUE_TYPE_ID_NULL:
                {
                    string className = "";
                    ElementType variableType = (ElementType)ret_debugger_cmd_reader.ReadByte();
                    switch (variableType)
                    {
                        case ElementType.String:
                        case ElementType.Class:
                        {
                            var type_id = ret_debugger_cmd_reader.ReadInt32();
                            className = await GetTypeName(sessionId, type_id, token);
                            break;
                        }
                        case ElementType.SzArray:
                        case ElementType.Array:
                        {
                            ElementType byte_type = (ElementType)ret_debugger_cmd_reader.ReadByte();
                            var rank = ret_debugger_cmd_reader.ReadInt32();
                            if (byte_type == ElementType.Class) {
                                var internal_type_id = ret_debugger_cmd_reader.ReadInt32();
                            }
                            var type_id = ret_debugger_cmd_reader.ReadInt32();
                            className = await GetTypeName(sessionId, type_id, token);
                            break;
                        }
                        default:
                        {
                            var type_id = ret_debugger_cmd_reader.ReadInt32();
                            className = await GetTypeName(sessionId, type_id, token);
                            break;
                        }
                    }
                    ret = JObject.FromObject(new {
                            value = new
                            {
                                type = "object",
                                subtype = "null",
                                className,
                                description = className
                            },
                            name
                        });
                    break;
                }
                case (ElementType)ValueTypeId.VALUE_TYPE_ID_TYPE:
                {
                    ret = new JObject{{"Type", "void"}};
                    break;
                }
                case (ElementType)ValueTypeId.VALUE_TYPE_ID_PARENT_VTYPE:
                {
                    ret = new JObject{{"Type", "void"}};
                    break;
                }
                case (ElementType)ValueTypeId.VALUE_TYPE_ID_FIXED_ARRAY:
                {
                    ret = new JObject{{"Type", "void"}};
                    break;
                }
            }
            if (isOwn)
                ret["isOwn"] = true;
            return ret;
        }
        public async Task<bool> IsAsyncMethod(SessionId sessionId, int methodId, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(methodId);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.METHOD, (int) CmdMethod.ASYNC_DEBUG_INFO, command_params, token);
            return ret_debugger_cmd_reader.ReadByte() == 1 ; //token
        }

        public async Task<JArray> StackFrameGetValues(SessionId sessionId, MethodInfo method, int thread_id, int frame_id, VarInfo[] var_ids, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            MonoBinaryReader ret_debugger_cmd_reader = null;
            command_params_writer.Write(thread_id);
            command_params_writer.Write(frame_id);
            command_params_writer.Write(var_ids.Length);
            foreach (var var in var_ids)
            {
                command_params_writer.Write(var.Index);
            }

            if (await IsAsyncMethod(sessionId, method.DebuggerId, token))
            {
                ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.STACK_FRAME, (int) CmdFrame.GET_THIS, command_params, token);
                ret_debugger_cmd_reader.ReadByte(); //ignore type
                var objectId = ret_debugger_cmd_reader.ReadInt32();
                var asyncLocals = await GetObjectValues(sessionId, objectId, true, false, false, false, token);
                asyncLocals = new JArray(asyncLocals.Where( asyncLocal => !asyncLocal["name"].Value<string>().Contains("<>") || asyncLocal["name"].Value<string>().EndsWith("__this")));
                foreach (var asyncLocal in asyncLocals)
                {
                    if (asyncLocal["name"].Value<string>().EndsWith("__this"))
                        asyncLocal["name"] = "this";
                    else if (asyncLocal["name"].Value<string>().Contains("<"))
                        asyncLocal["name"] = Regex.Match(asyncLocal["name"].Value<string>(), @"\<([^)]*)\>").Groups[1].Value;
                }
                return asyncLocals;
            }

            JArray locals = new JArray();
            ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.STACK_FRAME, (int) CmdFrame.GET_VALUES, command_params, token);
            foreach (var var in var_ids)
            {
                var var_json = await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, var.Name, false, -1, token);
                locals.Add(var_json);
            }
            if (!method.IsStatic())
            {
                ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.STACK_FRAME, (int) CmdFrame.GET_THIS, command_params, token);
                var var_json = await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, "this", false, -1, token);
                var_json.Add("fieldOffset", -1);
                locals.Add(var_json);
            }
            return locals;

        }
        public async Task<JArray> GetValueTypeValues(SessionId sessionId, int valueTypeId, bool accessorPropertiesOnly, CancellationToken token)
        {
            if (valueTypes[valueTypeId].valueTypeJsonProps == null)
            {
                valueTypes[valueTypeId].valueTypeJsonProps = await GetPropertiesValuesOfValueType(sessionId, valueTypeId, token);
            }
            if (accessorPropertiesOnly)
                return valueTypes[valueTypeId].valueTypeJsonProps;
            var ret = new JArray(valueTypes[valueTypeId].valueTypeJson.Union(valueTypes[valueTypeId].valueTypeJsonProps));
            return ret;
        }

        public async Task<JArray> GetValueTypeProxy(SessionId sessionId, int valueTypeId, CancellationToken token)
        {
            if (valueTypes[valueTypeId].valueTypeProxy != null)
                return valueTypes[valueTypeId].valueTypeProxy;
            valueTypes[valueTypeId].valueTypeProxy = new JArray(valueTypes[valueTypeId].valueTypeJson);

            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(valueTypes[valueTypeId].typeId);

            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_PROPERTIES, command_params, token);
            var nProperties = ret_debugger_cmd_reader.ReadInt32();

            for (int i = 0 ; i < nProperties; i++)
            {
                ret_debugger_cmd_reader.ReadInt32(); //propertyId
                string propertyNameStr = ret_debugger_cmd_reader.ReadString();

                var getMethodId = ret_debugger_cmd_reader.ReadInt32();
                ret_debugger_cmd_reader.ReadInt32(); //setmethod
                ret_debugger_cmd_reader.ReadInt32(); //attrs
                if (await MethodIsStatic(sessionId, getMethodId, token))
                    continue;
                var command_params_to_proxy = new MemoryStream();
                var command_params_writer_to_proxy = new MonoBinaryWriter(command_params_to_proxy);
                command_params_writer_to_proxy.Write(getMethodId);
                command_params_writer_to_proxy.Write(valueTypes[valueTypeId].valueTypeBuffer);
                command_params_writer_to_proxy.Write(0);
                valueTypes[valueTypeId].valueTypeProxy.Add(JObject.FromObject(new {
                            get = JObject.FromObject(new {
                                commandSet = CommandSet.VM,
                                command = CmdVM.INVOKE_METHOD,
                                buffer = Convert.ToBase64String(command_params_to_proxy.ToArray()),
                                length = command_params_to_proxy.ToArray().Length
                                }),
                            name = propertyNameStr
                        }));
            }
            return valueTypes[valueTypeId].valueTypeProxy;
        }

        public async Task<JArray> GetArrayValues(SessionId sessionId, int arrayId, CancellationToken token)
        {
            var length = await GetArrayLength(sessionId, arrayId, token);
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write(arrayId);
            command_params_writer.Write(0);
            command_params_writer.Write(length);
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.ARRAY_REF, (int) CmdArray.GET_VALUES, command_params, token);
            JArray array = new JArray();
            for (int i = 0 ; i < length ; i++)
            {
                var var_json = await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, i.ToString(), false, -1, token);
                array.Add(var_json);
            }
            return array;
        }
        public async Task<bool> EnableExceptions(SessionId sessionId, string state, CancellationToken token)
        {

            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            command_params_writer.Write((byte)EventKind.EXCEPTION);
            command_params_writer.Write((byte)SuspendPolicy.SUSPEND_POLICY_NONE);
            command_params_writer.Write((byte)1);
            command_params_writer.Write((byte)ModifierKind.EXCEPTION_ONLY);
            command_params_writer.Write(0); //exc_class
            if (state == "all")
                command_params_writer.Write((byte)1); //caught
            else
                command_params_writer.Write((byte)0); //caught
            if (state == "uncaught" || state == "all")
                command_params_writer.Write((byte)1); //uncaught
            else
                command_params_writer.Write((byte)0); //uncaught
            command_params_writer.Write((byte)1);//subclasses
            command_params_writer.Write((byte)0);//not_filtered_feature
            command_params_writer.Write((byte)0);//everything_else
            var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.EVENT_REQUEST, (int) CmdEventRequest.SET, command_params, token);
            return true;
        }
        public async Task<JArray> GetObjectValues(SessionId sessionId, int objectId, bool withProperties, bool withSetter, bool accessorPropertiesOnly, bool ownProperties, CancellationToken token)
        {
            var typeId = await GetTypeIdFromObject(sessionId, objectId, true, token);
            var className = await GetTypeName(sessionId, typeId[0], token);
            JArray ret = new JArray();
            if (await IsDelegate(sessionId, objectId, token))
            {
                var description = await GetDelegateMethodDescription(sessionId, objectId, token);

                var obj = JObject.FromObject(new {
                            value = new
                            {
                                type = "symbol",
                                value = description,
                                description
                            },
                            name = "Target"
                        });
                ret.Add(obj);
                return ret;
            }
            for (int i = 0; i < typeId.Count; i++)
            {
                if (!accessorPropertiesOnly)
                {
                    var fields = await GetTypeFields(sessionId, typeId[i], token);
                    JArray objectFields = new JArray();

                    var command_params = new MemoryStream();
                    var command_params_writer = new MonoBinaryWriter(command_params);
                    command_params_writer.Write(objectId);
                    command_params_writer.Write(fields.Count);
                    foreach (var field in fields)
                    {
                            command_params_writer.Write(field.Id);
                    }

                    var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.OBJECT_REF, (int) CmdObject.REF_GET_VALUES, command_params, token);

                    foreach (var field in fields)
                    {
                        long initialPos = ret_debugger_cmd_reader.BaseStream.Position;
                        int valtype = ret_debugger_cmd_reader.ReadByte();
                        ret_debugger_cmd_reader.BaseStream.Position = initialPos;
                        var fieldValue = await CreateJObjectForVariableValue(sessionId, ret_debugger_cmd_reader, field.Name, i == 0, field.TypeId, token);

                        if (ret.Where(attribute => attribute["name"].Value<string>().Equals(fieldValue["name"].Value<string>())).Any()) {
                            continue;
                        }
                        if (withSetter)
                        {
                            var command_params_to_set = new MemoryStream();
                            var command_params_writer_to_set = new MonoBinaryWriter(command_params_to_set);
                            command_params_writer_to_set.Write(objectId);
                            command_params_writer_to_set.Write(1);
                            command_params_writer_to_set.Write(field.Id);

                            fieldValue.Add("set", JObject.FromObject(new {
                                        commandSet = CommandSet.OBJECT_REF,
                                        command = CmdObject.REF_SET_VALUES,
                                        buffer = Convert.ToBase64String(command_params_to_set.ToArray()),
                                        valtype,
                                        length = command_params_to_set.ToArray().Length
                                }));
                        }
                        objectFields.Add(fieldValue);
                    }
                    ret = new JArray(ret.Union(objectFields));
                }
                if (!withProperties)
                    return ret;
                var command_params_obj = new MemoryStream();
                var command_params_obj_writer = new MonoBinaryWriter(command_params_obj);
                command_params_obj_writer.WriteObj(new DotnetObjectId("object", $"{objectId}"), this);
                var props = await CreateJArrayForProperties(sessionId, typeId[i], command_params_obj.ToArray(), ret, false, $"dotnet:object:{objectId}", i == 0, token);
                ret = new JArray(ret.Union(props));

                // ownProperties
                // Note: ownProperties should mean that we return members of the klass itself,
                // but we are going to ignore that here, because otherwise vscode/chrome don't
                // seem to ask for inherited fields at all.
                //if (ownProperties)
                    //break;
                /*if (accessorPropertiesOnly)
                    break;*/
            }
            if (accessorPropertiesOnly)
            {
                var retAfterRemove = new JArray();
                List<List<FieldTypeClass>> allFields = new List<List<FieldTypeClass>>();
                for (int i = 0; i < typeId.Count; i++)
                {
                    var fields = await GetTypeFields(sessionId, typeId[i], token);
                    allFields.Add(fields);
                }
                foreach (var item in ret)
                {
                    bool foundField = false;
                    for (int j = 0 ; j <  allFields.Count; j++)
                    {
                        foreach (var field in allFields[j])
                        {
                            if (field.Name.Equals(item["name"].Value<string>())) {
                                if (item["isOwn"] == null || (item["isOwn"].Value<bool>() && j == 0) || !item["isOwn"].Value<bool>())
                                    foundField = true;
                                break;
                            }
                        }
                        if (foundField)
                            break;
                    }
                    if (!foundField) {
                        retAfterRemove.Add(item);
                    }
                }
                ret = retAfterRemove;
            }
            return ret;
        }

        public async Task<JArray> GetObjectProxy(SessionId sessionId, int objectId, CancellationToken token)
        {
            var ret = await GetObjectValues(sessionId, objectId, false, true, false, false, token);
            var typeIds = await GetTypeIdFromObject(sessionId, objectId, true, token);
            foreach (var typeId in typeIds)
            {
                var command_params = new MemoryStream();
                var command_params_writer = new MonoBinaryWriter(command_params);
                command_params_writer.Write(typeId);

                var ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.TYPE, (int) CmdType.GET_PROPERTIES, command_params, token);
                var nProperties = ret_debugger_cmd_reader.ReadInt32();
                for (int i = 0 ; i < nProperties; i++)
                {
                    ret_debugger_cmd_reader.ReadInt32(); //propertyId
                    string propertyNameStr = ret_debugger_cmd_reader.ReadString();
                    var getMethodId = ret_debugger_cmd_reader.ReadInt32();
                    var setMethodId = ret_debugger_cmd_reader.ReadInt32(); //setmethod
                    var attrValue = ret_debugger_cmd_reader.ReadInt32(); //attrs
                    //Console.WriteLine($"{propertyNameStr} - {attrValue}");
                    if (ret.Where(attribute => attribute["name"].Value<string>().Equals(propertyNameStr)).Any())
                    {
                        var attr = ret.Where(attribute => attribute["name"].Value<string>().Equals(propertyNameStr)).First();

                        var command_params_to_set = new MemoryStream();
                        var command_params_writer_to_set = new MonoBinaryWriter(command_params_to_set);
                        command_params_writer_to_set.Write(setMethodId);
                        command_params_writer_to_set.Write((byte)ElementType.Class);
                        command_params_writer_to_set.Write(objectId);
                        command_params_writer_to_set.Write(1);
                        if (attr["set"] != null)
                        {
                            attr["set"] = JObject.FromObject(new {
                                        commandSet = CommandSet.VM,
                                        command = CmdVM.INVOKE_METHOD,
                                        buffer = Convert.ToBase64String(command_params_to_set.ToArray()),
                                        valtype = attr["set"]["valtype"],
                                        length = command_params_to_set.ToArray().Length
                                });
                        }
                        continue;
                    }
                    else
                    {
                        var command_params_to_get = new MemoryStream();
                        var command_params_writer_to_get = new MonoBinaryWriter(command_params_to_get);
                        command_params_writer_to_get.Write(getMethodId);
                        command_params_writer_to_get.Write((byte)ElementType.Class);
                        command_params_writer_to_get.Write(objectId);
                        command_params_writer_to_get.Write(0);

                        ret.Add(JObject.FromObject(new {
                                get = JObject.FromObject(new {
                                    commandSet = CommandSet.VM,
                                    command = CmdVM.INVOKE_METHOD,
                                    buffer = Convert.ToBase64String(command_params_to_get.ToArray()),
                                    length = command_params_to_get.ToArray().Length
                                    }),
                                name = propertyNameStr
                            }));
                    }
                    if (await MethodIsStatic(sessionId, getMethodId, token))
                        continue;
                }
            }
            return ret;
        }

        public async Task<bool> SetVariableValue(SessionId sessionId, int thread_id, int frame_id, int varId, string newValue, CancellationToken token)
        {
            var command_params = new MemoryStream();
            var command_params_writer = new MonoBinaryWriter(command_params);
            MonoBinaryReader ret_debugger_cmd_reader = null;
            command_params_writer.Write(thread_id);
            command_params_writer.Write(frame_id);
            command_params_writer.Write(1);
            command_params_writer.Write(varId);
            JArray locals = new JArray();
            ret_debugger_cmd_reader = await SendDebuggerAgentCommand(sessionId, (int) CommandSet.STACK_FRAME, (int) CmdFrame.GET_VALUES, command_params, token);
            int etype = ret_debugger_cmd_reader.ReadByte();
            try
            {
                ret_debugger_cmd_reader = await SendDebuggerAgentCommandWithParms(sessionId, (int) CommandSet.STACK_FRAME, (int) CmdFrame.SET_VALUES, command_params, etype, newValue, token);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
