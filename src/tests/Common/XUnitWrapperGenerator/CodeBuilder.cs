// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static System.FormattableString;

namespace XUnitWrapperGenerator;

/// <summary>
///     A helper class for generating indented code.  Indentation is automatically added to lines.
///     Trailing whitespace is removed from lines.
/// </summary>
[DebuggerDisplay("Code = {_code}")]
public class CodeBuilder {
    private readonly Stack<int> _indentLevels;
    private string _currentIndentString;
    private readonly int _indentSize;
    private readonly StringBuilder _code;
    private const int DefaultAdditionalIndent = 1;

    private sealed class IndentationContext : IDisposable {
        private CodeBuilder Builder { get; }
        private bool _disposed;
        private string? EndLine { get; }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="builder">The <see cref="CodeBuilder"/> associated with this object.</param>
        /// <param name="additionalIndent">The number of indentation levels to add.</param>
        /// <param name="endLine">Line to add after disposing the indentation context</param>
        public IndentationContext(CodeBuilder builder, uint additionalIndent = DefaultAdditionalIndent, string? endLine = null) {
            Builder = builder;
            _disposed = false;
            EndLine = endLine;
            Builder.PushIndent(additionalIndent);
        }

        /// <summary>
        ///     Performs cleanup actions at the end of the lifetime.
        ///     This involves decreasing the level of indentation on
        ///     the <see cref="CodeBuilder"/> object that was used to
        ///     construct this <see cref="IndentationContext"/>.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;
            Builder.PopIndent();
            if (EndLine != null) Builder.AppendLine(EndLine);
            _disposed = true;
        }
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="indentSize">The number of spaces each level of indentation adds.</param>
    public CodeBuilder(uint indentSize = 4) {
        _indentLevels = new Stack<int>();
        _indentSize = Convert.ToInt32(indentSize);
        _currentIndentString = "";
        _code = new StringBuilder();
        _indentLevels.Push(0);
    }

    public bool IsEmpty => _code.Length == 0;

    public static CodeBuilder Create(string initialCode) {
        var code = new CodeBuilder();
        code.Append(initialCode);
        return code;
    }

    public static CodeBuilder CreateNewLine(string initialCode) {
        var code = new CodeBuilder();
        code.Append(initialCode);
        code.AppendLine();
        return code;
    }

    /// <summary>
    ///     Push a new indent level.
    /// </summary>
    /// <param name="additionalIndent">The amount of indentation to add.</param>
    public void PushIndent(uint additionalIndent = DefaultAdditionalIndent) {
        int existingIndent = _indentLevels.Peek();
        var newIndent = (int) (existingIndent + additionalIndent);
        _indentLevels.Push(newIndent);
        _currentIndentString = new string(' ', newIndent * _indentSize);
    }

    /// <summary>
    ///     Pop an indent level (and restore the indent level to before the last call to <see cref="PushIndent" />).
    /// </summary>
    public void PopIndent() {
        _indentLevels.Pop();
        _currentIndentString = new string(' ', _indentLevels.Peek() * _indentSize);
    }

    private bool AtStartOfLine() {
        if (_code.Length == 0) {
            return true;
        }

        return _code[_code.Length - 1] == '\n';
    }

    private void Append(string code, bool allowLeadingWhiteSpace) {
        if (string.IsNullOrEmpty(code)) return;

        string[] lines = code.Split('\n');

        // Do entire check first to avoid a partial write in the case of failure
        if (!allowLeadingWhiteSpace) {
            for (int i = 0; i < lines.Length; ++i) {
                if ((i > 0 || AtStartOfLine())
                    && (lines[i].Length > 0) && char.IsWhiteSpace(lines[i][0])) {
                    throw new ArgumentException(Invariant($@"Whitespace (0x{(int)lines[i][0]:x2}) at start of line {i} in input '{code}'"));
                }
            }
        }

        for (int i = 0; i < lines.Length; ++i) {
            if (i != 0) AppendLine();

            string line = lines[i];
            if (AtStartOfLine() && !string.IsNullOrWhiteSpace(line)) _code.Append(_currentIndentString);
            _code.Append(line);
        }
    }

    /// <summary>
    ///     Append the given code.  The currently active indentation level is applied at newlines.
    /// </summary>
    /// <exception cref="System.ArgumentException">Thrown when a line already contains leading whitespace</exception>
    /// <param name="code">The code to append.</param>
    public void Append(string code) => Append(code, allowLeadingWhiteSpace: false);

    /// <summary>
    ///     Append the given, already-indented code.  The currently active indentation level is also applied at newlines.
    /// </summary>
    /// <param name="code">The code to append.</param>
    public void AppendIndented(string code) => Append(code, allowLeadingWhiteSpace: true);

    /// <summary>
    ///     Append the given, already-indented code.  The currently active indentation level is also applied at newlines.
    /// </summary>
    /// <param name="code">The code to append.</param>
    public void Append(CodeBuilder code) => AppendIndented(code.GetCode());

    /// <summary>
    ///     Append the given code followed by a line terminator.  The currently active indentation level is applied at newlines.
    /// </summary>
    /// <param name="codeLine">The line to append.</param>
    public void AppendLine(string codeLine) {
        Append(codeLine);
        AppendLine();
    }

    /// <summary>Append a blank line.</summary>
    public void AppendLine() {
        int lastToKeep;
        for (lastToKeep = _code.Length - 1; lastToKeep >= 0; --lastToKeep) {
            if (_code[lastToKeep] == '\n' || !char.IsWhiteSpace(_code[lastToKeep])) {
                break;
            }
        }
        _code.Length = lastToKeep + 1;
        _code.AppendLine();
    }

    /// <summary>
    ///     Appends a block of code using the current indentation.
    /// </summary>
    /// <param name="block">The code block.</param>
    public void AppendBlock(string block) {
        if (block == null) throw new ArgumentNullException(nameof(block));

        using (var reader = new StringReader(block)) {
            string line = reader.ReadLine();
            if (line == null) return;

            AppendIndented(line);
            while ((line = reader.ReadLine()) != null) {
                AppendLine();
                AppendIndented(line);
            }
        }
    }

    /// <summary>
    ///     Creates a new <see cref="IndentationContext"/>.
    /// </summary>
    /// <param name="introduction">
    ///     String to add before the braces and indentation context.  If non-empty, then a new line will be
    ///     appended after it.
    /// </param>
    /// <param name="additionalIndent">Number of indentation levels to add.</param>
    /// <returns>A new <see cref="IndentationContext"/>.</returns>
    public IDisposable NewScope(string? introduction = null, uint additionalIndent = DefaultAdditionalIndent) {
        if (!string.IsNullOrEmpty(introduction)) {
            this.AppendLine(introduction!);
        }
        return new IndentationContext(this, additionalIndent);
    }

    /// <summary>
    ///     Creates a new <see cref="IndentationContext"/> with an introduction and
    ///     surrounded by braces.
    /// </summary>
    /// <param name="introduction">String to add before the braces and indentation context.</param>
    /// <param name="additionalIndent">Number of indentation levels to add.</param>
    /// <returns>A new <see cref="IndentationContext"/>.</returns>
    public IDisposable NewBracesScope(string? introduction = null, uint additionalIndent = DefaultAdditionalIndent) {
        if (!string.IsNullOrEmpty(introduction)) {
            this.Append(introduction!);
        }
        this.AppendLine(string.IsNullOrEmpty(introduction) ? "{" : " {");
        return new IndentationContext(this, additionalIndent: additionalIndent, endLine: "}");
    }

    /// <summary>Returns the built-up code.</summary>
    /// <returns>The built-up code.</returns>
    public string GetCode() => _code.ToString();
}
