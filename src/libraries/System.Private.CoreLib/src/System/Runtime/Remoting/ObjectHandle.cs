// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Remoting
{
    public class ObjectHandle : MarshalByRefObject
    {
        private readonly object? _wrappedObject;

        public ObjectHandle(object? o)
        {
            _wrappedObject = o;
        }

        public object? Unwrap()
        {
            return _wrappedObject;
        }
    }
}
