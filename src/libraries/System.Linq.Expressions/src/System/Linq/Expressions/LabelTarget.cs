// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Used to denote the target of a <see cref="GotoExpression"/>.
    /// </summary>
    public sealed class LabelTarget
    {
        private readonly Type _type;
        private readonly string _name;

        internal LabelTarget(Type type, string name)
        {
            _type = type;
            _name = name;
        }

        /// <summary>
        /// Gets the name of the label.
        /// </summary>
        /// <remarks>The label's name is provided for information purposes only.</remarks>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// The type of value that is passed when jumping to the label
        /// (or System.Void if no value should be passed).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public Type Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="Object"/>. 
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="Object"/>. </returns>
        public override string ToString()
        {
            return String.IsNullOrEmpty(this.Name) ? "UnamedLabel" : this.Name;
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with void type and no name.
        /// </summary>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label()
        {
            return Label(typeof(void), null);
        }

        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with void type and the given name.
        /// </summary>
        /// <param name="name">The name of the label.</param>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label(string name)
        {
            return Label(typeof(void), name);
        }

        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with the given type.
        /// </summary>
        /// <param name="type">The type of value that is passed when jumping to the label.</param>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label(Type type)
        {
            return Label(type, null);
        }

        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with the given type and name.
        /// </summary>
        /// <param name="type">The type of value that is passed when jumping to the label.</param>
        /// <param name="name">The name of the label.</param>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label(Type type, string name)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type);
            return new LabelTarget(type, name);
        }
    }
}
