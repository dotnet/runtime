# Error handling and MonoError

## MonoError

MonoError is the latest attempt at cleaning up and sanitizing error handling in the runtime. This document highlights some of the design goals and decisions, the implementation and the migration strategy.

### Design goals

-   Replace the majority of the adhoc error handling subsystems present today in the runtime. Each one is broken in a subtle way, has slightly different semantics and error conversion between them is spot, at best.

-   Map well to the final destination of all runtime errors: managed exceptions. This includes being compatible with .net when it comes to the kind of exception produced by a given error condition.

-   Be explicit, lack any magic. The loader-error setup does control flow happens in the background through a TLS variable, which made it very brittle and error prone.

-   Explicit and multiple error scopes. Make it possible to have multiple error scopes and make them explicit. We need to support nested scopes during type loading, even if reporting is flat.

-   Be as simple as possible. Error handling is the hardest part of the runtime to test so it must be simple. Which means complex error reporting, such as chaining, is out of question.

## Current implementation

The current implementation exists in mono-error.h and mono-error-internals.h. The split is so API users can consume errors, but they are not supported to be able to produce them - such use case has yet to arise.

#### Writing a function that produces errors

``` c
/**
 *
 * @returns NULL on error
 */
void*
my_function (int a, MonoError *error)
{
    if (a <= 0) {//
        mono_error_set_argument (error, "a", "argument a must be bigger than zero, it was %d", a);
        return NULL;
    }
    return malloc (a);
}
```

Important points from the above:

-   Add a "MonoError \*error" argument as the last to your function
-   Call one of the mono_error_set functions based on what managed exception this should produce and the available information
-   Document that a NULL returns means an error

## Writing a function that consumes errors

``` c
void
other_function (void)
{
    ERROR_DECL (error);
    void *res;

    res = my_function (10, error);
    //handling the error:
    //1st option: set the pending exception.  Only safe to do in icalls
    if (mono_error_set_pending_exception (error)) //returns TRUE if an exception was set
        return;

    //2nd option: legacy code that can't handle failures:
    mono_error_assert_ok (error);

    //3rd option (deprecated): raise an exception and write a FIXME note
    //  (implicit cleanup, no-op if there was no error)
    mono_error_raise_exception (error); /* FIXME don't raise here */

    //4th option: ignore
    mono_error_cleanup (error);
}
```

Important points from the above:

-   Use `ERROR_DECL (error)` to declare and initialize a `MonoError *error` variable. (Under the hood, it declares a local `MonoError error_value` using `ERROR_DECL_VALUE (error_value)`. You may use `ERROR_DECL_VALUE (e)` to declare a variable local variable yourself. It's pretty unusual to need to do that, however.)
-   Pass it to the required function and always do something with the result
-   Given we're still transitioning, not all code can handle in the same ways

## Handling the transition

The transition work is not complete and we're doing it piece-by-piece to ensure we don't introduce massive regressions in the runtime. The idea is to move the least amount of code a time to use the new error machinery.

Here are the rules for code conversion:

-   Mono API functions that need to call functions which take a MonoError should assert on failure or cleanup the error as there's no adequate alternative at this point. They **must not** use `mono_error_raise_exception` or `mono_error_set_pending_exception`

-   When possible, change the function signature. If not, add a \_checked variant and add the `MONO_RT_EXTERNAL_ONLY` to the non-checked version if it's in the Mono API. That symbol will prevent the rest of the Mono runtime from calling the non-checked version.

## Advanced technique: using a local error to raise a different exception

Suppose you want to call a function `foo_checked()` but you want to raise a different exception if it fails. In this case, it makes sense to create a local error variable to handle the call to `foo_checked`:

``` c
int
my_function (MonoObject *arg, MonoError *error)
{
    ERROR_DECL (local_error);
    int result = foo_checked (arg, local_error);
    if (!is_ok (local_error)) {
        mono_error_set_execution_engine (error, "Could not successfully call foo_checked, due to: %s", mono_error_get_message (local_error));
        mono_error_cleanup (local_error);
    }
    return result;
```

-   Pass `local_error` to `foo_checked`
-   Check the result and if it wasn't okay, set a different error code on `error` It is common to use `mono_error_get_message` to include the message from the local failure as part of the new exception
-   Cleanup `local_error` to release its resources

## Advanced technique: MonoErrorBoxed and mono_class_set_failure

Normally we store a `MonoError` on the stack. The usual scenario is that managed code calls into the runtime, we perform some operations, and then we either return a result or convert a `MonoError` into a pending exception. So a stack lifetime for a `MonoError` makes sense.

There is one scenario where we need a heap-allocated `MonoError` whose lifetime is tied to a `MonoImage`: the initialization of a managed class. `MonoErrorBoxed` is a thin wrapper around a `MonoError` that identifies a `MonoError` that is allocated in the mempool of a `MonoImage`. It is created using `mono_error_box()` and converted back to an ordinary `MonoError` using `mono_error_unbox()`.

``` c
static int
some_class_init_helper (MonoClass *k)
{
    if (mono_class_has_failure (k))
        return -1; /* Already a failure, don't bother trying to init it */
    ERROR_DECL (local_error);
    int result = foo_checked (k, local_error);
    if (!is_ok (error)) {
      mono_class_set_failure (k, mono_error_box (local_error, k->image));
      mono_error_cleanup (local_error);
    }
    return result;
}
```

-   Check whether the class is already marked as a failure
-   Pass a `local_error` to `foo_checked`
-   Check the result and if it wasn't okay, allocate a boxed `MonoError` in the mempool of the class's image
-   Mark the class that failed with the boxed error
-   Cleanup the `local_error` to release its resources

### Design issues

-   Memory management of the error setting functions is not consistent or clear
-   Use a static initializer in the declaration site instead of mono_error_init?
-   Force an error to always be set or only when there's an exception situation? I.E. mono_class_from_name failing to find the class X finding the class but it failed to load.
-   g_assert (mono_errork_ok (&error)) could be replaced by a macro that uses g_error so we can see the error contents on crashes.
