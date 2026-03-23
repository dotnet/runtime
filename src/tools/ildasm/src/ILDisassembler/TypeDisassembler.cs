// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace ILDisassembler;

/// <summary>
/// Disassembles a single type definition.
/// </summary>
internal sealed class TypeDisassembler
{
    private readonly MetadataReader _reader;
    private readonly PEReader _peReader;
    private readonly TypeDefinition _typeDef;

    public TypeDisassembler(MetadataReader reader, PEReader peReader, TypeDefinition typeDef)
    {
        _reader = reader;
        _peReader = peReader;
        _typeDef = typeDef;
    }

    public void Write(ILWriter writer)
    {
        var name = _reader.GetString(_typeDef.Name);
        var ns = _reader.GetString(_typeDef.Namespace);

        // Skip <Module> type (global functions container)
        // TODO: Handle global functions in <Module> type
        if (string.IsNullOrEmpty(name) || name == "<Module>")
        {
            return;
        }

        writer.WriteLine();

        // Build full type name
        // TODO: Handle nested types properly (currently flattened)
        string fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

        // Determine type kind and attributes
        var attributes = _typeDef.Attributes;
        string typeKeyword = GetTypeKeyword(attributes);
        string visibility = GetVisibility(attributes);
        string modifiers = GetModifiers(attributes);

        // TODO: Output generic type parameters and constraints
        // Write class header
        writer.WriteLine($".class {visibility}{modifiers}{typeKeyword} {fullName}");

        // Write extends clause
        if (!_typeDef.BaseType.IsNil)
        {
            string baseTypeName = GetTypeName(_typeDef.BaseType);
            writer.WriteLine($"       extends {baseTypeName}");
        }

        // TODO: Write implements clause for interfaces
        // TODO: Output .pack and .size for explicit layout
        // TODO: Output security declarations (.permissionset)
        // TODO: Output custom attributes on type

        writer.WriteLine("{");
        writer.Indent();

        // Write fields
        foreach (var fieldHandle in _typeDef.GetFields())
        {
            WriteField(writer, fieldHandle);
        }

        // TODO: Output .property declarations with .get/.set accessors
        // TODO: Output .event declarations with .addon/.removeon/.fire accessors

        // Write methods
        foreach (var methodHandle in _typeDef.GetMethods())
        {
            WriteMethod(writer, methodHandle);
        }

        writer.Dedent();
        writer.WriteLine($"}} // end of class {fullName}");
    }

    private void WriteField(ILWriter writer, FieldDefinitionHandle handle)
    {
        var field = _reader.GetFieldDefinition(handle);
        var name = _reader.GetString(field.Name);
        var attributes = field.Attributes;

        string visibility = GetFieldVisibility(attributes);
        string modifiers = GetFieldModifiers(attributes);

        // TODO: Decode field type from signature blob using SignatureDecoder
        // TODO: Handle generic field types
        // TODO: Output field initial values (.data)
        // TODO: Output field marshaling info
        // TODO: Output field RVA for mapped fields
        // TODO: Output custom attributes on field
        writer.WriteLine($".field {visibility}{modifiers}{name}");
    }

    private void WriteMethod(ILWriter writer, MethodDefinitionHandle handle)
    {
        var method = _reader.GetMethodDefinition(handle);
        var name = _reader.GetString(method.Name);
        var attributes = method.Attributes;

        string visibility = GetMethodVisibility(attributes);
        string modifiers = GetMethodModifiers(attributes);

        // TODO: Decode return type and parameter types from signature
        // TODO: Output calling conventions (instance, vararg, etc.)
        // TODO: Output generic method parameters
        // TODO: Output parameter names and attributes (in, out, optional)
        // TODO: Output default parameter values
        // TODO: Output P/Invoke info (.pinvokeimpl)
        // TODO: Output custom attributes on method and parameters
        writer.WriteLine();
        writer.WriteLine($".method {visibility}{modifiers}{name}() cil managed");
        writer.WriteLine("{");
        writer.Indent();

        // Write IL body if present
        if (method.RelativeVirtualAddress != 0)
        {
            WriteMethodBody(writer, method);
        }

        writer.Dedent();
        writer.WriteLine($"}} // end of method {name}");
    }

    private void WriteMethodBody(ILWriter writer, MethodDefinition method)
    {
        var body = _peReader.GetMethodBody(method.RelativeVirtualAddress);

        writer.WriteLine($"// Code size: {body.Size}");
        writer.WriteLine($".maxstack {body.MaxStack}");

        // TODO: Decode and output local variable declarations with types
        // TODO: Output exception handling regions (.try/.catch/.finally/.fault/.filter)
        // TODO: Support --bytes option to show IL bytes as hex comments
        // TODO: Support --source/--linenum options using PDB for source lines

        // Disassemble IL
        var ilReader = body.GetILReader();
        var ilDisasm = new InstructionDisassembler(_reader);

        while (ilReader.Offset < ilReader.Length)
        {
            int offset = ilReader.Offset;
            string instruction = ilDisasm.DisassembleInstruction(ref ilReader);
            writer.WriteLine($"IL_{offset:X4}: {instruction}");
        }
    }

    private static string GetTypeKeyword(TypeAttributes attributes)
    {
        if ((attributes & TypeAttributes.Interface) != 0)
        {
            return "interface ";
        }

        // Check for value type by looking at semantics
        // Note: Proper check requires looking at base type
        return "";
    }

    private static string GetVisibility(TypeAttributes attributes)
    {
        return (attributes & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.Public => "public ",
            TypeAttributes.NotPublic => "private ",
            TypeAttributes.NestedPublic => "nested public ",
            TypeAttributes.NestedPrivate => "nested private ",
            TypeAttributes.NestedFamily => "nested family ",
            TypeAttributes.NestedAssembly => "nested assembly ",
            TypeAttributes.NestedFamANDAssem => "nested famandassem ",
            TypeAttributes.NestedFamORAssem => "nested famorassem ",
            _ => ""
        };
    }

    private static string GetModifiers(TypeAttributes attributes)
    {
        string result = "";

        if ((attributes & TypeAttributes.Abstract) != 0)
        {
            result += "abstract ";
        }

        if ((attributes & TypeAttributes.Sealed) != 0)
        {
            result += "sealed ";
        }

        // Layout attributes
        result += (attributes & TypeAttributes.LayoutMask) switch
        {
            TypeAttributes.AutoLayout => "auto ",
            TypeAttributes.SequentialLayout => "sequential ",
            TypeAttributes.ExplicitLayout => "explicit ",
            _ => ""
        };

        // String format attributes
        result += (attributes & TypeAttributes.StringFormatMask) switch
        {
            TypeAttributes.AnsiClass => "ansi ",
            TypeAttributes.UnicodeClass => "unicode ",
            TypeAttributes.AutoClass => "autochar ",
            _ => ""
        };

        return result;
    }

    private static string GetFieldVisibility(FieldAttributes attributes)
    {
        return (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public => "public ",
            FieldAttributes.Private => "private ",
            FieldAttributes.Family => "family ",
            FieldAttributes.Assembly => "assembly ",
            FieldAttributes.FamANDAssem => "famandassem ",
            FieldAttributes.FamORAssem => "famorassem ",
            _ => ""
        };
    }

    private static string GetFieldModifiers(FieldAttributes attributes)
    {
        string result = "";

        if ((attributes & FieldAttributes.Static) != 0)
        {
            result += "static ";
        }

        if ((attributes & FieldAttributes.InitOnly) != 0)
        {
            result += "initonly ";
        }

        if ((attributes & FieldAttributes.Literal) != 0)
        {
            result += "literal ";
        }

        return result;
    }

    private static string GetMethodVisibility(MethodAttributes attributes)
    {
        return (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => "public ",
            MethodAttributes.Private => "private ",
            MethodAttributes.Family => "family ",
            MethodAttributes.Assembly => "assembly ",
            MethodAttributes.FamANDAssem => "famandassem ",
            MethodAttributes.FamORAssem => "famorassem ",
            _ => ""
        };
    }

    private static string GetMethodModifiers(MethodAttributes attributes)
    {
        string result = "";

        if ((attributes & MethodAttributes.HideBySig) != 0)
        {
            result += "hidebysig ";
        }

        if ((attributes & MethodAttributes.Static) != 0)
        {
            result += "static ";
        }

        if ((attributes & MethodAttributes.Virtual) != 0)
        {
            result += "virtual ";
        }

        if ((attributes & MethodAttributes.Final) != 0)
        {
            result += "final ";
        }

        if ((attributes & MethodAttributes.NewSlot) != 0)
        {
            result += "newslot ";
        }

        if ((attributes & MethodAttributes.Abstract) != 0)
        {
            result += "abstract ";
        }

        return result;
    }

    private string GetTypeName(EntityHandle handle)
    {
        if (handle.Kind == HandleKind.TypeReference)
        {
            var typeRef = _reader.GetTypeReference((TypeReferenceHandle)handle);
            var name = _reader.GetString(typeRef.Name);
            var ns = _reader.GetString(typeRef.Namespace);

            // Get assembly ref for scope
            string scope = "";
            if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                var asmRef = _reader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);
                scope = $"[{_reader.GetString(asmRef.Name)}]";
            }

            return string.IsNullOrEmpty(ns) ? $"{scope}{name}" : $"{scope}{ns}.{name}";
        }

        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var typeDef = _reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            var name = _reader.GetString(typeDef.Name);
            var ns = _reader.GetString(typeDef.Namespace);

            return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
        }

        return handle.ToString()!;
    }
}
