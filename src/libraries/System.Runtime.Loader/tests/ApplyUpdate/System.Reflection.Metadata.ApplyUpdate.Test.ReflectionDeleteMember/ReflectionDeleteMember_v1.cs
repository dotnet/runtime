// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class ReflectionDeleteMember
    {
        public int F1; // delete: not supported by Roslyn

        public ReflectionDeleteMember() { }
        // delete: public ReflectionDeleteMember(int arg) { F1 = arg; }

        public virtual int P1 { get; set; }
        public virtual void M1() { }

        public virtual int P2 { get; }
        public virtual void M2() { }

        public int P3 { get; set; }
        public void M3() { }
        public event Action E1 { add { } remove { } }
    }

    public class ReflectionDeleteMember_Derived : ReflectionDeleteMember
    {
        public new int F1;

        public override int P1 { get; set; } // delete: not supported by Roslyn
        public override void M1() { }        // delete: not supported by Roslyn

        public override int P2 { get; }
        public override void M2() { }

        // delete: public new int P3 { get; set; }
        // delete: public new void M3() { }
        // delete: public new event Action E1 { add { } remove { } }
    }

    public class ReflectionDeleteMember_Derived2 : ReflectionDeleteMember_Derived
    {
        public override int P1 { get; set; }
        public override void M1() { }

        public override int P2 { get; } // delete: not supported by Roslyn
        public override void M2() { }   // delete: not supported by Roslyn
    }

    public interface IReflectionDeleteMember
    {
        int P1 { get; set; }
        int P2 { get; }   // delete: not supported by Roslyn
        void M1();
        void M2();        // delete: not supported by Roslyn
        event Action E1;
        event Action E2;  // delete: not supported by Roslyn
    }
}
