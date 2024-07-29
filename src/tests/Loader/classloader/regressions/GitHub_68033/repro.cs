using System;
using Xunit;

public abstract class Foo : IComparable<Foo>, IComparable<Bar>
{
        public abstract int CompareTo(Foo other);
        public abstract int CompareTo(Bar other);
}

public sealed class Bar : Foo
{
        public override int CompareTo(Foo other)
        {
                return 1;
        }

        public override int CompareTo(Bar other)
        {
                return 1;
        }
}

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        new Bar(); // should not throw a TLE due circular reference to Bar in IComparable<Bar>
    }
}
