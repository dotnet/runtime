// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Security.Claims
{
    public class ClaimTests
    {
        [Fact]
        public void Ctor_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new Claim(null));
        }

        [Fact]
        public void BinaryWriteReadTest_Success()
        {
            var claim = new Claim(ClaimTypes.Actor, "value", ClaimValueTypes.String, "issuer", "originalIssuer");
            claim.Properties.Add("key1", "val1");
            claim.Properties.Add("key2", "val2");

            Claim clonedClaim = null;
            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream, Encoding.Default, true))
                {
                    claim.WriteTo(binaryWriter);
                    binaryWriter.Flush();
                }

                memoryStream.Position = 0;
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    clonedClaim = new Claim(binaryReader);
                }
            }

            Assert.Equal(claim.Type, clonedClaim.Type);
            Assert.Equal(claim.Value, clonedClaim.Value);
            Assert.Equal(claim.ValueType, clonedClaim.ValueType);
            Assert.Equal(claim.Issuer, clonedClaim.Issuer);
            Assert.Equal(claim.OriginalIssuer, clonedClaim.OriginalIssuer);
            Assert.Equal(claim.Properties.Count, clonedClaim.Properties.Count);
            Assert.Equal(claim.Properties.ElementAt(0), clonedClaim.Properties.ElementAt(0));
            Assert.Equal(claim.Properties.ElementAt(1), clonedClaim.Properties.ElementAt(1));
        }
    }
}
