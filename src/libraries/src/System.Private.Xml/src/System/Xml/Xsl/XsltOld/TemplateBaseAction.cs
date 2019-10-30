// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Xml;
    using System.Xml.XPath;
    using System.Globalization;

    // RootAction and TemplateActions have a litle in common -- they are responsible for variable allocation
    // TemplateBaseAction -- implenemts this shared behavior

    internal abstract class TemplateBaseAction : ContainerAction
    {
        protected int variableCount;      // space to allocate on frame for variables
        private int _variableFreeSlot;   // compile time counter responsiable for variable placement logic

        public int AllocateVariableSlot()
        {
            // Variable placement logic. Optimized
            int thisSlot = _variableFreeSlot;
            _variableFreeSlot++;
            if (this.variableCount < _variableFreeSlot)
            {
                this.variableCount = _variableFreeSlot;
            }
            return thisSlot;
        }

        public void ReleaseVariableSlots(int n)
        {
            // This code does optimisation of variable placement. Commented out for this version
            //      Reuse of the variable disable the check that variable was assigned before the actual use
            //      this check has to be done in compile time n future.
            //            this.variableFreeSlot -= n;
        }
    }
}
