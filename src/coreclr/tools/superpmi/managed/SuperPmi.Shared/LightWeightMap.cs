// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SuperPmi;

// Describes one map of a MethodContext or CompileResult: its packet, and how to create it.
public sealed class MapInfo
{
    private readonly Func<LightWeightMapBuffer> _factory;

    public MapInfo(int index, string name, PacketId packet, Func<LightWeightMapBuffer> factory)
    {
        Index = index;
        Name = name;
        Packet = packet;
        _factory = factory;
    }

    public int Index { get; }
    public string Name { get; }
    public PacketId Packet { get; }

    public LightWeightMapBuffer Create() => _factory();
}

// Dumps a map as a "name - count" line followed by one "index-<entry>" line per entry.
internal static class MapDumper
{
    public static void Dump<TKey, TItem>(string name, LightWeightMap<TKey, TItem> map, Action<TKey, TItem> dmp)
        where TKey : unmanaged
        where TItem : unmanaged
    {
        if (map == null)
            return;

        Logger.Out.Write($"{name} - {map.GetCount()}\n");
        for (int i = 0; i < map.GetCount(); i++)
        {
            Logger.Out.Write($"{i}-");
            dmp(map.GetRawKeys()[i], map.GetRawItems()[i]);
            Logger.Out.Write("\n");
        }
    }

    public static void Dump<TItem>(string name, DenseLightWeightMap<TItem> map, Action<uint, TItem> dmp)
        where TItem : unmanaged
    {
        if (map == null)
            return;

        Logger.Out.Write($"{name} - {map.GetCount()}\n");
        for (int i = 0; i < map.GetCount(); i++)
        {
            Logger.Out.Write($"{i}-");
            dmp((uint)i, map.GetRawItems()[i]);
            Logger.Out.Write("\n");
        }
    }
}

// The raw buffer shared by all the maps. Each entry is stored as a 4-byte length followed by the data;
// the "offset" of an entry is the position of its data.
public abstract class LightWeightMapBuffer
{
    private const int LengthPrefixSize = sizeof(uint);

    protected byte[] buffer = [];
    protected uint bufferLength;
    private byte[] pinnedBuffer;
    private List<byte[]> retiredPinnedBuffers;
    private bool locked;

    public abstract uint GetCount();

    public abstract uint CalculateArraySize();

    public abstract void ReadFromArray(ReadOnlySpan<byte> rawData);

    public abstract uint DumpToArray(Span<byte> bytes);

    public uint AddBuffer(ReadOnlySpan<byte> buff, bool forceUnique = false)
    {
        if (buff.Length == 0)
            return uint.MaxValue;

        int index = Contains(buff);
        if (index != -1 && !forceUnique)
            return (uint)index;

        if (locked)
            Logger.LogError("Added item that extended the buffer after it was locked by a call to GetBuffer()");

        Span<byte> entry = Grow((uint)buff.Length, out uint newOffset);
        buff.CopyTo(entry);
        return newOffset;
    }

    // Reserves a zero-filled entry of the given length and returns its offset.
    public uint CreateBuffer(uint len)
    {
        if (len == 0)
            return uint.MaxValue;

        if (locked)
            Logger.LogError("Added item that extended the buffer after it was locked by a call to GetBuffer()");

        Grow(len, out uint newOffset);
        return newOffset;
    }

    private Span<byte> Grow(uint len, out uint dataOffset)
    {
        uint newLength = bufferLength + LengthPrefixSize + len;
        if (newLength > buffer.Length)
            Array.Resize(ref buffer, (int)Math.Max(newLength, Math.Min((long)buffer.Length * 2, Array.MaxLength)));

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan((int)bufferLength), len);
        dataOffset = bufferLength + LengthPrefixSize;
        bufferLength = newLength;
        return buffer.AsSpan((int)dataOffset, (int)len);
    }

    // Returns the buffer contents starting at 'offset' (to the end of the buffer), or an empty span
    // for the "no buffer" offset (uint.MaxValue).
    public ReadOnlySpan<byte> GetBuffer(uint offset)
    {
        if (offset == uint.MaxValue)
            return default;

        if (offset >= bufferLength)
        {
            ErrorHandling.AssertCodeMsg(false, ExceptionCodes.LWM, "offset < bufferLength",
                $"Hit offset bigger than bufferLength {offset} >= {bufferLength}");
        }
        locked = true;
        return buffer.AsSpan((int)offset, (int)(bufferLength - offset));
    }

    // A stable pointer to the buffer contents at 'offset', or null for the "no buffer" offset. The
    // buffer is copied to a pinned array on first use, so the pointer is valid for the map's lifetime.
    public unsafe byte* GetBufferPointer(uint offset)
    {
        if (GetBuffer(offset).IsEmpty)
            return null;

        if (pinnedBuffer == null || pinnedBuffer.Length < bufferLength)
        {
            // Entries added after pinning get a new copy; the old one stays alive for earlier pointers.
            if (pinnedBuffer != null)
                (retiredPinnedBuffers ??= []).Add(pinnedBuffer);
            pinnedBuffer = GC.AllocateUninitializedArray<byte>((int)bufferLength, pinned: true);
            buffer.AsSpan(0, (int)bufferLength).CopyTo(pinnedBuffer);
        }
        return (byte*)Unsafe.AsPointer(ref pinnedBuffer[offset]);
    }

    // Mutable access to a buffer entry, as used by the recorder to fill a CreateBuffer() entry.
    public Span<byte> GetBufferForWrite(uint offset, uint length) => buffer.AsSpan((int)offset, (int)length);

    // Returns exactly the stored entry at 'offset', using its length prefix.
    public ReadOnlySpan<byte> GetBufferEntry(uint offset)
    {
        ReadOnlySpan<byte> data = GetBuffer(offset);
        if (data.IsEmpty)
            return data;

        uint len = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan((int)offset - LengthPrefixSize));
        return data.Slice(0, (int)len);
    }

    // Returns the null-terminated string stored at 'offset', or null for the "no buffer" offset.
    public string GetString(uint offset)
    {
        if (offset == uint.MaxValue)
            return null;

        ReadOnlySpan<byte> data = GetBuffer(offset);
        int nul = data.IndexOf((byte)0);
        return System.Text.Encoding.UTF8.GetString(nul >= 0 ? data.Slice(0, nul) : data);
    }

    public int Contains(ReadOnlySpan<byte> buff)
    {
        if (buff.Length == 0 || bufferLength == 0)
            return -1;

        uint offset = 0;
        while (offset + LengthPrefixSize + (uint)buff.Length <= bufferLength)
        {
            uint chunkLen = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan((int)offset));
            if (chunkLen == (uint)buff.Length &&
                buffer.AsSpan((int)(offset + LengthPrefixSize), buff.Length).SequenceEqual(buff))
            {
                return (int)(offset + LengthPrefixSize);
            }
            offset += LengthPrefixSize + chunkLen;
        }
        return -1;
    }

    public void Unlock() => locked = false;

    protected void ForgetPinnedBuffers()
    {
        pinnedBuffer = null;
        retiredPinnedBuffers = null;
    }

    protected void ReadBuffer(ReadOnlySpan<byte> rawData, ref int ptr)
    {
        buffer = Take(rawData, ref ptr, (int)bufferLength).ToArray();
    }

    protected int WriteBuffer(Span<byte> bytes, int ptr)
    {
        buffer.AsSpan(0, (int)bufferLength).CopyTo(bytes.Slice(ptr));
        return ptr + (int)bufferLength;
    }

    protected static uint ReadUInt32(ReadOnlySpan<byte> rawData, ref int ptr)
        => BinaryPrimitives.ReadUInt32LittleEndian(Take(rawData, ref ptr, sizeof(uint)));

    protected static ReadOnlySpan<byte> Take(ReadOnlySpan<byte> rawData, ref int ptr, int length)
    {
        if (length < 0 || (long)ptr + length > rawData.Length)
        {
            ErrorHandling.AssertCodeMsg(false, ExceptionCodes.LWM, "ptr + length <= size",
                $"Ran past the end of the map data {ptr + (long)length:x} > {rawData.Length:x}");
        }
        ReadOnlySpan<byte> result = rawData.Slice(ptr, length);
        ptr += length;
        return result;
    }

    protected static void CheckSize(int ptr, int size)
    {
        if (ptr != size)
        {
            ErrorHandling.AssertCodeMsg(false, ExceptionCodes.LWM, "(unsigned int)(ptr - rawData) == size",
                $"Unknown type - Ended with unexpected sizes {ptr:x} != {size:x}");
        }
    }

    protected static int ByteSize<T>(uint count) where T : unmanaged => checked((int)count * Unsafe.SizeOf<T>());
}

public sealed class LightWeightMap<TKey, TItem> : LightWeightMapBuffer
    where TKey : unmanaged
    where TItem : unmanaged
{
    private static ReadOnlySpan<byte> Tag => "LWM1"u8;

    private uint numItems;
    private TKey[] pKeys = [];
    private TItem[] pItems = [];

    public override uint GetCount() => numItems;

    public override void ReadFromArray(ReadOnlySpan<byte> rawData)
    {
        int ptr = 0;

        // The tag is optional, to roll forward previous formats which don't have
        // the tag, but which also have the same format.
        if (rawData.StartsWith(Tag))
            ptr += Tag.Length;

        numItems = ReadUInt32(rawData, ref ptr);
        if (numItems > 0)
        {
            bufferLength = ReadUInt32(rawData, ref ptr);
            pKeys = MemoryMarshal.Cast<byte, TKey>(Take(rawData, ref ptr, ByteSize<TKey>(numItems))).ToArray();
            pItems = MemoryMarshal.Cast<byte, TItem>(Take(rawData, ref ptr, ByteSize<TItem>(numItems))).ToArray();
            ReadBuffer(rawData, ref ptr);
        }

        CheckSize(ptr, rawData.Length);
    }

    public override uint CalculateArraySize()
    {
        uint size = 4 /* tag */ + sizeof(uint) /* numItems */;
        if (numItems > 0)
        {
            size += sizeof(uint);                     // size of bufferLength
            size += (uint)ByteSize<TKey>(numItems);   // size of keyset
            size += (uint)ByteSize<TItem>(numItems);  // size of itemset
            size += bufferLength;                     // bulk size of raw buffer
        }
        return size;
    }

    public override uint DumpToArray(Span<byte> bytes)
    {
        int ptr = 0;
        Tag.CopyTo(bytes);
        ptr += Tag.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(ptr), numItems);
        ptr += sizeof(uint);

        if (numItems > 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(ptr), bufferLength);
            ptr += sizeof(uint);
            ReadOnlySpan<byte> keys = MemoryMarshal.AsBytes(pKeys.AsSpan(0, (int)numItems));
            keys.CopyTo(bytes.Slice(ptr));
            ptr += keys.Length;
            ReadOnlySpan<byte> items = MemoryMarshal.AsBytes(pItems.AsSpan(0, (int)numItems));
            items.CopyTo(bytes.Slice(ptr));
            ptr += items.Length;
            ptr = WriteBuffer(bytes, ptr);
        }

        return (uint)ptr;
    }

    // Keys are kept sorted by memcmp() order of their bytes, so on a little-endian machine the
    // lowest bits of an integer key sort first. Returns false if the key already exists.
    public bool Add(TKey key, TItem item)
    {
        if (numItems == pKeys.Length)
        {
            int newSize = (pKeys.Length * 2) + 4;
            Array.Resize(ref pKeys, newSize);
            Array.Resize(ref pItems, newSize);
        }

        int first = 0;
        int last = (int)numItems - 1;
        while (first <= last)
        {
            int mid = (first + last) / 2;
            int res = Compare(in pKeys[mid], in key);
            if (res < 0)
            {
                first = mid + 1;
            }
            else if (res > 0)
            {
                last = mid - 1;
            }
            else
            {
                if (!AsBytes(in pItems[mid]).SequenceEqual(AsBytes(in item)))
                    Logger.LogDebug("Tried to add a new value for an existing key, the new value was ignored");
                return false;
            }
        }

        int insert = first;
        int countToMove = (int)numItems - insert;
        Array.Copy(pKeys, insert, pKeys, insert + 1, countToMove);
        Array.Copy(pItems, insert, pItems, insert + 1, countToMove);
        pKeys[insert] = key;
        pItems[insert] = item;
        numItems++;
        return true;
    }

    public void Update(int index, TItem item) => pItems[index] = item;

    public int GetIndex(TKey key)
    {
        int first = 0;
        int last = (int)numItems - 1;
        while (first <= last)
        {
            int mid = (first + last) / 2;
            int res = Compare(in pKeys[mid], in key);
            if (res < 0)
                first = mid + 1;
            else if (res > 0)
                last = mid - 1;
            else
                return mid;
        }
        return -1;
    }

    public TItem GetItem(int index)
    {
        ErrorHandling.AssertCodeMsg(index != -1, ExceptionCodes.LWM, "index != -1", "Didn't find Key");
        return pItems[index];
    }

    public TKey GetKey(int index)
    {
        ErrorHandling.AssertCodeMsg(index != -1, ExceptionCodes.LWM, "index != -1", "Didn't find Key (in GetKey)");
        return pKeys[index];
    }

    public TItem Get(TKey key)
    {
        int index = GetIndex(key);
        ErrorHandling.AssertCodeMsg(index != -1, ExceptionCodes.MC, "index != -1", "Didn't find Item (in Get)");
        return GetItem(index);
    }

    public ReadOnlySpan<TKey> GetRawKeys() => pKeys.AsSpan(0, (int)numItems);

    public ReadOnlySpan<TItem> GetRawItems() => pItems.AsSpan(0, (int)numItems);

    public LightWeightMap<TKey, TItem> Clone()
    {
        var copy = (LightWeightMap<TKey, TItem>)MemberwiseClone();
        copy.pKeys = (TKey[])pKeys.Clone();
        copy.pItems = (TItem[])pItems.Clone();
        copy.buffer = (byte[])buffer.Clone();
        copy.ForgetPinnedBuffers();
        copy.Unlock();
        return copy;
    }

    private static int Compare(in TKey a, in TKey b) => AsBytes(in a).SequenceCompareTo(AsBytes(in b));

    private static ReadOnlySpan<byte> AsBytes<T>(in T value) where T : unmanaged
        => MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value));
}

// A map whose keys are the indices [0 .. count - 1]. Keys are not stored.
public sealed class DenseLightWeightMap<TItem> : LightWeightMapBuffer
    where TItem : unmanaged
{
    private static ReadOnlySpan<byte> Tag => "DWM1"u8;

    private uint numItems;
    private TItem[] pItems = [];

    public override uint GetCount() => numItems;

    public override void ReadFromArray(ReadOnlySpan<byte> rawData)
    {
        // If this is a v1 LWM, convert it to a DenseLightWeightMap.
        if (!rawData.StartsWith(Tag))
        {
            ReadFromArrayAndConvertLWM1(rawData);
            return;
        }

        int ptr = Tag.Length;
        numItems = ReadUInt32(rawData, ref ptr);
        if (numItems > 0)
        {
            bufferLength = ReadUInt32(rawData, ref ptr);
            pItems = MemoryMarshal.Cast<byte, TItem>(Take(rawData, ref ptr, ByteSize<TItem>(numItems))).ToArray();
            ReadBuffer(rawData, ref ptr);
        }

        CheckSize(ptr, rawData.Length);
    }

    private void ReadFromArrayAndConvertLWM1(ReadOnlySpan<byte> rawData)
    {
        int ptr = 0;
        numItems = ReadUInt32(rawData, ref ptr);
        if (numItems > 0)
        {
            bufferLength = ReadUInt32(rawData, ref ptr);
            ReadOnlySpan<uint> keys = MemoryMarshal.Cast<byte, uint>(Take(rawData, ref ptr, ByteSize<uint>(numItems)));
            ReadOnlySpan<TItem> items = MemoryMarshal.Cast<byte, TItem>(Take(rawData, ref ptr, ByteSize<TItem>(numItems)));
            ReadBuffer(rawData, ref ptr);

            var seen = new bool[numItems];
            pItems = new TItem[numItems];
            for (int index = 0; index < numItems; index++)
            {
                uint key = keys[index];
                if (key >= numItems)
                    ErrorHandling.AssertCodeMsg(false, ExceptionCodes.LWM, "key < numItems", $"Illegal key {key}, numItems == {numItems}");
                if (seen[key])
                    ErrorHandling.AssertCodeMsg(false, ExceptionCodes.LWM, "!tKeySeen[key]", $"Duplicate key {key}");
                seen[key] = true;
                pItems[key] = items[index];
            }
        }

        CheckSize(ptr, rawData.Length);
    }

    public override uint CalculateArraySize()
    {
        uint size = 4 /* tag */ + sizeof(uint) /* numItems */;
        if (numItems > 0)
        {
            size += sizeof(uint);                     // size of bufferLength
            size += (uint)ByteSize<TItem>(numItems);  // size of itemset
            size += bufferLength;                     // bulk size of raw buffer
        }
        return size;
    }

    public override uint DumpToArray(Span<byte> bytes)
    {
        int ptr = 0;
        Tag.CopyTo(bytes);
        ptr += Tag.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(ptr), numItems);
        ptr += sizeof(uint);

        if (numItems > 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(ptr), bufferLength);
            ptr += sizeof(uint);
            ReadOnlySpan<byte> items = MemoryMarshal.AsBytes(pItems.AsSpan(0, (int)numItems));
            items.CopyTo(bytes.Slice(ptr));
            ptr += items.Length;
            ptr = WriteBuffer(bytes, ptr);
        }

        return (uint)ptr;
    }

    public bool Append(TItem item)
    {
        if (numItems == pItems.Length)
            Array.Resize(ref pItems, (pItems.Length * 2) + 4);

        pItems[numItems++] = item;
        return true;
    }

    public int GetIndex(uint key) => key >= numItems ? -1 : (int)key;

    public TItem GetItem(int index)
    {
        ErrorHandling.AssertCodeMsg(index != -1, ExceptionCodes.LWM, "index != -1", "Didn't find Key");
        return pItems[index];
    }

    public TItem Get(uint key) => GetItem(GetIndex(key));

    public ReadOnlySpan<TItem> GetRawItems() => pItems.AsSpan(0, (int)numItems);
}
