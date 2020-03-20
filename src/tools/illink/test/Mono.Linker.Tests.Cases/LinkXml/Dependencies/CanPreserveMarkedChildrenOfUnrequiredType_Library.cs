using System;

namespace Mono.Linker.Tests.Cases.LinkXml.Dependencies
{
	public class CanPreserveMarkedChildrenOfUnrequiredType_Library
	{
		public int Field1;
		
		public int Field2;
		
		public void Method1 () { }
		
		public void Method2 () { }
		
		public int Property1 {
			get {
				return Field1;
			}
			set {
				Field1 = value;
			}
		}
		
		public int Property2 {
			get {
				return Field2;
			}
			set {
				Field2 = value;
			}
		}
	}
}
