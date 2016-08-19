def __get_formatted_time() -> str:
    """The a common/cross-platform formatted time string."""
    import time
    date_time = '[{DateTime}]'.format(DateTime = time.strftime('%Y-%m-%d %H:%M'))
    return date_time

def __print_color(color: str, type: str, message: str):
    """Writes to the standard output the specified message."""
    #print(color + __get_formatted_time() + type + message + '\033[0m')
    print(__get_formatted_time() + type + message)

def debug(message: str):
    if __debug__:
        __print_color('\033[36;43;1m'   , '[DEBUG] ', str(message))

def error(message: str):
    __print_color('\033[31;1m'      , '[ERROR] ', str(message))

def info(message: str):
    __print_color('\033[37;1m'      , '[INFO ] ', str(message))

def warning(message: str):
    __print_color('\033[33;1m'      , '[WARN ] ', str(message))
