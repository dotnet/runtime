// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Represents a field of a DataObject. Use this attribute on a field to indicate
    /// properties such as primary key, identity, nullability, and length.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataObjectFieldAttribute : Attribute
    {
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
            PrimaryKey = primaryKey;
            IsIdentity = isIdentity;
            IsNullable = isNullable;
            Length = length;
        }

        public bool IsIdentity { get; }

        public bool IsNullable { get; }

        public int Length { get; }

        public bool PrimaryKey { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            return (obj is DataObjectFieldAttribute other) &&
                (other.IsIdentity == IsIdentity) &&
                (other.IsNullable == IsNullable) &&
                (other.Length == Length) &&
                (other.PrimaryKey == PrimaryKey);
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
