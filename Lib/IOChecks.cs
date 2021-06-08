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
using System.IO;

namespace Lib
{
    public static class IOChecks
    {
        #region Paths
        /// <summary>
        /// Checks if a directory exists. If not, tries to create.
        /// </summary>
        /// <param name="dir">A directory to check or create.</param>
        /// <returns>Returns the checked or created directory.</returns>
        public static string CheckDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (DirectoryNotFoundException)
                {
                    AppExit.Error("Ошибка создания директории " + dir);
                    //If wrong, creates in the App directory.
                    //AppTrace.Error("Директорию не создать:" + ex.Message);
                    //dir = Path.Combine(App.Dir, "_Recovery", Path.GetDirectoryName(dir));
                    //Directory.CreateDirectory(dir);
                    //AppTrace.Warning("Recovery directory {0} created.", dir);
                }
                catch (IOException)
                {
                    AppExit.Error("Нет диска для директории " + dir);
                    //AppTrace.Error("Сетевой диск не доступен: " + ex.Message);
                    //dir = Path.Combine(App.Dir, "_Recovery", Path.GetDirectoryName(dir));
                    //Directory.CreateDirectory(dir);
                    //AppTrace.Warning("Recovery directory {0} created.", dir);
                }
                catch (Exception ex)
                {
                    AppExit.Error(string.Concat("Другая ошибка создания директории ", dir, "\n", ex.Message));
                }
            }

            return dir;
        }

        /// <summary>
        /// Checks if a directory for the file exists. If not, tries to create.
        /// </summary>
        /// <param name="file">A file to check or create its directory.</param>
        /// <returns>Returns the checked or created directory.</returns>
        public static string CheckFileDirectory(string file)
        {
            string dir = Path.GetFullPath(file);
            string value = CheckDirectory(Path.GetDirectoryName(dir));

            return value;
        }
        #endregion Paths

        #region Rights
        /// <summary>
        /// Проверяет наличие прав на запись в папке.
        /// </summary>
        /// <param name="path">Путь к папке.</param>
        /// <param name="msg">Сообщение об ошибке.</param>
        public static void TestRights(string path, string msg)
        {
            string file = Path.Combine(path, Path.GetRandomFileName());
            try
            {
                CheckDirectory(path);
                File.WriteAllText(file, "Test " + file);
                File.Delete(file);
            }

            catch (Exception ex)
            {
                AppExit.Error(msg + ": " + ex.Message);
            }
        }
        #endregion Rights
    }
}
