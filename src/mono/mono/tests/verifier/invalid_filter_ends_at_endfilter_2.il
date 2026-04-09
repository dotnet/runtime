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
	.locals init (int32 val)

	ldloc.0
	brfalse INSIDE_FILTER

        .try 
        {
                nop
                leave END
        }
        filter
        {
                pop

                ldc.i4.0
                endfilter
//we try to jump from/to here as it was not part of the filter body
INSIDE_FILTER:
		nop
		nop
		br END
        }
        {
                nop

                leave END
        }

END:
        ldc.i4.0
        ret
}

