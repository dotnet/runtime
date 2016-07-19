// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    public class Dictionary_Generic_Tests_string_string : Dictionary_Generic_Tests<string, string>
    {
        protected override KeyValuePair<string, string> CreateT(int seed)
        {
            return new KeyValuePair<string, string>(CreateTKey(seed), CreateTKey(seed + 500));
        }

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

    public class Dictionary_Generic_Tests_int_int : Dictionary_Generic_Tests<int, int>
    {
        protected override bool DefaultValueAllowed => true;

        protected override KeyValuePair<int, int> CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new KeyValuePair<int, int>(rand.Next(), rand.Next());
        }

        protected override int CreateTKey(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override int CreateTValue(int seed) => CreateTKey(seed);
    }

    public class Dictionary_Generic_Tests_SimpleInt_int_With_Comparer_WrapStructural_SimpleInt : Dictionary_Generic_Tests<SimpleInt, int>
    {
        protected override bool DefaultValueAllowed { get { return true; } }

        public override IEqualityComparer<SimpleInt> GetKeyIEqualityComparer()
        {
            return new WrapStructural_SimpleInt();
        }

        public override IComparer<SimpleInt> GetKeyIComparer()
        {
            return new WrapStructural_SimpleInt();
        }

        protected override SimpleInt CreateTKey(int seed)
        {
            Random rand = new Random(seed);
            return new SimpleInt(rand.Next());
        }

        protected override int CreateTValue(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override KeyValuePair<SimpleInt, int> CreateT(int seed)
        {
            return new KeyValuePair<SimpleInt, int>(CreateTKey(seed), CreateTValue(seed));
        }
    }
}
