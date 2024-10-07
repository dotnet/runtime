# Contract ExecutionManager

This contract is for mapping a PC address to information about the
managed method corresponding to that address.


## APIs of contract

**TODO**

## Version 1

**TODO** Methods

### NibbleMap

Version 1 of this contract depends on a "nibble map" data structure
that allows mapping of a code address in a contiguous subsection of
the address space to the pointer to the start of that a code sequence.
It takes advantage of the fact that the code starts are aligned and
are spaced apart to represent their addresses as a 4-bit nibble value.

Given a contiguous region of memory in which we lay out a collection of non-overlapping code blocks that are
not too small (so that two adjacent ones aren't too close together) and  where the start of each code block is preceeded by a code header aligned on some power of 2,
we can break up the whole memory space into buckets of a fixed size (32-bytes in the current implementation), where
each bucket either has a code block header or not.
Thinking of each code block header address as a hex number, we can view it as: `[index, offset, zeros]`
where each index gives us a bucket and the offset gives us the position of the header within the bucket.
We encode each offset into a 4-bit nibble, reserving the special value 0 to mark the places in the map where a method doesn't start.

To find the start of a method given an address we first convert it into a bucket index (giving the map unit)
and an offset which we can then turn into the index of the nibble that covers that address.
If the nibble is non-zero, we have the start of a method and it is near the given address.
If the nibble is zero, we have to search backward first through the current map unit, and then through previous map
units until we find a non-zero nibble.

For example (all code addresses are relative to some unspecified base):

Suppose there is code starting at address 304 (0x130)

* Then the map index will be 304 / 32 = 9 and the byte offset will be 304 % 32 = 16
* Because addresses are 4-byte aligned, the nibble value will be 1 + 16 / 4 = 5  (we reserve 0 to mean no method).
* So the map unit containing index 9 will contain the value 0x5 << 24 (the map index 9 means we want the second nibble in the second map unit, and we number the nibbles starting from the most significant) , or 
0x05000000


Now suppose we do a lookup for address 306 (0x132)
* The map index will be 306 / 32 = 9 and the byte offset will be 306 % 32 = 18
* The nibble value will be 1 + 18 / 4 = 5
* To do the lookup, we will load the map unit with index 9 (so the second 32-bit unit in the map) and get the value 0x05000000
* We will then shift to focus on the nibble with map index 9 (which again has nibble shift 24), so
 the map unit will be 0x00000005 and we will get the nibble value 5.
* Therefore we know that there is a method start at map index 9, nibble value 5.
* The map index corresponds to an offset of 288 bytes and the nibble value 5 corresponds to an offset of (5 - 1) * 4 = 16 bytes
* So the method starts at offset 288 + 16 = 304, which is the address we were looking for.

Now suppose we do a lookup for address 302 (0x12E)

* The map index will be 302 / 32 = 9 and the byte offset will be 302 % 32 = 14
* The nibble value will be 1 + 14 / 4 = 4
* To do the lookup, we will load the map unit containing map index 9 and get the value 0x05000000
* We will then shift to focus on the nibble with map index 9 (which again has nibble shift 22), so we will get
  the nibble value 5.
* Therefore we know that there is a method start at map index 9, nibble value 5.
* But the address we're looking for is map index 9, nibble value 4.
* We know that methods can't start within 32-bytes of each other, so we know that the method we're looking for is not in the current nibble.
* We will then try to shift to the previous nibble in the map unit (0x00000005 >> 4 = 0x00000000)
* Therefore we know there is no method start at any map index in the current map unit.
* We will then align the map index to the start of the current map unit (map index 8) and move back to the previous map unit (map index 7)
* At that point, we scan backwards for a non-zero map unit and a non-zero nibble within the first non-zero map unit. Since there are none, we return null.
