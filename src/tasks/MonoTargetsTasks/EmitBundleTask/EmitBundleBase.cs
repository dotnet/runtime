// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public abstract class EmitBundleBase : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private CancellationTokenSource BuildTaskCancelled { get; } = new();

    private readonly Dictionary<string, string> _resourceDataSymbolDictionary = new();

    private readonly Dictionary<string, string[]> _resourcesForDataSymbolDictionary = new();

    private const string RegisteredName = "RegisteredName";

    /// Truncate encoded hashes used in file names and symbols to 24 characters.
    /// Represents a 128-bit hash output encoded in base64 format.
    private const int MaxEncodedHashLength = 24;

    /// Must have DestinationFile metadata, which is the output filename
    /// Could have RegisteredName, otherwise it would be the filename.
    /// RegisteredName should be prefixed with namespace in form of unix like path. For example: "/usr/share/zoneinfo/"
    [Required]
    public ITaskItem[] FilesToBundle { get; set; } = Array.Empty<ITaskItem>();

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
    /// Path to store build artifacts
    /// <summary>
    [Required]
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Resources that were bundled
    ///
    /// Successful bundling will set the following metadata on the items:
    ///   - DataSymbol
    ///   - LenSymbol
    /// <summary>
    [Output]
    public ITaskItem[] BundledResources { get; set; } = default!;

    // Set only if @BundleFile was set
    [Output]
    public string? BundleRegistrationFile { get; set; }

    public override bool Execute()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Log.LogError($"OutputDirectory={OutputDirectory} doesn't exist.");
            return false;
        }

        List<ITaskItem> bundledResources = new(FilesToBundle.Length);
        foreach (ITaskItem bundledResource in FilesToBundle)
        {
            var resourcePath = bundledResource.ItemSpec;

            bundledResource.SetMetadata("ResourceType", "DataResource");
            try
            {
                using FileStream resourceContents = File.OpenRead(resourcePath);
                using PEReader resourcePEReader = new(resourceContents);
                if (resourcePEReader.HasMetadata)
                {
                    string? managedAssemblyCulture = null;

                    var resourceMetadataReader = PEReaderExtensions.GetMetadataReader(resourcePEReader);
                    if (resourceMetadataReader.IsAssembly)
                    {
                        bundledResource.SetMetadata("ResourceType", "AssemblyResource");
                        managedAssemblyCulture = resourceMetadataReader.GetString(resourceMetadataReader.GetAssemblyDefinition().Culture);
                    }

                    bool isSatelliteAssembly = !string.IsNullOrEmpty(managedAssemblyCulture) && !managedAssemblyCulture!.Equals("neutral", StringComparison.OrdinalIgnoreCase);
                    if (resourcePath.EndsWith(".resources.dll", StringComparison.InvariantCultureIgnoreCase) || isSatelliteAssembly)
                    {
                        bundledResource.SetMetadata("ResourceType", "SatelliteAssemblyResource");
                        if (isSatelliteAssembly)
                            bundledResource.SetMetadata("Culture", managedAssemblyCulture);
                    }
                }
            }
            catch (BadImageFormatException e)
            {
                if (resourcePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    Log.LogMessage(MessageImportance.High, $"Resource '{resourcePath}' was interpreted with ResourceType 'DataResource' but has a '.dll' extension. Error: {e}");
            }

            var registeredName = bundledResource.GetMetadata(RegisteredName);
            if (string.IsNullOrEmpty(registeredName))
            {
                string culture = bundledResource.GetMetadata("Culture");
                registeredName = !string.IsNullOrEmpty(culture) ? culture + "/" + Path.GetFileName(resourcePath) : Path.GetFileName(resourcePath);
                bundledResource.SetMetadata(RegisteredName, registeredName);
            }

            string resourceDataSymbol = $"bundled_resource_{ToSafeSymbolName(TruncateEncodedHash(Utils.ComputeHashEx(resourcePath, Utils.HashAlgorithmType.SHA256, Utils.HashEncodingType.Base64Safe), MaxEncodedHashLength))}";
            if (_resourceDataSymbolDictionary.ContainsKey(registeredName))
            {
                throw new LogAsErrorException($"Multiple resources have the same {RegisteredName} '{registeredName}'. Ensure {nameof(FilesToBundle)} 'RegisteredName' metadata are set and unique.");
            }
            _resourceDataSymbolDictionary.Add(registeredName, resourceDataSymbol);

            string destinationFile = Path.Combine(OutputDirectory, resourceDataSymbol + GetDestinationFileExtension());
            bundledResource.SetMetadata("DestinationFile", destinationFile);

            string[] resourcesWithDataSymbol;
            if (_resourcesForDataSymbolDictionary.TryGetValue(resourceDataSymbol, out string[]? resourcesAlreadyWithDataSymbol))
            {
                _resourcesForDataSymbolDictionary.Remove(resourceDataSymbol);
                Log.LogMessage(MessageImportance.Low, $"Resource '{registeredName}' has the same output destination file '{destinationFile}' as '{string.Join("', '", resourcesAlreadyWithDataSymbol)}'");
                resourcesWithDataSymbol = resourcesAlreadyWithDataSymbol.Append(registeredName).ToArray();
            }
            else
            {
                resourcesWithDataSymbol = new[] { registeredName };
                Log.LogMessage(MessageImportance.Low, $"Resource '{registeredName}' is associated with output destination file '{destinationFile}'");
            }
            _resourcesForDataSymbolDictionary.Add(resourceDataSymbol, resourcesWithDataSymbol);

            bundledResources.Add(bundledResource);
        }

        // The DestinationFile (output filename) already includes a content hash. Grouping by this filename therefore
        // produces one group per file-content. We only want to emit one copy of each file-content, and one symbol for it.
        var remainingDestinationFilesToBundle = bundledResources.GroupBy(file => file.GetMetadata("DestinationFile")).ToArray();

        var verboseCount = 0;

        // Generate source file(s) containing each resource's byte data and size
        int allowedParallelism = Math.Max(Math.Min(bundledResources.Count, Environment.ProcessorCount), 1);
        IBuildEngine9? be9 = BuildEngine as IBuildEngine9;
        if (be9 is not null)
            allowedParallelism = be9.RequestCores(allowedParallelism);

        try
        {
            Parallel.For(0, remainingDestinationFilesToBundle.Length, new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism, CancellationToken = BuildTaskCancelled.Token }, (i, state) =>
            {
                var group = remainingDestinationFilesToBundle[i];

                var contentSourceFile = group.First();

                var inputFile = contentSourceFile.ItemSpec;
                var destinationFile = contentSourceFile.GetMetadata("DestinationFile");
                var registeredName = contentSourceFile.GetMetadata(RegisteredName);

                var count = Interlocked.Increment(ref verboseCount);
                Log.LogMessage(MessageImportance.Low, "{0}/{1} Bundling {2} ...", count, remainingDestinationFilesToBundle.Length, registeredName);

                Log.LogMessage(MessageImportance.Low, "Bundling {0} into {1}", inputFile, destinationFile);
                var symbolName = _resourceDataSymbolDictionary[registeredName];
                if (!EmitBundleFile(destinationFile, (codeStream) =>
                {
                    using var inputStream = File.OpenRead(inputFile);
                    using var outputUtf8Writer = new StreamWriter(codeStream, Utf8NoBom);
                    BundleFileToCSource(symbolName, inputStream, outputUtf8Writer);
                }))
                {
                    state.Stop();
                }
            });
        }
        finally
        {
            be9?.ReleaseCores(allowedParallelism);
        }

        foreach (ITaskItem bundledResource in bundledResources)
        {
            string registeredName = bundledResource.GetMetadata(RegisteredName);
            string resourceDataSymbol = _resourceDataSymbolDictionary[registeredName];
            bundledResource.SetMetadata("DataSymbol", $"{resourceDataSymbol}_data");
            bundledResource.SetMetadata("DataLenSymbol", $"{resourceDataSymbol}_data_len");
            bundledResource.SetMetadata("DataLenSymbolValue", symbolDataLen[resourceDataSymbol].ToString());
        }

        if (!string.IsNullOrEmpty(BundleFile))
        {
            string resourceSymbols = GatherUniqueExportedResourceDataSymbols(bundledResources);

            var files = bundledResources.Select(bundledResource => {
                var resourceType = bundledResource.GetMetadata("ResourceType");
                var registeredName = bundledResource.GetMetadata(RegisteredName);
                var resourceName = ToSafeSymbolName(registeredName, false);
                // Different timezone resources may have the same contents, use registered name to differentiate preallocated resources
                var resourceDataSymbol = _resourceDataSymbolDictionary[registeredName];

                string culture = bundledResource.GetMetadata("Culture");
                string? resourceSymbolName = null;
                if (File.Exists(bundledResource.GetMetadata("SymbolFile")))
                    resourceSymbolName = ToSafeSymbolName(bundledResource.GetMetadata("SymbolFile"));

                return (resourceType, registeredName, resourceName, resourceDataSymbol, culture, resourceSymbolName);
            }).ToList();

            Log.LogMessage(MessageImportance.Low, $"Bundling {files.Count} files for {BundleRegistrationFunctionName}");

            string bundleFilePath = Path.Combine(OutputDirectory, BundleFile);

            // Generate source file to preallocate resources and register bundled resources
            EmitBundleFile(bundleFilePath, (outputStream) =>
            {
                using var outputUtf8Writer = new StreamWriter(outputStream, Utf8NoBom);
                GenerateBundledResourcePreallocationAndRegistration(resourceSymbols, BundleRegistrationFunctionName, files, outputUtf8Writer);
            });

            BundleRegistrationFile = bundleFilePath;
        }

        BundledResources = bundledResources.ToArray();

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

    public abstract bool EmitBundleFile(string destinationFile, Action<Stream> writeToOutputStream);

    public abstract string GetDestinationFileExtension();

    private static Dictionary<string, int> symbolDataLen = new();

    private string GatherUniqueExportedResourceDataSymbols(List<ITaskItem> uniqueDestinationFiles)
    {
        StringBuilder resourceSymbols = new ();
        HashSet<string> resourcesAdded = new (); // Different Timezone resources may have the same contents
        foreach (var uniqueDestinationFile in uniqueDestinationFiles)
        {
            string registeredName = uniqueDestinationFile.GetMetadata(RegisteredName);
            string resourceDataSymbol = _resourceDataSymbolDictionary[registeredName];
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

    private static void GenerateBundledResourcePreallocationAndRegistration(string resourceSymbols, string bundleRegistrationFunctionName, ICollection<(string resourceType, string registeredName, string resourceName, string resourceDataSymbol, string culture, string? resourceSymbolName)> files, StreamWriter outputUtf8Writer)
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
            string resourceId = tuple.registeredName;

            // Generate Preloaded MonoBundled*Resource structs
            string preloadedStruct;
            switch (tuple.resourceType)
            {
                case "SatelliteAssemblyResource":
                {
                    preloadedStruct = satelliteAssemblyTemplate;
                    preloadedStruct = preloadedStruct.Replace("%Culture%", tuple.culture);
                    resourceId = tuple.registeredName;
                    preallocatedSatelliteAssemblies.Add($"    (MonoBundledResource *)&{tuple.resourceName}");
                    satelliteAssembliesCount += 1;
                    break;
                }
                case "AssemblyResource":
                {
                    preloadedStruct = assemblyTemplate;
                    // Add associated symfile information to MonoBundledAssemblyResource structs
                    string preloadedSymbolData = "";
                    if (!string.IsNullOrEmpty(tuple.resourceSymbolName))
                    {
                        preloadedSymbolData = $",\n{Utils.GetEmbeddedResource("mono-bundled-symbol.template")
                                                    .Replace("%ResourceSymbolName%", tuple.resourceSymbolName)
                                                    .Replace("%SymbolLen%", symbolDataLen[tuple.resourceSymbolName!].ToString())}";
                    }
                    preloadedStruct = preloadedStruct.Replace("%MonoBundledSymbolData%", preloadedSymbolData);
                    preallocatedAssemblies.Add($"    (MonoBundledResource *)&{tuple.resourceName}");
                    assembliesCount += 1;
                    break;
                }
                case "DataResource":
                {
                    preloadedStruct = symbolDataTemplate;
                    preallocatedData.Add($"    (MonoBundledResource *)&{tuple.resourceName}");
                    dataCount += 1;
                    break;
                }
                default:
                {
                    throw new Exception($"Unsupported ResourceType '{tuple.resourceType}' for Resource '{tuple.resourceName}' with registered name '{tuple.registeredName}'. Ensure that the resource's ResourceType metadata is populated.");
                }
            }

            var resourceDataSymbol = tuple.resourceDataSymbol;

            preallocatedSource.Add(preloadedStruct.Replace("%ResourceName%", tuple.resourceName)
                                         .Replace("%ResourceDataSymbol%", resourceDataSymbol)
                                         .Replace("%ResourceID%", resourceId)
                                         .Replace("%RegisteredFilename%", tuple.registeredName)
                                         .Replace("%Len%", $"{resourceDataSymbol}_data_len_val"));
        }

        List<string> addPreallocatedResources = new ();
        if (assembliesCount != 0) {
            preallocatedResources.AppendLine($"MonoBundledResource *{bundleRegistrationFunctionName}_assembly_resources[] = {{\n{string.Join(",\n", preallocatedAssemblies)}\n}};");
            addPreallocatedResources.Add($"    mono_bundled_resources_add ({bundleRegistrationFunctionName}_assembly_resources, {assembliesCount});");
        }
        if (satelliteAssembliesCount != 0) {
            preallocatedResources.AppendLine($"MonoBundledResource *{bundleRegistrationFunctionName}_satellite_assembly_resources[] = {{\n{string.Join(",\n", preallocatedSatelliteAssemblies)}\n}};");
            addPreallocatedResources.Add($"    mono_bundled_resources_add ({bundleRegistrationFunctionName}_satellite_assembly_resources, {satelliteAssembliesCount});");
        }
        if (dataCount != 0) {
            preallocatedResources.AppendLine($"MonoBundledResource *{bundleRegistrationFunctionName}_data_resources[] = {{\n{string.Join(",\n", preallocatedData)}\n}};");
            addPreallocatedResources.Add($"    mono_bundled_resources_add ({bundleRegistrationFunctionName}_data_resources, {dataCount});");
        }

        outputUtf8Writer.Write(Utils.GetEmbeddedResource("mono-bundled-resource-preallocation-and-registration.template")
                                .Replace("%ResourceSymbols%", resourceSymbols)
                                .Replace("%PreallocatedStructs%", string.Join("\n", preallocatedSource))
                                .Replace("%PreallocatedResources%", preallocatedResources.ToString())
                                .Replace("%BundleRegistrationFunctionName%", bundleRegistrationFunctionName)
                                .Replace("%AddPreallocatedResources%", string.Join("\n", addPreallocatedResources)));
    }

    private void BundleFileToCSource(string symbolName, FileStream inputStream, StreamWriter outputUtf8Writer)
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

        outputUtf8Writer.WriteLine("#include <stdint.h>");

        string[] resourcesForDataSymbol = _resourcesForDataSymbolDictionary[symbolName];
        outputUtf8Writer.WriteLine($"// Resource Registered Names: {string.Join(", ", resourcesForDataSymbol)}");
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
            int len = 0;
            if (symbolDataLen.TryGetValue(symbolName, out len))
            {
                if (len != generatedArrayLength)
                    Log.LogMessage(MessageImportance.High, $"There are duplicate resources with the same output symbol '{symbolName}' but have differing content sizes '{symbolDataLen[symbolName]}' != '{generatedArrayLength}'.");
            }
            else
            {
                symbolDataLen.Add(symbolName, generatedArrayLength);
            }
        }
    }

    private static string ToSafeSymbolName(string destinationFileName, bool filenameOnly = true)
    {
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

    private static string TruncateEncodedHash(string encodedHash, int maxEncodedHashLength)
        => string.IsNullOrEmpty(encodedHash)
            ? string.Empty
            : encodedHash.Substring(0, Math.Min(encodedHash.Length, maxEncodedHashLength));

    // Equivalent to "isalnum"
    private static bool IsAlphanumeric(char c) => c
        is (>= 'a' and <= 'z')
        or (>= 'A' and <= 'Z')
        or (>= '0' and <= '9');

    #endregion

}
