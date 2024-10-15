// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer
{
    using System;

    public class MyClass
    {
        public int Field = 0;
    }

    public struct MyStruct
    {
        public int Number;
    }

    public enum MyEnum
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    public class MemberClass<T>
    {
        [ThreadStatic]
        public static int t_status;
        public bool? this[string p1, float p2, short[] p3]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return null;
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public byte this[dynamic[] p1, ulong[] p2, dynamic p3]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return (byte)3;
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public dynamic this[MyClass p1, char? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return p1;
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public dynamic[] this[MyClass p1, MyStruct? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return new dynamic[]
                {
                p1, p2
                }

                ;
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public double[] this[float p1]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return new double[]
                {
                1.4, double.Epsilon, double.NaN
                }

                ;
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public dynamic this[int?[] p1]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return p1;
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public MyClass[] this[MyEnum p1]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return new MyClass[]
                {
                null, new MyClass()
                {
                Field = 3
                }
                }

                ;
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public T this[string p1]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public MyClass this[string p1, T p2]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return new MyClass();
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public dynamic this[T p1]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public T this[dynamic p1, T p2]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }

        public T this[T p1, short p2, dynamic p3, string p4]
        {
            get
            {
                MemberClass<T>.t_status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.t_status = 2;
            }
        }
    }

    public class MemberClassMultipleParams<T, U, V>
    {
        public static int Status;
        public T this[V v, U u]
        {
            get
            {
                MemberClassMultipleParams<T, U, V>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClassMultipleParams<T, U, V>.Status = 2;
            }
        }
    }

    public class MemberClassWithClassConstraint<T>
        where T : class
    {
        [ThreadStatic]
        public static int t_status;
        public int this[int x]
        {
            get
            {
                MemberClassWithClassConstraint<T>.t_status = 3;
                return 1;
            }

            set
            {
                MemberClassWithClassConstraint<T>.t_status = 4;
            }
        }

        public T this[decimal dec, dynamic d]
        {
            get
            {
                MemberClassWithClassConstraint<T>.t_status = 1;
                return null;
            }

            set
            {
                MemberClassWithClassConstraint<T>.t_status = 2;
            }
        }
    }

    public class MemberClassWithNewConstraint<T>
        where T : new()
    {
        [ThreadStatic]
        public static int t_status;
        public dynamic this[T t]
        {
            get
            {
                MemberClassWithNewConstraint<T>.t_status = 1;
                return new T();
            }

            set
            {
                MemberClassWithNewConstraint<T>.t_status = 2;
            }
        }
    }

    public class MemberClassWithAnotherTypeConstraint<T, U>
        where T : U
    {
        public static int Status;
        public U this[dynamic d]
        {
            get
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 3;
                return default(U);
            }

            set
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 4;
            }
        }

        public dynamic this[int x, U u, dynamic d]
        {
            get
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 2;
            }
        }
    }

    #region Negative tests - you should not be able to construct this with a dynamic object
    public class C
    {
    }

    public interface I
    {
    }

    public class MemberClassWithUDClassConstraint<T>
        where T : C, new()
    {
        public static int Status;
        public C this[T t]
        {
            get
            {
                MemberClassWithUDClassConstraint<T>.Status = 1;
                return new T();
            }

            set
            {
                MemberClassWithUDClassConstraint<T>.Status = 2;
            }
        }
    }

    public class MemberClassWithStructConstraint<T>
        where T : struct
    {
        public static int Status;
        public dynamic this[int x]
        {
            get
            {
                MemberClassWithStructConstraint<T>.Status = 1;
                return x;
            }

            set
            {
                MemberClassWithStructConstraint<T>.Status = 2;
            }
        }
    }

    public class MemberClassWithInterfaceConstraint<T>
        where T : I
    {
        public static int Status;
        public dynamic this[int x, T v]
        {
            get
            {
                MemberClassWithInterfaceConstraint<T>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClassWithInterfaceConstraint<T>.Status = 2;
            }
        }
    }
    #endregion
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass001.genclass001
{
    // <Title> Tests generic class indexer used in + operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void PlusOperator()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass001.genclass001
            dynamic dy = new MemberClass<int>();
            string p1 = null;
            int result = dy[string.Empty] + dy[p1] + 1;
            Assert.Equal(1, result);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass002.genclass002
{
    // <Title> Tests generic class indexer used in implicit operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,20\).*CS0649</Expects>
    //<Expects Status=warning>\(29,20\).*CS0649</Expects>

    public class Test
    {
        public class ClassWithImplicitOperator
        {
            public static implicit operator EmptyClass(ClassWithImplicitOperator t1)
            {
                dynamic dy = new MemberClass<ClassWithImplicitOperator>();
                ClassWithImplicitOperator p2 = t1;
                return dy[dy, p2];
            }
        }

        public class EmptyClass { }

        [Fact]
        public static void ImplicitOperator()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass002.genclass002
            ClassWithImplicitOperator target = new ClassWithImplicitOperator();
            EmptyClass result = target; //implicit
            Assert.Null(result);
            Assert.Equal(1, MemberClass<ClassWithImplicitOperator>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass003.genclass003
{
    // <Title> Tests generic class indexer used in implicit operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class ClassWithExplicitOperator
        {
            public byte field;
            public static explicit operator ClassWithExplicitOperator(byte t1)
            {
                dynamic dy = new MemberClass<int>();
                dynamic[] p1 = null;
                ulong[] p2 = null;
                dynamic p3 = null;
                dy[p1, p2, p3] = (byte)10;
                return new ClassWithExplicitOperator
                {
                    field = dy[p1, p2, p3]
                };
            }
        }

        [Fact]
        public static void ExplicitOperator()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass003.genclass003
            byte b = 1;
            ClassWithExplicitOperator result = (ClassWithExplicitOperator)b;
            Assert.Equal(3, result.field);
            Assert.Equal(1, MemberClass<int>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass005.genclass005
{
    // <Title> Tests generic class indexer used in using block and using expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.IO;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass005.genclass005
            dynamic dy = new MemberClass<string>();
            dynamic dy2 = new MemberClass<bool>();
            string result = null;
            using (MemoryStream sm = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(sm))
                {
                    sw.Write((string)(dy[string.Empty] ?? "Test"));
                    sw.Flush();
                    sm.Position = 0;
                    using (StreamReader sr = new StreamReader(sm, (bool)dy2[false]))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }

            Assert.Equal("Test", result);
            Assert.Equal(1, MemberClass<string>.t_status);
            Assert.Equal(1, MemberClass<bool>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass006.genclass006
{
    // <Title> Tests generic class indexer used in the for-condition.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void ForStatement_MultipleParameters()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass006.genclass006
            MemberClassMultipleParams<int, string, Test> mc = new MemberClassMultipleParams<int, string, Test>();
            dynamic dy = mc;
            string u = null;
            Test v = null;
            int index1 = 10;
            for (int i = 10; i > dy[v, u]; i--)
            {
                index1--;
            }

            Assert.Equal(0, index1);
            Assert.Equal(1, MemberClassMultipleParams<int, string, Test>.Status);
        }

        [Fact]
        public void ForStatement_ClassConstraints()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass006.genclass006
            dynamic dy = new MemberClassWithClassConstraint<Test>();
            int index = 0;
            for (int i = 0; i < 10; i = i + dy[i])
            {
                dy[i] = i;
                index++;
            }

            Assert.Equal(10, index);
            Assert.Equal(3, MemberClassWithClassConstraint<Test>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass008.genclass008
{
    // <Title> Tests generic class indexer used in the while/do expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DoWhileExpression()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass008.genclass008
            dynamic dy = new MemberClass<int>();
            string p1 = null;
            float p2 = 1.23f;
            short[] p3 = null;
            int index = 0;
            do
            {
                index++;
                if (index == 10)
                    break;
            }
            while (dy[p1, p2, p3] ?? true);

            Assert.Equal(10, index);
            Assert.Equal(1, MemberClass<int>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass009.genclass009
{
    // <Title> Tests generic class indexer used in switch expression.</Title>
    // <Description> Won't fix: no dynamic in switch expression </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void SwitchExpression()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass009.genclass009
            dynamic dy = new MemberClass<int>();
            bool isChecked = false;
            switch ((int)dy["Test"])
            {
                case 0:
                    isChecked = true;
                    break;
                default:
                    break;
            }

            Assert.True(isChecked);
            Assert.Equal(1, MemberClass<int>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass010.genclass010
{
    // <Title> Tests generic class indexer used in switch section statement list.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void SwitchSectionStatementList()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass010.genclass010
            dynamic dy = new MemberClass<int>();
            string a = "Test";
            MyClass p1 = new MyClass()
            {
                Field = 10
            };

            char? p2 = null;
            MyEnum[] p3 = new MyEnum[] { MyEnum.Second };

            dynamic result = null;
            switch (a)
            {
                case "Test":
                    dy[p1, p2, p3] = 10;
                    result = dy[p1, p2, p3];
                    break;
                default:
                    break;
            }

            Assert.Equal(10, ((MyClass)result).Field);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass011.genclass011
{
    // <Title> Tests generic class indexer used in switch default section statement list.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void SwitchDefaultSectionStatementList()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass011.genclass011
            MemberClass<int> mc = new MemberClass<int>();
            dynamic dy = mc;
            string a = "Test1";
            MyClass p1 = new MyClass()
            {
                Field = 10
            };

            MyStruct? p2 = new MyStruct()
            {
                Number = 11
            };

            MyEnum[] p3 = new MyEnum[10];
            dynamic[] result = null;
            switch (a)
            {
                case "Test":
                    break;
                default:
                    result = dy[p1, p2, p3];
                    dy[p1, p2, p3] = new dynamic[10];
                    break;
            }

            Assert.Equal(2, result.Length);
            Assert.Equal(10, ((MyClass)result[0]).Field);
            Assert.Equal(11, ((MyStruct)result[1]).Number);

            Assert.Equal(2, MemberClass<int>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass014.genclass014
{
    // <Title> Tests generic class indexer used in try/catch/finally.</Title>
    // <Description>
    // try/catch/finally that uses an anonymous method and refer two dynamic parameters.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55118", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public static void TryCatchFinally()
        {
            dynamic dy = new MemberClass<int>();
            dynamic result = -1;
            bool threwException = true;
            try
            {
                Func<string, dynamic> func = delegate (string x)
                {
                    throw new TimeoutException(dy[x].ToString());
                };


                result = func("Test");
                threwException = false;
            }
            catch (TimeoutException e)
            {
                Assert.Equal("0", e.Message);
            }
            finally
            {
                result = dy[new int?[3]];
            }

            Assert.True(threwException);
            int?[] resultIntegers = result;
            // Assert.Equal doesn't allow to build collections with nulls in anymore. Use Assert.Collection instead.
            Assert.Collection(resultIntegers, (a) => Assert.Null(a), (b) => Assert.Null(b), (c) => Assert.Null(c));
            Assert.Equal(1, MemberClass<int>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass015.genclass015
{
    // <Title> Tests generic class indexer used in iterator that calls to a lambda expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections;

    public class Test
    {
        private static dynamic s_dy = new MemberClass<string>();
        private static int s_num = 0;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            foreach (MyClass[] me in t.Increment())
            {
                if (MemberClass<string>.t_status != 1 || me.Length != 2)
                    return 1;
            }

            return 0;
        }

        public IEnumerable Increment()
        {
            while (s_num++ < 4)
            {
                Func<MyEnum, MyClass[]> func = (MyEnum p1) => s_dy[p1];
                yield return func((MyEnum)s_num);
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass016.genclass016
{
    // <Title> Tests generic class indexer used in object initializer inside a collection initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        private string _field = string.Empty;

        [Fact]
        public static void ObjectInitializerInsideCollectionInitializer()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass016.genclass016
            dynamic dy = new MemberClassWithClassConstraint<string>();
            decimal dec = 123M;
            List<Test> list = new List<Test>()
            {
                new Test()
                {
                    _field = dy[dec, dy]
                }
            };

            Assert.Equal(1, list.Count);
            Assert.Null(list[0]._field);
            Assert.Equal(1, MemberClassWithClassConstraint<string>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass017.genclass017
{
    // <Title> Tests generic class indexer used in anonymous type.</Title>
    // <Description>
    // anonymous type inside a query expression that introduces dynamic variables.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;
    using System.Collections.Generic;

    public class Test
    {
        private int _field;
        public Test()
        {
            _field = 10;
        }

        [Fact]
        public static void AnonymousTypeInsideQueryExpression()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass017.genclass017

            List<string> list = new List<string>() { "0", "4", null, "6", "4", "4", null };

            dynamic dy = new MemberClassWithAnotherTypeConstraint<string, string>();
            dynamic dy2 = new MemberClassWithNewConstraint<Test>();
            Test t = new Test()
            {
                _field = 1
            };

            var result = list.Where(p => p == dy[dy2]).Select(p => new
            {
                A = dy2[t],
                B = dy[dy2]
            }).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(3, MemberClassWithAnotherTypeConstraint<string, string>.Status);
            Assert.Equal(1, MemberClassWithNewConstraint<Test>.t_status);

            foreach (var m in result)
            {
                Assert.Equal(10, ((Test)m.A)._field);
                Assert.Null(m.B);
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass018.genclass018
{
    // <Title> Tests generic class indexer used in static generic method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void StaticGenericMethodBody()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass018.genclass018
            StaticGenericMethod<Test>();
        }

        private static void StaticGenericMethod<T>()
        {
            dynamic dy = new MemberClassWithNewConstraint<MyClass>();
            MyClass p1 = null;
            dy[p1] = dy;
            Assert.Equal(2, MemberClassWithNewConstraint<MyClass>.t_status);

            dynamic p2 = dy[p1];
            Assert.IsType<MyClass>(p2);
            Assert.Equal(1, MemberClassWithNewConstraint<MyClass>.t_status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass020.genclass020
{
    // <Title> Tests generic class indexer used in static method body.</Title>
    // <Description>
    // Negative
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : C
    {
        private int _field;
        public Test()
        {
            _field = 11;
        }

        [Fact]
        public static void StaticMethodBody()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass020.genclass020
            dynamic dy = new MemberClassWithUDClassConstraint<Test>();
            Test t = null;
            dy[t] = new C();
            Assert.Equal(2, MemberClassWithUDClassConstraint<Test>.Status);

            t = new Test()
            {
                _field = 10
            };
            Test result = (Test)dy[t];
            Assert.Equal(11, result._field);
            Assert.Equal(1, MemberClassWithUDClassConstraint<Test>.Status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass021.genclass021
{
    // <Title> Tests generic class indexer used in static method body.</Title>
    // <Description>
    // Negative
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void StaticMethodBody()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass021.genclass021
            dynamic dy = new MemberClassWithStructConstraint<char>();
            dy[int.MinValue] = new Test();
            Assert.Equal(2, MemberClassWithStructConstraint<char>.Status);

            dynamic result = dy[int.MaxValue];
            Assert.Equal(int.MaxValue, result);
            Assert.Equal(1, MemberClassWithStructConstraint<char>.Status);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass022.genclass022
{
    // <Title> Tests generic class indexer used in static method body.</Title>
    // <Description>
    // Negative
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : I
    {
        public class InnerTest : Test
        {
        }

        [Fact]
        public static void StaticMethodBody()
        {
            // ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass022.genclass022
            dynamic dy = new MemberClassWithInterfaceConstraint<InnerTest>();
            dy[int.MinValue, new InnerTest()] = new Test();
            Assert.Equal(2, MemberClassWithInterfaceConstraint<InnerTest>.Status);

            dynamic result = dy[int.MaxValue, null];
            Assert.Null(result);
            Assert.Equal(1, MemberClassWithInterfaceConstraint<InnerTest>.Status);
        }
    }
    //</Code>
}
