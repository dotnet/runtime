// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineTypeInitializer
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineTypeInitializer()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            FieldBuilder greetingField = type.DefineField("Greeting", typeof(string), FieldAttributes.Private | FieldAttributes.Static);
            ConstructorBuilder constructor = type.DefineTypeInitializer();

            // Generate IL for the method. The constructor calls its base class
            // constructor. The constructor stores its argument in the private field.
            ILGenerator constructorIlGenerator = constructor.GetILGenerator();
            constructorIlGenerator.Emit(OpCodes.Ldstr, "hello");
            constructorIlGenerator.Emit(OpCodes.Stsfld, greetingField);
            constructorIlGenerator.Emit(OpCodes.Ret);

            Helpers.VerifyConstructor(constructor, type, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName, CallingConventions.Standard, new Type[0]);

            Type createdType = type.CreateType();
            FieldInfo createdField = createdType.GetField("Greeting", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Equal("hello", createdField.GetValue(Activator.CreateInstance(createdType)));
        }

        [Fact]
        public void DefineTypeInitializer_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => type.DefineTypeInitializer());
        }
    }
}
