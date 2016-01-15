// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// -----------------------------------------------------------------------

// -----------------------------------------------------------------------

using Internal.Reflection.Extensions.NonPortable;

namespace System.Reflection
{
    /// <summary>
    /// Extension methods offering source-code compatibility with certain instance methods of <see cref="System.Reflection.MethodInfo"/> on other platforms.
    /// </summary>
    public static class MethodInfoExtensions
    {
        /// <summary>
        /// Returns the MethodInfo object for the method on the direct or indirect base class in which the method represented by this instance was first declared.
        /// </summary>
        /// <param name="method">MethodInfo object on which to perform lookup</param>
        /// <returns>A MethodInfo object for the first implementation of this method.</returns>
        public static MethodInfo GetBaseDefinition(this MethodInfo method)
        {
            Requires.NotNull(method, "method");

            while (true)
            {
                MethodInfo next = method.GetImplicitlyOverriddenBaseClassMember();
                if (next == null)
                {
                    return method;
                }
                method = next;
            }
        }
    }
}
