
opcode.def: make-opcodes-def.pl cil-opcodes.xml
	perl make-opcodes-def.pl < $(srcdir)/cil-opcodes.xml > opcode.def

defdir = $(includedir)/mono/cil
def_DATA = opcode.def

xmldir = $(share)/mono/cil
xml_DATA = cil-opcodes.xml

EXTRA_DIST=opcode.def $(xml_DATA) $(def_DATA)