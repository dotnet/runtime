#Tests for table global invariants

typedef-global-props {
	assembly assembly-with-types.exe

	#set type row 4 to the same name of row 3
	invalid offset table-row ( 2 4 ) + 4 set-uint
		read.uint (table-row ( 2 3 ) + 4)
}
