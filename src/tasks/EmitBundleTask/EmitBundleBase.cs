// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public abstract class EmitBundleBase : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private CancellationTokenSource BuildTaskCancelled { get; } = new();

    /// Must have DestinationFile metadata, which is the output filename
    /// Could have RegisteredName, otherwise it would be the filename.
    /// RegisteredName should be prefixed with namespace in form of unix like path. For example: "/usr/share/zoneinfo/"
    [Required]
    public ITaskItem[] FilesToBundle { get; set; } = default!;

    /// <summary>
    /// The function to call before mono runtime initialization
    /// in order to register the bundled resources in FilesToBundle
    /// </summary>
    public string BundleRegistrationFunctionName { get; set; } = "mono_register_resources_bundle";

    /// <summary>
    /// The filename for the generated source file that registers
    /// the bundled resources.
    /// </summary>
    public string BundleFile { get; set; } = "mono-bundled-source.c";

    /// <summary>
    /// The filename for the generated header file that declares
    /// MonoBundled*Resource struct types.
    /// </summary>
    public string BundleHeader { get; set; } = "mono-bundled-source.h";

    /// <summary>
    /// Filename for the unified source containing all byte data and len
    /// of the FilesToBundle resources.
    /// Leave empty to keep sources separate.
    /// </summary>
    public string? CombinedResourceSource { get; set; }

    /// <summary>
    /// Path to store build artifacts
    /// <summary>
    public string OutputDirectory {get; set; } = default!;

    public override bool Execute()
    {
        // The DestinationFile (output filename) already includes a content hash. Grouping by this filename therefore
        // produces one group per file-content. We only want to emit one copy of each file-content, and one symbol for it.
        var filesToBundleByDestinationFileName = FilesToBundle.GroupBy(f => f.GetMetadata("DestinationFile")).ToList();

        // We're handling the incrementalism within this task, because it needs to be based on file content hashes
        // and not on timetamps. The output filenames contain a content hash, so if any such file already exists on
        // disk with that name, we know it must be up-to-date.
        var remainingDestinationFilesToBundle = filesToBundleByDestinationFileName.Where(g => !File.Exists(g.Key)).ToArray();

        // If you're only touching the leaf project, we don't really need to tell you that.
        // But if there's more work to do it's valuable to show progress.
        var verbose = remainingDestinationFilesToBundle.Length > 1;
        var verboseCount = 0;

        var filesToBundleByRegisteredName = FilesToBundle.GroupBy(file => {
            var registeredName = file.GetMetadata("RegisteredName");
            if(string.IsNullOrEmpty(registeredName))
            {
                registeredName = Path.GetFileName(file.ItemSpec);
            }
            return registeredName;
        }).ToList();

        var files = filesToBundleByRegisteredName.Select(group => {
            var registeredFile = group.First();
            var outputFile = registeredFile.GetMetadata("DestinationFile");
            var registeredFilename = group.Key;
            var resourceName = ToSafeSymbolName(outputFile);
            string? resourceSymbolName = null;
<<<<<<< HEAD
            if (File.Exists(registeredFile.GetMetadata("SymbolFile")))
                resourceSymbolName = ToSafeSymbolName(registeredFile.GetMetadata("SymbolFile"));
=======
            if (File.Exists(registeredFile.GetMetadata("Symfile")))
                resourceSymbolName = ToSafeSymbolName(registeredFile.GetMetadata("Symfile"));
>>>>>>> 5a56c0e2b21 (Differentiate resource name and symbol)
            return (registeredFilename, resourceName, resourceSymbolName);
        }).ToList();

        Log.LogMessage(MessageImportance.Low, $"Bundling {files.Count} files for {BundleRegistrationFunctionName}");

        // Generate source file(s) containing each resource's byte data and size
        if (remainingDestinationFilesToBundle.Length > 0)
        {
            int allowedParallelism = Math.Max(Math.Min(remainingDestinationFilesToBundle.Length, Environment.ProcessorCount), 1);
            if (BuildEngine is IBuildEngine9 be9)
                allowedParallelism = be9.RequestCores(allowedParallelism);
            if (!string.IsNullOrEmpty(CombinedResourceSource))
                allowedParallelism = 1;

            Parallel.For(0, remainingDestinationFilesToBundle.Length, new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism, CancellationToken = BuildTaskCancelled.Token }, (i, state) =>
            {
                var group = remainingDestinationFilesToBundle[i];

                // Since the object filenames include a content hash, we can pick an arbitrary ITaskItem from each group,
                // since we know each group's ITaskItems all contain the same binary data
                var contentSourceFile = group.First();

                var outputFile = group.Key;
                if (!string.IsNullOrEmpty(CombinedResourceSource))
                    outputFile = Path.Combine(OutputDirectory, CombinedResourceSource);
                var inputFile = contentSourceFile.ItemSpec;
                if (verbose)
                {
                    var registeredName = contentSourceFile.GetMetadata("RegisteredName");
                    if(string.IsNullOrEmpty(registeredName))
                    {
                        registeredName = Path.GetFileName(inputFile);
                    }
                    var count = Interlocked.Increment(ref verboseCount);
                    Log.LogMessage(MessageImportance.Low, "{0}/{1} Bundling {2} ...", count, remainingDestinationFilesToBundle.Length, registeredName);
                }

                Log.LogMessage(MessageImportance.Low, "Bundling {0} into {1}", inputFile, outputFile);
                var symbolName = ToSafeSymbolName(group.Key);
                if (!Emit(outputFile, (codeStream) => {
                    using var inputStream = File.OpenRead(inputFile);
                    BundleFileToCSource(symbolName, inputStream, codeStream);
                }))
                {
                    state.Stop();
                }
            });
        }

        // Generate header containing MonoBundled*Resource typedefs
        File.WriteAllText(Path.Combine(OutputDirectory, BundleHeader), Utils.GetEmbeddedResource("mono-bundled-source.h"));

        // Generate source file to preallocate resources and register bundled resources
        Emit(Path.Combine(OutputDirectory, BundleFile), (inputStream) =>
        {
            using var outputUtf8Writer = new StreamWriter(inputStream, Utf8NoBom);
            GenerateBundledResourcePreallocationAndRegistration(BundleRegistrationFunctionName, files, outputUtf8Writer);
        });

        return true;
    }

    public void Cancel()
    {
        BuildTaskCancelled.Cancel();
    }

    #region Helpers

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly byte[] HexToUtf8Lookup = InitLookupTable();
    private static readonly byte[] NewLineAndIndentation = new[] { (byte)0x0a, (byte)0x20, (byte)0x20 };

    private static byte[] InitLookupTable()
    {
        // Every 6 bytes in this array represents the output for a different input byte value.
        // For example, the input byte 0x1a (26 decimal) corresponds to bytes 156-161 (26*6=156),
        // whose values will be ['0', 'x', '1', 'a', ',', ' '], which is the UTF-8 representation
        // for "0x1a, ". This is just a faster alternative to calling .ToString("x2") on every
        // byte of the input file and then pushing that string through UTF8Encoding.
        var lookup = new byte[256 * 6];
        for (int i = 0; i < 256; i++)
        {
            string byteAsHex = i.ToString("x2");
            char highOrderChar = BitConverter.IsLittleEndian ? byteAsHex[0] : byteAsHex[1];
            char lowOrderChar = BitConverter.IsLittleEndian ? byteAsHex[1] : byteAsHex[0];
            lookup[i * 6 + 0] = (byte)'0';
            lookup[i * 6 + 1] = (byte)'x';
            lookup[i * 6 + 2] = (byte)highOrderChar;
            lookup[i * 6 + 3] = (byte)lowOrderChar;
            lookup[i * 6 + 4] = (byte)',';
            lookup[i * 6 + 5] = (byte)' ';
        }

        return lookup;
    }

    public abstract bool Emit(string destinationFile, Action<Stream> inputProvider);

    private static Dictionary<string, int> symbolDataLen = new();

    private static void GenerateBundledResourcePreallocationAndRegistration(string bundleRegistrationFunctionName, ICollection<(string registeredFilename, string resourceName, string? resourceSymbolName)> files, StreamWriter outputUtf8Writer)
    {
        StringBuilder preallocatedSource = new();

        string assemblyTemplate = Utils.GetEmbeddedResource("mono-bundled-assembly.template");
        string satelliteAssemblyTemplate = Utils.GetEmbeddedResource("mono-bundled-satellite-assembly.template");
        string symbolDataTemplate = Utils.GetEmbeddedResource("mono-bundled-data.template");

        var preallocatedResources = new StringBuilder();
        var preallocatedAssemblies = new StringBuilder("MonoBundledResource *bundledAssemblyResources[] = { ");
        var preallocatedSatelliteAssemblies = new StringBuilder("MonoBundledResource *bundledSatelliteAssemblyResources[] = { ");
        var preallocatedData = new StringBuilder("MonoBundledResource *bundledDataResources[] = { ");
        int assembliesCount = 0;
        int satelliteAssembliesCount = 0;
        int dataCount = 0;
        foreach (var tuple in files)
        {
            string resourceId = tuple.registeredFilename;

            // extern symbols
            StringBuilder preallocatedResourceData = new();
            preallocatedResourceData.AppendLine($"extern const unsigned char {tuple.resourceName}_data[];");
            if (!string.IsNullOrEmpty(tuple.resourceSymbolName))
            {
                preallocatedResourceData.AppendLine($"extern const unsigned char {tuple.resourceSymbolName}_data[];");
            }
            preallocatedSource.AppendLine(preallocatedResourceData.ToString());

            // Generate Preloaded MonoBundled*Resource structs
            string preloadedStruct;
            switch (GetFileType(tuple.registeredFilename)) {
            case "MONO_BUNDLED_ASSEMBLY": {
                preloadedStruct = assemblyTemplate;
                preallocatedAssemblies.Append($"(MonoBundledResource *)&{tuple.resourceName}, ");
                assembliesCount += 1;
                break;
            }
            case "MONO_BUNDLED_SATELLITE_ASSEMBLY": {
                preloadedStruct = satelliteAssemblyTemplate;
                resourceId = $"{tuple.culture}/{tuple.registeredFilename}";
                preallocatedSatelliteAssemblies.Append($"(MonoBundledResource *)&{tuple.resourceName}, ");
                satelliteAssembliesCount += 1;
                break;
            }
            case "MONO_BUNDLED_DATA":
            default: {
                preloadedStruct = symbolDataTemplate;
                preallocatedData.Append($"(MonoBundledResource *)&{tuple.resourceName}, ");
                dataCount += 1;
                break;
            }
            }

            // Add associated symfile information to MonoBundledAssemblyResource/MonoBundleSatelliteAssemblyResource structs
            string preloadedSymfile = "";
            if (!string.IsNullOrEmpty(tuple.resourceSymbolName))
            {
                preloadedSymfile = Utils.GetEmbeddedResource("mono-bundled-symbol.template")
                                            .Replace("%ResourceSymbolName%", tuple.resourceSymbolName)
                                            .Replace("%SymbolLen%", symbolDataLen[tuple.resourceSymbolName].ToString());
            }
            preallocatedSource.AppendLine(preloadedStruct.Replace("%ResourceName%", tuple.resourceName)
                                         .Replace("%ResourceID%", resourceId)
                                         .Replace("%RegisteredFilename%", tuple.registeredFilename)
                                         .Replace("%Len%", symbolDataLen[tuple.resourceName].ToString())
                                         .Replace("%MonoBundledSymbolData%", preloadedSymfile));
        }

        var addPreallocatedResources = new StringBuilder();
        if (assembliesCount != 0) {
            preallocatedAssemblies.AppendLine("};");
            preallocatedResources.AppendLine(preallocatedAssemblies.ToString());
            addPreallocatedResources.AppendLine($"    mono_bundled_resources_add (bundledAssemblyResources, {assembliesCount});");
        }
        if (satelliteAssembliesCount != 0) {
            preallocatedSatelliteAssemblies.AppendLine("};");
            preallocatedResources.AppendLine(preallocatedSatelliteAssemblies.ToString());
            addPreallocatedResources.AppendLine($"    mono_bundled_resources_add (bundledSatelliteAssemblyResources, {satelliteAssembliesCount});");
        }
        if (dataCount != 0) {
            preallocatedData.AppendLine("};");
            preallocatedResources.AppendLine(preallocatedData.ToString());
            addPreallocatedResources.AppendLine($"    mono_bundled_resources_add (bundledDataResources, {dataCount});");
        }

        outputUtf8Writer.Write(Utils.GetEmbeddedResource("mono-bundled-resource-preallocation-and-registration.template")
                                .Replace("%PreallocatedStructs%", preallocatedSource.ToString())
                                .Replace("%PreallocatedResources%", preallocatedResources.ToString())
                                .Replace("%BundleRegistrationFunctionName%", bundleRegistrationFunctionName)
                                .Replace("%AddPreallocatedResources%", addPreallocatedResources.ToString()));
    }

    private static void BundleFileToCSource(string symbolName, FileStream inputStream, Stream outputStream)
    {
        // Emits a C source file in the same format as "xxd --include". Example:
        //
        // unsigned char Some_File_dll[] = {
        //   0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x0a
        // };
        // unsigned int Some_File_dll_len = 6;

        var buf = new byte[4096];
        int bytesRead;
        var generatedArrayLength = 0;
        var bytesEmitted = 0;

        using var outputUtf8Writer = new StreamWriter(outputStream, Utf8NoBom);

        outputUtf8Writer.Write($"unsigned char {symbolName}_data[] = {{");
        outputUtf8Writer.Flush();
        while ((bytesRead = inputStream.Read(buf, 0, buf.Length)) > 0)
        {
            for (var i = 0; i < bytesRead; i++)
            {
                if (bytesEmitted++ % 12 == 0)
                {
                    outputStream.Write(NewLineAndIndentation, 0, NewLineAndIndentation.Length);
                }

                var byteValue = buf[i];
                outputStream.Write(HexToUtf8Lookup, byteValue * 6, 6);
            }

            generatedArrayLength += bytesRead;
        }

        outputUtf8Writer.WriteLine("0\n};");
        outputUtf8Writer.WriteLine($"unsigned int {symbolName}_len = {generatedArrayLength};");
        outputUtf8Writer.Flush();
        outputStream.Flush();

        symbolDataLen.Add(symbolName, generatedArrayLength);
    }

    private static string ToSafeSymbolName(string destinationFileName)
    {
        // Since destinationFileName includes a content hash, we can safely strip off the directory name
        // as the filename is always unique enough. This avoid disclosing information about the build
        // file structure in the resulting symbols.
        var filename = Path.GetFileName(destinationFileName);

        // Equivalent to the logic from "xxd --include"
        var sb = new StringBuilder();
        foreach (var c in filename)
        {
            sb.Append(IsAlphanumeric(c) ? c : '_');
        }

        return sb.ToString();
    }

    private static string GetFileType(string destinationFileName)
    {
        if (destinationFileName.EndsWith(".resources.dll"))
        {
            return "MONO_BUNDLED_SATELLITE_ASSEMBLY";
        }
        if (destinationFileName.EndsWith(".dll"))
        {
            return "MONO_BUNDLED_ASSEMBLY";
        }
        return "MONO_BUNDLED_DATA";
    }

    // Equivalent to "isalnum"
    private static bool IsAlphanumeric(char c) => c
        is (>= 'a' and <= 'z')
        or (>= 'A' and <= 'Z')
        or (>= '0' and <= '9');

    #endregion

}
