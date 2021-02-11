// An assembly that refereces the TestingReferenceAssembly

class Z : X {
	public Z () {
		Y = 1;
	}
}

class HasFieldFromReferenceAssembly {
	public X Fld;
}
