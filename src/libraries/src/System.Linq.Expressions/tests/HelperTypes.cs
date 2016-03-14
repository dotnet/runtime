// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

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
        private static readonly object[] False = new object[] { false };
        private static readonly object[] True = new object[] { true };

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return False;
            yield return True;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
