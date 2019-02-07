// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class StackSpillerTests
    {
        [Fact]
        public static void Spill_Binary()
        {
            Test((l, r) => Expression.Add(l, r), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Binary_IndexAssign()
        {
            Reflection.PropertyInfo item = typeof(Dictionary<int, int>).GetProperty("Item");

            Test((d, i, v) =>
            {
                IndexExpression index = Expression.Property(d, item, i);

                return
                    Expression.Block(
                        Expression.Assign(index, v),
                        index
                    );
            }, Expression.Constant(new Dictionary<int, int>()), Expression.Constant(0), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_Binary_MemberAssign()
        {
            Reflection.PropertyInfo baz = typeof(Bar).GetProperty(nameof(Bar.Baz));

            Test((l, r) =>
            {
                MemberExpression prop = Expression.Property(l, baz);

                return
                    Expression.Block(
                        Expression.Assign(prop, r),
                        prop
                    );
            }, Expression.Constant(new Bar()), Expression.Constant(42));
        }

        [Fact]
        public static void Spill_TryFinally()
        {
            Test(a => Expression.TryFinally(a, Expression.Empty()), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_TryCatch()
        {
            foreach (var div in new[] { 1, 0 })
            {
                Test((n, d, a) => Expression.TryCatch(Expression.Divide(n, d), Expression.Catch(typeof(DivideByZeroException), a)), Expression.Constant(1), Expression.Constant(div), Expression.Constant(42));
            }
        }

        [Fact]
        public static void Spill_Loop()
        {
            Test((a, b) =>
            {
                LabelTarget @break = Expression.Label(typeof(int));
                return Expression.Add(a, Expression.Loop(Expression.Break(@break, b), @break));
            }, Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Member()
        {
            Test(s => Expression.Property(s, nameof(string.Length)), Expression.Constant("bar"));
        }

        [Fact]
        public static void Spill_TypeBinary()
        {
            Test(s => Expression.TypeIs(s, typeof(string)), Expression.Constant("bar", typeof(object)));
            Test(s => Expression.TypeEqual(s, typeof(string)), Expression.Constant("bar", typeof(object)));
        }

        [Fact]
        public static void Spill_Binary_Logical()
        {
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(false), Expression.Constant(false));
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(false), Expression.Constant(true));
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(true), Expression.Constant(false));
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(true), Expression.Constant(true));
        }

        [Fact]
        public static void Spill_Conditional()
        {
            Test((t, l, r) => Expression.Condition(t, l, r), Expression.Constant(false), Expression.Constant(1), Expression.Constant(2));
            Test((t, l, r) => Expression.Condition(t, l, r), Expression.Constant(true), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Index()
        {
            Reflection.PropertyInfo item = typeof(Dictionary<int, int>).GetProperty("Item");

            Test((d, i) => Expression.MakeIndex(d, item, new[] { i }), Expression.Constant(new Dictionary<int, int> { { 1, 2 } }), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_Call_Static()
        {
            Reflection.MethodInfo max = typeof(Math).GetMethod(nameof(Math.Max), new[] { typeof(int), typeof(int) });

            Test((x, y) => Expression.Call(max, x, y), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Call_Instance()
        {
            Reflection.MethodInfo substring = typeof(string).GetMethod(nameof(string.Substring), new[] { typeof(int), typeof(int) });

            Test((s, i, j) => Expression.Call(s, substring, i, j), Expression.Constant("foobar"), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Invocation()
        {
            Test((d, a, b) => Expression.Invoke(d, a, b), Expression.Constant(new Func<int, int, int>((x, y) => x + y)), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Invocation_Inline()
        {
            ParameterExpression x = Expression.Parameter(typeof(int));
            ParameterExpression y = Expression.Parameter(typeof(int));

            Test((a, b) => Expression.Invoke(Expression.Lambda(Expression.Subtract(x, y), x, y), a, b), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_New()
        {
            Reflection.ConstructorInfo ctor = typeof(TimeSpan).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });

            Test((h, m, s) => Expression.New(ctor, h, m, s), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
        }

        [Fact]
        public static void Spill_NewArrayInit()
        {
            Test((a, b, c) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(int[]));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.NewArrayInit(typeof(int), a, b, c)
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.ArrayIndex(p, Expression.Constant(0)),
                                Expression.ArrayIndex(p, Expression.Constant(1))
                            ),
                            Expression.ArrayIndex(p, Expression.Constant(2))
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_NewArrayBounds()
        {
            Reflection.MethodInfo getUpperBound = typeof(Array).GetMethod(nameof(Array.GetUpperBound));

            Test((a, b, c) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(int[,,]));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.NewArrayBounds(typeof(int), a, b, c)
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Call(p, getUpperBound, Expression.Constant(0)),
                                Expression.Call(p, getUpperBound, Expression.Constant(1))
                            ),
                            Expression.Call(p, getUpperBound, Expression.Constant(2))
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_ListInit()
        {
            Reflection.PropertyInfo item = typeof(List<int>).GetProperty("Item");

            Test((a, b, c) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(List<int>));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.ListInit(Expression.New(typeof(List<int>)), a, b, c)
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.MakeIndex(p, item, new[] { Expression.Constant(0) }),
                                Expression.MakeIndex(p, item, new[] { Expression.Constant(1) })
                            ),
                            Expression.MakeIndex(p, item, new[] { Expression.Constant(2) })
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_MemberInit_Bind()
        {
            Reflection.PropertyInfo baz = typeof(Bar).GetProperty(nameof(Bar.Baz));
            Reflection.PropertyInfo foo = typeof(Bar).GetProperty(nameof(Bar.Foo));
            Reflection.PropertyInfo qux = typeof(Bar).GetProperty(nameof(Bar.Qux));

            Test((a, b, c) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(Bar));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.MemberInit(
                                Expression.New(typeof(Bar)),
                                Expression.Bind(baz, a),
                                Expression.Bind(foo, b),
                                Expression.Bind(qux, c)
                            )
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Property(p, baz),
                                Expression.Property(p, foo)
                            ),
                            Expression.Property(p, qux)
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_MemberInit_MemberBind()
        {
            Reflection.PropertyInfo bar = typeof(BarHolder).GetProperty(nameof(BarHolder.Bar));
            Reflection.PropertyInfo baz = typeof(Bar).GetProperty(nameof(Bar.Baz));
            Reflection.PropertyInfo foo = typeof(Bar).GetProperty(nameof(Bar.Foo));
            Reflection.PropertyInfo qux = typeof(Bar).GetProperty(nameof(Bar.Qux));

            Test((a, b, c) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(BarHolder));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.MemberInit(
                                Expression.New(typeof(BarHolder)),
                                Expression.MemberBind(
                                    bar,
                                    Expression.Bind(baz, a),
                                    Expression.Bind(foo, b),
                                    Expression.Bind(qux, c)
                                )
                            )
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Property(Expression.Property(p, bar), baz),
                                Expression.Property(Expression.Property(p, bar), foo)
                            ),
                            Expression.Property(Expression.Property(p, bar), qux)
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_MemberInit_ListBind()
        {
            Reflection.PropertyInfo xs = typeof(ListHolder).GetProperty(nameof(ListHolder.Xs));
            Reflection.MethodInfo add = typeof(List<int>).GetMethod(nameof(List<int>.Add));
            Reflection.PropertyInfo item = typeof(List<int>).GetProperty("Item");

            Test((a, b, c) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(ListHolder));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.MemberInit(
                                Expression.New(typeof(ListHolder)),
                                Expression.ListBind(
                                    xs,
                                    Expression.ElementInit(add, a),
                                    Expression.ElementInit(add, b),
                                    Expression.ElementInit(add, c)
                                )
                            )
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Property(Expression.Property(p, xs), item, Expression.Constant(0)),
                                Expression.Property(Expression.Property(p, xs), item, Expression.Constant(1))
                            ),
                            Expression.Property(Expression.Property(p, xs), item, Expression.Constant(2))
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_Switch()
        {
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(a, Expression.Constant(1))), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(a, Expression.Constant(7))), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(Expression.Constant(7), a)), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(Expression.Constant(7), a)), Expression.Constant(1), Expression.Constant(2), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_Block()
        {
            Test((a, b) => Expression.Block(a, b), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_LabelGoto()
        {
            foreach (var t in new[] { true, false })
            {
                Test((c, a, b) =>
                {
                    LabelTarget lbl = Expression.Label(typeof(int));

                    return
                        Expression.Block(
                            Expression.IfThen(c, Expression.Goto(lbl, a)),
                            Expression.Label(lbl, b)
                        );
                }, Expression.Constant(t), Expression.Constant(1), Expression.Constant(2));
            }
        }

        private static Expression<Func<int>> Spill_RefInstance_IndexAssignment()
        {
            ParameterExpression v = Expression.Parameter(typeof(ValueVector));
            Reflection.PropertyInfo item = typeof(ValueVector).GetProperty("Item");
            ConstantExpression i = Expression.Constant(0);
            IndexExpression p = Expression.Property(v, item, i);
            ConstantExpression x = Expression.Constant(42);

            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Block(
                        new[] { v },
                        Expression.Assign(p, Spill(x)),
                        p
                    )
                );

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefInstance_IndexAssignment_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefInstance_IndexAssignment();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Spill_RefInstance_IndexAssignment_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefInstance_IndexAssignment();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 4
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueVector&,
                        [1] int32,
                        [2] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueVector,
                        [3] int32
                      )

                      // V0 = ref v                  // spill reference to v [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // v[0] = try { 42 }           // using spilled reference in V0
                      IL_000e: ldloc.0    
                      IL_000f: ldc.i4.0   
                      IL_0010: ldloc.1    
                      IL_0011: call       instance void valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueVector::set_Item(int32,int32)

                      // return v[0]
                      IL_0016: ldloca.s   V_2
                      IL_0018: ldc.i4.0   
                      IL_0019: call       instance int32 valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueVector::get_Item(int32)
                      IL_001e: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 5
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: LoadLocal(0)
                      IP_0002: LoadObject(0)
                      .try
                      {
                        IP_0003: EnterTryFinally[0] -> 6
                        IP_0004: LoadObject(42)
                        IP_0005: Goto[1] -> 8
                      }
                      finally
                      {
                        IP_0006: EnterFinally[0] -> 6
                        IP_0007: LeaveFinally()
                      }
                      IP_0008: Call(Void set_Item(Int32, Int32))
                      IP_0009: LoadLocal(0)
                      IP_0010: LoadObject(0)
                      IP_0011: Call(Int32 get_Item(Int32))
                    }");
        }

        private static Expression<Func<int>> Spill_RefInstance_Index()
        {
            ParameterExpression v = Expression.Parameter(typeof(ValueBar));
            Reflection.PropertyInfo item = typeof(ValueBar).GetProperty("Item");
            ConstantExpression x = Expression.Constant(42);

            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Block(
                        new[] { v },
                        Expression.Property(v, item, Spill(x))
                    )
                );

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefInstance_Index_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefInstance_Index();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Spill_RefInstance_Index_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefInstance_Index();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar&,
                        [1] int32,
                        [2] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [3] int32
                      )

                      // V0 = ref v                  // spill reference to v [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // return v[try { 42 }]        // using spilled reference in V0
                      IL_000e: ldloc.0    
                      IL_000f: ldloc.1    
                      IL_0010: call       instance int32 valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::get_Item(int32)
                      IL_0015: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 4
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: LoadLocal(0)
                      .try
                      {
                        IP_0002: EnterTryFinally[0] -> 5
                        IP_0003: LoadObject(42)
                        IP_0004: Goto[1] -> 7
                      }
                      finally
                      {
                        IP_0005: EnterFinally[0] -> 5
                        IP_0006: LeaveFinally()
                      }
                      IP_0007: Call(Int32 get_Item(Int32))
                    }");
        }

        private static Expression<Func<int>> Spill_RefInstance_MemberAssignment()
        {
            ParameterExpression v = Expression.Parameter(typeof(ValueBar));
            Reflection.PropertyInfo foo = typeof(ValueBar).GetProperty(nameof(ValueBar.Foo));
            ConstantExpression x = Expression.Constant(42);
            MemberExpression p = Expression.Property(v, foo);

            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Block(
                        new[] { v },
                        Expression.Assign(p, Spill(x)),
                        p
                    )
                );

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefInstance_MemberAssignment_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefInstance_MemberAssignment();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Spill_RefInstance_MemberAssignment_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefInstance_MemberAssignment();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar&,
                        [1] int32,
                        [2] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [3] int32
                      )

                      // V0 = ref v                  // spill reference to v [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // v.Foo = try { 42 }          // using spilled reference in V0
                      IL_000e: ldloc.0    
                      IL_000f: ldloc.1    
                      IL_0010: call       instance void valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::set_Foo(int32)

                      // return v.Foo
                      IL_0015: ldloca.s   V_2
                      IL_0017: call       instance int32 valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::get_Foo()
                      IL_001c: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 4
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: LoadLocal(0)
                      .try
                      {
                        IP_0002: EnterTryFinally[0] -> 5
                        IP_0003: LoadObject(42)
                        IP_0004: Goto[1] -> 7
                      }
                      finally
                      {
                        IP_0005: EnterFinally[0] -> 5
                        IP_0006: LeaveFinally()
                      }
                      IP_0007: Call(Void set_Foo(Int32))
                      IP_0008: LoadLocal(0)
                      IP_0009: Call(Int32 get_Foo())
                    }");
        }

        private static Expression<Func<int>> Spill_RefInstance_Call()
        {
            ParameterExpression v = Expression.Parameter(typeof(ValueBar));

            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Block(
                        new[] { v },
                        Expression.Call(
                            v,
                            typeof(ValueBar).GetMethod(nameof(ValueBar.Qux)),
                            Spill(Expression.Constant(42))
                        ),
                        Expression.Property(
                            v,
                            typeof(ValueBar).GetProperty(nameof(ValueBar.Foo))
                        )
                    )
                );

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefInstance_Call_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefInstance_Call();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Spill_RefInstance_Call_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefInstance_Call();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar&,
                        [1] int32,
                        [2] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [3] int32
                      )

                      // V0 = ref v                  // spill reference to v [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // V1 = try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // v.Qux(try { 42 })           // using spilled reference in V0
                      IL_000e: ldloc.0    
                      IL_000f: ldloc.1    
                      IL_0010: call       instance void valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::Qux(int32)

                      // return v.Foo
                      IL_0015: ldloca.s   V_2
                      IL_0017: call       instance int32 valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::get_Foo()
                      IL_001c: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 4
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: LoadLocal(0)
                      .try
                      {
                        IP_0002: EnterTryFinally[0] -> 5
                        IP_0003: LoadObject(42)
                        IP_0004: Goto[1] -> 7
                      }
                      finally
                      {
                        IP_0005: EnterFinally[0] -> 5
                        IP_0006: LeaveFinally()
                      }
                      IP_0007: Call(Void Qux(Int32))
                      IP_0008: LoadLocal(0)
                      IP_0009: Call(Int32 get_Foo())
                    }");
        }

        private static Expression<Func<int>> Spill_RefArgs_Call()
        {
            Reflection.MethodInfo assign = typeof(ByRefs).GetMethod(nameof(ByRefs.Assign));
            ParameterExpression x = Expression.Parameter(typeof(int));
            ConstantExpression v = Expression.Constant(42);
            BlockExpression b = Expression.Block(new[] { x }, Expression.Call(assign, x, Spill(v)), x);

            Expression<Func<int>> e = Expression.Lambda<Func<int>>(b);

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefArgs_Call_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefArgs_Call();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Spill_RefArgs_Call_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefArgs_Call();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] int32&,
                        [1] int32,
                        [2] int32,
                        [3] int32
                      )

                      // V0 = ref x                  // spill reference to x [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // V1 = try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // Assign(ref x, try { 42 })   // using spilled reference in V0
                      IL_000e: ldloc.0    
                      IL_000f: ldloc.1    
                      IL_0010: call       void class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ByRefs::Assign(int32&,int32)

                      // return x
                      IL_0015: ldloc.2    
                      IL_0016: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 4
                      .maxcontinuation 1

                      IP_0000: InitImmutableValue(0)
                      IP_0001: LoadLocal(0)
                      .try
                      {
                        IP_0002: EnterTryFinally[0] -> 5
                        IP_0003: LoadObject(42)
                        IP_0004: Goto[1] -> 7
                      }
                      finally
                      {
                        IP_0005: EnterFinally[0] -> 5
                        IP_0006: LeaveFinally()
                      }
                      IP_0007: Call(Void Assign(Int32 ByRef, Int32))
                      IP_0008: LoadLocal(0)
                    }");
        }

        private static Expression<Func<int>> Spill_RefArgs_New()
        {
            Reflection.ConstructorInfo ctor = typeof(ByRefs).GetConstructors()[0];
            ParameterExpression x = Expression.Parameter(typeof(int));
            ConstantExpression v = Expression.Constant(42);
            BlockExpression b = Expression.Block(new[] { x }, Expression.New(ctor, x, Spill(v)), x);

            Expression<Func<int>> e = Expression.Lambda<Func<int>>(b);

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefArgs_New_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefArgs_New();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Spill_RefArgs_New_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefArgs_New();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 5
                      .locals init (
                        [0] int32&,
                        [1] int32,
                        [2] int32,
                        [3] int32
                      )

                      // V0 = ref x                       // spill reference to x [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // V1 = try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // new ByRefs(ref x, try { 42 })    // using spilled reference in V0
                      IL_000e: ldloc.0    
                      IL_000f: ldloc.1    
                      IL_0010: newobj     instance void class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ByRefs::.ctor(int32&,int32)
                      IL_0015: pop        

                      // return x
                      IL_0016: ldloc.2    
                      IL_0017: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 4
                      .maxcontinuation 1

                      IP_0000: InitImmutableValue(0)
                      IP_0001: LoadLocal(0)
                      .try
                      {
                        IP_0002: EnterTryFinally[0] -> 5
                        IP_0003: LoadObject(42)
                        IP_0004: Goto[1] -> 7
                      }
                      finally
                      {
                        IP_0005: EnterFinally[0] -> 5
                        IP_0006: LeaveFinally()
                      }
                      IP_0007: New ByRefs(Void .ctor(Int32 ByRef, Int32))
                      IP_0008: Pop()
                      IP_0009: LoadLocal(0)
                    }");
        }

        private static Expression<Func<int>> Spill_RefArgs_Invoke()
        {
            ConstantExpression assign = Expression.Constant(new Assign((ref int l, int r) => { l = r; }));
            ParameterExpression x = Expression.Parameter(typeof(int));
            ConstantExpression v = Expression.Constant(42);
            BlockExpression b = Expression.Block(new[] { x }, Expression.Invoke(assign, x, Spill(v)), x);

            Expression<Func<int>> e = Expression.Lambda<Func<int>>(b);

            return e;
        }

        [Fact]
        public static void Spill_RefArgs_Invoke_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefArgs_Invoke();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 4
                      .locals init (
                        [0] int32&,
                        [1] int32,
                        [2] int32,
                        [3] int32
                      )

                      // V0 = ref x                       // spill reference to x [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // V1 = try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // f.Invoke(ref x, try { 42 })      // using spilled reference in V0
                      IL_000e: ldarg.0    
                      IL_000f: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Constants
                      IL_0014: ldc.i4.0   
                      IL_0015: ldelem.ref 
                      IL_0016: castclass  class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+Assign
                      IL_001b: ldloc.0    
                      IL_001c: ldloc.1    
                      IL_001d: callvirt   instance void class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+Assign::Invoke(int32&,int32)

                      // return x
                      IL_0022: ldloc.2    
                      IL_0023: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 5
                      .maxcontinuation 1

                      IP_0000: InitImmutableValue(0)
                      IP_0001: LoadCached(0: System.Linq.Expressions.Tests.StackSpillerTests+Assign)
                      IP_0002: LoadLocal(0)
                      .try
                      {
                        IP_0003: EnterTryFinally[0] -> 6
                        IP_0004: LoadObject(42)
                        IP_0005: Goto[1] -> 8
                      }
                      finally
                      {
                        IP_0006: EnterFinally[0] -> 6
                        IP_0007: LeaveFinally()
                      }
                      IP_0008: Call(Void Invoke(Int32 ByRef, Int32))
                      IP_0009: LoadLocal(0)
                    }");
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefArgs_Invoke_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefArgs_Invoke();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        private static Expression<Func<int>> Spill_RefArgs_Invoke_Inline()
        {
            ParameterExpression l = Expression.Parameter(typeof(int).MakeByRefType());
            ParameterExpression r = Expression.Parameter(typeof(int));
            Expression<Assign> assign = Expression.Lambda<Assign>(Expression.Assign(l, r), l, r);
            ParameterExpression x = Expression.Parameter(typeof(int));
            ConstantExpression v = Expression.Constant(42);
            BlockExpression b = Expression.Block(new[] { x }, Expression.Invoke(assign, x, Spill(v)), x);

            Expression<Func<int>> e = Expression.Lambda<Func<int>>(b);

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefArgs_Invoke_Inline_Eval(bool useInterpreter)
        {
            Expression<Func<int>> e = Spill_RefArgs_Invoke_Inline();

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Spill_RefArgs_Invoke_Inline_CodeGen()
        {
            Expression<Func<int>> e = Spill_RefArgs_Invoke_Inline();

            e.Verify(
                il: @"
                    .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] int32&,
                        [1] int32,
                        [2] int32,
                        [3] int32,
                        [4] int32&,
                        [5] int32,
                        [6] int32
                      )

                      // V0 = ref x                       // spill reference to x [V2] into V0
                      IL_0000: ldloca.s   V_2
                      IL_0002: stloc.0    

                      // V1 = try { 42 } finally { }
                      .try
                      {
                        IL_0003: ldc.i4.s   42
                        IL_0005: stloc.3    
                        IL_0006: leave      IL_000c
                      }
                      finally
                      {
                        IL_000b: endfinally 
                      }
                      IL_000c: ldloc.3    
                      IL_000d: stloc.1    

                      // l [V4] = ref x
                      // r [V5] = try { 42 }
                      IL_000e: ldloc.0    
                      IL_000f: ldloc.1    
                      IL_0010: stloc.s    V_5
                      IL_0012: stloc.s    V_4

                      // V6 = V5  (used to return value of assignment; ignored here)
                      IL_0014: ldloc.s    V_5
                      IL_0016: stloc.s    V_6

                      // l = r
                      IL_0018: ldloc.s    V_4
                      IL_001a: ldloc.s    V_6
                      IL_001c: stind.i4   

                      // return x
                      IL_001d: ldloc.2    
                      IL_001e: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 5
                      .maxcontinuation 1

                      IP_0000: InitImmutableValue(0)
                      IP_0001: CreateDelegate()
                      IP_0002: LoadLocal(0)
                      .try
                      {
                        IP_0003: EnterTryFinally[0] -> 6
                        IP_0004: LoadObject(42)
                        IP_0005: Goto[1] -> 8
                      }
                      finally
                      {
                        IP_0006: EnterFinally[0] -> 6
                        IP_0007: LeaveFinally()
                      }
                      IP_0008: Call(Void Invoke(Int32 ByRef, Int32))
                      IP_0009: LoadLocal(0)
                    }");
        }

        private static Expression<Func<ValueList>> Spill_RefInstance_ListInit()
        {
            ParameterExpression l = Expression.Parameter(typeof(ValueList));
            ListInitExpression i = Expression.ListInit(Expression.New(typeof(ValueList)), Spill(Expression.Constant(42)));
            BlockExpression b = Expression.Block(new[] { l }, Expression.Assign(l, i), l);

            Expression<Func<ValueList>> e = Expression.Lambda<Func<ValueList>>(b);

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefInstance_ListInit_Eval(bool useInterpreter)
        {
            Expression<Func<ValueList>> e = Spill_RefInstance_ListInit();

            Func<ValueList> f = e.Compile(useInterpreter);

            Assert.Equal(42, f()[0]);
        }

        [Fact]
        public static void Spill_RefInstance_ListInit_CodeGen()
        {
            Expression<Func<ValueList>> e = Spill_RefInstance_ListInit();

            e.Verify(
                il: @"
                    .method valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueList ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueList,
                        [1] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueList&,
                        [2] int32,
                        [3] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueList,
                        [4] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueList,
                        [5] int32
                      )

                      // t = new ValueList()
                      IL_0000: ldloca.s   V_4
                      IL_0002: initobj    valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueList
                      IL_0008: ldloc.s    V_4
                      IL_000a: stloc.0    

                      // V0 = ref t                  // spill reference to t [V0] into V1
                      IL_000b: ldloca.s   V_0
                      IL_000d: stloc.1    

                      // V2 = try { 42 } finally { }
                      .try
                      {
                        IL_000e: ldc.i4.s   42
                        IL_0010: stloc.s    V_5
                        IL_0012: leave      IL_0018
                      }
                      finally
                      {
                        IL_0017: endfinally 
                      }
                      IL_0018: ldloc.s    V_5
                      IL_001a: stloc.2    

                      // t.Add(try { 42 })           // using spilled reference in V0
                      IL_001b: ldloc.1    
                      IL_001c: ldloc.2    
                      IL_001d: call       instance void valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueList::Add(int32)

                      // l = t
                      IL_0022: ldloc.0    
                      IL_0023: stloc.3    

                      // return l
                      IL_0024: ldloc.3    
                      IL_0025: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 5
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: DefaultValue System.Linq.Expressions.Tests.StackSpillerTests+ValueList
                      IP_0002: Dup()
                      .try
                      {
                        IP_0003: EnterTryFinally[0] -> 6
                        IP_0004: LoadObject(42)
                        IP_0005: Goto[1] -> 8
                      }
                      finally
                      {
                        IP_0006: EnterFinally[0] -> 6
                        IP_0007: LeaveFinally()
                      }
                      IP_0008: Call(Void Add(Int32))
                      IP_0009: StoreLocal(0)
                      IP_0010: LoadLocal(0)
                      IP_0011: ValueTypeCopy()
                    }");
        }

        private static Expression<Func<ValueBar>> Spill_RefInstance_MemberInit_Assign_Field()
        {
            Reflection.FieldInfo baz = typeof(ValueBar).GetField(nameof(ValueBar.Baz));
            ParameterExpression l = Expression.Parameter(typeof(ValueBar));
            MemberInitExpression i = Expression.MemberInit(Expression.New(typeof(ValueBar)), Expression.Bind(baz, Spill(Expression.Constant(42))));
            BlockExpression b = Expression.Block(new[] { l }, Expression.Assign(l, i), l);

            Expression<Func<ValueBar>> e = Expression.Lambda<Func<ValueBar>>(b);

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefInstance_MemberInit_Assign_Field_Eval(bool useInterpreter)
        {
            Expression<Func<ValueBar>> e = Spill_RefInstance_MemberInit_Assign_Field();

            Func<ValueBar> f = e.Compile(useInterpreter);

            Assert.Equal(42, f().Baz);
        }

        [Fact]
        public static void Spill_RefInstance_MemberInit_Assign_Field_CodeGen()
        {
            Expression<Func<ValueBar>> e = Spill_RefInstance_MemberInit_Assign_Field();

            e.Verify(
                il: @"
                    .method valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [1] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar&,
                        [2] int32,
                        [3] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [4] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [5] int32
                      )

                      // t = new ValueBar()
                      IL_0000: ldloca.s   V_4
                      IL_0002: initobj    valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar
                      IL_0008: ldloc.s    V_4
                      IL_000a: stloc.0    

                      // V0 = ref t                  // spill reference to t [V0] into V1
                      IL_000b: ldloca.s   V_0
                      IL_000d: stloc.1    

                      // V2 = try { 42 } finally { }
                      .try
                      {
                        IL_000e: ldc.i4.s   42
                        IL_0010: stloc.s    V_5
                        IL_0012: leave      IL_0018
                      }
                      finally
                      {
                        IL_0017: endfinally 
                      }
                      IL_0018: ldloc.s    V_5
                      IL_001a: stloc.2    

                      // t.Baz = try { 42 }          // using spilled reference in V0
                      IL_001b: ldloc.1    
                      IL_001c: ldloc.2    
                      IL_001d: stfld      valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::Baz

                      // l = t
                      IL_0022: ldloc.0    
                      IL_0023: stloc.3    

                      // return l
                      IL_0024: ldloc.3    
                      IL_0025: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 5
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: DefaultValue System.Linq.Expressions.Tests.StackSpillerTests+ValueBar
                      IP_0002: Dup()
                      .try
                      {
                        IP_0003: EnterTryFinally[0] -> 6
                        IP_0004: LoadObject(42)
                        IP_0005: Goto[1] -> 8
                      }
                      finally
                      {
                        IP_0006: EnterFinally[0] -> 6
                        IP_0007: LeaveFinally()
                      }
                      IP_0008: StoreField(Int32 Baz)
                      IP_0009: StoreLocal(0)
                      IP_0010: LoadLocal(0)
                      IP_0011: ValueTypeCopy()
                    }");
        }

        private static Expression<Func<ValueBar>> Spill_RefInstance_MemberInit_Assign_Property()
        {
            Reflection.PropertyInfo foo = typeof(ValueBar).GetProperty(nameof(ValueBar.Foo));
            ParameterExpression l = Expression.Parameter(typeof(ValueBar));
            MemberInitExpression i = Expression.MemberInit(Expression.New(typeof(ValueBar)), Expression.Bind(foo, Spill(Expression.Constant(42))));
            BlockExpression b = Expression.Block(new[] { l }, Expression.Assign(l, i), l);

            Expression<Func<ValueBar>> e = Expression.Lambda<Func<ValueBar>>(b);

            return e;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Spill_RefInstance_MemberInit_Assign_Property_Eval(bool useInterpreter)
        {
            Expression<Func<ValueBar>> e = Spill_RefInstance_MemberInit_Assign_Property();

            Func<ValueBar> f = e.Compile(useInterpreter);

            Assert.Equal(42, f().Foo);
        }

        [Fact]
        public static void Spill_RefInstance_MemberInit_Assign_Property_CodeGen()
        {
            Expression<Func<ValueBar>> e = Spill_RefInstance_MemberInit_Assign_Property();

            e.Verify(
                il: @"
                    .method valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [1] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar&,
                        [2] int32,
                        [3] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [4] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [5] int32
                      )

                      // t = new ValueBar()
                      IL_0000: ldloca.s   V_4
                      IL_0002: initobj    valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar
                      IL_0008: ldloc.s    V_4
                      IL_000a: stloc.0    

                      // V0 = ref t                  // spill reference to t [V0] into V1
                      IL_000b: ldloca.s   V_0
                      IL_000d: stloc.1    

                      // V2 = try { 42 } finally { }
                      .try
                      {
                        IL_000e: ldc.i4.s   42
                        IL_0010: stloc.s    V_5
                        IL_0012: leave      IL_0018
                      }
                      finally
                      {
                        IL_0017: endfinally 
                      }
                      IL_0018: ldloc.s    V_5
                      IL_001a: stloc.2    

                      // t.Foo = try { 42 }          // using spilled reference in V0
                      IL_001b: ldloc.1    
                      IL_001c: ldloc.2    
                      IL_001d: call       instance void valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::set_Foo(int32)

                      // l = t
                      IL_0022: ldloc.0    
                      IL_0023: stloc.3    

                      // return l
                      IL_0024: ldloc.3    
                      IL_0025: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 5
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: DefaultValue System.Linq.Expressions.Tests.StackSpillerTests+ValueBar
                      IP_0002: Dup()
                      .try
                      {
                        IP_0003: EnterTryFinally[0] -> 6
                        IP_0004: LoadObject(42)
                        IP_0005: Goto[1] -> 8
                      }
                      finally
                      {
                        IP_0006: EnterFinally[0] -> 6
                        IP_0007: LeaveFinally()
                      }
                      IP_0008: Call(Void set_Foo(Int32))
                      IP_0009: StoreLocal(0)
                      IP_0010: LoadLocal(0)
                      IP_0011: ValueTypeCopy()
                    }");
        }

        private static Expression<Func<ValueBar>> Spill_RefInstance_MemberInit_MemberBind()
        {
            Reflection.PropertyInfo baz2 = typeof(ValueBar).GetProperty(nameof(ValueBar.Baz2));
            Reflection.FieldInfo foo = typeof(Baz).GetField(nameof(Baz.Foo));
            ParameterExpression l = Expression.Parameter(typeof(ValueBar));
            MemberInitExpression i = Expression.MemberInit(Expression.New(typeof(ValueBar).GetConstructor(new[] { typeof(Baz) }), Expression.New(typeof(Baz))), Expression.MemberBind(baz2, Expression.Bind(foo, Spill(Expression.Constant(42)))));
            BlockExpression b = Expression.Block(new[] { l }, Expression.Assign(l, i), l);

            Expression<Func<ValueBar>> e = Expression.Lambda<Func<ValueBar>>(b);

            return e;
        }

        [Fact]
        public static void Spill_RefInstance_MemberInit_MemberBind_CodeGen()
        {
            Expression<Func<ValueBar>> e = Spill_RefInstance_MemberInit_MemberBind();

            e.Verify(
                il: @"
                    .method valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 6
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [1] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar&,
                        [2] class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+Baz,
                        [3] int32,
                        [4] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [5] int32
                      )
                
                      IL_0000: newobj     instance void class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+Baz::.ctor()
                      IL_0005: newobj     instance void valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::.ctor(class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+Baz)
                      IL_000a: stloc.0    
                      IL_000b: ldloca.s   V_0
                      IL_000d: stloc.1    
                      IL_000e: ldloc.1    
                      IL_000f: call       instance class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+Baz valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::get_Baz2()
                      IL_0014: stloc.2    
                      .try
                      {
                        IL_0015: ldc.i4.s   42
                        IL_0017: stloc.s    V_5
                        IL_0019: leave      IL_001f
                      }
                      finally
                      {
                        IL_001e: endfinally 
                      }
                      IL_001f: ldloc.s    V_5
                      IL_0021: stloc.3    
                      IL_0022: ldloc.2    
                      IL_0023: ldloc.3    
                      IL_0024: stfld      class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+Baz::Foo
                      IL_0029: ldloc.0    
                      IL_002a: stloc.s    V_4
                      IL_002c: ldloc.s    V_4
                      IL_002e: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 6
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: New Baz(Void .ctor())
                      IP_0002: New ValueBar(Void .ctor(Baz))
                      IP_0003: Dup()
                      IP_0004: Call(Baz get_Baz2())
                      IP_0005: Dup()
                      .try
                      {
                        IP_0006: EnterTryFinally[0] -> 9
                        IP_0007: LoadObject(42)
                        IP_0008: Goto[1] -> 11
                      }
                      finally
                      {
                        IP_0009: EnterFinally[0] -> 9
                        IP_0010: LeaveFinally()
                      }
                      IP_0011: StoreField(Int32 Foo)
                      IP_0012: Pop()
                      IP_0013: StoreLocal(0)
                      IP_0014: LoadLocal(0)
                      IP_0015: ValueTypeCopy()
                    }");
        }

        private static Expression<Func<ValueBar>> Spill_RefInstance_MemberInit_ListBind()
        {
            Reflection.PropertyInfo xs = typeof(ValueBar).GetProperty(nameof(ValueBar.Xs));
            Reflection.MethodInfo add = typeof(List<int>).GetMethod(nameof(List<int>.Add));
            ParameterExpression l = Expression.Parameter(typeof(ValueBar));
            MemberInitExpression i = Expression.MemberInit(Expression.New(typeof(ValueBar)), Expression.ListBind(xs, Expression.ElementInit(add, Spill(Expression.Constant(42)))));
            BlockExpression b = Expression.Block(new[] { l }, Expression.Assign(l, i), l);

            Expression<Func<ValueBar>> e = Expression.Lambda<Func<ValueBar>>(b);

            return e;
        }

        [Fact]
        public static void Spill_RefInstance_MemberInit_ListBind_CodeGen()
        {
            Expression<Func<ValueBar>> e = Spill_RefInstance_MemberInit_ListBind();

            e.Verify(
                il: @"
                    .method valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                    {
                      .maxstack 3
                      .locals init (
                        [0] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [1] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar&,
                        [2] class [System.Private.CoreLib]System.Collections.Generic.List`1<int32>,
                        [3] int32,
                        [4] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [5] valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar,
                        [6] int32
                      )
                
                      IL_0000: ldloca.s   V_5
                      IL_0002: initobj    valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar
                      IL_0008: ldloc.s    V_5
                      IL_000a: stloc.0    
                      IL_000b: ldloca.s   V_0
                      IL_000d: stloc.1    
                      IL_000e: ldloc.1    
                      IL_000f: call       instance class [System.Private.CoreLib]System.Collections.Generic.List`1<int32> valuetype [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.StackSpillerTests+ValueBar::get_Xs()
                      IL_0014: stloc.2    
                      .try
                      {
                        IL_0015: ldc.i4.s   42
                        IL_0017: stloc.s    V_6
                        IL_0019: leave      IL_001f
                      }
                      finally
                      {
                        IL_001e: endfinally 
                      }
                      IL_001f: ldloc.s    V_6
                      IL_0021: stloc.3    
                      IL_0022: ldloc.2    
                      IL_0023: ldloc.3    
                      IL_0024: callvirt   instance void class [System.Private.CoreLib]System.Collections.Generic.List`1<int32>::Add(int32)
                      IL_0029: ldloc.0    
                      IL_002a: stloc.s    V_4
                      IL_002c: ldloc.s    V_4
                      IL_002e: ret        
                    }",
                instructions: @"
                    object lambda_method(object[])
                    {
                      .locals 1
                      .maxstack 6
                      .maxcontinuation 1

                      IP_0000: InitMutableValue(0)
                      IP_0001: DefaultValue System.Linq.Expressions.Tests.StackSpillerTests+ValueBar
                      IP_0002: Dup()
                      IP_0003: Call(System.Collections.Generic.List`1[System.Int32] get_Xs())
                      IP_0004: Dup()
                      .try
                      {
                        IP_0005: EnterTryFinally[0] -> 8
                        IP_0006: LoadObject(42)
                        IP_0007: Goto[1] -> 10
                      }
                      finally
                      {
                        IP_0008: EnterFinally[0] -> 8
                        IP_0009: LeaveFinally()
                      }
                      IP_0010: Call(Void Add(Int32))
                      IP_0011: Pop()
                      IP_0012: StoreLocal(0)
                      IP_0013: LoadLocal(0)
                      IP_0014: ValueTypeCopy()
                    }");
        }

#if FEATURE_COMPILE

        [Fact]
        public static void Spill_Optimizations_Constant()
        {
            ParameterExpression xs = Expression.Parameter(typeof(int[]));
            ConstantExpression i = Expression.Constant(0);
            Expression v = Spill(Expression.Constant(1));

            Expression<Action<int[]>> e =
                Expression.Lambda<Action<int[]>>(
                    Expression.Assign(Expression.ArrayAccess(xs, i), v),
                    xs
                );

            e.VerifyIL(@"
                .method void ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32[])
                {
                  .maxstack 4
                  .locals init (
                    [0] int32[],
                    [1] int32,
                    [2] int32
                  )

                  // Save instance (`xs`) into V_0
                  IL_0000: ldarg.1
                  IL_0001: stloc.0

                  // Save rhs (`try { 1 } finally {}`) into V_1
                  .try
                  {
                    IL_0002: ldc.i4.1
                    IL_0003: stloc.2
                    IL_0004: leave      IL_000a
                  }
                  finally
                  {
                    IL_0009: endfinally
                  }
                  IL_000a: ldloc.2
                  IL_000b: stloc.1

                  // Load instance from V_0
                  IL_000c: ldloc.0

                  // <OPTIMIZATION> Evaluate index (`0`) </OPTIMIZATION>
                  IL_000d: ldc.i4.0

                  // Load rhs from V_1
                  IL_000e: ldloc.1

                  // Evaluate `instance[index] = rhs` index assignment
                  IL_000f: stelem.i4

                  IL_0010: ret
                }"
            );
        }

        [Fact]
        public static void Spill_Optimizations_Default()
        {
            ParameterExpression xs = Expression.Parameter(typeof(int[]));
            DefaultExpression i = Expression.Default(typeof(int));
            Expression v = Spill(Expression.Constant(1));

            Expression<Action<int[]>> e =
                Expression.Lambda<Action<int[]>>(
                    Expression.Assign(Expression.ArrayAccess(xs, i), v),
                    xs
                );

            e.VerifyIL(@"
                .method void ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32[])
                {
                  .maxstack 4
                  .locals init (
                    [0] int32[],
                    [1] int32,
                    [2] int32
                  )

                  // Save instance (`xs`) into V_0
                  IL_0000: ldarg.1
                  IL_0001: stloc.0

                  // Save rhs (`try { 1 } finally {}`) into V_1
                  .try
                  {
                    IL_0002: ldc.i4.1
                    IL_0003: stloc.2
                    IL_0004: leave      IL_000a
                  }
                  finally
                  {
                    IL_0009: endfinally
                  }
                  IL_000a: ldloc.2
                  IL_000b: stloc.1

                  // Load instance from V_0
                  IL_000c: ldloc.0

                  // <OPTIMIZATION> Evaluate index (`0`) </OPTIMIZATION>
                  IL_000d: ldc.i4.0

                  // Load rhs from V_1
                  IL_000e: ldloc.1

                  // Evaluate `instance[index] = rhs` index assignment
                  IL_000f: stelem.i4

                  IL_0010: ret
                }"
            );
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void Spill_Optimizations_LiteralField_NetFramework()
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Add(
                        Expression.Field(null, typeof(Math).GetField(nameof(Math.PI))),
                        Spill(Expression.Constant(0.0))
                    )
                );

            e.VerifyIL(@"
                .method float64 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                {
                  .maxstack 3
                  .locals init (
                    [0] float64,
                    [1] float64
                  )

                  // Save rhs (`try { 0.0 } finally {}`) into V_0
                  .try
                  {
                    IL_0000: ldc.r8     0
                    IL_0009: stloc.1
                    IL_000a: leave      IL_0010
                  }
                  finally
                  {
                    IL_000f: endfinally
                  }
                  IL_0010: ldloc.1
                  IL_0011: stloc.0

                  // <OPTIMIZATION> Evaluate lhs (`Math.PI` gets inlined) </OPTIMIZATION>
                  IL_0012: ldc.r8     3.14159265358979

                  // Load rhs from V_0
                  IL_001b: ldloc.0

                  // Evaluate `lhs + rhs`
                  IL_001c: add

                  IL_001d: ret
                }"
            );
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Spill_Optimizations_LiteralField_NotNetFramework()
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Add(
                        Expression.Field(null, typeof(Math).GetField(nameof(Math.PI))),
                        Spill(Expression.Constant(0.0))
                    )
                );

            e.VerifyIL(@"
                .method float64 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                {
                  .maxstack 3
                  .locals init (
                    [0] float64,
                    [1] float64
                  )

                  // Save rhs (`try { 0.0 } finally {}`) into V_0
                  .try
                  {
                    IL_0000: ldc.r8     0
                    IL_0009: stloc.1
                    IL_000a: leave      IL_0010
                  }
                  finally
                  {
                    IL_000f: endfinally
                  }
                  IL_0010: ldloc.1
                  IL_0011: stloc.0

                  // <OPTIMIZATION> Evaluate lhs (`Math.PI` gets inlined) </OPTIMIZATION>
                  IL_0012: ldc.r8     3.141592653589793

                  // Load rhs from V_0
                  IL_001b: ldloc.0

                  // Evaluate `lhs + rhs`
                  IL_001c: add

                  IL_001d: ret
                }"
            );
        }

        [Fact]
        public static void Spill_Optimizations_StaticReadOnlyField()
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.Call(
                        typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) }),
                        Expression.Field(null, typeof(bool).GetField(nameof(bool.TrueString))),
                        Spill(Expression.Constant("!"))
                    )
                );

            e.VerifyIL(@"
                .method string ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
                {
                  .maxstack 3
                  .locals init (
                    [0] string,
                    [1] string
                  )

                  // Save arg1 (`try { ""!"" } finally {}`) into V_0
                  .try
                  {
                    IL_0000: ldstr      ""!""
                    IL_0005: stloc.1
                    IL_0006: leave      IL_000c
                  }
                  finally
                  {
                    IL_000b: endfinally
                  }
                  IL_000c: ldloc.1
                  IL_000d: stloc.0

                  // <OPTIMIZATION> Evaluate arg0 (`bool::TrueString`) </OPTIMIZATION>
                  IL_000e: ldsfld     bool::TrueString

                  // Load arg1 from V_0
                  IL_0013: ldloc.0

                  // Evaluate `string.Concat(arg0, arg1)` call
                  IL_0014: call       string string::Concat(string,string)

                  IL_0019: ret
                }"
            );
        }

        [Fact]
        public static void Spill_Optimizations_RuntimeVariables1()
        {
            ParameterExpression f = Expression.Parameter(typeof(Action<IRuntimeVariables, int>));
            Expression<Action<Action<IRuntimeVariables, int>>> e =
                Expression.Lambda<Action<Action<IRuntimeVariables, int>>>(
                    Expression.Invoke(
                        f,
                        Expression.RuntimeVariables(),
                        Spill(Expression.Constant(2))
                    ),
                    f
                );

            e.VerifyIL(@"
                .method void ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,class [System.Private.CoreLib]System.Action`2<class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32>)
                {
                  .maxstack 4
                  .locals init (
                    [0] class [System.Private.CoreLib]System.Action`2<class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32>,
                    [1] int32,
                    [2] int32
                  )

                  // Save target (`f`) into V_0
                  IL_0000: ldarg.1
                  IL_0001: stloc.0

                  // Save arg1 (`try { 2 } finally {}`) into V_1
                  .try
                  {
                    IL_0002: ldc.i4.2
                    IL_0003: stloc.2
                    IL_0004: leave      IL_000a
                  }
                  finally
                  {
                    IL_0009: endfinally
                  }
                  IL_000a: ldloc.2
                  IL_000b: stloc.1

                  // Load target from V_0
                  IL_000c: ldloc.0

                  // <OPTIMIZATION> Load arg0 (`RuntimeVariables`) by calling RuntimeOps.CreateRuntimeVariables </OPTIMIZATION>
                  IL_000d: call       class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables class [System.Linq.Expressions]System.Runtime.CompilerServices.RuntimeOps::CreateRuntimeVariables()

                  // Load arg1 from V_1
                  IL_0012: ldloc.1

                  // Evaluate `target(arg0, arg1)` delegate invocation
                  IL_0013: callvirt   instance void class [System.Private.CoreLib]System.Action`2<class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32>::Invoke(class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32)

                  IL_0018: ret
                }"
            );
        }

        [Fact]
        public static void Spill_Optimizations_RuntimeVariables2()
        {
            ParameterExpression f = Expression.Parameter(typeof(Action<IRuntimeVariables, int>));
            ParameterExpression x = Expression.Parameter(typeof(int));
            Expression<Action<Action<IRuntimeVariables, int>, int>> e =
                Expression.Lambda<Action<Action<IRuntimeVariables, int>, int>>(
                    Expression.Invoke(
                        f,
                        Expression.RuntimeVariables(x),
                        Spill(Expression.Constant(2))
                    ),
                    f,
                    x
                );

            e.VerifyIL(@"
                .method void ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,class [System.Private.CoreLib]System.Action`2<class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32>,int32)
                {
                  .maxstack 10
                  .locals init (
                    [0] object[],
                    [1] class [System.Private.CoreLib]System.Action`2<class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32>,
                    [2] int32,
                    [3] int32
                  )

                  // Hoist `x` to a closure in V_0
                  IL_0000: ldc.i4.1
                  IL_0001: newarr     object
                  IL_0006: dup
                  IL_0007: ldc.i4.0
                  IL_0008: ldarg.2
                  IL_0009: newobj     instance void class [System.Private.CoreLib]System.Runtime.CompilerServices.StrongBox`1<int32>::.ctor(int32)
                  IL_000e: stelem.ref
                  IL_000f: stloc.0

                  // Save target (`f`) into V_1
                  IL_0010: ldarg.1
                  IL_0011: stloc.1

                  // Save arg1 (`try { 2 } finally {}`) into V_2
                  .try
                  {
                    IL_0012: ldc.i4.2
                    IL_0013: stloc.3
                    IL_0014: leave      IL_001a
                  }
                  finally
                  {
                    IL_0019: endfinally
                  }
                  IL_001a: ldloc.3
                  IL_001b: stloc.2

                  // Load target from V_1
                  IL_001c: ldloc.1

                  // <OPTIMIZATION> Load arg0 (`RuntimeVariables`) by calling RuntimeOps.CreateRuntimeVariables </OPTIMIZATION>
                  IL_001d: ldloc.0
                  IL_001e: ldarg.0
                  IL_001f: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Constants
                  IL_0024: ldc.i4.0
                  IL_0025: ldelem.ref
                  IL_0026: castclass  int64[]
                  IL_002b: call       class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables class [System.Linq.Expressions]System.Runtime.CompilerServices.RuntimeOps::CreateRuntimeVariables(object[],int64[])

                  // Load arg1 from V_2
                  IL_0030: ldloc.2

                  // Evaluate `target(arg0, arg1)` delegate invocation
                  IL_0031: callvirt   instance void class [System.Private.CoreLib]System.Action`2<class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32>::Invoke(class [System.Linq.Expressions]System.Runtime.CompilerServices.IRuntimeVariables,int32)

                  IL_0036: ret
                }"
            );
        }

        [Fact]
        public static void Spill_Optimizations_NoSpillBeyondSpillSite1()
        {
            ParameterExpression f = Expression.Parameter(typeof(Func<int, int, int, int>));
            ParameterExpression x = Expression.Parameter(typeof(int));
            ParameterExpression y = Expression.Parameter(typeof(int));
            ParameterExpression z = Expression.Parameter(typeof(int));

            Expression<Func<Func<int, int, int, int>, int, int, int, int>> e =
                Expression.Lambda<Func<Func<int, int, int, int>, int, int, int, int>>(
                    Expression.Invoke(
                        f,
                        Expression.TryFinally(
                            x,
                            Expression.Empty()
                        ),
                        y,  // NB: These occur after the spill site and don't have
                        z   //     to be stored in temporaries.
                    ),
                    f,
                    x,
                    y,
                    z
                );

            Func<Func<int, int, int, int>, int, int, int, int> d = e.Compile();
            Assert.Equal(7, d((a, b, c) => a + b * c, 1, 2, 3));

            e.VerifyIL(@"
                .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,class [System.Private.CoreLib]System.Func`4<int32,int32,int32,int32>,int32,int32,int32)
                {
                  .maxstack 5
                  .locals init (
                    [0] class [System.Private.CoreLib]System.Func`4<int32,int32,int32,int32>,
                    [1] int32,
                    [2] int32
                  )

                  // Save invocation target (`f`) into V_0
                  IL_0000: ldarg.1
                  IL_0001: stloc.0

                  // Save arg0 (`try { x } finally {}`) into V_1
                  .try
                  {
                    IL_0002: ldarg.2
                    IL_0003: stloc.2
                    IL_0004: leave      IL_000a
                  }
                  finally
                  {
                    IL_0009: endfinally
                  }
                  IL_000a: ldloc.2
                  IL_000b: stloc.1

                  // Load invocation target from V_0
                  IL_000c: ldloc.0

                  // Load arg0 from V_1
                  IL_000d: ldloc.1

                  // <OPTIMIZATION> Load arguments beyond spill site </OPTIMIZATION>
                  IL_000e: ldarg.3
                  IL_000f: ldarg.s    V_4

                  // Evaluate `f(try { x } finally {}, y, z)`
                  IL_0011: callvirt   instance int32 class [System.Private.CoreLib]System.Func`4<int32,int32,int32,int32>::Invoke(int32,int32,int32)

                  IL_0016: ret
                }");
        }

        [Fact]
        public static void Spill_Optimizations_NoSpillBeyondSpillSite2()
        {
            ParameterExpression f = Expression.Parameter(typeof(Func<int, int, int, int>));
            ParameterExpression x1 = Expression.Parameter(typeof(int));
            ParameterExpression x2 = Expression.Parameter(typeof(int));
            ParameterExpression x3 = Expression.Parameter(typeof(int));
            ParameterExpression x4 = Expression.Parameter(typeof(int));

            Expression<Func<int, int, int, int, int>> e =
                Expression.Lambda<Func<int, int, int, int, int>>(
                    Expression.Add(
                        Expression.Add(
                            Expression.Add(
                                x1, // NB: Occurs before spill site; needs to be saved.
                                Expression.TryFinally(
                                    x2,
                                    Expression.Empty()
                                )
                            ),
                            x3  // NB: Occurs beyond spill site; does not need to be saved.
                        ),
                        x4 // NB: Occurs beyond spill site; does not need to be saved.
                    ),
                    x1,
                    x2,
                    x3,
                    x4
                );

            Func<int, int, int, int, int> d = e.Compile();
            Assert.Equal(10, d(1, 2, 3, 4));

            e.VerifyIL(@"
                .method int32 ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,int32,int32,int32,int32)
                {
                  .maxstack 3
                  .locals init (
                    [0] int32,
                    [1] int32,
                    [2] int32,
                    [3] int32,
                    [4] int32
                  )

                  // Save `x1` into V_0
                  IL_0000: ldarg.1
                  IL_0001: stloc.0

                  // Save `try { x2 } finally {}` into V_1
                  .try
                  {
                    IL_0002: ldarg.2
                    IL_0003: stloc.s    V_4
                    IL_0005: leave      IL_000b
                  }
                  finally
                  {
                    IL_000a: endfinally
                  }
                  IL_000b: ldloc.s    V_4
                  IL_000d: stloc.1

                  // Eval `x1 + x2` and store into V_2
                  IL_000e: ldloc.0
                  IL_000f: ldloc.1
                  IL_0010: add
                  IL_0011: stloc.2

                  // Eval `(x1 + x2) + x3` and save into V_3
                  // <OPTIMIZATION> `x3` does not get stored in a temporary </OPTIMIZATION>
                  IL_0012: ldloc.2
                  IL_0013: ldarg.3
                  IL_0014: add
                  IL_0015: stloc.3

                  // Eval `((x1 + x2) + x3) + x4`
                  // <OPTIMIZATION> `x4` does not get stored in a temporary </OPTIMIZATION>
                  IL_0016: ldloc.3
                  IL_0017: ldarg.s    V_4
                  IL_0019: add

                  IL_001a: ret
                }");
        }

#endif

        private static void Test(Func<Expression, Expression> factory, Expression arg1)
        {
            Test(args => factory(args[0]), new[] { arg1 }, false);
            Test(args => factory(args[0]), new[] { arg1 }, true);
        }

        private static void Test(Func<Expression, Expression, Expression> factory, Expression arg1, Expression arg2)
        {
            Test(args => factory(args[0], args[1]), new[] { arg1, arg2 }, false);
            Test(args => factory(args[0], args[1]), new[] { arg1, arg2 }, true);
        }

        private static void Test(Func<Expression, Expression, Expression, Expression> factory, Expression arg1, Expression arg2, Expression arg3)
        {
            Test(args => factory(args[0], args[1], args[2]), new[] { arg1, arg2, arg3 }, false);
            Test(args => factory(args[0], args[1], args[2]), new[] { arg1, arg2, arg3 }, true);
        }

        private static void Test(Func<Expression[], Expression> factory, Expression[] args, bool useInterpreter)
        {
            object expected = Eval(factory(args), useInterpreter);

            for (var i = 0; i < args.Length; i++)
            {
                Expression[] newArgs = args.Select((arg, j) => j == i ? Spill(arg) : arg).ToArray();
                Assert.Equal(expected, Eval(factory(newArgs), useInterpreter));
            }

            for (var i = 0; i < args.Length; i++)
            {
                Expression[] newArgs = args.Select((arg, j) => j == i ? new Extension(arg) : arg).ToArray();
                Assert.Equal(expected, Eval(factory(newArgs), useInterpreter));
            }
        }

        private static object Eval(Expression expression, bool useInterpreter)
        {
            return Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile(useInterpreter)();
        }

        private static Expression Spill(Expression expression)
        {
            return Expression.TryFinally(expression, Expression.Empty());
        }

        class BarHolder
        {
            public Bar Bar { get; } = new Bar();
        }

        class ListHolder
        {
            public List<int> Xs { get; } = new List<int>();
        }

        class Bar
        {
            public int Baz { get; set; }
            public int Foo { get; set; }
            public int Qux { get; set; }
        }

        class Extension : Expression
        {
            private readonly Expression _reduced;

            public Extension(Expression reduced)
            {
                _reduced = reduced;
            }

            public override bool CanReduce => true;
            public override ExpressionType NodeType => ExpressionType.Extension;
            public override Type Type => _reduced.Type;
            public override Expression Reduce() => _reduced;
        }

        struct ValueVector
        {
            private int v0;

            public int this[int x]
            {
                get { return v0; }
                set { v0 = value; }
            }
        }

        struct ValueBar
        {
            public ValueBar(Baz baz)
            {
                Baz = 0;
                Foo = 0;
                Baz2 = baz;
                Xs = new List<int>();
            }

#pragma warning disable 0649
            public int Baz;
#pragma warning restore 0649

            public int Foo { get; set; }
            public Baz Baz2 { get; }
            public List<int> Xs { get; }

            public int this[int x]
            {
                get { return Foo = x; }
            }

            public void Qux(int x) => Foo = x;
        }

        class Baz
        {
            public Baz()
            {
                Foo = 0;
            }

            public int Foo;

            public int this[int x]
            {
                get { return Foo = x; }
            }

            public void Qux(int x) => Foo = x;
        }

        class ByRefs
        {
            public ByRefs(ref int x, int v)
            {
                x = v;
            }

            public static void Assign(ref int x, int v)
            {
                x = v;
            }
        }

        delegate void Assign(ref int x, int v);

        struct ValueList : IEnumerable<int>
        {
            private List<int> _values;

            public int this[int index]
            {
                get { return _values[index]; }
            }

            public void Add(int x)
            {
                if (_values == null)
                {
                    _values = new List<int>();
                }

                _values.Add(x);
            }
            
            public IEnumerator<int> GetEnumerator()
            {
                if (_values != null)
                {
                    foreach (var value in _values)
                    {
                        yield return value;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
