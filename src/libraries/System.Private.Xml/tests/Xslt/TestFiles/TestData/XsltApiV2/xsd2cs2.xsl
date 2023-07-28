<stylesheet version="1.0"
	xmlns="http://www.w3.org/1999/XSL/Transform"
	xmlns:xsd="http://www.w3.org/2001/XMLSchema"
	xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	xmlns:ext="urn:my_extensions"
	xmlns:extCS="urn:my_extensionsCS">
	<param name="sourceUri"/>

    <output method="text" encoding="UTF-8"/>

	<template match="/">
        using System;
        using System.Collections;
        using System.IO;
        using System.Xml;
        using System.Xml.Xsl;
        using System.Xml.Schema;
		using OLEDB.Test.ModuleCore;

        public class Sample {
			static int _ValidationErrorsCount = 0;
			static int _ValidationWarningsCount = 0;
			public static void ValidationCallbackOne(object sender, ValidationEventArgs args )
			{
				string msg = String.Format("test.xsd({0},{1}) {2}:{3}\r\n",args.Exception.LineNumber, args.Exception.LinePosition, args.Severity, args.Message);
				CError.WriteIgnore(msg);
				if(args.Severity == XmlSeverityType.Warning)
				{
					_ValidationWarningsCount++;
				}
				else
				{
					_ValidationErrorsCount++;
				}
			}
            public static int Main()
			{
				XmlUrlResolver ur = new XmlUrlResolver();

                <apply-templates/>

				return (_ValidationErrorsCount &lt;&lt; 16 | _ValidationWarningsCount);
            }
            private static XmlDocument doc = new XmlDocument();
            private static XmlNode[] GetChildren(XmlNode node) {
                XmlNodeList list = node.ChildNodes;
                if (list.Count == 0) return null;
                XmlNode[] array = new XmlNode[list.Count];
                for (int i = 0; i != list.Count; i ++) {
                    array[i] = list[i];
                }
                return array;
            }
        };
    </template>

	<template match="xsd:schema">
        XmlSchema schema = new XmlSchema();
        <call-template name="NamespaceDeclarations"><with-param name="varName" select="'schema'"/></call-template>
        <call-template name="UnhandledAttributes"><with-param name="varName" select="'schema'"/></call-template>
        <if test="@attributeFormDefault">
            schema.AttributeFormDefault = <call-template name="XmlSchemaForm"><with-param name="s" select="@attributeFormDefault"/></call-template>;
        </if>
        <if test="@blockDefault">
            schema.BlockDefault = <call-template name="XmlSchemaDerivationMethod"><with-param name="s" select="@blockDefault"/></call-template>;
        </if>
        <if test="@finalDefault">
            schema.FinalDefault = <call-template name="XmlSchemaDerivationMethod"><with-param name="s" select="@finalDefault"/></call-template>;
        </if>
        <if test="@elementFormDefault">
            schema.ElementFormDefault = <call-template name="XmlSchemaForm"><with-param name="s" select="@elementFormDefault"/></call-template>;
        </if>
        <if test="@targetNamespace">
            schema.TargetNamespace = "<value-of select="@targetNamespace"/>";
        </if>
        <if test="@id">
            schema.Id = "<value-of select="@id"/>";
        </if>
        <if test="@version">
            schema.Version = "<value-of select="@version"/>";
        </if>
        <for-each select="xsd:include|xsd:import|xsd:redefine">
            {
                <apply-templates select="."><with-param name="varName" select="local-name()"/></apply-templates>
                schema.Includes.Add(<value-of select="local-name()"/>);
            }
        </for-each>
        <for-each select="xsd:annotation|xsd:attribute|xsd:attributeGroup|xsd:complexType|xsd:simpleType|xsd:element|xsd:group|xsd:notation">
            {
				<apply-templates select="."><with-param name="varName" select="local-name()"/></apply-templates>
                schema.Items.Add(<value-of select="local-name()"/>);
            }
        </for-each>
		XmlTextWriter xmlWriter=null;
		try
		{
			xmlWriter = new XmlTextWriter("test.xsd", null);
			xmlWriter.Formatting = Formatting.Indented;

			schema.Write(xmlWriter);
			xmlWriter.Close();
			schema.Compile(new ValidationEventHandler(ValidationCallbackOne));
		}
		catch(Exception)
		{
			if (xmlWriter != null)
				xmlWriter.Close();
		}
	</template>

	<template match="xsd:include">
        <param name="varName"/>
        XmlSchemaInclude <value-of select="$varName"/> = new XmlSchemaInclude();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@schemaLocation">
			<value-of select="$varName"/>.SchemaLocation = ur.ResolveUri(new Uri("<value-of select="$sourceUri"/>"), "<value-of select="@schemaLocation"/>").ToString();
        </if>

	</template>

	<template match="xsd:import">
        <param name="varName"/>
        XmlSchemaImport <value-of select="$varName"/> = new XmlSchemaImport();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@schemaLocation">
			<value-of select="$varName"/>.SchemaLocation = ur.ResolveUri(new Uri("<value-of select="$sourceUri"/>"), "<value-of select="@schemaLocation"/>").ToString();
        </if>
        <if test="@namespace">
            <value-of select="$varName"/>.Namespace = "<value-of select="@namespace"/>";
        </if>
	</template>

	<template match="xsd:redefine">
        <param name="varName"/>
        XmlSchemaRedefine <value-of select="$varName"/> = new XmlSchemaRedefine();
        <if test="@id">
            <value-of select="$varName"/>.Id = "<value-of select="@id"/>";
        </if>
        <if test="@schemaLocation">
			<value-of select="$varName"/>.SchemaLocation = ur.ResolveUri(new Uri("<value-of select="$sourceUri"/>"), "<value-of select="@schemaLocation"/>").ToString();
        </if>
        <for-each select="xsd:annotation|xsd:attributeGroup|xsd:complexType|xsd:simpleType|xsd:group">
            {
	            <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Items.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template match="xsd:annotation">
		<param name="varName"/>
        XmlSchemaAnnotation <value-of select="$varName"/> = new XmlSchemaAnnotation();
        <if test="@id">
            <value-of select="$varName"/>.Id = "<value-of select="@id"/>";
        </if>
        <for-each select="xsd:documentation|xsd:appinfo">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Items.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template match="xsd:attribute">
        <param name="varName"/>
        XmlSchemaAttribute <value-of select="$varName"/> = new XmlSchemaAttribute();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@form">
            <value-of select="$varName"/>.Form = <call-template name="XmlSchemaForm"><with-param name="s" select="@form"/></call-template>;
        </if>
        <if test="@name">
            <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
        </if>
        <if test="@ref">
            <value-of select="$varName"/>.RefName = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@ref"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
        <if test="@type">
            <value-of select="$varName"/>.SchemaTypeName = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@type"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
        <if test="xsd:simpleType">
            <apply-templates select="xsd:simpleType"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
            <value-of select="$varName"/>.SchemaType = <value-of select="$varName"/>_<value-of select="local-name()"/>;
        </if>
        <if test="@fixed">
            <value-of select="$varName"/>.FixedValue = "<value-of select="ext:format(string(@fixed))"/>";
        </if>
        <if test="@default">
            <value-of select="$varName"/>.DefaultValue = "<value-of select="@default"/>";
        </if>
        <if test="@use">
            <value-of select="$varName"/>.Use = <call-template name="XmlSchemaUse"><with-param name="s" select="@use"/></call-template>;
        </if>
	</template>

	<template match="xsd:attributeGroup">
        <param name="varName"/>
        <choose>
            <when test="local-name(..)='schema' or local-name(..)='redefine'">
                XmlSchemaAttributeGroup <value-of select="$varName"/> = new XmlSchemaAttributeGroup();
                <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
                <if test="@name">
                    <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
                </if>
				<call-template name="Attributes"><with-param name="varName" select="$varName"/></call-template>
            </when>
            <otherwise>
                XmlSchemaAttributeGroupRef <value-of select="$varName"/> = new XmlSchemaAttributeGroupRef();
                <call-template name="XmlSchemaParticle"><with-param name="varName" select="$varName"/></call-template>
				<if test="@ref">
					<value-of select="$varName"/>.RefName = <call-template name="XmlQualifiedName">
							<with-param name="s" select="@ref"/>
							<with-param name="namespaces" select="namespace::*"/>
						</call-template>;
				</if>
            </otherwise>
        </choose>
	</template>

	<template match="xsd:complexType">
        <param name="varName"/>
        XmlSchemaComplexType <value-of select="$varName"/> = new XmlSchemaComplexType();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@abstract">
            <value-of select="$varName"/>.IsAbstract = XmlConvert.ToBoolean("<value-of select="@abstract"/>");
        </if>
        <if test="@block">
            <value-of select="$varName"/>.Block = <call-template name="XmlSchemaDerivationMethod"><with-param name="s" select="@block"/></call-template>;
        </if>
        <if test="@final">
            <value-of select="$varName"/>.Final = <call-template name="XmlSchemaDerivationMethod"><with-param name="s" select="@final"/></call-template>;
        </if>
        <if test="@mixed">
            <value-of select="$varName"/>.IsMixed = XmlConvert.ToBoolean("<value-of select="@mixed"/>");
        </if>
        <if test="@name">
            <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
        </if>
        <variable name="content" select="xsd:simpleContent|xsd:complexContent"/>
        <if test="$content">
            <apply-templates select="$content"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name($content)"/></with-param></apply-templates>
            <value-of select="$varName"/>.ContentModel = <value-of select="$varName"/>_<value-of select="local-name($content)"/>;
        </if>
        <call-template name="BaseComplexContent"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:simpleType">
        <param name="varName"/>
        XmlSchemaSimpleType <value-of select="$varName"/> = new XmlSchemaSimpleType();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@name">
            <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
        </if>
        <if test="@final">
            <value-of select="$varName"/>.Final = <call-template name="XmlSchemaDerivationMethod"><with-param name="s" select="@final"/></call-template>;
        </if>
        <for-each select="xsd:restriction|xsd:list|xsd:union">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Content = <value-of select="$varName"/>_<value-of select="local-name()"/>;
            }
        </for-each>
	</template>

	<template match="xsd:element">
        <param name="varName"/>
        XmlSchemaElement <value-of select="$varName"/> = new XmlSchemaElement();
        <call-template name="XmlSchemaParticle"><with-param name="varName" select="$varName"/></call-template>
        <if test="@abstract">
            <value-of select="$varName"/>.IsAbstract = XmlConvert.ToBoolean("<value-of select="@abstract"/>");
        </if>
        <if test="@block">
            <value-of select="$varName"/>.Block = <call-template name="XmlSchemaDerivationMethod"><with-param name="s" select="@block"/></call-template>;
        </if>
        <if test="@default">
            <value-of select="$varName"/>.DefaultValue = "<value-of select="@default"/>";
        </if>
        <if test="@final">
            <value-of select="$varName"/>.Final = <call-template name="XmlSchemaDerivationMethod"><with-param name="s" select="@final"/></call-template>;
        </if>
        <if test="@fixed">
            <value-of select="$varName"/>.FixedValue = "<value-of select="@fixed"/>";
        </if>
        <if test="@form">
            <value-of select="$varName"/>.Form = <call-template name="XmlSchemaForm"><with-param name="s" select="@form"/></call-template>;
        </if>
        <if test="@name">
            <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
        </if>
        <if test="@nillable">
            <value-of select="$varName"/>.IsNillable = XmlConvert.ToBoolean("<value-of select="@nillable"/>");
        </if>
        <if test="@ref">
            <value-of select="$varName"/>.RefName = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@ref"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
        <if test="@substitutionGroup">
            <value-of select="$varName"/>.SubstitutionGroup = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@substitutionGroup"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
        <if test="@fixed">
            <value-of select="$varName"/>.FixedValue = "<value-of select="@fixed"/>";
        </if>
        <if test="@default">
            <value-of select="$varName"/>.DefaultValue = "<value-of select="@default"/>";
        </if>
        <if test="@type">
            <value-of select="$varName"/>.SchemaTypeName = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@type"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
        <variable name="type" select="xsd:complexType|xsd:simpleType"/>
        <if test="$type">
            <apply-templates select="$type"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name($type)"/></with-param></apply-templates>
            <value-of select="$varName"/>.SchemaType = <value-of select="$varName"/>_<value-of select="local-name($type)"/>;
        </if>
        <for-each select="xsd:key|xsd:keyref|xsd:unique">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Constraints.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template match="xsd:group">
        <param name="varName"/>
        <choose>
            <when test="local-name(..)='schema' or local-name(..)='redefine'">
                XmlSchemaGroup <value-of select="$varName"/> = new XmlSchemaGroup();
                <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
                <if test="@name">
                    <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
                </if>
				<variable name="particle" select="xsd:choice|xsd:all|xsd:sequence"/>
				<if test="$particle">
					<apply-templates select="$particle"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name($particle)"/></with-param></apply-templates>
					<value-of select="$varName"/>.Particle = <value-of select="$varName"/>_<value-of select="local-name($particle)"/>;
				</if>
            </when>
            <otherwise>
                XmlSchemaGroupRef <value-of select="$varName"/> = new XmlSchemaGroupRef();
                <call-template name="XmlSchemaParticle"><with-param name="varName" select="$varName"/></call-template>
				<if test="@ref">
					<value-of select="$varName"/>.RefName = <call-template name="XmlQualifiedName">
							<with-param name="s" select="@ref"/>
							<with-param name="namespaces" select="namespace::*"/>
						</call-template>;
				</if>
            </otherwise>
        </choose>
	</template>

	<template match="xsd:notation">
        <param name="varName"/>
        XmlSchemaNotation <value-of select="$varName"/> = new XmlSchemaNotation();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@name">
            <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
        </if>
        <if test="@public">
            <value-of select="$varName"/>.Public = "<value-of select="@public"/>";
        </if>
        <if test="@system">
            <value-of select="$varName"/>.System = "<value-of select="@system"/>";
        </if>
	</template>

	<template match="xsd:simpleContent">
        <param name="varName"/>
        XmlSchemaSimpleContent <value-of select="$varName"/> = new XmlSchemaSimpleContent();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <variable name="content" select="xsd:restriction|xsd:extension"/>
        <if test="$content">
            <apply-templates select="$content"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name($content)"/></with-param></apply-templates>
            <value-of select="$varName"/>.Content = <value-of select="$varName"/>_<value-of select="local-name($content)"/>;
        </if>
	</template>

	<template match="xsd:complexContent">
        <param name="varName"/>
        XmlSchemaComplexContent <value-of select="$varName"/> = new XmlSchemaComplexContent();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@mixed">
            <value-of select="$varName"/>.IsMixed = XmlConvert.ToBoolean("<value-of select="@mixed"/>");
        </if>
        <variable name="content" select="xsd:restriction|xsd:extension"/>
        <if test="$content">
            <apply-templates select="$content"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name($content)"/></with-param></apply-templates>
            <value-of select="$varName"/>.Content = <value-of select="$varName"/>_<value-of select="local-name($content)"/>;
        </if>
	</template>

	<template match="xsd:restriction">
        <param name="varName"/>
        <choose>
            <when test="local-name(..)='simpleContent'">
                XmlSchemaSimpleContentRestriction <value-of select="$varName"/> = new XmlSchemaSimpleContentRestriction();
                <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
                <call-template name="SimpleRestriction"><with-param name="varName" select="$varName"/></call-template>
				<call-template name="Attributes"><with-param name="varName" select="$varName"/></call-template>
            </when>
            <when test="local-name(..)='complexContent'">
                XmlSchemaComplexContentRestriction <value-of select="$varName"/> = new XmlSchemaComplexContentRestriction();
                <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
                <if test="@base">
                    <value-of select="$varName"/>.BaseTypeName = <call-template name="XmlQualifiedName">
							<with-param name="s" select="@base"/>
							<with-param name="namespaces" select="namespace::*"/>
						</call-template>;
                </if>
                <call-template name="BaseComplexContent"><with-param name="varName" select="$varName"/></call-template>
            </when>
            <otherwise>
                XmlSchemaSimpleTypeRestriction <value-of select="$varName"/> = new XmlSchemaSimpleTypeRestriction();
                <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
                <call-template name="SimpleRestriction"><with-param name="varName" select="$varName"/></call-template>
            </otherwise>
        </choose>
	</template>

	<template match="xsd:extension">
        <param name="varName"/>
        <choose>
            <when test="local-name(..)='simpleContent'">
				XmlSchemaSimpleContentExtension <value-of select="$varName"/> = new XmlSchemaSimpleContentExtension();
				<call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
                <if test="@base">
                    <value-of select="$varName"/>.BaseTypeName = <call-template name="XmlQualifiedName">
							<with-param name="s" select="@base"/>
							<with-param name="namespaces" select="namespace::*"/>
						</call-template>;
                </if>
				<call-template name="Attributes"><with-param name="varName" select="$varName"/></call-template>
            </when>
            <otherwise>
				XmlSchemaComplexContentExtension <value-of select="$varName"/> = new XmlSchemaComplexContentExtension();
				<call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
                <if test="@base">
                    <value-of select="$varName"/>.BaseTypeName = <call-template name="XmlQualifiedName">
							<with-param name="s" select="@base"/>
							<with-param name="namespaces" select="namespace::*"/>
						</call-template>;
                </if>
				<call-template name="BaseComplexContent"><with-param name="varName" select="$varName"/></call-template>
            </otherwise>
        </choose>
	</template>

	<template match="xsd:list">
        <param name="varName"/>
        XmlSchemaSimpleTypeList <value-of select="$varName"/> = new XmlSchemaSimpleTypeList();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@itemType">
            <value-of select="$varName"/>.ItemTypeName = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@itemType"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
        <if test="xsd:simpleType">
            <apply-templates select="xsd:simpleType"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name(xsd:simpleType)"/></with-param></apply-templates>
            <value-of select="$varName"/>.ItemType = <value-of select="$varName"/>_<value-of select="local-name(xsd:simpleType)"/>;
        </if>
	</template>

	<template match="xsd:union">
        <param name="varName"/>
        XmlSchemaSimpleTypeUnion <value-of select="$varName"/> = new XmlSchemaSimpleTypeUnion();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@memberTypes">
			<variable name="namespaceDecls" select="namespace::*"/>
			<value-of select="$varName"/>.MemberTypes = new XmlQualifiedName[]
			{
				<for-each select="extCS:SplitQualifiedNameList(string(@memberTypes))">
					<call-template name="XmlQualifiedName">
						<with-param name="s" select="@memberType"/>
						<with-param name="namespaces" select="$namespaceDecls"/>
					</call-template>,
				</for-each>
			};
        </if>
        <for-each select="xsd:simpleType">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.BaseTypes.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template match="xsd:choice">
        <param name="varName"/>
        XmlSchemaChoice <value-of select="$varName"/> = new XmlSchemaChoice();
        <call-template name="XmlSchemaParticle"><with-param name="varName" select="$varName"/></call-template>
        <for-each select="xsd:element|xsd:group|xsd:choice|xsd:sequence|xsd:any">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Items.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template match="xsd:all">
        <param name="varName"/>
        XmlSchemaAll <value-of select="$varName"/> = new XmlSchemaAll();
        <call-template name="XmlSchemaParticle"><with-param name="varName" select="$varName"/></call-template>
        <for-each select="xsd:element">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Items.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template match="xsd:sequence">
        <param name="varName"/>
        XmlSchemaSequence <value-of select="$varName"/> = new XmlSchemaSequence();
        <call-template name="XmlSchemaParticle"><with-param name="varName" select="$varName"/></call-template>
        <for-each select="xsd:element|xsd:group|xsd:choice|xsd:sequence|xsd:any">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Items.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template match="xsd:any">
        <param name="varName"/>
        XmlSchemaAny <value-of select="$varName"/> = new XmlSchemaAny();
        <call-template name="XmlSchemaParticle"><with-param name="varName" select="$varName"/></call-template>
        <if test="@namespace">
            <value-of select="$varName"/>.Namespace = "<value-of select="ext:format(string(@namespace))"/>";
        </if>
        <if test="@processContents">
            <value-of select="$varName"/>.ProcessContents = <call-template name="XmlSchemaContentProcessing"><with-param name="s" select="@processContents"/></call-template>;
        </if>
	</template>

	<template match="xsd:anyAttribute">
        <param name="varName"/>
        XmlSchemaAnyAttribute <value-of select="$varName"/> = new XmlSchemaAnyAttribute();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@namespace">
            <value-of select="$varName"/>.Namespace = "<value-of select="@namespace"/>";
        </if>
        <if test="@processContents">
            <value-of select="$varName"/>.ProcessContents = <call-template name="XmlSchemaContentProcessing"><with-param name="s" select="@processContents"/></call-template>;
        </if>
	</template>

	<template match="xsd:length">
        <param name="varName"/>
        XmlSchemaLengthFacet <value-of select="$varName"/> = new XmlSchemaLengthFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:minLength">
        <param name="varName"/>
        XmlSchemaMinLengthFacet <value-of select="$varName"/> = new XmlSchemaMinLengthFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:maxLength">
        <param name="varName"/>
        XmlSchemaMaxLengthFacet <value-of select="$varName"/> = new XmlSchemaMaxLengthFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:pattern">
        <param name="varName"/>
        XmlSchemaPatternFacet <value-of select="$varName"/> = new XmlSchemaPatternFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:enumeration">
        <param name="varName"/>
        XmlSchemaEnumerationFacet <value-of select="$varName"/> = new XmlSchemaEnumerationFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:maxInclusive">
        <param name="varName"/>
        XmlSchemaMaxInclusiveFacet <value-of select="$varName"/> = new XmlSchemaMaxInclusiveFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:maxExclusive">
        <param name="varName"/>
        XmlSchemaMaxExclusiveFacet <value-of select="$varName"/> = new XmlSchemaMaxExclusiveFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:minInclusive">
        <param name="varName"/>
        XmlSchemaMinInclusiveFacet <value-of select="$varName"/> = new XmlSchemaMinInclusiveFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:minExclusive">
        <param name="varName"/>
        XmlSchemaMinExclusiveFacet <value-of select="$varName"/> = new XmlSchemaMinExclusiveFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:totalDigits">
        <param name="varName"/>
        XmlSchemaTotalDigitsFacet <value-of select="$varName"/> = new XmlSchemaTotalDigitsFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:fractionDigits">
        <param name="varName"/>
        XmlSchemaFractionDigitsFacet <value-of select="$varName"/> = new XmlSchemaFractionDigitsFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:encoding">
        <param name="varName"/>
        XmlSchemaEncodingFacet <value-of select="$varName"/> = new XmlSchemaEncodingFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:duration">
        <param name="varName"/>
        XmlSchemaDurationFacet <value-of select="$varName"/> = new XmlSchemaDurationFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:whiteSpace">
        <param name="varName"/>
        XmlSchemaWhiteSpaceFacet <value-of select="$varName"/> = new XmlSchemaWhiteSpaceFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:period">
        <param name="varName"/>
        XmlSchemaPeriodFacet <value-of select="$varName"/> = new XmlSchemaPeriodFacet();
        <call-template name="XmlSchemaFacet"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:documentation">
        <param name="varName"/>
        XmlSchemaDocumentation <value-of select="$varName"/> = new XmlSchemaDocumentation();
        <call-template name="Extra"><with-param name="varName" select="$varName"/></call-template>
        <if test="@xml:lang">
            <value-of select="$varName"/>.Language = "<value-of select="@xml:lang"/>";
        </if>
	</template>

	<template match="xsd:appinfo">
        <param name="varName"/>
        XmlSchemaAppInfo <value-of select="$varName"/> = new XmlSchemaAppInfo();
        <call-template name="Extra"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template match="xsd:key">
        <param name="varName"/>
        XmlSchemaKey <value-of select="$varName"/> = new XmlSchemaKey();
        <call-template name="XmlSchemaIdentityConstraint"><with-param name="varName" select="$varName"/></call-template>
 	</template>

	<template match="xsd:keyref">
        <param name="varName"/>
        XmlSchemaKeyref <value-of select="$varName"/> = new XmlSchemaKeyref();
        <call-template name="XmlSchemaIdentityConstraint"><with-param name="varName" select="$varName"/></call-template>
        <if test="@refer">
            <!--value-of select="$varName"/>.Refer = new XmlQualifiedName("<value-of select="@refer"/>");-->
            <value-of select="$varName"/>.Refer = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@refer"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
 	</template>

	<template match="xsd:unique">
        <param name="varName"/>
        XmlSchemaUnique <value-of select="$varName"/> = new XmlSchemaUnique();
        <call-template name="XmlSchemaIdentityConstraint"><with-param name="varName" select="$varName"/></call-template>
 	</template>

	<template match="xsd:selector">
        <param name="varName"/>
        XmlSchemaXPath <value-of select="$varName"/> = new XmlSchemaXPath();
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
		<value-of select="$varName"/>.XPath = "<value-of select="@xpath"/>";
 	</template>

	<template name="XmlSchemaParticle">
        <param name="varName"/>
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@minOccurs">
            <value-of select="$varName"/>.MinOccurs = <value-of select="@minOccurs"/>;
        </if>
        <if test="@maxOccurs">
            <value-of select="$varName"/>.MaxOccursString = "<value-of select="@maxOccurs"/>";
        </if>
	</template>

	<template name="XmlSchemaAnnotated">
        <param name="varName"/>
        <if test="@id">
            <value-of select="$varName"/>.Id = "<value-of select="@id"/>";
        </if>
        <if test="xsd:annotation">
            <apply-templates select="xsd:annotation"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name(xsd:annotation)"/></with-param></apply-templates>
            <value-of select="$varName"/>.Annotation = <value-of select="$varName"/>_<value-of select="local-name(xsd:annotation)"/>;
        </if>
        <call-template name="UnhandledAttributes"><with-param name="varName" select="$varName"/></call-template>
        <call-template name="NamespaceDeclarations"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template name="XmlSchemaFacet">
        <param name="varName"/>
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@value">
            <value-of select="$varName"/>.Value = "<value-of select="ext:format(string(@value))"/>";
        </if>
        <if test="@fixed">
            <value-of select="$varName"/>.IsFixed = XmlConvert.ToBoolean("<value-of select="@fixed"/>");
        </if>
	</template>

	<template name="BaseComplexContent">
        <param name="varName"/>
        <variable name="particle" select="xsd:group|xsd:choice|xsd:all|xsd:sequence"/>
        <if test="$particle">
            <apply-templates select="$particle"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name($particle)"/></with-param></apply-templates>
            <value-of select="$varName"/>.Particle = <value-of select="$varName"/>_<value-of select="local-name($particle)"/>;
        </if>
        <call-template name="Attributes"><with-param name="varName" select="$varName"/></call-template>
	</template>

	<template name="SimpleRestriction">
        <param name="varName"/>
        <if test="@base">
            <value-of select="$varName"/>.BaseTypeName = <call-template name="XmlQualifiedName">
					<with-param name="s" select="@base"/>
					<with-param name="namespaces" select="namespace::*"/>
				</call-template>;
        </if>
        <if test="xsd:simpleType">
            <apply-templates select="xsd:simpleType"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name(xsd:simpleType)"/></with-param></apply-templates>
            <value-of select="$varName"/>.BaseType = <value-of select="$varName"/>_<value-of select="local-name(xsd:simpleType)"/>;
        </if>
        <for-each select="xsd:length|xsd:minLength|xsd:maxLength|xsd:pattern|xsd:enumeration|xsd:maxInclusive|xsd:maxExclusive|xsd:minInclusive|xsd:minExclusive|xsd:totalDigits|xsd:fractionDigits|xsd:whiteSpace">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Facets.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
	</template>

	<template name="Attributes">
        <param name="varName"/>
        <for-each select="xsd:attribute|xsd:attributeGroup">
            {
                <apply-templates select="."><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></apply-templates>
                <value-of select="$varName"/>.Attributes.Add(<value-of select="$varName"/>_<value-of select="local-name()"/>);
            }
        </for-each>
        <if test="xsd:anyAttribute">
            <apply-templates select="xsd:anyAttribute"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name(xsd:anyAttribute)"/></with-param></apply-templates>
            <value-of select="$varName"/>.AnyAttribute = <value-of select="$varName"/>_<value-of select="local-name(xsd:anyAttribute)"/>;
        </if>
	</template>

	<template name="XmlSchemaIdentityConstraint">
        <param name="varName"/>
        <call-template name="XmlSchemaAnnotated"><with-param name="varName" select="$varName"/></call-template>
        <if test="@name">
            <value-of select="$varName"/>.Name = "<value-of select="@name"/>";
        </if>
        <if test="xsd:selector">
            <apply-templates select="xsd:selector"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name(xsd:selector)"/></with-param></apply-templates>
			<value-of select="$varName"/>.Selector = <value-of select="$varName"/>_<value-of select="local-name(xsd:selector)"/>;
        </if>
        <for-each select="xsd:field">
            {
				XmlSchemaXPath field = new XmlSchemaXPath();
				<call-template name="XmlSchemaAnnotated"><with-param name="varName" select="'field'"/></call-template>
				field.XPath = "<value-of select="@xpath"/>";
                <value-of select="$varName"/>.Fields.Add(field);
            }
        </for-each>
	</template>

	<template name="Extra">
        <param name="varName"/>
        <if test="@source">
            <value-of select="$varName"/>.Source = "<value-of select="@source"/>";
        </if>
        <call-template name="XmlElement"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></call-template>
        <value-of select="$varName"/>.Markup = GetChildren(<value-of select="$varName"/>_<value-of select="local-name()"/>);
	</template>


	<template name="XmlElement">
        <param name="varName"/>
		XmlElement <value-of select="$varName"/> = doc.CreateElement("<value-of select="name()"/>", "<value-of select="namespace-uri()"/>");
		<for-each select="@*">
            {
		        XmlAttribute <value-of select="$varName"/>_attr = doc.CreateAttribute("<value-of select="name()"/>", "<value-of select="namespace-uri()"/>");
                <value-of select="$varName"/>_attr.Value = "<value-of select="ext:format(string(.))"/>";
    		    <value-of select="$varName"/>.Attributes.Append(<value-of select="$varName"/>_attr);
            }
		</for-each>

		<for-each select="*|text()">
			<choose>
				<when test="local-name()=''">
                    <value-of select="$varName"/>.AppendChild(doc.CreateTextNode("<value-of select="ext:format(string(.))"/>"));
				</when>
				<otherwise>
                    {
                        <call-template name="XmlElement"><with-param name="varName"><value-of select="$varName"/>_<value-of select="local-name()"/></with-param></call-template>
                        <value-of select="$varName"/>.AppendChild(<value-of select="$varName"/>_<value-of select="local-name()"/>);
                    }
				</otherwise>
			</choose>
		</for-each>
	</template>

	<template name="NamespaceDeclarations">
        <param name="varName"/>
        <variable name="namespaceDecls" select="extCS:GetNamespaceDeclarations(.)"/>
        <if test="count($namespaceDecls)">
			<for-each select="$namespaceDecls">
				<value-of select="$varName"/>.Namespaces.Add("<value-of select="local-name()"/>", "<value-of select="."/>");
			</for-each>
        </if>
	</template>

	<template name="UnhandledAttributes">
        <param name="varName"/>
        <variable name="unhandledAttrs" select="@*[namespace-uri()!='']"/>
        <if test="count($unhandledAttrs)">
            <value-of select="$varName"/>.UnhandledAttributes = new XmlAttribute[<value-of select="count($unhandledAttrs)"/>];
		    <for-each select="$unhandledAttrs">
                {
		            XmlAttribute <value-of select="$varName"/>_attr = doc.CreateAttribute("<value-of select="name()"/>", "<value-of select="namespace-uri()"/>");
                    <value-of select="$varName"/>_attr.Value = "<value-of select="ext:format(string(.))"/>";
    		        <value-of select="$varName"/>.UnhandledAttributes[<value-of select="position() - 1"/>] = <value-of select="$varName"/>_attr;
                }
		    </for-each>
        </if>
	</template>

	<template name="XmlQualifiedName">
        <param name="s"/>
        <param name="namespaces"/>
        <choose>
            <when test="contains($s,':')">
				<variable name="prefix" select="substring-before($s, ':')"/>
				<variable name="lname" select="substring-after($s, ':')"/>
				new XmlQualifiedName("<value-of select="$lname"/>", "<value-of select="$namespaces[local-name()=$prefix]"/>")
			</when>
            <otherwise>
				new XmlQualifiedName("<value-of select="$s"/>", "<value-of select="$namespaces[local-name()='']"/>")
			</otherwise>
		</choose>
	</template>

	<template name="XmlSchemaForm">
        <param name="s"/>
        <choose>
            <when test="$s='qualified'">XmlSchemaForm.Qualified</when>
            <when test="$s='unqualified'">XmlSchemaForm.Unqualified</when>
            <otherwise>XmlSchemaForm.None</otherwise>
        </choose>
	</template>

	<template name="XmlSchemaDerivationMethod">
        <param name="s"/>
        <text>XmlSchemaDerivationMethod.Empty</text>
        <if test="contains($s,'substitution')"> | XmlSchemaDerivationMethod.Substitution</if>
        <if test="contains($s,'extension')"> | XmlSchemaDerivationMethod.Extension</if>
        <if test="contains($s,'restriction')"> | XmlSchemaDerivationMethod.Restriction</if>
        <if test="contains($s,'union')"> | XmlSchemaDerivationMethod.Union</if>
        <if test="contains($s,'list')"> | XmlSchemaDerivationMethod.List</if>
        <if test="contains($s,'#all')"> | XmlSchemaDerivationMethod.All</if>
	</template>

	<template name="XmlSchemaContentProcessing">
        <param name="s"/>
        <choose>
            <when test="$s='skip'">XmlSchemaContentProcessing.Skip</when>
            <when test="$s='lax'">XmlSchemaContentProcessing.Lax</when>
            <when test="$s='strict'">XmlSchemaContentProcessing.Strict</when>
            <otherwise>XmlSchemaContentProcessing.None</otherwise>
        </choose>
	</template>

	<template name="XmlSchemaUse">
        <param name="s"/>
        <choose>
            <when test="$s='prohibited'">XmlSchemaUse.Prohibited</when>
            <when test="$s='optional'">XmlSchemaUse.Optional</when>
            <when test="$s='required'">XmlSchemaUse.Required</when>
            <when test="$s='default'">XmlSchemaUse.Default</when>
            <when test="$s='fixed'">XmlSchemaUse.Fixed</when>
            <otherwise>XmlSchemaUse.None</otherwise>
        </choose>
	</template>

    <msxsl:script language="JavaScript" implements-prefix="ext">
        function format(source) {
			source = source.replace(/\\/g,"\\\\");
			source = source.replace(/"/g, '\\"');
			source = source.replace(/\r/g,"\\r");
			source = source.replace(/\n/g,"\\n");
			source = source.replace(/\t/g,"\\t");
            return source;
        }
    </msxsl:script>
    <msxsl:script language="C#" implements-prefix="extCS">
		// split a space separated list of qnames
		// returns a node set with one element for each qname
		// the qname is returned as an attribute on each element
        public XPathNodeIterator SplitQualifiedNameList(string source)
		{
			string xml = String.Empty;

			string[] atoms = source.Split();
			foreach(string atom in atoms)
			{
				string val = atom.Trim();
				if(val != String.Empty)
				{
					xml += String.Format("&lt;e memberType='{0}'/&gt;", val);
				}
			}

			XmlTextReader r = new XmlTextReader(xml, XmlNodeType.Element, null);
			XPathDocument doc = new XPathDocument(r);
			r.Close();

			XPathNavigator nav = doc.CreateNavigator();
			XPathNodeIterator nodeSet = nav.Select("e");

            return nodeSet;
        }

		// create a separate node set with all namespaces defined at current node level
		public XPathNodeIterator GetNamespaceDeclarations(XPathNodeIterator source)
		{
			string xml = String.Empty;

			source.MoveNext();
			XPathNavigator nav = source.Current;
			bool bMoved = nav.MoveToFirstNamespace(XPathNamespaceScope.Local);
			while(bMoved)
			{
				string name = (nav.LocalName == String.Empty ? "xmlns" : "xmlns:" + nav.LocalName);
				xml += String.Format(" {0}='{1}'", name, nav.Value);
				bMoved = nav.MoveToNextNamespace(XPathNamespaceScope.Local);
			}

			xml = String.Format("&lt;e {0}/&gt;", xml);

			XmlTextReader r = new XmlTextReader(xml, XmlNodeType.Element, null);
			XPathDocument doc = new XPathDocument(r);
			r.Close();

			nav = doc.CreateNavigator();
			nav.MoveToFirstChild();
			XPathNodeIterator nodeSet = nav.Select("namespace::*[local-name()!='xml']");

            return nodeSet;
        }
    </msxsl:script>

</stylesheet>
