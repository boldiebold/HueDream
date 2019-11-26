﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HueDream.Util {
    public static class ByteUtils {

        /// <summary>
        /// Convert an ASCII string and pad or truncate
        /// </summary>
        /// <returns>
        /// A byte array representing the padded/truncated string
        /// </returns>
        public static byte[] StringBytePad(string toPad, int len) {
            byte[] outBytes = Array.Empty<byte>();
            if (toPad != null) {
                string output = "";
                outBytes = new byte[len];
                if (toPad.Length > len) {
                    output = toPad.Substring(0, len);
                } else {
                    output = toPad;
                }
                System.Text.ASCIIEncoding encoding = new ASCIIEncoding();

                byte[] myBytes = encoding.GetBytes(output);
                for (int bb = 0; bb < len; bb++) {
                    if (bb < myBytes.Length) {
                        outBytes[bb] = myBytes[bb];
                    } else {
                        outBytes[bb] = 0;
                    }
                }
            } else {
                Console.WriteLine("NULL VALUE");
            }
            return outBytes;
        }

        public static IEnumerable<string> SplitHex(string str, int chunkSize) {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public static byte[] HexBytes(string input) {
            List<byte> output = new List<byte>();
            foreach (string hx in SplitHex(input, 2)) {
                output.Add(Convert.ToByte(hx, 16));
            }
            return output.ToArray();
        }

        /// <summary>
        /// Convert an integer to a byte
        /// </summary>
        /// <returns>
        /// A byte representation of the integer.
        /// </returns>
        public static byte IntByte(int toByte) {
            byte b = Convert.ToByte(toByte.ToString("X2"), 16);
            return b;
        }

        // Convert an array of integers to bytes
        public static byte[] IntBytes(int[] toBytes) {
            byte[] output = new byte[toBytes.Length];
            int c = 0;
            foreach (int i in toBytes) {
                output[c] = Convert.ToByte(i.ToString("X2"), 16);
                c++;
            }
            return output;
        }

        /// <summary>
        /// Convert an Hex string to it's integer representation
        /// </summary>
        /// <returns>
        /// The integer version of the hex string
        /// </returns>
        public static int HexInt(string intIn) {
            return int.Parse(intIn, System.Globalization.NumberStyles.HexNumber);
        }

        /// <summary>
        /// Convert a single hex string to it's byte representation
        /// </summary>
        /// <returns>
        /// A byte representing that value
        /// </returns>
        public static byte HexByte(string hexStr) {
            return Convert.ToByte(hexStr, 16);
        }

        /// <summary>
        /// Convert an arbitrary length String of hex characters with no spacing into it's ASCII representation
        /// </summary>
        /// <returns>
        /// An ASCII representation of the hex string
        /// </returns>
        public static string HexString(string hexString) {
            string sb = "";
            for (int i = 0; i < hexString.Length; i += 2) {
                string hs = hexString.Substring(i, 2);
                sb += HexChar(hs);
            }
            return sb;
        }

        /// <summary>
        /// Extract a hex string from a larger string from a known start and end position
        /// </summary>
        /// <returns>
        /// A string of hex-encoded values with no spacing
        /// </returns>
        public static string ExtractString(byte[] input, int start, int end) {
            int len = end - start;
            string strOut = "";
            if (len < input.Length) {
                byte[] subArr = new byte[len];
                Array.Copy(input, start, subArr, 0, len);

                foreach (byte b in subArr) {
                    strOut += Convert.ToChar(b);
                }
            } else {
                Console.WriteLine("Len for input request " + len + " is less than array len: " + input.Length);
            }
            return strOut;
        }

        public static int[] ExtractInt(byte[] input, int start, int end) {
            int len = end - start;
            int[] intOut = new int[len];
            if (len < input.Length) {
                byte[] subArr = new byte[len];
                Array.Copy(input, start, subArr, 0, len);
                Console.WriteLine("Copied. Len is " + len + ", subarr len is " + subArr.Length);
                int c = 0;

                foreach (byte b in subArr) {
                    intOut[c] = b;
                    c++;
                }
            } else {
                Console.WriteLine("Len for input request " + len + " is less than array len: " + input.Length);
            }
            Console.WriteLine("Returning: " + JsonConvert.SerializeObject(intOut));
            return intOut;
        }

        public static byte[] ExtractBytes(byte[] input, int start, int end) {
            int len = end - start;
            byte[] byteOut = new byte[len];
            if (len < input.Length) {
                byte[] subArr = new byte[len];
                Array.Copy(input, start, subArr, 0, len);

            } else {
                Console.WriteLine("Len for input request " + len + " is less than array len: " + input.Length);
            }
            return byteOut;
        }

        /// <summary>
        /// Um... Convert a hex string to ASCII. I think I need to figure out why I'm calling this from another method
        /// </summary>
        /// <returns>
        /// A byte array representing the padded/truncated string
        /// </returns>
        public static string HexChar(string hexString) {
            try {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2) {
                    string hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    uint decval = Convert.ToUInt32(hs, 16);
                    char character = Convert.ToChar(decval);
                    ascii += character;

                }

                return ascii;
            } catch (Exception ex) { Console.WriteLine(ex.Message); }

            return string.Empty;
        }

        

    }
}
