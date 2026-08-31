import subprocess as sub
import sys

commands = {}

def typeof (typ):
	return "typeof (" + typ + ")\n"

def inst (one, two):
	return "TypeBucket.Foo <TypeBucket." + one + ", TypeBucket." + two + ">.CallMe ();\n"

def bucketCall (filePrefix):
	return "TypeBucket." + filePrefix + ".CallMe ();\n"

def classDef (name):
	return "public struct " + name + " {\t\n public int inner; \t\n}\n"

def run (cmd):
	print(cmd)
	child = sub.Popen (cmd, shell=True)
	child.wait()
	error = child.returncode
	if error != 0:
		raise Exception ("Compilation error " + str(error))

def typeBucketInst (types, filePrefix):
	accum = "\npublic class " + filePrefix + "{\n"
	accum += "\tpublic static void CallMe () {\n"
	for t1 in types:
		for t2 in types:
			accum += inst (t1, t2)
	accum += "\t\n}\n}\n"
	return accum

def makeGenericDef (fileName):
	fileTemplate = """
using System.Runtime.CompilerServices;

namespace TypeBucket {
	public class Foo <S, P> {
		public static void CallMe () {
			A ();
			B ();
			C ();
			D ();
			E ();
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void A () {
		}
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void B () {
		}
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void C () {
		}
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void D () {
		}
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void E () {
		}
	}
}

"""
	csName = fileName + ".cs"
	with open(csName, 'w') as f:
		f.write (fileTemplate);
	cmd = commands ["mcs"] + " -t:library " + csName
	run (cmd)
	
def makeFile (prefix, insts, files, genericDefDll):
	types = []
	fileName = prefix + "TypeBucket"
	csName = fileName + ".cs"
	templatePrefix = "using TypeBucket;\n\tnamespace TypeBucket {\n"
	with open(csName, 'w') as f:
		f.write (templatePrefix)
		for i in range(100):
			name = "classy" + prefix + str(i)
			f.write (classDef (name))
			types.append (name)
		f.write(typeBucketInst (types, fileName))
		f.write ("\n}\n")

	insts.append(bucketCall (fileName))

	cmd = commands ["mcs"] + " -t:library " + csName + " -r:" + genericDefDll
	run (cmd)

	files.append (fileName)


template_head = """
using TypeBucket;

class MainClass {
	public static void Main () {
		"""

template_tail = """
	}
}
"""

def main (bin_prefix):
	commands ["mono"] = "MONO_PATH=. " + bin_prefix + "/bin/mono "
	commands ["mcs"] = "MONO_PATH=. " + bin_prefix + "/bin/mcs "

	generic_def_file = "Foo"
	makeGenericDef (generic_def_file)

	insts = []
	files = [generic_def_file]
	for prefix in ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"]:
		makeFile (prefix, insts, files, generic_def_file + ".dll")

	template = template_head + "\n\t".join(insts) + template_tail

	f = open("Hello.cs", 'w')
	f.write (template)
	f.close ();

	cmd = commands ["mcs"] + " Hello.cs"
	for f in files:
		cmd = cmd + " -r:" + f + ".dll"
	run (cmd)


	run (commands ["mono"] + "--aot=full " + "Hello.exe")

	files.append ("mscorlib")
	for f in files:
		run (commands ["mono"] + "--aot=full " +  f + ".dll")

if __name__ == "__main__":
	main (sys.argv [1])
