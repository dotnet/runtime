// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public partial class DefTypeTests
    {
        private ModuleDesc _testModule;

        public DefTypeTests()
        {
            var context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = context.CreateModuleForSimpleName("CoreTestAssembly");
            context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Theory]
        [InlineData("SimpleByteClass")]
        [InlineData("SimpleInt16Class")]
        [InlineData("SimpleInt32Class")]
        [InlineData("SimpleInt64Class")]
        [InlineData("ExplicitByteBase")]
        [InlineData("ExplicitInt16Base")]
        [InlineData("ExplicitInt32Base")]
        [InlineData("ExplicitInt64Base")]
        [InlineData("SequentialByteBase")]
        [InlineData("SequentialInt16Base")]
        [InlineData("SequentialInt32Base")]
        [InlineData("SequentialInt64Base")]
        public void IsZeroSizedReferenceType_NonEmptyType_ReturnsFalse(string className)
        {
            DefType nonEmptyClass = _testModule.GetType("Marshalling", className);
            Assert.False(nonEmptyClass.IsZeroSizedReferenceType);
        }

        [Theory]
        [InlineData("SimpleEmptyClass")]
        [InlineData("ExplicitEmptyBase")]
        [InlineData("ExplicitEmptySizeZeroBase")]
        [InlineData("SequentialEmptyBase")]
        public void IsZeroSizedReferenceType_EmptyType_ReturnsTrue(string className)
        {
            DefType emptyClass = _testModule.GetType("Marshalling", className);
            Assert.True(emptyClass.IsZeroSizedReferenceType);
        }
    }
}
