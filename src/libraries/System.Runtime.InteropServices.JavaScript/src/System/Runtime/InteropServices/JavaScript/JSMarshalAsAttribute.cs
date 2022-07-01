// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Should be used to specify type of JavaScript object, which should be result of argument marshaling.
    /// </summary>
    /// <typeparam name="T">One of the types defined in <see cref="JSType"/>, for example <see cref="JSType.MemoryView"/></typeparam>
    /// <example>
    /// <code>
    /// [JSImport("createFunction", "my-math-helper")]
    /// [return: JSMarshalAs&lt;JSType.Function&lt;JSType.Number, JSType.Number, JSType.Number&gt;&gt;]
    /// public static partial Func&lt;int, int, int&gt; createMath(int a, int b, string code);
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false, AllowMultiple = false)]
    [SupportedOSPlatform("browser")]
    public sealed class JSMarshalAsAttribute<T> : Attribute where T : JSType
    {
        /// <summary>
        /// Create a <see cref="JSMarshalAsAttribute{T}" /> configured by generic parameters of <see cref="JSType" />.
        /// </summary>
        public JSMarshalAsAttribute() { }
    }
}
