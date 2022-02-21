The dataflow analysis logic originates from IL Linker: http://github.com/mono/linker.

The purpose of this logic is to analyze dynamic behavior of the compiled code to make things like reflection work. This is done by analyzing the IL and reading dataflow annotations.

Let's try to keep this in sync. The ReferenceSource contains sources at the time of porting.

It should be updated whenever we take fixes from IL linker.