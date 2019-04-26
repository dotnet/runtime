' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Security
Imports System.IO

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Friend Class VB6InputFile

        '============================================================================
        ' Declarations
        '============================================================================

        Inherits VB6File

        '============================================================================
        ' Constructor
        '============================================================================
        Public Sub New(ByVal FileName As String, ByVal share As OpenShare)
            MyBase.New(FileName, OpenAccess.Read, share, -1)
        End Sub

        '============================================================================
        ' Operations
        '============================================================================
        Friend Overrides Sub OpenFile()
            Try
                m_file = New FileStream(m_sFullPath, FileMode.Open, CType(m_access, FileAccess), CType(m_share, FileShare))
            Catch ex As FileNotFoundException
                Throw VbMakeException(ex, vbErrors.FileNotFound)
            Catch ex As SecurityException
                Throw VbMakeException(vbErrors.FileNotFound)
            Catch ex As DirectoryNotFoundException
                Throw VbMakeException(ex, vbErrors.PathNotFound)
            Catch ex As IOException
                Throw VbMakeException(ex, vbErrors.PathFileAccess)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch ex As System.Threading.ThreadAbortException
                Throw ex
            Catch ex As Exception
                Throw VbMakeException(ex, vbErrors.PathNotFound)
            End Try

            m_Encoding = GetFileIOEncoding()
            m_sr = New StreamReader(m_file, m_Encoding, False, 128)
            m_eof = (m_file.Length = 0)  'Don't do a Peek here or it will buffer data, causing side-effects with the Lock function.
        End Sub

        Public Function ReadLine() As String
            Dim s As String
            s = m_sr.ReadLine()
            Diagnostics.Debug.Assert(Not m_Encoding Is Nothing)
            m_position += m_Encoding.GetByteCount(s) + 2
            'It appears that no one is calling this function. It has returned nothing
            ' since it was created, so keep it that way for compatibility reasons.
            Return Nothing
        End Function

        Friend Overrides Function CanInput() As Boolean
            Return True
        End Function

        Friend Overrides Function EOF() As Boolean
            Return m_eof
        End Function

        Public Overrides Function GetMode() As OpenMode
            Return OpenMode.Input
        End Function

        Friend Function ParseInputString(ByRef sInput As String) As Object
            ParseInputString = sInput

            '  variant must have last character as a pound sign
            '  that is different than the first
            If sInput.Chars(0) = CChar("#") AndAlso sInput.Length <> 1 Then
                '  isolate the string between the pound signs
                sInput = sInput.Substring(1, sInput.Length - 2)

                '  test for fixed string values first
                '  VT_EMPTY is not converted
                If sInput = "NULL" Then
                    ParseInputString = DBNull.Value
                ElseIf sInput = "TRUE" Then
                    ParseInputString = CObj(True)
                ElseIf sInput = "FALSE" Then
                    ParseInputString = CObj(False)
                ElseIf Left(sInput, 6) = "ERROR " Then
                    '   parse I4 value after "ERROR " string
                    Dim errValue As Integer

                    If sInput.Length > 6 Then
                        errValue = IntegerType.FromString(Mid(sInput, 7))
                    End If

                    ' error value is assigned to the input string for now
                    ParseInputString = errValue

                    '  test for date variant.  Note, input always uses the
                    '    universal date format; so use english LCID (0x40) for
                    '    coercion.
                    ' CALENDAR_SUPPORT
                Else
                    Try
                        ParseInputString = System.DateTime.Parse(ToHalfwidthNumbers(sInput, GetCultureInfo()))
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch ex As System.Threading.ThreadAbortException
                        Throw ex
                    Catch e As Exception
                    End Try
                End If
            End If
        End Function

        '======================================
        ' Input
        '======================================
        Friend Overloads Overrides Sub Input(ByRef obj As Object)
            Dim lChar As Integer
            Dim sField As String

            lChar = SkipWhiteSpaceEOF() 'Skip over leading whitespace

            If lChar = lchDoubleQuote Then
                lChar = m_sr.Read()
                m_position += 1

                obj = ReadInField(FIN_QSTRING)
                SkipTrailingWhiteSpace()
            ElseIf lChar = lchPound Then
                obj = ParseInputString(InputStr())
            Else
                sField = ReadInField(FIN_NUMBER)
                obj = ParseInputField(sField, VariantType.Empty)
                SkipTrailingWhiteSpace()
            End If
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Boolean)
            Value = BooleanType.FromObject(ParseInputString(InputStr()))
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

        Friend Overloads Overrides Sub Input(ByRef Value As Char)
            Dim s As String = InputStr()

            If s.Length > 0 Then
                Value = s.Chars(0)
            Else
                Value = ControlChars.NullChar
            End If
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Single)
            Value = SingleType.FromObject(InputNum(VariantType.Single), GetInvariantCultureInfo().NumberFormat)
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Double)
            Value = DoubleType.FromObject(InputNum(VariantType.Double), GetInvariantCultureInfo().NumberFormat)
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Decimal)
            Value = DecimalType.FromObject(InputNum(VariantType.Decimal), GetInvariantCultureInfo().NumberFormat)
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As String)
            Value = InputStr()
        End Sub

        Friend Overloads Overrides Sub Input(ByRef Value As Date)
            Value = DateType.FromObject(ParseInputString(InputStr()))
        End Sub

        Friend Overrides Function LOC() As Long
            'This calculation depends on the buffersize of the FileStream 
            'object, any changes in the urt classes could mess this up
            ' The FileStream is used by the StreamReader, which reads ahead
            ' into the 128 byte buffer specified when the StreamReader was created
            ' The m_file.Position is where the reader has read to, not the vb user
            'm_position tracks where the vb user has read to.
            Return ((m_position + 127) \ 128)
        End Function

    End Class

End Namespace
