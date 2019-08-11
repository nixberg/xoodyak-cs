using Xunit;

namespace Xoodyak.Tests
{
    public class XoodooTests
    {
        [Fact]
        public void ItWorks()
        {
            var xoodoo = new Xoodoo();

            for (int i = 0; i < 384; i++)
            {
                xoodoo.Permute();
            }

            uint[] expected = {
                0xfe04fab0, 0x42d5d8ce, 0x29c62ee7, 0x2a7ae5cf,
                0xea36eba3, 0x14649e0a, 0xfe12521b, 0xfe2eff69,
                0xf1826ca5, 0xfc4c41e0, 0x1597394f, 0xeb092faf
            };

            Assert.Equal(xoodoo.state, expected);
        }

        [Fact]
        public void Subscript()
        {
            var xoodoo = new Xoodoo();

            for (int i = 0; i < 48; i++)
            {
                xoodoo.XOR(i, (byte)i);
                Assert.Equal(xoodoo[i], (byte)i);
            }
        }
    }
}
