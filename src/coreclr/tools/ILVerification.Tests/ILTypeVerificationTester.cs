// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
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

        [Fact]
        public static void CyclicTypeSpecBase_ReturnsInvalidMetadataResult()
        {
            using PEReader peReader = GetPatchedCyclicTypeSpecBasePE(out TypeDefinitionHandle typeHandle);
            EcmaModule testModule = TestDataLoader.GetModuleForTestAssembly("MalformedTypeSpecTests.dll");
            var context = (ILVerifyTypeSystemContext)testModule.Context;
            var verifier = new Verifier(context, new VerifierOptions { IncludeMetadataTokensInErrorMessages = true });

            VerificationResult result = Assert.Single(verifier.Verify(peReader, typeHandle));
            Assert.Equal(typeHandle, result.Type);
            Assert.Equal(VerifierError.None, result.Code);
        }

        private static IEnumerable<VerificationResult> Verify(TestCase testCase)
        {
            EcmaModule module = TestDataLoader.GetModuleForTestAssembly(testCase.ModuleName);
            var typeHandle = (TypeDefinitionHandle)MetadataTokens.EntityHandle(testCase.MetadataToken);
            EcmaType type = module.GetType(typeHandle);
            var verifier = new Verifier((ILVerifyTypeSystemContext)type.Context, new VerifierOptions
            {
                IncludeMetadataTokensInErrorMessages = true,
                SanityChecks = true
            });
            return verifier.Verify(module.PEReader, typeHandle);
        }

        private static PEReader GetPatchedCyclicTypeSpecBasePE(out TypeDefinitionHandle typeHandle)
        {
            byte[] image = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Tests", "MalformedTypeSpecTests.dll"));
            ReplaceBytes(
                image,
                new byte[] { 0x15, 0x12, 0x08, 0x01, 0x20, 0x06, 0x08 },
                new byte[] { 0x15, 0x12, 0x08, 0x01, 0x20, 0x0A, 0x08 });

            var peReader = new PEReader(new MemoryStream(image));
            MetadataReader metadataReader = peReader.GetMetadataReader();
            typeHandle = metadataReader.TypeDefinitions.Single(handle => metadataReader.GetString(metadataReader.GetTypeDefinition(handle).Name) == "Extender");
            return peReader;
        }

        private static void ReplaceBytes(byte[] bytes, byte[] search, byte[] replace)
        {
            int index = IndexOf(bytes, search);
            Assert.True(index >= 0, "Expected TypeSpec signature was not found.");
            Assert.True(IndexOf(bytes, search, index + 1) < 0, "Expected TypeSpec signature was not unique.");
            Array.Copy(replace, 0, bytes, index, replace.Length);
        }

        private static int IndexOf(byte[] bytes, byte[] search, int start = 0)
        {
            for (int i = start; i <= bytes.Length - search.Length; i++)
            {
                int j = 0;
                for (; j < search.Length && bytes[i + j] == search[j]; j++) ;
                if (j == search.Length)
                    return i;
            }

            return -1;
        }
    }
}
