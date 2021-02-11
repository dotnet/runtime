// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: The class denotes how to specify the usage of an attribute
**
**
===========================================================*/

namespace System
{
    /* By default, attributes are inherited and multiple attributes are not allowed */
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        private readonly AttributeTargets _attributeTarget;
        private bool _allowMultiple;
        private bool _inherited;

        internal static readonly AttributeUsageAttribute Default = new AttributeUsageAttribute(AttributeTargets.All);

        public AttributeUsageAttribute(AttributeTargets validOn)
        {
            _attributeTarget = validOn;
            _inherited = true;
        }

        internal AttributeUsageAttribute(AttributeTargets validOn, bool allowMultiple, bool inherited)
        {
            _attributeTarget = validOn;
            _allowMultiple = allowMultiple;
            _inherited = inherited;
        }

        public AttributeTargets ValidOn => _attributeTarget;

        public bool AllowMultiple
        {
            get => _allowMultiple;
            set => _allowMultiple = value;
        }

        public bool Inherited
        {
            get => _inherited;
            set => _inherited = value;
        }
    }
}
