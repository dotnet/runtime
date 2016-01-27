; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
; 

; 
; ==--==
	.386
	.model	flat

	option	casemap:none
	public	_DoubleToNumber,_NumberToDouble

; NUMBER structure

nPrecision	equ	(dword ptr 0)
nScale		equ	(dword ptr 4)
nSign		equ	(dword ptr 8)
nDigits		equ	(word ptr 12)

	.code

; Powers of 10 from 1.0E1 to 1.0E15 increasing by 1

Pow10By1	label	tbyte

	dt	1.0E1
	dt	1.0E2
	dt	1.0E3
	dt	1.0E4
	dt	1.0E5
	dt	1.0E6
	dt	1.0E7
	dt	1.0E8
	dt	1.0E9
	dt	1.0E10
	dt	1.0E11
	dt	1.0E12
	dt	1.0E13
	dt	1.0E14
	dt	1.0E15

; Powers of 10 from 1.0E16 to 1.0E336 increasing by 16

Pow10By16	label	tbyte

	dt	1.0E16
	dt	1.0E32
	dt	1.0E48
	dt	1.0E64
	dt	1.0E80
	dt	1.0E96
	dt	1.0E112
	dt	1.0E128
	dt	1.0E144
	dt	1.0E160
	dt	1.0E176
	dt	1.0E192
	dt	1.0E208
	dt	1.0E224
	dt	1.0E240
	dt	1.0E256
	dt	1.0E272
	dt	1.0E288
	dt	1.0E304
	dt	1.0E320
	dt	1.0E336

; Single precision constants

Single10	dd	10.0
SingleINF	dd	7F800000H

g_CwStd		dw	137fH		;Mask all errors, 64-bit, round near

; void _cdecl DoubleToNumber(double value, int precision, NUMBER* number)

_DoubleToNumber		proc

value		equ	(qword ptr [ebp+8])
precision	equ	(dword ptr [ebp+16])
number		equ	(dword ptr [ebp+20])
paramSize	=	16

cwsave		equ (word ptr [ebp-24])
digits		equ	(tbyte ptr [ebp-20])
temp		equ	(tbyte ptr [ebp-10])
localSize	=	24

	push	ebp
	mov	ebp,esp
	sub	esp,localSize
	push	edi
	push	ebx
	fnstcw  cwsave
	fldcw g_CwStd
	fld	value
	fstp	temp
	mov	edi,number
	mov	eax,precision
	mov	nPrecision[edi],eax
	movzx	eax,word ptr temp[8]
	mov	edx,eax
	shr	edx,15
	mov	nSign[edi],edx
	and	eax,7FFFH
	je	DN1
	cmp	eax,7FFFH
	jne	DN10
	mov	eax,80000000H
	cmp	dword ptr temp[4],eax
	jne	DN1
	cmp	dword ptr temp[0],0
	jne	DN1
	dec	eax
DN1:	mov	nScale[edi],eax
	mov	nDigits[edi],0
	jmp	DN30
DN10:	fld	value
	sub	eax,16382+58		;Remove bias and 58 bits
	imul	eax,19728		;log10(2) * 2^16 = .30103 * 65536
	add	eax,0FFFFH		;Round up
	sar	eax,16			;Only use high half
	lea	edx,[eax+18]
	mov	nScale[edi],edx
 	neg	eax
	call	ScaleByPow10
	fbstp	digits
	xor	eax,eax
	xor	ebx,ebx
	mov	ecx,precision
	inc	ecx
	mov	edx,8
	mov	al,byte ptr digits[8]
	test	al,0F0H
	jne	DN11
	dec	nScale[edi]
	jmp	DN12
DN11:	shr	al,4
	dec	ecx
	je	DN20
	add	al,'0'
	mov	nDigits[edi+ebx*2],ax
	inc	ebx
	mov	al,byte ptr digits[edx]
DN12:	and	al,0FH
	dec	ecx
	je	DN20
	add	al,'0'
	mov	nDigits[edi+ebx*2],ax
	inc	ebx
	dec	edx
	jl  DN22					; We've run out of digits & don't have a rounding digit, so we'll skip the rounding step.
	mov	al,byte ptr digits[edx]
	jmp	DN11
DN20:	cmp	al,5
	jb	DN22
DN21:	dec	ebx
	inc	nDigits[edi+ebx*2]
	cmp	nDigits[edi+ebx*2],'9'
	jbe	DN23
	or	ebx,ebx
	jne	DN21
	mov	nDigits[edi+ebx*2],'1'
	inc	nScale[edi]
	jmp	DN23
DN22:	dec	ebx
	cmp	nDigits[edi+ebx*2],'0'
	je	DN22
DN23:	mov	nDigits[edi+ebx*2+2],0
DN30:
	fldcw	cwsave			;;Restore original CW
	pop	ebx
	pop	edi
	mov	esp,ebp
	pop	ebp
	ret	;made _cdecl for WinCE paramSize

_DoubleToNumber		endp

; void _cdecl NumberToDouble(NUMBER* number, double* value)
_NumberToDouble		proc

number		equ	(dword ptr [ebp+8])
value		equ	(dword ptr [ebp+12])
paramSize	=	8

cwsave		equ (word  ptr [ebp-8])
temp		equ	(dword ptr [ebp-4])
localSize	=	8

	push	ebp     
	mov	ebp,esp					; Save the stack ptr
	sub	esp,localSize			;
	fnstcw  cwsave
	fldcw g_CwStd		
	fldz						; zero the register
	mov	ecx,number				; move precision into ecx
	xor	edx,edx					; clear edx
	cmp	dx,nDigits[ecx]			; if the first digit is 0 goto SignResult
	je	SignResult
	mov	eax,nScale[ecx]			; store the scale in eax
	cmp	eax,-330				; if the scale is less than or equal to -330 goto Cleanup
	jle	Cleanup
	cmp	eax,310					; if the scale is less than 310, goto ParseDigits
	jl	ParseDigits
	fstp	st(0)				; store value on the top of the floating point stack
	fld	SingleINF				; Load infinity
	jmp	SignResult				; Goto SignResult
ParseDigits:	
	movzx	eax,nDigits[ecx+edx*2]; load the character at nDigits[edx];
	sub	eax,'0'					; subtract '0'
	jc	ScaleResult				; jump to ScaleResult if this produces a negative value
	mov	temp,eax				; store the first digit in temp
	fmul	Single10			; Multiply by 10
	fiadd	temp				; Add the digit which we just found
	inc	edx						; increment the counter
	cmp	edx,18					; if (eax<18) goto ParseDigits
	jb	ParseDigits
ScaleResult:	
	mov	eax,nScale[ecx]			; eax = scale
	sub	eax,edx					; scale -= (number of digits)
	call	ScaleByPow10		; multiply the result by 10^scale
SignResult:	
	cmp	nSign[ecx],0			; If the sign is 0 already go to Cleanup, otherwise change the sign.
	je	Cleanup
	fchs
Cleanup:	
	mov	edx,value				; store value in edx
	fstp	qword ptr [edx]		; copy from value to the fp stack
	fldcw	cwsave				; Restore original CW		
	mov	esp,ebp					; restore the stack frame & exit.
	pop	ebp
	ret	;Made _cdecl for WinCE  paramSize

_NumberToDouble		endp

; Scale st(0) by 10^eax
		
ScaleByPow10	proc
	test	eax,eax
	je	SP2
	jl	SP3
	mov	edx,eax
	and	edx,0FH
	je	SP1
	lea	edx,[edx+edx*4]
	fld	Pow10By1[edx*2-10]
	fmul
SP1:	mov	edx,eax
	shr	edx,4
        test    edx, edx                ; remove partial flag stall caused by shr
	je	SP2
	lea	edx,[edx+edx*4]
	fld	Pow10By16[edx*2-10]
	fmul
SP2:	ret
SP3:	neg	eax
	mov	edx,eax
	and	edx,0FH
	je	SP4
	lea	edx,[edx+edx*4]
	fld	Pow10By1[edx*2-10]
	fdiv
SP4:	mov	edx,eax
	shr	edx,4
        test    edx, edx                ; remove partial flag stall caused by shr
	je	SP5
	lea	edx,[edx+edx*4]
	fld	Pow10By16[edx*2-10]
	fdiv
SP5:	ret
ScaleByPow10	endp
		
	end
