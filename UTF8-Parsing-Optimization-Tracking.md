# UTF-8 Parsing Optimization for System.Text.Json Value Converters

## Overview
Starting from .NET 10, several built-in types now support UTF-8 parsing through `TryParse(ReadOnlySpan<byte>, ...)` methods. This presents an optimization opportunity for System.Text.Json value converters to avoid UTF-8 to UTF-16 transcoding overhead.

## Target Timeline
- **Implementation Date**: After November 2026 (when .NET 8 drops out of support)
- **Reason**: This optimization requires .NET 10+ APIs and cannot be backported to .NET 8

## Types with UTF-8 TryParse Support in .NET 10

### Numeric Types
- `byte` - ByteConverter.cs
- `sbyte` - SByteConverter.cs
- `short` (Int16) - Int16Converter.cs
- `ushort` (UInt16) - UInt16Converter.cs
- `int` (Int32) - Int32Converter.cs
- `uint` (UInt32) - UInt32Converter.cs
- `long` (Int64) - Int64Converter.cs
- `ulong` (UInt64) - UInt64Converter.cs
- `Int128` - Int128Converter.cs
- `UInt128` - UInt128Converter.cs
- `IntPtr` (nint) - IntPtrConverter.cs
- `UIntPtr` (nuint) - UIntPtrConverter.cs
- `decimal` - DecimalConverter.cs
- `float` (Single) - SingleConverter.cs
- `double` (Double) - DoubleConverter.cs
- `Half` - HalfConverter.cs

### Other Types
- `char` - CharConverter.cs
- `Guid` - GuidConverter.cs
- `Version` - VersionConverter.cs (this PR)

## Implementation Strategy

### Current Implementation Pattern
Currently, these converters follow this pattern:
```csharp
public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    // 1. Read UTF-8 bytes from reader
    // 2. Transcode to UTF-16 (string or char span)
    // 3. Call T.TryParse(ReadOnlySpan<char>, ...)
}
```

### Optimized Implementation Pattern (.NET 10+)
```csharp
public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
#if NET10_0_OR_GREATER
    // Fast path: Parse directly from UTF-8 bytes
    ReadOnlySpan<byte> utf8Bytes = reader.HasValueSequence 
        ? reader.ValueSequence.ToArray()
        : reader.ValueSpan;
    
    if (T.TryParse(utf8Bytes, out T? result))
    {
        return result;
    }
#else
    // Fallback: Transcode and parse
    string? str = reader.GetString();
    if (T.TryParse(str, out T? result))
    {
        return result;
    }
#endif
    ThrowHelper.ThrowFormatException();
}
```

### Performance Benefits
- Eliminates UTF-8 to UTF-16 transcoding overhead
- Reduces memory allocation (no intermediate string/char buffer)
- Faster parsing for all numeric and other supported types
- Particularly beneficial for high-throughput JSON deserialization scenarios

## Related Files

### Converters Directory
`src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/`

### Specific Converters to Update
- VersionConverter.cs (PR context)
- All numeric converters listed above
- GuidConverter.cs
- CharConverter.cs

## Notes for Implementation

### Considerations
1. **Validation Consistency**: Ensure UTF-8 parsing maintains the same validation rules as UTF-16 parsing
2. **Error Messages**: Verify error messages and exception types remain consistent
3. **ValueSequence Handling**: Handle both contiguous (`ValueSpan`) and segmented (`ValueSequence`) UTF-8 data
4. **Escaped Values**: Handle escaped JSON strings appropriately
5. **Whitespace**: Maintain consistent whitespace handling (e.g., Version converter rejects leading/trailing whitespace)

### Testing Strategy
- Benchmark UTF-8 vs UTF-16 parsing paths to validate performance improvement
- Ensure all existing tests pass with new implementation
- Add specific tests for UTF-8 path edge cases
- Test with both escaped and unescaped JSON values
- Test with ValueSequence scenarios (multi-segment UTF-8 data)

## References
- Original Issue: #118201 (Version parsing with leading plus signs)
- VersionConverter PR: (link to this PR)
- IUtf8SpanParsable Interface: Standardizes UTF-8 parsing across types
- .NET 8 Support Timeline: Ends November 2026

## Action Items
- [ ] Create tracking issue in dotnet/runtime
- [ ] Link to VersionConverter.cs implementation
- [ ] Note all converters that could benefit from UTF-8 parsing
- [ ] Schedule for implementation after .NET 8 EOL
- [ ] Assign to area owner for System.Text.Json
