using System;

namespace TestMono
{
	public interface IBase {
		int Do();
	}

	public interface IDerived : IBase {
	}

	public class Base : IBase {
		int IBase.Do() {
			return 1 + Do();
		}
		public virtual int Do() {
			return 1;
		}
	}

	public class Derived : Base, IDerived {
	}

	class Class1
	{
		static int Main(string[] args)
		{
			IDerived id = new Derived();
			if (id.Do() != 2)
				return 1;
			IBase ib = (IBase) id;
			if (ib.Do() != 2)
				return 2;
			Derived d = (Derived) id;
			if (d.Do() != 1)
				return 3;
			Base b = (Base) id;
			if (b.Do() != 1)
				return 4;
			return 0;
		}
	}
}
