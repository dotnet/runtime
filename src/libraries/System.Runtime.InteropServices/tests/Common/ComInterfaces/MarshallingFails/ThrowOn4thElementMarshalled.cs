// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces.MarshallingFails
{
    [CustomMarshaller(typeof(int), MarshalMode.ElementIn, typeof(ThrowOn4thElementMarshalled))]
    [CustomMarshaller(typeof(int), MarshalMode.ElementOut, typeof(ThrowOn4thElementMarshalled))]
    internal static class ThrowOn4thElementMarshalled
    {
        static int _marshalledCount = 0;
        static int _unmarshalledCount = 0;
        public static int FreeCount { get; private set; }
        public static nint ConvertToUnmanaged(int managed)
        {
            if (_marshalledCount++ == 3)
            {
                _marshalledCount = 0;
                throw new MarshallingFailureException("The element was the 4th element (with 0-based index 3)");
            }
            return managed;
        }

        public static int ConvertToManaged(nint unmanaged)
        {
            if (_unmarshalledCount++ == 3)
            {
                _unmarshalledCount = 0;
                throw new MarshallingFailureException("The element was the 4th element (with 0-based index 3)");
            }
            return (int)unmanaged;
        }
        public static void Free(nint unmanaged) => ++FreeCount;
    }

}
