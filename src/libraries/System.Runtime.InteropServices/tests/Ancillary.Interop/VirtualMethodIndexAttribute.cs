// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class VirtualMethodIndexAttribute : Attribute
    {
        public VirtualMethodIndexAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public bool ImplicitThisParameter { get; set; } = true;

        public MarshalDirection Direction { get; set; } = MarshalDirection.Bidirectional;

        /// <summary>
        /// Gets or sets how to marshal string arguments to the method.
        /// </summary>
        /// <remarks>
        /// If this field is set to a value other than <see cref="StringMarshalling.Custom" />,
        /// <see cref="StringMarshallingCustomType" /> must not be specified.
        /// </remarks>
        public StringMarshalling StringMarshalling { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> used to control how string arguments to the method are marshalled.
        /// </summary>
        /// <remarks>
        /// If this field is specified, <see cref="StringMarshalling" /> must not be specified
        /// or must be set to <see cref="StringMarshalling.Custom" />.
        /// </remarks>
        public Type? StringMarshallingCustomType { get; set; }

        /// <summary>
        /// Gets or sets whether the callee sets an error (SetLastError on Windows or errno
        /// on other platforms) before returning from the attributed method.
        /// </summary>
        public bool SetLastError { get; set; }
    }
}
