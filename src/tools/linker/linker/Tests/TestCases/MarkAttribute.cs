using System;

namespace TestCases
{
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Field)]
	public class MarkAttribute : Attribute
	{
	}
}