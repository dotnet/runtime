// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace System.Diagnostics
{
    // This attribute is used to control what is displayed for the given class or field 
    // in the data windows in the debugger.  The single argument to this attribute is
    // the string that will be displayed in the value column for instances of the type.  
    // This string can include text between { and } which can be either a field, 
    // property or method (as will be documented in mscorlib).  In the C# case, 
    // a general expression will be allowed which only has implicit access to the this pointer 
    // for the current instance of the target type. The expression will be limited, 
    // however: there is no access to aliases, locals, or pointers. 
    // In addition, attributes on properties referenced in the expression are not processed.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerDisplayAttribute : Attribute
    {
        private Type? _target;

        public DebuggerDisplayAttribute(string? value)
        {
            Value = value ?? "";
            Name = "";
            Type = "";
        }

        public string Value { get; }

        public string? Name { get; set; }

        public string? Type { get; set; }

        public Type? Target
        {
            get => _target;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                TargetTypeName = value.AssemblyQualifiedName;
                _target = value;
            }
        }

        public string? TargetTypeName { get; set; }
    }
}
