using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

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


        static public void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
        }



        //DirectoryInfo src = new DirectoryInfo(@"C:\temp");
        //DirectoryInfo dst = new DirectoryInfo(@"C:\temp3");
        ///*
        // * My example NCR.txt
        // *     *.txt
        // *     a.lbl
        // */
        //CopyFiles(src, dst, true);

        static void CopyFiles(DirectoryInfo source, DirectoryInfo destination, bool overwrite)
        {
            List<FileInfo> files = new List<FileInfo>();

            string[] fileNames = File.ReadAllLines("C:\\NCR.txt");

            foreach (string f in fileNames)
            {
                files.AddRange(source.GetFiles(f));
            }

            if (!destination.Exists)
                destination.Create();

            foreach (FileInfo file in files)
            {
                file.CopyTo(destination.FullName + @"\" + file.Name, overwrite);
            }
        }






        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }






        /// The common managed way to check whether a file is in use is to open the file in a try block. If the file is in use, it will throw an IOException.
        public bool IsFileLocked(string filename)
        {
            bool Locked = false;
            try
            {
                FileStream fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                fs.Close();
            }
            catch (IOException ex)
            {
                Locked = true;
            }
            return Locked;
        }


        /// <summary>Another way to check whether a file is in use is to call the CreateFile API. If a file is in use, the handle return is invalid.</summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName,
            FileSystemRights dwDesiredAccess, FileShare dwShareMode, IntPtr securityAttrs,
            FileMode dwCreationDisposition, FileOptions dwFlagsAndAttributes, IntPtr hTemplateFile);

        const int ERROR_SHARING_VIOLATION = 32;

        private bool IsFileInUse(string fileName)
        {
            bool inUse = false;

            SafeFileHandle fileHandle =
            CreateFile(fileName, FileSystemRights.Modify, FileShare.Write, IntPtr.Zero, FileMode.OpenOrCreate, FileOptions.None, IntPtr.Zero);

            if (fileHandle.IsInvalid)
            {
                if (Marshal.GetLastWin32Error() == ERROR_SHARING_VIOLATION)
                {
                    inUse = true;
                }
            }
            fileHandle.Close();
            return inUse;
        }

    }
}
