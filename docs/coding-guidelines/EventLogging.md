# CoreClr Event Logging Design

## Introduction

Event Logging is a mechanism by which CoreClr can provide a variety of information on it's state. This Logging works by inserting explicit logging calls by the developer within the VM . The Event Logging mechanism is largely based on [ETW- Event Tracing For Windows](https://msdn.microsoft.com/en-us/library/windows/desktop/bb968803(v=vs.85).aspx)

# Adding Events to the Runtime

- Edit the [Event manifest](../../src/coreclr/vm/ClrEtwAll.man) to add a new event. For guidelines on adding new events, take a  look at the existing events in the manifest and this guide for [ETW Manifests](https://msdn.microsoft.com/en-us/library/dd996930%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396).
- The build system should automatically generate the required artifacts for the added events.
- Add entries in the [exclusion list](../../src/coreclr/vm/ClrEtwAllMeta.lst) if necessary
- The Event Logging Mechanism provides the following two functions, which can be used within the VM:
	- **FireEtw**EventName, this is used to trigger the event
	- **EventEnabled**EventName, this is used to see if any consumer has subscribed to this event


# Adding New Logging System

Though the Event logging system was designed for ETW, the build system provides a mechanism, basically an [adapter script- genEventing.py](../../src/coreclr/scripts/genEventing.py) so that other Logging System can be added and used by CoreClr. An Example of such an extension for [LTTng logging system](https://lttng.org/) can be found in [genLttngProvider.py](../../src/coreclr/scripts/genLttngProvider.py )
