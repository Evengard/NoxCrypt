/* 
 * Copyright (C) 2012 NoxForum.net <http://www.noxforum.net/>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoxCrypt
{
    public class NoxCrypt
    {
        public enum FileType
        {
            Player,
            Map,
            Thing,
            Modifier,
            Gamedata,
            Monster,
            Soundset
        };

        public static void EncryptBitwise(byte[] data)
        {
            // 128,64,32,16,8,4,2,1
            byte bittester = 0;
            byte val2 = 0;
            byte bitcount = 0;
            byte bitloc = 0;
            byte bitloc2 = 0;

            byte[] temdata = new byte[data.Length];
            Array.Copy(data, temdata, data.Length);

            // Set 1st 7 bits, write NULL bit 8, set bit 9
            for (int i=0; i<data.Length-0x09; i++) //dataLen - 9
            {
                for(int j=0; j<8; j++)
                {
                    if (bitloc == 8)
                    {
                        bitloc = 0;
                        bittester++;
                    }

                    if (bitloc2 == 7)
                    {
                        val2 ^= 1 << 7;
                        data[bitcount] = val2;
                        bitcount++;
                        val2 = 0;
                        bitloc2 = 0;
                    }

                    val2 ^= (byte)(((temdata[bittester] & (1 << bitloc))>>bitloc) << bitloc2);
                    bitloc++;
                    bitloc2++;
                }	
            }
        }

        public static void DecryptBitwise(byte[] data)
        {
            // 128,64,32,16,8,4,2,1
            uint bittester = 0;
            byte val2 = 0;
            byte bitcount = 0;
            byte bitloc = 0;

            // get 1st 7 bits, skip 8, get 9
            for(int i=0; i<data.Length-0x09; i++)
            {
                val2=0;
                for(int j=0; j<8; j++)
                {
                    if (bitcount == 7)
                    {
                        bitcount = 0;
                        bitloc++;
                    }

                    if (bitloc == 8)
                    {
                        data[bittester] = 0;
                        bittester++;
                        bitloc = 0;
                    }

                    val2 ^= (byte)(((data[bittester] & (1 << bitloc))>>bitloc) << j);
                    bitloc++;
                    bitcount++;
                }

                data[i] = val2;
            }
        }

        public static void Encrypt(byte[] data, FileType filetype)
        {
            Crypt(data, filetype, true);
        }

        public static void Decrypt(byte[] data, FileType filetype)
        {
            Crypt(data, filetype, false);
        }

        private static void Crypt(byte[] data, FileType filetype, bool encrypt)
        {
            uint[] table = null;

            switch (filetype)
            {
                case FileType.Player: table = Tables.player; break;
                case FileType.Map: table = Tables.map; break;
                case FileType.Thing: table = Tables.thing; break;
                case FileType.Modifier: table = Tables.modifier; break;
                case FileType.Gamedata: table = Tables.gamedata; break;
                case FileType.Monster: table = Tables.monster; break;
                case FileType.Soundset: table = Tables.soundset; break;
            }

            int taOffset = encrypt ? 0x400 : 0x418;
            uint part1, part2;

            for (int dataIdx = 0; dataIdx < data.Length; dataIdx += 8)
            {
                int offset = taOffset;
                part1 = (uint)((data[dataIdx+0] << 24) | (data[dataIdx+1] << 16) | (data[dataIdx+2] << 8) | data[dataIdx+3]);
                part2 = (uint)((data[dataIdx+4] << 24) | (data[dataIdx+5] << 16) | (data[dataIdx+6] << 8) | data[dataIdx+7]);

                for (int i = 0; i < 8; i++)
                {
                    uint sum = 0;

                    part1 ^= table[offset++];
                    sum += table[0x000 + ((part1 >> 24) & 0xFF)];
                    sum += table[0x100 + ((part1 >> 16) & 0xFF)];
                    sum ^= table[0x200 + ((part1 >> 8) & 0xFF)];
                    sum += table[0x300 + ((part1 >> 0) & 0xFF)];

                    sum ^= table[offset++];
                    part2 ^= sum;

                    sum = 0;
                    sum += table[0x000 + ((part2 >> 24) & 0xFF)];
                    sum += table[0x100 + ((part2 >> 16) & 0xFF)];
                    sum ^= table[0x200 + ((part2 >> 8) & 0xFF)];
                    sum += table[0x300 + ((part2 >> 0) & 0xFF)];
                    part1 ^= sum;
                }

                part1 ^= table[offset++];
                part2 ^= table[offset];

                data[dataIdx + 0] = ((byte)((part2 >> 24) & 0xFF));
                data[dataIdx + 1] = ((byte)((part2 >> 16) & 0xFF));
                data[dataIdx + 2] = ((byte)((part2 >> 8) & 0xFF));
                data[dataIdx + 3] = ((byte)((part2 >> 0) & 0xFF));
                data[dataIdx + 4] = ((byte)((part1 >> 24) & 0xFF));
                data[dataIdx + 5] = ((byte)((part1 >> 16) & 0xFF));
                data[dataIdx + 6] = ((byte)((part1 >> 8) & 0xFF));
                data[dataIdx + 7] = ((byte)((part1 >> 0) & 0xFF));
            }
        }
    }
}