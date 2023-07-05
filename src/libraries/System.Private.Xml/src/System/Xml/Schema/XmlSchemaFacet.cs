// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    internal enum FacetType
    {
        None,
        Length,
        MinLength,
        MaxLength,
        Pattern,
        Whitespace,
        Enumeration,
        MinExclusive,
        MinInclusive,
        MaxExclusive,
        MaxInclusive,
        TotalDigits,
        FractionDigits,
    }

    public abstract class XmlSchemaFacet : XmlSchemaAnnotated
    {
        private string? _value;
        private bool _isFixed;
        private FacetType _facetType;

        [XmlAttribute("value")]
        public string? Value
        {
            get { return _value; }
            set { _value = value; }
        }

        [XmlAttribute("fixed"), DefaultValue(false)]
        public virtual bool IsFixed
        {
            get { return _isFixed; }
            set
            {
                if (!(this is XmlSchemaEnumerationFacet) && !(this is XmlSchemaPatternFacet))
                {
                    _isFixed = value;
                }
            }
        }

        internal FacetType FacetType
        {
            get
            {
                return _facetType;
            }
            set
            {
                _facetType = value;
            }
        }
    }

    public abstract class XmlSchemaNumericFacet : XmlSchemaFacet { }

    public class XmlSchemaLengthFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaLengthFacet()
        {
            FacetType = FacetType.Length;
        }
    }

    public class XmlSchemaMinLengthFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaMinLengthFacet()
        {
            FacetType = FacetType.MinLength;
        }
    }

    public class XmlSchemaMaxLengthFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaMaxLengthFacet()
        {
            FacetType = FacetType.MaxLength;
        }
    }

    public class XmlSchemaPatternFacet : XmlSchemaFacet
    {
        public XmlSchemaPatternFacet()
        {
            FacetType = FacetType.Pattern;
        }
    }

    public class XmlSchemaEnumerationFacet : XmlSchemaFacet
    {
        public XmlSchemaEnumerationFacet()
        {
            FacetType = FacetType.Enumeration;
        }
    }

    public class XmlSchemaMinExclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMinExclusiveFacet()
        {
            FacetType = FacetType.MinExclusive;
        }
    }

    public class XmlSchemaMinInclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMinInclusiveFacet()
        {
            FacetType = FacetType.MinInclusive;
        }
    }

    public class XmlSchemaMaxExclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMaxExclusiveFacet()
        {
            FacetType = FacetType.MaxExclusive;
        }
    }

    public class XmlSchemaMaxInclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMaxInclusiveFacet()
        {
            FacetType = FacetType.MaxInclusive;
        }
    }

    public class XmlSchemaTotalDigitsFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaTotalDigitsFacet()
        {
            FacetType = FacetType.TotalDigits;
        }
    }

    public class XmlSchemaFractionDigitsFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaFractionDigitsFacet()
        {
            FacetType = FacetType.FractionDigits;
        }
    }

    public class XmlSchemaWhiteSpaceFacet : XmlSchemaFacet
    {
        public XmlSchemaWhiteSpaceFacet()
        {
            FacetType = FacetType.Whitespace;
        }
    }
}
