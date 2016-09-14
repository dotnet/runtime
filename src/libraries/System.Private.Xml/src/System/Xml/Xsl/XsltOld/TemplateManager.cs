// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using System.Collections;

    internal class TemplateManager
    {
        private XmlQualifiedName _mode;
        internal ArrayList templates;
        private Stylesheet _stylesheet;    // Owning stylesheet

        private class TemplateComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                Debug.Assert(x != null && x is TemplateAction);
                Debug.Assert(y != null && y is TemplateAction);

                TemplateAction tx = (TemplateAction)x;
                TemplateAction ty = (TemplateAction)y;

                Debug.Assert(!Double.IsNaN(tx.Priority));
                Debug.Assert(!Double.IsNaN(ty.Priority));

                if (tx.Priority == ty.Priority)
                {
                    Debug.Assert(tx.TemplateId != ty.TemplateId || tx == ty);
                    return tx.TemplateId - ty.TemplateId;
                }
                else
                {
                    return tx.Priority > ty.Priority ? 1 : -1;
                }
            }
        }

        private static TemplateComparer s_TemplateComparer = new TemplateComparer();

        internal XmlQualifiedName Mode
        {
            get { return _mode; }
        }

        internal TemplateManager(Stylesheet stylesheet, XmlQualifiedName mode)
        {
            _mode = mode;
            _stylesheet = stylesheet;
        }

        internal void AddTemplate(TemplateAction template)
        {
            Debug.Assert(template != null);
            Debug.Assert(
                ((object)_mode == (object)template.Mode) ||
                (template.Mode == null && _mode.Equals(XmlQualifiedName.Empty)) ||
                _mode.Equals(template.Mode)
            );

            if (this.templates == null)
            {
                this.templates = new ArrayList();
            }

            this.templates.Add(template);
        }

        internal void ProcessTemplates()
        {
            if (this.templates != null)
            {
                this.templates.Sort(s_TemplateComparer);
            }
        }

        internal TemplateAction FindTemplate(Processor processor, XPathNavigator navigator)
        {
            if (this.templates == null)
            {
                return null;
            }

            Debug.Assert(this.templates != null);
            for (int templateIndex = this.templates.Count - 1; templateIndex >= 0; templateIndex--)
            {
                TemplateAction action = (TemplateAction)this.templates[templateIndex];
                int matchKey = action.MatchKey;

                if (matchKey != Compiler.InvalidQueryKey)
                {
                    if (processor.Matches(navigator, matchKey))
                    {
                        return action;
                    }
                }
            }

            return null;
        }
    }
}
