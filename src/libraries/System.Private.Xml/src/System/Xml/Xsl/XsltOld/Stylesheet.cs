// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class Stylesheet
    {
        private readonly ArrayList _imports = new ArrayList();
        private Hashtable? _modeManagers;
        private readonly Hashtable _templateNameTable = new Hashtable();
        private Hashtable? _attributeSetTable;
        private int _templateCount;
        //private ArrayList     preserveSpace;
        private Hashtable? _queryKeyTable;
        private ArrayList? _whitespaceList;
        private bool _whitespace;
        private readonly Hashtable _scriptObjectTypes = new Hashtable();
        private TemplateManager? _templates;


        private sealed class WhitespaceElement
        {
            private readonly int _key;
            private readonly double _priority;
            private bool _preserveSpace;

            internal double Priority
            {
                get { return _priority; }
            }

            internal int Key
            {
                get { return _key; }
            }

            internal bool PreserveSpace
            {
                get { return _preserveSpace; }
            }

            internal WhitespaceElement(int Key, double priority, bool PreserveSpace)
            {
                _key = Key;
                _priority = priority;
                _preserveSpace = PreserveSpace;
            }

            internal void ReplaceValue(bool PreserveSpace)
            {
                _preserveSpace = PreserveSpace;
            }
        }

        internal bool Whitespace { get { return _whitespace; } }
        internal ArrayList Imports { get { return _imports; } }
        internal Hashtable? AttributeSetTable { get { return _attributeSetTable; } }

        internal void AddSpace(Compiler compiler, string query, double Priority, bool PreserveSpace)
        {
            WhitespaceElement elem;
            if (_queryKeyTable != null)
            {
                if (_queryKeyTable.Contains(query))
                {
                    elem = (WhitespaceElement)_queryKeyTable[query]!;
                    elem.ReplaceValue(PreserveSpace);
                    return;
                }
            }
            else
            {
                _queryKeyTable = new Hashtable();
                _whitespaceList = new ArrayList();
            }
            int key = compiler.AddQuery(query);
            elem = new WhitespaceElement(key, Priority, PreserveSpace);
            _queryKeyTable[query] = elem;
            _whitespaceList!.Add(elem);
        }

        internal void SortWhiteSpace()
        {
            if (_queryKeyTable != null)
            {
                for (int i = 0; i < _whitespaceList!.Count; i++)
                {
                    for (int j = _whitespaceList.Count - 1; j > i; j--)
                    {
                        WhitespaceElement elem1, elem2;
                        elem1 = (WhitespaceElement)_whitespaceList[j - 1]!;
                        elem2 = (WhitespaceElement)_whitespaceList[j]!;
                        if (elem2.Priority < elem1.Priority)
                        {
                            _whitespaceList[j - 1] = elem2;
                            _whitespaceList[j] = elem1;
                        }
                    }
                }
                _whitespace = true;
            }
            if (_imports != null)
            {
                for (int importIndex = _imports.Count - 1; importIndex >= 0; importIndex--)
                {
                    Stylesheet stylesheet = (Stylesheet)_imports[importIndex]!;
                    if (stylesheet.Whitespace)
                    {
                        stylesheet.SortWhiteSpace();
                        _whitespace = true;
                    }
                }
            }
        }

        internal bool PreserveWhiteSpace(Processor proc, XPathNavigator node)
        {
            // last one should win. I.E. We starting from the end. I.E. Lowest priority should go first
            if (_whitespaceList != null)
            {
                for (int i = _whitespaceList.Count - 1; 0 <= i; i--)
                {
                    WhitespaceElement elem = (WhitespaceElement)_whitespaceList[i]!;
                    if (proc.Matches(node, elem.Key))
                    {
                        return elem.PreserveSpace;
                    }
                }
            }
            if (_imports != null)
            {
                for (int importIndex = _imports.Count - 1; importIndex >= 0; importIndex--)
                {
                    Stylesheet stylesheet = (Stylesheet)_imports[importIndex]!;
                    if (!stylesheet.PreserveWhiteSpace(proc, node))
                        return false;
                }
            }
            return true;
        }

        internal void AddAttributeSet(AttributeSetAction attributeSet)
        {
            Debug.Assert(attributeSet.Name != null);
            _attributeSetTable ??= new Hashtable();

            if (_attributeSetTable.ContainsKey(attributeSet.Name) == false)
            {
                _attributeSetTable[attributeSet.Name] = attributeSet;
            }
            else
            {
                // merge the attribute-sets
                ((AttributeSetAction)_attributeSetTable[attributeSet.Name]!).Merge(attributeSet);
            }
        }

        internal void AddTemplate(TemplateAction template)
        {
            XmlQualifiedName? mode = template.Mode;

            //
            // Ensure template has a unique name
            //

            Debug.Assert(_templateNameTable != null);

            if (template.Name != null)
            {
                if (_templateNameTable.ContainsKey(template.Name) == false)
                {
                    _templateNameTable[template.Name] = template;
                }
                else
                {
                    throw XsltException.Create(SR.Xslt_DupTemplateName, template.Name.ToString());
                }
            }


            if (template.MatchKey != Compiler.InvalidQueryKey)
            {
                _modeManagers ??= new Hashtable();
                mode ??= XmlQualifiedName.Empty;

                TemplateManager? manager = (TemplateManager?)_modeManagers[mode];

                if (manager == null)
                {
                    manager = new TemplateManager(this, mode);

                    _modeManagers[mode] = manager;

                    if (mode.IsEmpty)
                    {
                        Debug.Assert(_templates == null);
                        _templates = manager;
                    }
                }
                Debug.Assert(manager != null);

                template.TemplateId = ++_templateCount;
                manager.AddTemplate(template);
            }
        }

        internal void ProcessTemplates()
        {
            if (_modeManagers != null)
            {
                IDictionaryEnumerator enumerator = _modeManagers.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Debug.Assert(enumerator.Value is TemplateManager);
                    TemplateManager manager = (TemplateManager)enumerator.Value;
                    manager.ProcessTemplates();
                }
            }

            if (_imports != null)
            {
                for (int importIndex = _imports.Count - 1; importIndex >= 0; importIndex--)
                {
                    Debug.Assert(_imports[importIndex] is Stylesheet);
                    Stylesheet? stylesheet = (Stylesheet?)_imports[importIndex];
                    Debug.Assert(stylesheet != null);

                    //
                    // Process templates in imported stylesheet
                    //

                    stylesheet.ProcessTemplates();
                }
            }
        }


        internal void ReplaceNamespaceAlias(Compiler compiler)
        {
            if (_modeManagers != null)
            {
                IDictionaryEnumerator enumerator = _modeManagers.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    TemplateManager manager = (TemplateManager)enumerator.Value!;
                    if (manager.templates != null)
                    {
                        for (int i = 0; i < manager.templates.Count; i++)
                        {
                            TemplateAction template = (TemplateAction)manager.templates[i]!;
                            template.ReplaceNamespaceAlias(compiler);
                        }
                    }
                }
            }
            if (_templateNameTable != null)
            {
                IDictionaryEnumerator enumerator = _templateNameTable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    TemplateAction template = (TemplateAction)enumerator.Value!;
                    template.ReplaceNamespaceAlias(compiler);
                }
            }
            if (_imports != null)
            {
                for (int importIndex = _imports.Count - 1; importIndex >= 0; importIndex--)
                {
                    Stylesheet stylesheet = (Stylesheet)_imports[importIndex]!;
                    stylesheet.ReplaceNamespaceAlias(compiler);
                }
            }
        }

        internal TemplateAction? FindTemplate(Processor processor, XPathNavigator navigator, XmlQualifiedName mode)
        {
            Debug.Assert(processor != null && navigator != null);
            Debug.Assert(mode != null);
            TemplateAction? action = null;

            //
            // Try to find template within this stylesheet first
            //
            if (_modeManagers != null)
            {
                TemplateManager? manager = (TemplateManager?)_modeManagers[mode];

                if (manager != null)
                {
                    Debug.Assert(manager.Mode.Equals(mode));
                    action = manager.FindTemplate(processor, navigator);
                }
            }

            //
            // If unsuccessful, search in imported documents from backwards
            //

            return action ?? FindTemplateImports(processor, navigator, mode);
        }

        internal TemplateAction? FindTemplateImports(Processor processor, XPathNavigator navigator, XmlQualifiedName mode)
        {
            TemplateAction? action = null;

            //
            // Do we have imported stylesheets?
            //

            if (_imports != null)
            {
                for (int importIndex = _imports.Count - 1; importIndex >= 0; importIndex--)
                {
                    Debug.Assert(_imports[importIndex] is Stylesheet);
                    Stylesheet? stylesheet = (Stylesheet?)_imports[importIndex];
                    Debug.Assert(stylesheet != null);

                    //
                    // Search in imported stylesheet
                    //

                    action = stylesheet.FindTemplate(processor, navigator, mode);

                    if (action != null)
                    {
                        return action;
                    }
                }
            }

            return action;
        }

        internal TemplateAction? FindTemplate(Processor processor, XPathNavigator navigator)
        {
            Debug.Assert(processor != null && navigator != null);
            Debug.Assert(_templates == null && _modeManagers == null || _templates == _modeManagers![XmlQualifiedName.Empty]);

            TemplateAction? action = null;

            //
            // Try to find template within this stylesheet first
            //

            if (_templates != null)
            {
                action = _templates.FindTemplate(processor, navigator);
            }

            //
            // If unsuccessful, search in imported documents from backwards
            //

            action ??= FindTemplateImports(processor, navigator);

            return action;
        }

        internal TemplateAction? FindTemplate(XmlQualifiedName name)
        {
            //Debug.Assert(this.templateNameTable == null);

            TemplateAction? action = null;

            //
            // Try to find template within this stylesheet first
            //

            if (_templateNameTable != null)
            {
                action = (TemplateAction?)_templateNameTable[name];
            }

            //
            // If unsuccessful, search in imported documents from backwards
            //

            if (action == null && _imports != null)
            {
                for (int importIndex = _imports.Count - 1; importIndex >= 0; importIndex--)
                {
                    Debug.Assert(_imports[importIndex] is Stylesheet);
                    Stylesheet? stylesheet = (Stylesheet?)_imports[importIndex];
                    Debug.Assert(stylesheet != null);

                    //
                    // Search in imported stylesheet
                    //

                    action = stylesheet.FindTemplate(name);

                    if (action != null)
                    {
                        return action;
                    }
                }
            }

            return action;
        }

        internal TemplateAction? FindTemplateImports(Processor processor, XPathNavigator navigator)
        {
            TemplateAction? action = null;

            //
            // Do we have imported stylesheets?
            //

            if (_imports != null)
            {
                for (int importIndex = _imports.Count - 1; importIndex >= 0; importIndex--)
                {
                    Debug.Assert(_imports[importIndex] is Stylesheet);
                    Stylesheet? stylesheet = (Stylesheet?)_imports[importIndex];
                    Debug.Assert(stylesheet != null);

                    //
                    // Search in imported stylesheet
                    //

                    action = stylesheet.FindTemplate(processor, navigator);

                    if (action != null)
                    {
                        return action;
                    }
                }
            }

            return action;
        }

        internal Hashtable ScriptObjectTypes
        {
            get { return _scriptObjectTypes; }
        }
    }
}
