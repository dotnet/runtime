// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
**  An attribute to suppress violation messages/warnings   
**  by static code analysis tools. 
**
** 
===========================================================*/

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(
     AttributeTargets.All,
     Inherited = false,
     AllowMultiple = true
     )
    ]
    [Conditional("CODE_ANALYSIS")]
    public sealed class SuppressMessageAttribute : Attribute
    {
        public SuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }

        public string Category { get; }
        public string CheckId { get; }
        public string Scope { get; set; }
        public string Target { get; set; }
        public string MessageId { get; set; }
        public string Justification { get; set; }
    }
}
