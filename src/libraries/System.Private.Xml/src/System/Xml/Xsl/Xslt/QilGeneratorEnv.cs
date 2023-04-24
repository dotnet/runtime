// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.XPath;
using FunctionInfo = System.Xml.Xsl.XPath.XPathBuilder.FunctionInfo<System.Xml.Xsl.Xslt.QilGenerator.FuncId>;
using T = System.Xml.Xsl.XmlQueryTypeFactory;

namespace System.Xml.Xsl.Xslt
{
    internal sealed partial class QilGenerator : IXPathEnvironment
    {
        // Everywhere in this code in case of error in the stylesheet we should throw XslLoadException.
        // This helper IErrorHelper implementation is used to wrap XmlException's into XslLoadException's.

        private readonly struct ThrowErrorHelper : IErrorHelper
        {
            public void ReportError(string res, params string?[]? args)
            {
                Debug.Assert(args == null || args.Length == 0, "Error message must already be composed in res");
                throw new XslLoadException(SR.Xml_UserException, res);
            }

            public void ReportWarning(string res, params string?[]? args)
            {
                Debug.Fail("Should never get here");
            }
        }

        // -------------------------------- IXPathEnvironment --------------------------------
        // IXPathEnvironment represents static context (namespaces, focus) and most naturaly implemented by QilGenerator itself
        //                        $var current() key()
        // */@select or */@test     +     +       +
        // template/@match          -     -       +
        // key/@match               -     -       -
        // key/@use                 -     +       -
        // number/@count            +     -       +
        // number/@from             +     -       +

        private bool _allowVariables = true;
        private bool _allowCurrent = true;
        private bool _allowKey = true;

        private void SetEnvironmentFlags(bool allowVariables, bool allowCurrent, bool allowKey)
        {
            _allowVariables = allowVariables;
            _allowCurrent = allowCurrent;
            _allowKey = allowKey;
        }

        XPathQilFactory IXPathEnvironment.Factory { get { return _f; } }

        // IXPathEnvironment interface
        QilNode IFocus.GetCurrent() { return this.GetCurrentNode(); }
        QilNode IFocus.GetPosition() { return this.GetCurrentPosition(); }
        QilNode IFocus.GetLast() { return this.GetLastPosition(); }

        string IXPathEnvironment.ResolvePrefix(string prefix)
        {
            return ResolvePrefixThrow(true, prefix);
        }

        QilNode IXPathEnvironment.ResolveVariable(string prefix, string name)
        {
            if (!_allowVariables)
            {
                throw new XslLoadException(SR.Xslt_VariablesNotAllowed);
            }
            string ns = ResolvePrefixThrow(/*ignoreDefaultNs:*/true, prefix);
            Debug.Assert(ns != null);

            // Look up in params and variables of the current scope and all outer ones
            QilNode? var = _scope.LookupVariable(name, ns);

            if (var == null)
            {
                throw new XslLoadException(SR.Xslt_InvalidVariable, Compiler.ConstructQName(prefix, name));
            }

            // All Node* parameters are guaranteed to be in document order with no duplicates, so TypeAssert
            // this so that optimizer can use this information to avoid redundant sorts and duplicate removal.
            XmlQueryType varType = var.XmlType!;
            if (var.NodeType == QilNodeType.Parameter && varType.IsNode && varType.IsNotRtf && varType.MaybeMany && !varType.IsDod)
            {
                var = _f.TypeAssert(var, XmlQueryTypeFactory.NodeSDod);
            }

            return var;
        }

        // NOTE: DO NOT call QilNode.Clone() while executing this method since fixup nodes cannot be cloned
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Suppressing the warning for the ResolveFunction call on the Scripts since " +
            "Scripts functionality is not supported by .NET Core")]
        QilNode IXPathEnvironment.ResolveFunction(string prefix, string name, IList<QilNode> args, IFocus env)
        {
            Debug.Assert(!args.IsReadOnly, "Writable collection expected");
            if (prefix.Length == 0)
            {
                FunctionInfo? func;
                if (FunctionTable.TryGetValue(name, out func))
                {
                    func.CastArguments(args, name, _f);

                    switch (func.id)
                    {
                        case FuncId.Current:
                            if (!_allowCurrent)
                            {
                                throw new XslLoadException(SR.Xslt_CurrentNotAllowed);
                            }
                            // NOTE: This is the only place where the current node (and not the context node) must be used
                            return ((IXPathEnvironment)this).GetCurrent()!;
                        case FuncId.Key:
                            if (!_allowKey)
                            {
                                throw new XslLoadException(SR.Xslt_KeyNotAllowed);
                            }
                            return CompileFnKey(args[0], args[1], env);
                        case FuncId.Document: return CompileFnDocument(args[0], args.Count > 1 ? args[1] : null);
                        case FuncId.FormatNumber: return CompileFormatNumber(args[0], args[1], args.Count > 2 ? args[2] : null);
                        case FuncId.UnparsedEntityUri: return CompileUnparsedEntityUri(args[0]);
                        case FuncId.GenerateId: return CompileGenerateId(args.Count > 0 ? args[0] : env.GetCurrent()!);
                        case FuncId.SystemProperty: return CompileSystemProperty(args[0]);
                        case FuncId.ElementAvailable: return CompileElementAvailable(args[0]);
                        case FuncId.FunctionAvailable: return CompileFunctionAvailable(args[0]);
                        default:
                            Debug.Fail($"{func.id} is present in the function table, but absent from the switch");
                            return null;
                    }
                }
                else
                {
                    throw new XslLoadException(SR.Xslt_UnknownXsltFunction, Compiler.ConstructQName(prefix, name));
                }
            }
            else
            {
                string ns = ResolvePrefixThrow(/*ignoreDefaultNs:*/true, prefix);
                Debug.Assert(ns != null);
                if (ns == XmlReservedNs.NsMsxsl)
                {
                    if (name == "node-set")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/1, name, args.Count);
                        return CompileMsNodeSet(args[0]);
                    }
                    else if (name == "string-compare")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/2, /*maxArg:*/4, name, args.Count);
                        return _f.InvokeMsStringCompare(
                            /*x:      */_f.ConvertToString(args[0]),
                            /*y:      */_f.ConvertToString(args[1]),
                            /*lang:   */2 < args.Count ? _f.ConvertToString(args[2]) : _f.String(string.Empty),
                            /*options:*/3 < args.Count ? _f.ConvertToString(args[3]) : _f.String(string.Empty)
                        );
                    }
                    else if (name == "utc")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/1, name, args.Count);
                        return _f.InvokeMsUtc(/*datetime:*/_f.ConvertToString(args[0]));
                    }
                    else if (name == "format-date" || name == "format-time")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/3, name, args.Count);
                        return _f.InvokeMsFormatDateTime(
                            /*datetime:*/_f.ConvertToString(args[0]),
                            /*format:  */1 < args.Count ? _f.ConvertToString(args[1]) : _f.String(string.Empty),
                            /*lang:    */2 < args.Count ? _f.ConvertToString(args[2]) : _f.String(string.Empty),
                            /*isDate:  */_f.Boolean(name == "format-date")
                        );
                    }
                    else if (name == "local-name")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/1, name, args.Count);
                        return _f.InvokeMsLocalName(_f.ConvertToString(args[0]));
                    }
                    else if (name == "namespace-uri")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/1, name, args.Count);
                        return _f.InvokeMsNamespaceUri(_f.ConvertToString(args[0]), env.GetCurrent()!);
                    }
                    else if (name == "number")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/1, name, args.Count);
                        return _f.InvokeMsNumber(args[0]);
                    }
                }

                if (ns == XmlReservedNs.NsExsltCommon)
                {
                    if (name == "node-set")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/1, name, args.Count);
                        return CompileMsNodeSet(args[0]);
                    }
                    else if (name == "object-type")
                    {
                        FunctionInfo.CheckArity(/*minArg:*/1, /*maxArg:*/1, name, args.Count);
                        return EXslObjectType(args[0]);
                    }
                }

                // NOTE: If you add any function here, add it to IsFunctionAvailable as well

                // Ensure that all node-set parameters are DocOrderDistinct
                for (int i = 0; i < args.Count; i++)
                    args[i] = _f.SafeDocOrderDistinct(args[i]);

                if (_compiler.Settings.EnableScript)
                {
                    XmlExtensionFunction? scrFunc = _compiler.Scripts.ResolveFunction(name, ns, args.Count, (IErrorHelper)this);
                    if (scrFunc != null)
                    {
                        return GenerateScriptCall(_f.QName(name, ns, prefix), scrFunc, args);
                    }
                }
                else
                {
                    if (_compiler.Scripts.ScriptClasses.ContainsKey(ns))
                    {
                        ReportWarning(SR.Xslt_ScriptsProhibited);
                        return _f.Error(_lastScope!.SourceLine, SR.Xslt_ScriptsProhibited);
                    }
                }

                return _f.XsltInvokeLateBound(_f.QName(name, ns, prefix), args);
            }
        }

        private QilNode GenerateScriptCall(QilName name, XmlExtensionFunction scrFunc, IList<QilNode> args)
        {
            XmlQueryType xmlTypeFormalArg;

            for (int i = 0; i < args.Count; i++)
            {
                xmlTypeFormalArg = scrFunc.GetXmlArgumentType(i);
                switch (xmlTypeFormalArg.TypeCode)
                {
                    case XmlTypeCode.Boolean: args[i] = _f.ConvertToBoolean(args[i]); break;
                    case XmlTypeCode.Double: args[i] = _f.ConvertToNumber(args[i]); break;
                    case XmlTypeCode.String: args[i] = _f.ConvertToString(args[i]); break;
                    case XmlTypeCode.Node: args[i] = xmlTypeFormalArg.IsSingleton ? _f.ConvertToNode(args[i]) : _f.ConvertToNodeSet(args[i]); break;
                    case XmlTypeCode.Item: break;
                    default: Debug.Fail($"This XmlTypeCode should never be inferred from a Clr type: {xmlTypeFormalArg.TypeCode}"); break;
                }
            }

            return _f.XsltInvokeEarlyBound(name, scrFunc.Method!, scrFunc.XmlReturnType!, args);
        }

        private string ResolvePrefixThrow(bool ignoreDefaultNs, string prefix)
        {
            if (ignoreDefaultNs && prefix.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                string? ns = _scope.LookupNamespace(prefix);
                if (ns == null)
                {
                    if (prefix.Length != 0)
                    {
                        throw new XslLoadException(SR.Xslt_InvalidPrefix, prefix);
                    }
                    ns = string.Empty;
                }
                return ns;
            }
        }

        //------------------------------------------------
        // XSLT Functions
        //------------------------------------------------

        public enum FuncId
        {
            Current,
            Document,
            Key,
            FormatNumber,
            UnparsedEntityUri,
            GenerateId,
            SystemProperty,
            ElementAvailable,
            FunctionAvailable,
        }

        private static readonly XmlTypeCode[] s_argFnDocument = { XmlTypeCode.Item, XmlTypeCode.Node };
        private static readonly XmlTypeCode[] s_argFnKey = { XmlTypeCode.String, XmlTypeCode.Item };
        private static readonly XmlTypeCode[] s_argFnFormatNumber = { XmlTypeCode.Double, XmlTypeCode.String, XmlTypeCode.String };

        public static Dictionary<string, FunctionInfo> FunctionTable = CreateFunctionTable();
        private static Dictionary<string, FunctionInfo> CreateFunctionTable()
        {
            Dictionary<string, FunctionInfo> table = new Dictionary<string, FunctionInfo>(16);
            table.Add("current", new FunctionInfo(FuncId.Current, 0, 0, null));
            table.Add("document", new FunctionInfo(FuncId.Document, 1, 2, s_argFnDocument));
            table.Add("key", new FunctionInfo(FuncId.Key, 2, 2, s_argFnKey));
            table.Add("format-number", new FunctionInfo(FuncId.FormatNumber, 2, 3, s_argFnFormatNumber));
            table.Add("unparsed-entity-uri", new FunctionInfo(FuncId.UnparsedEntityUri, 1, 1, XPathBuilder.argString));
            table.Add("generate-id", new FunctionInfo(FuncId.GenerateId, 0, 1, XPathBuilder.argNodeSet));
            table.Add("system-property", new FunctionInfo(FuncId.SystemProperty, 1, 1, XPathBuilder.argString));
            table.Add("element-available", new FunctionInfo(FuncId.ElementAvailable, 1, 1, XPathBuilder.argString));
            table.Add("function-available", new FunctionInfo(FuncId.FunctionAvailable, 1, 1, XPathBuilder.argString));
            return table;
        }

        public static bool IsFunctionAvailable(string localName, string nsUri)
        {
            if (XPathBuilder.IsFunctionAvailable(localName, nsUri))
            {
                return true;
            }
            if (nsUri.Length == 0)
            {
                return FunctionTable.ContainsKey(localName) && localName != "unparsed-entity-uri";
            }
            if (nsUri == XmlReservedNs.NsMsxsl)
            {
                return (
                    localName == "node-set" ||
                    localName == "format-date" ||
                    localName == "format-time" ||
                    localName == "local-name" ||
                    localName == "namespace-uri" ||
                    localName == "number" ||
                    localName == "string-compare" ||
                    localName == "utc"
                );
            }
            if (nsUri == XmlReservedNs.NsExsltCommon)
            {
                return localName == "node-set" || localName == "object-type";
            }
            return false;
        }

        public static bool IsElementAvailable(XmlQualifiedName name)
        {
            if (name.Namespace == XmlReservedNs.NsXslt)
            {
                string localName = name.Name;
                return (
                    localName == "apply-imports" ||
                    localName == "apply-templates" ||
                    localName == "attribute" ||
                    localName == "call-template" ||
                    localName == "choose" ||
                    localName == "comment" ||
                    localName == "copy" ||
                    localName == "copy-of" ||
                    localName == "element" ||
                    localName == "fallback" ||
                    localName == "for-each" ||
                    localName == "if" ||
                    localName == "message" ||
                    localName == "number" ||
                    localName == "processing-instruction" ||
                    localName == "text" ||
                    localName == "value-of" ||
                    localName == "variable"
                );
            }
            // NOTE: msxsl:script is not an "instruction", so we return false for it
            return false;
        }

        private QilNode CompileFnKey(QilNode name, QilNode keys, IFocus env)
        {
            QilNode result;
            QilIterator i, n, k;
            if (keys.XmlType!.IsNode)
            {
                if (keys.XmlType.IsSingleton)
                {
                    result = CompileSingleKey(name, _f.ConvertToString(keys), env);
                }
                else
                {
                    result = _f.Loop(i = _f.For(keys), CompileSingleKey(name, _f.ConvertToString(i), env));
                }
            }
            else if (keys.XmlType.IsAtomicValue)
            {
                result = CompileSingleKey(name, _f.ConvertToString(keys), env);
            }
            else
            {
                result = _f.Loop(n = _f.Let(name), _f.Loop(k = _f.Let(keys),
                    _f.Conditional(_f.Not(_f.IsType(k, T.AnyAtomicType)),
                        _f.Loop(i = _f.For(_f.TypeAssert(k, T.NodeS)), CompileSingleKey(n, _f.ConvertToString(i), env)),
                        CompileSingleKey(n, _f.XsltConvert(k, T.StringX), env)
                    )
                ));
            }
            return _f.DocOrderDistinct(result);
        }

        private QilNode CompileSingleKey(QilNode name, QilNode key, IFocus env)
        {
            Debug.Assert(name.XmlType == T.StringX && key.XmlType == T.StringX);
            QilNode result;

            if (name.NodeType == QilNodeType.LiteralString)
            {
                string keyName = (QilLiteral)name;

                _compiler.ParseQName(keyName, out string prefix, out string local, default(ThrowErrorHelper));
                string nsUri = ResolvePrefixThrow(/*ignoreDefaultNs:*/true, prefix);
                QilName qname = _f.QName(local, nsUri, prefix);

                if (!_compiler.Keys.Contains(qname))
                {
                    throw new XslLoadException(SR.Xslt_UndefinedKey, keyName);
                }
                result = CompileSingleKey(_compiler.Keys[qname], key, env);
            }
            else
            {
                _generalKey ??= CreateGeneralKeyFunction();
                QilIterator i = _f.Let(name);
                QilNode resolvedName = ResolveQNameDynamic(/*ignoreDefaultNs:*/true, i);
                result = _f.Invoke(_generalKey, _f.ActualParameterList(i, resolvedName, key, env.GetCurrent()!));
                result = _f.Loop(i, result);
            }
            return result;
        }

        private QilNode CompileSingleKey(List<Key> defList, QilNode key, IFocus env)
        {
            Debug.Assert(defList != null && defList.Count > 0);
            if (defList.Count == 1)
            {
                return _f.Invoke(defList[0].Function!, _f.ActualParameterList(env.GetCurrent()!, key));
            }

            QilIterator i = _f.Let(key);
            QilNode result = _f.Sequence();
            foreach (Key keyDef in defList)
            {
                result.Add(_f.Invoke(keyDef.Function!, _f.ActualParameterList(env.GetCurrent()!, i)));
            }
            return _f.Loop(i, result);
        }

        private QilNode CompileSingleKey(List<Key> defList, QilIterator key, QilIterator context)
        {
            Debug.Assert(defList != null && defList.Count > 0);
            QilList result = _f.BaseFactory.Sequence();
            QilNode? keyRef = null;

            foreach (Key keyDef in defList)
            {
                keyRef = _f.Invoke(keyDef.Function!, _f.ActualParameterList(context, key));
                result.Add(keyRef);
            }
            return defList.Count == 1 ? keyRef! : result;
        }

        private QilFunction CreateGeneralKeyFunction()
        {
            QilIterator name = _f.Parameter(T.StringX);
            QilIterator resolvedName = _f.Parameter(T.QNameX);
            QilIterator key = _f.Parameter(T.StringX);
            QilIterator context = _f.Parameter(T.NodeNotRtf);

            QilNode fdef = _f.Error(SR.Xslt_UndefinedKey, name);
            for (int idx = 0; idx < _compiler.Keys.Count; idx++)
            {
                fdef = _f.Conditional(_f.Eq(resolvedName, _compiler.Keys[idx][0].Name!.DeepClone(_f.BaseFactory)),
                    CompileSingleKey(_compiler.Keys[idx], key, context),
                    fdef
                );
            }

            QilFunction result = _f.Function(_f.FormalParameterList(name, resolvedName, key, context), fdef, _f.False());
            result.DebugName = "key";
            _functions.Add(result);
            return result;
        }

        private QilNode CompileFnDocument(QilNode uris, QilNode? baseNode)
        {
            QilNode result;
            QilIterator i, u;
            QilIterator? j;

            if (!_compiler.Settings.EnableDocumentFunction)
            {
                ReportWarning(SR.Xslt_DocumentFuncProhibited);
                return _f.Error(_lastScope!.SourceLine, SR.Xslt_DocumentFuncProhibited);
            }
            if (uris.XmlType!.IsNode)
            {
                result = _f.DocOrderDistinct(_f.Loop(i = _f.For(uris),
                    CompileSingleDocument(_f.ConvertToString(i), baseNode ?? i)
                ));
            }
            else if (uris.XmlType.IsAtomicValue)
            {
                result = CompileSingleDocument(_f.ConvertToString(uris), baseNode);
            }
            else
            {
                u = _f.Let(uris);
                j = (baseNode != null) ? _f.Let(baseNode) : null;
                result = _f.Conditional(_f.Not(_f.IsType(u, T.AnyAtomicType)),
                    _f.DocOrderDistinct(_f.Loop(i = _f.For(_f.TypeAssert(u, T.NodeS)),
                        CompileSingleDocument(_f.ConvertToString(i), j ?? i)
                    )),
                    CompileSingleDocument(_f.XsltConvert(u, T.StringX), j)
                );
                result = (baseNode != null) ? _f.Loop(j!, result) : result;
                result = _f.Loop(u, result);
            }
            return result;
        }

        private QilNode CompileSingleDocument(QilNode uri, QilNode? baseNode)
        {
            XPathQilFactory.CheckString(uri);
            QilNode baseUri;

            if (baseNode == null)
            {
                baseUri = _f.String(_lastScope!.SourceLine!.Uri);
            }
            else
            {
                XPathQilFactory.CheckNodeSet(baseNode);
                if (baseNode.XmlType!.IsSingleton)
                {
                    baseUri = _f.InvokeBaseUri(baseNode);
                }
                else
                {
                    // According to errata E14, it is an error if the second argument node-set is empty
                    // and the URI reference is relative. We pass an empty string as a baseUri to indicate
                    // that case.
                    QilIterator i;
                    baseUri = _f.StrConcat(_f.Loop(i = _f.FirstNode(baseNode), _f.InvokeBaseUri(i)));
                }
            }

            XPathQilFactory.CheckString(baseUri);
            return _f.DataSource(uri, baseUri);
        }

        private QilNode CompileFormatNumber(QilNode value, QilNode formatPicture, QilNode? formatName)
        {
            XPathQilFactory.CheckDouble(value);
            XPathQilFactory.CheckString(formatPicture);
            XmlQualifiedName? resolvedName;

            if (formatName == null)
            {
                resolvedName = new XmlQualifiedName();
                // formatName must be non-null in the f.InvokeFormatNumberDynamic() call below
                formatName = _f.String(string.Empty);
            }
            else
            {
                XPathQilFactory.CheckString(formatName);
                if (formatName.NodeType == QilNodeType.LiteralString)
                {
                    resolvedName = ResolveQNameThrow(/*ignoreDefaultNs:*/true, formatName);
                }
                else
                {
                    resolvedName = null;
                }
            }

            if (resolvedName != null)
            {
                DecimalFormatDecl format;
                if (_compiler.DecimalFormats.Contains(resolvedName))
                {
                    format = _compiler.DecimalFormats[resolvedName];
                }
                else
                {
                    if (resolvedName != DecimalFormatDecl.Default.Name)
                    {
                        throw new XslLoadException(SR.Xslt_NoDecimalFormat, (string)(QilLiteral)formatName);
                    }
                    format = DecimalFormatDecl.Default;
                }

                // If both formatPicture and formatName are literal strings, there is no need to reparse
                // formatPicture on every execution of this format-number(). Instead, we create a DecimalFormatter
                // object on the first execution, save its index into a global variable, and reuse that object
                // on all subsequent executions.
                if (formatPicture.NodeType == QilNodeType.LiteralString)
                {
                    QilIterator fmtIdx = _f.Let(_f.InvokeRegisterDecimalFormatter(formatPicture, format));
                    fmtIdx.DebugName = _f.QName($"formatter{_formatterCnt++}", XmlReservedNs.NsXslDebug).ToString();
                    _gloVars.Add(fmtIdx);
                    return _f.InvokeFormatNumberStatic(value, fmtIdx);
                }

                _formatNumberDynamicUsed = true;
                QilNode name = _f.QName(resolvedName.Name, resolvedName.Namespace);
                return _f.InvokeFormatNumberDynamic(value, formatPicture, name, formatName);
            }
            else
            {
                _formatNumberDynamicUsed = true;
                QilIterator i = _f.Let(formatName);
                QilNode name = ResolveQNameDynamic(/*ignoreDefaultNs:*/true, i);
                return _f.Loop(i, _f.InvokeFormatNumberDynamic(value, formatPicture, name, i));
            }
        }

        private QilNode CompileUnparsedEntityUri(QilNode n)
        {
            XPathQilFactory.CheckString(n);
            return _f.Error(_lastScope!.SourceLine, SR.Xslt_UnsupportedXsltFunction, "unparsed-entity-uri");
        }

        private QilNode CompileGenerateId(QilNode n)
        {
            XPathQilFactory.CheckNodeSet(n);
            if (n.XmlType!.IsSingleton)
            {
                return _f.XsltGenerateId(n);
            }
            else
            {
                QilIterator i;
                return _f.StrConcat(_f.Loop(i = _f.FirstNode(n), _f.XsltGenerateId(i)));
            }
        }

        private XmlQualifiedName ResolveQNameThrow(bool ignoreDefaultNs, QilNode qilName)
        {
            string name = (QilLiteral)qilName;
            _compiler.ParseQName(name, out string prefix, out string local, default(ThrowErrorHelper));
            string nsUri = ResolvePrefixThrow(/*ignoreDefaultNs:*/ignoreDefaultNs, prefix);

            return new XmlQualifiedName(local, nsUri);
        }

        private QilNode CompileSystemProperty(QilNode name)
        {
            XPathQilFactory.CheckString(name);
            if (name.NodeType == QilNodeType.LiteralString)
            {
                XmlQualifiedName qname = ResolveQNameThrow(/*ignoreDefaultNs:*/true, name);
                if (EvaluateFuncCalls)
                {
                    XPathItem propValue = XsltFunctions.SystemProperty(qname);
                    if (propValue.ValueType == XsltConvert.StringType)
                    {
                        return _f.String(propValue.Value);
                    }
                    else
                    {
                        Debug.Assert(propValue.ValueType == XsltConvert.DoubleType);
                        return _f.Double((double)propValue.ValueAsDouble);
                    }
                }
                name = _f.QName(qname.Name, qname.Namespace);
            }
            else
            {
                name = ResolveQNameDynamic(/*ignoreDefaultNs:*/true, name);
            }
            return _f.InvokeSystemProperty(name);
        }

        private QilNode CompileElementAvailable(QilNode name)
        {
            XPathQilFactory.CheckString(name);
            if (name.NodeType == QilNodeType.LiteralString)
            {
                XmlQualifiedName qname = ResolveQNameThrow(/*ignoreDefaultNs:*/false, name);
                if (EvaluateFuncCalls)
                {
                    return _f.Boolean(IsElementAvailable(qname));
                }
                name = _f.QName(qname.Name, qname.Namespace);
            }
            else
            {
                name = ResolveQNameDynamic(/*ignoreDefaultNs:*/false, name);
            }
            return _f.InvokeElementAvailable(name);
        }

        private QilNode CompileFunctionAvailable(QilNode name)
        {
            XPathQilFactory.CheckString(name);
            if (name.NodeType == QilNodeType.LiteralString)
            {
                XmlQualifiedName qname = ResolveQNameThrow(/*ignoreDefaultNs:*/true, name);
                if (EvaluateFuncCalls)
                {
                    // Script blocks and extension objects cannot implement neither null nor XSLT namespace
                    if (qname.Namespace.Length == 0 || qname.Namespace == XmlReservedNs.NsXslt)
                    {
                        return _f.Boolean(QilGenerator.IsFunctionAvailable(qname.Name, qname.Namespace));
                    }
                    // We might precalculate the result for script namespaces as well
                }
                name = _f.QName(qname.Name, qname.Namespace);
            }
            else
            {
                name = ResolveQNameDynamic(/*ignoreDefaultNs:*/true, name);
            }
            return _f.InvokeFunctionAvailable(name);
        }

        private QilNode CompileMsNodeSet(QilNode n)
        {
            if (n.XmlType!.IsNode && n.XmlType.IsNotRtf)
            {
                return n;
            }

            return _f.XsltConvert(n, T.NodeSDod);
        }

        //------------------------------------------------
        // EXSLT Functions
        //------------------------------------------------

        private QilNode EXslObjectType(QilNode n)
        {
            if (EvaluateFuncCalls)
            {
                switch (n.XmlType!.TypeCode)
                {
                    case XmlTypeCode.Boolean: return _f.String("boolean");
                    case XmlTypeCode.Double: return _f.String("number");
                    case XmlTypeCode.String: return _f.String("string");
                    default:
                        if (n.XmlType.IsNode && n.XmlType.IsNotRtf)
                        {
                            return _f.String("node-set");
                        }
                        break;
                }
            }
            return _f.InvokeEXslObjectType(n);
        }
    }
}
