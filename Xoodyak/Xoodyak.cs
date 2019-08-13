using System;
using System.IO;

namespace Xoodyak
{
    public class Xoodyak
    {
        enum Flag : byte
        {
            Zero = 0x00,
            AbsorbKey = 0x02,
            Absorb = 0x03,
            Ratchet = 0x10,
            SqueezeKey = 0x20,
            Squeeze = 0x40,
            Crypt = 0x80,
        }

        enum Mode
        {
            Hash,
            Keyed,
        }

        struct Rates
        {
            internal const long Hash = 16;
            internal const long Input = 44;
            internal const long Output = 24;
            internal const long Ratchet = 16;

            internal long absorb;
            internal long squeeze;

            internal Rates(long absorb, long squeeze)
            {
                this.absorb = absorb;
                this.squeeze = squeeze;
            }
        }

        enum Phase
        {
            Up,
            Down,
        }

        Mode mode;
        Rates rates;
        Phase phase = Phase.Up;
        Xoodoo xoodoo = new Xoodoo();

        public Xoodyak()
        {
            mode = Mode.Hash;
            rates = new Rates(Rates.Hash, Rates.Hash);
        }

        public Xoodyak(byte[] key, byte[] id, byte[] counter)
        {
            id = id ?? new byte[0];
            counter = counter ?? new byte[0];

            if (key.Length + id.Length >= Rates.Input)
            {
                throw new ArgumentException("Key + ID too long");
            }

            mode = Mode.Keyed;
            rates = new Rates(Rates.Input, Rates.Output);

            var buffer = new MemoryStream(key.Length + id.Length + 1);
            buffer.Write(key, 0, key.Length);
            buffer.Write(id, 0, id.Length);
            buffer.WriteByte((byte)(id.Length));
            buffer.Position = 0;
            AbsorbAny(buffer, buffer.Length, rates.absorb, Flag.AbsorbKey);

            if (counter.Length > 0)
            {
                AbsorbAny(new MemoryStream(counter), counter.Length, 1, Flag.Zero);
            }
        }

        private void AbsorbAny(Stream input, long count, long rate, Flag downFlag)
        {
            var block = new byte[rate];

            do
            {
                var blockSize = Math.Min(count, rate);
                var bytesRead = input.Read(block, 0, (int)blockSize);
                if (blockSize != bytesRead)
                {
                    throw new ArgumentException("Invalid count", "count");
                }

                if (phase != Phase.Up)
                {
                    Up(null, 0, Flag.Zero);
                }
                Down(block, bytesRead, downFlag);
                downFlag = Flag.Zero;

                count -= bytesRead;

            } while (count > 0);
        }

        private void Crypt(Stream input, Stream output, long count, bool decrypt)
        {
            var inputBlock = new byte[Rates.Output];
            var outputBlock = new byte[Rates.Output];
            var flag = Flag.Crypt;

            do
            {
                var blockSize = Math.Min(count, Rates.Output);
                var bytesRead = input.Read(inputBlock, 0, (int)blockSize);
                if (blockSize != bytesRead)
                {
                    throw new ArgumentException("Invalid count", "count");
                }

                Up(null, 0, flag);
                flag = Flag.Zero;

                for (int i = 0; i < bytesRead; i++)
                {
                    outputBlock[i] = (byte)(inputBlock[i] ^ xoodoo[i]);
                }

                if (decrypt)
                {
                    Down(outputBlock, bytesRead, Flag.Zero);
                }
                else
                {
                    Down(inputBlock, bytesRead, Flag.Zero);
                }

                output.Write(outputBlock, 0, bytesRead);

                count -= bytesRead;

            } while (count > 0);
        }

        private void SqueezeAny(Stream output, long count, Flag upFlag)
        {
            var bytesToWrite = Math.Min(count, rates.squeeze);
            count -= bytesToWrite;

            Up(output, bytesToWrite, upFlag);

            while (count > 0)
            {
                bytesToWrite = Math.Min(count, rates.squeeze);
                count -= bytesToWrite;

                Down(null, 0, Flag.Zero);
                Up(output, bytesToWrite, Flag.Zero);
            }
        }

        private void Down(byte[] block, long count, Flag flag)
        {
            phase = Phase.Down;
            for (int i = 0; i < count; i++)
            {
                xoodoo[i] ^= block[i];
            }
            xoodoo[count] ^= 0x01;
            if (mode == Mode.Hash)
            {
                xoodoo[47] ^= (byte)((byte)flag & 0x01);
            }
            else
            {
                xoodoo[47] ^= (byte)flag;
            }
        }

        private void Up(Stream output, long count, Flag flag)
        {
            phase = Phase.Up;
            if (mode != Mode.Hash)
            {
                xoodoo[47] ^= (byte)flag;
            }
            xoodoo.Permute();
            for (long i = 0; i < count; i++)
            {
                output.WriteByte(xoodoo[i]);
            }
        }

        public void Absorb(Stream input, long count)
        {
            AbsorbAny(input, count, rates.absorb, Flag.Absorb);
        }

        public void Encrypt(Stream plaintext, Stream ciphertext, long count)
        {
            if (mode != Mode.Keyed)
            {
                throw new InvalidOperationException("Xoodyak not in keyed mode");
            }
            Crypt(plaintext, ciphertext, count, false);
        }

        public void Decrypt(Stream ciphertext, Stream plaintext, long count)
        {
            if (mode != Mode.Keyed)
            {
                throw new InvalidOperationException("Xoodyak not in keyed mode");
            }
            Crypt(ciphertext, plaintext, count, true);
        }

        public void Squeeze(Stream output, long count)
        {
            SqueezeAny(output, count, Flag.Squeeze);
        }

        public void SqueezeKey(Stream output, long count)
        {
            if (mode != Mode.Keyed)
            {
                throw new InvalidOperationException("Xoodyak not in keyed mode");
            }
            SqueezeAny(output, count, Flag.SqueezeKey);
        }

        public void Ratchet()
        {
            if (mode != Mode.Keyed)
            {
                throw new InvalidOperationException("Xoodyak not in keyed mode");
            }
            var buffer = new MemoryStream((int)Rates.Ratchet);
            SqueezeAny(buffer, Rates.Ratchet, Flag.Ratchet);
            AbsorbAny(buffer, buffer.Length, rates.absorb, Flag.Zero);
        }
    }
}
