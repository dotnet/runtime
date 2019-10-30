// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Generates debug information for lambdas in an expression tree.
    /// </summary>
    public abstract class DebugInfoGenerator
    {
        /// <summary>
        /// Creates PDB symbol generator.
        /// </summary>
        /// <returns>PDB symbol generator.</returns>
        public static DebugInfoGenerator CreatePdbGenerator()
        {
            // Creating PDBs is not supported in .NET Core
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Marks a sequence point.
        /// </summary>
        /// <param name="method">The lambda being generated.</param>
        /// <param name="ilOffset">IL offset where to mark the sequence point.</param>
        /// <param name="sequencePoint">Debug information corresponding to the sequence point.</param>
        public abstract void MarkSequencePoint(LambdaExpression method, int ilOffset, DebugInfoExpression sequencePoint);

#if FEATURE_COMPILE
        internal virtual void MarkSequencePoint(LambdaExpression method, MethodBase methodBase, ILGenerator ilg, DebugInfoExpression sequencePoint)
        {
            MarkSequencePoint(method, ilg.ILOffset, sequencePoint);
        }

        internal virtual void SetLocalName(LocalBuilder localBuilder, string name)
        {
            // nop
        }
#endif
    }
}