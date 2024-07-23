using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace StressLogAnalyzer.Tests;

public unsafe class StressMessageFormatterTests
{
    private abstract record StressMessageArgument
    {
        private StressMessageArgument() { }
        public record Utf8String(string Value) : StressMessageArgument;
        public record Utf16String(string Value) : StressMessageArgument;
        public record SignedInteger(long Value) : StressMessageArgument;
        public record FloatingPoint(double Value) : StressMessageArgument;
        public record UnsignedInteger(ulong Value) : StressMessageArgument;
    }

    private readonly ISpecialPointerFormatter NoOpSpecialPointerFormatter = new Mock<ISpecialPointerFormatter>().Object;

    [Fact]
    public void NoFormatSpecifier()
    {
        var (target, message) = CreateFixture("Hello, World!");

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Equal("Hello, World!", formatter.GetFormattedMessage(message));
    }

    [Fact]
    public void UnsupportedWidthSpecifier()
    {
        var (target, message) = CreateFixture("The answer is %ld", new StressMessageArgument.SignedInteger(42));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Throws<InvalidOperationException>(() => formatter.GetFormattedMessage(message));
    }

    [Fact]
    public void UnsupportedFormatSpecifier()
    {
        var (target, message) = CreateFixture("The answer is %e", new StressMessageArgument.SignedInteger(0));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Throws<InvalidOperationException>(() => formatter.GetFormattedMessage(message));
    }

    public static IEnumerable<object?[]> IntegerSpecifier32BitWidthCases()
    {
        foreach (string specifier in new[] { "d", "i" })
        {
            foreach (int? width in new int?[] { null, 4, 40 })
            {
                foreach (long value in new[] { 0, int.MaxValue, 42, -24, int.MinValue, -1 })
                {
                    yield return [$"%0{width?.ToString() ?? ""}{specifier}", value, width, '0'];
                    yield return [$"%{width?.ToString() ?? ""}{specifier}", value, width, ' '];
                }
            }
        }
    }

    public static IEnumerable<object?[]> IntegerSpecifier64BitWidthCases()
    {
        foreach (string specifier in new[] {  "lld", "lli", "zd", "zi" })
        {
            foreach (int? width in new int?[] { null, 4, 40 })
            {
                foreach (long value in new[] { 0, long.MaxValue, 42, -24, long.MinValue, -1 })
                {
                    yield return [$"%0{width?.ToString() ?? ""}{specifier}", value, width, '0'];
                    yield return [$"%{width?.ToString() ?? ""}{specifier}", value, width, ' '];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(IntegerSpecifier32BitWidthCases))]
    [MemberData(nameof(IntegerSpecifier64BitWidthCases))]
    public void SignedIntegerSpecifier(string specifier, long value, int? width, char paddingChar)
    {
        var (target, message) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.SignedInteger(value));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        string formattedValue = (width, paddingChar) switch
        {
            (null, _) => value.ToString(),
            (int specifiedWidth, '0') => value.ToString($"D{specifiedWidth}"),
            (int specifiedWidth, char padChar) => value.ToString().PadLeft(specifiedWidth, padChar),
        };
        Assert.Equal($"The answer is {formattedValue}", formatter.GetFormattedMessage(message));
    }

    public static IEnumerable<object[]> UnsignedIntegerSpecifierTestCases()
    {
        foreach (string specifier in new[] { "%u", "%lu", "%llu", "%zu", "%I64u" })
        {
            foreach (ulong value in new[] { 0ul, uint.MaxValue, ulong.MaxValue, ulong.MaxValue - 1, 42ul })
            {
                yield return [specifier, value];
            }
        }
    }

    [Theory]
    [MemberData(nameof(UnsignedIntegerSpecifierTestCases))]
    public void UnsignedIntegerSpecifier(string specifier, ulong value)
    {
        var (target, message) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.UnsignedInteger(value));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Equal($"The answer is {value}", formatter.GetFormattedMessage(message));
    }

    public static IEnumerable<object[]> HexIntegerSpecifierTestCases()
    {
        foreach (string specifier in new[] { "%x", "%X", "%llx", "%llX", "%zx", "%zX", "%Ix" })
        {
            foreach (ulong value in new[] { 0ul, uint.MaxValue, ulong.MaxValue, ulong.MaxValue - 1, 42ul })
            {
                yield return [specifier, value];
            }
        }
    }

    [Theory]
    [MemberData(nameof(HexIntegerSpecifierTestCases))]
    public void HexIntegerSpecifier(string specifier, ulong value)
    {
        var (target, message) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.UnsignedInteger(value));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        // Use the same case 'x' or 'X' as the specifier for the validation.
        Assert.Equal($"The answer is {value.ToString(specifier[^1..])}", formatter.GetFormattedMessage(message));
    }

    public static IEnumerable<object[]> HexWithPrefixIntegerSpecifierTestCases()
    {
        foreach (string specifier in new[] { "%#x", "%#X" })
        {
            foreach (ulong value in new[] { 0ul, uint.MaxValue, ulong.MaxValue, ulong.MaxValue - 1, 42ul })
            {
                yield return [specifier, value];
            }
        }
    }

    [Theory]
    [MemberData(nameof(HexWithPrefixIntegerSpecifierTestCases))]
    public void HexWithPrefixIntegerSpecifier(string specifier, ulong value)
    {
        var (target, message) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.UnsignedInteger(value));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Equal($"The answer is 0x{value:x}", formatter.GetFormattedMessage(message));
    }

    public static IEnumerable<object[]> PointerSpecifierTestCases()
    {
        foreach (string specifier in new[] { "%p", "%I64p" })
        {
            foreach (ulong value in new[] { 0ul, uint.MaxValue, ulong.MaxValue, ulong.MaxValue - 1, 42ul })
            {
                yield return [specifier, value];
            }
        }
    }

    [Theory]
    [MemberData(nameof(PointerSpecifierTestCases))]
    public void PointerSpecifier(string specifier, ulong value)
    {
        var (target, message) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.UnsignedInteger(value));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Equal($"The answer is {value:X16}", formatter.GetFormattedMessage(message));
    }



    [Fact]
    public void HexWithPrefixIntegerSpecifierWithPadding()
    {
        var (target, message) = CreateFixture("The answer is %#010x", new StressMessageArgument.UnsignedInteger(0x50));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Equal("The answer is 0x00000050", formatter.GetFormattedMessage(message));
    }

    public static IEnumerable<object[]> StringSpecifierTestCases()
    {
        foreach (string value in new[] {
                "",
                "Hello, World!",
                "Recursive pattern %d",
                "Unicode \\uD83C\\uDF32 \\u6728 \\uD83D\\uDD25 \\u706B \\uD83C\\uDF3E \\u571F \\uD83D\\uDEE1 \\u91D1 \\uD83C\\uDF0A \\u6C34"
        })
        {
            yield return ["%s", value, false];
            foreach (string specifier in new[] { "%S", "%ls" })
            {
                yield return [specifier, value, true];
            }
        }
    }

    [Theory]
    [MemberData(nameof(StringSpecifierTestCases))]
    public void StringSpecifier(string specifier, string value, bool utf16)
    {
        var (target, message) = CreateFixture($"The answer is '{specifier}'",
            utf16 ? new StressMessageArgument.Utf16String(value)
            : new StressMessageArgument.Utf8String(value));

        StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
        Assert.Equal($"The answer is '{value}'", formatter.GetFormattedMessage(message));
    }

    [Fact]
    public void MethodTableSpecifier()
    {
        ulong value = 0x123;
        var (target, message) = CreateFixture("We have a %pT", new StressMessageArgument.UnsignedInteger(value));

        var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
        specialPointerFormatter.Setup(f => f.FormatMethodTable(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"MethodTable: 0x{targetPointer.Value:X}");
        StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
        Assert.Equal($"We have a MethodTable: 0x{value:X}", formatter.GetFormattedMessage(message));
    }

    [Fact]
    public void MethodDescSpecifier()
    {
        ulong value = 0x123;
        var (target, message) = CreateFixture("We have a %pM", new StressMessageArgument.UnsignedInteger(value));

        var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
        specialPointerFormatter.Setup(f => f.FormatMethodDesc(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"MethodDesc: 0x{targetPointer.Value:X}");
        StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
        Assert.Equal($"We have a MethodDesc: 0x{value:X}", formatter.GetFormattedMessage(message));
    }

    [Fact]
    public void VTableSpecifier()
    {
        ulong value = 0x123;
        var (target, message) = CreateFixture("We have a %pV", new StressMessageArgument.UnsignedInteger(value));

        var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
        specialPointerFormatter.Setup(f => f.FormatVTable(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"VTable: 0x{targetPointer.Value:X}");
        StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
        Assert.Equal($"We have a VTable: 0x{value:X}", formatter.GetFormattedMessage(message));
    }

    [Fact]
    public void StackTraceSpecifier()
    {
        ulong value = 0x123;
        var (target, message) = CreateFixture("We have a %pK", new StressMessageArgument.UnsignedInteger(value));

        var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
        specialPointerFormatter.Setup(f => f.FormatStackTrace(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"StackTrace: 0x{targetPointer.Value:X}");
        StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
        Assert.Equal($"We have a StackTrace: 0x{value:X}", formatter.GetFormattedMessage(message));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(123.45)]
    [InlineData(-42.0)]
    public void SimpleFloatingPoint(double value)
    {
        var (target, message) = CreateFixture("The answer is %f", new StressMessageArgument.FloatingPoint(value));

        Assert.Equal($"The answer is {value:0.######}", new StressMessageFormatter(target, NoOpSpecialPointerFormatter).GetFormattedMessage(message));
    }

    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(123.45, "123.45")]
    [InlineData(-42.0, "-42")]
    public void FloatingPointWithPrecision(double value, string expected)
    {
        var (target, message) = CreateFixture("The answer is %.6f", new StressMessageArgument.FloatingPoint(value));

        Assert.Equal($"The answer is {expected}", new StressMessageFormatter(target, NoOpSpecialPointerFormatter).GetFormattedMessage(message));
    }

    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(123.45, "123.45")]
    [InlineData(-42.0, "-42")]
    public void FloatingPointWithPrecisionAndWidth(double value, string expected)
    {
        var (target, message) = CreateFixture("The answer is %10.6f", new StressMessageArgument.FloatingPoint(value));

        Assert.Equal($"The answer is {expected.PadLeft(10)}", new StressMessageFormatter(target, NoOpSpecialPointerFormatter).GetFormattedMessage(message));
    }

    [Theory]
    [InlineData(0.0, "0000")]
    [InlineData(123.45, "0123.45")]
    [InlineData(-42.0, "-0042")]
    public void FloatingPointWithPrecisionAndWidthAndZeroPadding(double value, string expected)
    {
        var (target, message) = CreateFixture("The answer is %08.3f", new StressMessageArgument.FloatingPoint(value));

        Assert.Equal($"The answer is {expected}", new StressMessageFormatter(target, NoOpSpecialPointerFormatter).GetFormattedMessage(message));
    }

    private static (Target target, StressMsgData Message) CreateFixture(string format, params StressMessageArgument[] args)
    {
        // Add a dummy value at 0 to make the format string a non-null pointer and null terminate it.
        List<byte> memorySpace = [0x42, .. Encoding.UTF8.GetBytes(format), 0];
        TargetPointer[] arguments = new TargetPointer[args.Length];

        // Process the provided arguments for the message to insert them into the fake "memory space".
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case StressMessageArgument.Utf8String(string utf8String):
                    arguments[i] = (ulong)memorySpace.Count;
                    memorySpace.AddRange(Encoding.UTF8.GetBytes(utf8String));
                    memorySpace.Add(0); // Null-terminate the string.
                    break;
                case StressMessageArgument.Utf16String(string utf16String):
                    arguments[i] = (ulong)memorySpace.Count;
                    memorySpace.AddRange(Encoding.Unicode.GetBytes(utf16String));
                    memorySpace.AddRange([0, 0]); // Null-terminate the string.
                    break;
                case StressMessageArgument.SignedInteger(long signedInteger):
                    arguments[i] = (ulong)signedInteger;
                    break;
                case StressMessageArgument.UnsignedInteger(ulong unsignedInteger):
                    arguments[i] = unsignedInteger;
                    break;
                case StressMessageArgument.FloatingPoint(double floatingPoint):
                    arguments[i] = BitConverter.DoubleToUInt64Bits(floatingPoint);
                    break;
            }
        }

        Target target = Target.Create(
            new ContractDescriptorParser.ContractDescriptor
            {
            },
            Array.Empty<TargetPointer>(),
            (address, buffer) => ReadFromCallback(address, buffer, memorySpace),
            true,
            8);

        StressMsgData message = new(
            Facility: 0,
            FormatString: new TargetPointer(1),
            Timestamp: 0,
            Args: arguments);

        return (target, message);

        static unsafe int ReadFromCallback(ulong address, Span<byte> buffer, List<byte> memorySpace)
        {
            return CollectionsMarshal.AsSpan(memorySpace).Slice((int)address, buffer.Length).TryCopyTo(buffer) ? buffer.Length : -1;
        }
    }
}
