using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using System.Dynamic;
using System.Runtime.CompilerServices;

namespace RTS
{
    public class Util
    {

        public static string ReadFile(string path, string fileName)
        {
            string content;

            using (StreamReader fs = new StreamReader(Path.Combine(path, fileName)))
            {
                content = fs.ReadToEnd();
            }

            return content;
        }

        public static void WriteToFile(string path, string fileName, string content)
        {
            using (FileStream fs = new FileStream(Path.Combine(path, fileName), FileMode.OpenOrCreate))
            {
                using (StreamWriter wfs = new StreamWriter(fs))
                {
                    wfs.Write(content);
                }
            }
        }

        public class Singleton<T> where T:class,new()
        {
            static T _self;
            public static T Get()
            {
                if (_self == null) _self = new T();
                return _self;
            }
        }
    }
}
