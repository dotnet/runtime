// Unit test template for GPT-5.3 support in Microsoft.ML.Tokenizers
// These tests should be added to the test suite in dotnet/machinelearning
//
// Target location: test/Microsoft.ML.Tokenizers.Tests/TiktokenTests.cs
// or similar test file in the tokenizers test project

using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.ML.Tokenizers.Tests
{
    public class GPT53TokenizerTests
    {
        [Fact]
        public void CreateForModel_GPT53_Succeeds()
        {
            // Arrange & Act
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");

            // Assert
            Assert.NotNull(tokenizer);
        }

        [Theory]
        [InlineData("gpt-5.3")]
        [InlineData("gpt-5.3-mini")]
        [InlineData("gpt-5.3-codex")]
        [InlineData("gpt-5.3-turbo")]
        public void CreateForModel_GPT53Variants_Succeeds(string modelName)
        {
            // Arrange & Act
            var tokenizer = TiktokenTokenizer.CreateForModel(modelName);

            // Assert
            Assert.NotNull(tokenizer);
        }

        [Fact]
        public void GPT53_UsesO200kBaseEncoding()
        {
            // Arrange
            var gpt53Tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");
            var gpt4oTokenizer = TiktokenTokenizer.CreateForModel("gpt-4o"); // Known o200k_base model

            const string testText = "Hello, world! This is a test.";

            // Act
            var gpt53Tokens = gpt53Tokenizer.EncodeToIds(testText);
            var gpt4oTokens = gpt4oTokenizer.EncodeToIds(testText);

            // Assert - Should produce identical tokens since they use the same vocabulary
            Assert.Equal(gpt4oTokens.Count, gpt53Tokens.Count);
            for (int i = 0; i < gpt53Tokens.Count; i++)
            {
                Assert.Equal(gpt4oTokens[i], gpt53Tokens[i]);
            }
        }

        [Fact]
        public void GPT53_ProducesSameTokensAsGPT52()
        {
            // Arrange
            var gpt53Tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");
            var gpt52Tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.2");

            const string testText = "The quick brown fox jumps over the lazy dog.";

            // Act
            var gpt53Tokens = gpt53Tokenizer.EncodeToIds(testText);
            var gpt52Tokens = gpt52Tokenizer.EncodeToIds(testText);

            // Assert - GPT-5.3 and GPT-5.2 use the same vocabulary
            Assert.Equal(gpt52Tokens.Count, gpt53Tokens.Count);
            for (int i = 0; i < gpt53Tokens.Count; i++)
            {
                Assert.Equal(gpt52Tokens[i], gpt53Tokens[i]);
            }
        }

        [Fact]
        public void GPT53_EncodeAndDecode_RoundTrip()
        {
            // Arrange
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");
            const string originalText = "GPT-5.3 tokenizer test with special characters: 你好, مرحبا, 🎉";

            // Act
            var tokens = tokenizer.EncodeToIds(originalText);
            var decodedText = tokenizer.Decode(tokens);

            // Assert
            Assert.Equal(originalText, decodedText);
        }

        [Fact]
        public void GPT53_CountTokens_ReturnsCorrectCount()
        {
            // Arrange
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");
            const string testText = "Count these tokens.";

            // Act
            var tokenIds = tokenizer.EncodeToIds(testText);
            var tokenCount = tokenizer.CountTokens(testText);

            // Assert
            Assert.Equal(tokenIds.Count, tokenCount);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("Hello", 1)]
        [InlineData("Hello, world!", 4)]
        [InlineData("The quick brown fox", 4)]
        public void GPT53_CountTokens_VariousInputs(string input, int expectedMinTokens)
        {
            // Arrange
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");

            // Act
            var tokenCount = tokenizer.CountTokens(input);

            // Assert
            Assert.True(tokenCount >= expectedMinTokens, 
                $"Expected at least {expectedMinTokens} tokens, but got {tokenCount}");
        }

        [Fact]
        public void GPT53Mini_WorksWithVariantModel()
        {
            // Arrange
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3-mini");
            const string testText = "Testing GPT-5.3-mini variant.";

            // Act
            var tokens = tokenizer.EncodeToIds(testText);
            var decoded = tokenizer.Decode(tokens);

            // Assert
            Assert.NotEmpty(tokens);
            Assert.Equal(testText, decoded);
        }

        [Fact]
        public void GPT53_SpecialTokens_EndOfText()
        {
            // Arrange
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");

            // Act
            var specialTokens = tokenizer.SpecialTokens;

            // Assert
            Assert.NotNull(specialTokens);
            Assert.Contains("<|endoftext|>", specialTokens.Keys);
            Assert.Equal(199999, specialTokens["<|endoftext|>"]); // o200k_base end of text token
        }

        [Fact]
        public void GPT53_MaxTokenLength_HandlesLongText()
        {
            // Arrange
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");
            var longText = string.Join(" ", Enumerable.Repeat("word", 10000));

            // Act
            var tokens = tokenizer.EncodeToIds(longText);

            // Assert
            Assert.NotEmpty(tokens);
            Assert.True(tokens.Count > 1000, "Long text should produce many tokens");
        }
    }
}
