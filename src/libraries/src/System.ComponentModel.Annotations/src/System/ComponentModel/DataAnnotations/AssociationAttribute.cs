// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Used to mark an Entity member as an association
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false,
        Inherited = true)]
    [Obsolete("This attribute is no longer in use and will be ignored if applied.")]
    public sealed class AssociationAttribute : Attribute
    {
        private string _name;
        private string _thisKey;
        private string _otherKey;
        private bool _isForeignKey;

        /// <summary>
        /// Full form of constructor
        /// </summary>
        /// <param name="name">The name of the association. For bi-directional associations,
        /// the name must be the same on both sides of the association</param>
        /// <param name="thisKey">Comma separated list of the property names of the key values
        /// on this side of the association</param>
        /// <param name="otherKey">Comma separated list of the property names of the key values
        /// on the other side of the association</param>
        public AssociationAttribute(string name, string thisKey, string otherKey)
        {
            _name = name;
            _thisKey = thisKey;
            _otherKey = otherKey;
        }

        /// <summary>
        /// Gets the name of the association. For bi-directional associations, the name must
        /// be the same on both sides of the association
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets a comma separated list of the property names of the key values
        /// on this side of the association
        /// </summary>
        public string ThisKey
        {
            get { return _thisKey; }
        }

        /// <summary>
        /// Gets a comma separated list of the property names of the key values
        /// on the other side of the association
        /// </summary>
        public string OtherKey
        {
            get { return _otherKey; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this association member represents
        /// the foreign key side of an association
        /// </summary>
        public bool IsForeignKey
        {
            get { return _isForeignKey; }
            set { _isForeignKey = value; }
        }

        /// <summary>
        /// Gets the collection of individual key members specified in the ThisKey string.
        /// </summary>
        public IEnumerable<string> ThisKeyMembers
        {
            get
            {
                return GetKeyMembers(ThisKey);
            }
        }

        /// <summary>
        /// Gets the collection of individual key members specified in the OtherKey string.
        /// </summary>
        public IEnumerable<string> OtherKeyMembers
        {
            get
            {
                return GetKeyMembers(OtherKey);
            }
        }

        /// <summary>
        /// Parses the comma delimited key specified
        /// </summary>
        /// <param name="key">The key to parse</param>
        /// <returns>Array of individual key members</returns>
        private static string[] GetKeyMembers(string key)
        {
            return key.Replace(" ", string.Empty).Split(',');
        }
    }
}
