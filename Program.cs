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

#define TRACE

using Lib;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace Pop3cli
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(App.Banner);

            //AppTrace.Information(App.Title);
            AppTrace.Verbose(Environment.CommandLine);

            //if (args.Length == 0)// || Environment.CommandLine.Contains("?"))
            //Usage(null, 2);


            //Console.WriteLine("====================");
            //Console.WriteLine();

            //string src = Path.Combine(AppConfig.Get("Eml"), "3935?.eml");

            var settings = ConfigurationManager.AppSettings;

            switch (args.Length)
            {
                case 0:
                    {
                        string host = settings["Host"];

                        string src = settings["Src"].ExpandPath();
                        string bak = settings["Bak"].ExpandPath();
                        string dst = settings["Dst"].ExpandPath();

                        if (string.IsNullOrEmpty(host))
                        {
                            AppTrace.Information("Распаковка вложений");
                            ExtractAttachmentsFromFiles(src, dst);
                        }

                        else
                        {
                            AppTrace.Information("Загрузка сообщений");

                            int port = int.Parse(settings["Port"] ?? "110");
                            bool ssl = (settings["Ssl"] ?? "0").Equals("1");

                            string user = settings["User"];
                            string pass = settings["Pass"];

                            LoadMessages(host, port, ssl, user, pass, src, bak, dst);
                        }
                        break;
                    }

                case 1:
                    {
                        string arg = args[0];

                        if (arg.Equals("-?") || arg.Equals("/?"))
                        {
                            Usage();
                        }

                        else
                        {
                            string dst = settings["Dst"].ExpandPath();
                            AppTrace.Information($"Распаковка вложений из {arg}");
                            ExtractAttachmentsFromFiles(arg, dst);
                        }
                        break;
                    }

                case 2:
                    {
                        string arg1 = args[0];
                        string arg2 = args[1];

                        AppTrace.Information($"Распаковка вложений из {arg1} в {arg2}");
                        ExtractAttachmentsFromFiles(arg1, arg2);
                        break;
                    }

                default:
                    {
                        Usage();
                        break;
                    }
            }
#if DEBUG
            Console.WriteLine();
            Console.WriteLine("======== Press Enter to end program");
            Console.ReadLine();
#endif
            AppExit.Information("Сделано.");
        }

        static void Usage()
        {
            const string usage = "Параметры запуска:\n" +
                "0: Читать файл конфигурации и, если указан Host, загрузить файлы с этого POP3 сервера,\n" +
                "   если нет - просто распаковать все вложения из файлов по маске Src в папку Dst.\n\n" +
                "1: Если -? или /h - показать эту помощь, иначе - по указанной маске вместо Src в конфиге.\n\n" +
                "2: Использовать указанные параметры вместо Src и Dst в конфиге.";

            Console.WriteLine(usage);
            //AppExit.Information("О программе.");

            Environment.Exit(1);
        }

        private static void ExtractAttachmentsFromFiles(string src, string dst)
        {
            if (Directory.Exists(src))
            {
                string[] files = Directory.GetFiles(src);

                foreach (string file in files)
                {
                    Extract(null, file, dst);
                }
            }

            else if (src.Contains("*") || src.Contains("?"))
            {
                string path = Path.GetDirectoryName(src);

                if (!Directory.Exists(path))
                {
                    AppExit.Error($"{path} not exists!", 2);
                }

                string[] files = Directory.GetFiles(path, Path.GetFileName(src));

                foreach (string file in files)
                {
                    Extract(null, file, dst);
                }
            }

            else if (File.Exists(src))
            {
                Extract(null, src, dst);
            }

            else
            {
                AppExit.Error($"{src} not found!");
            }
        }

        private static void LoadMessages(string host, int port, bool ssl, string user, string pass, string src, string bak, string dst)
        {
            try
            {
                //prepare pop client
                var client = new Pop3.Pop3Client(host, port, ssl, user, pass)
                {
                    IsAutoReconnect = true
                };

                //remove the following line if no tracing is needed
                //client.Trace += new Pop3.TraceHandler(Console.WriteLine);
                client.Trace += new Pop3.TraceHandler(AppTrace.Verbose);
                client.ReadTimeout = 180000; //give server 180 seconds to answer

                //establish connection
                client.Connect();

                //get mailbox statistics
                client.GetMailboxStats(out int NumberOfMails, out int MailboxSize);

                //get a list of unique mail ids
                client.GetUniqueEmailIdList(out List<Pop3.EmailUid> EmailUids);

                //get email
                for (int i = 0; i < EmailUids.Count; i++)
                {
                    string path = Path.Combine(src, EmailUids[i].Uid + ".eml");

                    if (!File.Exists(path))
                    {
                        client.GetRawEmail(EmailUids[i].EmailId, out string text);
                        Extract(text, path, dst);
                    }
                }

                //cleanup server deleted email
                if (!Directory.Exists(bak))
                {
                    Directory.CreateDirectory(bak);
                }

                string[] files = Directory.GetFiles(src, "*.eml");

                foreach (string file in files)
                {
                    string filename = Path.GetFileNameWithoutExtension(file);
                    bool found = false;

                    foreach (var e in EmailUids)
                    {
                        if (e.Uid.Equals(filename))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        AppTrace.Verbose("{0} bak", filename);
                        File.Move(file, Path.Combine(bak, filename + ".eml"));
                    }
                }

                //close connection
                client.Disconnect();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Run Time Error Occured:")
                    .AppendLine(ex.Message)
                    .AppendLine(ex.StackTrace);
                AppTrace.Error(sb);
            }
        }

        private static void Extract(string text, string path, string folder)
        {
            var email = new EmailText(text);

            if (text == null)
            {
                email.Load(path);
            }

            else
            {
                email.Save(path);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[{Path.GetFileName(path)}]")
                .AppendLine($"  {email.From}, {email.Date:dd.MM HH:mm}")
                .AppendLine($"  {email.Subject}");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            foreach (var attach in email.Attachments)
            {
                sb.AppendLine($"  - {attach.Filename}");
                email.SaveAttachment(attach, folder);
            }

            AppTrace.Information(sb);
        }
    }
}
