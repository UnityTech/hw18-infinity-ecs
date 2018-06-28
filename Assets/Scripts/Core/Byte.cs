using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    [System.Serializable]
    public struct byte4
    {
        public byte x;
        public byte y;
        public byte z;
        public byte w;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte4(byte val) { x = y = z = w = val; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte4(byte _x, byte _y, byte _z, byte _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte4(int4 val)
        {
            x = (byte)val.x;
            y = (byte)val.y;
            z = (byte)val.z;
            w = (byte)val.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte4(byte d) { return new byte4(d); }

        public override string ToString()
        {
            return string.Format("byte4({0}, {1}, {2}, {3})", x, y, z, w);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("float4({0}, {1}, {2}, {3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider), w.ToString(format, formatProvider));
        }

    }
}