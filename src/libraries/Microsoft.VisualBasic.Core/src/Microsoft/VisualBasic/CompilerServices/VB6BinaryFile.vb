' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils
Imports System.Runtime.Versioning
Imports System.Diagnostics.CodeAnalysis

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Friend Class VB6BinaryFile

        '============================================================================
        ' Declarations
        '============================================================================

        Inherits VB6RandomFile

        '============================================================================
        ' Constructor
        '============================================================================
        Public Sub New(ByVal FileName As String, ByVal access As OpenAccess, ByVal share As OpenShare)
            MyBase.New(FileName, access, share, -1)
        End Sub

        ' the implementation of Lock in base class VB6RandomFile does not handle m_lRecordLen=-1
        <UnsupportedOSPlatform("macos")>
        Friend Overloads Overrides Sub Lock(ByVal lStart As Long, ByVal lEnd As Long)
            If lStart > lEnd Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Start"))
            End If

            Dim absRecordLength As Long
            Dim lStartByte As Long
            Dim lLength As Long

            If m_lRecordLen = -1 Then
                ' if record len is -1, then using absolute bytes
                absRecordLength = 1
            Else
                absRecordLength = m_lRecordLen
            End If

            lStartByte = (lStart - 1) * absRecordLength
            lLength = (lEnd - lStart + 1) * absRecordLength

            m_file.Lock(lStartByte, lLength)
        End Sub

        ' see Lock description
        <UnsupportedOSPlatform("macos")>
        Friend Overloads Overrides Sub Unlock(ByVal lStart As Long, ByVal lEnd As Long)
            If lStart > lEnd Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Start"))
            End If

            Dim absRecordLength As Long
            Dim lStartByte As Long
            Dim lLength As Long

            If m_lRecordLen = -1 Then
                ' if record len is -1, then using absolute bytes
                absRecordLength = 1
            Else
                absRecordLength = m_lRecordLen
            End If

            lStartByte = (lStart - 1) * absRecordLength
            lLength = (lEnd - lStart + 1) * absRecordLength
            m_file.Unlock(lStartByte, lLength)
        End Sub

        Public Overrides Function GetMode() As OpenMode
            Return OpenMode.Binary
        End Function

        Friend Overloads Overrides Function Seek() As Long
            'm_file.position is the last read byte as a zero based offset
            'Seek returns the position of the next byte to read
            Return (m_position + 1)
        End Function

        Friend Overloads Overrides Sub Seek(ByVal BaseOnePosition As Long)
            If BaseOnePosition <= 0 Then
                Throw VbMakeException(vbErrors.BadRecordNum)
            End If

            Dim BaseZeroPosition As Long = BaseOnePosition - 1

            m_file.Position = BaseZeroPosition
            m_position = BaseZeroPosition

            If Not m_sr Is Nothing Then
                m_sr.DiscardBufferedData()
            End If
        End Sub

        Friend Overrides Function LOC() As Long
            Return m_position
        End Function

        Friend Overrides Function CanInput() As Boolean
            Return True
        End Function

        Friend Overrides Function CanWrite() As Boolean
            Return True
        End Function

        <RequiresUnreferencedCode("Implementation of Vb6InputFile is unsafe.")>
        Friend Overloads Overrides Sub Input(ByRef Value As Object)
            Value = InputStr()
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As String)
            Value = InputStr()
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Char)
            Dim s As String = InputStr()

            If s.Length > 0 Then
                Value = s.Chars(0)
            Else
                Value = ControlChars.NullChar
            End If
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Boolean)
            Value = BooleanType.FromString(InputStr())
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Byte)
            Value = ByteType.FromObject(InputNum(VariantType.Byte))
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Short)
            Value = ShortType.FromObject(InputNum(VariantType.Short))
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Integer)
            Value = IntegerType.FromObject(InputNum(VariantType.Integer))
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Long)
            Value = LongType.FromObject(InputNum(VariantType.Long))
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Single)
            Value = SingleType.FromObject(InputNum(VariantType.Single))
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Double)
            Value = DoubleType.FromObject(InputNum(VariantType.Double))
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Decimal)
            Value = DecimalType.FromObject(InputNum(VariantType.Decimal))
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Date)
            Value = DateType.FromString(InputStr(), GetCultureInfo())
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As String, Optional ByVal RecordNumber As Long = 0, Optional ByVal StringIsFixedLength As Boolean = False)
            ValidateWriteable()

            PutString(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As String, Optional ByVal RecordNumber As Long = 0, Optional ByVal StringIsFixedLength As Boolean = False)
            ValidateReadable()

            Dim ByteLength As Integer
            If Value Is Nothing Then
                ByteLength = 0
            Else
                Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
                ByteLength = m_Encoding.GetByteCount(Value)
            End If
            Value = GetFixedLengthString(RecordNumber, ByteLength)
        End Sub

        Protected Overrides Function InputStr() As String
            Dim lChar As Integer

            ' The NullReferenceException is for compatibility with VB6 which threw a NullReferenceException when
            ' reading from a file that was write-only. The inner exception was added to provide more context.
            If (m_access <> OpenAccess.ReadWrite) AndAlso (m_access <> OpenAccess.Read) Then
                Dim JustNeedTheMessage As New NullReferenceException ' We don't have access to the localized resources for this string.
                Throw New NullReferenceException(JustNeedTheMessage.Message, New IO.IOException(SR.FileOpenedNoRead))
            End If

            ' read past any leading spaces or tabs
            'Skip over leading whitespace
            lChar = SkipWhiteSpaceEOF()

            If lChar = lchDoubleQuote Then
                lChar = m_sr.Read()
                m_position += 1
                InputStr = ReadInField(FIN_QSTRING)
            Else
                InputStr = ReadInField(FIN_STRING)
            End If

            SkipTrailingWhiteSpace()
        End Function

    End Class

End Namespace
