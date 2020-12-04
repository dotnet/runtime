// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class MaskedTextProviderTests
    {
        private const string TestSimpleDigitMask = "0-00-0";
        private const string TestComplexDigitMask = "$000,000.00";
        private const string TestComplexDateTimeMask = "00/00/0000 00:00:00";
        private const string TestToUpperCaseMask = ">??|??";
        private const string TestToLowerCaseMask = "<??|??";
        private const string TestAlphanumericMask = "Aa";
        private const string TestLetterMask = "L&?";
        private const string TestDigitMask = "#09";
        private const string TestMaskResultReplaceChar = "2-2_-_";
        private const string TestMaskResultReplaceString = "2-3_-_";
        private const int TestLCID = 1033;
        private const char TestPromptChar = '-';
        private const char TestPasswordChar = '&';

        [Fact]
        public void Ctor_Mask()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Same(TestSimpleDigitMask, maskedTextProvider.Mask);
            Assert.Equal(TestSimpleDigitMask.Length, maskedTextProvider.Length);
        }

        [Fact]
        public void Ctor_Mask_RestrictToAscii()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask, true);
            Assert.Same(TestSimpleDigitMask, maskedTextProvider.Mask);
            Assert.True(maskedTextProvider.AsciiOnly);
        }

        [Fact]
        public void Ctor_Mask_Culture()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask, new CultureInfo("en-US"));
            Assert.Same(TestSimpleDigitMask, maskedTextProvider.Mask);
            Assert.Equal(TestLCID, maskedTextProvider.Culture.LCID);
        }

        [Fact]
        public void Ctor_Mask_Culture__RestrictToAscii()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask, new CultureInfo("en-US"), true);
            Assert.Same(TestSimpleDigitMask, maskedTextProvider.Mask);
            Assert.Equal(TestLCID, maskedTextProvider.Culture.LCID);
            Assert.True(maskedTextProvider.AsciiOnly);
        }

        [Fact]
        public void Ctor_Mask_PasswordChar_AllowPromptAsInput()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask, TestPasswordChar, false);
            Assert.Same(TestSimpleDigitMask, maskedTextProvider.Mask);
            Assert.Equal(TestPasswordChar, maskedTextProvider.PasswordChar);
            Assert.False(maskedTextProvider.AllowPromptAsInput);
        }

        [Fact]
        public void Ctor_Mask_Culture_PasswordChar_AllowPromptAsInput()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask, new CultureInfo("en-US"), TestPasswordChar, false);
            Assert.Same(TestSimpleDigitMask, maskedTextProvider.Mask);
            Assert.Equal(TestLCID, maskedTextProvider.Culture.LCID);
            Assert.Equal(TestPasswordChar, maskedTextProvider.PasswordChar);
            Assert.False(maskedTextProvider.AllowPromptAsInput);
        }

        [Fact]
        public void Ctor_Mask_Culture_AllowPromptAsInput_PromptChar_PasswordChar_RestrictToAscii()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask, new CultureInfo("en-US"), false, TestPromptChar, TestPasswordChar, false);
            Assert.Same(TestSimpleDigitMask, maskedTextProvider.Mask);
            Assert.Equal(TestLCID, maskedTextProvider.Culture.LCID);
            Assert.False(maskedTextProvider.AllowPromptAsInput);
            Assert.Equal(TestPromptChar, maskedTextProvider.PromptChar);
            Assert.Equal(TestPasswordChar, maskedTextProvider.PasswordChar);
            Assert.False(maskedTextProvider.AllowPromptAsInput);
        }

        [Fact]
        public void Clone()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            var maskedTextProviderClone = maskedTextProvider.Clone();

            Assert.Equal(maskedTextProvider.ToString(), maskedTextProviderClone.ToString());
        }

        [Fact]
        public void EditPositionCount()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Equal(4, maskedTextProvider.EditPositionCount);
            Assert.Equal(4, maskedTextProvider.AvailableEditPositionCount);
            Assert.Equal(0, maskedTextProvider.AssignedEditPositionCount);
        }

        [Fact]
        public void EditPosition()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            var positions = maskedTextProvider.EditPositions;
            Assert.NotNull(positions);
            Assert.Equal(4, positions.Cast<int>().ToEnumerable().Count());
        }

        [Fact]
        public void Add_Char_Input_True()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.Add('1'));
        }

        [Fact]
        public void Add_Char_Input_False()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.False(maskedTextProvider.Add('T'));
        }

        [Fact]
        public void Add_String_Input_True()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.Add("11"));
        }

        [Fact]
        public void Add_String_Input_False()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.False(maskedTextProvider.Add("TT"));
        }

        [Fact]
        public void Clear()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            maskedTextProvider.Add("12");
            Assert.Equal(2, maskedTextProvider.AssignedEditPositionCount);

            maskedTextProvider.Clear();
            Assert.Equal(0, maskedTextProvider.AssignedEditPositionCount);
        }

        [Fact]
        public void Clear_MaskedTextResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            maskedTextProvider.Clear(out MaskedTextResultHint resultHint);
            Assert.Equal(MaskedTextResultHint.NoEffect, resultHint);

            maskedTextProvider.Add("12");
            Assert.Equal(2, maskedTextProvider.AssignedEditPositionCount);

            maskedTextProvider.Clear(out resultHint);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
            Assert.Equal(0, maskedTextProvider.AssignedEditPositionCount);
        }

        [Fact]
        public void FindAssignedEditPositionFrom()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Equal(-1, maskedTextProvider.FindAssignedEditPositionFrom(0, true));

            maskedTextProvider.Add("12");
            Assert.Equal(0, maskedTextProvider.FindAssignedEditPositionFrom(0, true));
            Assert.Equal(2, maskedTextProvider.FindAssignedEditPositionFrom(5, false));
        }

        [Fact]
        public void FindAssignedEditPositionInRange()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Equal(-1, maskedTextProvider.FindAssignedEditPositionInRange(0, 1, true));

            maskedTextProvider.Add("12");
            Assert.Equal(0, maskedTextProvider.FindAssignedEditPositionInRange(0, 1, true));
            Assert.Equal(2, maskedTextProvider.FindAssignedEditPositionInRange(0, 5, false));
        }

        [Fact]
        public void FindEditPositionFrom()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Equal(0, maskedTextProvider.FindEditPositionFrom(0, true));
            Assert.Equal(5, maskedTextProvider.FindEditPositionFrom(5, false));
        }

        [Fact]
        public void FindEditPositionInRange()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Equal(0, maskedTextProvider.FindEditPositionInRange(0, 1, true));
            Assert.Equal(5, maskedTextProvider.FindEditPositionInRange(0, 5, false));
        }

        [Fact]
        public void FindNonEditPositionFrom()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Equal(1, maskedTextProvider.FindNonEditPositionFrom(0, true));
            Assert.Equal(4, maskedTextProvider.FindNonEditPositionFrom(5, false));
        }

        [Fact]
        public void FindNonEditPositionInRange()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.Equal(1, maskedTextProvider.FindNonEditPositionInRange(0, 2, true));
            Assert.Equal(4, maskedTextProvider.FindNonEditPositionInRange(0, 5, false));
        }

        [Fact]
        public void FindUnassignedEditPositionFrom()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            maskedTextProvider.Set("12");
            Assert.Equal(3, maskedTextProvider.FindUnassignedEditPositionFrom(0, true));
            Assert.Equal(5, maskedTextProvider.FindUnassignedEditPositionFrom(5, false));
        }

        [Fact]
        public void FindUnassignedEditPositionInRange()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            maskedTextProvider.Set("12");
            Assert.Equal(3, maskedTextProvider.FindUnassignedEditPositionInRange(0, 5, true));
            Assert.Equal(5, maskedTextProvider.FindUnassignedEditPositionInRange(0, 5, false));
        }

        [Fact]
        public void GetOperationResultFromHint()
        {
            Assert.True(MaskedTextProvider.GetOperationResultFromHint(MaskedTextResultHint.Success));
            Assert.False(MaskedTextProvider.GetOperationResultFromHint(MaskedTextResultHint.PositionOutOfRange));
        }

        [Fact]
        public void InsertAt_Input_Position()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.False(maskedTextProvider.InsertAt('1', -1));
            Assert.False(maskedTextProvider.InsertAt('1', 6));
            Assert.True(maskedTextProvider.InsertAt('1', 0));
        }

        [Fact]
        public void InsertAt_Input_Position_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt(string.Empty, 0, out int testPosition, out var resultHint));
            Assert.Equal(MaskedTextResultHint.NoEffect, resultHint);

            Assert.True(maskedTextProvider.InsertAt('1', 0, out testPosition, out resultHint));
            Assert.Equal(0, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);

            Assert.False(maskedTextProvider.InsertAt('1', 6, out testPosition, out resultHint));
            Assert.Equal(6, testPosition);
            Assert.Equal(MaskedTextResultHint.PositionOutOfRange, resultHint);
        }

        [Fact]
        public void InsertAt_InputString_Position()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.False(maskedTextProvider.InsertAt("1", -1));
            Assert.False(maskedTextProvider.InsertAt("1", 6));
            Assert.True(maskedTextProvider.InsertAt("1", 0));
        }

        [Fact]
        public void InsertAt_InputString_Position_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt(string.Empty, 0, out int testPosition, out var resultHint));
            Assert.Equal(0, testPosition);
            Assert.Equal(MaskedTextResultHint.NoEffect, resultHint);

            Assert.True(maskedTextProvider.InsertAt("1", 0, out testPosition, out resultHint));
            Assert.Equal(0, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);

            Assert.False(maskedTextProvider.InsertAt("1", 6, out testPosition, out resultHint));
            Assert.Equal(6, testPosition);
            Assert.Equal(MaskedTextResultHint.PositionOutOfRange, resultHint);
        }

        [Fact]
        public void IsAvailablePosition()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.False(maskedTextProvider.IsAvailablePosition(6));
            Assert.True(maskedTextProvider.IsAvailablePosition(0));
            Assert.False(maskedTextProvider.IsAvailablePosition(1));
        }

        [Fact]
        public void IsEditPosition()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.False(maskedTextProvider.IsEditPosition(6));
            Assert.True(maskedTextProvider.IsEditPosition(0));
            Assert.False(maskedTextProvider.IsEditPosition(1));
        }

        [Fact]
        public void CharValidation()
        {
            Assert.True(MaskedTextProvider.IsValidInputChar('1'));
            Assert.True(MaskedTextProvider.IsValidMaskChar('M'));
            Assert.True(MaskedTextProvider.IsValidPasswordChar('\0'));
            Assert.False(MaskedTextProvider.IsValidInputChar('\n'));
        }

        [Fact]
        public void Remove()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("1", 0));
            Assert.Equal(1, maskedTextProvider.AssignedEditPositionCount);
            Assert.True(maskedTextProvider.Remove());
            Assert.Equal(0, maskedTextProvider.AssignedEditPositionCount);
        }

        [Fact]
        public void Remove_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("1", 0));
            Assert.Equal(1, maskedTextProvider.AssignedEditPositionCount);

            Assert.True(maskedTextProvider.Remove(out var testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
            Assert.Equal(0, testPosition);
        }

        [Fact]
        public void RemoveAt_Position()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));
            Assert.Equal(2, maskedTextProvider.AssignedEditPositionCount);

            Assert.True(maskedTextProvider.RemoveAt(0));
            Assert.Equal(1, maskedTextProvider.AssignedEditPositionCount);
        }

        [Fact]
        public void RemoveAt_StartPosition_EndPosition()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);

            Assert.True(maskedTextProvider.InsertAt("12", 0));
            Assert.Equal(2, maskedTextProvider.AssignedEditPositionCount);

            Assert.True(maskedTextProvider.RemoveAt(0,2));
            Assert.Equal(0, maskedTextProvider.AssignedEditPositionCount);
        }

        [Fact]
        public void RemoveAt_StartPosition_EndPosition_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));
            Assert.Equal(2, maskedTextProvider.AssignedEditPositionCount);

            Assert.False(maskedTextProvider.RemoveAt(0, 6, out int testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal(6, testPosition);
            Assert.Equal(MaskedTextResultHint.PositionOutOfRange, resultHint);

            Assert.False(maskedTextProvider.RemoveAt(-1, 4, out testPosition, out resultHint));
            Assert.Equal(-1, testPosition);
            Assert.Equal(MaskedTextResultHint.PositionOutOfRange, resultHint);

            Assert.True(maskedTextProvider.RemoveAt(0, 2, out testPosition, out resultHint));
            Assert.Equal(0, maskedTextProvider.AssignedEditPositionCount);
            Assert.Equal(0, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
        }

        [Fact]
        public void Replace_InputChar_Position()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));
            Assert.True(maskedTextProvider.Replace('2', 0));
            Assert.Equal(TestMaskResultReplaceChar, maskedTextProvider.ToDisplayString());
        }

        [Fact]
        public void Replace_InputChar_Position_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));
            
            Assert.True(maskedTextProvider.Replace('2', 0, out int testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal(TestMaskResultReplaceChar, maskedTextProvider.ToDisplayString());
            Assert.Equal(0, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
        }

        [Fact]
        public void Replace_InputChar_StartPosition_EndPosition_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));
            
            Assert.True(maskedTextProvider.Replace('2', 0, 5, out int testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal("2-__-_", maskedTextProvider.ToDisplayString());
            Assert.Equal(0, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
        }

        [Fact]
        public void Replace_InputString_Position()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));
            Assert.True(maskedTextProvider.Replace("23", 0));
            Assert.Equal(TestMaskResultReplaceString, maskedTextProvider.ToDisplayString());
        }

        [Fact]
        public void Replace_InputString_Position_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));
            
            Assert.True(maskedTextProvider.Replace("23", 0, out int testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal(TestMaskResultReplaceString, maskedTextProvider.ToDisplayString());
            Assert.Equal(2, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
        }

        [Fact]
        public void Replace_InputString_StartPosition_EndPosition_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.InsertAt("12", 0));

            Assert.False(maskedTextProvider.Replace("2", 0, 6, out int testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal(MaskedTextResultHint.PositionOutOfRange, resultHint);

            Assert.False(maskedTextProvider.Replace("2", -1, 5, out  testPosition, out resultHint));
            Assert.Equal(MaskedTextResultHint.PositionOutOfRange, resultHint);

            Assert.True(maskedTextProvider.Replace("2", 0, 5, out testPosition, out resultHint));
            Assert.Equal("2-__-_", maskedTextProvider.ToDisplayString());
            Assert.Equal(0, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
        }

        [Fact]
        public void Set_InputString()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.Set("1234"));
            Assert.Equal("1-23-4", maskedTextProvider.ToDisplayString());
        }

        [Fact]
        public void Set_InputString_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.Set("1234", out int testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal("1-23-4", maskedTextProvider.ToDisplayString());
            Assert.Equal(5, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
        }

        [Fact]
        public void ToStringTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            maskedTextProvider.PasswordChar = '*';

            Assert.True(maskedTextProvider.Set("1234"));
            Assert.Equal("1-23-4", maskedTextProvider.ToString());
            Assert.Equal("*-**-*", maskedTextProvider.ToString(false));
            Assert.Equal("1-2", maskedTextProvider.ToString(0,3));
            Assert.Equal("*-*", maskedTextProvider.ToString(false,0,3));
            Assert.Equal("*-**-*", maskedTextProvider.ToDisplayString());

            Assert.True(maskedTextProvider.Set("12"));
            Assert.Equal("1-2_-_", maskedTextProvider.ToString(true, true));
            Assert.Equal("2_-", maskedTextProvider.ToString(true, true, 2, 3));
            Assert.Equal("*_-", maskedTextProvider.ToString(false,true, true, 2, 3));
        }

        [Fact]
        public void VerifyChar()
        {
            var maskedTextProvider = new MaskedTextProvider(TestSimpleDigitMask);
            Assert.True(maskedTextProvider.VerifyChar('1', 0, out MaskedTextResultHint resultHint));
            Assert.Equal(MaskedTextResultHint.Success, resultHint);
            Assert.False(maskedTextProvider.VerifyChar('T', 0, out resultHint));
            Assert.Equal(MaskedTextResultHint.DigitExpected, resultHint);
        }

        [Fact]
        public void VerifyEscapeChar()
        {
            var maskedTextProvider = new MaskedTextProvider
                (TestSimpleDigitMask, '*', true) {ResetOnSpace = true};
            Assert.True(maskedTextProvider.VerifyEscapeChar(' ', 0));
            Assert.True(maskedTextProvider.VerifyEscapeChar('_', 0));
            Assert.False(maskedTextProvider.VerifyEscapeChar('T', 0));
        }

        [Fact]
        public void VerifyString_Input()
        {
            var maskedTextProvider = new MaskedTextProvider(TestComplexDigitMask, new CultureInfo("en-US"));
            Assert.True(maskedTextProvider.VerifyString("10000000"));
            Assert.False(maskedTextProvider.VerifyString("A"));
        }

        [Fact]
        public void VerifyString_Input_TestPosition_ResultHint()
        {
            var maskedTextProvider = new MaskedTextProvider(TestComplexDigitMask, new CultureInfo("en-US"));

            Assert.True(maskedTextProvider.VerifyString("10000000", out int testPosition, out MaskedTextResultHint resultHint));
            Assert.Equal(10, testPosition);
            Assert.Equal(MaskedTextResultHint.Success, resultHint);

            Assert.False(maskedTextProvider.VerifyString("A", out testPosition, out  resultHint));
            Assert.Equal(1, testPosition);
            Assert.Equal(MaskedTextResultHint.DigitExpected, resultHint);
        }

        [Fact]
        public void ComplexDigitMaskTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestComplexDigitMask, new CultureInfo("en-US"));
            maskedTextProvider.Set("10000000");
            Assert.Equal("$100,000.00", maskedTextProvider.ToString());
        }

        [Fact]
        public void DateTimeMaskTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestComplexDateTimeMask, new CultureInfo("en-US"));
            maskedTextProvider.Set("01012000101010");
            Assert.Equal("01/01/2000 10:10:10", maskedTextProvider.ToString());
        }

        [Fact]
        public void UpperCaseMaskTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestToUpperCaseMask);
            maskedTextProvider.Set("aaAA");
            Assert.Equal("AAAA", maskedTextProvider.ToString());
        }

        [Fact]
        public void LowerCaseMaskTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestToLowerCaseMask);
            maskedTextProvider.Set("AAaa");
            Assert.Equal("aaaa", maskedTextProvider.ToString());
        }

        [Fact]
        public void AlphanumericMaskTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestAlphanumericMask);
            maskedTextProvider.Set("Ab");
            Assert.Equal("Ab", maskedTextProvider.ToString());
        }

        [Fact]
        public void LetterMaskTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestLetterMask);
            maskedTextProvider.Set("Abc");
            Assert.Equal("Abc", maskedTextProvider.ToString());
        }

        [Fact]
        public void DigitMaskTest()
        {
            var maskedTextProvider = new MaskedTextProvider(TestDigitMask);
            maskedTextProvider.Set("+10");
            Assert.Equal("+10", maskedTextProvider.ToString());
        }
    }
}
