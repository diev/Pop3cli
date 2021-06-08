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
using System.Reflection;

namespace Lib
{
    //Extension methods must be defined in a static class
    public static class StringExtension
    {
        // This is the extension method.
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined.

        //public static string TrimAndReduce(this string str)
        //{
        //    return ConvertWhitespacesToSingleSpaces(str).Trim();
        //}

        //public static string ConvertWhitespacesToSingleSpaces(this string value)
        //{
        //    return Regex.Replace(value, @"\s+", " ");
        //}

        public static string TrimAnyQuotes(this string value)
        {
            string s = value.Trim();

            if (s[0] == '\'' || s[0] == '\"')
            {
                return s.Substring(1, s.Length - 2);
            }

            return s;
        }

        public static string ExpandPath(this string value)
        {
            if (value.Contains("{"))
            {
                string format = value.Replace("%Now%", "0").Replace("%App%", "1");
                value = string.Format(format, 
                    DateTime.Now, 
                    Assembly.GetCallingAssembly().GetName().Name);
            }

            if (value.Contains("%"))
            {
                value = Environment.ExpandEnvironmentVariables(value);
            }

            return value;
        }
    }
}
