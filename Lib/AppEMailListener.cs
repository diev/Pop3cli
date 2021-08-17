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
    public class AppEMailListener : TraceListener
    {
        private string _to;

        public AppEMailListener(string initializeData)
        {
            //Dictionary<string, string> data = initializeData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            //    .Select(t => t.Split(new char[] { '=' }, 2))
            //    .ToDictionary(t => t[0].Trim(), t => t[1].TrimAnyQuotes(), StringComparer.InvariantCultureIgnoreCase);

            //_to = data["To"];
            _to = initializeData;
        }

        public override void Write(string message)
        {
            //string to = Attributes["to"];
            string subj = message.Contains("Information")
                ? "Information"
                : message.Contains("Warning")
                    ? "Warning"
                    : message.Contains("Error")
                        ? "Error"
                        : "Verbose";
            try
            {
                Mailer.Send(_to, subj, message);
            }
            catch (Exception ex)
            {
                // Не вышло - так не вышло
                Console.WriteLine(ex);
            }
        }

        public override void WriteLine(string message)
        {
            //string to = Attributes["to"];
            try
            {
                Mailer.Send(_to, message);
            }
            catch (Exception ex)
            {
                // Не вышло - так не вышло
                Console.WriteLine(ex);
            }
        }

        //protected override string[] GetSupportedAttributes()
        //{
        //    //return base.GetSupportedAttributes();
        //    return new string[] { "to" };
        //}
    }
}
