// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DataObjectAttribute : Attribute
    {
        public static readonly DataObjectAttribute DataObject = new DataObjectAttribute(true);

        public static readonly DataObjectAttribute NonDataObject = new DataObjectAttribute(false);

        public static readonly DataObjectAttribute Default = NonDataObject;

        public DataObjectAttribute() : this(true)
        {
        }

        public DataObjectAttribute(bool isDataObject)
        {
            IsDataObject = isDataObject;
        }

        public bool IsDataObject { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            return (obj is DataObjectAttribute other) && (other.IsDataObject == IsDataObject);
        }

        public override int GetHashCode() => IsDataObject.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
