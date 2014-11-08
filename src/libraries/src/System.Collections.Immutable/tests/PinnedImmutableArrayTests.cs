namespace System.Collections.Immutable.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class PinnedImmutableArrayTests
    {
        private static readonly ImmutableArray<int> emptyDefault = default(ImmutableArray<int>);

        [Fact]
        public void CtorDefault()
        {
            Assert.Throws<ArgumentNullException>(() => PinnedImmutableArray.Create(emptyDefault));
        }

        [Fact]
        public void CtorRefType()
        {
            Assert.Throws<ArgumentException>(() => PinnedImmutableArray.Create(ImmutableArray.Create<string>()));
            Assert.Throws<ArgumentException>(() => PinnedImmutableArray.Create(ImmutableArray.Create<StructWithReferenceTypeField>()));
        }

        [Fact]
        public void GetPointer()
        {
            var array = ImmutableArray.Create(1, 2, 3);
            using (var handle = PinnedImmutableArray.Create(array))
            {
                var copy = ImmutableArray.Create<int>(handle.Pointer, 3);
                Assert.Equal<int>(array, copy);
            }
        }

        /// <summary>
        /// Verifies that calling Dispose multiple times doesn't throw (per the Dispose guidelines).
        /// </summary>
        [Fact]
        public void DisposeTwice()
        {
            var array = ImmutableArray.Create(1, 2, 3);
            var helper = PinnedImmutableArray.Create(array);
            helper.Dispose();
            helper.Dispose();
        }

        [Fact]
        public void AccessPointerAfterDisposeThrows()
        {
            var array = ImmutableArray.Create(1, 2, 3);
            var helper = PinnedImmutableArray.Create(array);
            helper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => helper.Pointer);
        }

        private struct StructWithReferenceTypeField
        {
            public string foo;

            public StructWithReferenceTypeField(string foo)
            {
                this.foo = foo;
            }
        }
    }
}
