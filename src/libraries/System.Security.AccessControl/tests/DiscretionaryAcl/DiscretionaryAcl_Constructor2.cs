// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_Constructor2
    {
        public static IEnumerable<object[]> DiscretionaryACL_Constructor2()
        {
            yield return new object[] { false, false, 0, 0 };
            yield return new object[] { false, true, 127, 0 };
            yield return new object[] { true, false, 255, 0 };
            yield return new object[] { true, true, 0, 0 };
            yield return new object[] { false, false, 127, 1 };
            yield return new object[] { false, true, 255, 1 };
            yield return new object[] { true, false, 2, 1 };
            yield return new object[] { true, true, 4, 1 };
        }

        [Theory]
        [MemberData(nameof(DiscretionaryACL_Constructor2))]
        public static void Constructor2(bool isContainer, bool isDS, byte revision, int capacity)
        {
            bool result = true;
            byte[] dAclBinaryForm = null;
            byte[] rAclBinaryForm = null;

            RawAcl rawAcl = null;

            DiscretionaryAcl discretionaryAcl = null;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, revision, capacity);
            rawAcl = new RawAcl(revision, capacity);

            if (isContainer == discretionaryAcl.IsContainer &&
                isDS == discretionaryAcl.IsDS &&
                revision == discretionaryAcl.Revision &&
                0 == discretionaryAcl.Count &&
                8 == discretionaryAcl.BinaryLength &&
                true == discretionaryAcl.IsCanonical)
            {
                dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                rAclBinaryForm = new byte[rawAcl.BinaryLength];
                discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                if (!Utils.IsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                    result = false;

                //redundant index check
                for (int i = 0; i < discretionaryAcl.Count; i++)
                {
                    if (!Utils.IsAceEqual(discretionaryAcl[i], rawAcl[i]))
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;
            }

            Assert.True(result);
        }

        [Fact]
        public static void Constructor2_NegativeCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiscretionaryAcl(false, false, 0, -1));
        }
    }
}
