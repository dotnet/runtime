using System;
using System.Security.Permissions;
using System.Security.Principal;

[StrongNameIdentityPermission (SecurityAction.InheritanceDemand, PublicKey="0024000004800000940000000602000000240000525341310004000011000000db294bcb78b7361ed6eb5656b612ce266fc81da8c8c6cb04116fc29b5e1d09a02f6c0f387f6d97a1ce9bdbbeb2d874832ae2d2971e70144ea039c710dccab5fb0a36cb14268a83c9b435c1e7318e7915518b68c8ed056b104e76166d6cabe9b77383f26bcf6a0a0b09d04f37b2a407b47d39421a34f2fbc6e6701a1d5c2e8cbb")]
public abstract class BaseInheritanceDemand {

	public virtual int Test () 
	{
		return 1;
	}
}

public class InheritanceDemand : BaseInheritanceDemand {

	public override int Test ()
	{
		Console.WriteLine ("*1* [this should NOT print]");
		return base.Test ();
	}
}

public class NoSecurity {

	public int Test ()
	{
		Console.WriteLine ("*0* [this should print]");
		return 0;
	}
}
