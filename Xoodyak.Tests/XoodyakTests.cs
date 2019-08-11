using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Xoodyak.Tests
{
    public class XoodyakTests
    {
        class HashKAT
        {
            public string msg = "";
            public string md = "";
        }

        [Fact]
        public void HashMode()
        {
            var url = "https://gist.github.com/nixberg/49e0389945c217fe535954597fe4a1a1/raw/74c5405e15a11bab4396d1472c78ae1b4d223f3e/hashkat.json";

            var json = string.Empty;
            using (var webClient = new System.Net.WebClient())
            {
                json = webClient.DownloadString(url);
            }
            var kats = JsonConvert.DeserializeObject<List<HashKAT>>(json);

            foreach (var kat in kats)
            {
                var msg = ParseHex(kat.msg);
                var md = ParseHex(kat.md);

                var msgStream = new MemoryStream(msg);
                var newMDStream = new MemoryStream((int)md.Length);

                var xooyak = new Xoodyak();
                xooyak.Absorb(msgStream, msg.Length);
                xooyak.Squeeze(newMDStream, md.Length);

                Assert.Equal(md.Length, newMDStream.Length);
                Assert.Equal(md, newMDStream.ToArray());
            }
        }

        class AeadKAT
        {
            public string key = "";
            public string nonce = "";
            public string pt = "";
            public string ad = "";
            public string ct = "";
        }

        [Fact]
        public void KeyedMode()
        {
            var url = "https://gist.github.com/nixberg/d14efa8c34dd4e88cbd85aeb18cb0777/raw/45715afd6a923d2ab3a4d60c90516de0540f757c/aeadkat.json";

            var json = string.Empty;
            using (var webClient = new System.Net.WebClient())
            {
                json = webClient.DownloadString(url);
            }
            var kats = JsonConvert.DeserializeObject<List<AeadKAT>>(json);

            foreach (var kat in kats)
            {
                var key = ParseHex(kat.key);
                var nonce = ParseHex(kat.nonce);
                var pt = ParseHex(kat.pt);
                var ad = ParseHex(kat.ad);
                var ct = ParseHex(kat.ct);
                var tag = new List<byte>(ct).GetRange((int)pt.Length, ct.Length - pt.Length).ToArray();

                // Encrypt:

                var nonceStream = new MemoryStream(ParseHex(kat.nonce));
                var ptStream = new MemoryStream(pt);
                var adStream = new MemoryStream(ad);
                var ctStream = new MemoryStream(ct);

                var newCTStream = new MemoryStream((int)ct.Length);

                var xooyak = new Xoodyak(key, null, null);
                xooyak.Absorb(nonceStream, nonce.Length);
                xooyak.Absorb(adStream, ad.Length);
                xooyak.Encrypt(ptStream, newCTStream, pt.Length);
                xooyak.Squeeze(newCTStream, tag.Length);

                Assert.Equal(ct.Length, newCTStream.Length);
                Assert.Equal(ct, newCTStream.ToArray());

                // Decrypt:

                nonceStream.Position = 0;
                ptStream.Position = 0;
                adStream.Position = 0;
                ctStream.Position = 0;

                var newPTStream = new MemoryStream((int)pt.Length);
                var newTagStream = new MemoryStream((int)tag.Length);

                xooyak = new Xoodyak(key, null, null);
                xooyak.Absorb(nonceStream, nonce.Length);
                xooyak.Absorb(adStream, ad.Length);
                xooyak.Decrypt(ctStream, newPTStream, pt.Length);
                xooyak.Squeeze(newTagStream, tag.Length);

                Assert.Equal(pt.Length, newPTStream.Length);
                Assert.Equal(pt, newPTStream.ToArray());

                Assert.Equal(tag.Length, newTagStream.Length);
                Assert.Equal(tag, newTagStream.ToArray());
            }
        }

        public static byte[] ParseHex(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Not a hex string", "hex");
            }

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int hi = ParseNibble(hex[i * 2]);
                int lo = ParseNibble(hex[i * 2 + 1]);
                bytes[i] = (byte)((hi << 4) | lo);
            }

            return bytes;
        }

        private static int ParseNibble(char c)
        {
            unchecked
            {
                uint i = (uint)(c - '0');
                if (i < 10)
                {
                    return (int)i;
                }
                i = ((uint)c & ~0x20u) - 'A';
                if (i < 6)
                {
                    return (int)i + 10;
                }
                throw new ArgumentException("Invalid nibble: " + c);
            }
        }
    }
}
