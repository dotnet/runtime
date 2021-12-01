// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Xml;

namespace System.Configuration
{
    // A utility class for writing XML to a TextWriter.
    //
    // When this class is used to copy an XML document that may include a "<!DOCTYPE" directive,
    // we must track what is written until the "<!DOCTYPE" or first document element is found.
    // This is needed because the XML reader does not give us accurate spacing information
    // for the beginning of the "<!DOCTYPE" element.
    //
    // Note that tracking this information is expensive, as it requires a scan of everything that is written
    // until "<!DOCTYPE" or the first element is found.
    //
    // Note also that this class is used at runtime to copy sections, so performance of all
    // writing functions directly affects application startup time.
    internal sealed class XmlUtilWriter
    {
        private const char Space = ' ';
        private const string NewLine = "\r\n";

        private static readonly string s_spaces8 = new string(Space, 8);
        private static readonly string s_spaces4 = new string(Space, 4);
        private static readonly string s_spaces2 = new string(Space, 2);
        private readonly Stream _baseStream; // stream under TextWriter when tracking position
        private object _lineStartCheckpoint; // checkpoint taken at the start of each line

        internal XmlUtilWriter(TextWriter writer, bool trackPosition)
        {
            Writer = writer;
            TrackPosition = trackPosition;
            LineNumber = 1;
            LinePosition = 1;
            IsLastLineBlank = true;

            if (TrackPosition)
            {
                _baseStream = ((StreamWriter)Writer).BaseStream;
                _lineStartCheckpoint = CreateStreamCheckpoint();
            }
        }

        internal TextWriter Writer { get; }

        internal bool TrackPosition { get; }

        internal int LineNumber { get; private set; }

        internal int LinePosition { get; private set; }

        internal bool IsLastLineBlank { get; private set; }

        // Update the position after the character is written to the stream.
        private void UpdatePosition(char ch)
        {
            switch (ch)
            {
                case '\r':
                    LineNumber++;
                    LinePosition = 1;
                    IsLastLineBlank = true;
                    break;
                case '\n':
                    _lineStartCheckpoint = CreateStreamCheckpoint();
                    break;
                case Space:
                case '\t':
                    LinePosition++;
                    break;
                default:
                    LinePosition++;
                    IsLastLineBlank = false;
                    break;
            }
        }

        // Write a string to _writer.
        // If we are tracking position, determine the line number and position
        internal int Write(string s)
        {
            if (TrackPosition)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char ch = s[i];
                    Writer.Write(ch);
                    UpdatePosition(ch);
                }
            }
            else Writer.Write(s);

            return s.Length;
        }

        // Write a character to _writer.
        // If we are tracking position, determine the line number and position
        internal int Write(char ch)
        {
            Writer.Write(ch);
            if (TrackPosition) UpdatePosition(ch);
            return 1;
        }

        internal void Flush()
        {
            Writer.Flush();
        }

        // Escape a text string
        internal int AppendEscapeTextString(string s)
        {
            return AppendEscapeXmlString(s, false, 'A');
        }

        // Escape a XML string to preserve XML markup.
        internal int AppendEscapeXmlString(string s, bool inAttribute, char quoteChar)
        {
            int charactersWritten = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];

                bool appendCharEntity = false;
                string entityRef = null;
                if (((ch < 32) && (ch != '\t') && (ch != '\r') && (ch != '\n')) || (ch > 0xFFFD))
                    appendCharEntity = true;
                else
                {
                    switch (ch)
                    {
                        case '<':
                            entityRef = "lt";
                            break;
                        case '>':
                            entityRef = "gt";
                            break;
                        case '&':
                            entityRef = "amp";
                            break;
                        case '\'':
                            if (inAttribute && (quoteChar == ch)) entityRef = "apos";
                            break;
                        case '"':
                            if (inAttribute && (quoteChar == ch)) entityRef = "quot";
                            break;
                        case '\n':
                        case '\r':
                            appendCharEntity = inAttribute;
                            break;
                    }
                }

                if (appendCharEntity) charactersWritten += AppendCharEntity(ch);
                else
                {
                    if (entityRef != null) charactersWritten += AppendEntityRef(entityRef);
                    else charactersWritten += Write(ch);
                }
            }

            return charactersWritten;
        }

        internal int AppendEntityRef(string entityRef)
        {
            Write('&');
            Write(entityRef);
            Write(';');
            return entityRef.Length + 2;
        }

        internal int AppendCharEntity(char ch)
        {
            string numberToWrite = ((int)ch).ToString("X");
            Write('&');
            Write('#');
            Write('x');
            Write(numberToWrite);
            Write(';');
            return numberToWrite.Length + 4;
        }

        internal int AppendCData(string cdata)
        {
            Write("<![CDATA[");
            Write(cdata);
            Write("]]>");
            return cdata.Length + 12;
        }

        internal int AppendProcessingInstruction(string name, string value)
        {
            Write("<?");
            Write(name);
            AppendSpace();
            Write(value);
            Write("?>");
            return name.Length + value.Length + 5;
        }

        internal int AppendComment(string comment)
        {
            Write("<!--");
            Write(comment);
            Write("-->");
            return comment.Length + 7;
        }

        internal int AppendAttributeValue(XmlTextReader reader)
        {
            int charactersWritten = 0;
            char quote = reader.QuoteChar;

            // In !DOCTYPE, quote is '\0' for second public attribute.
            // Protect ourselves from writing invalid XML by always
            // supplying a valid quote char.
            if ((quote != '"') && (quote != '\'')) quote = '"';

            charactersWritten += Write(quote);
            while (reader.ReadAttributeValue())
                if (reader.NodeType == XmlNodeType.Text)
                    charactersWritten += AppendEscapeXmlString(reader.Value, true, quote);
                else charactersWritten += AppendEntityRef(reader.Name);

            charactersWritten += Write(quote);
            return charactersWritten;
        }

        // Append whitespace, ensuring there is at least one space.
        internal int AppendRequiredWhiteSpace(int fromLineNumber, int fromLinePosition, int toLineNumber,
            int toLinePosition)
        {
            int charactersWritten = AppendWhiteSpace(fromLineNumber, fromLinePosition, toLineNumber, toLinePosition);
            if (charactersWritten == 0) charactersWritten += AppendSpace();

            return charactersWritten;
        }

        internal int AppendWhiteSpace(int fromLineNumber, int fromLinePosition, int toLineNumber, int toLinePosition)
        {
            int charactersWritten = 0;
            while (fromLineNumber++ < toLineNumber)
            {
                charactersWritten += AppendNewLine();
                fromLinePosition = 1;
            }

            charactersWritten += AppendSpaces(toLinePosition - fromLinePosition);
            return charactersWritten;
        }

        // Append indent
        //      linePosition - starting line position
        //      indent - number of spaces to indent each unit of depth
        //      depth - depth to indent
        //      newLine - insert new line before indent?
        internal int AppendIndent(int linePosition, int indent, int depth, bool newLine)
        {
            int charactersWritten = 0;
            if (newLine) charactersWritten += AppendNewLine();

            int c = linePosition - 1 + indent * depth;
            charactersWritten += AppendSpaces(c);
            return charactersWritten;
        }

        // Write spaces up to the line position, taking into account the
        // current line position of the writer.
        internal int AppendSpacesToLinePosition(int linePosition)
        {
            if (linePosition <= 0) return 0;

            int delta = linePosition - LinePosition;
            if ((delta < 0) && IsLastLineBlank) SeekToLineStart();

            return AppendSpaces(linePosition - LinePosition);
        }

        internal int AppendNewLine()
        {
            return Write(NewLine);
        }

        // Write spaces to the writer provided.  Since we do not want waste
        // memory by allocating do not use "new String(' ', count)".
        internal int AppendSpaces(int count)
        {
            int c = count;
            while (c > 0)
                if (c >= 8)
                {
                    Write(s_spaces8);
                    c -= 8;
                }
                else
                {
                    if (c >= 4)
                    {
                        Write(s_spaces4);
                        c -= 4;
                    }
                    else
                    {
                        if (c >= 2)
                        {
                            Write(s_spaces2);
                            c -= 2;
                        }
                        else
                        {
                            Write(Space);
                            break;
                        }
                    }
                }

            return count > 0 ? count : 0;
        }

        internal int AppendSpace()
        {
            return Write(Space);
        }

        // Reset the stream to the beginning of the current blank line.
        internal void SeekToLineStart()
        {
            RestoreStreamCheckpoint(_lineStartCheckpoint);
        }

        // Create a checkpoint that can be restored with RestoreStreamCheckpoint().
        internal object CreateStreamCheckpoint()
        {
            return new StreamWriterCheckpoint(this);
        }

        // Restore the writer state that was recorded with CreateStreamCheckpoint().
        internal void RestoreStreamCheckpoint(object o)
        {
            StreamWriterCheckpoint checkpoint = (StreamWriterCheckpoint)o;

            Flush();

            LineNumber = checkpoint._lineNumber;
            LinePosition = checkpoint._linePosition;
            IsLastLineBlank = checkpoint._isLastLineBlank;

            _baseStream.Seek(checkpoint._streamPosition, SeekOrigin.Begin);
            _baseStream.SetLength(checkpoint._streamLength);
            _baseStream.Flush();
        }

        // Class that contains the state of the writer and its underlying stream.
        private sealed class StreamWriterCheckpoint
        {
            internal readonly bool _isLastLineBlank;
            internal readonly int _lineNumber;
            internal readonly int _linePosition;
            internal readonly long _streamLength;
            internal readonly long _streamPosition;

            internal StreamWriterCheckpoint(XmlUtilWriter writer)
            {
                writer.Flush();
                _lineNumber = writer.LineNumber;
                _linePosition = writer.LinePosition;
                _isLastLineBlank = writer.IsLastLineBlank;

                writer._baseStream.Flush();
                _streamPosition = writer._baseStream.Position;
                _streamLength = writer._baseStream.Length;
            }
        }
    }
}
