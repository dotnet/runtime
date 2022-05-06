#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_STACK_CONTENTS_GETTER_SETTER
#include "ep-stack-contents.h"
#include "ep-rt.h"

/*
 * EventPipeStackContents.
 */

EventPipeStackContents *
ep_stack_contents_alloc (void)
{
	EventPipeStackContents *instance = ep_rt_object_alloc (EventPipeStackContents);
	ep_raise_error_if_nok (instance != NULL);
	ep_raise_error_if_nok (ep_stack_contents_init (instance) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ep_stack_contents_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

EventPipeStackContents *
ep_stack_contents_init (EventPipeStackContents *stack_contents)
{
	EP_ASSERT (stack_contents != NULL);

	ep_stack_contents_reset (stack_contents);
	return stack_contents;
}

void
ep_stack_contents_fini (EventPipeStackContents *stack_contents)
{
	;
}

void
ep_stack_contents_free (EventPipeStackContents *stack_contents)
{
	ep_return_void_if_nok (stack_contents != NULL);
	ep_stack_contents_fini (stack_contents);
	ep_rt_object_free (stack_contents);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_stack_contents;
const char quiet_linker_empty_file_warning_eventpipe_stack_contents = 0;
#endif
