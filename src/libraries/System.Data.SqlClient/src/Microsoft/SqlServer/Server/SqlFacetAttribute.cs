// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.Server
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class SqlFacetAttribute : Attribute
    {
        /// <summary>
        /// Is this a fixed size field?
        /// </summary>
        public bool IsFixedLength
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum size of the field (in bytes or characters depending on the field type)
        /// or -1 if the size can be unlimited.
        /// </summary>
        public int MaxSize
        {
            get;
            set;
        }

        /// <summary>
        /// Precision, only valid for numeric types.
        /// </summary>
        public int Precision
        {
            get;
            set;
        }

        /// <summary>
        /// Scale, only valid for numeric types. 
        /// </summary>
        public int Scale
        {
            get;
            set;
        }

        /// <summary>
        /// Is this field nullable?
        /// </summary>
        public bool IsNullable
        {
            get;
            set;
        }
    }
}
