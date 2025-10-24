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

            AssertThrowsWithHelpfulMessage(nameof(TypeBuilder.GetConstructor), () => instantiatedType.GetConstructor(Type.EmptyTypes));
            AssertThrowsWithHelpfulMessage(nameof(TypeBuilder.GetConstructor), () => instantiatedType.GetConstructors());
            AssertThrowsWithHelpfulMessage(nameof(TypeBuilder.GetMethod), () => instantiatedType.GetMethod("Test"));
            AssertThrowsWithHelpfulMessage(nameof(TypeBuilder.GetMethod), () => instantiatedType.GetMethods());
            AssertThrowsWithHelpfulMessage(nameof(TypeBuilder.GetField), () => instantiatedType.GetField("_test"));
            AssertThrowsWithHelpfulMessage(nameof(TypeBuilder.GetField), () => instantiatedType.GetFields());

            static void AssertThrowsWithHelpfulMessage(string suggestedAlternative, TestDelegate code)
            {
                 NotSupportedException ex = Assert.Throws<NotSupportedException>(code);
                 Assert.Contains($"TypeBuilder.{suggestedAlternative}", ex.Message);
            }
        }
}
