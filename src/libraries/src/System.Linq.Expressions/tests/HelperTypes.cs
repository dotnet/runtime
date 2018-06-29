// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public interface I
    {
        void M();
    }

    public class C : IEquatable<C>, I
    {
        void I.M()
        {
        }

        public override bool Equals(object o)
        {
            return o is C && Equals((C)o);
        }

        public bool Equals(C c)
        {
            return c != null;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public class D : C, IEquatable<D>
    {
        public int Val;
        public string S;

        public D()
        {
        }
        public D(int val)
            : this(val, "")
        {
        }

        public D(int val, string s)
        {
            Val = val;
            S = s;
        }

        public override bool Equals(object o)
        {
            return o is D && Equals((D)o);
        }

        public bool Equals(D d)
        {
            return d != null && d.Val == Val;
        }

        public override int GetHashCode()
        {
            return Val;
        }
    }

    public enum E
    {
        A = 1,
        B = 2,
        Red = 0,
        Green,
        Blue
    }

    public enum El : long
    {
        A,
        B,
        C
    }

    public enum Eu : uint
    {
        Foo,
        Bar,
        Baz
    }

    public struct S : IEquatable<S>
    {
        public override bool Equals(object o)
        {
            return (o is S) && Equals((S)o);
        }
        public bool Equals(S other)
        {
            return true;
        }
        public override int GetHashCode()
        {
            return 0;
        }
    }

    public struct Sp : IEquatable<Sp>
    {
        public Sp(int i, double d)
        {
            I = i;
            D = d;
        }

        public int I;
        public double D;

        public override bool Equals(object o)
        {
            return (o is Sp) && Equals((Sp)o);
        }
        public bool Equals(Sp other)
        {
            return other.I == I && other.D.Equals(D);
        }
        public override int GetHashCode()
        {
            return I.GetHashCode() ^ D.GetHashCode();
        }
    }

    public struct Ss : IEquatable<Ss>
    {
        public Ss(S s)
        {
            Val = s;
        }

        public S Val;

        public override bool Equals(object o)
        {
            return (o is Ss) && Equals((Ss)o);
        }
        public bool Equals(Ss other)
        {
            return other.Val.Equals(Val);
        }
        public override int GetHashCode()
        {
            return Val.GetHashCode();
        }
    }

    public struct Sc : IEquatable<Sc>
    {
        public Sc(string s)
        {
            S = s;
        }

        public string S;

        public override bool Equals(object o)
        {
            return (o is Sc) && Equals((Sc)o);
        }
        public bool Equals(Sc other)
        {
            return other.S == S;
        }
        public override int GetHashCode()
        {
            return S.GetHashCode();
        }
    }

    public struct Scs : IEquatable<Scs>
    {
        public Scs(string s, S val)
        {
            S = s;
            Val = val;
        }

        public string S;
        public S Val;

        public override bool Equals(object o)
        {
            return (o is Scs) && Equals((Scs)o);
        }
        public bool Equals(Scs other)
        {
            return other.S == S && other.Val.Equals(Val);
        }
        public override int GetHashCode()
        {
            return S.GetHashCode() ^ Val.GetHashCode();
        }
    }

    public class BaseClass
    {
    }

    public class FC
    {
        public int II;
        public static int SI;
        public const int CI = 42;
        public static readonly int RI = 42;
    }

    public struct FS
    {
        public int II;
        public static int SI;
        public const int CI = 42;
        public static readonly int RI = 42;
    }

    public class PC
    {
        public int II { get; set; }
        public static int SI { get; set; }

        public int this[int i]
        {
            get { return 1; }
            set { }
        }
    }

    public struct PS
    {
        public int II { get; set; }
        public static int SI { get; set; }
    }

    internal class CompilationTypes : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> Booleans = new[]
        {
#if FEATURE_COMPILE && FEATURE_INTERPRET
            new object[] {false},
#endif
            new object[] {true},
        };

        public IEnumerator<object[]> GetEnumerator() => Booleans.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class NoOpVisitor : ExpressionVisitor
    {
        internal static readonly NoOpVisitor Instance = new NoOpVisitor();

        private NoOpVisitor()
        {
        }
    }

    public static class Unreadable<T>
    {
        public static T WriteOnly { set { } }
    }

    public class GenericClass<T>
    {
        public void Method() { }

        public static T Field;

        public static T Property => Field;
    }

    public class NonGenericClass
    {
        #pragma warning disable 0067
        public event EventHandler Event;
        #pragma warning restore 0067

        public void GenericMethod<T>() { }
        public static void StaticMethod() { }

        public static readonly NonGenericClass NonGenericField = new NonGenericClass();

        public static NonGenericClass NonGenericProperty => NonGenericField;
    }

    public class InvalidTypesData : IEnumerable<object[]>
    {
        private static readonly object[] GenericTypeDefinition = new object[] { typeof(GenericClass<>) };
        private static readonly object[] ContainsGenericParameters = new object[] { typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>)) };

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return GenericTypeDefinition;
            yield return ContainsGenericParameters;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class UnreadableExpressionsData : IEnumerable<object[]>
    {
        private static readonly object[] Property = new object[] { Expression.Property(null, typeof(Unreadable<bool>), nameof(Unreadable<bool>.WriteOnly)) };
        private static readonly object[] Indexer = new object[] { Expression.Property(null, typeof(Unreadable<bool>).GetProperty(nameof(Unreadable<bool>.WriteOnly)), new Expression[0]) };

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return Property;
            yield return Indexer;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class OpenGenericMethodsData : IEnumerable<object[]>
    {
        private static readonly object[] GenericClass = new object[] { typeof(GenericClass<>).GetMethod(nameof(GenericClass<string>.Method)) };
        private static readonly object[] GenericMethod = new object[] { typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.GenericMethod)) };

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return GenericClass;
            yield return GenericMethod;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public enum ByteEnum : byte { A = byte.MaxValue }
    public enum SByteEnum : sbyte { A = sbyte.MaxValue }
    public enum Int16Enum : short { A = short.MaxValue }
    public enum UInt16Enum : ushort { A = ushort.MaxValue }
    public enum Int32Enum : int { A = int.MaxValue }
    public enum UInt32Enum : uint { A = uint.MaxValue }
    public enum Int64Enum : long { A = long.MaxValue }
    public enum UInt64Enum : ulong { A = ulong.MaxValue }

#if FEATURE_COMPILE
    public static class NonCSharpTypes
    {
        private static Type _charEnumType;
        private static Type _boolEnumType;

        private static ModuleBuilder GetModuleBuilder()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            return assembly.DefineDynamicModule("Name");
        }

        public static Type CharEnumType
        {
            get
            {
                if (_charEnumType == null)
                {
                    EnumBuilder eb = GetModuleBuilder().DefineEnum("CharEnumType", TypeAttributes.Public, typeof(char));
                    eb.DefineLiteral("A", 'A');
                    eb.DefineLiteral("B", 'B');
                    eb.DefineLiteral("C", 'C');
                    _charEnumType = eb.CreateTypeInfo();
                }

                return _charEnumType;
            }
        }

        public static Type BoolEnumType
        {
            get
            {
                if (_boolEnumType == null)
                {
                    EnumBuilder eb = GetModuleBuilder().DefineEnum("BoolEnumType", TypeAttributes.Public, typeof(bool));
                    eb.DefineLiteral("False", false);
                    eb.DefineLiteral("True", true);
                    _boolEnumType = eb.CreateTypeInfo();
                }

                return _boolEnumType;
            }
        }
    }
#endif

    public class FakeExpression : Expression
    {
        public FakeExpression(ExpressionType customNodeType, Type customType)
        {
            CustomNodeType = customNodeType;
            CustomType = customType;
        }

        public ExpressionType CustomNodeType { get; set; }
        public Type CustomType { get; set; }

        public override ExpressionType NodeType => CustomNodeType;
        public override Type Type => CustomType;
    }

    public struct Number : IEquatable<Number>
    {
        private readonly int _value;

        public Number(int value)
        {
            _value = value;
        }

        public static readonly Number MinValue = new Number(int.MinValue);
        public static readonly Number MaxValue = new Number(int.MaxValue);

        public static Number operator +(Number l, Number r) => new Number(unchecked(l._value + r._value));
        public static Number operator -(Number l, Number r) => new Number(l._value - r._value);
        public static Number operator *(Number l, Number r) => new Number(unchecked(l._value * r._value));
        public static Number operator /(Number l, Number r) => new Number(l._value / r._value);
        public static Number operator %(Number l, Number r) => new Number(l._value % r._value);

        public static Number operator &(Number l, Number r) => new Number(l._value & r._value);
        public static Number operator |(Number l, Number r) => new Number(l._value | r._value);
        public static Number operator ^(Number l, Number r) => new Number(l._value ^ r._value);

        public static bool operator >(Number l, Number r) => l._value > r._value;
        public static bool operator >=(Number l, Number r) => l._value >= r._value;
        public static bool operator <(Number l, Number r) => l._value < r._value;
        public static bool operator <=(Number l, Number r) => l._value <= r._value;
        public static bool operator ==(Number l, Number r) => l._value == r._value;
        public static bool operator !=(Number l, Number r) => l._value != r._value;

        public override bool Equals(object obj) => obj is Number && Equals((Number)obj);
        public bool Equals(Number other) => _value == other._value;
        public override int GetHashCode() => _value;
    }

    public static class ExpressionAssert
    {
        public static void Verify(this LambdaExpression expression, string il, string instructions)
        {
#if FEATURE_COMPILE
            expression.VerifyIL(il);
#endif

            // FEATURE_COMPILE is not directly required, 
            // but this functionality relies on private reflection and that would not work with AOT
#if FEATURE_INTERPRET && FEATURE_COMPILE
            expression.VerifyInstructions(instructions);
#endif
        }
    }

    public class RunOnceEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private bool _called;

        public RunOnceEnumerable(IEnumerable<T> source)
        {
            _source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            Assert.False(_called);
            _called = true;
            return _source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class Truthiness
    {
        private bool Value { get; }

        public Truthiness(bool value)
        {
            Value = value;
        }

        public static implicit operator bool(Truthiness truth) => truth.Value;

        public static bool operator true(Truthiness truth) => truth.Value;

        public static bool operator false(Truthiness truth) => !truth.Value;

        public static Truthiness operator !(Truthiness truth) => new Truthiness(!truth.Value);
    }
}
