// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public interface IHostObject
        {

        }
        public class HostObject : HostObjectBase
        {
            public HostObject(string hostName, params object[] _params) : base(Runtime.New(hostName, _params))
            { }
        }

        public abstract class HostObjectBase : JSObject, IHostObject
        {

            protected HostObjectBase(int js_handle) : base(js_handle)
            {
                var result = Runtime.BindHostObject(js_handle, (int)(IntPtr)Handle, out int exception);
                if (exception != 0)
                    throw new JSException($"HostObject Error binding: {result.ToString()}");

            }

            internal HostObjectBase(IntPtr js_handle) : base(js_handle)
            { }


        }
    }
}
