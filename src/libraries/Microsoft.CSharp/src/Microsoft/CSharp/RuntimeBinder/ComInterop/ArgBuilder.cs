// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    /// <summary>
    /// ArgBuilder provides an argument value used by the MethodBinder.  One ArgBuilder exists for each
    /// physical parameter defined on a method.
    ///
    /// Contrast this with ParameterWrapper which represents the logical argument passed to the method.
    /// </summary>
    internal abstract class ArgBuilder
    {
        /// <summary>
        /// Provides the Expression which provides the value to be passed to the argument.
        /// </summary>
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal abstract Expression Marshal(Expression parameter);

        /// <summary>
        /// Provides the Expression which provides the value to be passed to the argument.
        /// This method is called when result is intended to be used ByRef.
        /// </summary>
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal virtual Expression MarshalToRef(Expression parameter)
        {
            return Marshal(parameter);
        }

        /// <summary>
        /// Provides an Expression which will update the provided value after a call to the method.
        /// May return null if no update is required.
        /// </summary>
        internal virtual Expression UnmarshalFromRef(Expression newValue)
        {
            return newValue;
        }
    }
}
