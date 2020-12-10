#if INCLUDE_MOCK_CORELIB

using System;

[assembly: MockCorelibAttributeToRemove]

namespace System
{
	public class MockCorelibAttributeToRemove : Attribute
	{
	}
}

#endif