// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace ILLink.Tasks
{
    static class RuntimeHeaderParser
    {
        internal static Dictionary<string, string> ReadNamespaces(IEnumerable<string> lines)
        {
            var namespaces = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string sourceLine in lines)
            {
                string line = sourceLine.Trim();
                if (!line.StartsWith("#define "))
                    continue;

                int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                if (commentIndex >= 0)
                    line = line.Substring(0, commentIndex);
                char[] separators = { '"', ' ', '\t' };
                string[] elements = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                int startIndex = "g_".Length;
                if ((elements.Length != 3 && elements.Length != 4) ||
                    !elements[1].StartsWith("g_", StringComparison.Ordinal) ||
                    !elements[1].EndsWith("NS", StringComparison.Ordinal))
                    throw new InvalidDataException($"Invalid namespace definition: {line}");
                string name = elements[1].Substring(startIndex, elements[1].LastIndexOf('N') - startIndex);
                if (elements.Length == 3)
                {
                    namespaces[name] = elements[2];
                }
                else
                {
                    if (!elements[2].StartsWith("g_", StringComparison.Ordinal) ||
                        !elements[2].EndsWith("NS", StringComparison.Ordinal))
                        throw new InvalidDataException($"Invalid namespace prefix: {line}");
                    string prefix = elements[2].Substring(startIndex, elements[2].LastIndexOf('N') - startIndex);
                    if (!namespaces.TryGetValue(prefix, out string? value))
                        throw new InvalidDataException($"Unknown namespace prefix {prefix}: {line}");
                    namespaces[name] = value + elements[3];
                }
            }

            return namespaces;
        }

        internal sealed class DefineTracker
        {
            readonly HashSet<string> _defineConstants;
            readonly Action<string> _logError;
            readonly string _filePath;
            readonly Stack<bool?> _activeSections = new Stack<bool?>();
            readonly Stack<bool> _elseSections = new Stack<bool>();
            int _lineNumber;

            internal DefineTracker(HashSet<string> defineConstants, Action<string> logError, string filePath)
            {
                _defineConstants = defineConstants;
                _logError = logError;
                _filePath = filePath;
            }

            internal bool ProcessLine(string line, int lineNumber = 0)
            {
                _lineNumber = lineNumber == 0 ? _lineNumber + 1 : lineNumber;
                line = line.Trim();
                int commentIndex = line.IndexOf('/');
                if (line.StartsWith("#") && commentIndex >= 0)
                    line = line.Substring(0, commentIndex).TrimEnd();

                int ifdefLength = 0;
                bool negativeIfDef = false;
                if (line.StartsWith("#ifdef "))
                {
                    ifdefLength = 7;
                }
                else if (line.StartsWith("#ifndef "))
                {
                    ifdefLength = 8;
                    negativeIfDef = true;
                }
                else if (line.StartsWith("#if "))
                {
                    ifdefLength = 4;
                }

                if (ifdefLength > 0)
                {
                    string defineName = line.Substring(ifdefLength).Trim();
                    if (!IsIdentifier(defineName))
                    {
                        _logError($"Unsupported preprocessor condition in '{_filePath}' at line {_lineNumber}: {line}");
                        return true;
                    }

                    _elseSections.Push(false);
                    if (_activeSections.Count > 0 && _activeSections.Peek() != true)
                    {
                        _activeSections.Push(null);
                    }
                    else
                    {
                        _activeSections.Push(_defineConstants.Contains(defineName) != negativeIfDef);
                    }

                    return true;
                }

                if (line == "#else")
                {
                    if (_activeSections.Count == 0 || _elseSections.Peek())
                    {
                        _logError($"Could not figure out ifdefs in '{_filePath}' around line {_lineNumber}.");
                    }
                    else
                    {
                        _elseSections.Pop();
                        _elseSections.Push(true);
                        bool? activeSection = _activeSections.Pop();
                        _activeSections.Push(activeSection.HasValue ? !activeSection.Value : (bool?)null);
                    }

                    return true;
                }

                if (line == "#endif")
                {
                    if (_activeSections.Count == 0)
                        _logError($"Could not figure out ifdefs in '{_filePath}' around line {_lineNumber}.");
                    else
                    {
                        _activeSections.Pop();
                        _elseSections.Pop();
                    }

                    return true;
                }

                if (line.StartsWith("#if") || line.StartsWith("#else") ||
                    line.StartsWith("#elif") || line.StartsWith("#endif"))
                {
                    _logError($"Unsupported preprocessor directive in '{_filePath}' at line {_lineNumber}: {line}");
                    return true;
                }

                return false;
            }

            internal void Complete()
            {
                if (_activeSections.Count != 0)
                    _logError($"Unterminated preprocessor condition in '{_filePath}' at line {_lineNumber}.");
            }

            internal bool IsActiveSection => _activeSections.Count == 0 || _activeSections.Peek() == true;

            static bool IsIdentifier(string text)
            {
                if (text.Length == 0)
                    return false;

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (c != '_' && !(c >= 'A' && c <= 'Z') && !(c >= 'a' && c <= 'z') &&
                        !(i > 0 && c >= '0' && c <= '9'))
                        return false;
                }

                return true;
            }
        }
    }
}
