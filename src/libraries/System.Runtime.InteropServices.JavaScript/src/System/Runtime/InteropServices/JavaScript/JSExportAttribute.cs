// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Indicates that a source generator should export the attributed method to JavaScript and create thunks necessary to marshal its arguments and any return value or thrown exception.
    /// </summary>
    /// <remarks>
    /// For marshaling arguments of complex types <see cref="JSMarshalAsAttribute{T}" />.
    /// This attribute is meaningless if the source generator associated with it is not enabled.
    /// The current built-in source generator only supports C# and only supplies an implementation when applied to static, non-partial, or non-generic methods.
    /// applied to static, non-partial, non-generic methods.
    /// Exported methods cannot be trimmed by the ILLink.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    [SupportedOSPlatform("browser")]
    public sealed class JSExportAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JSExportAttribute" /> class.
        /// </summary>
        public JSExportAttribute()
        {
        }
    }
}
