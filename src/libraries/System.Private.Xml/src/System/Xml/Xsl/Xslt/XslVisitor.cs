// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.Xslt
{
    internal abstract class XslVisitor<T>
    {
        protected virtual T Visit(XslNode node)
        {
            switch (node.NodeType)
            {
                case XslNodeType.ApplyImports: return VisitApplyImports((XslNode)node);
                case XslNodeType.ApplyTemplates: return VisitApplyTemplates((XslNode)node);
                case XslNodeType.Attribute: return VisitAttribute((NodeCtor)node);
                case XslNodeType.AttributeSet: return VisitAttributeSet((AttributeSet)node);
                case XslNodeType.CallTemplate: return VisitCallTemplate((XslNode)node);
                case XslNodeType.Choose: return VisitChoose((XslNode)node);
                case XslNodeType.Comment: return VisitComment((XslNode)node);
                case XslNodeType.Copy: return VisitCopy((XslNode)node);
                case XslNodeType.CopyOf: return VisitCopyOf((XslNode)node);
                case XslNodeType.Element: return VisitElement((NodeCtor)node);
                case XslNodeType.Error: return VisitError((XslNode)node);
                case XslNodeType.ForEach: return VisitForEach((XslNode)node);
                case XslNodeType.If: return VisitIf((XslNode)node);
                case XslNodeType.Key: return VisitKey((Key)node);
                case XslNodeType.List: return VisitList((XslNode)node);
                case XslNodeType.LiteralAttribute: return VisitLiteralAttribute((XslNode)node);
                case XslNodeType.LiteralElement: return VisitLiteralElement((XslNode)node);
                case XslNodeType.Message: return VisitMessage((XslNode)node);
                case XslNodeType.Nop: return VisitNop((XslNode)node);
                case XslNodeType.Number: return VisitNumber((Number)node);
                case XslNodeType.Otherwise: return VisitOtherwise((XslNode)node);
                case XslNodeType.Param: return VisitParam((VarPar)node);
                case XslNodeType.PI: return VisitPI((XslNode)node);
                case XslNodeType.Sort: return VisitSort((Sort)node);
                case XslNodeType.Template: return VisitTemplate((Template)node);
                case XslNodeType.Text: return VisitText((Text)node);
                case XslNodeType.UseAttributeSet: return VisitUseAttributeSet((XslNode)node);
                case XslNodeType.ValueOf: return VisitValueOf((XslNode)node);
                case XslNodeType.ValueOfDoe: return VisitValueOfDoe((XslNode)node);
                case XslNodeType.Variable: return VisitVariable((VarPar)node);
                case XslNodeType.WithParam: return VisitWithParam((VarPar)node);
                default: return VisitUnknown((XslNode)node);
            }
        }

        protected virtual T VisitApplyImports(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitApplyTemplates(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitAttribute(NodeCtor node) { return VisitChildren(node); }
        protected virtual T VisitAttributeSet(AttributeSet node) { return VisitChildren(node); }
        protected virtual T VisitCallTemplate(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitChoose(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitComment(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitCopy(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitCopyOf(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitElement(NodeCtor node) { return VisitChildren(node); }
        protected virtual T VisitError(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitForEach(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitIf(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitKey(Key node) { return VisitChildren(node); }
        protected virtual T VisitList(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitLiteralAttribute(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitLiteralElement(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitMessage(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitNop(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitNumber(Number node) { return VisitChildren(node); }
        protected virtual T VisitOtherwise(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitParam(VarPar node) { return VisitChildren(node); }
        protected virtual T VisitPI(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitSort(Sort node) { return VisitChildren(node); }
        protected virtual T VisitTemplate(Template node) { return VisitChildren(node); }
        protected virtual T VisitText(Text node) { return VisitChildren(node); }
        protected virtual T VisitUseAttributeSet(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitValueOf(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitValueOfDoe(XslNode node) { return VisitChildren(node); }
        protected virtual T VisitVariable(VarPar node) { return VisitChildren(node); }
        protected virtual T VisitWithParam(VarPar node) { return VisitChildren(node); }
        protected virtual T VisitUnknown(XslNode node) { return VisitChildren(node); }

        protected virtual T VisitChildren(XslNode node)
        {
            foreach (XslNode child in node.Content)
            {
                this.Visit(child);
            }
            return default(T);
        }
    }
}
