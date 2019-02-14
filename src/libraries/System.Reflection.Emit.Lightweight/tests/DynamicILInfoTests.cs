// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicILInfoTests
    {
        private static string HelloWorld() => "hello, world".ToUpper();

        [Fact]
        public void GetTokenFor_String_Success()
        {
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(HelloWorld), typeof(string), new Type[] { }, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(string), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x72, 0x01, 0x00, 0x00, 0x70, 0x6f, 0x04, 0x00, 0x00, 0x0a, 0x0a, 0x2b, 0x00, 0x06, 0x2a
            };
            int token0 = dynamicILInfo.GetTokenFor("hello, world");
            int token1 = dynamicILInfo.GetTokenFor(typeof(string).GetMethod("ToUpper", Type.EmptyTypes).MethodHandle);
            PutInteger4(token0, 0x0002, code);
            PutInteger4(token1, 0x0007, code);
            dynamicILInfo.SetCode(code, 1);

            string ret = (string)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, HelloWorld());
        }

        private static long Fib(long value)
        {
            if (value == 0 || value == 1)
                return value;
            return Fib(value - 1) + Fib(value - 2);
        }
        
        [Fact]
        public void GetTokenFor_DynamicMethod_Success()
        {
            // Calling DynamicMethod recursively
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(Fib), typeof(long), new Type[] { typeof(long) }, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(long), false);
            sigHelper.AddArgument(typeof(bool), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x02, 0x16, 0x6a, 0x2e, 0x0a, 0x02, 0x17, 0x6a, 0xfe, 0x01, 0x16, 0xfe, 0x01, 0x2b, 0x01,
                0x16, 0x0b, 0x07, 0x2d, 0x04, 0x02, 0x0a, 0x2b, 0x16, 0x02, 0x17, 0x6a, 0x59, 0x28, 0x02, 0x00,
                0x00, 0x06, 0x02, 0x18, 0x6a, 0x59, 0x28, 0x02, 0x00, 0x00, 0x06, 0x58, 0x0a, 0x2b, 0x00, 0x06,
                0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor(dynamicMethod);
            PutInteger4(token0, 0x001e, code);
            PutInteger4(token0, 0x0027, code);
            dynamicILInfo.SetCode(code, 3);

            long ret = (long)dynamicMethod.Invoke(null, new object[] { 20 });
            Assert.Equal(ret, Fib(20));
        }

        private static Person Mock()
        {
            Person p = new Person("Bill", 50, 30000f);
            p.m_name = "Bill Gates";
            p.Age++;
            p.IncSalary(300);
            return p;
        }

        private class Person
        {
            int m_age;
            float m_salary;
            public string m_name;

            public int Age { get { return m_age; } set { m_age = value; } }
            public float Salary { get { return m_salary; } }

            public Person(string name, int age, float salary)
            {
                m_name = name;
                m_age = age;
                m_salary = salary;
            }

            public void IncSalary(float percentage)
            {
                m_salary = m_salary * percentage;
            }

            public override bool Equals(object other)
            {
                Person other2 = other as Person;
                if (other2 == null) return false;

                if (m_name != other2.m_name) return false;
                if (m_age != other2.m_age) return false;
                if (m_salary != other2.m_salary) return false;
                return true;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        [Fact]
        public void GetTokenFor_CtorMethodAndField_Success()
        {
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(Mock), typeof(Person), new Type[] { }, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(Person), false);
            sigHelper.AddArgument(typeof(Person), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x72, 0x49, 0x00, 0x00, 0x70, 0x1f, 0x32, 0x22, 0x00, 0x60, 0xea, 0x46, 0x73, 0x0f, 0x00,
                0x00, 0x06, 0x0a, 0x06, 0x72, 0x53, 0x00, 0x00, 0x70, 0x7d, 0x04, 0x00, 0x00, 0x04, 0x06, 0x25,
                0x6f, 0x0c, 0x00, 0x00, 0x06, 0x17, 0x58, 0x6f, 0x0d, 0x00, 0x00, 0x06, 0x00, 0x06, 0x22, 0x00,
                0x00, 0x96, 0x43, 0x6f, 0x10, 0x00, 0x00, 0x06, 0x00, 0x06, 0x0b, 0x2b, 0x00, 0x07, 0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor("Bill");
            int token1 = dynamicILInfo.GetTokenFor(typeof(Person).GetConstructor(new Type[] { typeof(string), typeof(int), typeof(float) }).MethodHandle);
            int token2 = dynamicILInfo.GetTokenFor("Bill Gates");
            int token3 = dynamicILInfo.GetTokenFor(typeof(Person).GetField("m_name").FieldHandle);
            int token4 = dynamicILInfo.GetTokenFor(typeof(Person).GetMethod("get_Age").MethodHandle);
            int token5 = dynamicILInfo.GetTokenFor(typeof(Person).GetMethod("set_Age").MethodHandle);
            int token6 = dynamicILInfo.GetTokenFor(typeof(Person).GetMethod("IncSalary").MethodHandle);
            PutInteger4(token0, 0x0002, code);
            PutInteger4(token1, 0x000e, code);
            PutInteger4(token2, 0x0015, code);
            PutInteger4(token3, 0x001a, code);
            PutInteger4(token4, 0x0021, code);
            PutInteger4(token5, 0x0028, code);
            PutInteger4(token6, 0x0034, code);
            dynamicILInfo.SetCode(code, 4);

            Person ret = (Person)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, Mock());
        }

        private class MyList<T> : IEnumerable<T>
        {
            List<T> list;

            public MyList() { list = new List<T>(); }
            public void Add(T val) { list.Add(val); }

            public IEnumerator<T> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
        }

        private static int SumInteger()
        {
            MyList<int> list = new MyList<int>();
            list.Add(100);
            list.Add(200);
            list.Add(300);

            int sum = 0;
            foreach (int item in list)
            {
                sum += item;
            }
            return sum;
        }

        [Fact]
        public void GetTokenFor_IntGenerics_Success()
        {
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(SumInteger), typeof(int), new Type[] { }, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(MyList<int>), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(System.Collections.Generic.IEnumerator<int>), false);
            sigHelper.AddArgument(typeof(bool), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x73, 0x1c, 0x00, 0x00, 0x0a, 0x0a, 0x06, 0x1f, 0x64, 0x6f, 0x1d, 0x00, 0x00, 0x0a, 0x00,
                0x06, 0x20, 0xc8, 0x00, 0x00, 0x00, 0x6f, 0x1d, 0x00, 0x00, 0x0a, 0x00, 0x06, 0x20, 0x2c, 0x01,
                0x00, 0x00, 0x6f, 0x1d, 0x00, 0x00, 0x0a, 0x00, 0x16, 0x0b, 0x00, 0x06, 0x6f, 0x1e, 0x00, 0x00,
                0x0a, 0x13, 0x04, 0x2b, 0x0e, 0x11, 0x04, 0x6f, 0x1f, 0x00, 0x00, 0x0a, 0x0c, 0x00, 0x07, 0x08,
                0x58, 0x0b, 0x00, 0x11, 0x04, 0x6f, 0x20, 0x00, 0x00, 0x0a, 0x13, 0x05, 0x11, 0x05, 0x2d, 0xe5,
                0xde, 0x14, 0x11, 0x04, 0x14, 0xfe, 0x01, 0x13, 0x05, 0x11, 0x05, 0x2d, 0x08, 0x11, 0x04, 0x6f,
                0x21, 0x00, 0x00, 0x0a, 0x00, 0xdc, 0x00, 0x07, 0x0d, 0x2b, 0x00, 0x09, 0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor(typeof(MyList<int>).GetConstructors()[0].MethodHandle, typeof(MyList<int>).TypeHandle);
            int token1 = dynamicILInfo.GetTokenFor(typeof(MyList<int>).GetMethod("Add").MethodHandle, typeof(MyList<int>).TypeHandle);
            int token2 = dynamicILInfo.GetTokenFor(typeof(MyList<int>).GetMethod("GetEnumerator").MethodHandle, typeof(MyList<int>).TypeHandle);
            int token3 = dynamicILInfo.GetTokenFor(typeof(System.Collections.Generic.IEnumerator<int>).GetMethod("get_Current").MethodHandle, typeof(System.Collections.Generic.IEnumerator<int>).TypeHandle);
            int token4 = dynamicILInfo.GetTokenFor(typeof(System.Collections.IEnumerator).GetMethod("MoveNext").MethodHandle);
            int token5 = dynamicILInfo.GetTokenFor(typeof(System.IDisposable).GetMethod("Dispose").MethodHandle);
            PutInteger4(token0, 0x0002, code);
            PutInteger4(token1, 0x000b, code);
            PutInteger4(token1, 0x0017, code);
            PutInteger4(token1, 0x0023, code);
            PutInteger4(token2, 0x002d, code);
            PutInteger4(token3, 0x0038, code);
            PutInteger4(token4, 0x0046, code);
            PutInteger4(token5, 0x0060, code);
            dynamicILInfo.SetCode(code, 2);

            byte[] exceptions = {
                0x41, 0x1c, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x33, 0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x00,
                0x52, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };
            dynamicILInfo.SetExceptions(exceptions);

            int ret = (int)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, SumInteger());
        }

        private static string ContactString()
        {
            MyList<string> list = new MyList<string>();
            list.Add("Hello~");
            list.Add("World!");
            string sum = string.Empty;
            foreach (string item in list)
            {
                sum += item;
            }
            return sum;
        }

        [Fact]
        public void GetTokenFor_StringGenerics_Success()
        {
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(ContactString), typeof(string), Type.EmptyTypes, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(MyList<string>), false);
            sigHelper.AddArgument(typeof(string), false);
            sigHelper.AddArgument(typeof(string), false);
            sigHelper.AddArgument(typeof(string), false);
            sigHelper.AddArgument(typeof(System.Collections.Generic.IEnumerator<string>), false);
            sigHelper.AddArgument(typeof(bool), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x73, 0x26, 0x00, 0x00, 0x0a, 0x0a, 0x06, 0x72, 0x29, 0x01, 0x00, 0x70, 0x6f, 0x27, 0x00,
                0x00, 0x0a, 0x00, 0x06, 0x72, 0x37, 0x01, 0x00, 0x70, 0x6f, 0x27, 0x00, 0x00, 0x0a, 0x00, 0x7e,
                0x28, 0x00, 0x00, 0x0a, 0x0b, 0x00, 0x06, 0x6f, 0x29, 0x00, 0x00, 0x0a, 0x13, 0x04, 0x2b, 0x12,
                0x11, 0x04, 0x6f, 0x2a, 0x00, 0x00, 0x0a, 0x0c, 0x00, 0x07, 0x08, 0x28, 0x2b, 0x00, 0x00, 0x0a,
                0x0b, 0x00, 0x11, 0x04, 0x6f, 0x20, 0x00, 0x00, 0x0a, 0x13, 0x05, 0x11, 0x05, 0x2d, 0xe1, 0xde,
                0x14, 0x11, 0x04, 0x14, 0xfe, 0x01, 0x13, 0x05, 0x11, 0x05, 0x2d, 0x08, 0x11, 0x04, 0x6f, 0x21,
                0x00, 0x00, 0x0a, 0x00, 0xdc, 0x00, 0x07, 0x0d, 0x2b, 0x00, 0x09, 0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor(typeof(MyList<string>).GetConstructor(Type.EmptyTypes).MethodHandle, typeof(MyList<string>).TypeHandle);
            int token1 = dynamicILInfo.GetTokenFor("Hello~");
            int token2 = dynamicILInfo.GetTokenFor(typeof(MyList<string>).GetMethod("Add").MethodHandle, typeof(MyList<string>).TypeHandle);
            int token3 = dynamicILInfo.GetTokenFor("World!");
            int token4 = dynamicILInfo.GetTokenFor(typeof(string).GetField("Empty").FieldHandle);
            int token5 = dynamicILInfo.GetTokenFor(typeof(MyList<string>).GetMethod("GetEnumerator").MethodHandle, typeof(MyList<string>).TypeHandle);
            int token6 = dynamicILInfo.GetTokenFor(typeof(System.Collections.Generic.IEnumerator<string>).GetMethod("get_Current").MethodHandle, typeof(System.Collections.Generic.IEnumerator<string>).TypeHandle);
            int token7 = dynamicILInfo.GetTokenFor(typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }).MethodHandle);
            int token8 = dynamicILInfo.GetTokenFor(typeof(System.Collections.IEnumerator).GetMethod("MoveNext").MethodHandle);
            int token9 = dynamicILInfo.GetTokenFor(typeof(System.IDisposable).GetMethod("Dispose").MethodHandle);

            PutInteger4(token0, 0x0002, code);
            PutInteger4(token1, 0x0009, code);
            PutInteger4(token2, 0x000e, code);
            PutInteger4(token3, 0x0015, code);
            PutInteger4(token2, 0x001a, code);
            PutInteger4(token4, 0x0020, code);
            PutInteger4(token5, 0x0028, code);
            PutInteger4(token6, 0x0033, code);
            PutInteger4(token7, 0x003c, code);
            PutInteger4(token8, 0x0045, code);
            PutInteger4(token9, 0x005f, code);
            dynamicILInfo.SetCode(code, 2);
            byte[] exceptions = {
                0x41, 0x1c, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x2e, 0x00, 0x00, 0x00, 0x23, 0x00, 0x00, 0x00,
                0x51, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };
            dynamicILInfo.SetExceptions(exceptions);

            string ret = (string)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, ContactString());
        }

        private static int ExceptionTest()
        {
            int caught = 0;
            try
            {
                try
                {
                    int j = 0;
                    int i = 1 / j;
                }
                catch
                {
                    caught++;
                }

                try
                {
                    Type.GetType("A.B", true);
                }
                catch (Exception)
                {
                    caught++;
                }

                string s = null;
                s.ToUpper();
            }
            catch (NullReferenceException)
            {
                caught++;
            }
            finally
            {
                caught += 2;
            }
            return caught;
        }

        [Fact]
        public void GetTokenFor_Exception_Success()
        {
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(ExceptionTest), typeof(int), Type.EmptyTypes, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(string), false);
            sigHelper.AddArgument(typeof(int), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x16, 0x0a, 0x00, 0x00, 0x16, 0x0b, 0x17, 0x07, 0x5b, 0x0c, 0x00, 0xde, 0x09, 0x26, 0x00,
                0x06, 0x17, 0x58, 0x0a, 0x00, 0xde, 0x00, 0x00, 0x00, 0x72, 0xed, 0x01, 0x00, 0x70, 0x17, 0x28,
                0x32, 0x00, 0x00, 0x0a, 0x26, 0x00, 0xde, 0x09, 0x26, 0x00, 0x06, 0x17, 0x58, 0x0a, 0x00, 0xde,
                0x00, 0x00, 0x14, 0x0d, 0x09, 0x6f, 0x05, 0x00, 0x00, 0x0a, 0x26, 0x00, 0xde, 0x09, 0x26, 0x00,
                0x06, 0x17, 0x58, 0x0a, 0x00, 0xde, 0x00, 0x00, 0xde, 0x07, 0x00, 0x06, 0x18, 0x58, 0x0a, 0x00,
                0xdc, 0x00, 0x06, 0x13, 0x04, 0x2b, 0x00, 0x11, 0x04, 0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor("A.B");
            int token1 = dynamicILInfo.GetTokenFor(typeof(System.Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool) }).MethodHandle);
            int token2 = dynamicILInfo.GetTokenFor(typeof(string).GetMethod("ToUpper", Type.EmptyTypes).MethodHandle);
            PutInteger4(token0, 0x001a, code);
            PutInteger4(token1, 0x0020, code);
            PutInteger4(token2, 0x0036, code);
            dynamicILInfo.SetCode(code, 2);

            int token3 = dynamicILInfo.GetTokenFor(typeof(System.Object).TypeHandle);
            int token4 = dynamicILInfo.GetTokenFor(typeof(System.Exception).TypeHandle);
            int token5 = dynamicILInfo.GetTokenFor(typeof(System.NullReferenceException).TypeHandle);
            byte[] exceptions = {
                0x41, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x00,
                0x0e, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x18, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x3b, 0x00, 0x00, 0x00,
                0x3e, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x47, 0x00, 0x00, 0x00, 0x4a, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
                };
            PutInteger4(token3, 0x0018, exceptions);
            PutInteger4(token4, 0x0030, exceptions);
            PutInteger4(token5, 0x0048, exceptions);
            dynamicILInfo.SetExceptions(exceptions);

            int ret = (int)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, ExceptionTest());
        }

        private static bool MyRule(int value) => value > 10;
        private bool MyRule(string value) => value.Length > 10;
        private delegate bool Satisfy<T>(T item);
        private class Finder
        {
            public static T Find<T>(T[] items, Satisfy<T> standard)
            {
                foreach (T item in items)
                    if (standard(item)) return item;
                return default(T);
            }
        }

        private static bool GenericMethod()
        {
            int[] intarray = new int[6];
            intarray[0] = 2;
            intarray[1] = 9;
            intarray[2] = -1;
            intarray[3] = 14;
            intarray[4] = 3;
            intarray[5] = 55;
            int i = Finder.Find(intarray, MyRule);

            string[] strarray = new string[] { "Hello", "1", "world", "dynamicmethod", "find it already" };
            string s = Finder.Find(strarray, new DynamicILInfoTests().MyRule);

            return (i == intarray[3] && s == strarray[3]);
        }

        [Fact]
        public void Test_GenericMethod()
        {
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(GenericMethod), typeof(bool), Type.EmptyTypes, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(System.Int32[]), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(System.String[]), false);
            sigHelper.AddArgument(typeof(string), false);
            sigHelper.AddArgument(typeof(bool), false);
            sigHelper.AddArgument(typeof(System.String[]), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x1c, 0x8d, 0x1f, 0x00, 0x00, 0x01, 0x0a, 0x06, 0x16, 0x18, 0x9e, 0x06, 0x17, 0x1f, 0x09,
                0x9e, 0x06, 0x18, 0x15, 0x9e, 0x06, 0x19, 0x1f, 0x0e, 0x9e, 0x06, 0x1a, 0x19, 0x9e, 0x06, 0x1b,
                0x1f, 0x37, 0x9e, 0x06, 0x14, 0xfe, 0x06, 0x12, 0x00, 0x00, 0x06, 0x73, 0x3a, 0x00, 0x00, 0x0a,
                0x28, 0x06, 0x00, 0x00, 0x2b, 0x0b, 0x1b, 0x8d, 0x0e, 0x00, 0x00, 0x01, 0x13, 0x05, 0x11, 0x05,
                0x16, 0x72, 0x85, 0x02, 0x00, 0x70, 0xa2, 0x11, 0x05, 0x17, 0x72, 0x91, 0x02, 0x00, 0x70, 0xa2,
                0x11, 0x05, 0x18, 0x72, 0x95, 0x02, 0x00, 0x70, 0xa2, 0x11, 0x05, 0x19, 0x72, 0xa1, 0x02, 0x00,
                0x70, 0xa2, 0x11, 0x05, 0x1a, 0x72, 0xbd, 0x02, 0x00, 0x70, 0xa2, 0x11, 0x05, 0x0c, 0x08, 0x73,
                0x18, 0x00, 0x00, 0x06, 0xfe, 0x06, 0x13, 0x00, 0x00, 0x06, 0x73, 0x3b, 0x00, 0x00, 0x0a, 0x28,
                0x07, 0x00, 0x00, 0x2b, 0x0d, 0x07, 0x06, 0x19, 0x94, 0x33, 0x0b, 0x09, 0x08, 0x19, 0x9a, 0x28,
                0x3c, 0x00, 0x00, 0x0a, 0x2b, 0x01, 0x16, 0x13, 0x04, 0x2b, 0x00, 0x11, 0x04, 0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor(typeof(int).TypeHandle);
            int token1 = dynamicILInfo.GetTokenFor(typeof(DynamicILInfoTests).GetMethod("MyRule", BindingFlags.NonPublic | BindingFlags.Static).MethodHandle);
            int token2 = dynamicILInfo.GetTokenFor(typeof(Satisfy<int>).GetConstructor(new Type[] { typeof(System.Object), typeof(System.IntPtr) }).MethodHandle, typeof(Satisfy<int>).TypeHandle);
            int token3 = dynamicILInfo.GetTokenFor(typeof(Finder).GetMethod("Find").MakeGenericMethod(typeof(int)).MethodHandle);
            int token4 = dynamicILInfo.GetTokenFor(typeof(string).TypeHandle);
            int token5 = dynamicILInfo.GetTokenFor("Hello");
            int token6 = dynamicILInfo.GetTokenFor("1");
            int token7 = dynamicILInfo.GetTokenFor("world");
            int token8 = dynamicILInfo.GetTokenFor("dynamicmethod");
            int token9 = dynamicILInfo.GetTokenFor("find it already");
            int token10 = dynamicILInfo.GetTokenFor(typeof(DynamicILInfoTests).GetConstructor(Type.EmptyTypes).MethodHandle);
            int token11 = dynamicILInfo.GetTokenFor(typeof(DynamicILInfoTests).GetMethod("MyRule", BindingFlags.NonPublic | BindingFlags.Instance).MethodHandle);
            int token12 = dynamicILInfo.GetTokenFor(typeof(Satisfy<string>).GetConstructor(new Type[] { typeof(System.Object), typeof(System.IntPtr) }).MethodHandle, typeof(Satisfy<string>).TypeHandle);
            int token13 = dynamicILInfo.GetTokenFor(typeof(Finder).GetMethod("Find").MakeGenericMethod(typeof(string)).MethodHandle);
            int token14 = dynamicILInfo.GetTokenFor(typeof(string).GetMethod("op_Equality").MethodHandle);
            PutInteger4(token0, 0x0003, code);
            PutInteger4(token1, 0x0027, code);
            PutInteger4(token2, 0x002c, code);
            PutInteger4(token3, 0x0031, code);
            PutInteger4(token4, 0x0038, code);
            PutInteger4(token5, 0x0042, code);
            PutInteger4(token6, 0x004b, code);
            PutInteger4(token7, 0x0054, code);
            PutInteger4(token8, 0x005d, code);
            PutInteger4(token9, 0x0066, code);
            PutInteger4(token10, 0x0070, code);
            PutInteger4(token11, 0x0076, code);
            PutInteger4(token12, 0x007b, code);
            PutInteger4(token13, 0x0080, code);
            PutInteger4(token14, 0x0090, code);
            dynamicILInfo.SetCode(code, 4);

            bool ret = (bool)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, GenericMethod());
        }

        private static int TwoDimTest()
        {
            int a = 6;
            int b = 8;
            int[,] array = new int[a, b];
            for (int i = 0; i < a; i++)
                for (int j = 0; j < b; j++)
                    array[i, j] = i * j;

            for (int i = 1; i < a; i++)
                for (int j = 1; j < b; j++)
                    array[i, j] += array[i - 1, j - 1];

            return array[a - 1, b - 1];
        }

        [Fact]
        public void Test_TwoDimTest()
        {
            // 2-D array (set/address/get)
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(TwoDimTest), typeof(int), Type.EmptyTypes, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(System.Int32[,]), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(int), false);
            sigHelper.AddArgument(typeof(bool), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x1c, 0x0a, 0x1e, 0x0b, 0x06, 0x07, 0x73, 0x3e, 0x00, 0x00, 0x0a, 0x0c, 0x16, 0x0d, 0x2b,
                0x27, 0x16, 0x13, 0x04, 0x2b, 0x13, 0x08, 0x09, 0x11, 0x04, 0x09, 0x11, 0x04, 0x5a, 0x28, 0x3f,
                0x00, 0x00, 0x0a, 0x11, 0x04, 0x17, 0x58, 0x13, 0x04, 0x11, 0x04, 0x07, 0xfe, 0x04, 0x13, 0x06,
                0x11, 0x06, 0x2d, 0xe2, 0x09, 0x17, 0x58, 0x0d, 0x09, 0x06, 0xfe, 0x04, 0x13, 0x06, 0x11, 0x06,
                0x2d, 0xcf, 0x17, 0x0d, 0x2b, 0x3c, 0x17, 0x13, 0x04, 0x2b, 0x28, 0x08, 0x09, 0x11, 0x04, 0x28,
                0x40, 0x00, 0x00, 0x0a, 0x25, 0x71, 0x1f, 0x00, 0x00, 0x01, 0x08, 0x09, 0x17, 0x59, 0x11, 0x04,
                0x17, 0x59, 0x28, 0x41, 0x00, 0x00, 0x0a, 0x58, 0x81, 0x1f, 0x00, 0x00, 0x01, 0x11, 0x04, 0x17,
                0x58, 0x13, 0x04, 0x11, 0x04, 0x07, 0xfe, 0x04, 0x13, 0x06, 0x11, 0x06, 0x2d, 0xcd, 0x09, 0x17,
                0x58, 0x0d, 0x09, 0x06, 0xfe, 0x04, 0x13, 0x06, 0x11, 0x06, 0x2d, 0xba, 0x08, 0x06, 0x17, 0x59,
                0x07, 0x17, 0x59, 0x28, 0x41, 0x00, 0x00, 0x0a, 0x13, 0x05, 0x2b, 0x00, 0x11, 0x05, 0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor(typeof(System.Int32[,]).GetConstructor(new Type[] { typeof(int), typeof(int) }).MethodHandle);
            int token1 = dynamicILInfo.GetTokenFor(typeof(System.Int32[,]).GetMethod("Set").MethodHandle);
            int token2 = dynamicILInfo.GetTokenFor(typeof(System.Int32[,]).GetMethod("Address").MethodHandle);
            int token3 = dynamicILInfo.GetTokenFor(typeof(int).TypeHandle);
            int token4 = dynamicILInfo.GetTokenFor(typeof(System.Int32[,]).GetMethod("Get").MethodHandle);
            PutInteger4(token0, 0x0008, code);
            PutInteger4(token1, 0x001f, code);
            PutInteger4(token2, 0x0050, code);
            PutInteger4(token3, 0x0056, code);
            PutInteger4(token4, 0x0063, code);
            PutInteger4(token3, 0x0069, code);
            PutInteger4(token4, 0x0094, code);
            dynamicILInfo.SetCode(code, 6);

            int ret = (int)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, TwoDimTest());
        }

        private class G<T>
        {
            public string M<K>(K arg)
            {
                return typeof(T).ToString() + typeof(K).ToString();
            }
        }
        
        private static string CallGM()
        {
            string s = null;
            G<int> g = new G<int>();
            s += g.M(100);
            s += g.M("hello");

            G<string> g2 = new G<string>();
            s += g2.M(100);
            s += g2.M("world");

            G<Type> g3 = new G<Type>();
            s += g3.M(100L);
            s += g3.M(new object());

            return s;
        }

        [Fact]
        public void Test_CallGM()
        {
            // GenericMethod inside GenericType
            DynamicMethod dynamicMethod = new DynamicMethod(nameof(CallGM), typeof(string), Type.EmptyTypes, typeof(DynamicILInfoTests), false);
            DynamicILInfo dynamicILInfo = dynamicMethod.GetDynamicILInfo();

            SignatureHelper sigHelper = SignatureHelper.GetLocalVarSigHelper();
            sigHelper.AddArgument(typeof(string), false);
            sigHelper.AddArgument(typeof(G<int>), false);
            sigHelper.AddArgument(typeof(G<string>), false);
            sigHelper.AddArgument(typeof(G<System.Type>), false);
            sigHelper.AddArgument(typeof(string), false);
            dynamicILInfo.SetLocalSignature(sigHelper.GetSignature());

            byte[] code = {
                0x00, 0x14, 0x0a, 0x73, 0x42, 0x00, 0x00, 0x0a, 0x0b, 0x06, 0x07, 0x1f, 0x64, 0x6f, 0x09, 0x00,
                0x00, 0x2b, 0x28, 0x2b, 0x00, 0x00, 0x0a, 0x0a, 0x06, 0x07, 0x72, 0x5f, 0x03, 0x00, 0x70, 0x6f,
                0x0a, 0x00, 0x00, 0x2b, 0x28, 0x2b, 0x00, 0x00, 0x0a, 0x0a, 0x73, 0x44, 0x00, 0x00, 0x0a, 0x0c,
                0x06, 0x08, 0x1f, 0x64, 0x6f, 0x0b, 0x00, 0x00, 0x2b, 0x28, 0x2b, 0x00, 0x00, 0x0a, 0x0a, 0x06,
                0x08, 0x72, 0x95, 0x02, 0x00, 0x70, 0x6f, 0x0c, 0x00, 0x00, 0x2b, 0x28, 0x2b, 0x00, 0x00, 0x0a,
                0x0a, 0x73, 0x46, 0x00, 0x00, 0x0a, 0x0d, 0x06, 0x09, 0x1f, 0x64, 0x6a, 0x6f, 0x0d, 0x00, 0x00,
                0x2b, 0x28, 0x2b, 0x00, 0x00, 0x0a, 0x0a, 0x06, 0x09, 0x73, 0x2c, 0x00, 0x00, 0x0a, 0x6f, 0x0e,
                0x00, 0x00, 0x2b, 0x28, 0x2b, 0x00, 0x00, 0x0a, 0x0a, 0x06, 0x13, 0x04, 0x2b, 0x00, 0x11, 0x04,
                0x2a
                };
            int token0 = dynamicILInfo.GetTokenFor(typeof(G<int>).GetConstructor(Type.EmptyTypes).MethodHandle, typeof(G<int>).TypeHandle);
            int token1 = dynamicILInfo.GetTokenFor(typeof(G<int>).GetMethod("M").MakeGenericMethod(typeof(int)).MethodHandle, typeof(G<int>).TypeHandle);
            int token2 = dynamicILInfo.GetTokenFor(typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }).MethodHandle);
            int token3 = dynamicILInfo.GetTokenFor("hello");
            int token4 = dynamicILInfo.GetTokenFor(typeof(G<int>).GetMethod("M").MakeGenericMethod(typeof(string)).MethodHandle, typeof(G<int>).TypeHandle);
            int token5 = dynamicILInfo.GetTokenFor(typeof(G<string>).GetConstructor(Type.EmptyTypes).MethodHandle, typeof(G<string>).TypeHandle);
            int token6 = dynamicILInfo.GetTokenFor(typeof(G<string>).GetMethod("M").MakeGenericMethod(typeof(int)).MethodHandle, typeof(G<string>).TypeHandle);
            int token7 = dynamicILInfo.GetTokenFor("world");
            int token8 = dynamicILInfo.GetTokenFor(typeof(G<string>).GetMethod("M").MakeGenericMethod(typeof(string)).MethodHandle, typeof(G<string>).TypeHandle);
            int token9 = dynamicILInfo.GetTokenFor(typeof(G<System.Type>).GetConstructor(Type.EmptyTypes).MethodHandle, typeof(G<System.Type>).TypeHandle);
            int token10 = dynamicILInfo.GetTokenFor(typeof(G<System.Type>).GetMethod("M").MakeGenericMethod(typeof(long)).MethodHandle, typeof(G<System.Type>).TypeHandle);
            int token11 = dynamicILInfo.GetTokenFor(typeof(System.Object).GetConstructor(Type.EmptyTypes).MethodHandle);
            int token12 = dynamicILInfo.GetTokenFor(typeof(G<System.Type>).GetMethod("M").MakeGenericMethod(typeof(System.Object)).MethodHandle, typeof(G<System.Type>).TypeHandle);
            PutInteger4(token0, 0x0004, code);
            PutInteger4(token1, 0x000e, code);
            PutInteger4(token2, 0x0013, code);
            PutInteger4(token3, 0x001b, code);
            PutInteger4(token4, 0x0020, code);
            PutInteger4(token2, 0x0025, code);
            PutInteger4(token5, 0x002b, code);
            PutInteger4(token6, 0x0035, code);
            PutInteger4(token2, 0x003a, code);
            PutInteger4(token7, 0x0042, code);
            PutInteger4(token8, 0x0047, code);
            PutInteger4(token2, 0x004c, code);
            PutInteger4(token9, 0x0052, code);
            PutInteger4(token10, 0x005d, code);
            PutInteger4(token2, 0x0062, code);
            PutInteger4(token11, 0x006a, code);
            PutInteger4(token12, 0x006f, code);
            PutInteger4(token2, 0x0074, code);
            dynamicILInfo.SetCode(code, 3);

            string ret = (string)dynamicMethod.Invoke(null, null);
            Assert.Equal(ret, CallGM());
        }

        private static void PutInteger4(int value, int startPos, byte[] array)
        {
            array[startPos++] = (byte)value;
            array[startPos++] = (byte)(value >> 8);
            array[startPos++] = (byte)(value >> 16);
            array[startPos++] = (byte)(value >> 24);
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public unsafe void SetX_NullInput_ThrowsArgumentNullException(bool skipVisibility)
        {
            DynamicMethod method = GetDynamicMethod(skipVisibility);
            DynamicILInfo dynamicILInfo = method.GetDynamicILInfo();

            Assert.Throws<ArgumentNullException>(() => dynamicILInfo.SetCode(null, 1, 8));
            Assert.Throws<ArgumentNullException>(() => dynamicILInfo.SetExceptions(null, 1));
            Assert.Throws<ArgumentNullException>(() => dynamicILInfo.SetLocalSignature(null, 1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public unsafe void SetX_NegativeInputSize_ThrowsArgumentOutOfRangeException(bool skipVisibility)
        {
            DynamicMethod method = GetDynamicMethod(skipVisibility);
            DynamicILInfo dynamicILInfo = method.GetDynamicILInfo();
            var bytes = new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02 };

            Assert.Throws<ArgumentOutOfRangeException>(() => {fixed (byte* bytesPtr = bytes) { dynamicILInfo.SetCode(bytesPtr, -1, 8); }});
            Assert.Throws<ArgumentOutOfRangeException>(() => {fixed (byte* bytesPtr = bytes) { dynamicILInfo.SetExceptions(bytesPtr, -1); }});
            Assert.Throws<ArgumentOutOfRangeException>(() => {fixed (byte* bytesPtr = bytes) { dynamicILInfo.SetLocalSignature(bytesPtr, -1); }});
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetDynamicILInfo_NotSameNotNull(bool skipVisibility)
        {
            DynamicMethod method = GetDynamicMethod(skipVisibility);
            DynamicILInfo dynamicILInfo = method.GetDynamicILInfo();
            
            Assert.NotNull(dynamicILInfo);
            Assert.Equal(dynamicILInfo, method.GetDynamicILInfo());
            Assert.Equal(method, dynamicILInfo.DynamicMethod);
        }
        
        private DynamicMethod GetDynamicMethod(bool skipVisibility)
        {
            return new DynamicMethod(nameof(DynamicMethod), typeof(void), 
                new Type[] { typeof(object), typeof(int), typeof(string) },
                typeof(Object),
                skipVisibility);
        }
    }
}