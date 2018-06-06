using System;
using System.Security.Cryptography;
using System.Text;

namespace StoreCrew.Utils.Security
{
    static class HexHashAssistant
    {
        public static string ComputeHash(string s, bool addSeparators = false)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            return ComputeHash(bytes, addSeparators);
        }

        public static string ComputeHash(byte[] bytes, bool addSeparators = false)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            MD5 md5 = new MD5CryptoServiceProvider();
            return GetHexString(md5.ComputeHash(bytes), addSeparators);
        }

        static string GetHexString(byte[] bytes, bool addSeparators = false)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                if (addSeparators && i > 0 && i % 2 == 0) builder.Append('-');

                byte b = bytes[i];
                int byteAsInt = (int)b;
                int hex1 = byteAsInt & 0xF;
                int hex2 = (byteAsInt >> 4) & 0xF;

                builder.Append(hex2 <= 9 ? (char)('0' + hex2) : (char)('A' + (hex2 - 10)));
                builder.Append(hex1 <= 9 ? (char)('0' + hex1) : (char)('A' + (hex1 - 10)));
            }

            return builder.ToString();
        }
    }
}
