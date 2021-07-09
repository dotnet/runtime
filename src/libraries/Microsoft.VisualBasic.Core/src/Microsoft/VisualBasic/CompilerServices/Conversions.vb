' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.ComponentModel
Imports System.Globalization
Imports System.Diagnostics
Imports System.Dynamic
Imports System.Reflection

Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.ConversionResolution
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils
Imports System.Diagnostics.CodeAnalysis

Namespace Microsoft.VisualBasic.CompilerServices

    <EditorBrowsable(EditorBrowsableState.Never)>
    Public NotInheritable Class Conversions
        Private Const ConversionsTrimmerMessage As String = "The Expression origin object cannot be statically analyzed and may be trimmed"

        Private Sub New()
        End Sub

        Public Shared Function ToBoolean(ByVal Value As String) As Boolean

            If Value Is Nothing Then
                Value = ""
            End If

            Try
                Dim loc As CultureInfo = GetCultureInfo()
                If String.Compare(Value, Boolean.FalseString, True, loc) = 0 Then
                    Return False
                ElseIf String.Compare(Value, Boolean.TrueString, True, loc) = 0 Then
                    Return True
                End If

                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CBool(i64Value)
                End If

                Return CBool(ParseDouble(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Boolean"), e)
            End Try

        End Function

        Public Shared Function ToBoolean(ByVal Value As Object) As Boolean

            If Value Is Nothing Then
                Return False
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CBool(DirectCast(Value, Boolean))
                        Else
                            Return CBool(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CBool(DirectCast(Value, SByte))
                        Else
                            Return CBool(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CBool(DirectCast(Value, Byte))
                        Else
                            Return CBool(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CBool(DirectCast(Value, Int16))
                        Else
                            Return CBool(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CBool(DirectCast(Value, UInt16))
                        Else
                            Return CBool(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CBool(DirectCast(Value, Int32))
                        Else
                            Return CBool(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CBool(DirectCast(Value, UInt32))
                        Else
                            Return CBool(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CBool(DirectCast(Value, Int64))
                        Else
                            'Using ToInt64 also handles enums
                            Return CBool(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CBool(DirectCast(Value, UInt64))
                        Else
                            Return CBool(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToBoolean(Nothing)
                        Else
                            Return CBool(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CBool(DirectCast(Value, Single))
                        Else
                            Return CBool(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CBool(DirectCast(Value, Double))
                        Else
                            Return CBool(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CBool(StringValue)
                        Else
                            Return CBool(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Boolean"))
        End Function

        Public Shared Function ToByte(ByVal Value As String) As Byte

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CByte(i64Value)
                End If

                Return CByte(ParseDouble(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Byte"), e)  'UNSIGNED: make these strings constants
            End Try

        End Function

        Public Shared Function ToByte(ByVal Value As Object) As Byte

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CByte(DirectCast(Value, Boolean))
                        Else
                            Return CByte(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CByte(DirectCast(Value, SByte))
                        Else
                            Return CByte(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CByte(DirectCast(Value, Byte))
                        Else
                            Return CByte(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CByte(DirectCast(Value, Int16))
                        Else
                            Return CByte(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CByte(DirectCast(Value, UInt16))
                        Else
                            Return CByte(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CByte(DirectCast(Value, Int32))
                        Else
                            Return CByte(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CByte(DirectCast(Value, UInt32))
                        Else
                            Return CByte(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CByte(DirectCast(Value, Int64))
                        Else
                            Return CByte(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CByte(DirectCast(Value, UInt64))
                        Else
                            Return CByte(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToByte(Nothing)
                        Else
                            Return CByte(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CByte(DirectCast(Value, Single))
                        Else
                            Return CByte(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CByte(DirectCast(Value, Double))
                        Else
                            Return CByte(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)
                        If StringValue IsNot Nothing Then
                            Return CByte(StringValue)
                        Else
                            Return CByte(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Byte"))

        End Function

        <CLSCompliant(False)>
        Public Shared Function ToSByte(ByVal Value As String) As SByte

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CSByte(i64Value)
                End If

                Return CSByte(ParseDouble(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "SByte"), e)
            End Try

        End Function

        <CLSCompliant(False)>
        Public Shared Function ToSByte(ByVal Value As Object) As SByte

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface Is Nothing Then
                GoTo ThrowInvalidCast
            End If

            ValueTypeCode = ValueInterface.GetTypeCode()

            Select Case ValueTypeCode

                Case TypeCode.Boolean
                    If TypeOf Value Is Boolean Then
                        Return CSByte(DirectCast(Value, Boolean))
                    Else
                        Return CSByte(ValueInterface.ToBoolean(Nothing))
                    End If

                Case TypeCode.SByte
                    If TypeOf Value Is SByte Then
                        Return CSByte(DirectCast(Value, SByte))
                    Else
                        Return CSByte(ValueInterface.ToSByte(Nothing))
                    End If

                Case TypeCode.Byte
                    If TypeOf Value Is Byte Then
                        Return CSByte(DirectCast(Value, Byte))
                    Else
                        Return CSByte(ValueInterface.ToByte(Nothing))
                    End If

                Case TypeCode.Int16
                    If TypeOf Value Is Int16 Then
                        Return CSByte(DirectCast(Value, Int16))
                    Else
                        Return CSByte(ValueInterface.ToInt16(Nothing))
                    End If

                Case TypeCode.UInt16
                    If TypeOf Value Is UInt16 Then
                        Return CSByte(DirectCast(Value, UInt16))
                    Else
                        Return CSByte(ValueInterface.ToUInt16(Nothing))
                    End If

                Case TypeCode.Int32
                    If TypeOf Value Is Int32 Then
                        Return CSByte(DirectCast(Value, Int32))
                    Else
                        Return CSByte(ValueInterface.ToInt32(Nothing))
                    End If

                Case TypeCode.UInt32
                    If TypeOf Value Is UInt32 Then
                        Return CSByte(DirectCast(Value, UInt32))
                    Else
                        Return CSByte(ValueInterface.ToUInt32(Nothing))
                    End If

                Case TypeCode.Int64
                    If TypeOf Value Is Int64 Then
                        Return CSByte(DirectCast(Value, Int64))
                    Else
                        Return CSByte(ValueInterface.ToInt64(Nothing))
                    End If

                Case TypeCode.UInt64
                    If TypeOf Value Is UInt64 Then
                        Return CSByte(DirectCast(Value, UInt64))
                    Else
                        Return CSByte(ValueInterface.ToUInt64(Nothing))
                    End If

                Case TypeCode.Decimal
                    If TypeOf Value Is Decimal Then
                        Return ValueInterface.ToSByte(Nothing)
                    Else
                        Return CSByte(ValueInterface.ToDecimal(Nothing))
                    End If

                Case TypeCode.Single
                    If TypeOf Value Is Single Then
                        Return CSByte(DirectCast(Value, Single))
                    Else
                        Return CSByte(ValueInterface.ToSingle(Nothing))
                    End If

                Case TypeCode.Double
                    If TypeOf Value Is Double Then
                        Return CSByte(DirectCast(Value, Double))
                    Else
                        Return CSByte(ValueInterface.ToDouble(Nothing))
                    End If

                Case TypeCode.DateTime,
                     TypeCode.Char
                    ' Fall through to error

                Case TypeCode.String
                    Dim StringValue As String = TryCast(Value, String)

                    If StringValue IsNot Nothing Then
                        Return CSByte(StringValue)
                    Else
                        Return CSByte(ValueInterface.ToString(Nothing))
                    End If

                Case Else
                    ' Fall through to error
            End Select

ThrowInvalidCast:
            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "SByte"))

        End Function

        Public Shared Function ToShort(ByVal Value As String) As Short

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CShort(i64Value)
                End If

                Return CShort(ParseDouble(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Short"), e)
            End Try

        End Function

        Public Shared Function ToShort(ByVal Value As Object) As Short

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode
            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CShort(DirectCast(Value, Boolean))
                        Else
                            Return CShort(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CShort(DirectCast(Value, SByte))
                        Else
                            Return CShort(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CShort(DirectCast(Value, Byte))
                        Else
                            Return CShort(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CShort(DirectCast(Value, Int16))
                        Else
                            Return CShort(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CShort(DirectCast(Value, UInt16))
                        Else
                            Return CShort(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CShort(DirectCast(Value, Int32))
                        Else
                            Return CShort(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CShort(DirectCast(Value, UInt32))
                        Else
                            Return CShort(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CShort(DirectCast(Value, Int64))
                        Else
                            Return CShort(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CShort(DirectCast(Value, UInt64))
                        Else
                            Return CShort(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToInt16(Nothing)
                        Else
                            Return CShort(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CShort(DirectCast(Value, Single))
                        Else
                            Return CShort(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CShort(DirectCast(Value, Double))
                        Else
                            Return CShort(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CShort(StringValue)
                        Else
                            Return CShort(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Short"))

        End Function

        <CLSCompliant(False)>
        Public Shared Function ToUShort(ByVal Value As String) As UShort

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CUShort(i64Value)
                End If

                Return CUShort(ParseDouble(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "UShort"), e)
            End Try

        End Function

        <CLSCompliant(False)>
        Public Shared Function ToUShort(ByVal Value As Object) As UShort

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CUShort(DirectCast(Value, Boolean))
                        Else
                            Return CUShort(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CUShort(DirectCast(Value, SByte))
                        Else
                            Return CUShort(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CUShort(DirectCast(Value, Byte))
                        Else
                            Return CUShort(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CUShort(DirectCast(Value, Int16))
                        Else
                            Return CUShort(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CUShort(DirectCast(Value, UInt16))
                        Else
                            Return CUShort(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CUShort(DirectCast(Value, Int32))
                        Else
                            Return CUShort(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CUShort(DirectCast(Value, UInt32))
                        Else
                            Return CUShort(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CUShort(DirectCast(Value, Int64))
                        Else
                            Return CUShort(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CUShort(DirectCast(Value, UInt64))
                        Else
                            Return CUShort(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToUInt16(Nothing)
                        Else
                            Return CUShort(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CUShort(DirectCast(Value, Single))
                        Else
                            Return CUShort(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CUShort(DirectCast(Value, Double))
                        Else
                            Return CUShort(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CUShort(StringValue)
                        Else
                            Return CUShort(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "UShort"))
        End Function

        Public Shared Function ToInteger(ByVal Value As String) As Integer

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CInt(i64Value)
                End If

                Return CInt(ParseDouble(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Integer"), e)
            End Try

        End Function

        Public Shared Function ToInteger(ByVal Value As Object) As Integer

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CInt(DirectCast(Value, Boolean))
                        Else
                            Return CInt(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CInt(DirectCast(Value, SByte))
                        Else
                            Return CInt(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CInt(DirectCast(Value, Byte))
                        Else
                            Return CInt(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CInt(DirectCast(Value, Int16))
                        Else
                            Return CInt(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CInt(DirectCast(Value, UInt16))
                        Else
                            Return CInt(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CInt(DirectCast(Value, Int32))
                        Else
                            Return CInt(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CInt(DirectCast(Value, UInt32))
                        Else
                            Return CInt(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CInt(DirectCast(Value, Int64))
                        Else
                            Return CInt(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CInt(DirectCast(Value, UInt64))
                        Else
                            Return CInt(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToInt32(Nothing)
                        Else
                            Return CInt(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CInt(DirectCast(Value, Single))
                        Else
                            Return CInt(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CInt(DirectCast(Value, Double))
                        Else
                            Return CInt(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CInt(StringValue)
                        Else
                            Return CInt(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Integer"))
        End Function

        <CLSCompliant(False)>
        Public Shared Function ToUInteger(ByVal Value As String) As UInteger

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CUInt(i64Value)
                End If

                Return CUInt(ParseDouble(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "UInteger"), e)
            End Try

        End Function

        <CLSCompliant(False)>
        Public Shared Function ToUInteger(ByVal Value As Object) As UInteger

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CUInt(DirectCast(Value, Boolean))
                        Else
                            Return CUInt(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CUInt(DirectCast(Value, SByte))
                        Else
                            Return CUInt(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CUInt(DirectCast(Value, Byte))
                        Else
                            Return CUInt(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CUInt(DirectCast(Value, Int16))
                        Else
                            Return CUInt(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CUInt(DirectCast(Value, UInt16))
                        Else
                            Return CUInt(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CUInt(DirectCast(Value, Int32))
                        Else
                            Return CUInt(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CUInt(DirectCast(Value, UInt32))
                        Else
                            Return CUInt(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CUInt(DirectCast(Value, Int64))
                        Else
                            Return CUInt(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CUInt(DirectCast(Value, UInt64))
                        Else
                            Return CUInt(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToUInt32(Nothing)
                        Else
                            Return CUInt(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CUInt(DirectCast(Value, Single))
                        Else
                            Return CUInt(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CUInt(DirectCast(Value, Double))
                        Else
                            Return CUInt(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CUInt(StringValue)
                        Else
                            Return CUInt(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "UInteger"))

        End Function

        Public Shared Function ToLong(ByVal Value As String) As Long

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CLng(i64Value)
                End If

                'Using Decimal parse so that we full range of Int64
                ' and still get currency and thousands parsing
                Return CLng(ParseDecimal(Value, Nothing))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Long"), e)
            End Try

        End Function

        Public Shared Function ToLong(ByVal Value As Object) As Long

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CLng(DirectCast(Value, Boolean))
                        Else
                            Return CLng(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CLng(DirectCast(Value, SByte))
                        Else
                            Return CLng(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CLng(DirectCast(Value, Byte))
                        Else
                            Return CLng(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CLng(DirectCast(Value, Int16))
                        Else
                            Return CLng(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CLng(DirectCast(Value, UInt16))
                        Else
                            Return CLng(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CLng(DirectCast(Value, Int32))
                        Else
                            Return CLng(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CLng(DirectCast(Value, UInt32))
                        Else
                            Return CLng(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CLng(DirectCast(Value, Int64))
                        Else
                            Return CLng(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CLng(DirectCast(Value, UInt64))
                        Else
                            Return CLng(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToInt64(Nothing)
                        Else
                            Return CLng(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CLng(DirectCast(Value, Single))
                        Else
                            Return CLng(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CLng(DirectCast(Value, Double))
                        Else
                            Return CLng(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CLng(StringValue)
                        Else
                            Return CLng(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Long"))
        End Function

        <CLSCompliant(False)>
        Public Shared Function ToULong(ByVal Value As String) As ULong

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim ui64Value As UInt64

                If IsHexOrOctValue(Value, ui64Value) Then
                    Return CULng(ui64Value)
                End If

                'Using Decimal parse so that we full range of Int64
                ' and still get currency and thousands parsing
                Return CULng(ParseDecimal(Value, Nothing))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "ULong"), e)
            End Try

        End Function

        <CLSCompliant(False)>
        Public Shared Function ToULong(ByVal Value As Object) As ULong

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CULng(DirectCast(Value, Boolean))
                        Else
                            Return CULng(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CULng(DirectCast(Value, SByte))
                        Else
                            Return CULng(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CULng(DirectCast(Value, Byte))
                        Else
                            Return CULng(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CULng(DirectCast(Value, Int16))
                        Else
                            Return CULng(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CULng(DirectCast(Value, UInt16))
                        Else
                            Return CULng(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CULng(DirectCast(Value, Int32))
                        Else
                            Return CULng(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CULng(DirectCast(Value, UInt32))
                        Else
                            Return CULng(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CULng(DirectCast(Value, Int64))
                        Else
                            Return CULng(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CULng(DirectCast(Value, UInt64))
                        Else
                            Return CULng(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToUInt64(Nothing)
                        Else
                            Return CULng(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CULng(DirectCast(Value, Single))
                        Else
                            Return CULng(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CULng(DirectCast(Value, Double))
                        Else
                            Return CULng(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CULng(StringValue)
                        Else
                            Return CULng(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "ULong"))
        End Function

        Public Shared Function ToDecimal(ByVal Value As Boolean) As Decimal
            If Value Then
                Return -1D
            Else
                Return 0D
            End If
        End Function

        Public Shared Function ToDecimal(ByVal Value As String) As Decimal
            Return ToDecimal(Value, Nothing)
        End Function

        Friend Shared Function ToDecimal(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Decimal
            If Value Is Nothing Then
                Return 0D
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CDec(i64Value)
                End If

                Return ParseDecimal(Value, NumberFormat)

            Catch e1 As OverflowException
                Throw VbMakeException(vbErrors.Overflow)
            Catch e2 As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Decimal"))
            End Try
        End Function

        Public Shared Function ToDecimal(ByVal Value As Object) As Decimal
            Return ToDecimal(Value, Nothing)
        End Function

        Friend Shared Function ToDecimal(ByVal Value As Object, ByVal NumberFormat As NumberFormatInfo) As Decimal

            If Value Is Nothing Then
                Return 0D
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CDec(DirectCast(Value, Boolean))
                        Else
                            Return CDec(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CDec(DirectCast(Value, SByte))
                        Else
                            Return CDec(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CDec(DirectCast(Value, Byte))
                        Else
                            Return CDec(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CDec(DirectCast(Value, Int16))
                        Else
                            Return CDec(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CDec(DirectCast(Value, UInt16))
                        Else
                            Return CDec(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CDec(DirectCast(Value, Int32))
                        Else
                            Return CDec(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CDec(DirectCast(Value, UInt32))
                        Else
                            Return CDec(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CDec(DirectCast(Value, Int64))
                        Else
                            Return CDec(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CDec(DirectCast(Value, UInt64))
                        Else
                            Return CDec(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        Return ValueInterface.ToDecimal(Nothing)

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CDec(DirectCast(Value, Single))
                        Else
                            Return CDec(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CDec(DirectCast(Value, Double))
                        Else
                            Return CDec(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error                        

                    Case TypeCode.String
                        Return ToDecimal(ValueInterface.ToString(Nothing), NumberFormat)

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Decimal"))
        End Function

        Private Shared Function ParseDecimal(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Decimal
            Dim NormalizedNumberFormat As NumberFormatInfo
            Dim culture As CultureInfo = GetCultureInfo()

            If NumberFormat Is Nothing Then
                NumberFormat = culture.NumberFormat
            End If

            ' Normalize number format settings to enable us to first use the numeric settings for both currency and number parsing
            ' compatible with VB6
            NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat)

            Const flags As NumberStyles =
                    NumberStyles.AllowDecimalPoint Or
                    NumberStyles.AllowExponent Or
                    NumberStyles.AllowLeadingSign Or
                    NumberStyles.AllowLeadingWhite Or
                    NumberStyles.AllowThousands Or
                    NumberStyles.AllowTrailingSign Or
                    NumberStyles.AllowParentheses Or
                    NumberStyles.AllowTrailingWhite Or
                    NumberStyles.AllowCurrencySymbol

            Value = ToHalfwidthNumbers(Value, culture)

            Try
                ' Use numeric settings to parse
                Return Decimal.Parse(Value, flags, NormalizedNumberFormat)
            Catch FormatEx As FormatException When Not (NumberFormat Is NormalizedNumberFormat)
                ' Use currency settings to parse
                Return Decimal.Parse(Value, flags, NumberFormat)
            Catch Ex As Exception
                Throw Ex
            End Try

        End Function

        Private Shared Function GetNormalizedNumberFormat(ByVal InNumberFormat As NumberFormatInfo) As NumberFormatInfo
            '  This method returns a NumberFormat with the relevant Currency Settings to be the same as the Number Settings
            '  In - NumberFormat to be normalized - this is not changed by this Method
            '  Return - Normalized NumberFormat

            Dim OutNumberFormat As NumberFormatInfo

            With InNumberFormat
                If (Not .CurrencyDecimalSeparator Is Nothing) AndAlso
                   (Not .NumberDecimalSeparator Is Nothing) AndAlso
                   (Not .CurrencyGroupSeparator Is Nothing) AndAlso
                   (Not .NumberGroupSeparator Is Nothing) AndAlso
                   (.CurrencyDecimalSeparator.Length = 1) AndAlso
                   (.NumberDecimalSeparator.Length = 1) AndAlso
                   (.CurrencyGroupSeparator.Length = 1) AndAlso
                   (.NumberGroupSeparator.Length = 1) AndAlso
                   (.CurrencyDecimalSeparator.Chars(0) = .NumberDecimalSeparator.Chars(0)) AndAlso
                   (.CurrencyGroupSeparator.Chars(0) = .NumberGroupSeparator.Chars(0)) AndAlso
                   (.CurrencyDecimalDigits = .NumberDecimalDigits) Then
                    Return InNumberFormat
                End If
            End With

            With InNumberFormat
                If (Not .CurrencyDecimalSeparator Is Nothing) AndAlso
                   (Not .NumberDecimalSeparator Is Nothing) AndAlso
                   (.CurrencyDecimalSeparator.Length = .NumberDecimalSeparator.Length) AndAlso
                   (Not .CurrencyGroupSeparator Is Nothing) AndAlso
                   (Not .NumberGroupSeparator Is Nothing) AndAlso
                   (.CurrencyGroupSeparator.Length = .NumberGroupSeparator.Length) Then

                    Dim i As Integer
                    For i = 0 To .CurrencyDecimalSeparator.Length - 1
                        If (.CurrencyDecimalSeparator.Chars(i) <> .NumberDecimalSeparator.Chars(i)) Then GoTo MisMatch
                    Next

                    For i = 0 To .CurrencyGroupSeparator.Length - 1
                        If (.CurrencyGroupSeparator.Chars(i) <> .NumberGroupSeparator.Chars(i)) Then GoTo MisMatch
                    Next

                    Return InNumberFormat
                End If
            End With

MisMatch:

            OutNumberFormat = DirectCast(InNumberFormat.Clone, NumberFormatInfo)

            ' Set the Currency Settings to be the Same as the Numeric Settings
            With OutNumberFormat
                .CurrencyDecimalSeparator = .NumberDecimalSeparator
                .CurrencyGroupSeparator = .NumberGroupSeparator
                .CurrencyDecimalDigits = .NumberDecimalDigits
            End With

            Return OutNumberFormat
        End Function

        Public Shared Function ToSingle(ByVal Value As String) As Single
            Return ToSingle(Value, Nothing)
        End Function

        Friend Shared Function ToSingle(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Single

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CSng(i64Value)
                End If

                Dim Result As Double = ParseDouble(Value, NumberFormat)
                If (Result < Single.MinValue OrElse Result > Single.MaxValue) AndAlso
                   Not Double.IsInfinity(Result) Then
                    Throw New OverflowException
                End If
                Return CSng(Result)

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Single"), e)
            End Try

        End Function

        Public Shared Function ToSingle(ByVal Value As Object) As Single
            Return ToSingle(Value, Nothing)
        End Function

        Friend Shared Function ToSingle(ByVal Value As Object, ByVal NumberFormat As NumberFormatInfo) As Single

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode
            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CSng(DirectCast(Value, Boolean))
                        Else
                            Return CSng(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CSng(DirectCast(Value, SByte))
                        Else
                            Return CSng(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CSng(DirectCast(Value, Byte))
                        Else
                            Return CSng(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CSng(DirectCast(Value, Int16))
                        Else
                            Return CSng(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CSng(DirectCast(Value, UInt16))
                        Else
                            Return CSng(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CSng(DirectCast(Value, Int32))
                        Else
                            Return CSng(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CSng(DirectCast(Value, UInt32))
                        Else
                            Return CSng(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CSng(DirectCast(Value, Int64))
                        Else
                            Return CSng(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CSng(DirectCast(Value, UInt64))
                        Else
                            Return CSng(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToSingle(Nothing)
                        Else
                            Return CSng(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return DirectCast(Value, Single)
                        Else
                            Return ValueInterface.ToSingle(Nothing)
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CSng(DirectCast(Value, Double))
                        Else
                            Return CSng(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Return ToSingle(ValueInterface.ToString(Nothing), NumberFormat)

                    Case Else
                        ' Fall through to error
                End Select

            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Single"))
        End Function

        Public Shared Function ToDouble(ByVal Value As String) As Double
            Return ToDouble(Value, Nothing)
        End Function

        Friend Shared Function ToDouble(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Double

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CDbl(i64Value)
                End If
                Return ParseDouble(Value, NumberFormat)

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Double"), e)
            End Try

        End Function

        Public Shared Function ToDouble(ByVal Value As Object) As Double
            Return ToDouble(Value, Nothing)
        End Function

        Friend Shared Function ToDouble(ByVal Value As Object, ByVal NumberFormat As NumberFormatInfo) As Double

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CDbl(DirectCast(Value, Boolean))
                        Else
                            Return CDbl(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.SByte
                        If TypeOf Value Is SByte Then
                            Return CDbl(DirectCast(Value, SByte))
                        Else
                            Return CDbl(ValueInterface.ToSByte(Nothing))
                        End If

                    Case TypeCode.Byte
                        If TypeOf Value Is Byte Then
                            Return CDbl(DirectCast(Value, Byte))
                        Else
                            Return CDbl(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CDbl(DirectCast(Value, Int16))
                        Else
                            Return CDbl(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.UInt16
                        If TypeOf Value Is UInt16 Then
                            Return CDbl(DirectCast(Value, UInt16))
                        Else
                            Return CDbl(ValueInterface.ToUInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CDbl(DirectCast(Value, Int32))
                        Else
                            Return CDbl(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.UInt32
                        If TypeOf Value Is UInt32 Then
                            Return CDbl(DirectCast(Value, UInt32))
                        Else
                            Return CDbl(ValueInterface.ToUInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CDbl(DirectCast(Value, Int64))
                        Else
                            Return CDbl(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.UInt64
                        If TypeOf Value Is UInt64 Then
                            Return CDbl(DirectCast(Value, UInt64))
                        Else
                            Return CDbl(ValueInterface.ToUInt64(Nothing))
                        End If

                    Case TypeCode.Decimal
                        If TypeOf Value Is Decimal Then
                            Return ValueInterface.ToDouble(Nothing)
                        Else
                            Return CDbl(ValueInterface.ToDecimal(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CDbl(DirectCast(Value, Single))
                        Else
                            Return CDbl(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CDbl(DirectCast(Value, Double))
                        Else
                            Return CDbl(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.DateTime,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.String
                        Return ToDouble(ValueInterface.ToString(Nothing), NumberFormat)

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Double"))

        End Function

        Private Shared Function ParseDouble(ByVal Value As String) As Double
            Return ParseDouble(Value, Nothing)
        End Function

        Friend Shared Function TryParseDouble(ByVal Value As String, ByRef Result As Double) As Boolean
            Dim NumberFormat As NumberFormatInfo
            Dim NormalizedNumberFormat As NumberFormatInfo
            Dim culture As CultureInfo = GetCultureInfo()

            NumberFormat = culture.NumberFormat
            NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat)

            Const flags As NumberStyles =
                    NumberStyles.AllowDecimalPoint Or
                    NumberStyles.AllowExponent Or
                    NumberStyles.AllowLeadingSign Or
                    NumberStyles.AllowLeadingWhite Or
                    NumberStyles.AllowThousands Or
                    NumberStyles.AllowTrailingSign Or
                    NumberStyles.AllowParentheses Or
                    NumberStyles.AllowTrailingWhite Or
                    NumberStyles.AllowCurrencySymbol

            Value = ToHalfwidthNumbers(Value, culture)

            ' The below code handles the 80% case efficiently and is inefficient only when the numeric and currency settings
            ' are different
            If NumberFormat Is NormalizedNumberFormat Then
                Return Double.TryParse(Value, flags, NormalizedNumberFormat, Result)
            Else
                Try
                    ' Use numeric settings to parse
                    ' Note that we use Parse instead of TryParse in order to distinguish whether the conversion failed
                    ' due to FormatException or other exception like OverFlowException, etc.
                    Result = Double.Parse(Value, flags, NormalizedNumberFormat)
                    Return True
                Catch FormatEx As FormatException
                    ' Use currency settings to parse
                    Try
                        Return Double.TryParse(Value, flags, NumberFormat, Result)
                    Catch ex As ArgumentException
                        Return False
                    End Try
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch Ex As Exception
                    Return False
                End Try
            End If

        End Function

        Private Shared Function ParseDouble(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Double
            Dim NormalizedNumberFormat As NumberFormatInfo
            Dim culture As CultureInfo = GetCultureInfo()

            If NumberFormat Is Nothing Then
                NumberFormat = culture.NumberFormat
            End If

            ' Normalize number format settings to enable us to first use the numeric settings for both currency and number parsing
            ' compatible with VB6
            NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat)
            Const flags As NumberStyles =
                    NumberStyles.AllowDecimalPoint Or
                    NumberStyles.AllowExponent Or
                    NumberStyles.AllowLeadingSign Or
                    NumberStyles.AllowLeadingWhite Or
                    NumberStyles.AllowThousands Or
                    NumberStyles.AllowTrailingSign Or
                    NumberStyles.AllowParentheses Or
                    NumberStyles.AllowTrailingWhite Or
                    NumberStyles.AllowCurrencySymbol
            Value = ToHalfwidthNumbers(Value, culture)

            Try
                ' Use numeric settings to parse
                Return Double.Parse(Value, flags, NormalizedNumberFormat)
            Catch FormatEx As FormatException When Not (NumberFormat Is NormalizedNumberFormat)
                ' Use currency settings to parse
                Return Double.Parse(Value, flags, NumberFormat)
            Catch Ex As Exception
                Throw Ex
            End Try

        End Function

        Public Shared Function ToDate(ByVal Value As String) As Date
            Dim ParsedDate As DateTime

            If TryParseDate(Value, ParsedDate) Then
                Return ParsedDate
            Else
                'Truncate the string to 32 characters for the message
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Date"))
            End If
        End Function

        Public Shared Function ToDate(ByVal Value As Object) As Date

            If Value Is Nothing Then
                Return Nothing
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode
                    Case TypeCode.Boolean,
                         TypeCode.SByte,
                         TypeCode.Byte,
                         TypeCode.Int16,
                         TypeCode.UInt16,
                         TypeCode.Int32,
                         TypeCode.UInt32,
                         TypeCode.Int64,
                         TypeCode.UInt64,
                         TypeCode.Decimal,
                         TypeCode.Single,
                         TypeCode.Double,
                         TypeCode.Char
                        ' Fall through to error

                    Case TypeCode.DateTime
                        If TypeOf Value Is DateTime Then
                            Return CDate(DirectCast(Value, DateTime))
                        Else
                            Return CDate(ValueInterface.ToDateTime(Nothing))
                        End If

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CDate(StringValue)
                        Else
                            Return CDate(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select

            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Date"))
        End Function

        Friend Shared Function TryParseDate(ByVal Value As String, ByRef Result As DateTime) As Boolean
            Const ParseStyle As DateTimeStyles =
                        DateTimeStyles.AllowWhiteSpaces Or
                        DateTimeStyles.NoCurrentDateDefault
            Dim Culture As CultureInfo = GetCultureInfo()
            Return DateTime.TryParse(ToHalfwidthNumbers(Value, Culture), Culture, ParseStyle, Result)
        End Function

        Public Shared Function ToChar(ByVal Value As String) As Char
            If (Value Is Nothing) OrElse (Value.Length = 0) Then
                Return ControlChars.NullChar
            End If

            Return Value.Chars(0)
        End Function

        Public Shared Function ToChar(ByVal Value As Object) As Char

            If Value Is Nothing Then
                Return ControlChars.NullChar
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface IsNot Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode
                    Case TypeCode.Boolean,
                         TypeCode.SByte,
                         TypeCode.Byte,
                         TypeCode.Int16,
                         TypeCode.UInt16,
                         TypeCode.Int32,
                         TypeCode.UInt32,
                         TypeCode.Int64,
                         TypeCode.UInt64,
                         TypeCode.Decimal,
                         TypeCode.Single,
                         TypeCode.Double,
                         TypeCode.DateTime
                        ' Fall through to error

                    Case TypeCode.Char
                        If TypeOf Value Is Char Then
                            Return CChar(DirectCast(Value, Char))
                        Else
                            Return ValueInterface.ToChar(Nothing)
                        End If

                    Case TypeCode.String
                        Dim StringValue As String = TryCast(Value, String)

                        If StringValue IsNot Nothing Then
                            Return CChar(StringValue)
                        Else
                            Return CChar(ValueInterface.ToString(Nothing))
                        End If

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Char"))
        End Function

        Public Shared Function ToCharArrayRankOne(ByVal Value As String) As Char()

            If Value Is Nothing Then

                Value = ""

            End If

            Return Value.ToCharArray()

        End Function

        Public Shared Function ToCharArrayRankOne(ByVal Value As Object) As Char()

            If Value Is Nothing Then

                Return "".ToCharArray()

            End If

            Dim ArrayValue As Char() = TryCast(Value, Char())

            If ArrayValue IsNot Nothing AndAlso ArrayValue.Rank = 1 Then

                Return ArrayValue

            Else
                Dim ValueInterface As IConvertible

                ValueInterface = TryCast(Value, IConvertible)

                If Not ValueInterface Is Nothing Then

                    If (ValueInterface.GetTypeCode() = TypeCode.String) Then
                        Return ValueInterface.ToString(Nothing).ToCharArray()
                    End If

                End If

            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Char()"))

        End Function

        Public Shared Shadows Function ToString(ByVal Value As Boolean) As String
            If Value Then
                Return Boolean.TrueString
            Else
                Return Boolean.FalseString
            End If
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Byte) As String
            Return Value.ToString(Nothing, Nothing)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Char) As String
            Return Value.ToString()
        End Function

        Public Shared Function FromCharArray(ByVal Value() As Char) As String
            Return New String(Value)
        End Function

        Public Shared Function FromCharAndCount(ByVal Value As Char, ByVal Count As Integer) As String
            Return New String(Value, Count)
        End Function

        Public Shared Function FromCharArraySubset(ByVal Value() As Char, ByVal StartIndex As Integer, ByVal Length As Integer) As String
            Return New String(Value, StartIndex, Length)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Short) As String
            Return Value.ToString(Nothing, Nothing)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Integer) As String
            Return Value.ToString(Nothing, Nothing)
        End Function

        <CLSCompliant(False)>
        Public Shared Shadows Function ToString(ByVal Value As UInteger) As String
            Return Value.ToString(Nothing, Nothing)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Long) As String
            Return Value.ToString(Nothing, Nothing)
        End Function

        <CLSCompliant(False)>
        Public Shared Shadows Function ToString(ByVal Value As ULong) As String
            Return Value.ToString(Nothing, Nothing)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Single) As String
            Return ToString(Value, Nothing)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Double) As String
            Return ToString(Value, Nothing)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Single, ByVal NumberFormat As NumberFormatInfo) As String
            Return Value.ToString(Nothing, NumberFormat)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Double, ByVal NumberFormat As NumberFormatInfo) As String
            Return Value.ToString("G", NumberFormat)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Date) As String
            Dim TimeTicks As Long = Value.TimeOfDay.Ticks

            If (TimeTicks = Value.Ticks) OrElse
                (Value.Year = 1899 AndAlso Value.Month = 12 AndAlso Value.Day = 30) Then 'OA Date with no date is 1899-12-30
                'No date (1/1/1)
                'VSW 395746: Can't change OA DATE now because of backwards compatibility.
                Return Value.ToString("T", Nothing)
            ElseIf TimeTicks = 0 Then
                'No time, or is midnight
                Return Value.ToString("d", Nothing)
            Else
                Return Value.ToString("G", Nothing)
            End If
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Decimal) As String
            Return ToString(Value, Nothing)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Decimal, ByVal NumberFormat As NumberFormatInfo) As String
            Return Value.ToString("G", NumberFormat)
        End Function

        Public Shared Shadows Function ToString(ByVal Value As Object) As String

            If Value Is Nothing Then
                Return Nothing

            Else
                Dim StringValue As String = TryCast(Value, String)

                If StringValue IsNot Nothing Then
                    Return StringValue
                End If
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If Not ValueInterface Is Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode
                    Case TypeCode.Boolean
                        Return CStr(ValueInterface.ToBoolean(Nothing))

                    Case TypeCode.SByte
                        Return CStr(ValueInterface.ToSByte(Nothing))

                    Case TypeCode.Byte
                        Return CStr(ValueInterface.ToByte(Nothing))

                    Case TypeCode.Int16
                        Return CStr(ValueInterface.ToInt16(Nothing))

                    Case TypeCode.UInt16
                        Return CStr(ValueInterface.ToUInt16(Nothing))

                    Case TypeCode.Int32
                        Return CStr(ValueInterface.ToInt32(Nothing))

                    Case TypeCode.UInt32
                        Return CStr(ValueInterface.ToUInt32(Nothing))

                    Case TypeCode.Int64
                        Return CStr(ValueInterface.ToInt64(Nothing))

                    Case TypeCode.UInt64
                        Return CStr(ValueInterface.ToUInt64(Nothing))

                    Case TypeCode.Decimal
                        Return CStr(ValueInterface.ToDecimal(Nothing))

                    Case TypeCode.Single
                        Return CStr(ValueInterface.ToSingle(Nothing))

                    Case TypeCode.Double
                        Return CStr(ValueInterface.ToDouble(Nothing))

                    Case TypeCode.Char
                        Return CStr(ValueInterface.ToChar(Nothing))

                    Case TypeCode.DateTime
                        Return CStr(ValueInterface.ToDateTime(Nothing))

                    Case TypeCode.String
                        Return CStr(ValueInterface.ToString(Nothing))

                    Case Else
                        ' Fall through to error
                End Select

            Else
                Dim CharArray As Char() = TryCast(Value, Char())

                If CharArray IsNot Nothing Then
                    Return New String(CharArray)
                End If
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "String"))

        End Function

        Public Shared Function ToGenericParameter(Of T)(ByVal Value As Object) As T

            'Think carefully about this function and how it handles Enums,
            'System.Enum, System.ValueType, Object, etc.  This function should not
            'do a latebound conversion.

            If Value Is Nothing Then
                Return Nothing
            End If

            Dim ReflectedType As Type = GetType(T)
            Dim tc As TypeCode = GetTypeCode(ReflectedType)

            Select Case tc
                Case TypeCode.Boolean
                    Return DirectCast(CObj(CBool(Value)), T)
                Case TypeCode.SByte
                    Return DirectCast(CObj(CSByte(Value)), T)
                Case TypeCode.Byte
                    Return DirectCast(CObj(CByte(Value)), T)
                Case TypeCode.Int16
                    Return DirectCast(CObj(CShort(Value)), T)
                Case TypeCode.UInt16
                    Return DirectCast(CObj(CUShort(Value)), T)
                Case TypeCode.Int32
                    Return DirectCast(CObj(CInt(Value)), T)
                Case TypeCode.UInt32
                    Return DirectCast(CObj(CUInt(Value)), T)
                Case TypeCode.Int64
                    Return DirectCast(CObj(CLng(Value)), T)
                Case TypeCode.UInt64
                    Return DirectCast(CObj(CULng(Value)), T)
                Case TypeCode.Decimal
                    Return DirectCast(CObj(CDec(Value)), T)
                Case TypeCode.Single
                    Return DirectCast(CObj(CSng(Value)), T)
                Case TypeCode.Double
                    Return DirectCast(CObj(CDbl(Value)), T)
                Case TypeCode.DateTime
                    Return DirectCast(CObj(CDate(Value)), T)
                Case TypeCode.Char
                    Return DirectCast(CObj(CChar(Value)), T)
                Case TypeCode.String
                    Return DirectCast(CObj(CStr(Value)), T)
                Case Else
                    Return DirectCast(Value, T)
            End Select
        End Function

        Private Shared Function CastSByteEnum(ByVal Expression As SByte, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Private Shared Function CastByteEnum(ByVal Expression As Byte, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Private Shared Function CastInt16Enum(ByVal Expression As Int16, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Private Shared Function CastUInt16Enum(ByVal Expression As UInt16, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Private Shared Function CastInt32Enum(ByVal Expression As Int32, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Private Shared Function CastUInt32Enum(ByVal Expression As UInt32, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Private Shared Function CastInt64Enum(ByVal Expression As Int64, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Private Shared Function CastUInt64Enum(ByVal Expression As UInt64, ByVal TargetType As Type) As Object
            If IsEnum(TargetType) Then Return [Enum].ToObject(TargetType, Expression)
            Return Expression
        End Function

        Friend Shared Function ForceValueCopy(ByVal Expression As Object, ByVal TargetType As Type) As Object

            Dim iconv As IConvertible = TryCast(Expression, IConvertible)

            If iconv Is Nothing Then
                Return Expression
            End If

            Debug.Assert(iconv.GetTypeCode = GetTypeCode(TargetType), "expected types to match")

            Select Case iconv.GetTypeCode()

                Case TypeCode.Boolean : Return iconv.ToBoolean(Nothing)
                Case TypeCode.SByte : Return CastSByteEnum(iconv.ToSByte(Nothing), TargetType)
                Case TypeCode.Byte : Return CastByteEnum(iconv.ToByte(Nothing), TargetType)
                Case TypeCode.Int16 : Return CastInt16Enum(iconv.ToInt16(Nothing), TargetType)
                Case TypeCode.UInt16 : Return CastUInt16Enum(iconv.ToUInt16(Nothing), TargetType)
                Case TypeCode.Int32 : Return CastInt32Enum(iconv.ToInt32(Nothing), TargetType)
                Case TypeCode.UInt32 : Return CastUInt32Enum(iconv.ToUInt32(Nothing), TargetType)
                Case TypeCode.Int64 : Return CastInt64Enum(iconv.ToInt64(Nothing), TargetType)
                Case TypeCode.UInt64 : Return CastUInt64Enum(iconv.ToUInt64(Nothing), TargetType)
                Case TypeCode.Decimal : Return iconv.ToDecimal(Nothing)
                Case TypeCode.Single : Return iconv.ToSingle(Nothing)
                Case TypeCode.Double : Return iconv.ToDouble(Nothing)
                Case TypeCode.DateTime : Return iconv.ToDateTime(Nothing)
                Case TypeCode.Char : Return iconv.ToChar(Nothing)

                Case TypeCode.Empty
                    Debug.Fail("shouldn't reach here")

                Case TypeCode.Object,
                     TypeCode.DBNull,
                     TypeCode.String

                    'fall through

            End Select

            Return Expression

        End Function

        Private Shared Function ChangeIntrinsicType(ByVal Expression As Object, ByVal TargetType As Type) As Object

            'This function will not handle user-defined conversion resolution, and so handles
            'only conversions between intrinsic types.
            Debug.Assert(IsIntrinsicType(Expression.GetType) OrElse IsEnum(Expression.GetType), "this function converts between intrinsic types only")

            Select Case GetTypeCode(TargetType)

                Case TypeCode.Boolean : Return CBool(Expression)
                Case TypeCode.SByte : Return CastSByteEnum(CSByte(Expression), TargetType)
                Case TypeCode.Byte : Return CastByteEnum(CByte(Expression), TargetType)
                Case TypeCode.Int16 : Return CastInt16Enum(CShort(Expression), TargetType)
                Case TypeCode.UInt16 : Return CastUInt16Enum(CUShort(Expression), TargetType)
                Case TypeCode.Int32 : Return CastInt32Enum(CInt(Expression), TargetType)
                Case TypeCode.UInt32 : Return CastUInt32Enum(CUInt(Expression), TargetType)
                Case TypeCode.Int64 : Return CastInt64Enum(CLng(Expression), TargetType)
                Case TypeCode.UInt64 : Return CastUInt64Enum(CULng(Expression), TargetType)
                Case TypeCode.Decimal : Return CDec(Expression)
                Case TypeCode.Single : Return CSng(Expression)
                Case TypeCode.Double : Return CDbl(Expression)
                Case TypeCode.DateTime : Return CDate(Expression)
                Case TypeCode.Char : Return CChar(Expression)
                Case TypeCode.String : Return CStr(Expression)

                Case TypeCode.Empty,
                     TypeCode.Object,
                     TypeCode.DBNull
                    'fall though to error
            End Select
            Debug.Fail("Expected intrinsic type only, not: " & TargetType.Name)
            Throw New Exception  'would be nice to have an internal runtime exception or something like that

        End Function

        <RequiresUnreferencedCode(ConversionsTrimmerMessage)>
        Public Shared Function ChangeType(
                ByVal Expression As Object,
                <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)>
                ByVal TargetType As Type) As Object
            Return ChangeType(Expression, TargetType, False)
        End Function

        <RequiresUnreferencedCode("Calls ObjectUserDefinedConversion")>
        Friend Shared Function ChangeType(
                ByVal Expression As Object,
                <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)>
                ByVal TargetType As Type,
                ByVal Dynamic As Boolean) As Object
            If TargetType Is Nothing Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidNullValue1, "TargetType"))
            End If

            If Expression Is Nothing Then
                If IsValueType(TargetType) Then
                    'method.invoke will do this for us, so when casting arguments to param types during 
                    'latebinding, the createinstance call isn't needed, but ChangeType used in a generalized
                    'manner should return a default instance.
                    Return Activator.CreateInstance(TargetType)
                Else
                    Return Nothing
                End If
            End If

            Dim SourceType As Type = Expression.GetType
            Debug.Assert(Not SourceType.IsByRef, "never expected to see byref source type")

            'Dig through ByRef types which might come in.
            If TargetType.IsByRef Then TargetType = TargetType.GetElementType

            If TargetType Is SourceType OrElse IsRootObjectType(TargetType) Then
                Return Expression
            End If

            Dim TargetTypeCode As TypeCode = GetTypeCode(TargetType)

            'All conversions between intrinsic types are natively built-in
            'and require no user-defined conversion resolution.
            If IsIntrinsicType(TargetTypeCode) Then
                Dim SourceTypeCode As TypeCode = GetTypeCode(SourceType)
                If IsIntrinsicType(SourceTypeCode) Then
                    Return ChangeIntrinsicType(Expression, TargetType)
                End If
            End If

            If TargetType.IsInstanceOfType(Expression) Then
                Return Expression
            End If

            If IsCharArrayRankOne(TargetType) AndAlso IsStringType(SourceType) Then
                Return CType(DirectCast(Expression, String), Char())
            End If
            If IsStringType(TargetType) AndAlso IsCharArrayRankOne(SourceType) Then
                Return CStr(DirectCast(Expression, Char()))
            End If

            Debug.Assert(ClassifyPredefinedConversion(TargetType, SourceType) = ConversionClass.None OrElse
                         ClassifyPredefinedConversion(TargetType, SourceType) = ConversionClass.Narrowing,
                         "expected all predefined conversions handled by this point")

            If Dynamic Then
                Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(Expression)
                If idmop IsNot Nothing Then
                    Return IDOBinder.UserDefinedConversion(idmop, TargetType)
                End If
            End If
            Return ObjectUserDefinedConversion(Expression, TargetType)
        End Function

        <Obsolete("do not use this method", True)>
        <DebuggerHidden()>
        <DebuggerStepThrough()>
        <RequiresUnreferencedCode(ConversionsTrimmerMessage)>
        Public Shared Function FallbackUserDefinedConversion(
                ByVal Expression As Object,
                <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)>
                ByVal TargetType As Type) As Object

            Return ObjectUserDefinedConversion(Expression, TargetType)
        End Function 'FallbackUserDefinedConversion

        <DebuggerHidden()>
        <DebuggerStepThrough()>
        <RequiresUnreferencedCode("Calls Container.InvokeMethod which is unsafe.")>
        Private Shared Function ObjectUserDefinedConversion(
                ByVal Expression As Object,
                <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)>
                ByVal TargetType As Type) As Object

            Dim SourceType As Type = Expression.GetType
            If ClassifyPredefinedConversion(TargetType, SourceType) = ConversionClass.None AndAlso
               (IsClassOrValueType(SourceType) OrElse IsClassOrValueType(TargetType)) AndAlso
               Not (IsIntrinsicType(SourceType) AndAlso IsIntrinsicType(TargetType)) Then

                'Conversions of the form S-->T use only one user-defined conversion at a time, i.e.,
                'user-defined conversions are not chained together.  It may be necessary to convert to and
                'from intermediate types using predefined conversions to match the signature of the
                'user-defined conversion exactly, so the conversion "path" is comprised of at most three
                'parts:
                '
                '    1) [ predefined conversion  S-->Sx ]
                '    2) User-defined conversion Sx-->Tx
                '    3) [ predefined conversion Tx-->T  ]
                '
                '    Where Sx is the intermediate source type
                '      and Tx is the intermediate target type
                '
                '    Steps 1 and 3 are optional given S == Sx or Tx == T.
                '
                'Given the source operand and target type, resolve the conversion operator and invoke it.
                'When invoking the conversion operator, the conversion from S-->Sx is done when matching
                'the arguments.  After invocation, we must handle the conversion from the result of the
                'invocation to the target type, i.e., Tx-->T.

                'Resolve the operator.
                Dim OperatorMethod As Method = Nothing
                Dim Result As ConversionClass =
                    ClassifyUserDefinedConversion(TargetType, SourceType, OperatorMethod)

                If OperatorMethod IsNot Nothing Then
                    Debug.Assert(Result = ConversionClass.Widening OrElse Result = ConversionClass.Narrowing,
                                 "operator method not expected for invalid conversion")

                    'Invoke the operator. This handles the conversion S-->Sx.
                    Dim BaseReference As Symbols.Container = New Symbols.Container(OperatorMethod.DeclaringType)
                    Dim InvocationResult As Object =
                        BaseReference.InvokeMethod(
                            OperatorMethod,
                            New Object() {Expression},
                            Nothing,
                            BindingFlags.InvokeMethod)

                    'Now convert the result of the invocation to the target type, Tx-->T. 

#If DEBUG Then
                    If InvocationResult IsNot Nothing Then

                        'disabling the assert below when we're converting to Nullable(Of T)
                        'since the Runtime hasn't been updated yet to handle Nullable.  In this case the assert 
                        'is harmless, but ClassifyPredefinedConversion hasn't been updated to consider Nullable conversions,
                        'and updating this across the entire runtime would be significant feature work.

                        If Not _
                           (TargetType.IsGenericType AndAlso
                            Not TargetType.IsGenericTypeDefinition AndAlso
                            TargetType.GetGenericTypeDefinition().Equals(GetType(Nullable(Of ))) AndAlso
                            TargetType.GetGenericArguments().Length > 0 AndAlso
                            InvocationResult.GetType().Equals(TargetType.GetGenericArguments()(0))) Then

                            Dim PostConversion As ConversionClass = ClassifyPredefinedConversion(TargetType, InvocationResult.GetType)

                            Debug.Assert(
                                PostConversion = ConversionClass.Narrowing OrElse
                                PostConversion = ConversionClass.Identity OrElse
                                PostConversion = ConversionClass.Widening,
                                "User defined conversion returned unexpected result")
                        End If
                    End If
#End If
                    Return ChangeType(InvocationResult, TargetType)

                ElseIf Result = ConversionClass.Ambiguous Then
                    Throw New InvalidCastException(
                        SR.Format(
                            SR.AmbiguousCast2,
                            VBFriendlyName(SourceType),
                            VBFriendlyName(TargetType)))
                End If

            End If

            Throw New InvalidCastException(
                SR.Format(
                    SR.InvalidCast_FromTo,
                    VBFriendlyName(SourceType),
                    VBFriendlyName(TargetType)))

        End Function 'UserDefinedConversion

        ' Simplified version of ObjectUserDefinedConversion, above
        ' Determines if conversion is possible
        <RequiresUnreferencedCode("Calls ClassifyUserDefinedConversion")>
        Friend Shared Function CanUserDefinedConvert(ByVal Expression As Object, ByVal TargetType As Type) As Boolean

            Dim SourceType As Type = Expression.GetType
            If ClassifyPredefinedConversion(TargetType, SourceType) = ConversionClass.None AndAlso
               (IsClassOrValueType(SourceType) OrElse IsClassOrValueType(TargetType)) AndAlso
               Not (IsIntrinsicType(SourceType) AndAlso IsIntrinsicType(TargetType)) Then

                'Resolve the operator.
                Dim OperatorMethod As Method = Nothing
                Dim Result As ConversionClass =
                    ClassifyUserDefinedConversion(TargetType, SourceType, OperatorMethod)

                Return OperatorMethod IsNot Nothing
            End If

            Return False
        End Function 'CanUserDefinedConvert

    End Class

End Namespace
