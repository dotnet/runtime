// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics;

namespace System.ComponentModel
{
    internal static class ComponentModelSwitches
    {
        private static volatile BooleanSwitch commonDesignerServices;
                
        public static BooleanSwitch CommonDesignerServices 
        {
            get 
            {
                if (commonDesignerServices == null) 
                {
                    commonDesignerServices = new BooleanSwitch("CommonDesignerServices", "Assert if any common designer service is not found.");
                }
                return commonDesignerServices;
            }
        }                                                                                                                                                                      
    }
}
