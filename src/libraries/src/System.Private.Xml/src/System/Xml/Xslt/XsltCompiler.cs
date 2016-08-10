// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl {

    //----------------------------------------------------------------------------------------------------
    //  Clarification on null values in this API:
    //      stylesheet              - cannot be null
    //      settings                - if null, XsltSettings.Default will be used
    //      stylesheetResolver      - if null, XmlNullResolver will be used for includes/imports
    //      typeBuilder             - cannot be null
    //----------------------------------------------------------------------------------------------------

    /// <summary>
    /// Compiles XSLT stylesheet into a TypeBuilder
    /// </summary>
    public class XsltCompiler {
        public static CompilerErrorCollection CompileToType(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver, TypeBuilder typeBuilder) {
            if (stylesheet == null)
                throw new ArgumentNullException(nameof(stylesheet));

            if (typeBuilder == null)
                throw new ArgumentNullException(nameof(typeBuilder));

            if (settings == null)
                settings = XsltSettings.Default;

            CompilerErrorCollection errors;
            QilExpression qil;

            // Get DebuggableAttribute of the assembly. If there are many of them, JIT seems to pick up a random one.
            // I could not discover any pattern here, so let's take the first attribute found.
            object[] debuggableAttrs = typeBuilder.Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);
            bool debug = debuggableAttrs.Length > 0 && ((DebuggableAttribute) debuggableAttrs[0]).IsJITTrackingEnabled;

            errors = new Compiler(settings, debug).Compile(stylesheet, stylesheetResolver, out qil).Errors;

            if (!errors.HasErrors) {
                new XmlILGenerator().Generate(qil, typeBuilder);
            }

            return errors;
        }
    }
}
