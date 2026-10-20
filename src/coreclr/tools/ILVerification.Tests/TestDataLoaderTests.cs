// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Xunit;

namespace ILVerification.Tests
{
    public class TestDataLoaderTests
    {
        // The name parts are passed as GetTestMethodsFromDll passes them to the selector. For a special name such as
        // 'special.StoreLocUninit..ctor_Invalid_StackUninit', the first part is already the friendly name.
        [Theory]
        [InlineData("Leave.ToOtherFault", "LeaveOutOfFault.LeaveIntoFault.LeaveIntoHandler", "LeaveIntoFault")]
        [InlineData("StoreLocUninit", "StackUninit", "StackUninit")]
        public static void InvalidILWithUnknownErrorIsRejected(string testName, string errors, string unknownError)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() =>
                TestDataLoader.SelectMethodWithInvalidIL(new[] { testName, "Invalid", errors }, MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal($"Test '{testName}' expects '{unknownError}', which is not a VerifierError.", e.Message);
        }

        [Fact]
        public static void InvalidILWithKnownErrorsIsParsed()
        {
            var testCase = (InvalidILTestCase)TestDataLoader.SelectMethodWithInvalidIL(
                new[] { "Leave.ToOtherFault", "Invalid", "LeaveOutOfFault.LeaveIntoHandler" }, MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal(new[] { VerifierError.LeaveOutOfFault, VerifierError.LeaveIntoHandler }, testCase.ExpectedVerifierErrors);
        }

        [Fact]
        public static void InvalidTypeWithUnknownErrorIsRejected()
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() =>
                TestDataLoader.SelectTypeWithInvalidType(
                    new[] { "MissingMethod", "InvalidType", "InterfaceImplHasDuplicate@InterfaceMethodNotImplemnted" }, MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal("Test 'MissingMethod' expects 'InterfaceMethodNotImplemnted', which is not a VerifierError.", e.Message);
        }

        [Fact]
        public static void InvalidTypeWithKnownErrorsIsParsed()
        {
            var testCase = (InvalidTypeTestCase)TestDataLoader.SelectTypeWithInvalidType(
                new[] { "MissingMethod", "InvalidType", "InterfaceImplHasDuplicate@InterfaceMethodNotImplemented" }, MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(new[] { VerifierError.InterfaceImplHasDuplicate, VerifierError.InterfaceMethodNotImplemented }, testCase.ExpectedVerifierErrors);
        }
    }
}
