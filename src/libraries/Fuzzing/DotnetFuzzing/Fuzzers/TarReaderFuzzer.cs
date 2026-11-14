// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Formats.Tar;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

internal sealed class TarReaderFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.Formats.Tar"];
    public string[] TargetCoreLibPrefixes => [];
    public string Dictionary => "tar.dict";

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return;
        }

        TestArchive(CopyToRentedArray(bytes), bytes.Length, async: false).GetAwaiter().GetResult();
        TestArchive(CopyToRentedArray(bytes), bytes.Length, async: true).GetAwaiter().GetResult();
    }

    private static byte[] CopyToRentedArray(ReadOnlySpan<byte> bytes)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
        try
        {
            bytes.CopyTo(buffer);
            return buffer;
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    private static async Task TestArchive(byte[] buffer, int length, bool async)
    {
        try
        {
            using var stream = new MemoryStream(buffer, 0, length);
            await using var reader = new TarReader(stream, leaveOpen: true);

            while (true)
            {
                TarEntry? entry = async
                    ? await reader.GetNextEntryAsync()
                    : reader.GetNextEntry();

                if (entry is null)
                {
                    break;
                }

                // Access the parsed header fields to exercise the numeric and string parsers.
                _ = entry.Name;
                _ = entry.EntryType;
                _ = entry.Format;
                _ = entry.Length;
                _ = entry.Mode;
                _ = entry.Uid;
                _ = entry.Gid;
                _ = entry.ModificationTime;
                _ = entry.LinkName;

                if (entry.DataStream is Stream data)
                {
                    if (async)
                    {
                        await data.CopyToAsync(Stream.Null);
                    }
                    else
                    {
                        data.CopyTo(Stream.Null);
                    }
                }
            }
        }
        catch (InvalidDataException)
        {
            // Expected for malformed archives.
        }
        catch (EndOfStreamException)
        {
            // Expected for truncated archives.
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
