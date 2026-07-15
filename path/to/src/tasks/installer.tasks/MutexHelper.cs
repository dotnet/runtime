# Implement a helper class to handle Mutex releases correctly
from typing import Optional
import threading

class MutexHelper:
    def __init__(self):
        self.mutex = threading.Lock()

    def acquire_mutex(self) -> bool:
        try:
            self.mutex.acquire()
            return True
        except Exception as e:
            print(f"Error acquiring mutex: {e}")
            return False

    def release_mutex(self) -> bool:
        try:
            self.mutex.release()
            return True
        except Exception as e:
            print(f"Error releasing mutex: {e}")
            return False