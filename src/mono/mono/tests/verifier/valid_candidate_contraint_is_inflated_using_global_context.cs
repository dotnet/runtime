using System;

/*

.class public abstract auto ansi beforefieldinit IronRuby.Runtime.Calls.ProtocolConversionAction`1<.ctor (class IronRuby.Runtime.Calls.ProtocolConversionAction`1<!TSelf>) TSelf>
       extends IronRuby.Runtime.Calls.ProtocolConversionAction
       implements class [mscorlib]System.IEquatable`1<!TSelf>,
                  [Microsoft.Scripting]Microsoft.Scripting.Runtime.IExpressionSerializable
{
  .field public static initonly !TSelf Instance
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void IronRuby.Runtime.Calls.ProtocolConversionAction::.ctor()
    IL_0006:  ret
  } // end of method ProtocolConversionAction`1::.ctor


.class public abstract auto ansi beforefieldinit 
IronRuby.Runtime.Calls.ConvertToReferenceTypeAction`2<.ctor (class IronRuby.Runtime.Calls.ConvertToReferenceTypeAction`2<!TSelf,!TTargetType>) TSelf,class TTargetType>
       extends class IronRuby.Runtime.Calls.ProtocolConversionAction`1<!TSelf>
{
  
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class IronRuby.Runtime.Calls.ProtocolConversionAction`1<!TSelf>::.ctor()
    IL_0006:  ret
  } // end of method ConvertToReferenceTypeAction`2::.ctor

*/

namespace IronRuby.Runtime.Calls {

public abstract class ProtocolConversionAction<TSelf>
	where TSelf : ProtocolConversionAction<TSelf>, new ()
{

}

public abstract class ConvertToReferenceTypeAction<TSelf, TTargetType> : ProtocolConversionAction <TSelf>
 	where TSelf : ConvertToReferenceTypeAction<TSelf, TTargetType>, new ()
	where TTargetType : class
{
	
}
}

public class Foo {}
public class Bar {}

public class BarToFoo : IronRuby.Runtime.Calls.ConvertToReferenceTypeAction <BarToFoo, Foo>
{
	
}

public class Driver
{
	static void Main () {
		//new Bar<Inst> ().Tst ();
		var x = new BarToFoo ();
	}
}