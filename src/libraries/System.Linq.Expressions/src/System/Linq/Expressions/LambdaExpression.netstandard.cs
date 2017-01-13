// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    public partial class Expression<TDelegate> : LambdaExpression
    {
        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile(DebugInfoGenerator debugInfoGenerator)
        {
            return Compile();
        }
    }

    public abstract partial class LambdaExpression : Expression
    {
        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile(DebugInfoGenerator debugInfoGenerator)
        {
            return Compile();
        }
    }
}
