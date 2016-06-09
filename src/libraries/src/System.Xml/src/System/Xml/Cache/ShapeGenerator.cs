// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if ENABLEDATABINDING
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Text;

namespace System.Xml.XPath.DataBinding
{
    internal sealed class ShapeGenerator
    {
        private Hashtable elementTypesProcessed;
        private IXmlNamespaceResolver nsResolver;

        public ShapeGenerator(IXmlNamespaceResolver nsResolver) {
            this.elementTypesProcessed = new Hashtable();
            this.nsResolver = nsResolver;
        }

        public Shape GenerateFromSchema(XmlSchemaElement xse) {
            XmlQualifiedName xseName = xse.QualifiedName;
            XmlSchemaType schemaType = xse.ElementSchemaType;
            XmlSchemaComplexType complexType = schemaType as XmlSchemaComplexType;
            if (null != complexType) {
                XmlSchemaParticle particle = null;
                Shape rootShape = null;
                
                XmlSchemaContentType contentType = complexType.ElementDecl.ContentValidator.ContentType;
                switch (contentType) {
                    case XmlSchemaContentType.Mixed:
                    case XmlSchemaContentType.TextOnly:
                        rootShape = new Shape(GenName(xseName) + "_Text", BindingType.Text);
                        rootShape.AddParticle(xse);
                    break;

                    case XmlSchemaContentType.Empty:
                        rootShape = new Shape(null, BindingType.Sequence);
                        break;

                    case XmlSchemaContentType.ElementOnly:
                        particle = complexType.ContentTypeParticle;
                        rootShape = ProcessParticle(particle, null);
                    break;

                }
            
                Debug.Assert(rootShape != null);
                if (complexType.AttributeUses.Values.Count > 0) {
                    if (rootShape.BindingType != BindingType.Sequence) {
                        Shape s = new Shape(null, BindingType.Sequence);
                        s.AddSubShape(rootShape);
                        rootShape = s;
                    }
                    int pos = 0;
                    string[] names = rootShape.SubShapeNames();

                    ICollection attributes = complexType.AttributeUses.Values;
                    XmlSchemaAttribute[] xsaArray = new XmlSchemaAttribute[attributes.Count];
                    attributes.CopyTo(xsaArray, 0);
                    Array.Sort(xsaArray, new XmlSchemaAttributeComparer());
                    foreach(XmlSchemaAttribute xsa in xsaArray) {
                        string name = GenAttrName(xsa.QualifiedName, names);
                        Shape attrShape = new Shape(name, BindingType.Attribute);
                        attrShape.AddParticle(xsa);
                        rootShape.AddAttrShapeAt(attrShape, pos++);
                    }
                }
                
                if (rootShape.BindingType != BindingType.Text) {
                    rootShape.Name = GenName(xseName);
                    rootShape.ContainerDecl = xse;
                }
                return rootShape;
            }
            else { // simple type
                Shape s = new Shape(GenName(xseName), BindingType.Text);
                s.AddParticle(xse);
                return s;
            }
        }

        public Shape GenerateFromSchema(XmlSchemaAttribute xsa) {
            Shape s = new Shape(GenName(xsa.QualifiedName), BindingType.Attribute);
            s.AddParticle(xsa);
            return s;
        }

        Shape ProcessParticle(XmlSchemaParticle xsp, Shape parent) {
            Shape s;
            if (xsp == XmlSchemaParticle.Empty) {
                return null;
            }
            if (xsp is XmlSchemaElement) {
                s = ProcessParticleElement((XmlSchemaElement)xsp);
            } 
            else if (xsp is XmlSchemaSequence) {
                s = ProcessParticleGroup((XmlSchemaSequence)xsp, BindingType.Sequence);
            } 
            else if (xsp is XmlSchemaChoice) {
                s = ProcessParticleGroup((XmlSchemaChoice)xsp, BindingType.Choice);
            }
            else if (xsp is XmlSchemaAll) {
                s = ProcessParticleGroup((XmlSchemaAll)xsp, BindingType.All);
            }
            else { //XmlSchemaAny
                return null; //Ignore Any in the content model
            }
            if (xsp.MaxOccurs > 1) {
                Shape rep = new Shape(s.Name, BindingType.Repeat);
                rep.AddSubShape(s);
                s = rep;
            }
            if (parent != null)
                parent.AddSubShape(s);
            return s;
        }

        Shape ProcessParticleElement(XmlSchemaElement xse) {
            // watch out for recursive schema
            Shape s = (Shape)this.elementTypesProcessed[xse];
            if (null != s)
                return s;

            bool complex = xse.ElementSchemaType is XmlSchemaComplexType; 
            s = new Shape(GenName(xse.QualifiedName), complex ? BindingType.ElementNested : BindingType.Element);
            s.AddParticle(xse);
            
            if (complex) {
                this.elementTypesProcessed.Add(xse, s);
                s.NestedShape = GenerateFromSchema(xse);
                this.elementTypesProcessed.Remove(xse);
            }
            return s;
        }

        Shape ProcessParticleGroup(XmlSchemaGroupBase xsg, BindingType bt) {
            Shape s = new Shape(null, bt);
            StringBuilder sb = new StringBuilder();
            foreach (XmlSchemaParticle xsp in xsg.Items) {
                Shape sub = ProcessParticle(xsp, s);
                if (sub != null) { //sub can be null if the child particle is xs:any
                    if (sb.Length > 0)
                        sb.Append('_');
                    sb.Append(sub.Name);
                }
            }
            // need to also test if paretn != null for this to work
            //if (s.IsGroup && s.SubShapes.Count == 1) {
            //    Shape sub = (Shape)s.SubShapes[0];
            //    s.Clear();
            //    return sub;
            //}
            s.Name = sb.ToString();
            return s;
        }

        string GenName(XmlQualifiedName xqn) {
            string ns = xqn.Namespace;
            string ln = xqn.Name;
            if (ns.Length != 0) {
                string prefix = (null==this.nsResolver) ? null : this.nsResolver.LookupPrefix(ns);
            if (prefix != null && prefix.Length != 0)
                    return String.Concat(prefix, ":", ln);
            }
            return ln;
        }

        string GenAttrName(XmlQualifiedName xqn, string[] names) {
            string name = GenName(xqn);
            if (null != names) {
                for (int i=0; i<names.Length; i++) {
                    if (name == names[i]) {
                        return String.Concat("@", name);
                    }
                }
            }
            return name;
        }

        public void ResetState() {
            this.elementTypesProcessed.Clear();
        }

        class XmlSchemaAttributeComparer : IComparer {
            public virtual int Compare(object a, object b) {
                XmlSchemaAttribute xsaA = (XmlSchemaAttribute)a;
                XmlSchemaAttribute xsaB = (XmlSchemaAttribute)b;
                return XmlQualifiedName.Compare(xsaA.QualifiedName, xsaB.QualifiedName);
            }
        }
    }
}
#endif