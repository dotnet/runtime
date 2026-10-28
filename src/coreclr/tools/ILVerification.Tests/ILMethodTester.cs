// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Xunit;

namespace ILVerification.Tests
{
    public class ILMethodTester
    {
        [Theory(DisplayName = "")]
        [MemberData(nameof(TestDataLoader.GetMethodsWithValidIL), MemberType = typeof(TestDataLoader))]
        [Trait("", "Valid IL Tests")]
        public static void TestMethodsWithValidIL(ValidILTestCase validIL)
        {
            var results = Verify(validIL);
            Assert.Empty(results);
        }

        [Theory(DisplayName = "")]
        [MemberData(nameof(TestDataLoader.GetMethodsWithInvalidIL), MemberType = typeof(TestDataLoader))]
        [Trait("", "Invalid IL Tests")]
        public static void TestMethodsWithInvalidIL(InvalidILTestCase invalidIL)
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
            EcmaModule module = TestDataLoader.GetModuleForTestAssembly(testCase.ModuleName, allowIgnoresAccessChecksTo: true);
            var methodHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(testCase.MetadataToken);
            var method = module.GetMethod(methodHandle);
            var verifier = new Verifier((ILVerifyTypeSystemContext)method.Context, new VerifierOptions
            {
                IncludeMetadataTokensInErrorMessages = true,
                SanityChecks = true
            });

            return verifier.Verify(module.PEReader, methodHandle);
        }

        [Fact]
        public static void IgnoresAccessChecksToIsOffByDefault()
        {
            EcmaModule module = TestDataLoader.GetModuleForTestAssembly("AccessTestsIgnoreChecks");
            MetadataReader reader = module.MetadataReader;
            MethodDefinitionHandle methodHandle = reader.MethodDefinitions
                .First(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "Load.PrivateField_Valid");
            var verifier = new Verifier((ILVerifyTypeSystemContext)module.Context, new VerifierOptions());

            Assert.Contains(verifier.Verify(module.PEReader, methodHandle), r => r.Code == VerifierError.FieldAccess);
        }

        [Fact]
        public static void UnknownSyntheticCallCannotProveReturnLifetime()
        {
            EcmaModule module = TestDataLoader.GetModuleForTestAssembly("ReturnTests");
            MetadataReader reader = module.MetadataReader;
            MethodDefinitionHandle handle = reader.MethodDefinitions
                .First(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "Return.RefIdentity_Valid");
            EcmaMethod caller = module.GetMethod(handle);
            var callee = new SyntheticMethod(caller.OwningType, new MethodSignature(
                MethodSignatureFlags.Static, 0, caller.Signature.ReturnType, TypeDesc.EmptyTypes));
            var errors = new List<VerifierError>();
            var importer = new ILImporter(caller, new SyntheticCallIL(caller, callee))
            {
                SanityChecks = true,
                ReportVerificationError = (_, error) => errors.Add(error)
            };

            importer.Verify();

            Assert.Equal(new[] { VerifierError.Unverifiable, VerifierError.ReturnPtrToStack }, errors);
        }

        private sealed class SyntheticMethod : MethodDesc
        {
            public SyntheticMethod(TypeDesc owningType, MethodSignature signature)
            {
                OwningType = owningType;
                Signature = signature;
            }

            public override TypeSystemContext Context => OwningType.Context;
            public override TypeDesc OwningType { get; }
            public override MethodSignature Signature { get; }
            public override Utf8Span Name => "Synthetic"u8;
            public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
        }

        private sealed class SyntheticCallIL : MethodIL
        {
            private readonly MethodDesc _callee;

            public SyntheticCallIL(MethodDesc caller, MethodDesc callee)
            {
                OwningMethod = caller;
                _callee = callee;
            }

            public override MethodDesc OwningMethod { get; }
            public override int MaxStack => 1;
            public override bool IsInitLocals => true;
            public override byte[] GetILBytes() => [(byte)ILOpcode.call, 1, 0, 0, 6, (byte)ILOpcode.ret];
            public override LocalVariableDefinition[] GetLocals() => Array.Empty<LocalVariableDefinition>();
            public override ILExceptionRegion[] GetExceptionRegions() => Array.Empty<ILExceptionRegion>();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw) => _callee;
        }
    }
}
