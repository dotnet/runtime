using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.IO;
using System.Xml.Linq;

[assembly: TestDll.TestAttribute]

namespace TestDll
{
    enum TestEnum
    {
        A,
        B, 
        C
    }

    enum TestEnumCustomSize: byte
    {
        A,
        B, 
        C
    }
    
    class TestAttribute : Attribute
    {

    }
    
    class InheritedTestAttribute : TestAttribute
    {

    }
    
    class TestWithParamsAttribute : Attribute
    {
        public TestWithParamsAttribute(int _i, string _s, bool _b, float _f)
        {
            i = _i;
            s = _s;
            b = _b;
            f = _f;
        }

        public int i;
        public string s;
        public bool b;
        public float f;
    }

    class AnotherTestAttribute : Attribute
    {

    }

    public interface TestInterface
    {
        int Method();
    }

    public class ClassImplementingInterface : TestInterface
    {
        public int Method()
        {
            return 42;
        }
    }
    
    public class ClassDerivingFromClassImplementingInterface : ClassImplementingInterface
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithSequentialLayout
    {
        public int a;
        public object b;
        public bool c;
        public float d;
        public string e;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    public class ClassWithExplicitLayout
    {
        [FieldOffset(0)] public int a;
        [FieldOffset(8)] public object b;
        [FieldOffset(16)] public bool c;
        [FieldOffset(20)] public float d;
        [FieldOffset(24)] public string e;
    }    
    
    [StructLayout(LayoutKind.Explicit)]
    public class DerivedClassWithExplicitLayout : ClassWithExplicitLayout
    {
        [FieldOffset(32+0)] public int a;
        [FieldOffset(32+8)] public object b;
        [FieldOffset(32+16)] public bool c;
        [FieldOffset(32+20)] public float d;
        [FieldOffset(32+24)] public string e;
    }     
    
    public struct StructImplementingInterface : TestInterface
    {
        public int i;

        public void Setup()
        {
            i = 42;
        }
        
        public int Method()
        {
            return i;
        }
    }    

    public abstract class BaseClass
    {
        public abstract int Method();
    }

    public class InheritedClass : BaseClass
    {
        public override int Method()
        {
            return 42;
        }
    }
    
    public class GenericClass<T>
    {
        public T genericField;
        public T[] genericArrayField;
    }
    
    public class GenericStringInstance : GenericClass<string>
    {
    }      
    
    public class ClassWithNestedClass
    {
        public class NestedClass
        {

        }
    }

    public class GenericClassWithNestedClass<T>
    {
        public class NestedClass
        {

        }
    }
    
    [TestAttribute]
    [InheritedTestAttribute]
    [TestWithParamsAttribute(42, "foo", true, 1.0f)]
    public class ClassWithAttribute
    {
        [TestAttribute]
        public void MethodWithAttribute()
        {
            
        }
    }
    
    [InheritedTestAttribute]
    public class ClassWithInheritedAttribute
    {
    }    

    public class TestClassWithMethods
    {
        void A()
        {
        }

        int B()
        {
            return 0;
        }

        float C(float a, float b)
        {
            return a + b;
        }
    }
    
    public class TestClassWithConstructor
    {
        private int i;

        TestClassWithConstructor()
        {
            i = 42;
        }
            
        int GetI()
        {
            return i;
        }
    }    

    public class TestClassWithFields
    {
        public int x = 123;
        private int y = 456;
        private static int z;
        [NonSerialized]
        protected int w;

        void SetupFields()
        {
            x = 123;
            y = 456;
        }
    }
    
    public class TestClassWithReferenceField
    {
        public TestClassWithReferenceField reference = null;

        TestClassWithReferenceField GetField()
        {
            return reference;
        }
    }    
    
    public struct TestStructWithFields
    {
        public int x;
        private int y;
        private static int z;

        void SetupFields()
        {
            x = 123;
            y = 456;
        }

        int SumFields()
        {
            return x + y;
        }
    }

    public class ClassWithStructFields
    {
        private TestStructWithFields a, b;
        private StructImplementingInterface c;
        private StructImplementingInterface d, e, f, g;

        public void Setup()
        {
            c.i = 42;
            d.i = 43;
            e.i = 44;
            f.i = 45;
            g.i = 46;
        }
    }

    public class BaseClassWithFields
    {
        public int x;
        private int y;
        private static int z;
    }

    public class DerivedClassWithFields : BaseClassWithFields
    {
        public int a;
        private int b;
        private static int c;
    }
    
    public class TestException : Exception
    {
    }

    public class TestClassWithFinalizer
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void FinalizerCalled();

        ~TestClassWithFinalizer()
        {
            FinalizerCalled();
        }
    }

    public class ICallTest
    {
        public class NestedClass
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            public static extern int InternalMethodInNestedClass();
        }
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int InternalMethod();
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern string InternalMethodReturnsStackTrace();
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalMethodWhichThrows();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalMethodWhichBlocks();
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalMethodWhichReturnsExceptionInRefParam(ref Exception e);

        [DllImport("foo.lib", CallingConvention = CallingConvention.Cdecl)]
        private static extern int DllImportFunction(int a, int b);
        
        static string ReturnStackTrace()
        {
            var s = new StackTrace();
            return s.ToString();
        }

        static int CallInternalMethod()
        {
            return InternalMethod();
        }
        
        static public void CallInternalMethodWhichBlocks()
        {
            InternalMethodWhichBlocks();
        }        
        
        static string CallInternalMethodReturnsStackTrace()
        {
            return InternalMethodReturnsStackTrace();
        }
        
        static int CallInternalMethodInNestedClass()
        {
            return NestedClass.InternalMethodInNestedClass();
        }
        
        static string CallInternalMethodWhichThrowsAndCatchExceptionMono()
        {
            try
            {
                InternalMethodWhichThrows();
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        } 
        
        static string CallInternalMethodWhichThrowsAndCatchExceptionCoreCLR()
        {
            try
            {
                Exception e = null;
                InternalMethodWhichReturnsExceptionInRefParam(ref e);
                if (e != null)
                    throw e;
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        static int CallDllImportFunction(int a, int b)
        {
            return DllImportFunction(a, b);
        }
    }

    public class ThreadTest
    {
        static void ThrowException()
        {
            throw new Exception("Exception thrown");
        }        
        
        static bool RunThreadWhichThrows()
        {
            var thread = new Thread(ThrowException) { IsBackground = true };
            thread.Start();
            thread.Join();
            return true;
        }
        
        static void RunThreadWhichBlocksInInternalMethod()
        {
            var thread = new Thread(ICallTest.CallInternalMethodWhichBlocks) { IsBackground = true };
            thread.Start();
        }        
    }
    
    public class TestClass
    {
        static public int StaticMethodReturningInt()
        {
            return 42;
        }

        static public int StaticPrivateMethodReturningInt()
        {
            return 42;
        }

        static public int StaticMethodWithTwoArgsReturningInt(int a, int b)
        {
            return a + b;
        }

        static public float StaticMethodWithTwoArgsReturningFloat(float a, float b)
        {
            return a + b;
        }

        static public Object StaticMethodWithTwoArgsReturningObject(Object o)
        {
            return o;
        }

        public float MethodWithTwoArgsReturningFloat(float a, float b)
        {
            return a + b;
        }

        public int MethodWithTwoArgsReturningInt(float a, float b)
        {
            return (int)(a + b);
        }
        
        public float AnotherMethodWithTwoArgsReturningFloat(float a, float b)
        {
            return a + b;
        }
        
        static public Guid StaticMethodReturningGUID()
        {
            return Guid.Parse("81a130d2-502f-4cf1-a376-63edeb000e9f");
        }

        static public Guid StaticMethodWithGUIDArg(Guid arg)
        {
            return arg;
        }

        static public unsafe void* StaticMethodWithPtrArg(void* ptr)
        {
            return ptr;
        }

        static public void StaticMethodWithObjectOutArg(Object a, out Object b)
        {
            b = a;
        }

        static public int StaticMethodWithStringArg(string s)
        {
            return s.Length;
        }

        static int[] StaticMethodReturningArray()
        {
            return new[] {1, 2, 3, 4, 5, 6};
        }
        
        static int[,] StaticMethodReturning2DArray()
        {
            return new[,] {{1, 2, 3}, {4, 5, 6}};
        }

        private static int StaticIntProperty { get; set; }
        internal int IntProperty
        {
            get { return 0; }
        }

        public static string ReadAllTextSafe(string path)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
            
            return null;
        }
    }

    public class DerivedClass : TestClass
    {
    }

    public class XmlTest
    {
        static bool TestParseXmlWithWin1252Encoding()
        {
            try
            {
                XDocument.Load("Test.xml", LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }
    }

    public class ClassWithFields
    {
        public sbyte _sbyte;
        public byte _byte;
        public short _short;
        public ushort _ushort;
        public int _int;
        public uint _uint;
        public long _long;
        public ulong _ulong;

        public float _float;
        public double _double;

        public bool _bool;
        public char _char;

        public string _string;
        public object _object;

        public ClassWithFields _class;

    }
}
