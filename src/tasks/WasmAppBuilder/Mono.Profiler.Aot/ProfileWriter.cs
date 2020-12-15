// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Buffers.Binary;

namespace Mono.Profiler.Aot {
    //
    // Write the contents of a .aotprofile
    // See mono/profiler/aot.h for a description of the file format
    //
    public sealed class ProfileWriter : ProfileBase {
        private Stream? s_stream;
        private int s_id;
        private byte[] s_intBuf = new byte [4];

        private readonly Dictionary<TypeRecord, int> s_typeIds = new Dictionary<TypeRecord, int> ();
        private readonly Dictionary<ModuleRecord, int> s_moduleIds = new Dictionary<ModuleRecord, int> ();

        private void WriteInt32 (int intValue)
        {
            BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(s_intBuf), intValue);
            for (int i = 0; i < 4; i++)
                s_stream!.WriteByte (s_intBuf [i]);
        }

        private void WriteString (string str)
        {
            WriteInt32 (str.Length);
            var buf = Encoding.UTF8.GetBytes (str);
            s_stream!.Write (buf, 0, buf.Length);
        }

        private int AddModule (ModuleRecord m)
        {
            int mId;
            if (s_moduleIds.TryGetValue (m, out mId))
                return mId;

            mId = s_id++;
            s_moduleIds [m] = mId;

            WriteRecord (RecordType.IMAGE, mId);
            WriteString (m.Name);
            WriteString (m.Mvid);

            return mId;
        }

        private int AddType (TypeRecord t)
        {
            int tId;
            if (s_typeIds.TryGetValue (t, out tId))
                return tId;

            var moduleId = AddModule (t.Module!);

            int instId = -1;
            if (t.GenericInst != null)
                instId = AddGenericInstance (t.GenericInst);

            tId = s_id++;
            s_typeIds [t] = tId;

            WriteRecord (RecordType.TYPE, tId);
            s_stream!.WriteByte ((byte)MonoTypeEnum.MONO_TYPE_CLASS);
            WriteInt32 (moduleId);
            WriteInt32 (instId);
            WriteString (t.Name);

            return tId;
        }

        private int AddGenericInstance (GenericInstRecord gi)
        {
            // add the types first, before we start writing the GINST record
            for (int i = 0; i < gi.Types.Length; i++)
                AddType (gi.Types [i]);

            var gId = s_id++;

            WriteRecord (RecordType.GINST, gId);
            WriteInt32 (gi.Types.Length);

            for (int i = 0; i < gi.Types.Length; i++)
                WriteInt32 (AddType (gi.Types [i]));

            return gId;
        }

        private void WriteMethod (MethodRecord m)
        {
            var typeId = AddType (m.Type);

            int instId = -1;
            if (m.GenericInst != null)
                instId = AddGenericInstance (m.GenericInst);

            WriteRecord (RecordType.METHOD, s_id++);
            WriteInt32 (typeId);
            WriteInt32 (instId);
            WriteInt32 (m.ParamCount);
            WriteString (m.Name);
            WriteString (m.Signature);
        }

        private void WriteRecord (RecordType rt, int value)
        {
            s_stream!.WriteByte ((byte)rt);
            WriteInt32 (value);
        }

        public void WriteAllData (Stream s, ProfileData data)
        {
            s_stream = s;

            var buf = Encoding.UTF8.GetBytes (MAGIC);
            s_stream.Write (buf, 0, buf.Length);

            WriteInt32 ((MAJOR_VERSION << 16) | MINOR_VERSION);

            foreach (var m in data.Methods!)
                WriteMethod (m);

            // make sure ew have all the types
            // sometime the profile contain type, which is not referenced from the methods
            foreach (var t in data.Types!)
                AddType (t);

            // just to be complete, do not miss any module too
            foreach (var module in data.Modules!)
                AddModule (module);

            WriteRecord (RecordType.NONE, 0);
        }
    }
}
