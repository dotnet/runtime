// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Xsl.Qil;
using ContextInfo = System.Xml.Xsl.Xslt.XsltInput.ContextInfo;
using XPathQilFactory = System.Xml.Xsl.XPath.XPathQilFactory;

namespace System.Xml.Xsl.Xslt
{
    // Set of classes that represent XSLT AST

    // XSLT AST is a tree of nodes that represent content of xsl template.
    // All nodes are subclasses of QilNode. This was done to keep NodeCtor and Text ctors
    // the sames nodes thay will be in resulting QilExpression tree.
    // So we have: ElementCtor, AttributeCtor, QilTextCtor, CommentCtor, PICtor, NamespaceDecl, List.
    // Plus couple subclasses of XslNode that represent different xslt instructions
    // including artifitial: Sort, ExNamespaceDecl, UseAttributeSets

    internal enum XslNodeType
    {
        Unknown = 0,
        ApplyImports,
        ApplyTemplates,
        Attribute,
        AttributeSet,
        CallTemplate,
        Choose,
        Comment,
        Copy,
        CopyOf,
        Element,
        Error,
        ForEach,
        If,
        Key,
        List,
        LiteralAttribute,
        LiteralElement,
        Message,
        Nop,
        Number,
        Otherwise,
        Param,
        PI,
        Sort,
        Template,
        Text,
        UseAttributeSet,
        ValueOf,
        ValueOfDoe,
        Variable,
        WithParam,
    }

    internal sealed class NsDecl
    {
        public readonly NsDecl? Prev;
        public readonly string? Prefix;  // Empty string denotes the default namespace, null - extension or excluded namespace
        public readonly string? NsUri;   // null means "#all" -- all namespace defined above this one are excluded.

        public NsDecl(NsDecl? prev, string? prefix, string? nsUri)
        {
            Debug.Assert(nsUri != null || Prefix == null);
            this.Prev = prev;
            this.Prefix = prefix;
            this.NsUri = nsUri;
        }
    }

    internal class XslNode
    {
        public readonly XslNodeType NodeType;
        public ISourceLineInfo? SourceLine;
        public NsDecl? Namespaces;
        public readonly QilName? Name;   // name or mode
        public readonly object? Arg;    // select or test or terminate or stylesheet;-)
        public readonly XslVersion XslVersion;
        public XslFlags Flags;
        private List<XslNode>? _content;

        public XslNode(XslNodeType nodeType, QilName? name, object? arg, XslVersion xslVer)
        {
            this.NodeType = nodeType;
            this.Name = name;
            this.Arg = arg;
            this.XslVersion = xslVer;
        }

        public XslNode(XslNodeType nodeType)
        {
            this.NodeType = nodeType;
            this.XslVersion = XslVersion.Current;
        }

        public string? Select { get { return (string?)Arg; } }
        public bool ForwardsCompatible { get { return XslVersion == XslVersion.ForwardsCompatible; } }

        // -------------------------------- Content Management --------------------------------

        private static readonly IList<XslNode> s_emptyList = new List<XslNode>().AsReadOnly();

        public IList<XslNode> Content
        {
            get { return _content ?? s_emptyList; }
        }

        public void SetContent(List<XslNode>? content)
        {
            _content = content;
        }

        public void AddContent(XslNode node)
        {
            Debug.Assert(node != null);
            _content ??= new List<XslNode>();
            _content.Add(node);
        }

        public void InsertContent(IEnumerable<XslNode> collection)
        {
            if (_content == null)
            {
                _content = new List<XslNode>(collection);
            }
            else
            {
                _content.InsertRange(0, collection);
            }
        }
    }

    internal abstract class ProtoTemplate : XslNode
    {
        public QilFunction? Function;                   // Compiled body

        public ProtoTemplate(XslNodeType nt, QilName? name, XslVersion xslVer) : base(nt, name, null, xslVer) { }
        public abstract string GetDebugName();
    }

    internal enum CycleCheck
    {
        NotStarted = 0,
        Processing = 1,
        Completed = 2,
    }

    internal sealed class AttributeSet : ProtoTemplate
    {
        public CycleCheck CycleCheck;     // Used to detect circular references

        public AttributeSet(QilName name, XslVersion xslVer) : base(XslNodeType.AttributeSet, name, xslVer) { }

        public override string GetDebugName() => $"<xsl:attribute-set name=\"{Name!.QualifiedName}\">";

        public new void AddContent(XslNode node)
        {
            Debug.Assert(node != null && node.NodeType == XslNodeType.List);
            base.AddContent(node);
        }

        public void MergeContent(AttributeSet other)
        {
            InsertContent(other.Content);
        }
    }

    internal sealed class Template : ProtoTemplate
    {
        public readonly string? Match;
        public readonly QilName Mode;
        public readonly double Priority;
        public int ImportPrecedence;
        public int OrderNumber;

        public Template(QilName? name, string? match, QilName mode, double priority, XslVersion xslVer)
            : base(XslNodeType.Template, name, xslVer)
        {
            this.Match = match;
            this.Mode = mode;
            this.Priority = priority;
        }

        public override string GetDebugName()
        {
            StringBuilder dbgName = new StringBuilder();
            dbgName.Append("<xsl:template");
            if (Match != null)
            {
                dbgName.Append(" match=\"");
                dbgName.Append(Match);
                dbgName.Append('"');
            }
            if (Name != null)
            {
                dbgName.Append(" name=\"");
                dbgName.Append(Name.QualifiedName);
                dbgName.Append('"');
            }
            if (!double.IsNaN(Priority))
            {
                dbgName.Append(" priority=\"");
                dbgName.Append(Priority.ToString(CultureInfo.InvariantCulture));
                dbgName.Append('"');
            }
            if (Mode.LocalName.Length != 0)
            {
                dbgName.Append(" mode=\"");
                dbgName.Append(Mode.QualifiedName);
                dbgName.Append('"');
            }
            dbgName.Append('>');
            return dbgName.ToString();
        }
    }

    internal sealed class VarPar : XslNode
    {
        public XslFlags DefValueFlags;
        public QilNode? Value;          // Contains value for WithParams and global VarPars

        public VarPar(XslNodeType nt, QilName name, string? select, XslVersion xslVer) : base(nt, name, select, xslVer) { }
    }

    internal sealed class Sort : XslNode
    {
        public readonly string? Lang;
        public readonly string? DataType;
        public readonly string? Order;
        public readonly string? CaseOrder;

        public Sort(string select, string? lang, string? dataType, string? order, string? caseOrder, XslVersion xslVer)
            : base(XslNodeType.Sort, null, select, xslVer)
        {
            this.Lang = lang;
            this.DataType = dataType;
            this.Order = order;
            this.CaseOrder = caseOrder;
        }
    }

    internal sealed class Keys : KeyedCollection<QilName, List<Key>>
    {
        protected override QilName GetKeyForItem(List<Key> list)
        {
            Debug.Assert(list != null && list.Count > 0);
            return list[0].Name!;
        }
    }

    internal sealed class Key : XslNode
    {
        public readonly string? Match;
        public readonly string? Use;
        public QilFunction? Function;

        public Key(QilName name, string? match, string? use, XslVersion xslVer)
            : base(XslNodeType.Key, name, null, xslVer)
        {
            // match and use can be null in case of incorrect stylesheet
            Debug.Assert(name != null);
            this.Match = match;
            this.Use = use;
        }

        public string GetDebugName()
        {
            StringBuilder dbgName = new StringBuilder();
            dbgName.Append("<xsl:key name=\"");
            dbgName.Append(Name!.QualifiedName);
            dbgName.Append('"');

            if (Match != null)
            {
                dbgName.Append(" match=\"");
                dbgName.Append(Match);
                dbgName.Append('"');
            }
            if (Use != null)
            {
                dbgName.Append(" use=\"");
                dbgName.Append(Use);
                dbgName.Append('"');
            }
            dbgName.Append('>');
            return dbgName.ToString();
        }
    }

    internal enum NumberLevel
    {
        Single,
        Multiple,
        Any,
    }

    internal sealed class Number : XslNode
    {
        public readonly NumberLevel Level;
        public readonly string? Count;
        public readonly string? From;
        public readonly string? Value;
        public readonly string Format;
        public readonly string? Lang;
        public readonly string? LetterValue;
        public readonly string? GroupingSeparator;
        public readonly string? GroupingSize;

        public Number(NumberLevel level, string? count, string? from, string? value,
            string format, string? lang, string? letterValue, string? groupingSeparator, string? groupingSize,
            XslVersion xslVer) : base(XslNodeType.Number, null, null, xslVer)
        {
            this.Level = level;
            this.Count = count;
            this.From = from;
            this.Value = value;
            this.Format = format;
            this.Lang = lang;
            this.LetterValue = letterValue;
            this.GroupingSeparator = groupingSeparator;
            this.GroupingSize = groupingSize;
        }
    }

    internal sealed class NodeCtor : XslNode
    {
        public readonly string NameAvt;
        public readonly string? NsAvt;

        public NodeCtor(XslNodeType nt, string nameAvt, string? nsAvt, XslVersion xslVer)
            : base(nt, null, null, xslVer)
        {
            this.NameAvt = nameAvt;
            this.NsAvt = nsAvt;
        }
    }

    internal sealed class Text : XslNode
    {
        public readonly SerializationHints Hints;

        public Text(string data, SerializationHints hints, XslVersion xslVer)
            : base(XslNodeType.Text, null, data, xslVer)
        {
            this.Hints = hints;
        }
    }

    internal sealed class XslNodeEx : XslNode
    {
        public readonly ISourceLineInfo? ElemNameLi;
        public readonly ISourceLineInfo? EndTagLi;

        public XslNodeEx(XslNodeType t, QilName? name, object? arg, ContextInfo ctxInfo, XslVersion xslVer)
            : base(t, name, arg, xslVer)
        {
            ElemNameLi = ctxInfo.elemNameLi;
            EndTagLi = ctxInfo.endTagLi;
        }

        public XslNodeEx(XslNodeType t, QilName? name, object? arg, XslVersion xslVer) : base(t, name, arg, xslVer)
        {
        }
    }

    internal static class AstFactory
    {
        public static XslNode XslNode(XslNodeType nodeType, QilName? name, string? arg, XslVersion xslVer)
        {
            return new XslNode(nodeType, name, arg, xslVer);
        }

        public static XslNode ApplyImports(QilName mode, Stylesheet? sheet, XslVersion xslVer)
        {
            return new XslNode(XslNodeType.ApplyImports, mode, sheet, xslVer);
        }

        public static XslNodeEx ApplyTemplates(QilName mode, string select, ContextInfo ctxInfo, XslVersion xslVer)
        {
            return new XslNodeEx(XslNodeType.ApplyTemplates, mode, select, ctxInfo, xslVer);
        }

        // Special node for start apply-templates
        public static XslNodeEx ApplyTemplates(QilName mode)
        {
            return new XslNodeEx(XslNodeType.ApplyTemplates, mode, /*select:*/null, XslVersion.Current);
        }

        public static NodeCtor Attribute(string nameAvt, string? nsAvt, XslVersion xslVer)
        {
            return new NodeCtor(XslNodeType.Attribute, nameAvt, nsAvt, xslVer);
        }

        public static AttributeSet AttributeSet(QilName name)
        {
            return new AttributeSet(name, XslVersion.Current);
        }

        public static XslNodeEx CallTemplate(QilName? name, ContextInfo ctxInfo)
        {
            return new XslNodeEx(XslNodeType.CallTemplate, name, null, ctxInfo, XslVersion.Current);
        }

        public static XslNode Choose()
        {
            return new XslNode(XslNodeType.Choose);
        }

        public static XslNode Comment()
        {
            return new XslNode(XslNodeType.Comment);
        }

        public static XslNode Copy()
        {
            return new XslNode(XslNodeType.Copy);
        }

        public static XslNode CopyOf(string? select, XslVersion xslVer)
        {
            return new XslNode(XslNodeType.CopyOf, null, select, xslVer);
        }

        public static NodeCtor Element(string nameAvt, string? nsAvt, XslVersion xslVer)
        {
            return new NodeCtor(XslNodeType.Element, nameAvt, nsAvt, xslVer);
        }

        public static XslNode Error(string message)
        {
            return new XslNode(XslNodeType.Error, null, message, XslVersion.Current);
        }

        public static XslNodeEx ForEach(string? select, ContextInfo ctxInfo, XslVersion xslVer)
        {
            return new XslNodeEx(XslNodeType.ForEach, null, select, ctxInfo, xslVer);
        }

        public static XslNode If(string? test, XslVersion xslVer)
        {
            return new XslNode(XslNodeType.If, null, test, xslVer);
        }

        public static Key Key(QilName name, string? match, string? use, XslVersion xslVer)
        {
            return new Key(name, match, use, xslVer);
        }

        public static XslNode List()
        {
            return new XslNode(XslNodeType.List);
        }

        public static XslNode LiteralAttribute(QilName name, string value, XslVersion xslVer)
        {
            return new XslNode(XslNodeType.LiteralAttribute, name, value, xslVer);
        }

        public static XslNode LiteralElement(QilName name)
        {
            return new XslNode(XslNodeType.LiteralElement, name, null, XslVersion.Current);
        }

        public static XslNode Message(bool term)
        {
            return new XslNode(XslNodeType.Message, null, term, XslVersion.Current);
        }

        public static XslNode Nop()
        {
            return new XslNode(XslNodeType.Nop);
        }

        public static Number Number(NumberLevel level, string? count, string? from, string? value,
            string format, string? lang, string? letterValue, string? groupingSeparator, string? groupingSize,
            XslVersion xslVer)
        {
            return new Number(level, count, from, value, format, lang, letterValue, groupingSeparator, groupingSize, xslVer);
        }

        public static XslNode Otherwise()
        {
            return new XslNode(XslNodeType.Otherwise);
        }

        public static XslNode PI(string name, XslVersion xslVer)
        {
            return new XslNode(XslNodeType.PI, null, name, xslVer);
        }

        public static Sort Sort(string select, string? lang, string? dataType, string? order, string? caseOrder, XslVersion xslVer)
        {
            return new Sort(select, lang, dataType, order, caseOrder, xslVer);
        }

        public static Template Template(QilName? name, string? match, QilName mode, double priority, XslVersion xslVer)
        {
            return new Template(name, match, mode, priority, xslVer);
        }

        public static XslNode Text(string data)
        {
            return new Text(data, SerializationHints.None, XslVersion.Current);
        }

        public static XslNode Text(string data, SerializationHints hints)
        {
            return new Text(data, hints, XslVersion.Current);
        }

        public static XslNode UseAttributeSet(QilName name)
        {
            return new XslNode(XslNodeType.UseAttributeSet, name, null, XslVersion.Current);
        }

        public static VarPar VarPar(XslNodeType nt, QilName name, string? select, XslVersion xslVer)
        {
            return new VarPar(nt, name, select, xslVer);
        }

        public static VarPar WithParam(QilName name)
        {
            return VarPar(XslNodeType.WithParam, name, /*select*/null, XslVersion.Current);
        }

        private static readonly QilFactory s_f = new QilFactory();

        public static QilName QName(string local, string uri, string prefix)
        {
            return s_f.LiteralQName(local, uri, prefix);
        }

        public static QilName QName(string local)
        {
            return s_f.LiteralQName(local);
        }
    }
}
