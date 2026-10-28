// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.CommandLine;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Xunit;

namespace ILAssembler.Tests;

public sealed class Win32ResourceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("ilasm-win32-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Theory]
    [InlineData("--resource", "")]
    [InlineData("-RESOURCE=", "-x64")]
    [InlineData("-res:", "-arm64")]
    public void ResFile_EmitsSortedDirectoriesAndPreservesManagedResources(string resourceOption, string machine)
    {
        byte[] resources = CreateResFile();
        using PEReader pe = Assemble(resources, resourceOption, machine);
        Assert.Equal(machine switch { "-x64" => Machine.Amd64, "-arm64" => Machine.Arm64, _ => Machine.I386 }, pe.PEHeaders.CoffHeader.Machine);
        Dictionary<string, (uint CodePage, byte[] Data)> actual = ReadResources(pe);
        Assert.Equal(4, actual.Count);
        Assert.Equal(new byte[] { 1, 2, 3 }, actual["#10/#2/1033"].Data);
        Assert.Equal(new byte[] { 4 }, actual["#10/#2/1041"].Data);
        Assert.Empty(actual["TYPE/na\u00efve/0"].Data);
        Assert.Equal(new byte[] { 5, 6 }, actual["//0"].Data);
        Assert.All(actual.Values, value => Assert.Equal(0u, value.CodePage));
        MetadataReader metadata = pe.GetMetadataReader();
        ManifestResource managedResource = metadata.GetManifestResource(Assert.Single(metadata.ManifestResources));
        Assert.Equal("Managed", metadata.GetString(managedResource.Name));
        Assert.Equal(new byte[] { 3, 0, 0, 0, 7, 8, 9 },
            pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress).GetContent(0, 7));
    }

    [Theory]
    [InlineData(Machine.I386, "")]
    [InlineData(Machine.Amd64, "-x64")]
    [InlineData(Machine.Arm64, "-arm64")]
    public void CoffFile_RelocatesResourceDataUsingSymbolSectionAndAddend(Machine machine, string target)
    {
        using PEReader pe = Assemble(CreateCoffFile(machine), "--resource", target);
        var resource = Assert.Single(ReadResources(pe));
        Assert.Equal("#10/#2/1033", resource.Key);
        Assert.Equal(1252u, resource.Value.CodePage);
        Assert.Equal(new byte[] { 1, 2, 3 }, resource.Value.Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void CoffFile_PayloadCanEndAtSectionBoundary(int size)
    {
        byte[] bytes = CreateCoffFile(Machine.I386);
        WriteUInt32(bytes, 172, size == 0 ? 8u : 4u);
        WriteUInt32(bytes, 176, (uint)size);

        using PEReader pe = Assemble(bytes);
        var resource = Assert.Single(ReadResources(pe));
        Assert.Equal(size == 0 ? Array.Empty<byte>() : new byte[] { 1, 2, 3, 0 }, resource.Value.Data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResourceFile_DeterministicAcrossCompilations(bool coff)
    {
        byte[] input = coff ? CreateCoffFile(Machine.I386) : CreateResFile();
        using (PEReader first = Assemble(input))
        {
            Assert.NotEmpty(ReadResources(first));
        }
        byte[] expected = File.ReadAllBytes(Path.Combine(_directory, "output.dll"));
        using PEReader second = Assemble(input);
        Assert.Equal(expected, second.GetEntireImage().GetContent());
    }

    [Fact]
    public void NullResFile_EmitsAnEmptyDirectory()
    {
        using PEReader pe = Assemble(CreateResFile(nullResourceOnly: true));
        Assert.Empty(ReadResources(pe));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("truncated-res")]
    [InlineData("res-header-size")]
    [InlineData("res-data-size")]
    [InlineData("res-duplicate")]
    [InlineData("coff-sections")]
    [InlineData("coff-section-size")]
    [InlineData("coff-relocation-table")]
    [InlineData("coff-relocation-offset")]
    [InlineData("coff-relocation-type")]
    [InlineData("coff-symbol-index")]
    [InlineData("coff-symbol-section")]
    [InlineData("coff-symbol-value")]
    [InlineData("coff-directory-offset")]
    [InlineData("coff-directory-header")]
    [InlineData("coff-directory-entries")]
    [InlineData("coff-directory-cycle")]
    [InlineData("coff-directory-depth")]
    [InlineData("coff-name-offset")]
    [InlineData("coff-name-length")]
    [InlineData("coff-data-entry")]
    [InlineData("coff-payload-size")]
    [InlineData("coff-payload-end")]
    [InlineData("coff-missing-relocation")]
    public void InvalidResource_DoesNotReplaceOutputOrPdb(string failure)
    {
        byte[] bytes = failure.StartsWith("coff-", StringComparison.Ordinal) ? CreateCoffFile(Machine.I386) : CreateResFile();
        switch (failure)
        {
            case "empty": bytes = []; break;
            case "truncated-res": bytes = bytes[..^4]; break;
            case "res-header-size": WriteUInt32(bytes, 36, uint.MaxValue); break;
            case "res-data-size": WriteUInt32(bytes, 32, uint.MaxValue); break;
            case "res-duplicate": bytes = [.. bytes, .. bytes.AsSpan(32).ToArray()]; break;
            case "coff-sections": bytes[20 + 40 + 6] = (byte)'3'; break;
            case "coff-section-size": WriteUInt32(bytes, 20 + 40 + 16, uint.MaxValue); break;
            case "coff-relocation-table": WriteUInt32(bytes, 20 + 24, uint.MaxValue); break;
            case "coff-relocation-offset": WriteUInt32(bytes, 200, 87); break;
            case "coff-relocation-type": bytes[208] = 0xff; break;
            case "coff-symbol-index": WriteUInt32(bytes, 204, 1); break;
            case "coff-symbol-section": bytes[210 + 12] = 1; break;
            case "coff-symbol-value": WriteUInt32(bytes, 210 + 8, uint.MaxValue); break;
            case "coff-directory-offset": WriteUInt32(bytes, 120, 0xfffffff0); break;
            case "coff-directory-header": WriteUInt32(bytes, 120, 0x80000050); break;
            case "coff-directory-entries": bytes[114] = 10; break;
            case "coff-directory-cycle": WriteUInt32(bytes, 120, 0x80000000); break;
            case "coff-directory-depth": WriteUInt32(bytes, 168, 0x80000048); break;
            case "coff-name-offset":
                bytes[112] = 1;
                bytes[114] = 0;
                WriteUInt32(bytes, 116, 0xfffffff0);
                break;
            case "coff-name-length":
                bytes[112] = 1;
                bytes[114] = 0;
                WriteUInt32(bytes, 116, 0x80000054);
                bytes[184] = 4;
                break;
            case "coff-data-entry": WriteUInt32(bytes, 168, 80); break;
            case "coff-payload-size": WriteUInt32(bytes, 176, uint.MaxValue); break;
            case "coff-payload-end": WriteUInt32(bytes, 176, 5); break;
            case "coff-missing-relocation": bytes[52] = 0; break;
        }

        string resourcePath = Path.Combine(_directory, "native.res");
        if (failure != "missing")
        {
            File.WriteAllBytes(resourcePath, bytes);
        }
        string output = Path.Combine(_directory, "output.dll");
        string pdb = Path.ChangeExtension(output, ".pdb");
        File.WriteAllBytes(output, [1, 2, 3]);
        File.WriteAllBytes(pdb, [4, 5, 6]);
        Assert.Equal(1, Run(resourcePath, "--resource", "-err"));
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(output));
        Assert.Equal(new byte[] { 4, 5, 6 }, File.ReadAllBytes(pdb));
    }

    private PEReader Assemble(byte[] resources, string resourceOption = "--resource", string target = "")
    {
        // The extension is deliberately unrelated to the format.
        string resourcePath = Path.Combine(_directory, "native.data");
        File.WriteAllBytes(resourcePath, resources);
        Assert.Equal(0, Run(resourcePath, resourceOption, target));
        return new PEReader(File.OpenRead(Path.Combine(_directory, "output.dll")));
    }

    private int Run(string resourcePath, string resourceOption, string target)
    {
        string source = Path.Combine(_directory, "input.il");
        File.WriteAllText(source, ".assembly Test {} .mresource public Managed {}");
        File.WriteAllBytes(Path.Combine(_directory, "Managed"), [7, 8, 9]);
        string output = Path.Combine(_directory, "output.dll");
        string[] resourceArguments = resourceOption == "--resource" ? [resourceOption, resourcePath] : [resourceOption + resourcePath];
        string[] args = ["-nologo", "-quiet", "-dll", "-det", $"-output={output}", .. resourceArguments,
            .. (target.Length == 0 ? Array.Empty<string>() : [target]), source];
        var command = new IlasmRootCommand();
        ParseResult parsed = command.Parse(NativeCommandLine.Normalize(args, command));
        Assert.Empty(parsed.Errors);
        return new Program(command, parsed).Run();
    }

    private static byte[] CreateResFile(bool nullResourceOnly = false)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode, leaveOpen: true);
        WriteResource(0, 0, 0, []);
        if (!nullResourceOnly)
        {
            WriteResource(10, 2, 1041, [4]);
            WriteResource("TYPE", "na\u00efve", 0, []);
            WriteResource(17, 1, 0, [0xff]); // RT_DLGINCLUDE is not embedded.
            WriteResource(10, 2, 1033, [1, 2, 3]);
            WriteResource("", "", 0, [5, 6]);
        }
        return stream.ToArray();

        void WriteResource(object type, object name, ushort language, byte[] data)
        {
            long start = stream.Position;
            writer.Write(data.Length);
            writer.Write(0);
            WriteIdentifier(type);
            WriteIdentifier(name);
            while (stream.Position % 4 != 0) { writer.Write((byte)0); }
            writer.Write(0u);
            writer.Write((ushort)0x1030);
            writer.Write(language);
            writer.Write(0u);
            writer.Write(0u);
            long headerEnd = stream.Position;
            stream.Position = start + 4;
            writer.Write((uint)(headerEnd - start));
            stream.Position = headerEnd;
            writer.Write(data);
            while (stream.Position % 4 != 0) { writer.Write((byte)0); }
        }

        void WriteIdentifier(object value)
        {
            if (value is int id)
            {
                writer.Write(ushort.MaxValue);
                writer.Write((ushort)id);
            }
            else
            {
                foreach (char c in (string)value) { writer.Write((ushort)c); }
                writer.Write((ushort)0);
            }
        }
    }

    private static byte[] CreateCoffFile(Machine machine)
    {
        // Two sections, rather than cvtres's optional .debug$S followed by the two resource sections.
        byte[] bytes = new byte[232];
        using var writer = new BinaryWriter(new MemoryStream(bytes));
        writer.Write((ushort)machine);
        writer.Write((ushort)2);
        writer.Write(0u);
        writer.Write(210u);
        writer.Write(1u);
        writer.Write(0u);
        WriteSection(".rsrc$01", 88, 100, 200, 1);
        WriteSection(".rsrc$02", 12, 188, 0, 0);
        writer.BaseStream.Position = 100;
        WriteDirectory(10, 24, true);
        WriteDirectory(2, 48, true);
        WriteDirectory(1033, 72, false);
        writer.Write(4u); // Addend, added to the symbol's section-relative value.
        writer.Write(3u);
        writer.Write(1252u);
        writer.Write(0u);
        writer.BaseStream.Position = 196;
        writer.Write(new byte[] { 1, 2, 3 });
        writer.BaseStream.Position = 200;
        writer.Write(72u);
        writer.Write(0u);
        writer.Write((ushort)(machine switch { Machine.I386 => 7, Machine.Amd64 => 3, _ => 2 }));
        writer.Write(new byte[8]);
        writer.Write(4u);
        writer.Write((short)2);
        writer.Write((ushort)0);
        writer.Write((byte)3);
        writer.Write((byte)0);
        writer.Write(4u);
        return bytes;

        void WriteSection(string name, uint size, uint data, uint relocations, ushort relocationCount)
        {
            writer.Write(Encoding.ASCII.GetBytes(name));
            writer.Write(new byte[8 - name.Length]);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(size);
            writer.Write(data);
            writer.Write(relocations);
            writer.Write(0u);
            writer.Write(relocationCount);
            writer.Write((ushort)0);
            writer.Write(0x40000040u);
        }

        void WriteDirectory(uint id, uint offset, bool subdirectory)
        {
            writer.Write(new byte[12]);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write(id);
            writer.Write(offset | (subdirectory ? 0x80000000u : 0));
        }
    }

    private static Dictionary<string, (uint CodePage, byte[] Data)> ReadResources(PEReader pe)
    {
        DirectoryEntry directory = pe.PEHeaders.PEHeader!.ResourceTableDirectory;
        Assert.NotEqual(0, directory.RelativeVirtualAddress);
        byte[] section = pe.GetSectionData(directory.RelativeVirtualAddress).GetContent(0, directory.Size).ToArray();
        var result = new Dictionary<string, (uint, byte[])>();
        ReadDirectory(0, "", 0);
        return result;

        void ReadDirectory(int offset, string prefix, int level)
        {
            int named = BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(offset + 12));
            int ids = BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(offset + 14));
            string? lastName = null;
            uint lastId = 0;
            for (int i = 0; i < named + ids; i++)
            {
                uint id = ReadUInt32(section, offset + 16 + i * 8);
                string key;
                if (i < named)
                {
                    Assert.NotEqual(0u, id & 0x80000000);
                    int nameOffset = (int)(id & 0x7fffffff);
                    int length = BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(nameOffset));
                    key = Encoding.Unicode.GetString(section, nameOffset + 2, length * 2);
                    Assert.True(lastName is null || StringComparer.OrdinalIgnoreCase.Compare(lastName, key) < 0);
                    lastName = key;
                }
                else
                {
                    Assert.Equal(0u, id & 0x80000000);
                    Assert.True(i == named || id > lastId);
                    lastId = id;
                    key = level == 2 ? id.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"#{id}";
                }
                uint child = ReadUInt32(section, offset + 20 + i * 8);
                if (level < 2)
                {
                    Assert.NotEqual(0u, child & 0x80000000);
                    ReadDirectory((int)(child & 0x7fffffff), prefix + key + "/", level + 1);
                }
                else
                {
                    Assert.Equal(0u, child & 0x80000000);
                    int dataEntry = (int)child;
                    int rva = checked((int)ReadUInt32(section, dataEntry));
                    int length = checked((int)ReadUInt32(section, dataEntry + 4));
                    result.Add(prefix + key, (ReadUInt32(section, dataEntry + 8),
                        pe.GetSectionData(rva).GetContent(0, length).ToArray()));
                }
            }
        }
    }

    private static uint ReadUInt32(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));

    private static void WriteUInt32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
}
