// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// DelegateTest.cs - NUnit Test Cases for the System.Delegate class
//
// (C) Ximian, Inc.  http://www.ximian.com
//

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Tests
{
    public static class CreateDelegateTests
    {
        #region Tests
        [Fact]
        public static void CreateDelegate1_Method_Static()
        {
            C c = new C();
            MethodInfo mi = typeof(C).GetMethod("S");
            Delegate dg = Delegate.CreateDelegate(typeof(D), mi);
            Assert.Same(mi, dg.Method);
            Assert.Null(dg.Target);
            D d = (D)dg;
            d(c);
        }

        [Fact]
        public static void CreateDelegate1_Method_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(D), (MethodInfo)null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("method", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate1_Type_Null()
        {
            MethodInfo mi = typeof(C).GetMethod("S");
            try
            {
                Delegate.CreateDelegate((Type)null, mi);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("type", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2()
        {
            E e;

            e = (E)Delegate.CreateDelegate(typeof(E), new B(), "Execute");
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            e = (E)Delegate.CreateDelegate(typeof(E), new C(), "Execute");
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            e = (E)Delegate.CreateDelegate(typeof(E), new C(), "DoExecute");
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));
        }

        [Fact]
        public static void CreateDelegate2_Method_ArgumentsMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "StartExecute");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2_Method_CaseMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(), "ExecutE");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2_Method_DoesNotExist()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "DoesNotExist");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2_Method_Null()
        {
            C c = new C();
            try
            {
                Delegate.CreateDelegate(typeof(D), c, (string)null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("method", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2_Method_ReturnTypeMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "DoExecute");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2_Method_Static()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(), "Run");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2_Target_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(D), null, "N");
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("target", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate2_Type_Null()
        {
            C c = new C();
            try
            {
                Delegate.CreateDelegate((Type)null, c, "N");
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("type", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3()
        {
            E e;

            // matching static method
            e = (E)Delegate.CreateDelegate(typeof(E), typeof(B), "Run");
            Assert.NotNull(e);
            Assert.Equal(5, e(new C()));

            // matching static method
            e = (E)Delegate.CreateDelegate(typeof(E), typeof(C), "Run");
            Assert.NotNull(e);
            Assert.Equal(5, e(new C()));

            // matching static method
            e = (E)Delegate.CreateDelegate(typeof(E), typeof(C), "DoRun");
            Assert.NotNull(e);
            Assert.Equal(107, e(new C()));
        }

        [Fact]
        public static void CreateDelegate3_Method_ArgumentsMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), typeof(B),
                    "StartRun");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3_Method_CaseMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), typeof(B), "RuN");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3_Method_DoesNotExist()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), typeof(B),
                    "DoesNotExist");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3_Method_Instance()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), typeof(B), "Execute");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3_Method_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(D), typeof(C), (string)null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("method", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3_Method_ReturnTypeMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), typeof(B),
                    "DoRun");
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3_Target_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(D), (Type)null, "S");
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("target", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate3_Type_Null()
        {
            try
            {
                Delegate.CreateDelegate((Type)null, typeof(C), "S");
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("type", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4()
        {
            E e;

            B b = new B();

            // instance method, exact case, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), b, "Execute", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, exact case, do not ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), b, "Execute", false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, case mismatch, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), b, "ExecutE", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            C c = new C();

            // instance method, exact case, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "Execute", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, exact case, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "DoExecute", true);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // instance method, exact case, do not ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "Execute", false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, case mismatch, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "ExecutE", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));
        }

        [Fact]
        public static void CreateDelegate4_Method_ArgumentsMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "StartExecute", false);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4_Method_CaseMismatch()
        {
            // instance method, case mismatch, do not igore case
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "ExecutE", false);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4_Method_DoesNotExist()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "DoesNotExist", false);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4_Method_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(D), new C(),
                    (string)null, true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("method", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4_Method_ReturnTypeMismatch()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "DoExecute", false);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4_Method_Static()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(), "Run", true);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4_Target_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(D), null, "N", true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("target", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate4_Type_Null()
        {
            C c = new C();
            try
            {
                Delegate.CreateDelegate((Type)null, c, "N", true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("type", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate9()
        {
            E e;

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", false, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", false, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", true, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", true, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", false, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", false, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", true, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", true, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", false, false);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // do not ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", false, true);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", true, false);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", true, true);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));
        }

        [Fact]
        public static void CreateDelegate9_Method_ArgumentsMismatch()
        {
            // throw bind failure
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "StartExecute", false, true);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "StartExecute", false, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Method_CaseMismatch()
        {
            E e;

            // do not ignore case, throw bind failure
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "ExecutE", false, true);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "ExecutE", false, false);
            Assert.Null(e);

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "ExecutE", true, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "ExecutE", true, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));
        }

        [Fact]
        public static void CreateDelegate9_Method_DoesNotExist()
        {
            // throw bind failure
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "DoesNotExist", false, true);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "DoesNotExist", false, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Method_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    (string)null, false, false);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("method", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate9_Method_ReturnTypeMismatch()
        {
            // throw bind failure
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "DoExecute", false, true);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "DoExecute", false, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Method_Static()
        {
            // throw bind failure
            try
            {
                Delegate.CreateDelegate(typeof(E), new B(),
                    "Run", true, true);
            }
            catch (ArgumentException ex)
            {
                // Error binding to target method
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Run", true, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Target_Null()
        {
            try
            {
                Delegate.CreateDelegate(typeof(E), (object)null,
                    "Execute", true, false);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("target", ex.ParamName);
            }
        }

        [Fact]
        public static void CreateDelegate9_Type_Null()
        {
            try
            {
                Delegate.CreateDelegate((Type)null, new B(),
                    "Execute", true, false);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("type", ex.ParamName);
            }
        }

        #endregion Tests

        #region Test Setup

        public class B
        {

            public virtual string retarg3(string s)
            {
                return s;
            }

            static int Run(C x)
            {
                return 5;
            }

            public static void DoRun(C x)
            {
            }

            public static int StartRun(C x, B b)
            {
                return 6;
            }

            int Execute(C c)
            {
                return 4;
            }

            public static void DoExecute(C c)
            {
            }

            public int StartExecute(C c, B b)
            {
                return 3;
            }
        }

        public class C : B, Iface
        {
            public string retarg(string s)
            {
                return s;
            }

            public string retarg2(Iface iface, string s)
            {
                return s + "2";
            }

            public override string retarg3(string s)
            {
                return s + "2";
            }

            static void Run(C x)
            {
            }

            public new static int DoRun(C x)
            {
                return 107;
            }

            void Execute(C c)
            {
            }

            public new int DoExecute(C c)
            {
                return 102;
            }

            public static void M()
            {
            }

            public static void N(C c)
            {
            }

            public static void S(C c)
            {
            }

            private void PrivateInstance()
            {
            }
        }

        public interface Iface
        {
            string retarg(string s);
        }

        public delegate void D(C c);
        public delegate int E(C c);
        #endregion Test Setup
    }
}