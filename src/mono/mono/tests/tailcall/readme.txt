--------------------------------------------------------------------

February 21 2018

Initial tests are imported from coreclr roughly via:

cd /dev2
git clone https://github.com/dotnet/coreclr # 66f840939e81a6e240a01edfea61976363bc51d6
cd /dev2/mono/mono/tests
mkdir -p tailcall/coreclr
cd tailcall/coreclr
cp -prv /dev2/coreclr/tests/src/* .
find . | grep proj$ | xargs rm
find . | grep -vi tail | xargs rm
a few times:
find . | xargs rmdir

and that this point optional:
	for a in `find . -type f`; do if  [ ! -e $(basename $a) ] ; then mv $a $(basename $a)  ; fi done
	a few:
	find . | grep -vi tail | xargs rmdir


	and then optionally pick up stragglers.

--------------------------------------------------------------------

February 21 2018
There a few buckets of known behavior in mono and .NET regarding tail calls.

Intuition is only partly correct.

Intuitively, a tail call can be performed when the outgoing parameters
match incoming parameters in "location" (specific register or stack location)
and count. Some wiggle room could be afforded for integer types -- signedess
would not matter, nor exact size, as long as fits in a regster.

Passing the address of a local would seem disallowed, however might be allowed
within the red zone? This is mentioned in ECMA.

Managed-pointer-ness would be helpful to match up, but this requirement
depends flexible details -- as long as GC info is accurate and/or GC is not
allowed while a location changes, ok.

Exception handling would likely get in the way -- you can't have a handler in scope
if your frame has been torn down, as exception handling needs a way
to know your scope is live -- either via RIP or a thread local linked list.
As well, this is mentioned in ECMA.

Mono does not tailcall virtual calls.
  No intuition assists me here.
  FIXME: Specific test cases that demonstrate this.

Mono does not tailcall "something about generics, generic value types, generic context".
  No intuition assists me here.
  FIXME: Specific test cases that demonstrate this.

.NET goes very far out of its way to allow the outgoing and incoming signatures
to vary arbitrarily, including growing the parameter list unboundedly.
This is very surprising. There is a specification (FIXME) to do this for CoreCLR/Unix,
however CoreCLR/Unix presently does not do this.

--------------------------------------------------------------------

February 21 2018

The imported CoreCLR tailcall tests have a variety of good and bad aspects.
 - Most are .il.
 - Some are .cs.
 - Some have explicit tail calls.
 - Some have non demarked but easily optimized calls.
 - Some can be run on Unix. Some cannot -- p/invoke to user32.dll.
 - P/invoke is a useful portably interesting variation, even if user32.dll is not portable.
 - Most can be built stand alone, but not all.
 - When executed:
    Some succeeded whether or not a tailcall optimization is done.
    Some run forever.
    Some run out of stack, at least in the absence of tailcall.
 - Some use generics, some do not -- at least the first F# test (citation needed).

These variations make for usual simple run/don't-run, or succeed/fail partitioning of the tests
not trivial.

Due to the fact that many are not runnable, it becomes desirable to
use mono --compile-all switch, which will not run the code, just JIT it.

However this switch skips generics.

Therefore, we must either fix this switch, and/or develop new runnable portable tests,
that clearly indicate their success or failure, with an exit code.

It is important to consider the CoreCLR tests as just a starting point, and not the
desired end state of tailcall tests.

--------------------------------------------------------------------

February 21 2018

An approximate test plan should be:
  generate a combinatorial series of tests with the following variables

 - tailcall in try and outside of try
	Inside try cannot be optimized.
 - tailcall in except and outside of except
	Inside except cannot be optimized?
 - tailcall with exactly matching signatures
	Exactly matching is easier. Non-matching varies in ease.
 - tailcall with non matching signatures, but for which all parameters are in registers
	Same registers is easy.
 - tailcall with all parameters in registers and tailcall using stack for registers
	All registers should be easy, no matter the precise signature.
	Same stack should be easy.
	Varying stack, bigger or smaller, is very difficult but not impossible.
 - tailcall with managed pointers and non-managed pointers (integers)
 - tailcall with integer and tailcall with float
 - tailcall where parameters includes a passthrough ref parameter
 - tailcall to same function and tailcall to other function
   - Same function is particularly trivial and less interesting.
 - Tailcall with:
	generic reference types
	non-generic reference types
	generic value types
	non-generic value types
	reference types
	non-reference typess -- integer, float, value
 	virtual and non-virtual
	static and non-static
	varargs and non-varargs (how to construct varargs?)
	p/invoke and non-p/invoke
	call to same assembly and call to outside assembly (ECMA discerns this)
