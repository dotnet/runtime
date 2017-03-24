using System;

namespace Microsoft.CSharp.RuntimeBinder
{
    // This class is used to keep the tuple of runtime object values and 
    // the type that we want to use for the argument. This is different than the runtime
    // value's type because unless the static time type was dynamic, we want to use the
    // static time type. Also, we may have null values, in which case we would not be 
    // able to get the type.
    internal sealed class ArgumentObject
    {
        internal Type Type;
        internal object Value;
        internal CSharpArgumentInfo Info;
    }
}