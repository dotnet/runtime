// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class OrderedDictionary_Generic_Tests_string_string : OrderedDictionary_Generic_Tests<string, string>
    {
        protected override KeyValuePair<string, string> CreateT(int seed) =>
            new KeyValuePair<string, string>(CreateTKey(seed), CreateTValue(seed + 500));

        protected override string CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }

        protected override string CreateTValue(int seed) => CreateTKey(seed);
    }

    public class OrderedDictionary_Generic_Tests_object_byte : OrderedDictionary_Generic_Tests<object, byte>
    {
        protected override KeyValuePair<object, byte> CreateT(int seed) =>
            new KeyValuePair<object, byte>(CreateTKey(seed), CreateTValue(seed + 500));

        protected override object CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }

        protected override byte CreateTValue(int seed) => (byte)new Random(seed).Next();
    }

    public class OrderedDictionary_Generic_Tests_int_int : OrderedDictionary_Generic_Tests<int, int>
    {
        protected override bool DefaultValueAllowed { get { return true; } }

        protected override KeyValuePair<int, int> CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new KeyValuePair<int, int>(rand.Next(), rand.Next());
        }

        protected override int CreateTKey(int seed) => new Random(seed).Next();

        protected override int CreateTValue(int seed) => CreateTKey(seed);
    }

    [OuterLoop]
    public class OrderedDictionary_Generic_Tests_EquatableBackwardsOrder_int : OrderedDictionary_Generic_Tests<EquatableBackwardsOrder, int>
    {
        protected override KeyValuePair<EquatableBackwardsOrder, int> CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new KeyValuePair<EquatableBackwardsOrder, int>(new EquatableBackwardsOrder(rand.Next()), rand.Next());
        }

        protected override EquatableBackwardsOrder CreateTKey(int seed)
        {
            Random rand = new Random(seed);
            return new EquatableBackwardsOrder(rand.Next());
        }

        protected override int CreateTValue(int seed) => new Random(seed).Next();

        protected override IDictionary<EquatableBackwardsOrder, int> GenericIDictionaryFactory() =>
            new OrderedDictionary<EquatableBackwardsOrder, int>();
    }
}
