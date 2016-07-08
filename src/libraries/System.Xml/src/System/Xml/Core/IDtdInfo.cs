// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Xml
{
    //
    // IDtdInfo interface
    //
    /// <summary>
    /// This is an interface for a compiled DTD information. 
    /// It exposes information and functionality that XmlReader need in order to be able
    /// to expand entities, add default attributes and correctly normalize attribute values 
    /// according to their data types.
    /// </summary>
    internal interface IDtdInfo
    {
        /// <summary>
        /// DOCTYPE name
        /// </summary>
        XmlQualifiedName Name { get; }

        /// <summary>
        /// Internal DTD subset as specified in the XML document
        /// </summary>
        string InternalDtdSubset { get; }

        /// <summary>
        /// Returns true if the DTD contains any declaration of a default attribute
        /// </summary>
        bool HasDefaultAttributes { get; }

        /// <summary>
        /// Returns true if the DTD contains any declaration of an attribute 
        /// whose type is other than CDATA
        /// </summary>
        bool HasNonCDataAttributes { get; }

        /// <summary>
        /// Looks up a DTD attribute list definition by its name. 
        /// </summary>
        /// <param name="prefix">The prefix of the attribute list to look for</param>
        /// <param name="localName">The local name of the attribute list to look for</param>
        /// <returns>Interface representing an attribute list or null if none was found.</returns>
        IDtdAttributeListInfo LookupAttributeList(string prefix, string localName);

        /// <summary>
        /// Returns an enumerator of all attribute lists defined in the DTD.
        /// </summary>
        IEnumerable<IDtdAttributeListInfo> GetAttributeLists();

        /// <summary>
        /// Looks up a general DTD entity by its name.
        /// </summary>
        /// <param name="name">The name of the entity to look for</param>
        /// <returns>Interface representing an entity or null if none was found.</returns>
        IDtdEntityInfo LookupEntity(string name);
    };

    //
    // IDtdAttributeListInfo interface
    //
    /// <summary>
    /// Exposes information about attributes declared in an attribute list in a DTD 
    /// that XmlReader need in order to be able to add default attributes 
    /// and correctly normalize attribute values according to their data types.
    /// </summary>
    internal interface IDtdAttributeListInfo
    {
        /// <summary>
        /// Prefix of an element this attribute list belongs to.
        /// </summary>
        string Prefix { get; }
        /// <summary>
        /// Local name of an element this attribute list belongs to.
        /// </summary>
        string LocalName { get; }
        /// <summary>
        /// Returns true if the attribute list has some declared attributes with
        /// type other than CDATA.
        /// </summary>
        bool HasNonCDataAttributes { get; }
        /// <summary>
        /// Looks up a DTD attribute definition by its name.
        /// </summary>
        /// <param name="prefix">The prefix of the attribute to look for</param>
        /// <param name="localName">The local name of the attribute to look for</param>
        /// <returns>Interface representing an attribute or null is none was found</returns>
        IDtdAttributeInfo LookupAttribute(string prefix, string localName);
        /// <summary>
        /// Returns enumeration of all default attributes
        /// defined in this attribute list.
        /// </summary>
        /// <returns>Enumerator of default attribute.</returns>
        IEnumerable<IDtdDefaultAttributeInfo> LookupDefaultAttributes();
        /// <summary>
        /// Looks up a ID attribute defined in the attribute list. Returns
        /// null if the attribute list does define an ID attribute.
        /// </summary>
        IDtdAttributeInfo LookupIdAttribute();
    }

    //
    // IDtdAttributeInfo interface
    //
    /// <summary>
    /// Exposes information about an attribute declared in a DTD 
    /// that XmlReader need in order to be able to correctly normalize 
    /// the attribute value according to its data types.
    /// </summary>
    internal interface IDtdAttributeInfo
    {
        /// <summary>
        /// The prefix of the attribute
        /// </summary>
        string Prefix { get; }
        /// <summary>
        /// The local name of the attribute
        /// </summary>
        string LocalName { get; }
        /// <summary>
        /// The line number of the DTD attribute definition
        /// </summary>
        int LineNumber { get; }
        /// <summary>
        /// The line position of the DTD attribute definition
        /// </summary>
        int LinePosition { get; }
        /// <summary>
        /// Returns true if the attribute is of a different type than CDATA
        /// </summary>
        bool IsNonCDataType { get; }
        /// <summary>
        /// Returns true if the attribute was declared in an external DTD subset
        /// </summary>
        bool IsDeclaredInExternal { get; }
        /// <summary>
        /// Returns true if the attribute is xml:space or xml:lang
        /// </summary>
        bool IsXmlAttribute { get; }
    }

    //
    // IDtdDefaultAttributeInfo interface
    //
    /// <summary>
    /// Exposes information about a default attribute 
    /// declared in a DTD that XmlReader need in order to be able to add 
    /// this attribute to the XML document (it is not present already) 
    /// or correctly normalize the attribute value according to its data types.
    /// </summary>
    internal interface IDtdDefaultAttributeInfo : IDtdAttributeInfo
    {
        /// <summary>
        /// The expanded default value of the attribute
        /// the consumer assumes that all entity references
        /// were already resolved in the value and that the value
        /// is correctly normalized.
        /// </summary>
        string DefaultValueExpanded { get; }
        /// <summary>
        /// The typed default value of the attribute.
        /// </summary>
        object DefaultValueTyped { get; }        /// <summary>
                                                 /// The line number of the default value (in the DTD)
                                                 /// </summary>
        int ValueLineNumber { get; }
        /// <summary>
        /// The line position of the default value (in the DTD)
        /// </summary>
        int ValueLinePosition { get; }
    }

    //
    // IDtdEntityInfo interface
    //
    /// <summary>
    /// Exposes information about a general entity 
    /// declared in a DTD that XmlReader need in order to be able
    /// to expand the entity.
    /// </summary>
    internal interface IDtdEntityInfo
    {
        /// <summary>
        /// The name of the entity
        /// </summary>
        string Name { get; }
        /// <summary>
        /// true if the entity is external (its value is in an external input)
        /// </summary>
        bool IsExternal { get; }
        /// <summary>
        /// true if the entity was declared in external DTD subset
        /// </summary>
        bool IsDeclaredInExternal { get; }
        /// <summary>
        /// true if this is an unparsed entity
        /// </summary>
        bool IsUnparsedEntity { get; }
        /// <summary>
        /// true if this is a parameter entity
        /// </summary>
        bool IsParameterEntity { get; }
        /// <summary>
        /// The base URI of the entity value
        /// </summary>
        string BaseUriString { get; }
        /// <summary>
        /// The URI of the XML document where the entity was declared
        /// </summary>
        string DeclaredUriString { get; }
        /// <summary>
        /// SYSTEM identifier (URI) of the entity value - only used for external entities
        /// </summary>
        string SystemId { get; }
        /// <summary>
        /// PUBLIC identifier of the entity value - only used for external entities
        /// </summary>
        string PublicId { get; }
        /// <summary>
        /// Replacement text of an entity. Valid only for internal entities.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// The line number of the entity value
        /// </summary>
        int LineNumber { get; }
        /// <summary>
        /// The line position of the entity value
        /// </summary>
        int LinePosition { get; }
    }
}
