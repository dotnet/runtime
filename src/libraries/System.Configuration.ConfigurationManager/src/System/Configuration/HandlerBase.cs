// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Xml;

namespace System.Configuration
{
    internal static class HandlerBase
    {
        private static XmlNode GetAndRemoveAttribute(XmlNode node, string attrib, bool fRequired)
        {
            XmlNode a = node.Attributes.RemoveNamedItem(attrib);

            // If the attribute is required and was not present, throw
            if (fRequired && a == null)
            {
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Config_missing_required_attribute, attrib, node.Name),
                    node);
            }

            return a;
        }

        private static XmlNode GetAndRemoveStringAttributeInternal(XmlNode node, string attrib, bool fRequired, ref string val)
        {
            XmlNode a = GetAndRemoveAttribute(node, attrib, fRequired);
            if (a != null)
                val = a.Value;

            return a;
        }

        internal static XmlNode GetAndRemoveStringAttribute(XmlNode node, string attrib, ref string val)
        {
            return GetAndRemoveStringAttributeInternal(node, attrib, false /*fRequired*/, ref val);
        }

        // input.Xml cursor must be at a true/false XML attribute
        private static XmlNode GetAndRemoveBooleanAttributeInternal(XmlNode node, string attrib, bool fRequired, ref bool val)
        {
            XmlNode a = GetAndRemoveAttribute(node, attrib, fRequired);
            if (a != null)
            {
                try
                {
                    val = bool.Parse(a.Value);
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException(
                            SR.Format(SR.Config_invalid_boolean_attribute, a.Name),
                            e, a);
                }
            }

            return a;
        }

        internal static XmlNode GetAndRemoveBooleanAttribute(XmlNode node, string attrib, ref bool val)
        {
            return GetAndRemoveBooleanAttributeInternal(node, attrib, false /*fRequired*/, ref val);
        }

        // input.Xml cursor must be an integer XML attribute
        private static XmlNode GetAndRemoveIntegerAttributeInternal(XmlNode node, string attrib, bool fRequired, ref int val)
        {
            XmlNode a = GetAndRemoveAttribute(node, attrib, fRequired);
            if (a != null)
            {
                if (a.Value.Trim() != a.Value)
                {
                    throw new ConfigurationErrorsException(
                        SR.Format(SR.Config_invalid_integer_attribute, a.Name), a);
                }

                try
                {
                    val = int.Parse(a.Value, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException(
                        SR.Format(SR.Config_invalid_integer_attribute, a.Name),
                        e, a);
                }
            }

            return a;
        }

        internal static XmlNode GetAndRemoveIntegerAttribute(XmlNode node, string attrib, ref int val)
        {
            return GetAndRemoveIntegerAttributeInternal(node, attrib, false /*fRequired*/, ref val);
        }

        internal static void CheckForUnrecognizedAttributes(XmlNode node)
        {
            if (node.Attributes.Count != 0)
            {
                throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_base_unrecognized_attribute, node.Attributes[0].Name),
                                node);
            }
        }

        // if attribute not found return null
        internal static string RemoveAttribute(XmlNode node, string name)
        {

            XmlNode attribute = node.Attributes.RemoveNamedItem(name);

            if (attribute != null)
            {
                return attribute.Value;
            }

            return null;
        }

        // if attr not found throw standard message - "attribute x required"
        internal static string RemoveRequiredAttribute(XmlNode node, string name)
        {
            return RemoveRequiredAttribute(node, name, false/*allowEmpty*/);
        }

        internal static string RemoveRequiredAttribute(XmlNode node, string name, bool allowEmpty)
        {
            XmlNode attribute = node.Attributes.RemoveNamedItem(name);

            if (attribute == null)
            {
                throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_base_required_attribute_missing, name),
                                node);
            }

            if (string.IsNullOrEmpty(attribute.Value) && allowEmpty == false)
            {
                throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_base_required_attribute_empty, name),
                                node);
            }

            return attribute.Value;
        }

        internal static void CheckForNonElement(XmlNode node)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                throw new ConfigurationErrorsException(SR.Config_base_elements_only, node);
            }
        }

        internal static bool IsIgnorableAlsoCheckForNonElement(XmlNode node)
        {
            if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.Whitespace)
            {
                return true;
            }

            CheckForNonElement(node);

            return false;
        }

        internal static void CheckForChildNodes(XmlNode node)
        {
            if (node.HasChildNodes)
            {
                throw new ConfigurationErrorsException(SR.Config_base_no_child_nodes, node.FirstChild);
            }
        }

        internal static void ThrowUnrecognizedElement(XmlNode node)
        {
            throw new ConfigurationErrorsException(SR.Config_base_unrecognized_element, node);
        }
    }
}
