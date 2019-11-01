' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports System.Security
Imports System.Globalization
Imports System.IO
Imports System.Text

Imports Microsoft.VisualBasic.CompilerServices.StructUtils
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    Friend Enum tagVT As Short
        VT_EMPTY = 0
        VT_NULL = 1
        VT_I2 = 2
        VT_I4 = 3
        VT_R4 = 4
        VT_R8 = 5
        VT_CY = 6
        VT_DATE = 7
        VT_BSTR = 8
        VT_DISPATCH = 9
        VT_ERROR = 10
        VT_BOOL = 11
        VT_VARIANT = 12
        VT_UNKNOWN = 13
        VT_DECIMAL = 14
        VT_I1 = 16
        VT_UI1 = 17
        VT_UI2 = 18
        VT_UI4 = 19
        VT_I8 = 20
        VT_UI8 = 21
        VT_INT = 22
        VT_UINT = 23
        VT_VOID = 24
        VT_HRESULT = 25
        VT_PTR = 26
        VT_SAFEARRAY = 27
        VT_CARRAY = 28
        VT_USERDEFINED = 29
        VT_LPSTR = 30
        VT_LPWSTR = 31
        VT_RECORD = 36
        VT_FILETIME = 64
        VT_BLOB = 65
        VT_STREAM = 66
        VT_STORAGE = 67
        VT_STREAMED_OBJECT = 68
        VT_STORED_OBJECT = 69
        VT_BLOB_OBJECT = 70
        VT_CF = 71
        VT_CLSID = 72
        VT_BSTR_BLOB = 4095
        VT_VECTOR = 4096
        VT_ARRAY = 8192
        VT_BYREF = 16384
        VT_RESERVED = &H8000S
        VT_ILLEGAL = &HFFFFS
        VT_ILLEGALMASKED = 4095
        VT_TYPEMASK = 4095
    End Enum

    Friend Enum VT As Short
        [Error] = tagVT.VT_ERROR
        [Boolean] = tagVT.VT_BOOL
        [Byte] = tagVT.VT_UI1
        [Short] = tagVT.VT_I2
        [Integer] = tagVT.VT_I4
        [Decimal] = tagVT.VT_DECIMAL
        [Single] = tagVT.VT_R4
        [Double] = tagVT.VT_R8
        [String] = tagVT.VT_BSTR
        [ByteArray] = tagVT.VT_UI1 Or _
                      tagVT.VT_ARRAY
        [CharArray] = tagVT.VT_UI2 Or _
                      tagVT.VT_ARRAY
        [Date] = tagVT.VT_DATE
        [Long] = tagVT.VT_I8
        [Char] = tagVT.VT_UI2
        [Variant] = tagVT.VT_VARIANT
        [Array] = tagVT.VT_ARRAY
        [DBNull] = tagVT.VT_NULL
        [Empty] = tagVT.VT_EMPTY
        [Structure] = tagVT.VT_RECORD
        [Currency] = tagVT.VT_CY
    End Enum

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
    Friend NotInheritable Class PutHandler
        Implements IRecordEnum
        Public m_oFile As VB6File

        Sub New(ByVal oFile As VB6File)
            MyBase.New()
            m_oFile = oFile
        End Sub

        Function Callback(ByVal field_info As Reflection.FieldInfo, ByRef vValue As Object) As Boolean Implements IRecordEnum.Callback
            Dim FieldType As System.Type = field_info.FieldType

            If FieldType Is Nothing Then
                Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "Empty")), vbErrors.IllegalFuncCall)
            End If

            If FieldType.IsArray() Then
                Dim attributeList As Object()
                Dim ElementType As System.Type
                Dim attrFixedArray As VBFixedArrayAttribute
                Dim FixedStringLength As Integer = -1

                attributeList = field_info.GetCustomAttributes(GetType(VBFixedArrayAttribute), False)
                If Not attributeList Is Nothing AndAlso attributeList.Length <> 0 Then
                    attrFixedArray = CType(attributeList(0), VBFixedArrayAttribute)
                Else
                    attrFixedArray = Nothing
                End If

                ElementType = FieldType.GetElementType()

                If ElementType Is GetType(System.String) Then
                    attributeList = field_info.GetCustomAttributes(GetType(VBFixedStringAttribute), False)
                    If attributeList Is Nothing OrElse attributeList.Length = 0 Then
                        FixedStringLength = -1
                    Else
                        FixedStringLength = CType(attributeList(0), VBFixedStringAttribute).Length
                    End If
                End If

                If attrFixedArray Is Nothing Then

                    m_oFile.PutDynamicArray(0, CType(vValue, System.Array), False, FixedStringLength)

                Else

                    m_oFile.PutFixedArray(0, CType(vValue, System.Array), ElementType, FixedStringLength, attrFixedArray.FirstBound, attrFixedArray.SecondBound)

                End If

            Else
                Select Case Type.GetTypeCode(FieldType)
                    Case TypeCode.String
                        Dim s As String

                        If Not vValue Is Nothing Then
                            s = vValue.ToString()
                        Else
                            s = Nothing
                        End If

                        Dim attributeList As Object() = field_info.GetCustomAttributes(GetType(VBFixedStringAttribute), False)

                        'If (field_info.Attributes And Reflection.FieldAttributes.HasFieldMarshal) <> Reflection.FieldAttributes.HasFieldMarshal Then
                        If attributeList Is Nothing OrElse attributeList.Length = 0 Then
                            m_oFile.PutStringWithLength(0, s)
                        Else
                            Dim ma As VBFixedStringAttribute
                            Dim length As Integer

                            ma = CType(attributeList(0), VBFixedStringAttribute)
                            length = ma.Length

                            If length = 0 Then
                                length = -1
                            End If

                            m_oFile.PutFixedLengthString(0, s, length)
                        End If
                    Case TypeCode.Single
                        m_oFile.PutSingle(0, SingleType.FromObject(vValue))
                    Case TypeCode.Double
                        m_oFile.PutDouble(0, DoubleType.FromObject(vValue))
                    Case TypeCode.Int16
                        m_oFile.PutShort(0, ShortType.FromObject(vValue))
                    Case TypeCode.Int32
                        m_oFile.PutInteger(0, IntegerType.FromObject(vValue))
                    Case TypeCode.Byte
                        m_oFile.PutByte(0, ByteType.FromObject(vValue))
                    Case TypeCode.Int64
                        m_oFile.PutLong(0, LongType.FromObject(vValue))
                    Case TypeCode.DateTime
                        m_oFile.PutDate(0, DateType.FromObject(vValue))
                    Case TypeCode.Boolean
                        m_oFile.PutBoolean(0, BooleanType.FromObject(vValue))
                    Case TypeCode.Decimal
                        m_oFile.PutDecimal(0, DecimalType.FromObject(vValue))
                    Case TypeCode.Char
                        m_oFile.PutChar(0, CharType.FromObject(vValue))
                    Case TypeCode.DBNull
                        Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "DBNull")), vbErrors.IllegalFuncCall)
                    Case Else 'Case TypeCode.Object
                        If FieldType Is GetType(Object) Then
                            m_oFile.PutObject(vValue, 0)
                        ElseIf FieldType Is GetType(System.Exception) Then
                            Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "Exception")), vbErrors.IllegalFuncCall)
                        ElseIf FieldType Is GetType(System.Reflection.Missing) Then
                            Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "Missing")), vbErrors.IllegalFuncCall)
                        Else
                            Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, FieldType.Name)), vbErrors.IllegalFuncCall)
                        End If
                End Select
            End If

            Return False
        End Function
    End Class

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Friend NotInheritable Class GetHandler
        Implements IRecordEnum
        Dim m_oFile As VB6File

        Sub New(ByVal oFile As VB6File)
            MyBase.New()
            m_oFile = oFile
        End Sub

        Function Callback(ByVal field_info As Reflection.FieldInfo, ByRef vValue As Object) As Boolean Implements IRecordEnum.Callback
            Dim FieldType As System.Type

            FieldType = field_info.FieldType

            If FieldType Is Nothing Then
                Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "Empty")), vbErrors.IllegalFuncCall)
            End If

            If FieldType.IsArray() Then
                Dim attributeList As Object() = field_info.GetCustomAttributes(GetType(VBFixedArrayAttribute), False)
                Dim arr As System.Array = Nothing
                Dim FixedStringLength As Integer = -1

                Dim FixedStringAttributeList As Object() = field_info.GetCustomAttributes(GetType(VBFixedStringAttribute), False)
                If Not FixedStringAttributeList Is Nothing AndAlso FixedStringAttributeList.Length > 0 Then
                    Dim FixedStringAttribute As VBFixedStringAttribute = CType(FixedStringAttributeList(0), VBFixedStringAttribute)
                    If FixedStringAttribute.Length > 0 Then
                        FixedStringLength = FixedStringAttribute.Length
                    End If
                End If

                If attributeList Is Nothing OrElse attributeList.Length = 0 Then
                    m_oFile.GetDynamicArray(arr, FieldType.GetElementType, FixedStringLength)
                Else
                    Dim attr As VBFixedArrayAttribute = CType(attributeList(0), VBFixedArrayAttribute)
                    Dim FirstBound As Integer = attr.FirstBound
                    Dim SecondBound As Integer = attr.SecondBound
                    arr = CType(vValue, System.Array)

                    m_oFile.GetFixedArray(0, arr, FieldType.GetElementType(), FirstBound, SecondBound, FixedStringLength)
                End If

                vValue = arr
            Else
                Select Case Type.GetTypeCode(FieldType)
                    Case TypeCode.String
                        Dim attributeList As Object() = field_info.GetCustomAttributes(GetType(VBFixedStringAttribute), False)

                        If attributeList Is Nothing OrElse attributeList.Length = 0 Then
                            vValue = m_oFile.GetLengthPrefixedString(0)
                        Else

                            Dim ma As VBFixedStringAttribute = CType(attributeList(0), VBFixedStringAttribute)
                            Dim length As Integer = ma.Length

                            If length = 0 Then
                                length = -1
                            End If
                            vValue = m_oFile.GetFixedLengthString(0, length)
                        End If
                    Case TypeCode.Single
                        vValue = m_oFile.GetSingle(0)
                    Case TypeCode.Double
                        vValue = m_oFile.GetDouble(0)
                    Case TypeCode.Int16
                        vValue = m_oFile.GetShort(0)
                    Case TypeCode.Int32
                        vValue = m_oFile.GetInteger(0)
                    Case TypeCode.Byte
                        vValue = m_oFile.GetByte(0)
                    Case TypeCode.Int64
                        vValue = m_oFile.GetLong(0)
                    Case TypeCode.DateTime
                        vValue = m_oFile.GetDate(0)
                    Case TypeCode.Boolean
                        vValue = m_oFile.GetBoolean(0)
                    Case TypeCode.Decimal
                        vValue = m_oFile.GetDecimal(0)
                    Case TypeCode.Char
                        vValue = m_oFile.GetChar(0)
                    Case TypeCode.DBNull
                        Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "DBNull")), vbErrors.IllegalFuncCall)
                    Case Else
                        'Case TypeCode.Object
                        If FieldType Is GetType(Object) Then
                            m_oFile.GetObject(vValue)
                        ElseIf FieldType Is GetType(System.Exception) Then
                            Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "Exception")), vbErrors.IllegalFuncCall)
                        ElseIf FieldType Is GetType(System.Reflection.Missing) Then
                            Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, "Missing")), vbErrors.IllegalFuncCall)
                        Else
                            Throw VbMakeException(New ArgumentException(GetResourceString(SR.Argument_UnsupportedFieldType2, field_info.Name, FieldType.Name)), vbErrors.IllegalFuncCall)
                        End If
                End Select
            End If

            Return False
        End Function
    End Class

    '**********************************************
    '*
    '* VB6File
    '*
    '* Base for all VB6 compatible file i/o
    '*
    '**********************************************
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Friend MustInherit Class VB6File
        Friend m_lCurrentColumn As Integer
        Friend m_lWidth As Integer
        Friend m_lRecordLen As Integer
        Friend m_lRecordStart As Long
        Friend m_sFullPath As String
        Friend m_share As OpenShare
        Friend m_access As OpenAccess
        Friend m_eof As Boolean
        Friend m_position As Long
        Friend m_file As FileStream
        Friend m_fAppend As Boolean
        Friend m_bPrint As Boolean
        Protected m_sw As StreamWriter
        Protected m_sr As StreamReader
        Protected m_bw As BinaryWriter
        Protected m_br As BinaryReader
        Protected m_Encoding As Encoding

        Protected Const lchTab As Integer = 9
        Protected Const lchCR As Integer = 13
        Protected Const lchLF As Integer = 10
        Protected Const lchSpace As Integer = 32
        Protected Const lchIntlSpace As Integer = &H3000I
        Protected Const lchDoubleQuote As Integer = 34
        Protected Const lchPound As Integer = AscW("#")
        Protected Const lchComma As Integer = AscW(",")
        Protected Const EOF_INDICATOR As Integer = -1
        Protected Const EOF_CHAR As Integer = &H1A
        Protected Const FIN_NUMTERMCHAR As Short = 6
        Protected Const FIN_LINEINP As Short = 0
        Protected Const FIN_QSTRING As Short = 1
        Protected Const FIN_STRING As Short = 2
        Protected Const FIN_NUMBER As Short = 3

        '============================================================================
        ' Construction functions.
        '============================================================================
        Protected Sub New()
            MyBase.New()
        End Sub

        Protected Sub New(ByVal sPath As String, ByVal access As OpenAccess, ByVal share As OpenShare, ByVal lRecordLen As Integer)
            MyBase.New()

            If access <> OpenAccess.Read AndAlso
               access <> OpenAccess.ReadWrite AndAlso
               access <> OpenAccess.Write Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "Access"))
            End If
            m_access = access

            If (share <> OpenShare.Shared AndAlso
                share <> OpenShare.LockRead AndAlso
                share <> OpenShare.LockReadWrite AndAlso
                share <> OpenShare.LockWrite) Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "Share"))
            End If

            m_share = share

            m_lRecordLen = lRecordLen
            m_sFullPath = (New FileInfo(sPath)).FullName
        End Sub

        '============================================================================
        ' Open/Close/Information functions.
        '============================================================================
        Friend Function GetAbsolutePath() As String
            Return m_sFullPath
        End Function

        Friend Overridable Sub OpenFile()
            Try
                If File.Exists(m_sFullPath) Then
                    m_file = New FileStream(m_sFullPath, FileMode.Open, CType(m_access, FileAccess), CType(m_share, FileShare))
                Else
                    m_file = New FileStream(m_sFullPath, FileMode.Create, CType(m_access, FileAccess), CType(m_share, FileShare))
                End If

            Catch e2 As SecurityException
                Throw VbMakeException(vbErrors.FileNotFound)

            End Try
        End Sub

        Friend Overridable Sub CloseFile()
            CloseTheFile()
        End Sub

        Protected Sub CloseTheFile()
            If m_sw Is Nothing Then
                'nothing to do
            Else
                m_sw.Close()
                m_sw = Nothing
            End If

            If m_sr Is Nothing Then
                'nothing to do
            Else
                m_sr.Close()
                m_sr = Nothing
            End If

            If Not m_file Is Nothing Then
                m_file.Close()
                m_file = Nothing
            End If
        End Sub

        Friend Function GetColumn() As Integer
            Return m_lCurrentColumn
        End Function

        Friend Sub SetColumn(ByVal lColumn As Integer)
            If m_lWidth <> 0 AndAlso m_lCurrentColumn <> 0 AndAlso
                (lColumn + 14) > m_lWidth Then
                WriteLine(Nothing)
            Else
                SPC(lColumn - m_lCurrentColumn)
            End If
        End Sub

        Friend Function GetWidth() As Integer
            Return m_lWidth
        End Function

        Friend Sub SetWidth(ByVal RecordWidth As Integer)
            If RecordWidth < 0 OrElse RecordWidth > 255 Then
                Throw VbMakeException(vbErrors.IllegalFuncCall)
            End If

            m_lWidth = RecordWidth
        End Sub

        '============================================================================
        ' Output functions.
        '============================================================================
        Friend Overridable Sub WriteLine(ByVal s As String)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Sub WriteString(ByVal s As String)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Function EOF() As Boolean
            Return m_eof
        End Function

        Friend Function LOF() As Long
            Return m_file.Length
        End Function

        Friend Overridable Function LOC() As Long
            If (m_lRecordLen = -1) OrElse (GetMode() <> OpenMode.Random) Then
                Return (m_position + 1)
            End If

            If m_lRecordLen = 0 Then
                Throw VbMakeException(vbErrors.InternalError)
            Else
                Dim pos As Long
                pos = m_position

                If pos = 0 Then
                    Return 0
                End If

                Return (m_position \ m_lRecordLen) + 1
            End If
        End Function

        Friend Overridable Function GetStreamReader() As StreamReader
            Return m_sr
        End Function

        Friend Sub SetRecord(ByVal RecordNumber As Long)
            Dim lSeekPos As Long

            If m_lRecordLen = 0 Then
                Exit Sub
            End If

            If RecordNumber = 0 Then
                Exit Sub
            ElseIf m_lRecordLen = -1 Then
                If RecordNumber = -1 Then
                    'Binary file, leave at current position
                    Exit Sub
                Else
                    'No records, use actual byte position
                    lSeekPos = RecordNumber - 1
                End If
            ElseIf RecordNumber = -1 Then
                'Go to next record
                lSeekPos = GetPos()

                If lSeekPos = 0 Then
                    m_lRecordStart = 0
                    Exit Sub
                End If

                If (lSeekPos Mod m_lRecordLen) = 0 Then
                    'Already on record boundary
                    m_lRecordStart = lSeekPos
                    Exit Sub
                End If

                'Go to next record
                lSeekPos = m_lRecordLen * (lSeekPos \ m_lRecordLen + 1)
            ElseIf RecordNumber <> 0 Then
                'Go to specified record
                'lSeekPos = (RecordNumber - 1) * m_lRecordLen

                If m_lRecordLen = -1 Then
                    lSeekPos = RecordNumber
                Else
                    lSeekPos = (RecordNumber - 1) * m_lRecordLen
                End If
            End If

            SeekOffset(lSeekPos)
            m_lRecordStart = lSeekPos
        End Sub

        Friend Overridable Overloads Sub Seek(ByVal BaseOnePosition As Long)
            If BaseOnePosition <= 0 Then
                Throw VbMakeException(vbErrors.BadRecordNum)
            End If

            Dim BaseZeroPosition As Long = BaseOnePosition - 1

            If BaseZeroPosition > m_file.Length Then
                m_file.SetLength(BaseZeroPosition)
            End If

            m_file.Position = BaseZeroPosition
            m_position = BaseZeroPosition

            m_eof = (m_position >= m_file.Length)

            If Not m_sr Is Nothing Then
                m_sr.DiscardBufferedData()
            End If

        End Sub

        'Function Seek
        '
        'RANDOM MODE - Returns number of next record
        'other modes - Returns the byte position at which the next operation 
        '              will take place
        Friend Overridable Overloads Function Seek() As Long
            'm_position is the last read byte as a zero based offset
            'Seek returns the position of the next byte to read
            Return (m_position + 1)
        End Function

        Friend Sub SeekOffset(ByVal offset As Long)
            'Do not call m_file.SetLength here because that could extend the file length,
            'which shouldn't happen until a subsequent Write or Put operation.
            m_position = offset
            m_file.Position = offset

            If Not m_sr Is Nothing Then
                m_sr.DiscardBufferedData()
            End If

        End Sub

        Friend Function GetPos() As Long
            Return m_position
        End Function

        Friend Overridable Overloads Sub Lock()
            'Lock the whole file, not just the current size of file, since file could change.
            m_file.Lock(0, Int32.MaxValue)
        End Sub

        Friend Overridable Overloads Sub Unlock()
            m_file.Unlock(0, Int32.MaxValue)
        End Sub

        Friend Overridable Overloads Sub Lock(ByVal Record As Long)
            If m_lRecordLen = -1 Then
                m_file.Lock((Record - 1), 1)
            Else
                m_file.Lock((Record - 1) * m_lRecordLen, m_lRecordLen)
            End If
        End Sub

        Friend Overridable Overloads Sub Unlock(ByVal Record As Long)
            If m_lRecordLen = -1 Then
                m_file.Unlock((Record - 1), 1)
            Else
                m_file.Unlock((Record - 1) * m_lRecordLen, m_lRecordLen)
            End If
        End Sub

        Friend Overridable Overloads Sub Lock(ByVal RecordStart As Long, ByVal RecordEnd As Long)
            If m_lRecordLen = -1 Then
                m_file.Lock((RecordStart - 1), (RecordEnd - RecordStart) + 1)
            Else
                m_file.Lock((RecordStart - 1) * m_lRecordLen, ((RecordEnd - RecordStart) + 1) * m_lRecordLen)
            End If
        End Sub

        Friend Overridable Overloads Sub Unlock(ByVal RecordStart As Long, ByVal RecordEnd As Long)
            If m_lRecordLen = -1 Then
                m_file.Unlock((RecordStart - 1), (RecordEnd - RecordStart) + 1)
            Else
                m_file.Unlock((RecordStart - 1) * m_lRecordLen, ((RecordEnd - RecordStart) + 1) * m_lRecordLen)
            End If
        End Sub

        Friend Function LineInput() As String
            ValidateReadable()
            Dim Result As String = m_sr.ReadLine()
            If Result Is Nothing Then
                Result = ""
            End If

            Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
            m_position += m_Encoding.GetByteCount(Result) + 2
            m_eof = CheckEOF(m_sr.Peek())
            Return Result
        End Function

        Friend Overridable Function CanInput() As Boolean
            Return False
        End Function

        Friend Overridable Function CanWrite() As Boolean
            Return False
        End Function

        Protected Overridable Sub InputObject(ByRef Value As Object)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Protected Overridable Function InputStr() As String
            Dim lChar As Integer

            ValidateReadable()

            'Read past any leading spaces or tabs
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

        Protected Overridable Function InputNum(ByVal vt As VariantType) As Object
            Dim sField As String

            ValidateReadable()

            'Read past any leading spaces or tabs
            'Skip over leading whitespace
            SkipWhiteSpaceEOF()

            sField = ReadInField(FIN_NUMBER)

            ' considering adding validity checks for expected varianttype
            InputNum = sField
            SkipTrailingWhiteSpace()
        End Function

        Public MustOverride Function GetMode() As OpenMode

        Friend Function InputString(ByVal lLen As Integer) As String
            Dim sb As StringBuilder
            Dim i As Integer
            Dim lInput As Integer
            Dim FileOpenMode As OpenMode

            ValidateReadable()

            sb = New StringBuilder(lLen)
            FileOpenMode = GetMode()

            For i = 1 To lLen
                If FileOpenMode = OpenMode.Binary Then
                    lInput = m_br.Read()
                    m_position += 1

                    If (lInput = -1) Then     'Binary files don't stop upon reading 26=CTRL-Z
                        Exit For
                    End If
                ElseIf FileOpenMode = OpenMode.Input Then
                    lInput = m_sr.Read()
                    m_position += 1

                    If (lInput = -1) Or (lInput = 26) Then      'Input files do stop upon reading 26=CTRL-Z
                        m_eof = True
                        Throw VbMakeException(vbErrors.EndOfFile)
                    End If
                Else
                    Throw VbMakeException(vbErrors.BadFileMode)
                End If

                If lInput <> 0 Then
                    sb.Append(ChrW(lInput))
                End If
            Next i

            If FileOpenMode = OpenMode.Binary Then
                m_eof = (m_br.PeekChar() = EOF_INDICATOR)
            Else
                m_eof = CheckEOF(m_sr.Peek())
            End If

            Return sb.ToString()
        End Function

        Friend Sub SPC(ByVal iCount As Integer)
            Dim lCurPos As Integer
            Dim lWidth As Integer
            Dim s As String

            If iCount <= 0 Then
                '            iCount = 0
                Exit Sub
            End If

            lCurPos = GetColumn()
            lWidth = GetWidth()

            If lWidth <> 0 Then
                ' File output with line length limit
                If iCount >= lWidth Then
                    iCount = iCount Mod lWidth ' Modulo the line length
                End If

                If (iCount + lCurPos) > lWidth Then
                    ' Spaces don't fit on this line.  Subtract what fits and put the
                    ' rest on next line.
                    iCount -= (lWidth - lCurPos)
                    GoTo NewLine
                End If
            End If

            iCount += lCurPos

            ' If tab position is less than current position,
            ' goto next line.
            If (iCount < lCurPos) Then
NewLine:
                WriteLine(Nothing)
                'FileOutString(iodata, FILE_EOL, FILE_EOL_LEN)
                lCurPos = 0
            End If

            If (iCount > lCurPos) Then
                s = New System.String(" "c, iCount - lCurPos)
                [WriteString](s)
            End If
        End Sub

        Friend Sub Tab(ByVal Column As Integer)
            Dim lCurPos As Integer
            Dim lWidth As Integer
            Dim s As String

            If Column < 1 Then
                Column = 1
            End If

            'When tabbing, we go to the space before the column
            'so the next print will be in that column
            Column -= 1

            lCurPos = GetColumn()
            lWidth = GetWidth()

            If lWidth <> 0 Then
                ' File output with line length limit
                If Column >= lWidth Then
                    Column = Column Mod lWidth ' Modulo the line length
                End If
            End If

            ' If tab position is less than current position,
            ' goto next line.
            If (Column < lCurPos) Then
                WriteLine(Nothing)
                lCurPos = 0
            End If

            If (Column > lCurPos) Then
                s = New System.String(" "c, Column - lCurPos)
                [WriteString](s)
            End If
        End Sub

        Friend Sub SetPrintMode()
            Dim mode As OpenMode

            mode = GetMode()

            If mode = OpenMode.Input OrElse
                mode = OpenMode.Binary OrElse
                mode = OpenMode.Random Then
                Throw VbMakeException(vbErrors.BadFileMode)
            End If

            m_bPrint = True
        End Sub

        Friend Shared Function VTType(ByVal VarName As Object) As VT
            If VarName Is Nothing Then
                Return VT.Variant
            End If

            Return VTFromComType(VarName.GetType())
        End Function

        Friend Shared Function VTFromComType(ByVal typ As System.Type) As VT
            If typ Is Nothing Then
                Return VT.Variant
            End If

            If typ.IsArray() Then
                typ = typ.GetElementType()
                If typ.IsArray Then
                    Return CType(VT.Array Or VT.Variant, VT)
                End If

                Dim Result As VT = VTFromComType(typ)
                If (Result And VT.Array) <> 0 Then
                    'Element type is also an array, so just return "array of objects"
                    Return CType(VT.Array Or VT.Variant, VT)
                End If
                Return CType(Result Or VT.Array, VT)

            ElseIf typ.IsEnum() Then
                typ = System.Enum.GetUnderlyingType(typ)
            End If

            If typ Is Nothing Then
                Return VT.Empty
            End If

            Select Case Type.GetTypeCode(typ)
                Case TypeCode.String
                    Return VT.String
                Case TypeCode.Int32
                    Return VT.Integer
                Case TypeCode.Int16
                    Return VT.Short
                Case TypeCode.Int64
                    Return VT.Long
                Case TypeCode.Single
                    Return VT.Single
                Case TypeCode.Double
                    Return VT.Double
                Case TypeCode.DateTime
                    Return VT.Date
                Case TypeCode.Boolean
                    Return VT.Boolean
                Case TypeCode.Decimal
                    Return VT.Decimal
                Case TypeCode.Byte
                    Return VT.Byte
                Case TypeCode.Char
                    Return VT.Char
                Case TypeCode.DBNull
                    Return VT.DBNull
            End Select

            If typ Is GetType(System.Reflection.Missing) Then
                Return VT.Error

            ElseIf typ Is GetType(System.Exception) OrElse typ.IsSubclassOf(GetType(System.Exception)) Then
                Return VT.Error

                'Must come after all the Intrinsic types
            ElseIf typ.IsValueType() Then
                Return VT.Structure

            Else
                Return VT.Variant

            End If
        End Function

        Friend Sub PutFixedArray(ByVal RecordNumber As Long, ByVal arr As System.Array, ByVal ElementType As System.Type,
            Optional ByVal FixedStringLength As Integer = -1, Optional ByVal FirstBound As Integer = -1,
            Optional ByVal SecondBound As Integer = -1)

            SetRecord(RecordNumber)
            If ElementType Is Nothing Then
                ElementType = arr.GetType().GetElementType()
            End If
            PutArrayData(arr, ElementType, FixedStringLength, FirstBound, SecondBound)
        End Sub

        Friend Sub PutDynamicArray(ByVal RecordNumber As Long, ByVal arr As System.Array,
            Optional ByVal ContainedInVariant As Boolean = True, Optional ByVal FixedStringLength As Integer = -1)

            Dim FirstBound As Integer
            Dim SecondBound As Integer
            Dim cDims As Integer

            If arr Is Nothing Then
                cDims = 0
            Else
                cDims = arr.Rank()
                FirstBound = arr.GetUpperBound(0)
            End If

            If cDims = 1 Then
                SecondBound = -1
            ElseIf cDims = 2 Then
                SecondBound = arr.GetUpperBound(1)
            ElseIf cDims <> 0 Then
                Throw New ArgumentException(GetResourceString(SR.Argument_UnsupportedArrayDimensions))
            End If

            SetRecord(RecordNumber)

            If ContainedInVariant Then
                Dim vtype As VT

                vtype = VTType(arr)
                m_bw.Write(CShort(vtype))
                m_position += 2

                If (vtype And VT.Array) = 0 Then
                    Throw VbMakeException(vbErrors.InvalidTypeLibVariable)
                End If
            End If

            PutArrayDesc(arr)
            If cDims <> 0 Then
                PutArrayData(arr, arr.GetType().GetElementType(), FixedStringLength, FirstBound, SecondBound)
            End If
        End Sub

        Friend Sub LengthCheck(ByVal Length As Integer)
            If m_lRecordLen = -1 Then
                Exit Sub
            End If

            If Length > m_lRecordLen Then
                Throw VbMakeException(vbErrors.BadRecordLen)
            Else
                If (GetPos() + Length) > (m_lRecordStart + m_lRecordLen) Then
                    Throw VbMakeException(vbErrors.BadRecordLen)
                End If
            End If
        End Sub

        'Writes a fixed length string member of a structure to the file
        Friend Sub PutFixedLengthString(ByVal RecordNumber As Long, ByVal s As String, ByVal lengthToWrite As Integer)
            Dim PadChar As Char = " "c

            If s Is Nothing Then
                s = ""
            End If

            If s = "" Then
                PadChar = ChrW(0)
            End If

            'Need to handle double byte chars in s
            Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
            Dim ByteLength As Integer = m_Encoding.GetByteCount(s)

            If ByteLength > lengthToWrite Then
                If ByteLength = s.Length Then
                    s = Left(s, lengthToWrite)
                Else
                    'String contains multi-byte characters.  Truncate to 'length' bytes.
                    Dim Bytes() As Byte = m_Encoding.GetBytes(s)
                    s = m_Encoding.GetString(Bytes, 0, lengthToWrite)

                    Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
                    ByteLength = m_Encoding.GetByteCount(s)
                    If ByteLength > lengthToWrite Then
                        For i As Integer = lengthToWrite - 1 To 0 Step -1
                            Bytes(i) = 0
                            s = m_Encoding.GetString(Bytes, 0, lengthToWrite)
                            ByteLength = m_Encoding.GetByteCount(s)
                            If ByteLength <= lengthToWrite Then
                                Exit For
                            End If
                        Next
                    End If
                    Diagnostics.Debug.Assert(ByteLength <= lengthToWrite)
                End If
            End If

            If ByteLength < lengthToWrite Then
                s = s & StrDup(lengthToWrite - ByteLength, PadChar)
            End If

            Diagnostics.Debug.Assert(m_Encoding.GetByteCount(s) = lengthToWrite)

            SetRecord(RecordNumber)
            LengthCheck(lengthToWrite)
            m_sw.Write(s)
            m_position += lengthToWrite
        End Sub

        Friend Sub PutVariantString(ByVal RecordNumber As Long, ByVal s As String)
            If s Is Nothing Then
                s = ""
            End If

            Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
            Dim ByteLength As Integer = m_Encoding.GetByteCount(s)

            SetRecord(RecordNumber)
            LengthCheck(ByteLength + 2 + 2) 'Add sizeof string length and vartype
            m_bw.Write(CShort(VT.String))
            m_bw.Write(CShort(ByteLength))

            If (ByteLength <> 0) Then
                m_sw.Write(s)
            End If

            m_position += ByteLength + 2 + 2
        End Sub

        Friend Sub PutString(ByVal RecordNumber As Long, ByVal s As String)
            If s Is Nothing Then
                s = ""
            End If

            Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
            Dim ByteLength As Integer = m_Encoding.GetByteCount(s)

            SetRecord(RecordNumber)
            LengthCheck(ByteLength)

            If (ByteLength <> 0) Then
                m_sw.Write(s)
            End If

            m_position += ByteLength
        End Sub

        Friend Sub PutStringWithLength(ByVal RecordNumber As Long, ByVal s As String)
            If s Is Nothing Then
                s = ""
            End If

            Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
            Dim ByteLength As Integer = m_Encoding.GetByteCount(s)

            SetRecord(RecordNumber)
            LengthCheck(ByteLength + 2)
            m_bw.Write(CShort(ByteLength))

            If ByteLength <> 0 Then
                'Must use streamwriter to get the unicode/ansi conversion done
                m_sw.Write(s)
            End If

            m_position += ByteLength + 2
        End Sub

        Friend Sub PutDate(ByVal RecordNumber As Long, ByVal dt As Date, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 8
            Dim dbl As Double

            If ContainedInVariant Then
                RecLength += 2
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Date)
            End If

            dbl = dt.ToOADate()
            m_bw.Write(dbl)
            m_position += RecLength
        End Sub

        Friend Sub PutShort(ByVal RecordNumber As Long, ByVal i As Short, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 2

            If ContainedInVariant Then
                RecLength += 2
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Short)
            End If

            m_bw.Write(i)
            m_position += RecLength
        End Sub

        Friend Sub PutInteger(ByVal RecordNumber As Long, ByVal l As Integer, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 4

            If ContainedInVariant Then
                RecLength += 2
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Integer)
            End If

            m_bw.Write(l)
            m_position += RecLength
        End Sub

        Friend Sub PutLong(ByVal RecordNumber As Long, ByVal l As Long, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 8

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Long)
            End If

            m_bw.Write(l)
            m_position += RecLength
        End Sub

        Friend Sub PutByte(ByVal RecordNumber As Long, ByVal byt As Byte, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 1

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Byte)
            End If

            m_bw.Write(byt)
            m_position += RecLength
        End Sub

        Friend Sub PutChar(ByVal RecordNumber As Long, ByVal ch As Char, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 2

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Char)
            End If

            m_bw.Write(ch)
            m_position += RecLength
        End Sub

        Friend Sub PutSingle(ByVal RecordNumber As Long, ByVal sng As Single, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 4

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Single)
            End If

            m_bw.Write(sng)
            m_position += RecLength
        End Sub

        Friend Sub PutDouble(ByVal RecordNumber As Long, ByVal dbl As Double, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 8

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Double)
            End If

            m_bw.Write(dbl)
            m_position += RecLength
        End Sub

        Friend Sub PutEmpty(ByVal RecordNumber As Long)
            'This will always be a Variant
            SetRecord(RecordNumber)
            LengthCheck(2)
            m_bw.Write(VT.Empty)
            m_position += 2
        End Sub

        Friend Sub PutBoolean(ByVal RecordNumber As Long, ByVal b As Boolean, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 2

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Boolean)
            End If

            If b Then
                m_bw.Write(CShort(-1))
            Else
                m_bw.Write(CShort(0))
            End If

            m_position += RecLength
        End Sub

        Friend Sub PutDecimal(ByVal RecordNumber As Long, ByVal dec As Decimal, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 16

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Decimal)
            End If

            Dim lo, mid, hi As Integer
            Dim flags As Byte
            Dim sign As Byte
            Dim bits() As Integer

            bits = System.Decimal.GetBits(dec)
            flags = CByte((bits(3) And &H7FFFFFFFI) \ &H10000I)
            lo = bits(0)
            mid = bits(1)
            hi = bits(2)

            If (bits(3) And &H80000000I) <> 0 Then
                sign = 128
            End If

            m_bw.Write(CShort(VT.Decimal)) ' Decimal contains the vtype as first 2 bytes
            m_bw.Write(flags)
            m_bw.Write(sign)
            m_bw.Write(hi)
            m_bw.Write(lo)
            m_bw.Write(mid)
            m_position += RecLength
        End Sub

        Friend Sub PutCurrency(ByVal RecordNumber As Long, ByVal dec As Decimal, Optional ByVal ContainedInVariant As Boolean = False)
            Dim RecLength As Integer = 16

            If ContainedInVariant Then
                RecLength += 2 ' Add length of vartype
            End If

            SetRecord(RecordNumber)
            LengthCheck(RecLength)

            If ContainedInVariant Then
                m_bw.Write(VT.Currency)
            End If

            m_bw.Write(System.Decimal.ToOACurrency(dec))
            m_position += RecLength
        End Sub

        Friend Sub PutRecord(ByVal RecordNumber As Long, ByVal o As ValueType)
            If o Is Nothing Then
                Throw New NullReferenceException
            End If

            Dim intf As IRecordEnum
            Dim ph As PutHandler

            SetRecord(RecordNumber)

            ph = New PutHandler(Me)
            intf = ph

            If intf Is Nothing Then
                Throw VbMakeException(vbErrors.IllegalFuncCall)
            End If

            EnumerateUDT(o, intf, False)
        End Sub

        Friend Function ComTypeFromVT(ByVal vtype As VT) As System.Type
            Select Case vtype
                Case VT.Variant
                    Return GetType(System.Object)
                Case VT.Empty
                    Return Nothing
                Case VT.DBNull
                    Return GetType(System.DBNull)
                Case VT.Short
                    Return GetType(System.Int16)
                Case VT.Integer
                    Return GetType(System.Int32)
                Case VT.Long
                    Return GetType(System.Int64)
                Case VT.Single
                    Return GetType(System.Single)
                Case VT.Double
                    Return GetType(System.Double)
                Case VT.Date
                    Return GetType(System.DateTime)
                Case VT.String
                    Return GetType(System.String)
                Case VT.Error
                    Return GetType(System.Exception)
                Case VT.Boolean
                    Return GetType(System.Boolean)
                Case VT.Decimal
                    Return GetType(System.Decimal)
                Case VT.Byte
                    Return GetType(System.Byte)
                Case VT.Char
                    Return GetType(System.Char)
                    'Case VT.Structure
                    '    'Return m_Type
                Case Else
                    Throw VbMakeException(vbErrors.InvalidTypeLibVariable)
            End Select
        End Function

        Friend Sub GetFixedArray(ByVal RecordNumber As Long, ByRef arr As System.Array,
        ByVal FieldType As System.Type, Optional ByVal FirstBound As Integer = -1,
            Optional ByVal SecondBound As Integer = -1, Optional ByVal FixedStringLength As Integer = -1)

            If SecondBound = -1 Then
                arr = System.Array.CreateInstance(FieldType, FirstBound + 1)
            Else
                arr = System.Array.CreateInstance(FieldType, FirstBound + 1, SecondBound + 1)
            End If

            SetRecord(RecordNumber)
            GetArrayData(arr, FieldType, FirstBound, SecondBound, FixedStringLength)
        End Sub

        Friend Sub GetDynamicArray(ByRef arr As System.Array, ByVal t As System.Type, Optional ByVal FixedStringLength As Integer = -1)
            arr = GetArrayDesc(t)

            Dim cDims As Integer = arr.Rank
            Dim FirstBound As Integer = arr.GetUpperBound(0)
            Dim SecondBound As Integer

            If cDims = 1 Then
                SecondBound = -1
            Else
                SecondBound = arr.GetUpperBound(1)
            End If

            GetArrayData(arr, t, FirstBound, SecondBound, FixedStringLength)
        End Sub

        Private Sub PutArrayDesc(ByVal arr As System.Array)
            Dim cDims As Short
            Dim i As Integer

            If arr Is Nothing Then
                cDims = 0
            Else
                cDims = CShort(arr.Rank())
            End If
            m_bw.Write(cDims)
            m_position += 2

            If cDims = 0 Then
                Exit Sub
            End If

            For i = 0 To cDims - 1
                m_bw.Write(CInt(arr.GetLength(i)))
                m_bw.Write(CInt(arr.GetLowerBound(i))) 'Lower bound
                m_position += 8
            Next i
        End Sub

        Friend Function GetArrayDesc(ByVal typ As System.Type) As System.Array
            Dim cDims As Integer
            Dim lElementCounts() As Integer
            Dim lLowerBounds() As Integer
            Dim i As Integer

            ' for reading, read cDims, and how many in each, and redim
            cDims = m_br.ReadInt16()
            m_position += 2

            If cDims = 0 Then
                Return System.Array.CreateInstance(typ, 0)
            End If

            ReDim lElementCounts(cDims - 1)
            ReDim lLowerBounds(cDims - 1)

            For i = 0 To cDims - 1
                lElementCounts(i) = m_br.ReadInt32()
                lLowerBounds(i) = m_br.ReadInt32()
                m_position += 8
            Next i

            Return System.Array.CreateInstance(typ, lElementCounts, lLowerBounds)
        End Function

        Friend Overridable Function GetLengthPrefixedString(ByVal RecordNumber As Long) As String
            SetRecord(RecordNumber)

            If EOF() Then
                Return ""
            End If

            Return ReadString()
        End Function

        Friend Overridable Function GetFixedLengthString(ByVal RecordNumber As Long, ByVal ByteLength As Integer) As String
            SetRecord(RecordNumber)
            Return ReadString(ByteLength)
        End Function

        Protected Overloads Function ReadString(ByVal ByteLength As Integer) As String
            Dim byteArray As Byte()

            If ByteLength = 0 Then
                Return Nothing
            End If

            byteArray = m_br.ReadBytes(ByteLength)
            m_position += ByteLength

            Return m_Encoding.GetString(byteArray)
        End Function

        Protected Overloads Function ReadString() As String
            Dim ByteLen As Integer

            ByteLen = m_br.ReadInt16()
            m_position += 2

            If ByteLen = 0 Then
                Return Nothing
            End If

            LengthCheck(ByteLen)
            Return ReadString(ByteLen)

        End Function

        Friend Function GetDate(ByVal RecordNumber As Long) As Date
            Dim dbl As Double

            SetRecord(RecordNumber)
            dbl = m_br.ReadDouble()
            m_position += 8
            Return System.DateTime.FromOADate(dbl)
        End Function

        Friend Function GetShort(ByVal RecordNumber As Long) As Short
            Dim s As Short

            SetRecord(RecordNumber)
            s = m_br.ReadInt16()
            m_position += 2
            Return s
        End Function

        Friend Function GetInteger(ByVal RecordNumber As Long) As Integer
            Dim i As Integer

            SetRecord(RecordNumber)
            i = m_br.ReadInt32()
            m_position += 4
            Return i
        End Function

        Friend Function GetLong(ByVal RecordNumber As Long) As Long
            Dim l As Long

            SetRecord(RecordNumber)
            l = m_br.ReadInt64()
            m_position += 8
            Return l
        End Function

        Friend Function GetByte(ByVal RecordNumber As Long) As Byte
            Dim b As Byte

            SetRecord(RecordNumber)
            b = m_br.ReadByte()
            m_position += 1
            Return b
        End Function

        Friend Function GetChar(ByVal RecordNumber As Long) As Char
            Dim c As Char

            SetRecord(RecordNumber)
            c = m_br.ReadChar()
            m_position += 1
            Return c
        End Function

        Friend Function GetSingle(ByVal RecordNumber As Long) As Single
            Dim s As Single

            SetRecord(RecordNumber)
            s = m_br.ReadSingle()
            m_position += 4
            Return s
        End Function

        Friend Function GetDouble(ByVal RecordNumber As Long) As Double
            Dim d As Double

            SetRecord(RecordNumber)
            d = m_br.ReadDouble()
            m_position += 8
            Return d
        End Function

        Friend Function GetDecimal(ByVal RecordNumber As Long) As Decimal
            Dim vt As Integer
            Dim lo, mid, hi As Integer
            Dim flags As Byte
            Dim negative As Boolean
            Dim sign As Byte

            SetRecord(RecordNumber)
            vt = m_br.ReadInt16()
            flags = m_br.ReadByte()
            sign = m_br.ReadByte()
            hi = m_br.ReadInt32()
            lo = m_br.ReadInt32()
            mid = m_br.ReadInt32()
            m_position += 16

            If sign <> 0 Then
                negative = True
            End If

            Return New Decimal(lo, mid, hi, negative, flags)
        End Function

        Friend Function GetCurrency(ByVal RecordNumber As Long) As Decimal
            Dim i64 As Int64

            SetRecord(RecordNumber)
            i64 = m_br.ReadInt64()
            m_position += 8
            Return Decimal.FromOACurrency(i64)
        End Function

        Friend Function GetBoolean(ByVal RecordNumber As Long) As Boolean
            Dim i As Short

            SetRecord(RecordNumber)
            i = m_br.ReadInt16()
            m_position += 2

            If i = 0 Then
                Return False
            Else
                Return True
            End If
        End Function

        Friend Sub GetRecord(ByVal RecordNumber As Long, ByRef o As ValueType, Optional ByVal ContainedInVariant As Boolean = False)
            Dim intf As IRecordEnum
            Dim ph As GetHandler

            If o Is Nothing Then
                Throw New NullReferenceException
            End If

            SetRecord(RecordNumber)
            ph = New GetHandler(Me)
            intf = ph

            If intf Is Nothing Then
                Throw VbMakeException(vbErrors.IllegalFuncCall)
            End If

            EnumerateUDT(o, intf, True)
        End Sub

        Friend Sub PutArrayData(ByVal arr As System.Array, ByVal typ As System.Type, ByVal FixedStringLength As Integer,
            ByVal FirstBound As Integer, ByVal SecondBound As Integer)

            Dim vtype As VT
            Dim obj As Object
            Dim iElementX As Integer
            Dim iElementY As Integer
            Dim iUpperElementX As Integer
            Dim iUpperElementY As Integer
            Dim sTemp As String
            Dim ArrUBoundX, ArrUBoundY As Integer
            Dim FixedBlankString As String = Nothing
            Dim FixedCharArray As Char() = Nothing

            If arr Is Nothing Then
                ArrUBoundY = -1
                ArrUBoundX = -1
            ElseIf (arr.GetUpperBound(0) > FirstBound) Then
                Throw New ArgumentException(GetResourceString(SR.Argument_ArrayDimensionsDontMatch))
            End If

            If typ Is Nothing Then
                typ = arr.GetType().GetElementType()
            End If

            vtype = VTFromComType(typ)

            If SecondBound = -1 Then
                iUpperElementX = 0
                iUpperElementY = FirstBound
                If Not arr Is Nothing Then
                    ArrUBoundY = arr.GetUpperBound(0)
                End If
            Else
                iUpperElementX = SecondBound
                iUpperElementY = FirstBound
                If Not arr Is Nothing Then
                    If arr.Rank <> 2 OrElse arr.GetUpperBound(1) <> SecondBound Then
                        Throw New ArgumentException(GetResourceString(SR.Argument_ArrayDimensionsDontMatch))
                    End If
                    ArrUBoundY = arr.GetUpperBound(0)
                    ArrUBoundX = arr.GetUpperBound(1)
                End If
            End If

            If vtype = VT.String Then
                If FixedStringLength = 0 Then
                    'Use length of first String element
                    If SecondBound = -1 Then
                        obj = arr.GetValue(0)
                    Else
                        obj = arr.GetValue(0, 0)
                    End If
                    If Not obj Is Nothing Then
                        FixedStringLength = obj.ToString().Length
                    End If
                End If
                If FixedStringLength = 0 Then
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidFixedLengthString))
                ElseIf FixedStringLength > 0 Then
                    FixedBlankString = StrDup(FixedStringLength, " "c)
                    FixedCharArray = FixedBlankString.ToCharArray() 'Used for padding
                End If
            End If

            Dim vtByteLength As Integer = GetByteLength(vtype)
            ' Only attempt to write data down as a byte array for improved performance if:
            '   1. 1-Dimension array.
            '   2. Array is of the supported type (see GetByteLength).
            '   3. The given bound (iUpperElement - fixed size array) is the same as real size of the array (ArrUBound).
            '       (The first check at the start of the array ensure that iUpperElement (FirstBound) will never < ArrUBound.
            If (SecondBound = -1) AndAlso (vtByteLength > 0) AndAlso (iUpperElementY = ArrUBoundY) Then
                ' Calculate the total byte length we're writing down.
                Dim totalLength As Integer = vtByteLength * (iUpperElementY + 1)
                ' The totalLength has to be less than the record length (See LengthCheck).
                If GetPos() + totalLength <= m_lRecordStart + m_lRecordLen Then
                    Dim byteArr(totalLength - 1) As Byte
                    System.Buffer.BlockCopy(arr, 0, byteArr, 0, totalLength)
                    m_bw.Write(byteArr)
                    m_position += totalLength
                    Return
                End If
            End If

            For iElementX = 0 To iUpperElementX
                For iElementY = 0 To iUpperElementY
                    Try
                        If SecondBound = -1 Then
                            If iElementY > ArrUBoundY Then
                                obj = Nothing
                            Else
                                obj = arr.GetValue(iElementY)
                            End If
                        Else
                            If iElementY > ArrUBoundY OrElse iElementX > ArrUBoundX Then
                                obj = Nothing
                            Else
                                'These are supposed to be ordered Y, X 
                                ' because of the order VB6 writes out
                                obj = arr.GetValue(iElementY, iElementX)
                            End If
                        End If
                    Catch Ex As IndexOutOfRangeException
                        'The VBFixedArrayAttribute size must be larger than the array, pad it.
                        obj = 0
                    End Try

                    Select Case vtype

                        Case VT.DBNull, VT.Empty
                            'Nothing

                        Case VT.Byte    '1 byte
                            LengthCheck(1)
                            m_bw.Write(ByteType.FromObject(obj))
                            m_position += 1

                        Case VT.Short '2 bytes
                            LengthCheck(2)
                            m_bw.Write(ShortType.FromObject(obj))
                            m_position += 2

                        Case VT.Boolean '2 bytes
                            LengthCheck(2)
                            Dim b As Boolean = BooleanType.FromObject(obj)

                            If b Then
                                m_bw.Write(CShort(-1))
                            Else
                                m_bw.Write(CShort(0))
                            End If
                            m_position += 2

                        Case VT.Integer '4 Bytes
                            LengthCheck(4)
                            m_bw.Write(IntegerType.FromObject(obj))
                            m_position += 4

                        Case VT.Long    '8 Bytes
                            LengthCheck(8)
                            m_bw.Write(LongType.FromObject(obj))
                            m_position += 8

                        Case VT.Single   '4 bytes
                            LengthCheck(4)
                            m_bw.Write(SingleType.FromObject(obj))
                            m_position += 4

                        Case VT.Error    '4 bytes
                            Throw VbMakeException(vbErrors.TypeMismatch)

                        Case VT.Double   '8 bytes
                            LengthCheck(8)
                            m_bw.Write(DoubleType.FromObject(obj))
                            m_position += 8

                        Case VT.Date     '8 bytes
                            LengthCheck(8)
                            m_bw.Write(CDbl(DateType.FromObject(obj).ToOADate()))
                            m_position += 8

                        Case VT.Decimal  '8 bytes
                            LengthCheck(8)
                            m_bw.Write(System.Decimal.ToOACurrency(DecimalType.FromObject(obj)))
                            m_position += 8

                        Case VT.String
                            Dim ByteLength As Integer

                            If obj Is Nothing Then
                                If FixedStringLength > 0 Then
                                    sTemp = FixedBlankString
                                    ByteLength = FixedStringLength
                                    Debug.Assert(m_Encoding.GetByteCount(sTemp) = ByteLength)
                                Else
                                    sTemp = ""
                                    ByteLength = 0
                                End If
                            Else
                                sTemp = obj.ToString()
                                Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
                                ByteLength = m_Encoding.GetByteCount(sTemp)

                                If FixedStringLength > 0 AndAlso ByteLength > FixedStringLength Then
                                    'We need to truncate the string to the fixed string length (in bytes, not characters)
                                    If ByteLength = sTemp.Length Then
                                        'SBCS or DBCS but the string contains only SBCS characters
                                        sTemp = Microsoft.VisualBasic.Left(sTemp, FixedStringLength)
                                        Debug.Assert(m_Encoding.GetByteCount(sTemp) = FixedStringLength)
                                        ByteLength = FixedStringLength
                                    Else
                                        'String contains multi-byte characters.  Truncate to 'FixedStringLength'
                                        '  bytes (if cuts off half of a DBCS character, that character 
                                        '  is replaced with a single Chr(0))
                                        Dim Bytes() As Byte = m_Encoding.GetBytes(sTemp)
                                        sTemp = m_Encoding.GetString(Bytes, 0, FixedStringLength)

                                        ByteLength = m_Encoding.GetByteCount(sTemp)
                                        Debug.Assert(ByteLength <= FixedStringLength)
                                    End If
                                End If
                            End If

                            If ByteLength > System.Int16.MaxValue Then
                                'Size for strings is 2 bytes, thus the Short.MaxValue limitation
                                Throw VbMakeException(New ArgumentException(GetResourceString(SR.FileIO_StringLengthExceeded)), vbErrors.IllegalFuncCall)
                            End If

                            'Do a length check and write out the length if not fixed length
                            If FixedStringLength > 0 Then
                                LengthCheck(FixedStringLength)
                                m_sw.Write(sTemp)
                                Debug.Assert(ByteLength = m_Encoding.GetByteCount(sTemp) AndAlso ByteLength <= FixedStringLength)
                                If ByteLength < FixedStringLength Then
                                    'Pad with spaces
                                    m_sw.Write(FixedCharArray, 0, FixedStringLength - ByteLength)
                                End If
                                m_position += FixedStringLength
                            Else
                                LengthCheck(ByteLength + 2)
                                m_bw.Write(CShort(ByteLength))
                                m_sw.Write(sTemp)
                                m_position += (2 + ByteLength)
                            End If

                        Case VT.Char   '2 bytes
                            LengthCheck(2)
                            m_bw.Write(CharType.FromObject(obj))
                            m_position += 2

                        Case VT.Variant
                            PutObject(obj, 0, True)

                        Case VT.Structure
                            PutObject(obj, 0, False)

                        Case Else
                            If (vtype And VT.Array) <> 0 Then
                                'Arrays of arrays not supported
                                Throw VbMakeException(vbErrors.TypeMismatch)
                            Else
                                Throw VbMakeException(vbErrors.InvalidTypeLibVariable)
                            End If

                            vtype = vtype Xor VT.Array

                            If vtype = VT.Variant Then
                                Throw VbMakeException(vbErrors.TypeMismatch)
                            End If

                            If vtype > VT.Variant AndAlso (vtype <> VT.Byte AndAlso vtype <> VT.Decimal AndAlso vtype <> VT.Char AndAlso vtype <> VT.Long) Then
                                Throw VbMakeException(vbErrors.InvalidTypeLibVariable)
                            End If
                    End Select
                Next iElementY
            Next iElementX
        End Sub

        Friend Sub GetArrayData(ByVal arr As System.Array, ByVal typ As System.Type, Optional ByVal FirstBound As Integer = -1,
            Optional ByVal SecondBound As Integer = -1, Optional ByVal FixedStringLength As Integer = -1)

            Dim vtype As VT
            Dim obj As Object = Nothing
            Dim iElementX As Integer
            Dim iElementY As Integer
            Dim iUpperElementX As Integer
            Dim iUpperElementY As Integer

            If arr Is Nothing Then
                Throw New ArgumentException(GetResourceString(SR.Argument_ArrayNotInitialized))
            End If

            If typ Is Nothing Then
                typ = arr.GetType().GetElementType()
            End If
            vtype = VTFromComType(typ)

            If SecondBound = -1 Then
                iUpperElementX = 0
                iUpperElementY = FirstBound
            Else
                iUpperElementX = SecondBound
                iUpperElementY = FirstBound
            End If

            Dim vtByteLength As Integer = GetByteLength(vtype)
            ' Only attempt to read data as a byte array for improved performance if:
            '   1. 1-Dimension array.
            '   2. Array is of the supported type (see GetByteLength).
            '   3. The given bound (iUpperElement - fixed size array) is the same as the real size of the array.
            If (SecondBound = -1) AndAlso (vtByteLength > 0) AndAlso (iUpperElementY = arr.GetUpperBound(0)) Then
                ' Calculate the total byte length we're reading.
                Dim totalLength As Integer = vtByteLength * (iUpperElementY + 1)
                ' The totalLength has to be less than the length in byte of the array.
                If totalLength <= arr.Length * vtByteLength Then
                    System.Buffer.BlockCopy(m_br.ReadBytes(totalLength), 0, arr, 0, totalLength)
                    m_position += totalLength
                    Return
                End If
            End If

            For iElementX = 0 To iUpperElementX
                For iElementY = 0 To iUpperElementY
                    Select Case vtype
                        Case VT.DBNull, VT.Empty
                            'Nothing
                        Case VT.Byte    '1 byte
                            obj = m_br.ReadByte()
                            m_position += 1
                        Case VT.Short   '2 bytes
                            obj = m_br.ReadInt16()
                            m_position += 2
                        Case VT.Boolean '2 bytes
                            obj = CBool(m_br.ReadInt16())
                            m_position += 2
                        Case VT.Integer  '4 Bytes
                            obj = m_br.ReadInt32()
                            m_position += 4
                        Case VT.Long     '8 Bytes
                            obj = m_br.ReadInt64()
                            m_position += 8
                        Case VT.Single   '4 bytes
                            obj = m_br.ReadSingle()
                            m_position += 4
                        Case VT.Error    '4 bytes
                            'consider error case
                        Case VT.Double   '8 bytes
                            obj = m_br.ReadDouble()
                            m_position += 8
                        Case VT.Date     '8 bytes
                            obj = System.DateTime.FromOADate(m_br.ReadDouble())
                            m_position += 8
                        Case VT.Decimal  '8 bytes
                            Dim l As Long
                            l = m_br.ReadInt64()
                            m_position += 8
                            obj = System.Decimal.FromOACurrency(l)
                        Case VT.String
                            If FixedStringLength >= 0 Then
                                obj = ReadString(FixedStringLength)
                            Else
                                obj = ReadString()
                            End If
                        Case VT.Char
                            obj = m_br.ReadChar()
                            m_position += 1
                        Case VT.Variant
                            If SecondBound = -1 Then
                                obj = arr.GetValue(iElementY)
                            Else
                                obj = arr.GetValue(iElementY, iElementX)
                            End If

                            GetObject(obj, 0, True)
                        Case VT.Structure
                            If SecondBound = -1 Then
                                obj = arr.GetValue(iElementY)
                            Else
                                obj = arr.GetValue(iElementY, iElementX)
                            End If

                            GetObject(obj, 0, False)
                        Case Else
                            If (vtype And VT.Array) <> 0 Then
                                'OK
                            Else
                                Throw VbMakeException(vbErrors.InvalidTypeLibVariable)
                            End If

                            vtype = vtype Xor VT.Array

                            If vtype = VT.Variant Then
                                Throw VbMakeException(vbErrors.TypeMismatch)
                            End If

                            If vtype > VT.Variant AndAlso (vtype <> VT.Byte AndAlso vtype <> VT.Decimal AndAlso vtype <> VT.Char AndAlso vtype <> VT.Long) Then
                                Throw VbMakeException(vbErrors.InvalidTypeLibVariable)
                            End If
                    End Select

                    Try
                        If SecondBound = -1 Then
                            arr.SetValue(obj, iElementY)
                        Else
                            arr.SetValue(obj, iElementY, iElementX)
                        End If
                    Catch Ex As IndexOutOfRangeException
                        Throw New ArgumentException(GetResourceString(SR.Argument_ArrayDimensionsDontMatch))
                    End Try
                Next iElementY
            Next iElementX
        End Sub

        ''' ;GetByteLength
        ''' <summary>
        ''' This function is also used to check if a value type is supported for optimized FilePut in array case.
        ''' Given a VT value, determine the byte length of that type. Return -1 if that type is not supported.
        ''' </summary>
        Private Function GetByteLength(ByVal vtype As VT) As Integer
            Select Case vtype
                Case VT.Byte    '1 byte
                    Return 1
                Case VT.Short '2 bytes
                    Return 2
                Case VT.Integer '4 Bytes
                    Return 4
                Case VT.Long    '8 Bytes
                    Return 8
                Case VT.Single   '4 bytes
                    Return 4
                Case VT.Double   '8 bytes
                    Return 8
                Case Else
                    Return -1
            End Select
        End Function

        Private Sub PrintTab(ByVal ti As TabInfo)
            If ti.Column = -1 Then
                Dim CurColumn As Integer

                CurColumn = GetColumn()
                CurColumn += (14 - (CurColumn Mod 14))
                SetColumn(CurColumn)
            Else
                Tab(ti.Column)
            End If
        End Sub

        Private Function AddSpaces(ByVal s As String) As String
            Dim NegativeSign As String

            NegativeSign = Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NegativeSign

            If NegativeSign.Length = 1 Then
                If s.Chars(0) = NegativeSign.Chars(0) Then
                    'Append trailing space
                    Return s & " "
                End If
            ElseIf Left(s, NegativeSign.Length) = NegativeSign Then
                'Append trailing space
                Return s & " "
            End If

            'Append both leading and trailing space
            Return System.String.Concat(" ", s, " ")
        End Function

        Friend Sub PrintLine(ByVal ParamArray Output() As Object)
            Print(Output)
            WriteLine(Nothing)
        End Sub

        Friend Sub Print(ByVal ParamArray Output() As Object)
            Dim i As Integer
            Dim s As String
            Dim obj As Object
            Dim typ As Type
            Dim ParamCount As Integer
            Dim LastTabOrSpc As Integer

            SetPrintMode()

            If (Output Is Nothing) OrElse (Output.Length = 0) Then
                Exit Sub
            End If

            ParamCount = Output.GetUpperBound(0)
            LastTabOrSpc = -1

            For i = 0 To ParamCount
                s = Nothing
                obj = Output(i)

                If obj Is Nothing Then
                    typ = Nothing
                Else
                    typ = obj.GetType()
                    If typ.IsEnum() Then
                        typ = System.Enum.GetUnderlyingType(typ)
                    End If
                End If

                If obj Is Nothing Then
                    'Treat as empty
                    s = ""
                End If

                If typ Is Nothing Then
                    s = ""
                Else
                    Select Case Type.GetTypeCode(typ)
                        Case TypeCode.String
                            s = obj.ToString()
                        Case TypeCode.Int16
                            s = AddSpaces(StringType.FromShort(ShortType.FromObject(obj)))
                        Case TypeCode.Int32
                            s = AddSpaces(StringType.FromInteger(IntegerType.FromObject(obj)))
                        Case TypeCode.Int64
                            s = AddSpaces(StringType.FromLong(LongType.FromObject(obj)))
                        Case TypeCode.Byte
                            s = AddSpaces(StringType.FromByte(ByteType.FromObject(obj)))
                        Case TypeCode.DateTime
                            s = StringType.FromDate(DateType.FromObject(obj)) & " "
                        Case TypeCode.Double
                            s = AddSpaces(StringType.FromDouble(DoubleType.FromObject(obj)))
                        Case TypeCode.Single
                            s = AddSpaces(StringType.FromSingle(SingleType.FromObject(obj)))
                        Case TypeCode.Decimal
                            s = AddSpaces(StringType.FromDecimal(DecimalType.FromObject(obj)))
                        Case TypeCode.DBNull
                            s = "Null"
                        Case TypeCode.Boolean
                            s = StringType.FromBoolean(BooleanType.FromObject(obj))
                        Case TypeCode.Char
                            s = StringType.FromChar(CharType.FromObject(obj))
                        Case Else
                            If typ Is GetType(TabInfo) Then
                                PrintTab(CType(obj, TabInfo))
                                LastTabOrSpc = i
                                Continue For
                            ElseIf typ Is GetType(SpcInfo) Then
                                SPC(CType(obj, SpcInfo).Count)
                                LastTabOrSpc = i
                                Continue For
                            ElseIf typ Is GetType(System.Reflection.Missing) Then
                                s = "Error 448"
                            Else
                                Throw New ArgumentException(GetResourceString(SR.Argument_UnsupportedIOType1, VBFriendlyName(typ)))
                            End If
                    End Select
                End If

                If LastTabOrSpc <> (i - 1) Then
                    Dim lCurPos As Integer
                    lCurPos = GetColumn()
                    SetColumn(lCurPos + (14 - (lCurPos Mod 14)))
                End If
                WriteString(s)
            Next i
        End Sub

        Friend Sub WriteLineHelper(ByVal ParamArray Output() As Object)
            InternalWriteHelper(Output)
            WriteLine(Nothing)
        End Sub

        Friend Sub WriteHelper(ByVal ParamArray Output() As Object)
            InternalWriteHelper(Output)
            WriteString(",")
        End Sub

        Private Sub InternalWriteHelper(ByVal ParamArray Output() As Object)
            Dim SpcInfoType As Type = GetType(SpcInfo)
            Dim CurrentType As Type = SpcInfoType
            Dim value As Object
            Dim i As Integer

            'Always write in invariant format for cross culture compatibility
            Dim InvariantNumberFormat As NumberFormatInfo = GetInvariantCultureInfo().NumberFormat

            For i = 0 To Output.GetUpperBound(0)
                value = Output(i)

                If value Is Nothing Then
                    WriteString("#ERROR 448#")
                Else
                    If Not (CurrentType Is SpcInfoType) Then
                        WriteString(",")
                    End If

                    CurrentType = value.GetType()

                    If CurrentType Is SpcInfoType Then
                        SPC(CType(value, SpcInfo).Count)
                    ElseIf CurrentType Is GetType(TabInfo) Then
                        Dim ti As TabInfo = CType(value, TabInfo)

                        If ti.Column >= 0 Then
                            PrintTab(ti)
                        End If
                    ElseIf CurrentType Is GetType(System.Reflection.Missing) Then
                        WriteString("#ERROR 448#")
                    Else
                        Select Case Type.GetTypeCode(CurrentType)
                            Case TypeCode.String
                                WriteString(GetQuotedString(value.ToString()))
                            Case TypeCode.Int16
                                WriteString(StringType.FromShort(ShortType.FromObject(value)))
                            Case TypeCode.Int32
                                WriteString(StringType.FromInteger(IntegerType.FromObject(value)))
                            Case TypeCode.Int64
                                WriteString(StringType.FromLong(LongType.FromObject(value)))
                            Case TypeCode.Byte
                                WriteString(StringType.FromByte(ByteType.FromObject(value)))
                            Case TypeCode.DateTime
                                WriteString(FormatUniversalDate(DateType.FromObject(value)))
                            Case TypeCode.Double
                                WriteString(IOStrFromDouble(DoubleType.FromObject(value), InvariantNumberFormat))
                            Case TypeCode.Single
                                WriteString(IOStrFromSingle(SingleType.FromObject(value), InvariantNumberFormat))
                            Case TypeCode.Decimal
                                WriteString(IOStrFromDecimal(DecimalType.FromObject(value), InvariantNumberFormat))
                            Case TypeCode.DBNull
                                WriteString("#NULL#")
                            Case TypeCode.Boolean
                                If BooleanType.FromObject(value) Then
                                    WriteString("#TRUE#")
                                Else
                                    WriteString("#FALSE#")
                                End If
                            Case TypeCode.Char
                                WriteString(StringType.FromChar(CharType.FromObject(value)))
                            Case Else
                                ' consider support for UDT
                                If TypeOf value Is Char() AndAlso CType(value, Array).Rank = 1 Then
                                    WriteString(CStr(CharArrayType.FromObject(value)))
                                Else
                                    Throw VbMakeException(vbErrors.IllegalFuncCall)
                                End If
                        End Select
                    End If
                End If
            Next
        End Sub

        Private Function IOStrFromSingle(ByVal Value As Single, ByVal NumberFormat As NumberFormatInfo) As String
            Return Value.ToString(Nothing, NumberFormat)
        End Function

        Private Function IOStrFromDouble(ByVal Value As Double, ByVal NumberFormat As NumberFormatInfo) As String
            Return Value.ToString(Nothing, NumberFormat)
        End Function

        Private Function IOStrFromDecimal(ByVal Value As Decimal, ByVal NumberFormat As NumberFormatInfo) As String
            Return Value.ToString("G29", NumberFormat)
        End Function

        Friend Function FormatUniversalDate(ByVal dt As Date) As String
            Dim bHasDate As Boolean
            Dim sFormat As String

            'sb = New StringBuilder("#", 24)

            sFormat = sTimeFormat

            '  only insert date If not at the "start of time" (1/1/0)

            If (dt.Year <> 0 OrElse dt.Month <> 1 OrElse dt.Day <> 1) Then
                bHasDate = True
                sFormat = sDateFormat
            End If

            '  only insert time If not midnight (00:00:00)
            If ((dt.Hour + dt.Minute + dt.Second) <> 0) Then
                '  insert space separator If date was output
                If bHasDate Then
                    sFormat = sDateTimeFormat
                End If
            End If

            Return dt.ToString(sFormat, m_WriteDateFormatInfo)

            '    sb.Append("#")
            '    FormatUniversalDate = sb.ToString()
        End Function

        Protected Function GetQuotedString(ByVal Value As String) As String
            'Wrap Value with quotes, but make sure to escape quotes contained in Value.
            Return """" & Value.Replace("""", """""") & """"
        End Function

        Protected Sub ValidateRec(ByVal RecordNumber As Long)
            If RecordNumber < 1 Then
                Throw VbMakeException(vbErrors.BadRecordNum)
            End If
        End Sub

        Friend Overridable Sub GetObject(ByRef Value As Object, Optional ByVal RecordNumber As Long = 0, Optional ByVal ContainedInVariant As Boolean = True)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As ValueType, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As System.Array, Optional ByVal RecordNumber As Long = 0,
            Optional ByVal ArrayIsDynamic As Boolean = False, Optional ByVal StringIsFixedLength As Boolean = False)

            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Boolean, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Byte, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Short, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Integer, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Long, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Char, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Single, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Double, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Decimal, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As String, Optional ByVal RecordNumber As Long = 0, Optional ByVal StringIsFixedLength As Boolean = False)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub [Get](ByRef Value As Date, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Sub PutObject(ByVal Value As Object, Optional ByVal RecordNumber As Long = 0, Optional ByVal ContainedInVariant As Boolean = True)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Object, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As ValueType, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As System.Array, Optional ByVal RecordNumber As Long = 0,
            Optional ByVal ArrayIsDynamic As Boolean = False, Optional ByVal StringIsFixedLength As Boolean = False)

            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Boolean, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Byte, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Short, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Integer, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Long, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Char, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Single, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Double, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Decimal, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As String, Optional ByVal RecordNumber As Long = 0, Optional ByVal StringIsFixedLength As Boolean = False)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Put(ByVal Value As Date, Optional ByVal RecordNumber As Long = 0)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        '======================================
        ' Input
        '======================================
        Friend Overridable Overloads Sub Input(ByRef obj As Object)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Boolean)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Byte)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Short)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Integer)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Long)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Char)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Single)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Double)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Decimal)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As String)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Friend Overridable Overloads Sub Input(ByRef Value As Date)
            Throw VbMakeException(vbErrors.BadFileMode)
        End Sub

        Protected Function SkipWhiteSpace() As Integer
            Dim lChar As Integer = m_sr.Peek()

            If CheckEOF(lChar) Then
                m_eof = True
                GoTo SkipWhiteSpaceExit
            End If

            Do While (IntlIsSpace(lChar) OrElse (lChar = lchTab))
                m_sr.Read()
                m_position += 1
                lChar = m_sr.Peek()

                If CheckEOF(lChar) Then
                    m_eof = True
                    Exit Do
                End If
            Loop

SkipWhiteSpaceExit:
            Return lChar
        End Function

        Private Function GetFileInTerm(ByVal iTermType As Short) As String
            Select Case iTermType
                Case FIN_NUMTERMCHAR
                    GetFileInTerm = " ," & ControlChars.Tab & ControlChars.Cr
                Case FIN_LINEINP
                    GetFileInTerm = ControlChars.Cr
                Case FIN_QSTRING
                    GetFileInTerm = chDblQuote
                Case FIN_STRING
                    GetFileInTerm = "," & ControlChars.Cr
                Case FIN_NUMBER
                    GetFileInTerm = " ," & ControlChars.Tab & ControlChars.Cr
                Case Else
                    Throw VbMakeException(vbErrors.IllegalFuncCall)
            End Select
        End Function

        Protected Function IntlIsSpace(ByVal lch As Integer) As Boolean
            ' consider testing for intl spaces
            Return (lch = lchSpace) Or (lch = lchIntlSpace)
        End Function

        Protected Function IntlIsDoubleQuote(ByVal lch As Integer) As Boolean
            ' consider testing for intl double quotes
            Return (lch = lchDoubleQuote)
        End Function

        Protected Function IntlIsComma(ByVal lch As Integer) As Boolean
            ' consider testing for intl commas
            Return (lch = lchComma)
        End Function

        Protected Function SkipWhiteSpaceEOF() As Integer
            Dim retValue As Integer = SkipWhiteSpace()

            If CheckEOF(retValue) Then
                Throw VbMakeException(vbErrors.EndOfFile)
            End If
            Return retValue
        End Function

        Protected Sub SkipTrailingWhiteSpace()
            Dim lChar As Integer

            '  get the field termination character
            lChar = m_sr.Peek()
            If CheckEOF(lChar) Then
                m_eof = True
                Exit Sub
            End If

            '  If field was teminated by space/tab (numeric) or quote
            '  quoted-string, scan ahead over any further spaces/tabs
            If (IntlIsSpace(lChar) OrElse IntlIsDoubleQuote(lChar) OrElse lChar = lchTab) Then
                lChar = m_sr.Read() 'Remove it
                m_position += 1

                'Remove any remaining whitespace
                lChar = m_sr.Peek()
                If CheckEOF(lChar) Then
                    m_eof = True
                    Exit Sub
                End If

                Do While (IntlIsSpace(lChar) OrElse (lChar = lchTab))
                    m_sr.Read() 'Remove it
                    m_position += 1
                    lChar = m_sr.Peek() 'Look at next char

                    If CheckEOF(lChar) Then
                        m_eof = True
                        Exit Sub
                    End If
                Loop
            End If

            '  If a carriage-return terminates the field, scan over
            '  a following line-feed If there
            If (lChar = lchCR) Then
                lChar = m_sr.Read()
                m_position += 1

                If CheckEOF(lChar) Then
                    m_eof = True
                    Exit Sub
                End If

                If (m_sr.Peek() = lchLF) Then
                    lChar = m_sr.Read()
                    m_position += 1
                End If
            ElseIf IntlIsComma(lChar) Then
                ' Go past the comma
                lChar = m_sr.Read()
                m_position += 1
            End If

            lChar = m_sr.Peek()
            If CheckEOF(lChar) Then
                m_eof = True
                Exit Sub
            End If
        End Sub

        Protected Function ReadInField(ByVal iTermType As Short) As String
            Dim sTermChars As String
            Dim lChar As Integer
            Dim sb As StringBuilder

            sb = New StringBuilder
            sTermChars = GetFileInTerm(iTermType)

            ' Peek at the first character
            lChar = m_sr.Peek()
            If CheckEOF(lChar) Then
                m_eof = True
            Else
                Do While (sTermChars.IndexOf(ChrW(lChar)) = -1)
                    lChar = m_sr.Read()
                    m_position += 1

                    If lChar <> 0 Then
                        sb.Append(ChrW(lChar))
                    End If

                    lChar = m_sr.Peek()

                    If CheckEOF(lChar) Then
                        m_eof = True
                        Exit Do
                    End If
                Loop
            End If

            '  if no error, finish up
            '  if reading a string, or field string exists,
            '  append buffer to string.
            '  if the string is not quoted, and we are not
            '  in line-input mode, then RTrim the string.
            If (iTermType = FIN_STRING OrElse iTermType = FIN_NUMBER) Then
                ReadInField = RTrim(sb.ToString())
            Else
                ReadInField = sb.ToString()
            End If
        End Function

        Protected Function CheckEOF(ByVal lChar As Integer) As Boolean
            Return (lChar = EOF_INDICATOR OrElse lChar = EOF_CHAR)
        End Function

        ' The NullReferenceException is for compatibility with VB6 which threw a NullReferenceException when
        ' reading from a file that was write-only. The inner exception was added to provide more context.
        Private Sub ValidateReadable()
            If (m_access <> OpenAccess.ReadWrite) AndAlso (m_access <> OpenAccess.Read) Then
                Dim JustNeedTheMessage As New NullReferenceException ' We don't have access to the localized resources for this string.
                Throw New NullReferenceException(JustNeedTheMessage.Message, New IO.IOException(GetResourceString(SR.FileOpenedNoRead)))
            End If
        End Sub

    End Class

End Namespace
