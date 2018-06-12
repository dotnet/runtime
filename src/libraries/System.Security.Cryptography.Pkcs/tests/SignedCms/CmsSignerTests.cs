// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
	public static partial class CmsSignerTests
	{
        [ActiveIssue(30257)]
		[Fact]
		public static void SignerIdentifierType_InvalidValues()
		{
			CmsSigner signer = new CmsSigner();
			Assert.ThrowsAny<CryptographicException>(() => signer.SignerIdentifierType = SubjectIdentifierType.Unknown);
			Assert.ThrowsAny<CryptographicException>(() => signer.SignerIdentifierType = (SubjectIdentifierType)4);
			Assert.ThrowsAny<CryptographicException>(() => signer.SignerIdentifierType = (SubjectIdentifierType)(-1));
		}
	}
}
