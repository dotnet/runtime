using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.CodeGenerator.UnitTests
{
    public static partial class GeneratorTests
    {
        [Fact]
        public static void CanUse()
        {
            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();
            generator.Initialize(new InitializationContext());
            generator.Execute(new SourceGeneratorContext());
        }
    }
}
