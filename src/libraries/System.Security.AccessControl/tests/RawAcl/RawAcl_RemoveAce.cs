// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class RawAcl_RemoveAce
    {
        public static IEnumerable<object[]> RawAcl_RemoveAce_TestData()
        {
            yield return new object[] { "O:LAG:SYD:" };
            yield return new object[] { "O:LAG:SYD:AI(A;ID;FA;;;BA)" };
            yield return new object[] { "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
            yield return new object[] { "O:NUG:SYD:AI(D;CINP;FA;;;SO)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)(A;OICI;FA;;;BG)" };
        }

        [Theory]
        [MemberData(nameof(RawAcl_RemoveAce_TestData))]
        public static void TestRemoveAce(string sddl)
        {
            RawSecurityDescriptor rawSecurityDescriptor = null;
            RawAcl rawAcl = null;
            RawAcl rawAclVerifier = null;
            GenericAce ace = null;
            rawSecurityDescriptor = new RawSecurityDescriptor(sddl);
            rawAclVerifier = rawSecurityDescriptor.DiscretionaryAcl;
            rawAcl = Utils.CopyRawACL(rawAclVerifier);
            Assert.True(null != rawAcl && null != rawAclVerifier);
            int index = 0;
            int count = 0;
            //save current count
            count = rawAcl.Count;

            //test remove at -1
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                index = -1;
                rawAcl.RemoveAce(index);
            });

            //test remove at 0, only need to catch ArgumentOutOfRangeException if Count = 0
            index = 0;
            try
            {
                //save a copy of the ace
                ace = rawAcl[index];
                rawAcl.RemoveAce(index);
                Assert.False(0 == count);

                //verify the count number decrease one
                Assert.False(rawAcl.Count != count - 1);
                //verify the rawAcl.BinaryLength is updated correctly
                Assert.False(rawAcl.BinaryLength != rawAclVerifier.BinaryLength - ace.BinaryLength);

                //verify the removed ace is equal to the originial ace
                Assert.False(!Utils.IsAceEqual(ace, rawAclVerifier[index]));

                //verify right side aces are equal
                Assert.False(!Utils.AclPartialEqual(rawAcl, rawAclVerifier, index, rawAcl.Count - 1, index + 1, count - 1));
            }
            catch (ArgumentOutOfRangeException)
            {
                Assert.True(0 == count);
                //it is expected, do nothing
            }

            //now insert that ace back
            if (ace != null)
                rawAcl.InsertAce(index, ace);
            //test remove at Count/2, do not need to catch ArgumentOutOfRangeException
            index = count / 2;
            //when count/2 = 0 it is reduandent
            if (0 != index)
            {
                //save a copy of the ace
                ace = rawAcl[index];
                rawAcl.RemoveAce(index);
                //verify the count number decrease one
                Assert.False(rawAcl.Count != count - 1);
                //verify the rawAcl.BinaryLength is updated correctly
                Assert.False(rawAcl.BinaryLength != rawAclVerifier.BinaryLength - ace.BinaryLength);

                //verify the removed ace is equal to the originial ace
                Assert.False(!Utils.IsAceEqual(ace, rawAclVerifier[index]));
                //verify the left and right side aces are equal
                Assert.False(!Utils.AclPartialEqual(rawAcl, rawAclVerifier, 0, index - 1, 0, index - 1) || !Utils.AclPartialEqual(rawAcl, rawAclVerifier, index, rawAcl.Count - 1, index + 1, count - 1));

                //now insert that removed ace
                rawAcl.InsertAce(index, ace);
            }

            //test remove at Count - 1, do not need to catch ArgumentOutOfRangeException
            index = count - 1;
            //when count -1 = -1, 0, or count/2, it is reduandent
            if (-1 != index && 0 != index && count / 2 != index)
            {
                //save a copy of the ace
                ace = rawAcl[index];
                rawAcl.RemoveAce(index);
                //verify the count number decrease one
                Assert.False(rawAcl.Count != count - 1);
                //verify the rawAcl.BinaryLength is updated correctly
                Assert.False(rawAcl.BinaryLength != rawAclVerifier.BinaryLength - ace.BinaryLength);

                //verify the removed ace is equal to the originial ace
                Assert.False(!Utils.IsAceEqual(ace, rawAclVerifier[index]));
                //verify the left and right side aces are equal
                Assert.False(!Utils.AclPartialEqual(rawAcl, rawAclVerifier, 0, index - 1, 0, index - 1) || !Utils.AclPartialEqual(rawAcl, rawAclVerifier, index, rawAcl.Count - 1, index + 1, count - 1));

                //now insert that inserted ace

                rawAcl.InsertAce(index, ace);
            }

            //test remove at Count
            index = count;
            //when count  = 0, or count/2, it is reduandent
            if (0 != index && count / 2 != index)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                        ace = rawAcl[index];
                        rawAcl.RemoveAce(index);
                });
            }
        }
    }
}