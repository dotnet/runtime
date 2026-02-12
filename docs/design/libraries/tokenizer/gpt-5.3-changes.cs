// GPT-5.3 Support for TiktokenTokenizer
// ======================================
//
// This file documents the changes needed to add GPT-5.3 support to the TiktokenTokenizer
// in the Microsoft.ML.Tokenizers library.
//
// Target File: src/Microsoft.ML.Tokenizers/Model/TiktokenTokenizer.cs  
// Target Repository: dotnet/machinelearning
//
// CONFIRMED: GPT-5.3 uses the o200k_base vocabulary (same as GPT-5.2, GPT-5.1, GPT-4o)
// Source: https://www.npmjs.com/package/gpt-tokenizer
//         https://github.com/niieani/gpt-tokenizer
//
// ======================================
// CHANGE 1: Update _modelPrefixToEncoding array
// ======================================
// Location: Line ~487 in TiktokenTokenizer.cs
// Add the "gpt-5.3-" prefix mapping immediately after the "gpt-5.2-" entry
//
// BEFORE:
//     ( "gpt-5.2-", ModelEncoding.O200kBase ),
//     ( "gpt-5.1-", ModelEncoding.O200kBase ),
//
// AFTER:
//     ( "gpt-5.3-", ModelEncoding.O200kBase ),  // e.g., gpt-5.3-mini, gpt-5.3-codex  
//     ( "gpt-5.2-", ModelEncoding.O200kBase ),
//     ( "gpt-5.1-", ModelEncoding.O200kBase ),
//
// Complete context for the change:
private static readonly (string Prefix, ModelEncoding Encoding)[] _modelPrefixToEncoding =
    [
        ( "o1-", ModelEncoding.O200kBase ),       // e.g. o1-mini
        ( "o3-", ModelEncoding.O200kBase ),       // e.g. o3-mini
        ( "o4-mini-", ModelEncoding.O200kBase ),  // e.g. o4-mini

        // chat
        ( "gpt-5.3-", ModelEncoding.O200kBase ),  // ← ADD THIS LINE
        ( "gpt-5.2-", ModelEncoding.O200kBase ),
        ( "gpt-5.1-", ModelEncoding.O200kBase ),
        ( "gpt-5-", ModelEncoding.O200kBase ),
        ( "gpt-4.1-", ModelEncoding.O200kBase ),
        ( "gpt-4.5-", ModelEncoding.O200kBase ),
        ( "gpt-4o-", ModelEncoding.O200kBase ),
        ( "chatgpt-4o-", ModelEncoding.O200kBase ),
        ( "gpt-4-", ModelEncoding.Cl100kBase ),
        ( "gpt-3.5-", ModelEncoding.Cl100kBase ),
        // ... rest of entries
    ];

// ======================================
// CHANGE 2: Update _modelToEncoding dictionary
// ======================================  
// Location: Line ~527 in TiktokenTokenizer.cs
// Add the "gpt-5.3" model name mapping immediately after the "gpt-5.2" entry
//
// BEFORE:
//     { "gpt-5.2", ModelEncoding.O200kBase },
//     { "gpt-5.1", ModelEncoding.O200kBase },
//
// AFTER:
//     { "gpt-5.3", ModelEncoding.O200kBase },
//     { "gpt-5.2", ModelEncoding.O200kBase },
//     { "gpt-5.1", ModelEncoding.O200kBase },
//
// Complete context for the change:
private static readonly Dictionary<string, ModelEncoding> _modelToEncoding =
    new Dictionary<string, ModelEncoding>(StringComparer.OrdinalIgnoreCase)
    {
        // reasoning
        { "o1", ModelEncoding.O200kBase },
        { "o3", ModelEncoding.O200kBase },
        { "o4-mini", ModelEncoding.O200kBase },

        // chat
        { "gpt-5.3", ModelEncoding.O200kBase },  // ← ADD THIS LINE
        { "gpt-5.2", ModelEncoding.O200kBase },
        { "gpt-5.1", ModelEncoding.O200kBase },
        { "gpt-5", ModelEncoding.O200kBase },
        { "gpt-4.1", ModelEncoding.O200kBase },
        { "gpt-4o", ModelEncoding.O200kBase },
        { "gpt-4", ModelEncoding.Cl100kBase },
        { "gpt-3.5-turbo", ModelEncoding.Cl100kBase },
        // ... rest of dictionary
    };

// ======================================
// USAGE EXAMPLES
// ======================================
// After these changes, GPT-5.3 models can be used:
//
// Example 1 - Base model:
//     var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");
//     var ids = tokenizer.EncodeToIds("Hello, GPT-5.3!");
//
// Example 2 - Model variants:
//     var tokenizerMini = TiktokenTokenizer.CreateForModel("gpt-5.3-mini");  
//     var tokenizerCodex = TiktokenTokenizer.CreateForModel("gpt-5.3-codex");
//
// Example 3 - Fine-tuned models (prefix match):
//     var tokenizerFT = TiktokenTokenizer.CreateForModel("gpt-5.3-custom-20260101");
//
// ======================================
// TESTING
// ======================================
// The following tests should be added to verify the changes:
//
// 1. Test that TiktokenTokenizer.CreateForModel("gpt-5.3") succeeds
// 2. Test that TiktokenTokenizer.CreateForModel("gpt-5.3-mini") succeeds  
// 3. Test that the tokenizer uses O200kBase encoding
// 4. Test that tokenization matches GPT-5.2 behavior (same vocabulary)
// 5. Test that GetModelEncoding("gpt-5.3") returns ModelEncoding.O200kBase
//
// ======================================
// COMPATIBILITY NOTES
// ======================================
// - GPT-5.3 uses the SAME vocabulary as GPT-5.2, so existing o200k_base data files work
// - No new vocabulary files or data packages are needed
// - The change is backward compatible - existing code continues to work
// - Forward compatible with future GPT-5.3 variants (e.g., gpt-5.3-turbo)
