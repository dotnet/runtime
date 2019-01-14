using System;
using System.Collections;
using System.Runtime.CompilerServices;

//
// This assembly is not AOT-ed, so all calls into it transition to the interpreter
//

public interface InterpOnlyIFace
{
	int get_Field2 ();

	Type virt<T> ();
}

public class InterpOnly : InterpOnlyIFace
{
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int entry_1 (int i) {
		return i + 1;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static ArrayList corlib_call () {
		return new ArrayList (5);
	}

	public virtual int get_Field2 () {
		return 1;
	}

	public virtual Type virt<T> () {
		return typeof(T);
	}
}

public struct InterpOnlyStruct : InterpOnlyIFace
{
	public int Field;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public int get_Field () {
		return Field;
	}

	public int get_Field2 () {
		return Field;
	}

	public Type virt<T> () {
		return typeof(T);
	}
}
