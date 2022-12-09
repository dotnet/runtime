#pragma once

using namespace System;

namespace TestLibrary {
	public ref class TestClass
	{
	public:
		static String^ GetString()
		{
			return "GetString";
		}

		static int GetInteger()
		{
			return 42;
		}

		[System::Diagnostics::CodeAnalysis::RequiresUnreferencedCodeAttribute("Warn from C++/CLI")]
		static void TriggerWarningFromCppCLI()
		{
		}
	};
}
