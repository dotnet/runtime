// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public abstract class GenericAce_Tests
    {
        public void GenericAce_VerifyAces(GenericAce expectedAce, GenericAce resultAce)
        {
            Assert.Equal(expectedAce.AceType, resultAce.AceType);
            Assert.Equal(expectedAce.AceFlags, resultAce.AceFlags);
            Assert.Equal(expectedAce.AuditFlags, resultAce.AuditFlags);
            Assert.Equal(expectedAce.BinaryLength, resultAce.BinaryLength);
            Assert.Equal(expectedAce.InheritanceFlags, resultAce.InheritanceFlags);
            Assert.Equal(expectedAce.IsInherited, resultAce.IsInherited);
            Assert.Equal(expectedAce.PropagationFlags, resultAce.PropagationFlags);
        }

        public void GenericAce_VerifyBinaryForms(byte[] expectedBinaryForm, byte[] resultBinaryForm, int resultOffset)
        {
            Assert.Equal(expectedBinaryForm, resultBinaryForm.Skip(resultOffset));
        }
    }
}
