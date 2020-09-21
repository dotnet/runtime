using Xunit;

namespace System.Reflection.Tests
{
    public class PointerTests
    {
        public unsafe struct BitwiseComparable
        {
            public int* PublicInt;
        }

        public unsafe struct MemberwiseComparable
        {
            public string ReferenceType;
            public int* PublicInt;
        }

        [Fact]
        public unsafe void BitwiseEquality_AreEqual()
        {
            int someNumber = 1;
            var a = new BitwiseComparable();
            a.PublicInt = &someNumber;
            var b = a;

            Assert.True(a.Equals(b));
        }

        [Fact]
        public unsafe void BitwiseEquality_EqualWithSelf()
        {
            int someNumber = 1;
            var a = new BitwiseComparable();
            a.PublicInt = &someNumber;

            Assert.True(a.Equals(a));
        }

        [Fact]
        public unsafe void MemberwiseEquality_AreEqual()
        {
            int someNumber = 1;
            var a = new MemberwiseComparable();
            a.PublicInt = &someNumber;
            var b = a;

            Assert.True(a.Equals(b));
        }

        [Fact]
        public unsafe void MemberwiseEquality_EqualWithSelf()
        {
            int someNumber = 1;
            var a = new MemberwiseComparable();
            a.PublicInt = &someNumber;

            Assert.True(a.Equals(a));
        }
    }
}
