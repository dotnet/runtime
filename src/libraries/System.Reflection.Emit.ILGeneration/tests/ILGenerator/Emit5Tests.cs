// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ILGeneratorEmit5
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63805", TestRuntimes.Mono)]
        public void MaxStackOverflowTest()
        {
            Run(1 << 5);
            Run(1 << 10);

            // Previously this threw because the computed stack depth was 2^16 + 1, which is 1 mod 2^16
            // and 1 is too small.
            Run(1 << 14);

            static void Run(int num)
            {
                var meth = GetCode(num);
                Assert.NotNull(meth);
                Assert.Equal(typeof(int), meth.ReturnType);

                var body = meth.GetMethodBody();
                Assert.Equal(4, body.MaxStackSize); // Previously the depth was computed as 4 * num + 1.

                var val = (int)meth.Invoke(null, null);
                Assert.Equal(4 * num, val);
            }

            /// <summary>
            /// The <paramref name="num"/> parameter is the number of basic blocks. Each has a max stack
            /// depth of four. There is one final basic block with max stack of one. The ILGenerator
            /// erroneously adds these, so the final value can overflow 2^16. When that result mod 2^16
            /// is less than required, the CLR throws an <see cref="InvalidProgramException"/>.
            /// </summary>
            static MethodInfo GetCode(int num)
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                var ilg = method.GetILGenerator();

                var loc = ilg.DeclareLocal(typeof(int));
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Stloc, loc);

                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Ldloc, loc);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_2);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Stloc, loc);

                    // Unconditional jump to next block.
                    var labNext = ilg.DefineLabel();
                    ilg.Emit(OpCodes.Br, labNext);
                    ilg.MarkLabel(labNext);
                }

                ilg.Emit(OpCodes.Ldloc, loc);
                ilg.Emit(OpCodes.Ret);

                // Create the type where this method is in
                Type createdType = type.CreateType();
                MethodInfo createdMethod = createdType.GetMethod("meth1");

                return createdMethod;
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63805", TestRuntimes.Mono)]
        public void MaxStackNonEmptyForward()
        {
            // This test uses forward branches to "new" basic blocks where the stack depth
            // at the branch location is non-empty.

            Run(1 << 0);
            Run(1 << 1);
            Run(1 << 5);

            // This one seems to overwhelm the jit or something.
            // Run(1 << 10);

            void Run(int num)
            {
                var meth = GetCode(num);
                Assert.NotNull(meth);
                Assert.Equal(typeof(int), meth.ReturnType);

                var body = meth.GetMethodBody();
                Assert.Equal(2 * num + 3, body.MaxStackSize); // Previously the depth was computed as 4 * num + 1.

                var val = (int)meth.Invoke(null, null);
                Assert.Equal(4 * num, val);
            }

            static MethodInfo GetCode(int num)
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                var ilg = method.GetILGenerator();

                ilg.Emit(OpCodes.Ldc_I4_0);
                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);

                    // Unconditional jump to next block.
                    var labNext = ilg.DefineLabel();
                    ilg.Emit(OpCodes.Br, labNext);
                    ilg.MarkLabel(labNext);
                }

                // Each block leaves two values on the stack. Add them into the previous value.
                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                }

                ilg.Emit(OpCodes.Ret);

                // Create the type where this method is in
                Type createdType = type.CreateType();
                return createdType.GetMethod("meth1");
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63805", TestRuntimes.Mono)]
        public void MaxStackNonEmptyBackward()
        {
            // This test uses backward branches to "new" basic blocks where the stack depth
            // at the branch location is non-empty.

            Run(1 << 1);
            Run(1 << 2);
            Run(1 << 3);
            Run(1 << 4);
            Run(1 << 5);

            // This one seems to overwhelm the jit or something.
            // Run(1 << 10);

            void Run(int num)
            {
                var meth = GetCode(num);
                Assert.NotNull(meth);
                Assert.Equal(typeof(int), meth.ReturnType);

                var body = meth.GetMethodBody();
                Assert.Equal(4 * num + 2, body.MaxStackSize);

                var val = (int)meth.Invoke(null, null);
                Assert.Equal(4 * num, val);
            }

            static MethodInfo GetCode(int num)
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                var ilg = method.GetILGenerator();

                var labels = new Label[num + 1];
                for (int i = 0; i <= num; i++)
                    labels[i] = ilg.DefineLabel();

                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Br, labels[0]);

                for (int i = num; --i >= 0;)
                {
                    ilg.MarkLabel(labels[i]);

                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);

                    // Unconditional jump to "next" block (which is really before this code).
                    ilg.Emit(OpCodes.Br, labels[i + 1]);
                }

                ilg.MarkLabel(labels[num]);

                // Each block leaves two values on the stack. Add them into the previous value.
                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                }

                ilg.Emit(OpCodes.Ret);

                // Create the type where this method is in
                Type createdType = type.CreateType();
                return createdType.GetMethod("meth1");
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63805", TestRuntimes.Mono)]
        public void AmbiguousDepth()
        {
            var meth = GetCode();
            Assert.NotNull(meth);
            Assert.Equal(typeof(int), meth.ReturnType);

            var body = meth.GetMethodBody();
            // Observed depth of 2, with "adjustment" of 1.
            Assert.Equal(2 + 1, body.MaxStackSize);

            try
            {
                meth.Invoke(null, new object[] { false });
                Assert.True(false);
            }
            catch (TargetInvocationException ex)
            {
                Assert.IsType<InvalidProgramException>(ex.InnerException);
            }

            static MethodInfo GetCode()
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[] { typeof(bool) });
                var ilg = method.GetILGenerator();

                // The label is targeted with stack depth zero.
                var lab = ilg.DefineLabel();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Brfalse, lab);

                // The label is marked with a larger stack depth, one. This IL is invalid.
                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.MarkLabel(lab);

                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.Emit(OpCodes.Add);
                ilg.Emit(OpCodes.Ret);

                // Create the type where this method is in
                Type createdType = type.CreateType();
                return createdType.GetMethod("meth1");
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63805", TestRuntimes.Mono)]
        public void UnreachableDepth()
        {
            var meth = GetCode();
            Assert.NotNull(meth);
            Assert.Equal(typeof(int), meth.ReturnType);

            var body = meth.GetMethodBody();
            // Observed depth of 2, with no "adjustment".
            Assert.Equal(2, body.MaxStackSize);

            var val = (int)meth.Invoke(null, null);
            Assert.Equal(2, val);

            static MethodInfo GetCode()
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                var ilg = method.GetILGenerator();

                var lab = ilg.DefineLabel();

                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.Emit(OpCodes.Br, lab);

                // Unreachable.
                ilg.Emit(OpCodes.Ldarg_0);

                // Depth 
                ilg.MarkLabel(lab);
                ilg.Emit(OpCodes.Add);
                ilg.Emit(OpCodes.Ret);

                // Create the type where this method is in
                Type createdType = type.CreateType();
                return createdType.GetMethod("meth1");
            }
        }
    }
}
