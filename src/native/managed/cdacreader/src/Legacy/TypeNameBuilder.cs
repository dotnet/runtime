// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Helpers;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[Flags]
internal enum TypeNameFormat
{
    FormatNamespace = 1,
    FormatFullInst = 2,
    FormatAngleBrackets = 4,
    FormatAssembly = 8,
    FormatGenericParam = 16,
}

internal struct TypeNameBuilder
{
    [Flags]
    private enum ParseState
    {
        Start = 1,
        Name = 2,
        GenArgs = 4,
        PtrArr = 8,
        ByRef = 16,
        AssemSpec = 32,
        Error
    }
    private StringBuilder TypeString;
    private Target Target;

    private ParseState State;
    private bool UseAngleBracketsForGenerics { get; init; }
    private bool NestedName;
    private bool HasAssemblySpec;
    private bool FirstInstArg;
    private int InstNesting;
    private Stack<int>? GenericStartsStack;

    private TypeNameBuilder(StringBuilder typeString, Target target, TypeNameFormat format)
    {
        TypeString = typeString;
        Target = target;
        UseAngleBracketsForGenerics = format.HasFlag(TypeNameFormat.FormatAngleBrackets);
        State = ParseState.Start;
    }

    public static void AppendType(Target target, StringBuilder stringBuilder, Contracts.TypeHandle typeHandle, TypeNameFormat format)
    {
        AppendType(target, stringBuilder, typeHandle, default, format);
    }
    public static void AppendType(Target target, StringBuilder stringBuilder, Contracts.TypeHandle typeHandle, ReadOnlySpan<MethodTableHandle> typeInstantiation, TypeNameFormat format)
    {
        TypeNameBuilder builder = new(stringBuilder, target, format);
        AppendTypeCore(ref builder, typeHandle, typeInstantiation, format);
    }
    private static void AppendTypeCore(ref TypeNameBuilder tnb, Contracts.TypeHandle typeHandle, ReadOnlySpan<Contracts.MethodTableHandle> instantiation, TypeNameFormat format)
    {
        bool toString = format.HasFlag(TypeNameFormat.FormatNamespace) && !format.HasFlag(TypeNameFormat.FormatFullInst) && !format.HasFlag(TypeNameFormat.FormatAssembly);

        if (typeHandle.IsNull)
        {
            tnb.AddName("(null)");
        }
        else
        {
            var typeSystemContract = tnb.Target.Contracts.RuntimeTypeSystem;
            if (typeSystemContract.HasTypeParam(typeHandle))
            {
                var elemType = typeSystemContract.GetSignatureCorElementType(typeHandle);
                if (elemType != Contracts.CorElementType.ValueType)
                {
                    typeSystemContract.IsArray(typeHandle, out uint rank);
                    AppendTypeCore(ref tnb, typeSystemContract.GetTypeParam(typeHandle), default(ReadOnlySpan<Contracts.MethodTableHandle>), (TypeNameFormat)(format & ~TypeNameFormat.FormatAssembly));
                    AppendParamTypeQualifier(ref tnb, elemType, rank);
                }
                else
                {
                    tnb.TypeString.Append("VALUETYPE");
                    AppendTypeCore(ref tnb, typeSystemContract.GetTypeParam(typeHandle), Array.Empty<Contracts.MethodTableHandle>(), format & ~TypeNameFormat.FormatAssembly);
                }
            }
            else if (typeSystemContract.IsGenericVariable(typeHandle, out TargetPointer modulePointer, out uint genericParamToken))
            {
                Contracts.ModuleHandle module = tnb.Target.Contracts.Loader.GetModuleHandle(modulePointer);
                EcmaMetadataReader reader = tnb.Target.Contracts.Metadata.GetMetadata(module).EcmaMetadataReader;
                EcmaMetadataCursor cursor = reader.GetCursor(genericParamToken);
                if (format.HasFlag(TypeNameFormat.FormatGenericParam))
                {
                    uint owner = reader.GetColumnAsToken(cursor, MetadataColumnIndex.GenericParam_Owner);
                    if (EcmaMetadataReader.TokenToTable(owner) == MetadataTable.TypeDef)
                    {
                        tnb.TypeString.Append('!');
                    }
                    else
                    {
                        tnb.TypeString.Append("!!");
                    }
                }
                tnb.AddName(reader.GetColumnAsUtf8String(cursor, MetadataColumnIndex.GenericParam_Name));
                format &= ~TypeNameFormat.FormatAssembly;
            }
            else if (typeSystemContract.IsFunctionPointer(typeHandle, out ReadOnlySpan<TypeHandle> retAndArgTypes, out byte callConv))
            {
                if (format.HasFlag(TypeNameFormat.FormatNamespace))
                {
                    StringBuilder stringBuilder = new();
                    AppendType(tnb.Target, stringBuilder, retAndArgTypes[0], format);
                    stringBuilder.Append('(');
                    for (int i = 1; i < retAndArgTypes.Length; i++)
                    {
                        if (i != 1)
                        {
                            stringBuilder.Append(", ");
                        }
                        AppendType(tnb.Target, stringBuilder, retAndArgTypes[i], format);
                    }

                    if ((callConv & 0x7) == 0x5) // Is this the VARARG calling convention?
                    {
                        if (retAndArgTypes.Length > 2)
                        {
                            stringBuilder.Append(", ");
                        }
                        stringBuilder.Append("...");
                    }
                    stringBuilder.Append(')');
                    tnb.AddNameNoEscaping(stringBuilder);
                }
                else
                {
                    tnb.AddNameNoEscaping(new StringBuilder());
                }
            }
            else
            {
                // ...otherwise it's just a plain type def or an instantiated type
                MethodTableHandle methodTable = typeHandle.AsMethodTable;
                uint typeDefToken = typeSystemContract.GetTypeDefToken(methodTable);
                Contracts.ModuleHandle moduleHandle = tnb.Target.Contracts.Loader.GetModuleHandle(typeSystemContract.GetModule(methodTable));
                if (EcmaMetadataReader.RidFromToken(typeDefToken) == 0)
                {
                    tnb.AddName("(dynamicClass)");
                }
                else
                {
                    EcmaMetadataReader reader = tnb.Target.Contracts.Metadata.GetMetadata(moduleHandle).EcmaMetadataReader;
                    AppendNestedTypeDef(ref tnb, reader, typeDefToken, format);
                }

                // Append instantiation

                if (format.HasFlag(TypeNameFormat.FormatNamespace) || format.HasFlag(TypeNameFormat.FormatAssembly))
                {
                    ReadOnlySpan<MethodTableHandle> instantiationSpan = typeSystemContract.GetInstantiation(methodTable);

                    if ((instantiationSpan.Length > 0) && (!typeSystemContract.IsGenericTypeDefinition(methodTable) || toString))
                    {
                        if (instantiation.Length == 0)
                        {
                            instantiation = instantiationSpan;
                        }
                        AppendInst(ref tnb, instantiation, format);
                    }
                }
            }
            if (format.HasFlag(TypeNameFormat.FormatAssembly))
            {
                TargetPointer modulePtr = typeSystemContract.GetModule(typeHandle);

                Contracts.ModuleHandle module = tnb.Target.Contracts.Loader.GetModuleHandle(modulePtr);
                // NOTE: The DAC variant of assembly name generation is different than the runtime version. The DAC variant is simpler, and only uses SimpleName
                EcmaMetadataReader mr = tnb.Target.Contracts.Metadata.GetMetadata(module).EcmaMetadataReader;
                EcmaMetadataCursor cursor = mr.GetCursor(0x20000001);
                string assemblySimpleName = mr.GetColumnAsUtf8String(cursor, MetadataColumnIndex.Assembly_Name);

                tnb.AddAssemblySpec(assemblySimpleName);
            }
        }
    }

    private static void AppendInst(ref TypeNameBuilder tnb, ReadOnlySpan<MethodTableHandle> inst, TypeNameFormat format)
    {
        tnb.OpenGenericArguments();
        foreach (MethodTableHandle arg in inst)
        {
            tnb.OpenGenericArgument();
            if (format.HasFlag(TypeNameFormat.FormatFullInst) && !tnb.Target.Contracts.RuntimeTypeSystem.IsGenericVariable(arg, out _, out _))
            {
                AppendTypeCore(ref tnb, arg, default, format | TypeNameFormat.FormatNamespace | TypeNameFormat.FormatAssembly);
            }
            else
            {
                AppendTypeCore(ref tnb, arg, default, format & (TypeNameFormat.FormatNamespace | TypeNameFormat.FormatAngleBrackets));
            }
            tnb.CloseGenericArgument();
        }
        tnb.CloseGenericArguments();
    }

    private void OpenGenericArguments()
    {
        if (!CheckParseState(ParseState.Name))
        {
            Fail();
            return;
        }

        State = ParseState.Start;
        InstNesting++;
        FirstInstArg = true;

        TypeString.Append(UseAngleBracketsForGenerics ? '<' : '[');
    }

    private void OpenGenericArgument()
    {
        if (!CheckParseState(ParseState.Start))
        {
            Fail();
            return;
        }
        if (InstNesting == 0)
        {
            Fail();
            return;
        }

        State = ParseState.Start;
        NestedName = false;
        if (!FirstInstArg)
        {
            TypeString.Append(',');
        }

        TypeString.Append(UseAngleBracketsForGenerics ? '<' : '[');
        PushOpenGenericArgument();
    }

    private void CloseGenericArgument()
    {
        if (!CheckParseState(ParseState.Name | ParseState.GenArgs | ParseState.PtrArr | ParseState.ByRef | ParseState.AssemSpec))
        {
            Fail();
            return;
        }
        if (InstNesting == 0)
        {
            Fail();
            return;
        }

        State = ParseState.Start;

        if (HasAssemblySpec)
        {
            TypeString.Append(UseAngleBracketsForGenerics ? '>' : ']');
        }

        PopOpenGenericArgument();
    }

    private void CloseGenericArguments()
    {
        if (InstNesting == 0)
        {
            Fail();
            return;
        }
        if (!CheckParseState(ParseState.Start))
        {
            Fail();
        }

        State = ParseState.GenArgs;
        InstNesting--;

        if (FirstInstArg)
        {
            if (TypeString.Length > 0)
                TypeString.Remove(TypeString.Length - 1, 1);
        }
        else
        {
            TypeString.Append(UseAngleBracketsForGenerics ? '>' : ']');
        }
    }
    private void PushOpenGenericArgument()
    {
        GenericStartsStack ??= new();
        GenericStartsStack.Push(TypeString.Length);
    }

    private void PopOpenGenericArgument()
    {
        int strIndex = GenericStartsStack!.Pop();

        if (!HasAssemblySpec)
        {
            TypeString.Remove(strIndex - 1, 1);
        }
        HasAssemblySpec = false;
    }

    private void AddAssemblySpec(string? assemblySpec)
    {
        if (!CheckParseState(ParseState.Name | ParseState.GenArgs | ParseState.PtrArr | ParseState.ByRef))
        {
            Fail();
            return;
        }

        State = ParseState.AssemSpec;
        if (!string.IsNullOrEmpty(assemblySpec))
        {
            if (InstNesting > 0)
                EscapeEmbeddedAssemblyName(assemblySpec);
            else
                EscapeAssemblyName(assemblySpec);

            HasAssemblySpec = true;
        }
    }

    private static void AppendNestedTypeDef(ref TypeNameBuilder tnb, EcmaMetadataReader reader, uint typeDefToken, TypeNameFormat format)
    {
        EcmaMetadataCursor cursor = reader.GetCursor(typeDefToken);
        System.Reflection.TypeAttributes typeDefAttributes = (System.Reflection.TypeAttributes)reader.GetColumnAsConstant(cursor, MetadataColumnIndex.TypeDef_Flags);
        if ((int)(typeDefAttributes & System.Reflection.TypeAttributes.VisibilityMask) >= (int)System.Reflection.TypeAttributes.NestedPublic)
        {
            uint currentTypeDefToken = typeDefToken;
            List<uint> nestedTokens = new();
            EcmaMetadataCursor nestedTypesCursor = reader.GetCursor(EcmaMetadataReader.CreateToken(MetadataTable.NestedClass, 1));
            while (reader.TryFindRowFromCursor(nestedTypesCursor, MetadataColumnIndex.NestedClass_NestedClass, currentTypeDefToken, out EcmaMetadataCursor foundNestedClassRecord))
            {
                currentTypeDefToken = reader.GetColumnAsToken(foundNestedClassRecord, MetadataColumnIndex.NestedClass_EnclosingClass);
                nestedTokens.Add(currentTypeDefToken);
            }

            for (int i = nestedTokens.Count - 1; i >= 0; i--)
            {
                AppendTypeDef(ref tnb, reader, nestedTokens[i], format);
            }
        }
        AppendTypeDef(ref tnb, reader, typeDefToken, format);
    }

    private static void AppendTypeDef(ref TypeNameBuilder tnb, EcmaMetadataReader reader, uint typeDefToken, TypeNameFormat format)
    {
        EcmaMetadataCursor cursor = reader.GetCursor(typeDefToken);
        string? typeNamespace = null;
        if (format.HasFlag(TypeNameFormat.FormatNamespace))
        {
            typeNamespace = reader.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeNamespace);
        }
        tnb.AddName(reader.GetColumnAsUtf8String(cursor, MetadataColumnIndex.TypeDef_TypeName), typeNamespace);
    }

    private static void AppendParamTypeQualifier(ref TypeNameBuilder tnb, CorElementType kind, uint rank)
    {
        switch (kind)
        {
            case CorElementType.Byref:
                tnb.AddByRef();
                break;
            case CorElementType.Ptr:
                tnb.AddPointer();
                break;
            case CorElementType.SzArray:
                tnb.AddSzArray();
                break;
            case CorElementType.Array:
                tnb.AddArray(rank);
                break;
        }
    }

    private void AddByRef()
    {
        if (!CheckParseState(ParseState.Name | ParseState.GenArgs | ParseState.PtrArr))
        {
            Fail();
            return;
        }

        State = ParseState.ByRef;
        TypeString.Append('&');
    }

    private void AddPointer()
    {
        if (!CheckParseState(ParseState.Name | ParseState.GenArgs | ParseState.PtrArr))
        {
            Fail();
            return;
        }

        State = ParseState.PtrArr;
        TypeString.Append('*');
    }

    private void AddSzArray()
    {
        if (!CheckParseState(ParseState.Name | ParseState.GenArgs | ParseState.PtrArr))
        {
            Fail();
            return;
        }

        State = ParseState.PtrArr;
        TypeString.Append("[]");
    }

    private void AddArray(uint rank)
    {
        if (!CheckParseState(ParseState.Name | ParseState.GenArgs | ParseState.PtrArr))
        {
            Fail();
            return;
        }

        State = ParseState.PtrArr;
        if (rank == 0)
            return;

        if (rank == 1)
        {
            TypeString.Append("[*]");
        }
        else if (rank > 64)
        {
            TypeString.Append($"[{rank}]");
        }
        else
        {
            TypeString.Append('[');
            for (uint i = 1; i < rank; i++)
            {
                TypeString.Append(',');
            }
            TypeString.Append(']');
        }
        TypeString.Append("[]");
    }

    private static ReadOnlySpan<char> TypeNameReservedChars()
    {
        return ",[]&*+\\";
    }

    private static bool IsTypeNameReservedChar(char c)
    {
        return TypeNameReservedChars().IndexOf(c) != -1;
    }

    private void EscapeName(string name)
    {
        foreach (char c in name)
        {
            if (IsTypeNameReservedChar(c))
            {
                TypeString.Append('\\');
            }
            TypeString.Append(c);
        }
    }

    private void AddName(string name, string? _namespace = null)
    {
        if (name == null)
        {
            Fail();
            return;
        }

        if (!CheckParseState(ParseState.Start | ParseState.Name))
        {
            Fail();
            return;
        }

        State = ParseState.Name;
        if (NestedName)
            TypeString.Append('+');

        NestedName = true;
        if (!string.IsNullOrEmpty(_namespace))
        {
            EscapeName(_namespace);
            TypeString.Append('.');
        }

        EscapeName(name);
    }

    private void EscapeAssemblyName(string assemblyName)
    {
        TypeString.Append(assemblyName);
    }

    private void EscapeEmbeddedAssemblyName(string assemblyName)
    {
        foreach (char c in assemblyName)
        {
            if (c == ']')
                TypeString.Append('\\');
            TypeString.Append(c);
        }
    }

    private void AddNameNoEscaping(StringBuilder? name)
    {
        if (name == null)
        {
            Fail();
            return;
        }

        if (!CheckParseState(ParseState.Start | ParseState.Name))
        {
            Fail();
            return;
        }

        State = ParseState.Name;

        if (NestedName)
            TypeString.Append('+');

        NestedName = true;
        TypeString.Append(name);
    }

    private void Fail()
    {
        State = ParseState.Error;
    }

    private bool CheckParseState(ParseState validStates)
    {
        // Error is always invalid
        if (State == ParseState.Error)
            return false;

        return (State & validStates) != 0;
    }
}
