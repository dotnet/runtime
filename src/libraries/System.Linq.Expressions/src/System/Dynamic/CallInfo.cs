// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;

namespace System.Dynamic
{
    /// <summary>
    /// Describes arguments in the dynamic binding process.
    /// </summary>
    /// <remarks>
    /// <see cref="ArgumentCount"/> - all inclusive number of arguments.
    /// <see cref="ArgumentNames"/> - names for those arguments that are named.
    ///
    /// Argument names match to the argument values in left to right order
    /// and last name corresponds to the last argument.
    /// </remarks>
    /// <example>
    /// <code>
    ///   Foo(arg1, arg2, arg3, name1 = arg4, name2 = arg5, name3 = arg6)
    /// </code>
    ///   will correspond to
    /// <code>
    ///   new CallInfo(6, "name1", "name2", "name3")
    /// </code>
    /// </example>
    public sealed class CallInfo
    {
        /// <summary>
        /// Creates a new <see cref="CallInfo"/> that represents arguments in the dynamic binding process.
        /// </summary>
        /// <param name="argCount">The number of arguments.</param>
        /// <param name="argNames">The argument names.</param>
        /// <returns>The new <see cref="CallInfo"/> instance.</returns>
        public CallInfo(int argCount, params string[] argNames)
            : this(argCount, (IEnumerable<string>)argNames)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CallInfo"/> that represents arguments in the dynamic binding process.
        /// </summary>
        /// <param name="argCount">The number of arguments.</param>
        /// <param name="argNames">The argument names.</param>
        /// <returns>The new <see cref="CallInfo"/> instance.</returns>
        public CallInfo(int argCount, IEnumerable<string> argNames)
        {
            ArgumentNullException.ThrowIfNull(argNames);

            var argNameCol = argNames.ToReadOnly();

            if (argCount < argNameCol.Count) throw System.Linq.Expressions.Error.ArgCntMustBeGreaterThanNameCnt();
            ContractUtils.RequiresNotNullItems(argNameCol, nameof(argNames));

            ArgumentCount = argCount;
            ArgumentNames = argNameCol;
        }

        /// <summary>
        /// The number of arguments.
        /// </summary>
        public int ArgumentCount { get; }

        /// <summary>
        /// The argument names.
        /// </summary>
        public ReadOnlyCollection<string> ArgumentNames { get; }

        /// <summary>
        /// Serves as a hash function for the current <see cref="CallInfo"/>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="CallInfo"/>.</returns>
        public override int GetHashCode()
        {
            return ArgumentCount ^ ArgumentNames.ListHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="CallInfo"/> instance is considered equal to the current instance.
        /// </summary>
        /// <param name="obj">The instance of <see cref="CallInfo"/> to compare with the current instance.</param>
        /// <returns>true if the specified instance is equal to the current one otherwise, false.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is CallInfo other && ArgumentCount == other.ArgumentCount && ArgumentNames.ListEquals(other.ArgumentNames);
        }
    }
}
