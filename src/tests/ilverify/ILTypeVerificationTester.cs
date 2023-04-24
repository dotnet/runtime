// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Internal.TypeSystem.Ecma;
using Xunit;

namespace ILVerification.Tests
{
    public class ILTypeVerificationTester
    {
        [Theory(DisplayName = "")]
        [MemberData(nameof(TestDataLoader.GetTypesWithValidType), MemberType = typeof(TestDataLoader))]
        [Trait("", "Valid type implementation tests")]
        public static void TestValidTypes(ValidTypeTestCase validType)
        {
            IEnumerable<VerificationResult> results = Verify(validType);
            Assert.Empty(results);
        }

        [Theory(DisplayName = "")]
        [MemberData(nameof(TestDataLoader.GetTypesWithInvalidType), MemberType = typeof(TestDataLoader))]
        [Trait("", "Invalid type implementation tests")]
        public static void TestInvalidTypes(InvalidTypeTestCase invalidType)
        {
            IEnumerable<VerificationResult> results = null;

            try
            {
                results = Verify(invalidType);
            }
            catch
            {
                //in some cases ILVerify throws exceptions when things look too wrong to continue
                //currently these are not caught. In tests we just catch these and do the asserts.
                //Once these exceptions are better handled and ILVerify instead of crashing aborts the verification
                //gracefully we can remove this empty catch block.
            }
            finally
            {
                Assert.NotNull(results);
                Assert.Equal(invalidType.ExpectedVerifierErrors.Count, results.Count());

                foreach (VerifierError item in invalidType.ExpectedVerifierErrors)
                {
                    IEnumerable<string> actual = results.Select(e => e.Code.ToString());
                    Assert.True(results.Where(r => r.Code == item).Count() > 0, $"Actual errors where: {string.Join(",", actual)}");
                }
            }
        }

        private static IEnumerable<VerificationResult> Verify(TestCase testCase)
        {
            EcmaModule module = TestDataLoader.GetModuleForTestAssembly(testCase.ModuleName);
            var typeHandle = (TypeDefinitionHandle)MetadataTokens.EntityHandle(testCase.MetadataToken);
            var type = (EcmaType)module.GetType(typeHandle);
            var verifier = new Verifier((ILVerifyTypeSystemContext)type.Context, new VerifierOptions
            {
                IncludeMetadataTokensInErrorMessages = true,
                SanityChecks = true
            });
            return verifier.Verify(module.PEReader, typeHandle);
        }
    }
}
