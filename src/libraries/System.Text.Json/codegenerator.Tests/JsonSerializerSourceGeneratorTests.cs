using Xunit;

namespace System.Text.Json.CodeGenerator.Tests
{
    // TODO(@kevinwkt): Temporary end2end tests to use the generated code using codegen.
    public class JsonSerializerSouceGeneratorTests
    {
        // Temporary test to make sure code was generated.
        [Fact]
        public static void TestGeneratedCode()
        {
            Assert.Equal("Hello", HelloWorldGenerated.HelloWorld.SayHello());
        }
    }
}
