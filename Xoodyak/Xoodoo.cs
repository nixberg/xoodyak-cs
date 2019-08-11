using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Xoodyak.Tests")]
namespace Xoodyak
{
    class Xoodoo
    {
        internal uint[] state = new uint[12];

        private static readonly uint[] RoundConstants = {
            0x058, 0x038, 0x3c0, 0x0d0,
            0x120, 0x014, 0x060, 0x02c,
            0x380, 0x0f0, 0x1a0, 0x012
        };

        internal byte this[long index]
        {
            get
            {
#if BIGENDIAN
                index += 3 - 2 * (index % 4);
#endif
                unsafe
                {
                    fixed (uint* pointer = state)
                    {
                        return ((byte*)pointer)[index];
                    }
                }
            }
        }

        internal void XOR(long index, byte value)
        {
#if BIGENDIAN
                index += 3 - 2 * (index % 4);
#endif
            unsafe
            {
                fixed (uint* pointer = state)
                {
                    ((byte*)pointer)[index] ^= value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Rotate(uint v, int n)
        {
            return (v >> n) | (v << (32 - n));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Swap(int i, int j)
        {
            uint temp = state[i];
            state[i] = state[j];
            state[j] = temp;
        }

        internal void Permute()
        {
            foreach (uint roundConstant in RoundConstants)
            {
                var e = new uint[4];

                for (int i = 0; i < 4; i++)
                {
                    e[i] = Rotate(state[i] ^ state[i + 4] ^ state[i + 8], 18);
                    e[i] ^= Rotate(e[i], 9);
                }

                for (int i = 0; i < 12; i++)
                {
                    state[i] ^= e[(i - 1) & 3];
                }

                Swap(7, 4);
                Swap(7, 5);
                Swap(7, 6);
                state[0] ^= roundConstant;

                for (int i = 0; i < 4; i++)
                {
                    var a = state[i];
                    var b = state[i + 4];
                    var c = Rotate(state[i + 8], 21);

                    state[i + 8] = Rotate((b & ~a) ^ c, 24);
                    state[i + 4] = Rotate((a & ~c) ^ b, 31);
                    state[i] ^= c & ~b;
                }

                Swap(8, 10);
                Swap(9, 11);
            }
        }
    }
}
