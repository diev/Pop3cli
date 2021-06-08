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
using System.Text;

namespace Lib
{
    public static class Password
    {
        /// <summary>
        /// Decodes a Base64 string to a plain text string in a specified encoding.
        /// </summary>
        /// <param name="toDecode">The Base64 coded string to decode.</param>
        /// <param name="encResult">The encoding for the result text string: 0: ASCII, 65001: UTF8, 866: DOS, 1251: Windows, etc.</param>
        /// <returns>The plain text string ready to use.</returns>
        public static string Decode(this string toDecode, int encResult = 0)
        {
            if (toDecode.Contains("*"))
            {
                AppTrace.Warning("Звездочки вместо пароля.");
                throw new ArgumentException("Asteriks instead of password", "Password");
                //return toDecode;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(toDecode);

                if (encResult == 0)
                {
                    return Encoding.ASCII.GetString(bytes);
                }

                else
                {
                    Encoding encoding = Encoding.GetEncoding(encResult);
                    return encoding.GetString(bytes);
                }
            }

            catch (Exception e)
            {
                AppTrace.Error("Пароль поврежден.");
                throw new Exception("Password broken", e);
                //ex.MessageError of Base64:
                //Входные данные не являются действительной строкой Base64, поскольку содержат символ в кодировке,
                //отличной от Base64, больше двух символов заполнения или недопустимый символ среди символов заполнения.
            }
            //return string.Empty;
        }

        /// <summary>
        /// Encodes the string to Base64.
        /// </summary>
        /// <param name="toEncode">The source string to encode.</param>
        /// <returns>The string encoded to store.</returns>
        public static string Encode(this string toEncode)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(toEncode);
            return Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
        }
    }
}
