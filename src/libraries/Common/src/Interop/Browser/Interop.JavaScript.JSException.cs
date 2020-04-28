using System;
internal static partial class Interop
{

    internal static partial class JavaScript
    {
        public class JSException : Exception
        {
            public JSException(string msg) : base(msg) { }
        }
    }
}
