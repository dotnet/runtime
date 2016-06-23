// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// Read the output of QilXmlWriter.
    /// </summary>
    /// <remarks>This internal class allows roundtripping between the Xml serialization format for
    /// QIL and the in-memory data structure.</remarks>
    internal sealed class QilXmlReader
    {
        private static Regex s_lineInfoRegex = new Regex(@"\[(\d+),(\d+) -- (\d+),(\d+)\]");
        private static Regex s_typeInfoRegex = new Regex(@"(\w+);([\w|\|]+);(\w+)");
        private static Dictionary<string, MethodInfo> s_nameToFactoryMethod;

        private QilFactory _f;
        private XmlReader _r;
        private Stack<QilList> _stk;
        private bool _inFwdDecls;
        private Dictionary<string, QilNode> _scope, _fwdDecls;

        static QilXmlReader()
        {
            s_nameToFactoryMethod = new Dictionary<string, MethodInfo>();

            // Build table that maps QilNodeType name to factory method info
            foreach (MethodInfo mi in typeof(QilFactory).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                ParameterInfo[] parms = mi.GetParameters();
                int i;

                // Only match methods that take QilNode parameters
                for (i = 0; i < parms.Length; i++)
                {
                    if (parms[i].ParameterType != typeof(QilNode))
                        break;
                }

                if (i == parms.Length)
                {
                    // Enter the method that takes the maximum number of parameters
                    if (!s_nameToFactoryMethod.ContainsKey(mi.Name) || s_nameToFactoryMethod[mi.Name].GetParameters().Length < parms.Length)
                        s_nameToFactoryMethod[mi.Name] = mi;
                }
            }
        }

        public QilXmlReader(XmlReader r)
        {
            _r = r;
            _f = new QilFactory();
        }

        public QilExpression Read()
        {
            _stk = new Stack<QilList>();
            _inFwdDecls = false;
            _scope = new Dictionary<string, QilNode>();
            _fwdDecls = new Dictionary<string, QilNode>();

            _stk.Push(_f.Sequence());

            while (_r.Read())
            {
                switch (_r.NodeType)
                {
                    case XmlNodeType.Element:
                        bool emptyElem = _r.IsEmptyElement;

                        // XmlReader does not give an event for empty elements, so synthesize one
                        if (StartElement() && emptyElem)
                            EndElement();
                        break;

                    case XmlNodeType.EndElement:
                        EndElement();
                        break;

                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.Comment:
                    case XmlNodeType.ProcessingInstruction:
                        break;

                    default:
                        Debug.Fail("Unexpected event " + _r.NodeType + ", value " + _r.Value);
                        break;
                }
            }

            Debug.Assert(_fwdDecls.Keys.Count == 0, "One or more forward declarations were never defined");
            Debug.Assert(_stk.Peek()[0].NodeType == QilNodeType.QilExpression, "Serialized qil tree did not contain a QilExpression node");
            return (QilExpression)_stk.Peek()[0];
        }

        private bool StartElement()
        {
            QilNode nd;
            ReaderAnnotation ann = new ReaderAnnotation();
            string s;

            // Special case certain element names
            s = _r.LocalName;
            switch (_r.LocalName)
            {
                case "LiteralString":
                    nd = _f.LiteralString(ReadText());
                    break;

                case "LiteralInt32":
                    nd = _f.LiteralInt32(Int32.Parse(ReadText(), CultureInfo.InvariantCulture));
                    break;

                case "LiteralInt64":
                    nd = _f.LiteralInt64(Int64.Parse(ReadText(), CultureInfo.InvariantCulture));
                    break;

                case "LiteralDouble":
                    nd = _f.LiteralDouble(Double.Parse(ReadText(), CultureInfo.InvariantCulture));
                    break;

                case "LiteralDecimal":
                    nd = _f.LiteralDecimal(Decimal.Parse(ReadText(), CultureInfo.InvariantCulture));
                    break;

                case "LiteralType":
                    nd = _f.LiteralType(ParseType(ReadText()));
                    break;

                case "LiteralQName":
                    nd = ParseName(_r.GetAttribute("name"));
                    Debug.Assert(nd != null, "LiteralQName element must have a name attribute");
                    Debug.Assert(_r.IsEmptyElement, "LiteralQName element must be empty");
                    break;

                case "For":
                case "Let":
                case "Parameter":
                case "Function":
                case "RefTo":
                    ann.Id = _r.GetAttribute("id");
                    ann.Name = ParseName(_r.GetAttribute("name"));
                    goto default;

                case "XsltInvokeEarlyBound":
                    ann.ClrNamespace = _r.GetAttribute("clrNamespace");
                    goto default;

                case "ForwardDecls":
                    _inFwdDecls = true;
                    goto default;

                default:
                    // Create sequence
                    nd = _f.Sequence();
                    break;
            }

            // Save xml type and source line information
            ann.XmlType = ParseType(_r.GetAttribute("xmlType")); ;
            nd.SourceLine = ParseLineInfo(_r.GetAttribute("lineInfo"));
            nd.Annotation = ann;

            if (nd is QilList)
            {
                // Push new parent list onto stack
                _stk.Push((QilList)nd);
                return true;
            }

            // Add node to its parent's list
            _stk.Peek().Add(nd);
            return false;
        }

        private void EndElement()
        {
            MethodInfo facMethod = null;
            object[] facArgs;
            QilList list;
            QilNode nd;
            ReaderAnnotation ann;

            list = _stk.Pop();
            ann = (ReaderAnnotation)list.Annotation;

            // Special case certain element names
            string s = _r.LocalName;
            switch (_r.LocalName)
            {
                case "QilExpression":
                    {
                        Debug.Assert(list.Count > 0, "QilExpression node requires a Root expression");
                        QilExpression qil = _f.QilExpression(list[list.Count - 1]);

                        // Be flexible on order and presence of QilExpression children
                        for (int i = 0; i < list.Count - 1; i++)
                        {
                            switch (list[i].NodeType)
                            {
                                case QilNodeType.True:
                                case QilNodeType.False:
                                    qil.IsDebug = list[i].NodeType == QilNodeType.True;
                                    break;

                                case QilNodeType.FunctionList:
                                    qil.FunctionList = (QilList)list[i];
                                    break;

                                case QilNodeType.GlobalVariableList:
                                    qil.GlobalVariableList = (QilList)list[i];
                                    break;

                                case QilNodeType.GlobalParameterList:
                                    qil.GlobalParameterList = (QilList)list[i];
                                    break;
                            }
                        }
                        nd = qil;
                        break;
                    }

                case "ForwardDecls":
                    _inFwdDecls = false;
                    return;

                case "Parameter":
                case "Let":
                case "For":
                case "Function":
                    {
                        string id = ann.Id;
                        QilName name = ann.Name;
                        Debug.Assert(id != null, _r.LocalName + " must have an id attribute");
                        Debug.Assert(!_inFwdDecls || ann.XmlType != null, "Forward decl for " + _r.LocalName + " '" + id + "' must have an xmlType attribute");

                        // Create node (may be discarded later if it was already declared in forward declarations section)
                        switch (_r.LocalName)
                        {
                            case "Parameter":
                                Debug.Assert(list.Count <= (_inFwdDecls ? 0 : 1), "Parameter '" + id + "' must have 0 or 1 arguments");
                                Debug.Assert(ann.XmlType != null, "Parameter '" + id + "' must have an xmlType attribute");
                                if (_inFwdDecls || list.Count == 0)
                                    nd = _f.Parameter(null, name, ann.XmlType);
                                else
                                    nd = _f.Parameter(list[0], name, ann.XmlType);
                                break;

                            case "Let":
                                Debug.Assert(list.Count == (_inFwdDecls ? 0 : 1), "Let '" + id + "' must have 0 or 1 arguments");
                                if (_inFwdDecls)
                                    nd = _f.Let(_f.Unknown(ann.XmlType));
                                else
                                    nd = _f.Let(list[0]);
                                break;

                            case "For":
                                Debug.Assert(list.Count == 1, "For '" + id + "' must have 1 argument");
                                nd = _f.For(list[0]);
                                break;

                            default:
                                Debug.Assert(list.Count == (_inFwdDecls ? 2 : 3), "Function '" + id + "' must have 2 or 3 arguments");
                                if (_inFwdDecls)
                                    nd = _f.Function(list[0], list[1], ann.XmlType);
                                else
                                    nd = _f.Function(list[0], list[1], list[2], ann.XmlType != null ? ann.XmlType : list[1].XmlType);
                                break;
                        }

                        // Set DebugName
                        if (name != null)
                            ((QilReference)nd).DebugName = name.ToString();

                        if (_inFwdDecls)
                        {
                            Debug.Assert(!_scope.ContainsKey(id), "Multiple nodes have id '" + id + "'");
                            _fwdDecls[id] = nd;
                            _scope[id] = nd;
                        }
                        else
                        {
                            if (_fwdDecls.ContainsKey(id))
                            {
                                // Replace forward declaration
                                Debug.Assert(_r.LocalName == Enum.GetName(typeof(QilNodeType), nd.NodeType), "Id '" + id + "' is not not bound to a " + _r.LocalName + " forward decl");
                                nd = _fwdDecls[id];
                                _fwdDecls.Remove(id);

                                if (list.Count > 0) nd[0] = list[0];
                                if (list.Count > 1) nd[1] = list[1];
                            }
                            else
                            {
                                // Put reference in scope
                                Debug.Assert(!_scope.ContainsKey(id), "Id '" + id + "' is already in scope");
                                _scope[id] = nd;
                            }
                        }
                        nd.Annotation = ann;
                        break;
                    }

                case "RefTo":
                    {
                        // Lookup reference
                        string id = ann.Id;
                        Debug.Assert(id != null, _r.LocalName + " must have an id attribute");

                        Debug.Assert(_scope.ContainsKey(id), "Id '" + id + "' is not in scope");
                        _stk.Peek().Add(_scope[id]);
                        return;
                    }

                case "Sequence":
                    nd = _f.Sequence(list);
                    break;

                case "FunctionList":
                    nd = _f.FunctionList(list);
                    break;

                case "GlobalVariableList":
                    nd = _f.GlobalVariableList(list);
                    break;

                case "GlobalParameterList":
                    nd = _f.GlobalParameterList(list);
                    break;

                case "ActualParameterList":
                    nd = _f.ActualParameterList(list);
                    break;

                case "FormalParameterList":
                    nd = _f.FormalParameterList(list);
                    break;

                case "SortKeyList":
                    nd = _f.SortKeyList(list);
                    break;

                case "BranchList":
                    nd = _f.BranchList(list);
                    break;

                case "XsltInvokeEarlyBound":
                    {
                        Debug.Assert(ann.ClrNamespace != null, "XsltInvokeEarlyBound must have a clrNamespace attribute");
                        Debug.Assert(list.Count == 2, "XsltInvokeEarlyBound must have exactly 2 arguments");
                        Debug.Assert(list.XmlType != null, "XsltInvokeEarlyBound must have an xmlType attribute");
                        MethodInfo mi = null;
                        QilName name = (QilName)list[0];

                        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            Type t = asm.GetType(ann.ClrNamespace);
                            if (t != null)
                            {
                                mi = t.GetMethod(name.LocalName);
                                break;
                            }
                        }

                        Debug.Assert(mi != null, "Cannot find method " + ann.ClrNamespace + "." + name.ToString());

                        nd = _f.XsltInvokeEarlyBound(name, _f.LiteralObject(mi), list[1], ann.XmlType);
                        break;
                    }

                default:
                    {
                        // Find factory method which will be used to construct the Qil node
                        Debug.Assert(s_nameToFactoryMethod.ContainsKey(_r.LocalName), "Method " + _r.LocalName + " could not be found on QilFactory");
                        facMethod = s_nameToFactoryMethod[_r.LocalName];
                        Debug.Assert(facMethod.GetParameters().Length == list.Count, "NodeType " + _r.LocalName + " does not allow " + list.Count + " parameters");

                        // Create factory method arguments
                        facArgs = new object[list.Count];
                        for (int i = 0; i < facArgs.Length; i++)
                            facArgs[i] = list[i];

                        // Create node and set its properties
                        nd = (QilNode)facMethod.Invoke(_f, facArgs);
                        break;
                    }
            }

            nd.SourceLine = list.SourceLine;

            // Add node to its parent's list
            _stk.Peek().Add(nd);
        }

        private string ReadText()
        {
            string s = string.Empty;

            if (!_r.IsEmptyElement)
            {
                while (_r.Read())
                {
                    switch (_r.NodeType)
                    {
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                            s += _r.Value;
                            continue;
                    }

                    break;
                }
            }

            return s;
        }

        private ISourceLineInfo ParseLineInfo(string s)
        {
            if (s != null && s.Length > 0)
            {
                Match m = s_lineInfoRegex.Match(s);
                Debug.Assert(m.Success && m.Groups.Count == 5, "Malformed lineInfo attribute");
                return new SourceLineInfo("",
                    Int32.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    Int32.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    Int32.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                    Int32.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)
                );
            }
            return null;
        }

        private XmlQueryType ParseType(string s)
        {
            if (s != null && s.Length > 0)
            {
                Match m = s_typeInfoRegex.Match(s);
                Debug.Assert(m.Success && m.Groups.Count == 4, "Malformed Type info");

                XmlQueryCardinality qc = new XmlQueryCardinality(m.Groups[1].Value);
                bool strict = bool.Parse(m.Groups[3].Value);

                string[] codes = m.Groups[2].Value.Split('|');
                XmlQueryType[] types = new XmlQueryType[codes.Length];

                for (int i = 0; i < codes.Length; i++)
                    types[i] = XmlQueryTypeFactory.Type((XmlTypeCode)Enum.Parse(typeof(XmlTypeCode), codes[i]), strict);

                return XmlQueryTypeFactory.Product(XmlQueryTypeFactory.Choice(types), qc);
            }
            return null;
        }

        private QilName ParseName(string name)
        {
            string prefix, local, uri;
            int idx;

            if (name != null && name.Length > 0)
            {
                // If name contains '}' character, then namespace is non-empty
                idx = name.LastIndexOf('}');
                if (idx != -1 && name[0] == '{')
                {
                    uri = name.Substring(1, idx - 1);
                    name = name.Substring(idx + 1);
                }
                else
                {
                    uri = string.Empty;
                }

                // Parse QName
                ValidateNames.ParseQNameThrow(name, out prefix, out local);

                return _f.LiteralQName(local, uri, prefix);
            }
            return null;
        }

        private class ReaderAnnotation
        {
            public string Id;
            public QilName Name;
            public XmlQueryType XmlType;
            public string ClrNamespace;
        }
    }
}
