// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;

namespace System.Dynamic
{
    /// <summary>
    /// Describes arguments in the dynamic binding process.
    /// </summary>
    /// <remarks>
    /// ArgumentCount - all inclusive number of arguments.
    /// ArgumentNames - names for those arguments that are named.
    ///
    /// Argument names match to the argument values in left to right order
    /// and last name corresponds to the last argument.
    ///
    /// Example:
    ///   Foo(arg1, arg2, arg3, name1 = arg4, name2 = arg5, name3 = arg6)
    ///
    ///   will correspond to:
    ///    ArgumentCount: 6
    ///    ArgumentNames: {"name1", "name2", "name3"}
    /// </remarks>
    public sealed class CallInfo
    {
        /// <summary>
        /// Creates a new CallInfo that represents arguments in the dynamic binding process.
        /// </summary>
        /// <param name="argCount">The number of arguments.</param>
        /// <param name="argNames">The argument names.</param>
        /// <returns>The new CallInfo</returns>
        public CallInfo(int argCount, params string[] argNames)
            : this(argCount, (IEnumerable<string>)argNames)
        {
        }

        /// <summary>
        /// Creates a new CallInfo that represents arguments in the dynamic binding process.
        /// </summary>
        /// <param name="argCount">The number of arguments.</param>
        /// <param name="argNames">The argument names.</param>
        /// <returns>The new CallInfo</returns>
        public CallInfo(int argCount, IEnumerable<string> argNames)
        {
            ContractUtils.RequiresNotNull(argNames, nameof(argNames));

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
        /// Serves as a hash function for the current CallInfo.
        /// </summary>
        /// <returns>A hash code for the current CallInfo.</returns>
        public override int GetHashCode()
        {
            return ArgumentCount ^ ArgumentNames.ListHashCode();
        }

        /// <summary>
        /// Determines whether the specified CallInfo instance is considered equal to the current.
        /// </summary>
        /// <param name="obj">The instance of CallInfo to compare with the current instance.</param>
        /// <returns>true if the specified instance is equal to the current one otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            var other = obj as CallInfo;
            return other != null && ArgumentCount == other.ArgumentCount && ArgumentNames.ListEquals(other.ArgumentNames);
        }
    }
}
