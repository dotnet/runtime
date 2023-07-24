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
    // This type is only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
    // These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).
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

        /// <summary>
        /// Gets or sets how exceptions should be handled for the <see cref="MarshalDirection.UnmanagedToManaged"/> stub.
        /// </summary
        /// <remarks>
        /// If this field is set to a value other than <see cref="ExceptionMarshalling.Custom" />,
        /// <see cref="ExceptionMarshallingType" /> must not be specified.
        /// </remarks>
        public ExceptionMarshalling ExceptionMarshalling { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> used to control how an exception is marshalled to the return value.
        /// </summary>
        /// <remarks>
        /// If this field is specified, <see cref="ExceptionMarshalling" /> must not be specified
        /// or must be set to <see cref="ExceptionMarshalling.Custom" />.
        /// </remarks>
        public Type? ExceptionMarshallingType { get; set; }
    }
}
