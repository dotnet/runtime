// Finding: AssemblyDefinition.GetAssemblyName() and AssemblyReference.GetAssemblyName() turn the culture string from metadata into
// a CultureInfo (through AssemblyName.CultureName). In globalization-invariant mode (InvariantGlobalization=true, common in
// containers) that throws CultureNotFoundException for every non-neutral culture, so reading any satellite assembly, or any
// reference to one, fails. In any mode, a malformed culture string in the image throws CultureNotFoundException instead of the
// BadImageFormatException that MetadataReader uses for bad input, so tools that read untrusted assemblies see an unexpected
// exception type. GetAssemblyNameInfo() keeps the culture as a string and works in both cases.
// Run: dotnet run 35-Metadata-GetAssemblyName-Culture.cs
#:property InvariantGlobalization=true
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

bool reproduced = false;
foreach (string culture in new[] { "de", "." })
{
    using var pe = new PEReader(BuildAssembly(culture));
    MetadataReader reader = pe.GetMetadataReader();
    AssemblyDefinition definition = reader.GetAssemblyDefinition();
    string outcome;
    try
    {
        outcome = "returned " + definition.GetAssemblyName().FullName;
    }
    catch (Exception ex)
    {
        outcome = $"threw {ex.GetType().Name}";
        reproduced |= ex is not BadImageFormatException;
    }

    Console.WriteLine($"culture '{culture}': GetAssemblyName() {outcome}; GetAssemblyNameInfo() returns {definition.GetAssemblyNameInfo().FullName}");
}

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

static MemoryStream BuildAssembly(string culture)
{
    var metadata = new MetadataBuilder();
    metadata.AddModule(0, metadata.GetOrAddString("Satellite.resources.dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
    metadata.AddAssembly(metadata.GetOrAddString("Satellite.resources"), new Version(1, 0, 0, 0), metadata.GetOrAddString(culture), default, 0, AssemblyHashAlgorithm.Sha1);
    metadata.AddTypeDefinition(0, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
    var builder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
    var blob = new BlobBuilder();
    builder.Serialize(blob);
    return new MemoryStream(blob.ToArray());
}
