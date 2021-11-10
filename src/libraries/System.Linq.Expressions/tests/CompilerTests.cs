// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class CompilerTests
    {
        [Theory]
        [ClassData(typeof(CompilationTypes))]
        [OuterLoop("Takes over a minute to complete")]
        public static void CompileDeepTree_NoStackOverflow(bool useInterpreter)
        {
            var e = (Expression)Expression.Constant(0);

            int n = 10000;

            for (var i = 0; i < n; i++)
                e = Expression.Add(e, Expression.Constant(1));

            Func<int> f = Expression.Lambda<Func<int>>(e).Compile(useInterpreter);

            Assert.Equal(n, f());
        }

#if FEATURE_COMPILE
        [Fact]
        public static void EmitConstantsToIL_NonNullableValueTypes()
        {
            VerifyEmitConstantsToIL((bool)true);

            VerifyEmitConstantsToIL((char)'a');

            VerifyEmitConstantsToIL((sbyte)42);
            VerifyEmitConstantsToIL((byte)42);
            VerifyEmitConstantsToIL((short)42);
            VerifyEmitConstantsToIL((ushort)42);
            VerifyEmitConstantsToIL((int)42);
            VerifyEmitConstantsToIL((uint)42);
            VerifyEmitConstantsToIL((long)42);
            VerifyEmitConstantsToIL((ulong)42);

            VerifyEmitConstantsToIL((float)3.14);
            VerifyEmitConstantsToIL((double)3.14);
            VerifyEmitConstantsToIL((decimal)49.95m);
        }

        [Fact]
        public static void EmitConstantsToIL_NullableValueTypes()
        {
            VerifyEmitConstantsToIL((bool?)null);
            VerifyEmitConstantsToIL((bool?)true);

            VerifyEmitConstantsToIL((char?)null);
            VerifyEmitConstantsToIL((char?)'a');

            VerifyEmitConstantsToIL((sbyte?)null);
            VerifyEmitConstantsToIL((sbyte?)42);
            VerifyEmitConstantsToIL((byte?)null);
            VerifyEmitConstantsToIL((byte?)42);
            VerifyEmitConstantsToIL((short?)null);
            VerifyEmitConstantsToIL((short?)42);
            VerifyEmitConstantsToIL((ushort?)null);
            VerifyEmitConstantsToIL((ushort?)42);
            VerifyEmitConstantsToIL((int?)null);
            VerifyEmitConstantsToIL((int?)42);
            VerifyEmitConstantsToIL((uint?)null);
            VerifyEmitConstantsToIL((uint?)42);
            VerifyEmitConstantsToIL((long?)null);
            VerifyEmitConstantsToIL((long?)42);
            VerifyEmitConstantsToIL((ulong?)null);
            VerifyEmitConstantsToIL((ulong?)42);

            VerifyEmitConstantsToIL((float?)null);
            VerifyEmitConstantsToIL((float?)3.14);
            VerifyEmitConstantsToIL((double?)null);
            VerifyEmitConstantsToIL((double?)3.14);
            VerifyEmitConstantsToIL((decimal?)null);
            VerifyEmitConstantsToIL((decimal?)49.95m);

            VerifyEmitConstantsToIL((DateTime?)null);
        }

        [Fact]
        public static void EmitConstantsToIL_ReferenceTypes()
        {
            VerifyEmitConstantsToIL((string)null);
            VerifyEmitConstantsToIL((string)"bar");
        }

        [Fact]
        public static void EmitConstantsToIL_Enums()
        {
            VerifyEmitConstantsToIL(ConstantsEnum.A);
            VerifyEmitConstantsToIL((ConstantsEnum?)null);
            VerifyEmitConstantsToIL((ConstantsEnum?)ConstantsEnum.A);
        }

        [Fact]
        public static void EmitConstantsToIL_ShareReferences()
        {
            var o = new object();
            VerifyEmitConstantsToIL(Expression.Equal(Expression.Constant(o), Expression.Constant(o)), 1, true);
        }

        [Fact]
        public static void EmitConstantsToIL_LiftedToClosure()
        {
            VerifyEmitConstantsToIL(DateTime.Now, 1);
            VerifyEmitConstantsToIL((DateTime?)DateTime.Now, 1);
        }

        [Fact]
        public static void VariableBinder_CatchBlock_Filter1()
        {
            // See https://github.com/dotnet/runtime/issues/18676 for reported issue

            Verify_VariableBinder_CatchBlock_Filter(
                Expression.Catch(
                    Expression.Parameter(typeof(Exception), "ex"),
                    Expression.Empty(),
                    Expression.Parameter(typeof(bool), "???")
                )
            );
        }

        [Fact]
        public static void VariableBinder_CatchBlock_Filter2()
        {
            // See https://github.com/dotnet/runtime/issues/18676 for reported issue

            Verify_VariableBinder_CatchBlock_Filter(
                Expression.Catch(
                    typeof(Exception),
                    Expression.Empty(),
                    Expression.Parameter(typeof(bool), "???")
                )
            );
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public static void VerifyIL_Simple()
        {
            Expression<Func<int>> f = () => Math.Abs(42);

            f.VerifyIL(
                @".method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                  {
                    .maxstack 1

                    IL_0000: ldc.i4.s   42
                    IL_0002: call       int32 class [System.Private.CoreLib]System.Math::Abs(int32)
                    IL_0007: ret
                  }");
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public static void VerifyIL_Exceptions()
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            Expression<Func<int, int>> f =
                Expression.Lambda<Func<int, int>>(
                    Expression.TryCatchFinally(
                        Expression.Call(
                            typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }),
                            Expression.Divide(
                                Expression.Constant(42),
                                x
                            )
                        ),
                        Expression.Empty(),
                        Expression.Catch(
                            typeof(DivideByZeroException),
                            Expression.Constant(-1)
                        )
                    ),
                    x
                );

            f.VerifyIL(
                @".method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32)
                  {
                    .maxstack 4
                    .locals init (
                      [0] int32
                    )

                    .try
                    {
                      .try
                      {
                        IL_0000: ldc.i4.s   42
                        IL_0002: ldarg.1
                        IL_0003: div
                        IL_0004: call       int32 class [System.Private.CoreLib]System.Math::Abs(int32)
                        IL_0009: stloc.0
                        IL_000a: leave      IL_0017
                      }
                      catch (class [System.Private.CoreLib]System.DivideByZeroException)
                      {
                        IL_000f: pop
                        IL_0010: ldc.i4.m1
                        IL_0011: stloc.0
                        IL_0012: leave      IL_0017
                      }
                      IL_0017: leave      IL_001d
                    }
                    finally
                    {
                      IL_001c: endfinally
                    }
                    IL_001d: ldloc.0
                    IL_001e: ret
                  }");
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public static void VerifyIL_Closure1()
        {
            Expression<Func<Func<int>>> f = () => () => 42;

            f.VerifyIL(
                @".method class [System.Private.CoreLib]System.Func`1<int32> ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                  {
                    .maxstack 3

                    IL_0000: ldarg.0
                    IL_0001: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Constants
                    IL_0006: ldc.i4.0
                    IL_0007: ldelem.ref
                    IL_0008: castclass  class [System.Private.CoreLib]System.Reflection.MethodInfo
                    IL_000d: ldtoken    class [System.Private.CoreLib]System.Func`1<int32>
                    IL_0012: call       class [System.Private.CoreLib]System.Type class [System.Private.CoreLib]System.Type::GetTypeFromHandle(valuetype [System.Private.CoreLib]System.RuntimeTypeHandle)
                    IL_0017: ldnull
                    IL_0018: callvirt   instance class [System.Private.CoreLib]System.Delegate class [System.Private.CoreLib]System.Reflection.MethodInfo::CreateDelegate(class [System.Private.CoreLib]System.Type,object)
                    IL_001d: castclass  class [System.Private.CoreLib]System.Func`1<int32>
                    IL_0022: ret
                  }

                  // closure.Constants[0]
                  .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                  {
                    .maxstack 1

                    IL_0000: ldc.i4.s   42
                    IL_0002: ret
                  }",
                appendInnerLambdas: true);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public static void VerifyIL_Closure2()
        {
            Expression<Func<int, Func<int>>> f = x => () => x;

            f.VerifyIL(
                @".method class [System.Private.CoreLib]System.Func`1<int32> ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32)
                  {
                    .maxstack 8
                    .locals init (
                      [0] object[]
                    )

                    IL_0000: ldc.i4.1
                    IL_0001: newarr     object
                    IL_0006: dup
                    IL_0007: ldc.i4.0
                    IL_0008: ldarg.1
                    IL_0009: newobj     instance void class [System.Private.CoreLib]System.Runtime.CompilerServices.StrongBox`1<int32>::.ctor(int32)
                    IL_000e: stelem.ref
                    IL_000f: stloc.0
                    IL_0010: ldarg.0
                    IL_0011: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Constants
                    IL_0016: ldc.i4.0
                    IL_0017: ldelem.ref
                    IL_0018: castclass  class [System.Private.CoreLib]System.Reflection.MethodInfo
                    IL_001d: ldtoken    class [System.Private.CoreLib]System.Func`1<int32>
                    IL_0022: call       class [System.Private.CoreLib]System.Type class [System.Private.CoreLib]System.Type::GetTypeFromHandle(valuetype [System.Private.CoreLib]System.RuntimeTypeHandle)
                    IL_0027: ldnull
                    IL_0028: ldloc.0
                    IL_0029: newobj     instance void class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::.ctor(object[],object[])
                    IL_002e: callvirt   instance class [System.Private.CoreLib]System.Delegate class [System.Private.CoreLib]System.Reflection.MethodInfo::CreateDelegate(class [System.Private.CoreLib]System.Type,object)
                    IL_0033: castclass  class [System.Private.CoreLib]System.Func`1<int32>
                    IL_0038: ret
                  }

                  // closure.Constants[0]
                  .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                  {
                    .maxstack 2
                    .locals init (
                      [0] object[]
                    )

                    IL_0000: ldarg.0
                    IL_0001: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Locals
                    IL_0006: stloc.0
                    IL_0007: ldloc.0
                    IL_0008: ldc.i4.0
                    IL_0009: ldelem.ref
                    IL_000a: castclass  class [System.Private.CoreLib]System.Runtime.CompilerServices.StrongBox`1<int32>
                    IL_000f: ldfld      class [System.Private.CoreLib]System.Runtime.CompilerServices.StrongBox`1<int32>::Value
                    IL_0014: ret
                  }",
                appendInnerLambdas: true);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public static void VerifyIL_Closure3()
        {
            // Using an unchecked addition to ensure that an add instruction is emitted (and not add.ovf)
            Expression<Func<int, Func<int, int>>> f = x => y => unchecked(x + y);

            f.VerifyIL(
                @".method class [System.Private.CoreLib]System.Func`2<int32,int32> ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32)
                  {
                    .maxstack 8
                    .locals init (
                      [0] object[]
                    )

                    IL_0000: ldc.i4.1
                    IL_0001: newarr     object
                    IL_0006: dup
                    IL_0007: ldc.i4.0
                    IL_0008: ldarg.1
                    IL_0009: newobj     instance void class [System.Private.CoreLib]System.Runtime.CompilerServices.StrongBox`1<int32>::.ctor(int32)
                    IL_000e: stelem.ref
                    IL_000f: stloc.0
                    IL_0010: ldarg.0
                    IL_0011: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Constants
                    IL_0016: ldc.i4.0
                    IL_0017: ldelem.ref
                    IL_0018: castclass  class [System.Private.CoreLib]System.Reflection.MethodInfo
                    IL_001d: ldtoken    class [System.Private.CoreLib]System.Func`2<int32,int32>
                    IL_0022: call       class [System.Private.CoreLib]System.Type class [System.Private.CoreLib]System.Type::GetTypeFromHandle(valuetype [System.Private.CoreLib]System.RuntimeTypeHandle)
                    IL_0027: ldnull
                    IL_0028: ldloc.0
                    IL_0029: newobj     instance void class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::.ctor(object[],object[])
                    IL_002e: callvirt   instance class [System.Private.CoreLib]System.Delegate class [System.Private.CoreLib]System.Reflection.MethodInfo::CreateDelegate(class [System.Private.CoreLib]System.Type,object)
                    IL_0033: castclass  class [System.Private.CoreLib]System.Func`2<int32,int32>
                    IL_0038: ret
                  }

                  // closure.Constants[0]
                  .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32)
                  {
                    .maxstack 2
                    .locals init (
                      [0] object[]
                    )

                    IL_0000: ldarg.0
                    IL_0001: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Locals
                    IL_0006: stloc.0
                    IL_0007: ldloc.0
                    IL_0008: ldc.i4.0
                    IL_0009: ldelem.ref
                    IL_000a: castclass  class [System.Private.CoreLib]System.Runtime.CompilerServices.StrongBox`1<int32>
                    IL_000f: ldfld      class [System.Private.CoreLib]System.Runtime.CompilerServices.StrongBox`1<int32>::Value
                    IL_0014: ldarg.1
                    IL_0015: add
                    IL_0016: ret
                  }",
                appendInnerLambdas: true);
        }

        internal static void VerifyIL(this LambdaExpression expression, string expected, bool appendInnerLambdas = false)
        {
            string actual = expression.GetIL(appendInnerLambdas);

            string nExpected = Normalize(expected);
            string nActual = Normalize(actual);

            Assert.Equal(nExpected, nActual);
        }

        private static string Normalize(string s)
        {
            Collections.Generic.IEnumerable<string> lines =
                s
                .Replace("\r\n", "\n")
                .Split(new[] { '\n' })
                .Select(line => line.Trim())
                .Where(line => line != "" && !line.StartsWith("//"));

            string beforeLambdaUniquifierRemoval = string.Join("\n", lines);
            return Regex.Replace(beforeLambdaUniquifierRemoval, "lambda_method[0-9]*", "lambda_method");
        }

        private static void VerifyEmitConstantsToIL<T>(T value)
        {
            VerifyEmitConstantsToIL<T>(value, 0);
        }

        private static void VerifyEmitConstantsToIL<T>(T value, int expectedCount)
        {
            VerifyEmitConstantsToIL(Expression.Constant(value, typeof(T)), expectedCount, value);
        }

        private static void VerifyEmitConstantsToIL(Expression e, int expectedCount, object expectedValue)
        {
            Delegate f = Expression.Lambda(e).Compile();

            var c = f.Target as Closure;
            Assert.NotNull(c);
            Assert.Equal(expectedCount, c.Constants.Length);

            object o = f.DynamicInvoke();
            Assert.Equal(expectedValue, o);
        }

        private static void Verify_VariableBinder_CatchBlock_Filter(CatchBlock @catch)
        {
            Expression<Action> e =
                Expression.Lambda<Action>(
                    Expression.TryCatch(
                        Expression.Empty(),
                        @catch
                    )
                );

            Assert.Throws<InvalidOperationException>(() => e.Compile());
        }
#endif
    }

    public enum ConstantsEnum
    {
        A
    }
}
