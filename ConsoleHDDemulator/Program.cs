using System;
using System.IO;

namespace ConsoleHDDemulator
{
    class Program
    {
        static void Main(string[] args)
        {
            DriveInfo[] dis = DriveInfo.GetDrives();
            try
            {
                foreach (DriveInfo di in dis)
                {
                    Console.WriteLine("Диск {0} имеется в системе и его тип {1}.", di.Name, di.DriveType);
                }

                Console.WriteLine(@"----------------------");

                foreach (DriveInfo di in dis)
                {
                    if (di.IsReady)
                    {
                        Console.WriteLine(
                            "Диск {0} есть в системе, метка \"{1}\", тип {2}, свободно {3}, всего {4}, формат {5}",
                            di.Name, di.VolumeLabel, di.DriveType, SizeSuffix(di.AvailableFreeSpace), SizeSuffix(di.TotalSize), di.DriveFormat);
                    }
                }

            }
            catch (IOException e)
            {
                Console.WriteLine(e.GetType().Name);
            }
            Console.ReadKey();
        }


        /// https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc
        /// 

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}
