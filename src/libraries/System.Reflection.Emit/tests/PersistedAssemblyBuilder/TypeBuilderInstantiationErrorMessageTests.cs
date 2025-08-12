// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderInstantiationErrorMessageTests
    {
        [Fact]
        public void TypeBuilderInstantiation_ThrowsWithHelpfulMessage()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.DefineGenericParameters("T");
            
            Type instantiatedType = type.MakeGenericType(typeof(string));
            
            AssertThrowsWithHelpfulMessage(() => instantiatedType.GetConstructor(Type.EmptyTypes), nameof(TypeBuilder.GetConstructor));
            AssertThrowsWithHelpfulMessage(() => instantiatedType.GetConstructors(), nameof(TypeBuilder.GetConstructor));
            AssertThrowsWithHelpfulMessage(() => instantiatedType.GetConstructors(), nameof(TypeBuilder.GetConstructor));
            AssertThrowsWithHelpfulMessage(() => instantiatedType.GetMethod("Test"), nameof(TypeBuilder.GetMethod));
            AssertThrowsWithHelpfulMessage(() => instantiatedType.GetMethods(), nameof(TypeBuilder.GetMethod));
            AssertThrowsWithHelpfulMessage(() => instantiatedType.GetField("_test"), nameof(TypeBuilder.GetField));
            AssertThrowsWithHelpfulMessage(() => instantiatedType.GetFields(), nameof(TypeBuilder.GetField));

            static void AssertThrowsWithHelpfulMessage(string suggestedAlternative, TestDelegate code)
            {
                 NotSupportedException ex = Assert.Throws<NotSupportedException>(code);
                 Assert.Contains($"TypeBuilder.{suggestedAlternative}", ex.Message);
            }            
        }
}