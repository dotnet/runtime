##
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
##

from abc import ABC, ABCMeta, abstractmethod

from utils import next_power_of_2


class BitonicISA(ABC, metaclass=ABCMeta):

    @abstractmethod
    def vector_size(self):
        pass

    @abstractmethod
    def max_bitonic_sort_vectors(self):
        pass

    def largest_merge_variant_needed(self):
        return next_power_of_2(self.max_bitonic_sort_vectors()) / 2;

    @abstractmethod
    def vector_size(self):
        pass

    @abstractmethod
    def vector_type(self):
        pass

    @classmethod
    @abstractmethod
    def supported_types(cls):
        pass

    @abstractmethod
    def generate_prologue(self, f):
        pass

    @abstractmethod
    def generate_epilogue(self, f):
        pass


    @abstractmethod
    def generate_1v_basic_sorters(self, f, ascending):
        pass

    @abstractmethod
    def generate_1v_merge_sorters(self, f, ascending):
        pass

    def generate_1v_sorters(self, f, ascending):
        self.generate_1v_basic_sorters(f, ascending)
        self.generate_1v_merge_sorters(f, ascending)

    @abstractmethod
    def generate_compounded_sorter(self, f, width, ascending, inline):
        pass

    @abstractmethod
    def generate_compounded_merger(self, f, width, ascending, inline):
        pass

    @abstractmethod
    def generate_entry_points(self, f):
        pass

    @abstractmethod
    def generate_master_entry_point(self, f):
        pass
