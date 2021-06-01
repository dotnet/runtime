// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    public abstract class XmlSchemaParticle : XmlSchemaAnnotated
    {
        [Flags]
        private enum Occurs
        {
            None,
            Min,
            Max
        };
        private decimal _minOccurs = decimal.One;
        private decimal _maxOccurs = decimal.One;
        private Occurs _flags = Occurs.None;

        [XmlAttribute("minOccurs")]
        public string? MinOccursString
        {
            get
            {
                return (_flags & Occurs.Min) == 0 ? null : XmlConvert.ToString(_minOccurs);
            }
            set
            {
                if (value == null)
                {
                    _minOccurs = decimal.One;
                    _flags &= ~Occurs.Min;
                }
                else
                {
                    _minOccurs = XmlConvert.ToInteger(value);
                    if (_minOccurs < decimal.Zero)
                    {
                        throw new XmlSchemaException(SR.Sch_MinOccursInvalidXsd, string.Empty);
                    }
                    _flags |= Occurs.Min;
                }
            }
        }

        [XmlAttribute("maxOccurs")]
        public string? MaxOccursString
        {
            get
            {
                return (_flags & Occurs.Max) == 0 ? null : (_maxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(_maxOccurs);
            }
            set
            {
                if (value == null)
                {
                    _maxOccurs = decimal.One;
                    _flags &= ~Occurs.Max;
                }
                else
                {
                    if (value == "unbounded")
                    {
                        _maxOccurs = decimal.MaxValue;
                    }
                    else
                    {
                        _maxOccurs = XmlConvert.ToInteger(value);
                        if (_maxOccurs < decimal.Zero)
                        {
                            throw new XmlSchemaException(SR.Sch_MaxOccursInvalidXsd, string.Empty);
                        }
                        else if (_maxOccurs == decimal.Zero && (_flags & Occurs.Min) == 0)
                        {
                            _minOccurs = decimal.Zero;
                        }
                    }
                    _flags |= Occurs.Max;
                }
            }
        }

        [XmlIgnore]
        public decimal MinOccurs
        {
            get { return _minOccurs; }
            set
            {
                if (value < decimal.Zero || value != decimal.Truncate(value))
                {
                    throw new XmlSchemaException(SR.Sch_MinOccursInvalidXsd, string.Empty);
                }
                _minOccurs = value;
                _flags |= Occurs.Min;
            }
        }

        [XmlIgnore]
        public decimal MaxOccurs
        {
            get { return _maxOccurs; }
            set
            {
                if (value < decimal.Zero || value != decimal.Truncate(value))
                {
                    throw new XmlSchemaException(SR.Sch_MaxOccursInvalidXsd, string.Empty);
                }
                _maxOccurs = value;
                if (_maxOccurs == decimal.Zero && (_flags & Occurs.Min) == 0)
                {
                    _minOccurs = decimal.Zero;
                }
                _flags |= Occurs.Max;
            }
        }

        internal virtual bool IsEmpty
        {
            get { return _maxOccurs == decimal.Zero; }
        }

        internal bool IsMultipleOccurrence
        {
            get { return _maxOccurs > decimal.One; }
        }

        internal virtual string NameString
        {
            get
            {
                return string.Empty;
            }
        }

        internal XmlQualifiedName GetQualifiedName()
        {
            XmlSchemaElement? elem = this as XmlSchemaElement;
            if (elem != null)
            {
                return elem.QualifiedName;
            }
            else
            {
                XmlSchemaAny? any = this as XmlSchemaAny;
                if (any != null)
                {
                    string? ns = any.Namespace;
                    if (ns != null)
                    {
                        ns = ns.Trim();
                    }
                    else
                    {
                        ns = string.Empty;
                    }
                    return new XmlQualifiedName("*", ns.Length == 0 ? "##any" : ns);
                }
            }
            return XmlQualifiedName.Empty; //If ever called on other particles
        }

        private sealed class EmptyParticle : XmlSchemaParticle
        {
            internal override bool IsEmpty
            {
                get { return true; }
            }
        }

        internal static readonly XmlSchemaParticle Empty = new EmptyParticle();
    }
}
