// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;
using MS.Internal.Xml.XPath;
using System.Security;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class Key
    {
        private readonly XmlQualifiedName _name;
        private readonly int _matchKey;
        private readonly int _useKey;
        private ArrayList? _keyNodes;

        public Key(XmlQualifiedName name, int matchkey, int usekey)
        {
            _name = name;
            _matchKey = matchkey;
            _useKey = usekey;
            _keyNodes = null;
        }

        public XmlQualifiedName Name { get { return _name; } }
        public int MatchKey { get { return _matchKey; } }
        public int UseKey { get { return _useKey; } }

        public void AddKey(XPathNavigator root, Hashtable table)
        {
            _keyNodes ??= new ArrayList();
            _keyNodes.Add(new DocumentKeyList(root, table));
        }

        public Hashtable? GetKeys(XPathNavigator root)
        {
            if (_keyNodes != null)
            {
                for (int i = 0; i < _keyNodes.Count; i++)
                {
                    if (((DocumentKeyList)_keyNodes[i]!).RootNav.IsSamePosition(root))
                    {
                        return ((DocumentKeyList)_keyNodes[i]!).KeyTable;
                    }
                }
            }
            return null;
        }

        public Key Clone()
        {
            return new Key(_name, _matchKey, _useKey);
        }
    }

    internal struct DocumentKeyList
    {
        private readonly XPathNavigator _rootNav;
        private readonly Hashtable _keyTable;

        public DocumentKeyList(XPathNavigator rootNav, Hashtable keyTable)
        {
            _rootNav = rootNav;
            _keyTable = keyTable;
        }

        public XPathNavigator RootNav { get { return _rootNav; } }
        public Hashtable KeyTable { get { return _keyTable; } }
    }

    internal sealed class RootAction : TemplateBaseAction
    {
        private const int QueryInitialized = 2;
        private const int RootProcessed = 3;

        private readonly Hashtable _attributeSetTable = new Hashtable();
        private readonly Hashtable _decimalFormatTable = new Hashtable();
        private List<Key>? _keyList;
        private XsltOutput? _output;

        internal XsltOutput Output => _output ??= new XsltOutput();

        /*
         * Compile
         */
        internal override void Compile(Compiler compiler)
        {
            CompileDocument(compiler, /*inInclude*/ false);
        }

        internal void InsertKey(XmlQualifiedName name, int MatchKey, int UseKey)
        {
            _keyList ??= new List<Key>();
            _keyList.Add(new Key(name, MatchKey, UseKey));
        }

        internal AttributeSetAction GetAttributeSet(XmlQualifiedName name)
        {
            AttributeSetAction? action = (AttributeSetAction?)_attributeSetTable[name];
            if (action == null)
            {
                throw XsltException.Create(SR.Xslt_NoAttributeSet, name.ToString());
            }
            return action;
        }


        public void PorcessAttributeSets(Stylesheet rootStylesheet)
        {
            MirgeAttributeSets(rootStylesheet);

            // As we mentioned we need to invert all lists.
            foreach (AttributeSetAction attSet in _attributeSetTable.Values)
            {
                attSet.containedActions?.Reverse();
            }

            //  ensures there are no cycles in the attribute-sets use dfs marking method
            CheckAttributeSets_RecurceInList(new Hashtable(), _attributeSetTable.Keys);
        }

        private void MirgeAttributeSets(Stylesheet stylesheet)
        {
            // mirge stylesheet.AttributeSetTable to this.AttributeSetTable

            if (stylesheet.AttributeSetTable != null)
            {
                foreach (AttributeSetAction srcAttSet in stylesheet.AttributeSetTable.Values)
                {
                    ArrayList? srcAttList = srcAttSet.containedActions;
                    AttributeSetAction? dstAttSet = (AttributeSetAction?)_attributeSetTable[srcAttSet.Name!];
                    if (dstAttSet == null)
                    {
                        dstAttSet = new AttributeSetAction();
                        {
                            dstAttSet.name = srcAttSet.Name;
                            dstAttSet.containedActions = new ArrayList();
                        }
                        _attributeSetTable[srcAttSet.Name!] = dstAttSet;
                    }

                    ArrayList? dstAttList = dstAttSet.containedActions;
                    // We adding attributes in reverse order for purpuse. In the mirged list most important attset should go last one
                    // so we'll need to invert dstAttList finaly.
                    if (srcAttList != null)
                    {
                        for (int src = srcAttList.Count - 1; 0 <= src; src--)
                        {
                            // We can ignore duplicate attributes here.
                            dstAttList!.Add(srcAttList[src]);
                        }
                    }
                }
            }

            foreach (Stylesheet importedStylesheet in stylesheet.Imports)
            {
                MirgeAttributeSets(importedStylesheet);
            }
        }

        private void CheckAttributeSets_RecurceInList(Hashtable markTable, ICollection setQNames)
        {
            const string PROCESSING = "P";
            const string DONE = "D";

            foreach (XmlQualifiedName qname in setQNames)
            {
                object? mark = markTable[qname];
                if (mark == (object)PROCESSING)
                {
                    throw XsltException.Create(SR.Xslt_CircularAttributeSet, qname.ToString());
                }
                else if (mark == (object)DONE)
                {
                    continue; // optimization: we already investigated this attribute-set.
                }
                else
                {
                    Debug.Assert(mark == null);

                    markTable[qname] = (object)PROCESSING;
                    CheckAttributeSets_RecurceInContainer(markTable, GetAttributeSet(qname));
                    markTable[qname] = (object)DONE;
                }
            }
        }

        private void CheckAttributeSets_RecurceInContainer(Hashtable markTable, ContainerAction container)
        {
            if (container.containedActions == null)
            {
                return;
            }
            foreach (Action action in container.containedActions)
            {
                if (action is UseAttributeSetsAction)
                {
                    CheckAttributeSets_RecurceInList(markTable, ((UseAttributeSetsAction)action).UsedSets!);
                }
                else if (action is ContainerAction)
                {
                    CheckAttributeSets_RecurceInContainer(markTable, (ContainerAction)action);
                }
            }
        }

        internal void AddDecimalFormat(XmlQualifiedName name, DecimalFormat formatinfo)
        {
            DecimalFormat? exist = (DecimalFormat?)_decimalFormatTable[name];
            if (exist != null)
            {
                NumberFormatInfo info = exist.info;
                NumberFormatInfo newinfo = formatinfo.info;
                if (info.NumberDecimalSeparator != newinfo.NumberDecimalSeparator ||
                    info.NumberGroupSeparator != newinfo.NumberGroupSeparator ||
                    info.PositiveInfinitySymbol != newinfo.PositiveInfinitySymbol ||
                    info.NegativeSign != newinfo.NegativeSign ||
                    info.NaNSymbol != newinfo.NaNSymbol ||
                    info.PercentSymbol != newinfo.PercentSymbol ||
                    info.PerMilleSymbol != newinfo.PerMilleSymbol ||
                    exist.zeroDigit != formatinfo.zeroDigit ||
                    exist.digit != formatinfo.digit ||
                    exist.patternSeparator != formatinfo.patternSeparator
                )
                {
                    throw XsltException.Create(SR.Xslt_DupDecimalFormat, name.ToString());
                }
            }
            _decimalFormatTable[name] = formatinfo;
        }

        internal DecimalFormat? GetDecimalFormat(XmlQualifiedName name)
        {
            return _decimalFormatTable[name] as DecimalFormat;
        }

        internal List<Key>? KeyList
        {
            get { return _keyList; }
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);

            switch (frame.State)
            {
                case Initialized:
                    frame.AllocateVariables(variableCount);
                    XPathNavigator root = processor.Document.Clone();
                    root.MoveToRoot();
                    frame.InitNodeSet(new XPathSingletonIterator(root));

                    if (this.containedActions != null && this.containedActions.Count > 0)
                    {
                        processor.PushActionFrame(frame);
                    }
                    frame.State = QueryInitialized;
                    break;
                case QueryInitialized:
                    Debug.Assert(frame.State == QueryInitialized);
                    frame.NextNode(processor);
                    Debug.Assert(Processor.IsRoot(frame.Node!));
                    if (processor.Debugger != null)
                    {
                        // this is like apply-templates, but we don't have it on stack.
                        // Pop the stack, otherwise last instruction will be on it.
                        processor.PopDebuggerStack();
                    }
                    processor.PushTemplateLookup(frame.NodeSet, /*mode:*/null, /*importsOf:*/null);

                    frame.State = RootProcessed;
                    break;

                case RootProcessed:
                    Debug.Assert(frame.State == RootProcessed);
                    frame.Finished();
                    break;
                default:
                    Debug.Fail("Invalid RootAction execution state");
                    break;
            }
        }
    }
}
