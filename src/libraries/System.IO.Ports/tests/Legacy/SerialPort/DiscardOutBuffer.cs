// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;
using System.IO;
using System.IO.PortsTests;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;

public class DiscardOutBuffer : PortsTest
{
    //The string used with Write(str) to fill the input buffer
    public static readonly string DEFAULT_STRING = "Hello World";

    //The buffer length used whe filling the output buffer
    // This was set to 8, but the TX Fifo on a UART can swallow that completely, so you can't then tell if the data has been sent or not.
    public static readonly int DEFAULT_BUFFER_LENGTH = 128;

    #region Test Cases

    [ActiveIssue(15752)]
    [ConditionalFact(nameof(HasOneSerialPort))]
    public void OutBufferFilled_Discard_Once()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        {
            Debug.WriteLine("Verifying Discard method after write buffer has been filled");
            com1.Open();
            com1.WriteTimeout = 500;
            com1.Handshake = Handshake.RequestToSend;

            Task task = Task.Run(() => WriteRndByteArray(com1, DEFAULT_BUFFER_LENGTH));

            WaitForTxBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

            VerifyDiscard(com1);

            // Wait for write method to fail with IOException
            Assert.Throws<AggregateException>(() => task.Wait(2000));
            Assert.IsType<IOException>(task.Exception.InnerException);
        }
    }

    [ActiveIssue(15752)]
    [ConditionalFact(nameof(HasOneSerialPort))]
    public void OutBufferFilled_Discard_Multiple()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        {
            Debug.WriteLine("Verifying call Discard method several times after output buffer has been filled");

            com1.Open();
            com1.WriteTimeout = 500;
            com1.Handshake = Handshake.RequestToSend;

            Task task = Task.Run(() => WriteRndByteArray(com1, DEFAULT_BUFFER_LENGTH));

            WaitForTxBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

            VerifyDiscard(com1);
            VerifyDiscard(com1);
            VerifyDiscard(com1);

            // Wait for write method to fail with IOException
            Assert.Throws<AggregateException>(() => task.Wait(2000));
            Assert.IsType<IOException>(task.Exception.InnerException);
        }
    }

    [ActiveIssue(15752)]
    [ConditionalFact(nameof(HasOneSerialPort))]
    public void OutBufferFilled_Discard_Cycle()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        {
            Debug.WriteLine(
                "Verifying call Discard method after input buffer has been filled discarded and filled again");

            com1.Open();
            com1.WriteTimeout = 500;
            com1.Handshake = Handshake.RequestToSend;

            Task task = Task.Run(() => WriteRndByteArray(com1, DEFAULT_BUFFER_LENGTH));

            WaitForTxBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

            VerifyDiscard(com1);

            // Wait for write method to fail with IOException
            Assert.Throws<AggregateException>(() => task.Wait(2000));
            Assert.IsType<IOException>(task.Exception.InnerException);

            Task task2 = Task.Run(() => WriteRndByteArray(com1, DEFAULT_BUFFER_LENGTH));

            WaitForTxBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

            VerifyDiscard(com1);

            // Wait for write method to fail with IOException
            Assert.Throws<AggregateException>(() => task2.Wait(2000));
            Assert.IsType<IOException>(task2.Exception.InnerException);
        }
    }

    [ConditionalFact(nameof(HasNullModem))]
    public void InAndOutBufferFilled_Discard()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            int origBytesToRead;

            Debug.WriteLine("Verifying Discard method after input buffer has been filled");

            com1.Open();
            com2.Open();
            com1.WriteTimeout = 500;

            com1.Handshake = Handshake.RequestToSend;
            com2.Write(DEFAULT_STRING);

            WaitForTxBufferToLoad(com2, DEFAULT_STRING.Length);

            Task task = Task.Run(() => WriteRndByteArray(com1, DEFAULT_BUFFER_LENGTH));
            origBytesToRead = com1.BytesToRead;

            WaitForTxBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

            VerifyDiscard(com1);

            Assert.Equal(origBytesToRead,com1.BytesToRead);

            // Wait for write method to fail with IOException
            Assert.Throws<AggregateException>(() => task.Wait(2000));
            Assert.IsType<IOException>(task.Exception.InnerException);
        }
    }

    private void WriteRndByteArray(SerialPort com, int byteLength)
    {
        byte[] buffer = new byte[byteLength];
        Random rndGen = new Random();

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)rndGen.Next(0, 256);
        }

        // This will abort with an IOException when the test calls DiscardOutBuffer
        // we will catch that exception on the Task.Wait()
        com.Write(buffer, 0, buffer.Length);
    }
    #endregion

    #region Verification for Test Cases
    private void VerifyDiscard(SerialPort com)
    {
        com.DiscardOutBuffer();
        Assert.Equal(0, com.BytesToWrite);
    }
    #endregion

    /// <summary>
    /// Wait for the write data to be written into a blocked (by adverse flow control) port
    /// </summary>
    private static void WaitForTxBufferToLoad(SerialPort com, int bufferLength)
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (com.BytesToWrite < bufferLength)
        {
            System.Threading.Thread.Sleep(50);
            if (sw.ElapsedMilliseconds > 3000)
            {
                Assert.True(false, "Timeout while waiting for data to be written to port");
            }
        }
    }
}