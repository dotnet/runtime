' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports Microsoft.VisualBasic.FileIO
Imports System
Imports Xunit

Namespace Microsoft.VisualBasic.Tests.VB
    Public NotInheritable Class TextFieldParserTests
        Inherits FileIOTestBase

        Private Const HeaderLine As String = "seq,oneword,age,street,city,state,zip,dollar,pick,date,paragraph"
        Private Const ShortHeaderLine As String = "Field1,Field2,Field3,Field4"

        Private Shared ReadOnly FixedFormatData() As String = {
            "Err  1001                  Cannot access resource.",
            "Err  2014                  Resource not found.",
            "Acc  10/03/2009 User1      Administrator.",
            "Err  0323                  Warning: Invalid access attempt.",
            "Acc  10/03/2009 User2      Standard user.",
            "Acc  10/04/2009 User2      Standard user."
        }

        <Fact>
        Public Shared Sub BadPathTest()
            Using TestBase As New TextFieldParserTests
                Assert.Throws(Of IO.FileNotFoundException)(Function() New TextFieldParser(IO.Path.ChangeExtension(CreateTestFile(TestBase:=TestBase, TestData:=(HeaderLine & vbCrLf & """" & vbCrLf).ToCharArray, PathFromBase:="", TestFileName:=""), ".txt")))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CSVBadDataTest()
            Using TestBase As New TextFieldParserTests
                Using MyReader As New TextFieldParser(CreateTestFile(TestBase, TestData:=(HeaderLine & ",""").ToCharArray, PathFromBase:="", TestFileName:=""))
                    MyReader.TextFieldType = FieldType.Delimited
                    MyReader.Delimiters = New String() {","}
                    While Not MyReader.EndOfData
                        Assert.Throws(Of MalformedLineException)(Function() MyReader.ReadFields())
                    End While
                End Using
            End Using
        End Sub

        <Fact>
        Public Shared Sub CSVDataTest()
            Const CSVData As String =
                    HeaderLine & vbCrLf &
            "1,Test,99,Two Words,Rumnoman,KS,80586,$8333.85,GREEN,09/28/1929,""Ipmadaw udoteac vu ote ozehop ane lujat, lar buh cugi lef owici tat liz cogde.
Fatigiug kojitfu taapi iz alujok me zimipa koz latkekij vem fo si cepzizhub li por ejcirlu.
Ewujajlu mivec tooju ad bu cowic irtafit ehaoca vojhehfo aztidun zo wezecmo abe muz wikhutwen idce.
Vesu sejawga tef lahi dirueg si uwmac bidiw nowidza daime sapmim ki casdun urokir tawdac rahaw beiweed."""

            Using TestBase As New TextFieldParserTests
                Using MyReader As New TextFieldParser(CreateTestFile(TestBase, TestData:=CSVData.ToCharArray, PathFromBase:="", TestFileName:=""))
                    MyReader.TextFieldType = FieldType.Delimited
                    MyReader.Delimiters = New String() {","}
                    Dim currentRow As String()
                    Assert.False(MyReader.EndOfData)
                    Dim HeaderSplit() As String = HeaderLine.Split(CType(",", Char()))
                    currentRow = MyReader.ReadFields()
                    For i As Integer = 0 To HeaderSplit.Length - 1
                        Assert.Equal(HeaderSplit(i), currentRow(i))
                    Next

                    Assert.False(MyReader.EndOfData)
                    currentRow = MyReader.ReadFields()
                    Assert.Equal(11, currentRow.Length)
                    Assert.Equal("1", currentRow(0))
                    Assert.Equal("Test", currentRow(1))
                    Assert.Equal("99", currentRow(2))
                    Assert.Equal("Two Words", currentRow(3))
                    Assert.Equal("Rumnoman", currentRow(4))
                    Assert.Equal("KS", currentRow(5))
                    Assert.Equal("80586", currentRow(6))
                    Assert.Equal("$8333.85", currentRow(7))
                    Assert.Equal("GREEN", currentRow(8))
                    Assert.Equal("09/28/1929", currentRow(9))
                    Assert.Equal("Ipmadaw udoteac vu ote ozehop ane lujat, lar buh cugi lef owici tat liz cogde.
Fatigiug kojitfu taapi iz alujok me zimipa koz latkekij vem fo si cepzizhub li por ejcirlu.
Ewujajlu mivec tooju ad bu cowic irtafit ehaoca vojhehfo aztidun zo wezecmo abe muz wikhutwen idce.
Vesu sejawga tef lahi dirueg si uwmac bidiw nowidza daime sapmim ki casdun urokir tawdac rahaw beiweed.", currentRow(10))
                    Assert.True(MyReader.EndOfData)
                End Using
            End Using
        End Sub

        <Fact>
        Public Shared Sub CSVDataTestEmptyFields()
            Const CSVData As String =
                    ShortHeaderLine & vbCrLf & "1,,"""",4"

            Using TestBase As New TextFieldParserTests
                Using MyReader As New TextFieldParser(CreateTestFile(TestBase, TestData:=CSVData.ToCharArray, PathFromBase:="", TestFileName:=""))
                    MyReader.TextFieldType = FieldType.Delimited
                    MyReader.Delimiters = New String() {","}
                    Dim currentRow As String()
                    Assert.False(MyReader.EndOfData)
                    Dim HeaderSplit() As String = ShortHeaderLine.Split(CType(",", Char()))
                    currentRow = MyReader.ReadFields()
                    For i As Integer = 0 To HeaderSplit.Length - 1
                        Assert.Equal(HeaderSplit(i), currentRow(i))
                    Next

                    Assert.False(MyReader.EndOfData)
                    currentRow = MyReader.ReadFields()
                    Assert.Equal(4, currentRow.Length)
                    Assert.Equal("1", currentRow(0))
                    Assert.Equal("", currentRow(1))
                    Assert.Equal("", currentRow(2))
                    Assert.Equal("4", currentRow(3))
                    Assert.True(MyReader.EndOfData)
                End Using
            End Using
        End Sub

        <Fact>
        Public Shared Sub CSVDataTestLeadingandtrailingspaces()
            Const CSVData As String =
                    ShortHeaderLine & vbCrLf & "1, two,three ,4"

            Using TestBase As New TextFieldParserTests
                Using MyReader As New TextFieldParser(CreateTestFile(TestBase, TestData:=CSVData.ToCharArray, PathFromBase:="", TestFileName:=""))
                    MyReader.TextFieldType = FieldType.Delimited
                    MyReader.Delimiters = New String() {","}
                    Dim currentRow As String()
                    Assert.False(MyReader.EndOfData)
                    Dim HeaderSplit() As String = ShortHeaderLine.Split(CType(",", Char()))
                    currentRow = MyReader.ReadFields()
                    For i As Integer = 0 To HeaderSplit.Length - 1
                        Assert.Equal(HeaderSplit(i), currentRow(i))
                    Next

                    Assert.False(MyReader.EndOfData)
                    currentRow = MyReader.ReadFields()
                    Assert.Equal(4, currentRow.Length)
                    Assert.Equal("1", currentRow(0))
                    Assert.Equal("two", currentRow(1))
                    Assert.Equal("three", currentRow(2))
                    Assert.Equal("4", currentRow(3))
                End Using
            End Using
        End Sub

        <Fact>
        Public Shared Sub CSVDataTestSpacesOnly()
            Const CSVData As String =
                ShortHeaderLine & vbCrLf & "1,2,3, " & vbCrLf & " ,2,3,4"

            Using TestBase As New TextFieldParserTests
                Using MyReader As New TextFieldParser(CreateTestFile(TestBase, TestData:=CSVData.ToCharArray, PathFromBase:="", TestFileName:=""))
                    MyReader.TextFieldType = FieldType.Delimited
                    MyReader.Delimiters = New String() {","}
                    Dim currentRow As String()
                    Assert.False(MyReader.EndOfData)
                    Dim HeaderSplit() As String = ShortHeaderLine.Split(CType(",", Char()))
                    currentRow = MyReader.ReadFields()
                    For i As Integer = 0 To HeaderSplit.Length - 1
                        Assert.Equal(HeaderSplit(i), currentRow(i))
                    Next

                    Assert.False(MyReader.EndOfData)
                    currentRow = MyReader.ReadFields()
                    Assert.Equal(4, currentRow.Length)
                    Assert.Equal("1", currentRow(0))
                    Assert.Equal("2", currentRow(1))
                    Assert.Equal("3", currentRow(2))
                    Assert.Equal("", currentRow(3))
                    currentRow = MyReader.ReadFields()
                    Assert.Equal(4, currentRow.Length)
                    Assert.Equal("", currentRow(0))
                    Assert.Equal("2", currentRow(1))
                    Assert.Equal("3", currentRow(2))
                    Assert.Equal("4", currentRow(3))
                    Assert.True(MyReader.EndOfData)
                End Using
            End Using
        End Sub

        <Fact>
        Public Shared Sub FixedFormatTest()
            Dim FixedFieldWidths() As Integer = {5, 10, 6, -1}
            Using TestBase As New TextFieldParserTests
                Using MyReader As New TextFieldParser(CreateTestFile(TestBase, TestData:=String.Join(vbCrLf, FixedFormatData).ToCharArray, PathFromBase:="", TestFileName:=""))
                    MyReader.TextFieldType = FieldType.FixedWidth
                    MyReader.SetFieldWidths(FixedFieldWidths)

                    Dim CurrentRow As String()
                    Dim CurrentRowIndex As Integer = 0
                    While Not MyReader.EndOfData
                        CurrentRow = MyReader.ReadFields()
                        Dim CurrentRowData As String = FixedFormatData(CurrentRowIndex)
                        Assert.Equal(FixedFieldWidths.Length, CurrentRow.Length)
                        Assert.Equal(CurrentRow(0), CurrentRowData.Substring(0, FixedFieldWidths(0)).Trim)
                        Assert.Equal(CurrentRow(1), CurrentRowData.Substring(FixedFieldWidths(0), FixedFieldWidths(1)).Trim)
                        Assert.Equal(CurrentRow(2), CurrentRowData.Substring(FixedFieldWidths(0) + FixedFieldWidths(1), FixedFieldWidths(2)).Trim)
                        Assert.Equal(CurrentRow(3), CurrentRowData.Substring(FixedFieldWidths(0) + FixedFieldWidths(1) + FixedFieldWidths(2)).Trim)
                        CurrentRowIndex += 1
                    End While
                End Using
            End Using
        End Sub

        ' code taken from  https://docs.microsoft.com/en-us/dotnet/visual-basic/developing-apps/programming/drives-directories-files/how-to-read-from-text-files-with-multiple-formats
        <Fact>
        Public Shared Sub MultiFormatTest()
            Dim MultipleFormatData() As String = {
            "Err  1001 Cannot access resource.",
            "Err  2014 Resource not found.",
            "Acc  10/03/2009User1      Administrator.",
            "Err  0323 Warning: Invalid access attempt.",
            "Acc  10/03/2009User2      Standard user.",
            "Acc  10/04/2009User2      Standard user."
        }
            Dim ErrorFormat As Integer() = {5, 5, -1}
            Dim StdFormat As Integer() = {5, 10, 11, -1}

            Using TestBase As New TextFieldParserTests
                Using MyReader As New TextFieldParser(CreateTestFile(TestBase, TestData:=String.Join(vbCrLf, FixedFormatData).ToCharArray, PathFromBase:="", TestFileName:=""))
                    MyReader.TextFieldType = FieldType.FixedWidth
                    MyReader.FieldWidths = StdFormat

                    Dim useErrorFormat As Boolean
                    Dim CurrentRow As String()
                    Dim CurrentRowIndex As Integer = 0
                    While Not MyReader.EndOfData
                        Dim rowType = MyReader.PeekChars(3)
                        If String.Compare(rowType, "Err") = 0 Then
                            ' If this line describes an error, the format of the row will be different.
                            useErrorFormat = True
                            MyReader.SetFieldWidths(ErrorFormat)
                        Else
                            ' Otherwise parse the fields normally
                            useErrorFormat = False
                            MyReader.SetFieldWidths(StdFormat)
                        End If

                        CurrentRow = MyReader.ReadFields
                        Dim CurrentRowData As String = MultipleFormatData(CurrentRowIndex)
                        If useErrorFormat Then
                            Assert.Equal(ErrorFormat.Length, CurrentRow.Length)
                            Assert.Equal(CurrentRow(0), CurrentRowData.Substring(0, ErrorFormat(0)).Trim)
                            Assert.Equal(CurrentRow(1), CurrentRowData.Substring(ErrorFormat(0), ErrorFormat(1)).Trim)
                            Assert.Equal(CurrentRow(2), CurrentRowData.Substring(ErrorFormat(0) + ErrorFormat(1)).Trim)
                        Else
                            Assert.Equal(StdFormat.Length, CurrentRow.Length)
                            Assert.Equal(CurrentRow(0), CurrentRowData.Substring(0, StdFormat(0)).Trim)
                            Assert.Equal(CurrentRow(1), CurrentRowData.Substring(StdFormat(0), StdFormat(1)).Trim)
                            Assert.Equal(CurrentRow(2), CurrentRowData.Substring(StdFormat(0) + StdFormat(1), StdFormat(2)).Trim)
                            Assert.Equal(CurrentRow(3), CurrentRowData.Substring(StdFormat(0) + StdFormat(1) + StdFormat(2)).Trim)
                        End If
                        CurrentRowIndex += 1
                    End While
                End Using
            End Using
        End Sub

    End Class
End Namespace
