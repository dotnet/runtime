// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: The class denotes how to specify the usage of an attribute
**          
**
===========================================================*/

#nullable enable
using System.Reflection;

namespace System
{
    /* By default, attributes are inherited and multiple attributes are not allowed */
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        private AttributeTargets _attributeTarget = AttributeTargets.All; // Defaults to all
        private bool _allowMultiple = false; // Defaults to false
        private bool _inherited = true; // Defaults to true

        internal static readonly AttributeUsageAttribute Default = new AttributeUsageAttribute(AttributeTargets.All);

        //Constructors 
        public AttributeUsageAttribute(AttributeTargets validOn)
        {
            _attributeTarget = validOn;
        }
        internal AttributeUsageAttribute(AttributeTargets validOn, bool allowMultiple, bool inherited)
        {
            _attributeTarget = validOn;
            _allowMultiple = allowMultiple;
            _inherited = inherited;
        }

        public AttributeTargets ValidOn
        {
            get { return _attributeTarget; }
        }

        public bool AllowMultiple
        {
            get { return _allowMultiple; }
            set { _allowMultiple = value; }
        }

        public bool Inherited
        {
            get { return _inherited; }
            set { _inherited = value; }
        }
    }
}
