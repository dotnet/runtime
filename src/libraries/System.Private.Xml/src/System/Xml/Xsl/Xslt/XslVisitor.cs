// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.Xslt
{
    internal abstract class XslVisitor<T>
    {
        protected virtual T Visit(XslNode node) =>
            node.NodeType switch
            {
                XslNodeType.ApplyImports => VisitApplyImports((XslNode)node),
                XslNodeType.ApplyTemplates => VisitApplyTemplates((XslNode)node),
                XslNodeType.Attribute => VisitAttribute((NodeCtor)node),
                XslNodeType.AttributeSet => VisitAttributeSet((AttributeSet)node),
                XslNodeType.CallTemplate => VisitCallTemplate((XslNode)node),
                XslNodeType.Choose => VisitChoose((XslNode)node),
                XslNodeType.Comment => VisitComment((XslNode)node),
                XslNodeType.Copy => VisitCopy((XslNode)node),
                XslNodeType.CopyOf => VisitCopyOf((XslNode)node),
                XslNodeType.Element => VisitElement((NodeCtor)node),
                XslNodeType.Error => VisitError((XslNode)node),
                XslNodeType.ForEach => VisitForEach((XslNode)node),
                XslNodeType.If => VisitIf((XslNode)node),
                XslNodeType.Key => VisitKey((Key)node),
                XslNodeType.List => VisitList((XslNode)node),
                XslNodeType.LiteralAttribute => VisitLiteralAttribute((XslNode)node),
                XslNodeType.LiteralElement => VisitLiteralElement((XslNode)node),
                XslNodeType.Message => VisitMessage((XslNode)node),
                XslNodeType.Nop => VisitNop((XslNode)node),
                XslNodeType.Number => VisitNumber((Number)node),
                XslNodeType.Otherwise => VisitOtherwise((XslNode)node),
                XslNodeType.Param => VisitParam((VarPar)node),
                XslNodeType.PI => VisitPI((XslNode)node),
                XslNodeType.Sort => VisitSort((Sort)node),
                XslNodeType.Template => VisitTemplate((Template)node),
                XslNodeType.Text => VisitText((Text)node),
                XslNodeType.UseAttributeSet => VisitUseAttributeSet((XslNode)node),
                XslNodeType.ValueOf => VisitValueOf((XslNode)node),
                XslNodeType.ValueOfDoe => VisitValueOfDoe((XslNode)node),
                XslNodeType.Variable => VisitVariable((VarPar)node),
                XslNodeType.WithParam => VisitWithParam((VarPar)node),
                _ => VisitUnknown((XslNode)node),
            };

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
