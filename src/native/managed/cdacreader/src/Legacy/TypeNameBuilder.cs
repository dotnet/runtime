﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[Flags]
internal enum TypeNameFormat
{
    FormatNamespace = 1,
    FormatFullInst = 2,
    FormatAngleBrackets = 4,
    FormatAssembly = 8,
    FormatGenericParam = 16,
    FormatSignature = 32,
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

    private TypeNameBuilder(StringBuilder typeString, Target target, TypeNameFormat format, bool initialStateIsName = false)
    {
        TypeString = typeString;
        Target = target;
        UseAngleBracketsForGenerics = format.HasFlag(TypeNameFormat.FormatAngleBrackets);
        if (initialStateIsName)
            State = ParseState.Name;
        else
            State = ParseState.Start;
    }

    public static void AppendMethodInternal(Target target, StringBuilder stringBuilder, Contracts.MethodDescHandle method, TypeNameFormat format)
    {
        AppendMethodImpl(target, stringBuilder, method, default, format);
    }

    public static void AppendMethodImpl(Target target, StringBuilder stringBuilder, Contracts.MethodDescHandle method, ReadOnlySpan<TypeHandle> typeInstantiation, TypeNameFormat format)
    {
        IRuntimeTypeSystem runtimeTypeSystem = target.Contracts.RuntimeTypeSystem;
        ILoader loader = target.Contracts.Loader;
        string methodName;
        TypeHandle th = default;
        Contracts.ModuleHandle module = default;

        bool isNoMetadataMethod = runtimeTypeSystem.IsNoMetadataMethod(method, out methodName);
        if (isNoMetadataMethod)
        {
            if (runtimeTypeSystem.IsDynamicMethod(method))
            {
                stringBuilder.Append("DynamicClass");
            }
            else if (runtimeTypeSystem.IsILStub(method))
            {
                stringBuilder.Append("ILStubClass");
            }
        }
        else
        {
            th = runtimeTypeSystem.GetTypeHandle(runtimeTypeSystem.GetMethodTable(method));
            AppendType(target, stringBuilder, th, typeInstantiation, format);
        }

        stringBuilder.Append('.');

        if (isNoMetadataMethod)
        {
            stringBuilder.Append(methodName);
        }
        else if (runtimeTypeSystem.IsArrayMethod(method, out ArrayFunctionType functionType))
        {
            string arrayName;

            switch (functionType)
            {
                case ArrayFunctionType.Set:
                    arrayName = "Set";
                    break;
                case ArrayFunctionType.Get:
                    arrayName = "Get";
                    break;
                case ArrayFunctionType.Address:
                    arrayName = "Address";
                    break;
                case ArrayFunctionType.Constructor:
                    arrayName = ".ctor";
                    break;
                default:
                    throw new ArgumentException(nameof(method));
            }

            stringBuilder.Append(arrayName);
        }
        else
        {
            module = loader.GetModuleHandle(runtimeTypeSystem.GetModule(th));
            MetadataReader reader = target.Contracts.EcmaMetadata.GetMetadata(module)!;
            MethodDefinition methodDef = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)runtimeTypeSystem.GetMethodToken(method)));
            stringBuilder.Append(reader.GetString(methodDef.Name));
        }

        ReadOnlySpan<TypeHandle> genericMethodInstantiation = runtimeTypeSystem.GetGenericMethodInstantiation(method);
        if (genericMethodInstantiation.Length > 0 && !runtimeTypeSystem.IsGenericMethodDefinition(method))
        {
            AppendInst(target, stringBuilder, genericMethodInstantiation, format);
        }

        if (format.HasFlag(TypeNameFormat.FormatSignature))
        {
            ReadOnlySpan<byte> signature;
            MetadataReader? reader = default;
            if (!runtimeTypeSystem.IsStoredSigMethodDesc(method, out signature))
            {
                reader = target.Contracts.EcmaMetadata.GetMetadata(module);
                if (reader is not null)
                {
                    MethodDefinition methodDef = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)runtimeTypeSystem.GetMethodToken(method)));
                    signature = reader.GetBlobBytes(methodDef.Signature);
                }
            }

            ReadOnlySpan<TypeHandle> typeInstantiationSigFormat = default;
            if (!th.IsNull)
            {
                typeInstantiationSigFormat = runtimeTypeSystem.GetInstantiation(th);
                if (typeInstantiationSigFormat.IsEmpty && runtimeTypeSystem.IsArray(th, out _))
                {
                    // For arrays, fill in the instantiation with the element type handle
                    // See MethodTable::GetArrayInstantiation for coreclr equivalent
                    typeInstantiationSigFormat = new[] { runtimeTypeSystem.GetTypeParam(th) };
                }
            }

            SigFormat.AppendSigFormat(target, stringBuilder, signature, reader, null, null, null, typeInstantiationSigFormat, runtimeTypeSystem.GetGenericMethodInstantiation(method), true);
        }
    }

    public static TypeHandle GetExactOwningType(IRuntimeTypeSystem runtimeTypeSystem, TypeHandle possiblyDerivedType, MethodDescHandle method)
    {
        TypeHandle approxOwner = runtimeTypeSystem.GetTypeHandle(runtimeTypeSystem.GetMethodTable(method));

        uint typeDefTokenOfOwner = runtimeTypeSystem.GetTypeDefToken(approxOwner);
        TargetPointer moduleOfOwner = runtimeTypeSystem.GetModule(approxOwner);

        do
        {
            uint typeDefTokenOfPossiblyDerivedType = runtimeTypeSystem.GetTypeDefToken(possiblyDerivedType);
            TargetPointer moduleOfPossiblyDerivedType = runtimeTypeSystem.GetModule(possiblyDerivedType);

            if ((typeDefTokenOfOwner == typeDefTokenOfPossiblyDerivedType) && (moduleOfOwner == moduleOfPossiblyDerivedType))
            {
                return possiblyDerivedType;
            }

            TargetPointer parentTypePointer = runtimeTypeSystem.GetParentMethodTable(possiblyDerivedType);
            if (parentTypePointer.Value == 0)
                throw new InvalidOperationException("Invalid parent type");

            // TODO(cdac) - Consider adding infinite loop detection here
            possiblyDerivedType = runtimeTypeSystem.GetTypeHandle(parentTypePointer);
        } while (true);
    }

    public static void AppendType(Target target, StringBuilder stringBuilder, Contracts.TypeHandle typeHandle, TypeNameFormat format)
    {
        AppendType(target, stringBuilder, typeHandle, default, format);
    }

    public static void AppendType(Target target, StringBuilder stringBuilder, Contracts.TypeHandle typeHandle, ReadOnlySpan<TypeHandle> typeInstantiation, TypeNameFormat format)
    {
        TypeNameBuilder builder = new(stringBuilder, target, format);
        AppendTypeCore(ref builder, typeHandle, typeInstantiation, format);
    }

    private static void AppendTypeCore(ref TypeNameBuilder tnb, Contracts.TypeHandle typeHandle, ReadOnlySpan<Contracts.TypeHandle> instantiation, TypeNameFormat format)
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
                    AppendTypeCore(ref tnb, typeSystemContract.GetTypeParam(typeHandle), default(ReadOnlySpan<Contracts.TypeHandle>), (TypeNameFormat)(format & ~TypeNameFormat.FormatAssembly));
                    AppendParamTypeQualifier(ref tnb, elemType, rank);
                }
                else
                {
                    tnb.TypeString.Append("VALUETYPE");
                    AppendTypeCore(ref tnb, typeSystemContract.GetTypeParam(typeHandle), Array.Empty<Contracts.TypeHandle>(), format & ~TypeNameFormat.FormatAssembly);
                }
            }
            else if (typeSystemContract.IsGenericVariable(typeHandle, out TargetPointer modulePointer, out uint genericParamToken))
            {
                Contracts.ModuleHandle module = tnb.Target.Contracts.Loader.GetModuleHandle(modulePointer);
                MetadataReader reader = tnb.Target.Contracts.EcmaMetadata.GetMetadata(module)!;
                var handle = (GenericParameterHandle)MetadataTokens.Handle((int)genericParamToken);
                GenericParameter genericParam = reader.GetGenericParameter(handle);
                if (format.HasFlag(TypeNameFormat.FormatGenericParam))
                {
                    if (genericParam.Parent.Kind == HandleKind.TypeDefinition)
                    {
                        tnb.TypeString.Append('!');
                    }
                    else
                    {
                        tnb.TypeString.Append("!!");
                    }
                }
                tnb.AddName(reader.GetString(genericParam.Name));
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
                uint typeDefToken = typeSystemContract.GetTypeDefToken(typeHandle);
                Contracts.ModuleHandle moduleHandle = tnb.Target.Contracts.Loader.GetModuleHandle(typeSystemContract.GetModule(typeHandle));
                if (MetadataTokens.EntityHandle((int)typeDefToken).IsNil)
                {
                    tnb.AddName("(dynamicClass)");
                }
                else
                {
                    MetadataReader reader = tnb.Target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;
                    AppendNestedTypeDef(ref tnb, reader, (TypeDefinitionHandle)MetadataTokens.EntityHandle((int)typeDefToken), format);
                }

                // Append instantiation

                if (format.HasFlag(TypeNameFormat.FormatNamespace) || format.HasFlag(TypeNameFormat.FormatAssembly))
                {
                    ReadOnlySpan<TypeHandle> instantiationSpan = typeSystemContract.GetInstantiation(typeHandle);

                    if ((instantiationSpan.Length > 0) && (!typeSystemContract.IsGenericTypeDefinition(typeHandle) || toString))
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

                MetadataReader mr = tnb.Target.Contracts.EcmaMetadata.GetMetadata(module)!;
                string assemblySimpleName = mr.GetString(mr.GetAssemblyDefinition().Name);

                tnb.AddAssemblySpec(assemblySimpleName);
            }
        }
    }

    // Append a square-bracket-enclosed, comma-separated list of n type parameters in inst to the string s
    // and enclose each parameter in square brackets to disambiguate the commas
    // The following flags in the FormatFlags argument are significant: FormatNamespace FormatFullInst FormatAssembly FormatNoVersion
    private static void AppendInst(Target target, StringBuilder stringBuilder, ReadOnlySpan<TypeHandle> inst, TypeNameFormat format)
    {
        TypeNameBuilder tnb = new (stringBuilder, target, format, initialStateIsName: true);
        AppendInst(ref tnb, inst, format);
    }

    private static void AppendInst(ref TypeNameBuilder tnb, ReadOnlySpan<TypeHandle> inst, TypeNameFormat format)
    {
        tnb.OpenGenericArguments();
        foreach (TypeHandle arg in inst)
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
        else
        {
            FirstInstArg = false;
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
            TypeString.Append(", ");

            if (InstNesting > 0)
                EscapeEmbeddedAssemblyName(assemblySpec);
            else
                EscapeAssemblyName(assemblySpec);

            HasAssemblySpec = true;
        }
    }

    private static void AppendNestedTypeDef(ref TypeNameBuilder tnb, MetadataReader reader, TypeDefinitionHandle typeDefToken, TypeNameFormat format)
    {
        TypeDefinition typeDef = reader.GetTypeDefinition(typeDefToken);
        System.Reflection.TypeAttributes typeDefAttributes = typeDef.Attributes;
        if ((int)(typeDefAttributes & System.Reflection.TypeAttributes.VisibilityMask) >= (int)System.Reflection.TypeAttributes.NestedPublic)
        {
            List<TypeDefinitionHandle> nestedTokens = [];
            for (TypeDefinitionHandle enclosingType = typeDef.GetDeclaringType(); !enclosingType.IsNil; enclosingType = reader.GetTypeDefinition(enclosingType).GetDeclaringType())
            {
                nestedTokens.Add(enclosingType);
            }

            for (int i = nestedTokens.Count - 1; i >= 0; i--)
            {
                AppendTypeDef(ref tnb, reader, reader.GetTypeDefinition(nestedTokens[i]), format);
            }
        }
        AppendTypeDef(ref tnb, reader, typeDef, format);
    }

    private static void AppendTypeDef(ref TypeNameBuilder tnb, MetadataReader reader, TypeDefinition typeDef, TypeNameFormat format)
    {
        string? typeNamespace = null;
        if (format.HasFlag(TypeNameFormat.FormatNamespace))
        {
            typeNamespace = reader.GetString(typeDef.Namespace);
        }
        tnb.AddName(reader.GetString(typeDef.Name), typeNamespace);
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
