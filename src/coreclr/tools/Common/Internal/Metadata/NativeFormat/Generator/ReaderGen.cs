// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NativeFormatGen;

//
// This class generates most of the implementation of the MetadataReader for the NativeAOT format,
// ensuring that the contract defined by CsPublicGen2 is implemented.The generated file is
// 'NativeFormatReaderGen.cs', and any missing implementation is the supplied in the human-authored
// source counterpart 'NativeFormatReader.cs'.
//

internal sealed class ReaderGen : CsWriter
{
    public ReaderGen(string fileName)
        : base(fileName)
    {
    }

    public void EmitSource()
    {
        WriteLine("#pragma warning disable 649");
        WriteLine("#pragma warning disable 169");
        WriteLine("#pragma warning disable 282 // There is no defined ordering between fields in multiple declarations of partial class or struct");
        WriteLine("#pragma warning disable CA1066 // IEquatable<T> implementations aren't used");
        WriteLine("#pragma warning disable CA1822");
        WriteLine("#pragma warning disable IDE0059");
        WriteLine("#pragma warning disable SA1121");
        WriteLine("#pragma warning disable IDE0036, SA1129");
        WriteLine();

        WriteLine("using System;");
        WriteLine("using System.Collections.Generic;");
        WriteLine("using System.Diagnostics;");
        WriteLine("using System.Reflection;");
        WriteLine("using System.Runtime.CompilerServices;");
        WriteLine("using Internal.NativeFormat;");
        WriteLine();

        OpenScope("namespace Internal.Metadata.NativeFormat");

        foreach (var record in SchemaDef.RecordSchema)
        {
            EmitRecord(record);
            EmitHandle(record);
        }

        foreach (var typeName in SchemaDef.TypeNamesWithCollectionTypes)
        {
            EmitCollection(typeName + "HandleCollection", typeName + "Handle");
        }

        foreach (var primitiveType in SchemaDef.PrimitiveTypes)
        {
            EmitCollection(primitiveType.TypeName + "Collection", primitiveType.Name);
        }

        EmitOpaqueHandle();
        EmitCollection("HandleCollection", "Handle");
        EmitMetadataReader();

        CloseScope("Internal.Metadata.NativeFormat");
    }

    private void EmitRecord(RecordDef record)
    {
        WriteTypeAttributesForCoreLib();
        OpenScope($"public readonly partial struct {record.Name}");

        WriteLine("private readonly MetadataReader _reader;");
        WriteLine($"private readonly {record.Name}Handle _handle;");

        OpenScope($"internal {record.Name}(MetadataReader reader, {record.Name}Handle handle)");
        if (record.Name == "ConstantStringValue")
        {
            WriteLine("if (handle.IsNil)");
            WriteLine("    return;");
        }
        WriteLine("_reader = reader;");
        WriteLine("_handle = handle;");
        WriteLine("uint offset = (uint)handle.Offset;");
        WriteLine("NativeReader streamReader = reader._streamReader;");
        foreach (var member in record.Members)
        {
            if ((member.Flags & MemberDefFlags.NotPersisted) != 0)
                continue;
            WriteLine($"offset = streamReader.Read(offset, out {member.GetMemberFieldName()});");
        }
        CloseScope();

        WriteLineIfNeeded();
        WriteLine($"public {record.Name}Handle Handle => _handle;");

        foreach (var member in record.Members)
        {
            if ((member.Flags & MemberDefFlags.NotPersisted) != 0)
                continue;

            string memberType = member.GetMemberType();
            string fieldType = member.GetMemberType(MemberTypeKind.ReaderField);

            string fieldName = member.GetMemberFieldName();

            WriteLineIfNeeded();

            string description = member.GetMemberDescription();
            if (description != null)
                WriteDocComment(description);
            string optionalCast = (fieldType != memberType) ? $"({memberType})" : "";
            WriteLine($"public {memberType} {member.Name} => {optionalCast}{fieldName};");

            WriteLine($"private readonly {fieldType} {fieldName};");
        }

        CloseScope(record.Name);
    }

    private void EmitHandle(RecordDef record)
    {
        string handleName = $"{record.Name}Handle";

        WriteTypeAttributesForCoreLib();
        OpenScope($"public readonly partial struct {handleName}");

        WriteLine("internal readonly int _value;");

        OpenScope($"internal {handleName}(Handle handle) : this(handle._value)");
        CloseScope();

        OpenScope($"internal {handleName}(int value)");
        WriteLine("HandleType hType = (HandleType)(value >> 24);");
        WriteLine($"Debug.Assert(hType == 0 || hType == HandleType.{record.Name} || hType == HandleType.Null);");
        WriteLine($"_value = (value & 0x00FFFFFF) | (((int)HandleType.{record.Name}) << 24);");
        WriteLine("_Validate();");
        CloseScope();

        OpenScope("public override bool Equals(object obj)");
        WriteLine($"if (obj is {handleName})");
        WriteLine($"    return _value == (({handleName})obj)._value;");
        WriteLine("else if (obj is Handle)");
        WriteLine("    return _value == ((Handle)obj)._value;");
        WriteLine("else");
        WriteLine("    return false;");
        CloseScope("Equals");

        WriteLineIfNeeded();
        WriteLine($"public bool Equals({handleName} handle) => _value == handle._value;");

        WriteLineIfNeeded();
        WriteLine("public bool Equals(Handle handle) => _value == handle._value;");

        WriteLineIfNeeded();
        WriteLine("public override int GetHashCode() => (int)_value;");
 
        WriteLineIfNeeded();
        WriteLine($"public static implicit operator Handle({handleName} handle)");
        WriteLine("    => new Handle(handle._value);");

        WriteLineIfNeeded();
        WriteLine("internal int Offset => (_value & 0x00FFFFFF);");

        WriteLineIfNeeded();
        WriteLine($"public {record.Name} Get{record.Name}(MetadataReader reader)");
        WriteLine($"    => new {record.Name}(reader, this);");

        WriteLineIfNeeded();
        WriteLine("public bool IsNil => (_value & 0x00FFFFFF) == 0;");
 
        WriteScopeAttribute("[System.Diagnostics.Conditional(\"DEBUG\")]");
        OpenScope("internal void _Validate()");
        WriteLine($"if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.{record.Name})");
        WriteLine("    throw new ArgumentException();");
        CloseScope("_Validate");

        WriteLineIfNeeded();
        WriteLine("public override string ToString() => string.Format(\"{0:X8}\", _value);");

        CloseScope(handleName);
    }

    private void EmitCollection(string collectionTypeName, string elementTypeName)
    {
        WriteTypeAttributesForCoreLib();
        OpenScope($"public readonly partial struct {collectionTypeName}");

        WriteLine("private readonly NativeReader _reader;");
        WriteLine("private readonly uint _offset;");

        OpenScope($"internal {collectionTypeName}(NativeReader reader, uint offset)");
        WriteLine("_offset = offset;");
        WriteLine("_reader = reader;");
        CloseScope();

        OpenScope("public int Count");
        OpenScope("get");
        WriteLine("uint count;");
        WriteLine("_reader.DecodeUnsigned(_offset, out count);");
        WriteLine("return (int)count;");
        CloseScope();
        CloseScope("Count");

        OpenScope($"public Enumerator GetEnumerator()");
        WriteLine($"return new Enumerator(_reader, _offset);");
        CloseScope("GetEnumerator");

        WriteTypeAttributesForCoreLib();
        OpenScope($"public struct Enumerator");

        WriteLine("private readonly NativeReader _reader;");
        WriteLine("private uint _offset;");
        WriteLine("private uint _remaining;");
        WriteLine($"private {elementTypeName} _current;");

        OpenScope($"internal Enumerator(NativeReader reader, uint offset)");
        WriteLine("_reader = reader;");
        WriteLine("_offset = reader.DecodeUnsigned(offset, out _remaining);");
        WriteLine($"_current = default({elementTypeName});");
        CloseScope();

        OpenScope($"public {elementTypeName} Current");
        OpenScope("get");
        WriteLine("return _current;");
        CloseScope();
        CloseScope("Current");

        OpenScope("public bool MoveNext()");
        WriteLine("if (_remaining == 0)");
        WriteLine("    return false;");
        WriteLine("_remaining--;");
        WriteLine("_offset = _reader.Read(_offset, out _current);");
        WriteLine("return true;");
        CloseScope("MoveNext");

        OpenScope("public void Dispose()");
        CloseScope("Dispose");

        CloseScope("Enumerator");

        CloseScope(collectionTypeName);
    }

    private void EmitOpaqueHandle()
    {
        WriteTypeAttributesForCoreLib();
        OpenScope("public readonly partial struct Handle");

        foreach (var record in SchemaDef.RecordSchema)
        {
            string handleName = $"{record.Name}Handle";

            OpenScope($"public {handleName} To{handleName}(MetadataReader reader)");
            WriteLine($"return new {handleName}(this);");
            CloseScope($"To{handleName}");
        }

        CloseScope("Handle");
    }

    private void EmitMetadataReader()
    {
        WriteTypeAttributesForCoreLib();
        OpenScope("public partial class MetadataReader");

        foreach (var record in SchemaDef.RecordSchema)
        {
            OpenScope($"public {record.Name} Get{record.Name}({record.Name}Handle handle)");
            WriteLine($"return new {record.Name}(this, handle);");
            CloseScope($"Get{record.Name}");
        }

        CloseScope("MetadataReader");
    }
}
