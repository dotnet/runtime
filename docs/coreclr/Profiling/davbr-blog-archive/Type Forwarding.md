*This blog post originally appeared on David Broman's blog on 9/30/2009*


MSDN defines “type forwarding” as moving “a type to another assembly without having to recompile applications that use the original assembly”.  In this post, I’ll talk about examining a particular type in Microsoft’s .NET Framework library that gets forwarded, how you can use type forwarding for your own types, and what type forwarding looks like to consumers of the profiling API.  For some more official background on type forwarding, visit the MSDN [topic](http://msdn.microsoft.com/en-us/library/ms404275(VS.100).aspx).  If you Bing type forwarding you’ll find many blogs that talk about it as well.  Yes, that’s right.  I used Bing as a verb.  Get used to it; Bing is awesome.

Type forwarding is nothing new.  However, in CLR V4, we are enabling type forwarding to work with generic types.  And there has been some new refactoring in System.Core.  This means you should expect to see type forwarding used more often than it had been in the past.  So if you code up a profiler, you should make sure you can deal with type forwarding appropriately.  The good news is that profiler code that uses the profiling API to inspect types generally should not need to change.  But if you do certain kinds of metadata lookups yourself, you may need to be aware of type forwarding.  More on that later.

## Example: TimeZoneInfo

The example I’ll use where the .NET Framework uses type forwarding is the TimeZoneInfo class.  In CLR V4, TimeZoneInfo is now forwarded from System.Core.dll to mscorlib.dll.  If you open the CLR V4 copy of System.Core.dll in ildasm and choose Dump, you'll see the following:

| 
```
.class extern /*27000004*/ forwarder System.TimeZoneInfo
 {
 .assembly extern mscorlib /*23000001*/ 
 }
```
 |

In each assembly’s metadata is an exported types table.  The above means that System.Core.dll's exported types table includes an entry for System.TimeZoneInfo (indexed by token 27000004).  What's significant is that System.Core.dll no longer has a typeDef for System.TimeZoneInfo, only an exported type.  The fact that the token begins at the left with 0x27 tells you that it's an mdtExportedType (not a mdtTypeDef, which begins at the left with 0x02).

At run-time, if the CLR type loader encounters this exported type, it knows it must now look in mscorlib for System.TimeZoneInfo.  And by the way, if someday mscorlib chooses to forward the type elsewhere, and thus the type loader found another exported type with name System.TimeZoneInfo in mscorlib, then the type loader would have to make yet another hop to wherever that exported type pointed.

## Walkthrough 1: Observe the forwarding of System.TimeZoneInfo

This walkthrough assumes you have .NET 4.0 or later installed **and** an older release of .NET, such as .NET 3.5, installed.

Code up a simple C# app that uses System.TimeZoneInfo:
```
namespace test 
{ 
    class Class1 
    { 
        static void Main(string[] args) 
        { 
            System.TimeZoneInfo ti = null; 
        } 
    } 
}
```

Next, compile this into an exe using a CLR V2-based toolset (e.g., .NET 3.5).  You can use Visual Studio, or just run from the command-line (but be sure your path points to the pre-.NET 4.0 C# compiler!).  Example:

```
csc /debug+ /o- /r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\v3.5\System.Core.dll" Class1.cs
```

Again, be sure you’re using an old csc.exe from, say, a NET 3.5 installation.  To verify, open up Class1.exe in ildasm, and take a look at Main().  It should look something like this:

```
.method /*06000001*/ private hidebysig static 
 void Main(string[] args) cil managed
 {
 .entrypoint
 // Code size 4 (0x4)
 .maxstack 1
 **.locals /*11000001*/ init ([0] class [System.Core/*23000002*/]System.TimeZoneInfo/*01000006*/ ti)**
 IL\_0000: nop
 IL\_0001: ldnull
 IL\_0002: stloc.0
 IL\_0003: ret
 } // end of method Class1::Main
```

The key here is to note that the IL uses a TypeRef for System.TimeZoneInfo (01000006) that points to **System.Core.dll**.  When you run Class1.exe against a .NET 3.5 runtime, it will find System.TimeZoneInfo in System.Core.dll as usual, and just use that, since System.TimeZoneInfo actually is defined in System.Core.dll in pre-.NET 4.0 frameworks.  However, what happens when you run Class1.exe against .NET 4.0 without recompiling?  Type forwarding would get invoked!

Note that, if you were to build the above C# code using the .NET 4.0 C# compiler, it would automatically have generated a TypeRef that points to mscorlib.dll instead, so you wouldn't be able to observe the type forwarding at run-time.

Ok, so how do we run this pre-.NET 4.0 executable against .NET 4.0?  A config file, of course.  Paste the following into a file named Class1.exe.config that sits next to Class1.exe:

```
<configuration\>
 <startup\>
 <supportedRuntime version="v4.0.20506"/>
 </startup\>
 </configuration\>
```

The above will force Class1.exe to bind against .NET 4.0 Beta 1.  And when it comes time to look for TimeZoneInfo, the CLR will first look in System.Core.dll, find the exported types table entry, and then hop over to mscorlib.dll to load the type.  What does that look like to your profiler?  Make your guess and hold that thought.  First, another walkthrough…

## Walkthrough 2: Forwarding your own type

To experiment with forwarding your own types, the process is:

- Create Version 1 of your library 
 
  - Create version 1 of your library assembly that defines your type (MyLibAssemblyA.dll) 
  - Create an app that references your type in MyLibAssemblyA.dll (MyClient.exe) 
- Create version 2 of your library 
 
  - Recompile MyLibAssemblyA.dll to forward your type elsewhere (MyLibAssemblyB.dll) 
  - Don’t recompile MyClient.exe.  Let it still think the type is defined in MyLibAssemblyA.dll. 

### Version 1

Just make a simple C# DLL that includes your type Foo.  Something like this (MyLibAssemblyA.cs):

```
using System;
public class Foo
{
}
```

and compile it into MyLibAssemblyA.dll:

```
csc /target:library /debug+ /o- MyLibAssemblyA.cs
```

Then make yourself a client app that references Foo.

```
using System;
public class Test
{
  public static void Main()
  {
    Foo foo = new Foo();
    Console.WriteLine(typeof(Foo).AssemblyQualifiedName);
  }
}
```

and compile this into MyClient.exe:

```
csc /debug+ /o- /r:MyLibAssemblyA.dll MyClient.cs
```

When you run MyClient.exe, you get this boring output:

```
Foo, MyLibAssemblyA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
```

Ok, time to upgrade!

### Version 2
Time goes by, your library is growing, and its time to split it into two DLLs.  Gotta move Foo into the new DLL.  Save this into MyLibAssemblyB.cs
```
using System; 
public class Foo 
{ 
}
```

compile that into your new DLL, MyLibAssemblyB.dll:
```
csc /target:library /debug+ /o- MyLibAssemblyB.cs
```

And for the type forward.  MyLibAssemblyA.cs now becomes:
```
using System;
using System.Runtime.CompilerServices;
 [assembly: TypeForwardedTo(typeof(Foo))]
```

compile that into MyLibAssemblyA.dll (overwriting your Version 1 copy of that DLL):
```
csc /target:library /debug+ /o- /r:MyLibAssemblyB.dll MyLibAssemblyA.cs
```

Now, when you rerun MyClient.exe (without recompiling!), it will look for Foo first in MyLibAssemblyA.dll, and then hop over to MyLibAssemblyB.dll:
```
Foo, MyLibAssemblyB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
```

And this all despite the fact that MyClient.exe still believes that Foo lives in MyLibAssemblyA:
```
.method /*06000001*/ public hidebysig static 
 void Main() cil managed
 {
 .entrypoint
 // Code size 29 (0x1d)
 .maxstack 1
 .locals /*11000001*/ init ([0] class [MyLibAssemblyA/*23000002*/]Foo/*01000006*/ foo)
 IL\_0000: nop
 IL\_0001: newobj instance void [MyLibAssemblyA/*23000002*/]Foo/*01000006*/::.ctor() /* 0A000004 */
 IL\_0006: stloc.0
 **IL\_0007: ldtoken [MyLibAssemblyA/*23000002*/]Foo/*01000006*/**
 IL\_000c: call class [mscorlib/*23000001*/]System.Type/*01000007*/ [mscorlib/*23000001*/]System.Type/*01000007*/::GetTypeFromHandle(valuetype [mscorlib/*23000001*/]System.RuntimeTypeHandle/*01000008*/) /* 0A000005 */
 IL\_0011: callvirt instance string [mscorlib/*23000001*/]System.Type/*01000007*/::get\_AssemblyQualifiedName() /* 0A000006 */
 IL\_0016: call void [mscorlib/*23000001*/]System.Console/*01000009*/::WriteLine(string) /* 0A000007 */
 IL\_001b: nop
 IL\_001c: ret
 } // end of method Test::Main
```
 |

## Profilers

What does this look like to profilers?  Types are represented as ClassIDs, and modules as ModuleIDs.  When you query for info about a ClassID (via GetClassIDInfo2()), you get one and only one ModuleID to which it belongs.  So when a ClassID gets forwarded from one ModuleID to another, which does the profiling API report as its real home?  The answer: always the final module to which the type has been forwarded and therefore the module whose metadata contains the TypeDef (and not the exported type table entry).

This should make life easy for profilers, since they generally expect to be able to find the metadata TypeDef for a type inside the ModuleID that the profiling API claims is the type’s home.  So much of type forwarding will be transparent to your profiler.

However, type forwarding is important to understand if your profiler needs to follow metadata references directly.  More generally, if your profiler is reading through metadata and expects to come across a typeDef (e.g., perhaps a metadata reference points to a type in that module, or perhaps your profiler expects certain known types to be in certain modules), then your profiler should be prepared to find an mdtExportedType instead, and to deal gracefully with it rather than doing something silly like crashing.

In any case, whether you think your profiler will be affected by type forwarding, be sure to test, test, test!

  