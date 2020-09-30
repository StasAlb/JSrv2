using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace JSrv2
{
    public static class JApplet
    {
        public static string CreateCapData(string filename)
        {
            var res = "";
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (GZipStream zip = new GZipStream(fs, CompressionMode.Decompress))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip.CopyTo(ms);
                        Console.WriteLine(ms.ToArray().ToString());
                    }
                }
                fs.Close();
            }
            return res;
            byte[] bytes = null;
            long length = 0, reslen = 0;
            using (StreamReader sr = new StreamReader(filename))
            {
                length = sr.BaseStream.Length;
                bytes = new byte[length];
                sr.BaseStream.Read(bytes, 0, (int)length);
                sr.Close();
            }

            long pos = 0;
            int[] index = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int[] len = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int[] order = new int[] { 1, 2, 4, 3, 6, 7, 8, 10, 5, 9, 11, 12 };
            int size, flen;
            string fname;
            do
            {
                if (Utils.Bin2AHex(bytes, (int) pos, 4).ToLower() != "504b0304")
                {
                    if (pos > 0)
                        break;
                    throw new Exception("ошибка заголовка cab");
                }

                pos += 6; //до битовой маски
                if ((bytes[pos] & 0x8) != 0) //не очень понимаю почему анализируется битовая маска, а не следующее поле compress, но так было в с++ версии
                {
                    do
                    {
                        pos++;
                    } while (Utils.Bin2AHex(bytes, (int)pos, 4).ToLower()!= "504b0304");
                    if (pos == length)
                        break;
                    else
                        continue;
                }
                pos += 12; //до размера
                size = Utils.GetInt(Utils.ReverseAHex(Utils.Bin2AHex(bytes, (int)pos, 4)));
                pos += 4; //сжатый размер
                pos += 4; //несжатый размер
                flen = Utils.GetInt(Utils.ReverseAHex(Utils.Bin2AHex(bytes, (int) pos, 2)));
                pos += 2; //длина имени файла
                pos += 2; //длина доп полей
                fname = Utils.Bin2String(bytes, (int)pos, flen).ToLower();
                if (fname.IndexOf("header.cap") == -1 && fname.IndexOf("directory.cap") == -1 &&
                    fname.IndexOf("import.cap") == -1 &&
                    fname.IndexOf("applet.cap") == -1 && fname.IndexOf("class.cap") == -1 &&
                    fname.IndexOf("method.cap") == -1 &&
                    fname.IndexOf("staticfield.cap") == -1 && fname.IndexOf("export.cap") == -1 &&
                    fname.IndexOf("constantpool.cap") == -1 &&
                    fname.IndexOf("reflocation.cap") == -1 && fname.IndexOf("descriptor.cap") == -1)
                {
                    pos += flen;
                    pos += size;
                    continue;
                }
                pos += flen;
                int tp = bytes[pos];
                if (tp > 0 && tp < 13)
                {
                    index[tp - 1] = (int)pos;
                    len[tp - 1] = size;
                }
                pos += size;

            } while (pos < length);

            for (int i = 0; i < 12; i++)
            {
                if (len[order[i]-1] > 0)
                {
                    res += Utils.Bin2AHex(bytes, index[order[i]-1], len[order[i]-1]);
                    reslen += len[order[i]-1];
                }
            }

            if (reslen <= 127)
                return $"C4{reslen:X2}{res}";
            if (reslen <= 255)
                return $"C481{reslen:X2}{res}";
            if (reslen <= 65535)
                return $"C482{reslen:X4}{res}";
            throw new Exception("too long data");
        }
    }
}
