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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Mime
{
    public class MimeParser
    {
        #region Private fields

        private readonly StreamReader _streamReader;

        //buffer used by every ProcessMimeEntity() to store MIME entity
        private readonly StringBuilder _mimeEntity = new StringBuilder(100000);

        #endregion Private fields

        #region Constructors

        public MimeParser(StreamReader streamReader)
        {
            _streamReader = streamReader;
        }

        #endregion Constructors

        #region Debug info

        /// <summary>
        /// Used for debugging. Collects all unknown header lines for all (!) emails received
        /// </summary>
        public static readonly bool CollectUnknowHeaderLines = false;

        /// <summary>
        /// List of all unknown header lines received, for all (!) emails 
        /// </summary>
        public static readonly List<string> AllUnknowHeaderLines = new List<string>();

        #endregion Debug info

        #region Public methods

        /// <summary>
        /// Reads the stream to get full Mime message
        /// </summary>
        /// <param name="message">Mime message</param>
        /// <returns>true if read correctly</returns>
        public bool GetEmail(out MimeMessage message)
        {
            //prepare message, set defaults as specified in RFC 2046
            //although they get normally overwritten, we have to make sure there are at least defaults
            message = new MimeMessage();
            //{
            //    ContentTransferEncoding = TransferEncoding.SevenBit,
            //    TransferType = "7bit"
            //};

            //convert received email into MimeMessage
            MimeEntityReturnCode messageMimeReturnCode = ProcessMimeEntity(message, "");

            if (messageMimeReturnCode == MimeEntityReturnCode.bodyComplete ||
                messageMimeReturnCode == MimeEntityReturnCode.parentBoundaryEndFound)
            {
                //TraceFrom($"email with {message.Body.Length} body chars received");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the message body from a nested, multipart MIME message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="useHTML">Use the HTML version of the body if available</param>
        /// <returns>The text of the message body</returns>
        public string GetMessageBody(MimeMessage message, bool useHTML)
        {
            string body = string.Empty; // return value

            if (string.IsNullOrEmpty(message.Body))
            {
                for (int i = message.Entities.Count - 1; i >= 0; i--)
                {
                    if (message.Entities[i].Body != string.Empty)
                    {
                        if ((useHTML == message.Entities[i].IsBodyHtml) || i == 0)
                        {
                            body = message.Entities[i].Body;
                            break;
                        }
                    }
                    else
                    {
                        body = GetMessageBody(message.Entities[i], useHTML);

                        if (body != string.Empty)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                body = message.Body;
            }

            return body;
        }
        
        /// <summary>
        /// Tries to convert a string into an email address
        /// </summary>
        public MailAddress ConvertToMailAddress(string address)
        {
            address = DecodeEncB(address).Trim();

            if (address == "<>")
            {
                //empty email address, not recognised a such by .NET
                return null;
            }

            try
            {
                if (address.Contains("\""))
                {
                    int position = address.IndexOf('<');
                    string name = address.Substring(0, position).Trim();
                    address = address.Remove(0, position).Trim();

                    return new MailAddress(address, name);
                }
                else
                {
                    return new MailAddress(address);
                }
            }
            catch
            {
                CallGetEmailWarning($"address format not recognised: '{address}'");
            }

            return null;
        }

        /// <summary>
        /// Tries to convert string to date, following POP3 rules
        /// If there is a run time error, the smallest possible date is returned
        /// <example>Wed, 04 Jan 2006 07:58:08 -0800</example>
        /// <example>Mon, 9 Aug 2021 12:36:47 +0300</example>
        /// </summary>
        public DateTime ConvertToDateTime(string date)
        {
            //DateTime returnDateTime;
            //try
            //{
            //    //sample: 'Wed, 04 Jan 2006 07:58:08 -0800 (PST)'
            //    //remove day of the week before ','
            //    //remove date zone in '()', -800 indicates the zone already

            //    //remove day of week
            //    string cleanDateTime = date;
            //    string[] dateSplit = cleanDateTime.Split(MimeChars.CommaChars, 2);

            //    if (dateSplit.Length > 1)
            //    {
            //        cleanDateTime = dateSplit[1];
            //    }

            //    //remove time zone (PST)
            //    dateSplit = cleanDateTime.Split(MimeChars.BracketChars);

            //    if (dateSplit.Length > 1)
            //    {
            //        cleanDateTime = dateSplit[0];
            //    }

            //    //convert to DateTime
            //    var dtStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;
            //    IFormatProvider culture = new CultureInfo("en-US", true);

            //    if (!DateTime.TryParse(cleanDateTime, culture, dtStyles, out returnDateTime))
            //    {
            //        //try just to convert the date
            //        int DateLength = cleanDateTime.IndexOf(':') - 3;
            //        cleanDateTime = cleanDateTime.Substring(0, DateLength);

            //        if (DateTime.TryParse(cleanDateTime, culture, dtStyles, out returnDateTime))
            //        {
            //            CallGetEmailWarning($"got only date, time format not recognised: '{date}'");
            //        }
            //        else
            //        {
            //            CallGetEmailWarning($"date format not recognised: '{date}'");
            //            System.Diagnostics.Debugger.Break();  //didn't have a sample email to test this

            //            return DateTime.MinValue;
            //        }
            //    }
            //}
            //catch
            //{
            //    CallGetEmailWarning($"date format not recognised: '{date}'");

            //    return DateTime.MinValue;
            //}

            //'Mon, 9 Aug 2021 12:36:47 +0300'
            if (!DateTime.TryParse(date, out DateTime returnDateTime))
            {
                return DateTime.MinValue;
            }

            return returnDateTime;
        }

        /// <summary>
        /// Removes leading '&lt;' and trailing '&gt;' if both exist
        /// </summary>
        /// <param name="parameterString"></param>
        /// <returns>String without &lt;&gt;</returns>
        public string RemoveBrackets(string parameterString)
        {
            if (parameterString == null)
            {
                return null;
            }

            if (parameterString.Length < 1 ||
                  parameterString[0] != '<' ||
                  parameterString[parameterString.Length - 1] != '>')
            {
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this

                return parameterString;
            }
            else
            {
                return parameterString.Substring(1, parameterString.Length - 2);
            }
        }

        /// <summary>
        /// Converts byte array to string, using decoding as requested
        /// </summary>
        public string DecodeByteArrayToString(byte[] byteArray, Encoding byteEncoding)
        {
            if (byteArray == null)
            {
                //no bytes to convert
                return null;
            }

            Decoder byteArrayDecoder;

            if (byteEncoding == null)
            {
                //no encoding indicated. Let's try UTF8
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                byteArrayDecoder = Encoding.UTF8.GetDecoder();
            }
            else
            {
                byteArrayDecoder = byteEncoding.GetDecoder();
            }

            int charCount = byteArrayDecoder.GetCharCount(byteArray, 0, byteArray.Length);
            char[] bodyChars = new char[charCount];
            _ = byteArrayDecoder.GetChars(byteArray, 0, byteArray.Length, bodyChars, 0);

            //convert char[] to string
            return new string(bodyChars);
        }

        /// <summary>
        /// Decodes national encoded strings in From, Subject, Name, etc. to readable strings
        /// </summary>
        /// <param name="s">Encoded string with "=?enc?b?...?="</param>
        /// <returns>Decoded readable string</returns>
        public string DecodeEncB(string s)
        {
            #region Solution 1
            //// decode "pelo=?ISO-8859-1?Q?usu=E1rio=2Exls?=" to "pelo usuário.xls"

            ///* types of strings received
            //Exemplo de arquivo XLS para download pelo usuário.xls
            //Exemplo de arquivo XLS para_download pelo=?ISO-8859-1?Q?usu=E1rio=2Exls?=
            //Exemplo de arquivo XLS para_downl?==?ISO-8859-1?Q?oad_pelo_usu=E1rio=2Exls?=
            //Cópia de Exemplo de arquivo XLS para download pelo usuário.xls
            //=?ISO-8859-1?Q?C=F3pia_de_Exemplo_de_arquivo_XLS_para_downl?==?ISO-8859-1?Q?oad_pelo_usu=E1rio=2Exls?=
            //teste de acentuação áàãâ ÁÀÃÂ çÇ 1
            //teste de =?ISO-8859-1?Q?acentua=E7=E3o_=E1=E0=E3=E2_=C1=C0=C3?= =?ISO-8859-1?Q?=C2_=E7=C7_1?=

            //three kinds of ?=
            //"?= " - end of 1o. segment
            //"?=C2_" - coincidence: ? and =C2
            //"?=" - end of last segment (not followed by space like end of 1o. segment)
            //*/

            //string decodedText = "";
            //int initEncodedSegment, endEncodedSegment;

            //while (true)
            //{
            //    if (s.Length == 0)
            //    {
            //        break;
            //    }
            //    else if (s.IndexOf("=?") == -1)
            //    {
            //        decodedText += s;
            //        break;
            //    }

            //    // get encoded segment
            //    initEncodedSegment = s.IndexOf("=?");

            //    if (s.IndexOf("?= ") > -1)
            //    {
            //        endEncodedSegment = s.IndexOf("?= ") + 3; // Segment is followed by text (encoded or not)
            //    }
            //    else
            //    {
            //        if (s.LastIndexOf("?=") > -1)
            //        {
            //            endEncodedSegment = s.LastIndexOf("?=") + 2; // Segment goes to the end of the text
            //        }
            //        else
            //        {
            //            endEncodedSegment = s.Length; // Malformed text
            //        }
            //    }

            //    if (initEncodedSegment > 0)
            //    { // get not encoded text before encoded segment
            //        decodedText += s.Substring(0, initEncodedSegment);
            //    }

            //    string originalSegment = s.Substring(initEncodedSegment, endEncodedSegment - initEncodedSegment);
            //    string encodedSegment;

            //    if (s.Length > endEncodedSegment)
            //    { // encoded segment ends before end of original text
            //        encodedSegment = s.Substring(initEncodedSegment + 2, endEncodedSegment - initEncodedSegment - 5); // get encoded segment without first =? and last ?=
            //    }
            //    else
            //    {
            //        encodedSegment = s.Substring(initEncodedSegment + 2, endEncodedSegment - initEncodedSegment - 4); // get encoded segment without first =? and last ?=
            //    }

            //    // remove text before encoded segment and encoded segment of S
            //    s = s.Substring(endEncodedSegment, s.Length - endEncodedSegment);

            //    encodedSegment = encodedSegment.Replace("_", " "); // the spaces in the original text are changed to underline

            //    string encodeName = encodedSegment.Substring(0, encodedSegment.IndexOf("?")); // get the encoding name - Ex: ISO-8859-1
            //    Encoding enc = Encoding.GetEncoding(encodeName);

            //    // Define the encoding type - B for base64, Q for QuotedPrintable
            //    char contentTransferEncoding = ' ';

            //    if (encodedSegment.IndexOf("?B?") > -1)
            //    {
            //        contentTransferEncoding = 'B';
            //    }
            //    else if (encodedSegment.IndexOf("?Q?") > -1)
            //    {
            //        contentTransferEncoding = 'Q';
            //    }

            //    // decode
            //    try
            //    {
            //        if (contentTransferEncoding == 'B')
            //        {
            //            encodedSegment = encodedSegment.Replace(encodeName + "?B?", "");
            //            byte[] decodedBytes = Convert.FromBase64String(encodedSegment);
            //            encodedSegment = enc.GetString(decodedBytes);
            //        }
            //        else if (contentTransferEncoding == 'Q')
            //        {
            //            encodedSegment = encodedSegment.Replace(encodeName + "?Q?", "");
            //            encodedSegment = QuotedPrintable.Decode(encodedSegment).Replace("\r\n", "");
            //        }
            //    }
            //    catch
            //    { // Malformed text
            //        encodedSegment = originalSegment;
            //    }

            //    decodedText += encodedSegment;
            //}

            //return decodedText;
            #endregion Solution 1

            #region Solution 2
            //if (s.StartsWith("=?") && s.EndsWith("?="))
            //{
            //    // if it is, get parts (=? encoding ? transferEncodingId ? subject ?=)
            //    string data = s.Substring(2, s.Length - 4);
            //    int indexOfSplit = data.IndexOf("?");

            //    // encoding
            //    string encoding = data.Substring(0, indexOfSplit);
            //    Encoding enc = Encoding.GetEncoding(encoding);

            //    // get type of encoding - B for base64, Q for QuotedPrintable
            //    char contentTransferEncoding = data[indexOfSplit + 1];

            //    // subject
            //    string subject = data.Substring(indexOfSplit + 3);

            //    // decode
            //    if (contentTransferEncoding == 'B')
            //    {
            //        byte[] decodedBytes = Convert.FromBase64String(subject);
            //        return enc.GetString(decodedBytes);
            //    }
            //    else if (contentTransferEncoding == 'Q')
            //    {
            //        return QuotedPrintable.Decode(subject).Replace("\r\n", "");
            //    }
            //}
            //else
            //{
            //    return s;
            //}
            #endregion Solution 2

            while (s.Contains("=?"))
            {
                int start = s.IndexOf("=?"); // "[...|=?enc?b?xxxx?=..."
                int system = s.IndexOf('?', start + 2); // "...=?[enc|?b?xxxx?=..."

                int end = s.IndexOf("?=", system + 3); // "...=?enc?b?[xxxx|?=..."
                int length = end + 2 - start;

                string ss = s.Substring(start, length); // "...[=?enc?b?xxxx?=]..."

                ss = Attachment.CreateAttachmentFromString("", ss).Name; //lifehack!

                s = s.Remove(start, length).Insert(start, ss); // "...[XXXX]..."
            }

            return s;
        }

        #endregion Public methods

        #region Private methods

        /// <summary>
        /// Process a MIME entity
        /// 
        /// A MIME entity consists of header and body.
        /// Separator lines in the body might mark children MIME entities
        /// </summary>
        private MimeEntityReturnCode ProcessMimeEntity(MimeMessage message, string parentBoundaryStart)
        {
            bool hasParentBoundary = parentBoundaryStart.Length > 0;
            string parentBoundaryEnd = parentBoundaryStart + "--";
            string response;

            MimeEntityReturnCode boundaryMimeReturnCode;

            //some format fields are inherited from parent, only the default for
            //ContentType needs to be set here, otherwise the boundary parameter would be
            //inherited too !
            message.SetContentTypeFields(); // "text/plain; us-ascii");

            #region Header

            string headerField = null;     //consists of one start line and possibly several continuation lines

            // read header lines until empty line is found (end of header)
            while (true)
            {
                if (!ReadMultiLine(out response))
                {
                    CallGetEmailWarning("incomplete MIME entity header read");

                    //empty this message
                    while (ReadMultiLine(out _)) { }

                    System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this

                    return MimeEntityReturnCode.problem;
                }

                if (response.Length == 0)
                {
                    //empty line found => end of header
                    if (headerField != null)
                    {
                        ProcessHeaderField(message, headerField);
                    }
                    else
                    {
                        //there was only an empty header.
                    }

                    break;
                }

                //check if there is a parent boundary in the header (wrong format!)
                if (hasParentBoundary &&
                    ParentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    CallGetEmailWarning("MIME entity header prematurely ended by parent boundary");

                    //empty this message
                    while (ReadMultiLine(out _)) { }

                    System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this

                    return boundaryMimeReturnCode;
                }

                //read header field
                //one header field can extend over one start line and multiple continuation lines
                //a continuation line starts with at least 1 blank (' ') or tab
                if (response[0] == ' ' || response[0] == '\t')
                {
                    //continuation line found.

                    if (headerField == null)
                    {
                        CallGetEmailWarning("Email header starts with continuation line");

                        //empty this message
                        while (ReadMultiLine(out _)) { }

                        System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this

                        return MimeEntityReturnCode.problem;
                    }
                    else
                    {
                        string s = response.TrimStart(MimeChars.WhiteSpaceChars);

                        // append space, if needed, and continuation line
                        if (headerField.EndsWith(" ") ||
                            (headerField.EndsWith("?=") && s.StartsWith("=?")))
                        {
                            //previous line did end with a whitespace
                            headerField += s;
                        }
                        else
                        {
                            //previous line did not end with a whitespace
                            //need to replace CRLF with a ' '
                            headerField += ' ' + s;
                        }
                    }
                }
                else //a new header field line found
                {
                    if (headerField == null)
                    {
                        //very first field, just copy it and then check for continuation lines
                        headerField = response;
                    }
                    else
                    {
                        //new header line found
                        ProcessHeaderField(message, headerField);

                        //save the beginning of the next line
                        headerField = response;
                    }
                }
            } //end while read header lines

            #endregion Header

            #region Body

            //empty StringBuilder. For speed reasons, reuse StringBuilder defined as member of class
            _mimeEntity.Length = 0;

            string boundaryDelimiterLineStart = null;
            bool isBoundaryDefined = false;

            if (message.ContentType.Boundary != null)
            {
                isBoundaryDefined = true;
                boundaryDelimiterLineStart = "--" + message.ContentType.Boundary;
            }

            //prepare return code for the case there is no boundary in the body
            boundaryMimeReturnCode = MimeEntityReturnCode.bodyComplete;

            //read body lines
            while (ReadMultiLine(out response))
            {
                //check if there is a boundary line from this entity itself in the body
                if (isBoundaryDefined && 
                    response.TrimEnd() == boundaryDelimiterLineStart)
                {
                    //boundary line found.
                    //stop the processing here and start a delimited body processing
                    return ProcessDelimitedBody(message, boundaryDelimiterLineStart, parentBoundaryStart, parentBoundaryEnd);
                }

                //check if there is a parent boundary in the body
                if (hasParentBoundary && 
                    ParentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, out boundaryMimeReturnCode))
                {
                    //a parent boundary is found. Decode the content of the body received so far, then end this MIME entity
                    //note that boundaryMimeReturnCode is set here, but used in the return statement
                    break;
                }

                //process next line
                _mimeEntity.AppendLine(response);
            }

            //a complete MIME body read
            //convert received US ASCII characters to .NET string (Unicode)
            string transferEncodedMessage = _mimeEntity.ToString();
            bool isAttachmentSaved = false;

            switch (message.ContentTransferEncoding)
            {
                case TransferEncoding.SevenBit:
                case TransferEncoding.EightBit:
                    if (message.MediaMainType == "application" || message.MediaMainType == "image")
                    {
                        //Content-Type: application/octet-stream;
                        //    name="file_name.txt"
                        //Content-Transfer-Encoding: 7bit

                        byte[] asciiBodyBytes = Encoding.ASCII.GetBytes(transferEncodedMessage);
                        message.ContentStream = new MemoryStream(asciiBodyBytes, false);
                        SaveAttachment(message);
                        isAttachmentSaved = true;
                    }
                    else
                    {
                        SaveMessageBody(message, transferEncodedMessage);
                    }
                    break;

                case TransferEncoding.Base64:
                    //convert base 64 -> byte[]
                    byte[] bodyBytes = Convert.FromBase64String(transferEncodedMessage);
                    message.ContentStream = new MemoryStream(bodyBytes, false);

                    if (message.MediaMainType == "text")
                    {
                        //convert byte[] -> string
                        message.Body = DecodeByteArrayToString(bodyBytes, message.BodyEncoding);

                    }
                    else if (message.MediaMainType == "application" || message.MediaMainType == "image")
                    {
                        SaveAttachment(message);
                        isAttachmentSaved = true;
                    }

                    break;

                case TransferEncoding.QuotedPrintable:
                    SaveMessageBody(message, QuotedPrintable.Decode(transferEncodedMessage));
                    break;

                default:
                    SaveMessageBody(message, transferEncodedMessage);
                    //no need to raise a warning here, the warning was done when analising the header
                    break;
            }

            if (message.ContentDisposition != null && 
                message.ContentDisposition.DispositionType.ToLowerInvariant() == "attachment" && !isAttachmentSaved)
            {
                SaveAttachment(message);
                //isAttachmentSaved = true;
            }

            #endregion Body

            return boundaryMimeReturnCode;
        }

        /// <summary>
        /// Convert one MIME header field and update message accordingly
        /// </summary>
        private void ProcessHeaderField(MimeMessage message, string headerField)
        {
            int separatorPosition = headerField.IndexOf(':');

            if (separatorPosition < 1)
            {
                // header field type not found, skip this line
                CallGetEmailWarning($"character ':' missing in header format field: '{headerField}'");
                return;
            }

            //process header field type
            string headerLineType = headerField.Substring(0, separatorPosition).ToLowerInvariant();
            string headerLineContent = headerField.Substring(separatorPosition + 1).Trim(MimeChars.WhiteSpaceChars);

            if (headerLineType == "" || headerLineContent == "")
            {
                //1 of the 2 parts missing, drop the line
                return;
            }

            // add header line to headers
            message.Headers.Add(headerLineType, headerLineContent);

            //interpret if possible
            switch (headerLineType)
            {
                case "bcc":
                    AddMailAddresses(headerLineContent, message.Bcc);
                    break;

                case "cc":
                    AddMailAddresses(headerLineContent, message.CC);
                    break;

                case "content-description":
                    message.ContentDescription = headerLineContent;
                    break;

                case "content-disposition":
                    message.ContentDisposition = new ContentDisposition(headerLineContent);
                    break;

                case "content-id":
                    message.ContentId = headerLineContent;
                    break;

                case "content-transfer-encoding":
                    message.TransferType = headerLineContent;
                    message.ContentTransferEncoding = ConvertToTransferEncoding(headerLineContent);
                    break;

                case "content-type":
                    message.SetContentTypeFields(headerLineContent);
                    break;

                case "date":
                    message.DeliveryDate = ConvertToDateTime(headerLineContent);
                    break;

                case "delivered-to":
                    message.DeliveredTo = ConvertToMailAddress(headerLineContent);
                    break;

                case "from":
                    MailAddress address = ConvertToMailAddress(headerLineContent);

                    if (address != null)
                    {
                        message.From = address;
                    }
                    break;

                case "message-id":
                    message.MessageId = headerLineContent;
                    break;

                case "mime-version":
                    message.MimeVersion = headerLineContent;
                    //message.BodyEncoding = new Encoding();
                    break;

                case "sender":
                    message.Sender = ConvertToMailAddress(headerLineContent);
                    break;

                case "subject":
                    message.Subject = DecodeEncB(headerLineContent);
                    break;

                case "received":
                    //throw mail routing information away
                    break;

                case "reply-to":
                    //message.ReplyTo = ConvertToMailAddress(headerLineContent);
                    AddMailAddresses(headerLineContent, message.ReplyToList);
                    break;

                case "return-path":
                    message.ReturnPath = ConvertToMailAddress(headerLineContent);
                    break;

                case "to":
                    AddMailAddresses(headerLineContent, message.To);
                    break;

                default:
                    message.UnknowHeaderlines.Add(headerField);

                    if (CollectUnknowHeaderLines)
                    {
                        AllUnknowHeaderLines.Add(headerField);
                    }
                    break;
            }
        }

        /// <summary>
        /// Find individual addresses in the string and add it to address collection
        /// </summary>
        /// <param name="Addresses">String with possibly several email addresses</param>
        /// <param name="AddressCollection">Parsed addresses</param>
        private void AddMailAddresses(string addresses, MailAddressCollection addressCollection)
        {
            MailAddress adr;
            try
            {
                string[] addressSplit = addresses.Split(',');

                foreach (string adrString in addressSplit)
                {
                    adr = ConvertToMailAddress(adrString);

                    if (adr != null)
                    {
                        addressCollection.Add(adr);
                    }
                }
            }
            catch
            {
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
            }
        }

        /// <summary>
        /// Converts TransferEncoding as defined in the RFC into a .NET TransferEncoding
        /// 
        /// .NET doesn't know the type "bit8". It is translated here into "bit7", which
        /// requires the same kind of processing (none).
        /// </summary>
        /// <param name="TransferEncodingString"></param>
        /// <returns></returns>
        private TransferEncoding ConvertToTransferEncoding(string transferEncodingString)
        {
            // here, "bit8" is marked as "bit7" (i.e. no transfer encoding needed)
            // "binary" is illegal in SMTP
            // something like "7bit" / "8bit" / "binary" / "quoted-printable" / "base64"
            switch (transferEncodingString.Trim().ToLowerInvariant())
            {
                case "7bit":
                    return TransferEncoding.SevenBit;

                case "8bit":
                    return TransferEncoding.EightBit;

                case "quoted-printable":
                    return TransferEncoding.QuotedPrintable;

                case "base64":
                    return TransferEncoding.Base64;

                case "binary":
                    throw new Exception("SMTP does not support binary transfer encoding");

                default:
                    CallGetEmailWarning($"not supported content-transfer-encoding: {transferEncodingString}");
                    return TransferEncoding.Unknown;
            }
        }

        private MimeEntityReturnCode ProcessDelimitedBody(MimeMessage message,
            string boundaryStart, string parentBoundaryStart, string parentBoundaryEnd)
        {
            if (boundaryStart.Trim() == parentBoundaryStart.Trim())
            {
                //Mime entity boundaries have to be unique
                CallGetEmailWarning($"new boundary same as parent boundary: '{parentBoundaryStart}'");

                //empty this message
                while (ReadMultiLine(out _)) { }

                return MimeEntityReturnCode.problem;
            }

            MimeEntityReturnCode ReturnCode;

            do
            {
                //empty StringBuilder
                _mimeEntity.Length = 0;

                MimeMessage ChildPart = message.CreateChildEntity();

                //recursively call MIME part processing
                ReturnCode = ProcessMimeEntity(ChildPart, boundaryStart);

                if (ReturnCode == MimeEntityReturnCode.problem)
                {
                    //it seems the received email doesn't follow the MIME specification. Stop here
                    return MimeEntityReturnCode.problem;
                }

                //add the newly found child MIME part to the parent
                AddChildPartsToParent(ChildPart, message);

            } 
            while (ReturnCode != MimeEntityReturnCode.parentBoundaryEndFound);

            //disregard all future lines until parent boundary is found or end of complete message
            bool hasParentBoundary = parentBoundaryStart.Length > 0;

            while (ReadMultiLine(out string response))
            {
                if (hasParentBoundary && 
                    ParentBoundaryFound(response, parentBoundaryStart, parentBoundaryEnd, 
                    out MimeEntityReturnCode boundaryMimeReturnCode))
                {
                    return boundaryMimeReturnCode;
                }
            }

            return MimeEntityReturnCode.bodyComplete;
        }

        /// <summary>
        /// Add all attachments and alternative views from child to the parent
        /// </summary>
        private void AddChildPartsToParent(MimeMessage child, MimeMessage parent)
        {
            //add the child itself to the parent
            parent.Entities.Add(child);

            //add the alternative views of the child to the parent
            if (child.AlternateViews != null)
            {
                foreach (AlternateView childView in child.AlternateViews)
                {
                    parent.AlternateViews.Add(childView);
                }
            }

            //add the body of the child as alternative view to parent
            //this should be the last view attached here, because the POP 3 MIME client
            //is supposed to display the last alternative view
            if (child.MediaMainType == "text" && 
                child.ContentStream != null &&
                child.Parent.ContentType != null &&
                child.Parent.ContentType.MediaType.ToLowerInvariant() == "multipart/alternative")
            {
                parent.AlternateViews.Add(new AlternateView(child.ContentStream)
                {
                    ContentId = RemoveBrackets(child.ContentId),
                    ContentType = child.ContentType,
                    TransferEncoding = child.ContentTransferEncoding
                });
            }

            //add the attachments of the child to the parent
            if (child.Attachments != null)
            {
                foreach (Attachment childAttachment in child.Attachments)
                {
                    parent.Attachments.Add(childAttachment);
                }
            }
        }

        /// <summary>
        /// Check if the response line received is a parent boundary 
        /// </summary>
        private bool ParentBoundaryFound(string response, string parentBoundaryStart, string parentBoundaryEnd, out MimeEntityReturnCode boundaryMimeReturnCode)
        {
            boundaryMimeReturnCode = MimeEntityReturnCode.undefined;

            if (response == null || response.Length < 2 || response[0] != '-' || response[1] != '-')
            {
                //quick test: reponse doesn't start with "--", so cannot be a separator line
                return false;
            }

            if (response == parentBoundaryStart)
            {
                boundaryMimeReturnCode = MimeEntityReturnCode.parentBoundaryStartFound;
                return true;
            }
            else if (response == parentBoundaryEnd)
            {
                boundaryMimeReturnCode = MimeEntityReturnCode.parentBoundaryEndFound;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Copies the content found for the MIME entity to the MimeMessage body and creates
        /// a stream which can be used to create attachements, alternative views, ...
        /// </summary>
        private void SaveMessageBody(MimeMessage message, string contentString)
        {
            message.Body = contentString;

            MemoryStream bodyStream = new MemoryStream();
            StreamWriter bodyStreamWriter = new StreamWriter(bodyStream);

            bodyStreamWriter.Write(contentString);
            //_ = contentString.Length;

            bodyStreamWriter.Flush();
            message.ContentStream = bodyStream;
        }

        /// <summary>
        /// Each attachement is stored in its own MIME entity and read into this entity's
        /// ContentStream. SaveAttachment creates an attachment out of the ContentStream
        /// and attaches it to the parent MIME entity.
        /// </summary>
        private void SaveAttachment(MimeMessage message)
        {
            if (message.Parent == null)
            {
                System.Diagnostics.Debugger.Break(); //didn't have a sample email to test this
                return;
            }

            Attachment attachment = new Attachment(message.ContentStream, message.ContentType);

            //no idea why ContentDisposition is read only. on the other hand, it is anyway redundant
            if (message.ContentDisposition != null)
            {
                ContentDisposition messageContentDisposition = message.ContentDisposition;
                ContentDisposition attachmentContentDisposition = attachment.ContentDisposition;

                if (messageContentDisposition.CreationDate > DateTime.MinValue)
                {
                    attachmentContentDisposition.CreationDate = messageContentDisposition.CreationDate;
                }

                attachmentContentDisposition.DispositionType = messageContentDisposition.DispositionType;
                attachmentContentDisposition.FileName = messageContentDisposition.FileName;
                attachmentContentDisposition.Inline = messageContentDisposition.Inline;

                if (messageContentDisposition.ModificationDate > DateTime.MinValue)
                {
                    attachmentContentDisposition.ModificationDate = messageContentDisposition.ModificationDate;
                }

                attachmentContentDisposition.Parameters.Clear();

                if (messageContentDisposition.ReadDate > DateTime.MinValue)
                {
                    attachmentContentDisposition.ReadDate = messageContentDisposition.ReadDate;
                }

                if (messageContentDisposition.Size > 0)
                {
                    attachmentContentDisposition.Size = messageContentDisposition.Size;
                }

                foreach (string key in messageContentDisposition.Parameters.Keys)
                {
                    if (!attachmentContentDisposition.Parameters.ContainsKey(key))
                    {
                        attachmentContentDisposition.Parameters.Add(key, messageContentDisposition.Parameters[key]);
                    }
                }
            }

            //get ContentId
            string contentIdString = message.ContentId;

            if (contentIdString != null)
            {
                attachment.ContentId = RemoveBrackets(contentIdString);
            }

            attachment.TransferEncoding = message.ContentTransferEncoding;

            attachment.Name = DecodeEncB(attachment.Name);
            attachment.NameEncoding = Encoding.UTF8;

            message.Parent.Attachments.Add(attachment);
        }

        /// <summary>
        /// Read one line in multiline mode from a message file. 
        /// </summary>
        /// <param name="response">Line received</param>
        /// <returns>false: end of message</returns>
        private bool ReadMultiLine(out string response)
        {
            response = _streamReader.ReadLine();

            if (response == null)
            {
                CallGetEmailWarning("incomplete MIME text read");
                return false;
            }

            //check for byte stuffing, i.e. if a line starts with a '.', another '.' is added, unless
            //it is the last line
            if (response.Length > 0 && response[0] == '.')
            {
                if (response == ".")
                {
                    //closing line found
                    return false;
                }

                //remove the first '.'
                response = response.Substring(1, response.Length - 1);
            }

            return true;
        }

        private void CallGetEmailWarning(string warningText)
        {
            //CallWarning("GetEmail", "",
            //    $"Problem Email file {_filename}: {warningText}");
            Console.WriteLine($"Problem Email: {warningText}");
        }

        #endregion Private methods
    }
}
