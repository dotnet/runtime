// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class WriterGen : CsWriter
{
    public WriterGen(string fileName)
        : base(fileName)
    {
    }

    public void EmitSource()
    {
        WriteLine("#pragma warning disable 649, SA1121, IDE0036, SA1129");
        WriteLine();

        WriteLine("using System;");
        WriteLine("using System.IO;");
        WriteLine("using System.Collections.Generic;");
        WriteLine("using System.Reflection;");
        WriteLine("using System.Threading;");
        WriteLine("using Internal.LowLevelLinq;");
        WriteLine("using Internal.Metadata.NativeFormat.Writer;");
        WriteLine("using Internal.NativeFormat;");
        WriteLine("using HandleType = Internal.Metadata.NativeFormat.HandleType;");
        WriteLine("using Debug = System.Diagnostics.Debug;");
        WriteLine();

        OpenScope("namespace Internal.Metadata.NativeFormat.Writer");

        foreach (var record in SchemaDef.RecordSchema)
        {
            EmitRecord(record);
        }

        CloseScope("Internal.Metadata.NativeFormat.Writer");
    }

    private void EmitRecord(RecordDef record)
    {
        bool isConstantStringValue = record.Name == "ConstantStringValue";

        OpenScope($"public partial class {record.Name} : MetadataRecord");

        if ((record.Flags & RecordDefFlags.ReentrantEquals) != 0)
        {
            OpenScope($"public {record.Name}()");
            WriteLine("_equalsReentrancyGuard = new ThreadLocal<ReentrancyGuardStack>(() => new ReentrancyGuardStack());");
            CloseScope();
        }

        OpenScope("public override HandleType HandleType");
        OpenScope("get");
        WriteLine($"return HandleType.{record.Name};");
        CloseScope();
        CloseScope("HandleType");

        OpenScope("internal override void Visit(IRecordVisitor visitor)");
        foreach (var member in record.Members)
        {
            if ((member.Flags & MemberDefFlags.RecordRef) == 0)
                continue;

            WriteLine($"{member.Name} = visitor.Visit(this, {member.Name});");
        }
        CloseScope("Visit");

        OpenScope("public override sealed bool Equals(Object obj)");
        WriteLine("if (Object.ReferenceEquals(this, obj)) return true;");
        WriteLine($"var other = obj as {record.Name};");
        WriteLine("if (other == null) return false;");
        if ((record.Flags & RecordDefFlags.ReentrantEquals) != 0)
        {
            WriteLine("if (_equalsReentrancyGuard.Value.Contains(other))");
            WriteLine("    return true;");
            WriteLine("_equalsReentrancyGuard.Value.Push(other);");
            WriteLine("try");
            WriteLine("{");
        }
        foreach (var member in record.Members)
        {
            if ((member.Flags & MemberDefFlags.NotPersisted) != 0)
                continue;

            if ((record.Flags & RecordDefFlags.CustomCompare) != 0 && (member.Flags & MemberDefFlags.Compare) == 0)
                continue;

            if ((member.Flags & MemberDefFlags.Sequence) != 0)
            {
                if ((member.Flags & MemberDefFlags.CustomCompare) != 0)
                    WriteLine($"if (!{member.Name}.SequenceEqual(other.{member.Name}, {member.TypeName}Comparer.Instance)) return false;");
                else
                    WriteLine($"if (!{member.Name}.SequenceEqual(other.{member.Name})) return false;");
            }
            else
            if ((member.Flags & (MemberDefFlags.Map | MemberDefFlags.RecordRef)) != 0)
            {
                WriteLine($"if (!Object.Equals({member.Name}, other.{member.Name})) return false;");
            }
            else
            if ((member.Flags & MemberDefFlags.CustomCompare) != 0)
            {
                WriteLine($"if (!CustomComparer.Equals({member.Name}, other.{member.Name})) return false;");
            }
            else
            {
                WriteLine($"if ({member.Name} != other.{member.Name}) return false;");
            }
        }
        if ((record.Flags & RecordDefFlags.ReentrantEquals) != 0)
        {
            WriteLine("}");

            WriteLine("finally");
            WriteLine("{");
            WriteLine("    var popped = _equalsReentrancyGuard.Value.Pop();");
            WriteLine("    Debug.Assert(Object.ReferenceEquals(other, popped));");
            WriteLine("}");
        }
        WriteLine("return true;");
        CloseScope("Equals");
        if ((record.Flags & RecordDefFlags.ReentrantEquals) != 0)
            WriteLine("private ThreadLocal<ReentrancyGuardStack> _equalsReentrancyGuard;");

        OpenScope("public override sealed int GetHashCode()");
        WriteLine("if (_hash != 0)");
        WriteLine("    return _hash;");
        WriteLine("EnterGetHashCode();");

        // Compute hash seed using stable hashcode
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(record.Name);
        byte[] hashBytes = System.Security.Cryptography.SHA256.Create().ComputeHash(nameBytes);
        int hashSeed = System.BitConverter.ToInt32(hashBytes, 0);
        WriteLine($"int hash = {hashSeed};");

        foreach (var member in record.Members)
        {
            if ((member.Flags & MemberDefFlags.NotPersisted) != 0)
                continue;

            if ((record.Flags & RecordDefFlags.CustomCompare) != 0 && (member.Flags & MemberDefFlags.Compare) == 0)
                continue;

            if (member.TypeName as string == "ConstantStringValue")
            {
                WriteLine($"hash = ((hash << 13) - (hash >> 19)) ^ ({member.Name} == null ? 0 : {member.Name}.GetHashCode());");
            }
            else
            if ((member.Flags & MemberDefFlags.Array) != 0)
            {
                WriteLine($"for (int i = 0; i < {member.Name}.Length; i++)");
                WriteLine("{");
                WriteLine($"    hash = ((hash << 13) - (hash >> 19)) ^ {member.Name}[i].GetHashCode();");
                WriteLine("}");
            }
            else
            if ((member.Flags & (MemberDefFlags.List | MemberDefFlags.Map)) != 0)
            {
                if ((member.Flags & MemberDefFlags.EnumerateForHashCode) == 0)
                    continue;

                WriteLine($"for (int i = 0; i < {member.Name}.Count; i++)");
                WriteLine("{");
                WriteLine($"    hash = ((hash << 13) - (hash >> 19)) ^ ({member.Name}[i] == null ? 0 : {member.Name}[i].GetHashCode());");
                WriteLine("}");
            }
            else
            if ((member.Flags & MemberDefFlags.RecordRef) != 0 || isConstantStringValue)
            {
                WriteLine($"hash = ((hash << 13) - (hash >> 19)) ^ ({member.Name} == null ? 0 : {member.Name}.GetHashCode());");
            }
            else
            {
                WriteLine($"hash = ((hash << 13) - (hash >> 19)) ^ {member.Name}.GetHashCode();");
            }
        }
        WriteLine("LeaveGetHashCode();");
        WriteLine("_hash = hash;");
        WriteLine("return _hash;");
        CloseScope("GetHashCode");

        OpenScope("internal override void Save(NativeWriter writer)");
        if (isConstantStringValue)
        {
            WriteLine("if (Value == null)");
            WriteLine("    return;");
            WriteLine();
        }
        foreach (var member in record.Members)
        {
            if ((member.Flags & MemberDefFlags.NotPersisted) != 0)
                continue;

            var typeSet = member.TypeName as string[];
            if (typeSet != null)
            {
                if ((member.Flags & (MemberDefFlags.List | MemberDefFlags.Map)) != 0)
                {
                    WriteLine($"Debug.Assert({member.Name}.TrueForAll(handle => handle == null ||");
                    for (int i = 0; i < typeSet.Length; i++)
                        WriteLine($"    handle.HandleType == HandleType.{typeSet[i]}"
                            + ((i == typeSet.Length - 1) ? "));" : " ||"));
                }
                else
                {
                    WriteLine($"Debug.Assert({member.Name} == null ||");
                    for (int i = 0; i < typeSet.Length; i++)
                        WriteLine($"    {member.Name}.HandleType == HandleType.{typeSet[i]}"
                            + ((i == typeSet.Length - 1) ? ");" : " ||"));
                }
            }
            WriteLine($"writer.Write({member.Name});");
        }
        CloseScope("Save");

        OpenScope($"internal static {record.Name}Handle AsHandle({record.Name} record)");
        WriteLine("if (record == null)");
        WriteLine("{");
        WriteLine($"    return new {record.Name}Handle(0);");
        WriteLine("}");
        WriteLine("else");
        WriteLine("{");
        WriteLine("    return record.Handle;");
        WriteLine("}");
        CloseScope("AsHandle");

        OpenScope($"internal new {record.Name}Handle Handle");
        OpenScope("get");
        if (isConstantStringValue)
        {
            WriteLine("if (Value == null)");
            WriteLine("    return new ConstantStringValueHandle(0);");
            WriteLine("else");
            WriteLine("    return new ConstantStringValueHandle(HandleOffset);");
        }
        else
        {
            WriteLine($"return new {record.Name}Handle(HandleOffset);");
        }
        CloseScope();
        CloseScope("Handle");

        WriteLineIfNeeded();
        foreach (var member in record.Members)
        {
            if ((member.Flags & MemberDefFlags.NotPersisted) != 0)
                continue;

            string fieldType = member.GetMemberType(MemberTypeKind.WriterField);
            if ((member.Flags & (MemberDefFlags.List | MemberDefFlags.Map)) != 0)
            {
                WriteLine($"public {fieldType} {member.Name} = new {fieldType}();");
            }
            else
            {
                WriteLine($"public {fieldType} {member.Name};");
            }
        }

        CloseScope(record.Name);
    }
}
