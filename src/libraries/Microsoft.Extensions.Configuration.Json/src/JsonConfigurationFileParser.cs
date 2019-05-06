// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration.Json
{
    internal class JsonConfigurationFileParser
    {
        private JsonConfigurationFileParser() { }

        private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _context = new Stack<string>();
        private string _currentPath;

        public static IDictionary<string, string> Parse(Stream input)
            => new JsonConfigurationFileParser().ParseStream(input);

        private IDictionary<string, string> ParseStream(Stream input)
        {
            _data.Clear();

            using (var reader = new StreamReader(input))
            using (JsonDocument doc = JsonDocument.Parse(reader.ReadToEnd(), new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip }))
            {
                if (doc.RootElement.Type != JsonValueType.Object)
                {
                    throw new FormatException(Resources.FormatError_UnsupportedJSONToken(doc.RootElement.Type));
                }
                VisitElement(doc.RootElement);
            }

            return _data;
        }

        private void VisitElement(JsonElement element) {
            foreach (var property in element.EnumerateObject())
            {
                EnterContext(property.Name);
                VisitValue(property.Value);
                ExitContext();
            }
        }

        private void VisitValue(JsonElement value)
        {
            switch (value.Type) {
                case JsonValueType.Object:
                    VisitElement(value);
                    break;

                case JsonValueType.Array:
                    var index = 0;
                    foreach (var arrayElement in value.EnumerateArray()) {
                        EnterContext(index.ToString());
                        VisitValue(arrayElement);
                        ExitContext();
                        index++;
                    }
                    break;

                case JsonValueType.Number:
                case JsonValueType.String:
                case JsonValueType.True:
                case JsonValueType.False:
                case JsonValueType.Null:
                    var key = _currentPath;
                    if (_data.ContainsKey(key))
                    {
                        throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                    }
                    _data[key] = value.ToString();
                    break;

                default:
                    throw new FormatException(Resources.FormatError_UnsupportedJSONToken(value.Type));
            }
        }

        private void EnterContext(string context)
        {
            _context.Push(context);
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private void ExitContext()
        {
            _context.Pop();
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }
    }
}
