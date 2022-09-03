// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Indicates that the JSImport source generator should create a managed wrapper to invoke a specific imported JavaScript function and marshal its arguments, return values, and exceptions.
    /// To configure the marshaling behavior for specific values, <seealso cref="JSMarshalAsAttribute{T}"/>.
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
    /// [JSImport("globalThis.Math.sum")]
    /// public static partial int globalSum(int a, int b);
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    [SupportedOSPlatform("browser")]
    public sealed class JSImportAttribute : Attribute
    {
        /// <summary>
        /// The name of the target JavaScript function. This name will be used as a key to locate the function in the module.
        /// Functions nested inside of objects can be referred to by using the dot operator to connect one or more names.
        /// </summary>
        public string FunctionName { get; }

        /// <summary>
        /// Globally unique identifier of the ES6 module, if any, that contains the function. The module must be loaded via <see cref="JSHost.ImportAsync(string, string, Threading.CancellationToken)"/> before any attempt to invoke the function.
        /// </summary>
        public string? ModuleName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSImportAttribute"/>.
        /// </summary>
        /// <param name="functionName">Name of the function to be bound in the module. It allows dots for nested objects.</param>
        public JSImportAttribute(string functionName)
        {
            FunctionName = functionName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSImportAttribute"/>.
        /// </summary>
        /// <param name="functionName">
        /// The name of the target JavaScript function. This name will be used as a key to locate the function in the module.
        /// Functions nested inside of objects can be referred to by using the dot operator to connect one or more names.
        /// </param>
        /// <param name="moduleName">
        /// Globally unique identifier of the ES6 module, if any, that contains the function. The module must be loaded via <see cref="JSHost.ImportAsync(string, string, Threading.CancellationToken)"/> before any attempt to invoke the function.
        /// </param>
        public JSImportAttribute(string functionName, string moduleName)
        {
            FunctionName = functionName;
            ModuleName = moduleName;
        }
    }
}
