// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl {
    /// <summary>
    /// Common base class for XslCompiledTransform and compiled stylesheet classes
    /// </summary>
    public abstract class XslCompiledTransformBase : XsltCommand {
        // Permission set that contains Reflection [MemberAccess] permissions
        private static readonly PermissionSet MemberAccessPermissionSet;

        // Executable command for the compiled stylesheet
        internal XmlILCommand command;

        static XslCompiledTransformBase() {
            MemberAccessPermissionSet = new PermissionSet(PermissionState.None);
            MemberAccessPermissionSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
        }

        /// <summary>
        /// Constructor called by compiled stylesheet class constructors. Tries to deserialize a command from a specially named
        /// private static field, which contains either byte[] or XmlQueryStaticData instance.
        /// </summary>
        protected XslCompiledTransformBase() {
            Debug.Assert(!(this is XslCompiledTransform));
            FieldInfo fldData  = this.GetType().GetField(XmlQueryStaticData.DataFieldName,  BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo fldTypes = this.GetType().GetField(XmlQueryStaticData.TypesFieldName, BindingFlags.Static | BindingFlags.NonPublic);

            // If private fields are not there, it is not a compiled stylesheet class
            if (fldData != null && fldTypes != null) {
                // Need MemberAccess reflection permission to access a private data field and create a delegate
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();

                object value = fldData.GetValue(/*this:*/null);
                byte[] data = value as byte[];
                if (data != null) {
                    // Deserialize query static data and create the command
                    MethodInfo methExec = this.GetType().GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
                    Delegate delExec = Delegate.CreateDelegate(typeof(ExecuteDelegate), methExec);
                    value = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(data, (Type[])fldTypes.GetValue(/*this:*/null)));

                    // Store the constructed command in the same field
                    System.Threading.Thread.MemoryBarrier();
                    fldData.SetValue(/*this:*/null, value);
                }

                this.command = value as XmlILCommand;
            }
        }

        /// <summary>
        /// Constructor called by XslCompiledTransform constructor
        /// </summary>
        internal XslCompiledTransformBase(XmlILCommand command) {
            Debug.Assert(this is XslCompiledTransform);
            this.command = command;
        }

        //------------------------------------------------
        // OutputSettings implementation
        //------------------------------------------------

        /// <summary>
        /// Writer settings specified in the stylesheet
        /// </summary>
        public override XmlWriterSettings OutputSettings {
            get {
                return this.command != null ? this.command.StaticData.DefaultWriterSettings : null;
            }
        }

        //------------------------------------------------
        // Main Transform overloads
        //------------------------------------------------

        public override void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            CheckCommand();
            this.command.Execute((object)input, documentResolver, arguments, results);
        }

        public override void Transform(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            CheckCommand();
            this.command.Execute((object)input.CreateNavigator(), documentResolver, arguments, results);
        }

        //------------------------------------------------
        // Helper methods
        //------------------------------------------------

        internal void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            CheckCommand();
            this.command.Execute((object)inputUri, documentResolver, arguments, results);
        }

        private void CheckCommand() {
            if (this.command == null) {
                throw new InvalidOperationException(SR.GetString(SR.Xslt_NoStylesheetLoaded));
            }
        }
    }
}
