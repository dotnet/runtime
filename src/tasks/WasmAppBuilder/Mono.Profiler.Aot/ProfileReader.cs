// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Buffers.Binary;

namespace Mono.Profiler.Aot
{
    //
    // Read the contents of a .aotprofile created by the AOT profiler
    // See mono-profiler-aot.h for a description of the file format
    //
    public sealed class ProfileReader : ProfileBase
    {
        private byte[]? s_data;
        private int s_pos;

        public ProfileReader()
        {
        }

        private int ReadByte()
        {
            int res = s_data! [s_pos];
            s_pos++;
            return res;
        }

        private int ReadInt()
        {
            int res = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(s_data!, s_pos, 4));
            s_pos += 4;
            return res;
        }

        private string ReadString()
        {
            int len = ReadInt();
            var res = new string(Encoding.UTF8.GetChars(s_data!, s_pos, len));
            s_pos += len;
            return res;
        }

        public ProfileData ReadAllData(Stream stream)
        {
            byte[] buf = new byte [16];
            int len = stream.Read(buf, 0, MAGIC.Length);
            if (len != MAGIC.Length)
                throw new IOException("Input file is too small.");
            var magic = new string(Encoding.UTF8.GetChars(buf, 0, MAGIC.Length));
            if (magic != MAGIC)
                throw new IOException("Input file is not a AOT profiler output file.");

            // Profile files are not expected to be large, so reading them is ok
            len =(int)stream.Length - MAGIC.Length;
            s_data = new byte [len];
            s_pos = 0;
            int count = stream.Read(s_data, 0, len);
            if (count != len)
                throw new IOException("Can't read profile file.");

            int version = ReadInt();
            int expected_version =(MAJOR_VERSION << 16) | MINOR_VERSION;
            if (version != expected_version)
                throw new IOException(string.Format("Expected file version 0x{0:x}, got 0x{1:x}.", expected_version, version));

            var modules = new List<ModuleRecord>();
            var types = new List<TypeRecord>();
            var methods = new List<MethodRecord>();

            Dictionary<int, ProfileRecord> records = new Dictionary<int, ProfileRecord>();

            while (true) {
                RecordType rtype =(RecordType)s_data [s_pos];
                s_pos++;
                if (rtype == RecordType.NONE)
                    break;
                int id = ReadInt();
                switch (rtype) {
                case RecordType.IMAGE: {
                    string name = ReadString();
                    string mvid = ReadString();
                    var module = new ModuleRecord(id, name, mvid);
                    records [id] = module;
                    modules.Add(module);
                    break;
                }
                case RecordType.GINST: {
                    int argc = ReadInt();

                    TypeRecord[] tr = new TypeRecord [argc];
                    for (int i = 0; i < argc; ++i) {
                        int type_id = ReadInt();
                        tr [i] =(TypeRecord)records [type_id];
                    }
                    var ginst = new GenericInstRecord(id, tr);
                    records [id] = ginst;
                    break;
                }
                case RecordType.TYPE: {
                    MonoTypeEnum ttype =(MonoTypeEnum)ReadByte();

                    switch (ttype) {
                    case MonoTypeEnum.MONO_TYPE_CLASS: {
                        int image_id = ReadInt();
                        int ginst_id = ReadInt();
                        string name = ReadString();

                        GenericInstRecord? inst = null;
                        if (ginst_id != -1)
                            inst =(GenericInstRecord)records [ginst_id];

                        var module =(ModuleRecord)records [image_id];
                        var type = new TypeRecord(id, module, name, inst);
                        types.Add(type);
                        records [id] = type;
                        break;
                    }
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                }
                case RecordType.METHOD: {
                    int class_id = ReadInt();
                    int ginst_id = ReadInt();
                    int param_count = ReadInt();
                    string name = ReadString();
                    string sig = ReadString();

                    var type =(TypeRecord)records [class_id];
                    GenericInstRecord? ginst = ginst_id != -1 ? (GenericInstRecord)records [ginst_id] : null;
                    var method = new MethodRecord(id, type, ginst, name, sig, param_count);
                    methods.Add(method);
                    records [id] = method;
                    break;
                }
                default:
                    throw new NotImplementedException(rtype.ToString());
                }
            }

            return new ProfileData(modules.ToArray(), types.ToArray(), methods.ToArray());
        }
    }

}