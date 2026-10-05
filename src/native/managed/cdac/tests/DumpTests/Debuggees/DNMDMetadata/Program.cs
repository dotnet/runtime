// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;

internal static class Program
{
    private const int FirstDNMDContractRuntimeVersion = 12;

    private static void Main()
    {
        if (Environment.Version.Major >= FirstDNMDContractRuntimeVersion)
        {
            if (!MetadataUpdater.IsSupported)
                throw new InvalidOperationException("Metadata updates are not enabled for the dump debuggee.");

            MetadataUpdater.ApplyUpdate(typeof(Program).Assembly,
                CreateDenseTypeRefDelta(typeof(Program).Module.ModuleVersionId), [0], []);
        }

        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DNMDMetadataAssembly"), AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("DNMDMetadataModule");
        TypeBuilder builder = module.DefineType("DNMDDump.Sample", TypeAttributes.Public);
        builder.DefineField("Value", typeof(int), FieldAttributes.Public);
        Type type = builder.CreateType();

        GC.KeepAlive(assembly);
        GC.KeepAlive(module);
        GC.KeepAlive(type);
        Environment.FailFast("cDAC DNMD metadata dump test");
    }

    private static byte[] CreateDenseTypeRefDelta(Guid mvid)
    {
        List<byte> image = [];

        Write32(0x424A5342); // ECMA-335 II.24.2.1 metadata signature
        Write16(1);
        Write16(1);
        Write32(0);
        Write32(12);
        image.AddRange(Encoding.ASCII.GetBytes("v4.0.30319"));
        image.Add(0);
        while (image.Count % sizeof(uint) != 0)
            image.Add(0);
        Write16(0);
        Write16(4);

        int jtd = StreamHeader("#JTD", 0);
        int strings = StreamHeader("#Strings", 16);
        int guid = StreamHeader("#GUID", 32);
        int tables = StreamHeader("#-", 62);

        Patch32(jtd, (uint)image.Count);
        Patch32(strings, (uint)image.Count);
        image.Add(0);
        image.AddRange(Encoding.ASCII.GetBytes("Example"));
        image.Add(0);
        image.AddRange(Encoding.ASCII.GetBytes("Added"));
        image.Add(0);
        while (image.Count % sizeof(uint) != 0)
            image.Add(0);

        Patch32(guid, (uint)image.Count);
        image.AddRange(mvid.ToByteArray());
        image.AddRange(new Guid("adc561fe-9740-4578-8cc2-d19356de33af").ToByteArray());

        int tablesStart = image.Count;
        Patch32(tables, (uint)tablesStart);
        Write32(0);
        image.AddRange([2, 0, 0, 1]);
        Write64((1ul << 0) | (1ul << 1) | (1ul << 30));
        Write64(0);
        Write32(1);
        Write32(1);
        Write32(1);

        Write16(0);
        Write16(0);
        Write16(1);
        Write16(2);
        Write16(0);

        Write32(4);
        Write16(9);
        Write16(1);

        Write32(0x01000001);
        Write32(0);

        Patch32(tables + sizeof(uint), (uint)(image.Count - tablesStart));
        return image.ToArray();

        void Write16(ushort value)
        {
            image.Add((byte)value);
            image.Add((byte)(value >> 8));
        }

        void Write32(uint value)
        {
            for (int i = 0; i < sizeof(uint); i++)
                image.Add((byte)(value >> (i * 8)));
        }

        void Write64(ulong value)
        {
            Write32((uint)value);
            Write32((uint)(value >> 32));
        }

        void Patch32(int offset, uint value)
        {
            for (int i = 0; i < sizeof(uint); i++)
                image[offset + i] = (byte)(value >> (i * 8));
        }

        int StreamHeader(string name, uint size)
        {
            int offset = image.Count;
            Write32(0);
            Write32(size);
            image.AddRange(Encoding.ASCII.GetBytes(name));
            image.Add(0);
            while (image.Count % sizeof(uint) != 0)
                image.Add(0);
            return offset;
        }
    }
}
