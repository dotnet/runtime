// This blog post originally appeared on David Broman's blog on 10/13/2005

#include "SigParse.cpp"

 // ---------------------------------------------------------------------
 // ---------------------------------------------------------------------
 // This file demonstrates how to use the general-purpose parser (SigParser) by
 // deriving a new class from it and overriding the virtuals.
 //
 // In this case we're simply printing the notifications to stdout as we receive
 // them, using pretty indenting.
 //
 // Look at PlugInToYourProfiler.cpp to see how to drive this.
 // ---------------------------------------------------------------------
 // ---------------------------------------------------------------------


 #define dimensionof(a) (sizeof(a)/sizeof(*(a)))
 #define MAKE_CASE(__elt) case __elt: return #__elt;
 #define MAKE_CASE_OR(__elt) case __elt: return #__elt "|";

class SigFormat : public SigParser
{
private:
	UINT nIndentLevel;

public:
	SigFormat() {nIndentLevel = 0; }
	UINT GetIndentLevel() { return nIndentLevel;}

protected:
	LPCSTR SigIndexTypeToString(sig_index_type sit)
	{
		switch(sit)
		{
			default:
			DebugBreak();
			return "unknown index type";
			MAKE_CASE(SIG_INDEX_TYPE_TYPEDEF)
			MAKE_CASE(SIG_INDEX_TYPE_TYPEREF)
			MAKE_CASE(SIG_INDEX_TYPE_TYPESPEC)
		}
	}

	LPCSTR SigMemberTypeOptionToString(sig_elem_type set)
	{
		switch(set & 0xf0)
		{
			default:
			DebugBreak();
			return "unknown element type";
			case 0:
			return "";

			MAKE_CASE_OR(SIG_GENERIC)
			MAKE_CASE_OR(SIG_HASTHIS)
			MAKE_CASE_OR(SIG_EXPLICITTHIS)
		}
	}

	LPCSTR SigMemberTypeToString(sig_elem_type set)
	{
		switch(set & 0xf)
		{
			default:
			DebugBreak();
			return "unknown element type";
			MAKE_CASE(SIG_METHOD_DEFAULT)
			MAKE_CASE(SIG_METHOD_C)
			MAKE_CASE(SIG_METHOD_STDCALL)
			MAKE_CASE(SIG_METHOD_THISCALL)
			MAKE_CASE(SIG_METHOD_FASTCALL)
			MAKE_CASE(SIG_METHOD_VARARG)
			MAKE_CASE(SIG_FIELD)
			MAKE_CASE(SIG_LOCAL_SIG)
			MAKE_CASE(SIG_PROPERTY)
		}
	}

	LPCSTR SigElementTypeToString(sig_elem_type set)
	{
		switch(set)
		{
			default:
			DebugBreak();
			return "unknown element type";
			MAKE_CASE(ELEMENT_TYPE_END)
			MAKE_CASE(ELEMENT_TYPE_VOID)
			MAKE_CASE(ELEMENT_TYPE_BOOLEAN)
			MAKE_CASE(ELEMENT_TYPE_CHAR)
			MAKE_CASE(ELEMENT_TYPE_I1)
			MAKE_CASE(ELEMENT_TYPE_U1)
			MAKE_CASE(ELEMENT_TYPE_I2)
			MAKE_CASE(ELEMENT_TYPE_U2)
			MAKE_CASE(ELEMENT_TYPE_I4)
			MAKE_CASE(ELEMENT_TYPE_U4)
			MAKE_CASE(ELEMENT_TYPE_I8)
			MAKE_CASE(ELEMENT_TYPE_U8)
			MAKE_CASE(ELEMENT_TYPE_R4)
			MAKE_CASE(ELEMENT_TYPE_R8)
			MAKE_CASE(ELEMENT_TYPE_STRING)
			MAKE_CASE(ELEMENT_TYPE_PTR)
			MAKE_CASE(ELEMENT_TYPE_BYREF)
			MAKE_CASE(ELEMENT_TYPE_VALUETYPE)
			MAKE_CASE(ELEMENT_TYPE_CLASS)
			MAKE_CASE(ELEMENT_TYPE_VAR)
			MAKE_CASE(ELEMENT_TYPE_ARRAY)
			MAKE_CASE(ELEMENT_TYPE_GENERICINST)
			MAKE_CASE(ELEMENT_TYPE_TYPEDBYREF)
			MAKE_CASE(ELEMENT_TYPE_I)
			MAKE_CASE(ELEMENT_TYPE_U)
			MAKE_CASE(ELEMENT_TYPE_FNPTR)
			MAKE_CASE(ELEMENT_TYPE_OBJECT)
			MAKE_CASE(ELEMENT_TYPE_SZARRAY)
			MAKE_CASE(ELEMENT_TYPE_MVAR)
			MAKE_CASE(ELEMENT_TYPE_CMOD_REQD)
			MAKE_CASE(ELEMENT_TYPE_CMOD_OPT)
			MAKE_CASE(ELEMENT_TYPE_INTERNAL)
			MAKE_CASE(ELEMENT_TYPE_MODIFIER)
			MAKE_CASE(ELEMENT_TYPE_SENTINEL)
			MAKE_CASE(ELEMENT_TYPE_PINNED)
		}
	}

	void PrintIndent()
	{
		const char k_szSpaces[] = " ";

		// You should probably assert or throw an exception if nIndentLevel
		// is bigger than dimensionof(k_szSpaces)-1. Error handling is minimized
		// in this sample for better readability.

		printf(k_szSpaces + ((dimensionof(k_szSpaces)-1) - nIndentLevel));
	}

	void IncIndent()
	{
		nIndentLevel += 2;
	}

	void DecIndent()
	{
		nIndentLevel -= 2;
	}

 	// Simple wrapper around printf that prints the indenting spaces for you
	void Print(const char* format, ...)
	{
		va_list argList;
		va_start(argList, format);
		PrintIndent();
		vprintf(format, argList);
	}

 	// a method with given elem_type
	virtual void NotifyBeginMethod(sig_elem_type elem_type)
	{
		Print("BEGIN METHOD\n");
		IncIndent();
	}

	virtual void NotifyEndMethod()
	{
		DecIndent();
		Print("END METHOD\n");
	}

 	// total parameters for the method
	virtual void NotifyParamCount(sig_count count)
	{
		Print("Param count = '%d'\n", count);
	}

 	// starting a return type
	virtual void NotifyBeginRetType()
	{
		Print("BEGIN RET TYPE\n");
		IncIndent();
	}
	virtual void NotifyEndRetType()
	{
		DecIndent();
		Print("END RET TYPE\n");
	}

 	// starting a parameter
	virtual void NotifyBeginParam()
	{
		Print("BEGIN PARAM\n");
		IncIndent();
	}

	virtual void NotifyEndParam()
	{
		DecIndent();
		Print("END PARAM\n");
	}

 	// sentinel indication the location of the "..." in the method signature
	virtual void NotifySentinel()
	{
		Print("...\n");
	}

 	// number of generic parameters in this method signature (if any)
	virtual void NotifyGenericParamCount(sig_count count)
	{
		Print("Generic param count = '%d'\n", count);
	}

	//----------------------------------------------------

 	// a field with given elem_type
	virtual void NotifyBeginField(sig_elem_type elem_type)
	{
		Print("BEGIN FIELD: '%s%s'\n", SigMemberTypeOptionToString(elem_type), SigMemberTypeToString(elem_type));
		IncIndent();
	}

	virtual void NotifyEndField()
	{
		DecIndent();
		Print("END FIELD\n");
	}

	//----------------------------------------------------

	// a block of locals with given elem_type (always just LOCAL_SIG for now)
	virtual void NotifyBeginLocals(sig_elem_type elem_type)
	{
		Print("BEGIN LOCALS: '%s%s'\n", SigMemberTypeOptionToString(elem_type), SigMemberTypeToString(elem_type));
		IncIndent();
	}

	virtual void NotifyEndLocals()
	{
		DecIndent();
		Print("END LOCALS\n");
	}


 	// count of locals with a block
	virtual void NotifyLocalsCount(sig_count count)
	{
		Print("Locals count: '%d'\n", count);
	}

 	// starting a new local within a local block
	virtual void NotifyBeginLocal()
	{
		Print("BEGIN LOCAL\n");
		IncIndent();
	}

	virtual void NotifyEndLocal()
	{
		DecIndent();
		Print("END LOCAL\n");
	}


 	// the only constraint available to locals at the moment is ELEMENT_TYPE_PINNED
	virtual void NotifyConstraint(sig_elem_type elem_type)
	{
		Print("Constraint: '%s%s'\n", SigMemberTypeOptionToString(elem_type), SigMemberTypeToString(elem_type));
	}


	//----------------------------------------------------

	// a property with given element type
	virtual void NotifyBeginProperty(sig_elem_type elem_type)
	{
		Print("BEGIN PROPERTY: '%s%s'\n", SigMemberTypeOptionToString(elem_type), SigMemberTypeToString(elem_type));
		IncIndent();
	}

	virtual void NotifyEndProperty()
	{
		DecIndent();
		Print("END PROPERTY\n");
	}


	//----------------------------------------------------

	// starting array shape information for array types
	virtual void NotifyBeginArrayShape()
	{
		Print("BEGIN ARRAY SHAPE\n");
		IncIndent();
	}

	virtual void NotifyEndArrayShape()
	{
		DecIndent();
		Print("END ARRAY SHAPE\n");
	}


 	// array rank (total number of dimensions)
	virtual void NotifyRank(sig_count count)
	{
		Print("Rank: '%d'\n", count);
	}

 	// number of dimensions with specified sizes followed by the size of each
	virtual void NotifyNumSizes(sig_count count)
	{
		Print("Num Sizes: '%d'\n", count);
	}

	virtual void NotifySize(sig_count count)
	{
		Print("Size: '%d'\n", count);
	}

	// BUG BUG lower bounds can be negative, how can this be encoded?
	// number of dimensions with specified lower bounds followed by lower bound of each
	virtual void NotifyNumLoBounds(sig_count count)
	{
		Print("Num Low Bounds: '%d'\n", count);
	}

	virtual void NotifyLoBound(sig_count count)
	{
		Print("Low Bound: '%d'\n", count);
	}

	//----------------------------------------------------


	// starting a normal type (occurs in many contexts such as param, field, local, etc)
	virtual void NotifyBeginType()
	{
		Print("BEGIN TYPE\n");
		IncIndent();
	}

	virtual void NotifyEndType()
	{
		DecIndent();
		Print("END TYPE\n");
	}

	virtual void NotifyTypedByref()
	{
		Print("Typed byref\n");
	}

	// the type has the 'byref' modifier on it -- this normally proceeds the type definition in the context
	// the type is used, so for instance a parameter might have the byref modifier on it
	// so this happens before the BeginType in that context
	virtual void NotifyByref()
	{
		Print("Byref\n");
	}

 	// the type is "VOID" (this has limited uses, function returns and void pointer)
	virtual void NotifyVoid()
	{
		Print("Void\n");
	}

 	// the type has the indicated custom modifiers (which can be optional or required)
	virtual void NotifyCustomMod(sig_elem_type cmod, sig_index_type indexType, sig_index index)
	{
		Print(
			"Custom modifiers: '%s', index type: '%s', index: '0x%x'\n",
			SigElementTypeToString(cmod),
			SigIndexTypeToString(indexType),
			index);
	}

	// the type is a simple type, the elem_type defines it fully
	virtual void NotifyTypeSimple(sig_elem_type elem_type)
	{
		Print("Type simple: '%s'\n", SigElementTypeToString(elem_type));
	}

	// the type is specified by the given index of the given index type (normally a type index in the type metadata)
	// this callback is normally qualified by other ones such as NotifyTypeClass or NotifyTypeValueType
	virtual void NotifyTypeDefOrRef(sig_index_type indexType, int index)
	{
		Print("Type def or ref: '%s', index: '0x%x'\n", SigIndexTypeToString(indexType), index);
	}

	// the type is an instance of a generic
	// elem_type indicates value_type or class
	// indexType and index indicate the metadata for the type in question
	// number indicates the number of type specifications for the generic types that will follow
	virtual void NotifyTypeGenericInst(sig_elem_type elem_type, sig_index_type indexType, sig_index index, sig_mem_number number)
	{
		Print(
			"Type generic instance: '%s', index type: '%s', index: '0x%x', member number: '%d'\n",
			SigElementTypeToString(elem_type),
			SigIndexTypeToString(indexType),
			index,
			number);
	}

	// the type is the type of the nth generic type parameter for the class
	virtual void NotifyTypeGenericTypeVariable(sig_mem_number number)
	{
		Print("Type generic type variable: number: '%d'\n", number);
	}

	// the type is the type of the nth generic type parameter for the member
	virtual void NotifyTypeGenericMemberVariable(sig_mem_number number)
	{
		Print("Type generic member variable: number: '%d'\n", number);
	}

	// the type will be a value type
	virtual void NotifyTypeValueType()
	{
		Print("Type value type\n");
	}

	// the type will be a class
	virtual void NotifyTypeClass()
	{
		Print("Type class\n");
	}

 	// the type is a pointer to a type (nested type notifications follow)
	virtual void NotifyTypePointer()
	{
		Print("Type pointer\n");
	}

 	// the type is a function pointer, followed by the type of the function
	virtual void NotifyTypeFunctionPointer()
	{
		Print("Type function pointer\n");
	}

 	// the type is an array, this is followed by the array shape, see above, as well as modifiers and element type
	virtual void NotifyTypeArray()
	{
		Print("Type array\n");
	}

 	// the type is a simple zero-based array, this has no shape but does have custom modifiers and element type
	virtual void NotifyTypeSzArray()
	{
		Print("Type sz array\n");
	}
};
