Note that the AO_stack implementation is licensed under the GPL,
unlike the lower level routines.

The header file atomic_ops_stack.h defines a linked stack abstraction.
Stacks may be accessed by multiple concurrent threads.  The implementation
is 1-lock-free, i.e. it will continue to make progress if at most one
thread becomes inactive while operating on the data structure.

(The implementation can be built to be N-lock-free for any given N.  But that
seems to rarely be useful, especially since larger N involve some slowdown.)

This makes it safe to access these data structures from non-reentrant
signal handlers, provided at most one non-signal-handler thread is
accessing the data structure at once.  This latter condition can be
ensured by acquiring an ordinary lock around the non-hndler accesses
to the data structure.

For details see:

Hans-J. Boehm, "An Almost Non-Blocking Stack", PODC 2004,
http://portal.acm.org/citation.cfm?doid=1011767.1011774, or
http://www.hpl.hp.com/techreports/2004/HPL-2004-105.html
(This is not exactly the implementation described there, since the
interface was cleaned up in the interim.  But it should perform
very similarly.)

We use a fully lock-free implementation when the underlying hardware
makes that less expensive, i.e. when we have a double-wide compare-and-swap
operation available.  (The fully lock-free implementation uses an AO_t-
sized version count, and assumes it does not wrap during the time any
given operation is active.  This seems reasonably safe on 32-bit hardware,
and very safe on 64-bit hardware.) If a fully lock-free implementation
is used, the macro AO_STACK_IS_LOCK_FREE will be defined.

The implementation is interesting only because it allows reuse of
existing nodes.  This is necessary, for example, to implement a memory
allocator.

Since we want to leave the precise stack node type up to the client,
we insist only that each stack node contains a link field of type AO_t.
When a new node is pushed on the stack, the push operation expects to be
passed the pointer to this link field, which will then be overwritten by
this link field.  Similarly, the pop operation returns a pointer to the
link field of the object that previously was on the top of the stack.

The cleanest way to use these routines is probably to define the stack node
type with an initial AO_t link field, so that the conversion between the
link-field pointer and the stack element pointer is just a compile-time
cast.  But other possibilities exist.  (This would be cleaner in C++ with
templates.)

A stack is represented by an AO_stack_t structure.  (This is normally
2 or 3 times the size of a pointer.)  It may be statically initialized
by setting it to AO_STACK_INITIALIZER, or dynamically initialized to
an empty stack with AO_stack_init.  There are only three operations for
accessing stacks:

void AO_stack_init(AO_stack_t *list);
void AO_stack_push_release(AO_stack_t *list, AO_t *new_element);
AO_t * AO_stack_pop_acquire(volatile AO_stack_t *list);

We require that the objects pushed as list elements remain addressable
as long as any push or pop operation are in progress.  (It is OK for an object
to be "pop"ped off a stack and "deallocated" with a concurrent "pop" on
the same stack still in progress, but only if "deallocation" leaves the
object addressable.  The second "pop" may still read the object, but
the value it reads will not matter.)

We require that the headers (AO_stack objects) remain allocated and
valid as long as any operations on them are still in-flight.

We also provide macros AO_REAL_HEAD_PTR that converts an AO_stack_t
to a pointer to the link field in the next element, and AO_REAL_NEXT_PTR
that converts a link field to a real, dereferencable, pointer to the link field
in the next element.  This is intended only for debugging, or to traverse
the list after modification has ceased.  There is otherwise no guarantee that
walking a stack using this macro will produce any kind of consistent
picture of the data structure.
