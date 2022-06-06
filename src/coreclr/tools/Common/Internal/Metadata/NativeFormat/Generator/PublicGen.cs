// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

//
// This class generates the common API declarations that any metadata reader must implement.
// In general, this script consumes the metadata record schema defined in SchemaDef.cs and
// generates two interfaces and two structs for each - one(interface, struct); pair corresponding
// to the metadata record itself, and one(interface, struct); pair corresponding to the "Handle"
// used to reference the specific record type.The interfaces are used as a way of
// enforcing that the structs implement all required public members, but are not publicly consumed
// and are declared as internal. The use of structs instead of classes for each record was driven
// by a requirement from the Roslyn team that a metadata reader must minimize as much as possible
// the number of allocations made; thus, structs are allocated on the stack and exist only as
// long as the declaring scope remains on the stack.
//
// Each record interface simply declares as properties the members declared in the schema definition,
// and each struct is declared as partial and as implmenting the interface, thus requiring all
// interface properties to be supplied by the metadata reader implementation.
//
// Each handle interface requires type-specific equality functionality by itself implementing
// IEquatable<XXXHandle>, and the handle structs similarly declare this interface to require that
// the implementation be supplied by the reader.
//

class PublicGen : CsWriter
{
    public PublicGen(string fileName)
        : base(fileName)
    {
    }

    public void EmitSource()
    {
        WriteLine("using System;");
        WriteLine("using System.Reflection;");
        WriteLine("using System.Collections.Generic;");
        WriteLine("using System.Runtime.CompilerServices;");
        WriteLine();

        WriteLine("#pragma warning disable 108     // base type 'uint' is not CLS-compliant");
        WriteLine("#pragma warning disable 3009    // base type 'uint' is not CLS-compliant");
        WriteLine("#pragma warning disable 282     // There is no defined ordering between fields in multiple declarations of partial class or struct");
        WriteLine();

        OpenScope("namespace Internal.Metadata.NativeFormat");

        EmitEnums();

        CloseScope("Internal.Metadata.NativeFormat");
    }

    private void EmitEnums()
    {
        foreach (var record in SchemaDef.EnumSchema)
        {
            EmitEnum(record);
        }

        //
        // HandleType enum is not the schema
        //
        EmitEnum(
            new RecordDef(
                name: "HandleType",
                baseTypeName: "byte",
                members:
                    new MemberDef[] {
                        new MemberDef(name: "Null", value: "0x0")
                    }
                    .Concat(
                        SchemaDef.HandleSchema.Select((name, index) =>
                            new MemberDef(name: name, value: $"0x{index + 1:x}"))
                    )
                    .ToArray()
                )
            );
    }

    private void EmitEnum(RecordDef record)
    {
        if ((record.Flags & RecordDefFlags.Flags) != 0)
            WriteScopeAttribute("[Flags]");
        WriteTypeAttributesForCoreLib();
        OpenScope($"public enum {record.Name} : {record.BaseTypeName}");

        foreach (var member in record.Members)
        {
            if (member.Comment != null)
            {
                WriteLineIfNeeded();
                WriteDocComment(member.Comment);
            }
            WriteLine($"{member.Name} = {member.Value},");
        }

        CloseScope(record.Name);
    }
}
