' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Text
Imports System.Globalization
Imports System.Reflection
Imports System.Diagnostics
Imports Microsoft.VisualBasic.CompilerServices.Symbols

Namespace Microsoft.VisualBasic.CompilerServices

    ' Purpose: various helpers for the vb runtime functions
    Partial Public NotInheritable Class Utils

        Friend Const SEVERITY_ERROR As Integer = &H80000000I
        Friend Const FACILITY_CONTROL As Integer = &HA0000I
        Friend Const FACILITY_RPC As Integer = &H10000I
        Friend Const FACILITY_ITF As Integer = &H40000I
        Friend Const SCODE_FACILITY As Integer = &H1FFF0000I
        Private Const ERROR_INVALID_PARAMETER As Integer = 87

        Friend Const chPeriod As Char = "."c
        Friend Const chSpace As Char = ChrW(32)
        Friend Const chIntlSpace As Char = ChrW(&H3000)
        Friend Const chZero As Char = "0"c
        Friend Const chHyphen As Char = "-"c
        Friend Const chPlus As Char = "+"c
        Friend Const chLetterA As Char = "A"c
        Friend Const chLetterZ As Char = "Z"c
        Friend Const chColon As Char = ":"c
        Friend Const chSlash As Char = "/"c
        Friend Const chBackslash As Char = "\"c
        Friend Const chTab As Char = ControlChars.Tab
        Friend Const chCharH0A As Char = ChrW(&HA)
        Friend Const chCharH0B As Char = ChrW(&HB)
        Friend Const chCharH0C As Char = ChrW(&HC)
        Friend Const chCharH0D As Char = ChrW(&HD)
        Friend Const chLineFeed As Char = ChrW(10)
        Friend Const chDblQuote As Char = ChrW(34)

        Friend Const chGenericManglingChar As Char = "`"c

        Friend Const OptionCompareTextFlags As CompareOptions = (CompareOptions.IgnoreCase Or CompareOptions.IgnoreWidth Or CompareOptions.IgnoreKanaType)

        Private Const ResourceMsgDefault As String = "Message text unavailable.  Resource file 'Microsoft.VisualBasic resources' not found."
        Private Const VBDefaultErrorID As String = "ID95"
        Friend Shared m_achIntlSpace() As Char = {chSpace, chIntlSpace}
        Private Shared ReadOnly s_voidType As Type = System.Type.GetType("System.Void")
        Private Shared s_VBRuntimeAssembly As System.Reflection.Assembly

        Private Shared Function GetFallbackMessage(ByVal name As String, ByVal ParamArray args() As Object) As String
            'last-ditch effort; just give back name
            Return name
        End Function

        Friend Shared Function GetResourceString(ByVal ResourceId As vbErrors) As String
            Dim id As String = "ID" & CStr(ResourceId)
            Return SR.GetResourceString(id, id)
        End Function

        <System.ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Friend Shared Function GetResourceString(ByVal ResourceKey As String) As String

            Dim s As String = Nothing

            Try
                s = SR.GetResourceString(ResourceKey)
                ' this may be unknown error, so try getting default message
                If s Is Nothing Then
                    s = SR.GetResourceString(VBDefaultErrorID)
                End If

                'if we have found nothing, get a fallback message.
                If s Is Nothing Then
                    s = GetFallbackMessage(ResourceKey)
                End If
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                s = ResourceMsgDefault
            End Try

            Return s
        End Function

        ''' <summary>
        ''' Retrieves a resource string and formats it by replacing placeholders with parameters.
        ''' </summary>
        ''' <param name="ResourceKey">The resource string identifier</param>
        ''' <param name="Args">An array of parameters used to replace placeholders</param>
        ''' <returns>The resource string if found or an error message string</returns>
        Public Shared Function GetResourceString(ByVal ResourceKey As String, ByVal ParamArray Args() As String) As String

            System.Diagnostics.Debug.Assert(Not ResourceKey = "", "ResourceKey is missing")
            System.Diagnostics.Debug.Assert(Not Args Is Nothing, "No Args")

            Dim UnformattedString As String = Nothing
            Dim FormattedString As String = Nothing
            Try
                'Get unformatted string which may have place holders ie "Hello, {0}. How is {1}?"
                UnformattedString = GetResourceString(ResourceKey)

                'Replace placeholders with items from the passed in array
                FormattedString = String.Format(System.Threading.Thread.CurrentThread.CurrentCulture, UnformattedString, Args)

                'Rethrow hosting exceptions
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex

            Catch ex As Exception
                System.Diagnostics.Debug.Fail("Unable to get and format string for ResourceKey: " & ResourceKey)
            Finally
                System.Diagnostics.Debug.Assert(Not UnformattedString = "", "Unable to get string for ResourceKey: " & ResourceKey)
                System.Diagnostics.Debug.Assert(Not FormattedString = "", "Unable to format string for ResourceKey: " & ResourceKey)
            End Try

            'Return the string if we have one otherwise return a default error message
            If Not FormattedString = "" Then
                Return FormattedString
            Else
                Return UnformattedString 'will contain an error string from the attempt to load via the GetResourceString() overload we call internally
            End If
        End Function

        Friend Shared Function StdFormat(ByVal s As String) As String
            Dim nfi As NumberFormatInfo
            Dim iIndex As Integer
            Dim c0, c1, c2 As Char
            Dim sb As StringBuilder

            nfi = Threading.Thread.CurrentThread.CurrentCulture.NumberFormat
            iIndex = s.IndexOf(nfi.NumberDecimalSeparator)

            If iIndex = -1 Then
                Return s
            End If

            Try
                c0 = s.Chars(0)
                c1 = s.Chars(1)
                c2 = s.Chars(2)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                'Ignore, should default to 0 values
            End Try

            If s.Chars(iIndex) = chPeriod Then
                'Optimization: no period replacement needed
                'avoids creating stringbuilder and copying string 

                'If format is "0.xxxx" then replace 0 with space 
                If c0 = chZero AndAlso c1 = chPeriod Then
                    Return s.Substring(1)

                    'If format is "-0.xxxx", "+0.xxxx", " 0.xxxx" then shift everything down over the zero
                ElseIf (c0 = chHyphen OrElse c0 = chPlus OrElse c0 = chSpace) AndAlso c1 = chZero AndAlso c2 = chPeriod Then
                    'Fall down below and use a stringbuilder
                Else
                    'No change
                    Return s
                End If
            End If

            sb = New StringBuilder(s)
            sb.Chars(iIndex) = chPeriod ' change decimal separator to "."

            'If format is "0.xxxx" then replace 0 with space 
            If (c0 = chZero AndAlso c1 = chPeriod) Then
                StdFormat = sb.ToString(1, sb.Length - 1)
                'If format is "-0.xxxx", "+0.xxxx", " 0.xxxx" then shift everything down over the zero
            ElseIf (c0 = chHyphen OrElse c0 = chPlus OrElse c0 = chSpace) AndAlso c1 = chZero AndAlso c2 = chPeriod Then
                sb.Remove(1, 1)
                StdFormat = sb.ToString()
            Else
                StdFormat = sb.ToString()
            End If
        End Function

        Friend Shared Function OctFromLong(ByVal Val As Long) As String
            'System.Radix is being removed from the .NET platform, so compute this locally.
            Dim Buffer As String = ""
            Dim ModVal As Integer
            Dim CharZero As Integer = Convert.ToInt32(chZero)
            Dim Negative As Boolean

            If Val < 0 Then
                Val = Int64.MaxValue + Val + 1
                Negative = True
            End If

            'Pull apart the number and put the digits (in reverse order) into the buffer.
            Do
                ModVal = CInt(Val Mod 8)
                Val = Val >> 3
                Buffer = Buffer & ChrW(ModVal + CharZero)
            Loop While Val > 0

            Buffer = StrReverse(Buffer)

            If Negative Then
                Buffer = "1" & Buffer
            End If

            Return Buffer
        End Function

        Friend Shared Function OctFromULong(ByVal Val As ULong) As String
            'System.Radix is being removed from the .NET platform, so compute this locally.
            Dim Buffer As String = ""
            Dim ModVal As Integer
            Dim CharZero As Integer = Convert.ToInt32(chZero)

            'Pull apart the number and put the digits (in reverse order) into the buffer.
            Do
                ModVal = CInt(Val Mod 8UL)
                Val = Val >> 3
                Buffer = Buffer & ChrW(ModVal + CharZero)
            Loop While Val <> 0UL

            Buffer = StrReverse(Buffer)

            Return Buffer
        End Function

        Friend Shared Function GetCultureInfo() As CultureInfo
            Return CultureInfo.CurrentCulture
        End Function

        Friend Shared Function GetInvariantCultureInfo() As CultureInfo
            Return CultureInfo.InvariantCulture
        End Function

        Friend Shared ReadOnly Property VBRuntimeAssembly() As System.Reflection.Assembly
            Get
                If Not s_VBRuntimeAssembly Is Nothing Then
                    Return s_VBRuntimeAssembly
                End If

                ' if the cached assembly ref has not been set, then set it here
                s_VBRuntimeAssembly = GetType(Utils).Assembly
                Return s_VBRuntimeAssembly
            End Get
        End Property

        Friend Shared Function ToHalfwidthNumbers(ByVal s As String, ByVal culture As CultureInfo) As String
            Return s
        End Function

        Friend Shared Function IsHexOrOctValue(ByVal value As String, ByRef i64Value As Int64) As Boolean

            Dim ch As Char
            Dim length As Integer
            Dim firstNonspace As Integer
            Dim tmpValue As String

            length = value.Length

            Do While (firstNonspace < length)
                ch = value.Chars(firstNonspace)
                'We check that the length is at least FirstNonspace + 2 because otherwise the function
                'will throw undesired exceptions.
                If ch = "&"c AndAlso firstNonspace + 2 < length Then
                    GoTo GetSpecialValue
                End If
                If ch <> chSpace AndAlso ch <> chIntlSpace Then
                    Return False
                End If
                firstNonspace += 1
            Loop

            Return False

GetSpecialValue:
            ch = System.Char.ToLowerInvariant(value.Chars(firstNonspace + 1))

            tmpValue = ToHalfwidthNumbers(value.Substring(firstNonspace + 2), GetCultureInfo())
            If ch = "h"c Then
                i64Value = System.Convert.ToInt64(tmpValue, 16)
            ElseIf ch = "o"c Then
                i64Value = System.Convert.ToInt64(tmpValue, 8)
            Else
                Throw New FormatException
            End If
            Return True
        End Function

        Friend Shared Function IsHexOrOctValue(ByVal value As String, ByRef ui64Value As UInt64) As Boolean

            Dim ch As Char
            Dim length As Integer
            Dim firstNonspace As Integer
            Dim tmpValue As String

            length = value.Length

            Do While (firstNonspace < length)
                ch = value.Chars(firstNonspace)
                'We check that the length is at least FirstNonspace + 2 because otherwise the function
                'will throw undesired exceptions.
                If ch = "&"c AndAlso firstNonspace + 2 < length Then
                    GoTo GetSpecialValue
                End If
                If ch <> chSpace AndAlso ch <> chIntlSpace Then
                    Return False
                End If
                firstNonspace += 1
            Loop

            Return False

GetSpecialValue:
            ch = System.Char.ToLowerInvariant(value.Chars(firstNonspace + 1))

            tmpValue = ToHalfwidthNumbers(value.Substring(firstNonspace + 2), GetCultureInfo())
            If ch = "h"c Then
                ui64Value = System.Convert.ToUInt64(tmpValue, 16)
            ElseIf ch = "o"c Then
                ui64Value = System.Convert.ToUInt64(tmpValue, 8)
            Else
                Throw New FormatException
            End If
            Return True
        End Function

        Friend Shared Function VBFriendlyName(ByVal obj As Object) As String
            If obj Is Nothing Then
                Return "Nothing"
            End If

            Return VBFriendlyName(obj.GetType, obj)
        End Function

        Friend Shared Function VBFriendlyName(ByVal typ As System.Type) As String
            Return VBFriendlyNameOfType(typ)
        End Function

        Friend Shared Function VBFriendlyName(ByVal typ As System.Type, ByVal o As Object) As String
            If typ.IsCOMObject AndAlso (typ.FullName = "System.__ComObject") Then
                Return TypeNameOfCOMObject(o, False)
            End If

            Return VBFriendlyNameOfType(typ)
        End Function

        Friend Shared Function VBFriendlyNameOfType(ByVal typ As System.Type, Optional ByVal fullName As Boolean = False) As String

            Dim result As String
            Dim arraySuffix As String

            arraySuffix = GetArraySuffixAndElementType(typ)

            Debug.Assert(typ IsNot Nothing AndAlso Not typ.IsArray, "Error in array type processing")


            Dim tc As TypeCode
            If typ.IsEnum Then
                tc = TypeCode.Object
            Else
                tc = typ.GetTypeCode
            End If

            Select Case tc

                Case TypeCode.Boolean : result = "Boolean"
                Case TypeCode.SByte : result = "SByte"
                Case TypeCode.Byte : result = "Byte"
                Case TypeCode.Int16 : result = "Short"
                Case TypeCode.UInt16 : result = "UShort"
                Case TypeCode.Int32 : result = "Integer"
                Case TypeCode.UInt32 : result = "UInteger"
                Case TypeCode.Int64 : result = "Long"
                Case TypeCode.UInt64 : result = "ULong"
                Case TypeCode.Decimal : result = "Decimal"
                Case TypeCode.Single : result = "Single"
                Case TypeCode.Double : result = "Double"
                Case TypeCode.DateTime : result = "Date"
                Case TypeCode.Char : result = "Char"
                Case TypeCode.String : result = "String"

                Case Else

                    If IsGenericParameter(typ) Then
                        result = typ.Name
                        Exit Select
                    End If

                    Dim qualifier As String = Nothing 'yes, defaults to nothing but makes a warning go away about use before assignment
                    Dim name As String

                    Dim genericArgsSuffix As String = GetGenericArgsSuffix(typ)

                    If fullName Then
                        If typ.DeclaringType IsNot Nothing Then
                            qualifier = VBFriendlyNameOfType(typ.DeclaringType, fullName:=True)
                            name = typ.Name
                        Else
                            name = typ.FullName
                            ' Some types do not have FullName
                            If name Is Nothing Then
                                name = typ.Name
                            End If
                        End If
                    Else
                        name = typ.Name
                    End If

                    If genericArgsSuffix IsNot Nothing Then
                        Dim manglingCharIndex As Integer = name.LastIndexOf(chGenericManglingChar)

                        If manglingCharIndex <> -1 Then
                            name = name.Substring(0, manglingCharIndex)
                        End If

                        result = name & genericArgsSuffix
                    Else
                        result = name
                    End If

                    If qualifier IsNot Nothing Then
                        result = qualifier & chPeriod & result
                    End If

            End Select


            If arraySuffix IsNot Nothing Then
                result = result & arraySuffix
            End If

            Return result
        End Function

        Private Shared Function GetArraySuffixAndElementType(ByRef typ As Type) As String

            If Not typ.IsArray Then
                Return Nothing
            End If

            Dim arraySuffix As New Text.StringBuilder

            'Notice the reversing - VB array notation is reverse of clr array notation
            'i.e. (,)() in VB is [][,] in clr
            '
            Do

                arraySuffix.Append("("c)
                arraySuffix.Append(","c, typ.GetArrayRank() - 1)
                arraySuffix.Append(")"c)

                typ = typ.GetElementType

            Loop While typ.IsArray

            Return arraySuffix.ToString()
        End Function

        Private Shared Function GetGenericArgsSuffix(ByVal typ As Type) As String

            If Not typ.IsGenericType Then
                Return Nothing
            End If

            Dim typeArgs As Type() = typ.GetGenericArguments
            Dim totalTypeArgsCount As Integer = typeArgs.Length
            Dim typeArgsCount As Integer = totalTypeArgsCount

            If typ.DeclaringType IsNot Nothing AndAlso typ.DeclaringType.IsGenericType Then
                typeArgsCount = typeArgsCount - typ.DeclaringType.GetGenericArguments().Length
            End If

            If typeArgsCount = 0 Then
                Return Nothing
            End If

            Dim genericArgsSuffix As New Text.StringBuilder
            genericArgsSuffix.Append("(Of ")

            For i As Integer = totalTypeArgsCount - typeArgsCount To totalTypeArgsCount - 1

                genericArgsSuffix.Append(VBFriendlyNameOfType(typeArgs(i)))

                If i <> totalTypeArgsCount - 1 Then
                    genericArgsSuffix.Append(","c)
                End If
            Next

            genericArgsSuffix.Append(")"c)

            Return genericArgsSuffix.ToString
        End Function

        Friend Shared Function ParameterToString(ByVal parameter As ParameterInfo) As String

            Dim resultString As String = ""
            Dim parameterType As Type = parameter.ParameterType

            If parameter.IsOptional Then
                resultString &= "["
            End If

            If parameterType.IsByRef Then
                resultString &= "ByRef "
                parameterType = parameterType.GetElementType
            ElseIf IsParamArray(parameter) Then
                resultString &= "ParamArray "
            End If

            resultString &= parameter.Name & " As " & VBFriendlyNameOfType(parameterType, fullName:=True)

            If parameter.IsOptional Then

                Dim defaultValue As Object = parameter.DefaultValue

                If defaultValue Is Nothing Then
                    resultString &= " = Nothing"
                Else
                    Dim defaultValueType As System.Type = defaultValue.GetType
                    If defaultValueType IsNot s_voidType Then
                        If IsEnum(defaultValueType) Then
                            Throw New InvalidOperationException()
                        Else
                            resultString &= " = " & CStr(defaultValue)
                        End If
                    End If
                End If

                resultString &= "]"
            End If

            Return resultString
        End Function

        Friend Shared Function MethodToString(ByVal method As Reflection.MethodBase) As String

            Dim returnType As System.Type = Nothing
            Dim first As Boolean
            MethodToString = ""

            If method.MemberType = MemberTypes.Method Then returnType = DirectCast(method, MethodInfo).ReturnType

            If method.IsPublic Then
                MethodToString &= "Public "
            ElseIf method.IsPrivate Then
                MethodToString &= "Private "
            ElseIf method.IsAssembly Then
                MethodToString &= "Friend "
            End If

            If (method.Attributes And System.Reflection.MethodAttributes.Virtual) <> 0 Then
                If Not method.DeclaringType.IsInterface Then
                    MethodToString &= "Overrides "
                End If
            ElseIf IsShared(method) Then
                MethodToString &= "Shared "
            End If

            Dim op As UserDefinedOperator = UserDefinedOperator.UNDEF
            If IsUserDefinedOperator(method) Then
                op = MapToUserDefinedOperator(method)
            End If

            If op <> UserDefinedOperator.UNDEF Then
                If op = UserDefinedOperator.Narrow Then
                    MethodToString &= "Narrowing "
                ElseIf op = UserDefinedOperator.Widen Then
                    MethodToString &= "Widening "
                End If
                MethodToString &= "Operator "
            ElseIf returnType Is Nothing OrElse returnType Is s_voidType Then
                MethodToString &= "Sub "
            Else
                MethodToString &= "Function "
            End If

            If op <> UserDefinedOperator.UNDEF Then
                MethodToString &= OperatorNames(op)
            ElseIf method.MemberType = MemberTypes.Constructor Then
                MethodToString &= "New"
            Else
                MethodToString &= method.Name
            End If

            If IsGeneric(method) Then
                MethodToString &= "(Of "
                first = True
                For Each t As Type In GetTypeParameters(method)
                    If Not first Then MethodToString &= ", " Else first = False
                    MethodToString &= VBFriendlyNameOfType(t)
                Next
                MethodToString &= ")"
            End If

            MethodToString &= "("
            first = True

            For Each parameter As ParameterInfo In method.GetParameters()

                If Not first Then
                    MethodToString &= ", "
                Else
                    first = False
                End If

                MethodToString &= ParameterToString(parameter)
            Next

            MethodToString &= ")"

            If returnType Is Nothing OrElse returnType Is s_voidType Then
                'Sub has no return type
            Else
                MethodToString &= " As " & VBFriendlyNameOfType(returnType, fullName:=True)
            End If

        End Function

        Private Enum PropertyKind
            ReadWrite
            [ReadOnly]
            [WriteOnly]
        End Enum

        Friend Shared Function PropertyToString(ByVal prop As Reflection.PropertyInfo) As String

            Dim resultString As String = ""

            Dim kind As PropertyKind = PropertyKind.ReadWrite
            Dim parameters As ParameterInfo()
            Dim propertyType As Type

            'Most of the work will be done using the Getter or Setter.
            Dim accessor As MethodInfo = prop.GetGetMethod

            If accessor IsNot Nothing Then
                If prop.GetSetMethod IsNot Nothing Then
                    kind = PropertyKind.ReadWrite
                Else
                    kind = PropertyKind.ReadOnly
                End If

                parameters = accessor.GetParameters
                propertyType = accessor.ReturnType
            Else
                kind = PropertyKind.WriteOnly

                accessor = prop.GetSetMethod
                Dim setParameters As ParameterInfo() = accessor.GetParameters
                parameters = New ParameterInfo(setParameters.Length - 2) {}
                System.Array.Copy(setParameters, parameters, parameters.Length)
                propertyType = setParameters(setParameters.Length - 1).ParameterType
            End If

            resultString &= "Public "

            If (accessor.Attributes And MethodAttributes.Virtual) <> 0 Then
                If Not prop.DeclaringType.IsInterface Then
                    resultString &= "Overrides "
                End If
            ElseIf IsShared(accessor) Then
                resultString &= "Shared "
            End If

            If kind = PropertyKind.ReadOnly Then resultString &= "ReadOnly "
            If kind = PropertyKind.WriteOnly Then resultString &= "WriteOnly "

            resultString &= "Property " & prop.Name & "("

            Dim first As Boolean = True

            For Each parameter As ParameterInfo In parameters
                If Not first Then resultString &= ", " Else first = False

                resultString &= ParameterToString(parameter)
            Next

            resultString &= ") As " & VBFriendlyNameOfType(propertyType, fullName:=True)

            Return resultString
        End Function

        Friend Shared Function AdjustArraySuffix(ByVal sRank As String) As String
            Dim OneChar As Char
            Dim RevResult As String = Nothing
            Dim length As Integer = sRank.Length
            While length > 0
                OneChar = sRank.Chars(length - 1)
                Select Case OneChar
                    Case ")"c
                        RevResult = RevResult + "("c
                    Case "("c
                        RevResult = RevResult + ")"c
                    Case ","c
                        RevResult = RevResult + OneChar
                    Case Else
                        RevResult = OneChar + RevResult
                End Select
                length = length - 1
            End While
            Return RevResult
        End Function

        Friend Shared Function MemberToString(ByVal member As MemberInfo) As String
            Select Case member.MemberType
                Case MemberTypes.Method, MemberTypes.Constructor
                    Return MethodToString(DirectCast(member, MethodBase))

                Case MemberTypes.Field
                    Return FieldToString(DirectCast(member, FieldInfo))

                Case MemberTypes.Property
                    Return PropertyToString(DirectCast(member, PropertyInfo))

                Case Else
                    Return member.Name
            End Select
        End Function

        Friend Shared Function FieldToString(ByVal field As FieldInfo) As String
            Dim rtype As System.Type
            FieldToString = ""

            rtype = field.FieldType

            If field.IsPublic Then
                FieldToString &= "Public "
            ElseIf field.IsPrivate Then
                FieldToString &= "Private "
            ElseIf field.IsAssembly Then
                FieldToString &= "Friend "
            ElseIf field.IsFamily Then
                FieldToString &= "Protected "
            ElseIf field.IsFamilyOrAssembly Then
                FieldToString &= "Protected Friend "
            End If

            FieldToString &= field.Name
            FieldToString &= " As "
            FieldToString &= VBFriendlyNameOfType(rtype, fullName:=True)
        End Function
    End Class

End Namespace

