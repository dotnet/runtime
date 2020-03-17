using System;

#if WITH_MEMBERS
public class DisappearingType { }

public sealed class MissingAttribute : Attribute {}

public enum DisappearingEnum {
	V0
}
#endif


public sealed class MissingCtorAttribute : Attribute {
#if WITH_MEMBERS
	public MissingCtorAttribute (int i) {}
#endif
}

public sealed class BadAttrAttribute : Attribute {
#if WITH_MEMBERS
	public int Field, Field2;
	public int Property { get; set; }
	public int Property2 { get; set; }
	public int Property3 { get; set; }
#else
	public string Field2;
	public double Property2 { get; set; }
	public int Property3 { get { return 0; } }
#endif

}
