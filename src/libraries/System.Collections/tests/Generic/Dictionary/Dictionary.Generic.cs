// Copyright(c) Microsoft.All rights reserved.
// Licensed under the MIT license.See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class Dictionary_Generic_Tests_string_string : Dictionary_Generic_Tests<string, string>
    {
        protected override KeyValuePair<string, string> TFactory(int seed)
        {
            return new KeyValuePair<string, string>(TKeyFactory(seed), TKeyFactory(seed + 500));
        }

        protected override string TKeyFactory(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }

        protected override string TValueFactory(int seed)
        {
            return TKeyFactory(seed);
        }
    }

    public class Dictionary_Generic_Tests_int_int : Dictionary_Generic_Tests<int, int>
    {
        protected override bool DefaultValueAllowed { get { return true; } }

        protected override KeyValuePair<int, int> TFactory(int seed)
        {
            Random rand = new Random(seed);
            return new KeyValuePair<int, int>(rand.Next(), rand.Next());
        }

        protected override int TKeyFactory(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override int TValueFactory(int seed)
        {
            return TKeyFactory(seed);
        }
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

        protected override SimpleInt TKeyFactory(int seed)
        {
            Random rand = new Random(seed);
            return new SimpleInt(rand.Next());
        }

        protected override int TValueFactory(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }

        protected override KeyValuePair<SimpleInt, int> TFactory(int seed)
        {
            return new KeyValuePair<SimpleInt, int>(TKeyFactory(seed), TValueFactory(seed));
        }
    }
}
