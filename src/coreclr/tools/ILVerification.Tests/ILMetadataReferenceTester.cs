// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Xunit;

namespace ILVerification.Tests
{
    public class ILMetadataReferenceTester
    {
        [Fact]
        public static void ReportsUnusedMetadataReferences()
        {
            (EcmaModule module, VerificationResult[] results) =
                VerifyMetadataReferences("UnusedMetadataReferenceTests.dll");

            var verifier = new Verifier((ILVerifyTypeSystemContext)module.Context, new VerifierOptions());
            Assert.Empty(verifier.Verify(module.PEReader));

            AssertResult(results, HandleKind.AssemblyReference, "ILVerifyAssemblyThatDoesNotExist");
            AssertResult(results, HandleKind.ModuleReference, "ILVerifyManagedModuleThatDoesNotExist.netmodule");
            AssertResult(results, HandleKind.ExportedType, "ILVerifyAssemblyThatDoesNotExist");

            // The P/Invoke ModuleRef and resolvable references should not produce additional errors.
            Assert.Equal(3, results.Length);
        }

        [Fact]
        public static void ReportsInvalidMetadataReferenceKinds()
        {
            (_, VerificationResult[] results) =
                VerifyMetadataReferences("InvalidMetadataReferenceTests.dll");

            Assert.Equal(13, results.Length);
            Assert.Equal(1, results.Count(result => result.MetadataHandle.Kind == HandleKind.AssemblyReference));
            Assert.Equal(6, results.Count(result => result.MetadataHandle.Kind == HandleKind.TypeReference));
            Assert.Equal(2, results.Count(result => result.MetadataHandle.Kind == HandleKind.MemberReference));
            Assert.Equal(1, results.Count(result => result.MetadataHandle.Kind == HandleKind.TypeSpecification));
            Assert.Equal(1, results.Count(result => result.MetadataHandle.Kind == HandleKind.MethodSpecification));
            Assert.Equal(2, results.Count(result => result.MetadataHandle.Kind == HandleKind.StandaloneSignature));

            AssertResult(results, HandleKind.AssemblyReference, "ILVerifyAssemblyThatDoesNotExist");
            AssertResult(results, HandleKind.TypeReference, "ILVerifyAssemblyThatDoesNotExist");
            AssertResult(results, HandleKind.TypeReference, "ILVerifyTypeThatDoesNotExist");
            AssertResult(results, HandleKind.MemberReference, "ILVerifyMethodThatDoesNotExist");
            AssertResult(results, HandleKind.MemberReference, "ILVerifyFieldThatDoesNotExist");
            AssertResult(results, HandleKind.TypeSpecification, "ILVerifyTypeSpecTypeThatDoesNotExist");
            AssertResult(results, HandleKind.MethodSpecification, "ILVerifyMethodSpecTypeThatDoesNotExist");
            AssertResult(results, HandleKind.StandaloneSignature, "ILVerifyStandaloneMethodTypeThatDoesNotExist");
            AssertResult(results, HandleKind.StandaloneSignature, "ILVerifyStandaloneLocalTypeThatDoesNotExist");

            VerificationResult missingAssembly = Assert.Single(results, result =>
                result.MetadataHandle.Kind == HandleKind.AssemblyReference &&
                result.ExceptionID == ExceptionStringID.FileLoadErrorGeneric);
            Assert.Equal(
                new[] { "ILVerifyAssemblyThatDoesNotExist" },
                missingAssembly.GetArgumentValue<string[]>(nameof(TypeSystemException.Arguments)));

            Assert.All(results, result =>
            {
                Assert.False(result.MetadataHandle.IsNil);
                Assert.True(result.Code != VerifierError.None || result.ExceptionID != null);
            });
        }

        private static (EcmaModule Module, VerificationResult[] Results) VerifyMetadataReferences(string assemblyName)
        {
            EcmaModule module = TestDataLoader.GetModuleForTestAssembly(assemblyName);
            var verifier = new Verifier((ILVerifyTypeSystemContext)module.Context, new VerifierOptions());

            return (module, verifier.VerifyMetadataReferences(module.PEReader).ToArray());
        }

        private static void AssertResult(
            VerificationResult[] results,
            HandleKind kind,
            string messagePart)
        {
            Assert.Single(results, result =>
                result.MetadataHandle.Kind == kind &&
                result.Message.Contains(messagePart, StringComparison.Ordinal));
        }

        [Fact]
        public static void AcceptsTypeSpecsWithCustomModifiersOnAClass()
        {
            (EcmaModule module, VerificationResult[] results) =
                VerifyMetadataReferences("TypeSpecModifierTests.dll");
            MetadataReader reader = module.MetadataReader;

            // The 'unmanaged' constraint, modreq(UnmanagedType) class System.ValueType, is accepted.
            TypeSpecificationHandle unmanagedConstraint = Assert.Single(GetTypeSpecifications(reader),
                handle => DescribeTypeSpec(reader, handle) == "modifiers TypeHandle");
            Assert.DoesNotContain(results, result => result.MetadataHandle == (EntityHandle)unmanagedConstraint);

            // A class without custom modifiers is still reported (also through the MemberRef whose parent it is).
            Assert.All(results, result => Assert.Equal(VerifierError.BadTypeSpec, result.Code));
            VerificationResult reported = Assert.Single(results,
                result => result.MetadataHandle.Kind == HandleKind.TypeSpecification);
            Assert.Equal("TypeHandle", DescribeTypeSpec(reader, (TypeSpecificationHandle)reported.MetadataHandle));
        }

        private static IEnumerable<TypeSpecificationHandle> GetTypeSpecifications(MetadataReader reader)
        {
            for (int row = 1; row <= reader.GetTableRowCount(TableIndex.TypeSpec); row++)
            {
                yield return MetadataTokens.TypeSpecificationHandle(row);
            }
        }

        // Returns "modifiers <type code>" for a TypeSpec that starts with custom modifiers, and "<type code>" otherwise.
        private static string DescribeTypeSpec(MetadataReader reader, TypeSpecificationHandle handle)
        {
            BlobReader blob = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
            bool hasCustomModifiers = false;
            SignatureTypeCode typeCode;
            while ((typeCode = blob.ReadSignatureTypeCode()) is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
            {
                blob.ReadTypeHandle();
                hasCustomModifiers = true;
            }

            return hasCustomModifiers ? $"modifiers {typeCode}" : typeCode.ToString();
        }
    }
}
