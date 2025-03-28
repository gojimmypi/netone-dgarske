using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NETtime.WinCE.Globals
{
    public static class Utility
    {
        private static string _localPath;

        public static string LocalPath
        {
            get
            {
                if (_localPath == null)
                {
                    _localPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
                }

                return _localPath;
            }
        }
        public static int TimeOffUnlimitedBalance = int.MaxValue;

        public delegate void Invokeable();

        public static void SafeEnqueue<T>(Queue<T> queue, T item)
        {
            lock (queue)
            {
                queue.Enqueue(item);
            }
        }

        public static T SafeDequeue<T>(Queue<T> queue)
        {
            lock (queue)
            {
                return queue.Dequeue();
            }
        }

        public static int SafeQueueCount<T>(Queue<T> queue)
        {
            lock (queue)
            {
                return queue.Count;
            }
        }

        public static void SafeInvoke(Control control, Invokeable invokeable)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(invokeable);
            }
            else
            {
                invokeable();
            }
        }

        public static void RemoveReadOnlyFromFile(string filePath)
        {
            try
            {
                new FileInfo(filePath).Attributes &= ~FileAttributes.ReadOnly;
            }
            catch
            {
            }
        }

        public static void RemoveReadOnlyFromDirectory(string directory)
        {
            try
            {
                foreach (string subdirectory in Directory.GetDirectories(directory))
                {
                    RemoveReadOnlyFromDirectory(subdirectory);
                }

                foreach (string filepath in Directory.GetFiles(directory))
                {
                    RemoveReadOnlyFromFile(filepath);
                }

                new DirectoryInfo(directory).Attributes &= ~FileAttributes.ReadOnly;
            }
            catch
            {
            }
        }

        public static void ExtractDependencyFile(string name, string path, string file, bool overwrite, bool restart)
        {
            if (path == "")
            {
                path = LocalPath;
            }

            string filePath = path + "\\" + file;
            bool fileExists = File.Exists(filePath);
            if (!overwrite && fileExists)
            {
                return;
            }

            Directory.CreateDirectory(path);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                fs.Write(buffer, 0, buffer.Length);
                fs.Close();
                stream.Close();
            }

           
        }

        public static RegistryKey GetNetTimeKey()
        {
            return Registry.LocalMachine.CreateSubKey("SOFTWARE").CreateSubKey("Microsoft").CreateSubKey("NETtime");
        }        
    }
}
