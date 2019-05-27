// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Linq;
using Xunit;

namespace System.Xml.Tests
{
    // These tests will fail on Desktop as randomized hashing is disabled there.
    // Dictionary<XmlQualifiedName, ...> and Dictionary<SecureStringHasher, ...>
    //   should use randomized hashing even on Desktop
    public class XmlMiscTests
    {
        // Hashes of string for full .NET 2.0-4.6.2
        // if that algorithm will be used the test will fail
        private static readonly string[] _strings = new string[] { "foo", "asdf", "randhashing" };
        private static readonly Tuple<int, int, int>[] _knownNonRandomizedHashesOfStrings = new Tuple<int, int, int>[] {
            new Tuple<int, int, int>(-1788410455, -352695115, 1368290095),
            new Tuple<int, int, int>(1502598398, -1233826608, 1092416431)
        };

        // Few generated hash collisions for System.Text.StringOrCharArray.GetHashCode
        // These should have the same hash when randomized hashing is disabled on CoreCLR
        // The algorithm used for generation is as following:
        // - If we take closer look at the loop, we can note that hash code is a linear combination of partial hashes of every odd and every even character
        // - That means that for example string ABCDEF hash(ABCDEF) = partial_hash(ACE) + M*partial_hash(BDF)
        // - That significantly simplifies search as collision will always occur when parial_hash(ACE)=partial_hash(BDF)
        // - ACE and BDF pairs were searched brute force which really quickly gave results
        private static readonly Tuple<string, string>[] _collidingStringsPairs = new Tuple<string, string>[]
        {
            new Tuple<string, string>("01`@a@", "10@`@a"),
            new Tuple<string, string>("01Ps6t", "10sPt6"),
            new Tuple<string, string>("AFdBbC", "FABdCb"),
            new Tuple<string, string>("01xXUt", "10XxtU"),
            new Tuple<string, string>("01Ps6t", "10sPt6"),
            new Tuple<string, string>("12;E7J", "21E;J7"),
            new Tuple<string, string>("024u3P", "20u4P3"),
            new Tuple<string, string>("23`B8[", "32B`[8"),
            new Tuple<string, string>("039X3q", "30X9q3"),
            new Tuple<string, string>("23MoOl", "32oMlO"),
            new Tuple<string, string>("45QOd;", "54OQ;d"),
            new Tuple<string, string>("45HjrQ", "54jHQr"),
            new Tuple<string, string>("46v:r<", "64:v<r")
        };

        [Fact]
        public void StringsDontHashToAnyKnownNonRandomizedSets()
        {
            var setOfHashes = new Tuple<int, int, int>(_strings[0].GetHashCode(), _strings[1].GetHashCode(), _strings[2].GetHashCode());
            Assert.False(_knownNonRandomizedHashesOfStrings.Contains(setOfHashes));
        }

        [Fact]
        public void StringsDoNotUseAlgorithmSimilarToCoreClrWhenRandomizedHashingIsDisabled()
        {
            // Even though GetHashCode gives different results on .NET 4.6 and CoreCLR with disabled
            // hash randomization those pairs are causing collisions on both platforms as they meet
            // certain properties causing hash collisions
            
            // Checking few different hash codes - if at least one is different then
            // CoreCLR implementation is not being used
            foreach (var collPair in _collidingStringsPairs)
            {
                if (collPair.Item1.GetHashCode() != collPair.Item2.GetHashCode())
                {
                    return;
                }
            }

            Assert.True(false);
        }

        [Fact]
        public void XmlQualifiedNameUsesStringGetHashCode()
        {
            Assert.Equal("foo".GetHashCode(), new XmlQualifiedName("foo").GetHashCode());
            Assert.Equal("asdf".GetHashCode(), new XmlQualifiedName("asdf").GetHashCode());
            Assert.Equal("randhashing".GetHashCode(), new XmlQualifiedName("randhashing").GetHashCode());
        }

        [Fact]
        public void SecureStringHasherUsesStringGetHashCode()
        {
            Assert.Equal("foo".GetHashCode(), new SecureStringHasher().GetHashCode("foo"));
            Assert.Equal("asdf".GetHashCode(), new SecureStringHasher().GetHashCode("asdf"));
            Assert.Equal("randhashing".GetHashCode(), new SecureStringHasher().GetHashCode("randhashing"));
        }
    }
}
