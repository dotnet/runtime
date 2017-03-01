// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    /// <summary>
    /// Constructor1
    /// </summary>
    public partial class DiscretionaryAcl_Constructor1
    {
        public static IEnumerable<object[]> DiscretionaryACL_Constructor1()
        {
            yield return new object[] { false, false, 0 };
            yield return new object[] { false, true, 0 };
            yield return new object[] { true, false, 0 };
            yield return new object[] { true, true, 0 };
            yield return new object[] { false, false, 1 };
            yield return new object[] { false, true, 1 };
            yield return new object[] { true, false, 1 };
            yield return new object[] { true, true, 1 };
        }

        [Theory]
        [MemberData(nameof(DiscretionaryACL_Constructor1))]
        public static bool Constructor1(bool isContainer, bool isDS, int capacity)
        {
            bool result = true;
            byte[] dAclBinaryForm = null;
            byte[] rAclBinaryForm = null;

            RawAcl rawAcl = null;

            DiscretionaryAcl discretionaryAcl = null;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, capacity);
            rawAcl = new RawAcl(isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, capacity);

            if (isContainer == discretionaryAcl.IsContainer &&
                isDS == discretionaryAcl.IsDS &&
                (isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision) == discretionaryAcl.Revision &&
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
            return result;
        }


        [Fact]
        public static void Constructor1_NegativeCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiscretionaryAcl(false, false, -1));
        }
    }
}