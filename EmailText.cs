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
using System.IO;

namespace Pop3cli
{
    public struct Attachment
    {
        public string Filename;
        public int Start;
        public int Length;
    }

    class EmailText
    {
        private const string CRLF = "\r\n"; // перевод строки
        private const string LF2 = "\r\n\r\n"; // пустая строка (разделитель)

        public string Text;

        public string From;

        public string Subject;

        public DateTime Date;

        public List<Attachment> Attachments = new List<Attachment>();

        public EmailText(string text)
        {
            ParseText(text);
        }

        private void ParseText(string text)
        {
            Text = text;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string header = text.Substring(0, text.IndexOf(LF2) + 4).Replace('\t', ' ');

            From = GetField("From:");

            Subject = GetField("Subject:");

            DateTime.TryParse(GetField("Date:"), out Date);

            FindAttachments();

            string GetField(string field)
            {
                string value = "";
                int pos = header.IndexOf(CRLF + field);

                if (pos > -1)
                {
                    pos += field.Length + 2;
                    int pos2 = header.IndexOf(CRLF, pos);

                    while (header[pos2 + 2] == ' ')
                    {
                        pos2 = header.IndexOf(CRLF, pos2 + 3);
                    }

                    value = Decode(header.Substring(pos, pos2 - pos));
                }

                return value;
            }

            void FindAttachments()
            {
                int pos;
                int pos2 = header.Length;

                while ((pos = text.IndexOf(CRLF + "Content-Disposition: attachment;", pos2, StringComparison.OrdinalIgnoreCase)) > -1)
                {
                    pos = text.IndexOf("filename=\"", pos + 32, StringComparison.OrdinalIgnoreCase) + 10;
                    pos2 = text.IndexOf('\"', pos);
                    string filename = Decode(text.Substring(pos, pos2 - pos).Replace('\t', ' '));

                    pos = text.IndexOf(LF2, pos2) + 4;
                    pos2 = text.IndexOf(LF2, pos);

                    Attachments.Add(new Attachment()
                    {
                        Filename = filename,
                        Start = pos,
                        Length = pos2 - pos
                    });
                }
            }
        }

        public void Load(string path)
        {
            ParseText(File.ReadAllText(path));
        }

        public void Save(string path)
        {
            string folder = Path.GetDirectoryName(path);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(path, Text);
        }

        public string Decode(string s)
        {
            s = s.TrimStart().Replace("?=\r\n =?", "?==?").Replace(CRLF, "");

            while (s.Contains("=?"))
            {
                int p1 = s.IndexOf("=?"); //=?enc?b?...?=
                int p2 = s.IndexOf('?', p1 + 2);
                p2 = s.IndexOf("?=", p2 + 3);
                string str = s.Substring(p1, p2 + 2 - p1);

                str = System.Net.Mail.Attachment.CreateAttachmentFromString("", str).Name; // lifehack

                s = s.Remove(p1, p2 + 2 - p1).Insert(p1, str);
            }

            return s;
        }

        public void SaveAttachment(Attachment attach, string path)
        {
            byte[] bytes = Convert.FromBase64String(Text.Substring(attach.Start, attach.Length));
            File.WriteAllBytes(Path.Combine(path, attach.Filename), bytes);
        }
    }
}
