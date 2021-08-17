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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;

namespace Lib
{
    public static class AppConfig
    {
        static readonly Dictionary<string, string> _baseDictionary = new Dictionary<string, string>();

        public static bool Modified = false;

        static AppConfig()
        {
            NameValueCollection settings = ConfigurationManager.AppSettings;

            foreach (string key in settings.AllKeys)
            {
                string value = settings[key];

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                Add(key, value);
            }
        }

        public static void Save(bool force = false)
        {
            if (Modified || force)
            {
                try
                {
                    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                    Modified = false;
                }
                catch (ConfigurationErrorsException)
                {
                    Console.WriteLine($"Error writing App settings.");
                }
            }
        }

        public static void Display()
        {
            Console.WriteLine();

            foreach (KeyValuePair<string, string> p in _baseDictionary)
            {
                Console.WriteLine("\t{0}\t = {1}", p.Key, p.Value);
            }

            Console.WriteLine();
        }

        public static void Add(string key, string value)
        {
            if (_baseDictionary.ContainsKey(key))
            {
                _baseDictionary[key] = value;
            }
            else
            {
                _baseDictionary.Add(key, value);
            }

            Modified = true;
        }

        public static void AddDefault(string key, string value)
        {
            if (_baseDictionary.ContainsKey(key))
            {
                return;
            }

            _baseDictionary.Add(key, value);
            Modified = true;
        }

        public static string Get(string key)
        {
            //return _baseDictionary[key]; // Exception if key does not exist!
            _baseDictionary.TryGetValue(key, out string value);

            return value;
        }

        public static void Set(string key, string value)
        {
            _baseDictionary[key] = value;
            Modified = true;
        }

        /// <summary>
        /// Expands the stored value with %Now%, %App% и %переменные_среды%
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetPath(string key)
        {
            string value = _baseDictionary[key];

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

        public static bool IsSet(string key, out string value)
        {
            return _baseDictionary.TryGetValue(key, out value);
        }
    }
}
