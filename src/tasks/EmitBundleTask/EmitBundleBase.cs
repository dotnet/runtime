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
    public string? BundleFile { get; set; }

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

    /// <summary>
    /// Resources that were bundled
    ///
    /// Successful bundling will set the following metadata on the items:
    ///   - DataSymbol
    ///   - LenSymbol
    /// <summary>
    [Output]
    public ITaskItem[] BundledResources { get; set; } = default!;

    public override bool Execute()
    {
        // The DestinationFile (output filename) already includes a content hash. Grouping by this filename therefore
        // produces one group per file-content. We only want to emit one copy of each file-content, and one symbol for it.
        var remainingDestinationFilesToBundle = FilesToBundle.GroupBy(f => f.GetMetadata("DestinationFile")).ToArray();

        var verboseCount = 0;

        // Generate source file(s) containing each resource's byte data and size
        if (string.IsNullOrEmpty(CombinedResourceSource))
        {
            int allowedParallelism = Math.Max(Math.Min(remainingDestinationFilesToBundle.Length, Environment.ProcessorCount), 1);
            if (BuildEngine is IBuildEngine9 be9)
                allowedParallelism = be9.RequestCores(allowedParallelism);

            Parallel.For(0, remainingDestinationFilesToBundle.Length, new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism, CancellationToken = BuildTaskCancelled.Token }, (i, state) =>
            {
                var group = remainingDestinationFilesToBundle[i];

                // Since the object filenames include a content hash, we can pick an arbitrary ITaskItem from each group,
                // since we know each group's ITaskItems all contain the same binary data
                var contentSourceFile = group.First();

                var outputFile = group.Key;
                var inputFile = contentSourceFile.ItemSpec;

                var registeredName = contentSourceFile.GetMetadata("RegisteredName");
                if(string.IsNullOrEmpty(registeredName))
                {
                    registeredName = Path.GetFileName(inputFile);
                }
                var count = Interlocked.Increment(ref verboseCount);
                Log.LogMessage(MessageImportance.Low, "{0}/{1} Bundling {2} ...", count, remainingDestinationFilesToBundle.Length, registeredName);

                Log.LogMessage(MessageImportance.Low, "Bundling {0} into {1}", inputFile, outputFile);
                var symbolName = ToSafeSymbolName(outputFile);
                if (!Emit(outputFile, (codeStream) => {
                    using var inputStream = File.OpenRead(inputFile);
                    using var outputUtf8Writer = new StreamWriter(codeStream, Utf8NoBom);
                    BundleFileToCSource(symbolName, true, inputStream, outputUtf8Writer);
                }))
                {
                    state.Stop();
                }
            });
        }
        else
        {
            Emit(Path.Combine(OutputDirectory, CombinedResourceSource), (codeStream) => {
                using var outputUtf8Writer = new StreamWriter(codeStream, Utf8NoBom);
                bool shouldAddHeader = true;
                foreach (var destinationFileGroup in remainingDestinationFilesToBundle)
                {
                    ITaskItem destinationFile = destinationFileGroup.First();
                    var symbolName = ToSafeSymbolName(destinationFileGroup.Key);
                    using var inputStream = File.OpenRead(destinationFile.ItemSpec);
                    BundleFileToCSource(symbolName, shouldAddHeader, inputStream, outputUtf8Writer);
                    shouldAddHeader = false;
                }
            });
        }

        List<ITaskItem> bundledResources = new(FilesToBundle.Length);
        foreach (ITaskItem file in FilesToBundle)
        {
            ITaskItem bundledResource = file;
            string resourceDataSymbol = ToSafeSymbolName(bundledResource.GetMetadata("DestinationFile"));
            bundledResource.SetMetadata("DataSymbol", $"{resourceDataSymbol}_data");
            bundledResource.SetMetadata("DataLenSymbol", $"{resourceDataSymbol}_data_len");
            bundledResource.SetMetadata("DataLenSymbolValue", symbolDataLen[resourceDataSymbol].ToString());
            bundledResources.Add(bundledResource);
        }
        BundledResources = bundledResources.ToArray();

        if (!string.IsNullOrEmpty(BundleFile))
        {
            var filesToBundleByRegisteredName = FilesToBundle.GroupBy(file => {
                var registeredName = file.GetMetadata("RegisteredName");
                if(string.IsNullOrEmpty(registeredName))
                {
                    registeredName = Path.GetFileName(file.ItemSpec);
                }
                return registeredName;
            }).ToList();

            string resourceSymbols = GatherUniqueExportedResourceDataSymbols(remainingDestinationFilesToBundle);

            var files = filesToBundleByRegisteredName.Select(group => {
                ITaskItem registeredFile = group.First();
                var registeredFilename = group.Key;
                var resourceName = ToSafeSymbolName(registeredFilename, false);
                // Different timezone resources may have the same contents, use registered name to differentiate preallocated resources
                var resourceDataSymbol = ToSafeSymbolName(registeredFile.GetMetadata("DestinationFile"));

                string culture = registeredFile.GetMetadata("Culture");
                string? resourceSymbolName = null;
                if (File.Exists(registeredFile.GetMetadata("SymbolFile")))
                    resourceSymbolName = ToSafeSymbolName(registeredFile.GetMetadata("SymbolFile"));

                return (registeredFilename, resourceName, resourceDataSymbol, culture, resourceSymbolName);
            }).ToList();

            Log.LogMessage(MessageImportance.Low, $"Bundling {files.Count} files for {BundleRegistrationFunctionName}");

            // Generate source file to preallocate resources and register bundled resources
            Emit(Path.Combine(OutputDirectory, BundleFile), (outputStream) =>
            {
                using var outputUtf8Writer = new StreamWriter(outputStream, Utf8NoBom);
                GenerateBundledResourcePreallocationAndRegistration(resourceSymbols, BundleRegistrationFunctionName, files, outputUtf8Writer);
            });
        }

        return !Log.HasLoggedErrors;
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

    private static string GatherUniqueExportedResourceDataSymbols(IEnumerable<IGrouping<string, ITaskItem>> uniqueDestinationFiles)
    {
        StringBuilder resourceSymbols = new ();
        HashSet<string> resourcesAdded = new (); // Different Timezone resources may have the same contents
        foreach (var uniqueDestinationFile in uniqueDestinationFiles)
        {
            string resourceDataSymbol = ToSafeSymbolName(uniqueDestinationFile.Key);
            if (!resourcesAdded.Contains(resourceDataSymbol))
            {
                resourceSymbols.AppendLine($"extern uint8_t {resourceDataSymbol}_data[];");
                resourceSymbols.AppendLine($"extern const uint32_t {resourceDataSymbol}_data_len;");
                resourceSymbols.AppendLine($"#define {resourceDataSymbol}_data_len_val {symbolDataLen[resourceDataSymbol]}");
                resourcesAdded.Add(resourceDataSymbol);
            }
        }
        return resourceSymbols.ToString();
    }

    private static void GenerateBundledResourcePreallocationAndRegistration(string resourceSymbols, string bundleRegistrationFunctionName, ICollection<(string registeredFilename, string resourceName, string resourceDataSymbol, string culture, string? resourceSymbolName)> files, StreamWriter outputUtf8Writer)
    {
        List<string> preallocatedSource = new ();

        string assemblyTemplate = Utils.GetEmbeddedResource("mono-bundled-assembly.template");
        string satelliteAssemblyTemplate = Utils.GetEmbeddedResource("mono-bundled-satellite-assembly.template");
        string symbolDataTemplate = Utils.GetEmbeddedResource("mono-bundled-data.template");

        var preallocatedResources = new StringBuilder();
        List<string> preallocatedAssemblies = new ();
        List<string> preallocatedSatelliteAssemblies = new ();
        List<string> preallocatedData = new ();
        int assembliesCount = 0;
        int satelliteAssembliesCount = 0;
        int dataCount = 0;
        foreach (var tuple in files)
        {
            string resourceId = tuple.registeredFilename;

            // Generate Preloaded MonoBundled*Resource structs
            string preloadedStruct;
            if (!string.IsNullOrEmpty(tuple.culture))
            {
                preloadedStruct = satelliteAssemblyTemplate;
                preloadedStruct.Replace("%Culture%", tuple.culture);
                resourceId = $"{tuple.culture}/{tuple.registeredFilename}";
                preallocatedSatelliteAssemblies.Add($"    (MonoBundledResource *)&{tuple.resourceName}");
                satelliteAssembliesCount += 1;
            }
            else if (tuple.registeredFilename.EndsWith(".dll"))
            {
                preloadedStruct = assemblyTemplate;
                // Add associated symfile information to MonoBundledAssemblyResource structs
                string preloadedSymbolData = "";
                if (!string.IsNullOrEmpty(tuple.resourceSymbolName))
                {
                    preloadedSymbolData = $",\n{Utils.GetEmbeddedResource("mono-bundled-symbol.template")
                                                .Replace("%ResourceSymbolName%", tuple.resourceSymbolName)
                                                .Replace("%SymbolLen%", symbolDataLen[tuple.resourceSymbolName].ToString())}";
                }
                preloadedStruct = preloadedStruct.Replace("%MonoBundledSymbolData%", preloadedSymbolData);
                preallocatedAssemblies.Add($"    (MonoBundledResource *)&{tuple.resourceName}");
                assembliesCount += 1;
            }
            else
            {
                preloadedStruct = symbolDataTemplate;
                preallocatedData.Add($"    (MonoBundledResource *)&{tuple.resourceName}");
                dataCount += 1;
            }

            preallocatedSource.Add(preloadedStruct.Replace("%ResourceName%", tuple.resourceName)
                                         .Replace("%ResourceDataSymbol%", tuple.resourceDataSymbol)
                                         .Replace("%ResourceID%", resourceId)
                                         .Replace("%RegisteredFilename%", tuple.registeredFilename)
                                         .Replace("%Len%", $"{tuple.resourceDataSymbol}_data_len_val"));
        }

        var addPreallocatedResources = new StringBuilder();
        if (assembliesCount != 0) {
            preallocatedResources.AppendLine($"MonoBundledResource *bundledAssemblyResources[] = {{\n{string.Join(",\n", preallocatedAssemblies)}\n}};");
            addPreallocatedResources.AppendLine($"    mono_bundled_resources_add (bundledAssemblyResources, {assembliesCount});");
        }
        if (satelliteAssembliesCount != 0) {
            preallocatedResources.AppendLine($"MonoBundledResource *bundledSatelliteAssemblyResources[] = {{\n{string.Join(",\n", preallocatedSatelliteAssemblies)}\n}};");
            addPreallocatedResources.AppendLine($"    mono_bundled_resources_add (bundledSatelliteAssemblyResources, {satelliteAssembliesCount});");
        }
        if (dataCount != 0) {
            preallocatedResources.AppendLine($"MonoBundledResource *bundledDataResources[] = {{\n{string.Join(",\n", preallocatedData)}\n}};");
            addPreallocatedResources.AppendLine($"    mono_bundled_resources_add (bundledDataResources, {dataCount});");
        }

        outputUtf8Writer.Write(Utils.GetEmbeddedResource("mono-bundled-resource-preallocation-and-registration.template")
                                .Replace("%ResourceSymbols%", resourceSymbols)
                                .Replace("%PreallocatedStructs%", string.Join("\n", preallocatedSource))
                                .Replace("%PreallocatedResources%", preallocatedResources.ToString())
                                .Replace("%BundleRegistrationFunctionName%", bundleRegistrationFunctionName)
                                .Replace("%AddPreallocatedResources%", addPreallocatedResources.ToString()));
    }

    private void BundleFileToCSource(string symbolName, bool shouldAddHeader, FileStream inputStream, StreamWriter outputUtf8Writer)
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

        if (shouldAddHeader)
            outputUtf8Writer.WriteLine("#include <stdint.h>");

        outputUtf8Writer.Write($"uint8_t {symbolName}_data[] = {{");
        outputUtf8Writer.Flush();
        while ((bytesRead = inputStream.Read(buf, 0, buf.Length)) > 0)
        {
            for (var i = 0; i < bytesRead; i++)
            {
                if (bytesEmitted++ % 12 == 0)
                {
                    outputUtf8Writer.BaseStream.Write(NewLineAndIndentation, 0, NewLineAndIndentation.Length);
                }

                var byteValue = buf[i];
                outputUtf8Writer.BaseStream.Write(HexToUtf8Lookup, byteValue * 6, 6);
            }

            generatedArrayLength += bytesRead;
        }

        outputUtf8Writer.WriteLine("0\n};");
        outputUtf8Writer.WriteLine($"const uint32_t {symbolName}_data_len = {generatedArrayLength};");
        outputUtf8Writer.Flush();
        outputUtf8Writer.BaseStream.Flush();

        lock (symbolDataLen)
        {
            if (!symbolDataLen.TryAdd(symbolName, generatedArrayLength) && symbolDataLen[symbolName] != generatedArrayLength)
                Log.LogMessage(MessageImportance.High, $"There are duplicate resources with the same output symbol '{symbolName}' but have differing content sizes '{symbolDataLen[symbolName]}' != '{generatedArrayLength}'.");
        }
    }

    private static string ToSafeSymbolName(string destinationFileName, bool filenameOnly = true)
    {
        // Since destinationFileName includes a content hash, we can safely strip off the directory name
        // as the filename is always unique enough. This avoid disclosing information about the build
        // file structure in the resulting symbols.
        var filename = destinationFileName;
        if (filenameOnly)
            filename = Path.GetFileName(destinationFileName);

        // Equivalent to the logic from "xxd --include"
        var sb = new StringBuilder();
        foreach (var c in filename)
        {
            sb.Append(IsAlphanumeric(c) ? c :
                      (c == '+') ? "plus" : '_'); // To help differentiate timezones differing by a symbol (i.e. GMT+0 GMT-0)
        }

        return sb.ToString();
    }

    // Equivalent to "isalnum"
    private static bool IsAlphanumeric(char c) => c
        is (>= 'a' and <= 'z')
        or (>= 'A' and <= 'Z')
        or (>= '0' and <= '9');

    #endregion

}
