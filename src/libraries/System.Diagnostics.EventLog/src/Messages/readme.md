These files are used to produce an Event Message File.

For more information see https://docs.microsoft.com/en-us/windows/win32/eventlog/message-files.

The design of the EventLog class is to allow for the registration of event sources without specifying message files.

In the case an event source does not specify it's own message file, EventLog just provides a default message file
with 64K message IDs all that just pass through the first insertion string.  This allow the event source to still
use IDs for messages, but doesn't require the caller to actually pass a message file in order to achieve this.

The process for producing the message file requires mc.exe and rc.exe which do not work cross-platform, and they
require a VS install with C++ tools.  Since these files rarely (if ever) change, we just use a manual process for
updating this res file.

To update the checked in files, manually run generateEventLogMessagesRes.cmd from a Developer Command Prompt.