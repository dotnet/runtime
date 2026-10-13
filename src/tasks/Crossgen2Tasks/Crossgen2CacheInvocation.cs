// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;

namespace Microsoft.NET.Build.Tasks;

// Deployment and symbol conventions belong here, not in the cache storage or hashing algorithm.
internal sealed class Crossgen2CacheInvocation
{
    internal string[] CompilerDependencies { get; }
    internal string[] Outputs { get; }

    private Crossgen2CacheInvocation(string[] compilerDependencies, string[] outputs)
    {
        CompilerDependencies = compilerDependencies;
        Outputs = outputs;
    }

    internal static bool SupportsTarget(ITaskItem tool)
        => RuntimeInformation.ProcessArchitecture == Architecture.X64 &&
            tool.GetMetadata(MetadataKeys.TargetArch) == "x64" &&
            ((OperatingSystem.IsLinux() && tool.GetMetadata(MetadataKeys.TargetOS) == "linux") ||
             (OperatingSystem.IsWindows() && tool.GetMetadata(MetadataKeys.TargetOS) == "windows"));

    internal static void ValidateInputSymbols(Stream stream, BlobContentId expected)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        try
        {
            using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(stream, MetadataStreamOptions.LeaveOpen);
            MetadataReader reader = provider.GetMetadataReader();
            if (reader.DebugMetadataHeader is null || new BlobContentId(reader.DebugMetadataHeader.Id) != expected)
            {
                throw new InvalidDataException("Input PDB identity does not match its assembly; external Windows symbol readers are not supported.");
            }
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidDataException("Only matching portable input PDBs are supported; external Windows symbol readers are not supported.", ex);
        }
        stream.Position = 0;
    }

    internal static Crossgen2CacheInvocation Create(string tool, string jit, string image, string symbols,
        bool emitSymbols, string perfmapFormatVersion)
    {
        tool = Crossgen2Cache.InputPath(tool);
        image = Crossgen2Cache.InputPath(image);
        bool windows = OperatingSystem.IsWindows();
        if (!Crossgen2Cache.PathComparer.Equals(Path.GetFileName(tool), windows ? "crossgen2.exe" : "crossgen2") ||
            (File.GetAttributes(tool) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("The compiler must be a directly deployed crossgen2 executable, not a launcher or symbolic link.");
        }
        using (FileStream stream = File.OpenRead(tool))
        {
            if (windows)
            {
                using var pe = new PEReader(stream);
                if (pe.PEHeaders.PEHeader is null || pe.PEHeaders.CorHeader is not null ||
                    pe.PEHeaders.CoffHeader.Machine != Machine.Amd64 ||
                    (pe.PEHeaders.CoffHeader.Characteristics & Characteristics.ExecutableImage) == 0 ||
                    (pe.PEHeaders.CoffHeader.Characteristics & Characteristics.Dll) != 0)
                {
                    throw new InvalidDataException("Only a directly executed native Windows-x64 crossgen2 deployment is supported.");
                }
            }
            else if (stream.ReadByte() != 0x7f || stream.ReadByte() != 'E' || stream.ReadByte() != 'L' || stream.ReadByte() != 'F')
            {
                throw new InvalidDataException("Only a directly executed native ELF crossgen2 deployment is supported.");
            }
        }
        foreach (string extension in new[] { ".dll", ".deps.json", ".runtimeconfig.json", ".runtimeconfig.dev.json" })
        {
            string candidate = Path.ChangeExtension(tool, extension);
            try
            {
                File.GetAttributes(candidate);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            throw new InvalidDataException("Managed/apphost crossgen2 deployments are not supported.");
        }

        string directory = Path.GetDirectoryName(tool)!;
        var dependencies = new List<string>
        {
            tool,
            string.IsNullOrEmpty(jit) ? Path.Combine(directory, windows ? "clrjit_win_x64_x64.dll" : "libclrjit_unix_x64_x64.so") : jit,
            Path.Combine(directory, windows ? "jitinterface_x64.dll" : "libjitinterface_x64.so")
        };
        string symbolOutput = Path.ChangeExtension(image, ".ni.r2rmap");
        if (windows)
        {
            bool nativeImageName = image.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase) ||
                image.EndsWith(".ni.exe", StringComparison.OrdinalIgnoreCase);
            symbolOutput = Path.ChangeExtension(image, nativeImageName ? ".pdb" : ".ni.pdb");
            if (emitSymbols)
            {
                dependencies.Add(Path.Combine(directory, "Microsoft.DiaSymReader.Native.dll"));
            }
        }
        if (!string.IsNullOrEmpty(symbols))
        {
            symbols = Crossgen2Cache.InputPath(symbols);
            symbolOutput = Path.Combine(Path.GetDirectoryName(symbols)!, Path.GetFileName(symbolOutput));
        }
        if (emitSymbols && ((!windows && perfmapFormatVersion != "1") ||
            !Crossgen2Cache.PathComparer.Equals(symbols, symbolOutput)))
        {
            throw new InvalidDataException("Symbols require the compiler-generated filename in the declared output directory and, for perf maps, format version 1.");
        }

        return new Crossgen2CacheInvocation(dependencies.ToArray(), new[] { image, symbolOutput });
    }
}
