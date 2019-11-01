' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization
Imports System.Diagnostics
Imports System.Reflection

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    Friend Interface IRecordEnum
        Function Callback(ByVal FieldInfo As System.Reflection.FieldInfo, ByRef Value As Object) As Boolean
    End Interface

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Friend Class StructUtils
        ' Prevent creation.
        Private Sub New()
        End Sub

        Friend Shared Function EnumerateUDT(ByVal oStruct As ValueType, ByVal intfRecEnum As IRecordEnum, ByVal fGet As Boolean) As System.Object
            Dim fi() As System.Reflection.FieldInfo
            Dim iLowerBound As Integer
            Dim iUpperBound As Integer
            Dim typ As System.Type
            Dim i As Integer
            Dim FieldType As System.Type
            Dim FieldInfo As System.Reflection.FieldInfo
            Dim vt As VariantType
            Dim obj As Object

            typ = oStruct.GetType()
            vt = VarTypeFromComType(typ)

            If vt <> VariantType.UserDefinedType OrElse typ.IsPrimitive Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "oStruct"))
            End If

            fi = typ.GetFields(BindingFlags.Instance Or BindingFlags.Public)
            iLowerBound = 0
            iUpperBound = fi.GetUpperBound(0)

            For i = iLowerBound To iUpperBound
                FieldInfo = fi(i)
                FieldType = FieldInfo.FieldType
                obj = FieldInfo.GetValue(oStruct)

                If VarTypeFromComType(FieldType) = VariantType.UserDefinedType Then
                    If FieldType.IsPrimitive Then
                        Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedFieldType2, FieldInfo.Name, FieldType.Name)), vbErrors.IllegalFuncCall)
                    Else
                        Call EnumerateUDT(CType(obj, ValueType), intfRecEnum, fGet)
                    End If
                Else
                    Call intfRecEnum.Callback(FieldInfo, obj)
                End If

                If fGet Then
                    FieldInfo.SetValue(oStruct, obj)
                End If
            Next i

            Return Nothing
        End Function

        Friend Shared Function GetRecordLength(ByVal o As Object, Optional ByVal PackSize As Integer = -1) As Integer
            If o Is Nothing Then
                Return 0
            End If

            Dim intf As IRecordEnum
            Dim ph As StructByteLengthHandler

            ph = New StructByteLengthHandler(PackSize)
            intf = ph

            If intf Is Nothing Then
                Throw VbMakeException(vbErrors.IllegalFuncCall)
            End If

            EnumerateUDT(CType(o, ValueType), intf, False)
            Return ph.Length
        End Function

        Private NotInheritable Class StructByteLengthHandler
            Implements IRecordEnum
            Private m_StructLength As Integer
            Private m_PackSize As Integer

            Friend Sub New(ByVal PackSize As Integer)
                'PackSize - Only 1 and multiples of 2 allowed
                Debug.Assert(PackSize = 1, "PackSize is not actually set to anything other than 1 in the current library.  " _
                    & "If this is changed, care will need to be taken that the current code actually sets alignment correctly.")
                m_PackSize = PackSize
            End Sub

            Friend ReadOnly Property Length() As Integer
                Get
                    If m_PackSize = 1 Then
                        Return m_StructLength
                    Else
                        Return (m_StructLength + (m_StructLength Mod m_PackSize))
                    End If
                End Get
            End Property

            Friend Sub SetAlignment(ByVal size As Integer)
                If m_PackSize <> 1 Then
                    m_StructLength += (m_StructLength Mod size)
                End If
            End Sub

            Friend Function Callback(ByVal field_info As Reflection.FieldInfo, ByRef vValue As Object) As Boolean Implements IRecordEnum.Callback
                Dim FieldType As System.Type
                Dim align, size As Integer

                FieldType = field_info.FieldType

                If FieldType Is Nothing Then
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedFieldType2, field_info.Name, "Empty")), vbErrors.IllegalFuncCall)
                End If

                If FieldType.IsArray() Then
                    Dim attributeList As Object()
                    Dim ElementType As System.Type
                    Dim attrFixedArray As VBFixedArrayAttribute
                    Dim ElementCount, ElementSize As Integer

                    attributeList = field_info.GetCustomAttributes(GetType(VBFixedArrayAttribute), False)
                    If Not attributeList Is Nothing AndAlso attributeList.Length <> 0 Then
                        attrFixedArray = CType(attributeList(0), VBFixedArrayAttribute)
                    Else
                        attrFixedArray = Nothing
                    End If

                    ElementType = FieldType.GetElementType()

                    If attrFixedArray Is Nothing Then

                        ElementCount = 1
                        ElementSize = 4

                    Else

                        'This kind of mismatch will be ignored in length calculation
                        '   Structure ABC
                        '       Public <VBFixedArray(1, 2)> x As Integer()
                        '   End Structure
                        'We are going to ignore possible mismatch errors in what the 
                        'attribute has for the dimensions and the actual field declaration is
                        'The FilePut will catch these problems.  
                        'The array might not be initialized and parsing the name correctly to calculate the dims
                        'isn't worth the possible bugs we could introduce
                        ElementCount = attrFixedArray.Length

                        GetFieldSize(field_info, ElementType, align, ElementSize)

                    End If

                    SetAlignment(align)
                    m_StructLength += (ElementCount * ElementSize)

                    Return False

                End If

                GetFieldSize(field_info, FieldType, align, size)
                SetAlignment(align)
                m_StructLength += size

                Return False

            End Function

            Private Sub GetFieldSize(ByVal field_info As Reflection.FieldInfo, ByVal FieldType As System.Type, ByRef align As Integer, ByRef size As Integer)

                Select Case Type.GetTypeCode(FieldType)

                    Case TypeCode.String
                        Dim attributeList As Object() = field_info.GetCustomAttributes(GetType(VBFixedStringAttribute), False)

                        If attributeList Is Nothing OrElse attributeList.Length = 0 Then
                            align = 4
                            size = 4
                        Else

                            Dim ma As VBFixedStringAttribute
                            Dim length As Integer

                            ma = CType(attributeList(0), VBFixedStringAttribute)

                            length = ma.Length
                            If length = 0 Then
                                length = -1
                            End If
                            size = length
                        End If

                    Case TypeCode.Single
                        align = 4
                        size = 4

                    Case TypeCode.Double
                        align = 8
                        size = 8

                    Case TypeCode.Int16
                        align = 2
                        size = 2

                    Case TypeCode.Int32
                        align = 4
                        size = 4

                    Case TypeCode.Byte
                        align = 1
                        size = 1

                    Case TypeCode.Int64
                        align = 8
                        size = 8

                    Case TypeCode.DateTime
                        align = 8
                        size = 8

                    Case TypeCode.Boolean
                        align = 2
                        size = 2

                    Case TypeCode.Decimal
                        align = 16
                        size = 16

                    Case TypeCode.Char
                        align = 2
                        size = 2

                    Case TypeCode.DBNull
                        Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedFieldType2, field_info.Name, "DBNull")), vbErrors.IllegalFuncCall)
                End Select

                If FieldType Is GetType(System.Exception) Then
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedFieldType2, field_info.Name, "Exception")), vbErrors.IllegalFuncCall)
                ElseIf FieldType Is GetType(System.Reflection.Missing) Then
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedFieldType2, field_info.Name, "Missing")), vbErrors.IllegalFuncCall)

                    'If type defined for the Field is Object, then throw an exception
                    'NOTE: THIS IS NOT THE SAME AS "TypeOf FieldType Is Object"
                ElseIf FieldType Is GetType(Object) Then
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_UnsupportedFieldType2, field_info.Name, "Object")), vbErrors.IllegalFuncCall)
                End If
            End Sub
        End Class
    End Class
End Namespace
