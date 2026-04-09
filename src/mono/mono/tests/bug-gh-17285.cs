using System;

namespace ConsoleApp2
{
    class Program
    {
        class C
        {
            public void f(int i1, int i2, int i3, int i4, int i5, int i6, int i7)
            {
                throw new Exception("exception from f()");
            }
        }

        // If this test succeeds, it should run to completion. If it fails,
        // mono's exception handler will restore an incorrect stack pointer
        // when executing the exception handler, which will cause the runtime
        // to later crash after returning to a bogus address.
        static void Main()
        {
            try
            {
                C c = new C();
                int i1 = 0, i2 = 0, i3 = 0, i4 = 0, i5 = 0, i6 = 0, i7 = 0;
                c.f(i1, i2, i3, i4, i5, i6, i7);
            }
            catch (Exception e)
            {
                Console.WriteLine("caught: " + e);
            }
        }
    }
}

/*
With X86CallFrameOptimization enabled, LLVM will generate something like the
following code for Main:

CFA: [0] def_cfa: %rsp+0x8
CFA: [0] offset: pc at cfa-0x8
CFA: [4] def_cfa_offset: 0x20
CFA: [50] def_cfa_offset: 0x28
CFA: [52] def_cfa_offset: 0x30
CFA: [59] def_cfa_offset: 0x20
LLVM Method void ConsoleApp2.Program:Main () emitted at 0x41a5ebd0 to 0x41a5ec75 (code length 165) [gh-issue-17285.exe]
JitInfo EH clause 0 flags 0 try 21-32 handler 6c-b6abe448
JitInfo EH clause 1 flags 0 try 3f-59 handler 6c-b6abe448


   0:	48 83 ec 18          	sub    $0x18,%rsp
   4:	48 b8 d8 c6 5b f8 b1 	movabs $0x55b1f85bc6d8,%rax
   b:	55 00 00 
   e:	48 8b 08             	mov    (%rax),%rcx
  11:	48 b8 20 ed d6 f6 b1 	movabs $0x55b1f6d6ed20,%rax
  18:	55 00 00 
  1b:	48 83 38 00          	cmpq   $0x0,(%rax)
  1f:	75 3d                	jne    5e <soleApp2_Program_Main___+0x5e>
  21:	48 bf 70 33 58 f8 b1 	movabs $0x55b1f8583370,%rdi
  28:	55 00 00 
  2b:	be 10 00 00 00       	mov    $0x10,%esi
  30:	ff d1                	callq  *%rcx
  32:	48 b9 e8 c6 5b f8 b1 	movabs $0x55b1f85bc6e8,%rcx
  39:	55 00 00 
  3c:	4c 8b 11             	mov    (%rcx),%r10
  3f:	31 f6                	xor    %esi,%esi
  41:	31 d2                	xor    %edx,%edx
  43:	31 c9                	xor    %ecx,%ecx
  45:	45 31 c0             	xor    %r8d,%r8d
  48:	45 31 c9             	xor    %r9d,%r9d
  4b:	48 89 c7             	mov    %rax,%rdi
  4e:	6a 00                	pushq  $0x0
  50:	6a 00                	pushq  $0x0
  52:	41 ff d2             	callq  *%r10
  55:	48 83 c4 10          	add    $0x10,%rsp
  59:	48 83 c4 18          	add    $0x18,%rsp
  5d:	c3                   	retq   
  5e:	48 b8 d0 c6 5b f8 b1 	movabs $0x55b1f85bc6d0,%rax
  65:	55 00 00 
  68:	ff 10                	callq  *(%rax)
  6a:	eb b5                	jmp    21 <soleApp2_Program_Main___+0x21>
  6c:	48 89 44 24 10       	mov    %rax,0x10(%rsp)
  71:	48 8b 44 24 10       	mov    0x10(%rsp),%rax
  76:	48 b8 f0 c6 5b f8 b1 	movabs $0x55b1f85bc6f0,%rax
  7d:	55 00 00 
  80:	ff 10                	callq  *(%rax)
  82:	48 89 44 24 08       	mov    %rax,0x8(%rsp)
  87:	48 83 7c 24 08 00    	cmpq   $0x0,0x8(%rsp)
  8d:	74 ca                	je     59 <soleApp2_Program_Main___+0x59>
  8f:	48 8b 7c 24 08       	mov    0x8(%rsp),%rdi
  94:	48 b8 f8 c6 5b f8 b1 	movabs $0x55b1f85bc6f8,%rax
  9b:	55 00 00 
  9e:	ff 10                	callq  *(%rax)
  a0:	48 83 c4 18          	add    $0x18,%rsp
  a4:	c3                   	retq   

The call to f happens at 0x52. The exception handler starts at 0x6c. Note that
there are two adjustments, totaling 0x28 bytes, made to the stack pointer made
on the normal return codepath; there's only one 0x18 byte adjustment done when
returning from the exception handler. The stack pointer that is recovered when
unwinding from f is offset 0x10 bytes from the value reserved in the function
prologue, and is used to store f's 7th and 8th parameters (remember, "this" is
parameter 1) and maintain 16-byte alignment. This parameter space is not
accounted for by the runtime's EH code when jumping back to the exception
handler, so the stack pointer remains 16 bytes away from the real start of the
call frame when Main returns.

Why 7 non-this parameters? The amd64 sysv ABI, which we use on non-Windows
targets, specifies that up to 6 integer parameters may be passed in GP
registers. When f takes 6 non-this parameters or less, the call to f in Main
happens to fit entirely in the prologue-reserved stack space, so no call-site
adjustment is made.

When X86CallFrameOptimization is disabled, all call parameter space is reserved
in the prologue, and there's no need to compensate for any ephemeral function
call stack space.
*/
