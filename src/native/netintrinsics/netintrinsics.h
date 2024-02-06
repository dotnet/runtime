// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef NETINTRINSICS_H
#define NETINTRINSICS_H

#ifdef __cplusplus
extern "C"
{
#endif
double netintrinsics_acos(double x);

double netintrinsics_asin(double x);

double netintrinsics_atan2(double y, double x);

double netintrinsics_exp(double x);

int netintrinsics_ilogb(double x);

double netintrinsics_log(double x);

double netintrinsics_log10(double x);

double netintrinsics_pow(double x, double y);

float netintrinsics_acosf(float x);

float netintrinsics_asinf(float x);

float netintrinsics_atan2f(float y, float x);

float netintrinsics_expf(float x);

int netintrinsics_ilogbf(float x);

float netintrinsics_logf(float x);

float netintrinsics_log10f(float x);

float netintrinsics_powf(float x, float y);
#ifdef __cplusplus
}
#endif

#endif // NETINTRINSICS_H