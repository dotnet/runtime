// invalid

.assembly 'invalid_filter_ends_at_endfilter'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
        .entrypoint
        .maxstack 8

//we try here to put the filter after the handler block and end it with an endfilter

TRY_BB:
	nop
	leave END
TRY_END_BB:


HANDLER_BB:
	nop
	leave END

END_HANDLER_BB:
	nop
	nop
	br END

FILTER_BB:
	pop
	ldc.i4.0
	endfilter

END:
        ldc.i4.0
        ret
.try TRY_BB to TRY_END_BB filter FILTER_BB handler HANDLER_BB to END_HANDLER_BB
}

