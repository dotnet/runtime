' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.FileIO

    ''' <summary>
    '''  Enables parsing very large delimited or fixed width field files
    ''' </summary>
    ''' <remarks></remarks>
    Public Class TextFieldParser
        Implements IDisposable

        ''' <summary>
        '''  Creates a new TextFieldParser to parse the passed in file
        ''' </summary>
        ''' <param name="path">The path of the file to be parsed</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal path As String)

            ' Default to UTF-8 and detect encoding
            InitializeFromPath(path, System.Text.Encoding.UTF8, True)
        End Sub

        ''' <summary>
        '''  Creates a new TextFieldParser to parse the passed in file
        ''' </summary>
        ''' <param name="path">The path of the file to be parsed</param>
        ''' <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal path As String, ByVal defaultEncoding As System.Text.Encoding)

            ' Default to detect encoding
            InitializeFromPath(path, defaultEncoding, True)
        End Sub

        ''' <summary>
        '''  Creates a new TextFieldParser to parse the passed in file
        ''' </summary>
        ''' <param name="path">The path of the file to be parsed</param>
        ''' <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        ''' <param name="detectEncoding">Indicates whether or not to try to detect the encoding from the BOM</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal path As String, ByVal defaultEncoding As System.Text.Encoding, ByVal detectEncoding As Boolean)

            InitializeFromPath(path, defaultEncoding, detectEncoding)
        End Sub

        ''' <summary>
        '''  Creates a new TextFieldParser to parse a file represented by the passed in stream
        ''' </summary>
        ''' <param name="stream"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal stream As Stream)

            ' Default to UTF-8 and detect encoding
            InitializeFromStream(stream, System.Text.Encoding.UTF8, True)
        End Sub

        ''' <summary>
        '''  Creates a new TextFieldParser to parse a file represented by the passed in stream
        ''' </summary>
        ''' <param name="stream"></param>
        ''' <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal stream As Stream, ByVal defaultEncoding As System.Text.Encoding)

            ' Default to detect encoding
            InitializeFromStream(stream, defaultEncoding, True)
        End Sub

        ''' <summary>
        '''  Creates a new TextFieldParser to parse a file represented by the passed in stream
        ''' </summary>
        ''' <param name="stream"></param>
        ''' <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        ''' <param name="detectEncoding">Indicates whether or not to try to detect the encoding from the BOM</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal stream As Stream, ByVal defaultEncoding As System.Text.Encoding, ByVal detectEncoding As Boolean)

            InitializeFromStream(stream, defaultEncoding, detectEncoding)
        End Sub

        ''' <summary>
        '''  Creates a new TextFieldParser to parse a file represented by the passed in stream
        ''' </summary>
        ''' <param name="stream"></param>
        ''' <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        ''' <param name="detectEncoding">Indicates whether or not to try to detect the encoding from the BOM</param>
        ''' <param name="leaveOpen">Indicates whether or not to leave the passed in stream open</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal stream As Stream, ByVal defaultEncoding As System.Text.Encoding, ByVal detectEncoding As Boolean, ByVal leaveOpen As Boolean)

            m_LeaveOpen = leaveOpen
            InitializeFromStream(stream, defaultEncoding, detectEncoding)
        End Sub

        ''' <summary>
        '''  Creates a new TextFieldParser to parse a stream or file represented by the passed in TextReader
        ''' </summary>
        ''' <param name="reader">The TextReader that does the reading</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal reader As TextReader)

            If reader Is Nothing Then
                Throw GetArgumentNullException("reader")
            End If

            m_Reader = reader

            ReadToBuffer()

        End Sub

        ''' <summary>
        '''  An array of the strings that indicate a line is a comment
        ''' </summary>
        ''' <value>An array of comment indicators</value>
        ''' <remarks>Returns an empty array if not set</remarks>
#Disable Warning CA1819 ' Properties should not return arrays
        <EditorBrowsable(EditorBrowsableState.Advanced)>
        Public Property CommentTokens() As String()
#Enable Warning CA1819 ' Properties should not return arrays
            Get
                Return m_CommentTokens
            End Get
            Set(ByVal value As String())
                CheckCommentTokensForWhitespace(value)
                m_CommentTokens = value
                m_NeedPropertyCheck = True
            End Set
        End Property

        ''' <summary>
        '''  Indicates whether or not there is any data (non ignorable lines) left to read in the file
        ''' </summary>
        ''' <value>True if there's more data to read, otherwise False</value>
        ''' <remarks>Ignores comments and blank lines</remarks>
        Public ReadOnly Property EndOfData() As Boolean
            Get
                If m_EndOfData Then
                    Return m_EndOfData
                End If

                ' Make sure we're not at end of file
                If m_Reader Is Nothing Or m_Buffer Is Nothing Then
                    m_EndOfData = True
                    Return True
                End If

                'See if we can get a data line
                If PeekNextDataLine() IsNot Nothing Then
                    Return False
                End If

                m_EndOfData = True
                Return True
            End Get
        End Property

        ''' <summary>
        '''  The line to the right of the cursor.
        ''' </summary>
        ''' <value>The number of the line</value>
        ''' <remarks>LineNumber returns the location in the file and has nothing to do with rows or fields</remarks>
        <EditorBrowsable(EditorBrowsableState.Advanced)>
        Public ReadOnly Property LineNumber() As Long
            Get
                If m_LineNumber <> -1 Then

                    ' See if we're at the end of file
                    If m_Reader.Peek = -1 And m_Position = m_CharsRead Then
                        CloseReader()
                    End If
                End If

                Return m_LineNumber
            End Get
        End Property

        ''' <summary>
        '''  Returns the last malformed line if there is one.
        ''' </summary>
        ''' <value>The last malformed line</value>
        ''' <remarks></remarks>
        Public ReadOnly Property ErrorLine() As String
            Get
                Return m_ErrorLine
            End Get
        End Property

        ''' <summary>
        '''  Returns the line number of last malformed line if there is one.
        ''' </summary>
        ''' <value>The last malformed line number</value>
        ''' <remarks></remarks>
        Public ReadOnly Property ErrorLineNumber() As Long
            Get
                Return m_ErrorLineNumber
            End Get
        End Property

        ''' <summary>
        '''  Indicates the type of file being read, either fixed width or delimited
        ''' </summary>
        ''' <value>The type of fields in the file</value>
        ''' <remarks></remarks>
        Public Property TextFieldType() As FieldType
            Get
                Return m_TextFieldType
            End Get
            Set(ByVal value As FieldType)
                ValidateFieldTypeEnumValue(value, "value")
                m_TextFieldType = value
                m_NeedPropertyCheck = True
            End Set
        End Property

        ''' <summary>
        '''  Gets or sets the widths of the fields for reading a fixed width file
        ''' </summary>
        ''' <value>An array of the widths</value>
        ''' <remarks></remarks>
#Disable Warning CA1819 ' Properties should not return arrays
        Public Property FieldWidths() As Integer()
#Enable Warning CA1819 ' Properties should not return arrays
            Get
                Return m_FieldWidths
            End Get
            Set(ByVal value As Integer())
                If value IsNot Nothing Then
                    ValidateFieldWidthsOnInput(value)

                    ' Keep a copy so we can determine if the user changes elements of the array
                    m_FieldWidthsCopy = DirectCast(value.Clone(), Integer())
                Else
                    m_FieldWidthsCopy = Nothing
                End If

                m_FieldWidths = value
                m_NeedPropertyCheck = True
            End Set
        End Property

        ''' <summary>
        '''  Gets or sets the delimiters used in a file
        ''' </summary>
        ''' <value>An array of the delimiters</value>
        ''' <remarks></remarks>
#Disable Warning CA1819 ' Properties should not return arrays
        Public Property Delimiters() As String()
#Enable Warning CA1819 ' Properties should not return arrays
            Get
                Return m_Delimiters
            End Get
            Set(ByVal value As String())
                If value IsNot Nothing Then
                    ValidateDelimiters(value)

                    ' Keep a copy so we can determine if the user changes elements of the array
                    m_DelimitersCopy = DirectCast(value.Clone(), String())
                Else
                    m_DelimitersCopy = Nothing
                End If

                m_Delimiters = value

                m_NeedPropertyCheck = True

                ' Force rebuilding of regex
                m_BeginQuotesRegex = Nothing

            End Set
        End Property

        ''' <summary>
        ''' Helper function to enable setting delimiters without diming an array
        ''' </summary>
        ''' <param name="delimiters">A list of the delimiters</param>
        ''' <remarks></remarks>
        Public Sub SetDelimiters(ByVal ParamArray delimiters As String())
            Me.Delimiters = delimiters
        End Sub

        ''' <summary>
        ''' Helper function to enable setting field widths without diming an array
        ''' </summary>
        ''' <param name="fieldWidths">A list of field widths</param>
        ''' <remarks></remarks>
        Public Sub SetFieldWidths(ByVal ParamArray fieldWidths As Integer())
            Me.FieldWidths = fieldWidths
        End Sub

        ''' <summary>
        '''  Indicates whether or not leading and trailing white space should be removed when returning a field
        ''' </summary>
        ''' <value>True if white space should be removed, otherwise False</value>
        ''' <remarks></remarks>
        Public Property TrimWhiteSpace() As Boolean
            Get
                Return m_TrimWhiteSpace
            End Get
            Set(ByVal value As Boolean)
                m_TrimWhiteSpace = value
            End Set
        End Property

        ''' <summary>
        '''  Reads and returns the next line from the file
        ''' </summary>
        ''' <returns>The line read or Nothing if at the end of the file</returns>
        ''' <remarks>This is data unaware method. It simply reads the next line in the file.</remarks>
        <EditorBrowsable(EditorBrowsableState.Advanced)>
        Public Function ReadLine() As String
            If m_Reader Is Nothing Or m_Buffer Is Nothing Then
                Return Nothing
            End If

            Dim Line As String

            ' Set the method to be used when we reach the end of the buffer
            Dim BufferFunction As New ChangeBufferFunction(AddressOf ReadToBuffer)

            Line = ReadNextLine(m_Position, BufferFunction)

            If Line Is Nothing Then
                FinishReading()
                Return Nothing
            Else
                m_LineNumber += 1
                Return Line.TrimEnd(Chr(13), Chr(10))
            End If
        End Function

        ''' <summary>
        '''  Reads a non ignorable line and parses it into fields
        ''' </summary>
        ''' <returns>The line parsed into fields</returns>
        ''' <remarks>This is a data aware method. Comments and blank lines are ignored.</remarks>
        Public Function ReadFields() As String()
            If m_Reader Is Nothing Or m_Buffer Is Nothing Then
                Return Nothing
            End If

            ValidateReadyToRead()

            Select Case m_TextFieldType
                Case FieldType.FixedWidth
                    Return ParseFixedWidthLine()
                Case FieldType.Delimited
                    Return ParseDelimitedLine()
                Case Else
                    Debug.Fail("The TextFieldType is not supported")
            End Select
            Return Nothing
        End Function

        ''' <summary>
        '''  Enables looking at the passed in number of characters of the next data line without reading the line
        ''' </summary>
        ''' <param name="numberOfChars"></param>
        ''' <returns>A string consisting of the first NumberOfChars characters of the next line</returns>
        ''' <remarks>If numberOfChars is greater than the next line, only the next line is returned</remarks>
        Public Function PeekChars(ByVal numberOfChars As Integer) As String

            If numberOfChars <= 0 Then
                Throw GetArgumentExceptionWithArgName("numberOfChars", SR.TextFieldParser_NumberOfCharsMustBePositive, "numberOfChars")
            End If

            If m_Reader Is Nothing Or m_Buffer Is Nothing Then
                Return Nothing
            End If

            ' If we know there's no more data return Nothing
            If m_EndOfData Then
                Return Nothing
            End If

            ' Get the next line without reading it
            Dim Line As String = PeekNextDataLine()

            If Line Is Nothing Then
                m_EndOfData = True
                Return Nothing
            End If

            ' Strip of end of line chars
            Line = Line.TrimEnd(Chr(13), Chr(10))

            ' If the number of chars is larger than the line, return the whole line. Otherwise
            ' return the NumberOfChars characters from the beginning of the line
            If Line.Length < numberOfChars Then
                Return Line
            Else
                Dim info As New StringInfo(Line)
                Return info.SubstringByTextElements(0, numberOfChars)
            End If

        End Function

        ''' <summary>
        '''  Reads the file starting at the current position and moving to the end of the file
        ''' </summary>
        ''' <returns>The contents of the file from the current position to the end of the file</returns>
        ''' <remarks>This is not a data aware method. Everything in the file from the current position to the end is read</remarks>
        <EditorBrowsable(EditorBrowsableState.Advanced)>
        Public Function ReadToEnd() As String

            If m_Reader Is Nothing Or m_Buffer Is Nothing Then
                Return Nothing
            End If

            Dim Builder As New System.Text.StringBuilder(m_Buffer.Length)

            ' Get the lines in the Buffer first
            Builder.Append(m_Buffer, m_Position, m_CharsRead - m_Position)

            ' Add what we haven't read
            Builder.Append(m_Reader.ReadToEnd())

            FinishReading()

            Return Builder.ToString()

        End Function

        ''' <summary>
        '''  Indicates whether or not to handle quotes in a csv friendly way
        ''' </summary>
        ''' <value>True if we escape quotes otherwise false</value>
        ''' <remarks></remarks>
        <EditorBrowsable(EditorBrowsableState.Advanced)>
        Public Property HasFieldsEnclosedInQuotes() As Boolean
            Get
                Return m_HasFieldsEnclosedInQuotes
            End Get
            Set(ByVal value As Boolean)
                m_HasFieldsEnclosedInQuotes = value
            End Set
        End Property

        ''' <summary>
        '''  Closes the StreamReader
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub Close()
            CloseReader()
        End Sub

        ''' <summary>
        '''  Closes the StreamReader
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub Dispose() Implements System.IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        ''' <summary>
        ''' Standard implementation of IDisposable.Dispose for non sealed classes. Classes derived from
        ''' TextFieldParser should override this method. After doing their own cleanup, they should call
        ''' this method (MyBase.Dispose(disposing))
        ''' </summary>
        ''' <param name="disposing">Indicates we are called by Dispose and not GC</param>
        ''' <remarks></remarks>
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not Me.m_Disposed Then
                    Close()
                End If
                Me.m_Disposed = True
            End If
        End Sub

        ''' <summary>
        ''' Validates that the value being passed as an FieldType is a legal value
        ''' </summary>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Sub ValidateFieldTypeEnumValue(ByVal value As FieldType, ByVal paramName As String)
            If value < FieldType.Delimited OrElse value > FieldType.FixedWidth Then
                Throw New System.ComponentModel.InvalidEnumArgumentException(paramName, DirectCast(value, Integer), GetType(FieldType))
            End If
        End Sub

        ''' <summary>
        ''' Clean up following dispose pattern
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Finalize()
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(False)
            MyBase.Finalize()
        End Sub

        ''' <summary>
        '''  Closes the StreamReader
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CloseReader()

            FinishReading()
            If m_Reader IsNot Nothing Then
                If Not m_LeaveOpen Then
                    m_Reader.Close()
                End If
                m_Reader = Nothing
            End If
        End Sub

        ''' <summary>
        '''  Cleans up managed resources except the StreamReader and indicates reading is finished
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub FinishReading()

            m_LineNumber = -1
            m_EndOfData = True
            m_Buffer = Nothing
            m_DelimiterRegex = Nothing
            m_BeginQuotesRegex = Nothing

        End Sub

        ''' <summary>
        '''  Creates a StreamReader for the passed in Path
        ''' </summary>
        ''' <param name="path">The passed in path</param>
        ''' <param name="defaultEncoding">The encoding to default to if encoding can't be detected</param>
        ''' <param name="detectEncoding">Indicates whether or not to detect encoding from the BOM</param>
        ''' <remarks>We validate the arguments here for the three Public constructors that take a Path</remarks>
        Private Sub InitializeFromPath(ByVal path As String, ByVal defaultEncoding As System.Text.Encoding, ByVal detectEncoding As Boolean)

            If path = "" Then
                Throw GetArgumentNullException("path")
            End If

            If defaultEncoding Is Nothing Then
                Throw GetArgumentNullException("defaultEncoding")
            End If

            Dim fullPath As String = ValidatePath(path)
            Dim fileStreamTemp As New FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            m_Reader = New StreamReader(fileStreamTemp, defaultEncoding, detectEncoding)

            ReadToBuffer()

        End Sub

        ''' <summary>
        '''  Creates a StreamReader for a passed in stream
        ''' </summary>
        ''' <param name="stream">The passed in stream</param>
        ''' <param name="defaultEncoding">The encoding to default to if encoding can't be detected</param>
        ''' <param name="detectEncoding">Indicates whether or not to detect encoding from the BOM</param>
        ''' <remarks>We validate the arguments here for the three Public constructors that take a Stream</remarks>
        Private Sub InitializeFromStream(ByVal stream As Stream, ByVal defaultEncoding As System.Text.Encoding, ByVal detectEncoding As Boolean)

            If stream Is Nothing Then
                Throw GetArgumentNullException("stream")
            End If

            If Not stream.CanRead Then
                Throw GetArgumentExceptionWithArgName("stream", SR.TextFieldParser_StreamNotReadable, "stream")
            End If

            If defaultEncoding Is Nothing Then
                Throw GetArgumentNullException("defaultEncoding")
            End If

            m_Reader = New StreamReader(stream, defaultEncoding, detectEncoding)

            ReadToBuffer()
        End Sub

        ''' <summary>
        '''  Gets full name and path from passed in path.
        ''' </summary>
        ''' <param name="path">The path to be validated</param>
        ''' <returns>The full name and path</returns>
        ''' <remarks>Throws if the file doesn't exist or if the path is malformed</remarks>
        Private Function ValidatePath(ByVal path As String) As String

            ' Validate and get full path
            Dim fullPath As String = FileSystem.NormalizeFilePath(path, "path")

            ' Make sure the file exists
            If Not File.Exists(fullPath) Then
                Throw New IO.FileNotFoundException(GetResourceString(SR.IO_FileNotFound_Path, fullPath))
            End If

            Return fullPath
        End Function

        ''' <summary>
        '''  Indicates whether or not the passed in line should be ignored
        ''' </summary>
        ''' <param name="line">The line to be tested</param>
        ''' <returns>True if the line should be ignored, otherwise False</returns>
        ''' <remarks>Lines to ignore are blank lines and comments</remarks>
        Private Function IgnoreLine(ByVal line As String) As Boolean

            ' If the Line is Nothing, it has meaning (we've reached the end of the file) so don't
            ' ignore it
            If line Is Nothing Then
                Return False
            End If

            ' Ignore empty or whitespace lines
            Dim TrimmedLine As String = line.Trim()
            If TrimmedLine.Length = 0 Then
                Return True
            End If

            ' Ignore comments
            If m_CommentTokens IsNot Nothing Then
                For Each Token As String In m_CommentTokens
                    If Token = "" Then
                        Continue For
                    End If

                    If TrimmedLine.StartsWith(Token, StringComparison.Ordinal) Then
                        Return True
                    End If

                    ' Test original line in case whitespace char is a comment token
                    If line.StartsWith(Token, StringComparison.Ordinal) Then
                        Return True
                    End If
                Next
            End If

            Return False

        End Function

        ''' <summary>
        '''  Reads characters from the file into the buffer
        ''' </summary>
        ''' <returns>The number of Chars read. If no Chars are read, we're at the end of the file</returns>
        ''' <remarks></remarks>
        Private Function ReadToBuffer() As Integer

            Debug.Assert(m_Buffer IsNot Nothing, "There's no buffer")
            Debug.Assert(m_Reader IsNot Nothing, "There's no StreamReader")

            ' Set cursor to beginning of buffer
            m_Position = 0
            Dim BufferLength As Integer = m_Buffer.Length
            Debug.Assert(BufferLength >= DEFAULT_BUFFER_LENGTH, "Buffer shrunk to below default")

            ' If the buffer has grown, shrink it back to the default size
            If BufferLength > DEFAULT_BUFFER_LENGTH Then
                BufferLength = DEFAULT_BUFFER_LENGTH
                ReDim m_Buffer(BufferLength - 1)
            End If

            ' Read from the stream
            m_CharsRead = m_Reader.Read(m_Buffer, 0, BufferLength)

            ' Return the number of Chars read
            Return m_CharsRead
        End Function

        ''' <summary>
        '''  Moves the cursor and all the data to the right of the cursor to the front of the buffer. It
        '''  then fills the remainder of the buffer from the file
        ''' </summary>
        ''' <returns>The number of Chars read in filling the remainder of the buffer</returns>
        ''' <remarks>
        '''  This should be called when we want to make maximum use of the space in the buffer. Characters
        '''  to the left of the cursor have already been read and can be discarded.
        '''</remarks>
        Private Function SlideCursorToStartOfBuffer() As Integer

            Debug.Assert(m_Buffer IsNot Nothing, "There's no buffer")
            Debug.Assert(m_Reader IsNot Nothing, "There's no StreamReader")
            Debug.Assert(m_Position >= 0 And m_Position <= m_Buffer.Length, "The cursor is out of range")

            ' No need to slide if we're already at the beginning
            If m_Position > 0 Then
                Dim BufferLength As Integer = m_Buffer.Length
                Dim TempArray(BufferLength - 1) As Char
                Array.Copy(m_Buffer, m_Position, TempArray, 0, BufferLength - m_Position)

                ' Fill the rest of the buffer
                Dim CharsRead As Integer = m_Reader.Read(TempArray, BufferLength - m_Position, m_Position)
                m_CharsRead = m_CharsRead - m_Position + CharsRead

                m_Position = 0
                m_Buffer = TempArray

                Return CharsRead
            End If

            Return 0
        End Function

        ''' <summary>
        '''  Increases the size of the buffer. Used when we are at the end of the buffer, we need
        '''  to read more data from the file, and we can't discard what we've already read.
        ''' </summary>
        ''' <returns>The number of characters read to fill the new buffer</returns>
        ''' <remarks>This is needed for PeekChars and EndOfData</remarks>
        Private Function IncreaseBufferSize() As Integer

            Debug.Assert(m_Buffer IsNot Nothing, "There's no buffer")
            Debug.Assert(m_Reader IsNot Nothing, "There's no StreamReader")

            ' Set cursor
            m_PeekPosition = m_CharsRead

            ' Create a larger buffer and copy our data into it
            Dim BufferSize As Integer = m_Buffer.Length + DEFAULT_BUFFER_LENGTH

            ' Make sure the buffer hasn't grown too large
            If BufferSize > m_MaxBufferSize Then
                Throw GetInvalidOperationException(SR.TextFieldParser_BufferExceededMaxSize)
            End If

            Dim TempArray(BufferSize - 1) As Char

            Array.Copy(m_Buffer, TempArray, m_Buffer.Length)
            Dim CharsRead As Integer = m_Reader.Read(TempArray, m_Buffer.Length, DEFAULT_BUFFER_LENGTH)
            m_Buffer = TempArray
            m_CharsRead += CharsRead

            Debug.Assert(m_CharsRead <= BufferSize, "We've read more chars than we have space for")

            Return CharsRead
        End Function

        ''' <summary>
        '''  Returns the next line of data or nothing if there's no more data to be read
        ''' </summary>
        ''' <returns>The next line of data</returns>
        ''' <remarks>Moves the cursor past the line read</remarks>
        Private Function ReadNextDataLine() As String

            Dim Line As String

            ' Set function to use when we reach the end of the buffer
            Dim BufferFunction As New ChangeBufferFunction(AddressOf ReadToBuffer)

            Do
                Line = ReadNextLine(m_Position, BufferFunction)
                m_LineNumber += 1
            Loop While IgnoreLine(Line)

            If Line Is Nothing Then
                CloseReader()
            End If

            Return Line

        End Function

        ''' <summary>
        '''  Returns the next data line but doesn't move the cursor
        ''' </summary>
        ''' <returns>The next data line, or Nothing if there's no more data</returns>
        ''' <remarks></remarks>
        Private Function PeekNextDataLine() As String

            Dim Line As String

            ' Set function to use when we reach the end of the buffer
            Dim BufferFunction As New ChangeBufferFunction(AddressOf IncreaseBufferSize)

            ' Slide the data to the left so that we make maximum use of the buffer
            SlideCursorToStartOfBuffer()
            m_PeekPosition = 0

            Do
                Line = ReadNextLine(m_PeekPosition, BufferFunction)
            Loop While IgnoreLine(Line)

            Return Line
        End Function

        ''' <summary>
        '''  Function to call when we're at the end of the buffer. We either re fill the buffer
        '''  or change the size of the buffer
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Delegate Function ChangeBufferFunction() As Integer

        ''' <summary>
        '''  Gets the next line from the file and moves the passed in cursor past the line
        ''' </summary>
        ''' <param name="Cursor">Indicates the current position in the buffer</param>
        ''' <param name="ChangeBuffer">Function to call when we've reached the end of the buffer</param>
        ''' <returns>The next line in the file</returns>
        ''' <remarks>Returns Nothing if we are at the end of the file</remarks>
        Private Function ReadNextLine(ByRef Cursor As Integer, ByVal ChangeBuffer As ChangeBufferFunction) As String

            Debug.Assert(m_Buffer IsNot Nothing, "There's no buffer")
            Debug.Assert(Cursor >= 0 And Cursor <= m_CharsRead, "The cursor is out of range")

            ' Check to see if the cursor is at the end of the chars in the buffer. If it is, re fill the buffer
            If Cursor = m_CharsRead Then
                If ChangeBuffer() = 0 Then

                    ' We're at the end of the file
                    Return Nothing
                End If
            End If

            Dim Builder As StringBuilder = Nothing
            Do
                ' Walk through buffer looking for the end of a line. End of line can be vbLf (\n), vbCr (\r) or vbCrLf (\r\n)
                For i As Integer = Cursor To m_CharsRead - 1

                    Dim Character As Char = m_Buffer(i)
                    If Character = vbCr Or Character = vbLf Then

                        ' We've found the end of a line so add everything we've read so far to the
                        ' builder. We include the end of line char because we need to know what it is
                        ' in case it's embedded in a field.
                        If Builder IsNot Nothing Then
                            Builder.Append(m_Buffer, Cursor, i - Cursor + 1)
                        Else
                            Builder = New StringBuilder(i + 1)
                            Builder.Append(m_Buffer, Cursor, i - Cursor + 1)
                        End If
                        Cursor = i + 1

                        ' See if vbLf should be added as well
                        If Character = vbCr Then
                            If Cursor < m_CharsRead Then
                                If m_Buffer(Cursor) = vbLf Then
                                    Cursor += 1
                                    Builder.Append(vbLf)
                                End If
                            ElseIf ChangeBuffer() > 0 Then
                                If m_Buffer(Cursor) = vbLf Then
                                    Cursor += 1
                                    Builder.Append(vbLf)
                                End If
                            End If
                        End If

                        Return Builder.ToString()
                    End If
                Next i

                ' We've searched the whole buffer and haven't found an end of line. Save what we have, and read more to the buffer.
                Dim Size As Integer = m_CharsRead - Cursor

                If Builder Is Nothing Then
                    Builder = New StringBuilder(Size + DEFAULT_BUILDER_INCREASE)
                End If
                Builder.Append(m_Buffer, Cursor, Size)

            Loop While ChangeBuffer() > 0

            Return Builder.ToString()
        End Function

        ''' <summary>
        '''  Gets the next data line and parses it with the delimiters
        ''' </summary>
        ''' <returns>An array of the fields in the line</returns>
        ''' <remarks></remarks>
        Private Function ParseDelimitedLine() As String()

            Dim Line As String = ReadNextDataLine()
            If Line Is Nothing Then
                Return Nothing
            End If

            ' The line number is that of the line just read
            Dim CurrentLineNumber As Long = m_LineNumber - 1

            Dim Index As Integer = 0
            Dim Fields As New System.Collections.Generic.List(Of String)
            Dim Field As String
            Dim LineEndIndex As Integer = GetEndOfLineIndex(Line)

            While Index <= LineEndIndex

                ' Is the field delimited in quotes? We only care about this if
                ' EscapedQuotes is True
                Dim MatchResult As Match = Nothing
                Dim QuoteDelimited As Boolean = False

                If m_HasFieldsEnclosedInQuotes Then
                    MatchResult = BeginQuotesRegex.Match(Line, Index)
                    QuoteDelimited = MatchResult.Success
                End If

                If QuoteDelimited Then

                    'Move the Index beyond quote
                    Index = MatchResult.Index + MatchResult.Length
                    ' Look for the closing "
                    Dim EndHelper As New QuoteDelimitedFieldBuilder(m_DelimiterWithEndCharsRegex, m_SpaceChars)
                    EndHelper.BuildField(Line, Index)

                    If EndHelper.MalformedLine Then
                        m_ErrorLine = Line.TrimEnd(Chr(13), Chr(10))
                        m_ErrorLineNumber = CurrentLineNumber
                        Throw New MalformedLineException(GetResourceString(SR.TextFieldParser_MalFormedDelimitedLine, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)), CurrentLineNumber)
                    End If

                    If EndHelper.FieldFinished Then
                        Field = EndHelper.Field
                        Index = EndHelper.Index + EndHelper.DelimiterLength
                    Else
                        ' We may have an embedded line end character, so grab next line
                        Dim NewLine As String
                        Dim EndOfLine As Integer

                        Do
                            EndOfLine = Line.Length
                            ' Get the next data line
                            NewLine = ReadNextDataLine()

                            ' If we didn't get a new line, we're at the end of the file so our original line is malformed
                            If NewLine Is Nothing Then
                                m_ErrorLine = Line.TrimEnd(Chr(13), Chr(10))
                                m_ErrorLineNumber = CurrentLineNumber
                                Throw New MalformedLineException(GetResourceString(SR.TextFieldParser_MalFormedDelimitedLine, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)), CurrentLineNumber)
                            End If

                            If Line.Length + NewLine.Length > m_MaxLineSize Then
                                m_ErrorLine = Line.TrimEnd(Chr(13), Chr(10))
                                m_ErrorLineNumber = CurrentLineNumber
                                Throw New MalformedLineException(GetResourceString(SR.TextFieldParser_MaxLineSizeExceeded, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)), CurrentLineNumber)
                            End If

                            Line &= NewLine
                            LineEndIndex = GetEndOfLineIndex(Line)
                            EndHelper.BuildField(Line, EndOfLine)
                            If EndHelper.MalformedLine Then
                                m_ErrorLine = Line.TrimEnd(Chr(13), Chr(10))
                                m_ErrorLineNumber = CurrentLineNumber
                                Throw New MalformedLineException(GetResourceString(SR.TextFieldParser_MalFormedDelimitedLine, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)), CurrentLineNumber)
                            End If
                        Loop Until EndHelper.FieldFinished

                        Field = EndHelper.Field
                        Index = EndHelper.Index + EndHelper.DelimiterLength
                    End If

                    If m_TrimWhiteSpace Then
                        Field = Field.Trim()
                    End If

                    Fields.Add(Field)
                Else
                    ' Find the next delimiter
                    Dim DelimiterMatch As Match = m_DelimiterRegex.Match(Line, Index)
                    If DelimiterMatch.Success Then
                        Field = Line.Substring(Index, DelimiterMatch.Index - Index)

                        If m_TrimWhiteSpace Then
                            Field = Field.Trim()
                        End If

                        Fields.Add(Field)

                        ' Move the index
                        Index = DelimiterMatch.Index + DelimiterMatch.Length
                    Else
                        ' We're at the end of the line so the field consists of all that's left of the line
                        ' minus the end of line chars
                        Field = Line.Substring(Index).TrimEnd(Chr(13), Chr(10))

                        If m_TrimWhiteSpace Then
                            Field = Field.Trim()
                        End If
                        Fields.Add(Field)
                        Exit While
                    End If

                End If
            End While

            Return Fields.ToArray()

        End Function

        ''' <summary>
        '''  Gets the next data line and parses into fixed width fields
        ''' </summary>
        ''' <returns>An array of the fields in the line</returns>
        ''' <remarks></remarks>
        Private Function ParseFixedWidthLine() As String()

            Debug.Assert(m_FieldWidths IsNot Nothing, "No field widths")

            Dim Line As String = ReadNextDataLine()

            If Line Is Nothing Then
                Return Nothing
            End If

            ' Strip off trailing carriage return or line feed
            Line = Line.TrimEnd(Chr(13), Chr(10))

            Dim LineInfo As New StringInfo(Line)
            ValidateFixedWidthLine(LineInfo, m_LineNumber - 1)

            Dim Index As Integer = 0
            Dim Bound As Integer = m_FieldWidths.Length - 1
            Dim Fields(Bound) As String

            For i As Integer = 0 To Bound
                Fields(i) = GetFixedWidthField(LineInfo, Index, m_FieldWidths(i))
                Index += m_FieldWidths(i)
            Next

            Return Fields
        End Function

        ''' <summary>
        '''  Returns the field at the passed in index
        ''' </summary>
        ''' <param name="Line">The string containing the fields</param>
        ''' <param name="Index">The start of the field</param>
        ''' <param name="FieldLength">The length of the field</param>
        ''' <returns>The field</returns>
        ''' <remarks></remarks>
        Private Function GetFixedWidthField(ByVal Line As StringInfo, ByVal Index As Integer, ByVal FieldLength As Integer) As String

            Dim Field As String
            If FieldLength > 0 Then
                Field = Line.SubstringByTextElements(Index, FieldLength)
            Else
                ' Make sure the index isn't past the string
                If Index >= Line.LengthInTextElements Then
                    Field = String.Empty
                Else
                    Field = Line.SubstringByTextElements(Index).TrimEnd(Chr(13), Chr(10))
                End If
            End If

            If m_TrimWhiteSpace Then
                Return Field.Trim()
            Else
                Return Field
            End If
        End Function

        ''' <summary>
        '''  Gets the index of the first end of line character
        ''' </summary>
        ''' <param name="Line"></param>
        ''' <returns></returns>
        ''' <remarks>When there are no end of line characters, the index is the length (one past the end)</remarks>
        Private Function GetEndOfLineIndex(ByVal Line As String) As Integer

            Debug.Assert(Line IsNot Nothing, "We are parsing a Nothing")

            Dim Length As Integer = Line.Length
            Debug.Assert(Length > 0, "A blank line shouldn't be parsed")

            If Length = 1 Then
                Debug.Assert(Line(0) <> vbCr And Line(0) <> vbLf, "A blank line shouldn't be parsed")
                Return Length
            End If

            ' Check the next to last and last char for end line characters
            If Line(Length - 2) = vbCr Or Line(Length - 2) = vbLf Then
                Return Length - 2
            ElseIf Line(Length - 1) = vbCr Or Line(Length - 1) = vbLf Then
                Return Length - 1
            Else
                Return Length
            End If

        End Function

        ''' <summary>
        '''  Indicates whether or not a line is valid
        ''' </summary>
        ''' <param name="Line">The line to be tested</param>
        ''' <param name="LineNumber">The line number, used for exception</param>
        ''' <remarks></remarks>
        Private Sub ValidateFixedWidthLine(ByVal Line As StringInfo, ByVal LineNumber As Long)
            Debug.Assert(Line IsNot Nothing, "No Line sent")

            ' The only malformed line for fixed length fields is one that's too short
            If Line.LengthInTextElements < m_LineLength Then
                m_ErrorLine = Line.String
                m_ErrorLineNumber = m_LineNumber - 1
                Throw New MalformedLineException(GetResourceString(SR.TextFieldParser_MalFormedFixedWidthLine, LineNumber.ToString(CultureInfo.InvariantCulture)), LineNumber)
            End If

        End Sub

        ''' <summary>
        '''  Determines whether or not the field widths are valid, and sets the size of a line
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ValidateFieldWidths()

            If m_FieldWidths Is Nothing Then
                Throw GetInvalidOperationException(SR.TextFieldParser_FieldWidthsNothing)
            End If

            If m_FieldWidths.Length = 0 Then
                Throw GetInvalidOperationException(SR.TextFieldParser_FieldWidthsNothing)
            End If

            Dim WidthBound As Integer = m_FieldWidths.Length - 1
            m_LineLength = 0

            ' add all but the last element
            For i As Integer = 0 To WidthBound - 1
                Debug.Assert(m_FieldWidths(i) > 0, "Bad field width, this should have been caught on input")

                m_LineLength += m_FieldWidths(i)
            Next

            ' add the last field if it's greater than zero (IE not ragged).
            If m_FieldWidths(WidthBound) > 0 Then
                m_LineLength += m_FieldWidths(WidthBound)
            End If

        End Sub

        ''' <summary>
        '''  Checks the field widths at input.
        ''' </summary>
        ''' <param name="Widths"></param>
        ''' <remarks>
        '''  All field widths, except the last one, must be greater than zero. If the last width is
        '''  less than one it indicates the last field is ragged
        '''</remarks>
        Private Sub ValidateFieldWidthsOnInput(ByVal Widths() As Integer)

            Debug.Assert(Widths IsNot Nothing, "There are no field widths")

            Dim Bound As Integer = Widths.Length - 1
            For i As Integer = 0 To Bound - 1
                If Widths(i) < 1 Then
                    Throw GetArgumentExceptionWithArgName("FieldWidths", SR.TextFieldParser_FieldWidthsMustPositive, "FieldWidths")
                End If
            Next
        End Sub

        ''' <summary>
        '''  Validates the delimiters and creates the Regex objects for finding delimiters or quotes followed
        '''  by delimiters
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ValidateAndEscapeDelimiters()
            If m_Delimiters Is Nothing Then
                Throw GetArgumentExceptionWithArgName("Delimiters", SR.TextFieldParser_DelimitersNothing, "Delimiters")
            End If

            If m_Delimiters.Length = 0 Then
                Throw GetArgumentExceptionWithArgName("Delimiters", SR.TextFieldParser_DelimitersNothing, "Delimiters")
            End If

            Dim Length As Integer = m_Delimiters.Length

            Dim Builder As StringBuilder = New StringBuilder()
            Dim QuoteBuilder As StringBuilder = New StringBuilder()

            ' Add ending quote pattern. It will be followed by delimiters resulting in a string like:
            ' "[ ]*(d1|d2|d3)
            QuoteBuilder.Append(EndQuotePattern & "(")
            For i As Integer = 0 To Length - 1
                If m_Delimiters(i) IsNot Nothing Then

                    ' Make sure delimiter is legal
                    If m_HasFieldsEnclosedInQuotes Then
                        If m_Delimiters(i).IndexOf(""""c) > -1 Then
                            Throw GetInvalidOperationException(SR.TextFieldParser_IllegalDelimiter)
                        End If
                    End If

                    Dim EscapedDelimiter As String = Regex.Escape(m_Delimiters(i))

                    Builder.Append(EscapedDelimiter & "|")
                    QuoteBuilder.Append(EscapedDelimiter & "|")
                Else
                    Debug.Fail("Delimiter element is empty. This should have been caught on input")
                End If
            Next

            m_SpaceChars = WhitespaceCharacters

            ' Get rid of trailing | and set regex
            m_DelimiterRegex = New Regex(Builder.ToString(0, Builder.Length - 1), REGEX_OPTIONS)
            Builder.Append(vbCr & "|" & vbLf)
            m_DelimiterWithEndCharsRegex = New Regex(Builder.ToString(), REGEX_OPTIONS)

            ' Add end of line (either Cr, Ln, or nothing) and set regex
            QuoteBuilder.Append(vbCr & "|" & vbLf & ")|""$")
        End Sub

        ''' <summary>
        '''  Checks property settings to ensure we're able to read fields.
        ''' </summary>
        ''' <remarks>Throws if we're not able to read fields with current property settings</remarks>
        Private Sub ValidateReadyToRead()

            If m_NeedPropertyCheck Or ArrayHasChanged() Then
                Select Case m_TextFieldType
                    Case FieldType.Delimited

                        ValidateAndEscapeDelimiters()
                    Case FieldType.FixedWidth

                        ' Check FieldWidths
                        ValidateFieldWidths()

                    Case Else
                        Debug.Fail("Unknown TextFieldType")
                End Select

                ' Check Comment Tokens
                If m_CommentTokens IsNot Nothing Then
                    For Each Token As String In m_CommentTokens
                        If Token <> "" Then
                            If m_HasFieldsEnclosedInQuotes And m_TextFieldType = FieldType.Delimited Then

                                If String.Compare(Token.Trim(), """", StringComparison.Ordinal) = 0 Then
                                    Throw GetInvalidOperationException(SR.TextFieldParser_InvalidComment)
                                End If
                            End If
                        End If
                    Next
                End If

                m_NeedPropertyCheck = False
            End If
        End Sub

        ''' <summary>
        '''  Throws if any of the delimiters contain line end characters
        ''' </summary>
        ''' <param name="delimiterArray">A string array of delimiters</param>
        ''' <remarks></remarks>
        Private Sub ValidateDelimiters(ByVal delimiterArray() As String)
            If delimiterArray Is Nothing Then
                Return
            End If
            For Each delimiter As String In delimiterArray
                If delimiter = "" Then
                    Throw GetArgumentExceptionWithArgName("Delimiters", SR.TextFieldParser_DelimiterNothing, "Delimiters")
                End If
                If delimiter.IndexOfAny(New Char() {Chr(13), Chr(10)}) > -1 Then
                    Throw GetArgumentExceptionWithArgName("Delimiters", SR.TextFieldParser_EndCharsInDelimiter)
                End If
            Next
        End Sub

        ''' <summary>
        '''  Determines if the FieldWidths or Delimiters arrays have changed.
        ''' </summary>
        ''' <remarks>If the array has changed, we need to re initialize before reading.</remarks>
        Private Function ArrayHasChanged() As Boolean

            Dim lowerBound As Integer = 0
            Dim upperBound As Integer = 0

            Select Case m_TextFieldType
                Case FieldType.Delimited

                    Debug.Assert((m_DelimitersCopy Is Nothing And m_Delimiters Is Nothing) Or (m_DelimitersCopy IsNot Nothing And m_Delimiters IsNot Nothing), "Delimiters and copy are not both Nothing or both not Nothing")

                    ' Check null cases
                    If m_Delimiters Is Nothing Then
                        Return False
                    End If

                    lowerBound = m_DelimitersCopy.GetLowerBound(0)
                    upperBound = m_DelimitersCopy.GetUpperBound(0)

                    For i As Integer = lowerBound To upperBound
                        If m_Delimiters(i) <> m_DelimitersCopy(i) Then
                            Return True
                        End If
                    Next i

                Case FieldType.FixedWidth

                    Debug.Assert((m_FieldWidthsCopy Is Nothing And m_FieldWidths Is Nothing) Or (m_FieldWidthsCopy IsNot Nothing And m_FieldWidths IsNot Nothing), "FieldWidths and copy are not both Nothing or both not Nothing")

                    ' Check null cases
                    If m_FieldWidths Is Nothing Then
                        Return False
                    End If

                    lowerBound = m_FieldWidthsCopy.GetLowerBound(0)
                    upperBound = m_FieldWidthsCopy.GetUpperBound(0)

                    For i As Integer = lowerBound To upperBound
                        If m_FieldWidths(i) <> m_FieldWidthsCopy(i) Then
                            Return True
                        End If
                    Next i

                Case Else
                    Debug.Fail("Unknown TextFieldType")
            End Select

            Return False
        End Function

        ''' <summary>
        '''  Throws if any of the comment tokens contain whitespace
        ''' </summary>
        ''' <param name="tokens">A string array of comment tokens</param>
        ''' <remarks></remarks>
        Private Sub CheckCommentTokensForWhitespace(ByVal tokens() As String)
            If tokens Is Nothing Then
                Return
            End If
            For Each token As String In tokens
                If m_WhiteSpaceRegEx.IsMatch(token) Then
                    Throw GetArgumentExceptionWithArgName("CommentTokens", SR.TextFieldParser_WhitespaceInToken)
                End If
            Next
        End Sub

        ''' <summary>
        '''  Gets the appropriate regex for finding a field beginning with quotes
        ''' </summary>
        ''' <value>The right regex</value>
        ''' <remarks></remarks>
        Private ReadOnly Property BeginQuotesRegex() As Regex
            Get
                If m_BeginQuotesRegex Is Nothing Then
                    ' Get the pattern
                    Dim pattern As String = String.Format(CultureInfo.InvariantCulture, BEGINS_WITH_QUOTE, WhitespacePattern)
                    m_BeginQuotesRegex = New Regex(pattern, REGEX_OPTIONS)
                End If

                Return m_BeginQuotesRegex
            End Get
        End Property

        ''' <summary>
        '''  Gets the appropriate expression for finding ending quote of a field
        ''' </summary>
        ''' <value>The expression</value>
        ''' <remarks></remarks>
        Private ReadOnly Property EndQuotePattern() As String
            Get
                Return String.Format(CultureInfo.InvariantCulture, ENDING_QUOTE, WhitespacePattern)
            End Get
        End Property

        ''' <summary>
        ''' Returns a string containing all the characters which are whitespace for parsing purposes
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property WhitespaceCharacters() As String
            Get
                Dim builder As New StringBuilder
                For Each code As Integer In m_WhitespaceCodes

                    Dim spaceChar As Char = ChrW(code)
                    If Not CharacterIsInDelimiter(spaceChar) Then
                        builder.Append(spaceChar)
                    End If
                Next

                Return builder.ToString()
            End Get
        End Property

        ''' <summary>
        ''' Gets the character set of white-spaces to be used in a regex pattern
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property WhitespacePattern() As String
            Get
                Dim builder As New StringBuilder()
                For Each code As Integer In m_WhitespaceCodes
                    Dim spaceChar As Char = ChrW(code)
                    If Not CharacterIsInDelimiter(spaceChar) Then
                        ' Gives us something like \u00A0
                        builder.Append("\u" & code.ToString("X4", CultureInfo.InvariantCulture))
                    End If
                Next

                Return builder.ToString()
            End Get
        End Property

        ''' <summary>
        ''' Checks to see if the passed in character is in any of the delimiters
        ''' </summary>
        ''' <param name="testCharacter">The character to look for</param>
        ''' <returns>True if the character is found in a delimiter, otherwise false</returns>
        ''' <remarks></remarks>
        Private Function CharacterIsInDelimiter(ByVal testCharacter As Char) As Boolean

            Debug.Assert(m_Delimiters IsNot Nothing, "No delimiters set!")

            For Each delimiter As String In m_Delimiters
                If delimiter.IndexOf(testCharacter) > -1 Then
                    Return True
                End If
            Next
            Return False
        End Function

        ' Indicates reader has been disposed
        Private m_Disposed As Boolean

        ' The internal StreamReader that reads the file
        Private m_Reader As TextReader

        ' An array holding the strings that indicate a line is a comment
        Private m_CommentTokens() As String = New String() {}

        ' The line last read by either ReadLine or ReadFields
        Private m_LineNumber As Long = 1

        ' Flags whether or not there is data left to read. Assume there is at creation
        Private m_EndOfData As Boolean = False

        ' Holds the last malformed line
        Private m_ErrorLine As String = ""

        ' Holds the line number of the last malformed line
        Private m_ErrorLineNumber As Long = -1

        ' Indicates what type of fields are in the file (fixed width or delimited)
        Private m_TextFieldType As FieldType = FieldType.Delimited

        ' An array of the widths of the fields in a fixed width file
        Private m_FieldWidths() As Integer

        ' An array of the delimiters used for the fields in the file
        Private m_Delimiters() As String

        ' Holds a copy of the field widths last set so we can respond to changes in the array
        Private m_FieldWidthsCopy() As Integer

        ' Holds a copy of the field widths last set so we can respond to changes in the array
        Private m_DelimitersCopy() As String

        ' Regular expression used to find delimiters
        Private m_DelimiterRegex As Regex

        ' Regex used with BuildField
        Private m_DelimiterWithEndCharsRegex As Regex

        ' Options used for regular expressions
        Private Const REGEX_OPTIONS As RegexOptions = RegexOptions.CultureInvariant

        ' Codes for whitespace as used by String.Trim excluding line end chars as those are handled separately
        Private m_WhitespaceCodes() As Integer = {&H9, &HB, &HC, &H20, &H85, &HA0, &H1680, &H2000, &H2001, &H2002, &H2003, &H2004, &H2005, &H2006, &H2007, &H2008, &H2009, &H200A, &H200B, &H2028, &H2029, &H3000, &HFEFF}

        ' Regular expression used to find beginning quotes ignore spaces and tabs
        Private m_BeginQuotesRegex As Regex

        ' Regular expression for whitespace
        Private m_WhiteSpaceRegEx As Regex = New Regex("\s", REGEX_OPTIONS)

        ' Indicates whether or not white space should be removed from a returned field
        Private m_TrimWhiteSpace As Boolean = True

        ' The position of the cursor in the buffer
        Private m_Position As Integer = 0

        ' The position of the peek cursor
        Private m_PeekPosition As Integer = 0

        ' The number of chars in the buffer
        Private m_CharsRead As Integer = 0

        ' Indicates that the user has changed properties so that we need to validate before a read
        Private m_NeedPropertyCheck As Boolean = True

        ' The default size for the buffer
        Private Const DEFAULT_BUFFER_LENGTH As Integer = 4096

        ' This is a guess as to how much larger the string builder should be beyond the size of what
        ' we've already read
        Private Const DEFAULT_BUILDER_INCREASE As Integer = 10

        ' Buffer used to hold data read from the file. It holds data that must be read
        ' ahead of the cursor (for PeekChars and EndOfData)
        Private m_Buffer(DEFAULT_BUFFER_LENGTH - 1) As Char

        ' The minimum length for a valid fixed width line
        Private m_LineLength As Integer

        ' Indicates whether or not we handle quotes in a csv appropriate way
        Private m_HasFieldsEnclosedInQuotes As Boolean = True

        ' A string of the chars that count as spaces (used for csv format). The norm is spaces and tabs.
        Private m_SpaceChars As String

        ' The largest size a line can be.
        Private m_MaxLineSize As Integer = 10000000

        ' The largest size the buffer can be
        Private m_MaxBufferSize As Integer = 10000000

        ' Regex pattern to determine if field begins with quotes
        Private Const BEGINS_WITH_QUOTE As String = "\G[{0}]*"""

        ' Regex pattern to find a quote before a delimiter
        Private Const ENDING_QUOTE As String = """[{0}]*"

        ' Indicates passed in stream should be not be closed
        Private m_LeaveOpen As Boolean = False
    End Class

    ''' <summary>
    '''  Enum used to indicate the kind of file being read, either delimited or fixed length
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum FieldType As Integer
        '!!!!!!!!!! Changes to this enum must be reflected in ValidateFieldTypeEnumValue()
        Delimited
        FixedWidth
    End Enum

    ''' <summary>
    '''  Helper class that when passed a line and an index to a quote delimited field
    '''  will build the field and handle escaped quotes
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class QuoteDelimitedFieldBuilder
        ''' <summary>
        '''  Creates an instance of the class and sets some properties
        ''' </summary>
        ''' <param name="DelimiterRegex">The regex used to find any of the delimiters</param>
        ''' <param name="SpaceChars">Characters treated as space (usually space and tab)</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal DelimiterRegex As Regex, ByVal SpaceChars As String)
            m_DelimiterRegex = DelimiterRegex
            m_SpaceChars = SpaceChars
        End Sub

        ''' <summary>
        '''  Indicates whether or not the field has been built.
        ''' </summary>
        ''' <value>True if the field has been built, otherwise False</value>
        ''' <remarks>If the Field has been built, the Field property will return the entire field</remarks>
        Public ReadOnly Property FieldFinished() As Boolean
            Get
                Return m_FieldFinished
            End Get
        End Property

        ''' <summary>
        '''  The field being built
        ''' </summary>
        ''' <value>The field</value>
        ''' <remarks></remarks>
        Public ReadOnly Property Field() As String
            Get
                Return m_Field.ToString()
            End Get
        End Property

        ''' <summary>
        '''  The current index on the line. Used to indicate how much of the line was used to build the field
        ''' </summary>
        ''' <value>The current position on the line</value>
        ''' <remarks></remarks>
        Public ReadOnly Property Index() As Integer
            Get
                Return m_Index
            End Get
        End Property

        ''' <summary>
        '''  The length of the closing delimiter if one was found
        ''' </summary>
        ''' <value>The length of the delimiter</value>
        ''' <remarks></remarks>
        Public ReadOnly Property DelimiterLength() As Integer
            Get
                Return m_DelimiterLength
            End Get
        End Property

        ''' <summary>
        '''  Indicates that the current field breaks the subset of csv rules we enforce
        ''' </summary>
        ''' <value>True if the line is malformed, otherwise False</value>
        ''' <remarks>
        '''  The rules we enforce are:
        '''      Embedded quotes must be escaped
        '''      Only space characters can occur between a delimiter and a quote
        '''</remarks>
        Public ReadOnly Property MalformedLine() As Boolean
            Get
                Return m_MalformedLine
            End Get
        End Property

        ''' <summary>
        '''  Builds a field by walking through the passed in line starting at StartAt
        ''' </summary>
        ''' <param name="Line">The line containing the data</param>
        ''' <param name="StartAt">The index at which we start building the field</param>
        ''' <remarks></remarks>
        Public Sub BuildField(ByVal Line As String, ByVal StartAt As Integer)

            m_Index = StartAt
            Dim Length As Integer = Line.Length

            While m_Index < Length

                If Line(m_Index) = """"c Then

                    ' Are we at the end of the file?
                    If m_Index + 1 = Length Then
                        ' We've found the end of the field
                        m_FieldFinished = True
                        m_DelimiterLength = 1

                        ' Move index past end of line
                        m_Index += 1
                        Return
                    End If
                    ' Check to see if this is an escaped quote
                    If m_Index + 1 < Line.Length And Line(m_Index + 1) = """"c Then
                        m_Field.Append(""""c)
                        m_Index += 2
                        Continue While
                    End If

                    ' Find the next delimiter and make sure everything between the quote and
                    ' the delimiter is ignorable
                    Dim Limit As Integer
                    Dim DelimiterMatch As Match = m_DelimiterRegex.Match(Line, m_Index + 1)
                    If Not DelimiterMatch.Success Then
                        Limit = Length - 1
                    Else
                        Limit = DelimiterMatch.Index - 1
                    End If

                    For i As Integer = m_Index + 1 To Limit
                        If m_SpaceChars.IndexOf(Line(i)) < 0 Then
                            m_MalformedLine = True
                            Return
                        End If
                    Next

                    ' The length of the delimiter is the length of the closing quote (1) + any spaces + the length
                    ' of the delimiter we matched if any
                    m_DelimiterLength = 1 + Limit - m_Index
                    If DelimiterMatch.Success Then
                        m_DelimiterLength += DelimiterMatch.Length
                    End If

                    m_FieldFinished = True
                    Return
                Else
                    m_Field.Append(Line(m_Index))
                    m_Index += 1
                End If
            End While
        End Sub

        ' String builder holding the field
        Private m_Field As New StringBuilder

        ' Indicates m_Field contains the entire field
        Private m_FieldFinished As Boolean

        ' The current index on the field
        Private m_Index As Integer

        ' The length of the closing delimiter if one is found
        Private m_DelimiterLength As Integer

        ' The regular expression used to find the next delimiter
        Private m_DelimiterRegex As Regex

        ' Chars that should be counted as space (and hence ignored if occurring before or after a delimiter
        Private m_SpaceChars As String

        ' Indicates the line breaks the csv rules we enforce
        Private m_MalformedLine As Boolean

    End Class

End Namespace
