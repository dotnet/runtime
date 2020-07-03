using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.CodeGenerator.UnitTests
{
    // TODO(@kevinwkt): Temporary unit tests for Source Generator.
    public static class GeneratorTests
    {
        [Fact]
        public static void SourceGeneratorInitializationPass()
        {
            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();
            generator.Initialize(new InitializationContext());
        }

        [Fact]
        public static void SourceGeneratorInitializationFail()
        {
            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();
            generator.Initialize(new InitializationContext());
        }

        [Fact]
        public static void SourceGeneratorExecutionPass()
        {
            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();
            generator.Initialize(new InitializationContext());
            generator.Execute(new SourceGeneratorContext());
        }

        [Fact]
        public static void SourceGeneratorExecutionFail()
        {
            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();
            generator.Initialize(new InitializationContext());
            generator.Execute(new SourceGeneratorContext());
        }
    }
}
