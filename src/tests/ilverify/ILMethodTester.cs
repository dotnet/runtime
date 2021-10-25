// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Internal.TypeSystem.Ecma;
using Xunit;

namespace ILVerification.Tests
{
    public class ILMethodTester
    {
        [Theory(DisplayName = "")]
        [MemberData(nameof(TestDataLoader.GetMethodsWithValidIL), MemberType = typeof(TestDataLoader))]
        [Trait("", "Valid IL Tests")]
        void TestMethodsWithValidIL(ValidILTestCase validIL)
        {
            var results = Verify(validIL);
            Assert.Empty(results);
        }

        [Theory(DisplayName = "")]
        [MemberData(nameof(TestDataLoader.GetMethodsWithInvalidIL), MemberType = typeof(TestDataLoader))]
        [Trait("", "Invalid IL Tests")]
        void TestMethodsWithInvalidIL(InvalidILTestCase invalidIL)
        {
            IEnumerable<VerificationResult> results = null;

            try
            {
                results = Verify(invalidIL);
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
                Assert.Equal(invalidIL.ExpectedVerifierErrors.Count, results.Count());

                foreach (var item in invalidIL.ExpectedVerifierErrors)
                {
                    var actual = results.Select(e => e.Code.ToString());
                    Assert.True(results.Where(r => r.Code == item).Count() > 0, $"Actual errors were: {string.Join(",", actual)}");
                }
            }
        }

        private static IEnumerable<VerificationResult> Verify(TestCase testCase)
        {
            EcmaModule module = TestDataLoader.GetModuleForTestAssembly(testCase.ModuleName);
            var methodHandle = (MethodDefinitionHandle) MetadataTokens.EntityHandle(testCase.MetadataToken);
            var method = (EcmaMethod)module.GetMethod(methodHandle);
            var verifier = new Verifier((ILVerifyTypeSystemContext)method.Context, new VerifierOptions
            {
                IncludeMetadataTokensInErrorMessages = true,
                SanityChecks = true
            });

            return verifier.Verify(module.PEReader, methodHandle);
        }
    }
}
