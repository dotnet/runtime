' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.IO
Imports System.Runtime.Versioning

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
    Friend Class VB6RandomFile

        '============================================================================
        ' Declarations
        '============================================================================

        Inherits VB6File

        '============================================================================
        ' Constructor
        '============================================================================
        Public Sub New(ByVal FileName As String, ByVal access As OpenAccess, ByVal share As OpenShare, ByVal lRecordLen As Integer)
            MyBase.New(FileName, access, share, lRecordLen)
        End Sub

        '============================================================================
        ' Operations
        '============================================================================
        Private Sub OpenFileHelper(ByVal fm As FileMode, ByVal fa As OpenAccess)
            Try
                m_file = New FileStream(m_sFullPath, fm, CType(fa, FileAccess), CType(m_share, FileShare))
            Catch ex As FileNotFoundException
                Throw VbMakeException(ex, vbErrors.FileNotFound)
            Catch ex As DirectoryNotFoundException
                Throw VbMakeException(ex, vbErrors.PathNotFound)
            Catch ex As Security.SecurityException
                Throw VbMakeException(ex, vbErrors.FileNotFound)
            Catch ex As IOException
                Throw VbMakeException(ex, vbErrors.PathFileAccess)
            Catch ex As UnauthorizedAccessException
                Throw VbMakeException(ex, vbErrors.PathFileAccess)
            Catch ex As ArgumentException 'Invalid combination of FileMode and OpenAccess
                Throw VbMakeException(ex, vbErrors.PathFileAccess)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch ex As Exception
                Throw VbMakeException(vbErrors.InternalError)
            End Try
        End Sub

        Friend Overrides Sub OpenFile()
            Dim fm As FileMode
            Dim stm As Stream

            'Attempt the following 
            If File.Exists(m_sFullPath) Then
                fm = FileMode.Open
            ElseIf m_access = OpenAccess.Read Then
                fm = FileMode.OpenOrCreate
            Else
                fm = FileMode.Create
            End If

            If m_access = OpenAccess.Default Then
                'Must try ReadWrite/Write then Read
                m_access = OpenAccess.ReadWrite

                Try
                    OpenFileHelper(fm, m_access)
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch
                    'Try Write access
                    m_access = OpenAccess.Write
                    Try
                        OpenFileHelper(fm, m_access)
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch
                        'If that failed, try read access
                        m_access = OpenAccess.Read
                        OpenFileHelper(fm, m_access)
                    End Try
                End Try
            Else
                OpenFileHelper(fm, m_access)
            End If

            m_Encoding = GetFileIOEncoding()
            stm = m_file

            If (m_access = OpenAccess.Write) OrElse (m_access = OpenAccess.ReadWrite) Then
                m_sw = New StreamWriter(stm, m_Encoding)
                m_sw.AutoFlush = True
                m_bw = New BinaryWriter(stm, m_Encoding)
            End If

            If (m_access = OpenAccess.Read) OrElse (m_access = OpenAccess.ReadWrite) Then
                m_br = New BinaryReader(stm, m_Encoding)

                If GetMode() = OpenMode.Binary Then
                    ' pass false to prevent detection of encoding marks
                    m_sr = New StreamReader(stm, m_Encoding, False, 128)
                End If
            End If
        End Sub

        Friend Overrides Sub CloseFile()
            If Not m_sw Is Nothing Then
                m_sw.Flush()
            End If
            CloseTheFile()
        End Sub

        <UnsupportedOSPlatform("macos")> 
        Friend Overloads Overrides Sub Lock(ByVal lStart As Long, ByVal lEnd As Long)
            If lStart > lEnd Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Start"))
            End If

            Dim lStartByte As Long
            Dim lLength As Long

            lStartByte = (lStart - 1) * m_lRecordLen
            lLength = (lEnd - lStart + 1) * m_lRecordLen

            m_file.Lock(lStartByte, lLength)
        End Sub

        <UnsupportedOSPlatform("macos")> 
        Friend Overloads Overrides Sub Unlock(ByVal lStart As Long, ByVal lEnd As Long)
            If lStart > lEnd Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Start"))
            End If

            Dim lStartByte As Long
            Dim lLength As Long

            lStartByte = (lStart - 1) * m_lRecordLen
            lLength = (lEnd - lStart + 1) * m_lRecordLen
            m_file.Unlock(lStartByte, lLength)
        End Sub

        Public Overrides Function GetMode() As OpenMode
            GetMode = OpenMode.Random
        End Function

        Friend Overrides Function GetStreamReader() As StreamReader
            GetStreamReader = New StreamReader(m_file, m_Encoding)
        End Function

        Friend Overrides Function EOF() As Boolean
            m_eof = (m_position >= m_file.Length)
            Return m_eof
        End Function

        Friend Overrides Function LOC() As Long
            If m_lRecordLen = 0 Then
                Throw VbMakeException(vbErrors.InternalError)
            Else
                Dim pos As Long
                pos = m_position
                Return (pos + m_lRecordLen - 1) \ m_lRecordLen
            End If
        End Function

        Friend Overloads Overrides Sub Seek(ByVal Position As Long)
            SetRecord(Position)
        End Sub

        Friend Overloads Overrides Function Seek() As Long
            Return (LOC() + 1)
        End Function

        '======================================
        ' Get
        '======================================
        Friend Overrides Sub GetObject(ByRef Value As Object, Optional ByVal RecordNumber As Long = 0,
            Optional ByVal ContainedInVariant As Boolean = True)

            Dim typ As System.Type = Nothing
            Dim vtype As VT

            ValidateReadable()
            SetRecord(RecordNumber)

            If ContainedInVariant Then
                vtype = CType(m_br.ReadInt16(), VT)
                m_position += 2
            Else
                typ = Value.GetType

                Select Case Type.GetTypeCode(typ)
                    Case TypeCode.String
                        vtype = VT.String
                    Case TypeCode.Int16
                        vtype = VT.Short
                    Case TypeCode.Int32
                        vtype = VT.Integer
                    Case TypeCode.Int64
                        vtype = VT.Long
                    Case TypeCode.Byte
                        vtype = VT.Byte
                    Case TypeCode.DateTime
                        vtype = VT.Date
                    Case TypeCode.Double
                        vtype = VT.Double
                    Case TypeCode.Single
                        vtype = VT.Single
                    Case TypeCode.Decimal
                        vtype = VT.Decimal
                    Case TypeCode.Boolean
                        vtype = VT.Boolean
                    Case TypeCode.Char
                        vtype = VT.Char
                    Case TypeCode.Object
                        If typ.IsValueType Then
                            vtype = VT.Structure
                        Else
                            vtype = VT.Variant 'To force an exception later
                        End If
                    Case Else
                        vtype = VT.Variant  'To force an exception later
                End Select
            End If

            If (vtype And VT.Array) <> 0 Then
                Dim arr As System.Array = Nothing
                Dim v As VT = vtype Xor VT.Array
                GetDynamicArray(arr, ComTypeFromVT(v))
                Value = arr
            Else
                If vtype = VT.String Then
                    Value = GetLengthPrefixedString(0)
                ElseIf vtype = VT.Short Then
                    Value = GetShort(0)
                ElseIf vtype = VT.Integer Then
                    Value = GetInteger(0)
                ElseIf vtype = VT.Long Then
                    Value = GetLong(0)
                ElseIf vtype = VT.Byte Then
                    Value = GetByte(0)
                ElseIf vtype = VT.Date Then
                    Value = GetDate(0)
                ElseIf vtype = VT.Double Then
                    Value = GetDouble(0)
                ElseIf vtype = VT.Single Then
                    Value = GetSingle(0)
                ElseIf vtype = VT.Currency Then
                    Value = GetCurrency(0)
                ElseIf vtype = VT.Decimal Then
                    Value = GetDecimal(0)
                ElseIf vtype = VT.Boolean Then
                    Value = GetBoolean(0)
                ElseIf vtype = VT.Char Then
                    Value = GetChar(0)
                ElseIf vtype = VT.Structure Then
                    Dim valType As ValueType
                    valType = CType(Value, ValueType)
                    GetRecord(0, valType, False)
                    Value = valType
                ElseIf vtype = VT.DBNull AndAlso ContainedInVariant Then
                    Value = DBNull.Value
                ElseIf vtype = VT.DBNull Then
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedIOType1, "DBNull")), vbErrors.IllegalFuncCall)
                ElseIf vtype = VT.Empty Then
                    Value = Nothing
                ElseIf vtype = VT.Currency Then
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedIOType1, "Currency")), vbErrors.IllegalFuncCall)
                Else
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedIOType1, typ.FullName)), vbErrors.IllegalFuncCall)
                End If
            End If
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As ValueType, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            GetRecord(RecordNumber, Value, False)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As System.Array, Optional ByVal RecordNumber As Long = 0,
            Optional ByVal ArrayIsDynamic As Boolean = False, Optional ByVal StringIsFixedLength As Boolean = False)

            ValidateReadable()

            If (Value Is Nothing) Then
                Throw New ArgumentException(SR.Argument_ArrayNotInitialized)
            End If

            Dim typ As Type = Value.GetType().GetElementType
            Dim len As Integer = -1
            Dim obj As Object
            Dim cDims As Integer = Value.Rank()
            Dim FirstBound As Integer = -1
            Dim SecondBound As Integer = -1
            SetRecord(RecordNumber)

            If m_file.Position >= m_file.Length Then
                Return
            End If

            If StringIsFixedLength AndAlso (typ Is GetType(String)) Then
                'Use first element to determine fixed length
                If cDims = 1 Then
                    obj = Value.GetValue(0)
                ElseIf cDims = 2 Then
                    obj = Value.GetValue(0, 0)
                Else '0 or > 2
                    Throw New ArgumentException(SR.Argument_UnsupportedArrayDimensions)
                End If

                If obj Is Nothing Then
                    len = 0
                Else
                    len = DirectCast(obj, String).Length
                End If

                If len = 0 Then
                    Throw New ArgumentException(SR.Argument_InvalidFixedLengthString)
                End If
            End If

            If ArrayIsDynamic Then
                Value = GetArrayDesc(typ)
                cDims = Value.Rank()
            End If

            FirstBound = Value.GetUpperBound(0)

            If cDims = 1 Then
                'nothing to do
            ElseIf cDims = 2 Then
                SecondBound = Value.GetUpperBound(1)
            Else '0 or > 2
                Throw New ArgumentException(SR.Argument_UnsupportedArrayDimensions)
            End If

            If ArrayIsDynamic Then
                GetArrayData(Value, typ, FirstBound, SecondBound, len)
            Else
                GetFixedArray(RecordNumber, Value, typ, FirstBound, SecondBound, len)
            End If
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Boolean, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetBoolean(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Byte, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetByte(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Short, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetShort(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Integer, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetInteger(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Long, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetLong(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Char, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetChar(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Single, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetSingle(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Double, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetDouble(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Decimal, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetCurrency(RecordNumber)
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As String, Optional ByVal RecordNumber As Long = 0,
            Optional ByVal StringIsFixedLength As Boolean = False)

            ValidateReadable()

            If StringIsFixedLength Then
                Dim Length As Integer
                If Value Is Nothing Then
                    Length = 0
                Else
                    Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
                    Length = m_Encoding.GetByteCount(Value)
                End If
                Value = GetFixedLengthString(RecordNumber, Length)
            Else
                Value = GetLengthPrefixedString(RecordNumber)
            End If
        End Sub

        Friend Overloads Overrides Sub [Get](ByRef Value As Date, Optional ByVal RecordNumber As Long = 0)
            ValidateReadable()
            Value = GetDate(RecordNumber)
        End Sub

        Friend Overrides Sub PutObject(ByVal Value As Object, Optional ByVal RecordNumber As Long = 0,
            Optional ByVal ContainedInVariant As Boolean = True)

            Dim typ As Type

            ValidateWriteable()

            If Value Is Nothing Then
                'Put a VT_EMPTY
                PutEmpty(RecordNumber)
                Exit Sub
            End If

            typ = Value.GetType

            If typ Is Nothing Then
                Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedIOType1, "Empty")), vbErrors.IllegalFuncCall)
            ElseIf typ.IsArray Then
                PutDynamicArray(RecordNumber, CType(Value, System.Array))
                Exit Sub
            ElseIf typ.IsEnum Then
                typ = System.Enum.GetUnderlyingType(typ)
            End If

            Select Case Type.GetTypeCode(typ)
                Case TypeCode.String
                    PutVariantString(RecordNumber, Value.ToString())
                    Return
                Case TypeCode.Int16
                    PutShort(RecordNumber, ShortType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Int32
                    PutInteger(RecordNumber, IntegerType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Int64
                    PutLong(RecordNumber, LongType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Byte
                    PutByte(RecordNumber, ByteType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.DateTime
                    PutDate(RecordNumber, DateType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Double
                    PutDouble(RecordNumber, DoubleType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Single
                    PutSingle(RecordNumber, SingleType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Decimal
                    PutDecimal(RecordNumber, DecimalType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Boolean
                    PutBoolean(RecordNumber, BooleanType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.Char
                    PutChar(RecordNumber, CharType.FromObject(Value), ContainedInVariant)
                    Return
                Case TypeCode.DBNull
                    'Use PutShort since DBNull is only a two-byte vartype with no data
                    PutShort(RecordNumber, VT.DBNull, False)
                    Return
            End Select

            If typ Is GetType(System.Reflection.Missing) Then
                Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedIOType1, "Missing")), vbErrors.IllegalFuncCall)

            ElseIf typ.IsValueType() AndAlso Not ContainedInVariant Then
                PutRecord(RecordNumber, CType(Value, ValueType))

            ElseIf ContainedInVariant AndAlso typ.IsValueType Then
                Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_PutObjectOfValueType1, VBFriendlyName(typ, Value))), vbErrors.IllegalFuncCall)

            Else
                Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedIOType1, VBFriendlyName(typ, Value))), vbErrors.IllegalFuncCall)
            End If
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As ValueType, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutRecord(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As System.Array, Optional ByVal RecordNumber As Long = 0,
            Optional ByVal ArrayIsDynamic As Boolean = False, Optional ByVal StringIsFixedLength As Boolean = False)

            ValidateWriteable()

            If Value Is Nothing Then
                PutEmpty(RecordNumber)
                Return
            End If

            Dim FirstBound As Integer = Value.GetUpperBound(0)
            Dim SecondBound As Integer = -1
            Dim FixedStringLength As Integer = -1
            Dim typ As System.Type

            If Value.Rank = 2 Then
                SecondBound = Value.GetUpperBound(1)
            End If
            If StringIsFixedLength Then
                FixedStringLength = 0 'Fixed length string, but length calculated by Put function
            End If

            typ = Value.GetType().GetElementType()

            If ArrayIsDynamic Then
                PutDynamicArray(RecordNumber, Value, False, FixedStringLength)
            Else
                PutFixedArray(RecordNumber, Value, typ, FixedStringLength, FirstBound, SecondBound)
            End If
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Boolean, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutBoolean(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Byte, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutByte(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Short, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutShort(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Integer, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutInteger(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Long, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutLong(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Char, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutChar(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Single, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutSingle(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Double, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutDouble(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Decimal, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutCurrency(RecordNumber, Value)
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As String, Optional ByVal RecordNumber As Long = 0, Optional ByVal StringIsFixedLength As Boolean = False)
            ValidateWriteable()

            If StringIsFixedLength Then
                PutString(RecordNumber, Value)
            Else
                PutStringWithLength(RecordNumber, Value)
            End If
        End Sub

        Friend Overloads Overrides Sub Put(ByVal Value As Date, Optional ByVal RecordNumber As Long = 0)
            ValidateWriteable()
            PutDate(RecordNumber, Value)
        End Sub

        Protected Sub ValidateWriteable()
            If (m_access <> OpenAccess.ReadWrite) AndAlso (m_access <> OpenAccess.Write) Then
                Throw VbMakeExceptionEx(vbErrors.PathFileAccess, SR.FileOpenedNoWrite)
            End If
        End Sub

        Protected Sub ValidateReadable()
            If (m_access <> OpenAccess.ReadWrite) AndAlso (m_access <> OpenAccess.Read) Then
                Throw VbMakeExceptionEx(vbErrors.PathFileAccess, SR.FileOpenedNoRead)
            End If
        End Sub

    End Class

End Namespace
