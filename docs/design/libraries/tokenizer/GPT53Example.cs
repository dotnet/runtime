// This is a sample demonstrating how to use GPT-5.3 with Microsoft.ML.Tokenizers
// once the changes documented in this PR are applied to the dotnet/machinelearning repository.
//
// To run this example:
// 1. Ensure Microsoft.ML.Tokenizers 2.1.0+ is installed (after GPT-5.3 support is added)
// 2. Ensure Microsoft.ML.Tokenizers.Data.O200kBase is installed
// 3. Compile and run this program
//
// NOTE: This code will NOT work until the changes are applied to dotnet/machinelearning

using System;
using Microsoft.ML.Tokenizers;

namespace GPT53TokenizerExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("GPT-5.3 Tokenizer Example");
            Console.WriteLine("=".PadRight(50, '='));

            // Example 1: Base GPT-5.3 model
            DemonstrateTokenization("gpt-5.3", "Hello, GPT-5.3! This is a test.");

            // Example 2: GPT-5.3-mini variant
            DemonstrateTokenization("gpt-5.3-mini", "Tokenizing with GPT-5.3-mini model.");

            // Example 3: Compare with GPT-5.2 (should produce identical results)
            Console.WriteLine("\nComparing GPT-5.3 with GPT-5.2 (should be identical):");
            CompareTokenization("gpt-5.3", "gpt-5.2", "The quick brown fox jumps over the lazy dog.");

            Console.WriteLine("\nAll examples completed successfully!");
        }

        private static void DemonstrateTokenization(string modelName, string text)
        {
            Console.WriteLine($"\n--- Tokenizing with {modelName} ---");

            try
            {
                // Create tokenizer for the specified model
                var tokenizer = TiktokenTokenizer.CreateForModel(modelName);

                // Encode text to token IDs
                var tokenIds = tokenizer.EncodeToIds(text);
                
                Console.WriteLine($"Input text: {text}");
                Console.WriteLine($"Token count: {tokenIds.Count}");
                Console.WriteLine($"Token IDs: [{string.Join(", ", tokenIds)}]");

                // Decode back to text
                var decoded = tokenizer.Decode(tokenIds);
                Console.WriteLine($"Decoded text: {decoded}");
                Console.WriteLine($"Round-trip successful: {text == decoded}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine("NOTE: GPT-5.3 support has not been added to Microsoft.ML.Tokenizers yet.");
            }
        }

        private static void CompareTokenization(string model1, string model2, string text)
        {
            try
            {
                var tokenizer1 = TiktokenTokenizer.CreateForModel(model1);
                var tokenizer2 = TiktokenTokenizer.CreateForModel(model2);

                var tokens1 = tokenizer1.EncodeToIds(text);
                var tokens2 = tokenizer2.EncodeToIds(text);

                bool identical = tokens1.Count == tokens2.Count;
                if (identical)
                {
                    for (int i = 0; i < tokens1.Count; i++)
                    {
                        if (tokens1[i] != tokens2[i])
                        {
                            identical = false;
                            break;
                        }
                    }
                }

                Console.WriteLine($"  {model1}: {tokens1.Count} tokens");
                Console.WriteLine($"  {model2}: {tokens2.Count} tokens");
                Console.WriteLine($"  Tokenization is identical: {identical} ✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Comparison failed: {ex.Message}");
            }
        }
    }
}
