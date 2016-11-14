// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Security.Permissions;
using System.Security;
using System.Diagnostics;

namespace System
{
    public class Stub
    {
        public static string Assembly_EscapedCodeBase()
        {
            return null;
        }
    }


    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SRDescriptionAttribute : DescriptionAttribute
    {
        private bool _replaced;

        public override string Description
        {
            get
            {
                if (!_replaced)
                {
                    _replaced = true;
                    base.DescriptionValue = SR.GetResourceString(base.Description, "");
                }
                return base.Description;
            }
        }

        public SRDescriptionAttribute(string description) : base(description)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SRCategoryAttribute : CategoryAttribute
    {
        public SRCategoryAttribute(string category) : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            return SR.GetResourceString(value, "");
        }
    }


    // from Misc/SecurityUtils.cs
    internal static class SecurityUtils
    {
        /// <summary>
        ///     This helper method provides safe access to Activator.CreateInstance.
        ///     NOTE: This overload will work only with public .ctors. 
        /// </summary>
        internal static object SecureCreateInstance(Type type)
        {
            return SecureCreateInstance(type, null);
        }

        /// <summary>
        ///     This helper method provides safe access to Activator.CreateInstance.
        ///     Set allowNonPublic to true if you want non public ctors to be used. 
        /// </summary>
        internal static object SecureCreateInstance(Type type, object[] args)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return Activator.CreateInstance(type, args);
        }

        internal static object MethodInfoInvoke(MethodInfo method, object target, object[] args) {
            Type type = method.DeclaringType;
            return method.Invoke(target, args);
        }
    }

    internal static class HResults
    {
        internal const int License = unchecked((int)0x80131901);
    }
}

namespace System.ComponentModel
{
    [HostProtection(SharedState = true)]
    internal static class CompModSwitches
    {
        private static volatile BooleanSwitch commonDesignerServices;
        private static volatile TraceSwitch eventLog;
                
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
        
        public static TraceSwitch EventLog 
        {
            get 
            {
                if (eventLog == null) 
                {
                    eventLog = new TraceSwitch("EventLog", "Enable tracing for the EventLog component.");
                }
                return eventLog;
            }
        }
                                                                                                                                                                               
    }
}
