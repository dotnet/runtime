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
        public record UnsignedInteger(ulong Value) : StressMessageArgument;
    }

    private readonly ISpecialPointerFormatter NoOpSpecialPointerFormatter = new Mock<ISpecialPointerFormatter>().Object;

    [Fact]
    public void NoFormatSpecifier()
    {
        var (target, message, data) = CreateFixture("Hello, World!");
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            Assert.Equal("Hello, World!", formatter.GetFormattedMessage(message));
        }
    }

    [Fact]
    public void UnsupportedWidthSpecifier()
    {
        var (target, message, data) = CreateFixture("The answer is %ld", new StressMessageArgument.SignedInteger(42));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            Assert.Throws<InvalidOperationException>(() => formatter.GetFormattedMessage(message));
        }
    }

    [Fact]
    public void UnsupportedFormatSpecifier()
    {
        var (target, message, data) = CreateFixture("The answer is %f", new StressMessageArgument.SignedInteger(0));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            Assert.Throws<InvalidOperationException>(() => formatter.GetFormattedMessage(message));
        }
    }

    public static IEnumerable<object?[]> SignedIntegerSpecifierTestCases()
    {
        foreach (string specifier in new[] { "d", "i", "lld", "lli", "zd", "zi" })
        {
            foreach (int? width in new int?[] { null, 4, 40 })
            {
                foreach (long value in new[] { 0, int.MaxValue, long.MaxValue, long.MaxValue - 1, long.MinValue, int.MinValue, 42 })
                {
                    yield return [$"%0{width?.ToString() ?? ""}{specifier}", value, width, '0'];
                    yield return [$"%{width?.ToString() ?? ""}{specifier}", value, width, ' '];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(SignedIntegerSpecifierTestCases))]
    public void SignedIntegerSpecifier(string specifier, long value, int? width, char paddingChar)
    {
        var (target, message, data) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.SignedInteger(value));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            string formattedValue = (width, paddingChar) switch
            {
                (null, _) => value.ToString(),
                (int specifiedWidth, '0') => value.ToString($"D{specifiedWidth}"),
                (int specifiedWidth, char padChar) => value.ToString().PadLeft(specifiedWidth, padChar),
            };
            Assert.Equal($"The answer is {formattedValue}", formatter.GetFormattedMessage(message));
        }
    }

    public static IEnumerable<object[]> UnsignedIntegerSpecifierTestCases()
    {
        foreach (string specifier in new[] { "%u", "%llu", "%zu", "%I64u" })
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
        var (target, message, data) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.UnsignedInteger(value));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            Assert.Equal($"The answer is {value}", formatter.GetFormattedMessage(message));
        }
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
        var (target, message, data) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.UnsignedInteger(value));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            // Use the same case 'x' or 'X' as the specifier for the validation.
            Assert.Equal($"The answer is {value.ToString(specifier[^1..])}", formatter.GetFormattedMessage(message));
        }
    }

    public static IEnumerable<object[]> HexWithPrefixIntegerSpecifierTestCases()
    {
        foreach (string specifier in new[] { "%#x", "%#X", "%p", "%I64p" })
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
        var (target, message, data) = CreateFixture($"The answer is {specifier}", new StressMessageArgument.UnsignedInteger(value));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            Assert.Equal($"The answer is 0x{value:x}", formatter.GetFormattedMessage(message));
        }
    }

    [Fact]
    public void HexWithPrefixIntegerSpecifierWithPadding()
    {
        var (target, message, data) = CreateFixture("The answer is %#010x", new StressMessageArgument.UnsignedInteger(0x50));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            Assert.Equal("The answer is 0x00000050", formatter.GetFormattedMessage(message));
        }
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
        var (target, message, data) = CreateFixture($"The answer is '{specifier}'",
            utf16 ? new StressMessageArgument.Utf16String(value)
            : new StressMessageArgument.Utf8String(value));
        using (data)
        {
            StressMessageFormatter formatter = new(target, NoOpSpecialPointerFormatter);
            Assert.Equal($"The answer is '{value}'", formatter.GetFormattedMessage(message));
        }
    }

    [Fact]
    public void MethodTableSpecifier()
    {
        ulong value = 0x123;
        var (target, message, data) = CreateFixture("We have a %pT", new StressMessageArgument.UnsignedInteger(value));
        using (data)
        {
            var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
            specialPointerFormatter.Setup(f => f.FormatMethodTable(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"MethodTable: 0x{targetPointer.Value:X}");
            StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
            Assert.Equal($"We have a MethodTable: 0x{value:X}", formatter.GetFormattedMessage(message));
        }
    }

    [Fact]
    public void MethodDescSpecifier()
    {
        ulong value = 0x123;
        var (target, message, data) = CreateFixture("We have a %pM", new StressMessageArgument.UnsignedInteger(value));
        using (data)
        {
            var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
            specialPointerFormatter.Setup(f => f.FormatMethodDesc(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"MethodDesc: 0x{targetPointer.Value:X}");
            StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
            Assert.Equal($"We have a MethodDesc: 0x{value:X}", formatter.GetFormattedMessage(message));
        }
    }

    [Fact]
    public void VTableSpecifier()
    {
        ulong value = 0x123;
        var (target, message, data) = CreateFixture("We have a %pV", new StressMessageArgument.UnsignedInteger(value));
        using (data)
        {
            var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
            specialPointerFormatter.Setup(f => f.FormatVTable(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"VTable: 0x{targetPointer.Value:X}");
            StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
            Assert.Equal($"We have a VTable: 0x{value:X}", formatter.GetFormattedMessage(message));
        }
    }

    [Fact]
    public void StackTraceSpecifier()
    {
        ulong value = 0x123;
        var (target, message, data) = CreateFixture("We have a %pK", new StressMessageArgument.UnsignedInteger(value));
        using (data)
        {
            var specialPointerFormatter = new Mock<ISpecialPointerFormatter>();
            specialPointerFormatter.Setup(f => f.FormatStackTrace(It.IsAny<TargetPointer>())).Returns((TargetPointer targetPointer) => $"StackTrace: 0x{targetPointer.Value:X}");
            StressMessageFormatter formatter = new(target, specialPointerFormatter.Object);
            Assert.Equal($"We have a StackTrace: 0x{value:X}", formatter.GetFormattedMessage(message));
        }
    }

    struct DisposableGCHandle(GCHandle handle) : IDisposable
    {
        public void Dispose() => handle.Free();
    }

    private static (Target target, StressMsgData Message, IDisposable dataHandle) CreateFixture(string format, params StressMessageArgument[] args)
    {
        // Add a dummy value at 0 to make the format string a non-null pointer and null terminate it.
        List<byte> buffer = [0x42, .. Encoding.UTF8.GetBytes(format), 0];
        TargetPointer[] arguments = new TargetPointer[args.Length];

        // Process the provided arguments for the message to insert them into the fake "memory space".
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case StressMessageArgument.Utf8String(string utf8String):
                    arguments[i] = (ulong)buffer.Count;
                    buffer.AddRange(Encoding.UTF8.GetBytes(utf8String));
                    buffer.Add(0); // Null-terminate the string.
                    break;
                case StressMessageArgument.Utf16String(string utf16String):
                    arguments[i] = (ulong)buffer.Count;
                    buffer.AddRange(Encoding.Unicode.GetBytes(utf16String));
                    buffer.AddRange([0, 0]); // Null-terminate the string.
                    break;
                case StressMessageArgument.SignedInteger(long signedInteger):
                    arguments[i] = (ulong)signedInteger;
                    break;
                case StressMessageArgument.UnsignedInteger(ulong unsignedInteger):
                    arguments[i] = unsignedInteger;
                    break;
            }
        }

        GCHandle memorySpaceHandle = GCHandle.Alloc(buffer);

        Target target = Target.Create(
            new ContractDescriptorParser.ContractDescriptor
            {
            },
            Array.Empty<TargetPointer>(),
            &ReadFromCallback,
            (void*)GCHandle.ToIntPtr(memorySpaceHandle),
            true,
            8);

        StressMsgData message = new(
            Facility: 0,
            FormatString: new TargetPointer(1),
            Timestamp: 0,
            Args: arguments);

        return (target, message, new DisposableGCHandle(memorySpaceHandle));

        [UnmanagedCallersOnly]
        static unsafe int ReadFromCallback(ulong address, byte* buffer, uint length, void* context)
        {
            // context is a GCHandle to a byte array.
            var handle = GCHandle.FromIntPtr((IntPtr)context);
            var memorySpace = (List<byte>)handle.Target!;
            return CollectionsMarshal.AsSpan(memorySpace).Slice((int)address, (int)length).TryCopyTo(new Span<byte>(buffer, (int)length)) ? (int)length : -1;
        }
    }
}
