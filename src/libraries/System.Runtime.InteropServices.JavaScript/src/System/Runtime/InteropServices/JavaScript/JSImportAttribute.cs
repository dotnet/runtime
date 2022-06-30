// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Attribute used to indicate a source generator should import the function from JavaScript and create thunks necessary to marshal the arguments, results and exceptions.
    /// For marshaling arguments of complex types <seealso cref="JSMarshalAsAttribute{T}"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is meaningless if the source generator associated with it is not enabled.
    /// The current built-in source generator only supports C# and only supplies an implementation when
    /// applied to static, partial, non-generic methods.
    /// </remarks>
    /// <example>
    /// <code>
    /// [JSImport("sum", "my-math-helper")]
    /// public static partial int librarySum(int a, int b);
    /// [JSImport("Math.sum", "my-math-helper")]
    /// public static partial int libraryNamespaceSum(int a, int b);
    /// [JSImport("IMPORTS.sum")]
    /// public static partial int runtimeImportsSum(int a, int b);
    /// [JSImport("globalThis.Math.sum")]
    /// public static partial int globalSum(int a, int b);
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    [SupportedOSPlatform("browser")]
    public sealed class JSImportAttribute : Attribute
    {
        /// <summary>
        /// Name of the function to be bound in the IMPORTS object of the runtime instance in the JavaScript page. It allows dots for nested objects.
        /// </summary>
        public string FunctionName { get; }

        /// <summary>
        /// Globally unique identifier of the ES6 module, which need to be loaded by <see cref="JSHost.ImportAsync(string, string, Threading.CancellationToken)"/> before first use.
        /// </summary>
        public string? ModuleName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSImportAttribute"/>.
        /// </summary>
        /// <param name="functionName">Name of the function to be bound in the IMPORTS object of the runtime instance in the JavaScript page. It allows dots for nested objects.</param>
        public JSImportAttribute(string functionName)
        {
            FunctionName = functionName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSImportAttribute"/>.
        /// </summary>
        /// <param name="functionName">Name of the function to be bound in the IMPORTS object of the runtime instance in the JavaScript page.</param>
        /// <param name="moduleName">Globally unique identifier of the ES6 module, which need to be loaded by <see cref="JSHost.ImportAsync(string, string, Threading.CancellationToken)"/> before first use.</param>
        public JSImportAttribute(string functionName, string moduleName)
        {
            FunctionName = functionName;
            ModuleName = moduleName;
        }
    }
}
