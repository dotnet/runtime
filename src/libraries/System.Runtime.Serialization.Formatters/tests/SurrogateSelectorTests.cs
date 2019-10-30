// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class SurrogateSelectorTests
    {
        [Fact]
        public void AddSurrogate_InvalidArguments_ThrowExceptions()
        {
            var s = new SurrogateSelector();
            AssertExtensions.Throws<ArgumentNullException>("type", () => s.AddSurrogate(null, new StreamingContext(), new NonSerializablePairSurrogate()));
            AssertExtensions.Throws<ArgumentNullException>("surrogate", () => s.AddSurrogate(typeof(NonSerializablePair<int, string>), new StreamingContext(), null));
        }

        [Fact]
        public void ChainSelector_InvalidArguments_ThrowExceptions()
        {
            var s1 = new SurrogateSelector();
            AssertExtensions.Throws<ArgumentNullException>("selector", () => s1.ChainSelector(null));
            Assert.Throws<SerializationException>(() => s1.ChainSelector(s1));

            var s2 = new SurrogateSelector();
            s2.ChainSelector(s1);
            AssertExtensions.Throws<ArgumentException>("selector", () => s1.ChainSelector(s2));

            var s3 = new SurrogateSelector();
            s3.ChainSelector(s2);
            AssertExtensions.Throws<ArgumentException>("selector", () => s1.ChainSelector(s3));
        }

        [Fact]
        public void GetNextSelector_ReturnsCorrectSelector()
        {
            var s1 = new SurrogateSelector();
            var s2 = new SurrogateSelector();
            s2.ChainSelector(s1);
            Assert.Null(s1.GetNextSelector());
            Assert.Same(s1, s2.GetNextSelector());
        }

        [Fact]
        public void GetSurrogate_InvalidArguments_ThrowExceptions()
        {
            var s = new SurrogateSelector();
            var c = new StreamingContext();
            ISurrogateSelector selector;
            AssertExtensions.Throws<ArgumentNullException>("type", () => s.GetSurrogate(null, c, out selector));
        }

        [Fact]
        public void GetSurrogate_ChainsToNextSelector()
        {
            var c = new StreamingContext();
            var s1 = new SurrogateSelector();
            var s2 = new SurrogateSelector();
            s2.ChainSelector(s1);

            ISurrogateSelector selector;
            Assert.Null(s1.GetSurrogate(typeof(NonSerializablePair<int, string>), c, out selector));
            Assert.Same(s1, selector);

            s1.AddSurrogate(typeof(NonSerializablePair<int, string>), c, new NonSerializablePairSurrogate());
            Assert.NotNull(s1.GetSurrogate(typeof(NonSerializablePair<int, string>), c, out selector));
            Assert.Same(s1, selector);

            Assert.NotNull(s2.GetSurrogate(typeof(NonSerializablePair<int, string>), c, out selector));
            Assert.Same(s1, selector);

            s2.AddSurrogate(typeof(NonSerializablePair<int, string>), c, new NonSerializablePairSurrogate());
            Assert.NotNull(s2.GetSurrogate(typeof(NonSerializablePair<int, string>), c, out selector));
            Assert.Same(s2, selector);

            s2.RemoveSurrogate(typeof(NonSerializablePair<int, string>), c);
            Assert.NotNull(s2.GetSurrogate(typeof(NonSerializablePair<int, string>), c, out selector));
            Assert.Same(s1, selector);

            s1.RemoveSurrogate(typeof(NonSerializablePair<int, string>), c);
            Assert.Null(s2.GetSurrogate(typeof(NonSerializablePair<int, string>), c, out selector));
            Assert.Same(s1, selector);
        }

        [Fact]
        public void RemoveSurrogate_InvalidArguments_ThrowExceptions()
        {
            var s = new SurrogateSelector();
            var c = new StreamingContext();
            AssertExtensions.Throws<ArgumentNullException>("type", () => s.RemoveSurrogate(null, c));
            s.RemoveSurrogate(typeof(string), c); // no exception even if removal fails
        }
    }
}
