using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BinaryFormat
{
    public partial class Generator : ISourceGenerator
    {
        private void GenerateReaderWriter(
            GeneratorExecutionContext context,
            TypeDeclarationSyntax typeDecl,
            SemanticModel semanticModel)
        {
            if (!typeDecl.Modifiers.Any(tok => tok.IsKind(SyntaxKind.PartialKeyword)))
            {
                // Type must be partial
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        "TypeMustBePartial",
                        category: "BinaryFormat",
                        $"Type {typeDecl.Identifier.ValueText} must be partial",
                        severity: DiagnosticSeverity.Error,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        warningLevel: 0,
                        location: typeDecl.Identifier.GetLocation()));
                return;
            }

            var containerSymbol = semanticModel.GetDeclaredSymbol(typeDecl)!;
            ITypeSymbol receiverType = containerSymbol;

            bool hasBigEndianAttribute = containerSymbol.GetAttributes().Any(a => a.AttributeClass.Name == "BigEndianAttribute");
            bool hasLittleEndianAttribute = containerSymbol.GetAttributes().Any(a => a.AttributeClass.Name == "LittleEndianAttribute");

            var fieldsAndProps = receiverType.GetMembers()
                .Where(m => m is {
                    DeclaredAccessibility: Accessibility.Public,
                    Kind: SymbolKind.Field or SymbolKind.Property,
                })
                .Select(m => new DataMemberSymbol(m)).ToList();

            var stringBuilder = new StringBuilder();
            string classOrStruct = typeDecl is ClassDeclarationSyntax ? "class" : "struct";

            // FIXME: modifiers, class/struct/record
            stringBuilder.AppendLine($"using System.Buffers.Binary;");
            stringBuilder.AppendLine($"");
            stringBuilder.AppendLine($"namespace {containerSymbol.ContainingNamespace}");
            stringBuilder.AppendLine($"{{");
            stringBuilder.AppendLine($"    public partial {classOrStruct} {typeDecl.Identifier}");
            stringBuilder.AppendLine($"    {{");

            // Try to generate the static size constant
            GenerateBinarySize(context, typeDecl, semanticModel, stringBuilder, fieldsAndProps);

            if (hasLittleEndianAttribute && !hasBigEndianAttribute)
            {
                GenerateReadMethod(context, typeDecl, semanticModel, stringBuilder, "", "LittleEndian", fieldsAndProps);
                stringBuilder.AppendLine();
                GenerateWriteMethod(context, typeDecl, semanticModel, stringBuilder, "", "LittleEndian", fieldsAndProps);
            }
            else if (hasBigEndianAttribute && !hasLittleEndianAttribute)
            {
                GenerateReadMethod(context, typeDecl, semanticModel, stringBuilder, "", "BigEndian", fieldsAndProps);
                stringBuilder.AppendLine();
                GenerateWriteMethod(context, typeDecl, semanticModel, stringBuilder, "", "BigEndian", fieldsAndProps);
            }
            else
            {
                GenerateReadMethod(context, typeDecl, semanticModel, stringBuilder, "LittleEndian", "LittleEndian", fieldsAndProps);
                stringBuilder.AppendLine();
                GenerateReadMethod(context, typeDecl, semanticModel, stringBuilder, "BigEndian", "BigEndian", fieldsAndProps);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"        public static {typeDecl.Identifier} Read(ReadOnlySpan<byte> buffer, bool isLittleEndian, out int bytesRead)");
                stringBuilder.AppendLine($"        {{");
                stringBuilder.AppendLine($"            return isLittleEndian ? ReadLittleEndian(buffer, out bytesRead) : ReadBigEndian(buffer, out bytesRead);");
                stringBuilder.AppendLine($"        }}");
                stringBuilder.AppendLine();
                GenerateWriteMethod(context, typeDecl, semanticModel, stringBuilder, "LittleEndian", "LittleEndian", fieldsAndProps);
                stringBuilder.AppendLine();
                GenerateWriteMethod(context, typeDecl, semanticModel, stringBuilder, "BigEndian", "BigEndian", fieldsAndProps);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"        public void Write(Span<byte> buffer, bool isLittleEndian, out int bytesWritten)");
                stringBuilder.AppendLine($"        {{");
                stringBuilder.AppendLine($"            if (isLittleEndian)");
                stringBuilder.AppendLine($"            {{");
                stringBuilder.AppendLine($"                WriteLittleEndian(buffer, out bytesWritten);");
                stringBuilder.AppendLine($"            }}");
                stringBuilder.AppendLine($"            else");
                stringBuilder.AppendLine($"            {{");
                stringBuilder.AppendLine($"                WriteBigEndian(buffer, out bytesWritten);");
                stringBuilder.AppendLine($"            }}");
                stringBuilder.AppendLine($"        }}");
            }

            stringBuilder.AppendLine($"    }}");
            stringBuilder.AppendLine($"}}");

            context.AddSource($"{containerSymbol.Name}.Generated.cs", stringBuilder.ToString());
        }

        private void GenerateBinarySize(
            GeneratorExecutionContext context,
            TypeDeclarationSyntax typeDecl,
            SemanticModel semanticModel,
            StringBuilder stringBuilder,
            List<DataMemberSymbol> fieldsAndProps)
        {
            int size = 0;
            //StringBuilder variableOffset = new StringBuilder();
            //int variableOffsetIndex = 1;

            foreach (var m in fieldsAndProps)
            {
                var memberType = m.Type;

                if (memberType.TypeKind == TypeKind.Enum &&
                    memberType is INamedTypeSymbol nts)
                {
                    memberType = nts.EnumUnderlyingType;
                }

                switch (memberType.SpecialType)
                {
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                        int basicTypeSize = memberType.SpecialType switch
                        {
                            SpecialType.System_UInt16 => 2,
                            SpecialType.System_UInt32 => 4,
                            SpecialType.System_UInt64 => 8,
                            SpecialType.System_Int16 => 2,
                            SpecialType.System_Int32 => 4,
                            SpecialType.System_Int64 => 8,
                            _ => 0
                        };
                        size += basicTypeSize;
                        break;

                    case SpecialType.System_Byte:
                        size++;
                        break;

                    default:
                        var binarySizeField = memberType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => f.Name == "BinarySize");
                        if (binarySizeField != null && binarySizeField.ConstantValue is int binarySize)
                        {
                            // Handle nested types that have `const int BinarySize`
                            size += binarySize;
                            break;
                        }
                        return;
                }
            }

            stringBuilder.AppendLine($"        public const int BinarySize = {size};");
            stringBuilder.AppendLine();
        }

        private void GenerateReadMethod(
            GeneratorExecutionContext context,
            TypeDeclarationSyntax typeDecl,
            SemanticModel semanticModel,
            StringBuilder stringBuilder,
            string nameSuffix,
            string endianSuffix,
            List<DataMemberSymbol> fieldsAndProps)
        {
            int offset = 0;
            StringBuilder variableOffset = new StringBuilder();
            int variableOffsetIndex = 1;

            stringBuilder.AppendLine($"        public static {typeDecl.Identifier} Read{nameSuffix}(ReadOnlySpan<byte> buffer, out int bytesRead)");
            stringBuilder.AppendLine($"        {{");
            stringBuilder.AppendLine($"            var result = new {typeDecl.Identifier}");
            stringBuilder.AppendLine($"            {{");

            foreach (var m in fieldsAndProps)
            {
                var memberType = m.Type;
                string? readExpression;
                string castExpression = "";

                if (memberType.TypeKind == TypeKind.Enum &&
                    memberType is INamedTypeSymbol nts)
                {
                    // FIXME: Namespace
                    castExpression = $"({memberType.Name})";
                    memberType = nts.EnumUnderlyingType;
                }

                switch (memberType.SpecialType)
                {
                    // Endianness aware basic types
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                        string? basicTypeName = memberType.SpecialType switch
                        {
                            SpecialType.System_UInt16 => "UInt16",
                            SpecialType.System_UInt32 => "UInt32",
                            SpecialType.System_UInt64 => "UInt64",
                            SpecialType.System_Int16 => "Int16",
                            SpecialType.System_Int32 => "Int32",
                            SpecialType.System_Int64 => "Int64",
                            _ => throw new InvalidOperationException()
                        };
                        int basicTypeSize = memberType.SpecialType switch
                        {
                            SpecialType.System_UInt16 => 2,
                            SpecialType.System_UInt32 => 4,
                            SpecialType.System_UInt64 => 8,
                            SpecialType.System_Int16 => 2,
                            SpecialType.System_Int32 => 4,
                            SpecialType.System_Int64 => 8,
                            _ => 0
                        };
                        readExpression = $"{castExpression}BinaryPrimitives.Read{basicTypeName}{endianSuffix}(buffer.Slice({offset}{variableOffset}, {basicTypeSize}))";
                        offset += basicTypeSize;
                        break;

                    case SpecialType.System_Byte:
                        readExpression = $"{castExpression}buffer[{offset}{variableOffset}]";
                        offset ++;
                        break;

                    default:
                        var methods = memberType.GetMembers().OfType<IMethodSymbol>();
                        if (methods.Any(m => m.Name == $"Read{nameSuffix}"))
                        {
                            // FIXME: Missing namespace
                            readExpression = $"{m.Type.Name}.Read{nameSuffix}(buffer.Slice({offset}{variableOffset}), out var _{variableOffsetIndex})";
                        }
                        else
                        {
                            // FIXME: Missing namespace
                            readExpression = $"{m.Type.Name}.Read(buffer.Slice({offset}{variableOffset}), out var _{variableOffsetIndex})";
                        }

                        variableOffset.Append($" + _{variableOffsetIndex}");
                        variableOffsetIndex++;

                        // FIXME: Handle other basic type
                        // FIXME: Handle nested struct/classes by calling their Read
                        //throw new NotSupportedException();
                        break;
                }

                stringBuilder.AppendLine($"                {m.Name} = {readExpression},");
            }

            stringBuilder.AppendLine($"            }};");
            stringBuilder.AppendLine($"            bytesRead = {offset}{variableOffset};");
            stringBuilder.AppendLine($"            return result;");
            stringBuilder.AppendLine($"        }}");
        }

        private void GenerateWriteMethod(
            GeneratorExecutionContext context,
            TypeDeclarationSyntax typeDecl,
            SemanticModel semanticModel,
            StringBuilder stringBuilder,
            string nameSuffix,
            string endianSuffix,
            List<DataMemberSymbol> fieldsAndProps)
        {
            int offset = 0;
            StringBuilder variableOffset = new StringBuilder();
            int variableOffsetIndex = 1;

            stringBuilder.AppendLine($"        public void Write{nameSuffix}(Span<byte> buffer, out int bytesWritten)");
            stringBuilder.AppendLine($"        {{");

            foreach (var m in fieldsAndProps)
            {
                var memberType = m.Type;
                string? writeExpression;
                string castExpression = "";

                if (memberType.TypeKind == TypeKind.Enum &&
                    memberType is INamedTypeSymbol nts)
                {
                    // FIXME: Namespace
                    memberType = nts.EnumUnderlyingType;
                    castExpression = $"({memberType.Name})";
                }

                switch (memberType.SpecialType)
                {
                    // Endianness aware basic types
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                        string? basicTypeName = memberType.SpecialType switch
                        {
                            SpecialType.System_UInt16 => "UInt16",
                            SpecialType.System_UInt32 => "UInt32",
                            SpecialType.System_UInt64 => "UInt64",
                            SpecialType.System_Int16 => "Int16",
                            SpecialType.System_Int32 => "Int32",
                            SpecialType.System_Int64 => "Int64",
                            _ => throw new InvalidOperationException()
                        };
                        int basicTypeSize = memberType.SpecialType switch
                        {
                            SpecialType.System_UInt16 => 2,
                            SpecialType.System_UInt32 => 4,
                            SpecialType.System_UInt64 => 8,
                            SpecialType.System_Int16 => 2,
                            SpecialType.System_Int32 => 4,
                            SpecialType.System_Int64 => 8,
                            _ => 0
                        };
                        writeExpression = $"BinaryPrimitives.Write{basicTypeName}{endianSuffix}(buffer.Slice({offset}{variableOffset}, {basicTypeSize}), {castExpression}{m.Name})";
                        offset += basicTypeSize;
                        break;

                    case SpecialType.System_Byte:
                        writeExpression = $"buffer[{offset}{variableOffset}] = {castExpression}{m.Name}";
                        offset ++;
                        break;

                    default:
                        var methods = memberType.GetMembers().OfType<IMethodSymbol>();
                        if (methods.Any(m => m.Name == $"Write{nameSuffix}"))
                        {
                            // FIXME: Missing namespace
                            writeExpression = $"{m.Name}.Write{nameSuffix}(buffer.Slice({offset}{variableOffset}), out var _{variableOffsetIndex})";
                        }
                        else
                        {
                            // FIXME: Missing namespace
                            writeExpression = $"{m.Name}.Write(buffer.Slice({offset}{variableOffset}), out var _{variableOffsetIndex})";
                        }

                        variableOffset.Append($" + _{variableOffsetIndex}");
                        variableOffsetIndex++;

                        // FIXME: Handle other basic type
                        // FIXME: Handle nested struct/classes by calling their Read
                        //throw new NotSupportedException();
                        break;
                }

                stringBuilder.AppendLine($"            {writeExpression};");
            }

            stringBuilder.AppendLine($"            bytesWritten = {offset}{variableOffset};");
            stringBuilder.AppendLine($"        }}");
        }
    }
}
