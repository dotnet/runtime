// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Describes the binding types that are used in MemberInitExpression objects.
    /// </summary>
    public enum MemberBindingType
    {
        /// <summary>
        /// A binding that represents initializing a member with the value of an expression.
        /// </summary>
        Assignment,
        /// <summary>
        /// A binding that represents recursively initializing members of a member.
        /// </summary>
        MemberBinding,
        /// <summary>
        /// A binding that represents initializing a member of type <see cref="System.Collections.IList"/> or <see cref="System.Collections.Generic.ICollection{T}"/> from a list of elements.
        /// </summary>
        ListBinding
    }

    /// <summary>
    /// Provides the base class from which the classes that represent bindings that are used to initialize members of a newly created object derive.
    /// </summary>
    public abstract class MemberBinding
    {
        private MemberBindingType _type;
        private MemberInfo _member;

        /// <summary>
        /// Initializes an instance of <see cref="MemberBinding"/> class.
        /// </summary>
        /// <param name="type">The type of member binding.</param>
        /// <param name="member">The field or property to be initialized.</param>
        [Obsolete("Do not use this constructor. It will be removed in future releases.")]
        protected MemberBinding(MemberBindingType type, MemberInfo member)
        {
            _type = type;
            _member = member;
        }

        /// <summary>
        /// Gets the type of binding that is represented.
        /// </summary>
        public MemberBindingType BindingType
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the field or property to be initialized.
        /// </summary>
        public MemberInfo Member
        {
            get { return _member; }
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="Object"/>. 
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="Object"/>. </returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.MemberBindingToString(this);
        }
    }
}