// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Xml.XPath;
using MS.Internal.Xml.XPath;

namespace System.Xml.Schema
{
    internal sealed class CompiledIdentityConstraint
    {
        internal XmlQualifiedName name = XmlQualifiedName.Empty;
        private readonly ConstraintRole _role;
        private readonly Asttree _selector = null!;
        private readonly Asttree[] _fields = null!;
        internal XmlQualifiedName refer = XmlQualifiedName.Empty;

        public enum ConstraintRole
        {
            Unique,
            Key,
            Keyref
        }

        public ConstraintRole Role
        {
            get { return _role; }
        }

        public Asttree Selector
        {
            get { return _selector; }
        }

        public Asttree[] Fields
        {
            get { return _fields; }
        }

        public static readonly CompiledIdentityConstraint Empty = new CompiledIdentityConstraint();

        private CompiledIdentityConstraint() { }

        public CompiledIdentityConstraint(XmlSchemaIdentityConstraint constraint, XmlNamespaceManager nsmgr)
        {
            this.name = constraint.QualifiedName;

            //public Asttree (string xPath, bool isField, XmlNamespaceManager nsmgr)
            try
            {
                _selector = new Asttree(constraint.Selector!.XPath!, false, nsmgr);
            }
            catch (XmlSchemaException e)
            {
                e.SetSource(constraint.Selector!);
                throw;
            }
            XmlSchemaObjectCollection fields = constraint.Fields;
            Debug.Assert(fields.Count > 0);
            _fields = new Asttree[fields.Count];
            for (int idxField = 0; idxField < fields.Count; idxField++)
            {
                try
                {
                    _fields[idxField] = new Asttree(((XmlSchemaXPath)fields[idxField]).XPath!, true, nsmgr);
                }
                catch (XmlSchemaException e)
                {
                    e.SetSource(constraint.Fields[idxField]);
                    throw;
                }
            }
            if (constraint is XmlSchemaUnique)
            {
                _role = ConstraintRole.Unique;
            }
            else if (constraint is XmlSchemaKey)
            {
                _role = ConstraintRole.Key;
            }
            else
            {             // XmlSchemaKeyref
                _role = ConstraintRole.Keyref;
                this.refer = ((XmlSchemaKeyref)constraint).Refer;
            }
        }
    }
}
