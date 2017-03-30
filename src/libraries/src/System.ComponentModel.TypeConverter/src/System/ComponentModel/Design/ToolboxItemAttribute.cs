// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Permissions;
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2113:SecureLateBindingMethods", Scope = "member", Target = "System.ComponentModel.ToolboxItemAttribute.get_ToolboxItemType():System.Type")]

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Specifies attributes for a toolbox item.
    ///    </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ToolboxItemAttribute : Attribute
    {
        private Type _toolboxItemType;
        private string _toolboxItemTypeName;

        /// <summary>
        ///    <para>
        ///    Initializes a new instance of ToolboxItemAttribute and sets the type to
        ///    IComponent.
        ///    </para>
        /// </summary>
        public static readonly ToolboxItemAttribute Default = new ToolboxItemAttribute("System.Drawing.Design.ToolboxItem, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of ToolboxItemAttribute and sets the type to
        ///    <see langword='null'/>.
        ///    </para>
        /// </summary>
        public static readonly ToolboxItemAttribute None = new ToolboxItemAttribute(false);

        /// <summary>
        ///    <para>
        ///       Gets whether the attribute is the default attribute.
        ///    </para>
        /// </summary>
        public override bool IsDefaultAttribute()
        {
            return Equals(Default);
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of ToolboxItemAttribute and
        ///       specifies if default values should be used.
        ///    </para>
        /// </summary>
        public ToolboxItemAttribute(bool defaultType)
        {
            if (defaultType)
            {
                _toolboxItemTypeName = "System.Drawing.Design.ToolboxItem, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            }
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of ToolboxItemAttribute and
        ///       specifies the name of the type.
        ///    </para>
        /// </summary>
        public ToolboxItemAttribute(string toolboxItemTypeName)
        {
            string temp = toolboxItemTypeName.ToUpper(CultureInfo.InvariantCulture);
            Debug.Assert(temp.IndexOf(".DLL") == -1, $"Came across: {toolboxItemTypeName}. Please remove the .dll extension");
            _toolboxItemTypeName = toolboxItemTypeName;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of ToolboxItemAttribute and
        ///       specifies the type of the toolbox item.
        ///    </para>
        /// </summary>
        public ToolboxItemAttribute(Type toolboxItemType)
        {
            _toolboxItemType = toolboxItemType;
            _toolboxItemTypeName = toolboxItemType.AssemblyQualifiedName;
        }

        /// <summary>
        ///    <para>
        ///       Gets the toolbox item's type.
        ///    </para>
        /// </summary>
        public Type ToolboxItemType
        {
            get
            {
                if (_toolboxItemType == null)
                {
                    if (_toolboxItemTypeName != null)
                    {
                        try
                        {
                            _toolboxItemType = Type.GetType(_toolboxItemTypeName, true);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException(SR.Format(SR.ToolboxItemAttributeFailedGetType, _toolboxItemTypeName), ex);
                        }
                    }
                }
                return _toolboxItemType;
            }
        }

        public string ToolboxItemTypeName
        {
            get
            {
                if (_toolboxItemTypeName == null)
                {
                    return String.Empty;
                }
                return _toolboxItemTypeName;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            ToolboxItemAttribute other = obj as ToolboxItemAttribute;
            return (other != null) && (other.ToolboxItemTypeName == ToolboxItemTypeName);
        }

        public override int GetHashCode()
        {
            if (_toolboxItemTypeName != null)
            {
                return _toolboxItemTypeName.GetHashCode();
            }
            return base.GetHashCode();
        }
    }
}

