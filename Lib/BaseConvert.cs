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
    public static class BaseConvert
    {
        #region Base36
        /// <summary>
        /// Таблица символов для перекодировки дат от 1 до 31 в один знак (Base36)
        /// </summary>
        public const string BASE36 = @"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Возвращает целое число из строки в кодировке Base36
        /// </summary>
        /// <param name="toDecode">Строка в кодировке Base36</param>
        /// <returns>Декодированное число</returns>
        public static int FromBase36(this string toDecode)
        {
            int result = 0;
            char[] tmp = toDecode.ToUpper().ToCharArray();
            
            for (int i = 0; i < tmp.Length; i++)
            {
                result = result * 36 + BASE36.IndexOf(tmp[i]);
            }

            return result;
        }

        /// <summary>
        /// Возвращает строку в кодировке Base36 для целого числа. Опционально дополняет нулями слева?
        /// </summary>
        /// <param name="toEncode">Число закодировать</param>
        /// <param name="padleft">Дополнить нулями слева</param>
        /// <returns>Закодированная строка</returns>
        public static string ToBase36(this int toEncode, int padleft = 0)
        {
            char[] tmp = new char[] { '0', '0', '0', '0', '0', '0', '0', '0', '0', '0' }; //10
            int i = tmp.Length, n = 0;

            while (toEncode > 0)
            {
                int remainder = toEncode % 36;
                toEncode /= 36;
                tmp[--i] = BASE36[remainder];
                n++;
            }

            if (padleft > 0)
            {
                i = tmp.Length - padleft;
                n = padleft;
            }

            return new string(tmp, i, n);
        }
        #endregion

        #region Base64
        /// <summary>
        /// Возвращает строку после преобразования из кодировки Base64
        /// </summary>
        /// <param name="toDecode">Строка Base64</param>
        /// <param name="enc">Кодировка (1251)</param>
        /// <returns>Текстовая строка</returns>
        public static string FromBase64(this string toDecode, int enc = 1251)
        {
            if (string.IsNullOrEmpty(toDecode))
            {
                return string.Empty;
            }

            else
            {
                byte[] bytes = Convert.FromBase64String(toDecode);
                Encoding encoding = Encoding.GetEncoding(enc);
                string value = encoding.GetString(bytes);

                return value;
            }
        }

        /// <summary>
        /// Возвращает строку в кодировке Base64
        /// </summary>
        /// <param name="toEncode">Текстовая строка</param>
        /// <returns>Строка Base64</returns>
        public static string ToBase64(this string toEncode)
        {
            if (string.IsNullOrEmpty(toEncode))
            {
                return string.Empty;
            }

            else
            {
                byte[] bytes = Encoding.ASCII.GetBytes(toEncode);
                string value = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);

                return value;
            }
        }
        #endregion

        #region Base128
        /// <summary>
        /// Генерация таблицы символов для Base36 и т.д. (уже нестандартно)
        /// </summary>
        /// <returns>Таблица символов для перекодировки от 1 до 128 в один знак</returns>
        public static string GetBase()
        {
            char[] c = new char[128];
            int k = 0;

            //Base10
            for (int i = 48; i <= 57; i++) //0..9 (10)
            {
                c[k++] = (char)i;
            }

            //Base36
            for (int i = 65; i <= 90; i++) //A..Z (+26)
            {
                c[k++] = (char)i;
            }

            //Base62
            for (int i = 97; i <= 122; i++) //a..z (+26)
            {
                c[k++] = (char)i;
            }

            //Base62+
            c[k++] = (char)95;
            for (int i = 33; i <= 47; i++) //!"#$%&'()*+,-./
            {
                c[k++] = (char)i;
            }
            for (int i = 58; i <= 64; i++) //:;<=>?@
            {
                c[k++] = (char)i;
            }
            for (int i = 91; i <= 94; i++) //[\]^
            {
                c[k++] = (char)i;
            }
            for (int i = 123; i <= 126; i++) //{|}~
            {
                c[k++] = (char)i;
            }
            c[k++] = (char)96;

            return new string(c, 0, k);
        }
        #endregion

        #region Bank
        /// <summary>
        /// Возвращает строку в рублях из суммы в копейках
        /// </summary>
        /// <param name="sumInKop">Сумма в копейках</param>
        /// <returns>Сумма в рублях</returns>
        public static string FromKopeek(long sumInKop)
        {
            string value = string.Format("{0:N0}.{1:00}", sumInKop / 100, Math.Abs(sumInKop) % 100);
            return value;
        }
        #endregion

        /// <summary>
        /// Конвертирует строку/текст списка с любыми разделителями в массив строк
        /// </summary>
        /// <param name="list">Строка/текст списка</param>
        /// <param name="array">Массив для строк</param>
        /// <returns>Число полученных элементов</returns>
        public static int ListToArray(string list, out string[] array)
        {
            char[] sep = new char[] { ' ', ',', ';', '\n', '\r', '\t' };
            array = list.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            int value = array.Length;

            return value;
        }
    }
}
