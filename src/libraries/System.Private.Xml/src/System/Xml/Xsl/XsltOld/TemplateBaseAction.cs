// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;

namespace System.Xml.Xsl.XsltOld
{
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
    }
}
