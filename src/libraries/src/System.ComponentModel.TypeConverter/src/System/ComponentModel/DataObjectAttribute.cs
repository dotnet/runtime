// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DataObjectAttribute : Attribute
    {
        public static readonly DataObjectAttribute DataObject = new DataObjectAttribute(true);

        public static readonly DataObjectAttribute NonDataObject = new DataObjectAttribute(false);

        public static readonly DataObjectAttribute Default = NonDataObject;

        private bool _isDataObject;

        public DataObjectAttribute() : this(true)
        {
        }

        public DataObjectAttribute(bool isDataObject)
        {
            _isDataObject = isDataObject;
        }

        public bool IsDataObject
        {
            get
            {
                return _isDataObject;
            }
        }

        /// <internalonly/>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DataObjectAttribute other = obj as DataObjectAttribute;
            return (other != null) && (other.IsDataObject == IsDataObject);
        }

        /// <internalonly/>
        public override int GetHashCode()
        {
            return _isDataObject.GetHashCode();
        }

        /// <internalonly/>
        public override bool IsDefaultAttribute()
        {
            return (Equals(Default));
        }
    }
}
