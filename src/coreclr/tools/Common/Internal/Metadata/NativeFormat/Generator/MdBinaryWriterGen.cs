// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Generates the C# MdBinaryWriter class. This classes is responsible for correctly decoding
// data members in the .metadata file. See NativeFormatWriterGen.cs for how the MetadataWriter 
// use this class.
//

class MdBinaryWriterGen : CsWriter
{
    public MdBinaryWriterGen(string fileName)
        : base(fileName)
    {
    }

    public void EmitSource()
    {
        WriteLine("#pragma warning disable 649");
        WriteLine();

        WriteLine("using System.Collections.Generic;");
        WriteLine("using System.Reflection;");
        WriteLine("using Internal.NativeFormat;");
        WriteLine();

        OpenScope("namespace Internal.Metadata.NativeFormat.Writer");

        OpenScope("internal static partial class MdBinaryWriter");

        foreach (var primitiveType in SchemaDef.PrimitiveTypes)
        {
            EmitWritePrimitiveArray(primitiveType.Name);
        }

        foreach (var enumType in SchemaDef.EnumTypes)
        {
            EmitWriteEnum(enumType);
        }

        EmitWriteArray($"MetadataRecord");

        foreach (var typeName in SchemaDef.HandleSchema)
        {
            EmitWrite(typeName);
            EmitWriteArray(typeName);
        }

        CloseScope("MdBinaryWriter");
        CloseScope("Internal.Metadata.NativeFormat.Writer");
    }

    private void EmitWritePrimitiveArray(string typeName)
    {
        OpenScope($"public static void Write(this NativeWriter writer, {typeName}[] values)");
        WriteLine("if (values == null)");
        WriteLine("{");
        WriteLine("    writer.WriteUnsigned(0);");
        WriteLine("    return;");
        WriteLine("}");
        WriteLine("writer.WriteUnsigned((uint)values.Length);");
        WriteLine($"foreach ({typeName} value in values)");
        WriteLine("{");
        WriteLine("    writer.Write(value);");
        WriteLine("}");
        CloseScope("Write");
    }

    private void EmitWriteEnum(EnumType enumType)
    {
        OpenScope($"public static void Write(this NativeWriter writer, {enumType.Name} value)");
        WriteLine($"writer.WriteUnsigned((uint)value);");
        CloseScope("Write");
    }

    private void EmitWrite(string typeName)
    {
        OpenScope($"public static void Write(this NativeWriter writer, {typeName} record)");
        WriteLine("if (record != null)");
        WriteLine("    writer.WriteUnsigned((uint)record.Handle.Offset);");
        WriteLine("else");
        WriteLine("    writer.WriteUnsigned(0);");
        CloseScope("Write");
    }

    private void EmitWriteArray(string typeName)
    {
        OpenScope($"public static void Write(this NativeWriter writer, List<{typeName}> values)");
        WriteLine("if (values == null)");
        WriteLine("{");
        WriteLine("    writer.WriteUnsigned(0);");
        WriteLine("    return;");
        WriteLine("}");
        WriteLine("writer.WriteUnsigned((uint)values.Count);");
        WriteLine($"foreach ({typeName} value in values)");
        WriteLine("{");
        WriteLine("    writer.Write(value);");
        WriteLine("}");
        CloseScope("Write");
    }
}
