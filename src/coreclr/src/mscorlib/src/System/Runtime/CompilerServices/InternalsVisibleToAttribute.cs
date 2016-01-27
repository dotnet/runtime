// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//------------------------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    using System;


    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public sealed class InternalsVisibleToAttribute : Attribute
    {
        private string _assemblyName;
        private bool _allInternalsVisible = true;

        public InternalsVisibleToAttribute(string assemblyName)
        {
            this._assemblyName = assemblyName;
        }

        public string AssemblyName 
        {
            get 
            {
                return _assemblyName;
            }
        }

        public bool AllInternalsVisible
        {
            get { return _allInternalsVisible; }
            set { _allInternalsVisible = value; }
        }
    }

    /// <summary>
    ///     If AllInternalsVisible is not true for a friend assembly, the FriendAccessAllowed attribute
    ///     indicates which internals are shared with that friend assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Constructor |
                    AttributeTargets.Enum |
                    AttributeTargets.Event |
                    AttributeTargets.Field |
                    AttributeTargets.Interface |
                    AttributeTargets.Method |
                    AttributeTargets.Property |
                    AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    [FriendAccessAllowed]
    internal sealed class FriendAccessAllowedAttribute : Attribute {
    }
}

