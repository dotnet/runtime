// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Remoting
{
    public class ObjectHandle : MarshalByRefObject        
    {
        private object? _wrappedObject;

        private ObjectHandle()
        {
        }

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
