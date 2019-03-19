<xsl:stylesheet version="1.0"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:asn="http://schemas.dot.net/asnxml/201808/"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
  xml:space ="preserve"
>
  <xsl:strip-space elements="*"/>
  <xsl:output method="text" indent="no" />

  <xsl:template match="node()[name()]" priority="-9">
    <xsl:message terminate="yes">Error, unknown node "<xsl:copy-of select="."/>"</xsl:message>
  </xsl:template>

  <xsl:template match="node()[name()]" mode="FieldDef" priority="-9">
    <xsl:message terminate="yes">Error, unknown FieldDef node [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>
  
  <xsl:template match="node()[name()]" mode="CollectionElementType" priority="-9">
    <xsl:message terminate="yes">Error, unknown CollectionElementType node (<xsl:value-of select="local-name()"/>) [<xsl:copy-of select=".."/>]</xsl:message>
  </xsl:template>

  <xsl:template match="node()[name()]" mode="EncodeOptional" priority="-9">
    <xsl:message terminate="yes">Error, unknown EncodeOptional node [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>

  <xsl:template match="node()[name()]" mode="EncodeSimpleValue" priority="-9">
    <xsl:message terminate="yes">Error, unknown EncodeSimpleValue node [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>

  <xsl:template match="node()[name()]" mode="DefaultTag" priority="-9">
    <xsl:message terminate="yes">Error, unknown DefaultTag node [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>

  <xsl:template match="node()[name()]" mode="DecodeSimpleValue" priority="-9">
    <xsl:message terminate="yes">Error, unknown DecodeSimpleValue node [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>

  <xsl:template match="/" xml:space="default">
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="asn:Sequence">// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;<xsl:if test="asn:SequenceOf | asn:SetOf">
using System.Collections.Generic;</xsl:if>
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace <xsl:value-of select="@namespace" />
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct <xsl:value-of select="@name" />
    {<xsl:apply-templates mode="Validate" /><xsl:apply-templates mode="DefaultFieldDef" /><xsl:apply-templates mode="FieldDef" />
      <xsl:if test="*[@defaultDerInit]">
#if DEBUG  
        static <xsl:value-of select="@name" />()
        {
            <xsl:value-of select="@name" /> decoded = default;
            AsnReader reader;<xsl:if test="asn:SequenceOf[@defaultDerInit] | asn:SetOf[@defaultDerInit]">
            AsnReader collectionReader;</xsl:if><xsl:apply-templates mode="DefaultFieldVerify" />
        }
#endif
 </xsl:if>
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            <xsl:apply-templates mode="Encode" />
            writer.PopSequence(tag);
        }

        internal static <xsl:value-of select="@name" /> Decode(ReadOnlyMemory&lt;byte&gt; encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static <xsl:value-of select="@name" /> Decode(Asn1Tag expectedTag, ReadOnlyMemory&lt;byte&gt; encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out <xsl:value-of select="@name" /> decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out <xsl:value-of select="@name" /> decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out <xsl:value-of select="@name" /> decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);<xsl:if test="*[@explicitTag]">
            AsnReader explicitReader;</xsl:if><xsl:if test="*[@defaultDerInit]">
            AsnReader defaultReader;</xsl:if><xsl:if test="asn:SequenceOf | asn:SetOf">
            AsnReader collectionReader;</xsl:if>
            <xsl:apply-templates mode="Decode" />

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
</xsl:template>

    <xsl:template match="asn:Choice">// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;<xsl:if test="asn:SequenceOf | asn:SetOf">
using System.Collections.Generic;</xsl:if>
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace <xsl:value-of select="@namespace" />
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct <xsl:value-of select="@name" />
    {<xsl:apply-templates mode="Validate" /><xsl:apply-templates mode="ValidateChoice" /><xsl:apply-templates mode="FieldDef" />

#if DEBUG
        static <xsl:value-of select="@name" />()
        {
            var usedTags = new System.Collections.Generic.Dictionary&lt;Asn1Tag, string&gt;();
            Action&lt;Asn1Tag, string&gt; ensureUniqueTag = (tag, fieldName) =&gt;
            {
                if (usedTags.TryGetValue(tag, out string existing))
                {
                    throw new InvalidOperationException($"Tag '{tag}' is in use by both '{existing}' and '{fieldName}'");
                }

                usedTags.Add(tag, fieldName);
            };
            <xsl:apply-templates mode="EnsureUniqueTag" />
        }
#endif

        internal void Encode(AsnWriter writer)
        {
            bool wroteValue = false; 
            <xsl:apply-templates mode="EncodeChoice" />
            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static <xsl:value-of select="@name" /> Decode(ReadOnlyMemory&lt;byte&gt; encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, out <xsl:value-of select="@name" /> decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out <xsl:value-of select="@name" /> decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            Asn1Tag tag = reader.PeekTag();<xsl:if test="*[@explicitTag]">
            AsnReader explicitReader;</xsl:if><xsl:if test="asn:SequenceOf | asn:SetOf">
            AsnReader collectionReader;</xsl:if>
            <xsl:apply-templates select="*" mode="Decode" />
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
</xsl:template>

  <xsl:template match="*[@defaultDerInit and @optional]" mode="Validate">
    <xsl:message terminate="yes">Error: defaultDerInit and optional both specified in [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>
  
  <xsl:template match="*[@implicitTag and @explicitTag]" mode="Validate">
    <xsl:message terminate="yes">Error: implicitTag and explicitTag both specified in [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>
    
  <xsl:template match="*[@implicitTag and @universalTagNumber]" mode="Validate">
    <xsl:message terminate="yes">Error: implicitTag and universalTagNumber both specified in [<xsl:copy-of select="."/>]</xsl:message>
  </xsl:template>

  <xsl:template match="*[@defaultDerInit | @optional]" mode="ValidateChoice">
    <xsl:message terminate="yes">Error: neiher optional or defaultDerInit may be specified for fields in a Choice type (<xsl:copy-of select="."/>)</xsl:message>
  </xsl:template>

  <xsl:template match="*[@defaultDerInit]" mode="DefaultFieldDef">
        private static byte[] <xsl:call-template name="DefaultValueField"/> = { <xsl:value-of select="@defaultDerInit"/> };
  </xsl:template>

  <xsl:template match="*[@defaultDerInit]" mode="DefaultFieldVerify">

            reader = new AsnReader(<xsl:call-template name="DefaultValueField"/>, AsnEncodingRules.DER);<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'reader'"/></xsl:apply-templates>
            reader.ThrowIfNotEmpty();</xsl:template>

  <xsl:template match="*" mode="EnsureUniqueTag" xml:space="default">
    <xsl:choose>
      <xsl:when test="@universalTagNumber" xml:space="preserve">
            ensureUniqueTag(new Asn1Tag((UniversalTagNumber)<xsl:value-of select="@universalTagNumber"/>), "<xsl:value-of select="@name"/>");</xsl:when>
      <xsl:otherwise xml:space="preserve">
            ensureUniqueTag(<xsl:call-template name="DefaultOrContextTag" />, "<xsl:value-of select="@name"/>");</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*" mode="Encode" xml:space="default">
    <xsl:choose>
      <xsl:when test="@defaultDerInit and not(@explicitTag)" xml:space="preserve">
        
            // DEFAULT value handler for <xsl:value-of select="@name" />.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {<xsl:apply-templates select="." mode="EncodeValue">
                      <xsl:with-param name="writerName" select="'tmp'" />
                      <xsl:with-param name="indent" select="'        '" />
                    </xsl:apply-templates>
                    ReadOnlySpan&lt;byte&gt; encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(<xsl:call-template name="DefaultValueField"/>))
                    {
                        writer.WriteEncodedValue(encoded.ToArray());
                    }
                }
            }
</xsl:when>
      <xsl:when test="@optional">
        <xsl:apply-templates select="." mode="EncodeOptional" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="." mode="EncodeValue" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <!-- Struct OPTIONAL -->
  <xsl:template match="asn:AsnType | asn:AnyValue | asn:Boolean | asn:Integer | asn:BitString | asn:NamedBitList | asn:OctetString | asn:Enumerated | asn:UtcTime | asn:GeneralizedTime" mode="EncodeOptional">

            if (<xsl:value-of select="@name"/>.HasValue)
            {<xsl:apply-templates select="." mode="EncodeValue"><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
            }
</xsl:template>

  <!-- Class OPTIONAL -->
  <xsl:template match="asn:ObjectIdentifier | asn:UTF8String | asn:SequenceOf | asn:SetOf | asn:PrintableString | asn:T61String | asn:IA5String | asn:VisibleString | asn:BMPString" mode="EncodeOptional">

            if (<xsl:value-of select="@name"/> != null)
            {<xsl:apply-templates select="." mode="EncodeValue"><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
            }
</xsl:template>

  <!-- Struct CHOICE -->
  <xsl:template match="asn:AsnType | asn:AnyValue | asn:Boolean | asn:Integer | asn:BitString | asn:NamedBitList | asn:OctetString | asn:Enumerated | asn:UtcTime | asn:GeneralizedTime" mode="EncodeChoice">
            if (<xsl:value-of select="@name"/>.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();
                <xsl:apply-templates select="." mode="EncodeValue"><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
                wroteValue = true;
            }
</xsl:template>

  <!-- Class CHOICE -->
  <xsl:template match="asn:ObjectIdentifier | asn:UTF8String | asn:SequenceOf | asn:SetOf | asn:PrintableString | asn:T61String | asn:IA5String | asn:VisibleString | asn:BMPString" mode="EncodeChoice">
            if (<xsl:value-of select="@name"/> != null)
            {
                if (wroteValue)
                    throw new CryptographicException();
                <xsl:apply-templates select="." mode="EncodeValue"><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
                wroteValue = true;
            }
</xsl:template>

  <xsl:template match="*" mode="EncodeValue" xml:space="default">
    <xsl:param name="writerName" select="'writer'" />
    <xsl:param name="indent" />
    <xsl:choose>
      <xsl:when test="@defaultDerInit and @explicitTag" xml:space="preserve">

            // DEFAULT value handler for <xsl:value-of select="@name" />.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {<xsl:apply-templates select="." mode="EncodeSimpleValue">
                      <xsl:with-param name="writerName" select="'tmp'" />
                      <xsl:with-param name="indent" select="'        '" />
                    </xsl:apply-templates>
                    ReadOnlySpan&lt;byte&gt; encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(<xsl:call-template name="DefaultValueField"/>))
                    {
                        writer.PushSequence(<xsl:call-template name="ContextTag" />);
                        writer.WriteEncodedValue(encoded.ToArray());
                        writer.PopSequence(<xsl:call-template name="ContextTag" />);
                    }
                }
            }
</xsl:when>
      <xsl:when test="@explicitTag" xml:space="preserve">
            <xsl:value-of select="$indent"/>writer.PushSequence(<xsl:call-template name="ContextTag" />);<xsl:apply-templates select="." mode="EncodeSimpleValue"><xsl:with-param name="writerName" select="$writerName"/><xsl:with-param name="indent" select="$indent"/></xsl:apply-templates>
            <xsl:value-of select="$indent"/>writer.PopSequence(<xsl:call-template name="ContextTag" />);</xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="." mode="EncodeSimpleValue"><xsl:with-param name="writerName" select="$writerName"/><xsl:with-param name="indent" select="$indent" /></xsl:apply-templates>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template match="*" mode="Decode" xml:space="default">
    <xsl:choose>
      <xsl:when test="parent::asn:Choice">
        <xsl:choose>
          <xsl:when test="@explicitTag" xml:space="preserve">
            <xsl:if test="position() != 1">else </xsl:if>if (tag.HasSameClassAndValue(<xsl:call-template name="ContextTag" />))
            {
                explicitReader = reader.ReadSequence(<xsl:call-template name="ContextTag"/>);<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'explicitReader'"/><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
                explicitReader.ThrowIfNotEmpty();
            }</xsl:when>
          <xsl:otherwise xml:space="preserve">
            <xsl:if test="position() != 1">else </xsl:if>if (tag.HasSameClassAndValue(<xsl:call-template name="DefaultOrContextTag" />))
            {<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'reader'"/><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
            }</xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="@optional or @defaultDerInit">
        <xsl:choose>
          <xsl:when test="@explicitTag" xml:space="preserve">

            if (sequenceReader.HasData &amp;&amp; sequenceReader.PeekTag().HasSameClassAndValue(<xsl:call-template name="ContextTag" />))
            {
                explicitReader = sequenceReader.ReadSequence(<xsl:call-template name="ContextTag" />);<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'explicitReader'"/><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
                explicitReader.ThrowIfNotEmpty();
            }<xsl:call-template name="DefaultValueDecoder"/>
</xsl:when>
          <xsl:when test="@implicitTag" xml:space="preserve">

            if (sequenceReader.HasData &amp;&amp; sequenceReader.PeekTag().HasSameClassAndValue(<xsl:call-template name="ContextTag" />))
            {<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'sequenceReader'" /><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
            }<xsl:call-template name="DefaultValueDecoder"/>
</xsl:when>
          <xsl:when test="self::asn:AnyValue" xml:space="preserve">

            if (sequenceReader.HasData)
            {<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'sequenceReader'"/><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
            }<xsl:call-template name="DefaultValueDecoder"/>
</xsl:when>
          <xsl:otherwise xml:space="preserve">

            if (sequenceReader.HasData &amp;&amp; sequenceReader.PeekTag().HasSameClassAndValue(<xsl:apply-templates select="." mode="DefaultTag" />))
            {<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'sequenceReader'" /><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
            }<xsl:call-template name="DefaultValueDecoder"/>
</xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="@explicitTag" xml:space="preserve">

            explicitReader = sequenceReader.ReadSequence(<xsl:call-template name="ContextTag" />);<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'explicitReader'"/></xsl:apply-templates>
            explicitReader.ThrowIfNotEmpty();
</xsl:when>
      <xsl:when test="@implicitTag">
        <xsl:apply-templates select="." mode="DecodeSimpleValue">
          <xsl:with-param name="readerName" select="'sequenceReader'" />
        </xsl:apply-templates>
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="." mode="DecodeSimpleValue">
          <xsl:with-param name="readerName" select="'sequenceReader'" />
        </xsl:apply-templates>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="asn:AsnType" mode="FieldDef">
        internal <xsl:value-of select="@typeName"/><xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name" />;</xsl:template>

  <xsl:template match="asn:AsnType" mode="CollectionElementType"><xsl:value-of select="@typeName"/></xsl:template>

  <xsl:template match="asn:AsnType" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>.Encode(<xsl:value-of select="$writerName"/><xsl:call-template name="MaybeImplicitCallS"/>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:AsnType" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:choose>
      <xsl:when test="@optional | parent::asn:Choice" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="@typeName"/> tmp<xsl:value-of select="@name"/>;
            <xsl:value-of select="$indent"/><xsl:value-of select="@typeName"/>.Decode(<xsl:value-of select="$readerName"/>, <xsl:call-template name="MaybeImplicitCallP"/>out tmp<xsl:value-of select="@name"/>);
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = tmp<xsl:value-of select="@name"/>;
</xsl:when>
      <xsl:otherwise xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="@typeName"/>.Decode(<xsl:value-of select="$readerName"/>, <xsl:call-template name="MaybeImplicitCallP"/>out <xsl:value-of select="$name"/>);</xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template match="asn:AsnType" mode="DefaultTag">Asn1Tag.Sequence</xsl:template>

  <xsl:template match="asn:AnyValue" mode="FieldDef">
        internal ReadOnlyMemory&lt;byte&gt;<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name" />;</xsl:template>

  <xsl:template match="asn:AnyValue" mode="CollectionElementType">ReadOnlyMemory&lt;byte&gt;</xsl:template>

  <xsl:template match="asn:AnyValue" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="@implicitTag | @universalTagNumber" xml:space="preserve">
            <xsl:value-of select="$indent"/>// Validator for tag constraint for <xsl:value-of select="@name"/>
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    if (!Asn1Tag.TryDecode(<xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>.Span, out Asn1Tag validateTag, out _) ||
            <xsl:value-of select="$indent"/>        !validateTag.HasSameClassAndValue(<xsl:call-template name="DefaultOrContextTag" />))
            <xsl:value-of select="$indent"/>    {
            <xsl:value-of select="$indent"/>        throw new CryptographicException();
            <xsl:value-of select="$indent"/>    }
            <xsl:value-of select="$indent"/>}
</xsl:if>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteEncodedValue(<xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>.Span);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:AnyValue" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:choose>
        <xsl:when test="@optional | parent::asn:Choice" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadEncodedValue();</xsl:when>
        <xsl:when test="@implicitTag | @universalTagNumber" xml:space="preserve">
            <xsl:value-of select="$indent"/>if (!<xsl:value-of select="$readerName"/>.PeekTag().HasSameClassAndValue(<xsl:call-template name="DefaultOrContextTag"/>))
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    throw new CryptographicException();
            <xsl:value-of select="$indent"/>}

            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadEncodedValue();</xsl:when>
        <xsl:otherwise xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadEncodedValue();</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="asn:AnyValue" mode="DefaultTag">new Asn1Tag(<xsl:call-template name="DefaultValueField"/>[0])</xsl:template>

  <xsl:template match="asn:AnyValue[@universalTagNumber]" mode="DefaultTag">new Asn1Tag((UniversalTagNumber)<xsl:value-of select="@universalTagNumber"/>)</xsl:template>

  <xsl:template match="asn:Boolean" mode="FieldDef">
        internal bool<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:Boolean" mode="CollectionElementType">bool</xsl:template>

  <xsl:template match="asn:Boolean" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteBoolean(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:Boolean" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadBoolean(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:Boolean" mode="DefaultTag">Asn1Tag.Boolean</xsl:template>

  <xsl:template match="asn:Integer[not(@backingType)] | asn:Integer[@backingType = 'BigInteger']" mode="FieldDef">
        internal System.Numerics.BigInteger<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name" />;</xsl:template>
  
  <xsl:template match="asn:Integer[@backingType = 'ReadOnlyMemory']" mode="FieldDef">
        internal ReadOnlyMemory&lt;byte&gt;<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name" />;</xsl:template>

  <xsl:template match="asn:Integer[@backingType = 'byte']" mode="FieldDef">
        internal byte<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name" />;</xsl:template>

  <xsl:template match="asn:Integer[@backingType = 'int']" mode="FieldDef">
        internal int<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name" />;</xsl:template>

  <xsl:template match="asn:Integer[not(@backingType)] | asn:Integer[@backingType = 'BigInteger']" mode="CollectionElementType">System.Numerics.BigInteger</xsl:template>
  <xsl:template match="asn:Integer[@backingType = 'ReadOnlyMemory']" mode="CollectionElementType">ReadOnlyMemory&lt;byte&gt;</xsl:template>
  <xsl:template match="asn:Integer[@backingType = 'byte']" mode="CollectionElementType">byte</xsl:template>
  <xsl:template match="asn:Integer[@backingType = 'int']" mode="CollectionElementType">int</xsl:template>

  <xsl:template match="asn:Integer[not(@backingType = 'ReadOnlyMemory')]" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteInteger(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:Integer[@backingType = 'ReadOnlyMemory']" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteInteger(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>.Span);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:Integer[not(@backingType)] | asn:Integer[@backingType = 'BigInteger']" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadInteger(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:Integer[@backingType = 'ReadOnlyMemory']" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadIntegerBytes(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:Integer[@backingType = 'byte']" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:choose>
      <xsl:when test="@optional | parent::asn:Choice" xml:space="preserve">

            <xsl:value-of select="$indent"/>if (<xsl:value-of select="$readerName"/>.TryReadUInt8(<xsl:call-template name="MaybeImplicitCallP"/>out byte tmp<xsl:value-of select="@name"/>))
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$name"/> = tmp<xsl:value-of select="@name"/>;
            <xsl:value-of select="$indent"/>}
            <xsl:value-of select="$indent"/>else
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$readerName"/>.ThrowIfNotEmpty();
            <xsl:value-of select="$indent"/>}
</xsl:when>
      <xsl:otherwise xml:space="preserve">

            <xsl:value-of select="$indent"/>if (!<xsl:value-of select="$readerName"/>.TryReadUInt8(<xsl:call-template name="MaybeImplicitCallP"/>out <xsl:value-of select="$name"/>))
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$readerName"/>.ThrowIfNotEmpty();
            <xsl:value-of select="$indent"/>}
</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="asn:Integer[@backingType = 'int']" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:choose>
      <xsl:when test="@optional | parent::asn:Choice" xml:space="preserve">

            <xsl:value-of select="$indent"/>if (<xsl:value-of select="$readerName"/>.TryReadInt32(<xsl:call-template name="MaybeImplicitCallP"/>out int tmp<xsl:value-of select="@name"/>))
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$name"/> = tmp<xsl:value-of select="@name"/>;
            <xsl:value-of select="$indent"/>}
            <xsl:value-of select="$indent"/>else
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$readerName"/>.ThrowIfNotEmpty();
            <xsl:value-of select="$indent"/>}
</xsl:when>
      <xsl:otherwise xml:space="preserve">

            <xsl:value-of select="$indent"/>if (!<xsl:value-of select="$readerName"/>.TryReadInt32(<xsl:call-template name="MaybeImplicitCallP"/>out <xsl:value-of select="$name"/>))
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$readerName"/>.ThrowIfNotEmpty();
            <xsl:value-of select="$indent"/>}
</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="asn:Integer" mode="DefaultTag">Asn1Tag.Integer</xsl:template>

  <xsl:template match="asn:BitString" mode="FieldDef">
        internal ReadOnlyMemory&lt;byte&gt;<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:BitString" mode="CollectionElementType">ReadOnlyMemory&lt;byte&gt;</xsl:template>

  <xsl:template match="asn:BitString" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteBitString(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>.Span);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:BitString" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">

            <xsl:value-of select="$indent"/>if (<xsl:value-of select="$readerName"/>.TryReadPrimitiveBitStringValue(<xsl:call-template name="MaybeImplicitCallP"/>out _, out ReadOnlyMemory&lt;byte&gt; tmp<xsl:value-of select="@name"/>))
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$name"/> = tmp<xsl:value-of select="@name"/>;
            <xsl:value-of select="$indent"/>}
            <xsl:value-of select="$indent"/>else
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadBitString(<xsl:call-template name="MaybeImplicitCallP"/>out _);
            <xsl:value-of select="$indent"/>}
</xsl:if>
  </xsl:template>

  <xsl:template match="asn:BitString" mode="DefaultTag">Asn1Tag.PrimitiveBitString</xsl:template>

  <xsl:template match="asn:NamedBitList" mode="FieldDef">
        internal <xsl:value-of select="@backingType"/><xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:NamedBitList" mode="CollectionElementType"><xsl:value-of select="@backingType"/></xsl:template>

  <xsl:template match="asn:NamedBitList" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteNamedBitList(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:NamedBitList" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadNamedBitListValue&lt;<xsl:value-of select="@backingType"/>&gt;(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:NamedBitList" mode="DefaultTag">Asn1Tag.PrimitiveBitString</xsl:template>

  <xsl:template match="asn:OctetString" mode="FieldDef">
        internal ReadOnlyMemory&lt;byte&gt;<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:OctetString" mode="CollectionElementType">ReadOnlyMemory&lt;byte&gt;</xsl:template>

  <xsl:template match="asn:OctetString" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteOctetString(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>.Span);</xsl:if>
  </xsl:template>

  <xsl:template match="asn:OctetString" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">

            <xsl:value-of select="$indent"/>if (<xsl:value-of select="$readerName"/>.TryReadPrimitiveOctetStringBytes(<xsl:call-template name="MaybeImplicitCallP"/>out ReadOnlyMemory&lt;byte&gt; tmp<xsl:value-of select="@name"/>))
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$name"/> = tmp<xsl:value-of select="@name"/>;
            <xsl:value-of select="$indent"/>}
            <xsl:value-of select="$indent"/>else
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    <xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadOctetString(<xsl:call-template name="MaybeImplicitCall0"/>);
            <xsl:value-of select="$indent"/>}
</xsl:if>
  </xsl:template>

  <xsl:template match="asn:OctetString" mode="DefaultTag">Asn1Tag.PrimitiveOctetString</xsl:template>

  <xsl:template match="asn:ObjectIdentifier[not(@backingType)] | asn:ObjectIdentifier[@backingType = 'Oid']" mode="FieldDef">
        internal Oid <xsl:value-of select="@name" />;</xsl:template>
  <xsl:template match="asn:ObjectIdentifier[@backingType = 'string']" mode="FieldDef">
        internal string <xsl:value-of select="@name" />;</xsl:template>

  <xsl:template match="asn:ObjectIdentifier[not(@backingType)] | asn:ObjectIdentifier[@backingType = 'Oid']" mode="CollectionElementType">Oid</xsl:template>
  <xsl:template match="asn:ObjectIdentifier[@backingType = 'string']" mode="CollectionElementType">string</xsl:template>

  <xsl:template match="asn:ObjectIdentifier" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:choose>
      <!-- For when string and unpopulated friendly name are added -->
      <xsl:when test="0"></xsl:when>
      <xsl:otherwise xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteObjectIdentifier(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/>);</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="asn:ObjectIdentifier[not(@backingType)] | asn:ObjectIdentifier[@backingType = 'Oid']" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:choose>
      <!-- For when string and unpopulated friendly name are added -->
      <xsl:when test="0"></xsl:when>
      <xsl:otherwise xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadObjectIdentifier(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="asn:ObjectIdentifier[@backingType = 'string']" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:choose>
      <!-- For when string and unpopulated friendly name are added -->
      <xsl:when test="0"></xsl:when>
      <xsl:otherwise xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadObjectIdentifierAsString(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="asn:ObjectIdentifier" mode="DefaultTag">Asn1Tag.ObjectIdentifier</xsl:template>

  <xsl:template match="asn:Enumerated" mode="FieldDef">
        internal <xsl:value-of select="@backingType"/><xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:Enumerated" mode="CollectionElementType"><xsl:value-of select="@backingType"/></xsl:template>

  <xsl:template match="asn:Enumerated" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteEnumeratedValue(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:Enumerated" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadEnumeratedValue&lt;<xsl:value-of select="@backingType"/>&gt;(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:Enumerated" mode="DefaultTag">Asn1Tag.Enumerated</xsl:template>

  <!-- All character string types -->
  <xsl:template match="asn:UTF8String | asn:PrintableString | asn:T61String | asn:IA5String | asn:VisibleString | asn:BMPString" mode="FieldDef">
        internal string <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:UTF8String | asn:PrintableString | asn:T61String | asn:IA5String | asn:VisibleString | asn:BMPString" mode="CollectionElementType">string</xsl:template>

  <xsl:template match="asn:UTF8String | asn:PrintableString | asn:T61String | asn:IA5String | asn:VisibleString | asn:BMPString" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteCharacterString(<xsl:call-template name="MaybeImplicitCallP"/>UniversalTagNumber.<xsl:value-of select="local-name()"/>, <xsl:value-of select="$name"/>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:UTF8String | asn:PrintableString | asn:T61String | asn:IA5String | asn:VisibleString | asn:BMPString" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadCharacterString(<xsl:call-template name="MaybeImplicitCallP"/>UniversalTagNumber.<xsl:value-of select="local-name()"/>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:UTF8String | asn:PrintableString | asn:T61String | asn:IA5String | asn:VisibleString | asn:BMPString" mode="DefaultTag">new Asn1Tag(UniversalTagNumber.<xsl:value-of select="local-name()"/>)</xsl:template>

  <xsl:template match="asn:SequenceOf | asn:SetOf" mode="FieldDef">
        internal <xsl:apply-templates mode="CollectionElementType"/>[] <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:SequenceOf | asn:SetOf" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:variable name="collNoun">
      <xsl:choose>
        <xsl:when test="self::asn:SetOf">SetOf</xsl:when>
        <xsl:otherwise>Sequence</xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:if test="1" xml:space="preserve">

            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.Push<xsl:value-of select="$collNoun"/>(<xsl:call-template name="MaybeImplicitCall0"/>);
            <xsl:value-of select="$indent"/>for (int i = 0; i &lt; <xsl:value-of select="@name"/>.Length; i++)
            <xsl:value-of select="$indent"/>{<xsl:apply-templates mode="EncodeSimpleValue"><xsl:with-param name="name" select="concat(@name, '[i]')"/><xsl:with-param name="writerName" select="$writerName"/><xsl:with-param name="indent" select="concat('    ', $indent)"/></xsl:apply-templates> 
            <xsl:value-of select="$indent"/>}
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.Pop<xsl:value-of select="$collNoun"/>(<xsl:call-template name="MaybeImplicitCall0"/>);
</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:SequenceOf | asn:SetOf" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:variable name="collNoun">
      <xsl:choose>
        <xsl:when test="self::asn:SetOf">SetOf</xsl:when>
        <xsl:otherwise>Sequence</xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:if test="1" xml:space="preserve">

            <xsl:value-of select="$indent"/>// Decode SEQUENCE OF for <xsl:value-of select="@name"/>
            <xsl:value-of select="$indent"/>{
            <xsl:value-of select="$indent"/>    collectionReader = <xsl:value-of select="$readerName"/>.Read<xsl:value-of select="$collNoun"/>(<xsl:call-template name="MaybeImplicitCall0"/>);
            <xsl:value-of select="$indent"/>    var tmpList = new List&lt;<xsl:apply-templates mode="CollectionElementType"/>&gt;();
            <xsl:value-of select="$indent"/>    <xsl:apply-templates mode="CollectionElementType"/> tmpItem;

            <xsl:value-of select="$indent"/>    while (collectionReader.HasData)
            <xsl:value-of select="$indent"/>    {<xsl:apply-templates mode="DecodeSimpleValue"><xsl:with-param name="name" select="'tmpItem'"/><xsl:with-param name="readerName" select="'collectionReader'"/><xsl:with-param name="indent" select="concat('        ', $indent)"/></xsl:apply-templates> 
            <xsl:value-of select="$indent"/>        tmpList.Add(tmpItem);
            <xsl:value-of select="$indent"/>    }

            <xsl:value-of select="$indent"/>    decoded.<xsl:value-of select="@name"/> = tmpList.ToArray();
            <xsl:value-of select="$indent"/>}
</xsl:if>
  </xsl:template>

  <xsl:template match="asn:SequenceOf" mode="DefaultTag">Asn1Tag.Sequence</xsl:template>

  <xsl:template match="asn:SetOf" mode="DefaultTag">Asn1Tag.SetOf</xsl:template>

  <xsl:template match="asn:UtcTime | asn:GeneralizedTime" mode="FieldDef">
        internal DateTimeOffset<xsl:if test="@optional | parent::asn:Choice">?</xsl:if> <xsl:value-of select="@name"/>;</xsl:template>

  <xsl:template match="asn:UtcTime | asn:GeneralizedTime" mode="CollectionElementType">DateTimeOffset</xsl:template>

  <xsl:template match="asn:UtcTime" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteUtcTime(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:UtcTime" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:choose>
      <xsl:when test="@twoDigitYearMax" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadUtcTime(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="@twoDigitYearMax"/>);</xsl:when>
      <xsl:otherwise xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadUtcTime(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template match="asn:UtcTime" mode="DefaultTag">Asn1Tag.UtcTime</xsl:template>
  
  <xsl:template match="asn:GeneralizedTime" mode="EncodeSimpleValue" xml:space="default">
    <xsl:param name="writerName"/>
    <xsl:param name="indent" />
    <xsl:param name="name" select="@name"/>
    <xsl:variable name="nullable" select="@optional | parent::asn:Choice"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$writerName"/>.WriteGeneralizedTime(<xsl:call-template name="MaybeImplicitCallP"/><xsl:value-of select="$name"/><xsl:if test="$nullable">.Value</xsl:if>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:GeneralizedTime" mode="DecodeSimpleValue" xml:space="default">
    <xsl:param name="readerName" />
    <xsl:param name="indent" />
    <xsl:param name="name" select="concat('decoded.', @name)"/>
    <xsl:if test="1" xml:space="preserve">
            <xsl:value-of select="$indent"/><xsl:value-of select="$name"/> = <xsl:value-of select="$readerName"/>.ReadGeneralizedTime(<xsl:call-template name="MaybeImplicitCall0"/>);</xsl:if>
  </xsl:template>
  
  <xsl:template match="asn:GeneralizedTime" mode="DefaultTag">Asn1Tag.GeneralizedTime</xsl:template>

  <!-- ReadBoolean() vs ReadBoolean(tag) -->
  <xsl:template name="MaybeImplicitCall0"><xsl:if test="@implicitTag"><xsl:call-template name="ContextTag"/></xsl:if></xsl:template>

  <!-- TryReadInt32(out val) vs TryReadInt32(tag, out val) -->
  <xsl:template name="MaybeImplicitCallP"><xsl:if test="@implicitTag"><xsl:call-template name="ContextTag"/>, </xsl:if></xsl:template>

  <!-- Encode(writer) vs Encode(writer, tag) -->
  <xsl:template name="MaybeImplicitCallS"><xsl:if test="@implicitTag">, <xsl:call-template name="ContextTag"/></xsl:if></xsl:template>

  <xsl:template name="ContextTag">new Asn1Tag(TagClass.ContextSpecific, <xsl:value-of select="@implicitTag | @explicitTag"/>)</xsl:template>
  
  <xsl:template name="DefaultValueField">s_default<xsl:value-of select="@name"/></xsl:template>

  <xsl:template name="DefaultValueDecoder"><xsl:if test="@defaultDerInit">
            else
            {
                defaultReader = new AsnReader(<xsl:call-template name="DefaultValueField"/>, AsnEncodingRules.DER);<xsl:apply-templates select="." mode="DecodeSimpleValue"><xsl:with-param name="readerName" select="'defaultReader'"/><xsl:with-param name="indent" select="'    '"/></xsl:apply-templates>
            }</xsl:if></xsl:template>

  <xsl:template name="DefaultOrContextTag" xml:space="default">
      <xsl:choose>
          <xsl:when test="@implicitTag | @explicitTag"><xsl:call-template name="ContextTag"/></xsl:when>
          <xsl:otherwise><xsl:apply-templates select="." mode="DefaultTag"/></xsl:otherwise>
      </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
