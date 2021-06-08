#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Source https://github.com/diev/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//------------------------------------------------------------------------------
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lib
{
    public class AppLogFileListener : TraceListener
    {
        private readonly string _fileName;
        //private readonly string _format;

        //private readonly string _verbose;
        //private readonly string _information;
        //private readonly string _warning;
        //private readonly string _error;

        public AppLogFileListener(string initializeData)
        {
            //Dictionary<string, string> data = initializeData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            //    .Select(t => t.Split(new char[] { '=' }, 2))
            //    .ToDictionary(t => t[0].Trim(), t => t[1].TrimAnyQuotes(), StringComparer.InvariantCultureIgnoreCase);

            //string value;

            //if (data.TryGetValue("FileName", out value))
            //{
            //    _fileName = value.ExpandPath();
            //}
            ////TODO else

            //if (data.TryGetValue("Format", out value))
            //{
            //    _format = value.Replace("%Now%", "0").Replace("%Lvl%", "1");
            //}

            //if (data.TryGetValue("Verbose", out value))
            //{
            //    _verbose = value;
            //}

            //if (data.TryGetValue("Information", out value))
            //{
            //    _information = value;
            //}

            //if (data.TryGetValue("Warning", out value))
            //{
            //    _warning = value;
            //}

            //if (data.TryGetValue("Error", out value))
            //{
            //    _error = value;
            //}

            _fileName = initializeData.ExpandPath();
            string path = Path.GetDirectoryName(_fileName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public override void Write(string message)
        {
            //foreach (DictionaryEntry entry in this.Attributes)
            //{
            //    if (entry.Key.ToString().ToLower().Equals("format"))
            //    {
            //        _format = entry.Value.ToString();
            //        break;
            //    }
            //}

            string dt;
            string format = Attributes["format"].Replace("%Now%", "0").Replace("%Lvl%", "1");

            if (message.Contains("Information"))
            {
                dt = string.Format(format, DateTime.Now, Attributes["information"]);
            }

            else if (message.Contains("Warning"))
            {
                dt = string.Format(format, DateTime.Now, Attributes["warning"]);
            }

            else if (message.Contains("Error"))
            {
                dt = string.Format(format, DateTime.Now, Attributes["error"]);
            }

            else // Verbose
            {
                dt = string.Format(format, DateTime.Now, Attributes["verbose"]);
            }

            File.AppendAllText(_fileName, dt, Encoding.GetEncoding(1251));
        }

        public override void WriteLine(string message)
        {
            File.AppendAllText(_fileName, message + Environment.NewLine, Encoding.GetEncoding(1251));
        }

        protected override string[] GetSupportedAttributes()
        {
            return new string[] { "format", "information", "verbose", "warning", "error" };
        }
    }
}
