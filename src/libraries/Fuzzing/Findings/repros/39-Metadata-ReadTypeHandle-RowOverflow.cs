// Finding: BlobReader.ReadTypeHandle decodes a TypeDefOrRefOrSpec coded index as tokenType | (value >> 2) without checking that the
// row number fits in 24 bits. Compressed integers go up to 0x1FFFFFFF, so the row can spill into the table byte of the token:
// * an encoded TypeDef row 0x02000005 comes back as TypeDef row 5, silently aliasing another type instead of failing;
// * an encoded TypeDef row 0x01000005 becomes a handle of kind 0x03, which SignatureDecoder.DecodeTypeHandle doesn't expect.
//   Release builds throw BadImageFormatException, but Debug/Checked builds hit Debug.Assert(handle.IsNil) there, an assert that
//   can never hold because it sits inside if (!handle.IsNil).
// Run: dotnet run 39-Metadata-ReadTypeHandle-RowOverflow.cs   (the assert needs a Debug/Checked build: ./run-on-local-runtime.sh)
#:property AllowUnsafeBlocks=true
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

bool reproduced = false;
unsafe
{
    // Compressed integer 0x08000014 = (row 0x02000005 << 2) | tag 0 (TypeDef).
    byte[] aliased = [0xC8, 0x00, 0x00, 0x14];
    fixed (byte* p = aliased)
    {
        var reader = new BlobReader(p, aliased.Length);
        EntityHandle handle = reader.ReadTypeHandle();
        Console.WriteLine($"ReadTypeHandle(TypeDef row 0x02000005) = {handle.Kind} row {MetadataTokens.GetRowNumber(handle)} (expected a nil handle or BadImageFormatException)");
        reproduced |= !handle.IsNil;
    }

    // FIELD signature: 0x06, ELEMENT_TYPE_CLASS (0x12), coded index for TypeDef row 0x01000005 (compressed 0x04000014).
    byte[] signature = [0x06, 0x12, 0xC4, 0x00, 0x00, 0x14];
    fixed (byte* p = signature)
    {
        var reader = new BlobReader(p, signature.Length);
        var decoder = new SignatureDecoder<string, object?>(new Provider(), null!, null);
        try
        {
            Console.WriteLine("DecodeFieldSignature returned " + decoder.DecodeFieldSignature(ref reader));
        }
        catch (BadImageFormatException ex)
        {
            Console.WriteLine($"DecodeFieldSignature threw BadImageFormatException ({ex.Message}); Debug builds assert before this");
        }
    }
}

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

sealed class Provider : ISignatureTypeProvider<string, object?>
{
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => "typedef";
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => "typeref";
    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => "typespec";
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[,]";
    public string GetByReferenceType(string elementType) => elementType + "&";
    public string GetPointerType(string elementType) => elementType + "*";
    public string GetPinnedType(string elementType) => elementType;
    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType + "<>";
    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;
    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
}
