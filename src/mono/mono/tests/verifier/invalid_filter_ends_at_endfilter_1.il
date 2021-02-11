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
//we use here as it was not part of the filter body
		nop
		ldc.i4.0
		ret
        }
        {
                nop
                leave END
        }

END:
        ldc.i4.0
        ret
}

