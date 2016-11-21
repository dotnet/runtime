// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Compiler
{

    // Suppose we have something like:
    //
    //    (string s)=>()=>s.
    //
    // We wish to generate the outer as:
    //
    //      Func<string> OuterMethod(Closure closure, string s)
    //      {
    //          object[] locals = new object[1];
    //          locals[0] = new StrongBox<string>();
    //          ((StrongBox<string>)locals[0]).Value = s;
    //          return ((DynamicMethod)closure.Constants[0]).CreateDelegate(typeof(Func<string>), new Closure(null, locals));
    //      }
    //
    // ... and the inner as:
    //
    //      string InnerMethod(Closure closure)
    //      {
    //          object[] locals = closure.Locals;
    //          return ((StrongBox<string>)locals[0]).Value;
    //      }
    //
    // This class tracks that "s" was hoisted into a closure, as the 0th
    // element in the array
    //
    /// <summary>
    /// Stores information about locals and arguments that are hoisted into
    /// the closure array because they're referenced in an inner lambda.
    ///
    /// This class is sometimes emitted as a runtime constant for internal
    /// use to hoist variables/parameters in quoted expressions
    ///
    /// Invariant: this class stores no mutable state
    /// </summary>
    internal sealed class HoistedLocals
    {
        // The parent locals, if any
        internal readonly HoistedLocals Parent;

        // A mapping of hoisted variables to their indexes in the array
        internal readonly ReadOnlyDictionary<Expression, int> Indexes;

        // The variables, in the order they appear in the array
        internal readonly ReadOnlyCollection<ParameterExpression> Variables;

        // A virtual variable for accessing this locals array
        internal readonly ParameterExpression SelfVariable;

        internal HoistedLocals(HoistedLocals parent, ReadOnlyCollection<ParameterExpression> vars)
        {
            if (parent != null)
            {
                // Add the parent locals array as the 0th element in the array
                vars = new TrueReadOnlyCollection<ParameterExpression>(vars.AddFirst(parent.SelfVariable));
            }

            Dictionary<Expression, int> indexes = new Dictionary<Expression, int>(vars.Count);
            for (int i = 0; i < vars.Count; i++)
            {
                indexes.Add(vars[i], i);
            }

            SelfVariable = Expression.Variable(typeof(object[]), name: null);
            Parent = parent;
            Variables = vars;
            Indexes = new ReadOnlyDictionary<Expression, int>(indexes);
        }

        internal ParameterExpression ParentVariable => Parent?.SelfVariable;

        internal static object[] GetParent(object[] locals)
        {
            return ((StrongBox<object[]>)locals[0]).Value;
        }
    }
}
