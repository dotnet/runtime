#Tests for table global invariants

typedef-global-props {
	assembly assembly-with-types.exe

	#set type row 4 to the same name of row 3
	invalid offset table-row ( 2 4 ) + 4 set-ushort
		read.ushort (table-row ( 2 3 ) + 4)
}

typeref-global-props {
	assembly assembly-with-types.exe

	#set typeref row 3 to the same of row 2
	invalid offset table-row ( 1 2 ) + 0 set-ushort read.ushort (table-row ( 1 1 ) + 0), #scope
			offset table-row ( 1 2 ) + 2 set-ushort read.ushort (table-row ( 1 1 ) + 2), #name
			offset table-row ( 1 2 ) + 4 set-ushort read.ushort (table-row ( 1 1 ) + 4)  #namespace
}

fielddef-global-props {
	assembly assembly-with-fields.exe

	#two fields with same name,owner and signature
	badrt offset table-row (4 1) + 2 set-ushort read.ushort (table-row (4 0) + 2), #name
		  offset table-row (4 1) + 4 set-ushort read.ushort (table-row (4 0) + 4)  #signature

}