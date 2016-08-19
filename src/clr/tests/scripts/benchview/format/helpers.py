from benchview.utils.common import is_null_or_whitespace
from benchview.utils.common import is_number

from datetime import datetime
from os import path

import json

class JsonFormatSerializer(json.JSONEncoder):
    def default(self, obj):
        if hasattr(obj, '__dict__'):
            return obj.__dict__
        return json.JSONEncoder.default(self, obj)

def to_json_string(obj):
    return json.dumps(obj, cls = JsonFormatSerializer, sort_keys = True)

def write_object_as_json(fileName: str, obj):
    if obj is None:
        raise ValueError('Attempting to write None as serialized json.')

    with open(fileName, mode = 'w') as jsonfile:
        jsonfile.write(to_json_string(obj))

def get_timestamp_format() -> str:
    return '%Y-%m-%dT%H:%M:%SZ'

def is_valid_name(name: str) -> bool:
    return isinstance(name, str) and not is_null_or_whitespace(name)

def is_valid_description(description: str) -> bool:
    return description is None or (isinstance(description, str) and not is_null_or_whitespace(description))

def is_valid_datetime(dt: str) -> bool:
    try:
        datetime.strptime(dt, get_timestamp_format())
        return True
    except ValueError:
        return False
