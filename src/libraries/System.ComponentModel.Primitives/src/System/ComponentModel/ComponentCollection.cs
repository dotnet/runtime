// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Placeholder stub without functionality, here only to support existence of 
//  TypeConverter.
// Dependency chain: TypeConverter -> ITypeDescriptorContext -> IContainer -> ComponentCollection

using System.Collections;
 
namespace System.ComponentModel
{
    public class ComponentCollection : ReadOnlyCollectionBase
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ComponentCollection(IComponent[] components)
        {
            InnerList.AddRange(components);
        }

        /** The component in the container identified by name. */
        /// <summary>
        ///    <para>
        ///       Gets a specific <see cref='System.ComponentModel.Component'/> in the <see cref='System.ComponentModel.Container'/>
        ///       .
        ///    </para>
        /// </summary>
        public virtual IComponent this[string name]
        {
            get
            {
                if (name != null)
                {
                    IList list = InnerList;
                    foreach (IComponent comp in list)
                    {
                        if (comp != null && comp.Site != null && comp.Site.Name != null && string.Equals(comp.Site.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return comp;
                        }
                    }
                }
                return null;
            }
        }

        /** The component in the container identified by index. */
        /// <summary>
        ///    <para>
        ///       Gets a specific <see cref='System.ComponentModel.Component'/> in the <see cref='System.ComponentModel.Container'/>
        ///       .
        ///    </para>
        /// </summary>
        public virtual IComponent this[int index]
        {
            get
            {
                return (IComponent)InnerList[index];
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void CopyTo(IComponent[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }
    }
}

