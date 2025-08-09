using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using Xunit;

public class SelfReferentialStatics
{
    public interface IExample
    {
        public static Example DefaultExample { get; } = new();
    }
    public struct Example : IExample { }

    public struct MyStruct
    {
        static ImmutableArray<MyStruct> One;
    }

    public struct Foo<T>
    {
        public static readonly Foo<int> Empty = default;
    }

    public readonly struct StructureWithComplexStaticField
    {
        private readonly int _value;

        private StructureWithComplexStaticField(int value)
        {
            _value = value;
        }

        public static readonly ReadOnlyMemory<StructureWithComplexStaticField> StaticField =
            new StructureWithComplexStaticField[] { new(0), new(1), new(2), new(3) };

        public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);
    }


    public struct Bar
    {
        public static readonly Foo<Bar> Baz;
        public Bar(string message) => System.Console.WriteLine(message);
    }

    [Fact]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/118472")]
    public static void TestEntryPoint()
    {
        var example = IExample.DefaultExample;

        new Bar("Should print");
        Console.WriteLine(typeof(StructureWithComplexStaticField).FullName);
        Console.WriteLine(new Foo<char>());
        Console.WriteLine(example.GetType());
        Console.WriteLine(new MyStruct());
        Console.WriteLine("Worked");
    }
}
