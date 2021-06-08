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

namespace Lib
{
    public class AppConsoleListener : TraceListener
    {
        //private string _format;
        private readonly bool _stdErr;

        public AppConsoleListener(bool initializeData)
        {
            //    Dictionary<string, string> data = initializeData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            //        .Select(t => t.Split(new char[] { '=' }, 2))
            //        .ToDictionary(t => t[0].Trim(), t => t[1].TrimAnyQuotes(), StringComparer.InvariantCultureIgnoreCase);

            //    _format = data["Format"].Replace("%Now%", "0");

            _stdErr = initializeData;
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

            string format = Attributes["format"].Replace("%Now%", "0");

            if (message.Contains("Information"))
            {
                Console.ForegroundColor = ConsoleColor.White;
            }

            else if (message.Contains("Warning"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            else if (message.Contains("Error"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            //else Verbose

            Console.Write(format, DateTime.Now);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
            Console.ResetColor();
        }

        protected override string[] GetSupportedAttributes()
        {
            return new string[] { "format" };
        }
    }
}
