// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class DebugViewTests
    {
        private static readonly PropertyInfo s_debugView = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        [Fact]
        public static void Constant_Null()
        {
            Check("null", Expression.Constant(null));
            Check("null", Expression.Constant(null, typeof(string)));
            Check("null", Expression.Constant(null, typeof(int?)));
       }

        [Fact]
        public static void Constant_String()
        {
            Check("\"bar\"", Expression.Constant("bar", typeof(string)));
        }

        [Fact]
        public static void Constant_Primitives_Literals()
        {
            Check("False", Expression.Constant(false, typeof(bool)));
            Check("True", Expression.Constant(true, typeof(bool)));
            Check("\'a\'", Expression.Constant('a', typeof(char)));
            Check("42", Expression.Constant(42, typeof(int)));
        }

        [Fact]
        public static void Constant_Primitives_Suffixes()
        {
            Check("42U", Expression.Constant((uint)42, typeof(uint)));
            Check("42L", Expression.Constant((long)42, typeof(long)));
            Check("42UL", Expression.Constant((ulong)42, typeof(ulong)));
            Check("42F", Expression.Constant((float)42, typeof(float)));
            Check("42D", Expression.Constant((double)42, typeof(double)));
            Check("42M", Expression.Constant((decimal)42, typeof(decimal)));
        }

        [Fact]
        public static void Constant_Primitives_NoSuffix()
        {
            Check(".Constant<System.Byte>(42)", Expression.Constant((byte)42, typeof(byte)));
            Check(".Constant<System.SByte>(42)", Expression.Constant((sbyte)42, typeof(sbyte)));
            Check(".Constant<System.Int16>(42)", Expression.Constant((short)42, typeof(short)));
            Check(".Constant<System.UInt16>(42)", Expression.Constant((ushort)42, typeof(ushort)));
        }

        [Fact]
        public static void Constant_Primitives_WithDifferentType()
        {
            Check(".Constant<System.Object>(42)", Expression.Constant((int)42, typeof(object)));
            Check(".Constant<System.IComparable>(42)", Expression.Constant((long)42, typeof(IComparable)));
        }

        [Fact]
        public static void Constant_Custom()
        {
            Check(".Constant<System.Linq.Expressions.Tests.DebugViewTests+SomeTypeForConstant>(FOO)", Expression.Constant(new SomeTypeForConstant()));
        }

        [Fact]
        public static void Default()
        {
            Check(".Default(System.Void)", Expression.Empty());
            Check(".Default(System.Int32)", Expression.Default(typeof(int)));
        }

        [Fact]
        public static void Parameter()
        {
            Check("$var1", Expression.Parameter(typeof(int)));
            Check("$var1", Expression.Parameter(typeof(int), ""));

            Check("$a", Expression.Parameter(typeof(int), "a"));
            Check("$'a b'", Expression.Parameter(typeof(int), "a b"));
        }

        [Fact]
        public static void Binary_Arithmetic()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");

            ParameterExpression x = Expression.Parameter(typeof(double), "x");
            ParameterExpression y = Expression.Parameter(typeof(double), "y");

            Check("$a + $b", Expression.Add(a, b));
            Check("$a #+ $b", Expression.AddChecked(a, b));
            Check("$a - $b", Expression.Subtract(a, b));
            Check("$a #- $b", Expression.SubtractChecked(a, b));
            Check("$a * $b", Expression.Multiply(a, b));
            Check("$a #* $b", Expression.MultiplyChecked(a, b));
            Check("$a / $b", Expression.Divide(a, b));
            Check("$a % $b", Expression.Modulo(a, b));
            Check("$x ** $y", Expression.Power(x, y));
            Check("$a << $b", Expression.LeftShift(a, b));
            Check("$a >> $b", Expression.RightShift(a, b));
        }

        [Fact]
        public static void Binary_Logical()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");

            Check("$a & $b", Expression.And(a, b));
            Check("$a | $b", Expression.Or(a, b));
            Check("$a ^ $b", Expression.ExclusiveOr(a, b));
        }

        [Fact]
        public static void Binary_Comparison()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");

            Check("$a < $b", Expression.LessThan(a, b));
            Check("$a <= $b", Expression.LessThanOrEqual(a, b));
            Check("$a > $b", Expression.GreaterThan(a, b));
            Check("$a >= $b", Expression.GreaterThanOrEqual(a, b));
            Check("$a == $b", Expression.Equal(a, b));
            Check("$a != $b", Expression.NotEqual(a, b));
        }

        [Fact]
        public static void Binary_Coalesce()
        {
            ParameterExpression n = Expression.Parameter(typeof(int?), "n");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");

            Check("$n ?? $b", Expression.Coalesce(n, b));
        }

        [Fact]
        public static void Binary_Shortcircuiting()
        {
            ParameterExpression a = Expression.Parameter(typeof(bool), "a");
            ParameterExpression b = Expression.Parameter(typeof(bool), "b");

            Check("$a && $b", Expression.AndAlso(a, b));
            Check("$a || $b", Expression.OrElse(a, b));
        }

        [Fact]
        public static void Binary_Assign()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");

            ParameterExpression x = Expression.Parameter(typeof(double), "x");
            ParameterExpression y = Expression.Parameter(typeof(double), "y");

            Check("$a = $b",   Expression.Assign(a, b));
            Check("$a += $b",  Expression.AddAssign(a, b));
            Check("$a #+= $b", Expression.AddAssignChecked(a, b));
            Check("$a -= $b",  Expression.SubtractAssign(a, b));
            Check("$a #-= $b", Expression.SubtractAssignChecked(a, b));
            Check("$a *= $b",  Expression.MultiplyAssign(a, b));
            Check("$a #*= $b", Expression.MultiplyAssignChecked(a, b));
            Check("$a /= $b",  Expression.DivideAssign(a, b));
            Check("$a %= $b",  Expression.ModuloAssign(a, b));
            Check("$x **= $y", Expression.PowerAssign(x, y));
            Check("$a <<= $b", Expression.LeftShiftAssign(a, b));
            Check("$a >>= $b", Expression.RightShiftAssign(a, b));

            Check("$a &= $b",  Expression.AndAssign(a, b));
            Check("$a |= $b",  Expression.OrAssign(a, b));
            Check("$a ^= $b",  Expression.ExclusiveOrAssign(a, b));
        }

        [Fact]
        public static void Binary_ArrayIndex()
        {
            ParameterExpression xs = Expression.Parameter(typeof(int[]), "xs");
            ParameterExpression a = Expression.Parameter(typeof(int), "a");

            Check("$xs[$a]", Expression.ArrayIndex(xs, a));
        }

        [Fact]
        public static void Unary_Arithmetic()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");

            Check("+$a", Expression.UnaryPlus(a));
            Check("-$a", Expression.Negate(a));
            Check("#-$a", Expression.NegateChecked(a));
            Check(".Increment($a)", Expression.Increment(a));
            Check(".Decrement($a)", Expression.Decrement(a));
            Check("~$a", Expression.OnesComplement(a));
            Check("~$a", Expression.Not(a));
        }

        [Fact]
        public static void Unary_Boolean()
        {
            ParameterExpression b = Expression.Parameter(typeof(bool), "b");

            Check("!$b", Expression.Not(b));
            Check(".IsTrue($b)", Expression.IsTrue(b));
            Check(".IsFalse($b)", Expression.IsFalse(b));
        }

        [Fact]
        public static void Unary_Assign()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");

            Check("++$a", Expression.PreIncrementAssign(a));
            Check("$a++", Expression.PostIncrementAssign(a));
            Check("--$a", Expression.PreDecrementAssign(a));
            Check("$a--", Expression.PostDecrementAssign(a));
        }

        [Fact]
        public static void Unary_Convert()
        {
            ParameterExpression o = Expression.Parameter(typeof(object), "o");
            ParameterExpression x = Expression.Parameter(typeof(long), "x");

            Check("(System.Int32)$o", Expression.Convert(o, typeof(int)));
            Check("(System.Int32)$o", Expression.ConvertChecked(o, typeof(int)));
            Check("#(System.Int32)$x", Expression.ConvertChecked(x, typeof(int)));
            Check(".Unbox($o)", Expression.Unbox(o, typeof(int)));
            Check("$o .As System.String", Expression.TypeAs(o, typeof(string)));
        }

        [Fact]
        public static void Unary_Exceptions()
        {
            ParameterExpression e = Expression.Parameter(typeof(Exception), "e");
            ParameterExpression xs = Expression.Parameter(typeof(int[]), "xs");

            Check(".Rethrow", Expression.Rethrow());
            Check(".Throw $e", Expression.Throw(e));
        }

        [Fact]
        public static void Unary_ArrayLength()
        {
            ParameterExpression xs = Expression.Parameter(typeof(int[]), "xs");

            Check("$xs.Length", Expression.ArrayLength(xs));
        }

        [Fact]
        public static void Unary_Quote()
        {
            Expression<Action> expr = Expression.Lambda<Action>(Expression.Empty());

            Check("'(.Lambda #Lambda1<System.Action>)\\r\\n\\r\\n.Lambda #Lambda1<System.Action>() {\\r\\n    .Default(System.Void)\\r\\n}", Expression.Quote(expr));
        }

        [Fact]
        public static void Conditional()
        {
            ParameterExpression a = Expression.Parameter(typeof(bool), "a");
            ParameterExpression s1 = Expression.Parameter(typeof(bool), "ifTrue");
            ParameterExpression s2 = Expression.Parameter(typeof(bool), "ifFalse");

            Check(".If (\\r\\n    $a\\r\\n) {\\r\\n    $ifTrue\\r\\n} .Else {\\r\\n    .Default(System.Void)\\r\\n}", Expression.IfThen(a, s1));
            Check(".If (\\r\\n    $a\\r\\n) {\\r\\n    $ifTrue\\r\\n} .Else {\\r\\n    $ifFalse\\r\\n}", Expression.IfThenElse(a, s1, s2));
        }

        [Fact]
        public static void RuntimeVariables()
        {
            ParameterExpression a = Expression.Parameter(typeof(bool), "a");
            ParameterExpression b = Expression.Parameter(typeof(bool), "b");

            Check(".RuntimeVariables()", Expression.RuntimeVariables());
            Check(".RuntimeVariables($a)", Expression.RuntimeVariables(a));
            Check(".RuntimeVariables(\\r\\n    $a,\\r\\n    $b)", Expression.RuntimeVariables(a, b));
        }

        [Fact]
        public static void Member()
        {
            ParameterExpression s = Expression.Parameter(typeof(string), "s");

            Check("$s.Length", Expression.Property(s, "Length"));
            Check("System.DateTime.Now", Expression.Property(null, typeof(DateTime).GetProperty("Now")));
        }

        [Fact]
        public static void Invoke()
        {
            ParameterExpression f0 = Expression.Parameter(typeof(Func<int>), "f");
            ParameterExpression f1 = Expression.Parameter(typeof(Func<int, int>), "f");
            ParameterExpression f2 = Expression.Parameter(typeof(Func<int, int, int>), "f");
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");

            Check(".Invoke $f()", Expression.Invoke(f0));
            Check(".Invoke $f($x)", Expression.Invoke(f1, x));
            Check(".Invoke $f(\\r\\n    $x,\\r\\n    $y)", Expression.Invoke(f2, x, y));
        }

        [Fact]
        public static void Call()
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");
            ParameterExpression d = Expression.Parameter(typeof(double), "d");
            ParameterExpression s = Expression.Parameter(typeof(string), "s");

            Check(".Call $x.ToString()", Expression.Call(x, typeof(int).GetMethod("ToString", Type.EmptyTypes)));
            Check(".Call $s.Substring($x)", Expression.Call(s, typeof(string).GetMethod("Substring", new[] { typeof(int) }), x));
            Check(".Call $s.Substring(\\r\\n    $x,\\r\\n    $y)", Expression.Call(s, typeof(string).GetMethod("Substring", new[] { typeof(int), typeof(int) }), x, y));
            Check(".Call System.TimeSpan.FromSeconds($d)", Expression.Call(null, typeof(TimeSpan).GetMethod("FromSeconds", new[] { typeof(double) }), d));
        }

        [Fact]
        public static void NewArray()
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");

            Check(".NewArray System.Int32[$x]", Expression.NewArrayBounds(typeof(int), x));
            Check(".NewArray System.Int32[\\r\\n    $x,\\r\\n    $y]", Expression.NewArrayBounds(typeof(int), x, y));
            Check(".NewArray System.Int32[] {\\r\\n}", Expression.NewArrayInit(typeof(int)));
            Check(".NewArray System.Int32[] {\\r\\n    $x\\r\\n}", Expression.NewArrayInit(typeof(int), x));
            Check(".NewArray System.Int32[] {\\r\\n    $x,\\r\\n    $y\\r\\n}", Expression.NewArrayInit(typeof(int), x, y));
        }

        [Fact]
        public static void TypeBinary()
        {
            ParameterExpression o = Expression.Parameter(typeof(object), "o");

            Check("$o .Is System.Int32", Expression.TypeIs(o, typeof(int)));
            Check("$o .TypeEqual System.Int32", Expression.TypeEqual(o, typeof(int)));
        }

        [Fact]
        public static void New()
        {
            ParameterExpression l = Expression.Parameter(typeof(long), "l");
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");
            ParameterExpression z = Expression.Parameter(typeof(int), "z");

            Check(".New System.TimeSpan($l)", Expression.New(typeof(TimeSpan).GetConstructor(new[] { typeof(long) }), l));
            Check(".New System.TimeSpan(\\r\\n    $x,\\r\\n    $y,\\r\\n    $z)", Expression.New(typeof(TimeSpan).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) }), x, y, z));
        }

        [Fact]
        public static void Block()
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");

            Check(".Block() {\\r\\n    $x\\r\\n}", Expression.Block(x));
            Check(".Block() {\\r\\n    $x;\\r\\n    $y\\r\\n}", Expression.Block(x, y));
            Check(".Block<System.Void>() {\\r\\n    $x;\\r\\n    $y\\r\\n}", Expression.Block(typeof(void), x, y));
            Check(".Block(System.Int32 $x) {\\r\\n    $x\\r\\n}", Expression.Block(new[] { x }, x));
            Check(".Block(\\r\\n    System.Int32 $x,\\r\\n    System.Int32 $y) {\\r\\n    $x;\\r\\n    $y\\r\\n}", Expression.Block(new[] { x, y }, x, y));
            Check(".Block<System.Void>(\\r\\n    System.Int32 $x,\\r\\n    System.Int32 $y) {\\r\\n    $x;\\r\\n    $y\\r\\n}", Expression.Block(typeof(void), new[] { x, y }, x, y));
        }

        [Fact]
        public static void Label()
        {
            LabelTarget t1 = Expression.Label(typeof(void), "l1");
            LabelTarget t2 = Expression.Label(typeof(int), "l2");

            ParameterExpression x = Expression.Parameter(typeof(int), "x");

            Check(".Label\\r\\n.LabelTarget l1:", Expression.Label(t1));
            Check(".Label\\r\\n    $x\\r\\n.LabelTarget l2:", Expression.Label(t2, x));
        }

        [Fact]
        public static void Goto()
        {
            LabelTarget t1 = Expression.Label(typeof(void), "l1");
            LabelTarget t2 = Expression.Label(typeof(int), "l2");

            ParameterExpression x = Expression.Parameter(typeof(int), "x");

            Check(".Break l1 { }", Expression.Break(t1));
            Check(".Break l2 { $x }", Expression.Break(t2, x));
            Check(".Continue l1 { }", Expression.Continue(t1));
            Check(".Goto l1 { }", Expression.Goto(t1));
            Check(".Return l2 { $x }", Expression.Return(t2, x));
        }

        [Fact]
        public static void Loop()
        {
            LabelTarget t1 = Expression.Label(typeof(void), "l1");
            LabelTarget t2 = Expression.Label(typeof(int), "l2");

            Check(".Loop  {\\r\\n    .Default(System.Void)\\r\\n}", Expression.Loop(Expression.Empty()));
            Check(".Loop  {\\r\\n    .Default(System.Void)\\r\\n}\\r\\n.LabelTarget l2:", Expression.Loop(Expression.Empty(), t2));
            Check(".Loop .LabelTarget l1: {\\r\\n    .Default(System.Void)\\r\\n}\\r\\n.LabelTarget l2:", Expression.Loop(Expression.Empty(), t2, t1));
        }

        [Fact]
        public static void Switch()
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");
            ParameterExpression z = Expression.Parameter(typeof(int), "z");

            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");

            Check(".Switch ($x) {\\r\\n.Case ($y):\\r\\n        .Default(System.Void)\\r\\n}", Expression.Switch(x, Expression.SwitchCase(Expression.Empty(), y)));
            Check(".Switch ($x) {\\r\\n.Case ($y):\\r\\n.Case ($z):\\r\\n        .Default(System.Void)\\r\\n}", Expression.Switch(x, Expression.SwitchCase(Expression.Empty(), y, z)));
            Check(".Switch ($x) {\\r\\n.Case ($y):\\r\\n        .Default(System.Void)\\r\\n.Case ($z):\\r\\n        .Default(System.Void)\\r\\n}", Expression.Switch(x, Expression.SwitchCase(Expression.Empty(), y), Expression.SwitchCase(Expression.Empty(), z)));
            Check(".Switch ($x) {\\r\\n.Case ($y):\\r\\n        .Default(System.Void)\\r\\n.Default:\\r\\n        .Default(System.Void)\\r\\n}", Expression.Switch(x, Expression.Empty(), Expression.SwitchCase(Expression.Empty(), y)));
            Check(".Switch ($x) {\\r\\n.Case ($y):\\r\\n        $b\\r\\n.Default:\\r\\n        $a\\r\\n}", Expression.Switch(x, a, Expression.SwitchCase(b, y)));
        }

        [Fact]
        public static void Try()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");
            ParameterExpression c = Expression.Parameter(typeof(int), "c");

            ParameterExpression e = Expression.Parameter(typeof(Exception), "e");
            ParameterExpression i = Expression.Parameter(typeof(InvalidOperationException), "i");

            ParameterExpression f = Expression.Parameter(typeof(bool), "f");

            Check(".Try {\\r\\n    $a\\r\\n} .Finally {\\r\\n    .Default(System.Void)\\r\\n}", Expression.TryFinally(a, Expression.Empty()));
            Check(".Try {\\r\\n    $a\\r\\n} .Fault {\\r\\n    .Default(System.Void)\\r\\n}", Expression.TryFault(a, Expression.Empty()));
            Check(".Try {\\r\\n    $a\\r\\n} .Catch (System.Exception $e) {\\r\\n    $b\\r\\n}", Expression.TryCatch(a, Expression.Catch(e, b)));
            Check(".Try {\\r\\n    $a\\r\\n} .Catch (System.Exception $e) {\\r\\n    $b\\r\\n} .Catch (System.InvalidOperationException $i) {\\r\\n    $c\\r\\n}", Expression.TryCatch(a, Expression.Catch(e, b), Expression.Catch(i, c)));
            Check(".Try {\\r\\n    $a\\r\\n} .Catch (System.Exception $e) .If ($f) {\\r\\n    $b\\r\\n}", Expression.TryCatch(a, Expression.Catch(e, b, f)));
            Check(".Try {\\r\\n    $a\\r\\n} .Catch (System.Exception $e) {\\r\\n    $b\\r\\n} .Finally {\\r\\n    .Default(System.Void)\\r\\n}", Expression.TryCatchFinally(a, Expression.Empty(), Expression.Catch(e, b)));
        }

        [Fact]
        public static void Index()
        {
            ParameterExpression xs = Expression.Parameter(typeof(int[]), "xs");
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression d = Expression.Parameter(typeof(Dictionary<int, int>), "d");

            Check("$xs[$a]", Expression.ArrayAccess(xs, a));
            Check("$d.Item[$a]", Expression.MakeIndex(d, typeof(Dictionary<int, int>).GetProperty("Item"), new[] { a }));
        }

        [Fact]
        public static void ListInit()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");

            Check(".New System.Collections.Generic.List`1[System.Int32](){\\r\\n    $a\\r\\n}", Expression.ListInit(Expression.New(typeof(List<int>)), a));
            Check(".New System.Collections.Generic.List`1[System.Int32](){\\r\\n    $a,\\r\\n    $b\\r\\n}", Expression.ListInit(Expression.New(typeof(List<int>)), a, b));
            Check(".New System.Collections.Generic.Dictionary`2[System.Int32,System.Int32](){\\r\\n    {\\r\\n        $a,\\r\\n        $b\\r\\n    }\\r\\n}", Expression.ListInit(Expression.New(typeof(Dictionary<int, int>)), Expression.ElementInit(typeof(Dictionary<int, int>).GetMethod("Add"), a, b)));
        }

        [Fact]
        public static void MemberInit()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");
            ParameterExpression c = Expression.Parameter(typeof(int), "c");

            Check(".New System.Linq.Expressions.Tests.Bar(){\\r\\n    Foo = $a,\\r\\n    Qux = {\\r\\n        Baz = $b,\\r\\n        XS = {\\r\\n            $c\\r\\n        }\\r\\n    }\\r\\n}", Expression.MemberInit(Expression.New(typeof(Bar)), Expression.Bind(typeof(Bar).GetProperty("Foo"), a), Expression.MemberBind(typeof(Bar).GetProperty("Qux"), Expression.Bind(typeof(Qux).GetProperty("Baz"), b), Expression.ListBind(typeof(Qux).GetProperty("XS"), Expression.ElementInit(typeof(List<int>).GetMethod("Add"), c)))));
        }

        [Fact]
        public static void DebugInfo()
        {
            Check(".DebugInfo(bar.cs: 1, 2 - 3, 4)", Expression.DebugInfo(Expression.SymbolDocument("bar.cs"), 1, 2, 3, 4));
        }

        private static void Check(string s, Expression e) => Assert.Equal(s.Replace("\\r\\n", Environment.NewLine), GetDebugView(e));

        private static string GetDebugView(Expression expression) => (string)s_debugView.GetValue(expression);

        class SomeTypeForConstant
        {
            public override string ToString() => "FOO";
        }
    }

    public class Bar
    {
        public int Foo { get; set; }
        public Qux Qux { get; }
    }

    public class Qux
    {
        public int Baz { get; set; }
        public List<int> XS { get; }
    }
}
