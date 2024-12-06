// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot.android.crypto;

import java.nio.ByteBuffer;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import javax.crypto.Mac;
import javax.crypto.ShortBufferException;
import javax.crypto.spec.SecretKeySpec;

public final class PalPbkdf2 {
    private static final int ERROR_UNSUPPORTED_ALGORITHM = -1;
    private static final int SUCCESS = 1;

    public static int pbkdf2OneShot(String algorithmName, byte[] password, ByteBuffer salt, int iterations, ByteBuffer destination)
        throws ShortBufferException, InvalidKeyException, IllegalArgumentException {
        // salt and destination are DirectByteBuffers that point to memory created by .NET.
        // These must not be touched after this method returns.

        // We do not ever expect a ShortBufferException to ever get thrown since the buffer destination length is always
        // checked. Let it go through the checked exception and JNI will handle it as a generic failure.
        // InvalidKeyException should not throw except the the case of an empty key, which we already handle. Let JNI
        // handle it as a generic failure.

        // We use a custom implementation of PBKDF2 instead of the one provided by the Android platform for two reasons:
        // The first is that Android only added support for PBKDF2 + SHA-2 family of agorithms in API level 26, and we
        // need to support SHA-2 prior to that.
        // The second is that PBEKeySpec only supports char-based passwords, whereas .NET supports arbitrary byte keys.

        if (algorithmName == null || password == null || destination == null) {
            // These are essentially asserts since the .NET side should have already validated these.
            throw new IllegalArgumentException("algorithmName, password, and destination must not be null.");
        }
        // The .NET side already validates the hash algorithm name inputs.
        String javaAlgorithmName = "Hmac" + algorithmName;
        Mac mac;

        try {
            mac = Mac.getInstance(javaAlgorithmName);
        }
        catch (NoSuchAlgorithmException nsae) {
            return ERROR_UNSUPPORTED_ALGORITHM;
        }

        if (password.length == 0) {
            // SecretKeySpec does not permit empty keys. Since HMAC just zero extends the key, a single zero byte key is
            // the same as an empty key.
            password = new byte[] { 0 };
        }

        // Since the salt needs to be read for each block, mark its current position before entering the loop.
        if (salt != null) {
            salt.mark();
        }

        SecretKeySpec key = new SecretKeySpec(password, javaAlgorithmName);
        mac.init(key);

        // Since this is a one-shot, it should not be possible to exceed the extract limit since the .NET side is
        // limited to the length of a span (2^31 - 1 bytes). It would only take ~128 million SHA-1 blocks to fill an entire
        // span, and 128 million fits in a signed 32-bit integer.
        int blockCounter = 1;
        int destinationOffset = 0;
        byte[] blockCounterBuffer = new byte[4]; // Big-endian 32-bit integer
        byte[] block = new byte[mac.getMacLength()];
        byte[] u = new byte[block.length];

        while (destinationOffset < destination.capacity()) {
            writeBigEndianInt(blockCounter, blockCounterBuffer);

            if (salt != null) {
                mac.update(salt);
                salt.reset(); // Resets it back to the previous mark. It does not consume the mark, so we don't need to mark again.
            }

            mac.update(blockCounterBuffer);
            mac.doFinal(u, 0);

            System.arraycopy(u, 0, block, 0, block.length);

            // Start at 2 since we did the first iteration above.
            for (int i = 2; i <= iterations; i++) {
                mac.update(u);
                mac.doFinal(u, 0);

                for (int j = 0; j < u.length; j++) {
                    block[j] ^= u[j];
                }
            }

            destination.put(block, 0, Math.min(block.length, destination.capacity() - destinationOffset));
            destinationOffset += block.length;
            blockCounter++;
        }

        return SUCCESS;
    }

    private static void writeBigEndianInt(int value, byte[] destination) {
        destination[0] = (byte)((value >> 24) & 0xFF);
        destination[1] = (byte)((value >> 16) & 0xFF);
        destination[2] = (byte)((value >> 8) & 0xFF);
        destination[3] = (byte)(value & 0xFF);
    }
}
