// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ILGeneratorDefineLabel
    {
        [Fact]
        public void DefineLabel_DoesNotThrow()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), new Type[0]);
            ILGenerator ilGenerator = method.GetILGenerator();
            
            // We use labels in other tests in the code so no need to verify that they were placed correctly here.
            for (int i = 0; i < 17; ++i)
            {
                ilGenerator.DefineLabel();
            }
        }
    }
}
