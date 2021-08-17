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

// based on an article https://www.codeproject.com/Articles/15611/POP3-Email-Client-with-full-MIME-Support-NET-2-0

// POP3 Email Client
// =================
//
// copyright by Peter Huber, Singapore, 2006
// this code is provided as is, bugs are probable, free for any use at own risk, no 
// responsibility accepted. All rights, title and interest in and to the accompanying content retained.  :-)
//
// based on Standard for ARPA Internet Text Messages, http://rfc.net/rfc822.html
// based on MIME Standard,  Internet Message Bodies, http://rfc.net/rfc2045.html
// based on MIME Standard, Media Types, http://rfc.net/rfc2046.html
// based on QuotedPrintable Class from ASP emporium, http://www.aspemporium.com/classes.aspx?cid=6

// based on MIME Standard, E-mail Encapsulation of HTML (MHTML), http://rfc.net/rfc2110.html
// based on MIME Standard, Multipart/Related Content-type, http://rfc.net/rfc2112.html

// ?? RFC 2557       MIME Encapsulation of Aggregate Documents http://rfc.net/rfc2557.html
#endregion

namespace Mime
{
    //character array 'constants' used for analysing POP3 / IMAP4 / MIME
    internal struct MimeChars
    {
        internal static readonly char[] BlankChars = { ' ' };
        internal static readonly char[] BracketChars = { '(', ')' };
        internal static readonly char[] ColonChars = { ':' };
        internal static readonly char[] CommaChars = { ',' };
        internal static readonly char[] EqualChars = { '=' };
        internal static readonly char[] ForwardSlashChars = { '/' };
        internal static readonly char[] SemiColonChars = { ';' };

        internal static readonly char[] WhiteSpaceChars = { ' ', '\t' };
        internal static readonly char[] NonValueChars = { '"', '(', ')' };
    }
}
