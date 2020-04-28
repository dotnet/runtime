using System;
internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public class Array : CoreObject
        {
            public Array(params object[] _params) : base(Runtime.New<Array>(_params))
            { }
            internal Array(IntPtr js_handle) : base(js_handle)
            { }
            public int Push(params object[] elements) => (int)Invoke("push", elements);
            public object Pop() => (object)Invoke("pop");
            public object Shift() => Invoke("shift");
            public int UnShift(params object[] elements) => (int)Invoke("unshift", elements);
            public int IndexOf(object searchElement, int fromIndex = 0) => (int)Invoke("indexOf", searchElement, fromIndex);
            public int LastIndexOf(object searchElement) => (int)Invoke("lastIndexOf", searchElement);
            public int LastIndexOf(object searchElement, int endIndex) => (int)Invoke("lastIndexOf", searchElement, endIndex);
            public object this[int i]
            {
                get
                {
                    var indexValue = Runtime.GetByIndex(JSHandle, i, out int exception);

                    if (exception != 0)
                        throw new JSException((string)indexValue);
                    return indexValue;
                }
                set
                {
                    var res = Runtime.SetByIndex(JSHandle, i, value, out int exception);

                    if (exception != 0)
                        throw new JSException((string)res);

                }
            }
        }
    }
}
