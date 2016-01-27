// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Tracking whether a component signs up for a 
**     a strong contract spanning multiple versions.
**
===========================================================*/
using System;

namespace System.Runtime.Versioning {
 
    [Flags]
    [Serializable]
    public enum ComponentGuaranteesOptions
    {
        None = 0,
        Exchange = 0x1,
        Stable = 0x2,
        SideBySide = 0x4,
    }
 
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | 
                    AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate |
                    AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property |
                    AttributeTargets.Constructor | AttributeTargets.Event, 
                    AllowMultiple = false, Inherited = false)]
    public sealed class ComponentGuaranteesAttribute : Attribute {
        private ComponentGuaranteesOptions _guarantees;

        public ComponentGuaranteesAttribute(ComponentGuaranteesOptions guarantees)
        {
            _guarantees = guarantees;
        }

        public ComponentGuaranteesOptions Guarantees {
            get { return _guarantees; }
        }
    }
}
