// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor |
                    AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class SwitchAttribute : Attribute
    {
        private Type _type;
        private string _name;

        public SwitchAttribute(string switchName, Type switchType)
        {
            SwitchName = switchName;
            SwitchType = switchType;
        }

        public string SwitchName
        {
            get { return _name; }
            [MemberNotNull(nameof(_name))]
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value);

                _name = value;
            }
        }

        public Type SwitchType
        {
            get { return _type; }
            [MemberNotNull(nameof(_type))]
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _type = value;
            }
        }

        public string? SwitchDescription { get; set; }

        [RequiresUnreferencedCode("Types may be trimmed from the assembly.")]
        public static SwitchAttribute[] GetAll(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            List<object> switchAttribs = new List<object>();
            object[] attribs = assembly.GetCustomAttributes(typeof(SwitchAttribute), false);
            switchAttribs.AddRange(attribs);

            foreach (Type type in assembly.GetTypes())
            {
                GetAllRecursive(type, switchAttribs);
            }

            SwitchAttribute[] ret = new SwitchAttribute[switchAttribs.Count];
            switchAttribs.CopyTo(ret, 0);
            return ret;
        }

        private static void GetAllRecursive([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, List<object> switchAttribs)
        {
            GetAllRecursive((MemberInfo)type, switchAttribs);
            MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                   BindingFlags.Static);

            foreach (MemberInfo member in members)
            {
                // ignore Types here.  They will get covered by the top level assembly.GetTypes
                if (!(member is Type))
                    GetAllRecursive(member, switchAttribs);
            }
        }

        private static void GetAllRecursive(MemberInfo member, List<object> switchAttribs)
        {
            object[] attribs = member.GetCustomAttributes(typeof(SwitchAttribute), false);
            switchAttribs.AddRange(attribs);
        }
    }
}
