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

using System;

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
        private string category;
        private string justification;
        private string checkId;
        private string scope;
        private string target;
        private string messageId;
        
        public SuppressMessageAttribute(string category, string checkId)
        {
            this.category  = category;
            this.checkId = checkId;
        }
        
        public string Category
        {
            get { return category; }
        }
        
        public string CheckId
        {
            get { return checkId; }
        }
        
        public string Scope
        {
            get { return scope; }
            set { scope = value; }
        }
    
        public string Target
        {
            get { return target; }
            set { target = value; }
        }
    
        public string MessageId
        {
            get { return messageId; }
            set { messageId = value; }
        }
        
        public string Justification
        {
            get { return justification; }
            set { justification = value; }
        }
    }
}
