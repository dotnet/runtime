// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Generates the C# MdBinaryReader class. This classes is responsible for correctly decoding
// data members in the .metadata file. See NativeFormatReaderGen.cs for how the MetadataReader 
// use this class.
//

class MdBinaryReaderGen : CsWriter
{
    public MdBinaryReaderGen(string fileName)
        : base(fileName)
    {
    }

    public void EmitSource()
    {
        WriteLine("#pragma warning disable 649");
        WriteLine();

        WriteLine("using System;");
        WriteLine("using System.IO;");
        WriteLine("using System.Collections.Generic;");
        WriteLine("using System.Reflection;");
        WriteLine("using Internal.NativeFormat;");
        WriteLine("using Debug = System.Diagnostics.Debug;");
        WriteLine();

        OpenScope("namespace Internal.Metadata.NativeFormat");

        OpenScope("internal static partial class MdBinaryReader");

        foreach (var primitiveType in SchemaDef.PrimitiveTypes)
        {
            EmitReadPrimitiveCollection(primitiveType.TypeName, primitiveType.Name);
        }

        foreach (var enumType in SchemaDef.EnumTypes)
        {
            EmitReadEnum(enumType);
        }

        EmitReadCollection($"Handle");

        foreach (var typeName in SchemaDef.HandleSchema)
        {
            EmitRead($"{typeName}Handle");
        }

        foreach (var typeName in SchemaDef.TypeNamesWithCollectionTypes)
        {
            EmitReadCollection($"{typeName}Handle");
        }

        CloseScope("MdBinaryReader");
        CloseScope("Internal.Metadata.NativeFormat");
    }

    private void EmitReadCollection(string typeName)
    {
        string collectionTypeName = $"{typeName}Collection";

        OpenScope($"public static uint Read(this NativeReader reader, uint offset, out {collectionTypeName} values)");
        WriteLine($"values = new {collectionTypeName}(reader, offset);");
        WriteLine("uint count;");
        WriteLine("offset = reader.DecodeUnsigned(offset, out count);");
        WriteLine("for (uint i = 0; i < count; ++i)");
        WriteLine("{");
        WriteLine("    offset = reader.SkipInteger(offset);");
        WriteLine("}");
        WriteLine("return offset;");
        CloseScope("Read");
    }

    private void EmitReadPrimitiveCollection(string typeName, string shortName)
    {
        string collectionTypeName = $"{typeName}Collection";

        OpenScope($"public static unsafe uint Read(this NativeReader reader, uint offset, out {collectionTypeName} values)");
        WriteLine($"values = new {collectionTypeName}(reader, offset);");
        WriteLine("uint count;");
        WriteLine("offset = reader.DecodeUnsigned(offset, out count);");
        WriteLine($"offset = checked(offset + count * sizeof({shortName}));");
        WriteLine("return offset;");
        CloseScope("Read");
    }

    private void EmitReadEnum(EnumType enumType)
    {
        OpenScope($"public static uint Read(this NativeReader reader, uint offset, out {enumType.Name} value)");
        WriteLine($"uint ivalue;");
        WriteLine("offset = reader.DecodeUnsigned(offset, out ivalue);");
        WriteLine($"value = ({enumType.Name})ivalue;");
        WriteLine("return offset;");
        CloseScope("Read");
    }

    private void EmitRead(string typeName)
    {
        OpenScope($"public static uint Read(this NativeReader reader, uint offset, out {typeName} handle)");
        WriteLine("uint value;");
        WriteLine("offset = reader.DecodeUnsigned(offset, out value);");
        WriteLine($"handle = new {typeName}((int)value);");
        WriteLine("handle._Validate();");
        WriteLine("return offset;");
        CloseScope("Read");
    }
}
