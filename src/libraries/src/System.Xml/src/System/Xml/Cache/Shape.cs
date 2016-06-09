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
    internal enum BindingType {
        Text,
        Element,
        Attribute,
        ElementNested,
        Repeat, 
        Sequence, 
        Choice, 
        All
    }

    internal sealed class Shape
    {
        string name;
        BindingType bindingType;
        ArrayList particles; // XmlSchemaElement or XmlSchemaAttribute
        ArrayList subShapes;
        Shape nestedShape;
        PropertyDescriptor[] propertyDescriptors;
        XmlSchemaElement containerDecl;

        static object[] emptyIList = new object[0];

        public Shape(string name, BindingType bindingType)
        {
            this.name = name;
            this.bindingType = bindingType;
        }

        public string Name { 
            get { return this.name; }
            set { this.name = value; }
        }

        public BindingType BindingType {
            get { return this.bindingType; }
            set { this.bindingType = value; }
        }

        public XmlSchemaElement ContainerDecl {
            get { return this.containerDecl; }
            set { this.containerDecl = value; }
        }

        public bool IsNestedTable {
            get {
                switch (this.BindingType) {
                    case BindingType.ElementNested:
                    case BindingType.Repeat:
                    case BindingType.Sequence:
                    case BindingType.Choice:
                    case BindingType.All:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsGroup {
            get {
                switch (this.BindingType) {
                    case BindingType.Sequence:
                    case BindingType.Choice:
                    case BindingType.All:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public XmlSchemaType SchemaType {
            get {
                switch (this.bindingType) {
                    case BindingType.Text:
                    case BindingType.Element:
                    case BindingType.ElementNested: {
                        Debug.Assert(this.particles.Count == 1);
                        XmlSchemaElement xse = (XmlSchemaElement)this.particles[0];
                        return xse.ElementSchemaType;
                    }

                    case BindingType.Attribute: {
                        Debug.Assert(this.particles.Count == 1);
                        XmlSchemaAttribute xsa = (XmlSchemaAttribute)this.particles[0];
                        return xsa.AttributeSchemaType;
                    }

                    default:
                        return null;
                }
            }
        }

        public XmlSchemaElement XmlSchemaElement {
            get {
                switch (this.bindingType) {
                    case BindingType.Text:
                    case BindingType.Element:
                    case BindingType.ElementNested: {
                        Debug.Assert(this.particles.Count == 1);
                        return (XmlSchemaElement)this.particles[0];
                    }

                    default:
                        return this.containerDecl;
                }
            }
        }

        public IList Particles {
            get {
                if (null == this.particles)
                    return emptyIList;
                return this.particles;
            }
        }

        public IList SubShapes {
            get {
                if (null == this.subShapes)
                    return emptyIList;
                return this.subShapes;
            }
        }

        public Shape SubShape(int i) {
            return (Shape)SubShapes[i];
        }

        public Shape NestedShape {
            get {
                //Debug.Assert(this.bindingType == BindingType.ElementNested);
                return this.nestedShape;
            }
            set {
                this.nestedShape = value;
            }
        }

        public XmlQualifiedName AttributeName {
            get {
                Debug.Assert(this.bindingType == BindingType.Attribute);
                XmlSchemaAttribute xsa = (XmlSchemaAttribute)this.particles[0];
                return xsa.QualifiedName;
            }
        }

        public void Clear() {
            if (this.subShapes != null) {
                this.subShapes.Clear();
                this.subShapes = null;
            }
            if (this.particles != null) {
                this.particles.Clear();
                this.particles = null;
            }
        }

        public void AddParticle(XmlSchemaElement elem) {
            if (null == this.particles)
                this.particles = new ArrayList();
            Debug.Assert(this.bindingType != BindingType.Attribute);
            this.particles.Add(elem);
        }

        public void AddParticle(XmlSchemaAttribute elem) {
            Debug.Assert(this.bindingType == BindingType.Attribute);
            Debug.Assert(this.particles == null);
            this.particles = new ArrayList();
            this.particles.Add(elem);
        }

        public void AddSubShape(Shape shape) {
            if (null == this.subShapes)
                this.subShapes = new ArrayList();
            this.subShapes.Add(shape);
            foreach (object p in shape.Particles) {
                XmlSchemaElement xse = p as XmlSchemaElement;
                if (null != xse)
                    AddParticle(xse);
            }
        }

        public void AddAttrShapeAt(Shape shape, int pos) {
            if (null == this.subShapes)
                this.subShapes = new ArrayList();
            this.subShapes.Insert(pos, shape);
        }

        public string[] SubShapeNames() {
            string[] names = new string[SubShapes.Count];
            for (int i=0; i<SubShapes.Count; i++)
                names[i] = this.SubShape(i).Name;
            return names;
        }

        public PropertyDescriptor[] PropertyDescriptors {
            get {
                if (null == this.propertyDescriptors) {
                    PropertyDescriptor[] descs;
                    switch (this.BindingType) {
                        case BindingType.Element:
                        case BindingType.Text:
                        case BindingType.Attribute:
                        case BindingType.Repeat:
                            descs = new PropertyDescriptor[1];
                            descs[0] = new XPathNodeViewPropertyDescriptor(this);
                            break;

                        case BindingType.ElementNested:
                            descs = this.nestedShape.PropertyDescriptors;
                            break;

                        case BindingType.Sequence:
                        case BindingType.Choice:
                        case BindingType.All:
                            descs = new PropertyDescriptor[SubShapes.Count];
                            for (int i=0; i < descs.Length; i++) {
                                descs[i] = new XPathNodeViewPropertyDescriptor(this, this.SubShape(i), i);
                            }
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                    this.propertyDescriptors = descs;
                }
                return this.propertyDescriptors;
            }
        }

        public int FindNamedSubShape(string name) {
            for (int i=0; i<SubShapes.Count; i++) {
                Shape shape = SubShape(i);
                if (shape.Name == name)
                    return i;
            }
            return -1;
        }

        public int FindMatchingSubShape(object particle) {
            for (int i=0; i<SubShapes.Count; i++) {
                Shape shape = SubShape(i);
                if (shape.IsParticleMatch(particle))
                    return i;
            }
            return -1;
        }

        public bool IsParticleMatch(object particle) {
            for (int i=0; i<this.particles.Count; i++) {
                if (particle == this.particles[i])
                    return true;
            }
            return false;
        }

#if DEBUG
        public string DebugDump() {
            StringBuilder sb = new StringBuilder();
            DebugDump(sb,"");
            return sb.ToString();
        }
        void DebugDump(StringBuilder sb, String indent) {
            sb.AppendFormat("{0}{1} '{2}'", indent, this.BindingType.ToString(), this.Name);
            if (this.subShapes != null) {
                sb.AppendLine(" {");
                string subindent = String.Concat(indent, "  ");
                foreach (Shape s in this.SubShapes) {
                    s.DebugDump(sb, subindent);
                }
                sb.Append(indent);
                sb.Append('}');
            }
            sb.AppendLine();
        }
#endif
    }
}
#endif