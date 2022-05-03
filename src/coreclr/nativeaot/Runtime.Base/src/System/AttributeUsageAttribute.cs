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
    internal sealed class AttributeUsageAttribute : Attribute
    {
        //Constructors
        public AttributeUsageAttribute(AttributeTargets validOn)
        {
        }

        public AttributeUsageAttribute(AttributeTargets validOn, bool allowMultiple, bool inherited)
        {
        }

        //Properties.
        // Allowing the set properties as it allows a more readable syntax in the specifiers (and are commonly used)
        // The get properties will be needed only if these attributes are used at Runtime, however, the compiler
        // is getting an internal error if the gets are not defined.
#pragma warning disable CA1822
        public bool AllowMultiple
        {
            get { return false; }
            set { }
        }

        public bool Inherited
        {
            get { return false; }
            set { }
        }
#pragma warning restore CA1822
    }
}
