*This blog post originally appeared on David Broman's blog on 10/10/2005*


Believe it or not, my last (rather large) post on stack walking actually left out several miscellaneous details about using DoStackSnapshot.  I'll be posting those details separately.  We'll start off with some light reading on exception filters.  No deadlocks this time, I promise.

For those of you diehard C# fans, you might be unaware of the existence of exception filters in managed code. While VB.NET makes them explicitly available to the programmer, C# does not. Filters are important to understand when you call DoStackSnapshot, as your results might look a little weird if you don't know how to interpret them.

First, a little background. For the full deal, check out the MSDN Library topic on VB.NET's [try/catch/finally statements](http://msdn.microsoft.com/library/default.asp?url=/library/en-us/vblr7/html/vastmTryCatchFinally.asp). But here's an appetizer. In VB.NET you can do this:

```  
Function Negative() As Boolean  
    Return False  
End Function  
  
Function Positive() As Boolean  
    Return True  
End Function  
  
Sub Thrower  
    Throw New Exception  
End Sub  
  
Sub Main()  
    Try  
        Thrower()  
    Catch ex As Exception When Negative()  
        MsgBox("Negative")  
    Catch ex As Exception When Positive()  
        MsgBox("Positive")  
    End Try  
End Sub  
```

The filters are the things that come after "When". We all know that, when an exception is thrown, its type must match the type specified in a Catch clause in order for that Catch clause to be executed. "When" is a way to further restrict whether a Catch clause will be executed. Now, not only must the exception's type match, but also the When clause must evaluate to True for that Catch clause to be chosen. In the example above, when we run, we'll skip the first Catch clause (because its filter returned False), and execute the second, thus showing a message box with "Positive" in it.  
  
The thing you need to realize about DoStackSnapshot's behavior (indeed, CLR in general) is that the execution of a When clause is really a separate function call. In the above example, imagine we take a stack snapshot while inside Positive(). Our managed-only stack trace, as reported by DoStackSnapshot, would then look like this (stack grows up):   
  
Positive  
Main  
Thrower  
Main  
  
It's that highlighted Main that seems odd at first. While the exception is thrown inside Thrower(), the CLR needs to execute the filter clauses to figure out which Catch wins.  These filter executions are actually _function calls_.  Since filter clauses don't have their own names, we just use the name of the function containing the filter clause for stack reporting purposes.  Thus, the highlighted Main above is the execution of a filter clause located inside Main (in this case, "When Positive()").  When each filter clause completes, we "return" back to Thrower() to continue our search for the filter that returns True.  Since this is how the call stack is built up, that's what DoStackSnapshot will report.  

