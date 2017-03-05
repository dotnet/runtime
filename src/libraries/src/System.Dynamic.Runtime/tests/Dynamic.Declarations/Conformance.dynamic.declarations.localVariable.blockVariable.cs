// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.anonmethod001.anonmethod001
{
    public class Test
    {
        public delegate int Del(object x);
        private class Foo
        {
            public event Del Delete;
            public int Raise()
            {
                int x = Delete(3);
                return x;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            f.Delete += delegate (dynamic d)
            {
                return (int)d;
            }

            ;
            int x = f.Raise();
            if ((int)x != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.anonmethod002.anonmethod002
{
    public class Test
    {
        public delegate int Del(dynamic x);
        private class Foo
        {
            public event Del Delete;
            public int Raise()
            {
                int x = Delete(3);
                return x;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            f.Delete += delegate (dynamic d)
            {
                return (int)d;
            }

            ;
            int x = f.Raise();
            if ((int)x != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.for001.for001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int i = 0;
            for (dynamic d = 0; d < 3; d = d + 1)
            {
                d = i;
                if (d != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.for002.for002
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myFor
    {
        private int _index;
        private int _max;
        public void Initialize(int max)
        {
            _index = 0;
            _max = max;
        }

        public bool Done()
        {
            return _index < _max;
        }

        public void Next()
        {
            _index++;
        }

        public int Current()
        {
            return _index;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myFor();
            int i = 0;
            for (d.Initialize(5); d.Done(); d.Next())
            {
                if ((int)d.Current() != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.for003.for003
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myFor
    {
        private int _index;
        private int _max;
        public void Initialize(int max)
        {
            _index = 0;
            _max = max;
        }

        public bool Done
        {
            get
            {
                return _index < _max;
            }
        }

        public void Next()
        {
            _index++;
        }

        public int Current
        {
            get
            {
                return _index;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myFor();
            int i = 0;
            for (d.Initialize(5); d.Done; d.Next())
            {
                if ((int)d.Current != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.for004.for004
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myFor
    {
        private int _index;
        private int _max;
        public void Initialize(int max)
        {
            _index = 0;
            _max = max;
        }

        public static implicit operator bool (myFor x)
        {
            return x.Done;
        }

        public bool Done
        {
            get
            {
                return _index < _max;
            }
        }

        public void Next()
        {
            _index++;
        }

        public int Current
        {
            get
            {
                return _index;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myFor();
            int i = 0;
            for (d.Initialize(5); d; d.Next())
            {
                if ((int)d.Current != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.for005.for005
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myFor
    {
        private int _index;
        private int _max;
        public myFor(int max)
        {
            _index = 0;
            _max = max;
        }

        public bool Done
        {
            get
            {
                return _index < _max;
            }
        }

        public void Next()
        {
            _index++;
        }

        public int Current
        {
            get
            {
                return _index;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int i = 0;
            for (dynamic d = new myFor(5); d.Done; d.Next())
            {
                if ((int)d.Current != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.for006.for006
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // We only implement the op_true and op_false operators, but not a conversion from myFor to bool
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myFor
    {
        private int _index;
        private int _max;
        public myFor(int max)
        {
            _index = 0;
            _max = max;
        }

        public void Next()
        {
            _index++;
        }

        public int Current
        {
            get
            {
                return _index;
            }
        }

        public static bool operator true(myFor d)
        {
            return d._index < d._max;
        }

        public static bool operator false(myFor d)
        {
            return d._index >= d._max;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int i = 0;
            for (dynamic d = new myFor(5); d; d.Next())
            {
                if ((int)d.Current != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach001.freach001
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new List<int>()
            {
            1, 2, 3, 4, 5
            }

            ;
            int i = 1;
            foreach (dynamic item in list)
            {
                if ((int)item != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach002.freach002
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int i = 1;
            foreach (dynamic item in new List<int>()
            {
            1, 2, 3, 4, 5
            }

            )
            {
                if ((int)item != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach003.freach003
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int i = 1;
            foreach (dynamic item in new List<dynamic>()
            {
            1, 2, 3, 4, 5
            }

            )
            {
                if ((int)item != i)
                    return 1;
                i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach005.freach005
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(21,13\).*CS0219</Expects>
    using System.Collections.Generic;

    public class Foo
    {
        public void Bar()
        {
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new List<object>()
            {
            new Foo(), new Foo()}

            ;
            int i = 1;
            foreach (dynamic item in list)
            {
                item.Bar();
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach006.freach006
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class Foo : IEnumerable, IEnumerator
    {
        private int _x = 2;
        public void Bar()
        {
        }

        #region IEnumerable Members
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        #endregion
        #region IEnumerator Members
        public object Current
        {
            get
            {
                return _x;
            }
        }

        public bool MoveNext()
        {
            _x--;
            if (_x < 0)
                return false;
            else
                return true;
        }

        public void Reset()
        {
            _x = 2;
        }
        #endregion
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new Foo();
            int i = 1;
            foreach (var item in list)
            {
                if ((int)item != i--)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach007.freach007
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class Foo
    {
        private int _x = 2;
        public void Bar()
        {
        }

        #region IEnumerable Members
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        #endregion
        #region IEnumerator Members
        public object Current
        {
            get
            {
                return _x;
            }
        }

        public bool MoveNext()
        {
            _x--;
            if (_x < 0)
                return false;
            else
                return true;
        }

        public void Reset()
        {
            _x = 2;
        }
        #endregion
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new Foo();
            int i = 1;
            try
            {
                foreach (var item in list)
                {
                    if ((int)item != i--)
                        return 1;
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Foo", "System.Collections.IEnumerable"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach008.freach008
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Foo
    {
        public void Bar(int i)
        {
            Test.Status = i;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new List<dynamic>()
            {
            new Foo(), new Foo()}

            ;
            int i = 1;
            foreach (var item in list)
            {
                item.Bar(i);
                if (Test.Status != i++)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach009.freach009
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Foo
    {
        public void Bar(int i)
        {
            Test.Status = i;
        }
    }

    public class Foo2
    {
        public void Bar(int i)
        {
            Test.Status = i;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new List<dynamic>()
            {
            new Foo(), new Foo2()}

            ;
            int i = 1;
            foreach (var item in list)
            {
                item.Bar(i);
                if (Test.Status != i++)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.freach010.freach010
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Foo
    {
        public void Bar(int i)
        {
            Test.Status = i;
        }
    }

    public class Foo2
    {
        public void Bar(int i)
        {
            Test.Status = i;
        }
    }

    public class Test
    {
        public static int Status;
        public static void Method(Foo x)
        {
            Test.Status = 1;
        }

        public static void Method(Foo2 y)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new List<dynamic>()
            {
            new Foo(), new Foo2()}

            ;
            int i = 1;
            foreach (var item in list)
            {
                Test.Method(item);
                if (Test.Status != i++)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.lock001.lock001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int x = 2;
            dynamic d = new object();
            lock (d)
            {
                x = 4;
            }

            if (x != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.lock002.lock002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 2;
            object o = d;
            int x;
            lock (o)
            {
                x = 4;
            }

            if (x != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.negfreach001.negfreach001
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private class MyClass
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic list = new MyClass();
            int i = 1;
            try
            {
                foreach (dynamic item in list)
                {
                    if ((int)item != i)
                        return 1;
                    i++;
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Test.MyClass", "System.Collections.IEnumerable"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.negusing001.negusing001
{
    public class Test
    {
        private class MyClass
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            // string s = "Test";
            try
            {
                using (dynamic d = new MyClass())
                {
                    //<-this should work?!
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Test.MyClass", "System.IDisposable"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.query001.query001
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int[] numbers = new int[]
            {
            1, 2, 3, 4, 5
            }

            ;
            var v =
                from dynamic d in numbers
                select (int)d;
            int x = 0;
            foreach (var i in v)
            {
                if (numbers[x++] != i)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.query002.query002
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int[] numbers = new int[]
            {
            1, 2, 3, 4, 5
            }

            ;
            var v =
                from d in numbers
                let f = (dynamic)3
                select (int)f;
            foreach (var i in v)
            {
                if (i != 3)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.trycatch002.trycatch002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            try
            {
                dynamic d = 3;
                string s = (string)d.ToString();
                if (s != "3")
                    return 1;
            }
            catch
            {
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.using001.using001
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class MyClass : IDisposable
    {
        public static int Status;
        public void Foo()
        {
            MyClass.Status = 2;
        }

        public void Dispose()
        {
            if (MyClass.Status == 2)
                MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            using (dynamic d = new MyClass())
            {
                d.Foo();
            }

            if (MyClass.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.using002.using002
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class MyClass : IDisposable
    {
        public static int Status;
        public void Foo()
        {
            MyClass.Status = 2;
        }

        public void Dispose()
        {
            if (MyClass.Status == 2)
                MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            using (dynamic d = new MyClass())
            {
                d.Foo();
            }

            if (MyClass.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.using003.using003
{
    // <Title> Simple dynamic declarations </Title>
    // <Description> Was compiler time checking - CS1674
    //      runtime check to give dynamic object (e.g. IDO) an Opportunity to cast
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class MyClass
    {
        public static int Status;
        public void Foo()
        {
            MyClass.Status = 2;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            try
            {
                using (dynamic d = new MyClass())
                {
                    d.Foo();
                    if (MyClass.Status == 1)
                        return 0;
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "MyClass", "System.IDisposable"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.localVariable.blockVariable.using005.using005
{
    // <Title> Simple dynamic declarations </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class MyClass : IDisposable
    {
        public static int Status;
        public void Foo()
        {
            MyClass.Status = 2;
        }

        public void Dispose()
        {
            if (MyClass.Status == 2)
                MyClass.Status = 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = new MyClass();
            dynamic d = o;
            using (d)
            {
                d.Foo();
            }

            if (MyClass.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}
