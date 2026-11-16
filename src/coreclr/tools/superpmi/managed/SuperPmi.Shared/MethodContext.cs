// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace SuperPmi;

// All the EE-JIT details required to replay the compilation of a method. The CompileResult
// holds what the compilation produced.
//
// Serialized form: 'm' 'c', a 4-byte length, the packets, then '4' '2'. Each packet is a 1-byte
// PacketId, a 4-byte size, the serialized map, and a 0x42 canary.
public sealed partial class MethodContext
{
    private const int HeaderSize = 2 /* 'm', 'c' */ + sizeof(uint) /* data length */;
    private const int TrailerSize = 2; /* '4', '2' */
    private const int PacketOverhead = 1 /* packet id */ + sizeof(uint) /* size */ + 1 /* canary */;
    private const byte PacketCanary = 0x42;

    private static readonly MapInfo[] s_mapByPacket;

    public int index = -1;
    public CompileResult cr = new CompileResult();
    public bool ignoreStoredConfig;

    // Runs after the field initializers of all the partial declarations, including s_maps.
    static MethodContext()
    {
        s_mapByPacket = BuildPacketTable(s_maps);
    }

    internal static MapInfo[] BuildPacketTable(MapInfo[] maps)
    {
        var table = new MapInfo[256];
        foreach (MapInfo map in maps)
            table[(byte)map.Packet] = map;
        return table;
    }

    // 'buff' holds the 'totalLen' bytes of packet data followed by the 2-byte end canary.
    // Returns false (and logs) if the data is malformed.
    public static bool Initialize(int mcIndex, ReadOnlySpan<byte> buff, uint totalLen, bool readCompileResults, out MethodContext mc)
    {
        mc = new MethodContext { index = mcIndex };
        try
        {
            mc.MethodInitHelper(buff, totalLen, readCompileResults);
            return true;
        }
        catch (SpmiException e) when (e.Code is ExceptionCodes.MC or ExceptionCodes.LWM)
        {
            Logger.LogError($"Method {mcIndex} is of low integrity.");
            return false;
        }
    }

    // Reads one method context (header, data and end canary) from the current position of 'stream'.
    public static bool Initialize(int mcIndex, Stream stream, bool readCompileResults, out MethodContext mc)
    {
        mc = new MethodContext { index = mcIndex };
        try
        {
            Span<byte> header = stackalloc byte[HeaderSize];
            ErrorHandling.AssertCode(stream.ReadAtLeast(header, HeaderSize, throwOnEndOfStream: false) == HeaderSize,
                ExceptionCodes.MC, "ReadFile(hFile, buff, 2 + sizeof(unsigned int), &bytesRead, NULL) == TRUE");
            ErrorHandling.AssertCodeMsg(header[0] == 'm' && header[1] == 'c', ExceptionCodes.MC,
                "(buff[0] == 'm') && (buff[1] == 'c')", "Didn't find magic number");
            uint totalLen = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(2));

            byte[] buff2 = new byte[(long)totalLen + TrailerSize];
            ErrorHandling.AssertCode(stream.ReadAtLeast(buff2, buff2.Length, throwOnEndOfStream: false) == buff2.Length,
                ExceptionCodes.MC, "ReadFile(hFile, buff2, totalLen + 2, &bytesRead, NULL) == TRUE");
            ErrorHandling.AssertCodeMsg(buff2[totalLen] == '4' && buff2[totalLen + 1] == '2', ExceptionCodes.MC,
                "(buff2[totalLen] == '4') && (buff2[totalLen + 1] == '2')", "Didn't find end canary");

            mc.MethodInitHelper(buff2, totalLen, readCompileResults);
            return true;
        }
        catch (SpmiException e) when (e.Code is ExceptionCodes.MC or ExceptionCodes.LWM)
        {
            Logger.LogError($"Method {mcIndex} is of low integrity.");
            return false;
        }
    }

    private void MethodInitHelper(ReadOnlySpan<byte> buff2, uint totalLen, bool readCompileResults)
    {
        int buffIndex = 0;
        while (buffIndex < totalLen)
        {
            ErrorHandling.AssertCodeMsg(buffIndex + 1 + sizeof(uint) <= buff2.Length, ExceptionCodes.MC,
                "buffIndex + 5 <= totalLen", "Packet header runs past the end of the method context");
            byte packetType = buff2[buffIndex++];
            uint localsize = BinaryPrimitives.ReadUInt32LittleEndian(buff2.Slice(buffIndex));
            buffIndex += sizeof(uint);
            ErrorHandling.AssertCodeMsg((long)buffIndex + localsize + 1 <= buff2.Length, ExceptionCodes.MC,
                "buffIndex + localsize <= totalLen", "Packet runs past the end of the method context");
            ReadOnlySpan<byte> packetData = buff2.Slice(buffIndex, (int)localsize);

            MapInfo map = s_mapByPacket[packetType];
            if (map != null)
            {
                _maps[map.Index] = ReadMap(map, packetData);
            }
            else if ((map = CompileResult.s_mapByPacket[packetType]) != null)
            {
                if (readCompileResults)
                    cr.SetMap(map.Index, ReadMap(map, packetData));
            }
            else
            {
                ErrorHandling.LogException(ExceptionCodes.MC,
                    $"Read ran into unknown packet type {packetType}. Are you using a newer recorder?");
            }

            buffIndex += (int)localsize;
            byte canary = buff2[buffIndex++];
            ErrorHandling.AssertCodeMsg(canary == PacketCanary, ExceptionCodes.MC, "canary == 0x42", "Didn't find trailing canary for map");
        }

        ErrorHandling.AssertCodeMsg(buffIndex + 1 < buff2.Length && buff2[buffIndex] == '4' && buff2[buffIndex + 1] == '2',
            ExceptionCodes.MC, "(buff2[buffIndex++] == '4') && (buff2[buffIndex++] == '2')", "Didn't find trailing canary for map");
    }

    private static LightWeightMapBuffer ReadMap(MapInfo map, ReadOnlySpan<byte> data)
    {
        LightWeightMapBuffer result = map.Create();
        result.ReadFromArray(data);
        return result;
    }

    public uint calculateFileSize()
    {
        uint totalLen = 0;
        foreach (LightWeightMapBuffer map in _maps)
            totalLen += PacketSize(map);
        if (cr != null)
        {
            foreach (LightWeightMapBuffer map in cr.Maps)
                totalLen += PacketSize(map);
        }
        return totalLen;
    }

    private static uint PacketSize(LightWeightMapBuffer map)
        => map != null && map.GetCount() != 0 ? map.CalculateArraySize() + PacketOverhead : 0;

    public uint calculateRawFileSize() => HeaderSize + calculateFileSize() + TrailerSize;

    public byte[] Serialize()
    {
        uint totalLen = calculateFileSize();
        byte[] buff2 = new byte[HeaderSize + totalLen + TrailerSize];
        int buffIndex = 0;
        buff2[buffIndex++] = (byte)'m';
        buff2[buffIndex++] = (byte)'c';
        BinaryPrimitives.WriteUInt32LittleEndian(buff2.AsSpan(buffIndex), totalLen);
        buffIndex += sizeof(uint);

        for (int i = 0; i < _maps.Length; i++)
            buffIndex = WritePacket(buff2, buffIndex, s_maps[i].Packet, _maps[i]);

        if (cr != null)
        {
            LightWeightMapBuffer[] crMaps = cr.Maps;
            for (int i = 0; i < crMaps.Length; i++)
                buffIndex = WritePacket(buff2, buffIndex, CompileResult.s_maps[i].Packet, crMaps[i]);
        }

        buff2[buffIndex++] = (byte)'4';
        buff2[buffIndex++] = (byte)'2';
        ErrorHandling.AssertCode(buffIndex == buff2.Length, ExceptionCodes.ASSERT, "buffIndex == totalFileSize");
        return buff2;
    }

    private static int WritePacket(byte[] buff2, int buffIndex, PacketId packet, LightWeightMapBuffer map)
    {
        if (map == null || map.GetCount() == 0)
            return buffIndex;

        buff2[buffIndex++] = (byte)packet;
        uint loc = map.DumpToArray(buff2.AsSpan(buffIndex + sizeof(uint)));
        BinaryPrimitives.WriteUInt32LittleEndian(buff2.AsSpan(buffIndex), loc);
        buffIndex += sizeof(uint) + (int)loc;
        buff2[buffIndex++] = PacketCanary;
        return buffIndex;
    }

    public uint saveToFile(Stream stream)
    {
        byte[] data = Serialize();
        stream.Write(data);
        return (uint)data.Length;
    }

    // Statistics about each map: count of elements and total serialized size, as CSV columns.
    public void dumpStatToBuffer(StringBuilder sb)
    {
        foreach (LightWeightMapBuffer map in _maps)
            DumpStat(sb, map);
        foreach (LightWeightMapBuffer map in cr.Maps)
            DumpStat(sb, map);
    }

    private static void DumpStat(StringBuilder sb, LightWeightMapBuffer map)
    {
        if (map != null && map.GetCount() > 0)
            sb.Append(map.GetCount());
        sb.Append(',');
        if (map != null)
            sb.Append(map.CalculateArraySize());
        sb.Append(',');
    }

    public static void dumpStatTitleToBuffer(StringBuilder sb)
    {
        foreach (MapInfo map in s_maps)
            sb.Append(map.Name).Append(',').Append(map.Name).Append(" SZ,");
        foreach (MapInfo map in CompileResult.s_maps)
            sb.Append("CR_").Append(map.Name).Append(",CR_").Append(map.Name).Append(" SZ,");
    }
}
