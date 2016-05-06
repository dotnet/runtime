// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the default event for a
    ///       component.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultEventAttribute : Attribute
    {
        /// <summary>
        ///     This is the default event name.
        /// </summary>
        private readonly string _name;

        /// <summary>
        ///    <para>
        ///       Initializes
        ///       a new instance of the <see cref='System.ComponentModel.DefaultEventAttribute'/> class.
        ///    </para>
        /// </summary>
        public DefaultEventAttribute(string name)
        {
            _name = name;
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the default event for
        ///       the component this attribute is bound to.
        ///    </para>
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.DefaultEventAttribute'/>, which is
        ///    <see langword='null'/>.
        ///       This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly DefaultEventAttribute Default = new DefaultEventAttribute(null);

        public override bool Equals(object obj)
        {
            DefaultEventAttribute other = obj as DefaultEventAttribute;
            return (other != null) && other.Name == _name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
