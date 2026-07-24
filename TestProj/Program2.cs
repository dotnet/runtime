using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

interface ITest {
    void Foo([In, Out, MarshalUsing(CountElementName = "len")] int[] arr, int len);
}

class Test : ITest {
    void ITest.Foo([In, Out, MarshalUsing(CountElementName = "len")] int[] arr, int len) { }
}
