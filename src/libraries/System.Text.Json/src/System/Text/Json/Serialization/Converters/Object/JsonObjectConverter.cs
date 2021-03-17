// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for non-enumerable, non-primitive objects where public properties
    /// are (de)serialized as a JSON object.
    /// </summary>
    internal abstract class JsonObjectConverter<T> : JsonResumableConverter<T>
    {
        internal JsonObjectConverter()
        {
            // Populate ElementType if the runtime type implements IAsyncEnumerable.
            // Used to feed the (converter-agnostic) JsonClassInfo.ElementType instace
            // which is subsequently consulted by custom enumerable converters fed via JsonConverterAttribute.
            if (IAsyncEnumerableConverterFactory.TryGetAsyncEnumerableInterface(typeof(T), out Type? asyncEnumerableInterface))
            {
                ElementType = asyncEnumerableInterface.GetGenericArguments()[0];
            }
        }

        internal sealed override ClassType ClassType => ClassType.Object;
        internal sealed override Type? ElementType { get; }
    }
}
