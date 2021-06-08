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
using System.Text;

namespace Lib
{
    /// <summary>
    /// Properties of the end
    /// </summary>
    public static class AppExit
    {
        /// <summary>
        /// Завершить приложение с указанными кодом [0] и информационным сообщением
        /// </summary>
        /// <param name="code">Код завершения [0]</param>
        /// <param name="msg">Текст информации</param>
        public static void Information(string msg = null, int code = 0)
        {
            AppTrace.Information(Message(msg, code));
            Exit(code);
        }

        /// <summary>
        /// Завершить приложение с указанными кодом [1] и предупреждающим сообщением
        /// </summary>
        /// <param name="code">Код завершения [1]</param>
        /// <param name="msg">Текст предупреждения</param>
        public static void Warning(string msg = null, int code = 1)
        {
            AppTrace.Warning(Message(msg, code));
            Exit(code);
        }

        /// <summary>
        /// Завершить приложение с указанными кодом [2] и сообщением об ошибке
        /// </summary>
        /// <param name="code">Код завершения [2]</param>
        /// <param name="msg">Текст ошибки</param>
        public static void Error(string msg = null, int code = 2)
        {
            AppTrace.Error(Message(msg, code));
            Exit(code);
        }

        /// <summary>
        /// Завершить приложение с указанными кодом [0]
        /// </summary>
        /// <param name="code">Код завершения</param>
        public static void Exit(int code = 0)
        {
            Trace.Close();
            Environment.Exit(code);
        }

        public static string Message(string msg = null, int code = 0)
        {
            StringBuilder sb = new StringBuilder();

            if (code > 0)
            {
                sb.Append("Exit ");
                sb.Append(code);
            }

            if (msg != null)
            {
                sb.Append(": ");
                sb.Append(msg);
            }

            return sb.ToString();
        }
    }
}
