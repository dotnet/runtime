// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    public class ProductHeaderValue : ICloneable
    {
        private string _name;
        private string _version;

        public string Name
        {
            get { return _name; }
        }

        // We can't use the System.Version type, since a version can be e.g. "x11".
        public string Version
        {
            get { return _version; }
        }

        public ProductHeaderValue(string name)
            : this(name, null)
        {
        }

        public ProductHeaderValue(string name, string version)
        {
            HeaderUtilities.CheckValidToken(name, nameof(name));

            if (!string.IsNullOrEmpty(version))
            {
                HeaderUtilities.CheckValidToken(version, nameof(version));
                _version = version;
            }

            _name = name;
        }

        private ProductHeaderValue(ProductHeaderValue source)
        {
            Debug.Assert(source != null);

            _name = source._name;
            _version = source._version;
        }

        private ProductHeaderValue()
        {
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_version))
            {
                return _name;
            }
            return _name + "/" + _version;
        }

        public override bool Equals(object obj)
        {
            ProductHeaderValue other = obj as ProductHeaderValue;

            if (other == null)
            {
                return false;
            }

            return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_version, other._version, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            int result = StringComparer.OrdinalIgnoreCase.GetHashCode(_name);

            if (!string.IsNullOrEmpty(_version))
            {
                result = result ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_version);
            }

            return result;
        }

        public static ProductHeaderValue Parse(string input)
        {
            int index = 0;
            return (ProductHeaderValue)GenericHeaderParser.SingleValueProductParser.ParseValue(input, null, ref index);
        }

        public static bool TryParse(string input, out ProductHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (GenericHeaderParser.SingleValueProductParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (ProductHeaderValue)output;
                return true;
            }
            return false;
        }

        internal static int GetProductLength(string input, int startIndex, out ProductHeaderValue parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the name string: <name> in '<name>/<version>'.
            int nameLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (nameLength == 0)
            {
                return 0;
            }

            ProductHeaderValue result = new ProductHeaderValue();
            result._name = input.Substring(startIndex, nameLength);
            int current = startIndex + nameLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if ((current == input.Length) || (input[current] != '/'))
            {
                parsedValue = result;
                return current - startIndex;
            }

            current++; // Skip '/' delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the name string: <version> in '<name>/<version>'.
            int versionLength = HttpRuleParser.GetTokenLength(input, current);

            if (versionLength == 0)
            {
                return 0; // If there is a '/' separator it must be followed by a valid token.
            }

            result._version = input.Substring(current, versionLength);

            current = current + versionLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            parsedValue = result;
            return current - startIndex;
        }

        object ICloneable.Clone()
        {
            return new ProductHeaderValue(this);
        }
    }
}
