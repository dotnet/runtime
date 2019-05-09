// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.InteropServices.Expando
{
    /// <summary>
    /// IExpando is an interface which allows Objects implementing this interface to
    /// support the ability to modify the object by adding and removing members,
    /// represented by MemberInfo objects.
    /// </summary>
    [Guid("AFBF15E6-C37C-11d2-B88E-00A0C9B471B8")]
    internal interface IExpando : IReflect
    {
        /// <summary>
        /// Add a new Field to the reflection object. The field has
        /// name as its name.
        /// </summary>
        FieldInfo AddField(string name);

        /// <summary>
        /// Removes the specified member.
        /// </summary>
        void RemoveMember(MemberInfo m);
    }
}
