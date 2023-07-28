Public Class TestVbScope
    Public Shared Async Function Run() As Task
        Await RunVBScope(10)
        Await RunVBScope(1000)
    End Function

    Public Shared Async Function RunVBScope(data As Integer) As Task(Of Integer)
        Dim a As Integer
        a = 10
        If data < 999 Then
            Dim testVbScope As String
            Dim onlyInFirstScope As String
            testVbScope = "hello"
            onlyInFirstScope = "only-in-first-scope"
            System.Diagnostics.Debugger.Break()
            Await Task.Delay(1)
            Return data
        Else
            Dim testVbScope As String
            Dim onlyInSecondScope As String
            testVbScope = "hi"
            onlyInSecondScope = "only-in-second-scope"
            System.Diagnostics.Debugger.Break()
            Await Task.Delay(1)
            Return data
        End If

    End Function

End Class
