# Adding GPT-5.3 Support to Microsoft.ML.Tokenizers

## Summary
This document provides the complete specification for adding GPT-5.3 model support to the TiktokenTokenizer in the Microsoft.ML.Tokenizers library.

## Background

### Repository Context
The `Microsoft.ML.Tokenizers` library is maintained in the **dotnet/machinelearning** repository, not dotnet/runtime. The source file that requires changes is:
- **File**: `src/Microsoft.ML.Tokenizers/Model/TiktokenTokenizer.cs`
- **Repository**: https://github.com/dotnet/machinelearning

### Vocabulary Verification
Through web research, I confirmed that GPT-5.3 uses the **o200k_base** vocabulary encoding, which is the same vocabulary used by:
- GPT-5.2
- GPT-5.1  
- GPT-5
- GPT-4o
- GPT-4.1

**Sources**:
- npm package gpt-tokenizer: https://www.npmjs.com/package/gpt-tokenizer
- GitHub gpt-tokenizer: https://github.com/niieani/gpt-tokenizer
- OpenAI tiktoken issues: https://github.com/openai/tiktoken/issues/464

## Required Changes

The changes follow the exact same pattern used for adding GPT-5.2 support. Two arrays/dictionaries need to be updated in `TiktokenTokenizer.cs`:

### Change 1: Model Prefix Mapping
Add entry to the `_modelPrefixToEncoding` array (around line 487):

```csharp
( "gpt-5.3-", ModelEncoding.O200kBase ),
```

This enables support for model variants like `gpt-5.3-mini`, `gpt-5.3-codex`, etc.

### Change 2: Exact Model Name Mapping  
Add entry to the `_modelToEncoding` dictionary (around line 527):

```csharp
{ "gpt-5.3", ModelEncoding.O200kBase },
```

This enables support for the base `gpt-5.3` model name.

## Complete Code Reference

See `gpt-5.3-changes.cs` in this directory for the complete code changes with full context.

## Usage After Implementation

Once implemented, developers can use GPT-5.3 models with the tokenizer:

```csharp
using Microsoft.ML.Tokenizers;

// Create tokenizer for base gpt-5.3 model
var tokenizer = TiktokenTokenizer.CreateForModel("gpt-5.3");
var tokens = tokenizer.EncodeToIds("Hello, GPT-5.3!");
var decoded = tokenizer.Decode(tokens);

// Create tokenizer for model variants
var tokenizerMini = TiktokenTokenizer.CreateForModel("gpt-5.3-mini");
var tokenizerCodex = TiktokenTokenizer.CreateForModel("gpt-5.3-codex");
```

## Testing Requirements

The following tests should be added to verify correct implementation:

1. **Model Creation Tests**:
   - Verify `TiktokenTokenizer.CreateForModel("gpt-5.3")` succeeds
   - Verify `TiktokenTokenizer.CreateForModel("gpt-5.3-mini")` succeeds
   - Verify `TiktokenTokenizer.CreateForModel("gpt-5.3-codex")` succeeds

2. **Encoding Tests**:
   - Verify the tokenizer uses `ModelEncoding.O200kBase`
   - Verify `GetModelEncoding("gpt-5.3")` returns `ModelEncoding.O200kBase`

3. **Compatibility Tests**:
   - Verify tokenization of sample text produces the same results as GPT-5.2
   - Verify token IDs match between GPT-5.3 and GPT-5.2 for identical input

## Implementation Notes

- **No new vocabulary files needed**: GPT-5.3 uses the existing `o200k_base.tiktoken.deflate` file
- **No new data packages needed**: The existing `Microsoft.ML.Tokenizers.Data.O200kBase` package is sufficient  
- **Backward compatible**: Existing code using other models continues to work unchanged
- **Forward compatible**: The prefix match pattern will automatically support future GPT-5.3 variants

## Files in This Directory

- **README.md** (this file): Overview and implementation guide
- **gpt-5.3-changes.cs**: Complete code changes with context and examples

## Next Steps

To implement this change:

1. Clone the dotnet/machinelearning repository
2. Open `src/Microsoft.ML.Tokenizers/Model/TiktokenTokenizer.cs`
3. Apply the two changes documented in `gpt-5.3-changes.cs`
4. Add appropriate unit tests
5. Build and test the changes
6. Submit a pull request to dotnet/machinelearning

## References

- Microsoft.ML.Tokenizers NuGet: https://www.nuget.org/packages/Microsoft.ML.Tokenizers
- Documentation: https://learn.microsoft.com/en-us/dotnet/ai/how-to/use-tokenizers
- Source Code: https://github.com/dotnet/machinelearning/tree/main/src/Microsoft.ML.Tokenizers
