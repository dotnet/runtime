// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class SortAction : CompiledAction
    {
        private int _selectKey = Compiler.InvalidQueryKey;
        private Avt? _langAvt;
        private Avt? _dataTypeAvt;
        private Avt? _orderAvt;
        private Avt? _caseOrderAvt;
        // Compile time precalculated AVTs
        private string? _lang;
        private XmlDataType _dataType = XmlDataType.Text;
        private XmlSortOrder _order = XmlSortOrder.Ascending;
        private XmlCaseOrder _caseOrder = XmlCaseOrder.None;
        private Sort? _sort; //When we not have AVTs at all we can do this. null otherwise.
        private bool _forwardCompatibility;
        private InputScopeManager? _manager;

        private string? ParseLang(string? value)
        {
            if (value == null)
            { // Avt is not constant, or attribute wasn't defined
                return null;
            }
            // XmlComplianceUtil.IsValidLanguageID uses the outdated RFC 1766. It would be
            // better to remove this method completely and not call it here, but that may
            // change exception types for some stylesheets.
            CultureInfo cultInfo = new CultureInfo(value);
            if (!XmlComplianceUtil.IsValidLanguageID(value.ToCharArray(), 0, value.Length)
                && (value.Length == 0 || cultInfo == null)
            )
            {
                if (_forwardCompatibility)
                {
                    return null;
                }
                throw XsltException.Create(SR.Xslt_InvalidAttrValue, "lang", value);
            }
            return value;
        }

        private XmlDataType ParseDataType(string? value, InputScopeManager manager)
        {
            if (value == null)
            { // Avt is not constant, or attribute wasn't defined
                return XmlDataType.Text;
            }
            if (value == "text")
            {
                return XmlDataType.Text;
            }
            if (value == "number")
            {
                return XmlDataType.Number;
            }
            string prefix;
            PrefixQName.ParseQualifiedName(value, out prefix, out _);
            manager.ResolveXmlNamespace(prefix);
            if (prefix.Length == 0 && !_forwardCompatibility)
            {
                throw XsltException.Create(SR.Xslt_InvalidAttrValue, "data-type", value);
            }
            return XmlDataType.Text;
        }

        private XmlSortOrder ParseOrder(string? value)
        {
            if (value == null)
            { // Avt is not constant, or attribute wasn't defined
                return XmlSortOrder.Ascending;
            }
            if (value == "ascending")
            {
                return XmlSortOrder.Ascending;
            }
            if (value == "descending")
            {
                return XmlSortOrder.Descending;
            }
            if (_forwardCompatibility)
            {
                return XmlSortOrder.Ascending;
            }
            throw XsltException.Create(SR.Xslt_InvalidAttrValue, "order", value);
        }

        private XmlCaseOrder ParseCaseOrder(string? value)
        {
            if (value == null)
            { // Avt is not constant, or attribute wasn't defined
                return XmlCaseOrder.None;
            }
            if (value == "upper-first")
            {
                return XmlCaseOrder.UpperFirst;
            }
            if (value == "lower-first")
            {
                return XmlCaseOrder.LowerFirst;
            }
            if (_forwardCompatibility)
            {
                return XmlCaseOrder.None;
            }
            throw XsltException.Create(SR.Xslt_InvalidAttrValue, "case-order", value);
        }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckEmpty(compiler);
            if (_selectKey == Compiler.InvalidQueryKey)
            {
                _selectKey = compiler.AddQuery(".");
            }

            _forwardCompatibility = compiler.ForwardCompatibility;
            _manager = compiler.CloneScopeManager();

            _lang = ParseLang(PrecalculateAvt(ref _langAvt));
            _dataType = ParseDataType(PrecalculateAvt(ref _dataTypeAvt), _manager);
            _order = ParseOrder(PrecalculateAvt(ref _orderAvt));
            _caseOrder = ParseCaseOrder(PrecalculateAvt(ref _caseOrderAvt));

            if (_langAvt == null && _dataTypeAvt == null && _orderAvt == null && _caseOrderAvt == null)
            {
                _sort = new Sort(_selectKey, _lang, _dataType, _order, _caseOrder);
            }
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;

            if (Ref.Equal(name, compiler.Atoms.Select))
            {
                _selectKey = compiler.AddQuery(value);
            }
            else if (Ref.Equal(name, compiler.Atoms.Lang))
            {
                _langAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.DataType))
            {
                _dataTypeAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.Order))
            {
                _orderAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.CaseOrder))
            {
                _caseOrderAvt = Avt.CompileAvt(compiler, value);
            }
            else
            {
                return false;
            }
            return true;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            Debug.Assert(frame.State == Initialized);

            processor.AddSort(_sort ??
                new Sort(
                    _selectKey,
                    _langAvt == null ? _lang : ParseLang(_langAvt.Evaluate(processor, frame)),
                    _dataTypeAvt == null ? _dataType : ParseDataType(_dataTypeAvt.Evaluate(processor, frame), _manager!),
                    _orderAvt == null ? _order : ParseOrder(_orderAvt.Evaluate(processor, frame)),
                    _caseOrderAvt == null ? _caseOrder : ParseCaseOrder(_caseOrderAvt.Evaluate(processor, frame))
                )
            );
            frame.Finished();
        }
    }
}
