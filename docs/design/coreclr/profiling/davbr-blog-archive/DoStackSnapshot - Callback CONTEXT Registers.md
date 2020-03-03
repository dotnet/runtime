*This blog post originally appeared on David Broman's blog on 10/24/2005*


In my initial [post](DoStackSnapshot - Exception Filters.md) about DoStackSnapshot, I touched on how and when your profiler can "fill in the holes" by walking the unmanaged parts of the stack itself.  Doing this requires that your profiler have access to a register context at the top of the unmanaged block that you can use to begin your walk.  So it's quite reasonable for you to ask, "What registers will be valid in the context I receive in my StackSnapshotCallback call?"

The quick answer is that **nonvolatile (i.e., preserved), integer registers** should be valid.  You don't really need many registers to walk the stack anyway.  Obviously, you want a good stack pointer and instruction pointer.  And hey, a frame pointer is handy when you come across an EBP-based frame in x86 (RBP on x64).  These are all included in the set, of course.  Specifically by architecture, you can trust these fields in your context:

x86: Edi, Esi, Ebx, Ebp, Esp, Eip  
x64: Rdi, Rsi, Rbx, Rbp, Rsp, Rip, R12:R15  
ia64: IntS0:IntS3, RsBSP, StIFS, RsPFS, IntSp, StIIP, StIPSR

 

