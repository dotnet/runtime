// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable 1591

namespace PInvokeTest
{
    
    public unsafe partial struct GameControllerButtonBind
    {
        public GameControllerButtonBind
        (
            GameControllerBindType? bindType = null,
            GameControllerButtonBindValue? value = null
        ) : this()
        {
            if (bindType is not null)
            {
                BindType = bindType.Value;
            }

            if (value is not null)
            {
                Value = value.Value;
            }
        }


        public GameControllerBindType BindType;

        public GameControllerButtonBindValue Value;
    }

    public enum GameControllerBindType : int
    {
        ControllerBindtypeNone = 0x0,
        ControllerBindtypeButton = 0x1,
        ControllerBindtypeAxis = 0x2,
        ControllerBindtypeHat = 0x3,
        None = 0x0,
        Button = 0x1,
        Axis = 0x2,
        Hat = 0x3,
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct GameControllerButtonBindValue
    {
        public GameControllerButtonBindValue
        (
            int? button = null,
            int? axis = null,
            GameControllerButtonBindValueHat? hat = null
        ) : this()
        {
            if (button is not null)
            {
                Button = button.Value;
            }

            if (axis is not null)
            {
                Axis = axis.Value;
            }

            if (hat is not null)
            {
                Hat = hat.Value;
            }
        }


        [FieldOffset(0)]
        public int Button;

        [FieldOffset(0)]
        public int Axis;

        [FieldOffset(0)]
        public GameControllerButtonBindValueHat Hat;
    }

    public unsafe partial struct GameControllerButtonBindValueHat
    {
        public GameControllerButtonBindValueHat
        (
            int? hat = null,
            int? hatMask = null
        ) : this()
        {
            if (hat is not null)
            {
                Hat = hat.Value;
            }

            if (hatMask is not null)
            {
                HatMask = hatMask.Value;
            }
        }


        public int Hat;

        public int HatMask;
    }
}