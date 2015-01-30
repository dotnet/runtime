// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: The class denotes how to specify the usage of an attribute
**          
**
===========================================================*/
namespace System {

    using System.Reflection;
    /* By default, attributes are inherited and multiple attributes are not allowed */
[Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        internal AttributeTargets m_attributeTarget = AttributeTargets.All; // Defaults to all
        internal bool m_allowMultiple = false; // Defaults to false
        internal bool m_inherited = true; // Defaults to true
    
        internal static AttributeUsageAttribute Default = new AttributeUsageAttribute(AttributeTargets.All);

       //Constructors 
        public AttributeUsageAttribute(AttributeTargets validOn) {
            m_attributeTarget = validOn;
        }
       internal AttributeUsageAttribute(AttributeTargets validOn, bool allowMultiple, bool inherited) {
           m_attributeTarget = validOn;
           m_allowMultiple = allowMultiple;
           m_inherited = inherited;
       }
    
       
       //Properties 
        public AttributeTargets ValidOn 
        {
           get{ return m_attributeTarget; }
        }
    
        public bool AllowMultiple 
        {
            get { return m_allowMultiple; }
            set { m_allowMultiple = value; }
        }
    
        public bool Inherited 
        {
            get { return m_inherited; }
            set { m_inherited = value; }
        }
    }
}
