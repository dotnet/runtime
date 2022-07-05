// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Attribute used to indicate a source generator should export a method to JavaScript and create thunks necessary to marshal its arguments along with any return value or thrown exception.
    /// For marshaling arguments of complex types <seealso cref="JSMarshalAsAttribute{T}"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is meaningless if the source generator associated with it is not enabled.
    /// The current built-in source generator only supports C# and only supplies an implementation when
    /// applied to static, non-partial, non-generic methods.
    /// Exported methods cannot be trimmed by the linker.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    [SupportedOSPlatform("browser")]
    public sealed class JSExportAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JSExportAttribute"/>.
        /// </summary>
        public JSExportAttribute()
        {
        }
    }
}
