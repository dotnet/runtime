// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    /// Represents a field of a DataObject. Use this attribute on a field to indicate
    /// properties such as primary key, identity, nullability, and length.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataObjectFieldAttribute : Attribute
    {
        private bool _primaryKey;
        private bool _isIdentity;
        private bool _isNullable;
        private int _length;

        public DataObjectFieldAttribute(bool primaryKey) : this(primaryKey, false, false, -1)
        {
        }

        public DataObjectFieldAttribute(bool primaryKey, bool isIdentity) : this(primaryKey, isIdentity, false, -1)
        {
        }

        public DataObjectFieldAttribute(bool primaryKey, bool isIdentity, bool isNullable) : this(primaryKey, isIdentity, isNullable, -1)
        {
        }

        public DataObjectFieldAttribute(bool primaryKey, bool isIdentity, bool isNullable, int length)
        {
            _primaryKey = primaryKey;
            _isIdentity = isIdentity;
            _isNullable = isNullable;
            _length = length;
        }

        public bool IsIdentity
        {
            get
            {
                return _isIdentity;
            }
        }

        public bool IsNullable
        {
            get
            {
                return _isNullable;
            }
        }

        public int Length
        {
            get
            {
                return _length;
            }
        }

        public bool PrimaryKey
        {
            get
            {
                return _primaryKey;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DataObjectFieldAttribute other = obj as DataObjectFieldAttribute;
            return (other != null) &&
                (other.IsIdentity == IsIdentity) &&
                (other.IsNullable == IsNullable) &&
                (other.Length == Length) &&
                (other.PrimaryKey == PrimaryKey);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
