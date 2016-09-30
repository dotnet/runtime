// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para> Represents a verb that can be executed by a component's designer.</para>
    /// </summary>
    public class DesignerVerb : MenuCommand
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.DesignerVerb'/> class.
        ///    </para>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DesignerVerb(string text, EventHandler handler) : base(handler, StandardCommands.VerbFirst)
        {
            Properties["Text"] = text == null ? null : Regex.Replace(text, @"\(\&.\)", ""); // VSWHIDBEY 485835
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.DesignerVerb'/>
        ///       class.
        ///    </para>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DesignerVerb(string text, EventHandler handler, CommandID startCommandID) : base(handler, startCommandID)
        {
            Properties["Text"] = text == null ? null : Regex.Replace(text, @"\(\&.\)", "");// VSWHIDBEY 485835
        }

        /// <summary>
        /// Gets or sets the description of the menu item for the verb.
        /// </summary>
        public string Description
        {
            get
            {
                object result = Properties["Description"];
                if (result == null)
                {
                    return String.Empty;
                }
                return (string)result;
            }
            set
            {
                Properties["Description"] = value;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the text to show on the menu item for the verb.
        ///    </para>
        /// </summary>
        public string Text
        {
            get
            {
                object result = Properties["Text"];
                if (result == null)
                {
                    return String.Empty;
                }
                return (string)result;
            }
        }


        /// <summary>
        ///    <para>
        ///       Overrides object's ToString().
        ///    </para>
        /// </summary>
        public override string ToString()
        {
            return Text + " : " + base.ToString();
        }
    }
}

